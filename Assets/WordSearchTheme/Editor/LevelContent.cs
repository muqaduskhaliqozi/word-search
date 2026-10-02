using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Extra content for the Nature theme:
///  - 10 generated, progressively harder levels (bigger grids, more words, diagonal + backwards words)
///  - Level Select popup in the Main Menu (completed / current / locked).
/// Runs as part of Tools > Word Search > Apply Nature Theme (All Scenes).
/// </summary>
public static partial class NatureThemeBuilder
{
    private const string GeneratedPrefix = "NT_Level_";
    private static readonly List<string> recordedTitles = new List<string>();

    private struct WordDef
    {
        public string word;
        public Vector2Int[] cells; // (row, col)
    }

    private struct LevelDef
    {
        public string title;
        public string[] rows;
        public WordDef[] words;

        public LevelDef(string title, string[] rows, params WordDef[] words)
        {
            this.title = title;
            this.rows = rows;
            this.words = words;
        }
    }

    private static WordDef W(string word, params int[] rc)
    {
        Vector2Int[] cells = new Vector2Int[rc.Length / 2];
        for (int i = 0; i < cells.Length; i++) cells[i] = new Vector2Int(rc[i * 2], rc[i * 2 + 1]);
        return new WordDef { word = word, cells = cells };
    }

    // Grids were generated offline and verified: every word appears exactly once.
    // Difficulty ramps up: 7x7 -> 10x10, 6 -> 10 words, straight -> diagonal -> backwards -> all 8 directions.
    private static LevelDef[] ExtraLevels => new[]
    {
            new LevelDef("OCEAN", new[] { "CCCORAL", "RWRWOSO", "AHAFAPK", "BAGICVW", "BLESHWE", "PEJHWNH", "BNSHELL" },
                W("FISH", 2,3, 3,3, 4,3, 5,3), W("CRAB", 0,0, 1,0, 2,0, 3,0), W("WAVE", 1,3, 2,4, 3,5, 4,6), W("SHELL", 6,2, 6,3, 6,4, 6,5, 6,6), W("CORAL", 0,2, 0,3, 0,4, 0,5, 0,6), W("WHALE", 1,1, 2,1, 3,1, 4,1, 5,1)),
            new LevelDef("FOREST", new[] { "GTDHAOB", "POEDWPW", "PRELMIR", "BLREWNM", "IEFAWEO", "YJAFLOS", "GKTREES" },
                W("TREE", 6,2, 6,3, 6,4, 6,5), W("LEAF", 2,3, 3,3, 4,3, 5,3), W("BEAR", 3,0, 4,1, 5,2, 6,3), W("MOSS", 3,6, 4,6, 5,6, 6,6), W("DEER", 0,2, 1,2, 2,2, 3,2), W("OWL", 0,5, 1,4, 2,3), W("PINE", 1,5, 2,5, 3,5, 4,5)),
            new LevelDef("WEATHER", new[] { "HNSNOWS", "ICSGEFU", "TULTOON", "WWROOGN", "IMAIURY", "NSIWVDM", "DGNLIAH" },
                W("RAIN", 3,2, 4,2, 5,2, 6,2), W("SNOW", 0,2, 0,3, 0,4, 0,5), W("WIND", 3,0, 4,0, 5,0, 6,0), W("STORM", 1,2, 2,3, 3,4, 4,5, 5,6), W("CLOUD", 1,1, 2,2, 3,3, 4,4, 5,5), W("SUNNY", 0,6, 1,6, 2,6, 3,6, 4,6), W("FOG", 1,5, 2,5, 3,5), W("HAIL", 6,6, 6,5, 6,4, 6,3)),
            new LevelDef("BIRDS", new[] { "EEFKITGI", "ETEFUOOK", "VNGSCROW", "ONIBORTA", "DSELGAEH", "NTWAUPDA", "EMBALPAA", "OIALNLRV" },
                W("EAGLE", 4,6, 4,5, 4,4, 4,3, 4,2), W("ROBIN", 3,5, 3,4, 3,3, 3,2, 3,1), W("CROW", 2,4, 2,5, 2,6, 2,7), W("PARROT", 5,5, 4,5, 3,5, 2,5, 1,5, 0,5), W("DOVE", 4,0, 3,0, 2,0, 1,0), W("HAWK", 4,7, 3,7, 2,7, 1,7), W("SWAN", 4,1, 5,2, 6,3, 7,4)),
            new LevelDef("FLOWERS", new[] { "TULIPAAY", "OLLYKEPL", "IRISYPYD", "LYESORIH", "OISPLHIY", "TGYICSTL", "UKLRAUPO", "SYOPTDOO" },
                W("ROSE", 3,5, 3,4, 3,3, 3,2), W("TULIP", 0,0, 0,1, 0,2, 0,3, 0,4), W("LILY", 4,4, 5,3, 6,2, 7,1), W("DAISY", 7,5, 6,4, 5,3, 4,2, 3,1), W("ORCHID", 7,2, 6,3, 5,4, 4,5, 3,6, 2,7), W("LOTUS", 3,0, 4,0, 5,0, 6,0, 7,0), W("IRIS", 2,0, 2,1, 2,2, 2,3), W("POPPY", 4,3, 3,4, 2,5, 1,6, 0,7)),
            new LevelDef("BUGS", new[] { "HRUCPSAW", "FEDRHTAS", "LDEIRCNN", "YISCDWGA", "TPLKTNII", "ASNEACEL", "CELTEEBU", "NMOTHDNV" },
                W("ANT", 3,7, 2,6, 1,5), W("GNAT", 3,6, 4,5, 5,4, 6,3), W("MOTH", 7,1, 7,2, 7,3, 7,4), W("BEETLE", 6,6, 6,5, 6,4, 6,3, 6,2, 6,1), W("SPIDER", 5,1, 4,1, 3,1, 2,1, 1,1, 0,1), W("WASP", 0,7, 0,6, 0,5, 0,4), W("FLY", 1,0, 2,0, 3,0), W("CRICKET", 0,3, 1,3, 2,3, 3,3, 4,3, 5,3, 6,3), W("SNAIL", 1,7, 2,7, 3,7, 4,7, 5,7)),
            new LevelDef("GARDEN", new[] { "ESKTKOSHS", "BREDRFOFH", "WEWATEREO", "KAKDPNTEV", "NEKAECAHE", "ESOHEEDLL", "LDITAASIP", "ANAKHLOUN", "LSIWBSFSE" },
                W("SEED", 6,6, 5,5, 4,4, 3,3), W("SOIL", 8,5, 7,6, 6,7, 5,8), W("HOSE", 5,3, 5,2, 5,1, 5,0), W("RAKE", 1,4, 2,3, 3,2, 4,1), W("SHOVEL", 0,8, 1,8, 2,8, 3,8, 4,8, 5,8), W("FENCE", 1,5, 2,5, 3,5, 4,5, 5,5), W("PLANT", 6,8, 5,7, 4,6, 3,5, 2,4), W("WATER", 2,2, 2,3, 2,4, 2,5, 2,6)),
            new LevelDef("MOUNTAINS", new[] { "BERCPUVCI", "TOFLYALTD", "REUOLIRYC", "APSLFKNLS", "IREFDOAUH", "LYHIYEMEL", "RIGNRMRKP", "GLACIERAI", "ACWTEGDIR" },
                W("PEAK", 6,8, 5,7, 4,6, 3,5), W("CLIFF", 0,7, 1,6, 2,5, 3,4, 4,3), W("VALLEY", 0,6, 1,5, 2,4, 3,3, 4,2, 5,1), W("GLACIER", 7,0, 7,1, 7,2, 7,3, 7,4, 7,5, 7,6), W("SUMMIT", 3,8, 4,7, 5,6, 6,5, 7,4, 8,3), W("RIDGE", 8,8, 8,7, 8,6, 8,5, 8,4), W("CANYON", 8,1, 7,2, 6,3, 5,4, 4,5, 3,6), W("BOULDER", 0,0, 1,1, 2,2, 3,3, 4,4, 5,5, 6,6), W("TRAIL", 1,0, 2,0, 3,0, 4,0, 5,0)),
            new LevelDef("CAMPING", new[] { "CANOESTLS", "EKLEFINKS", "IRCAPRWCA", "EOIANMCHP", "TEMFPTIDM", "ONTOPKEKO", "RSNNIMCRC", "CKKNEKAAN", "HMGAETICB" },
                W("TENT", 8,5, 7,4, 6,3, 5,2), W("CAMPFIRE", 8,7, 7,6, 6,5, 5,4, 4,3, 3,2, 2,1, 1,0), W("LANTERN", 1,2, 2,3, 3,4, 4,5, 5,6, 6,7, 7,8), W("BACKPACK", 8,8, 7,7, 6,6, 5,5, 4,4, 3,3, 2,2, 1,1), W("COMPASS", 6,8, 5,8, 4,8, 3,8, 2,8, 1,8, 0,8), W("HIKING", 3,7, 4,6, 5,5, 6,4, 7,3, 8,2), W("CANOE", 0,0, 0,1, 0,2, 0,3, 0,4), W("MAP", 4,2, 3,3, 2,4), W("TORCH", 4,0, 5,0, 6,0, 7,0, 8,0), W("KNIFE", 1,7, 1,6, 1,5, 1,4, 1,3)),
            new LevelDef("SPACE", new[] { "DIORETSAMP", "RSNTPEKARL", "TPRTEGDERA", "IVUMAMTRNT", "BETLMIOOCH", "RAABPERCTU", "OXSUVWTKEF", "YMJALUBENT", "ALLYIPATOG", "TENALPAUSR" },
                W("PLANET", 9,5, 9,4, 9,3, 9,2, 9,1, 9,0), W("COMET", 5,7, 4,6, 3,5, 2,4, 1,3), W("GALAXY", 2,5, 3,4, 4,3, 5,2, 6,1, 7,0), W("ROCKET", 3,7, 4,7, 5,7, 6,7, 7,7, 8,7), W("ASTEROID", 0,7, 0,6, 0,5, 0,4, 0,3, 0,2, 0,1, 0,0), W("NEBULA", 7,8, 7,7, 7,6, 7,5, 7,4, 7,3), W("ORBIT", 6,0, 5,0, 4,0, 3,0, 2,0), W("METEOR", 4,4, 5,5, 6,6, 7,7, 8,8, 9,9), W("SATURN", 6,2, 5,2, 4,2, 3,2, 2,2, 1,2), W("JUPITER", 7,2, 6,3, 5,4, 4,5, 3,6, 2,7, 1,8)),
    };

    // ======================================================================
    // LEVEL GENERATION (GamePlay scene)
    // ======================================================================
    private static void GenerateExtraLevels(LevelManager lm)
    {
        SerializedObject so = new SerializedObject(lm);
        SerializedProperty levelsProp = so.FindProperty("levels");
        if (levelsProp == null) { Debug.LogError("[NatureTheme] LevelManager.levels not found"); return; }

        // existing hand-made levels (drop nulls + anything we generated on a previous run)
        List<GameObject> levels = new List<GameObject>();
        for (int i = 0; i < levelsProp.arraySize; i++)
        {
            GameObject g = levelsProp.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;
            if (g != null && !g.name.StartsWith(GeneratedPrefix)) levels.Add(g);
        }
        foreach (WordSearchLevel old in lm.GetComponentsInChildren<WordSearchLevel>(true))
        {
            if (old.name.StartsWith(GeneratedPrefix)) Object.DestroyImmediate(old.gameObject);
        }

        WordSearchLevel template = null;
        foreach (GameObject g in levels)
        {
            WordSearchLevel l = g.GetComponent<WordSearchLevel>();
            if (l != null && l.GetComponentInChildren<LetterTile>(true) != null &&
                l.GetComponentInChildren<WordTarget>(true) != null &&
                l.GetComponentInChildren<WordSelectionLine>(true) != null)
            {
                template = l;
                break;
            }
        }
        if (template == null)
        {
            Debug.LogError("[NatureTheme] No level with tiles, words and lines found to use as a template.");
            return;
        }

        LevelDef[] defs = ExtraLevels;
        for (int i = 0; i < defs.Length; i++)
        {
            int number = levels.Count + 1;
            GameObject go = BuildLevelFromTemplate(template, defs[i], number);
            if (go != null) levels.Add(go);
        }

        levelsProp.ClearArray();
        for (int i = 0; i < levels.Count; i++)
        {
            levelsProp.InsertArrayElementAtIndex(i);
            levelsProp.GetArrayElementAtIndex(i).objectReferenceValue = levels[i];
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log($"[NatureTheme] Levels in GamePlay: {levels.Count} ({defs.Length} generated).");
    }

    private static GameObject BuildLevelFromTemplate(WordSearchLevel template, LevelDef def, int number)
    {
        int n = def.rows.Length;
        GameObject go = Object.Instantiate(template.gameObject, template.transform.parent);
        go.name = $"{GeneratedPrefix}{number:00}_{def.title}";
        go.SetActive(false);
        WordSearchLevel level = go.GetComponent<WordSearchLevel>();

        // ---------------- tiles
        GridLayoutGroup grid = go.GetComponentInChildren<GridLayoutGroup>(true);
        LetterTile proto = grid.GetComponentInChildren<LetterTile>(true);
        List<Transform> kill = new List<Transform>();
        foreach (Transform t in grid.transform) if (t != proto.transform) kill.Add(t);
        foreach (Transform t in kill) Object.DestroyImmediate(t.gameObject);

        proto.ResetState();
        LetterTile[,] cells = new LetterTile[n, n];
        List<Object> allTiles = new List<Object>();
        for (int r = 0; r < n; r++)
        {
            for (int c = 0; c < n; c++)
            {
                LetterTile tile = (r == 0 && c == 0) ? proto : Object.Instantiate(proto.gameObject, grid.transform).GetComponent<LetterTile>();
                string letter = def.rows[r][c].ToString();
                tile.SetLetter(letter, r, c);
                tile.gameObject.name = $"Tile_{r}_{c}_{letter}";
                tile.transform.SetSiblingIndex(r * n + c);
                EditorUtility.SetDirty(tile);
                cells[r, c] = tile;
                allTiles.Add(tile);
            }
        }
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = n;

        // ---------------- word chips
        Transform wordsRoot = go.transform.Find("TargetWords");
        WordTarget protoWord = wordsRoot != null ? wordsRoot.GetComponentInChildren<WordTarget>(true) : go.GetComponentInChildren<WordTarget>(true);
        Transform wordParent = protoWord.transform.parent;
        kill.Clear();
        foreach (Transform t in wordParent) if (t != protoWord.transform && t.GetComponent<WordTarget>() != null) kill.Add(t);
        foreach (Transform t in kill) Object.DestroyImmediate(t.gameObject);

        List<Object> allWords = new List<Object>();
        for (int w = 0; w < def.words.Length; w++)
        {
            WordDef wd = def.words[w];
            WordTarget target = w == 0 ? protoWord : Object.Instantiate(protoWord.gameObject, wordParent).GetComponent<WordTarget>();
            target.gameObject.name = "WordTarget_" + wd.word;
            target.gameObject.SetActive(true);

            SerializedObject wso = new SerializedObject(target);
            wso.FindProperty("targetWord").stringValue = wd.word;
            SerializedProperty sol = wso.FindProperty("solutionTiles");
            sol.ClearArray();
            for (int k = 0; k < wd.cells.Length; k++)
            {
                sol.InsertArrayElementAtIndex(k);
                sol.GetArrayElementAtIndex(k).objectReferenceValue = cells[wd.cells[k].x, wd.cells[k].y];
            }
            wso.ApplyModifiedPropertiesWithoutUndo();

            Transform txt = target.transform.Find("WordText");
            if (txt != null)
            {
                TMP_Text t = txt.GetComponent<TMP_Text>();
                if (t != null) t.text = wd.word;
            }
            Transform strike = target.transform.Find("CompletedVisual");
            if (strike != null) strike.gameObject.SetActive(false);
            allWords.Add(target);
        }

        // ---------------- title
        Transform tp = go.transform.Find("ThemePanel");
        if (tp != null)
        {
            TMP_Text title = tp.GetComponentInChildren<TMP_Text>(true);
            if (title != null) title.text = def.title;
        }

        // ---------------- level script
        SerializedObject lso = new SerializedObject(level);
        lso.FindProperty("levelNumber").intValue = number;
        lso.FindProperty("levelTitle").stringValue = def.title;
        lso.FindProperty("enableTutorialHint").boolValue = false;
        lso.FindProperty("tutorialHintWord").objectReferenceValue = null;
        SetList(lso.FindProperty("letterTiles"), allTiles);
        SetList(lso.FindProperty("targetWords"), allWords);
        lso.ApplyModifiedPropertiesWithoutUndo();

        return go;
    }

    private static void SetList(SerializedProperty p, List<Object> values)
    {
        if (p == null) return;
        p.ClearArray();
        for (int i = 0; i < values.Count; i++)
        {
            p.InsertArrayElementAtIndex(i);
            p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }

    private static void RecordLevelTitles(LevelManager lm)
    {
        recordedTitles.Clear();
        SerializedObject so = new SerializedObject(lm);
        SerializedProperty levelsProp = so.FindProperty("levels");
        for (int i = 0; i < levelsProp.arraySize; i++)
        {
            GameObject g = levelsProp.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;
            WordSearchLevel l = g != null ? g.GetComponent<WordSearchLevel>() : null;
            recordedTitles.Add(l != null ? l.LevelTitle : "");
        }
        EditorPrefs.SetString("WS_NatureTheme_LevelTitles", string.Join("|", recordedTitles));
    }

    private static string[] LevelTitles()
    {
        if (recordedTitles.Count > 0) return recordedTitles.ToArray();
        string saved = EditorPrefs.GetString("WS_NatureTheme_LevelTitles", "");
        return string.IsNullOrEmpty(saved) ? new string[0] : saved.Split('|');
    }

    // ======================================================================
    // MAIN MENU: LEVELS button + Level Select popup
    // ======================================================================
    private static Button BuildLevelsButton(Transform canvas)
    {
        RectTransform b = Place(Ensure(canvas, "NT_LevelsButton"), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f),
            new Vector2(0, 310), new Vector2(440, 130));
        Image img = Sliced(b.gameObject, S("btn_orange"), 150f, 130f, true);
        Button btn = MakeButton(b.gameObject, img);
        RectTransform t = Stretch(Ensure(b, "NT_Text"));
        t.offsetMin = new Vector2(30, 12);
        t.offsetMax = new Vector2(-30, 0);
        Txt(t.gameObject, "LEVELS", 56, Brown, matPlain, true, 30f);
        GetOrAdd<UIPopIn>(b.gameObject).Configure(0.75f, new Vector2(0, -80), 0.3f, 0.5f);
        return btn;
    }

    private static LevelSelectPanel BuildLevelSelect(Transform canvas)
    {
        RectTransform pop = Stretch(Ensure(canvas, "NT_LevelSelect"));
        Image overlay = Img(pop.gameObject, null, Image.Type.Simple, 1f, false, true);
        overlay.color = Overlay;

        RectTransform card = Center(Ensure(pop, "NT_Card"), new Vector2(0, -30), new Vector2(960, 1380));
        Sliced(card.gameObject, S("panel"), 256f, 256f, true);

        RectTransform ribbon = Place(Ensure(card, "NT_Ribbon"), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 136));
        BuildPlank(ribbon, ribbon.sizeDelta, "LEVELS", 66, S("plank"), 150f, 1.1f);

        RectTransform prog = Place(Ensure(card, "NT_Progress"), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, -120), new Vector2(700, 60));
        TextMeshProUGUI progressText = Txt(prog.gameObject, "0 / 16 COMPLETED", 40, Brown, matPlain);

        // scroll view
        RectTransform view = Stretch(Ensure(card, "NT_Scroll"));
        view.offsetMin = new Vector2(46, 50);
        view.offsetMax = new Vector2(-46, -165);
        GetOrAdd<RectMask2D>(view.gameObject);
        Image viewHit = Img(view.gameObject, null, Image.Type.Simple, 1f, false, true);
        viewHit.color = new Color(1f, 1f, 1f, 0f); // invisible, but lets the user drag the list
        ScrollRect sr = GetOrAdd<ScrollRect>(view.gameObject);

        RectTransform content = Place(Ensure(view, "NT_Content"), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 0));
        GridLayoutGroup grid = GetOrAdd<GridLayoutGroup>(content.gameObject);
        grid.cellSize = new Vector2(180, 236);
        grid.spacing = new Vector2(32, 26);
        grid.padding = new RectOffset(0, 0, 24, 24);
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;
        ContentSizeFitter fitter = GetOrAdd<ContentSizeFitter>(content.gameObject);
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        sr.content = content;
        sr.viewport = view;
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Elastic;
        sr.scrollSensitivity = 30f;
        sr.inertia = true;

        // remove buttons that may have been saved in the scene by an older run
        List<GameObject> stale = new List<GameObject>();
        foreach (Transform t in content) stale.Add(t.gameObject);
        foreach (GameObject g in stale) Object.DestroyImmediate(g);

        // template button (kept hidden on the card, cloned at runtime)
        RectTransform tpl = Center(Ensure(card, "NT_LevelButtonTemplate"), Vector2.zero, new Vector2(180, 236));
        RectTransform face = Place(Ensure(tpl, "NT_Btn"), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(180, 180));
        Image faceImg = Img(face.gameObject, S("btn_square_orange"), Image.Type.Simple, 1f, false, true);
        MakeButton(tpl.gameObject, faceImg);

        RectTransform num = Place(Ensure(tpl, "NT_Number"), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, -84), new Vector2(170, 120));
        Txt(num.gameObject, "1", 76, Color.white, matPlain);

        RectTransform cap = Place(Ensure(tpl, "NT_Caption"), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), new Vector2(0, 26), new Vector2(200, 46));
        Txt(cap.gameObject, "NATURE", 28, Brown, matPlain, true, 14f);

        RectTransform badge = Place(Ensure(tpl, "NT_Badge"), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(66, -14), new Vector2(64, 64));
        Img(badge.gameObject, S("badge"), Image.Type.Simple, 1f, true);
        RectTransform bi = Stretch(Ensure(badge, "NT_BadgeIcon"), 15f);
        Img(bi.gameObject, S("icon_lock"), Image.Type.Simple, 1f, true);
        tpl.gameObject.SetActive(false);

        // close button
        RectTransform cl = Place(Ensure(card, "NT_Close"), new Vector2(1, 1), new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-34, -34), new Vector2(110, 110));
        Image ci = Img(cl.gameObject, S("btn_square_red"), Image.Type.Simple, 1f, false, true);
        Button close = MakeButton(cl.gameObject, ci);
        RectTransform cic = Center(Ensure(cl, "Icon"), new Vector2(0, 5), new Vector2(56, 56));
        Img(cic.gameObject, S("icon_close"), Image.Type.Simple, 1f, true);

        PopupAnimator anim = GetOrAdd<PopupAnimator>(pop.gameObject);
        anim.Setup(card, new RectTransform[0], null, new RectTransform[0], false, false);
        EditorUtility.SetDirty(anim);

        LevelSelectPanel panel = GetOrAdd<LevelSelectPanel>(pop.gameObject);
        string[] titles = LevelTitles();
        panel.Configure(titles.Length > 0 ? titles.Length : 16, titles);
        SetField(panel, "content", content);
        SetField(panel, "buttonTemplate", tpl.gameObject);
        SetField(panel, "scroll", sr);
        SetField(panel, "progressText", progressText);
        SetField(panel, "closeButton", close);
        SetField(panel, "completedSprite", S("btn_square_green"));
        SetField(panel, "currentSprite", S("btn_square_current"));
        SetField(panel, "lockedSprite", S("btn_square_grey"));
        SetField(panel, "checkIcon", S("icon_check"));
        SetField(panel, "lockIcon", S("icon_lock"));
        EditorUtility.SetDirty(panel);

        pop.gameObject.SetActive(false);
        return panel;
    }
}
