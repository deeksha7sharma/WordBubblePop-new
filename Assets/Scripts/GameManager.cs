using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WordBubblePop
{
    /// <summary>
    /// Word Bubble Pop: a spelling game for kids.
    /// Look at the picture and pop the letter bubbles in the right order
    /// to spell the word before the timer runs out.
    ///
    /// This script only contains the game rules. Everything you see is a normal
    /// object in the scene (see GameView), the bubble look is the prefab
    /// Assets/Prefabs/Bubble, and all numbers, words, colours, music and sounds
    /// are in Assets/Settings/GameSettings.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Links (set up automatically)")]
        public GameSettings settings;
        public GameView view;
        public Bubble bubblePrefab;
        public AudioSource musicSource;
        public AudioSource soundSource;

        const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string PrefLevel = "wbp_level";
        const string PrefStars = "wbp_stars";
        const string PrefSound = "wbp_sound";

        // ------------------------------------------------------------ state
        int level = 1;
        int stars;
        List<WordEntry> words = new List<WordEntry>();
        int idx;
        int levelStars;
        WordEntry current;
        string word = "DOG";
        int pos;
        int wrong;   // mistakes in this word (for stars)
        int miss;    // mistakes on the current letter (for hints)
        bool sound = true;
        bool running;
        bool locked = true;
        float timeLeft;
        int lastSec;
        float spawnTimer;

        readonly List<Bubble> bubbles = new List<Bubble>();
        readonly List<Image> slotBoxes = new List<Image>();
        readonly List<Text> slotLetters = new List<Text>();
        Rect lastSafeArea;
        Vector2 lastScreen;
        Vector2 earnedBasePos;

        // =====================================================================
        // Setup
        // =====================================================================

        void Awake()
        {
            if (settings == null || view == null || bubblePrefab == null)
            {
                Debug.LogError("Word Bubble Pop: links are missing. Use the menu Word Bubble Pop > Rebuild Scene.");
                enabled = false;
                return;
            }

            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            level = Mathf.Max(1, PlayerPrefs.GetInt(PrefLevel, 1));
            stars = PlayerPrefs.GetInt(PrefStars, 0);
            sound = PlayerPrefs.GetInt(PrefSound, 1) == 1;

            EnsureEventSystem();
            HookButtons();
            Speech.Init();
            if (view.slotTemplate != null) view.slotTemplate.gameObject.SetActive(false);
            if (view.learnedTemplate != null) view.learnedTemplate.SetActive(false);
            if (view.earned != null)
            {
                earnedBasePos = ((RectTransform)view.earned.transform).anchoredPosition;
                view.earned.alpha = 0;
            }
            ApplySound();
        }

        void Start()
        {
            if (!enabled) return;
            UpdateLayout(true);
            ShowStart();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) Speech.Stop();
        }

        void OnApplicationQuit()
        {
            Speech.Shutdown();
        }

        static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            var module = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (module != null)
            {
                es.AddComponent(module);
                return;
            }
#endif
            es.AddComponent<StandaloneInputModule>();
        }

        void HookButtons()
        {
            if (view.soundButton) view.soundButton.onClick.AddListener(ToggleSound);
            if (view.pictureButton) view.pictureButton.onClick.AddListener(() => Say(word.ToLowerInvariant()));
            if (view.playButton) view.playButton.onClick.AddListener(() =>
            {
                view.startScreen.SetActive(false);
                StartLevel();
            });
            if (view.resetButton) view.resetButton.onClick.AddListener(() =>
            {
                level = 1;
                stars = 0;
                Save();
                view.startScreen.SetActive(false);
                StartLevel();
            });
            if (view.nextButton) view.nextButton.onClick.AddListener(() =>
            {
                view.levelScreen.SetActive(false);
                StartLevel();
            });
            if (view.retryButton) view.retryButton.onClick.AddListener(() =>
            {
                view.overScreen.SetActive(false);
                StartLevel();
            });
        }

        // =====================================================================
        // Layout (phone safe area + max column width)
        // =====================================================================

        void UpdateLayout(bool force = false)
        {
            var screen = new Vector2(Screen.width, Screen.height);
            var sa = Screen.safeArea;
            if (force || sa != lastSafeArea || screen != lastScreen)
            {
                lastSafeArea = sa;
                lastScreen = screen;

                // Portrait phones fit the width; wide screens (Editor, tablets) fit the height.
                if (view.scaler != null)
                    view.scaler.matchWidthOrHeight = screen.x / Mathf.Max(1f, screen.y) > 1080f / 1920f ? 1f : 0f;

                if (view.safeArea != null)
                {
                    view.safeArea.anchorMin = new Vector2(sa.xMin / Mathf.Max(1, screen.x), sa.yMin / Mathf.Max(1, screen.y));
                    view.safeArea.anchorMax = new Vector2(sa.xMax / Mathf.Max(1, screen.x), sa.yMax / Mathf.Max(1, screen.y));
                    view.safeArea.offsetMin = Vector2.zero;
                    view.safeArea.offsetMax = Vector2.zero;
                }
            }

            if (view.column != null && view.safeArea != null)
            {
                float available = view.safeArea.rect.width;
                float width = available > 0 ? Mathf.Min(available, view.columnMaxWidth) : view.columnMaxWidth;
                if (Mathf.Abs(view.column.sizeDelta.x - width) > 0.5f)
                    view.column.sizeDelta = new Vector2(width, view.column.sizeDelta.y);
            }
        }

        // =====================================================================
        // Game flow
        // =====================================================================

        void ShowStart()
        {
            bool resume = level > 1;
            if (view.playText) view.playText.text = resume ? "Play level " + level : "Play";
            if (view.resetButton) view.resetButton.gameObject.SetActive(resume);

            // Example word on the card behind the start screen
            current = FindWord("DOG");
            word = current != null ? current.word.Trim().ToUpperInvariant() : "DOG";
            SetPicture(view.picture, current);
            BuildSlots(word);
            SetSlot(0, word[0], SlotState.Filled, false);
            SetSlot(1, null, SlotState.Current, false);
            idx = 0;
            timeLeft = settings.wordTime;
            RenderTop();
            RenderProgress();
            RenderTimer();

            view.startScreen.SetActive(true);
            if (view.levelScreen) view.levelScreen.SetActive(false);
            if (view.overScreen) view.overScreen.SetActive(false);
        }

        WordEntry FindWord(string w)
        {
            foreach (var e in settings.words)
                if (e != null && e.word != null && e.word.Trim().ToUpperInvariant() == w) return e;
            return settings.words.Count > 0 ? settings.words[0] : null;
        }

        void StartLevel()
        {
            StopAllCoroutines();
            words = WordData.PickWords(settings.words, level, settings.wordsPerLevel);
            if (words.Count == 0)
            {
                Debug.LogError("Word Bubble Pop: add some words in GameSettings.");
                return;
            }
            idx = 0;
            levelStars = 0;
            running = true;
            RenderTop();
            RenderProgress();
            NextWord();
        }

        void NextWord()
        {
            current = words[idx];
            word = current.word.Trim().ToUpperInvariant();
            pos = 0;
            wrong = 0;
            miss = 0;
            SetPicture(view.picture, current);
            BuildSlots(word);
            SetSlot(0, null, SlotState.Current, false);
            ClearBubbles(false);
            spawnTimer = SpawnInterval();
            timeLeft = settings.wordTime;
            lastSec = Mathf.CeilToInt(settings.wordTime);
            RenderTimer();
            locked = false;
        }

        char Need => word[pos];

        void Update()
        {
            UpdateLayout();
            if (!running) return;

            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            float w = view.bubbleField.rect.width;
            float h = view.bubbleField.rect.height;

            if (!locked)
            {
                timeLeft -= dt;
                int sec = Mathf.CeilToInt(timeLeft);
                if (sec != lastSec)
                {
                    lastSec = sec;
                    if (sec <= settings.timerWarningSeconds && sec > 0) Play(settings.tickSound);
                }
                RenderTimer();
                if (timeLeft <= 0f)
                {
                    GameOver();
                    return;
                }

                spawnTimer += dt;
                if (spawnTimer >= SpawnInterval() && bubbles.Count < MaxBubbles())
                {
                    spawnTimer = 0;
                    Spawn(w, h);
                }
            }

            for (int i = bubbles.Count - 1; i >= 0; i--)
            {
                var b = bubbles[i];
                if (b == null) { bubbles.RemoveAt(i); continue; }
                b.Tick(dt, w);
                if (b.Y > h + 10f)
                {
                    bubbles.RemoveAt(i);
                    Destroy(b.gameObject);
                }
            }

            // Gentle pulse on the box waiting for the next letter
            if (!locked && pos < slotBoxes.Count)
            {
                var o = slotBoxes[pos].GetComponent<Outline>();
                if (o != null)
                {
                    var c = settings.slotCurrentBorder;
                    c.a = 0.6f + 0.4f * Mathf.Sin(Time.time * 6f);
                    o.effectColor = c;
                }
            }
        }

        // ------------------------------------------------------------ difficulty

        float Speed(float fieldHeight) =>
            (settings.baseSpeed + settings.speedPerLevel * Mathf.Min(level, 8)) * Mathf.Max(0.75f, fieldHeight / 480f);

        float SpawnInterval() =>
            Mathf.Max(settings.spawnIntervalMin, settings.spawnInterval - level * settings.spawnFasterPerLevel);

        int MaxBubbles() => Mathf.Min(settings.maxBubblesLimit, settings.maxBubblesStart + level - 1);

        // ------------------------------------------------------------ bubbles

        void Spawn(float w, float h)
        {
            float size = Mathf.Clamp(w / 5.2f, settings.bubbleSizeMin, settings.bubbleSizeMax);
            float fx = Random.value;
            for (int t = 0; t < 6; t++)
            {
                bool crowded = false;
                foreach (var b in bubbles)
                {
                    if (b != null && !b.Popped && b.Y < size * 1.6f && Mathf.Abs(b.FX - fx) < 0.22f)
                    {
                        crowded = true;
                        break;
                    }
                }
                if (!crowded) break;
                fx = Random.value;
            }

            char letter = ChooseLetter(h);
            var colors = settings.bubbleColors;
            var tint = colors != null && colors.Length > 0 ? colors[Random.Range(0, colors.Length)] : Color.white;

            var bubble = Instantiate(bubblePrefab, view.bubbleField);
            bubble.name = "Bubble " + letter;
            bubble.Init(this, letter, size, fx, Speed(h) * Random.Range(0.85f, 1.15f), tint, w);
            if (miss >= settings.hintAfterMistakes && letter == Need) bubble.SetHint(true);
            bubbles.Add(bubble);
        }

        /// <summary>
        /// Makes sure the next letter the child needs is always on screen,
        /// mixed with other letters from the word and a few random ones.
        /// </summary>
        char ChooseLetter(float h)
        {
            char need = Need;
            bool needVisible = false;
            foreach (var b in bubbles)
            {
                if (b != null && !b.Popped && b.Letter == need && b.Y < h * 0.7f)
                {
                    needVisible = true;
                    break;
                }
            }

            float r = Random.value;
            if (!needVisible || r < 0.35f) return need;
            if (r < 0.65f) return word[Random.Range(0, word.Length)];
            return Alphabet[Random.Range(0, Alphabet.Length)];
        }

        void ClearBubbles(bool pop)
        {
            foreach (var b in bubbles)
            {
                if (b == null) continue;
                if (pop) b.Pop(view.splashSprite, view.splashColor);
                else Destroy(b.gameObject);
            }
            bubbles.Clear();
        }

        public void OnBubbleTapped(Bubble b)
        {
            if (locked || !running || b.Popped) return;

            if (b.Letter == Need)
            {
                Play(settings.popSound);
                bubbles.Remove(b);
                b.Pop(view.splashSprite, view.splashColor);
                SetSlot(pos, b.Letter, SlotState.Filled, true);
                pos++;
                miss = 0;
                foreach (var x in bubbles) if (x != null) x.SetHint(false);

                if (pos < word.Length)
                {
                    SetSlot(pos, null, SlotState.Current, false);
                    Say(b.Letter.ToString(), 1f);
                }
                else
                {
                    WordDone();
                }
            }
            else
            {
                Play(settings.wrongSound);
                wrong++;
                miss++;
                b.Shake();
                if (miss >= settings.hintAfterMistakes)
                    foreach (var x in bubbles)
                        if (x != null && x.Letter == Need) x.SetHint(true);
            }
        }

        // ------------------------------------------------------------ results

        void SayWord()
        {
            Say(string.Join(", ", word.ToCharArray()) + ". " + word.ToLowerInvariant());
        }

        void WordDone()
        {
            locked = true;
            int earned = wrong == 0 ? 3 : wrong <= 2 ? 2 : 1;
            stars += earned;
            levelStars += earned;
            RenderTop();
            StartCoroutine(ShowEarned(earned));
            Play(settings.winSound);
            ClearBubbles(true);
            SayWord();
            StartCoroutine(AfterWord());
        }

        IEnumerator AfterWord()
        {
            yield return new WaitForSeconds(2.8f);
            idx++;
            RenderProgress();
            if (idx >= words.Count) LevelDone();
            else NextWord();
        }

        void LevelDone()
        {
            running = false;
            if (view.levelTitle) view.levelTitle.text = "Well done! Level " + level + " complete";
            if (view.levelStarsText) view.levelStarsText.text = levelStars.ToString();

            if (view.learnedGrid != null && view.learnedTemplate != null)
            {
                for (int i = view.learnedGrid.childCount - 1; i >= 0; i--)
                {
                    var child = view.learnedGrid.GetChild(i).gameObject;
                    if (child != view.learnedTemplate) Destroy(child);
                }
                foreach (var entry in words)
                {
                    var cell = Instantiate(view.learnedTemplate, view.learnedGrid);
                    cell.SetActive(true);
                    var pic = cell.transform.Find("Picture");
                    if (pic != null) SetPicture(pic.GetComponent<Image>(), entry);
                    var label = cell.GetComponentInChildren<Text>(true);
                    if (label != null) label.text = entry.word.Trim().ToUpperInvariant();
                }
            }

            view.levelScreen.SetActive(true);
            StartCoroutine(Confetti(view.levelScreen.transform));
            level++;
            Save();
        }

        void GameOver()
        {
            locked = true;
            running = false;
            timeLeft = 0;
            RenderTimer();
            Play(settings.gameOverSound);
            ClearBubbles(true);

            for (int i = 0; i < word.Length; i++)
                SetSlot(i, word[i], i < pos ? SlotState.Filled : SlotState.Empty, false);

            SetPicture(view.answerPicture, current);
            if (view.answerWord) view.answerWord.text = word;
            if (view.overInfo)
                view.overInfo.text = idx == 0
                    ? "Try level " + level + " again. You can do it!"
                    : "You finished " + idx + (idx == 1 ? " word" : " words") + " in level " + level + ". Try again!";
            StartCoroutine(ShowGameOverLater());
        }

        IEnumerator ShowGameOverLater()
        {
            yield return new WaitForSeconds(0.6f);
            view.overScreen.SetActive(true);
            SayWord();
        }

        void Save()
        {
            PlayerPrefs.SetInt(PrefLevel, level);
            PlayerPrefs.SetInt(PrefStars, stars);
            PlayerPrefs.Save();
        }

        // ------------------------------------------------------------ sound

        void ToggleSound()
        {
            sound = !sound;
            PlayerPrefs.SetInt(PrefSound, sound ? 1 : 0);
            if (!sound) Speech.Stop();
            ApplySound();
            RenderTop();
        }

        void ApplySound()
        {
            Speech.Enabled = sound && settings.speakWords;
            if (musicSource == null) return;
            musicSource.loop = true;
            musicSource.volume = settings.musicVolume;
            if (sound && settings.music != null)
            {
                if (musicSource.clip != settings.music) musicSource.clip = settings.music;
                if (!musicSource.isPlaying) musicSource.Play();
            }
            else
            {
                musicSource.Stop();
            }
        }

        void Play(AudioClip clip)
        {
            if (sound && clip != null && soundSource != null) soundSource.PlayOneShot(clip, settings.soundVolume);
        }

        void Say(string text, float speedFactor = 1f)
        {
            if (sound && settings.speakWords) Speech.Say(text, true, settings.voiceSpeed * speedFactor);
        }

        // =====================================================================
        // Rendering helpers
        // =====================================================================

        static void SetPicture(Image target, WordEntry entry)
        {
            if (target == null) return;
            target.sprite = entry != null ? entry.picture : null;
            target.enabled = target.sprite != null;
            target.preserveAspect = true;
        }

        void RenderTop()
        {
            if (view.levelText)
                view.levelText.text = "Level " + level + "  <size=34><color=#4F6D73>" + WordData.LevelInfo(level) + "</color></size>";
            if (view.starText) view.starText.text = stars.ToString();
            if (view.soundText) view.soundText.text = sound ? "Sound On" : "Sound Off";
        }

        void RenderProgress()
        {
            if (view.progressDots == null) return;
            int count = Mathf.Max(1, settings.wordsPerLevel);
            for (int i = 0; i < view.progressDots.Length; i++)
            {
                var d = view.progressDots[i];
                if (d == null) continue;
                d.gameObject.SetActive(i < count);
                d.color = i < idx ? view.dotDone : i == idx ? view.dotCurrent : view.dotTodo;
            }
        }

        void RenderTimer()
        {
            float t = Mathf.Max(0f, timeLeft);
            float total = Mathf.Max(0.01f, settings.wordTime);
            bool low = t <= settings.timerWarningSeconds;
            if (view.timerFill)
            {
                var rt = view.timerFill.rectTransform;
                rt.anchorMin = new Vector2(0, rt.anchorMin.y);
                rt.anchorMax = new Vector2(t / total, rt.anchorMax.y);
                view.timerFill.color = low ? settings.timerLowColor : settings.timerColor;
            }
            if (view.timerText)
            {
                view.timerText.text = Mathf.CeilToInt(t) + "s";
                view.timerText.color = low ? settings.timerLowColor : settings.timerColor;
            }
        }

        enum SlotState { Empty, Current, Filled }

        void BuildSlots(string w)
        {
            foreach (var box in slotBoxes) if (box != null) Destroy(box.gameObject);
            slotBoxes.Clear();
            slotLetters.Clear();
            if (view.slotTemplate == null || view.slotsRow == null) return;

            int n = w.Length;
            var templateSize = view.slotTemplate.rectTransform.sizeDelta;
            float rowWidth = view.slotsRow.rect.width > 0 ? view.slotsRow.rect.width : 900f;
            float gap = view.slotGap;
            float slotW = Mathf.Min(templateSize.x, (rowWidth - (n - 1) * gap) / n);
            float total = n * slotW + (n - 1) * gap;

            for (int i = 0; i < n; i++)
            {
                var box = Instantiate(view.slotTemplate, view.slotsRow);
                box.name = "Slot " + (i + 1);
                box.gameObject.SetActive(true);
                var rt = box.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(slotW, templateSize.y);
                rt.anchoredPosition = new Vector2(-total / 2f + slotW / 2f + i * (slotW + gap), 0);
                slotBoxes.Add(box);
                slotLetters.Add(box.GetComponentInChildren<Text>(true));
                SetSlot(i, null, SlotState.Empty, false);
            }
        }

        void SetSlot(int i, char? letter, SlotState state, bool animate)
        {
            if (i < 0 || i >= slotBoxes.Count) return;
            var box = slotBoxes[i];
            var o = box.GetComponent<Outline>();
            if (slotLetters[i] != null) slotLetters[i].text = letter.HasValue ? letter.Value.ToString() : "";

            Color fill, border;
            switch (state)
            {
                case SlotState.Filled:
                    fill = settings.slotFilledColor;
                    border = settings.slotFilledBorder;
                    break;
                case SlotState.Current:
                    fill = settings.slotEmptyColor;
                    border = settings.slotCurrentBorder;
                    break;
                default:
                    fill = settings.slotEmptyColor;
                    border = settings.slotEmptyBorder;
                    break;
            }
            box.color = fill;
            if (o != null) o.effectColor = border;
            if (animate) StartCoroutine(PopScale(box.rectTransform));
        }

        static IEnumerator PopScale(RectTransform rt)
        {
            float t = 0;
            while (t < 0.3f && rt != null)
            {
                t += Time.deltaTime;
                float k = t / 0.3f;
                float s = k < 0.7f ? Mathf.Lerp(0.6f, 1.15f, k / 0.7f) : Mathf.Lerp(1.15f, 1f, (k - 0.7f) / 0.3f);
                rt.localScale = Vector3.one * s;
                yield return null;
            }
            if (rt != null) rt.localScale = Vector3.one;
        }

        IEnumerator ShowEarned(int earned)
        {
            if (view.earned == null) yield break;
            if (view.earnedText) view.earnedText.text = "+" + earned;
            var rt = (RectTransform)view.earned.transform;
            float t = 0;
            while (t < 1.4f)
            {
                t += Time.deltaTime;
                float k = t / 1.4f;
                view.earned.alpha = k < 0.2f ? k / 0.2f : k > 0.8f ? (1f - k) / 0.2f : 1f;
                rt.anchoredPosition = earnedBasePos + new Vector2(0, Mathf.Lerp(-10f, 24f, k));
                yield return null;
            }
            view.earned.alpha = 0;
            rt.anchoredPosition = earnedBasePos;
        }

        IEnumerator Confetti(Transform parent)
        {
            var area = (RectTransform)parent;
            float w = area.rect.width;
            var colors = view.confettiColors != null && view.confettiColors.Length > 0
                ? view.confettiColors
                : new[] { Color.yellow };
            var pieces = new List<(RectTransform rt, float speed, float spin)>();
            for (int i = 0; i < 40; i++)
            {
                var img = UIKit.Img("Confetti", parent, null, colors[i % colors.Length]);
                UIKit.Place(img.rectTransform, new Vector2(0, 1), new Vector2(0.5f, 0.5f),
                    new Vector2(Random.value * w, Random.Range(0f, 300f)), new Vector2(20, 30));
                pieces.Add((img.rectTransform, Random.Range(500f, 950f), Random.Range(-400f, 400f)));
            }

            float t = 0;
            while (t < 4f)
            {
                t += Time.deltaTime;
                foreach (var p in pieces)
                {
                    if (p.rt == null) continue;
                    p.rt.anchoredPosition += new Vector2(0, -p.speed * Time.deltaTime);
                    p.rt.Rotate(0, 0, p.spin * Time.deltaTime);
                }
                yield return null;
            }
            foreach (var p in pieces) if (p.rt != null) Destroy(p.rt.gameObject);
        }
    }
}
