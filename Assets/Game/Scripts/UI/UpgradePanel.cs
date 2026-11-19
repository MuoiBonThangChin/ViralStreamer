using System;
using System.Collections.Generic;
using MoreMountains.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StreamStar
{
    /// <summary>
    /// Bottom navigation (Gear, Skills, Room, Outfits) plus the sheet that slides up from it.
    /// The sheet is closed by default so the stage owns the screen.
    /// </summary>
    public class UpgradePanel : MonoBehaviour
    {
        class Row
        {
            public RectTransform Root;
            public Image Background, Icon, Button, CostIcon, Chip, BarTrack, BarFill;
            public TextMeshProUGUI Name, Info, ButtonTop, ButtonCost, ChipText;
            public Action Refresh;
            public float Shake = 1f;
        }

        static readonly string[] TabNames = { "Gear", "Skills", "Room", "Outfits" };
        static readonly string[] TabTitles = { "Stream Gear", "Skills", "Room Decor", "Outfits" };
        static readonly string[] TabIcons = { "tab_gear", "tab_skills", "tab_room", "tab_outfit" };

        public const float SheetHeight = 770f;
        const float RowHeight = 168f;
        const float RowGap = 14f;
        const float SlideTime = 0.32f;

        GameState _state;
        MainUI _ui;
        RectTransform _sheet;
        ScrollRect _scroll;
        readonly RectTransform[] _contents = new RectTransform[4];
        readonly Image[] _tabButtons = new Image[4];
        readonly List<Row>[] _rows = { new List<Row>(), new List<Row>(), new List<Row>(), new List<Row>() };
        TextMeshProUGUI _buyModeLabel, _title;
        Image _buyModeButton;
        int _tab;
        bool _open;
        float _slide; // 0 closed .. 1 open
        float _restY;
        float _refreshTimer;

        /// <summary>0 when the sheet is hidden, 1 when fully open (eased).</summary>
        public float OpenAmount { get; private set; }

        public static UpgradePanel Create(RectTransform sheet, RectTransform nav, GameState state, MainUI ui)
        {
            var panel = sheet.gameObject.AddComponent<UpgradePanel>();
            panel._state = state;
            panel._ui = ui;
            panel._sheet = sheet;
            panel._restY = sheet.anchoredPosition.y;
            panel.BuildSheet(sheet);
            panel.BuildNav(nav);
            panel.ShowTab(0);
            panel.ApplySlide();
            return panel;
        }

        // ------------------------------------------------------------ build

        void BuildNav(RectTransform nav)
        {
            for (int i = 0; i < 4; i++)
            {
                int index = i;
                UIFactory.Button("Nav" + TabNames[i], nav, Palette.Purple, () => OnNav(index), out var image);
                var rt = image.rectTransform;
                rt.anchorMin = new Vector2(i * 0.25f, 0f);
                rt.anchorMax = new Vector2((i + 1) * 0.25f, 1f);
                rt.offsetMin = new Vector2(9f, 0f);
                rt.offsetMax = new Vector2(-9f, 0f);
                var icon = UIFactory.Icon("Icon", image.transform, TabIcons[i], Color.white);
                UIFactory.Place(icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(84f, 84f));
                var label = UIFactory.Text("Label", image.transform, TabNames[i], 28f, Color.white, TextAlignmentOptions.Center);
                UIFactory.Place(label.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(230f, 40f));
                _tabButtons[i] = image;
            }
        }

        void BuildSheet(RectTransform root)
        {
            var background = UIFactory.Image("Background", root, ArtLibrary.Panel, Palette.Sheet, true);
            UIFactory.Stretch(background.rectTransform, 0, -80, 0, 0);

            // title tab sits across the top edge
            var titlePill = UIFactory.Image("Title", root, ArtLibrary.Pill, Palette.Purple, true);
            UIFactory.Place(titlePill.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -4f), new Vector2(440f, 92f));
            _title = UIFactory.Text("Text", titlePill.transform, "", 42f, Color.white, TextAlignmentOptions.Center);
            UIFactory.Stretch(_title.rectTransform);

            UIFactory.Button("Close", root, Palette.Pink, Close, out var close);
            close.sprite = ArtLibrary.Circle;
            close.type = Image.Type.Simple;
            UIFactory.Place(close.rectTransform, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-70f, -8f), new Vector2(92f, 92f));
            var cross = UIFactory.Text("X", close.transform, "X", 46f, Color.white, TextAlignmentOptions.Center);
            UIFactory.Stretch(cross.rectTransform);

            UIFactory.Button("BuyMode", root, Palette.Pink, CycleBuyMode, out _buyModeButton);
            UIFactory.Place(_buyModeButton.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(128f, -8f), new Vector2(208f, 92f));
            _buyModeLabel = UIFactory.Text("Label", _buyModeButton.transform, "BUY x1", 34f, Color.white, TextAlignmentOptions.Center);
            UIFactory.Stretch(_buyModeLabel.rectTransform, 0, 8, 0, 0);

            // ---- scroll view
            var scrollRt = UIFactory.Rect("Scroll", root);
            UIFactory.Stretch(scrollRt, 20f, 0f, 20f, 70f);
            _scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            _scroll.horizontal = false;
            _scroll.movementType = ScrollRect.MovementType.Elastic;
            _scroll.scrollSensitivity = 40f;
            var viewport = UIFactory.Image("Viewport", scrollRt, null, new Color(0, 0, 0, 0), true);
            UIFactory.Stretch(viewport.rectTransform);
            viewport.gameObject.AddComponent<RectMask2D>();
            _scroll.viewport = viewport.rectTransform;

            for (int i = 0; i < 4; i++)
            {
                var content = UIFactory.Rect("Content" + TabNames[i], viewport.transform);
                content.anchorMin = new Vector2(0f, 1f);
                content.anchorMax = new Vector2(1f, 1f);
                content.pivot = new Vector2(0.5f, 1f);
                content.offsetMin = Vector2.zero;
                content.offsetMax = Vector2.zero;
                _contents[i] = content;
            }

            for (int i = 0; i < GameConfig.Gear.Length; i++) AddGearRow(i);
            for (int i = 0; i < GameConfig.Skills.Length; i++) AddSkillRow(i);
            for (int i = 0; i < GameConfig.Decor.Length; i++) AddDecorRow(i);
            for (int i = 0; i < GameConfig.Outfits.Length; i++) AddOutfitRow(i);

            for (int i = 0; i < 4; i++)
                _contents[i].sizeDelta = new Vector2(0f, _rows[i].Count * (RowHeight + RowGap) + 28f);
        }

        // ------------------------------------------------------------ rows

        Row NewRow(int tab, string title, string iconName, Color fallback, bool gemPrice)
        {
            var row = new Row();
            int index = _rows[tab].Count;
            row.Background = UIFactory.Image("Row" + index, _contents[tab], ArtLibrary.Panel, Palette.Card);
            row.Root = row.Background.rectTransform;
            row.Root.anchorMin = new Vector2(0f, 1f);
            row.Root.anchorMax = new Vector2(1f, 1f);
            row.Root.pivot = new Vector2(0.5f, 1f);
            row.Root.offsetMin = new Vector2(0f, -(index + 1) * (RowHeight + RowGap));
            row.Root.offsetMax = new Vector2(0f, -index * (RowHeight + RowGap) - RowGap);

            var plate = UIFactory.Image("IconPlate", row.Root, ArtLibrary.Panel, Palette.Sheet);
            UIFactory.Place(plate.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(84f, 8f), new Vector2(128f, 128f));
            row.Icon = UIFactory.Icon("Icon", plate.transform, iconName, fallback);
            UIFactory.Place(row.Icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110f, 110f));

            // level chip overlaps the bottom of the icon plate
            row.Chip = UIFactory.Image("Chip", row.Root, ArtLibrary.Pill, Palette.Purple);
            UIFactory.Place(row.Chip.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(84f, -58f), new Vector2(112f, 38f));
            row.ChipText = UIFactory.Text("Text", row.Chip.transform, "", 24f, Color.white, TextAlignmentOptions.Center);
            UIFactory.Stretch(row.ChipText.rectTransform);

            row.Name = UIFactory.Text("Name", row.Root, title, 36f, Palette.Ink, TextAlignmentOptions.Left);
            row.Name.rectTransform.anchorMin = new Vector2(0f, 0.58f);
            row.Name.rectTransform.anchorMax = new Vector2(1f, 1f);
            row.Name.rectTransform.offsetMin = new Vector2(170f, 0f);
            row.Name.rectTransform.offsetMax = new Vector2(-296f, -12f);
            row.Name.overflowMode = TextOverflowModes.Ellipsis;

            row.Info = UIFactory.Text("Info", row.Root, "", 27f, Palette.InkDim, TextAlignmentOptions.Left);
            row.Info.rectTransform.anchorMin = new Vector2(0f, 0.28f);
            row.Info.rectTransform.anchorMax = new Vector2(1f, 0.58f);
            row.Info.rectTransform.offsetMin = new Vector2(170f, 0f);
            row.Info.rectTransform.offsetMax = new Vector2(-296f, 0f);
            row.Info.overflowMode = TextOverflowModes.Ellipsis;

            row.BarTrack = UIFactory.Image("Bar", row.Root, ArtLibrary.Pill, Palette.Track);
            row.BarTrack.rectTransform.anchorMin = new Vector2(0f, 0f);
            row.BarTrack.rectTransform.anchorMax = new Vector2(1f, 0f);
            row.BarTrack.rectTransform.offsetMin = new Vector2(170f, 20f);
            row.BarTrack.rectTransform.offsetMax = new Vector2(-296f, 38f);
            row.BarFill = UIFactory.Image("Fill", row.BarTrack.transform, ArtLibrary.Pill, Palette.Purple);
            row.BarFill.rectTransform.anchorMin = Vector2.zero;
            row.BarFill.rectTransform.anchorMax = new Vector2(0f, 1f);
            row.BarFill.rectTransform.offsetMin = Vector2.zero;
            row.BarFill.rectTransform.offsetMax = Vector2.zero;

            UIFactory.Button("Buy" + tab + "_" + index, row.Root, Palette.Mint, null, out row.Button);
            UIFactory.Place(row.Button.rectTransform, new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-148f, 0f), new Vector2(264f, 120f));

            row.ButtonTop = UIFactory.Text("Top", row.Button.transform, "", 25f, Palette.Navy, TextAlignmentOptions.Center);
            UIFactory.Place(row.ButtonTop.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(250f, 36f));

            row.CostIcon = UIFactory.Icon("CostIcon", row.Button.transform, gemPrice ? "icon_gem" : "icon_coin", gemPrice ? Palette.Cyan : Palette.Yellow);
            UIFactory.Place(row.CostIcon.rectTransform, new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(42f, 48f), new Vector2(46f, 46f));

            row.ButtonCost = UIFactory.Text("Cost", row.Button.transform, "", 38f, Palette.Navy, TextAlignmentOptions.Center);
            UIFactory.Place(row.ButtonCost.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(18f, 48f), new Vector2(180f, 50f));
            row.ButtonCost.enableAutoSizing = true;
            row.ButtonCost.fontSizeMin = 22f;
            row.ButtonCost.fontSizeMax = 38f;

            _rows[tab].Add(row);
            return row;
        }

        static void SetButton(Row row, string top, string cost, bool showCostIcon, Color color)
        {
            row.ButtonTop.text = top;
            row.ButtonCost.text = cost;
            row.CostIcon.enabled = showCostIcon;
            row.Button.color = color;
            Color text = color == Palette.Purple ? Color.white : color == Palette.Disabled ? Palette.InkDim : Palette.Navy;
            row.ButtonTop.color = text;
            row.ButtonCost.color = text;
            // centre the label when there is no price icon or no caption above it
            row.ButtonCost.rectTransform.anchoredPosition = new Vector2(showCostIcon ? 18f : 0f, top.Length > 0 ? 48f : 64f);
        }

        static void SetChip(Row row, string text)
        {
            bool show = !string.IsNullOrEmpty(text);
            row.Chip.gameObject.SetActive(show);
            if (show) row.ChipText.text = text;
        }

        /// <summary>Negative progress hides the bar.</summary>
        static void SetBar(Row row, float progress, Color color)
        {
            bool show = progress >= 0f;
            row.BarTrack.gameObject.SetActive(show);
            if (!show) return;
            row.BarFill.enabled = progress > 0.001f;
            row.BarFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp(progress, 0.06f, 1f), 1f);
            row.BarFill.color = color;
        }

        void OnResult(Row row, bool success, string floatText)
        {
            if (success)
            {
                FeelKit.I.Purchase(row.Button.transform, row.Icon.transform);
                _ui.FloatAt(row.Icon.transform, floatText, Palette.Mint);
                RefreshAll();
            }
            else
            {
                FeelKit.I.Denied();
                row.Shake = 0f;
            }
        }

        void AddGearRow(int i)
        {
            var def = GameConfig.Gear[i];
            var row = NewRow(0, def.Name, def.Icon, Palette.Cyan, false);
            row.Button.GetComponent<Button>().onClick.AddListener(() =>
            {
                int count = _state.GearPlannedCount(i);
                OnResult(row, _state.BuyGear(i), "+" + count + " Lv");
            });
            row.Refresh = () =>
            {
                int level = _state.Data.GearLevels[i];
                bool unlocked = _state.GearUnlocked(i);
                row.Icon.color = unlocked ? (ArtLibrary.Get(def.Icon) != null ? Color.white : Palette.Cyan) : new Color(0.25f, 0.22f, 0.4f, 0.55f);
                SetChip(row, level > 0 ? "LV " + level : "");
                if (!unlocked)
                {
                    row.Info.text = "Buy " + GameConfig.Gear[i - 1].Name + " first";
                    SetBar(row, -1f, Palette.Purple);
                    SetButton(row, "", "LOCKED", false, Palette.Disabled);
                    return;
                }
                int next = GameState.NextMilestone(level);
                string income = level > 0
                    ? "+" + NumberFormatter.Format(_state.GearIncome(i)) + "/s"
                    : "+" + NumberFormatter.Format(def.BaseIncome) + "/s each";
                row.Info.text = next > 0
                    ? "<color=#" + UIFactory.Hex(Palette.Green) + ">" + income + "</color>   x2 at Lv " + next
                    : "<color=#" + UIFactory.Hex(Palette.Green) + ">" + income + "</color>";
                if (next > 0)
                {
                    int previous = 0;
                    for (int m = 0; m < GameConfig.GearMilestones.Length; m++)
                        if (GameConfig.GearMilestones[m] <= level) previous = GameConfig.GearMilestones[m];
                    SetBar(row, (level - previous) / (float)(next - previous), Palette.Purple);
                }
                else SetBar(row, 1f, Palette.Yellow);
                int count = _state.GearPlannedCount(i);
                double cost = _state.GearPlannedCost(i);
                SetButton(row, "BUY x" + count, NumberFormatter.Format(cost), true, _state.Data.Coins >= cost ? Palette.Mint : Palette.Disabled);
            };
        }

        void AddSkillRow(int i)
        {
            var def = GameConfig.Skills[i];
            var row = NewRow(1, def.Name, def.Icon, Palette.Pink, false);
            row.Button.GetComponent<Button>().onClick.AddListener(() =>
            {
                int count = _state.SkillPlannedCount(i);
                OnResult(row, _state.BuySkill(i), "+" + count + " Lv");
            });
            row.Refresh = () =>
            {
                int level = _state.Data.SkillLevels[i];
                SetChip(row, "LV " + level);
                switch (i)
                {
                    case GameConfig.SkillTap: row.Info.text = NumberFormatter.Format(_state.TapValue) + " coins per tap"; break;
                    case GameConfig.SkillEngage: row.Info.text = "Tap adds " + level + "% of income/s"; break;
                    case GameConfig.SkillCrit: row.Info.text = (level * 2) + "% chance of x5 tap"; break;
                    case GameConfig.SkillHype: row.Info.text = "Hype lasts " + Mathf.RoundToInt(_state.HypeDuration) + "s"; break;
                    default: row.Info.text = "Offline earnings up to " + Mathf.RoundToInt((float)(_state.OfflineCapSeconds / 3600.0)) + "h"; break;
                }
                SetBar(row, def.MaxLevel > 0 ? level / (float)def.MaxLevel : (level % 10) / 10f, Palette.Pink);
                if (_state.SkillMaxed(i))
                {
                    SetButton(row, "", "MAX", false, Palette.Purple);
                    return;
                }
                int count = _state.SkillPlannedCount(i);
                double cost = _state.SkillPlannedCost(i);
                SetButton(row, "UPGRADE x" + count, NumberFormatter.Format(cost), true, _state.Data.Coins >= cost ? Palette.Mint : Palette.Disabled);
            };
        }

        void AddDecorRow(int i)
        {
            var def = GameConfig.Decor[i];
            var row = NewRow(2, def.Name, def.Icon, Palette.Mint, false);
            SetChip(row, "");
            SetBar(row, -1f, Palette.Purple);
            row.Button.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (_state.Data.DecorOwned[i]) return;
                bool bought = _state.BuyDecor(i);
                OnResult(row, bought, "+" + Mathf.RoundToInt(def.Bonus * 100f) + "%");
                if (bought) _ui.OnDecorBought(i);
            });
            row.Refresh = () =>
            {
                row.Info.text = "<color=#" + UIFactory.Hex(Palette.Green) + ">+" + Mathf.RoundToInt(def.Bonus * 100f) + "% income</color>, forever";
                if (_state.Data.DecorOwned[i]) SetButton(row, "", "OWNED", false, Palette.Purple);
                else SetButton(row, "BUY", NumberFormatter.Format(def.Cost), true, _state.Data.Coins >= def.Cost ? Palette.Mint : Palette.Disabled);
            };
        }

        void AddOutfitRow(int i)
        {
            var def = GameConfig.Outfits[i];
            var row = NewRow(3, def.Name, def.IconName, def.Tint, true);
            SetChip(row, "");
            SetBar(row, -1f, Palette.Purple);
            row.Button.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (_state.Data.OutfitOwned[i] && _state.Data.EquippedOutfit == i) return;
                bool wasOwned = _state.Data.OutfitOwned[i];
                bool ok = _state.BuyOrEquipOutfit(i);
                OnResult(row, ok, wasOwned ? "Equipped!" : "Unlocked!");
                if (ok) _ui.OnOutfitChanged(!wasOwned);
            });
            row.Refresh = () =>
            {
                string green = "<color=#" + UIFactory.Hex(Palette.Green) + ">";
                if (def.TapBonus > 0f) row.Info.text = green + "+" + Mathf.RoundToInt(def.TapBonus * 100f) + "% tap power</color>, forever";
                else if (def.IncomeBonus > 0f) row.Info.text = green + "+" + Mathf.RoundToInt(def.IncomeBonus * 100f) + "% income</color>, forever";
                else row.Info.text = "Nova's everyday look";

                if (!_state.Data.OutfitOwned[i])
                    SetButton(row, "UNLOCK", def.GemCost.ToString(), true, _state.Data.Gems >= def.GemCost ? Palette.Cyan : Palette.Disabled);
                else if (_state.Data.EquippedOutfit == i) SetButton(row, "", "WEARING", false, Palette.Purple);
                else SetButton(row, "", "WEAR", false, Palette.Yellow);
            };
        }

        // ------------------------------------------------------------ tabs / buy mode

        void OnNav(int tab)
        {
            if (_open && _tab == tab) Close();
            else
            {
                _open = true;
                ShowTab(tab);
                Feedback(_tabButtons[tab].transform, 1f);
            }
        }

        void Close()
        {
            if (!_open) return;
            _open = false;
            Highlight();
            Feedback(_tabButtons[_tab].transform, 0.85f);
        }

        void ShowTab(int tab)
        {
            _tab = tab;
            for (int i = 0; i < 4; i++) _contents[i].gameObject.SetActive(i == tab);
            _title.text = TabTitles[tab];
            _scroll.content = _contents[tab];
            _scroll.StopMovement();
            _contents[tab].anchoredPosition = Vector2.zero;
            Highlight();
            RefreshAll();
        }

        void Highlight()
        {
            for (int i = 0; i < 4; i++)
                _tabButtons[i].color = _open && i == _tab ? Palette.Pink : Palette.Purple;
        }

        static void Feedback(Transform target, float pitch)
        {
            FeelKit.I.Punch(target, 1.1f);
            FeelKit.I.Sfx("sfx_tab", 0.7f, pitch, true);
            FeelKit.I.Vibrate(FeelKit.Haptic.Selection);
        }

        void CycleBuyMode()
        {
            _state.BuyMode = _state.BuyMode == 1 ? 10 : _state.BuyMode == 10 ? -1 : 1;
            _buyModeLabel.text = _state.BuyMode > 0 ? "BUY x" + _state.BuyMode : "BUY MAX";
            Feedback(_buyModeButton.transform, 1.15f);
            RefreshAll();
        }

        public void RefreshAll()
        {
            var rows = _rows[_tab];
            for (int i = 0; i < rows.Count; i++) rows[i].Refresh();
        }

        void ApplySlide()
        {
            OpenAmount = MMTween.Tween(_slide, 0f, 1f, 0f, 1f, MMTween.MMTweenCurve.EaseOutCubic);
            // closed: parked below the screen, behind the navigation bar
            _sheet.anchoredPosition = new Vector2(0f, _restY - (1f - OpenAmount) * (SheetHeight + _restY + 120f));
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;

            float target = _open ? 1f : 0f;
            if (!Mathf.Approximately(_slide, target))
            {
                _slide = Mathf.MoveTowards(_slide, target, dt / SlideTime);
                ApplySlide();
            }
            if (_slide <= 0f) return;

            _refreshTimer -= dt;
            if (_refreshTimer <= 0f)
            {
                _refreshTimer = 0.1f;
                RefreshAll();
            }

            // "can't afford" shake on the buy button
            var rows = _rows[_tab];
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.Shake >= 1f) continue;
                row.Shake = Mathf.Min(1f, row.Shake + dt / 0.35f);
                float damp = MMTween.Tween(row.Shake, 0f, 1f, 1f, 0f, MMTween.MMTweenCurve.EaseOutCubic);
                float angle = Mathf.Sin(row.Shake * Mathf.PI * 6f) * 7f * damp;
                row.Button.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
        }
    }
}
