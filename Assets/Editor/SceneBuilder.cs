using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WordBubblePop.EditorTools
{
    /// <summary>
    /// Builds the game scene with real objects you can select and edit:
    ///   Assets/Scenes/WordBubblePop.unity   all screens (Hierarchy)
    ///   Assets/Prefabs/Bubble.prefab        look of one bubble
    ///   Assets/Settings/GameSettings.asset  numbers, words, colours, music
    /// Menu: Word Bubble Pop > Rebuild Scene
    /// </summary>
    public static class SceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/WordBubblePop.unity";
        public const string SettingsPath = "Assets/Settings/GameSettings.asset";
        public const string PrefabPath = "Assets/Prefabs/Bubble.prefab";
        const string UiFolder = "Assets/Art/UI/";
        const string PictureFolder = "Assets/Art/Pictures/";
        const string AudioFolder = "Assets/Audio/";

        static Sprite circle, ring, glow, box, star, water;

        [MenuItem("Word Bubble Pop/Rebuild Scene", false, 1)]
        static void RebuildFromMenu()
        {
            if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog("Rebuild scene?",
                    "This replaces the scene with a fresh copy. Changes made to objects in the scene will be lost.\n\n" +
                    "Your Game Settings (words, colours, music) and the Bubble prefab are kept.",
                    "Rebuild", "Cancel"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build();
        }

        [MenuItem("Word Bubble Pop/Open Game Settings", false, 20)]
        static void SelectSettings()
        {
            var s = AssetDatabase.LoadAssetAtPath<GameSettings>(SettingsPath);
            if (s != null) { Selection.activeObject = s; EditorGUIUtility.PingObject(s); }
        }

        [MenuItem("Word Bubble Pop/Open Bubble Prefab", false, 21)]
        static void OpenBubble()
        {
            var p = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (p != null) AssetDatabase.OpenAsset(p);
        }

        // =====================================================================

        public static void Build()
        {
            FixTextureImports();
            LoadSprites();
            var settings = EnsureSettings();
            var bubble = EnsureBubblePrefab();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera (with the audio listener so music can be heard)
            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.Sky;
            cam.orthographic = true;
            camGo.transform.position = new Vector3(0, 0, -10);

            BuildEventSystem();
            var view = BuildCanvas();

            // Game logic object with its two audio sources
            var gmGo = new GameObject("Game");
            var gm = gmGo.AddComponent<GameManager>();
            gm.settings = settings;
            gm.view = view;
            gm.bubblePrefab = bubble;
            gm.musicSource = NewAudio("Music", gmGo.transform);
            gm.soundSource = NewAudio("Sounds", gmGo.transform);

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();

            Selection.activeObject = view.gameObject;
            Debug.Log("Word Bubble Pop: scene built. Select objects in the Hierarchy to edit them, " +
                      "or open Assets/Settings/GameSettings for numbers, words, colours and music.");
        }

        static AudioSource NewAudio(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var a = go.AddComponent<AudioSource>();
            a.playOnAwake = false;
            return a;
        }

        static void BuildEventSystem()
        {
            var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            var module = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (module != null) { es.AddComponent(module); return; }
#endif
            es.AddComponent<StandaloneInputModule>();
        }

        // =====================================================================
        // Assets
        // =====================================================================

        static void FixTextureImports()
        {
            AssetDatabase.Refresh();
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art/UI", "Assets/Art/Pictures" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                MakeSprite(path);
            }
        }

        public static void MakeSprite(string path)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) return;
            bool roundBox = path.EndsWith("RoundBox.png");
            var border = roundBox ? new Vector4(60, 60, 60, 60) : Vector4.zero;
            if (imp.textureType == TextureImporterType.Sprite && imp.spriteBorder == border && !imp.mipmapEnabled) return;

            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.filterMode = FilterMode.Bilinear;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.spriteBorder = border;
            imp.SaveAndReimport();
        }

        static Sprite LoadSprite(string path)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s == null) Debug.LogWarning("Word Bubble Pop: missing sprite " + path);
            return s;
        }

        static void LoadSprites()
        {
            circle = LoadSprite(UiFolder + "Circle.png");
            ring = LoadSprite(UiFolder + "Ring.png");
            glow = LoadSprite(UiFolder + "Glow.png");
            box = LoadSprite(UiFolder + "RoundBox.png");
            star = LoadSprite(UiFolder + "Star.png");
            water = LoadSprite(UiFolder + "WaterGradient.png");
        }

        static GameSettings EnsureSettings()
        {
            var s = AssetDatabase.LoadAssetAtPath<GameSettings>(SettingsPath);
            if (s != null) return s;

            s = ScriptableObject.CreateInstance<GameSettings>();
            foreach (var w in WordData.DefaultWords)
                s.words.Add(new WordEntry { word = w, picture = AssetDatabase.LoadAssetAtPath<Sprite>(PictureFolder + w + ".png") });

            s.music = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioFolder + "Music.wav");
            s.popSound = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioFolder + "Pop.wav");
            s.wrongSound = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioFolder + "Wrong.wav");
            s.tickSound = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioFolder + "Tick.wav");
            s.winSound = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioFolder + "Win.wav");
            s.gameOverSound = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioFolder + "GameOver.wav");

            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
            AssetDatabase.CreateAsset(s, SettingsPath);
            AssetDatabase.SaveAssets();
            return s;
        }

        static Bubble EnsureBubblePrefab()
        {
            var existingGo = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var existing = existingGo != null ? existingGo.GetComponent<Bubble>() : null;
            if (existing != null) return existing;

            const float size = 200f;
            var root = new GameObject("Bubble", typeof(RectTransform), typeof(CanvasGroup));
            root.layer = 5;
            var rt = (RectTransform)root.transform;
            rt.sizeDelta = new Vector2(size, size);
            var b = root.AddComponent<Bubble>();

            var glowImg = UIKit.Img("Glow (hint)", rt, glow, Palette.Mango);
            glowImg.rectTransform.anchorMin = new Vector2(-0.35f, -0.35f);
            glowImg.rectTransform.anchorMax = new Vector2(1.35f, 1.35f);
            glowImg.rectTransform.offsetMin = glowImg.rectTransform.offsetMax = Vector2.zero;
            glowImg.gameObject.SetActive(false);

            var skin = UIKit.Rect("Skin", rt);
            UIKit.Stretch(skin);

            var body = UIKit.Img("Body", skin, circle, Color.white, true);
            UIKit.Stretch(body.rectTransform);
            var shadow = body.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.03f, 0.18f, 0.22f, 0.35f);
            shadow.effectDistance = new Vector2(0, -12);

            var ringImg = UIKit.Img("Ring", skin, ring, new Color(1, 1, 1, 0.92f));
            UIKit.Stretch(ringImg.rectTransform);

            var shine = UIKit.Img("Shine", skin, circle, new Color(1, 1, 1, 0.9f));
            shine.rectTransform.anchorMin = new Vector2(0.2f, 0.655f);
            shine.rectTransform.anchorMax = new Vector2(0.4f, 0.825f);
            shine.rectTransform.offsetMin = shine.rectTransform.offsetMax = Vector2.zero;

            var letter = UIKit.Label("Letter", skin, "A", 100, Palette.Ink, true);
            UIKit.Stretch(letter.rectTransform, 0, 0, 0, -12);

            b.skin = skin;
            b.body = body;
            b.glow = glowImg;
            b.letterText = letter;

            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            var saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return saved != null ? saved.GetComponent<Bubble>() : null;
        }

        // =====================================================================
        // Canvas and screens
        // =====================================================================

        static GameView BuildCanvas()
        {
            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.layer = 5;
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0;
            var canvas = (RectTransform)canvasGo.transform;

            var view = canvasGo.AddComponent<GameView>();
            view.scaler = scaler;
            view.splashSprite = circle;

            var bg = UIKit.Img("Background", canvas, null, Palette.Sky);
            UIKit.Stretch(bg.rectTransform);

            view.safeArea = UIKit.Rect("Safe Area", canvas);
            UIKit.Stretch(view.safeArea);
            view.column = UIKit.Rect("Game Column", view.safeArea);
            view.column.anchorMin = new Vector2(0.5f, 0);
            view.column.anchorMax = new Vector2(0.5f, 1);
            view.column.sizeDelta = new Vector2(1080, 0);

            BuildTopBar(view);
            BuildCard(view);
            BuildRiver(view);
            BuildStartScreen(view, canvas);
            BuildLevelScreen(view, canvas);
            BuildTimesUpScreen(view, canvas);
            return view;
        }

        static Button MakeButton(string name, Transform parent, string text, Color bg, Color fg, Color shadow, int fontSize)
        {
            var img = UIKit.Img(name, parent, box, bg, true, 1.25f);
            if (shadow.a > 0) UIKit.AddShadow(img.gameObject, shadow, 12);
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var colors = b.colors;
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            b.colors = colors;
            var label = UIKit.Label("Text", img.transform, text, fontSize, fg, true);
            UIKit.Stretch(label.rectTransform);
            return b;
        }

        static void BuildTopBar(GameView view)
        {
            var bar = UIKit.Rect("Top Bar", view.column);
            UIKit.TopRow(bar, 30, 104, 40, 40);

            var levelPill = UIKit.Img("Level Pill", bar, box, Palette.Paper, false, 1.25f);
            UIKit.AddShadow(levelPill.gameObject, Palette.LeafDeep, 8);
            UIKit.Place(levelPill.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(450, 104));
            view.levelText = UIKit.Label("Level Text", levelPill.transform, "Level 1  3 letters", 46, Palette.Ink, true, TextAnchor.MiddleLeft);
            UIKit.Stretch(view.levelText.rectTransform, 32, 0, 16, 0);

            view.soundButton = MakeButton("Sound Button", bar, "Sound On", Palette.Paper, Palette.Ink, Palette.LeafDeep, 38);
            UIKit.Place((RectTransform)view.soundButton.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), Vector2.zero, new Vector2(250, 104));
            view.soundText = view.soundButton.GetComponentInChildren<Text>();

            var starPill = UIKit.Img("Stars Pill", bar, box, Palette.Paper, false, 1.25f);
            UIKit.AddShadow(starPill.gameObject, Palette.LeafDeep, 8);
            UIKit.Place(starPill.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-270, 0), new Vector2(200, 104));
            var s = UIKit.Img("Star Icon", starPill.transform, star, Palette.Gold);
            UIKit.Place(s.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(26, 2), new Vector2(60, 60));
            view.starText = UIKit.Label("Stars Text", starPill.transform, "0", 48, Palette.Ink, true, TextAnchor.MiddleLeft);
            UIKit.Stretch(view.starText.rectTransform, 98, 0, 10, 0);
        }

        static void BuildCard(GameView view)
        {
            var card = UIKit.Img("Word Card", view.column, box, Palette.Paper, false, 1.25f);
            var outline = card.gameObject.AddComponent<Outline>();
            outline.effectColor = Palette.LeafDeep;
            outline.effectDistance = new Vector2(4, -4);
            UIKit.AddShadow(card.gameObject, Palette.LeafDeep, 16);
            UIKit.TopRow(card.rectTransform, 170, 600, 40, 40);
            var c = card.transform;

            var picBg = UIKit.Img("Picture Button", c, circle, Palette.PicBg, true);
            UIKit.Place(picBg.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(34, -34), new Vector2(230, 230));
            view.pictureButton = picBg.gameObject.AddComponent<Button>();
            view.pictureButton.targetGraphic = picBg;
            view.picture = UIKit.Img("Picture", picBg.transform, AssetDatabase.LoadAssetAtPath<Sprite>(PictureFolder + "DOG.png"), Color.white);
            view.picture.preserveAspect = true;
            UIKit.Place(view.picture.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(165, 165));

            var ask = UIKit.Label("Hint Text", c, "Look at the picture", 38, Palette.Muted, false, TextAnchor.MiddleLeft);
            UIKit.TopRow(ask.rectTransform, 52, 50, 294, 30);
            var what = UIKit.Label("Question Text", c, "What is this?", 78, Palette.LeafDeep, true, TextAnchor.MiddleLeft);
            UIKit.TopRow(what.rectTransform, 98, 104, 294, 30);
            var tap = UIKit.Label("Tap Text", c, "Tap the picture to hear the word", 33, Palette.Muted, false, TextAnchor.MiddleLeft);
            UIKit.TopRow(tap.rectTransform, 204, 46, 294, 30);

            // Letter boxes: the template is copied for each letter
            view.slotsRow = UIKit.Rect("Letter Boxes", c);
            UIKit.TopRow(view.slotsRow, 292, 170, 30, 30);
            var slot = UIKit.Img("Letter Box (template)", view.slotsRow, box, Color.white, false, 2.2f);
            var so = slot.gameObject.AddComponent<Outline>();
            so.effectColor = Palette.SlotBorder;
            so.effectDistance = new Vector2(5, -5);
            UIKit.Place(slot.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(140, 166));
            var sl = UIKit.Label("Letter", slot.transform, "A", 96, Palette.Ink, true);
            UIKit.Stretch(sl.rectTransform, 0, 0, 0, -8);
            slot.gameObject.SetActive(false);
            view.slotTemplate = slot;

            // Timer
            var track = UIKit.Img("Timer Track", c, box, Palette.Track, false, 3.6f);
            UIKit.TopRow(track.rectTransform, 500, 34, 44, 160);
            view.timerFill = UIKit.Img("Timer Fill", track.transform, box, Palette.Leaf, false, 3.6f);
            UIKit.Stretch(view.timerFill.rectTransform);
            view.timerText = UIKit.Label("Timer Text", c, "20s", 46, Palette.LeafDeep, true, TextAnchor.MiddleRight);
            UIKit.Place(view.timerText.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -484), new Vector2(130, 66));

            // Progress dots (one per word in the level; extra dots hide themselves)
            var dotRow = UIKit.Rect("Progress Dots", c);
            UIKit.TopRow(dotRow, 556, 26, 0, 0);
            var h = dotRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleCenter;
            h.spacing = 18;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            view.progressDots = new Image[10];
            for (int i = 0; i < view.progressDots.Length; i++)
            {
                var d = UIKit.Img("Dot " + (i + 1), dotRow, circle, i == 0 ? view.dotCurrent : view.dotTodo);
                UIKit.Layout(d, 38, 24);
                d.gameObject.SetActive(i < 5);
                view.progressDots[i] = d;
            }

            // "+3 stars" that floats up when a word is finished
            var earned = UIKit.Rect("Earned Stars", c);
            UIKit.Place(earned, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-30, -24), new Vector2(190, 70));
            view.earned = earned.gameObject.AddComponent<CanvasGroup>();
            view.earned.alpha = 0;
            view.earnedText = UIKit.Label("Text", earned, "+3", 56, Palette.Gold, true, TextAnchor.MiddleRight);
            UIKit.Stretch(view.earnedText.rectTransform, 0, 0, 74, 0);
            var es = UIKit.Img("Star Icon", earned, star, Palette.Gold);
            UIKit.Place(es.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), Vector2.zero, new Vector2(64, 64));
        }

        static void BuildRiver(GameView view)
        {
            var river = UIKit.Img("River", view.column, box, Palette.WaterDeep, true, 0.8f);
            river.rectTransform.anchorMin = Vector2.zero;
            river.rectTransform.anchorMax = Vector2.one;
            river.rectTransform.offsetMin = new Vector2(40, -90);
            river.rectTransform.offsetMax = new Vector2(-40, -806);
            river.gameObject.AddComponent<Mask>().showMaskGraphic = true;

            var w = UIKit.Img("Water", river.transform, water, Color.white);
            UIKit.Stretch(w.rectTransform);

            var lily1 = UIKit.Img("Lily Pad", river.transform, circle, Palette.LilyPad);
            UIKit.Place(lily1.rectTransform, new Vector2(0.16f, 0.78f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(130, 82));
            var lily2 = UIKit.Img("Lily Pad", river.transform, circle, Palette.LilyPad);
            UIKit.Place(lily2.rectTransform, new Vector2(0.82f, 0.38f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(104, 66));

            view.bubbleField = UIKit.Rect("Bubble Area", river.transform);
            UIKit.Stretch(view.bubbleField, 0, 90, 0, 0);
        }

        // ------------------------------------------------------------ popup screens

        static RectTransform MakeScreen(Transform parent, string name, out GameObject root)
        {
            var dim = UIKit.Img(name, parent, null, Palette.Dim, true);
            UIKit.Stretch(dim.rectTransform);
            root = dim.gameObject;

            var p = UIKit.Img("Panel", dim.transform, box, Palette.Paper, true, 1.25f);
            var outline = p.gameObject.AddComponent<Outline>();
            outline.effectColor = Palette.LeafDeep;
            outline.effectDistance = new Vector2(4, -4);
            UIKit.AddShadow(p.gameObject, Palette.LeafDeep, 18);
            UIKit.Place(p.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(920, 0));

            var v = p.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(56, 56, 60, 64);
            v.spacing = 30;
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            p.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return p.rectTransform;
        }

        static Button PanelButton(RectTransform panel, string name, string text, bool primary)
        {
            var b = primary
                ? MakeButton(name, panel, text, Palette.Mango, Palette.Ink, Palette.MangoDark, 62)
                : MakeButton(name, panel, text, Palette.SoftGreen, Palette.Muted, Color.clear, 42);
            UIKit.Layout(b, -1, primary ? 140 : 96);
            return b;
        }

        static void BuildStartScreen(GameView view, Transform canvas)
        {
            var panel = MakeScreen(canvas, "Start Screen", out var root);
            view.startScreen = root;

            LogoRow(panel, "WORD BUBBLE");
            LogoRow(panel, "POP");
            UIKit.Label("Title", panel, "Pop the bubbles, make the word!", 62, Palette.LeafDeep, true);
            Step(panel, "1", "Look at the picture, like a dog or a cat.");
            Step(panel, "2", "Pop the letter bubbles in the right order: D, O, G.");
            Step(panel, "3", "You have 20 seconds for each word. Finish in time to win stars!");

            view.playButton = PanelButton(panel, "Play Button", "Play", true);
            view.playText = view.playButton.GetComponentInChildren<Text>();
            view.resetButton = PanelButton(panel, "Start Over Button", "Start over", false);
            root.SetActive(false);
        }

        static void LogoRow(RectTransform panel, string text)
        {
            var row = UIKit.Rect("Logo", panel);
            UIKit.Layout(row, -1, 76);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleCenter;
            h.spacing = 6;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            var tints = new[]
            {
                Palette.BubbleTints[0], Palette.BubbleTints[1], Palette.BubbleTints[2],
                Palette.BubbleTints[3], Palette.BubbleTints[4], Palette.BubbleTints[5]
            };
            int i = 0;
            foreach (char ch in text)
            {
                if (ch == ' ') { UIKit.Layout(UIKit.Rect("Gap", row), 22, 70); continue; }
                var bub = UIKit.Img("Letter " + ch, row, circle, tints[i++ % tints.Length]);
                UIKit.Layout(bub, 70, 70);
                var t = UIKit.Label("Text", bub.transform, ch.ToString(), 40, Palette.Ink, true);
                UIKit.Stretch(t.rectTransform);
            }
        }

        static void Step(RectTransform panel, string number, string text)
        {
            var row = UIKit.Img("Step " + number, panel, box, Palette.SoftGreen, false, 2f);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(22, 22, 18, 18);
            h.spacing = 22;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;

            var num = UIKit.Img("Number", row.transform, circle, Palette.Leaf);
            UIKit.Layout(num, 60, 60);
            var n = UIKit.Label("Text", num.transform, number, 36, Color.white, true);
            UIKit.Stretch(n.rectTransform);

            var t = UIKit.Label("Text", row.transform, text, 38, Palette.Ink, false, TextAnchor.MiddleLeft);
            UIKit.Layout(t, 640).flexibleWidth = 1;
        }

        static void BuildLevelScreen(GameView view, Transform canvas)
        {
            var panel = MakeScreen(canvas, "Level Complete Screen", out var root);
            view.levelScreen = root;

            view.levelTitle = UIKit.Label("Title", panel, "Well done! Level 1 complete", 66, Palette.LeafDeep, true);

            var starsRow = UIKit.Rect("Stars", panel);
            UIKit.Layout(starsRow, -1, 100);
            var s = UIKit.Img("Star Icon", starsRow, star, Palette.Gold);
            UIKit.Place(s.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1, 0.5f), new Vector2(-8, 0), new Vector2(96, 96));
            view.levelStarsText = UIKit.Label("Stars Text", starsRow, "15", 90, Palette.Gold, true, TextAnchor.MiddleLeft);
            UIKit.Place(view.levelStarsText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(240, 100));

            UIKit.Label("Subtitle", panel, "Words you learned today:", 42, Palette.Ink);

            view.learnedGrid = UIKit.Rect("Learned Words", panel);
            var grid = view.learnedGrid.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(250, 104);
            grid.spacing = new Vector2(14, 14);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.UpperCenter;

            var cell = UIKit.Img("Learned Word (template)", view.learnedGrid, box, Palette.SoftGreen, false, 2f);
            var pic = UIKit.Img("Picture", cell.transform, AssetDatabase.LoadAssetAtPath<Sprite>(PictureFolder + "DOG.png"), Color.white);
            pic.preserveAspect = true;
            UIKit.Place(pic.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(74, 74));
            var word = UIKit.Label("Word", cell.transform, "DOG", 38, Palette.Ink, true, TextAnchor.MiddleLeft);
            UIKit.Stretch(word.rectTransform, 98, 0, 8, 0);
            cell.gameObject.SetActive(false);
            view.learnedTemplate = cell.gameObject;

            view.nextButton = PanelButton(panel, "Next Level Button", "Next level", true);
            root.SetActive(false);
        }

        static void BuildTimesUpScreen(GameView view, Transform canvas)
        {
            var panel = MakeScreen(canvas, "Times Up Screen", out var root);
            view.overScreen = root;

            UIKit.Label("Title", panel, "Oops! Time's up", 72, Palette.LeafDeep, true);
            UIKit.Label("Subtitle", panel, "That's okay! The word was:", 42, Palette.Ink);

            var answer = UIKit.Img("Answer", panel, box, Palette.SoftGreen, false, 1.25f);
            UIKit.Layout(answer, -1, 210);
            view.answerPicture = UIKit.Img("Picture", answer.transform, AssetDatabase.LoadAssetAtPath<Sprite>(PictureFolder + "DOG.png"), Color.white);
            view.answerPicture.preserveAspect = true;
            UIKit.Place(view.answerPicture.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1, 0.5f), new Vector2(-30, 0), new Vector2(160, 160));
            view.answerWord = UIKit.Label("Word", answer.transform, "DOG", 96, Palette.Ink, true, TextAnchor.MiddleLeft);
            UIKit.Place(view.answerWord.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(380, 160));

            view.overInfo = UIKit.Label("Info", panel, "You finished 2 words in level 1. Try again!", 40, Palette.Ink);
            view.retryButton = PanelButton(panel, "Play Again Button", "Play again", true);
            root.SetActive(false);
        }
    }

    /// <summary>
    /// New pictures dropped into Assets/Art become Sprites automatically,
    /// so they can be dragged straight into Game Settings.
    /// </summary>
    public class ArtImportSettings : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Art/Pictures/") && !assetPath.StartsWith("Assets/Art/UI/")) return;
            if (!assetImporter.importSettingsMissing) return; // keep any settings you changed yourself
            var imp = (TextureImporter)assetImporter;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            if (assetPath.EndsWith("RoundBox.png")) imp.spriteBorder = new Vector4(60, 60, 60, 60);
        }
    }
}
