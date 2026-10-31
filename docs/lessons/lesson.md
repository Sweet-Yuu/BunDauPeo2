# Lessons — sổ bài học

> **Đọc khi:** trước khi sửa code/asset, **chỉ phần có tag liên quan** (dùng `Grep "#tag" docs/lessons/lesson.md`, đừng đọc cả file).
> **Cập nhật khi:** agent làm sai, user chỉnh lại, hoặc phát hiện một cái bẫy của dự án. Ghi ngay trong task đó.

## Tag dùng chung

`#unity-files` `#scene-prefab` `#script` `#physics` `#input` `#ui` `#art-import` `#build` `#git` `#docs` `#process`
(Thêm tag mới ở đây trước khi dùng.)

## Cách ghi

- Thêm mục mới ở **đầu** danh sách (mới nhất trên cùng).
- Viết thành **luật hành động được**, không kể lể. Một mục ≤ 6 dòng.
- Lesson lặp lại ≥ 2 lần → nâng thành luật trong `guides/` hoặc `CLAUDE.md`, rồi ghi `Promoted to: <file>` vào mục.

## Mẫu

```
### L-NNN — Tiêu đề ngắn (YYYY-MM-DD)  #tag1 #tag2
- Triệu chứng: điều đã xảy ra sai
- Nguyên nhân gốc:
- Luật từ nay: làm X / không làm Y
```

## Danh sách

### L-004 — Kết luận cơ chế khi chưa tách biến (2026-10-31)  #process #unity-files
- Triệu chứng: sau vài lần Play qua MCP, file save lệch số so với bộ nhớ; tôi báo "biên dịch lệnh làm dừng Play", rồi "dừng Play không lưu", cả hai đều sai.
- Nguyên nhân gốc: công cụ `RunCommand` tự thoát Play khi mã lệnh chứa `ExitPlaymode`/`isPlaying = false`, nên phép thử "trừ tiền rồi dừng" chạy trên session đã chết; tôi kết luận trước khi đọc mã của công cụ.
- Luật từ nay: khi hành vi của công cụ lạ, đọc mã nguồn của công cụ (`Library/PackageCache`) trước, rồi mới đặt giả thuyết; mỗi phép thử chỉ đổi một thứ, dừng Play bằng lệnh riêng.

### L-003 — Code tiền/save nuốt lỗi IO và bắn event trần (2026-10-31)  #script
- Triệu chứng: reviewer chỉ ra `SaveService.Load` coi file bị khoá như save hỏng (game mới ghi đè save thật), và `Changed?.Invoke()` để handler lỗi làm `TrySpend` ném lỗi sau khi tiền đã trừ.
- Nguyên nhân gốc: kế hoạch và test chỉ có đầu vào "hỏng/rỗng", không có "tạm thời không đọc được"; event bắn thẳng sau khi đổi trạng thái.
- Luật từ nay: lỗi IO không được đồng nhất với dữ liệu hỏng, để nó nổi lên thay vì trả `null`; mọi `event` bắn sau khi đã đổi trạng thái dùng `SafeInvoke()` (Core). Test phải có ca "tài nguyên đang bị khoá" và "handler ném lỗi".

### L-002 — Tham chiếu asset giữ qua `NewScene(Single)` thành null (2026-10-30)  #unity-files #scene-prefab #script
- Triệu chứng: `ProjectSetup` sinh `Boot.unity` có `_catalog`/`_newGameConfig` trống; test `BootScene_IsFirstInBuild_AndBootstrapIsWired` đỏ. Lần chạy đầu còn lỗi "Cannot create a new scene additively with an untitled scene unsaved" ở batch mode.
- Nguyên nhân gốc: kế hoạch dùng `NewScene(Additive)`, batch mode buộc dùng `Single`; `Single` dỡ asset không được scene nào dùng nên biến C# đang giữ thành đối tượng đã huỷ.
- Luật từ nay: trong code Editor, nạp asset bằng `AssetDatabase.LoadAssetAtPath` **sau** khi tạo/đổi scene, không giữ biến từ trước. Sinh scene xong phải có test đọc lại tham chiếu từ file.

### L-001 — Spec tự vi phạm luật phụ thuộc assembly (2026-10-30)  #process #script
- Triệu chứng: spec nền móng đặt `StageDefinition` (Progression) trỏ tới `EnemyDefinition` (Combat) trong khi cấm hai assembly này phụ thuộc nhau; thiếu assembly Editor, thời điểm lưu save.
- Nguyên nhân gốc: viết bảng SO theo từng mảng, không kiểm từng tham chiếu với sơ đồ phụ thuộc.
- Luật từ nay: trước khi chốt spec kiến trúc, đối chiếu từng tham chiếu giữa các SO với sơ đồ assembly; code dùng `UnityEditor` phải nằm trong assembly Editor; save phải nói rõ khi nào ghi và ghi thế nào để không mất dữ liệu.
