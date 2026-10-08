using UnityEngine;
using UnityEngine.UI;

namespace WordBubblePop
{
    /// <summary>Small helpers for creating and placing UI objects.</summary>
    public static class UIKit
    {
        public static Font Regular => LoadFont("Fonts/Baloo2-500");
        public static Font Bold => LoadFont("Fonts/Baloo2-800");

        static Font LoadFont(string path)
        {
            var f = Resources.Load<Font>(path);
            if (f != null) return f;
            try { return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
            catch { return Resources.GetBuiltinResource<Font>("Arial.ttf"); }
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.layer = parent != null ? parent.gameObject.layer : 5;
            return (RectTransform)go.transform;
        }

        /// <param name="slicedCorner">For rounded boxes: bigger number = smaller corners.</param>
        public static Image Img(string name, Transform parent, Sprite sprite, Color color, bool raycast = false, float slicedCorner = 0f)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycast;
            if (slicedCorner > 0f)
            {
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = slicedCorner;
            }
            return img;
        }

        public static Text Label(string name, Transform parent, string text, int size, Color color,
            bool bold = false, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = bold ? Bold : Regular;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.supportRichText = true;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.lineSpacing = 0.9f;
            return t;
        }

        public static void AddShadow(GameObject go, Color color, float distance)
        {
            var s = go.AddComponent<Shadow>();
            s.effectColor = color;
            s.effectDistance = new Vector2(0, -distance);
        }

        public static LayoutElement Layout(Component c, float prefW = -1, float prefH = -1)
        {
            var le = c.gameObject.GetComponent<LayoutElement>();
            if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
            if (prefW >= 0) { le.preferredWidth = prefW; le.minWidth = prefW; }
            if (prefH >= 0) { le.preferredHeight = prefH; le.minHeight = prefH; }
            return le;
        }

        public static void Stretch(RectTransform rt, float left = 0, float bottom = 0, float right = 0, float top = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        public static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        /// <summary>Full width row anchored to the top of the parent.</summary>
        public static void TopRow(RectTransform rt, float y, float height, float left, float right)
        {
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.offsetMin = new Vector2(left, -y - height);
            rt.offsetMax = new Vector2(-right, -y);
        }
    }
}
