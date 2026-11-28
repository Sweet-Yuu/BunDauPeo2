# Kiểm dung lượng và hiệu năng

> **Đọc khi:** kênh 🔨 làm phiên cuối của một PR có scene/UI/asset mới, khi build Android hoặc WebGL, khi Leader review cuối nhánh.
> **Cập nhật khi:** user đổi ngưỡng, hoặc đo xong (ghi vào Sổ đo). User yêu cầu 2026-11-16: phải kiểm bằng Profiler để đề xuất tối ưu sớm, tránh bản build nặng hoặc lag.

## Ngưỡng (CEO đề xuất 2026-11-16, user chỉnh được)

Máy chuẩn: Android tầm trung, RAM 3–4 GB.

| Đo gì | Ngưỡng | Đo bằng |
|---|---|---|
| Dung lượng AAB tải về (CH Play) | ≤ 150 MB (Play chặn base > 200 MB) | kích thước file sau `BuildRelease`, mục Build Report trong `Editor.log` |
| Dung lượng WebGL (itch.io) | ≤ 50 MB nén | thư mục build |
| Tăng dung lượng giữa hai lần build | ≤ 10%, quá thì tìm asset gây tăng | Sổ đo bên dưới |
| Thời gian khung, màn đứng yên (Lobby, Restaurant) | ≤ 16,7 ms trong Editor; ≥ 30 fps ổn định trên máy chuẩn | Profiler CPU, main thread |
| Thời gian khung, trận đấu và Gacha reveal | ≤ 16,7 ms trong Editor; không khung nào > 50 ms trên máy chuẩn | Profiler CPU |
| GC alloc mỗi khung | 0 B khi đứng yên; trung bình ≤ 1 KB trong trận; không lần GC > 5 ms | Profiler CPU, cột GC Alloc |
| Bộ nhớ tổng trên máy chuẩn | ≤ 600 MB | Profiler Memory |
| Batches mỗi màn UI | ≤ 100 | Profiler Rendering, Frame Debugger |
| Mở app tới màn Tiêu đề / chuyển scene | ≤ 5 s / ≤ 3 s | đồng hồ, log thời gian |

## Khi nào đo, ai đo

1. **Phiên 🔨 cuối của mỗi PR có scene, UI hoặc asset mới:** đo trong Editor (Play, Profiler qua MCP `manage_profiler` hoặc cửa sổ Profiler) các màn đã đụng, mỗi màn ~10 s. Ghi dòng "Hiệu năng" trong báo cáo cuối kênh. Cần khoá Unity như mọi lần chạy Unity.
2. **Mỗi lần build Android hoặc WebGL:** kênh build ghi dung lượng vào Sổ đo.
3. **Trước thử kín CH Play (≤ 10/12) và trước công khai:** một phiên 🔨 "Kiểm hiệu năng" trong luồng 🚀 Phát hành, đo trên điện thoại thật (Development Build, Profiler nối máy). Cần điện thoại của user (U9).
4. **Vượt ngưỡng:** kênh đo ghi rõ màn nào, số đo, nghi do đâu. Leader đề xuất sửa trong luồng; nếu cần đổi kế hoạch hoặc ảnh hưởng nhiều luồng thì 👑 CEO đề xuất với user. Không tự tối ưu ngoài phạm vi task.

## Lỗi hay gặp cần soi trước

- `Update` tạo chuỗi, list, closure, LINQ mỗi khung (GC); `GetComponent`/`Find` mỗi khung.
- Canvas lớn bị dựng lại cả khi một chữ đổi: tách canvas động khỏi tĩnh.
- Texture không nén, không Max Size hợp lý, sprite chưa vào atlas (skill `unity:manage-sprite-atlas`).
- Âm thanh Load Type sai (skill `unity:optimize-audio`), font TMP atlas quá lớn (skill `unity:optimize-text-mesh-pro`).
- Package không dùng vẫn nằm trong build.

## Sổ đo (mới nhất trên cùng)

| Ngày | Bản / màn | Dung lượng | Khung (ms) | GC/khung | Bộ nhớ | Ghi chú |
|---|---|---|---|---|---|---|
| 2026-11-28 | `feature/weather` sau Task 4, Lobby lúc mưa, Editor Play (`ProfilerRecorder`, ~300 khung, so với quang) | chưa đo | Main Thread 4,99 ms trung bình (quang 5,01 ms) | trung vị 2519 B lúc mưa, 2643 B lúc quang (nền của Editor đang Play có cầu nối lệnh, cao hơn `GcMeasureTests`) | chưa đo | Máy dev Windows. Mưa không thêm GC hay thời gian so với quang; `WeatherView.Refresh` chỉ cấp phát khi số giây đổi |
| 2026-11-26 | `feature/online-4` sau O4-b, Editor batch (`GcMeasureTests`, 300 khung) | chưa đo | chưa đo | Hộp thư **20 thư quà** đứng yên 892 B (worst 948 B); Nhiệm vụ 892 B; Restaurant 650 B; khung trống của Editor 822 B | chưa đo | Máy dev Windows. Hộp thư 20 quà bằng trang Nhiệm vụ, trong ngưỡng 1 KB. Chưa đo thời gian khung (ms), dung lượng build, hay Android thật. |
| 2026-11-24 | `feature/character-level` sau Task 8, Editor batch (`GcMeasureTests`, 300 khung) | chưa đo | chưa đo | Characters danh sách 892 B, Characters chi tiết 1024 B (một khung đỉnh 12 KB lúc mở panel), Restaurant 782 B, Trận AUTO 692 B (khung trống của Editor: 822 B) | chưa đo | Máy dev Windows. Màn Nhân vật cao hơn nền Editor khoảng 70 B (danh sách), trong ngưỡng 1 KB trung bình; chưa đo trên Android thật. |
| 2026-11-18 | `feature/online-2` sau O2-d, Editor batch (`GcMeasureTests`, `ProfilerRecorder` "GC Allocated In Frame", 300 khung) | chưa đo | chưa đo | Restaurant 570–667 B, Bản đồ màn 812–892 B, Trận AUTO 612–692 B (khung trống của Editor: 742–822 B; hai lần chạy) | chưa đo | Máy dev Windows, không phải máy chuẩn. Trước sửa: Restaurant 1371, Bản đồ 1596, Trận 1092 B. Phần của code giờ ≈ 0 B ở ba màn; còn lại là nền Editor. Chưa đo trên Android thật (cần điện thoại). |
| | | | | | | (hàng trên là lần đo đầu) |
