# 03 — Kế hoạch triển khai

Trạng thái: ✅ xong · 🔶 một phần · ⬜ chưa làm. Cập nhật lần cuối: 2026-11-16.

## Giai đoạn 0 — Chuẩn bị
- ✅ Phân tích game tham chiếu → [01-game-analysis.md](01-game-analysis.md)
- ✅ Spec kỹ thuật và cân bằng → [02-spec.md](02-spec.md)
- ✅ Brief asset cho codex → [asset-brief.md](asset-brief.md)

## Giai đoạn 1 — Khung project
- ✅ Tạo project Unity 6000.0.73f1 (ProjectSettings + Packages lấy từ SandASMR để khớp phiên bản package)
- ✅ Sao chép Feel (MMFeedbacks, MMTools, NiceVibrations) và TextMesh Pro essentials
- ✅ `ProjectSetup` (Editor): sinh `Main.unity`, Build Settings, Player Settings dọc, scripting define cho NiceVibrations
- ✅ `GameAssetPostprocessor`: import sprite tự động

## Giai đoạn 2 — Lõi gameplay
- ✅ `GameConfig`: bảng gear / skill / decor / outfit / rank
- ✅ `GameState`: thu nhập, tap, hype, boost, rank, mua x1/x10/MAX
- ✅ `SaveData` + autosave + thu nhập offline
- ✅ `NumberFormatter`

## Giai đoạn 3 — UI
- ✅ `UIFactory` + `MainUI`: top bar, sân khấu, thanh hype, nút boost
- ✅ `UpgradePanel`: 4 tab, danh sách cuộn, trạng thái nút
- ✅ `ChatFeed`, `FloatingTextPool`, `GiftSpawner`, `Popups` (offline, rank up)

## Giai đoạn 4 — Game feel
- ✅ `FeelKit`: MMF_Player dựng bằng code cho tap, crit, mua, hype, rank up, quà
- ✅ Âm thanh qua `MMSoundManager`, rung qua NiceVibrations, freeze frame qua `MMTimeManager`

## Giai đoạn 5 — Asset
- ✅ Codex tạo 46 PNG + 10 WAV theo brief → [asset-report.md](asset-report.md)

## Giai đoạn 6 — Kiểm tra
- ✅ Biên dịch bằng Unity batchmode: không lỗi
- ✅ Smoke test Play Mode không giao diện (`SmokeTest.Run`): dựng UI, 60 lần tap, bấm mọi nút — 0 lỗi runtime
- ⬜ Chơi thử bằng tay trong Editor / trên máy thật để tinh chỉnh cân bằng, âm thanh và rung

## Giai đoạn 7 — Làm lại bố cục theo video gameplay
- ✅ Xem 60 khung hình từ 2 video YouTube → [04-video-analysis.md](04-video-analysis.md)
- ✅ Sân khấu toàn màn hình, HUD thẻ nổi sáng màu (thẻ hồ sơ có avatar, thẻ coin, gem, Boost)
- ✅ Thanh Hype dọc bên trái
- ✅ Thanh điều hướng đáy + bảng nâng cấp dạng bottom sheet trượt
- ✅ Dòng nâng cấp kiểu thẻ: nhãn LV, thanh tiến độ tới mốc ×2
- ✅ Hũ donation bấm để nhận (cơ chế mới, có lưu)
- ✅ Tim bay khi tap; popup và chat theo giao diện sáng
- ✅ Smoke test chụp 3 ảnh: [màn chính](screenshot-main.png), [sheet mở](screenshot-sheet.png), [sau khi bấm mọi nút](screenshot-all.png) — 0 lỗi runtime
- ⬜ Kiểm tra bố cục trên tỉ lệ khác 9:16 và máy có tai thỏ

## Việc để sau (ngoài phạm vi bản đầu)
- SDK quảng cáo/IAP thật (hiện Boost dùng cooldown)
- Prestige / reset để lấy hệ số vĩnh viễn
- Nhiệm vụ hằng ngày, thành tựu
- Hoạt ảnh nhân vật (Spine/khung hình) thay cho sprite tĩnh
- Nhân vật toàn thân và nhiều bối cảnh phòng (game tham chiếu có cả hai; hiện Nova là sprite bán thân, một phòng)
- Đa ngôn ngữ (hiện UI tiếng Anh)
