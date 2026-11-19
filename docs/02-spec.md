# 02 — Spec kỹ thuật: Stream Star: Idle Tycoon

## 1. Nền tảng

| Mục | Giá trị |
|---|---|
| Engine | Unity **6000.0.73f1**, Built-in Render Pipeline, uGUI 2.0 + TextMeshPro |
| Input | Input System (`InputSystemUIInputModule`), có fallback `StandaloneInputModule` |
| Mục tiêu | Android / iOS dọc, chạy được trong Editor và Standalone |
| Độ phân giải tham chiếu | 1080×1920, `CanvasScaler` Scale With Screen Size, match 0.5 |
| Game feel | **Feel v5.9.1** (MMFeedbacks + MMTools + NiceVibrations) sao chép từ `D:\GitProject\SandASMR\Assets\Feel` |
| Lưu trữ | JSON trong `PlayerPrefs` (key `streamstar.save.v1`) |

## 2. Kiến trúc

Toàn bộ scene được **dựng bằng code lúc chạy** — không phụ thuộc prefab hay thao tác tay trong Editor. `GameBootstrap` tự khởi động qua `[RuntimeInitializeOnLoadMethod]`, nên scene `Main.unity` chỉ cần tồn tại.

```
Assets/Game/
  Scripts/
    Core/    GameConfig, SaveData, GameState, NumberFormatter, GameBootstrap
    Feel/    FeelKit            (dựng MMF_Player bằng code, âm thanh, rung)
    Art/     ArtLibrary         (nạp sprite/audio từ Resources, có fallback)
    UI/      UIFactory, MainUI, UpgradePanel, ChatFeed, FloatingTextPool,
             GiftSpawner, Popups, TapZone
    Editor/  GameAssetPostprocessor, ProjectSetup
  Resources/Art, Resources/Audio   (asset do codex tạo)
  Scenes/Main.unity                (ProjectSetup sinh ra)
```

- `GameState` là C# thuần (không MonoBehaviour): giữ `SaveData`, tính toán kinh tế, phát event (`OnRankUp`, `OnHypeStart`, `OnPurchase`…).
- UI chỉ đọc `GameState` và gọi các hàm hành động (`Tap()`, `BuyGear()`…).
- `FeelKit` là điểm duy nhất chạm vào API Feel, để phần còn lại của game không phụ thuộc trực tiếp.
- Thiếu asset không làm hỏng game: `ArtLibrary` trả về sprite trắng dựng sẵn và UI tô màu thay thế.

## 3. Kinh tế

Mọi số lớn dùng `double`. Định dạng: `1.23K, M, B, T`, sau đó `aa, ab, …`.

### 3.1 Gear — thu nhập mỗi giây

`cost(L) = baseCost × 1.15^L` · `income = baseIncome × L × 2^(số mốc đã đạt)` · mốc tại level 10, 25, 50, 100, 150, 200, 250, 300. Mỗi mốc thưởng thêm 1 gem.

| # | Id | Tên | baseCost | baseIncome/s |
|---|---|---|---|---|
| 1 | webcam | Webcam | 15 | 0.5 |
| 2 | mic | Microphone | 100 | 3 |
| 3 | headset | Headset | 1.1K | 20 |
| 4 | keyboard | RGB Keyboard | 12K | 120 |
| 5 | lights | Ring Lights | 130K | 800 |
| 6 | chair | Gaming Chair | 1.4M | 5K |
| 7 | pc | Gaming PC | 20M | 40K |
| 8 | greenscreen | Green Screen | 330M | 300K |

Gear thứ *n* mở khoá khi gear *n−1* đạt level 1.

### 3.2 Skills

| Id | Hiệu ứng | Max | Cost |
|---|---|---|---|
| tap | Tap cơ bản = `(1+L) × 2^floor(L/10)` | ∞ | `10 × 1.18^L` |
| engage | Tap cộng thêm `1% × L` thu nhập/giây | 50 | `500 × 1.4^L` |
| crit | Tỉ lệ chí mạng `2% × L`, chí mạng ×5 | 25 | `250 × 1.6^L` |
| hype | Thời lượng Hype `8s + 1s × L` | 10 | `1K × 2^L` |
| offline | Trần offline `2h + 1h × L` | 10 | `5K × 2.2^L` |

### 3.3 Room decor (mua một lần, cộng dồn)

Plant 2K +10% · Poster 25K +15% · Desk Lamp 300K +20% · Shelf 5M +25% · Neon Sign 80M +35% · Trophy 2B +50%.

### 3.4 Outfits (mua bằng gem; bonus vĩnh viễn khi đã sở hữu, mặc bộ nào là tuỳ ý)

Casual: miễn phí · Gamer 15 gem: tap +50% · Idol 40 gem: thu nhập +50% · Cyber 90 gem: tap +100% · Royal 200 gem: thu nhập +100%.

### 3.5 Công thức tổng

```
incomePerSec = Σ gearIncome × (1 + Σ decor%) × outfitIncomeMult × rankMult × boostMult × hypeMult
rankMult     = 1 + 0.5 × rankIndex
tapValue     = (tapBase + incomePerSec_không_hype × engage%) × outfitTapMult × rankMult × hypeMult × (crit ? 5 : 1)
viewers      = floor(3 + 4 × incomePerSec^0.55) (×1.5 khi Hype)
followers   += viewers × 0.04 mỗi giây, +1×(rankIndex+1) mỗi lần tap
```

### 3.6 Rank (theo follower)

Newcomer 0 · Rookie 100 · Rising Star 1K · Affiliate 10K · Partner 100K · Influencer 1M · Celebrity 10M · Superstar 100M · Legend 1B. Lên rank *i* thưởng `5 × i` gem.

### 3.7 Hype, Gift, Boost, Offline

- **Hype**: mỗi tap +2.5%; sau 1.5 s không tap thì giảm 6%/s. Đầy → Hype Mode: `hypeMult = 3` trong thời lượng kỹ năng, thanh tụt dần về 0. UI không bị khoá.
- **Gift (Super Chat)**: 25–45 s xuất hiện một hộp quà bay ngang sân khấu trong 7 s. Chạm: `max(50, incomePerSec × 60)` coin, 20% rơi 1 gem.
- **Boost**: ×2 thu nhập 60 s, hồi 300 s. Thay cho rewarded ad.
- **Offline**: khi mở lại, `coin = incomePerSec × min(vắng mặt, trần) × 0.5`; hiện popup nếu vắng trên 60 s.
- **Autosave**: 10 s một lần, khi pause và khi thoát.

## 4. UI

Bố cục bản 2, làm lại sau khi phân tích video gameplay ([04-video-analysis.md](04-video-analysis.md)). Canvas tham chiếu 1080×1920, co theo chiều rộng.

```
┌────────────────────────────────┐
│ [(Nova) Nova ●LIVE 👁5 ] [🪙 10   ]│  Thẻ hồ sơ · thẻ coin (+thu nhập/s)
│ [       ★ Newcomer     ] [ +0.5/s ]│
│ [       ▓░ 22/100 fans ]   [💎 0] │  gem
│                             (🚀x2)│  nút Boost tròn
│ 🔥                                │
│ ▌                                 │  Thanh Hype dọc, đầy từ dưới lên
│ ▌            (Nova)         🎁    │  Sân khấu phủ toàn màn hình: phòng,
│ ▌                                 │  decor, nhân vật (tap), quà bay
│[HYPE]                             │
│ (🪙 CLAIM DONATIONS 2.5)          │  Hũ donation
│ ┌ chat mờ, 5 dòng ───────┐        │
│ └────────────────────────┘        │
│ [Gear] [Skills] [Room] [Outfits]  │  Thanh điều hướng
└────────────────────────────────┘
```

- **Sân khấu** chiếm trọn vùng an toàn. HUD là các thẻ trắng nổi (alpha 0.93), chữ tím đậm `#3B2A6B`; không còn thanh nào che ngang màn hình.
- **Thẻ hồ sơ** (trái trên): avatar tròn cắt từ sprite trang phục đang mặc, tên, LIVE + số viewer, huy hiệu rank, thanh follower.
- **Tiền tệ** (phải trên): thẻ coin kèm thu nhập/giây (xanh lá; hồng khi Hype; cam khi Boost), viên gem, rồi nút Boost.
- **Thanh Hype** dọc ở mép trái, nhãn bên dưới đổi giữa `TAP!`, `HYPE` và `x3 8s`.
- **Bảng nâng cấp** là bottom sheet cao 770, đóng mặc định. Bấm một nút điều hướng để mở tab tương ứng, bấm lại hoặc bấm X để đóng; trượt 0.32 s (MMTween EaseOutCubic). Khi mở, nhân vật trượt lên 390 để vẫn thấy mặt và vẫn tap được.
- **Dòng nâng cấp**: thẻ trắng gồm ảnh, nhãn `LV n`, tên, chỉ số (xanh lá), thanh tiến độ tới mốc ×2 kế tiếp (gear) hoặc tới cấp tối đa (skill), nút giá. Nút xám nhạt khi không đủ tiền; cập nhật mỗi 0.1 s và chỉ khi sheet đang mở.
- **Nút mua x1/x10/MAX** nằm ở mép trên bên trái của sheet.
- **Hũ donation**: nút xanh phía trên chat, hiện khi hũ đạt 3% trần; chuyển vàng và nhịp nhẹ khi đầy.
- **Popup**: thẻ tím nhạt, tiêu đề màu nhấn, chữ tím đậm.

### 4.1 Hũ donation (cơ chế mới)

`pot += incomePerSecBase × 0.5 × dt`, trần `incomePerSecBase × 0.5 × 240`. Tức là hũ đầy sau 4 phút và người chơi chủ động nhận thêm tối đa 50% thu nhập nền. Hũ không tích luỹ khi offline. Lưu trong `SaveData.DonationPot`.

## 5. Game feel — dùng Feel ở đâu

| Sự kiện | Feel |
|---|---|
| Tap nhân vật | `MMF_Player`: `MMF_SquashAndStretch` trên Nova, `MMF_NVPreset` LightImpact, âm `sfx_tap` qua `MMSoundManager`; số bay bằng `MMTween` |
| Chí mạng | `MMF_Scale` punch mạnh hơn + `MMF_Flash` nhẹ + haptic MediumImpact |
| Mua nâng cấp | `MMF_Scale` punch trên nút và icon, haptic Selection, `sfx_buy` |
| Không đủ tiền | `MMF_RotationShake`-kiểu lắc ngang bằng `MMTween`, haptic Warning, `sfx_error` |
| Coin HUD thay đổi | `MMF_Scale` punch bộ đếm coin (có cooldown) |
| Hype bắt đầu | `MMF_Flash` hồng, `MMF_Scale` thanh hype, haptic HeavyImpact, `sfx_hype`; Nova nhún theo nhịp suốt thời gian hype |
| Nhặt quà | `MMF_Scale` + burst coin bay về HUD (`MMTween` EaseInCubic), haptic Success, `sfx_gift` |
| Lên rank | `MMF_FreezeFrame` 0.12 s (`MMTimeManager`), `MMF_Flash` trắng, `MMF_Scale` huy hiệu, popup bật `EaseOutElastic`, haptic Success, `sfx_rankup` |
| Đổi tab / mở popup | `MMTween` EaseOutBack cho scale, `sfx_tab` |
| Nhạc nền | `MMSoundManager` track Music, loop |

Mọi `MMF_Player` chạy `TimescaleModes.Unscaled` cho UI để freeze-frame không làm đứng hình tween UI.

## 6. Asset

Danh sách, kích thước và tên file: [asset-brief.md](asset-brief.md) (giao cho codex). `GameAssetPostprocessor` tự đặt import: Sprite, không mipmap, 9-slice border 32 px cho `ui_panel`, `ui_button`, `ui_pill`.

## 7. Tiêu chí hoàn thành

- Project mở bằng Unity 6000.0.73f1 không lỗi biên dịch.
- Bấm Play ở `Main.unity`: chơi được toàn bộ vòng lặp tap → mua → rank → outfit, có lưu/khôi phục và thu nhập offline.
- Mọi sự kiện ở mục 5 có phản hồi Feel.
