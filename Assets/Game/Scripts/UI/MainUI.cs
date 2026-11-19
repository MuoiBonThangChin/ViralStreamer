using System;
using MoreMountains.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StreamStar
{
    /// <summary>Forwards pointer-down on the stage to the game (multi-touch friendly).</summary>
    public class TapZone : MonoBehaviour, IPointerDownHandler
    {
        public Action<Vector2> OnTap;
        public void OnPointerDown(PointerEventData eventData) => OnTap?.Invoke(eventData.position);
    }

    /// <summary>Builds the whole screen and runs the game loop.</summary>
    public class MainUI : MonoBehaviour
    {
        const float NavHeight = 196f;
        const string BackgroundArt = "BackgroundRoom/old-room";
        const float BlinkDuration = 0.12f;

        static readonly Color[] Confetti = { Palette.Pink, Palette.Cyan, Palette.Yellow, Palette.Mint, Color.white };
        static readonly Color[] Hearts = { Palette.Pink, Palette.Red, Palette.Hex("FF9EC4"), Color.white };
        static readonly Color HudCard = new Color(1f, 1f, 1f, 0.93f);

        GameState _state;
        FxLayer _fx;
        Popup _popup;
        ChatFeed _chat;
        UpgradePanel _panel;

        RectTransform _room, _safe, _stage, _charRoot, _hypeFill, _gift, _coinIcon, _gemPill, _rankBadge, _hypeBar, _donate;
        Image _boostProgress, _boostGlow, _hypeLabelPill;
        RectTransform _hypeFlame;
        Image _charSprite, _charGlow, _rankBadgeImage, _rankFill, _boostButton, _hypeFillImage, _stageTint, _avatar, _donateImage;
        Image[] _decor;
        float[] _decorPop;
        TextMeshProUGUI _coinText, _incomeText, _gemText, _rankText, _followerText, _viewerText, _boostLabel, _hypeLabel, _donateText;

        double _shownCoins;
        float _hudTimer, _saveTimer, _donationTimer = 4f;
        float _giftTimer, _giftLife, _giftPhase;
        bool _giftActive;
        float _time, _outfitPop = 1f;
        AspectRatioFitter _charFit;
        Sprite[] _idleFrames = new Sprite[0], _danceFrames = new Sprite[0]; // idle: 0 = eyes open, 1 = blink; dance: one pose per beat
        float _blinkTimer = 2f;
        Rect _lastSafeArea;

        public static MainUI Create(Canvas canvas, GameState state)
        {
            var ui = canvas.gameObject.AddComponent<MainUI>();
            ui._state = state;
            ui.Build(canvas.transform);
            return ui;
        }

        // ================================================================ build

        void Build(Transform canvas)
        {
            // Background fills the whole screen, including behind notches.
            var bgSprite = ArtLibrary.Get(BackgroundArt) ?? ArtLibrary.Get("bg_room");
            var bg = UIFactory.Image("Background", canvas, bgSprite, bgSprite != null ? Color.white : Palette.Hex("3A2A6E"));
            UIFactory.Stretch(bg.rectTransform);
            if (bgSprite != null)
            {
                var fitter = bg.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = bgSprite.rect.width / bgSprite.rect.height;
            }

            // Room furniture lives in its own layer that is fitted exactly like the background image, so the
            // normalised positions in GameConfig.Decor line up with the painted room on any screen shape.
            _room = UIFactory.Rect("Room", canvas);
            UIFactory.Stretch(_room);
            if (bgSprite != null)
            {
                var roomFit = _room.gameObject.AddComponent<AspectRatioFitter>();
                roomFit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                roomFit.aspectRatio = bgSprite.rect.width / bgSprite.rect.height;
            }

            _stageTint = UIFactory.Image("HypeTint", canvas, null, new Color(Palette.Pink.r, Palette.Pink.g, Palette.Pink.b, 0f));
            UIFactory.Stretch(_stageTint.rectTransform);

            _safe = UIFactory.Rect("SafeArea", canvas);
            UIFactory.Stretch(_safe);

            // draw order: stage, floating HUD, upgrade sheet, navigation bar
            BuildStage();
            BuildHud();

            var sheet = UIFactory.Rect("UpgradeSheet", _safe);
            sheet.anchorMin = new Vector2(0f, 0f);
            sheet.anchorMax = new Vector2(1f, 0f);
            sheet.pivot = new Vector2(0.5f, 0f);
            sheet.sizeDelta = new Vector2(0f, UpgradePanel.SheetHeight);
            sheet.anchoredPosition = new Vector2(0f, NavHeight);

            var navBar = UIFactory.Image("NavBar", _safe, ArtLibrary.Panel, Palette.Sheet, true);
            navBar.rectTransform.anchorMin = new Vector2(0f, 0f);
            navBar.rectTransform.anchorMax = new Vector2(1f, 0f);
            navBar.rectTransform.pivot = new Vector2(0.5f, 0f);
            navBar.rectTransform.offsetMin = new Vector2(0f, -400f); // extends under the home indicator
            navBar.rectTransform.offsetMax = new Vector2(0f, NavHeight);
            var nav = UIFactory.Rect("Buttons", navBar.transform);
            nav.anchorMin = new Vector2(0f, 1f);
            nav.anchorMax = new Vector2(1f, 1f);
            nav.pivot = new Vector2(0.5f, 1f);
            nav.offsetMin = new Vector2(15f, -NavHeight + 22f);
            nav.offsetMax = new Vector2(-15f, -22f);

            _panel = UpgradePanel.Create(sheet, nav, _state, this);

            _fx = FxLayer.Create(canvas);
            FeelKit.CreateFlash(canvas);
            _popup = Popup.Create(canvas);

            FeelKit.I.Bind(_charSprite.transform, _coinIcon, _hypeBar, _rankBadge);

            _state.OnRankUp += HandleRankUp;
            _state.OnHypeStart += HandleHypeStart;
            _state.OnHypeEnd += HandleHypeEnd;
            _state.OnGemsGained += HandleGems;

            _shownCoins = _state.Data.Coins;
            _giftTimer = UnityEngine.Random.Range(12f, 20f); // first gift comes early to teach the mechanic
            ApplyOutfit();
            ApplyRank();
            ApplySafeArea();
            RefreshHud();
        }

        void BuildStage()
        {
            // the room owns the whole screen; HUD cards float on top of it
            _stage = UIFactory.Rect("Stage", _safe);
            UIFactory.Stretch(_stage);

            // furniture sits in the room layer, behind the streamer; Z gives the back-to-front order
            _decor = new Image[GameConfig.Decor.Length];
            _decorPop = new float[GameConfig.Decor.Length];
            var byDepth = new int[_decor.Length];
            for (int i = 0; i < byDepth.Length; i++) byDepth[i] = i;
            Array.Sort(byDepth, (p, q) => GameConfig.Decor[p].Z.CompareTo(GameConfig.Decor[q].Z));
            foreach (int i in byDepth)
            {
                var def = GameConfig.Decor[i];
                var image = UIFactory.Icon("Decor_" + def.Id, _room, def.Icon, Palette.Mint);
                image.preserveAspect = false; // the rect already has the sprite's proportions
                var rt = image.rectTransform;
                rt.anchorMin = new Vector2(def.RoomRect.xMin, 1f - def.RoomRect.yMax);
                rt.anchorMax = new Vector2(def.RoomRect.xMax, 1f - def.RoomRect.yMin);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                image.gameObject.SetActive(_state.Data.DecorOwned[i]);
                _decor[i] = image;
                _decorPop[i] = 1f;
            }

            // tap anywhere on the stage
            var tap = UIFactory.Image("TapZone", _stage, null, new Color(0, 0, 0, 0), true);
            UIFactory.Stretch(tap.rectTransform);
            tap.gameObject.AddComponent<TapZone>().OnTap = HandleTap;

            _charRoot = UIFactory.Rect("Streamer", _stage);
            _charRoot.anchorMin = new Vector2(0.5f, 0.15f);
            _charRoot.anchorMax = new Vector2(0.5f, 0.73f);
            _charRoot.pivot = new Vector2(0.5f, 0f);
            _charRoot.offsetMin = Vector2.zero;
            _charRoot.offsetMax = Vector2.zero;
            var fit = _charRoot.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
            fit.aspectRatio = 0.44f; // replaced with the real frame ratio in ApplyOutfit
            _charFit = fit;

            _charGlow = UIFactory.Image("Glow", _charRoot, ArtLibrary.Glow, new Color(1f, 1f, 1f, 0.22f));
            UIFactory.Stretch(_charGlow.rectTransform, -120f, 40f, -120f, -40f);

            _charSprite = UIFactory.Image("Sprite", _charRoot, null, Color.white);
            _charSprite.preserveAspect = true;
            UIFactory.Stretch(_charSprite.rectTransform);
            _charSprite.rectTransform.pivot = new Vector2(0.5f, 0f); // squash from the feet

            // Gift ("super chat") floats across the stage
            UIFactory.Button("Gift", _stage, Color.white, HandleGift, out var giftImage);
            var giftSprite = ArtLibrary.Get("icon_gift");
            giftImage.sprite = giftSprite != null ? giftSprite : ArtLibrary.Circle;
            giftImage.color = giftSprite != null ? Color.white : Palette.Pink;
            giftImage.type = Image.Type.Simple;
            giftImage.preserveAspect = true;
            _gift = giftImage.rectTransform;
            UIFactory.Place(_gift, new Vector2(0f, 0.58f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(160f, 160f));
            var giftGlow = UIFactory.Image("Glow", _gift, ArtLibrary.Glow, new Color(1f, 0.9f, 0.4f, 0.55f));
            UIFactory.Stretch(giftGlow.rectTransform, -50f, -50f, -50f, -50f);
            giftGlow.transform.SetAsFirstSibling();
            _gift.gameObject.SetActive(false);
        }

        void BuildHud()
        {
            var hud = UIFactory.Rect("Hud", _safe);
            UIFactory.Stretch(hud);

            BuildProfile(hud);
            BuildCurrencies(hud);
            BuildHypeGauge(hud);
            BuildChat(hud);
        }

        /// <summary>Top-left card: avatar, name, rank and follower progress.</summary>
        void BuildProfile(Transform hud)
        {
            var card = UIFactory.Image("Profile", hud, ArtLibrary.Panel, HudCard, true);
            UIFactory.Place(card.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(508f, 160f));
            UIFactory.Shadow(card, -7f, 0.28f);

            // white rim + purple ring so the avatar pops out of the card
            var rimSprite = ArtLibrary.LayerLab("avatar_rim", ArtLibrary.Circle);
            var rim = UIFactory.Image("AvatarRim", card.transform, rimSprite, Color.white);
            UIFactory.Place(rim.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(80f, 0f), new Vector2(146f, 146f));
            UIFactory.Shadow(rim, -4f, 0.35f);
            var ring = UIFactory.Image("AvatarRing", card.transform, rimSprite, Palette.Purple);
            UIFactory.Place(ring.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(80f, 0f), new Vector2(132f, 132f));
            var mask = UIFactory.Image("AvatarMask", ring.transform, rimSprite, Palette.Sheet);
            UIFactory.Place(mask.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(118f, 118f));
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            _avatar = UIFactory.Image("Avatar", mask.transform, null, Color.white);
            _avatar.preserveAspect = true;
            UIFactory.Place(_avatar.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(190f, 253f));

            // Layer Lab outline ring drawn over the portrait edge
            var ringSprite = ArtLibrary.Get("LayerLab/avatar_ring");
            if (ringSprite != null)
            {
                var outline = UIFactory.Image("AvatarOutline", card.transform, ringSprite, Palette.Pink);
                UIFactory.Place(outline.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(80f, 0f), new Vector2(140f, 140f));
            }

            var name = UIFactory.Text("Name", card.transform, "Nova", 32f, Palette.Ink, TextAlignmentOptions.Left);
            UIFactory.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(164f, -10f), new Vector2(150f, 40f));

            // LIVE badge and viewer count share the name row
            var liveSprite = ArtLibrary.Get("ui_live");
            var live = UIFactory.Image("Live", card.transform, liveSprite != null ? liveSprite : ArtLibrary.Pill, liveSprite != null ? Color.white : Palette.Red);
            UIFactory.Place(live.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-130f, -11f), new Vector2(96f, 38f));
            if (liveSprite == null)
            {
                var liveText = UIFactory.Text("Text", live.transform, "LIVE", 22f, Color.white, TextAlignmentOptions.Center);
                UIFactory.Stretch(liveText.rectTransform);
            }
            var eye = UIFactory.Icon("ViewerIcon", card.transform, "icon_viewer", Palette.Cyan);
            UIFactory.Place(eye.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-88f, -12f), new Vector2(36f, 36f));
            _viewerText = UIFactory.Text("Viewers", card.transform, "0", 26f, Palette.Ink, TextAlignmentOptions.Left);
            UIFactory.Place(_viewerText.rectTransform, new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(-82f, -11f), new Vector2(76f, 38f));
            _viewerText.enableAutoSizing = true;
            _viewerText.fontSizeMin = 16f;
            _viewerText.fontSizeMax = 26f;

            _rankBadgeImage = UIFactory.Image("RankBadge", card.transform, ArtLibrary.LayerLab("tag", ArtLibrary.Pill), Palette.Purple);
            _rankBadge = _rankBadgeImage.rectTransform;
            UIFactory.Place(_rankBadge, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(326f, -80f), new Vector2(324f, 50f));
            UIFactory.Shadow(_rankBadgeImage, -3f, 0.3f);
            UIFactory.Gloss(_rankBadge, new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.92f), 0.28f);
            var starBadgeSprite = ArtLibrary.Get("LayerLab/badge");
            if (starBadgeSprite != null)
            {
                // scalloped Layer Lab seal behind the star
                var seal = UIFactory.Image("Seal", _rankBadge, starBadgeSprite, Palette.Yellow);
                UIFactory.Place(seal.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(26f, 0f), new Vector2(60f, 56f));
                UIFactory.Shadow(seal, -3f, 0.3f);
            }
            var star = UIFactory.Icon("Star", _rankBadge, "icon_star", Palette.Yellow);
            UIFactory.Place(star.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(40f, 40f));
            _rankText = UIFactory.Text("Name", _rankBadge, "", 28f, Color.white, TextAlignmentOptions.Center);
            UIFactory.Stretch(_rankText.rectTransform, 48f, 0f, 14f, 0f);
            _rankText.enableAutoSizing = true;
            _rankText.fontSizeMin = 18f;
            _rankText.fontSizeMax = 28f;

            var rankBarBg = UIFactory.Image("RankBar", card.transform, ArtLibrary.LayerLab("gauge_inner", ArtLibrary.Pill), Palette.Track);
            UIFactory.Place(rankBarBg.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(164f, -130f), new Vector2(324f, 30f));
            _rankFill = UIFactory.Image("Fill", rankBarBg.transform, ArtLibrary.LayerLab("gauge_fill", ArtLibrary.Pill), Palette.Cyan);
            _rankFill.rectTransform.anchorMin = new Vector2(0f, 0f);
            _rankFill.rectTransform.anchorMax = new Vector2(0f, 1f);
            _rankFill.rectTransform.offsetMin = Vector2.zero;
            _rankFill.rectTransform.offsetMax = Vector2.zero;
            UIFactory.Gloss(_rankFill.transform, new Vector2(0.08f, 0.55f), new Vector2(0.92f, 0.9f), 0.35f);
            _followerText = UIFactory.Text("Followers", rankBarBg.transform, "", 21f, Palette.Ink, TextAlignmentOptions.Center);
            UIFactory.Stretch(_followerText.rectTransform);
        }

        /// <summary>Top-right column: coins with income, gems, then the round boost button.</summary>
        void BuildCurrencies(Transform hud)
        {
            var coinPill = UIFactory.Image("Coins", hud, ArtLibrary.Panel, HudCard, true);
            UIFactory.Place(coinPill.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(372f, 112f));
            UIFactory.Shadow(coinPill, -7f, 0.28f);
            var coin = UIFactory.Icon("CoinIcon", coinPill.transform, "icon_coin", Palette.Yellow);
            _coinIcon = coin.rectTransform;
            UIFactory.Place(_coinIcon, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(58f, 0f), new Vector2(84f, 84f));
            _coinText = UIFactory.Text("Amount", coinPill.transform, "0", 52f, Palette.Ink, TextAlignmentOptions.Left);
            _coinText.rectTransform.anchorMin = new Vector2(0f, 0.36f);
            _coinText.rectTransform.anchorMax = new Vector2(1f, 1f);
            _coinText.rectTransform.offsetMin = new Vector2(112f, 0f);
            _coinText.rectTransform.offsetMax = new Vector2(-14f, -4f);
            _coinText.enableAutoSizing = true;
            _coinText.fontSizeMin = 28f;
            _coinText.fontSizeMax = 52f;
            _incomeText = UIFactory.Text("Income", coinPill.transform, "", 28f, Palette.Green, TextAlignmentOptions.Left);
            _incomeText.rectTransform.anchorMin = new Vector2(0f, 0f);
            _incomeText.rectTransform.anchorMax = new Vector2(1f, 0.4f);
            _incomeText.rectTransform.offsetMin = new Vector2(114f, 6f);
            _incomeText.rectTransform.offsetMax = new Vector2(-14f, 0f);

            var gemPill = UIFactory.Image("Gems", hud, ArtLibrary.Pill, HudCard, true);
            _gemPill = gemPill.rectTransform;
            UIFactory.Place(_gemPill, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-126f, -180f), new Vector2(204f, 68f));
            UIFactory.Shadow(gemPill, -5f, 0.28f);
            var gem = UIFactory.Icon("Icon", _gemPill, "icon_gem", Palette.Cyan);
            UIFactory.Place(gem.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(50f, 50f));
            _gemText = UIFactory.Text("Text", _gemPill, "0", 36f, Palette.Ink, TextAlignmentOptions.Center);
            UIFactory.Stretch(_gemText.rectTransform, 60f, 0f, 16f, 0f);

            // pulsing halo behind the button while the boost is ready (sibling placed before the button so it draws underneath)
            _boostGlow = UIFactory.Image("BoostGlow", hud, ArtLibrary.Glow, new Color(1f, 0.85f, 0.3f, 0f));
            UIFactory.Place(_boostGlow.rectTransform, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-96f, -308f), new Vector2(250f, 250f));

            UIFactory.Button("Boost", hud, Palette.Yellow, HandleBoost, out _boostButton);
            _boostButton.sprite = ArtLibrary.LayerLab("boost_base", ArtLibrary.Circle); // Layer Lab round button with 3D lip
            _boostButton.type = Image.Type.Simple;
            UIFactory.Place(_boostButton.rectTransform, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-96f, -308f), new Vector2(148f, 148f));
            UIFactory.Shadow(_boostButton, -7f, 0.35f);
            // Layer Lab double ring around the button
            var ring = UIFactory.Image("Ring", _boostButton.transform, ArtLibrary.LayerLab("boost_ring", ArtLibrary.Circle), new Color(1f, 1f, 1f, 0.9f));
            UIFactory.Place(ring.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(182f, 182f));
            // radial sweep: time left while active, cooldown left while recharging
            _boostProgress = UIFactory.Image("Progress", _boostButton.transform, ArtLibrary.Circle, new Color(Palette.Navy.r, Palette.Navy.g, Palette.Navy.b, 0.35f));
            UIFactory.Stretch(_boostProgress.rectTransform, 10f, 10f, 10f, 10f);
            _boostProgress.type = Image.Type.Filled;
            _boostProgress.fillMethod = Image.FillMethod.Radial360;
            _boostProgress.fillOrigin = (int)Image.Origin360.Top;
            _boostProgress.fillClockwise = false;
            _boostProgress.fillAmount = 0f;
            _boostProgress.enabled = false;
            var rocket = UIFactory.Icon("Icon", _boostButton.transform, "icon_boost", Palette.Pink);
            UIFactory.Place(rocket.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 14f), new Vector2(84f, 84f));
            _boostLabel = UIFactory.Text("Label", _boostButton.transform, "x2", 28f, Palette.Navy, TextAlignmentOptions.Center);
            UIFactory.Place(_boostLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(150f, 36f));
        }

        /// <summary>Vertical gauge on the left edge: fills from the bottom, flame on top.</summary>
        void BuildHypeGauge(Transform hud)
        {
            var track = UIFactory.Image("HypeGauge", hud, ArtLibrary.LayerLab("gauge_frame", ArtLibrary.Pill), new Color(1f, 1f, 1f, 0.95f));
            _hypeBar = track.rectTransform;
            UIFactory.Place(_hypeBar, new Vector2(0f, 0.66f), new Vector2(0.5f, 0.5f), new Vector2(64f, 0f), new Vector2(72f, 440f));
            UIFactory.Shadow(track, -6f, 0.3f);
            var inner = UIFactory.Image("Inner", _hypeBar, ArtLibrary.LayerLab("gauge_inner", ArtLibrary.Pill), new Color(Palette.Navy.r, Palette.Navy.g, Palette.Navy.b, 0.4f));
            UIFactory.Stretch(inner.rectTransform, 8f, 8f, 8f, 8f);
            _hypeFillImage = UIFactory.Image("Fill", _hypeBar, ArtLibrary.LayerLab("gauge_fill", ArtLibrary.Pill), Palette.Pink);
            _hypeFill = _hypeFillImage.rectTransform;
            _hypeFill.anchorMin = new Vector2(0f, 0f);
            _hypeFill.anchorMax = new Vector2(1f, 0f);
            _hypeFill.offsetMin = new Vector2(8f, 8f);
            _hypeFill.offsetMax = new Vector2(-8f, -8f);
            UIFactory.Gloss(_hypeFill, new Vector2(0.2f, 0.03f), new Vector2(0.45f, 0.97f), 0.4f);

            // quarter marks on the track
            for (int i = 1; i <= 3; i++)
            {
                var tick = UIFactory.Image("Tick" + i, _hypeBar, null, new Color(1f, 1f, 1f, 0.3f));
                tick.rectTransform.anchorMin = new Vector2(0.22f, 0f);
                tick.rectTransform.anchorMax = new Vector2(0.78f, 0f);
                tick.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                tick.rectTransform.anchoredPosition = new Vector2(0f, 8f + (440f - 16f) * i / 4f);
                tick.rectTransform.sizeDelta = new Vector2(0f, 3f);
            }

            var flame = UIFactory.Icon("Flame", _hypeBar, "skill_hype", Palette.Pink);
            _hypeFlame = flame.rectTransform;
            UIFactory.Place(_hypeFlame, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, 26f), new Vector2(112f, 112f));

            // label: white rim around a navy pill, turns pink while the hype train runs
            // Layer Lab ticket tag (notched ends) with its outline sprite as the rim
            var labelRim = UIFactory.Image("LabelRim", _hypeBar, ArtLibrary.LayerLab("tag_outline", ArtLibrary.Pill), new Color(1f, 1f, 1f, 0.95f));
            UIFactory.Place(labelRim.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(6f, -10f), new Vector2(160f, 62f));
            UIFactory.Shadow(labelRim, -4f, 0.3f);
            _hypeLabelPill = UIFactory.Image("LabelPill", _hypeBar, ArtLibrary.LayerLab("tag", ArtLibrary.Pill), new Color(Palette.Navy.r, Palette.Navy.g, Palette.Navy.b, 0.85f));
            UIFactory.Place(_hypeLabelPill.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(6f, -14f), new Vector2(150f, 54f));
            UIFactory.Shadow(_hypeLabelPill, -3f, 0.25f);
            _hypeLabel = UIFactory.Text("Label", _hypeLabelPill.transform, "HYPE", 26f, Color.white, TextAlignmentOptions.Center);
            UIFactory.Stretch(_hypeLabel.rectTransform);
            _hypeLabel.enableAutoSizing = true;
            _hypeLabel.fontSizeMin = 16f;
            _hypeLabel.fontSizeMax = 26f;
        }

        /// <summary>Bottom-left: translucent chat card with the donation jar button above it.</summary>
        void BuildChat(Transform hud)
        {
            var card = UIFactory.Image("ChatCard", hud, ArtLibrary.Panel, new Color(Palette.Navy.r, Palette.Navy.g, Palette.Navy.b, 0.5f));
            UIFactory.Place(card.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, NavHeight + 20f), new Vector2(640f, 258f));
            _chat = ChatFeed.Create(card.transform);
            UIFactory.Place((RectTransform)_chat.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(26f, 12f), new Vector2(590f, 234f));

            UIFactory.Button("Donations", hud, Palette.Mint, HandleDonations, out _donateImage);
            _donateImage.sprite = ArtLibrary.Pill;
            _donate = _donateImage.rectTransform;
            UIFactory.Place(_donate, new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(24f + 190f, NavHeight + 20f + 258f + 58f), new Vector2(380f, 88f));
            var jar = UIFactory.Icon("Icon", _donate, "icon_coin", Palette.Yellow);
            UIFactory.Place(jar.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(48f, 0f), new Vector2(60f, 60f));
            var caption = UIFactory.Text("Caption", _donate, "CLAIM DONATIONS", 22f, Palette.Navy, TextAlignmentOptions.Left);
            UIFactory.Place(caption.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(88f, -8f), new Vector2(280f, 30f));
            _donateText = UIFactory.Text("Amount", _donate, "0", 36f, Palette.Navy, TextAlignmentOptions.Left);
            UIFactory.Place(_donateText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(88f, 6f), new Vector2(280f, 46f));
            _donate.gameObject.SetActive(false);
        }

        // ================================================================ loop

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _time += dt;
            _state.Tick(dt);

            _saveTimer += dt;
            if (_saveTimer >= GameConfig.AutosaveInterval)
            {
                _saveTimer = 0f;
                _state.Save();
            }

            if (Screen.safeArea != _lastSafeArea) ApplySafeArea();

            // counter eases toward the real value so big jumps feel like a payout
            _shownCoins += (_state.Data.Coins - _shownCoins) * (1.0 - Math.Exp(-14.0 * dt));
            _hudTimer -= dt;
            if (_hudTimer <= 0f)
            {
                _hudTimer = 0.05f;
                RefreshHud();
            }

            AnimateStreamer(dt);
            AnimateHypeBar();
            AnimateDecor(dt);
            UpdateGift(dt);
            UpdateDonations(dt);
            _chat.Tick(dt, _state.Viewers, _state.HypeActive);
        }

        void RefreshHud()
        {
            _coinText.text = NumberFormatter.Format(_shownCoins);
            _incomeText.text = "+" + NumberFormatter.Format(_state.IncomePerSec) + "/s";
            _incomeText.color = _state.HypeActive ? Palette.Pink : _state.BoostActive ? Palette.Hex("E08A00") : Palette.Green;
            _gemText.text = _state.Data.Gems.ToString();
            _viewerText.text = NumberFormatter.Format(_state.Viewers);

            _rankFill.rectTransform.anchorMax = new Vector2(Mathf.Max(0.06f, _state.RankProgress), 1f);
            _followerText.text = _state.IsMaxRank
                ? NumberFormatter.Format(_state.Data.Followers) + " fans"
                : NumberFormatter.Format(_state.Data.Followers) + " / " +
                  NumberFormatter.Format(GameConfig.Ranks[_state.Data.RankIndex + 1].Followers) + " fans";

            // donation jar appears once there is something worth claiming, and pulses when full
            double pot = _state.Data.DonationPot;
            bool showJar = pot >= 1.0 && pot >= _state.DonationCap * 0.03;
            if (_donate.gameObject.activeSelf != showJar) _donate.gameObject.SetActive(showJar);
            if (showJar)
            {
                _donateText.text = NumberFormatter.Format(pot) + (_state.DonationFull ? "  FULL" : "");
                _donateImage.color = _state.DonationFull ? Palette.Yellow : Palette.Mint;
            }

            if (_state.BoostActive)
            {
                _boostLabel.text = NumberFormatter.Duration(_state.Data.BoostRemaining);
                _boostButton.color = Palette.Mint;
                _boostProgress.enabled = true;
                _boostProgress.color = new Color(1f, 1f, 1f, 0.5f); // bright arc = time left
                _boostProgress.fillAmount = Mathf.Clamp01(_state.Data.BoostRemaining / GameConfig.BoostDuration);
            }
            else if (_state.Data.BoostCooldown > 0f)
            {
                _boostLabel.text = NumberFormatter.Duration(_state.Data.BoostCooldown);
                _boostButton.color = Palette.Disabled;
                _boostProgress.enabled = true;
                _boostProgress.color = new Color(Palette.Navy.r, Palette.Navy.g, Palette.Navy.b, 0.35f); // dark sweep = cooldown left
                _boostProgress.fillAmount = Mathf.Clamp01(_state.Data.BoostCooldown / GameConfig.BoostCooldown);
            }
            else
            {
                _boostLabel.text = "x2";
                _boostButton.color = Palette.Yellow;
                _boostProgress.enabled = false;
            }
        }

        void AnimateStreamer(float dt)
        {
            // idle breathing, turns into a dance while hype is active
            float bob, tilt, scale;
            Sprite frame = null;
            if (_state.HypeActive)
            {
                float beat = _time * 9f;
                bob = Mathf.Abs(Mathf.Sin(beat)) * 34f;
                tilt = Mathf.Sin(beat * 0.5f) * 2.5f;
                scale = 1.04f + 0.03f * Mathf.Sin(beat * 2f);
                // new pose each time the bounce lands
                if (_danceFrames.Length > 0) frame = _danceFrames[(int)(beat / Mathf.PI) % _danceFrames.Length];
            }
            else
            {
                bob = Mathf.Sin(_time * 2.1f) * 7f;
                tilt = Mathf.Sin(_time * 1.3f) * 1.2f;
                scale = 1f;
            }

            if (frame == null && _idleFrames.Length > 0)
            {
                _blinkTimer -= dt;
                if (_blinkTimer < -BlinkDuration) _blinkTimer = UnityEngine.Random.Range(2.2f, 4.5f);
                frame = _idleFrames[_blinkTimer < 0f && _idleFrames.Length > 1 ? 1 : 0];
            }
            if (frame != null && _charSprite.sprite != frame) _charSprite.sprite = frame;

            if (_outfitPop < 1f)
            {
                _outfitPop = Mathf.Min(1f, _outfitPop + dt / 0.5f);
                scale *= MMTween.Tween(_outfitPop, 0f, 1f, 0.7f, 1f, MMTween.MMTweenCurve.EaseOutElastic);
            }

            // slide up so the streamer stays visible (and tappable) above the open sheet
            _charRoot.anchoredPosition = new Vector2(0f, bob + 390f * _panel.OpenAmount);
            _charRoot.localRotation = Quaternion.Euler(0f, 0f, tilt);
            _charRoot.localScale = new Vector3(scale, scale, 1f);

            float glow = _state.HypeActive ? 0.55f + 0.25f * Mathf.Sin(_time * 12f) : 0.2f + 0.05f * Mathf.Sin(_time * 2f);
            var glowColor = _state.HypeActive ? Palette.Pink : Color.white;
            _charGlow.color = new Color(glowColor.r, glowColor.g, glowColor.b, glow);

            float tint = _state.HypeActive ? 0.12f + 0.05f * Mathf.Sin(_time * 9f) : 0f;
            var c = _stageTint.color;
            _stageTint.color = new Color(c.r, c.g, c.b, Mathf.MoveTowards(c.a, tint, dt * 0.6f));
        }

        void AnimateHypeBar()
        {
            float hype = _state.Hype;
            _hypeFill.gameObject.SetActive(hype > 0.01f); // whole fill, so its gloss hides too
            _hypeFill.anchorMax = new Vector2(1f, Mathf.Max(0.16f, hype));
            if (_state.HypeActive)
            {
                _hypeFillImage.color = Color.Lerp(Palette.Pink, Palette.Yellow, 0.5f + 0.5f * Mathf.Sin(_time * 14f));
                _hypeLabel.text = "x" + Mathf.RoundToInt(GameConfig.HypeMult) + "  " + Mathf.CeilToInt(_state.HypeTimeLeft) + "s";
                _hypeLabelPill.color = Color.Lerp(Palette.Pink, Palette.Purple, 0.5f + 0.5f * Mathf.Sin(_time * 14f));
                float s = 1.15f + 0.1f * Mathf.Sin(_time * 14f);
                _hypeFlame.localScale = new Vector3(s, s, 1f);
            }
            else
            {
                _hypeFillImage.color = Color.Lerp(Palette.Purple, Palette.Pink, hype);
                _hypeLabel.text = hype > 0.01f ? "HYPE" : "TAP!";
                _hypeLabelPill.color = new Color(Palette.Navy.r, Palette.Navy.g, Palette.Navy.b, 0.85f);
                float s = 1f + 0.06f * hype + 0.02f * Mathf.Sin(_time * 3f); // flame swells as the gauge fills
                _hypeFlame.localScale = new Vector3(s, s, 1f);
            }

            // boost halo pulses while the boost is ready
            bool boostReady = !_state.BoostActive && _state.Data.BoostCooldown <= 0f;
            float glowAlpha = boostReady ? 0.35f + 0.2f * Mathf.Sin(_time * 4f) : 0f;
            _boostGlow.color = new Color(1f, 0.85f, 0.3f, glowAlpha);

            if (_donate.gameObject.activeSelf)
            {
                float pulse = _state.DonationFull ? 1f + 0.05f * Mathf.Sin(_time * 7f) : 1f;
                _donate.localScale = new Vector3(pulse, pulse, 1f);
            }
        }

        void AnimateDecor(float dt)
        {
            for (int i = 0; i < _decor.Length; i++)
            {
                if (_decorPop[i] >= 1f) continue;
                _decorPop[i] = Mathf.Min(1f, _decorPop[i] + dt / 0.6f);
                float s = MMTween.Tween(_decorPop[i], 0f, 1f, 0f, 1f, MMTween.MMTweenCurve.EaseOutElastic);
                _decor[i].rectTransform.localScale = new Vector3(s, s, 1f);
            }
        }

        // ================================================================ input handlers

        void HandleTap(Vector2 screenPosition)
        {
            if (_popup.IsOpen) return;
            double value = _state.Tap(out bool crit);
            Vector2 local = _fx.ScreenToLocal(screenPosition) + new Vector2(UnityEngine.Random.Range(-30f, 30f), 40f);

            if (crit)
            {
                FeelKit.I.Crit();
                _fx.SpawnText(local, "VIRAL! +" + NumberFormatter.Format(value), Palette.Yellow, 76f, 240f, 1.1f);
                _fx.Burst(local, 10, Confetti, 700f);
                _fx.SpawnFlyers(local, _coinIcon, 5, "icon_coin", Palette.Yellow);
            }
            else
            {
                FeelKit.I.Tap();
                _fx.SpawnText(local, "+" + NumberFormatter.Format(value), Color.white);
                _fx.Burst(local, 2, Hearts, 520f);
                if (_state.Data.TotalTaps % 4 == 0) _fx.SpawnFlyers(local, _coinIcon, 1, "icon_coin", Palette.Yellow);
            }
        }

        void HandleBoost()
        {
            if (_state.ActivateBoost())
            {
                FeelKit.I.Punch(_boostButton.transform, 1.25f);
                FeelKit.I.Sfx("sfx_hype", 0.7f, 1.2f);
                FeelKit.I.Vibrate(FeelKit.Haptic.Success);
                Vector2 at = _fx.WorldToLocal(_boostButton.transform.position);
                _fx.SpawnText(at + new Vector2(-190f, -130f), "SPONSORED! x2", Palette.Yellow, 56f, 120f, 1.4f);
                _fx.Burst(at, 14, Confetti, 800f);
                _chat.Push("StreamStar", "Sponsor boost is live: x2 income!", Palette.Yellow);
            }
            else
            {
                FeelKit.I.Punch(_boostButton.transform, 1.06f);
                FeelKit.I.Denied();
            }
            RefreshHud();
        }

        void HandleDonations()
        {
            double coins = _state.ClaimDonations();
            if (coins <= 0) return;
            Vector2 at = _fx.WorldToLocal(_donate.position);
            FeelKit.I.Gift();
            _fx.SpawnText(at + new Vector2(60f, 70f), "+" + NumberFormatter.Format(coins), Palette.Yellow, 64f, 200f, 1.2f);
            _fx.Burst(at, 16, Confetti, 800f);
            _fx.SpawnFlyers(at, _coinIcon, 10, "icon_coin", Palette.Yellow);
            _chat.Push("StreamStar", "Donations claimed. Thank you, chat!", Palette.Yellow);
            RefreshHud();
        }

        void HandleGift()
        {
            if (!_giftActive) return;
            _giftActive = false;
            _gift.gameObject.SetActive(false);
            _giftTimer = UnityEngine.Random.Range(GameConfig.GiftMinDelay, GameConfig.GiftMaxDelay);

            double coins = _state.CollectGift(out bool gem);
            Vector2 at = _fx.WorldToLocal(_gift.position);
            FeelKit.I.Gift();
            _fx.SpawnText(at, "SUPER CHAT! +" + NumberFormatter.Format(coins), Palette.Yellow, 64f, 220f, 1.4f);
            _fx.Burst(at, 22, Confetti);
            _fx.SpawnFlyers(at, _coinIcon, 12, "icon_coin", Palette.Yellow);
            if (gem) _fx.SpawnFlyers(at, _gemPill, 1, "icon_gem", Palette.Cyan, false);
            _chat.PushDonation(ChatFeed.RandomName(), NumberFormatter.Format(coins));
        }

        // ================================================================ game events

        void HandleRankUp(int rank, int gems)
        {
            var def = GameConfig.Ranks[rank];
            ApplyRank();
            FeelKit.I.RankUp();
            Vector2 center = _fx.WorldToLocal(_charRoot.position) + new Vector2(0f, 380f);
            _fx.Burst(center, 40, Confetti, 1300f);
            _popup.Show("RANK UP!",
                "<color=#" + UIFactory.Hex(def.Color) + ">" + def.Name + "</color>\n<size=34>Income x" +
                _state.RankMult.ToString("0.0") + "   +" + gems + " gems</size>",
                "AWESOME", "icon_star", def.Color);
            _chat.Push("StreamStar", "Nova reached " + def.Name + "!", Palette.Yellow);
            _state.Save();
        }

        void HandleHypeStart()
        {
            FeelKit.I.HypeStart();
            Vector2 center = _fx.WorldToLocal(_charRoot.position) + new Vector2(0f, 860f);
            _fx.SpawnText(center, "HYPE TRAIN! x" + Mathf.RoundToInt(GameConfig.HypeMult), Palette.Pink, 84f, 260f, 1.5f);
            _fx.Burst(center, 26, Confetti, 1200f);
        }

        void HandleHypeEnd()
        {
            FeelKit.I.Punch(_hypeBar, 0.94f);
        }

        void HandleGems(int amount)
        {
            if (_fx == null) return;
            Vector2 at = _fx.WorldToLocal(_gemPill.position);
            _fx.SpawnText(at + new Vector2(0f, -70f), "+" + amount, Palette.Cyan, 48f, -70f, 1f);
            FeelKit.I.Punch(_gemPill, 1.2f);
        }

        public void OnDecorBought(int index)
        {
            _decor[index].gameObject.SetActive(true);
            _decor[index].rectTransform.localScale = Vector3.zero;
            _decorPop[index] = 0f;
            _fx.Burst(_fx.WorldToLocal(_decor[index].transform.position), 12, Confetti, 600f);
        }

        public void OnOutfitChanged(bool newlyUnlocked)
        {
            ApplyOutfit();
            _outfitPop = 0f;
            Vector2 center = _fx.WorldToLocal(_charRoot.position) + new Vector2(0f, 360f);
            _fx.Burst(center, newlyUnlocked ? 36 : 14, Confetti, 1000f);
            if (newlyUnlocked)
            {
                FeelKit.I.Sfx("sfx_gift", 0.9f);
                FeelKit.I.Vibrate(FeelKit.Haptic.Success);
            }
        }

        public void FloatAt(Transform target, string text, Color color)
        {
            _fx.SpawnText(_fx.WorldToLocal(target.position) + new Vector2(0f, 50f), text, color, 44f, 110f, 0.8f);
        }

        public void ShowOffline(double coins, double seconds)
        {
            _popup.Show("WELCOME BACK!",
                "Your replays earned\n<color=#" + UIFactory.Hex(Palette.Pink) + ">" + NumberFormatter.Format(coins) +
                " coins</color> in " + NumberFormatter.Duration(seconds),
                "COLLECT", "icon_coin", Palette.Purple,
                () =>
                {
                    _fx.SpawnFlyers(Vector2.zero, _coinIcon, 14, "icon_coin", Palette.Yellow);
                    FeelKit.I.Vibrate(FeelKit.Haptic.Success);
                });
        }

        // ================================================================ helpers

        void UpdateGift(float dt)
        {
            if (!_giftActive)
            {
                _giftTimer -= dt;
                if (_giftTimer > 0f) return;
                _giftActive = true;
                _giftLife = 0f;
                _giftPhase = UnityEngine.Random.Range(0f, 6f);
                _gift.gameObject.SetActive(true);
                _gift.SetAsLastSibling();
                FeelKit.I.Sfx("sfx_coin", 0.6f, 1.3f);
            }

            _giftLife += dt;
            float t = _giftLife / GameConfig.GiftLifetime;
            if (t >= 1f)
            {
                _giftActive = false;
                _gift.gameObject.SetActive(false);
                _giftTimer = UnityEngine.Random.Range(GameConfig.GiftMinDelay, GameConfig.GiftMaxDelay);
                return;
            }
            float width = _stage.rect.width;
            float x = Mathf.Lerp(-90f, width + 90f, t);
            float y = Mathf.Sin(_giftPhase + _giftLife * 2.4f) * 70f;
            _gift.anchoredPosition = new Vector2(x, y);
            _gift.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_giftLife * 5f) * 10f);
            float pulse = 1f + 0.08f * Mathf.Sin(_giftLife * 8f);
            _gift.localScale = new Vector3(pulse, pulse, 1f);
        }

        void UpdateDonations(float dt)
        {
            // purely cosmetic: shows where the idle income comes from
            _donationTimer -= dt;
            if (_donationTimer > 0f) return;
            _donationTimer = UnityEngine.Random.Range(3.5f, 7f);
            double income = _state.IncomePerSec;
            if (income <= 0) return;
            double amount = income * UnityEngine.Random.Range(1.5f, 4f);
            _chat.PushDonation(ChatFeed.RandomName(), NumberFormatter.Format(amount));
            if (!_donate.gameObject.activeSelf || _panel.OpenAmount > 0.01f) return;
            Vector2 from = _fx.WorldToLocal(_chat.transform.position) + new Vector2(320f, 40f);
            _fx.SpawnFlyers(from, _donate, 2, "icon_coin", Palette.Yellow, false);
        }

        void ApplyOutfit()
        {
            var def = GameConfig.Outfits[_state.Data.EquippedOutfit];
            _idleFrames = LoadFrames(def.Folder + "Idle_", 0, 1);
            // hype train plays the outfit's Signature dance; outfits without one fall back to the regular dance
            // (folders may have gaps, so missing frames are skipped)
            _danceFrames = LoadFrames(def.Folder + "Signature/Dance/Dance_", 1, 12);
            if (_danceFrames.Length == 0) _danceFrames = LoadFrames(def.Folder + "Dance/Dance_", 1, 12);
            _blinkTimer = 2f;

            var sprite = _idleFrames.Length > 0 ? _idleFrames[0] : null;
            if (sprite != null) _charFit.aspectRatio = sprite.rect.width / sprite.rect.height;
            _charSprite.sprite = sprite != null ? sprite : ArtLibrary.Circle;
            _charSprite.color = sprite != null ? Color.white : def.Tint;

            // profile avatar: the dedicated face crop when it exists, else zoom in on the full-body idle pose
            var avatar = ArtLibrary.Get(def.Avatar);
            if (avatar != null)
            {
                _avatar.sprite = avatar;
                _avatar.color = Color.white;
                _avatar.rectTransform.sizeDelta = new Vector2(118f, 118f);
                _avatar.rectTransform.anchoredPosition = Vector2.zero;
            }
            else
            {
                _avatar.sprite = _charSprite.sprite;
                _avatar.color = _charSprite.color;
                bool tall = sprite != null && sprite.rect.height > sprite.rect.width * 1.8f;
                _avatar.rectTransform.sizeDelta = tall ? new Vector2(277f, 630f) : new Vector2(190f, 253f);
                _avatar.rectTransform.anchoredPosition = tall ? new Vector2(0f, -228f) : new Vector2(0f, -30f);
            }
        }

        static Sprite[] LoadFrames(string prefix, int first, int last)
        {
            var frames = new System.Collections.Generic.List<Sprite>();
            for (int i = first; i <= last; i++)
            {
                var sprite = ArtLibrary.Get(prefix + i);
                if (sprite != null) frames.Add(sprite);
            }
            return frames.ToArray();
        }

        void ApplyRank()
        {
            var def = GameConfig.Ranks[_state.Data.RankIndex];
            _rankBadgeImage.color = def.Color;
            _rankText.text = def.Name;
            _rankFill.color = _state.IsMaxRank ? Palette.Yellow : GameConfig.Ranks[_state.Data.RankIndex + 1].Color;
        }

        void ApplySafeArea()
        {
            _lastSafeArea = Screen.safeArea;
            if (Screen.width <= 0 || Screen.height <= 0) return;
            _safe.anchorMin = new Vector2(_lastSafeArea.xMin / Screen.width, _lastSafeArea.yMin / Screen.height);
            _safe.anchorMax = new Vector2(_lastSafeArea.xMax / Screen.width, _lastSafeArea.yMax / Screen.height);
        }

        void OnApplicationPause(bool paused)
        {
            if (paused && _state != null) _state.Save();
        }

        void OnApplicationQuit()
        {
            if (_state != null) _state.Save();
        }
    }
}
