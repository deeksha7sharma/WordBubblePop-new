using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WordBubblePop
{
    /// <summary>
    /// One letter bubble. The look comes from the prefab Assets/Prefabs/Bubble:
    /// open the prefab to change its shine, ring, shadow or letter style.
    /// </summary>
    public class Bubble : MonoBehaviour, IPointerDownHandler
    {
        [Header("Parts of the bubble (inside the prefab)")]
        public RectTransform skin;
        public Image body;
        public Image glow;
        public Text letterText;

        [Header("Look")]
        [Tooltip("How much white is mixed into the bubble colour (0 = strong colour, 1 = white)")]
        [Range(0, 1)] public float lightness = 0.45f;
        [Tooltip("Letter size compared to the bubble size")]
        [Range(0.2f, 0.8f)] public float letterScale = 0.5f;
        public Color glowColor = new Color32(0xFF, 0xC9, 0x3C, 0xFF);

        [Header("Movement")]
        [Tooltip("How far the bubble sways left and right")]
        public float sway = 12f;
        [Tooltip("Pop animation length in seconds")]
        public float popTime = 0.25f;

        public char Letter { get; private set; }
        public float Size { get; private set; }
        public float Y { get; private set; }
        public float FX { get; private set; }
        public bool Popped { get; private set; }

        GameManager game;
        RectTransform rt;
        CanvasGroup group;
        float speed, amp, phase, wobble;
        bool hint;

        public void Init(GameManager owner, char letter, float size, float fx, float riseSpeed, Color tint, float fieldWidth)
        {
            game = owner;
            rt = (RectTransform)transform;
            group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();

            Letter = letter;
            Size = size;
            FX = fx;
            Y = -size - 4f;
            speed = riseSpeed;
            amp = Random.Range(0.5f, 1.3f) * sway * size / 160f;
            phase = Random.Range(0f, 6f);
            wobble = Random.Range(1.2f, 2.2f);

            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            rt.sizeDelta = new Vector2(size, size);

            if (body != null) body.color = Color.Lerp(tint, Color.white, lightness);
            if (letterText != null)
            {
                letterText.text = letter.ToString();
                letterText.fontSize = Mathf.RoundToInt(size * letterScale);
            }
            SetHint(false);
            UpdatePosition(fieldWidth);
        }

        public void Tick(float dt, float fieldWidth)
        {
            if (Popped) return;
            Y += speed * dt;
            phase += dt * wobble;
            UpdatePosition(fieldWidth);

            if (hint && glow != null)
            {
                var c = glowColor;
                c.a = 0.55f + 0.45f * Mathf.Sin(Time.time * 7f);
                glow.color = c;
            }
        }

        void UpdatePosition(float fieldWidth)
        {
            float x = Mathf.Clamp(FX * (fieldWidth - Size) + Mathf.Sin(phase) * amp, 0, Mathf.Max(0, fieldWidth - Size));
            rt.anchoredPosition = new Vector2(x, Y);
        }

        public void SetHint(bool on)
        {
            hint = on;
            if (glow != null) glow.gameObject.SetActive(on);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!Popped && game != null) game.OnBubbleTapped(this);
        }

        // ---------------------------------------------------------------- effects

        public void Pop(Sprite splashSprite, Color splashColor)
        {
            if (Popped) return;
            Popped = true;
            group.blocksRaycasts = false;
            SetHint(false);
            if (splashSprite != null) SpawnSplash(splashSprite, splashColor);
            StartCoroutine(PopRoutine());
        }

        IEnumerator PopRoutine()
        {
            float t = 0;
            while (t < popTime)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / popTime);
                if (skin != null) skin.localScale = Vector3.one * (1f + 0.45f * k);
                group.alpha = 1f - k;
                yield return null;
            }
            Destroy(gameObject);
        }

        void SpawnSplash(Sprite sprite, Color color)
        {
            var parent = rt.parent;
            var center = rt.anchoredPosition + new Vector2(Size, Size) * 0.5f;
            for (int i = 0; i < 8; i++)
            {
                var dot = UIKit.Img("Splash", parent, sprite, color);
                float s = Size * 0.13f;
                UIKit.Place(dot.rectTransform, Vector2.zero, new Vector2(0.5f, 0.5f), center, new Vector2(s, s));
                float a = i / 8f * Mathf.PI * 2f;
                game.StartCoroutine(Fly(dot, center, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Size * 0.6f, color));
            }
        }

        static IEnumerator Fly(Image dot, Vector2 from, Vector2 delta, Color color)
        {
            float t = 0;
            while (t < 0.45f && dot != null)
            {
                t += Time.deltaTime;
                float k = t / 0.45f;
                dot.rectTransform.anchoredPosition = from + delta * (1f - (1f - k) * (1f - k));
                dot.rectTransform.localScale = Vector3.one * (1f - 0.7f * k);
                color.a = 1f - k;
                dot.color = color;
                yield return null;
            }
            if (dot != null) Destroy(dot.gameObject);
        }

        public void Shake()
        {
            StopCoroutine(nameof(ShakeRoutine));
            StartCoroutine(nameof(ShakeRoutine));
        }

        IEnumerator ShakeRoutine()
        {
            if (skin == null) yield break;
            float t = 0;
            while (t < 0.4f)
            {
                t += Time.deltaTime;
                float k = t / 0.4f;
                skin.anchoredPosition = new Vector2(Mathf.Sin(k * Mathf.PI * 6f) * Size * 0.09f * (1f - k), 0);
                yield return null;
            }
            skin.anchoredPosition = Vector2.zero;
        }
    }
}
