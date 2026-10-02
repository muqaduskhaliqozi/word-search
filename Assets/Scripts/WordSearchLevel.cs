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

    [Header("Juice")]
    [Tooltip("Seconds to wait after the last word before the Level Complete popup shows.")]
    [SerializeField] private float completePanelDelay = 0.9f;
    [SerializeField] private bool playIntroAnimation = true;

    public int LevelNumber => levelNumber;
    public string LevelTitle => levelTitle;
    public Button NextButton => nextButton;
    public bool IsLevelDone => levelDone;

    /// <summary>Raised when a word is found (word, tiles).</summary>
    public event Action<WordTarget, List<LetterTile>> WordFound;
    /// <summary>Raised once when every word of the level is found.</summary>
    public event Action LevelCompleted;

    private readonly List<LetterTile> currentPath = new List<LetterTile>();
    private readonly Dictionary<(int, int), LetterTile> tileGrid = new Dictionary<(int, int), LetterTile>();
    private readonly List<LetterTile> tutorialHintTiles = new List<LetterTile>();

    private bool isSelecting;
    private bool hintActive;
    private bool wrongAnimationPlaying;
    private bool levelDone;

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
        isSelecting = false;
        hintActive = false;
        wrongAnimationPlaying = false;
        levelDone = false;
        StopAllCoroutines();

        currentPath.Clear();
        tutorialHintTiles.Clear();
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

        PlayIntro();
    }

    /// <summary>Tiles pop in diagonally, word chips pop one after another.</summary>
    public void PlayIntro()
    {
        if (!playIntroAnimation || !Application.isPlaying || !isActiveAndEnabled) return;

        foreach (LetterTile tile in letterTiles)
        {
            if (tile != null) tile.PlayIntro(0.05f + (tile.Row + tile.Column) * 0.035f);
        }

        for (int i = 0; i < targetWords.Count; i++)
        {
            if (targetWords[i] != null) targetWords[i].PlayNudge(0.25f + i * 0.06f);
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

        if (hintTarget == null) return;

        List<LetterTile> tiles = GetWordTiles(hintTarget);
        if (tiles == null) return;

        hintActive = true;
        foreach (LetterTile tile in tiles)
        {
            if (tile != null)
            {
                tile.SetHint(true);
                tutorialHintTiles.Add(tile);
            }
        }
    }

    public void ClearTutorialHint()
    {
        if (!hintActive) return;

        hintActive = false;
        foreach (LetterTile tile in tutorialHintTiles)
        {
            if (tile != null && !tile.IsFound)
            {
                tile.SetHint(false);
            }
        }
        tutorialHintTiles.Clear();
    }

    // ------------------------------------------------------------------
    // Booster API (used by GameHUD)
    // ------------------------------------------------------------------

    /// <summary>
    /// Reveals the first letter of an unfound word (or the whole word if all first letters
    /// are already revealed). Returns false if nothing could be hinted.
    /// </summary>
    public bool UseHint()
    {
        if (levelDone) return false;

        WordTarget fallback = null;
        List<LetterTile> fallbackTiles = null;

        foreach (WordTarget target in targetWords)
        {
            if (target == null || target.IsCompleted) continue;
            List<LetterTile> tiles = GetWordTiles(target);
            if (tiles == null || tiles.Count == 0) continue;

            if (!tiles[0].IsHinted)
            {
                tiles[0].SetHint(true);
                target.PlayNudge();
                SfxPlayer.Play(SfxPlayer.Sfx.Hint);
                if (UIBurst.Instance != null) UIBurst.Instance.Sparkles(tiles[0].transform.position, new Color(1f, 0.8f, 0.3f), 10, 140f);
                return true;
            }

            if (fallback == null)
            {
                fallback = target;
                fallbackTiles = tiles;
            }
        }

        if (fallback != null)
        {
            foreach (LetterTile t in fallbackTiles) t.SetHint(true);
            fallback.PlayNudge();
            SfxPlayer.Play(SfxPlayer.Sfx.Hint);
            return true;
        }

        return false;
    }

    /// <summary>Shuffles the order of the word chips (they slide to their new places).</summary>
    public void ShuffleWordList()
    {
        List<Transform> chips = new List<Transform>();
        foreach (WordTarget target in targetWords)
        {
            if (target != null) chips.Add(target.transform);
        }
        if (chips.Count < 2) return;

        for (int i = chips.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (chips[i], chips[j]) = (chips[j], chips[i]);
        }

        for (int i = 0; i < chips.Count; i++)
        {
            chips[i].SetSiblingIndex(i);
        }

        foreach (LetterTile tile in letterTiles)
        {
            if (tile != null) tile.PlayFoundBounce((tile.Row + tile.Column) * 0.03f);
        }

        SfxPlayer.Play(SfxPlayer.Sfx.Shuffle);
    }

    /// <summary>The tiles that spell a word: assigned solution tiles, or found by searching the grid.</summary>
    public List<LetterTile> GetWordTiles(WordTarget target)
    {
        if (target == null) return null;
        string word = string.IsNullOrEmpty(target.TargetWord) ? "" : target.TargetWord.ToUpper();

        List<LetterTile> sol = target.SolutionTiles;
        if (sol != null && sol.Count > 0 && (word.Length == 0 || sol.Count == word.Length) && !sol.Contains(null))
        {
            return new List<LetterTile>(sol);
        }

        if (word.Length == 0) return null;

        foreach (LetterTile start in letterTiles)
        {
            if (start == null || string.IsNullOrEmpty(start.Letter)) continue;
            if (char.ToUpperInvariant(start.Letter[0]) != word[0]) continue;

            foreach (Vector2Int dir in Directions)
            {
                List<LetterTile> path = new List<LetterTile> { start };
                bool ok = true;
                for (int i = 1; i < word.Length; i++)
                {
                    if (!tileGrid.TryGetValue((start.Row + dir.x * i, start.Column + dir.y * i), out LetterTile t) ||
                        string.IsNullOrEmpty(t.Letter) || char.ToUpperInvariant(t.Letter[0]) != word[i])
                    {
                        ok = false;
                        break;
                    }
                    path.Add(t);
                }
                if (ok) return path;
            }
        }
        return null;
    }

    // ------------------------------------------------------------------
    // Tile events
    // ------------------------------------------------------------------

    public void OnTilePointerDown(LetterTile tile)
    {
        if (tile == null || wrongAnimationPlaying || levelDone) return;

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
        SfxPlayer.Play(SfxPlayer.Sfx.Select, 1f);
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

        bool grew = newPath.Count > currentPath.Count;

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

        if (grew)
        {
            // rising "tick" while dragging
            SfxPlayer.Play(SfxPlayer.Sfx.Select, 1f + (currentPath.Count - 1) * 0.08f);
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

        // A single tap is not a guess - just clear quietly.
        if (currentPath.Count == 1)
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
            matchedTarget.SetCompleted(true);

            List<LetterTile> foundTiles = new List<LetterTile>(currentPath);
            for (int i = 0; i < foundTiles.Count; i++)
            {
                LetterTile tile = foundTiles[i];
                if (tile != null)
                {
                    tile.SetSelected(false);
                    tile.SetFound(true);
                    tile.PlayFoundBounce(i * 0.05f);
                }
            }

            Color fxColor = Color.white;
            if (activeLine != null)
            {
                fxColor = activeLine.BaseColor;
                activeLine.Lock();
            }

            if (UIBurst.Instance != null && foundTiles.Count > 0)
            {
                Vector3 center = (foundTiles[0].transform.position + foundTiles[foundTiles.Count - 1].transform.position) * 0.5f;
                UIBurst.Instance.Sparkles(center, fxColor);
                UIBurst.Instance.Sparkles(matchedTarget.transform.position, fxColor, 8, 120f);
            }
            SfxPlayer.Play(SfxPlayer.Sfx.Found);

            currentPath.Clear();
            activeLine = null;
            activeLineIndex = -1;

            WordFound?.Invoke(matchedTarget, foundTiles);

            CheckLevelCompletion();
            return;
        }

        StartCoroutine(WrongSelectionRoutine());
    }

    private IEnumerator WrongSelectionRoutine()
    {
        wrongAnimationPlaying = true;
        SfxPlayer.Play(SfxPlayer.Sfx.Wrong);

        foreach (LetterTile tile in currentPath)
        {
            if (tile != null && !tile.IsFound) tile.PlayShake();
        }

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
        if (line == null || lineColors == null || lineColors.Count == 0) return;

        if (activeLineIndex >= 0)
        {
            line.SetColor(lineColors[activeLineIndex % lineColors.Count]);
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

        levelDone = true;
        Debug.Log($"[WordSearchLevel] LEVEL {levelNumber} COMPLETE!");
        StartCoroutine(LevelCompleteRoutine());
    }

    private IEnumerator LevelCompleteRoutine()
    {
        yield return new WaitForSecondsRealtime(0.35f);

        // celebratory wave across the whole board
        foreach (LetterTile tile in letterTiles)
        {
            if (tile != null) tile.PlayFoundBounce((tile.Row + tile.Column) * 0.04f);
        }

        yield return new WaitForSecondsRealtime(Mathf.Max(0f, completePanelDelay - 0.35f));

        LevelCompleted?.Invoke();

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
