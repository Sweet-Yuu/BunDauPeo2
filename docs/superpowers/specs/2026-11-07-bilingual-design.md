# Spec: Song ngữ Việt–Anh

> Ngày: 2026-11-07 · Trạng thái: **đã duyệt** (user, 2026-11-07, cả thiết kế lẫn bản viết) · Phạm vi: chọn ngôn ngữ (Core, Boot, `PlayerSave`), bảng chữ `UiText` cho mọi chữ người chơi thấy, tên tiếng Anh cho ScriptableObject, nút đổi ngôn ngữ trong panel Cài đặt DEV, test chặn chữ viết cứng.
> Nguồn: [roadmap](../../roadmap.md) (dòng "Song ngữ Việt–Anh", user chốt 2026-11-03), [D-005](../../decisions/decisions.md) (save, reset), [D-007](../../decisions/decisions.md) (Android trước), [unity-workflow.md](../../guides/unity-workflow.md) (UI và hình tạm). Kế hoạch: [plans/2026-11-07-bilingual.md](../plans/2026-11-07-bilingual.md).
> **Đã xong, đây là lịch sử.** Khi spec khác code, [D-012](../../decisions/decisions.md) hoặc [code-style.md](../../guides/code-style.md), những nơi đó thắng; không sửa spec này để theo kịp. Khác biệt đã biết: Reset đưa ngôn ngữ về theo máy ngay trong `GameSession.ResetToDefaults` (sửa N1 sau review), không qua `GameBootstrap` nghe `ResetCompleted`.

## 1. Mục tiêu

Mọi chữ người chơi thấy trong game đều có bản tiếng Việt và bản tiếng Anh, và người chơi đổi được ngôn ngữ. Làm ngay bây giờ vì mỗi màn mới làm thêm đều phải sửa lại chữ. Từ đây về sau, màn mới viết chữ thẳng vào bảng.

Thành công khi:
1. Chọn English trong panel Cài đặt DEV thì màn đang mở (trừ Battle, mục 6) vẽ lại toàn bộ bằng tiếng Anh: nhãn nút, dòng trạng thái, tên nhân vật, món, quái, màn, nâng cấp.
2. Tắt game rồi mở lại thì vẫn giữ ngôn ngữ đã chọn. Reset Save Data xoá lựa chọn này, và game quay về ngôn ngữ theo máy.
3. Lần đầu mở game: máy đặt tiếng Việt thì dùng tiếng Việt, mọi ngôn ngữ khác thì dùng tiếng Anh.
4. Test EditMode đỏ khi: một key thiếu bản dịch, hai bản dịch lệch số chỗ `{0}`, một SO trong catalog thiếu tên tiếng Anh, hoặc có chữ tiếng Việt viết thẳng trong script UI hay scene builder.
5. Ảnh chụp 4 màn (Quán, Gacha, Bản đồ, Trận) bằng tiếng Anh không có chữ tràn ô. User đã duyệt bảng dịch.

### Quyết định đã chốt với user (2026-11-07)

| Chủ đề | Quyết định |
|---|---|
| Cách làm | Bảng chuỗi C# tự viết (phương án A, mục 4). Không thêm package |
| Ngôn ngữ mặc định | Theo máy: `SystemLanguage.Vietnamese` thì tiếng Việt, mọi ngôn ngữ khác thì tiếng Anh. Áp cho lần đầu mở game và sau Reset Save Data |
| Chỗ đặt nút đổi | Panel Cài đặt DEV (`DevOverlay`). Bản cho người chơi làm trong spec UI hub |
| Ai dịch | Agent (🔨) dịch nháp toàn bộ, user duyệt bảng trước khi merge |
| Tên nhân vật và món ở bản tiếng Anh | Giữ tên Việt, **bỏ dấu**: "Cha Com", "Dau Phu", "Bun Dau Mam Tom", "Nem Chua Ran". Tên khác (khách, sự cố, nâng cấp, quái, màn, chiêu, banner) dịch nghĩa |

## 2. Hiện trạng (kiểm trong code ngày 2026-11-07, chỉ đọc)

### Chữ người chơi thấy: khoảng 190 chuỗi

Đếm sơ theo số chuỗi có dấu tiếng Việt; chuỗi chỉ có chữ ASCII ("Gacha", "AUTO", "x2", "CLEAR") không tính.

| Nhóm | Số chuỗi | File |
|---|---|---|
| Hàm chữ thuần | ~83 | `CombatViewText` 39, `RestaurantViewText` 34, `GachaViewText` 10 (`UI/Scripts/`) |
| Chữ viết thẳng trong view | ~18 | `StaffPanelRows` 8, `StaffPanel` 5, `TeamSelectPopup` 2, `AfkPopup` 1, `GachaHistoryPopup` 1, `BattleResultView` 1 |
| Nhãn do scene builder ghi vào scene | ~52 | `BattleSceneBuilder` 15, `RestaurantSceneBuilder` 15, `StageSelectSceneBuilder` 14, `GachaSceneBuilder` 8 (`Editor/`) |
| Tên trong ScriptableObject | ~39 | 6 nhân vật, các chiêu, 3 món, 3 loại khách, 3 sự cố, 6 nâng cấp, 4 quái, 7 màn, 1 banner. Giá trị do `ProjectSetup` ghi vào `_displayName` |
| `DevOverlay` (chỉ Editor và Development Build) | ~15 | `UI/Scripts/DevOverlay.cs` |

Chữ chỉ dev thấy thì **không** tính: `Debug.Log*`, thông báo lỗi của `Validate()` (`RestaurantConfig`, `CombatConfig`…), `Tooltip`, công cụ Editor (`ProjectSetup`, `AndroidBuild`, `PlaytestTools`, `SaveBackup`, `ResetSaveData`, `BackgroundPlay`), `PlaytestKit`.

### Component chữ và font

- Mọi chữ là uGUI `Text`, font có sẵn `LegacyRuntime.ttf` (`Resources.GetBuiltinResource<Font>`, gán trong 4 builder và `DevOverlay`). Không có TextMeshPro. `GachaSceneBuilder` ghi lý do: font TMP mặc định có thể thiếu dấu tiếng Việt.
- `LegacyRuntime.ttf` là font động: glyph nào thiếu thì Unity lấy từ font của hệ điều hành. Tiếng Việt hiện đúng trong Editor (Windows).
- Bản tiếng Anh chỉ dùng ký tự ASCII nên **không thêm rủi ro font**. Dấu tiếng Việt trên Android thật thì **chưa kiểm** (APK chưa cài lên máy). Đây là rủi ro có từ trước, đi theo mục "Build Android, test trên máy thật" của roadmap, spec này không xử lý.

### Những chỗ khác liên quan

- `PlayerSave` (Core) đọc bằng `JsonUtility.FromJson`: field thiếu trong save cũ giữ giá trị khởi tạo. `GameSession.ResetToDefaults()` ghi đè bằng `new PlayerSave()` rồi bắn `ResetCompleted`.
- `UiDriver` (PlaytestKit) tìm nút theo tên GameObject, không theo chữ trên nút. Đổi ngôn ngữ không làm hỏng smoke test.
- Các test `CombatViewTextTests` (51 chuỗi tiếng Việt), `RestaurantViewTextTests` (36), `StaffPanelRowsTests`, `GachaViewTextTests`, `SmokeLoopTests` so chuỗi tiếng Việt. Ngôn ngữ mặc định lúc chạy test là tiếng Việt (mục 5) nên các test này giữ nguyên.
- `DevOverlay` có `DontDestroyOnLoad`, ẩn nút Cài đặt ở màn Battle; menu tạm dừng của Battle mở panel qua `DevOverlay.OpenSettings()`.
- `BattleUnit` chép `DisplayName` lúc tạo trận.
- Không chỗ nào dùng `DisplayName` làm định danh: chỉ để hiện chữ.
- Bốn scene dựng lại được bằng `ProjectSetup.Rebuild{Restaurant,Gacha,StageSelect,Battle}Scene`.

## 3. Ngoài phạm vi (cố ý không làm)

- Ngôn ngữ thứ ba; dịch bằng máy lúc chạy.
- Package `com.unity.localization`; TextMeshPro; đổi font.
- Dịch chữ chỉ dev thấy (mục 2) và chữ của `DevOverlay`. Ngoại lệ: nhãn nút đổi ngôn ngữ ghi cả hai tiếng.
- Nút đổi ngôn ngữ cho người chơi trong bản phát hành: làm ở spec UI hub.
- Cập nhật tên nhân vật và quái đang đứng trong một trận khi đổi ngôn ngữ giữa trận.
- Kiểm dấu tiếng Việt trên điện thoại thật.
- Ảnh hoặc âm thanh theo ngôn ngữ.

## 4. Các phương án đã xét

| | A. Bảng C# tự viết (**chọn**) | B. Package `com.unity.localization` | C. Tự viết, bảng là file TSV |
|---|---|---|---|
| Cách làm | Key → (vi, en) trong file `.cs`, `UiText.Get`, component `LocalizedText` | String Table, `LocalizeStringEvent`, Smart String | Như A, chữ nằm trong TextAsset TSV |
| Thêm package | Không | Có, kéo theo Addressables (thêm bước build Addressables cho APK, APK to hơn) | Không |
| Nạp bảng | Đồng bộ, có ngay lúc biên dịch | Bất đồng bộ | Đọc file lúc khởi động |
| Lưu ngôn ngữ | `PlayerSave` | Mặc định `PlayerPrefs`, trái [D-005](../../decisions/decisions.md); phải viết selector riêng và thêm vào đường reset | `PlayerSave` |
| Test EditMode | Gọi thẳng | Phải nạp bảng trong test | Phải nạp asset |
| User duyệt bản dịch | Đọc hai cột cạnh nhau trong file `.cs` | Cửa sổ String Table hoặc CSV | Excel / Sheets |
| Hợp khi | 2 ngôn ngữ, ~200 chuỗi | Nhiều ngôn ngữ, có người dịch ngoài | Người dịch không đọc code |

Chọn A vì nhỏ nhất mà vẫn đủ cho 2 ngôn ngữ. Khi nào cần ngôn ngữ thứ ba hoặc người dịch ngoài thì chuyển bảng sang B hoặc C: key giữ nguyên, chỉ thay phần lưu bảng.

## 5. Thiết kế

### 5.1 Ngôn ngữ lúc chạy (Core)

File mới `Core/Scripts/GameLanguage.cs`:

```csharp
public enum Language { Vi, En }

public static class GameLanguage
{
    // Mặc định Vi: test EditMode và scene builder (chạy trong Editor) luôn thấy tiếng Việt, trừ khi test tự đặt.
    public static Language Current { get; set; } = Language.Vi;

    public static string Code(Language language);          // Vi → "vi", En → "en"
    public static bool TryParse(string code, out Language language); // "vi"/"en"; chuỗi khác (kể cả "") → false
    // Đã chọn thì theo lựa chọn; chưa chọn (hoặc mã lạ) thì theo máy.
    public static Language Resolve(string saved, SystemLanguage system) =>
        TryParse(saved, out var chosen) ? chosen : system == SystemLanguage.Vietnamese ? Language.Vi : Language.En;
    // Hai bản theo ngôn ngữ đang dùng; bản tiếng Anh rỗng thì lùi về tiếng Việt để màn không trống chữ.
    public static string Pick(string vi, string en) => Current == Language.En && !string.IsNullOrEmpty(en) ? en : vi;
}
```

`Current` là trạng thái chạy nằm ngoài `PlayerSave`, nên theo [D-005](../../decisions/decisions.md) luật 3 nó phải được đặt lại khi reset (mục 5.2).

### 5.2 Lưu và áp ngôn ngữ (Core, Boot)

- `PlayerSave` thêm `public string Language = "";`. Chuỗi rỗng nghĩa là chưa chọn, tức theo máy. Lưu bằng mã `"vi"` hoặc `"en"` chứ không lưu số của enum, để đổi thứ tự enum không làm hỏng save. Save cũ thiếu field thì đọc ra `""` ([D-005](../../decisions/decisions.md) luật 2).
- `GameSession.SetLanguage(Language language)` trả `bool`:
  1. Nhớ giá trị cũ, ghi `Save.Language = GameLanguage.Code(language)`, gọi `Persist()`.
  2. Lưu thất bại thì trả `Save.Language` về giá trị cũ và trả `false`. `GameLanguage.Current` không đổi.
  3. Lưu được thì đặt `GameLanguage.Current = language` và trả `true`.
- `GameSession` có thêm `ApplyLanguage(SystemLanguage system)`, đặt `GameLanguage.Current = GameLanguage.Resolve(Save.Language, system)`. Hàm này được gọi ở hai chỗ:
  - trong `GameBootstrap`, ngay sau khi tạo session và trước khi nạp scene khởi đầu, với `Application.systemLanguage`;
  - trong handler `ResetCompleted` mà `GameBootstrap` đăng ký. Reset ghi đè `Language = ""`, nên game quay về ngôn ngữ theo máy.
- Hàm nhận `SystemLanguage` qua tham số để test truyền vào được, không đọc `Application.systemLanguage` bên trong.

### 5.3 Bảng chữ (UI)

`UiText` là `static partial class` trong `UI/Scripts/Text/`, mỗi khu một file:

| File | Chứa |
|---|---|
| `UiText.cs` | `Get`, `Format`, gộp các bảng, `AllEntries` cho test |
| `UiText.Common.cs` | Chữ dùng chung: Đóng, Huỷ, Tiếp tục, tên vai (Đầu bếp…), số thập phân |
| `UiText.Gacha.cs`, `UiText.Restaurant.cs`, `UiText.Combat.cs` | Chữ của từng khu |

Mỗi file khai báo một mảng `private static readonly (string Key, string Vi, string En)[] Gacha = { ("gacha.new", "MỚI", "NEW"), ... };`. `UiText.cs` gộp các mảng vào một `Dictionary` khi được dùng lần đầu, và **ném lỗi khi có key trùng**. Tách file theo khu để các phiên chuyển chữ của 3 khu chạy song song mà không sửa chung một file.

- `UiText.Get(string key)` trả bản theo `GameLanguage.Current`. Key không có trong bảng thì trả `"#" + key`, để nhìn thấy ngay trên màn và test bắt được. Không ném lỗi, vì lỗi chữ không đáng làm chết màn.
- `UiText.Format(string key, params object[] args)` = `string.Format(CultureInfo.InvariantCulture, Get(key), args)`.
- Key đặt theo dạng `khu.tên`, viết thường, ví dụ `combat.wave`, `restaurant.income_missing_cook`.
- Số thập phân: tiếng Việt dùng dấu phẩy (`1,5 Coin/giây`, giữ như bây giờ), tiếng Anh dùng dấu chấm (`1.5 Coin/s`). Gom vào một hàm `UiText.Decimal(double value, string format)`. Giờ `HH:mm dd/MM` của lịch sử Gacha giữ nguyên ở cả hai tiếng.
- Tên tiền tệ (`Coin`, `Gem`, `Material`, in từ enum) giống nhau ở cả hai tiếng, giữ nguyên.

**Đổi code hiện có:**
- `CombatViewText`, `RestaurantViewText`, `GachaViewText`: giữ nguyên chữ ký hàm, chỉ thay chuỗi bằng `UiText.Get`/`Format`. Các view gọi chúng không đổi.
- Sáu view có chữ viết thẳng (mục 2): thay chuỗi bằng `UiText`.
- `StaffPanelRows.RoleName` và nhánh tên vai trong `RestaurantViewText.IncidentHint` dùng chung key `common.role_*`.

### 5.4 Nhãn cố định trong scene

Component mới `LocalizedText` (`UI/Scripts/Text/LocalizedText.cs`):

```csharp
[RequireComponent(typeof(Text))]
public sealed class LocalizedText : MonoBehaviour
{
    [SerializeField] private string _key;
    public string Key => _key;
    private void OnEnable() => GetComponent<Text>().text = UiText.Get(_key);
}
```

- Builder dùng một helper chung cho nhãn cố định: tạo `Text` với `UiText.Get(key)` làm chữ ban đầu, rồi gắn `LocalizedText` với key đó. Helper đặt cạnh `GachaSceneBuilder.NewText`/`AddText`.
- Chữ giữ chỗ mà view ghi đè lúc chạy ("Đợt 2/3", "Chưa có trận") cũng lấy qua `UiText.Get`, nhưng **không** gắn `LocalizedText`. Nếu gắn thì `OnEnable` sẽ ghi đè chữ view vừa đặt khi popup bật lại.
- Scene được dựng khi `GameLanguage.Current = Vi`, nên file `.unity` chứa chữ tiếng Việt như bây giờ. Sau khi sửa 4 builder thì dựng lại 4 scene bằng `ProjectSetup.Rebuild*Scene`.

### 5.5 Tên trong ScriptableObject

- Chín lớp SO có `_displayName` thêm `[SerializeField] private string _displayNameEn;`, và `DisplayName => GameLanguage.Pick(_displayName, _displayNameEn)`. Chín lớp đó là `CharacterDefinition`, `SkillDefinition` (dùng chung cho chiêu nhân vật và chiêu quái), `GachaBannerDefinition`, `DishDefinition`, `CustomerDefinition`, `IncidentDefinition`, `UpgradeDefinition`, `EnemyDefinition`, `StageDefinition`.
- Bảng dữ liệu trong `ProjectSetup` thêm cột tên tiếng Anh. Với asset đã có, `ProjectSetup` **chỉ điền `_displayNameEn` khi ô đó đang rỗng**, như cách nó đang giữ các số đã chỉnh. Chạy lại `Setup Project` là đủ, không phải sửa tay asset.
- Tên tiếng Anh theo mục 1: nhân vật và món giữ tên Việt bỏ dấu; các tên khác dịch nghĩa (ví dụ "Ruồi Bàn" → "Table Fly", "Bếp tắc ga" → "Gas Line Clogged"). Màn Truyện vẫn hiện nhãn `combat.node_story` ("Truyện" / "Story") như bây giờ.

### 5.6 Đổi ngôn ngữ (DevOverlay)

- Panel Cài đặt DEV có thêm một nút ở hàng còn trống của lưới. Nhãn nút là `"Ngôn ngữ: Tiếng Việt"` hoặc `"Language: English"` theo ngôn ngữ hiện tại. Bấm thì chuyển sang ngôn ngữ kia qua `GameSession.SetLanguage`.
  - Lưu thất bại thì dòng trạng thái của panel báo "Chưa lưu được, giữ ngôn ngữ cũ" và không làm gì thêm.
  - Lưu được thì đóng panel, rồi nạp lại scene đang mở (`SceneManager.LoadScene(active.name)`). Popup đang mở sẽ đóng, mọi view và `LocalizedText` vẽ lại bằng ngôn ngữ mới. Đây là đường đã có sẵn: các nút chuyển màn DEV cũng `LoadScene`.
- **Ở màn Battle thì không nạp lại scene**, để không làm hỏng trận đang chạy. Lựa chọn vẫn được lưu và có hiệu lực từ màn kế tiếp; dòng trạng thái báo "Ngôn ngữ đổi khi rời trận". Ghi `// shortcut: Battle không vẽ lại khi đổi ngôn ngữ, làm khi hub có nút đổi ngôn ngữ cho người chơi.`
- Nạp lại scene để vẽ lại là cách nhỏ nhất: các view không phải nghe sự kiện đổi ngôn ngữ. Spec UI hub dùng lại `GameSession.SetLanguage` và cách nạp lại này.
- `DevOverlay` cần `GameSession`: lấy từ `GameBootstrap` (đang có `Session`) hoặc đăng ký một mặt tiền nhỏ vào `ServiceLocator` như `GameResetter`. Kế hoạch chọn theo cách `GameResetter` đang làm.

### 5.7 Luật cho màn làm về sau

Ghi vào [code-style.md](../../guides/code-style.md) khi xong:
- Chữ người chơi thấy đi qua `UiText`.
- Nhãn cố định do builder dựng thì gắn `LocalizedText`.
- SO mới có tên hiển thị thì có cả `_displayNameEn`.

Test quét mã nguồn (mục 7) giữ luật này.

## 6. Luồng chạy

```
Boot: GameSession nạp save → ApplyLanguage(Application.systemLanguage) → nạp scene khởi đầu
Màn: view gọi *ViewText → UiText.Get(key) theo GameLanguage.Current; LocalizedText.OnEnable; SO.DisplayName → GameLanguage.Pick
Đổi: DevOverlay → GameSession.SetLanguage → lưu OK → Current đổi → LoadScene(active) (trừ Battle)
Reset: ResetToDefaults → Language = "" → ResetCompleted → ApplyLanguage(system)
```

## 7. Test

**EditMode:**

| Test | Bắt lỗi gì |
|---|---|
| `GameLanguageTests` | `Resolve`: đã lưu `"vi"`/`"en"` thì theo save bất kể máy; `""` hoặc mã lạ thì theo máy, với máy Vietnamese, English, Japanese, Unknown. `Pick`: tiếng Anh rỗng thì lùi về tiếng Việt |
| `UiTextTests` | Không key trùng; mỗi dòng có `Vi` và `En` không rỗng; số chỗ `{n}` trong hai bản trùng nhau (cùng tập chỉ số); `Get` key lạ trả `"#key"`; `Get` theo `Current` (đặt En thì ra bản En); `Decimal` ra dấu phẩy ở Vi và dấu chấm ở En |
| `SaveCompatibilityTests` (thêm ca) | Save cũ không có `Language` đọc ra `""` |
| `GameSession` (thêm ca vào test session/reset hiện có) | `SetLanguage` lưu `"en"` và đổi `Current`; lưu thất bại thì giữ `Save.Language` và `Current` cũ; reset đưa `Language` về `""` và `Current` về theo máy |
| `LocalizedTextTests` | `OnEnable` gán chữ theo `Current`; bật lại sau khi đổi ngôn ngữ thì chữ đổi |
| `*ViewTextTests` (thêm ca) | Mỗi file thêm vài ca đặt `Current = En`, so chuỗi tiếng Anh. Ca tiếng Việt cũ giữ nguyên |
| `LocalizedNameTests` | Mọi SO trong `CharacterCatalog`, `GachaBannerCatalog`, `RestaurantConfig` (món, khách, sự cố), `UpgradeCatalog`, `StageCatalog`, quái và chiêu có `_displayNameEn` không rỗng; tên tiếng Anh của nhân vật và món không có dấu |
| `HardcodedTextTests` | Quét `UI/Scripts/**/*.cs` và `Editor/*SceneBuilder.cs`. Bỏ dòng comment (`//`) và chuỗi trong `Debug.Log*`; gặp chuỗi có ký tự tiếng Việt có dấu thì đỏ, thông báo ghi file, số dòng, chuỗi. Bỏ qua `UiText*.cs` và `DevOverlay.cs` |
| Test scene (`*SceneTests` hiện có, thêm ca) | Đọc lại từ file scene: có `LocalizedText`, và mọi key của chúng có trong `UiText` (L-002) |

Mọi test có đặt `GameLanguage.Current` thì trả về `Vi` trong `TearDown`, để test khác không bị lệch.

**PlayMode** (`SmokeLoopTests`, save tạm):
- Boot → Quán.
- Đổi sang English bằng đúng hàm mà nút DEV gọi.
- Chờ scene nạp lại, rồi kiểm:
  - nhãn nút Nhân viên là "Staff";
  - `Save.Language == "en"`;
  - nạp lại save tạm vẫn ra `En`.
- Cuối test trả ngôn ngữ về.

**Hạn chế đã biết của test quét:** test chỉ bắt chuỗi có dấu. Chuỗi tiếng Việt không dấu ("Quay") hay chữ tiếng Anh viết thẳng thì lọt. Phần này dựa vào review. Ghi `shortcut:` trong test.

**Kiểm bằng mắt (lần dừng cho user):** đặt English, chụp 4 màn bằng `Capture1080("bi-<màn>.png")` (L-026) gồm Quán (mở cả panel Nhân viên), Gacha (trang kết quả), Bản đồ (popup chọn đội), Trận. Chữ tràn ô thì rút gọn bản dịch trước, không đổi layout. User đọc bảng `UiText.*.cs` cùng ảnh và duyệt.

## 8. Docs phải sửa khi xong

- [decisions.md](../../decisions/decisions.md): thêm D-012 "Song ngữ bằng bảng C# tự viết, ngôn ngữ lưu trong `PlayerSave`".
- [code-style.md](../../guides/code-style.md): luật mục 5.7.
- [unity-workflow.md](../../guides/unity-workflow.md):
  - sửa câu "`unity:localization` chưa dùng, vì chưa có yêu cầu đa ngôn ngữ" thành: dự án tự viết bảng (D-012), không dùng skill này;
  - mục Công cụ thử tự động: thêm cách đổi ngôn ngữ trong panel DEV.
- [structure.md](../../project/structure.md): `UI/Scripts/Text/`, `GameLanguage` trong Core, số test.
- [roadmap.md](../../roadmap.md):
  - dòng UI hub ghi "kèm nút đổi ngôn ngữ cho người chơi, dùng `GameSession.SetLanguage`";
  - dòng Song ngữ chuyển sang Đã xong.

## 9. Rủi ro

| Rủi ro | Cách giữ |
|---|---|
| Chữ tiếng Anh dài hơn, tràn ô cố định | Lần dừng chụp 4 màn; rút gọn bản dịch trước khi tính đổi layout |
| Dấu tiếng Việt trên Android thật | Có từ trước, không đổi bởi spec này; kiểm khi cài APK lên máy (roadmap) |
| Màn mới quên dùng `UiText` | `HardcodedTextTests` và luật trong code-style |
| `GameLanguage.Current` là trạng thái tĩnh, test này làm lệch test kia | Mặc định `Vi`, test đặt thì trả trong `TearDown` |
| Nạp lại scene khi đổi ngôn ngữ làm mất trạng thái của màn | Chỉ là trạng thái hiển thị (popup đang mở); dữ liệu nằm trong service. Battle không nạp lại |
