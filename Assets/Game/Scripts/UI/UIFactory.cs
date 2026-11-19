using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StreamStar
{
    /// <summary>Small helpers for building uGUI hierarchies from code.</summary>
    public static class UIFactory
    {
        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static void Stretch(RectTransform rt, float left = 0, float bottom = 0, float right = 0, float top = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>Fixed-size rect anchored to one point of its parent.</summary>
        public static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        public static Image Image(string name, Transform parent, Sprite sprite, Color color, bool raycast = false)
        {
            var rt = Rect(name, parent);
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycast;
            if (sprite != null && sprite.border.sqrMagnitude > 0f)
            {
                image.type = UnityEngine.UI.Image.Type.Sliced;
                // art is authored at 96px with ~30px corners; keep corners that size on screen
                image.pixelsPerUnitMultiplier = 1f;
            }
            return image;
        }

        /// <summary>Icon with a coloured-circle fallback when the sprite is missing.</summary>
        public static Image Icon(string name, Transform parent, string spriteName, Color fallback)
        {
            var sprite = ArtLibrary.Get(spriteName);
            var image = Image(name, parent, sprite != null ? sprite : ArtLibrary.Circle, sprite != null ? Color.white : fallback);
            image.preserveAspect = true;
            return image;
        }

        public static TextMeshProUGUI Text(string name, Transform parent, string content, float size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.Left, bool bold = true)
        {
            var rt = Rect(name, parent);
            var text = rt.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = align;
            text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.richText = true;
            return text;
        }

        public static Button Button(string name, Transform parent, Color color, Action onClick, out Image background)
        {
            background = Image(name, parent, ArtLibrary.Button, color, true);
            var button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.transition = Selectable.Transition.None; // Feel drives the feedback instead
            if (onClick != null) button.onClick.AddListener(() => onClick());
            return button;
        }

        /// <summary>Soft purple drop shadow under a graphic (cards, buttons) so the HUD lifts off the room art.</summary>
        public static void Shadow(Graphic graphic, float offsetY = -6f, float alpha = 0.3f)
        {
            var shadow = graphic.gameObject.AddComponent<UnityEngine.UI.Shadow>();
            shadow.effectColor = new Color(0.14f, 0.05f, 0.32f, alpha);
            shadow.effectDistance = new Vector2(0f, offsetY);
            shadow.useGraphicAlpha = false;
        }

        /// <summary>Glossy highlight strip (pill) laid inside a parent; anchors are normalised to the parent.</summary>
        public static Image Gloss(Transform parent, Vector2 anchorMin, Vector2 anchorMax, float alpha = 0.35f)
        {
            var gloss = Image("Gloss", parent, ArtLibrary.Pill, new Color(1f, 1f, 1f, alpha));
            gloss.rectTransform.anchorMin = anchorMin;
            gloss.rectTransform.anchorMax = anchorMax;
            gloss.rectTransform.offsetMin = Vector2.zero;
            gloss.rectTransform.offsetMax = Vector2.zero;
            return gloss;
        }

        public static string Hex(Color color) => ColorUtility.ToHtmlStringRGB(color);
    }
}
