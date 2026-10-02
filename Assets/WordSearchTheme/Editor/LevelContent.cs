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
    // 19 levels (7-25). Difficulty ramps up: 6x6 -> 10x10, 5 -> 12 words, straight -> diagonal -> backwards -> all 8 directions.
    private static LevelDef[] ExtraLevels => new[]
    {
            new LevelDef("OCEAN", new[] { "CORALC", "CHJOAT", "RSHELL", "AFISHD", "BUMABG", "WAVEKE" },
                W("FISH", 3,1, 3,2, 3,3, 3,4), W("CRAB", 1,0, 2,0, 3,0, 4,0), W("WAVE", 5,0, 5,1, 5,2, 5,3), W("SHELL", 2,1, 2,2, 2,3, 2,4, 2,5), W("CORAL", 0,0, 0,1, 0,2, 0,3, 0,4)),
            new LevelDef("FOREST", new[] { "BLMOSS", "TREEEB", "DFOARD", "EEWGFA", "EULIVG", "RSBEAR" },
                W("TREE", 1,0, 1,1, 1,2, 1,3), W("LEAF", 0,1, 1,2, 2,3, 3,4), W("BEAR", 5,2, 5,3, 5,4, 5,5), W("MOSS", 0,2, 0,3, 0,4, 0,5), W("DEER", 2,0, 3,0, 4,0, 5,0), W("OWL", 2,2, 3,2, 4,2)),
            new LevelDef("KITCHEN", new[] { "SIBRBHO", "CKWFORK", "OSNVWNP", "VBPIUHL", "ECOOFEA", "NLSWOET", "PMSALNE" },
                W("SPOON", 2,1, 3,2, 4,3, 5,4, 6,5), W("FORK", 1,3, 1,4, 1,5, 1,6), W("KNIFE", 1,1, 2,2, 3,3, 4,4, 5,5), W("PLATE", 2,6, 3,6, 4,6, 5,6, 6,6), W("BOWL", 3,1, 4,2, 5,3, 6,4), W("OVEN", 2,0, 3,0, 4,0, 5,0)),
            new LevelDef("WEATHER", new[] { "CWRAINA", "LUITVSO", "OSLNUPL", "UYTNDVC", "DSNOWYA", "OYSIRUN", "WFOGYMU" },
                W("RAIN", 0,2, 0,3, 0,4, 0,5), W("SNOW", 4,1, 4,2, 4,3, 4,4), W("WIND", 0,1, 1,2, 2,3, 3,4), W("STORM", 2,1, 3,2, 4,3, 5,4, 6,5), W("CLOUD", 0,0, 1,0, 2,0, 3,0, 4,0), W("SUNNY", 1,5, 2,4, 3,3, 4,2, 5,1), W("FOG", 6,1, 6,2, 6,3)),
            new LevelDef("FARM", new[] { "NTBARNK", "HRGIPGI", "OAUROUS", "RCCAIHF", "STTOEAM", "EOEEWYB", "HRPNEHN" },
                W("COW", 3,2, 4,3, 5,4), W("PIG", 1,4, 1,3, 1,2), W("SHEEP", 2,6, 3,5, 4,4, 5,3, 6,2), W("GOAT", 1,5, 2,4, 3,3, 4,2), W("HORSE", 1,0, 2,0, 3,0, 4,0, 5,0), W("BARN", 0,2, 0,3, 0,4, 0,5), W("HEN", 6,5, 6,4, 6,3), W("TRACTOR", 0,1, 1,1, 2,1, 3,1, 4,1, 5,1, 6,1)),
            new LevelDef("BIRDS", new[] { "IAKRCROW", "KWAHROWE", "IOMRLBRY", "DSFAPIER", "OAROSNLA", "RTEHVWGO", "CHTORRAP", "WNGDOVEN" },
                W("EAGLE", 7,6, 6,6, 5,6, 4,6, 3,6), W("ROBIN", 0,5, 1,5, 2,5, 3,5, 4,5), W("CROW", 0,4, 0,5, 0,6, 0,7), W("PARROT", 6,7, 6,6, 6,5, 6,4, 6,3, 6,2), W("DOVE", 7,3, 7,4, 7,5, 7,6), W("HAWK", 1,3, 1,2, 1,1, 1,0), W("SWAN", 4,4, 5,5, 6,6, 7,7)),
            new LevelDef("MUSIC", new[] { "NUETULFU", "AIMURDNG", "GTLBANDI", "GSVOTAOU", "DUOPIANO", "GIRNUVNP", "OAVHGMLM", "HURITNRE" },
                W("PIANO", 4,3, 4,4, 4,5, 4,6, 4,7), W("DRUM", 1,5, 1,4, 1,3, 1,2), W("GUITAR", 6,4, 5,4, 4,4, 3,4, 2,4, 1,4), W("FLUTE", 0,6, 0,5, 0,4, 0,3, 0,2), W("VIOLIN", 5,5, 4,4, 3,3, 2,2, 1,1, 0,0), W("SONG", 3,1, 4,2, 5,3, 6,4), W("HARP", 7,0, 6,1, 5,2, 4,3), W("BAND", 2,3, 2,4, 2,5, 2,6)),
            new LevelDef("FLOWERS", new[] { "ITILYOOD", "YLILORMR", "PSRYCTTR", "PTIHERUY", "OBIAOOLS", "PDLSDYII", "PEEMOTPN", "RSYSSIRI" },
                W("ROSE", 3,5, 4,4, 5,3, 6,2), W("TULIP", 2,6, 3,6, 4,6, 5,6, 6,6), W("LILY", 1,3, 1,2, 1,1, 1,0), W("DAISY", 5,4, 4,3, 3,2, 2,1, 1,0), W("ORCHID", 0,6, 1,5, 2,4, 3,3, 4,2, 5,1), W("LOTUS", 0,3, 1,4, 2,5, 3,6, 4,7), W("IRIS", 7,7, 7,6, 7,5, 7,4), W("POPPY", 5,0, 4,0, 3,0, 2,0, 1,0)),
            new LevelDef("BUGS", new[] { "ELTEEBCE", "MVATSLTN", "EANCPAEA", "LOGSIAKL", "HIAKDWCI", "TWYMECIA", "OEHLRTRN", "MANTFTCS" },
                W("ANT", 7,1, 7,2, 7,3), W("GNAT", 3,2, 2,2, 1,2, 0,2), W("MOTH", 7,0, 6,0, 5,0, 4,0), W("BEETLE", 0,5, 0,4, 0,3, 0,2, 0,1, 0,0), W("SPIDER", 1,4, 2,4, 3,4, 4,4, 5,4, 6,4), W("WASP", 5,1, 4,2, 3,3, 2,4), W("FLY", 7,4, 6,3, 5,2), W("CRICKET", 7,6, 6,6, 5,6, 4,6, 3,6, 2,6, 1,6), W("SNAIL", 7,7, 6,7, 5,7, 4,7, 3,7)),
            new LevelDef("SPORTS", new[] { "YRGMFYNST", "EREFLOGIS", "KUGCMNGNG", "CGHSCNKNG", "OBGHIOIEO", "HYEICXSTN", "BSKSOLGSI", "SSEBSGGSR", "OSUGCHOOP" },
                W("SOCCER", 5,6, 4,5, 3,4, 2,3, 1,2, 0,1), W("TENNIS", 5,7, 4,7, 3,7, 2,7, 1,7, 0,7), W("GOLF", 1,6, 1,5, 1,4, 1,3), W("HOCKEY", 5,0, 4,0, 3,0, 2,0, 1,0, 0,0), W("RUGBY", 1,1, 2,1, 3,1, 4,1, 5,1), W("BOXING", 7,3, 6,4, 5,5, 4,6, 3,7, 2,8), W("CHESS", 3,4, 4,3, 5,2, 6,1, 7,0), W("SKIING", 7,1, 6,2, 5,3, 4,4, 3,5, 2,6)),
            new LevelDef("GARDEN", new[] { "UPCSELGEK", "ILPECVLKS", "THUKSLTAR", "MCTNALPRR", "OYMBEEHHL", "OEAVNCEOO", "LIOSAWNSU", "BHRETAWEF", "SEEDELLSF" },
                W("SEED", 8,0, 8,1, 8,2, 8,3), W("SOIL", 6,3, 6,2, 6,1, 6,0), W("HOSE", 4,7, 5,7, 6,7, 7,7), W("RAKE", 3,7, 2,7, 1,7, 0,7), W("SHOVEL", 8,0, 7,1, 6,2, 5,3, 4,4, 3,5), W("FENCE", 8,8, 7,7, 6,6, 5,5, 4,4), W("PLANT", 3,6, 3,5, 3,4, 3,3, 3,2), W("WATER", 7,6, 7,5, 7,4, 7,3, 7,2), W("BLOOM", 7,0, 6,0, 5,0, 4,0, 3,0)),
            new LevelDef("DESSERTS", new[] { "TATOYUIFC", "AUUDFBNTF", "RKNTDRTEU", "TAGOMODLD", "CURUDWVFG", "PUDDINGFE", "ICOOKIEAD", "UPCAKEAWG", "NIFFUMTRC" },
                W("CAKE", 7,2, 7,3, 7,4, 7,5), W("COOKIE", 6,1, 6,2, 6,3, 6,4, 6,5, 6,6), W("DONUT", 4,4, 3,3, 2,2, 1,1, 0,0), W("PUDDING", 5,0, 5,1, 5,2, 5,3, 5,4, 5,5, 5,6), W("CANDY", 4,0, 3,1, 2,2, 1,3, 0,4), W("MUFFIN", 8,5, 8,4, 8,3, 8,2, 8,1, 8,0), W("BROWNIE", 1,5, 2,5, 3,5, 4,5, 5,5, 6,5, 7,5), W("WAFFLE", 7,7, 6,7, 5,7, 4,7, 3,7, 2,7), W("TART", 0,0, 1,0, 2,0, 3,0), W("FUDGE", 1,8, 2,8, 3,8, 4,8, 5,8)),
            new LevelDef("MOUNTAINS", new[] { "PSLOPEAKD", "FMENGCRAA", "SFMDAERER", "EVINDEVSG", "ARYLIYAUN", "COUCCKLMY", "NOAMPALMF", "BLIARTEIV", "GTBPASYTE" },
                W("PEAK", 0,4, 0,5, 0,6, 0,7), W("CLIFF", 5,4, 4,3, 3,2, 2,1, 1,0), W("VALLEY", 3,6, 4,6, 5,6, 6,6, 7,6, 8,6), W("GLACIER", 8,0, 7,1, 6,2, 5,3, 4,4, 3,5, 2,6), W("SUMMIT", 3,7, 4,7, 5,7, 6,7, 7,7, 8,7), W("RIDGE", 4,1, 3,2, 2,3, 1,4, 0,5), W("CANYON", 1,5, 2,4, 3,3, 4,2, 5,1, 6,0), W("BOULDER", 7,0, 6,1, 5,2, 4,3, 3,4, 2,5, 1,6), W("TRAIL", 7,5, 7,4, 7,3, 7,2, 7,1), W("SLOPE", 0,1, 0,2, 0,3, 0,4, 0,5)),
            new LevelDef("CAMPING", new[] { "KNIFECTTTA", "CTWTWLOPKE", "OKFGHYRLWR", "MHCHRECNYI", "PHSAIVHRNF", "AMCRPKPEFP", "SLATNKITWM", "SKNPNOCNLA", "VEOCRCTAGC", "TREANYCLBK" },
                W("TENT", 9,0, 8,1, 7,2, 6,3), W("CAMPFIRE", 8,9, 7,9, 6,9, 5,9, 4,9, 3,9, 2,9, 1,9), W("LANTERN", 9,7, 8,7, 7,7, 6,7, 5,7, 4,7, 3,7), W("BACKPACK", 9,8, 8,7, 7,6, 6,5, 5,4, 4,3, 3,2, 2,1), W("COMPASS", 1,0, 2,0, 3,0, 4,0, 5,0, 6,0, 7,0), W("HIKING", 3,3, 4,4, 5,5, 6,6, 7,7, 8,8), W("CANOE", 5,2, 6,2, 7,2, 8,2, 9,2), W("MAP", 5,1, 6,2, 7,3), W("TORCH", 0,6, 1,6, 2,6, 3,6, 4,6), W("KNIFE", 0,0, 0,1, 0,2, 0,3, 0,4)),
            new LevelDef("TRAVEL", new[] { "UYENRUOJKT", "PYTICKETRR", "BMLVSGIOSO", "HUREAIPAVP", "OSSGSRRYTS", "TEGTIIARVS", "EUSAIAUIAA", "LMDJSDSROP", "YHMIOBEACH", "RUVTOURIST" },
                W("PASSPORT", 7,9, 6,9, 5,9, 4,9, 3,9, 2,9, 1,9, 0,9), W("TICKET", 1,2, 1,3, 1,4, 1,5, 1,6, 1,7), W("AIRPORT", 6,3, 5,4, 4,5, 3,6, 2,7, 1,8, 0,9), W("HOTEL", 3,0, 4,0, 5,0, 6,0, 7,0), W("LUGGAGE", 7,0, 6,1, 5,2, 4,3, 3,4, 2,5, 1,6), W("CRUISE", 8,8, 7,7, 6,6, 5,5, 4,4, 3,3), W("BEACH", 8,5, 8,6, 8,7, 8,8, 8,9), W("MUSEUM", 2,1, 3,1, 4,1, 5,1, 6,1, 7,1), W("JOURNEY", 0,7, 0,6, 0,5, 0,4, 0,3, 0,2, 0,1), W("VISA", 9,2, 8,3, 7,4, 6,5), W("TOURIST", 9,3, 9,4, 9,5, 9,6, 9,7, 9,8, 9,9)),
            new LevelDef("SCHOOL", new[] { "HCECOYRDNO", "NREHCAETEL", "GLSABAMYLA", "OEHLIBRARY", "ESTKROPENO", "RSHOMEWORK", "KORENELADK", "ONLCTPAUSS", "OWIARESARE", "BLSCIENCED" },
                W("TEACHER", 1,7, 1,6, 1,5, 1,4, 1,3, 1,2, 1,1), W("PENCIL", 4,6, 5,5, 6,4, 7,3, 8,2, 9,1), W("ERASER", 8,9, 8,8, 8,7, 8,6, 8,5, 8,4), W("LIBRARY", 3,3, 3,4, 3,5, 3,6, 3,7, 3,8, 3,9), W("LESSON", 2,1, 3,1, 4,1, 5,1, 6,1, 7,1), W("HOMEWORK", 5,2, 5,3, 5,4, 5,5, 5,6, 5,7, 5,8, 5,9), W("RULER", 8,8, 7,7, 6,6, 5,5, 4,4), W("DESK", 9,9, 8,9, 7,9, 6,9), W("CHALK", 0,3, 1,3, 2,3, 3,3, 4,3), W("SCIENCE", 9,2, 9,3, 9,4, 9,5, 9,6, 9,7, 9,8), W("BOOK", 9,0, 8,0, 7,0, 6,0)),
            new LevelDef("JOBS", new[] { "ETYHFTBHUP", "SLSHSASNLL", "TOLIPBRFEK", "GVTPTNTMRE", "DRNRENGNES", "APLUMBERYR", "SROTCODDWU", "NBUFEHCHAN", "ERSINGERLC", "WRITEREKAB" },
                W("DOCTOR", 6,6, 6,5, 6,4, 6,3, 6,2, 6,1), W("NURSE", 7,9, 6,9, 5,9, 4,9, 3,9), W("PILOT", 2,4, 2,3, 2,2, 2,1, 2,0), W("CHEF", 7,6, 7,5, 7,4, 7,3), W("FARMER", 0,4, 1,5, 2,6, 3,7, 4,8, 5,9), W("ARTIST", 5,0, 4,1, 3,2, 2,3, 1,4, 0,5), W("LAWYER", 8,8, 7,8, 6,8, 5,8, 4,8, 3,8), W("DENTIST", 6,7, 5,6, 4,5, 3,4, 2,3, 1,2, 0,1), W("PLUMBER", 5,1, 5,2, 5,3, 5,4, 5,5, 5,6, 5,7), W("BAKER", 9,9, 9,8, 9,7, 9,6, 9,5), W("WRITER", 9,0, 9,1, 9,2, 9,3, 9,4, 9,5), W("SINGER", 8,2, 8,3, 8,4, 8,5, 8,6, 8,7)),
            new LevelDef("HOUSE", new[] { "SOFAHBWHEK", "CGLWMIRROR", "RHWNNORRMS", "KMIDVNFOOR", "OIOMAPGOAI", "EWTONAMDRA", "ENACREOAGT", "PWGAHDYMLS", "ROGOEEEGSP", "TEPRACNBMI" },
                W("KITCHEN", 3,0, 4,1, 5,2, 6,3, 7,4, 8,5, 9,6), W("BEDROOM", 9,7, 8,6, 7,5, 6,4, 5,3, 4,2, 3,1), W("WINDOW", 0,6, 1,5, 2,4, 3,3, 4,2, 5,1), W("DOOR", 5,7, 4,7, 3,7, 2,7), W("ROOF", 3,9, 3,8, 3,7, 3,6), W("GARAGE", 4,6, 5,5, 6,4, 7,3, 8,2, 9,1), W("STAIRS", 7,9, 6,9, 5,9, 4,9, 3,9, 2,9), W("CHIMNEY", 1,0, 2,1, 3,2, 4,3, 5,4, 6,5, 7,6), W("SOFA", 0,0, 0,1, 0,2, 0,3), W("LAMP", 7,8, 6,7, 5,6, 4,5), W("CARPET", 9,5, 9,4, 9,3, 9,2, 9,1, 9,0), W("MIRROR", 1,4, 1,5, 1,6, 1,7, 1,8, 1,9)),
            new LevelDef("SPACE", new[] { "MRGYETEMOC", "SETNXLSMDR", "NTEERARBOU", "RINBOSLENA", "UPAUNTTADT", "TULLUESRGE", "AJPAMRSETK", "SIAYSOMSOC", "KYECLIPSEO", "TIBRODGMKR" },
                W("PLANET", 6,2, 5,2, 4,2, 3,2, 2,2, 1,2), W("COMET", 0,9, 0,8, 0,7, 0,6, 0,5), W("GALAXY", 5,8, 4,7, 3,6, 2,5, 1,4, 0,3), W("ROCKET", 9,9, 8,9, 7,9, 6,9, 5,9, 4,9), W("ASTEROID", 2,5, 3,5, 4,5, 5,5, 6,5, 7,5, 8,5, 9,5), W("NEBULA", 1,3, 2,3, 3,3, 4,3, 5,3, 6,3), W("ORBIT", 9,4, 9,3, 9,2, 9,1, 9,0), W("METEOR", 6,4, 5,5, 4,6, 3,7, 2,8, 1,9), W("SATURN", 7,0, 6,0, 5,0, 4,0, 3,0, 2,0), W("JUPITER", 6,1, 5,1, 4,1, 3,1, 2,1, 1,1, 0,1), W("ECLIPSE", 8,2, 8,3, 8,4, 8,5, 8,6, 8,7, 8,8), W("COSMOS", 7,9, 7,8, 7,7, 7,6, 7,5, 7,4)),
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
