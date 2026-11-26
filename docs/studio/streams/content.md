# 🍜 Luồng Nội dung game

> **Đọc khi:** mở 🧭 Leader Nội dung game hoặc kênh con của luồng này.
> **Cập nhật khi:** mở/xong kênh con, user chốt, PR merge. Chỉ Leader Nội dung game ghi.

## Bây giờ

- **Phạm vi:** các mục gameplay trong roadmap "Tiếp theo": Thời tiết, Shipper + nấu ăn, Nội thất lưới 8×8, Cốt truyện Chương 1; và sổ ý tưởng `docs/design/ideas.md` (ví dụ giá nâng cấp quán bằng Material, nhân vật bằng Coin).
- **Leader:** 🧭 Nội dung game #1 (`local_95aae363-67d6-40fa-8849-673dbe1f854c`). Giai đoạn này chỉ viết spec (không cần khoá Unity); xin CEO khoá trước khi mở 🔨.
- **Thứ tự spec (user đổi 2026-11-21, qua CEO):** Thời tiết → Cốt truyện Ch1 → **Vẽ lại quán bằng tilemap + Nội thất 8×8 (một spec chung; hình quán cũ cất vào `_Old/` cạnh nó, không xoá)** → Shipper + nấu ăn. Trễ thì cắt Shipper trước.
- **Spec xong:** Thời tiết (`docs/thoi-tiet-spec`, `4abe294`), Cốt truyện Ch1 (`docs/cot-truyen-spec`, `aeeb9b3`). Cả hai chờ PR #22 merge + khoá Unity; làm 🔨 lần lượt vì chung `PlayerSave`/`ProjectSetup`.
- **Spec xong 2026-11-23:** Vẽ lại quán tilemap + Nội thất (`docs/tilemap-noi-that-spec`, chờ 📋 commit): spec `2026-11-23-restaurant-tilemap-furniture-design.md`, kế hoạch cùng tên trong `plans/`; 4 task tối thiểu + 2 đầy đủ, 3 phiên 🔨, ~+60 EditMode +2 PlayMode. Sàn thoi 8×8 Tilemap + camera thế giới (đổi quyết định 01/11, user chốt), nội thất mua bằng Coin, điểm Trang trí 40/40/20 chỉ ảnh hưởng AFK, nút Quản lý gộp Nhân viên + Nâng cấp + Nội thất, `PlayerSave.Furniture`; server/tiền không đổi. Cắt khi trễ: Bộ cài sẵn → zoom → chấm báo → DEV → hộp Cất tất cả. 🔨 làm sau Thời tiết và Cốt truyện (chung `PlayerSave`/`ProjectSetup`/`RestaurantSceneBuilder`); Task 3 có spike tile thoi từ hình vuông có sẵn, không ra thì xin ảnh user. Chưa có review bằng subagent.
- **(cũ) Spec kế:** Vẽ lại quán + Nội thất. Luật cứng: tile tạm chỉ dùng hình có sẵn của Unity hoặc ảnh user gửi; 🎨 xin ảnh tham khảo tilemap nếu cần. Luật `_Old/` (user chốt 2026-11-21, `roles/designer.md` mục "Hình placeholder và thay ảnh"): chuyển bằng Unity để `.meta` đi theo; object trong scene thì lưu prefab vào `_Old/` rồi xoá khỏi scene; vẽ lại từ đầu, scene/prefab đang dùng không trỏ vào `_Old/`. Lưu ý lưới thoi ngược bố cục quán đã chốt 01/11 nên spec phải xử lý điểm này.
- **Designer cũ:** 🎨 Game designer (`local_e53e2e08-d782-488d-bfd6-efd6c42a0b13`) vẫn giữ `docs/design/ideas.md`, `screen-flow.md`.

## Nhật ký

- 2026-11-16 — Lập luồng.
- 2026-11-21 — Mở Leader #1; user chốt thứ tự; mở 🎨 Spec Thời tiết.
- 2026-11-21 — Spec + kế hoạch Thời tiết xong, user duyệt (nhánh `docs/thoi-tiet-spec`, chờ 📋 commit): 4 task, 2 phiên 🔨, +39 EditMode. Trạng thái ở `PlayerSave.Weather`, không đổi server. 🔨 làm sau khi PR #22 merge, cần khoá Unity. Kế: spec Cốt truyện Ch1.
- 2026-11-21 — Spec + kế hoạch Cốt truyện Ch1 xong, user duyệt (nhánh `docs/cot-truyen-spec`, chờ 📋 commit): 7 task, 3 phiên 🔨, +72 EditMode +1 PlayMode. Péo chủ quán, Hợp chất X trong mắm tôm, boss Tôm Chúa. Thoại trong ScriptableObject, cờ `PlayerSave.SeenStoryScenes`; server/tiền không đổi. Cắt khi trễ: Task 6 → 5 → 4. Chung `PlayerSave`/`ProjectSetup` với Thời tiết nên 🔨 làm lần lượt, sau PR #22 + khoá Unity. TODO(user): `vision.md` còn ghi "Cốt truyện sau MVP"; câu "đội nhân vật tôm của Péo" ở `release/drafts.md` lệch với "đội ta không phải tôm". Kế: Shipper + nấu ăn.
- 2026-11-26 — User đổi hẳn cốt truyện (báo qua 🎨 Cốt truyện): spec + kế hoạch Ch1 cũ (`aeeb9b3`) **TẠM DỪNG, không giao 🔨**. Mới: Péo là người chuyển sinh, thế giới Threat City, cuối game chọn tha thứ hoặc căm thù; tóm tắt ở `docs/design/story-core.md`, bản đầy đủ ở Google Doc "Cốt truyện chung" của user. Còn dùng được: hệ thống hội thoại (mục 3–6 spec cũ: ScriptableObject, `SeenStoryScenes`, màn hội thoại, Log/Auto, ô Cốt truyện). Huỷ: đổi quái sang tôm, kịch bản Hợp chất X. Chờ user chia chương rồi 🎨 viết lại spec/kế hoạch. TODO(user): `vision.md` còn "Cốt truyện sau MVP"; đổi tên Zola/Y/TikTak xa thương hiệu thật. Hàng khoá Unity: mục Cốt truyện coi như chưa sẵn sàng, nhắn CEO.
- 2026-11-25 — User chốt thứ tự khoá Unity (qua CEO): Cấp nhân vật → Online đợt 4 Hộp quà (2 phiên) → Thời tiết → Cốt truyện → Vẽ lại quán. Thời tiết lùi ~2 phiên; nhánh code Thời tiết cắt từ `main` sau khi đợt 4 merge. Spec Shipper chưa mở, chờ Online O3 xong (user chốt 2026-11-23); luồng nghỉ tới lượt khoá.
- 2026-11-23 — Spec + kế hoạch Vẽ lại quán + Nội thất xong, user duyệt (`docs/tilemap-noi-that-spec`). Kế: Shipper + nấu ăn khi user bảo mở.
- 2026-11-21 — User đổi thứ tự (qua CEO): spec kế là Vẽ lại quán tilemap + Nội thất 8×8 gộp một, Shipper xuống cuối và là mục cắt đầu tiên.
