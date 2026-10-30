# Cấu trúc thư mục & đặt tên

> **Đọc khi:** tạo file/thư mục mới, tìm chỗ đặt asset hoặc script.
> **Cập nhật khi:** thêm thư mục cấp 1–2 dưới `Assets/` hoặc đổi quy ước đặt tên.

## Hiện tại

```
Assets/
  Scenes/      SampleScene.unity (scene mẫu)
  Settings/    URP, Renderer2D, Input actions (do template sinh ra)
```

## Quy ước (đã duyệt, áp dụng khi thêm nội dung)

```
Assets/
  _Project/            toàn bộ nội dung của game (tách khỏi asset bên thứ ba)
    Core/              lõi dùng chung: ví, save, ServiceLocator, kiểu dữ liệu cơ sở
    Characters/        định nghĩa nhân vật, skill, Roster
    Gacha/  Restaurant/  Combat/  UI/
    Boot/              khởi động game, nối service
    Editor/            công cụ chỉ chạy trong Editor
    Scenes/
    Tests/             EditMode tests
  Settings/            giữ nguyên
```

- Chia theo **tính năng**, không theo loại. Mỗi folder tính năng có các folder con `Scripts/`, `Data/` (asset SO), `Art/`, `Animations/`, `Prefabs/`.
- Mỗi tính năng là một assembly `BunDauPeo.<Tên>`. File `.asmdef` tạo cùng script đầu tiên, vì asmdef không có script sẽ sinh cảnh báo.
- Namespace trùng tên assembly, riêng `Editor` dùng `BunDauPeo.EditorTools` để không che lớp `UnityEditor.Editor`.
- Tên file C# = tên class chính (bắt buộc với MonoBehaviour).
- Prefab/asset: `PascalCase`, không dấu cách. Sprite: `Tên_trạng_thái_frame` (ví dụ `Player_Run_01`).

## Luật phụ thuộc (asmdef thực thi)

```
Core  <-  Characters  <-  Gacha, Restaurant, Combat  <-  UI, Boot
```

- `Gacha`, `Restaurant`, `Combat` **không phụ thuộc lẫn nhau**; dùng chung qua `Wallet`, `Roster`, `PlayerSave`.
- `UI` và `Boot` được tham chiếu mọi tính năng. `Editor` và `Tests` chỉ chạy trong Editor.
- Một SO chỉ được trỏ tới SO cùng assembly hoặc ở tầng dưới.
- Logic là class C# thuần nhận phụ thuộc qua constructor. Chỉ MonoBehaviour view mới dùng `ServiceLocator`.

Lý do: [D-002](../decisions/decisions.md).
