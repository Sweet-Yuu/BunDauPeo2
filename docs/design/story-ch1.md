# Cốt truyện Chương 1 (bản viết của user)

> ⚠ **Lỗi thời 2026-11-26:** kịch bản Hợp chất X/tôm này bị thay bởi cốt truyện mới ở [story-core.md](story-core.md). Giữ lại làm tham khảo định dạng bảng thoại; **không nạp vào game**.

> **Đọc khi:** viết hoặc sửa lời thoại, tên nhân vật, thêm đoạn truyện. 🔨 đọc file này để nạp nội dung vào game.
> **Cập nhật khi:** user sửa chữ. Chỉ **user** quyết nội dung; 🎨 chỉ sửa cấu trúc khi user nhờ.
> Cấu trúc và luật hiển thị: [spec Cốt truyện Ch1](../superpowers/specs/2026-11-21-story-ch1-design.md). Bản dưới là **nháp của 🎨**, sửa đè lên trực tiếp.

## Cách viết

- Mỗi câu một dòng trong bảng: **Người nói | Bên | Tiếng Việt | English**.
- **Người nói**: tên trong mục "Nhân vật" bên dưới; để trống (`—`) = lời dẫn (không có tên, không có ô hình).
- **Bên**: `Trái` hoặc `Phải` (ô người nói hiện bên đó; người kia mờ đi).
- Mỗi câu nên ≤ 3 dòng chữ trên màn (khoảng 120 ký tự). Một đoạn nên ≤ 8 câu (thoại trước trận thì càng ngắn càng tốt vì thể lực đã trừ).
- Cần đoạn mới hoặc đoạn đặt ở chỗ khác: thêm mục theo mẫu cuối file và ghi **chiếu ở đâu**. Số đoạn và vị trí đã chốt trong spec (mục 3, 9); muốn thêm thì báo 🧭 Leader/🎨 để cập nhật kế hoạch.
- Thiếu bản tiếng Anh thì để `TODO`; 🔨 sẽ không nạp được đoạn còn `TODO` (game cần cả hai ngôn ngữ).

## Thế giới và giọng điệu

- **Bối cảnh:** TODO(user) — thêm vài dòng (thành phố, quán ở đâu, thời đại...).
- **Giọng điệu:** hài, nhẹ (nháp). TODO(user) xác nhận.
- **Hợp chất X:** chất lạ lẫn vào lô mắm tôm, làm tôm nổi loạn. Ai bỏ vào: TODO(user), hiện chỉ gợi để dành Chương 2.

## Nhân vật (người nói)

| Tên (Việt / Anh) | Vai | Bên mặc định | Màu ô tạm | Ghi chú |
|---|---|---|---|---|
| Péo / Péo | Chủ quán bún đậu, dẫn truyện | Trái | vàng | TODO(user): tính cách, cách xưng hô |
| Tép Nhảy / Leaping Shrimplet | Tôm nhỏ, nhanh, thành viên phe địch | Phải | xanh lam | |
| Tôm Chúa / Shrimp Lord | Boss Chương 1 | Phải | đỏ | |
| TODO | | | | thêm người nói mới ở đây |

Tên quái trong trận (chỉ số giữ nguyên): Tép Nhảy, Tôm Sú (Tiger Prawn), Tôm Càng (Claw Shrimp), Tôm Chúa (Shrimp Lord).

## Các đoạn truyện

### 1. `story_1_open` — Truyện 1: "Mắm tôm dậy mùi" / "Shrimp Paste Rising"
Chiếu ở: nút Truyện đầu bản đồ, **trước màn 1-1**. Clear xong mở 1-1.

| Người nói | Bên | Tiếng Việt | English |
|---|---|---|---|
| Péo | Trái | Chào buổi sáng! Hôm nay nhập lô mắm tôm mới, thơm nức cả phố. | Good morning! A fresh batch of shrimp paste, the whole street can smell it. |
| Péo | Trái | Ủa? Sao hũ mắm sủi bọt xanh lè thế này? | Huh? Why is the jar bubbling green? |
| Tép Nhảy | Phải | Xì xì... mắm... mắm... | Hiss... paste... paste... |
| Péo | Trái | Trời ơi, tôm bò ra khỏi hũ! Còn biết nói nữa?! | The shrimp are crawling out of the jar! And they talk?! |
| Tép Nhảy | Phải | Trả mắm cho bọn ta! | Give us back our paste! |
| Péo | Trái | Mắm của tôi mà... Thôi, nhân viên đâu, ra tiếp khách đặc biệt! | It's MY paste... Fine. Staff, serve our special guests! |

### 2. `story_1_x` — Truyện 2: "Hợp chất X" / "Compound X"
Chiếu ở: nút Truyện thứ hai, **sau 1-2, trước 1-3**.

| Người nói | Bên | Tiếng Việt | English |
|---|---|---|---|
| Péo | Trái | Mấy con tôm này mạnh hơn hẳn lúc nãy. | These shrimp are much stronger than before. |
| Péo | Trái | Hũ mắm có dán nhãn lạ: "Hợp chất X". Ai đã trộn thứ này vào? | The jar has a strange label: "Compound X". Who mixed this in? |
| Tép Nhảy | Phải | Hợp chất X... làm bọn ta tỉnh dậy. Tôm Chúa đang gọi. | Compound X... woke us up. Lord Shrimp is calling. |
| Péo | Trái | Tôm Chúa? Nghe tên đã thấy mặn rồi. Đi thôi! | Shrimp Lord? Sounds salty already. Let's go! |

### 3. `story_1_boss_before` — "Chum mắm của Tôm Chúa" / "The Lord's Paste Vat"
Chiếu ở: **đầu trận 1-5** (sau khi vào trận, thể lực đã trừ), tự chiếu một lần.

| Người nói | Bên | Tiếng Việt | English |
|---|---|---|---|
| Tôm Chúa | Phải | Ai dám bén mảng đến chum mắm của ta? | Who dares approach my vat of paste? |
| Péo | Trái | Mắm của tôi! Trả đây! | That's MY paste! Give it back! |
| Tôm Chúa | Phải | Hợp chất X cho ta sức mạnh. Ngươi chỉ là người bán bún! | Compound X gave me power. You are just a noodle seller! |
| Péo | Trái | Bán bún thì sao? Cả đội, lên! | So what if I sell noodles? Team, charge! |

### 4. `story_1_boss_after` — "Hũ mắm sạch lại" / "A Clean Jar"
Chiếu ở: **sau khi thắng 1-5**, khi bấm Tiếp tục ở màn kết quả (thưởng đã nhận). Kết Chương 1.

| Người nói | Bên | Tiếng Việt | English |
|---|---|---|---|
| Tôm Chúa | Phải | Ta... thua... một quán bún đậu sao... | I... lost... to a tofu-and-noodle shop... |
| — | — | Hợp chất X tan khỏi mắm tôm, hũ mắm trong trở lại. | Compound X faded from the paste, and the jar turned clear again. |
| Péo | Trái | Xong rồi... Nhưng ai đã bỏ thứ đó vào mắm của mình? | It's over... But who put that in my paste? |
| — | — | Hết Chương 1. | End of Chapter 1. |

## Mẫu đoạn mới (chép xuống dưới để thêm)

### N. `story_1_<tên_ngắn>` — "<Tiêu đề Việt>" / "<English title>"
Chiếu ở: TODO (màn nào, trước hay sau; Truyện hay trận đánh)

| Người nói | Bên | Tiếng Việt | English |
|---|---|---|---|
| | | | |

## Ghi chú / ý để sau

- TODO(user): ý cho Chương 2, kẻ đứng sau Hợp chất X, thêm nhân vật...
