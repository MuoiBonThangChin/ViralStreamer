# 04 — Phân tích video gameplay (60 khung hình)

Ngày phân tích: 2026-11-16.

## Nguồn

| Video | Kênh | Độ dài | Khung hình lấy |
|---|---|---|---|
| [Viral Streamer: Idle Clicker - Gameplay Walkthrough Part 1](https://www.youtube.com/watch?v=CZQyK5Fu4rQ) | Kru Mobile | 9:08 | 55 khung, cách nhau ~9 giây (t = 4s … 532s) |
| [Gameplay Walkthrough Part 5](https://www.youtube.com/watch?v=USnGTFhuphg) | Kru Mobile | 12:00 | 5 khung (t = 58s, 180s, 302s, 432s, 561s) |

Cách làm: tua video tới từng mốc thời gian trong trình duyệt, vẽ khung hình lên một lưới rồi đọc từng lưới. Không tải video về máy.

Phân bố 60 khung hình:

| Nội dung | Số khung | Mốc thời gian (Part 1) |
|---|---|---|
| Hội thoại mở đầu (trợ lý điện thoại "Siciee") | 2 | 4s, 13s |
| Màn chính khi đang tap | 21 | 22–77s, 250–532s |
| Màn chính khi đứng yên (thấy đủ HUD) | 6 | 50s, 59s, 341s, 414s, 450s, 486s |
| Bảng "House Upgrade" đang mở | 8 | 259s, 313s, 377s, 423s, 468s, 514s, 532s |
| Popup (Claim Donation, Auto click, Big Money Pack) | 5 | 277s, 286s, 304s, 359s, 505s |
| Quảng cáo video chen giữa (không phải gameplay) | 17 | 86s–241s |
| Giai đoạn sau, phòng học (Part 5) | 5 | — |

Đáng chú ý: trong 9 phút của Part 1 có một quảng cáo dài khoảng 2,5 phút (17/55 khung hình). Điều này khớp với phàn nàn về quảng cáo trong review ở [01-game-analysis.md](01-game-analysis.md).

## Bố cục màn chính của game tham chiếu

```
┌──────────────────────────────────────┐
│ [avatar  tên        ]     [vé    3 ] ⚙│  thẻ hồ sơ trái · tiền tệ phải
│ [        Rank 1     ]     [xu 0.09/s] 🚪│
│ [        👥 1/12k ▓░]               ADS│  cột nút tròn bên phải
│ 👁 1                                 💰│
│ ▌                                    💰│
│ ▌ thanh hype          NHÂN VẬT         │  nhân vật toàn thân, giữa màn hình
│ ▌ dọc, bên trái       (toàn thân)      │  phòng chiếm trọn màn hình
│ ▌                                      │
│ (Claim Donation 0,84)                  │  nút xanh, tích luỹ dần
│ ┌ khung chat mờ, 2 dòng ─────────────┐ │
│ └────────────────────────────────────┘ │
│  [👗]    [🔨]    [$]    [👆]           │  4 nút điều hướng
└──────────────────────────────────────┘
```

Những điểm rút ra từ khung hình:

1. **Phòng và nhân vật là trung tâm.** Nền phòng phủ toàn màn hình; HUD là các thẻ nhỏ nổi lên trên, không có thanh nào che ngang.
2. **HUD sáng màu.** Thẻ trắng/tím nhạt bo tròn, chữ tím đậm; nút hành động chính màu xanh lá, tiêu đề màu tím.
3. **Thẻ hồ sơ góc trái trên** gom avatar, tên, rank và thanh follower (ví dụ `44,2k/200k`) vào một chỗ.
4. **Tiền tệ góc phải trên** là 2 viên thuốc xếp dọc; thu nhập hiển thị dạng `4,90/s`.
5. **Thanh hype dọc bên trái**, đầy dần từ dưới lên, có ngọn lửa ở đỉnh. Khi đầy: nhân vật nhảy, màn hình phủ các đốm sáng màu.
6. **Tap** sinh ra tim đỏ, emoji và xu bay lên quanh nhân vật.
7. **Bảng nâng cấp là bottom sheet** chỉ mở khi bấm nút điều hướng, cao khoảng nửa màn hình, có nút X và công tắc "Max". Nhân vật vẫn thấy ở nửa trên và vẫn tap được.
8. **Mỗi dòng nâng cấp** gồm: ảnh vật phẩm, nhãn `LV19`, tên, 2 dòng chỉ số (`+96% Click Income`, `+48% Passive Income`), thanh tiến độ và nút giá màu xanh.
9. **"Claim Donation"** là nút xanh phía trên chat, số tiền tăng dần và người chơi bấm để nhận.
10. **Popup** thẻ sáng, tiêu đề tím nằm đè lên mép trên, nút X ở góc.
11. **Mua nội thất làm phòng thay đổi** rõ rệt (phòng trống → có sofa, bàn, kệ sách, máy tính); Part 5 chuyển sang bối cảnh khác (phòng học).

## So sánh với bản Stream Star trước khi sửa

| Hạng mục | Game tham chiếu | Stream Star (trước) | Quyết định |
|---|---|---|---|
| Diện tích sân khấu | ~100% màn hình | ~44% (top bar 280 + panel đáy 800 trên 1920) | Sân khấu phủ toàn màn hình |
| HUD | Thẻ nổi, sáng | Thanh navy đặc che ngang | Thẻ nổi trắng, chữ tím đậm |
| Bảng nâng cấp | Sheet mở/đóng | Luôn hiện, chiếm 42% | Sheet trượt lên từ thanh điều hướng |
| Thanh hype | Dọc, bên trái | Ngang, sát panel | Dọc bên trái, có ngọn lửa |
| Thông tin rank | Thẻ hồ sơ có avatar | Viên thuốc + thanh riêng | Thẻ hồ sơ có avatar theo trang phục |
| Dòng nâng cấp | Thẻ trắng, nhãn LV, thanh tiến độ | Dòng tối, không có tiến độ | Thẻ trắng, nhãn LV, thanh tới mốc x2 |
| Donation | Tích luỹ, bấm để nhận | Chỉ là chữ trong chat | Thêm hũ donation bấm để nhận |
| Chat | Khung mờ phía trên nav | Chữ trần trên nền | Khung mờ bo tròn |

Không sao chép: tên, nhân vật, hình vẽ, bối cảnh, câu chữ, số liệu và mọi cơ chế quảng cáo của game gốc. Stream Star giữ nhân vật Nova, art riêng, và Boost vẫn dùng cooldown thay cho quảng cáo.
