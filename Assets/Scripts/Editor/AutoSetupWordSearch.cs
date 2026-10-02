using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class AutoSetupWordSearch
{
    [MenuItem("Tools/Word Search/Build Everything (Prefabs & Scene)")]
    public static void BuildAll()
    {
        Debug.Log("[AutoSetupWordSearch] Building Level 1 and Level 2 Word Search system...");

        EnsureDirectories();

        // 1. Create Base Prefabs
        GameObject letterTilePrefab = CreateLetterTilePrefab();
        GameObject wordTargetPrefab = CreateWordTargetPrefab();
        GameObject linePrefab = CreateSelectionLinePrefab();
        GameObject levelCompletePrefab = CreateLevelCompletePanelPrefab();

        // 2. Create Level 1 Prefab (FRUITS)
        GameObject level1Prefab = CreateLevelPrefab(
            "Level_1",
            1,
            "FRUITS",
            new string[5, 5]
            {
                { "A", "P", "P", "L", "E" },
                { "K", "I", "W", "I", "X" },
                { "G", "R", "A", "P", "E" },
                { "N", "U", "T", "S", "O" },
                { "B", "E", "R", "R", "Y" }
            },
            new List<(string word, (int r, int c)[] coords)>
            {
                ("APPLE", new[] { (0,0), (0,1), (0,2), (0,3), (0,4) }),
                ("KIWI",  new[] { (1,0), (1,1), (1,2), (1,3) }),
                ("GRAPE", new[] { (2,0), (2,1), (2,2), (2,3), (2,4) }),
                ("NUT",   new[] { (3,0), (3,1), (3,2) })
            },
            true, // enable tutorial hint for first word
            letterTilePrefab,
            wordTargetPrefab,
            linePrefab,
            levelCompletePrefab
        );

        // 3. Create Level 2 Prefab (ANIMALS)
        GameObject level2Prefab = CreateLevelPrefab(
            "Level_2",
            2,
            "ANIMALS",
            new string[5, 5]
            {
                { "C", "A", "T", "X", "Z" },
                { "L", "I", "O", "N", "P" },
                { "B", "E", "A", "R", "Q" },
                { "F", "R", "O", "G", "W" },
                { "D", "O", "G", "S", "M" }
            },
            new List<(string word, (int r, int c)[] coords)>
            {
                ("CAT",  new[] { (0,0), (0,1), (0,2) }),
                ("LION", new[] { (1,0), (1,1), (1,2), (1,3) }),
                ("BEAR", new[] { (2,0), (2,1), (2,2), (2,3) }),
                ("FROG", new[] { (3,0), (3,1), (3,2), (3,3) })
            },
            false, // no tutorial hint for level 2
            letterTilePrefab,
            wordTargetPrefab,
            linePrefab,
            levelCompletePrefab
        );

        // 4. Create Level 3 Prefab (EXPLORE)
        GameObject level3Prefab = CreateLevelPrefab(
            "Level_3",
            3,
            "LET'S EXPLORE",
            new string[5, 5]
            {
                { "W", "I", "N", "X", "Z" },
                { "P", "L", "A", "Y", "Q" },
                { "F", "R", "O", "G", "W" },
                { "C", "A", "T", "S", "M" },
                { "L", "I", "O", "N", "K" }
            },
            new List<(string word, (int r, int c)[] coords)>
            {
                ("WIN",  new[] { (0,0), (0,1), (0,2) }),
                ("PLAY", new[] { (1,0), (1,1), (1,2), (1,3) })
            },
            false,
            letterTilePrefab,
            wordTargetPrefab,
            linePrefab,
            levelCompletePrefab
        );

        // 5. Create Level 4 Prefab (COLORS)
        GameObject level4Prefab = CreateLevelPrefab(
            "Level_4",
            4,
            "COLORS",
            new string[5, 5]
            {
                { "E", "R", "E", "D", "G" },
                { "P", "U", "X", "O", "R" },
                { "I", "Z", "L", "W", "E" },
                { "N", "D", "Q", "B", "E" },
                { "K", "C", "Y", "A", "N" }
            },
            new List<(string word, (int r, int c)[] coords)>
            {
                ("RED",   new[] { (0,1), (0,2), (0,3) }),
                ("BLUE",  new[] { (3,3), (2,2), (1,1), (0,0) }),
                ("PINK",  new[] { (1,0), (2,0), (3,0), (4,0) }),
                ("GREEN", new[] { (0,4), (1,4), (2,4), (3,4), (4,4) }),
                ("GOLD",  new[] { (0,4), (1,3), (2,2), (3,1) }),
                ("CYAN",  new[] { (4,1), (4,2), (4,3), (4,4) })
            },
            false,
            letterTilePrefab,
            wordTargetPrefab,
            linePrefab,
            levelCompletePrefab
        );

        // 6. Configure Gameplay Scene
        SetupGameplayScene(level1Prefab, level2Prefab, level3Prefab, level4Prefab, levelCompletePrefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[AutoSetupWordSearch] Levels 1, 2, 3 and 4 successfully built and saved!");
    }

    private static void EnsureDirectories()
    {
        if (!Directory.Exists("Assets/Prefabs")) Directory.CreateDirectory("Assets/Prefabs");
        if (!Directory.Exists("Assets/Prefabs/Levels")) Directory.CreateDirectory("Assets/Prefabs/Levels");
        if (!Directory.Exists("Assets/Prefabs/UI")) Directory.CreateDirectory("Assets/Prefabs/UI");
        AssetDatabase.Refresh();
    }

    private static GameObject CreateLetterTilePrefab()
    {
        string path = "Assets/Prefabs/LetterTile.prefab";

        GameObject root = new GameObject("LetterTile", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LetterTile));
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(110, 110);

        Image bgImage = root.GetComponent<Image>();
        bgImage.color = new Color(0.96f, 0.94f, 0.92f, 1f);
        bgImage.raycastTarget = true;

        GameObject sel = CreateHighlightChild(root, "SelectionHighlight", new Color(0.29f, 0.56f, 0.89f, 0.5f));
        sel.SetActive(false);

        GameObject fnd = CreateHighlightChild(root, "FoundHighlight", new Color(0.02f, 0.84f, 0.63f, 0.6f));
        fnd.SetActive(false);

        GameObject hnt = CreateHighlightChild(root, "HintHighlight", new Color(1.0f, 0.62f, 0.11f, 0.6f));
        hnt.SetActive(false);

        GameObject txtObj = new GameObject("LetterText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(root.transform, false);
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
        tmp.text = "A";
        tmp.fontSize = 50;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.17f, 0.18f, 0.26f, 1f);
        tmp.raycastTarget = false;

        LetterTile script = root.GetComponent<LetterTile>();
        SerializedObject so = new SerializedObject(script);
        so.FindProperty("letterText").objectReferenceValue = tmp;
        so.FindProperty("selectionHighlight").objectReferenceValue = sel;
        so.FindProperty("foundHighlight").objectReferenceValue = fnd;
        so.FindProperty("hintHighlight").objectReferenceValue = hnt;
        so.ApplyModifiedProperties();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject CreateHighlightChild(GameObject parent, string name, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(parent.transform, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        Image img = obj.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return obj;
    }

    private static GameObject CreateWordTargetPrefab()
    {
        string path = "Assets/Prefabs/WordTarget.prefab";

        GameObject root = new GameObject("WordTarget", typeof(RectTransform), typeof(WordTarget));
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(140, 50);

        GameObject txtObj = new GameObject("WordText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(root.transform, false);
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
        tmp.text = "WORD";
        tmp.fontSize = 30;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.17f, 0.18f, 0.26f, 1f);
        tmp.raycastTarget = false;

        GameObject visObj = new GameObject("CompletedVisual", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        visObj.transform.SetParent(root.transform, false);
        RectTransform visRt = visObj.GetComponent<RectTransform>();
        visRt.anchorMin = new Vector2(0.05f, 0.45f);
        visRt.anchorMax = new Vector2(0.95f, 0.55f);
        visRt.sizeDelta = Vector2.zero;

        Image img = visObj.GetComponent<Image>();
        img.color = new Color(0.02f, 0.84f, 0.63f, 0.9f);
        img.raycastTarget = false;
        visObj.SetActive(false);

        WordTarget script = root.GetComponent<WordTarget>();
        SerializedObject so = new SerializedObject(script);
        so.FindProperty("wordText").objectReferenceValue = tmp;
        so.FindProperty("completedVisual").objectReferenceValue = visObj;
        so.ApplyModifiedProperties();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject CreateSelectionLinePrefab()
    {
        string path = "Assets/Prefabs/UI/WordSelectionLine.prefab";

        GameObject root = new GameObject("WordSelectionLine", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(WordSelectionLine));
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(100, 60);

        Image img = root.GetComponent<Image>();
        img.color = new Color(0.29f, 0.56f, 0.89f, 0.8f);
        img.raycastTarget = false;

        WordSelectionLine script = root.GetComponent<WordSelectionLine>();
        SerializedObject so = new SerializedObject(script);
        so.FindProperty("lineRect").objectReferenceValue = rt;
        so.FindProperty("lineImage").objectReferenceValue = img;
        so.FindProperty("thickness").floatValue = 65f;
        so.ApplyModifiedProperties();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject CreateLevelCompletePanelPrefab()
    {
        string path = "Assets/Prefabs/UI/LevelCompletePanel.prefab";

        GameObject root = new GameObject("LevelCompletePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        Image overlayImg = root.GetComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.75f);
        overlayImg.raycastTarget = true;

        GameObject panel = new GameObject("PanelCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(root.transform, false);
        RectTransform panelRt = panel.GetComponent<RectTransform>();
        panelRt.sizeDelta = new Vector2(500, 450);

        Image cardImg = panel.GetComponent<Image>();
        cardImg.color = new Color(1f, 1f, 1f, 1f);

        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(panel.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchoredPosition = new Vector2(0, 140);
        titleRt.sizeDelta = new Vector2(450, 60);

        TextMeshProUGUI titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
        titleTmp.text = "LEVEL COMPLETE";
        titleTmp.fontSize = 38;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = new Color(0.11f, 0.21f, 0.34f, 1f);

        GameObject subObj = new GameObject("SubtitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        subObj.transform.SetParent(panel.transform, false);
        RectTransform subRt = subObj.GetComponent<RectTransform>();
        subRt.anchoredPosition = new Vector2(0, 60);
        subRt.sizeDelta = new Vector2(450, 50);

        TextMeshProUGUI subTmp = subObj.GetComponent<TextMeshProUGUI>();
        subTmp.text = "GREAT!";
        subTmp.fontSize = 32;
        subTmp.fontStyle = FontStyles.Bold;
        subTmp.alignment = TextAlignmentOptions.Center;
        subTmp.color = new Color(0.02f, 0.84f, 0.63f, 1f);

        GameObject btnObj = new GameObject("NextButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(panel.transform, false);
        RectTransform btnRt = btnObj.GetComponent<RectTransform>();
        btnRt.anchoredPosition = new Vector2(0, -90);
        btnRt.sizeDelta = new Vector2(240, 75);

        Image btnImg = btnObj.GetComponent<Image>();
        btnImg.color = new Color(0.02f, 0.84f, 0.63f, 1f);

        GameObject btnTxtObj = new GameObject("ButtonText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        btnTxtObj.transform.SetParent(btnObj.transform, false);
        RectTransform btnTxtRt = btnTxtObj.GetComponent<RectTransform>();
        btnTxtRt.anchorMin = Vector2.zero;
        btnTxtRt.anchorMax = Vector2.one;
        btnTxtRt.sizeDelta = Vector2.zero;

        TextMeshProUGUI btnTmp = btnTxtObj.GetComponent<TextMeshProUGUI>();
        btnTmp.text = "NEXT";
        btnTmp.fontSize = 32;
        btnTmp.fontStyle = FontStyles.Bold;
        btnTmp.alignment = TextAlignmentOptions.Center;
        btnTmp.color = Color.white;

        root.SetActive(false);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject CreateLevelPrefab(
        string prefabName,
        int levelNum,
        string categoryTitle,
        string[,] gridLetters,
        List<(string word, (int r, int c)[] coords)> wordDefs,
        bool enableHint,
        GameObject tilePrefab,
        GameObject targetPrefab,
        GameObject linePrefab,
        GameObject completePrefab)
    {
        string path = $"Assets/Prefabs/Levels/{prefabName}.prefab";

        GameObject root = new GameObject(prefabName, typeof(RectTransform), typeof(WordSearchLevel));
        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.sizeDelta = Vector2.zero;

        // ThemePanel
        GameObject themeObj = new GameObject("ThemePanel", typeof(RectTransform));
        themeObj.transform.SetParent(root.transform, false);
        RectTransform themeRt = themeObj.GetComponent<RectTransform>();
        themeRt.anchoredPosition = new Vector2(0, 520);
        themeRt.sizeDelta = new Vector2(600, 80);

        GameObject titleTextObj = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        titleTextObj.transform.SetParent(themeObj.transform, false);
        RectTransform titleTextRt = titleTextObj.GetComponent<RectTransform>();
        titleTextRt.anchorMin = Vector2.zero;
        titleTextRt.anchorMax = Vector2.one;
        titleTextRt.sizeDelta = Vector2.zero;

        TextMeshProUGUI titleTmp = titleTextObj.GetComponent<TextMeshProUGUI>();
        titleTmp.text = categoryTitle;
        titleTmp.fontSize = 42;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = new Color(0.11f, 0.21f, 0.34f, 1f);

        // TargetWords Container
        GameObject targetsContainer = new GameObject("TargetWords", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        targetsContainer.transform.SetParent(root.transform, false);
        RectTransform targetsRt = targetsContainer.GetComponent<RectTransform>();
        targetsRt.anchoredPosition = new Vector2(0, 410);
        targetsRt.sizeDelta = new Vector2(600, 70);

        HorizontalLayoutGroup hlg = targetsContainer.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 20;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;

        // SelectionLines Container
        GameObject linesContainer = new GameObject("SelectionLines", typeof(RectTransform));
        linesContainer.transform.SetParent(root.transform, false);
        RectTransform linesRt = linesContainer.GetComponent<RectTransform>();
        linesRt.anchorMin = Vector2.zero;
        linesRt.anchorMax = Vector2.one;
        linesRt.sizeDelta = Vector2.zero;

        List<WordSelectionLine> linesList = new List<WordSelectionLine>();
        for (int i = 0; i < 6; i++)
        {
            GameObject lineObj = PrefabUtility.InstantiatePrefab(linePrefab, linesContainer.transform) as GameObject;
            lineObj.name = $"SelectionLine_{i + 1}";
            WordSelectionLine lineScript = lineObj.GetComponent<WordSelectionLine>();
            lineScript.Hide();
            linesList.Add(lineScript);
        }

        // LetterGrid (5x5)
        GameObject gridObj = new GameObject("LetterGrid", typeof(RectTransform), typeof(GridLayoutGroup));
        gridObj.transform.SetParent(root.transform, false);
        RectTransform gridRt = gridObj.GetComponent<RectTransform>();
        gridRt.anchoredPosition = new Vector2(0, -60);
        gridRt.sizeDelta = new Vector2(600, 600);

        GridLayoutGroup glg = gridObj.GetComponent<GridLayoutGroup>();
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 5;
        glg.cellSize = new Vector2(110, 110);
        glg.spacing = new Vector2(8, 8);
        glg.childAlignment = TextAnchor.MiddleCenter;

        LetterTile[,] tileMatrix = new LetterTile[5, 5];
        List<LetterTile> allTilesList = new List<LetterTile>();

        for (int r = 0; r < 5; r++)
        {
            for (int c = 0; c < 5; c++)
            {
                string charStr = gridLetters[r, c];
                GameObject tileObj = PrefabUtility.InstantiatePrefab(tilePrefab, gridObj.transform) as GameObject;
                tileObj.name = $"Tile_{r}_{c}_{charStr}";

                LetterTile tileScript = tileObj.GetComponent<LetterTile>();
                tileScript.SetLetter(charStr, r, c);

                tileMatrix[r, c] = tileScript;
                allTilesList.Add(tileScript);
            }
        }

        // Word Targets
        List<WordTarget> targetScripts = new List<WordTarget>();
        WordTarget firstTargetWord = null;

        for (int i = 0; i < wordDefs.Count; i++)
        {
            var def = wordDefs[i];
            GameObject wordObj = PrefabUtility.InstantiatePrefab(targetPrefab, targetsContainer.transform) as GameObject;
            wordObj.name = $"Word_{i + 1}_{def.word}";

            WordTarget targetScript = wordObj.GetComponent<WordTarget>();
            SerializedObject soWord = new SerializedObject(targetScript);
            soWord.FindProperty("targetWord").stringValue = def.word;
            soWord.FindProperty("wordText").objectReferenceValue = wordObj.GetComponentInChildren<TextMeshProUGUI>();

            SerializedProperty solProp = soWord.FindProperty("solutionTiles");
            solProp.ClearArray();

            for (int s = 0; s < def.coords.Length; s++)
            {
                var coord = def.coords[s];
                LetterTile solutionTile = tileMatrix[coord.r, coord.c];
                solProp.InsertArrayElementAtIndex(s);
                solProp.GetArrayElementAtIndex(s).objectReferenceValue = solutionTile;
            }

            soWord.ApplyModifiedProperties();
            targetScripts.Add(targetScript);

            if (i == 0) firstTargetWord = targetScript;
        }

        // WordSearchLevel script configuration
        WordSearchLevel levelScript = root.GetComponent<WordSearchLevel>();
        SerializedObject soLevel = new SerializedObject(levelScript);
        soLevel.FindProperty("levelNumber").intValue = levelNum;
        soLevel.FindProperty("levelTitle").stringValue = categoryTitle;
        soLevel.FindProperty("levelTitleText").objectReferenceValue = titleTmp;
        soLevel.FindProperty("enableTutorialHint").boolValue = enableHint;
        soLevel.FindProperty("tutorialHintWord").objectReferenceValue = enableHint ? firstTargetWord : null;

        SerializedProperty targetsProp = soLevel.FindProperty("targetWords");
        targetsProp.ClearArray();
        for (int i = 0; i < targetScripts.Count; i++)
        {
            targetsProp.InsertArrayElementAtIndex(i);
            targetsProp.GetArrayElementAtIndex(i).objectReferenceValue = targetScripts[i];
        }

        SerializedProperty tilesProp = soLevel.FindProperty("letterTiles");
        tilesProp.ClearArray();
        for (int i = 0; i < allTilesList.Count; i++)
        {
            tilesProp.InsertArrayElementAtIndex(i);
            tilesProp.GetArrayElementAtIndex(i).objectReferenceValue = allTilesList[i];
        }

        SerializedProperty linesProp = soLevel.FindProperty("selectionLines");
        linesProp.ClearArray();
        for (int i = 0; i < linesList.Count; i++)
        {
            linesProp.InsertArrayElementAtIndex(i);
            linesProp.GetArrayElementAtIndex(i).objectReferenceValue = linesList[i];
        }

        soLevel.ApplyModifiedProperties();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log($"[AutoSetupWordSearch] Saved prefab: {path}");
        return prefab;
    }

    private static void SetupGameplayScene(GameObject level1Prefab, GameObject level2Prefab, GameObject level3Prefab, GameObject level4Prefab, GameObject completePrefab)
    {
        string scenePath = "Assets/Scenes/GamePlay.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // Ensure EventSystem
        EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject esObj = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Debug.Log("[AutoSetupWordSearch] Created missing EventSystem.");
        }

        // Find Canvas
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
        }

        // Ensure Shared LevelCompletePanel under Canvas
        Transform existingPanel = canvas.transform.Find("LevelCompletePanel");
        GameObject sharedCompletePanelObj = null;
        if (existingPanel != null)
        {
            sharedCompletePanelObj = existingPanel.gameObject;
        }
        else
        {
            sharedCompletePanelObj = PrefabUtility.InstantiatePrefab(completePrefab, canvas.transform) as GameObject;
            sharedCompletePanelObj.name = "LevelCompletePanel";
        }
        sharedCompletePanelObj.SetActive(false);
        Button sharedNextBtn = sharedCompletePanelObj.GetComponentInChildren<Button>();

        // Remove loose/legacy hierarchy elements directly under Canvas if any exist outside level prefabs
        Transform legacyGrid = canvas.transform.Find("LetterGrid");
        if (legacyGrid != null) Object.DestroyImmediate(legacyGrid.gameObject);
        Transform legacyTargets = canvas.transform.Find("TargetWords");
        if (legacyTargets != null) Object.DestroyImmediate(legacyTargets.gameObject);

        // Clean up old scene level instances
        Transform oldLevel1 = canvas.transform.Find("Level_1");
        if (oldLevel1 != null) Object.DestroyImmediate(oldLevel1.gameObject);
        Transform oldLevel2 = canvas.transform.Find("Level_2");
        if (oldLevel2 != null) Object.DestroyImmediate(oldLevel2.gameObject);
        Transform oldLevel3 = canvas.transform.Find("Level_3");
        if (oldLevel3 != null) Object.DestroyImmediate(oldLevel3.gameObject);
        Transform oldLevel4 = canvas.transform.Find("Level_4");
        if (oldLevel4 != null) Object.DestroyImmediate(oldLevel4.gameObject);

        // Instantiate Level prefabs under Canvas
        GameObject level1Instance = PrefabUtility.InstantiatePrefab(level1Prefab, canvas.transform) as GameObject;
        level1Instance.name = "Level_1";
        level1Instance.SetActive(true);

        GameObject level2Instance = PrefabUtility.InstantiatePrefab(level2Prefab, canvas.transform) as GameObject;
        level2Instance.name = "Level_2";
        level2Instance.SetActive(false);

        GameObject level3Instance = PrefabUtility.InstantiatePrefab(level3Prefab, canvas.transform) as GameObject;
        level3Instance.name = "Level_3";
        level3Instance.SetActive(false);

        GameObject level4Instance = PrefabUtility.InstantiatePrefab(level4Prefab, canvas.transform) as GameObject;
        level4Instance.name = "Level_4";
        level4Instance.SetActive(false);

        // Setup LevelManager
        LevelManager levelManager = Object.FindFirstObjectByType<LevelManager>();
        if (levelManager == null)
        {
            GameObject lmObj = new GameObject("LevelManager", typeof(LevelManager));
            lmObj.transform.SetParent(canvas.transform, false);
            levelManager = lmObj.GetComponent<LevelManager>();
        }

        SerializedObject soManager = new SerializedObject(levelManager);
        soManager.FindProperty("sharedCompletePanel").objectReferenceValue = sharedCompletePanelObj;
        soManager.FindProperty("sharedNextButton").objectReferenceValue = sharedNextBtn;

        SerializedProperty levelsProp = soManager.FindProperty("levels");
        levelsProp.ClearArray();
        levelsProp.InsertArrayElementAtIndex(0);
        levelsProp.GetArrayElementAtIndex(0).objectReferenceValue = level1Instance;
        levelsProp.InsertArrayElementAtIndex(1);
        levelsProp.GetArrayElementAtIndex(1).objectReferenceValue = level2Instance;
        levelsProp.InsertArrayElementAtIndex(2);
        levelsProp.GetArrayElementAtIndex(2).objectReferenceValue = level3Instance;
        levelsProp.InsertArrayElementAtIndex(3);
        levelsProp.GetArrayElementAtIndex(3).objectReferenceValue = level4Instance;

        soManager.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[AutoSetupWordSearch] Saved GamePlay.unity scene with Levels 1, 2, 3, and 4 references.");
    }
}
