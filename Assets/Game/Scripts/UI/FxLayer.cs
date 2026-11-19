using System.Collections.Generic;
using MoreMountains.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StreamStar
{
    /// <summary>
    /// Pooled UI effects drawn above everything: floating numbers, coins flying to the HUD and sparkle bursts.
    /// Motion curves come from Feel's MMTween. Runs on unscaled time so freeze frames do not stall it.
    /// </summary>
    public class FxLayer : MonoBehaviour
    {
        class FloatText
        {
            public TextMeshProUGUI Text;
            public float Time, Duration, Rise;
            public Vector2 Start;
        }

        class Flyer
        {
            public Image Image;
            public float Time, Duration, Delay;
            public Vector2 From, Control, To;
            public bool PunchOnArrive;
        }

        class Particle
        {
            public Image Image;
            public float Time, Duration, Spin;
            public Vector2 Position, Velocity;
        }

        RectTransform _rt;
        readonly List<FloatText> _texts = new List<FloatText>();
        readonly Stack<FloatText> _textPool = new Stack<FloatText>();
        readonly List<Flyer> _flyers = new List<Flyer>();
        readonly Stack<Flyer> _flyerPool = new Stack<Flyer>();
        readonly List<Particle> _particles = new List<Particle>();
        readonly Stack<Particle> _particlePool = new Stack<Particle>();

        public static FxLayer Create(Transform parent)
        {
            var rt = UIFactory.Rect("FxLayer", parent);
            UIFactory.Stretch(rt);
            var layer = rt.gameObject.AddComponent<FxLayer>();
            layer._rt = rt;
            return layer;
        }

        public Vector2 ScreenToLocal(Vector2 screenPosition)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_rt, screenPosition, null, out var local);
            return local;
        }

        public Vector2 WorldToLocal(Vector3 worldPosition) => _rt.InverseTransformPoint(worldPosition);

        // ------------------------------------------------------------ spawners

        public void SpawnText(Vector2 local, string content, Color color, float size = 54f, float rise = 190f, float duration = 0.9f)
        {
            FloatText item;
            if (_textPool.Count > 0) item = _textPool.Pop();
            else
            {
                item = new FloatText { Text = UIFactory.Text("FloatText", _rt, "", size, color, TextAlignmentOptions.Center) };
                var rt = item.Text.rectTransform;
                rt.sizeDelta = new Vector2(600, 100);
                item.Text.outlineWidth = 0.2f;
                item.Text.outlineColor = new Color32(27, 20, 54, 255);
            }
            item.Text.gameObject.SetActive(true);
            item.Text.transform.SetAsLastSibling();
            item.Text.text = content;
            item.Text.fontSize = size;
            item.Text.color = color;
            item.Start = local;
            item.Rise = rise;
            item.Duration = duration;
            item.Time = 0f;
            Apply(item);
            _texts.Add(item);
        }

        /// <summary>Icons arc from a point to a HUD element, then punch it.</summary>
        public void SpawnFlyers(Vector2 from, Transform target, int count, string spriteName, Color fallback, bool punch = true)
        {
            if (target == null) return;
            Vector2 to = WorldToLocal(target.position);
            var sprite = ArtLibrary.Get(spriteName);
            for (int i = 0; i < count; i++)
            {
                Flyer item;
                if (_flyerPool.Count > 0) item = _flyerPool.Pop();
                else
                {
                    item = new Flyer { Image = UIFactory.Image("Flyer", _rt, null, Color.white) };
                    item.Image.rectTransform.sizeDelta = new Vector2(64, 64);
                }
                item.Image.sprite = sprite != null ? sprite : ArtLibrary.Circle;
                item.Image.color = sprite != null ? Color.white : fallback;
                item.Image.gameObject.SetActive(true);
                item.Image.transform.SetAsLastSibling();
                Vector2 scatter = Random.insideUnitCircle * 140f;
                item.From = from;
                item.Control = from + scatter + new Vector2(0f, 120f);
                item.To = to;
                item.Delay = i * 0.035f;
                item.Duration = Random.Range(0.55f, 0.8f);
                item.Time = 0f;
                item.PunchOnArrive = punch;
                item.Image.rectTransform.anchoredPosition = from;
                item.Image.rectTransform.localScale = Vector3.zero;
                _flyers.Add(item);
            }
        }

        public void Burst(Vector2 local, int count, Color[] colors, float speed = 900f)
        {
            for (int i = 0; i < count; i++)
            {
                Particle item;
                if (_particlePool.Count > 0) item = _particlePool.Pop();
                else item = new Particle { Image = UIFactory.Image("Particle", _rt, ArtLibrary.Sparkle, Color.white) };
                float size = Random.Range(26f, 60f);
                item.Image.rectTransform.sizeDelta = new Vector2(size, size);
                item.Image.color = colors[Random.Range(0, colors.Length)];
                item.Image.gameObject.SetActive(true);
                item.Image.transform.SetAsLastSibling();
                float angle = Random.Range(0f, Mathf.PI * 2f);
                item.Velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * 0.8f + 0.5f) * Random.Range(0.35f, 1f) * speed;
                item.Position = local;
                item.Spin = Random.Range(-360f, 360f);
                item.Duration = Random.Range(0.6f, 1.1f);
                item.Time = 0f;
                _particles.Add(item);
            }
        }

        // ------------------------------------------------------------ animation

        void Update()
        {
            float dt = Time.unscaledDeltaTime;

            for (int i = _texts.Count - 1; i >= 0; i--)
            {
                var item = _texts[i];
                item.Time += dt;
                if (item.Time >= item.Duration)
                {
                    item.Text.gameObject.SetActive(false);
                    _textPool.Push(item);
                    _texts.RemoveAt(i);
                    continue;
                }
                Apply(item);
            }

            for (int i = _flyers.Count - 1; i >= 0; i--)
            {
                var item = _flyers[i];
                item.Time += dt;
                float t = item.Time - item.Delay;
                if (t < 0f) continue;
                if (t >= item.Duration)
                {
                    item.Image.gameObject.SetActive(false);
                    _flyerPool.Push(item);
                    _flyers.RemoveAt(i);
                    if (item.PunchOnArrive && FeelKit.I != null) FeelKit.I.CoinPunch();
                    continue;
                }
                float eased = MMTween.Tween(t, 0f, item.Duration, 0f, 1f, MMTween.MMTweenCurve.EaseInOutCubic);
                Vector2 a = Vector2.LerpUnclamped(item.From, item.Control, eased);
                Vector2 b = Vector2.LerpUnclamped(item.Control, item.To, eased);
                item.Image.rectTransform.anchoredPosition = Vector2.LerpUnclamped(a, b, eased);
                float pop = MMTween.Tween(Mathf.Min(t, 0.2f), 0f, 0.2f, 0f, 1f, MMTween.MMTweenCurve.EaseOutOverhead);
                float shrink = Mathf.Lerp(1f, 0.6f, eased);
                item.Image.rectTransform.localScale = Vector3.one * (pop * shrink);
            }

            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                var item = _particles[i];
                item.Time += dt;
                if (item.Time >= item.Duration)
                {
                    item.Image.gameObject.SetActive(false);
                    _particlePool.Push(item);
                    _particles.RemoveAt(i);
                    continue;
                }
                item.Velocity += new Vector2(0f, -1800f) * dt;
                item.Velocity *= 1f - 1.5f * dt;
                item.Position += item.Velocity * dt;
                float life = item.Time / item.Duration;
                var rt = item.Image.rectTransform;
                rt.anchoredPosition = item.Position;
                rt.localRotation = Quaternion.Euler(0f, 0f, item.Spin * item.Time);
                rt.localScale = Vector3.one * (1f - life * life);
            }
        }

        static void Apply(FloatText item)
        {
            float rise = MMTween.Tween(item.Time, 0f, item.Duration, 0f, item.Rise, MMTween.MMTweenCurve.EaseOutCubic);
            float pop = MMTween.Tween(Mathf.Min(item.Time, 0.22f), 0f, 0.22f, 0.3f, 1f, MMTween.MMTweenCurve.EaseOutOverhead);
            float fadeStart = item.Duration * 0.55f;
            float alpha = item.Time < fadeStart
                ? 1f
                : MMTween.Tween(item.Time, fadeStart, item.Duration, 1f, 0f, MMTween.MMTweenCurve.EaseInQuadratic);
            var rt = item.Text.rectTransform;
            rt.anchoredPosition = item.Start + new Vector2(0f, rise);
            rt.localScale = Vector3.one * pop;
            item.Text.alpha = alpha;
        }
    }
}
