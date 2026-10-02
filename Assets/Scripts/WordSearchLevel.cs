using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WordSearchLevel : MonoBehaviour
{
    [Header("Level Information")]
    [SerializeField] private int levelNumber = 1;
    [SerializeField] private string levelTitle = "FRUITS";
    [SerializeField] private TextMeshProUGUI levelTitleText;

    [Header("Level References")]
    [SerializeField] private List<WordTarget> targetWords = new List<WordTarget>();
    [SerializeField] private List<LetterTile> letterTiles = new List<LetterTile>();

    [Header("Selection Lines")]
    [SerializeField] private List<WordSelectionLine> selectionLines = new List<WordSelectionLine>();

    [Header("Line Colors")]
    [SerializeField]
    private List<Color> lineColors = new List<Color>
    {
        new Color(0.29f, 0.56f, 0.89f, 0.8f), // Blue
        new Color(0.93f, 0.51f, 0.98f, 0.8f), // Purple
        new Color(0.02f, 0.84f, 0.63f, 0.8f), // Green
        new Color(1.00f, 0.62f, 0.11f, 0.8f), // Orange
        new Color(0.95f, 0.26f, 0.26f, 0.8f)  // Red
    };

    [Header("Wrong Selection")]
    [SerializeField] private float wrongShakeDuration = 0.25f;

    [Header("UI Panels & Buttons")]
    [SerializeField] private GameObject levelCompletePanel;
    [SerializeField] private Button nextButton;

    [Header("Tutorial Hint Settings")]
    [SerializeField] private bool enableTutorialHint = false;
    [SerializeField] private WordTarget tutorialHintWord;

    public int LevelNumber => levelNumber;
    public string LevelTitle => levelTitle;
    public Button NextButton => nextButton;

    private readonly List<LetterTile> currentPath = new List<LetterTile>();
    private readonly Dictionary<(int, int), LetterTile> tileGrid = new Dictionary<(int, int), LetterTile>();

    private bool isSelecting;
    private bool hintActive;
    private bool wrongAnimationPlaying;

    private WordSelectionLine activeLine;
    private int activeLineIndex = -1;

    // --- Smooth diagonal selection ---
    private LetterTile startTile;
    private Camera uiCamera; // stays null for Screen Space - Overlay canvas

    // x = row step, y = column step (8 directions)
    private static readonly Vector2Int[] Directions =
    {
        new Vector2Int( 0,  1), new Vector2Int( 0, -1),
        new Vector2Int( 1,  0), new Vector2Int(-1,  0),
        new Vector2Int( 1,  1), new Vector2Int( 1, -1),
        new Vector2Int(-1,  1), new Vector2Int(-1, -1)
    };

    private void Awake()
    {
        InitializeLevel();
    }

    private void OnEnable()
    {
        InitializeLevel();

        if (enableTutorialHint)
        {
            ShowTutorialHint();
        }
    }

    private void Update()
    {
        if (!isSelecting || wrongAnimationPlaying || startTile == null) return;

        // Pointer position (touch on mobile, mouse in editor)
        Vector2 pointer;
        bool released;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            pointer = touch.position;
            released = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
        }
        else
        {
            pointer = Input.mousePosition;
            released = Input.GetMouseButtonUp(0);
        }

        UpdateSelectionFromPointer(pointer);

        if (released)
        {
            FinishSelection();
        }
    }

    public void InitializeLevel()
    {
        Debug.Log($"[WordSearchLevel] Initializing Level {levelNumber} - {levelTitle}");

        isSelecting = false;
        hintActive = false;
        wrongAnimationPlaying = false;

        currentPath.Clear();
        activeLine = null;
        activeLineIndex = -1;
        startTile = null;

        if (levelTitleText != null)
        {
            levelTitleText.text = levelTitle;
        }

        // Build Grid dictionary
        tileGrid.Clear();
        foreach (LetterTile tile in letterTiles)
        {
            if (tile == null) continue;

            tile.InitTile(this);
            tile.ResetState();

            var key = (tile.Row, tile.Column);
            if (tileGrid.ContainsKey(key))
            {
                Debug.LogError($"[WordSearchLevel] Duplicate grid position: Row {tile.Row} Column {tile.Column} on Level {levelNumber}");
            }
            else
            {
                tileGrid.Add(key, tile);
            }
        }

        Debug.Log($"[WordSearchLevel] Level {levelNumber} grid contains {tileGrid.Count} unique tiles.");

        foreach (WordTarget target in targetWords)
        {
            if (target != null)
            {
                target.SetCompleted(false);
            }
        }

        foreach (WordSelectionLine line in selectionLines)
        {
            if (line != null)
            {
                line.Hide();
            }
        }

        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(false);
        }
    }

    public void SetSharedCompletePanel(GameObject panel)
    {
        levelCompletePanel = panel;
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(false);
        }
    }

    public void SetSharedNextButton(Button button)
    {
        nextButton = button;
    }

    public void ShowTutorialHint()
    {
        WordTarget hintTarget = tutorialHintWord;
        if (hintTarget == null && targetWords.Count > 0)
        {
            hintTarget = targetWords[0];
        }

        if (hintTarget == null || hintTarget.SolutionTiles == null) return;

        hintActive = true;
        foreach (LetterTile tile in hintTarget.SolutionTiles)
        {
            if (tile != null)
            {
                tile.SetHint(true);
            }
        }
    }

    public void ClearTutorialHint()
    {
        if (!hintActive) return;

        hintActive = false;
        foreach (LetterTile tile in letterTiles)
        {
            if (tile != null)
            {
                tile.SetHint(false);
            }
        }
    }

    // ------------------------------------------------------------------
    // Tile events
    // ------------------------------------------------------------------

    public void OnTilePointerDown(LetterTile tile)
    {
        if (tile == null || wrongAnimationPlaying) return;

        ClearTutorialHint();
        ClearCurrentSelection();

        activeLine = GetAvailableLine();
        if (activeLine == null)
        {
            Debug.LogWarning($"[WordSearchLevel] No available selection line on Level {levelNumber}");
            return;
        }

        ApplyLineColor(activeLine);

        // Find the UI camera (null for Screen Space - Overlay)
        Canvas canvas = GetComponentInParent<Canvas>();
        uiCamera = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? canvas.worldCamera
            : null;

        isSelecting = true;
        startTile = tile;
        AddTileToSelection(tile);
    }

    public void OnTilePointerEnter(LetterTile tile)
    {
        // Selection is now handled in Update() using the pointer position,
        // so diagonal drags no longer clip tile corners.
    }

    public void OnTilePointerUp(LetterTile tile)
    {
        FinishSelection();
    }

    private void FinishSelection()
    {
        if (!isSelecting || wrongAnimationPlaying) return;

        isSelecting = false;
        startTile = null;
        ValidateCurrentSelection();
    }

    // ------------------------------------------------------------------
    // Smooth 8-direction selection
    // ------------------------------------------------------------------

    private Vector2 TileScreenPos(LetterTile tile)
    {
        return RectTransformUtility.WorldToScreenPoint(uiCamera, tile.transform.position);
    }

    private void UpdateSelectionFromPointer(Vector2 pointer)
    {
        Vector2 startPos = TileScreenPos(startTile);
        Vector2 delta = pointer - startPos;
        Vector2 deltaDir = delta.normalized;

        // Pick the direction whose on-screen vector best matches the drag
        Vector2Int bestDir = Vector2Int.zero;
        Vector2 bestVec = Vector2.zero;
        float bestScore = -2f;

        foreach (Vector2Int dir in Directions)
        {
            if (!tileGrid.TryGetValue((startTile.Row + dir.x, startTile.Column + dir.y), out LetterTile neighbour))
                continue;

            Vector2 vec = TileScreenPos(neighbour) - startPos;
            if (vec.sqrMagnitude < 0.0001f) continue;

            float score = Vector2.Dot(deltaDir, vec.normalized);
            if (score > bestScore)
            {
                bestScore = score;
                bestDir = dir;
                bestVec = vec;
            }
        }

        List<LetterTile> newPath = new List<LetterTile> { startTile };

        if (bestVec != Vector2.zero)
        {
            // How many tiles along that direction has the pointer travelled?
            int steps = Mathf.RoundToInt(Vector2.Dot(delta, bestVec) / bestVec.sqrMagnitude);
            steps = Mathf.Max(0, steps);

            for (int i = 1; i <= steps; i++)
            {
                if (!tileGrid.TryGetValue((startTile.Row + bestDir.x * i, startTile.Column + bestDir.y * i), out LetterTile t))
                    break;

                newPath.Add(t);
            }
        }

        ApplyPath(newPath);
    }

    private void ApplyPath(List<LetterTile> newPath)
    {
        // Skip if nothing changed
        if (newPath.Count == currentPath.Count)
        {
            bool same = true;
            for (int i = 0; i < newPath.Count; i++)
            {
                if (newPath[i] != currentPath[i])
                {
                    same = false;
                    break;
                }
            }

            if (same) return;
        }

        // Deselect tiles that are no longer in the path
        foreach (LetterTile t in currentPath)
        {
            if (t != null && !t.IsFound && !newPath.Contains(t))
            {
                t.SetSelected(false);
            }
        }

        currentPath.Clear();
        currentPath.AddRange(newPath);

        // Select tiles in the new path
        foreach (LetterTile t in currentPath)
        {
            if (t != null && !t.IsFound)
            {
                t.SetSelected(true);
            }
        }

        UpdateSelectionLine();
    }

    // ------------------------------------------------------------------
    // Selection helpers
    // ------------------------------------------------------------------

    private void AddTileToSelection(LetterTile tile)
    {
        if (tile == null || currentPath.Contains(tile)) return;

        currentPath.Add(tile);
        if (!tile.IsFound)
        {
            tile.SetSelected(true);
        }

        UpdateSelectionLine();
    }

    private void UpdateSelectionLine()
    {
        if (activeLine == null) return;

        // Only one tile selected: hide the bar until the drag moves to a second tile
        if (currentPath.Count < 2)
        {
            if (!activeLine.IsLocked)
            {
                activeLine.Hide();
            }
            return;
        }

        RectTransform firstRect = currentPath[0].GetComponent<RectTransform>();
        RectTransform lastRect = currentPath[currentPath.Count - 1].GetComponent<RectTransform>();

        if (firstRect != null && lastRect != null)
        {
            activeLine.ShowBetween(firstRect, lastRect);
        }
    }

    private void ClearCurrentSelection()
    {
        foreach (LetterTile tile in currentPath)
        {
            if (tile != null && !tile.IsFound)
            {
                tile.SetSelected(false);
            }
        }

        currentPath.Clear();

        if (activeLine != null && !activeLine.IsLocked)
        {
            activeLine.Hide();
        }

        activeLine = null;
        activeLineIndex = -1;
        isSelecting = false;
        startTile = null;
    }

    private void ValidateCurrentSelection()
    {
        if (currentPath.Count == 0)
        {
            ClearCurrentSelection();
            return;
        }

        string selectedLetters = "";
        foreach (LetterTile tile in currentPath)
        {
            if (tile != null)
            {
                selectedLetters += tile.Letter;
            }
        }

        Debug.Log($"[WordSearchLevel] Selected: {selectedLetters}");

        WordTarget matchedTarget = null;
        foreach (WordTarget target in targetWords)
        {
            if (target == null || target.IsCompleted) continue;

            if (target.ValidatePath(currentPath))
            {
                matchedTarget = target;
                break;
            }
        }

        if (matchedTarget != null)
        {
            Debug.Log($"[WordSearchLevel] CORRECT WORD: {matchedTarget.TargetWord}");

            matchedTarget.SetCompleted(true);

            foreach (LetterTile tile in currentPath)
            {
                if (tile != null)
                {
                    tile.SetSelected(false);
                    tile.SetFound(true);
                }
            }

            if (activeLine != null)
            {
                activeLine.Lock();
            }

            currentPath.Clear();
            activeLine = null;
            activeLineIndex = -1;

            CheckLevelCompletion();
            return;
        }

        Debug.Log($"[WordSearchLevel] WRONG WORD: {selectedLetters}");
        StartCoroutine(WrongSelectionRoutine());
    }

    private IEnumerator WrongSelectionRoutine()
    {
        wrongAnimationPlaying = true;

        if (activeLine != null)
        {
            yield return activeLine.ShakeAndHide(wrongShakeDuration);
        }

        foreach (LetterTile tile in currentPath)
        {
            if (tile != null && !tile.IsFound)
            {
                tile.SetSelected(false);
            }
        }

        currentPath.Clear();
        activeLine = null;
        activeLineIndex = -1;
        wrongAnimationPlaying = false;
    }

    private WordSelectionLine GetAvailableLine()
    {
        for (int i = 0; i < selectionLines.Count; i++)
        {
            WordSelectionLine line = selectionLines[i];
            if (line != null && !line.IsLocked)
            {
                activeLineIndex = i;
                return line;
            }
        }
        return null;
    }

    private void ApplyLineColor(WordSelectionLine line)
    {
        if (line == null) return;

        if (activeLineIndex >= 0 && activeLineIndex < lineColors.Count)
        {
            line.SetColor(lineColors[activeLineIndex]);
        }
    }

    private void CheckLevelCompletion()
    {
        bool allCompleted = true;
        foreach (WordTarget target in targetWords)
        {
            if (target != null && !target.IsCompleted)
            {
                allCompleted = false;
                break;
            }
        }

        if (!allCompleted) return;

        Debug.Log($"[WordSearchLevel] LEVEL {levelNumber} COMPLETE!");

        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[WordSearchLevel] Complete panel is not assigned.");
        }
    }
}