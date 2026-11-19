using System.Collections.Generic;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.UI;
#if MOREMOUNTAINS_NICEVIBRATIONS_INSTALLED
using Lofelt.NiceVibrations;
using MoreMountains.FeedbacksForThirdParty;
#endif

namespace StreamStar
{
    /// <summary>
    /// The only place that talks to Feel. Builds MMF_Players in code (no prefabs needed) and exposes one
    /// method per game moment.
    /// </summary>
    public class FeelKit : MonoBehaviour
    {
        public static FeelKit I { get; private set; }

        public enum Haptic { Selection, Success, Warning, Light, Medium, Heavy }

        const int FlashId = 7;

        MMSoundManager _sound;
        MMF_Player _tap, _crit, _hype, _rankUp, _gift, _coinPunch;
        readonly Dictionary<Transform, MMF_Player> _punchers = new Dictionary<Transform, MMF_Player>();
        float _lastCoinPunch;

        public static FeelKit Create()
        {
            var go = new GameObject("FeelKit");
            DontDestroyOnLoad(go);
            I = go.AddComponent<FeelKit>();

            // Feel managers: time (freeze frames) and sound (pooled audio sources, tracks).
            var time = new GameObject("MMTimeManager");
            time.transform.SetParent(go.transform, false);
            time.AddComponent<MMTimeManager>();

            I._sound = MMSoundManager.HasInstance ? MMSoundManager.Instance : null;
            if (I._sound == null)
            {
                var sound = new GameObject("MMSoundManager");
                I._sound = sound.AddComponent<MMSoundManager>();
            }
            return I;
        }

        /// <summary>Creates the full-screen flash target used by MMF_Flash.</summary>
        public static void CreateFlash(Transform parent)
        {
            var rt = UIFactory.Rect("Flash", parent);
            UIFactory.Stretch(rt);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = false;
            var group = rt.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            var flash = rt.gameObject.AddComponent<MMFlash>();
            flash.FlashID = FlashId;
            flash.Interruptable = true;
        }

        public void Bind(Transform character, Transform coinCounter, Transform hypeBar, Transform rankBadge)
        {
            // Tap: squash the streamer, tiny haptic tick, pop sound.
            _tap = NewPlayer("Tap");
            AddSquash(_tap, character, 0.14f, 0.93f);
            AddSound(_tap, "sfx_tap", 0.55f, 0.92f, 1.12f);
            AddHaptic(_tap, Haptic.Light);
            _tap.Initialization();

            // Crit: bigger punch, quick flash, stronger haptic.
            _crit = NewPlayer("Crit");
            AddScale(_crit, character, 0.28f, 1.16f);
            AddFlash(_crit, Palette.Yellow, 0.12f, 0.22f);
            AddSound(_crit, "sfx_crit", 0.8f, 0.95f, 1.08f);
            AddHaptic(_crit, Haptic.Medium);
            _crit.Initialization();

            _hype = NewPlayer("Hype");
            AddScale(_hype, hypeBar, 0.45f, 1.18f);
            AddScale(_hype, character, 0.45f, 1.2f);
            AddFlash(_hype, Palette.Pink, 0.35f, 0.5f);
            AddSound(_hype, "sfx_hype", 0.9f, 1f, 1f);
            AddHaptic(_hype, Haptic.Heavy);
            _hype.Initialization();

            _rankUp = NewPlayer("RankUp");
            var freeze = Add<MMF_FreezeFrame>(_rankUp);
            freeze.FreezeFrameDuration = 0.12f;
            AddFlash(_rankUp, Color.white, 0.5f, 0.85f);
            AddScale(_rankUp, rankBadge, 0.6f, 1.35f);
            AddScale(_rankUp, character, 0.6f, 1.25f);
            AddSound(_rankUp, "sfx_rankup", 1f, 1f, 1f);
            AddHaptic(_rankUp, Haptic.Success);
            _rankUp.Initialization();

            _gift = NewPlayer("Gift");
            AddScale(_gift, coinCounter, 0.4f, 1.25f);
            AddFlash(_gift, Palette.Yellow, 0.25f, 0.3f);
            AddSound(_gift, "sfx_gift", 0.9f, 1f, 1f);
            AddHaptic(_gift, Haptic.Success);
            _gift.Initialization();

            _coinPunch = NewPlayer("CoinPunch");
            AddScale(_coinPunch, coinCounter, 0.16f, 1.1f);
            _coinPunch.Initialization();
        }

        // ------------------------------------------------------------ game moments

        public void Tap() => Play(_tap);
        public void Crit() => Play(_crit);
        public void HypeStart() => Play(_hype);
        public void RankUp() => Play(_rankUp);
        public void Gift() => Play(_gift);

        public void CoinPunch()
        {
            if (Time.unscaledTime - _lastCoinPunch < 0.12f) return;
            _lastCoinPunch = Time.unscaledTime;
            Play(_coinPunch);
        }

        /// <summary>Scale punch on any UI element (buttons, icons). Players are created on demand and reused.</summary>
        public void Punch(Transform target, float amount = 1.12f)
        {
            if (target == null) return;
            if (!_punchers.TryGetValue(target, out var player) || player == null)
            {
                player = NewPlayer("Punch_" + target.name);
                AddScale(player, target, 0.2f, amount);
                player.Initialization();
                _punchers[target] = player;
            }
            Play(player);
        }

        public void Purchase(Transform button, Transform icon)
        {
            Punch(button, 1.1f);
            Punch(icon, 1.25f);
            Sfx("sfx_buy", 0.8f, Random.Range(0.96f, 1.06f));
            Vibrate(Haptic.Selection);
        }

        public void Denied()
        {
            Sfx("sfx_error", 0.7f);
            Vibrate(Haptic.Warning);
        }

        public void Sfx(string clipName, float volume = 1f, float pitch = 1f, bool ui = false)
        {
            var clip = ArtLibrary.Clip(clipName);
            if (clip == null || _sound == null) return;
            var options = MMSoundManagerPlayOptions.Default;
            options.MmSoundManagerTrack = ui ? MMSoundManager.MMSoundManagerTracks.UI : MMSoundManager.MMSoundManagerTracks.Sfx;
            options.Volume = volume;
            options.Pitch = pitch;
            _sound.PlaySound(clip, options);
        }

        public void StartMusic()
        {
            var clip = ArtLibrary.Clip("bgm_loop");
            if (clip == null || _sound == null) return;
            var options = MMSoundManagerPlayOptions.Default;
            options.MmSoundManagerTrack = MMSoundManager.MMSoundManagerTracks.Music;
            options.Loop = true;
            options.Volume = 0.35f;
            options.Fade = true;
            options.FadeInitialVolume = 0f;
            options.FadeDuration = 2f;
            options.ID = 9001;
            _sound.PlaySound(clip, options);
        }

        public void Vibrate(Haptic haptic)
        {
#if MOREMOUNTAINS_NICEVIBRATIONS_INSTALLED
            HapticPatterns.PlayPreset(ToPreset(haptic));
#endif
        }

        // ------------------------------------------------------------ builders

        MMF_Player NewPlayer(string label)
        {
            var go = new GameObject("MMF_" + label);
            go.transform.SetParent(transform, false);
            var player = go.AddComponent<MMF_Player>();
            player.InitializationMode = MMFeedbacks.InitializationModes.Script;
            // UI feedback must keep running during freeze frames
            player.ForceTimescaleMode = true;
            player.ForcedTimescaleMode = TimescaleModes.Unscaled;
            return player;
        }

        static T Add<T>(MMF_Player player) where T : MMF_Feedback
        {
            var feedback = (T)player.AddFeedback(typeof(T));
            feedback.Timing.TimescaleMode = TimescaleModes.Unscaled;
            return feedback;
        }

        static AnimationCurve PunchCurve() =>
            new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.25f, 1f), new Keyframe(0.6f, -0.25f), new Keyframe(1f, 0f));

        static void AddScale(MMF_Player player, Transform target, float duration, float peak)
        {
            if (target == null) return;
            var scale = Add<MMF_Scale>(player);
            scale.Mode = MMF_Scale.Modes.Absolute;
            scale.AnimateScaleTarget = target;
            scale.AnimateScaleDuration = duration;
            scale.RemapCurveZero = 1f;
            scale.RemapCurveOne = peak;
            scale.AnimateScaleTweenX = new MMTweenType(PunchCurve());
            scale.AnimateScaleTweenY = new MMTweenType(PunchCurve());
            scale.AnimateScaleTweenZ = new MMTweenType(PunchCurve());
        }

        static void AddSquash(MMF_Player player, Transform target, float duration, float squash)
        {
            if (target == null) return;
            var feedback = Add<MMF_SquashAndStretch>(player);
            feedback.SquashAndStretchTarget = target;
            feedback.Mode = MMF_SquashAndStretch.Modes.Absolute;
            feedback.Axis = MMF_SquashAndStretch.PossibleAxis.YtoXZ;
            feedback.AnimateScaleDuration = duration;
            feedback.RemapCurveZero = 1f;
            feedback.RemapCurveOne = squash;
            feedback.AnimateCurve = PunchCurve();
        }

        static void AddFlash(MMF_Player player, Color color, float duration, float alpha)
        {
            var flash = Add<MMF_Flash>(player);
            flash.FlashColor = color;
            flash.FlashDuration = duration;
            flash.FlashAlpha = alpha;
            flash.FlashID = FlashId;
        }

        static void AddSound(MMF_Player player, string clipName, float volume, float minPitch, float maxPitch)
        {
            var clip = ArtLibrary.Clip(clipName);
            if (clip == null) return;
            var sound = Add<MMF_MMSoundManagerSound>(player);
            sound.Sfx = clip;
            sound.MinVolume = volume;
            sound.MaxVolume = volume;
            sound.MinPitch = minPitch;
            sound.MaxPitch = maxPitch;
            sound.MmSoundManagerTrack = MMSoundManager.MMSoundManagerTracks.Sfx;
            sound.MaximumConcurrentInstances = 6;
        }

        static void AddHaptic(MMF_Player player, Haptic haptic)
        {
#if MOREMOUNTAINS_NICEVIBRATIONS_INSTALLED
            var preset = Add<MMF_NVPreset>(player);
            preset.Preset = ToPreset(haptic);
#endif
        }

#if MOREMOUNTAINS_NICEVIBRATIONS_INSTALLED
        static HapticPatterns.PresetType ToPreset(Haptic haptic)
        {
            switch (haptic)
            {
                case Haptic.Selection: return HapticPatterns.PresetType.Selection;
                case Haptic.Success: return HapticPatterns.PresetType.Success;
                case Haptic.Warning: return HapticPatterns.PresetType.Warning;
                case Haptic.Medium: return HapticPatterns.PresetType.MediumImpact;
                case Haptic.Heavy: return HapticPatterns.PresetType.HeavyImpact;
                default: return HapticPatterns.PresetType.LightImpact;
            }
        }
#endif

        static void Play(MMF_Player player)
        {
            if (player != null) player.PlayFeedbacks();
        }
    }
}
