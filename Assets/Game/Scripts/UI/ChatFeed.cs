using MoreMountains.Tools;
using TMPro;
using UnityEngine;

namespace StreamStar
{
    /// <summary>Fake live chat. Message rate scales with the viewer count.</summary>
    public class ChatFeed : MonoBehaviour
    {
        const int LineCount = 5;
        const float LineHeight = 46f;

        static readonly string[] Names =
        {
            "pixelfox", "mochi_cat", "NightOwl99", "luna.exe", "tofu_king", "BeepBoop", "sodapop", "kiwi_bird",
            "zzz_sleepy", "RocketRaccoon7", "minty", "ghostpepper", "noodle_arm", "starry_ky", "bao_bun", "DJ_Panda",
            "quietstorm", "lemon_drop", "captain_nap", "8bit_hero"
        };

        static readonly string[] Messages =
        {
            "hiii Nova!", "first time here, love the vibe", "LETS GOOO", "that setup is so clean", "pog", "gg",
            "one more round!", "lol", "the lighting looks great today", "greetings from Hanoi", "followed!",
            "can you say hi to my cat", "this stream fixed my day", "W stream", "new mic? sounds crisp",
            "clip that!", "how long have you been streaming?", "so cozy", "hello from the night shift",
            "that room glow up tho", "hype hype hype", "teach me your ways", "ok that was smooth", "omg same"
        };

        static readonly string[] HypeMessages = { "HYPE TRAIN!!", "CHOO CHOO", "WE ARE SO BACK", "NOVA NOVA NOVA", "INSANE", "LET'S GOOOOO" };

        static readonly Color[] NameColors = { Palette.Cyan, Palette.Pink, Palette.Mint, Palette.Yellow, Palette.Hex("B79CFF"), Palette.Hex("FF9E6B") };

        TextMeshProUGUI[] _lines;
        readonly string[] _content = new string[LineCount];
        float _timer = 0.5f;
        float _pop = 1f;

        public static ChatFeed Create(Transform parent)
        {
            var rt = UIFactory.Rect("Chat", parent);
            var feed = rt.gameObject.AddComponent<ChatFeed>();
            feed._lines = new TextMeshProUGUI[LineCount];
            for (int i = 0; i < LineCount; i++)
            {
                var line = UIFactory.Text("Line" + i, rt, "", 30f, Color.white, TextAlignmentOptions.Left, false);
                UIFactory.Place(line.rectTransform, Vector2.zero, new Vector2(0f, 0.5f), new Vector2(0f, (i + 0.5f) * LineHeight), new Vector2(520f, LineHeight));
                line.overflowMode = TextOverflowModes.Ellipsis;
                line.outlineWidth = 0.22f;
                line.outlineColor = new Color32(27, 20, 54, 255);
                line.alpha = 1f - i * 0.17f;
                feed._lines[i] = line;
                feed._content[i] = "";
            }
            return feed;
        }

        public void Tick(float dt, double viewers, bool hype)
        {
            _timer -= dt;
            if (_timer <= 0f)
            {
                // 3 viewers -> a message every ~2.6s, thousands -> several per second
                float rate = Mathf.Clamp(0.35f + Mathf.Log10((float)viewers + 1f) * 0.55f, 0.35f, 4f) * (hype ? 2f : 1f);
                _timer = Random.Range(0.6f, 1.4f) / rate;
                string[] pool = hype && Random.value < 0.6f ? HypeMessages : Messages;
                Push(Names[Random.Range(0, Names.Length)], pool[Random.Range(0, pool.Length)], NameColors[Random.Range(0, NameColors.Length)]);
            }

            if (_pop < 1f)
            {
                _pop = Mathf.Min(1f, _pop + dt / 0.25f);
                float scale = MMTween.Tween(_pop, 0f, 1f, 0.6f, 1f, MMTween.MMTweenCurve.EaseOutOverhead);
                _lines[0].rectTransform.localScale = new Vector3(scale, scale, 1f);
            }
        }

        public void Push(string user, string message, Color nameColor)
        {
            Add("<color=#" + UIFactory.Hex(nameColor) + "><b>" + user + "</b></color>  " + message);
        }

        public void PushDonation(string user, string amount)
        {
            Add("<color=#" + UIFactory.Hex(Palette.Yellow) + "><b>" + user + " donated " + amount + "!</b></color>");
        }

        public static string RandomName() => Names[Random.Range(0, Names.Length)];

        void Add(string richText)
        {
            for (int i = LineCount - 1; i > 0; i--) _content[i] = _content[i - 1];
            _content[0] = richText;
            for (int i = 0; i < LineCount; i++) _lines[i].text = _content[i];
            _pop = 0f;
        }
    }
}
