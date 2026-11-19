# 01 — Phân tích game tham chiếu & định hướng sản phẩm

Nguồn: trang Google Play `com.cscmobi.livestream.idle.clicker` (đọc ngày 2026-11-16), gồm mô tả, ảnh chụp màn hình và review người chơi.

## 1. Hồ sơ game tham chiếu

| Mục | Giá trị |
|---|---|
| Tên | Viral Streamer: Idle Clicker |
| Nhà phát hành | CSCMobi Studios |
| Thể loại | Simulation / Idle clicker, màn hình dọc |
| Chỉ số | 4.7★, ~22K review, 1M+ lượt tải, #5 top free simulation |
| Mô hình doanh thu | Quảng cáo (interstitial + rewarded) và IAP (gỡ quảng cáo, gói boost) |
| Cập nhật gần nhất | 11/11/2026 |

## 2. Vòng lặp gameplay (suy ra từ mô tả + ảnh)

1. **Chạm** vào nhân vật streamer để tăng tốc tiến độ (kiếm tiền / kéo người xem).
2. **Người xem** vào kênh và gửi **donate ảo** — đây là thu nhập thụ động.
3. Dùng coin để **nâng cấp**: thiết bị (gear), phòng (room), trang phục (outfit), boost thu nhập.
4. Nâng cấp làm tăng người xem và thu nhập → đạt **rank** mới → mở nội dung mới.
5. Lặp lại, với mục tiêu ngắn hạn luôn hiển thị ("clear goals").

Các hệ thống nhận diện được:

- **Tap**: thao tác chính, đơn giản một chạm.
- **Idle income**: thu nhập theo giây, kể cả khi không chạm.
- **Rank**: thang danh vọng "từ số 0 thành ngôi sao".
- **Room**: nội thất, thiết bị, đồ trang trí hiển thị trực tiếp trong phòng.
- **Outfit**: mở khoá và thay trang phục cho nhân vật.
- **Chat**: khung chat của người xem chạy trên màn hình ("Watch the chat").
- **Chế độ cao trào**: review nhắc tới trạng thái nhân vật "nhảy" tạm khoá thao tác khác — tương đương một fever/hype mode.
- **Boost**: nhân thu nhập có thời hạn, gắn với rewarded ad.

## 3. Điểm mạnh và điểm yếu (từ review)

**Mạnh**
- Ý tưởng dễ hiểu, hợp trend livestream.
- Art nhân vật được khen, tiến trình nâng cấp rõ ràng.
- Có cảnh báo trước khi hiện quảng cáo (đếm 3 giây) — người chơi đánh giá cao.

**Yếu**
- Quảng cáo ép buộc quá dày, kể cả ngay lúc mở game và ngay sau rewarded ad.
- Gói gỡ quảng cáo không gỡ hết → mất niềm tin.
- Grind nặng ở giai đoạn giữa.
- Trạng thái "nhảy" khoá UI, người chơi không thao tác được gì khác.

## 4. Định hướng sản phẩm của chúng ta

Làm một game **cùng thể loại và cùng cơ chế cốt lõi** (idle clicker chủ đề streamer), nhưng là sản phẩm **nguyên bản**: tên, nhân vật, art, âm thanh, số liệu cân bằng và bố cục UI đều tự thiết kế, không sao chép tài sản của game tham chiếu.

- **Tên tạm**: *Stream Star: Idle Tycoon*
- **Nhân vật**: "Nova" — streamer phong cách chibi, hoodie + tai nghe, tông thân thiện mọi lứa tuổi.
- **Khác biệt có chủ đích** (rút từ điểm yếu ở mục 3):
  - Không có quảng cáo ép buộc. Boost dùng **cooldown** thay cho rewarded ad (dễ thay bằng SDK ads sau này).
  - **Hype Mode không khoá UI** — vẫn mua nâng cấp được khi đang hype.
  - Game feel là trọng tâm: mọi thao tác đều có phản hồi hình + tiếng + rung qua **Feel**.
  - Nhịp tiến trình mượt hơn nhờ mốc x2 thu nhập theo level và thưởng gem khi lên rank.

## 5. Tóm tắt thiết kế (GDD rút gọn)

- **Màn hình**: một màn duy nhất, dọc 1080×1920. Trên: HUD. Giữa: sân khấu phòng stream. Dưới: bảng nâng cấp 4 tab.
- **Tiền tệ**: Coin (chính), Follower (đo tiến độ rank), Gem (hiếm, mua outfit).
- **Tap**: chạm Nova → coin + follower + nạp thanh Hype; có tỉ lệ chí mạng.
- **Hype Mode**: thanh đầy → ×3 thu nhập và tap trong vài giây.
- **Gear** (8 món): nguồn thu nhập/giây, giá tăng 15%/level, mốc level nhân đôi.
- **Skills** (5 kỹ năng): sức tap, engagement, chí mạng, thời lượng hype, giới hạn offline.
- **Room** (6 đồ decor): mua một lần, cộng % thu nhập và hiện trong phòng.
- **Outfits** (5 bộ): mua bằng gem, đổi ngoại hình và cộng bonus vĩnh viễn.
- **Rank** (9 bậc): theo follower; lên rank được gem + hệ số thu nhập.
- **Sự kiện**: hộp quà "Super Chat" bay ngang, chạm để nhận thưởng lớn.
- **Boost**: ×2 thu nhập 60 giây, hồi 5 phút.
- **Offline**: nhận 50% thu nhập trong thời gian vắng mặt, tối đa 2 giờ (nâng được).

Chi tiết số liệu và kỹ thuật ở [02-spec.md](02-spec.md); lộ trình ở [03-plan.md](03-plan.md).
