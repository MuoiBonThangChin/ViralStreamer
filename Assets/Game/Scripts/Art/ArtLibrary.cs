using System.Collections.Generic;
using UnityEngine;

namespace StreamStar
{
    /// <summary>
    /// Loads sprites and audio from Resources by name. Missing UI shapes are generated procedurally so the
    /// game stays playable before the art pass lands.
    /// </summary>
    public static class ArtLibrary
    {
        static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
        static readonly Dictionary<string, AudioClip> Clips = new Dictionary<string, AudioClip>();

        public static Sprite Panel => Get("ui_panel") ?? Procedural("ui_panel");
        public static Sprite Button => Get("ui_button") ?? Procedural("ui_panel");
        public static Sprite Pill => Get("ui_pill") ?? Procedural("ui_pill");
        // note: ui_panel / ui_button / ui_pill / ui_circle and the icon_* / tab_* sprites are Layer Lab GUI Pro art
        // (originals backed up in tools/ui-original-backup)
        public static Sprite Circle => Get("ui_circle") ?? Procedural("ui_circle");
        public static Sprite Glow => Get("ui_glow") ?? Procedural("ui_glow");
        public static Sprite Sparkle => Get("ui_sparkle") ?? Procedural("ui_glow");

        /// <summary>Layer Lab HUD sprite from Resources/Art/LayerLab, or the fallback when it is not imported.</summary>
        public static Sprite LayerLab(string name, Sprite fallback) => Get("LayerLab/" + name) ?? fallback;

        /// <summary>Returns null when the art does not exist.</summary>
        public static Sprite Get(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (Sprites.TryGetValue(name, out var cached)) return cached;

            Sprite sprite = Resources.Load<Sprite>("Art/" + name);
            if (sprite == null)
            {
                // imported as a plain texture (importer settings not applied yet)
                var texture = Resources.Load<Texture2D>("Art/" + name);
                if (texture != null)
                {
                    float b = IsSliced(name) ? Mathf.Round(texture.width / 3f) : 0f;
                    sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
                }
            }
            Sprites[name] = sprite;
            return sprite;
        }

        public static bool IsSliced(string name) => name == "ui_panel" || name == "ui_button" || name == "ui_pill";

        public static AudioClip Clip(string name)
        {
            if (Clips.TryGetValue(name, out var cached)) return cached;
            var clip = Resources.Load<AudioClip>("Audio/" + name);
            Clips[name] = clip;
            return clip;
        }

        // ------------------------------------------------------------ procedural fallbacks

        static Sprite Procedural(string kind)
        {
            string key = "__proc_" + kind;
            if (Sprites.TryGetValue(key, out var cached) && cached != null) return cached;

            const int size = 96;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color32[size * size];
            float radius = kind == "ui_panel" ? 28f : size / 2f;
            float half = size / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f, py = y + 0.5f;
                    float alpha;
                    if (kind == "ui_glow")
                    {
                        float d = Vector2.Distance(new Vector2(px, py), new Vector2(half, half)) / half;
                        alpha = Mathf.Clamp01(1f - d);
                        alpha *= alpha;
                    }
                    else
                    {
                        // signed distance to a rounded rectangle
                        float qx = Mathf.Abs(px - half) - (half - radius);
                        float qy = Mathf.Abs(py - half) - (half - radius);
                        float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
                        float dist = outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
                        alpha = Mathf.Clamp01(0.5f - dist);
                    }
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            float border = kind == "ui_panel" ? 32f : kind == "ui_pill" ? 47f : 0f;
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            sprite.hideFlags = HideFlags.HideAndDontSave;
            Sprites[key] = sprite;
            return sprite;
        }
    }
}
