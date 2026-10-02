using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

/// <summary>
/// One-click re-skin of the Splash, Main Menu and GamePlay scenes into the "Nature" look
/// (wood planks, cream panels, chunky rounded buttons, Fredoka font, effects).
/// Menu: Tools > Word Search > Apply Nature Theme (All Scenes)
/// Safe to run more than once: objects it creates are prefixed "NT_" and reused.
/// </summary>
public static partial class NatureThemeBuilder
{
    private const string Root = "Assets/WordSearchTheme";
    private const string SpriteDir = Root + "/Sprites/";
    private const string FontDir = Root + "/Fonts/";

    private const string SplashScene = "Assets/Scenes/SplashScene.unity";
    private const string MenuScene = "Assets/Scenes/MainMenu.unity";
    private const string GameScene = "Assets/Scenes/GamePlay.unity";

    // palette
    // Classic palette: off-white surfaces with a warm brown -> gold accent
    private static readonly Color Navy = new Color(0.33f, 0.21f, 0.09f, 1f);      // dark text (kept name for compatibility)
    private static readonly Color Brown = new Color(0.42f, 0.28f, 0.13f, 1f);     // UI text on light surfaces
    private static readonly Color OffWhite = new Color(0.969f, 0.953f, 0.925f, 1f); // #F7F3EC
    private static readonly Color GradTop = new Color(0.48f, 0.29f, 0.11f, 1f);   // letter gradient (top)
    private static readonly Color GradBottom = new Color(0.80f, 0.59f, 0.24f, 1f); // letter gradient (bottom)
    private static readonly Color Overlay = new Color(0.16f, 0.11f, 0.06f, 0.55f);
    private static readonly Color WoodDark = new Color(0.42f, 0.2f, 0.06f, 1f);

    private static TMP_FontAsset font;
    private static Material matWood, matGreen, matBlue, matOrange, matPlain;

    // ======================================================================
    [MenuItem("Tools/Word Search/Apply Theme (All Scenes)")]
    public static void ApplyAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string previous = EditorSceneManager.GetActiveScene().path;

        try
        {
            EditorUtility.DisplayProgressBar("Nature Theme", "Importing sprites...", 0.1f);
            ImportSprites();
            EditorUtility.DisplayProgressBar("Nature Theme", "Building font...", 0.2f);
            BuildFont();

            // Gameplay first: it generates the levels and records their titles for the Level Select screen.
            EditorUtility.DisplayProgressBar("Nature Theme", "Gameplay + levels...", 0.35f);
            ProcessScene(GameScene, BuildGameplay);
            EditorUtility.DisplayProgressBar("Nature Theme", "Splash scene...", 0.6f);
            ProcessScene(SplashScene, BuildSplash);
            EditorUtility.DisplayProgressBar("Nature Theme", "Main menu...", 0.8f);
            ProcessScene(MenuScene, BuildMenu);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        if (!string.IsNullOrEmpty(previous) && File.Exists(previous)) EditorSceneManager.OpenScene(previous);
        Debug.Log("<color=#5DBB3A><b>[NatureTheme]</b></color> Splash, MainMenu and GamePlay re-skinned. Press Play on SplashScene!");
    }

    [MenuItem("Tools/Word Search/Reset Saved Progress")]
    public static void ResetProgress()
    {
        LevelProgress.ResetAll();
        PlayerPrefs.DeleteKey(GameHUD.HintsKey);
        PlayerPrefs.Save();
        Debug.Log("[NatureTheme] Level progress and hints reset.");
    }

    [MenuItem("Tools/Word Search/Unlock All Levels (testing)")]
    public static void UnlockAll()
    {
        int count = LevelProgress.LevelCount > 0 ? LevelProgress.LevelCount : 99;
        LevelProgress.Unlocked = count - 1;
        Debug.Log("[NatureTheme] All levels unlocked (the last one is shown as current).");
    }

    private static void ProcessScene(string path, System.Action build)
    {
        if (!File.Exists(path))
        {
            Debug.LogWarning("[NatureTheme] Scene not found: " + path);
            return;
        }
        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        try
        {
            build();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[NatureTheme] Updated " + path);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[NatureTheme] FAILED on " + path + " - scene not saved. Please send this error:\n" + e);
        }
    }

    // ======================================================================
    // ASSETS
    // ======================================================================
    private static readonly Dictionary<string, Vector4> Borders = new Dictionary<string, Vector4>
    {
        // L, B, R, T (pixels)
        { "board", new Vector4(72, 72, 72, 72) },
        { "panel", new Vector4(64, 64, 64, 64) },
        { "plank", new Vector4(72, 0, 72, 0) },
        { "plank_small", new Vector4(56, 0, 56, 0) },
        { "chip", new Vector4(42, 0, 42, 0) },
        { "chip_found", new Vector4(42, 0, 42, 0) },
        { "plaque", new Vector4(42, 0, 42, 0) },
        { "btn_green", new Vector4(75, 0, 75, 0) },
        { "btn_orange", new Vector4(75, 0, 75, 0) },
        { "btn_blue", new Vector4(75, 0, 75, 0) },
        { "bar_bg", new Vector4(42, 0, 42, 0) },
        { "bar_fill", new Vector4(28, 0, 28, 0) },
        { "line_capsule", new Vector4(32, 0, 32, 0) },
        { "tile_flat", new Vector4(34, 34, 34, 34) },
    };

    private static void ImportSprites()
    {
        if (!Directory.Exists(SpriteDir))
        {
            Debug.LogError("[NatureTheme] Missing folder " + SpriteDir);
            return;
        }

        foreach (string file in Directory.GetFiles(SpriteDir, "*.png"))
        {
            string path = file.Replace("\\", "/");
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) continue;

            string key = Path.GetFileNameWithoutExtension(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.filterMode = FilterMode.Bilinear;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.spritePixelsPerUnit = 100;
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
        if (s == null) Debug.LogWarning("[NatureTheme] Sprite not found: " + name);
        return s;
    }

    private static void BuildFont()
    {
        string assetPath = FontDir + "Fredoka-Bold SDF.asset";
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);

        if (font == null)
        {
            Font ttf = AssetDatabase.LoadAssetAtPath<Font>(FontDir + "Fredoka-Bold.ttf");
            if (ttf != null)
            {
                TMP_FontAsset fa = TMP_FontAsset.CreateFontAsset(ttf, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
                if (fa != null)
                {
                    fa.name = "Fredoka-Bold SDF";
                    fa.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,:;!?'\"+-*/%&()#@<>=_");
                    AssetDatabase.CreateAsset(fa, assetPath);
                    if (fa.atlasTextures != null)
                    {
                        for (int i = 0; i < fa.atlasTextures.Length; i++)
                        {
                            if (fa.atlasTextures[i] == null) continue;
                            fa.atlasTextures[i].name = "Fredoka-Bold SDF Atlas " + i;
                            AssetDatabase.AddObjectToAsset(fa.atlasTextures[i], fa);
                        }
                    }
                    fa.material.name = "Fredoka-Bold SDF Material";
                    AssetDatabase.AddObjectToAsset(fa.material, fa);
                    EditorUtility.SetDirty(fa);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.ImportAsset(assetPath);
                    font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
                }
            }
        }

        if (font == null)
        {
            Debug.LogWarning("[NatureTheme] Could not build Fredoka font asset, falling back to TMP default font.");
            font = TMP_Settings.defaultFontAsset;
        }

        matPlain = font != null ? font.material : null;
        // white text on the gold accent: thin warm outline + soft shadow
        matWood = OutlineMaterial("Fredoka Accent Light", new Color(0.45f, 0.28f, 0.1f, 1f), 0.08f);
        matGreen = matWood;
        matBlue = matPlain;
        matOrange = matPlain;
    }

    private static Material OutlineMaterial(string name, Color outline, float width)
    {
        if (font == null || font.material == null) return null;
        string path = FontDir + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(font.material);
            AssetDatabase.CreateAsset(m, path);
        }
        else
        {
            m.shader = font.material.shader;
            m.CopyPropertiesFromMaterial(font.material);
        }
        m.name = name;

        m.EnableKeyword("OUTLINE_ON");
        if (m.HasProperty("_OutlineColor")) m.SetColor("_OutlineColor", outline);
        if (m.HasProperty("_OutlineWidth")) m.SetFloat("_OutlineWidth", width);
        if (m.HasProperty("_FaceDilate")) m.SetFloat("_FaceDilate", 0.05f);

        m.EnableKeyword("UNDERLAY_ON");
        if (m.HasProperty("_UnderlayColor")) m.SetColor("_UnderlayColor", new Color(0.3f, 0.18f, 0.05f, 0.35f));
        if (m.HasProperty("_UnderlayOffsetX")) m.SetFloat("_UnderlayOffsetX", 0f);
        if (m.HasProperty("_UnderlayOffsetY")) m.SetFloat("_UnderlayOffsetY", -0.75f);
        if (m.HasProperty("_UnderlaySoftness")) m.SetFloat("_UnderlaySoftness", 0.15f);
        if (m.HasProperty("_UnderlayDilate")) m.SetFloat("_UnderlayDilate", 0.2f);

        EditorUtility.SetDirty(m);
        return m;
    }

    // ======================================================================
    // GENERIC HELPERS
    // ======================================================================
    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        foreach (Transform c in root)
        {
            Transform r = FindDeep(c, name);
            if (r != null) return r;
        }
        return null;
    }

    private static GameObject FindInScene(string name)
    {
        foreach (GameObject root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
        {
            Transform t = FindDeep(root.transform, name);
            if (t != null) return t.gameObject;
        }
        return null;
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

    /// <summary>Finds a direct child by name or creates a new UI object.</summary>
    private static RectTransform Ensure(Transform parent, string name)
    {
        Transform t = parent.Find(name);
        if (t == null)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = 5; // UI
            go.transform.SetParent(parent, false);
            t = go.transform;
        }
        return (RectTransform)t;
    }

    private static RectTransform Place(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        return rt;
    }

    private static RectTransform Center(RectTransform rt, Vector2 pos, Vector2 size)
    {
        Vector2 h = new Vector2(0.5f, 0.5f);
        return Place(rt, h, h, h, pos, size);
    }

    private static RectTransform Stretch(RectTransform rt, float inset = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        return rt;
    }

    private static Image Img(GameObject go, Sprite s, Image.Type type = Image.Type.Simple, float ppuMul = 1f, bool preserve = false, bool raycast = false)
    {
        Image img = GetOrAdd<Image>(go);
        img.sprite = s;
        img.type = type;
        img.color = Color.white;
        img.pixelsPerUnitMultiplier = ppuMul;
        img.preserveAspect = preserve;
        img.raycastTarget = raycast;
        img.fillCenter = true;
        return img;
    }

    private static Image Sliced(GameObject go, Sprite s, float nativeHeight, float height, bool raycast = false)
    {
        return Img(go, s, Image.Type.Sliced, nativeHeight / Mathf.Max(1f, height), false, raycast);
    }

    private static TextMeshProUGUI Txt(GameObject go, string text, float size, Color color, Material mat, bool autoSize = false, float minSize = 20f)
    {
        TextMeshProUGUI t = GetOrAdd<TextMeshProUGUI>(go);
        if (font != null) t.font = font;
        if (mat != null) t.fontSharedMaterial = mat;
        if (text != null) t.text = text;
        t.fontStyle = FontStyles.Normal;
        t.fontSize = size;
        t.enableAutoSizing = autoSize;
        t.fontSizeMax = size;
        t.fontSizeMin = minSize;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.enableWordWrapping = false;
        t.overflowMode = TextOverflowModes.Overflow;
        t.raycastTarget = false;
        t.characterSpacing = 0f;
        t.margin = Vector4.zero;
        return t;
    }

    /// <summary>Brown -> gold vertical gradient on a text.</summary>
    private static TextMeshProUGUI Grad(TextMeshProUGUI t)
    {
        t.color = Color.white;
        t.enableVertexGradient = true;
        t.colorGradient = new VertexGradient(GradTop, GradTop, GradBottom, GradBottom);
        return t;
    }

    private static Button MakeButton(GameObject go, Graphic target, bool idlePulse = false)
    {
        Button b = GetOrAdd<Button>(go);
        b.transition = Selectable.Transition.None;
        b.targetGraphic = target;
        if (target != null) target.raycastTarget = true;
        Navigation nav = b.navigation;
        nav.mode = Navigation.Mode.None;
        b.navigation = nav;
        UIButtonJuice j = GetOrAdd<UIButtonJuice>(go);
        j.SetIdlePulse(idlePulse, 0.04f);
        return b;
    }

    private static void SetField(Object target, string field, Object value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if (p == null) { Debug.LogWarning($"[NatureTheme] Field '{field}' not found on {target}"); return; }
        p.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetFloat(Object target, string field, float value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if (p == null) return;
        p.floatValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetBool(Object target, string field, bool value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if (p == null) return;
        p.boolValue = value;
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

    private static void SetArray(Object target, string field, Object[] values)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if (p == null) { Debug.LogWarning($"[NatureTheme] Array '{field}' not found on {target}"); return; }
        p.ClearArray();
        for (int i = 0; i < values.Length; i++)
        {
            p.InsertArrayElementAtIndex(i);
            p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ======================================================================
    // SHARED PIECES
    // ======================================================================
    private static void SetupCanvas(Canvas canvas)
    {
        CanvasScaler scaler = GetOrAdd<CanvasScaler>(canvas.gameObject);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;
        GetOrAdd<CanvasAutoMatch>(canvas.gameObject);
    }

    /// <summary>Makes the existing full-screen background image crisp and undistorted.</summary>
    private static void SetupBackground(GameObject bg)
    {
        if (bg == null) return;
        Image img = bg.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = null; // single-toned, classy off-white
            img.color = OffWhite;
            img.raycastTarget = false;
            img.preserveAspect = false;
        }
        RectTransform rt = (RectTransform)bg.transform;
        Center(rt, Vector2.zero, new Vector2(1080, 1920));
        AspectRatioFitter fit = GetOrAdd<AspectRatioFitter>(bg);
        fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        float ratio = 940f / 1672f;
        if (img != null && img.sprite != null) ratio = img.sprite.rect.width / img.sprite.rect.height;
        fit.aspectRatio = ratio;
        bg.transform.SetSiblingIndex(0);
    }

    private static RectTransform BuildAmbient(Transform canvas, bool clouds, bool leaves, bool sparkles)
    {
        RectTransform amb = Stretch(Ensure(canvas, "NT_Ambient"));
        amb.SetSiblingIndex(1);

        if (clouds)
        {
            RectTransform cl = Stretch(Ensure(amb, "NT_Clouds"));
            Vector2[] pos = { new Vector2(-330, 760), new Vector2(360, 640), new Vector2(60, 860) };
            float[] size = { 300, 240, 200 };
            float[] speed = { 14, -10, 8 };
            for (int i = 0; i < 3; i++)
            {
                RectTransform c = Center(Ensure(cl, "NT_Cloud" + i), pos[i], new Vector2(size[i], size[i] * 0.5f));
                Image im = Img(c.gameObject, S("cloud"), Image.Type.Simple, 1f, true);
                im.color = new Color(1f, 1f, 1f, 0.8f);
                GetOrAdd<UIFloat>(c.gameObject).Configure(6f, 0.5f, 0f, 0f, speed[i]);
            }
        }

        if (leaves)
        {
            RectTransform lf = Stretch(Ensure(amb, "NT_Leaves"));
            GetOrAdd<UIAmbientParticles>(lf.gameObject).Configure(
                UIAmbientParticles.Mode.FallingLeaves,
                new[] { S("leaf") },
                new[] { new Color(0.36f, 0.72f, 0.22f, 0.9f), new Color(0.55f, 0.8f, 0.25f, 0.9f), new Color(0.95f, 0.7f, 0.2f, 0.85f) },
                7, new Vector2(34f, 58f), new Vector2(45f, 85f));
        }

        if (sparkles)
        {
            RectTransform sp = Stretch(Ensure(amb, "NT_Sparkles"));
            GetOrAdd<UIAmbientParticles>(sp.gameObject).Configure(
                UIAmbientParticles.Mode.Twinkle,
                new[] { S("sparkle") },
                new[] { new Color(1f, 1f, 1f, 0.85f), new Color(1f, 0.95f, 0.7f, 0.8f) },
                9, new Vector2(26f, 56f), new Vector2(0f, 0f));
        }
        amb.gameObject.SetActive(false); // keep it simple: no clouds / leaves / sparkles
        return amb;
    }

    private static RectTransform BuildFxLayer(Transform canvas)
    {
        RectTransform fx = Stretch(Ensure(canvas, "NT_Fx"));
        UIBurst burst = GetOrAdd<UIBurst>(fx.gameObject);
        burst.SetSprites(S("sparkle"), S("glow"), S("confetti"), S("star"));
        fx.SetAsLastSibling();
        return fx;
    }

    /// <summary>A wooden plank banner with leaves on both ends. Returns the text object.</summary>
    private static TextMeshProUGUI BuildPlank(RectTransform container, Vector2 size, string text, float fontSize, Sprite plank, float nativeH, float leafScale = 1f)
    {
        RectTransform ll = Place(Ensure(container, "NT_LeavesL"), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-10f * leafScale, 14f * leafScale), new Vector2(150, 115) * leafScale);
        Img(ll.gameObject, S("leaves_left"), Image.Type.Simple, 1f, true);
        RectTransform lr = Place(Ensure(container, "NT_LeavesR"), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(10f * leafScale, 14f * leafScale), new Vector2(150, 115) * leafScale);
        Img(lr.gameObject, S("leaves_right"), Image.Type.Simple, 1f, true);

        RectTransform p = Stretch(Ensure(container, "NT_Plank"));
        Sliced(p.gameObject, plank, nativeH, size.y);

        RectTransform tr = Stretch(Ensure(container, "NT_Text"));
        tr.offsetMin = new Vector2(50, 8);
        tr.offsetMax = new Vector2(-50, 0);
        TextMeshProUGUI t = Txt(tr.gameObject, text, fontSize, Color.white, matWood, true, 24f);

        ll.gameObject.SetActive(false);
        lr.gameObject.SetActive(false);
        ll.SetSiblingIndex(0);
        lr.SetSiblingIndex(1);
        p.SetSiblingIndex(2);
        tr.SetSiblingIndex(3);
        return t;
    }

    private static void BuildLogo(Transform parent, Vector2 pos)
    {
        // NT_Logo pops in; its child NT_LogoFloat bobs (two components never fight over the same transform)
        RectTransform logo = Center(Ensure(parent, "NT_Logo"), pos, new Vector2(860, 420));
        RectTransform body = Stretch(Ensure(logo, "NT_LogoFloat"));

        // "WORD" letter tiles
        RectTransform tiles = Center(Ensure(body, "NT_Tiles"), new Vector2(0, 90), new Vector2(760, 190));
        string word = "WORD";
        for (int i = 0; i < word.Length; i++)
        {
            RectTransform tile = Center(Ensure(tiles, "NT_Tile" + i), new Vector2(-273 + i * 182, 0), new Vector2(170, 178));
            Img(tile.gameObject, S("tile"));
            RectTransform lt = Stretch(Ensure(tile, "NT_Letter"));
            lt.offsetMin = new Vector2(0, 12);
            Grad(Txt(lt.gameObject, word[i].ToString(), 120, Color.white, matPlain));
            GetOrAdd<UIFloat>(tile.gameObject).Configure(8f, 2f, 5f, 0f);
        }

        // "SEARCH" plank
        RectTransform plank = Center(Ensure(body, "NT_Banner"), new Vector2(0, -110), new Vector2(700, 160));
        BuildPlank(plank, plank.sizeDelta, "SEARCH", 110, S("plank"), 150f, 1.3f);

        GetOrAdd<UIFloat>(body.gameObject).Configure(10f, 1.2f, 0f, 0f);
        GetOrAdd<UIPopIn>(logo.gameObject).Configure(0.15f, new Vector2(0, 120), 0.3f, 0.6f);
    }

    // ======================================================================
    // SPLASH
    // ======================================================================
    private static void BuildSplash()
    {
        Canvas canvas = MainCanvas();
        if (canvas == null) { Debug.LogError("[NatureTheme] Splash: no canvas"); return; }
        Transform c = canvas.transform;
        SetupCanvas(canvas);

        SetupBackground(c.Find("Image") != null ? c.Find("Image").gameObject : null);
        BuildAmbient(c, true, true, true);
        BuildLogo(c, new Vector2(0, 260));

        // tagline
        RectTransform tag = Center(Ensure(c, "NT_Tagline"), new Vector2(0, -20), new Vector2(900, 80));
        Txt(tag.gameObject, "Find the hidden words!", 46, Brown, matPlain);
        GetOrAdd<UIPopIn>(tag.gameObject).Configure(0.5f, new Vector2(0, -40), 0.6f, 0.45f);

        // loading bar
        GameObject barGo = FindInScene("LoadingBar");
        Slider slider = barGo != null ? barGo.GetComponent<Slider>() : null;
        RectTransform shine = null;
        if (barGo != null)
        {
            RectTransform bar = Place((RectTransform)barGo.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f),
                new Vector2(0, 330), new Vector2(760, 84));

            Transform bgT = bar.Find("Background");
            if (bgT != null)
            {
                Stretch((RectTransform)bgT);
                Sliced(bgT.gameObject, S("bar_bg"), 84f, 84f);
            }

            Transform area = bar.Find("Fill Area");
            if (area != null)
            {
                RectTransform ar = Stretch((RectTransform)area);
                ar.offsetMin = new Vector2(18, 20);
                ar.offsetMax = new Vector2(-18, -18);
                Transform fill = area.Find("Fill");
                if (fill != null)
                {
                    RectTransform fr = (RectTransform)fill;
                    fr.anchorMin = Vector2.zero;
                    fr.anchorMax = new Vector2(0f, 1f);
                    fr.pivot = new Vector2(0.5f, 0.5f);
                    fr.offsetMin = Vector2.zero;
                    fr.offsetMax = Vector2.zero;
                    fr.localScale = Vector3.one;
                    Sliced(fill.gameObject, S("bar_fill"), 56f, 46f);
                    if (slider != null) slider.fillRect = fr;
                }
            }

            shine = Center(Ensure(bar, "NT_Shine"), Vector2.zero, new Vector2(110, 110));
            Image si = Img(shine.gameObject, S("glow"), Image.Type.Simple, 1f, true);
            si.color = new Color(1f, 0.95f, 0.8f, 0.7f);
            shine.SetAsLastSibling();

            if (slider != null)
            {
                slider.interactable = false;
                slider.transition = Selectable.Transition.None;
                slider.handleRect = null;
                slider.direction = Slider.Direction.LeftToRight;
                slider.minValue = 0f;
                slider.maxValue = 1f;
            }
            GetOrAdd<UIPopIn>(barGo).Configure(0.35f, new Vector2(0, -60), 0.6f, 0.45f);
        }

        GameObject txtGo = FindInScene("Loadingtxt");
        TextMeshProUGUI loadingText = null;
        if (txtGo != null)
        {
            Place((RectTransform)txtGo.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), new Vector2(0, 230), new Vector2(600, 80));
            loadingText = Txt(txtGo, "Loading", 48, Brown, matPlain);
            loadingText.richText = true;
        }

        GameObject ctrlGo = FindInScene("SplashController");
        SplashController ctrl = ctrlGo != null ? ctrlGo.GetComponent<SplashController>() : Object.FindObjectOfType<SplashController>();
        if (ctrl != null)
        {
            if (loadingText != null) SetField(ctrl, "loadingText", loadingText);
            if (shine != null) SetField(ctrl, "barShine", shine);
            SetFloat(ctrl, "loadingDuration", 3.5f);
        }

        BuildFxLayer(c);
    }

    // ======================================================================
    // MAIN MENU
    // ======================================================================
    private static void BuildMenu()
    {
        Canvas canvas = MainCanvas();
        if (canvas == null) { Debug.LogError("[NatureTheme] Menu: no canvas"); return; }
        Transform c = canvas.transform;
        SetupCanvas(canvas);

        SetupBackground(c.Find("bg") != null ? c.Find("bg").gameObject : null);
        BuildAmbient(c, true, true, true);
        BuildLogo(c, new Vector2(0, 420));

        // level plaque
        RectTransform plaque = Place(Ensure(c, "NT_LevelPlaque"), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f),
            new Vector2(0, 720), new Vector2(380, 90));
        Sliced(plaque.gameObject, S("plaque"), 84f, 90f);
        RectTransform pt = Stretch(Ensure(plaque, "NT_Text"));
        pt.offsetMin = new Vector2(20, 4);
        TextMeshProUGUI levelLabel = Txt(pt.gameObject, "LEVEL 1", 48, Brown, matPlain);
        GetOrAdd<UIPopIn>(plaque.gameObject).Configure(0.45f, new Vector2(0, -50), 0.5f, 0.45f);

        // play button
        GameObject play = FindInScene("Play");
        if (play != null)
        {
            RectTransform pr = Place((RectTransform)play.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f),
                new Vector2(0, 500), new Vector2(560, 180));
            Image img = Sliced(play, S("btn_green"), 150f, 180f, true);
            MakeButton(play, img, true);

            Transform label = play.transform.Find("Text (TMP)");
            if (label != null)
            {
                RectTransform lr = Stretch((RectTransform)label);
                lr.offsetMin = new Vector2(0, 16);
                Txt(label.gameObject, "PLAY", 96, Color.white, matGreen);
            }
            GetOrAdd<UIPopIn>(play).Configure(0.6f, new Vector2(0, -80), 0.3f, 0.5f);

            // little sparkle orbiting the play button
            RectTransform glint = Center(Ensure(pr, "NT_Glint"), new Vector2(210, 50), new Vector2(70, 70));
            Img(glint.gameObject, S("sparkle"), Image.Type.Simple, 1f, true);
            GetOrAdd<UIFloat>(glint.gameObject).Configure(6f, 3f, 20f, 0.25f);
        }

        // LEVELS button + level select popup
        Button levelsButton = BuildLevelsButton(c);
        LevelSelectPanel levelSelect = BuildLevelSelect(c);

        RectTransform tag = Place(Ensure(c, "NT_Tagline"), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), new Vector2(0, 150), new Vector2(900, 70));
        Txt(tag.gameObject, "Swipe letters to find words", 38, Brown, matPlain);
        GetOrAdd<UIPopIn>(tag.gameObject).Configure(0.8f, Vector2.zero, 0.8f, 0.4f);

        MainMenuController ctrl = Object.FindObjectOfType<MainMenuController>();
        if (ctrl != null)
        {
            SetField(ctrl, "levelLabel", levelLabel);
            SetField(ctrl, "levelsButton", levelsButton);
            SetField(ctrl, "levelSelect", levelSelect);
        }

        levelSelect.transform.SetAsLastSibling();
        BuildFxLayer(c);
    }

    // ======================================================================
    // GAMEPLAY
    // ======================================================================
    private const float GridY = -80f;
    private const float GridMax = 790f;
    private const float GridSpacing = 10f;
    private const float MaxCell = 170f;

    private static readonly Color[] LinePalette =
    {
        new Color(0.85f, 0.65f, 0.28f, 0.45f), // gold
        new Color(0.78f, 0.47f, 0.33f, 0.45f), // terracotta
        new Color(0.55f, 0.66f, 0.45f, 0.45f), // sage
        new Color(0.47f, 0.60f, 0.72f, 0.45f), // dusty blue
        new Color(0.68f, 0.52f, 0.62f, 0.45f), // mauve
        new Color(0.62f, 0.45f, 0.28f, 0.45f), // walnut
        new Color(0.80f, 0.56f, 0.52f, 0.45f), // rose
        new Color(0.60f, 0.62f, 0.38f, 0.45f), // olive
    };

    private static void BuildGameplay()
    {
        Canvas canvas = MainCanvas();
        if (canvas == null) { Debug.LogError("[NatureTheme] Gameplay: no canvas"); return; }
        Transform c = canvas.transform;
        SetupCanvas(canvas);

        SetupBackground(c.Find("Image") != null ? c.Find("Image").gameObject : null);
        BuildAmbient(c, true, true, false);

        // ---------------------------------------------------------- top bar
        RectTransform topbar = null;
        Button backButton = null, hintTop = null;
        TextMeshProUGUI hintTopCount = null;
        RectTransform banner = null;
        TextMeshProUGUI levelText = null;

        Transform tb = c.Find("Topbar");
        if (tb != null)
        {
            topbar = Place((RectTransform)tb, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(0, 180));

            Transform back = tb.Find("backbutton");
            if (back != null)
            {
                Place((RectTransform)back, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(115, 0), new Vector2(150, 150));
                Image bi = Img(back.gameObject, S("btn_square_orange"), Image.Type.Simple, 1f, false, true);
                backButton = MakeButton(back.gameObject, bi);
                RectTransform ic = Center(Ensure(back, "Icon"), new Vector2(0, 6), new Vector2(84, 84));
                Img(ic.gameObject, S("icon_arrow"), Image.Type.Simple, 1f, true);
            }

            banner = Center(Ensure(tb, "NT_LevelBanner"), new Vector2(0, 0), new Vector2(500, 140));
            TextMeshProUGUI bannerText = BuildPlank(banner, banner.sizeDelta, "Level 1", 70, S("plank"), 150f);
            // use the original level text object so LevelManager keeps working
            Transform lt = tb.Find("leveltxt");
            if (lt != null)
            {
                lt.SetParent(banner, false);
                RectTransform lr = Stretch((RectTransform)lt);
                lr.offsetMin = new Vector2(50, 8);
                lr.offsetMax = new Vector2(-50, 0);
                levelText = Txt(lt.gameObject, "Level 1", 70, Color.white, matWood, true, 30f);
                bannerText.gameObject.SetActive(false);
            }
            else levelText = bannerText;

            RectTransform hint = Place(Ensure(tb, "NT_HintTop"), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-115, 0), new Vector2(150, 150));
            Image hi = Img(hint.gameObject, S("btn_square_blue"), Image.Type.Simple, 1f, false, true);
            hintTop = MakeButton(hint.gameObject, hi);
            RectTransform hic = Center(Ensure(hint, "Icon"), new Vector2(0, 6), new Vector2(96, 96));
            Img(hic.gameObject, S("icon_bulb"), Image.Type.Simple, 1f, true);
            hintTopCount = BuildBadge(hint, new Vector2(52, 52));

            Transform coinIcon = tb.Find("coinicon");
            if (coinIcon != null) coinIcon.gameObject.SetActive(false);
            Transform coinText = tb.Find("cointext");
            if (coinText != null) coinText.gameObject.SetActive(false);
        }

        // ---------------------------------------------------------- word panel
        Transform themePanel = c.Find("Themepanel");
        if (themePanel != null)
        {
            Center((RectTransform)themePanel, new Vector2(0, 540), new Vector2(940, 320));
            Sliced(themePanel.gameObject, S("panel"), 256f, 256f);
            Transform oldTitle = themePanel.Find("titletext");
            if (oldTitle != null) oldTitle.gameObject.SetActive(false);

            RectTransform ribbon = Place(Ensure(themePanel, "NT_Ribbon"), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(480, 104));
            TextMeshProUGUI rt = BuildPlank(ribbon, ribbon.sizeDelta, "", 56, S("plank"), 150f, 0.85f);
            rt.gameObject.SetActive(false); // the per-level TitleText sits on top of this plank
        }

        // ---------------------------------------------------------- board
        RectTransform board = Center(Ensure(c, "NT_Board"), new Vector2(0, GridY), new Vector2(GridMax + 100f, GridMax + 100f));
        Sliced(board.gameObject, S("board"), 256f, 256f);

        // ---------------------------------------------------------- levels
        LevelManager lm = Object.FindObjectOfType<LevelManager>(true);
        if (lm != null)
        {
            // LevelManager is a plain Transform: give it a full-screen RectTransform so level layouts are reliable.
            if (!(lm.transform is RectTransform)) lm.gameObject.AddComponent<RectTransform>();
            RectTransform lmr = lm.transform as RectTransform;
            if (lmr != null) Stretch(lmr);
            lm.gameObject.layer = 5;

            GenerateExtraLevels(lm);

            foreach (WordSearchLevel level in lm.GetComponentsInChildren<WordSearchLevel>(true))
            {
                StyleLevel(level);
            }

            if (levelText != null) SetField(lm, "levelText", levelText);
            RecordLevelTitles(lm);
        }

        // ---------------------------------------------------------- bottom bar
        RectTransform bottom = Place(Ensure(c, "NT_BottomBar"), new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(0, 300));
        Button shuffle = BuildRoundButton(bottom, "NT_Shuffle", new Vector2(-310, 175), "btn_round_green", "icon_shuffle", "SHUFFLE", out _);
        Button hintBottom = BuildRoundButton(bottom, "NT_Hint", new Vector2(0, 175), "btn_round_blue", "icon_bulb", "HINT", out RectTransform hintRect);
        TextMeshProUGUI hintBottomCount = BuildBadge(hintRect, new Vector2(70, 70));
        Button settings = BuildRoundButton(bottom, "NT_Settings", new Vector2(310, 175), "btn_round_purple", "icon_gear", "SETTINGS", out _);

        // ---------------------------------------------------------- level complete
        RectTransform completePanel = StyleCompletePanel(c);

        // ---------------------------------------------------------- settings popup
        Button sClose, sHome, sRestart, sSound;
        TextMeshProUGUI soundLabel;
        PopupAnimator settingsPopup = BuildSettings(c, out sClose, out sHome, out sRestart, out sSound, out soundLabel);

        RectTransform fx = BuildFxLayer(c);

        // ---------------------------------------------------------- sibling order
        int idx = 2;
        if (topbar != null) topbar.SetSiblingIndex(idx++);
        if (themePanel != null) themePanel.SetSiblingIndex(idx++);
        board.SetSiblingIndex(idx++);
        if (lm != null) lm.transform.SetSiblingIndex(idx++);
        bottom.SetSiblingIndex(idx++);
        if (completePanel != null) completePanel.SetSiblingIndex(idx++);
        settingsPopup.transform.SetSiblingIndex(idx++);
        fx.SetAsLastSibling();

        // ---------------------------------------------------------- HUD wiring
        GameHUD hud = GetOrAdd<GameHUD>(canvas.gameObject);
        if (lm != null) SetField(hud, "levelManager", lm);
        SetField(hud, "backButton", backButton);
        SetField(hud, "levelBanner", banner);
        SetField(hud, "board", board);
        SetArray(hud, "hintButtons", new Object[] { hintTop, hintBottom });
        SetArray(hud, "hintCountTexts", new Object[] { hintTopCount, hintBottomCount });
        SetField(hud, "shuffleButton", shuffle);
        SetField(hud, "settingsButton", settings);
        SetField(hud, "settingsPopup", settingsPopup);
        SetField(hud, "settingsCloseButton", sClose);
        SetField(hud, "settingsHomeButton", sHome);
        SetField(hud, "settingsRestartButton", sRestart);
        SetField(hud, "soundButton", sSound);
        SetField(hud, "soundLabel", soundLabel);
    }

    private static TextMeshProUGUI BuildBadge(Transform parent, Vector2 pos)
    {
        RectTransform badge = Center(Ensure(parent, "NT_Badge"), pos, new Vector2(66, 66));
        Img(badge.gameObject, S("badge"), Image.Type.Simple, 1f, true);
        RectTransform t = Stretch(Ensure(badge, "NT_Count"));
        t.offsetMin = new Vector2(0, 3);
        return Txt(t.gameObject, "3", 38, Color.white, matPlain);
    }

    private static Button BuildRoundButton(Transform parent, string name, Vector2 pos, string sprite, string icon, string label, out RectTransform rect)
    {
        RectTransform holder = Place(Ensure(parent, name), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), pos, new Vector2(200, 200));
        Image img = Img(holder.gameObject, S(sprite), Image.Type.Simple, 1f, true, true);
        Button b = MakeButton(holder.gameObject, img);

        RectTransform ic = Center(Ensure(holder, "Icon"), new Vector2(0, 4), new Vector2(104, 104));
        Img(ic.gameObject, S(icon), Image.Type.Simple, 1f, true);

        RectTransform plaque = Center(Ensure(holder, "NT_Label"), new Vector2(0, -102), new Vector2(220, 66));
        Sliced(plaque.gameObject, S("plaque"), 84f, 66f);
        RectTransform ll = Center(Ensure(plaque, "NT_LeafL"), new Vector2(-112, 6), new Vector2(70, 54));
        Img(ll.gameObject, S("leaves_left"), Image.Type.Simple, 1f, true);
        RectTransform lr = Center(Ensure(plaque, "NT_LeafR"), new Vector2(112, 6), new Vector2(70, 54));
        Img(lr.gameObject, S("leaves_right"), Image.Type.Simple, 1f, true);
        ll.gameObject.SetActive(false);
        lr.gameObject.SetActive(false);
        ll.SetSiblingIndex(0);
        lr.SetSiblingIndex(1);
        RectTransform lt = Stretch(Ensure(plaque, "NT_Text"));
        lt.offsetMin = new Vector2(12, 3);
        lt.offsetMax = new Vector2(-12, 0);
        Txt(lt.gameObject, label, 34, Brown, matPlain, true, 18f);
        lt.SetAsLastSibling();

        rect = holder;
        return b;
    }

    private static void StyleLevel(WordSearchLevel level)
    {
        RectTransform lr = (RectTransform)level.transform;
        Stretch(lr);

        // -------- title (sits on the panel ribbon)
        Transform tp = level.transform.Find("ThemePanel");
        if (tp != null)
        {
            Center((RectTransform)tp, new Vector2(0, 700), new Vector2(400, 96));
            Transform title = tp.Find("TitleText");
            if (title != null)
            {
                RectTransform tr = Stretch((RectTransform)title);
                tr.offsetMin = new Vector2(0, 8);
                Txt(title.gameObject, null, 58, Color.white, matWood, true, 28f);
            }
        }

        // -------- word chips
        Transform tw = level.transform.Find("TargetWords");
        if (tw != null)
        {
            Center((RectTransform)tw, new Vector2(0, 512), new Vector2(860, 190));
            HorizontalLayoutGroup hlg = tw.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null) Object.DestroyImmediate(hlg);
            VerticalLayoutGroup vlg = tw.GetComponent<VerticalLayoutGroup>();
            if (vlg != null) Object.DestroyImmediate(vlg);
            GridLayoutGroup glg = tw.GetComponent<GridLayoutGroup>();
            if (glg != null) Object.DestroyImmediate(glg);
            ContentSizeFitter csf = tw.GetComponent<ContentSizeFitter>();
            if (csf != null) Object.DestroyImmediate(csf);
            GetOrAdd<WordChipFlow>(tw.gameObject).Configure(16f, 16f);

            foreach (WordTarget wt in tw.GetComponentsInChildren<WordTarget>(true))
            {
                StyleChip(wt);
            }
        }

        // -------- grid
        GridLayoutGroup grid = level.GetComponentInChildren<GridLayoutGroup>(true);
        float cell = 120f;
        if (grid != null)
        {
            RectTransform gr = (RectTransform)grid.transform;
            int count = 0;
            foreach (Transform t in grid.transform) count++;
            int cols = grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount ? Mathf.Max(1, grid.constraintCount) : Mathf.CeilToInt(Mathf.Sqrt(count));
            int rows = Mathf.Max(1, Mathf.CeilToInt(count / (float)cols));
            int dim = Mathf.Max(cols, rows);
            cell = Mathf.Floor(Mathf.Min(MaxCell, (GridMax - (dim - 1) * GridSpacing) / dim));

            grid.cellSize = new Vector2(cell, cell);
            grid.spacing = new Vector2(GridSpacing, GridSpacing);
            grid.padding = new RectOffset(0, 0, 0, 0);
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = cols;

            Vector2 content = new Vector2(cols * cell + (cols - 1) * GridSpacing, rows * cell + (rows - 1) * GridSpacing);
            Center(gr, new Vector2(0, GridY), content);

            foreach (LetterTile tile in grid.GetComponentsInChildren<LetterTile>(true))
            {
                StyleTile(tile, cell);
            }
        }

        // -------- selection lines (drawn ON TOP of tiles, semi-transparent)
        Transform lines = level.transform.Find("SelectionLines");
        float thickness = cell * 0.84f;
        List<WordSelectionLine> lineList = new List<WordSelectionLine>();
        if (lines != null)
        {
            Stretch((RectTransform)lines);
            lines.SetAsLastSibling();

            foreach (WordSelectionLine l in lines.GetComponentsInChildren<WordSelectionLine>(true)) lineList.Add(l);

            // make sure there is one bar per word (+1 spare for the current drag)
            int words = level.GetComponentsInChildren<WordTarget>(true).Length;
            while (lineList.Count > 0 && lineList.Count < words + 1)
            {
                GameObject copy = Object.Instantiate(lineList[0].gameObject, lines);
                copy.name = "Line-" + (lineList.Count + 1).ToString("00");
                lineList.Add(copy.GetComponent<WordSelectionLine>());
            }

            foreach (WordSelectionLine l in lineList)
            {
                RectTransform rt = (RectTransform)l.transform;
                Center(rt, Vector2.zero, new Vector2(thickness, thickness));
                Image img = Img(l.gameObject, S("line_capsule"), Image.Type.Sliced, 64f / thickness);
                img.color = LinePalette[0];
                SetFloat(l, "thickness", thickness);
                SetField(l, "lineRect", rt);
                SetField(l, "lineImage", img);
                l.gameObject.SetActive(false);
            }
        }

        // -------- level script settings
        SerializedObject so = new SerializedObject(level);
        SerializedProperty colors = so.FindProperty("lineColors");
        if (colors != null)
        {
            colors.ClearArray();
            for (int i = 0; i < LinePalette.Length; i++)
            {
                colors.InsertArrayElementAtIndex(i);
                colors.GetArrayElementAtIndex(i).colorValue = LinePalette[i];
            }
        }
        SerializedProperty linesProp = so.FindProperty("selectionLines");
        if (linesProp != null && lineList.Count > 0)
        {
            linesProp.ClearArray();
            for (int i = 0; i < lineList.Count; i++)
            {
                linesProp.InsertArrayElementAtIndex(i);
                linesProp.GetArrayElementAtIndex(i).objectReferenceValue = lineList[i];
            }
        }
        SerializedProperty wrong = so.FindProperty("wrongShakeDuration");
        if (wrong != null) wrong.floatValue = 0.4f;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void StyleChip(WordTarget wt)
    {
        RectTransform rt = (RectTransform)wt.transform;
        string word = wt.TargetWord ?? "";
        float w = Mathf.Max(130f, 56f + word.Length * 30f);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, 70f);
        rt.localScale = Vector3.one;

        Image chip = Sliced(wt.gameObject, S("chip"), 84f, 70f);

        Transform txt = wt.transform.Find("WordText");
        if (txt != null)
        {
            RectTransform tr = Stretch((RectTransform)txt);
            tr.offsetMin = new Vector2(10, 6);
            tr.offsetMax = new Vector2(-10, 0);
            Txt(txt.gameObject, null, 40, Navy, matPlain, true, 22f);
            txt.SetAsLastSibling();
        }

        Transform strike = wt.transform.Find("CompletedVisual");
        if (strike != null)
        {
            RectTransform sr = (RectTransform)strike;
            sr.anchorMin = new Vector2(0.14f, 0.54f);
            sr.anchorMax = new Vector2(0.86f, 0.54f);
            sr.pivot = new Vector2(0f, 0.5f);
            sr.offsetMin = new Vector2(0, -3);
            sr.offsetMax = new Vector2(0, 3);
            Image si = Img(strike.gameObject, null);
            si.color = new Color(1f, 1f, 1f, 0.9f);
            strike.gameObject.SetActive(false);
            strike.SetAsLastSibling();
        }

        SetField(wt, "chipImage", chip);
        SetField(wt, "normalSprite", S("chip"));
        SetField(wt, "foundSprite", S("chip_found"));
        SetColor(wt, "normalTextColor", Navy);
        SetColor(wt, "foundTextColor", Color.white);
    }

    private static void StyleTile(LetterTile tile, float cell)
    {
        GameObject go = tile.gameObject;
        Image bg = Img(go, S("tile"), Image.Type.Simple, 1f, false, true);
        bg.color = Color.white;

        float lip = cell * 0.07f;
        string[] highlights = { "SelectionHighlight", "FoundHighlight", "HintHighlight" };
        Color[] colors =
        {
            new Color(0.95f, 0.82f, 0.55f, 0.45f),
            new Color(0.93f, 0.88f, 0.78f, 0.35f),
            new Color(0.90f, 0.70f, 0.35f, 0.6f)
        };
        for (int i = 0; i < highlights.Length; i++)
        {
            Transform h = go.transform.Find(highlights[i]);
            if (h == null) continue;
            RectTransform hr = Stretch((RectTransform)h);
            hr.offsetMin = new Vector2(3, lip);
            hr.offsetMax = new Vector2(-3, -2);
            Image hi = Img(h.gameObject, S("tile_flat"), Image.Type.Sliced, 160f / Mathf.Max(1f, cell));
            hi.color = colors[i];
        }

        Transform lt = go.transform.Find("LetterText");
        if (lt != null)
        {
            RectTransform tr = Stretch((RectTransform)lt);
            tr.offsetMin = new Vector2(0, lip);
            tr.offsetMax = Vector2.zero;
            Grad(Txt(lt.gameObject, null, Mathf.Round(cell * 0.52f), Color.white, matPlain));
            lt.SetAsLastSibling();
        }

        // letters use a brown->gold vertex gradient; the tile tints it (white = untouched)
        SetColor(tile, "normalLetterColor", Color.white);
        SetColor(tile, "selectedLetterColor", new Color(0.85f, 0.78f, 0.7f, 1f));
        SetFloat(tile, "selectedScale", 1.1f);
    }

    private static RectTransform StyleCompletePanel(Transform canvas)
    {
        Transform panel = canvas.Find("LevelCompletePanel");
        if (panel == null) return null;

        RectTransform pr = Stretch((RectTransform)panel);
        Image overlay = GetOrAdd<Image>(panel.gameObject);
        overlay.sprite = null;
        overlay.color = Overlay;
        overlay.raycastTarget = true;

        RectTransform rays = Center(Ensure(panel, "NT_Rays"), new Vector2(0, 60), new Vector2(1250, 1250));
        Image ri = Img(rays.gameObject, S("rays"), Image.Type.Simple, 1f, true);
        ri.color = new Color(1f, 0.9f, 0.65f, 0.22f);
        rays.SetSiblingIndex(0);

        Transform card = panel.Find("PanelCard");
        RectTransform cardRect = null;
        RectTransform[] stars = new RectTransform[3];
        RectTransform nextRect = null, subRect = null;

        if (card != null)
        {
            cardRect = Center((RectTransform)card, new Vector2(0, 0), new Vector2(820, 760));
            Sliced(card.gameObject, S("panel"), 256f, 256f);

            RectTransform ribbon = Place(Ensure(card, "NT_Ribbon"), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(660, 140));
            TextMeshProUGUI ribbonText = BuildPlank(ribbon, ribbon.sizeDelta, "LEVEL COMPLETE", 60, S("plank"), 150f, 1.1f);

            Transform title = card.Find("TitleText");
            if (title != null)
            {
                title.SetParent(ribbon, false);
                RectTransform tr = Stretch((RectTransform)title);
                tr.offsetMin = new Vector2(60, 10);
                tr.offsetMax = new Vector2(-60, 0);
                Txt(title.gameObject, "LEVEL COMPLETE", 60, Color.white, matWood, true, 28f);
                ribbonText.gameObject.SetActive(false);
            }

            RectTransform starRow = Center(Ensure(card, "NT_Stars"), new Vector2(0, 150), new Vector2(700, 260));
            Vector2[] sp = { new Vector2(-200, -20), new Vector2(0, 20), new Vector2(200, -20) };
            float[] ss = { 180, 230, 180 };
            float[] rot = { 14, 0, -14 };
            for (int i = 0; i < 3; i++)
            {
                RectTransform holder = Center(Ensure(starRow, "NT_StarSlot" + i), sp[i], new Vector2(ss[i], ss[i]));
                holder.localRotation = Quaternion.Euler(0, 0, rot[i]);
                Img(holder.gameObject, S("star_empty"), Image.Type.Simple, 1f, true);
                RectTransform star = Stretch(Ensure(holder, "NT_Star"));
                Img(star.gameObject, S("star"), Image.Type.Simple, 1f, true);
                stars[i] = star;
            }

            Transform sub = card.Find("SubtitleText");
            if (sub != null)
            {
                subRect = Center((RectTransform)sub, new Vector2(0, -60), new Vector2(700, 100));
                Txt(sub.gameObject, "AWESOME!", 76, new Color(0.55f, 0.27f, 0.08f, 1f), matPlain);
            }

            Transform next = card.Find("NextButton");
            if (next != null)
            {
                nextRect = Center((RectTransform)next, new Vector2(0, -240), new Vector2(460, 150));
                Image ni = Sliced(next.gameObject, S("btn_green"), 150f, 150f, true);
                MakeButton(next.gameObject, ni, true);
                Transform bt = next.Find("ButtonText");
                if (bt != null)
                {
                    RectTransform btr = Stretch((RectTransform)bt);
                    btr.offsetMin = new Vector2(0, 14);
                    Txt(bt.gameObject, "NEXT", 72, Color.white, matGreen);
                }
            }
        }

        PopupAnimator anim = GetOrAdd<PopupAnimator>(panel.gameObject);
        anim.Setup(cardRect, stars, rays, new[] { subRect, nextRect }, true, true);
        EditorUtility.SetDirty(anim);
        panel.gameObject.SetActive(false);
        return pr;
    }

    private static PopupAnimator BuildSettings(Transform canvas, out Button close, out Button home, out Button restart, out Button sound, out TextMeshProUGUI soundLabel)
    {
        RectTransform pop = Stretch(Ensure(canvas, "NT_SettingsPopup"));
        Image overlay = Img(pop.gameObject, null, Image.Type.Simple, 1f, false, true);
        overlay.color = Overlay;

        RectTransform card = Center(Ensure(pop, "NT_Card"), new Vector2(0, 0), new Vector2(760, 820));
        Sliced(card.gameObject, S("panel"), 256f, 256f, true);

        RectTransform ribbon = Place(Ensure(card, "NT_Ribbon"), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(520, 130));
        BuildPlank(ribbon, ribbon.sizeDelta, "SETTINGS", 62, S("plank"), 150f, 1f);

        sound = BuildPillButton(card, "NT_Sound", new Vector2(0, 150), "btn_blue", matBlue, "SOUND: ON", out soundLabel);
        restart = BuildPillButton(card, "NT_Restart", new Vector2(0, -20), "btn_orange", matOrange, "RESTART", out _);
        home = BuildPillButton(card, "NT_Home", new Vector2(0, -190), "btn_green", matGreen, "HOME", out _);

        RectTransform cl = Place(Ensure(card, "NT_Close"), new Vector2(1, 1), new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-30, -30), new Vector2(110, 110));
        Image ci = Img(cl.gameObject, S("btn_square_red"), Image.Type.Simple, 1f, false, true);
        close = MakeButton(cl.gameObject, ci);
        RectTransform cic = Center(Ensure(cl, "Icon"), new Vector2(0, 5), new Vector2(56, 56));
        Img(cic.gameObject, S("icon_close"), Image.Type.Simple, 1f, true);

        PopupAnimator anim = GetOrAdd<PopupAnimator>(pop.gameObject);
        anim.Setup(card, new RectTransform[0], null, new RectTransform[0], false, false);
        EditorUtility.SetDirty(anim);
        pop.gameObject.SetActive(false);
        return anim;
    }

    private static Button BuildPillButton(Transform parent, string name, Vector2 pos, string sprite, Material mat, string label, out TextMeshProUGUI text)
    {
        RectTransform b = Center(Ensure(parent, name), pos, new Vector2(520, 140));
        Image img = Sliced(b.gameObject, S(sprite), 150f, 140f, true);
        Button btn = MakeButton(b.gameObject, img);
        RectTransform t = Stretch(Ensure(b, "NT_Text"));
        t.offsetMin = new Vector2(30, 14);
        t.offsetMax = new Vector2(-30, 0);
        bool primary = sprite == "btn_green";
        text = Txt(t.gameObject, label, 56, primary ? Color.white : Brown, primary ? matWood : matPlain, true, 28f);
        return btn;
    }
}
