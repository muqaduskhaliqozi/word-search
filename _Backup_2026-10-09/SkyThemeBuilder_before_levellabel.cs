using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

/// <summary>
/// Re-skins the Splash and Main Menu scenes to the "Sky" mockups
/// (blue sky, Word Search logo, green/blue pill buttons, yellow icon tiles, Settings screen).
/// Menu: Tools > Word Search > Apply Sky Menu Theme
/// All positions are in 1080x1920 design pixels measured from the mockups (x from left, y from top).
/// Old UI under the canvas is only deactivated (not deleted), so nothing is lost.
/// </summary>
public static class SkyThemeBuilder
{
    private const string Root = "Assets/WordSearchTheme";
    private const string SpriteDir = Root + "/SkySprites/";
    private const string FontDir = Root + "/Fonts/";
    private const string SplashScene = "Assets/Scenes/SplashScene.unity";
    private const string MenuScene = "Assets/Scenes/MainMenu.unity";
    private const string GameScene = "Assets/Scenes/GamePlay.unity";
    private const string RootName = "SK_Root";

    private const float W = 1080f, H = 1920f;

    private static TMP_FontAsset fredoka, lato;

    private static Color C(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out Color c); return c; }

    // ======================================================================
    [MenuItem("Tools/Word Search/Apply Sky Theme (Splash + Menu + Gameplay)")]
    public static void ApplyAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string previous = EditorSceneManager.GetActiveScene().path;
        try
        {
            EditorUtility.DisplayProgressBar("Sky Theme", "Importing sprites...", 0.1f);
            ImportSprites();
            EditorUtility.DisplayProgressBar("Sky Theme", "Fonts...", 0.25f);
            LoadFonts();
            BuildRuntimeResources();
            EditorUtility.DisplayProgressBar("Sky Theme", "Splash...", 0.5f);
            ProcessScene(SplashScene, BuildSplash);
            EditorUtility.DisplayProgressBar("Sky Theme", "Main menu + settings...", 0.75f);
            ProcessScene(MenuScene, BuildMenu);
            EditorUtility.DisplayProgressBar("Sky Theme", "Gameplay (all levels)...", 0.9f);
            ProcessScene(GameScene, BuildGameplay);
        }
        finally { EditorUtility.ClearProgressBar(); }

        PortableFeaturesEditor.ConfigureNotifications();
        AssetDatabase.SaveAssets();
        if (!string.IsNullOrEmpty(previous) && File.Exists(previous)) EditorSceneManager.OpenScene(previous);
        Debug.Log("<color=#1E8CF0><b>[SkyTheme]</b></color> Splash + Main Menu + Settings rebuilt. Press Play on SplashScene.");
    }

    private static void ProcessScene(string path, System.Action build)
    {
        if (!File.Exists(path)) { Debug.LogWarning("[SkyTheme] Scene not found: " + path); return; }
        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        try
        {
            build();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[SkyTheme] Updated " + path);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[SkyTheme] FAILED on " + path + " - scene not saved:\n" + e);
        }
    }

    // ======================================================================
    // ASSETS
    // ======================================================================
    private static readonly Dictionary<string, Vector4> Borders = new Dictionary<string, Vector4>
    {
        { "sk_card_sliceable", new Vector4(88, 88, 88, 88) },
        { "sk_card_header", new Vector4(68, 68, 68, 124) },
        { "sk_chip", new Vector4(34, 0, 34, 0) },
        { "sk_chip_found", new Vector4(34, 0, 34, 0) },
        { "at_bar_bg", new Vector4(35, 0, 35, 0) },
        { "at_bar_fill", new Vector4(28, 0, 28, 0) },
        { "at_btn_green", new Vector4(80, 0, 80, 0) },
        { "at_btn_blue", new Vector4(80, 0, 80, 0) },
        { "sk_bar_fill", new Vector4(23, 0, 23, 0) },
        { "sk_bar_bg", new Vector4(35, 0, 35, 0) },
        { "sk_line", new Vector4(2, 2, 2, 2) },
    };

    private static void ImportSprites()
    {
        foreach (string file in Directory.GetFiles(SpriteDir, "*.png"))
        {
            string path = file.Replace("\\", "/");
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) { AssetDatabase.ImportAsset(path); ti = AssetImporter.GetAtPath(path) as TextureImporter; }
            if (ti == null) continue;
            string key = Path.GetFileNameWithoutExtension(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.filterMode = FilterMode.Bilinear;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.textureCompression = TextureImporterCompression.Compressed;
            ti.crunchedCompression = false;
            ti.spritePixelsPerUnit = 100;
            ti.maxTextureSize = 2048;
            ti.spriteBorder = Borders.TryGetValue(key, out Vector4 b) ? b : Vector4.zero;
            TextureImporterSettings s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.spriteMeshType = SpriteMeshType.FullRect;
            ti.SetTextureSettings(s);
            ti.SaveAndReimport();
        }
    }

    private static Sprite S(string name)
    {
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + name + ".png");
        if (s == null) Debug.LogWarning("[SkyTheme] Sprite not found: " + name);
        return s;
    }

    private static void LoadFonts()
    {
        fredoka = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontDir + "Fredoka-Bold SDF.asset");
        if (fredoka == null) fredoka = MakeFont("Fredoka-Bold");
        lato = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontDir + "Lato-Bold SDF.asset");
        if (lato == null) lato = MakeFont("Lato-Bold");
        if (fredoka == null) fredoka = TMP_Settings.defaultFontAsset;
        if (lato == null) lato = fredoka;
    }

    private static TMP_FontAsset MakeFont(string name)
    {
        string assetPath = FontDir + name + " SDF.asset";
        Font ttf = AssetDatabase.LoadAssetAtPath<Font>(FontDir + name + ".ttf");
        if (ttf == null) return null;
        TMP_FontAsset fa = TMP_FontAsset.CreateFontAsset(ttf, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
        if (fa == null) return null;
        fa.name = name + " SDF";
        fa.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,:;!?'\"+-*/%&()#@<>=_");
        AssetDatabase.CreateAsset(fa, assetPath);
        if (fa.atlasTextures != null)
        {
            for (int i = 0; i < fa.atlasTextures.Length; i++)
            {
                if (fa.atlasTextures[i] == null) continue;
                fa.atlasTextures[i].name = name + " SDF Atlas " + i;
                AssetDatabase.AddObjectToAsset(fa.atlasTextures[i], fa);
            }
        }
        fa.material.name = name + " SDF Material";
        AssetDatabase.AddObjectToAsset(fa.material, fa);
        EditorUtility.SetDirty(fa);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(assetPath);
        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
    }

    // ======================================================================
    // HELPERS
    // ======================================================================
    private enum Anchor { Top, Middle, Bottom }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }

    private static RectTransform Ensure(Transform parent, string name)
    {
        Transform t = parent.Find(name);
        if (t == null)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            t = go.transform;
        }
        t.gameObject.SetActive(true);
        return (RectTransform)t;
    }

    private static RectTransform Stretch(RectTransform rt, float l = 0, float b = 0, float r = 0, float t = 0)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
        rt.localScale = Vector3.one; rt.localRotation = Quaternion.identity;
        return rt;
    }

    /// <summary>Places a rect by its centre in design pixels (x from left, y from top of a 1080x1920 screen).</summary>
    private static RectTransform At(RectTransform rt, Anchor a, float x, float y, Vector2 size)
    {
        float ay = a == Anchor.Top ? 1f : a == Anchor.Bottom ? 0f : 0.5f;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, ay);
        rt.pivot = new Vector2(0.5f, 0.5f);
        float py = a == Anchor.Top ? -y : a == Anchor.Bottom ? H - y : H / 2f - y;
        rt.anchoredPosition = new Vector2(x - W / 2f, py);
        rt.sizeDelta = size;
        rt.localScale = Vector3.one; rt.localRotation = Quaternion.identity;
        return rt;
    }

    /// <summary>Moves children of a previous run's "SafeArea" node back up so the builder finds them again.</summary>
    private static void Unwrap(Transform parent)
    {
        Transform node = parent.Find("SafeArea");
        if (node == null) return;
        int index = node.GetSiblingIndex();
        var kids = new List<Transform>();
        foreach (Transform k in node) kids.Add(k);
        foreach (Transform k in kids) { k.SetParent(parent, false); k.SetSiblingIndex(index++); }
        Object.DestroyImmediate(node.gameObject);
    }

    /// <summary>Art + fonts for the panels that build themselves at runtime (consent, no-internet, offer).</summary>
    private static void BuildRuntimeResources()
    {
        const string dir = Root + "/Resources";
        const string path = dir + "/SkyUIResources.asset";
        if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder(Root, "Resources");
        SkyUIResources res = AssetDatabase.LoadAssetAtPath<SkyUIResources>(path);
        if (res == null) { res = ScriptableObject.CreateInstance<SkyUIResources>(); AssetDatabase.CreateAsset(res, path); }
        res.font = fredoka;
        res.buttonFont = lato;
        res.headerCard = S("sk_card_header");
        res.plainCard = S("sk_card_sliceable");
        res.greenButton = S("at_btn_green");
        res.blueButton = S("at_btn_blue");
        res.closeButton = S("sk_btn_close");
        res.noAdsBadge = S("at_noads");
        res.logo = S("at_logo");
        res.coin = S("at_coin");
        EditorUtility.SetDirty(res);
        AssetDatabase.SaveAssets();
    }

    private static Vector2 Size(Sprite s, float scale = 1f) => s != null ? s.rect.size * scale : new Vector2(100, 100);

    private static Image Img(GameObject go, Sprite s, bool raycast = false, Image.Type type = Image.Type.Simple)
    {
        Image img = GetOrAdd<Image>(go);
        img.sprite = s; img.type = type; img.color = Color.white;
        img.preserveAspect = false; img.raycastTarget = raycast; img.pixelsPerUnitMultiplier = 1f;
        return img;
    }

    /// <summary>Sprite image at its native size, centred at (x, y).</summary>
    /// <param name="solidDy">Sprite pixels from the sprite centre to the centre of its solid face
    /// (negative = face sits above the centre because of a drop shadow). (x, y) is where the FACE centre goes.</param>
    private static Image Pic(Transform parent, string name, string sprite, Anchor a, float x, float y, float scale = 1f, bool raycast = false, float solidDy = 0f)
    {
        Sprite s = S(sprite);
        RectTransform rt = At(Ensure(parent, name), a, x, y - solidDy * scale, Size(s, scale));
        return Img(rt.gameObject, s, raycast);
    }

    /// <summary>9-sliced sprite at an arbitrary size; corners scaled by cornerScale.</summary>
    private static Image PicSliced(Transform parent, string name, string sprite, Anchor a, float x, float y, Vector2 size, float cornerScale, bool raycast = false)
    {
        Sprite s = S(sprite);
        RectTransform rt = At(Ensure(parent, name), a, x, y, size);
        Image img = Img(rt.gameObject, s, raycast, Image.Type.Sliced);
        img.pixelsPerUnitMultiplier = 1f / Mathf.Max(0.01f, cornerScale);
        return img;
    }

    private static TextMeshProUGUI Txt(GameObject go, string text, TMP_FontAsset f, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center, bool auto = false)
    {
        TextMeshProUGUI t = GetOrAdd<TextMeshProUGUI>(go);
        t.font = f; t.fontSharedMaterial = f != null ? f.material : null;
        t.text = text; t.fontSize = size; t.fontStyle = FontStyles.Normal;
        t.enableAutoSizing = auto; t.fontSizeMax = size; t.fontSizeMin = size * 0.6f;
        t.color = color; t.alignment = align; t.enableWordWrapping = false;
        t.overflowMode = TextOverflowModes.Overflow; t.raycastTarget = false;
        t.characterSpacing = 0f; t.margin = Vector4.zero; t.enableVertexGradient = false;
        return t;
    }

    private static TextMeshProUGUI Label(Transform parent, string name, string text, TMP_FontAsset f, float size, Color color, Anchor a, float x, float y, float width, TextAlignmentOptions align = TextAlignmentOptions.Center, bool auto = false)
    {
        RectTransform rt = At(Ensure(parent, name), a, x, y, new Vector2(width, size * 1.6f));
        return Txt(rt.gameObject, text, f, size, color, align, auto);
    }

    /// <summary>Text that fills the face of a glossy button sprite (padding + bottom rim excluded).</summary>
    private static TextMeshProUGUI FaceText(RectTransform button, string text, TMP_FontAsset f, float size, Color color, Vector4 pad, float rim)
    {
        RectTransform rt = Stretch(Ensure(button, "SK_Text"), pad.x, pad.w + rim, pad.z, pad.y);
        return Txt(rt.gameObject, text, f, size, color);
    }

    private static Button MakeButton(GameObject go, Graphic target)
    {
        Button b = GetOrAdd<Button>(go);
        b.transition = Selectable.Transition.None;
        b.targetGraphic = target;
        if (target != null) target.raycastTarget = true;
        Navigation nav = b.navigation; nav.mode = Navigation.Mode.None; b.navigation = nav;
        GetOrAdd<UIButtonJuice>(go).SetIdlePulse(false, 0f);
        return b;
    }

    private static Button PicButton(Transform parent, string name, string sprite, Anchor a, float x, float y, float scale = 1f, float solidDy = 0f)
    {
        Image img = Pic(parent, name, sprite, a, x, y, scale, true, solidDy);
        return MakeButton(img.gameObject, img);
    }

    // Artist atlas sprites: offset from sprite centre to solid face centre, and the face rect inside the sprite (l, t, r, b insets)
    private const float DyGreen = -12f, DyBlue = -8.5f, DySquare = -10f, DyNoAds = -7f, DyTile = -11f;
    private static readonly Vector4 GreenFace = new Vector4(14, 2, 14, 38);
    private static readonly Vector4 BlueFace = new Vector4(13, 4, 13, 33);
    private static Vector4 Sc(Vector4 v, float s) => v * s;

    private static void PopIn(Component c, float delay, Vector2 slide, float fromScale = 0.6f)
    {
        GetOrAdd<UIPopIn>(c.gameObject).Configure(delay, slide, fromScale, 0.45f);
    }

    private static void SetField(Object target, string field, Object value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if (p == null) { Debug.LogWarning($"[SkyTheme] Field '{field}' not found on {target}"); return; }
        p.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetColor(Object target, string field, Color value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if (p == null) return;
        p.colorValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Canvas MainCanvas()
    {
        foreach (GameObject root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
        {
            Canvas c = root.GetComponentInChildren<Canvas>(true);
            if (c != null && c.isRootCanvas) return c;
        }
        return null;
    }

    /// <summary>Hides the old UI (keeps it in the scene) and returns a fresh full-screen root for the new UI.</summary>
    private static RectTransform PrepareCanvas(Canvas canvas)
    {
        CanvasScaler scaler = GetOrAdd<CanvasScaler>(canvas.gameObject);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(W, H);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;
        GetOrAdd<CanvasAutoMatch>(canvas.gameObject);

        foreach (Transform child in canvas.transform)
            if (child.name != RootName) child.gameObject.SetActive(false);

        RectTransform root = Stretch(Ensure(canvas.transform, RootName));
        Unwrap(root);
        root.SetAsLastSibling();
        return root;
    }

    private static void Background(Transform parent, string sprite)
    {
        RectTransform bg = Stretch(Ensure(parent, "SK_Bg"));
        bg.SetAsFirstSibling();
        Img(bg.gameObject, S(sprite), true); // raycast so nothing behind is clickable
        AspectRatioFitter f = GetOrAdd<AspectRatioFitter>(bg.gameObject);
        f.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        f.aspectRatio = W / H;
    }

    // glossy sprite paddings (left, top, right, bottom) + bottom rim, matching gen.py
    private static readonly Vector4 PillPad = new Vector4(18, 10, 18, 28);
    private const float PillRim = 12f;
    private static readonly Vector4 SmallPad = new Vector4(10, 6, 10, 16);
    private const float SmallRim = 9f;

    // ======================================================================
    // SPLASH
    // ======================================================================
    private static void BuildSplash()
    {
        Canvas canvas = MainCanvas();
        if (canvas == null) { Debug.LogError("[SkyTheme] Splash: no canvas"); return; }
        RectTransform root = PrepareCanvas(canvas);
        Background(root, "at_bg");

        Image logo = Pic(root, "SK_Logo", "at_logo", Anchor.Top, 540, 595, 1.0f);
        PopIn(logo, 0.15f, new Vector2(0, 40), 0.7f);

        TextMeshProUGUI loading = Label(root, "SK_LoadingText", "Loading...", fredoka, 60, C("343434"), Anchor.Bottom, 540, 1540, 600);

        // slider
        Sprite barBg = S("at_bar_bg");
        RectTransform bar = At(Ensure(root, "SK_LoadingBar"), Anchor.Bottom, 540, 1630, Size(barBg));
        RectTransform bgr = Stretch(Ensure(bar, "Background"));
        Img(bgr.gameObject, barBg, false, Image.Type.Sliced);
        RectTransform area = Stretch(Ensure(bar, "Fill Area"), 25, 7, 25, 8);
        RectTransform fill = Ensure(area, "Fill");
        fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(0f, 1f); fill.pivot = new Vector2(0.5f, 0.5f);
        fill.offsetMin = Vector2.zero; fill.offsetMax = Vector2.zero; fill.localScale = Vector3.one;
        Img(fill.gameObject, S("at_bar_fill"), false, Image.Type.Sliced);
        Slider slider = GetOrAdd<Slider>(bar.gameObject);
        slider.fillRect = fill; slider.handleRect = null; slider.targetGraphic = null;
        slider.interactable = false; slider.transition = Selectable.Transition.None;
        slider.direction = Slider.Direction.LeftToRight; slider.minValue = 0f; slider.maxValue = 1f; slider.value = 0.6f;

        SplashController ctrl = Object.FindObjectOfType<SplashController>(true);
        if (ctrl != null)
        {
            SetField(ctrl, "loadingBar", slider);
            SetField(ctrl, "loadingText", loading);
            SetField(ctrl, "barShine", null);
            EditorUtility.SetDirty(ctrl);
        }
        else Debug.LogWarning("[SkyTheme] SplashController not found in the splash scene.");

        SafeArea.Wrap(root, "SK_Bg");
    }

    // ======================================================================
    // MAIN MENU + SETTINGS + LEVEL SELECT
    // ======================================================================
    private static void BuildMenu()
    {
        Canvas canvas = MainCanvas();
        if (canvas == null) { Debug.LogError("[SkyTheme] Menu: no canvas"); return; }

        // keep the level titles from the old level-select popup
        LevelSelectPanel oldPanel = null;
        foreach (LevelSelectPanel p in canvas.GetComponentsInChildren<LevelSelectPanel>(true))
            if (p.gameObject.name != "SK_LevelSelect") { oldPanel = p; break; }

        RectTransform root = PrepareCanvas(canvas);
        Background(root, "at_bg");

        // ---------------- top bar
        Button settingsBtn = PicButton(root, "SK_SettingsButton", "at_btn_settings", Anchor.Top, 84, 103, 1.0f, DySquare);
        Button noAdsBtn = PicButton(root, "SK_NoAdsButton", "at_noads", Anchor.Top, 979, 119, 0.94f, DyNoAds);

        // ---------------- logo
        Image logo = Pic(root, "SK_Logo", "at_logo", Anchor.Top, 540, 550, 0.975f);
        PopIn(logo, 0.1f, new Vector2(0, 40), 0.7f);

        // ---------------- main buttons
        Button levels = PicButton(root, "SK_LevelsButton", "at_btn_green", Anchor.Middle, 540, 1254, 0.99f, DyGreen);
        FaceText((RectTransform)levels.transform, "Levels", lato, 54, C("13570D"), Sc(GreenFace, 0.99f), 0f);
        PopIn(levels, 0.3f, new Vector2(0, -40));
        Button newGame = PicButton(root, "SK_NewGameButton", "at_btn_blue", Anchor.Middle, 540, 1421, 0.99f, DyBlue);
        FaceText((RectTransform)newGame.transform, "New Game", lato, 50, C("0E3D8E"), Sc(BlueFace, 0.99f), 0f);
        PopIn(newGame, 0.38f, new Vector2(0, -40));

        // ---------------- bottom tiles
        string[] tiles = { "at_tile_trophy", "at_tile_rate", "at_tile_privacy", "at_tile_more" };
        string[] names = { "Leaderboard", "Rate us", "Privacy Policy", "More Game" };
        float[] xs = { 285, 454, 622, 791 };
        Button[] tileButtons = new Button[4];
        for (int i = 0; i < 4; i++)
        {
            tileButtons[i] = PicButton(root, "SK_Tile_" + names[i].Replace(" ", ""), tiles[i], Anchor.Bottom, xs[i], 1664, 1.0f, DyTile);
            PopIn(tileButtons[i], 0.45f + i * 0.05f, new Vector2(0, -30));
            TextMeshProUGUI l = Label(root, "SK_TileLabel_" + names[i].Replace(" ", ""), names[i], fredoka, 24, C("4A4A4A"), Anchor.Bottom, xs[i], 1769, 190, TextAlignmentOptions.Center, true);
            PopIn(l, 0.5f + i * 0.05f, Vector2.zero, 1f);
        }

        // ---------------- settings screen
        SettingsRefs sr = BuildSettings(root);

        // ---------------- level select
        LevelSelectPanel panel = BuildLevelSelect(root, oldPanel);

        // ---------------- wiring
        MainMenuController ctrl = Object.FindObjectOfType<MainMenuController>(true);
        if (ctrl != null)
        {
            SetField(ctrl, "levelLabel", null);
            SetField(ctrl, "levelsButton", levels);
            SetField(ctrl, "levelSelect", panel);
            EditorUtility.SetDirty(ctrl);
        }
        else Debug.LogWarning("[SkyTheme] MainMenuController not found in the menu scene.");

        SkyMenu sky = GetOrAdd<SkyMenu>(root.gameObject);
        SetField(sky, "menu", ctrl);
        SetField(sky, "settingsButton", settingsBtn);
        SetField(sky, "noAdsButton", noAdsBtn);
        SetField(sky, "newGameButton", newGame);
        SetField(sky, "leaderboardButton", tileButtons[0]);
        SetField(sky, "rateButton", tileButtons[1]);
        SetField(sky, "privacyButton", tileButtons[2]);
        SetField(sky, "moreGamesButton", tileButtons[3]);
        SetField(sky, "settingsScreen", sr.screen);
        SetField(sky, "settingsBackButton", sr.back);
        SetField(sky, "removeAdsCard", sr.adsCard);
        SetField(sky, "removeAdsBuyButton", sr.buy);
        SetField(sky, "priceText", sr.price);
        SetField(sky, "soundToggle", sr.sound);
        SetField(sky, "musicToggle", sr.music);
        SetField(sky, "vibrateToggle", sr.vibrate);
        SetField(sky, "toggleOn", S("at_toggle_on"));
        SetField(sky, "toggleOff", S("at_toggle_off"));
        SetField(sky, "settingsPrivacyButton", sr.privacy);
        SetField(sky, "termsButton", sr.terms);
        EditorUtility.SetDirty(sky);

        sr.screen.transform.SetAsLastSibling();
        panel.transform.SetAsLastSibling();
        SafeArea.Wrap(sr.screen.transform, "SK_Bg");
        SafeArea.Wrap(panel.transform);
        SafeArea.Wrap(root, "SK_Bg", "SK_Settings", "SK_LevelSelect");
        sr.screen.SetActive(false);
        panel.gameObject.SetActive(false);
    }

    private struct SettingsRefs
    {
        public GameObject screen, adsCard;
        public Button back, buy, sound, music, vibrate, privacy, terms;
        public TMP_Text price;
    }

    private static SettingsRefs BuildSettings(Transform root)
    {
        SettingsRefs r = new SettingsRefs();
        RectTransform screen = Stretch(Ensure(root, "SK_Settings"));
        Unwrap(screen);
        r.screen = screen.gameObject;
        Background(screen, "sk_bg_settings");

        r.back = PicButton(screen, "SK_Back", "at_btn_back", Anchor.Top, 129, 80, 0.89f, DySquare);
        Label(screen, "SK_Title", "Settings", fredoka, 62, C("2B4457"), Anchor.Top, 540, 71, 600);

        // ---------- Remove Ads card
        Image card = Pic(screen, "SK_RemoveAdsCard", "sk_card_ads", Anchor.Top, 540, 380);
        r.adsCard = card.gameObject;
        Transform c = card.transform;
        // children use the same design coordinates: wrap them in a full-screen holder inside the card
        RectTransform holder = Ensure(c, "SK_Content");
        holder.anchorMin = holder.anchorMax = new Vector2(0.5f, 0.5f); holder.pivot = new Vector2(0.5f, 0.5f);
        holder.sizeDelta = new Vector2(W, H); holder.anchoredPosition = new Vector2(0, 380 - H / 2f);
        // (holder's top edge = screen top, so Anchor.Top coordinates line up with the mockup)
        Label(holder, "SK_Header", "Remove Ads", fredoka, 48, Color.white, Anchor.Top, 540, 272, 600);
        Pic(holder, "SK_AdsIcon", "at_noads", Anchor.Top, 248, 410, 1.06f, false, DyNoAds);
        Label(holder, "SK_Desc", "Remove Banner and Pop-Up Ads", fredoka, 40, C("1E90F0"), Anchor.Top, 686, 367, 600, TextAlignmentOptions.Center, true);
        Image rib = Pic(holder, "SK_Ribbon", "sk_ribbon", Anchor.Top, 513, 457);
        Txt(Stretch(Ensure(rib.transform, "SK_Text"), 20, 4, 20, 0).gameObject, "50% Off", fredoka, 36, Color.white);
        const float pv = 0.66f; // price button: artist's green pill, 9-sliced to 270 x 82 face
        Image buyImg = PicSliced(holder, "SK_Buy", "at_btn_green", Anchor.Top, 847, 457 - DyGreen * pv, new Vector2(270 + 28 * pv, 164 * pv), pv, true);
        r.buy = MakeButton(buyImg.gameObject, buyImg);
        r.price = FaceText((RectTransform)r.buy.transform, "Rs: 2990", fredoka, 38, C("2E7D0A"), Sc(GreenFace, pv), 0f);

        // ---------- toggles card
        Pic(screen, "SK_OptionsCard", "sk_card", Anchor.Top, 540, 824);
        string[] icons = { "sk_ic_sound", "sk_ic_music", "sk_ic_vibrate" };
        string[] labels = { "Sound", "Music", "Vibrate" };
        float[] ys = { 705, 826, 949 };
        Button[] toggles = new Button[3];
        for (int i = 0; i < 3; i++)
        {
            Pic(screen, "SK_Icon_" + labels[i], icons[i], Anchor.Top, 177, ys[i]);
            Label(screen, "SK_Label_" + labels[i], labels[i], fredoka, 36, C("1E90F0"), Anchor.Top, 398, ys[i] + 2, 300, TextAlignmentOptions.Left);
            toggles[i] = PicButton(screen, "SK_Toggle_" + labels[i], i == 1 ? "at_toggle_on" : "at_toggle_off", Anchor.Top, 903, ys[i]);
        }
        r.sound = toggles[0]; r.music = toggles[1]; r.vibrate = toggles[2];
        foreach (float y in new float[] { 765, 886 })
        {
            RectTransform sep = At(Ensure(screen, "SK_Sep_" + y), Anchor.Top, 560, y, new Vector2(820, 3));
            Img(sep.gameObject, S("sk_line"), false, Image.Type.Sliced);
        }

        // ---------- bottom buttons
        r.privacy = PicButton(screen, "SK_PrivacyButton", "at_btn_green", Anchor.Top, 540, 1166, 1.0f, DyGreen);
        FaceText((RectTransform)r.privacy.transform, "Privacy Policy", fredoka, 46, C("1D5A07"), GreenFace, 0f);
        r.terms = PicButton(screen, "SK_TermsButton", "at_btn_blue", Anchor.Top, 540, 1354, 1.0f, DyBlue);
        FaceText((RectTransform)r.terms.transform, "Terms of Service", fredoka, 46, C("0E3D8E"), BlueFace, 0f);
        return r;
    }

    private static LevelSelectPanel BuildLevelSelect(Transform root, LevelSelectPanel oldPanel)
    {
        RectTransform pop = Stretch(Ensure(root, "SK_LevelSelect"));
        Unwrap(pop);
        Image overlay = Img(pop.gameObject, null, true);
        overlay.color = new Color(0.05f, 0.22f, 0.38f, 0.55f);

        RectTransform card = Ensure(pop, "SK_Card");
        card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f); card.pivot = new Vector2(0.5f, 0.5f);
        card.anchoredPosition = new Vector2(0, -20); card.sizeDelta = new Vector2(980, 1420); card.localScale = Vector3.one;
        Img(card.gameObject, S("sk_card_sliceable"), true, Image.Type.Sliced);

        RectTransform title = Ensure(card, "SK_Title");
        title.anchorMin = title.anchorMax = new Vector2(0.5f, 1f); title.pivot = new Vector2(0.5f, 0.5f);
        title.anchoredPosition = new Vector2(0, -95); title.sizeDelta = new Vector2(600, 100);
        Txt(title.gameObject, "Levels", fredoka, 70, C("1E8CF0"));

        RectTransform prog = Ensure(card, "SK_Progress");
        prog.anchorMin = prog.anchorMax = new Vector2(0.5f, 1f); prog.pivot = new Vector2(0.5f, 0.5f);
        prog.anchoredPosition = new Vector2(0, -165); prog.sizeDelta = new Vector2(700, 60);
        TextMeshProUGUI progressText = Txt(prog.gameObject, "0 / 500 COMPLETED", fredoka, 36, C("6B8AA0"));

        Sprite closeS = S("sk_btn_close");
        RectTransform cl = Ensure(card, "SK_Close");
        cl.anchorMin = cl.anchorMax = new Vector2(1f, 1f); cl.pivot = new Vector2(0.5f, 0.5f);
        cl.anchoredPosition = new Vector2(-50, -50); cl.sizeDelta = Size(closeS);
        Button close = MakeButton(cl.gameObject, Img(cl.gameObject, closeS, true));

        // scroll view
        RectTransform view = Stretch(Ensure(card, "SK_Scroll"), 50, 60, 50, 210);
        GetOrAdd<RectMask2D>(view.gameObject);
        Image viewHit = Img(view.gameObject, null, true);
        viewHit.color = new Color(1f, 1f, 1f, 0f);
        ScrollRect scroll = GetOrAdd<ScrollRect>(view.gameObject);

        RectTransform content = Ensure(view, "SK_Content");
        content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1);
        content.anchoredPosition = Vector2.zero; content.sizeDelta = Vector2.zero;
        GridLayoutGroup grid = GetOrAdd<GridLayoutGroup>(content.gameObject);
        grid.cellSize = new Vector2(180, 236); grid.spacing = new Vector2(34, 22);
        grid.padding = new RectOffset(0, 0, 20, 30); grid.childAlignment = TextAnchor.UpperCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 4;
        ContentSizeFitter fit = GetOrAdd<ContentSizeFitter>(content.gameObject);
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize; fit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        scroll.content = content; scroll.viewport = view; scroll.horizontal = false; scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Elastic; scroll.scrollSensitivity = 30f; scroll.inertia = true;
        List<GameObject> stale = new List<GameObject>();
        foreach (Transform t in content) stale.Add(t.gameObject);
        foreach (GameObject g in stale) Object.DestroyImmediate(g);

        // level button template (names are what LevelSelectPanel looks for)
        RectTransform tpl = Ensure(card, "NT_LevelButtonTemplate");
        tpl.anchorMin = tpl.anchorMax = new Vector2(0.5f, 0.5f); tpl.pivot = new Vector2(0.5f, 0.5f);
        tpl.anchoredPosition = Vector2.zero; tpl.sizeDelta = new Vector2(180, 236);
        RectTransform face = Ensure(tpl, "NT_Btn");
        face.anchorMin = face.anchorMax = new Vector2(0.5f, 1f); face.pivot = new Vector2(0.5f, 1f);
        face.anchoredPosition = Vector2.zero; face.sizeDelta = new Vector2(180, 180);
        Image faceImg = Img(face.gameObject, S("sk_lv_current"), true);
        MakeButton(tpl.gameObject, faceImg);
        RectTransform num = Ensure(tpl, "NT_Number");
        num.anchorMin = num.anchorMax = new Vector2(0.5f, 1f); num.pivot = new Vector2(0.5f, 0.5f);
        num.anchoredPosition = new Vector2(0, -84); num.sizeDelta = new Vector2(170, 110);
        Txt(num.gameObject, "1", fredoka, 72, Color.white);
        RectTransform cap = Ensure(tpl, "NT_Caption");
        cap.anchorMin = cap.anchorMax = new Vector2(0.5f, 0f); cap.pivot = new Vector2(0.5f, 0.5f);
        cap.anchoredPosition = new Vector2(0, 24); cap.sizeDelta = new Vector2(200, 44);
        Txt(cap.gameObject, "ANIMALS", fredoka, 26, C("4A6A80"), TextAlignmentOptions.Center, true);
        RectTransform badge = Ensure(tpl, "NT_Badge");
        badge.anchorMin = badge.anchorMax = new Vector2(0.5f, 1f); badge.pivot = new Vector2(0.5f, 0.5f);
        badge.anchoredPosition = new Vector2(68, -12); badge.sizeDelta = new Vector2(60, 60);
        Img(badge.gameObject, S("sk_badge"));
        RectTransform bi = Stretch(Ensure(badge, "NT_BadgeIcon"), 14, 13, 14, 13);
        Image biImg = Img(bi.gameObject, S("sk_ic_lock")); biImg.preserveAspect = true;
        tpl.gameObject.SetActive(false);

        PopupAnimator anim = GetOrAdd<PopupAnimator>(pop.gameObject);
        anim.Setup(card, new RectTransform[0], null, new RectTransform[0], false, false);
        EditorUtility.SetDirty(anim);

        LevelSelectPanel panel = GetOrAdd<LevelSelectPanel>(pop.gameObject);
        if (oldPanel != null && oldPanel != panel)
        {
            SerializedObject src = new SerializedObject(oldPanel);
            SerializedObject dst = new SerializedObject(panel);
            dst.CopyFromSerializedProperty(src.FindProperty("levelTitles"));
            dst.FindProperty("levelCount").intValue = src.FindProperty("levelCount").intValue;
            dst.ApplyModifiedPropertiesWithoutUndo();
        }
        SetField(panel, "content", content);
        SetField(panel, "buttonTemplate", tpl.gameObject);
        SetField(panel, "scroll", scroll);
        SetField(panel, "progressText", progressText);
        SetField(panel, "closeButton", close);
        SetField(panel, "completedSprite", S("sk_lv_done"));
        SetField(panel, "currentSprite", S("sk_lv_current"));
        SetField(panel, "lockedSprite", S("sk_lv_locked"));
        SetField(panel, "checkIcon", S("sk_ic_check"));
        SetField(panel, "lockIcon", S("sk_ic_lock"));
        SetColor(panel, "completedNumberColor", Color.white);
        SetColor(panel, "currentNumberColor", C("9A5400"));
        SetColor(panel, "lockedNumberColor", C("7D93A5"));
        EditorUtility.SetDirty(panel);
        return panel;
    }

    // ======================================================================
    // GAMEPLAY
    // Layout measured from the gameplay mockup (448 px wide frame x 2.411).
    // ======================================================================
    private const float WordCardY = 370f;      // centre of the word list card (design y)
    private const float BoardY = 935f;         // centre of the letter grid
    private const float GridMaxSky = 720f;     // biggest grid side so the board clears the bottom buttons
    private const float GridGap = 8f;
    private const float MaxCellSky = 140f;

    private static readonly Color[] SkyLines =
    {
        new Color(0.20f, 0.60f, 1.00f, 0.55f),
        new Color(0.62f, 0.36f, 0.96f, 0.55f),
        new Color(0.35f, 0.80f, 0.25f, 0.55f),
        new Color(1.00f, 0.60f, 0.15f, 0.55f),
        new Color(0.96f, 0.35f, 0.55f, 0.55f),
        new Color(0.10f, 0.75f, 0.75f, 0.55f),
        new Color(1.00f, 0.80f, 0.20f, 0.60f),
        new Color(0.95f, 0.30f, 0.30f, 0.55f),
    };

    private static void SetArray(Object target, string field, Object[] values)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if (p == null) { Debug.LogWarning($"[SkyTheme] Field '{field}' not found on {target}"); return; }
        p.ClearArray();
        for (int i = 0; i < values.Length; i++)
        {
            p.InsertArrayElementAtIndex(i);
            p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetFloatField(Object target, string field, float v)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if (p == null) return;
        p.floatValue = v;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>Rect anchored to the top-centre of a card's visible face (card sprites have a 24 px shadow pad).</summary>
    private static RectTransform CardItem(Transform card, string name, float x, float yFromTop, Vector2 size)
    {
        RectTransform rt = Ensure(card, name);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, -(24f + yFromTop));
        rt.sizeDelta = size;
        rt.localScale = Vector3.one; rt.localRotation = Quaternion.identity;
        return rt;
    }

    private static void BuildGameplay()
    {
        Canvas canvas = MainCanvas();
        if (canvas == null) { Debug.LogError("[SkyTheme] Gameplay: no canvas"); return; }
        Transform c = canvas.transform;

        CanvasScaler scaler = GetOrAdd<CanvasScaler>(canvas.gameObject);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(W, H);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;
        GetOrAdd<CanvasAutoMatch>(canvas.gameObject);

        LevelManager lm = Object.FindObjectOfType<LevelManager>(true);
        Transform completePanel = c.Find("LevelCompletePanel");
        Transform fx = c.Find("NT_Fx");

        // hide the old HUD (kept in the scene, just inactive)
        foreach (Transform child in c)
        {
            if (lm != null && child == lm.transform) continue;
            if (child == completePanel || child == fx || child.name.StartsWith("SK_")) continue;
            child.gameObject.SetActive(false);
        }

        Background(c, "at_bg");

        // ---------------- word card + board (behind the level content)
        Image wordCard = PicSliced(c, "SK_WordCard", "sk_card_header", Anchor.Middle, 540, WordCardY, new Vector2(1000 + 48, 300 + 48), 1f);
        Image board = PicSliced(c, "SK_Board", "sk_card_sliceable", Anchor.Middle, 540, BoardY, new Vector2(GridMaxSky + 120, GridMaxSky + 120), 1f);

        // ---------------- HUD
        RectTransform hud = Stretch(Ensure(c, "SK_HUD"));
        GetOrAdd<SafeArea>(hud.gameObject); // HUD has no backdrop, so the whole node follows the safe area
        Button back = PicButton(hud, "SK_Back", "at_btn_back", Anchor.Top, 70, 90, 0.93f);

        Image coinBar = Pic(hud, "SK_CoinBar", "at_btn_blue_flat", Anchor.Top, 365, 90, 0.99f);
        RectTransform cb = (RectTransform)coinBar.transform;
        Sprite coinS = S("at_coin");
        RectTransform coin = Ensure(cb, "SK_Coin");
        coin.anchorMin = coin.anchorMax = new Vector2(0.5f, 0.5f); coin.pivot = new Vector2(0.5f, 0.5f);
        coin.anchoredPosition = new Vector2(-141, 6); coin.sizeDelta = Size(coinS); coin.localScale = Vector3.one;
        Img(coin.gameObject, coinS);
        RectTransform ct = Ensure(cb, "SK_CoinText");
        ct.anchorMin = ct.anchorMax = new Vector2(0.5f, 0.5f); ct.pivot = new Vector2(0.5f, 0.5f);
        ct.anchoredPosition = new Vector2(4, 8); ct.sizeDelta = new Vector2(200, 80); ct.localScale = Vector3.one;
        TextMeshProUGUI coinText = Txt(ct.gameObject, "7,256", fredoka, 46, Color.white, TextAlignmentOptions.Center, true);
        Sprite plusS = S("at_plus");
        RectTransform plus = Ensure(cb, "SK_Plus");
        plus.anchorMin = plus.anchorMax = new Vector2(0.5f, 0.5f); plus.pivot = new Vector2(0.5f, 0.5f);
        plus.anchoredPosition = new Vector2(145, 5); plus.sizeDelta = Size(plusS); plus.localScale = Vector3.one;
        Button plusBtn = MakeButton(plus.gameObject, Img(plus.gameObject, plusS, true));

        Button noAds = PicButton(hud, "SK_NoAds", "at_noads", Anchor.Top, 712, 87, 0.71f);
        Button shop = PicButton(hud, "SK_Shop", "at_btn_bag", Anchor.Top, 862, 90, 0.84f);
        Button settingsTop = PicButton(hud, "SK_SettingsTop", "at_btn_settings", Anchor.Top, 1007, 90, 0.84f);

        TextMeshProUGUI levelText = Label(hud, "SK_LevelText", "Level 1", fredoka, 44, C("2B4457"), Anchor.Middle, 540, 178, 500);

        // bottom bar: Shuffle + Hint only (settings lives in the top bar)
        Button shuffle = PicButton(hud, "SK_Shuffle", "at_game_shuffle", Anchor.Bottom, 403, 1468);
        Button hint = PicButton(hud, "SK_Hint", "at_game_hint", Anchor.Bottom, 677, 1468);
        Transform oldSettings = hud.Find("SK_SettingsBottom");
        if (oldSettings != null) oldSettings.gameObject.SetActive(false);
        TextMeshProUGUI shuffleCount, hintCount;
        Image shuffleBadge = PowerBadge((RectTransform)shuffle.transform, out shuffleCount);
        Image hintBadge = PowerBadge((RectTransform)hint.transform, out hintCount);

        // ---------------- popups
        TextMeshProUGUI rewardText = StyleCompleteSky(completePanel);
        Button sClose, sHome, sRestart, sSound, sMusic, sVibrate;
        PopupAnimator settingsPopup = BuildGameSettings(c, out sClose, out sHome, out sRestart, out sSound, out sMusic, out sVibrate);

        // ---------------- levels
        if (lm != null)
        {
            foreach (WordSearchLevel level in lm.GetComponentsInChildren<WordSearchLevel>(true)) StyleLevelSky(level);
            SetField(lm, "levelText", levelText);
            EditorUtility.SetDirty(lm);
        }

        // ---------------- order: bg, cards, levels, HUD, popups, fx
        int idx = 0;
        c.Find("SK_Bg").SetSiblingIndex(idx++);
        wordCard.transform.SetSiblingIndex(idx++);
        board.transform.SetSiblingIndex(idx++);
        if (lm != null) lm.transform.SetSiblingIndex(idx++);
        hud.SetSiblingIndex(idx++);
        if (completePanel != null) completePanel.SetAsLastSibling();
        settingsPopup.transform.SetAsLastSibling();
        if (fx != null) fx.SetAsLastSibling();
        if (completePanel != null) SafeArea.Wrap(completePanel, "NT_Rays");
        SafeArea.Wrap(settingsPopup.transform);

        // ---------------- wiring
        GameHUD h = GetOrAdd<GameHUD>(canvas.gameObject);
        if (lm != null) SetField(h, "levelManager", lm);
        SetField(h, "backButton", back);
        SetField(h, "levelBanner", levelText.rectTransform);
        SetField(h, "board", board.rectTransform);
        SetFloatField(h, "boardPadding", 60f);
        SetArray(h, "hintButtons", new Object[] { hint });
        SetArray(h, "hintCountTexts", new Object[0]);
        SetField(h, "shuffleButton", shuffle);
        SetField(h, "settingsButton", settingsTop);
        SetArray(h, "moreSettingsButtons", new Object[0]);
        SetField(h, "hintBadge", hintBadge);
        SetField(h, "hintBadgeText", hintCount);
        SetField(h, "shuffleBadge", shuffleBadge);
        SetField(h, "shuffleBadgeText", shuffleCount);
        SetColor(h, "badgeCountColor", C("1565D8"));
        SetColor(h, "badgeAdColor", C("4CAF1A"));
        SetField(h, "settingsPopup", settingsPopup);
        SetField(h, "settingsCloseButton", sClose);
        SetField(h, "settingsHomeButton", sHome);
        SetField(h, "settingsRestartButton", sRestart);
        SetField(h, "soundButton", sSound);
        SetField(h, "soundLabel", null);
        SetField(h, "musicButton", sMusic);
        SetField(h, "vibrateButton", sVibrate);
        SetField(h, "toggleOn", S("at_toggle_on"));
        SetField(h, "toggleOff", S("at_toggle_off"));
        SetField(h, "coinText", coinText);
        SetArray(h, "getCoinsButtons", new Object[] { plusBtn, shop });
        SetField(h, "coinBar", cb);
        SetField(h, "noAdsButton", noAds);
        SetField(h, "levelRewardText", rewardText);
        EditorUtility.SetDirty(h);
    }

    /// <summary>Round badge on the top-right corner of a power-up button: uses left, or "AD" when empty.</summary>
    private static Image PowerBadge(RectTransform button, out TextMeshProUGUI count)
    {
        RectTransform b = Ensure(button, "SK_Badge");
        b.anchorMin = b.anchorMax = new Vector2(1f, 1f); b.pivot = new Vector2(0.5f, 0.5f);
        b.anchoredPosition = new Vector2(-14, -14); b.sizeDelta = new Vector2(72, 72); b.localScale = Vector3.one;
        Image img = Img(b.gameObject, S("sk_badge_white"));
        img.color = C("1565D8");
        RectTransform t = Stretch(Ensure(b, "SK_Count"), 4, 4, 4, 2);
        count = Txt(t.gameObject, "3", fredoka, 44, Color.white, TextAlignmentOptions.Center, true);
        count.fontSizeMin = 26f;
        b.SetAsLastSibling();
        return img;
    }

    private static void StyleLevelSky(WordSearchLevel level)
    {
        RectTransform lr = Stretch((RectTransform)level.transform);

        // title on the blue header of the word card
        Transform tp = level.transform.Find("ThemePanel");
        if (tp != null)
        {
            RectTransform tpr = At((RectTransform)tp, Anchor.Middle, 540, WordCardY - 150 + 40, new Vector2(700, 76));
            foreach (Image im in tp.GetComponents<Image>()) im.enabled = false;
            Transform title = tp.Find("TitleText");
            if (title != null)
            {
                RectTransform tr = Stretch((RectTransform)title);
                Txt(title.gameObject, title.GetComponent<TMP_Text>() != null ? title.GetComponent<TMP_Text>().text : "", fredoka, 46, Color.white, TextAlignmentOptions.Center, true);
            }
        }

        // word chips inside the card
        Transform tw = level.transform.Find("TargetWords");
        if (tw != null)
        {
            At((RectTransform)tw, Anchor.Middle, 540, WordCardY + 38, new Vector2(940, 200));
            foreach (WordTarget wt in tw.GetComponentsInChildren<WordTarget>(true))
            {
                RectTransform rt = (RectTransform)wt.transform;
                string word = wt.TargetWord ?? "";
                rt.sizeDelta = new Vector2(Mathf.Max(130f, 56f + word.Length * 30f), 70f);
                Image chip = Img(wt.gameObject, S("sk_chip"), false, Image.Type.Sliced);
                Transform txt = wt.transform.Find("WordText");
                if (txt != null)
                {
                    RectTransform tr = Stretch((RectTransform)txt, 10, 6, 10, 0);
                    Txt(txt.gameObject, word.ToUpper(), fredoka, 38, C("1E6FC0"), TextAlignmentOptions.Center, true);
                    txt.SetAsLastSibling();
                }
                Transform strike = wt.transform.Find("CompletedVisual");
                if (strike != null)
                {
                    Image si = strike.GetComponent<Image>();
                    if (si != null) si.color = new Color(1f, 1f, 1f, 0.9f);
                }
                SetField(wt, "chipImage", chip);
                SetField(wt, "normalSprite", S("sk_chip"));
                SetField(wt, "foundSprite", S("sk_chip_found"));
                SetColor(wt, "normalTextColor", C("1E6FC0"));
                SetColor(wt, "foundTextColor", Color.white);
                EditorUtility.SetDirty(wt);
            }
        }

        // grid + tiles
        GridLayoutGroup grid = level.GetComponentInChildren<GridLayoutGroup>(true);
        float cell = 100f;
        if (grid != null)
        {
            int count = 0;
            foreach (Transform t in grid.transform) if (t.gameObject.activeSelf) count++;
            int cols = grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount ? Mathf.Max(1, grid.constraintCount) : Mathf.CeilToInt(Mathf.Sqrt(count));
            int rows = Mathf.Max(1, Mathf.CeilToInt(count / (float)cols));
            int dim = Mathf.Max(cols, rows);
            cell = Mathf.Floor(Mathf.Min(MaxCellSky, (GridMaxSky - (dim - 1) * GridGap) / dim));
            grid.cellSize = new Vector2(cell, cell);
            grid.spacing = new Vector2(GridGap, GridGap);
            grid.padding = new RectOffset(0, 0, 0, 0);
            grid.childAlignment = TextAnchor.MiddleCenter;
            Vector2 content = new Vector2(cols * cell + (cols - 1) * GridGap, rows * cell + (rows - 1) * GridGap);
            At((RectTransform)grid.transform, Anchor.Middle, 540, BoardY, content);
            foreach (LetterTile tile in grid.GetComponentsInChildren<LetterTile>(true)) StyleTileSky(tile, cell);
        }

        // selection bars
        Transform lines = level.transform.Find("SelectionLines");
        if (lines != null)
        {
            float thickness = cell * 0.84f;
            foreach (WordSelectionLine l in lines.GetComponentsInChildren<WordSelectionLine>(true))
            {
                RectTransform rt = (RectTransform)l.transform;
                rt.sizeDelta = new Vector2(thickness, thickness);
                Image img = l.GetComponent<Image>();
                if (img != null) { img.pixelsPerUnitMultiplier = 64f / thickness; img.color = SkyLines[0]; }
                SetFloatField(l, "thickness", thickness);
            }
        }

        SerializedObject so = new SerializedObject(level);
        SerializedProperty colors = so.FindProperty("lineColors");
        if (colors != null)
        {
            colors.ClearArray();
            for (int i = 0; i < SkyLines.Length; i++)
            {
                colors.InsertArrayElementAtIndex(i);
                colors.GetArrayElementAtIndex(i).colorValue = SkyLines[i];
            }
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(level);
    }

    private static void StyleTileSky(LetterTile tile, float cell)
    {
        GameObject go = tile.gameObject;
        Img(go, S("sk_tile_game"), true);
        float lip = cell * 0.09f;
        string[] names = { "SelectionHighlight", "FoundHighlight", "HintHighlight" };
        Color[] cols =
        {
            new Color(0.45f, 0.75f, 1.00f, 0.45f),
            new Color(0.60f, 0.88f, 0.40f, 0.40f),
            new Color(1.00f, 0.78f, 0.20f, 0.60f),
        };
        for (int i = 0; i < names.Length; i++)
        {
            Transform h = go.transform.Find(names[i]);
            if (h == null) continue;
            RectTransform hr = Stretch((RectTransform)h, 4, lip, 4, 4);
            Image hi = h.GetComponent<Image>();
            if (hi != null) hi.color = cols[i];
        }
        Transform lt = go.transform.Find("LetterText");
        if (lt != null)
        {
            Stretch((RectTransform)lt, 0, lip, 0, 0);
            TextMeshProUGUI t = Txt(lt.gameObject, null, fredoka, Mathf.Round(cell * 0.5f), C("1F3B57"));
            t.text = tile.Letter;
            lt.SetAsLastSibling();
        }
        SetColor(tile, "normalLetterColor", C("1F3B57"));
        SetColor(tile, "selectedLetterColor", C("0E5BB5"));
        EditorUtility.SetDirty(tile);
    }

    private static TextMeshProUGUI StyleCompleteSky(Transform panel)
    {
        if (panel == null) return null;
        Image overlay = GetOrAdd<Image>(panel.gameObject);
        overlay.sprite = null; overlay.color = new Color(0.05f, 0.22f, 0.38f, 0.6f); overlay.raycastTarget = true;

        Transform raysT = panel.Find("NT_Rays");
        RectTransform rays = raysT as RectTransform;
        if (rays != null) { Image ri = rays.GetComponent<Image>(); if (ri != null) ri.color = new Color(1f, 1f, 1f, 0.25f); }

        Unwrap(panel);
        Transform card = panel.Find("PanelCard");
        if (card == null) return null;
        RectTransform cardRect = (RectTransform)card;
        cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f); cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero; cardRect.sizeDelta = new Vector2(860 + 48, 820 + 48);
        Image ci = Img(card.gameObject, S("sk_card_header"), true, Image.Type.Sliced);

        Transform ribbon = card.Find("NT_Ribbon");
        if (ribbon != null)
        {
            CardItem(card, "NT_Ribbon", 0, 40, new Vector2(760, 80));
            Transform plank = ribbon.Find("NT_Plank");
            if (plank != null) plank.gameObject.SetActive(false);
            Transform title = ribbon.Find("TitleText");
            if (title != null)
            {
                Stretch((RectTransform)title);
                Txt(title.gameObject, "Level Complete!", fredoka, 52, Color.white, TextAlignmentOptions.Center, true);
            }
        }

        RectTransform[] stars = new RectTransform[3];
        Transform starRow = card.Find("NT_Stars");
        if (starRow != null)
        {
            CardItem(card, "NT_Stars", 0, 250, new Vector2(700, 260));
            for (int i = 0; i < 3; i++)
            {
                Transform st = starRow.Find("NT_StarSlot" + i + "/NT_Star");
                stars[i] = st as RectTransform;
            }
        }

        RectTransform subRect = null;
        Transform sub = card.Find("SubtitleText");
        if (sub != null)
        {
            subRect = CardItem(card, "SubtitleText", 0, 440, new Vector2(700, 100));
            Txt(sub.gameObject, "AWESOME!", fredoka, 70, C("1E8CF0"));
        }

        RectTransform reward = CardItem(card, "SK_Reward", 0, 545, new Vector2(320, 90));
        Sprite coinS = S("at_coin");
        RectTransform coin = Ensure(reward, "SK_Coin");
        coin.anchorMin = coin.anchorMax = new Vector2(0.5f, 0.5f); coin.pivot = new Vector2(0.5f, 0.5f);
        coin.anchoredPosition = new Vector2(-60, 0); coin.sizeDelta = Size(coinS); coin.localScale = Vector3.one;
        Img(coin.gameObject, coinS);
        RectTransform rt = Ensure(reward, "SK_RewardText");
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(40, 2); rt.sizeDelta = new Vector2(180, 90); rt.localScale = Vector3.one;
        TextMeshProUGUI rewardText = Txt(rt.gameObject, "+25", fredoka, 58, C("F29B00"), TextAlignmentOptions.Left);

        RectTransform nextRect = null;
        Transform next = card.Find("NextButton");
        if (next != null)
        {
            const float ns = 0.8f;
            Sprite g = S("at_btn_green");
            nextRect = CardItem(card, "NextButton", 0, 690 - DyGreen * ns, Size(g, ns));
            Image ni = Img(next.gameObject, g, true);
            MakeButton(next.gameObject, ni);
            Transform bt = next.Find("ButtonText");
            if (bt != null)
            {
                Vector4 f = Sc(GreenFace, ns);
                Stretch((RectTransform)bt, f.x, f.w, f.z, f.y);
                Txt(bt.gameObject, "Next", lato, 60, C("13570D"));
            }
        }

        PopupAnimator anim = GetOrAdd<PopupAnimator>(panel.gameObject);
        anim.Setup(cardRect, stars, rays, new[] { subRect, reward, nextRect }, true, true);
        EditorUtility.SetDirty(anim);
        panel.gameObject.SetActive(false);
        return rewardText;
    }

    private static PopupAnimator BuildGameSettings(Transform canvas, out Button close, out Button home, out Button restart,
                                                   out Button sound, out Button music, out Button vibrate)
    {
        RectTransform pop = Stretch(Ensure(canvas, "SK_SettingsPopup"));
        Unwrap(pop);
        Image overlay = Img(pop.gameObject, null, true);
        overlay.color = new Color(0.05f, 0.22f, 0.38f, 0.6f);

        RectTransform card = Ensure(pop, "SK_Card");
        card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f); card.pivot = new Vector2(0.5f, 0.5f);
        card.anchoredPosition = Vector2.zero; card.sizeDelta = new Vector2(900 + 48, 900 + 48); card.localScale = Vector3.one;
        Img(card.gameObject, S("sk_card_header"), true, Image.Type.Sliced);

        Txt(CardItem(card, "SK_Title", 0, 40, new Vector2(600, 80)).gameObject, "Settings", fredoka, 52, Color.white);

        string[] icons = { "sk_ic_sound", "sk_ic_music", "sk_ic_vibrate" };
        string[] labels = { "Sound", "Music", "Vibrate" };
        float[] ys = { 170, 290, 410 };
        Button[] toggles = new Button[3];
        for (int i = 0; i < 3; i++)
        {
            Sprite ic = S(icons[i]);
            Img(CardItem(card, "SK_Icon_" + labels[i], -340, ys[i], Size(ic)).gameObject, ic);
            Txt(CardItem(card, "SK_Label_" + labels[i], -120, ys[i] + 2, new Vector2(320, 60)).gameObject, labels[i], fredoka, 40, C("1E90F0"), TextAlignmentOptions.Left);
            Sprite tg = S(i == 1 ? "at_toggle_on" : "at_toggle_off");
            RectTransform tr = CardItem(card, "SK_Toggle_" + labels[i], 320, ys[i], Size(tg));
            toggles[i] = MakeButton(tr.gameObject, Img(tr.gameObject, tg, true));
        }
        foreach (float y in new float[] { 230, 350 })
            Img(CardItem(card, "SK_Sep_" + y, 0, y, new Vector2(760, 3)).gameObject, S("sk_line"), false, Image.Type.Sliced);
        sound = toggles[0]; music = toggles[1]; vibrate = toggles[2];

        const float bs = 0.85f;
        Sprite gs = S("at_btn_green"), bsS = S("at_btn_blue");
        RectTransform rr = CardItem(card, "SK_Restart", 0, 560 - DyGreen * bs, Size(gs, bs));
        restart = MakeButton(rr.gameObject, Img(rr.gameObject, gs, true));
        FaceText(rr, "Restart", lato, 54, C("13570D"), Sc(GreenFace, bs), 0f);
        RectTransform hr = CardItem(card, "SK_Home", 0, 720 - DyBlue * bs, Size(bsS, bs));
        home = MakeButton(hr.gameObject, Img(hr.gameObject, bsS, true));
        FaceText(hr, "Home", lato, 54, C("0E3D8E"), Sc(BlueFace, bs), 0f);

        Sprite cs = S("sk_btn_close");
        RectTransform cl = Ensure(card, "SK_Close");
        cl.anchorMin = cl.anchorMax = new Vector2(1f, 1f); cl.pivot = new Vector2(0.5f, 0.5f);
        cl.anchoredPosition = new Vector2(-40, -40); cl.sizeDelta = Size(cs); cl.localScale = Vector3.one;
        close = MakeButton(cl.gameObject, Img(cl.gameObject, cs, true));

        PopupAnimator anim = GetOrAdd<PopupAnimator>(pop.gameObject);
        anim.Setup(card, new RectTransform[0], null, new RectTransform[0], false, false);
        EditorUtility.SetDirty(anim);
        pop.gameObject.SetActive(false);
        return anim;
    }
}
