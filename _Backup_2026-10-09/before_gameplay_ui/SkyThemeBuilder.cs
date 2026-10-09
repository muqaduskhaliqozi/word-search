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
    private const string RootName = "SK_Root";

    private const float W = 1080f, H = 1920f;

    private static TMP_FontAsset fredoka, lato;

    private static Color C(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out Color c); return c; }

    // ======================================================================
    [MenuItem("Tools/Word Search/Apply Sky Menu Theme (Splash + Main Menu)")]
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
            EditorUtility.DisplayProgressBar("Sky Theme", "Splash...", 0.5f);
            ProcessScene(SplashScene, BuildSplash);
            EditorUtility.DisplayProgressBar("Sky Theme", "Main menu + settings...", 0.75f);
            ProcessScene(MenuScene, BuildMenu);
        }
        finally { EditorUtility.ClearProgressBar(); }

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

    private static Vector2 Size(Sprite s, float scale = 1f) => s != null ? s.rect.size * scale : new Vector2(100, 100);

    private static Image Img(GameObject go, Sprite s, bool raycast = false, Image.Type type = Image.Type.Simple)
    {
        Image img = GetOrAdd<Image>(go);
        img.sprite = s; img.type = type; img.color = Color.white;
        img.preserveAspect = false; img.raycastTarget = raycast; img.pixelsPerUnitMultiplier = 1f;
        return img;
    }

    /// <summary>Sprite image at its native size, centred at (x, y).</summary>
    private static Image Pic(Transform parent, string name, string sprite, Anchor a, float x, float y, float scale = 1f, bool raycast = false)
    {
        Sprite s = S(sprite);
        RectTransform rt = At(Ensure(parent, name), a, x, y, Size(s, scale));
        return Img(rt.gameObject, s, raycast);
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

    private static Button PicButton(Transform parent, string name, string sprite, Anchor a, float x, float y, float scale = 1f)
    {
        Image img = Pic(parent, name, sprite, a, x, y, scale, true);
        return MakeButton(img.gameObject, img);
    }

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
        Background(root, "sk_bg_menu");

        Image logo = Pic(root, "SK_Logo", "sk_logo", Anchor.Top, 540, 595, 0.97f);
        PopIn(logo, 0.15f, new Vector2(0, 40), 0.7f);

        TextMeshProUGUI loading = Label(root, "SK_LoadingText", "Loading...", fredoka, 60, C("343434"), Anchor.Bottom, 540, 1540, 600);

        // slider
        Sprite barBg = S("sk_bar_bg");
        RectTransform bar = At(Ensure(root, "SK_LoadingBar"), Anchor.Bottom, 540, 1630, Size(barBg));
        RectTransform bgr = Stretch(Ensure(bar, "Background"));
        Img(bgr.gameObject, barBg, false, Image.Type.Sliced);
        RectTransform area = Stretch(Ensure(bar, "Fill Area"), 12, 12, 12, 12);
        RectTransform fill = Ensure(area, "Fill");
        fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(0f, 1f); fill.pivot = new Vector2(0.5f, 0.5f);
        fill.offsetMin = Vector2.zero; fill.offsetMax = Vector2.zero; fill.localScale = Vector3.one;
        Img(fill.gameObject, S("sk_bar_fill"), false, Image.Type.Sliced);
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
        Background(root, "sk_bg_menu");

        // ---------------- top bar
        Button settingsBtn = PicButton(root, "SK_SettingsButton", "sk_btn_settings", Anchor.Top, 84, 114);
        Button noAdsBtn = PicButton(root, "SK_NoAdsButton", "sk_noads", Anchor.Top, 979, 126);

        // ---------------- logo
        Image logo = Pic(root, "SK_Logo", "sk_logo", Anchor.Top, 540, 550, 0.94f);
        PopIn(logo, 0.1f, new Vector2(0, 40), 0.7f);

        // ---------------- main buttons
        Button levels = PicButton(root, "SK_LevelsButton", "sk_btn_green", Anchor.Middle, 540, 1263);
        FaceText((RectTransform)levels.transform, "Levels", lato, 54, C("13570D"), PillPad, PillRim);
        PopIn(levels, 0.3f, new Vector2(0, -40));
        Button newGame = PicButton(root, "SK_NewGameButton", "sk_btn_blue", Anchor.Middle, 540, 1430);
        FaceText((RectTransform)newGame.transform, "New Game", lato, 50, C("0E3D8E"), PillPad, PillRim);
        PopIn(newGame, 0.38f, new Vector2(0, -40));

        // ---------------- bottom tiles
        string[] tiles = { "sk_tile_trophy", "sk_tile_rate", "sk_tile_privacy", "sk_tile_more" };
        string[] names = { "Leaderboard", "Rate us", "Privacy Policy", "More Game" };
        float[] xs = { 285, 454, 622, 791 };
        Button[] tileButtons = new Button[4];
        for (int i = 0; i < 4; i++)
        {
            tileButtons[i] = PicButton(root, "SK_Tile_" + names[i].Replace(" ", ""), tiles[i], Anchor.Bottom, xs[i], 1672);
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
        SetField(sky, "toggleOn", S("sk_toggle_on"));
        SetField(sky, "toggleOff", S("sk_toggle_off"));
        SetField(sky, "settingsPrivacyButton", sr.privacy);
        SetField(sky, "termsButton", sr.terms);
        EditorUtility.SetDirty(sky);

        sr.screen.transform.SetAsLastSibling();
        panel.transform.SetAsLastSibling();
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
        r.screen = screen.gameObject;
        Background(screen, "sk_bg_settings");

        r.back = PicButton(screen, "SK_Back", "sk_btn_back", Anchor.Top, 129, 86);
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
        Pic(holder, "SK_AdsIcon", "sk_noads_big", Anchor.Top, 248, 417);
        Label(holder, "SK_Desc", "Remove Banner and Pop-Up Ads", fredoka, 40, C("1E90F0"), Anchor.Top, 686, 367, 600, TextAlignmentOptions.Center, true);
        Image rib = Pic(holder, "SK_Ribbon", "sk_ribbon", Anchor.Top, 513, 457);
        Txt(Stretch(Ensure(rib.transform, "SK_Text"), 20, 4, 20, 0).gameObject, "50% Off", fredoka, 36, Color.white);
        r.buy = PicButton(holder, "SK_Buy", "sk_btn_green_small", Anchor.Top, 847, 462);
        r.price = FaceText((RectTransform)r.buy.transform, "Rs: 2990", fredoka, 38, C("2E7D0A"), SmallPad, SmallRim);

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
            toggles[i] = PicButton(screen, "SK_Toggle_" + labels[i], i == 1 ? "sk_toggle_on" : "sk_toggle_off", Anchor.Top, 903, ys[i]);
        }
        r.sound = toggles[0]; r.music = toggles[1]; r.vibrate = toggles[2];
        foreach (float y in new float[] { 765, 886 })
        {
            RectTransform sep = At(Ensure(screen, "SK_Sep_" + y), Anchor.Top, 560, y, new Vector2(820, 3));
            Img(sep.gameObject, S("sk_line"), false, Image.Type.Sliced);
        }

        // ---------- bottom buttons
        r.privacy = PicButton(screen, "SK_PrivacyButton", "sk_btn_green", Anchor.Top, 540, 1175);
        FaceText((RectTransform)r.privacy.transform, "Privacy Policy", fredoka, 46, C("1D5A07"), PillPad, PillRim);
        r.terms = PicButton(screen, "SK_TermsButton", "sk_btn_blue", Anchor.Top, 540, 1363);
        FaceText((RectTransform)r.terms.transform, "Terms of Service", fredoka, 46, C("0E3D8E"), PillPad, PillRim);
        return r;
    }

    private static LevelSelectPanel BuildLevelSelect(Transform root, LevelSelectPanel oldPanel)
    {
        RectTransform pop = Stretch(Ensure(root, "SK_LevelSelect"));
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
}
