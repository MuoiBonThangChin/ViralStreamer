using System;
using System.Collections.Generic;
using MoreMountains.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StreamStar
{
    /// <summary>Single modal dialog (offline earnings, rank up). Requests queue up if one is already showing.</summary>
    public class Popup : MonoBehaviour
    {
        struct Request
        {
            public string Title, Body, Button, Icon;
            public Color Accent;
            public Action OnClose;
        }

        CanvasGroup _group;
        RectTransform _card;
        Image _accent, _icon, _button;
        TextMeshProUGUI _title, _body, _buttonLabel;
        readonly Queue<Request> _queue = new Queue<Request>();
        Action _onClose;
        float _time;
        bool _open;

        public bool IsOpen => _open;

        public static Popup Create(Transform parent)
        {
            var root = UIFactory.Rect("Popup", parent);
            UIFactory.Stretch(root);
            var popup = root.gameObject.AddComponent<Popup>();
            popup._group = root.gameObject.AddComponent<CanvasGroup>();

            var dim = UIFactory.Image("Dim", root, null, new Color(0.05f, 0.03f, 0.12f, 0.78f), true);
            UIFactory.Stretch(dim.rectTransform);

            var card = UIFactory.Image("Card", root, ArtLibrary.Panel, Palette.Sheet, true);
            UIFactory.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860, 760));
            popup._card = card.rectTransform;

            popup._accent = UIFactory.Image("Accent", card.transform, ArtLibrary.Panel, Palette.Purple);
            UIFactory.Place(popup._accent.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -24), new Vector2(812, 150));

            popup._title = UIFactory.Text("Title", popup._accent.transform, "", 64f, Color.white, TextAlignmentOptions.Center);
            UIFactory.Stretch(popup._title.rectTransform);

            var glow = UIFactory.Image("Glow", card.transform, ArtLibrary.Glow, new Color(1f, 0.82f, 0.25f, 0.55f));
            UIFactory.Place(glow.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0, -310), new Vector2(380, 380));

            popup._icon = UIFactory.Image("Icon", card.transform, null, Color.white);
            popup._icon.preserveAspect = true;
            UIFactory.Place(popup._icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0, -310), new Vector2(210, 210));

            popup._body = UIFactory.Text("Body", card.transform, "", 44f, Palette.Ink, TextAlignmentOptions.Center);
            popup._body.textWrappingMode = TextWrappingModes.Normal;
            UIFactory.Place(popup._body.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 190), new Vector2(760, 170));

            UIFactory.Button("Ok", card.transform, Palette.Mint, popup.Close, out popup._button);
            UIFactory.Place(popup._button.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0, 100), new Vector2(460, 120));
            popup._buttonLabel = UIFactory.Text("Label", popup._button.transform, "OK", 50f, Palette.Navy, TextAlignmentOptions.Center);
            UIFactory.Stretch(popup._buttonLabel.rectTransform);

            root.gameObject.SetActive(false);
            return popup;
        }

        public void Show(string title, string body, string button, string icon, Color accent, Action onClose = null)
        {
            var request = new Request { Title = title, Body = body, Button = button, Icon = icon, Accent = accent, OnClose = onClose };
            if (_open) _queue.Enqueue(request);
            else Open(request);
        }

        void Open(Request request)
        {
            _open = true;
            _time = 0f;
            _onClose = request.OnClose;
            _title.text = request.Title;
            _body.text = request.Body;
            _buttonLabel.text = request.Button;
            _accent.color = request.Accent;
            var sprite = ArtLibrary.Get(request.Icon);
            _icon.sprite = sprite != null ? sprite : ArtLibrary.Circle;
            _icon.color = sprite != null ? Color.white : request.Accent;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            _group.alpha = 0f;
            _card.localScale = Vector3.one * 0.6f;
            if (FeelKit.I != null) FeelKit.I.Sfx("sfx_tab", 0.8f, 0.9f, true);
        }

        void Close()
        {
            if (!_open) return;
            if (FeelKit.I != null)
            {
                FeelKit.I.Sfx("sfx_coin", 0.8f);
                FeelKit.I.Vibrate(FeelKit.Haptic.Selection);
            }
            var callback = _onClose;
            _onClose = null;
            _open = false;
            callback?.Invoke();
            if (_queue.Count > 0) Open(_queue.Dequeue());
            else gameObject.SetActive(false);
        }

        void Update()
        {
            if (!_open) return;
            _time += Time.unscaledDeltaTime;
            _group.alpha = Mathf.Clamp01(_time / 0.15f);
            float scale = MMTween.Tween(Mathf.Min(_time, 0.7f), 0f, 0.7f, 0.6f, 1f, MMTween.MMTweenCurve.EaseOutElastic);
            _card.localScale = Vector3.one * scale;
            // gentle attention pulse on the button once the card has settled
            float pulse = _time > 0.8f ? 1f + 0.04f * Mathf.Sin((_time - 0.8f) * 5f) : 1f;
            _button.rectTransform.localScale = Vector3.one * pulse;
        }
    }
}
