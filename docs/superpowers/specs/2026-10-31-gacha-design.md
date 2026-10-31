# Spec: Gacha

> Ngày: 2026-10-31 · Trạng thái: **đã duyệt (2026-10-31)** · Phạm vi: banner, roll, đảm bảo (pity), nhân vật trùng thành mảnh, UI tối thiểu để thử.
> Kiến trúc, `Wallet`, `Roster`, `SaveService`, luật phụ thuộc asmdef: xem [spec nền móng](2026-10-30-foundation-architecture-design.md). Luật chơi: [game-design.md](../../design/game-design.md#gacha). Kế hoạch: [plans/2026-10-31-gacha.md](../plans/2026-10-31-gacha.md).

## 1. Mục tiêu

Người chơi tiêu Gem để quay 1 hoặc 10 lượt trên một banner, nhận nhân vật vào `Roster`; nhân vật đã có thì nhận mảnh. Tiền, nhân vật, mảnh, bộ đếm đảm bảo và save luôn nhất quán: thiếu Gem thì không đổi gì cả. Có một màn hình tối thiểu để thử vòng quay.

### Quyết định đã chốt với user (2026-10-31)

| Chủ đề | Quyết định |
|---|---|
| Nhân vật trùng | Thành **mảnh** của nhân vật đó. Số mảnh mỗi lần trùng theo độ hiếm, đặt trong banner SO, khởi điểm R/SR/SSR đều **1**, user chỉnh được sau. Mảnh phải được lưu và bị Reset Save Data xoá |
| Nơi giữ mảnh | Trong `PlayerSave`, qua `Roster` (`AddFragments`, `GetFragments`). Không có `Inventory` riêng; màn kho sau này đọc mảnh từ `Roster` |
| Nâng sao | Mảnh dùng để nâng sao nhân vật (tối đa 5★), mảnh dư khi đủ sao thì nằm trong kho. **Làm ở spec riêng, sau Combat**; spec này chỉ cộng mảnh |
| Số banner | **1** banner mẫu, UI không chọn banner. Banner có `Id`, bộ đếm đảm bảo lưu theo `Id` banner, nên thêm banner sau này chỉ là thêm asset và bộ chọn trên UI |
| Tỉ lệ khởi điểm | R 70 / SR 25 / SSR 5. Trong cùng độ hiếm, mọi nhân vật của banner có xác suất như nhau |
| Chi phí khởi điểm | 100 Gem một lượt, 900 Gem mười lượt |
| Đảm bảo SSR | Lượt thứ **50** kể từ SSR gần nhất chắc chắn ra SSR. Đếm theo từng banner, quay lẻ hay quay 10 đều tính từng lượt, về 0 khi ra SSR (kể cả SSR trúng tỉ lệ thường). Không soft pity. Ngưỡng 0 là tắt |
| Đảm bảo quay 10 | Lượt thứ 10 của lần quay 10 mà chưa có SR trở lên thì nâng thành SR hoặc SSR (theo tỉ lệ SR:SSR). Bật/tắt bằng field trong banner |
| Nguồn ngẫu nhiên | Tiêm một `System.Random` vào `GachaService` để test bằng seed cố định hoặc lớp kế thừa kịch bản |

Mọi con số ở bảng trên là **số khởi điểm đặt trong ScriptableObject**, chỉnh trong Inspector không cần sửa code.

### Vision đã cập nhật

Luật trùng → mảnh → nâng sao trước đây đi ngược mục "Ngoài phạm vi" của [vision.md](../../vision/vision.md). User cho phép sửa vision theo đề xuất, và file đó đã cập nhật ngày 2026-10-31.

## 2. Cấu trúc và ranh giới asmdef

```
Core  <-  Characters  <-  Gacha  <-  UI, Boot
```

| Assembly | Thêm mới / thay đổi |
|---|---|
| `Core` | `FragmentCount`, `PityEntry`; field mới của `PlayerSave`; `Wallet.CanAfford` |
| `Characters` | `Roster.AddFragments`, `Roster.GetFragments` |
| `Gacha` (asmdef mới, chỉ tham chiếu `Core` và `Characters`) | `GachaBannerDefinition`, `GachaBannerCatalog`, `GachaRoller`, `GachaService`, `GachaPullResult` |
| `UI` (asmdef mới, tham chiếu `Core`, `Characters`, `Gacha`, `UnityEngine.UI`) | `GachaView` |
| `Boot` | thêm tham chiếu `Gacha`; `GameBootstrap` nối `GachaService` và nạp scene khởi đầu. `GameSession` không đổi |
| `Editor` (thêm tham chiếu `Gacha`, `UI`, `Unity.InputSystem`, `UnityEngine.UI`) | `ProjectSetup` sinh banner mẫu, scene `Gacha`, vá scene `Boot` |
| `Tests` | thêm tham chiếu `Gacha`, `UI`, `Unity.InputSystem` (`InputSystemUIInputModule`) và `UnityEngine.UI` (`EventSystem`, `StandaloneInputModule`, `RawImage`) |

`Gacha` không tham chiếu `Restaurant`, `Combat`, `Boot`, `UI`. Nó nhận `persist` dưới dạng `Func<bool>` vì không biết `GameSession`.

## 3. Mô hình dữ liệu

### 3.1 Save và `Wallet` (`Core`)

```csharp
[Serializable] public struct FragmentCount { public string CharacterId; public long Count; }
[Serializable] public struct PityEntry     { public string BannerId;    public int  Pulls; }

// PlayerSave: thêm hai field, có giá trị khởi tạo để save cũ vẫn đọc được.
public List<FragmentCount> Fragments = new();
public List<PityEntry> GachaPity = new();
```

- `Version` giữ `1`: chỉ thêm field có giá trị khởi tạo, không cần migration (spec nền móng mục 3.6). Test dựng một JSON save cũ không có hai field này và kiểm tra `Fragments`, `GachaPity` rỗng, các field cũ còn nguyên.
- `Pulls` là số lượt kể từ SSR gần nhất. Không có dòng cho banner thì coi là 0.
- `Wallet.CanAfford(params CurrencyAmount[] costs)`: trả `true` nếu `TrySpend(costs)` sẽ thành công, **không đổi gì và không phát `Changed`**. `TrySpend` gọi lại chính logic kiểm này (một hàm riêng tư dùng chung, trả về bảng số dư còn lại) để hai hàm không lệch nhau. Chi phí rỗng hoặc toàn dòng 0 thì `CanAfford` và `TrySpend` đều `true`, đúng như hiện nay; Gacha chặn ca đó ở `Validate()` của banner.

### 3.2 Mảnh trong `Roster` (`Characters`)

- `bool AddFragments(string id, long count)`: `false` nếu `count <= 0`, hoặc nhân vật chưa sở hữu, hoặc không có trong catalog. Thành công thì cộng vào `save.Fragments` (tràn `long` thì chặn ở `long.MaxValue`) và phát `Changed` như các thao tác khác của `Roster`.
- `long GetFragments(string id)`: 0 nếu chưa có dòng nào.
- Chưa có thao tác tiêu mảnh: spec nâng sao thêm.
- Không có service mới cho mảnh. `ResetToDefaults()` ghi đè cả `PlayerSave` rồi gọi `Roster.NotifyChanged()` (đã có), nên mảnh và pity tự về rỗng và view tự vẽ lại ([D-005](../../decisions/decisions.md)).

### 3.3 `GachaBannerDefinition : GameDefinition` (`Gacha`)

| Field (private `[SerializeField]`, mỗi field có property đọc cùng tên) | Ý nghĩa | Khởi điểm |
|---|---|---|
| `_displayName` | tên hiển thị | "Banner Thường" |
| `_singleCost`, `_tenCost` | `CurrencyAmount[]` | Gem 100; Gem 900 |
| `_rarityWeights` | `RarityWeights {float R, SR, SSR}` (trọng số, không cần tổng 100) | 70 / 25 / 5 |
| `_ssrPityThreshold` | `int`, 0 = tắt | 50 |
| `_guaranteeSrInTen` | `bool` | true |
| `_duplicateFragments` | `RarityCounts {int R, SR, SSR}`: mảnh nhận khi trùng | 1 / 1 / 1 |
| `_characters` | `CharacterDefinition[]` | cả 6 nhân vật mẫu |

- Độ hiếm của nhân vật lấy từ `CharacterDefinition.Rarity`, không nhắc lại trong banner. Chọn độ hiếm trước theo `_rarityWeights`, rồi chọn **đều** một nhân vật trong độ hiếm đó. Trọng số từng nhân vật thuộc banner nhân vật nổi bật (ngoài phạm vi, mục 8).
- Không có field `Art`: bản nháp ở spec nền móng mục 3.3 có, nhưng MVP không dùng; thêm khi có art banner.
- `List<string> Validate()` (và `OnValidate` chỉ `LogWarning` kết quả) báo:
  - `Id` trống;
  - chi phí rỗng, hoặc tổng mọi dòng bằng 0 (nếu không, `TrySpend` không có dòng nào trả `true` và lượt quay thành miễn phí), hoặc có dòng âm;
  - trọng số độ hiếm âm hoặc cả ba bằng 0; số mảnh âm;
  - phần tử `_characters` null;
  - độ hiếm có trọng số > 0 mà không có nhân vật nào của độ hiếm đó;
  - `_ssrPityThreshold > 0` mà không có nhân vật SSR;
  - `_guaranteeSrInTen` bật mà trọng số SR và SSR đều bằng 0, hoặc không có nhân vật SR/SSR nào.
- `GachaBannerCatalog : Catalog<GachaBannerDefinition>` (kiểm trùng `Id` có sẵn trong `Catalog<T>`).

## 4. Logic

### 4.1 Nguồn ngẫu nhiên

`GachaService` và `GachaRoller` nhận một `System.Random`. Production tạo `new System.Random()`. Test dùng `new System.Random(seed)` cho thống kê và một lớp kế thừa `System.Random` ghi đè `NextDouble()` để chạy kịch bản. **Cần kiểm khi viết test đầu tiên** rằng `NextDouble` là `virtual` và ghi đè được trong runtime Unity; nếu không thì quay lại dùng một interface nhỏ.

Số lần gọi `NextDouble` là một phần của hợp đồng vì test kịch bản dựa vào nó: mỗi lượt gọi **một** lần để chọn độ hiếm (**bỏ qua** khi lượt đó bị ép SSR bởi pity) và **một** lần để chọn nhân vật.

### 4.2 `GachaRoller` (class thuần, không biết ví hay save)

`RollOne(banner, ref int pulls, Rarity? minRarity) -> CharacterDefinition`:
1. `threshold > 0` và `pulls + 1 >= threshold` → độ hiếm là SSR, không gọi RNG cho bước này.
2. Ngược lại: tổng trọng số của các độ hiếm `>= minRarity` (nếu có); `u = NextDouble() * tổng`; duyệt R, SR, SSR, bỏ qua độ hiếm có trọng số 0 **và độ hiếm thấp hơn `minRarity`** (cả hai không bao giờ trúng và không bị trừ vào `u`), trả độ hiếm đầu tiên thoả `u < trọng số`, ngược lại `u -= trọng số`. Dùng `<` (không phải `<=`) để trọng số 0 không bao giờ trúng. Nếu sai số làm tròn khiến vòng duyệt chạy hết, trả độ hiếm hợp lệ cuối cùng (trọng số > 0 và `>= minRarity`). Ví dụ: `minRarity = SR` và `NextDouble() = 0.0` phải ra SR, không ra R.
3. Chọn nhân vật: các nhân vật đúng độ hiếm, `index = (int)(NextDouble() * n)`, chặn ở `n - 1` (phòng `NextDouble` trả 1.0 trong test).
4. `pulls = (độ hiếm == SSR) ? 0 : pulls + 1`.

`RollTen`: 10 lần `RollOne` tuần tự trên cùng `pulls`. Lượt thứ 10 truyền `minRarity = SR` khi `_guaranteeSrInTen` bật và 9 lượt đầu chưa có SR trở lên; việc này diễn ra **trong** lượt 10 (không thay kết quả sau khi đã roll) nên bộ đếm đúng khi lượt đó ra SSR. Quay lẻ không có đảm bảo SR.

Vì sao pity chạy trong cùng vòng lặp: quay 10 ở ngay ranh giới ngưỡng (đang ở 45 lượt) phải ép SSR đúng lượt 50 và đặt lại bộ đếm trước các lượt còn lại.

### 4.3 `GachaService`

```csharp
public GachaService(PlayerSave save, Wallet wallet, Roster roster, CharacterCatalog characters,
                    GachaBannerCatalog banners, System.Random random, Func<bool> persist);
public GachaPullResult PullSingle(string bannerId);
public GachaPullResult PullTen(string bannerId);
public int GetPity(string bannerId);
```

`Pull*` thực hiện, theo thứ tự:
1. **Kiểm banner.** Có trong catalog (`UnknownBanner`), `Validate()` rỗng và mọi nhân vật nằm trong `CharacterCatalog` (`InvalidBanner`). Chưa đổi gì.
2. **Kiểm tiền.** `wallet.CanAfford(cost)` sai thì `NotEnoughCurrency`, **chưa gọi RNG**, chưa đổi gì.
3. **Roll** trên bản sao bộ đếm pity. Chưa trừ tiền, chưa đụng `Roster`: nếu bước này ném exception thì không có gì thay đổi để hoàn lại.
4. **Trừ tiền.** `wallet.TrySpend(cost)`. Sau bước 2 nó không thể sai; nếu vẫn sai thì trả `NotEnoughCurrency` và không áp kết quả.
5. **Áp kết quả** theo thứ tự trúng: chưa sở hữu thì `roster.Add(id)` (`IsNew`); đã sở hữu thì `roster.AddFragments(id, mảnh theo độ hiếm)`. "Đã sở hữu" kiểm sau mỗi lần áp, nên bản thứ hai của cùng một nhân vật trong lần quay 10 là trùng. Ghi bộ đếm cuối vào `save.GachaPity`.
6. **Lưu** bằng `persist()` đúng **một lần**. Trả `false` thì `Saved = false`; trạng thái bộ nhớ giữ nguyên (không hoàn tiền) và `OnApplicationPause`/`OnApplicationQuit` sẽ thử lưu lại.

```csharp
public enum GachaPullStatus { Success, UnknownBanner, InvalidBanner, NotEnoughCurrency }
public sealed class GachaPullResult {
    public GachaPullStatus Status; public bool Saved;
    public IReadOnlyList<GachaPullEntry> Entries;  // Character, IsNew, FragmentsGained
}
```

Luồng này triển khai mục 4.3 "Gacha" của spec nền móng. `Wallet.Changed` phát lúc trừ tiền, trước khi `Roster` đổi; view phải vẽ lại từ trạng thái sau `Pull*` trả về, không từ giữa chừng.

### 4.4 Nối dây ở `Boot`

- `GameBootstrap` thêm `[SerializeField] GachaBannerCatalog _bannerCatalog` và `[SerializeField] string _startScene`. Kiểm `_bannerCatalog` null giống `_catalog` và `_newGameConfig` (`Debug.LogError` kèm tên field rồi `return`). Sau khi tạo session: đăng ký `new GachaService(session.Save, session.Wallet, session.Roster, _catalog, _bannerCatalog, new System.Random(), session.Persist)` vào `ServiceLocator` (`GachaView` lấy nó từ đó), rồi nạp `_startScene` nếu không rỗng. Đây là bước 3 của mục 4.1 spec nền móng ("mở scene hub"): spec hub sau này đổi giá trị này.
- `GachaService` giữ chính object `Save` của session; `ResetToDefaults()` ghi đè tại chỗ nên nó thấy số mới. Test `GameSessionTests.ResetToDefaults_ClearsEveryFieldOfTheSave` hiện có đã chứng minh `List` thu nhỏ về rỗng; chỉ cần thêm assert cho `Fragments` và `GachaPity`.
- `Boot.unity` đã có sẵn nên `ProjectSetup` phải **vá** scene (gán `_bannerCatalog`, `_startScene`) khi field còn trống, không chỉ tạo mới. Có test đọc lại tham chiếu từ file (bài học L-002).

## 5. UI tối thiểu

Chỉ để thử vòng quay; hub thật ở spec UI hub. Màn ngang, `CanvasScaler` tham chiếu 1920×1080. **Mọi hình trong Canvas là component `Image`** dùng `UISprite`/`Knob` có sẵn, không `Raw Image`, không thêm file ảnh ([unity-workflow.md](../../guides/unity-workflow.md#ui-và-hình-tạm)). Chữ dùng `Text` của uGUI với font dựng sẵn: font TextMeshPro mặc định có thể thiếu dấu tiếng Việt và phải import thêm tài sản.

```
[Gem: 1500]                                   Banner Thường
                                              SSR đảm bảo sau: 50 lượt
 ┌──────────── kết quả (2 hàng x 5 ô) ────────────┐
 │ [R  Nem Chua  TRÙNG +1 mảnh] [SR ...] [SSR ...] │
 └─────────────────────────────────────────────────┘
 [ Quay 1 (100 Gem) ]   [ Quay 10 (900 Gem) ]      thông báo
```

- `GachaView` có `[SerializeField] GachaBannerDefinition _banner` do `ProjectSetup` gán, để lấy `Id`, tên, giá và ngưỡng pity (nên banner cần property đọc cho các field ở mục 3.3). Còn `Wallet`, `Roster`, `GachaService` lấy từ `ServiceLocator` (view được phép).
- Ô kết quả tô màu theo độ hiếm (R xám, SR xanh, SSR vàng), ghi tên, "MỚI" hoặc "TRÙNG +N mảnh".
- Nút mờ khi `Wallet.CanAfford(chi phí)` sai; bấm khi `Status != Success` thì hiện thông báo (thiếu Gem, banner lỗi); `Saved == false` thì hiện cảnh báo lưu. View nghe `Wallet.Changed` và `Roster.Changed` để vẽ lại, và lấy số "SSR đảm bảo sau N lượt" từ `threshold - GachaService.GetPity(_banner.Id)`.
- Logic chọn màu/chữ nằm trong hàm tĩnh nhỏ để test được; phần dựng scene kiểm bằng Play Mode.
- Scene `Gacha.unity` do `ProjectSetup` sinh, thêm vào Build Settings (sau `Boot`), và `Boot` nạp nó qua `_startScene`. Scene cần đủ để bấm được:
  - một `Camera` (scene `EmptyScene` không có sẵn, thiếu thì Unity báo "no cameras rendering");
  - một `EventSystem` với **`InputSystemUIInputModule`**, không `StandaloneInputModule`: project chỉ dùng Input System (`activeInputHandler: 1`, package `com.unity.inputsystem` 1.19.0, assembly `Unity.InputSystem`), module cũ sẽ không nhận chuột/chạm;
  - Canvas và `GachaView` đã gán `_banner`.

## 6. Test

EditMode, cùng assembly `BunDauPeo.Tests`. Số test cụ thể ở kế hoạch.

- **Wallet:** `CanAfford` đúng với đủ/thiếu/nhiều loại/dòng cùng loại/dòng âm/không dòng; không đổi số dư và không phát `Changed`; khớp với kết quả `TrySpend` trên cùng đầu vào.
- **Roster (mảnh):** `AddFragments` cộng và phát `Changed`; `false` khi `count <= 0`, chưa sở hữu, Id lạ; tràn bị chặn; `GetFragments` đúng; số lưu trong save.
- **Tương thích save:** JSON cũ không có `Fragments`/`GachaPity` đọc được, field cũ còn nguyên; round-trip hai field mới qua `SaveService`.
- **Banner `Validate`:** từng lỗi ở mục 3.3 có một ca (gồm chi phí rỗng, chi phí tổng 0, đảm bảo 10 lượt mà SR và SSR đều trọng số 0); banner mẫu hợp lệ.
- **Roller (RNG kịch bản):** luôn 0.0 → toàn R, đúng lượt 50 là SSR rồi bộ đếm về 0; pity xuyên ranh giới quay 10 (45 + quay 10); lượt 10 nâng SR khi chưa có SR; không nâng khi đã có SR ở lượt trước; quay lẻ không có đảm bảo SR; ngưỡng 0 tắt pity; SSR trúng tỉ lệ thường đặt lại bộ đếm; trọng số độ hiếm bằng 0 không bao giờ trúng (kể cả `NextDouble` = 0.0); `minRarity = SR` với `NextDouble` = 0.0 ra SR, không ra R (độ hiếm thấp hơn `minRarity` bị bỏ qua); `NextDouble` trả 1.0 (mô phỏng sai số làm tròn) thì trả độ hiếm hợp lệ cuối cùng, không văng lỗi; chọn nhân vật đều trong độ hiếm, `NextDouble` = 1.0 không vượt mảng.
- **Roller (thống kê):** `new System.Random(seed cố định)`, 100.000 lượt, pity tắt, độ hiếm lệch khỏi tỉ lệ kỳ vọng không quá 5σ; nhân vật cùng độ hiếm xuất hiện đều. Seed cố định nên kết quả luôn giống nhau giữa các lần chạy, không flaky; đỏ nghĩa là thuật toán đổi, phải tìm lỗi, **không** đổi seed cho xanh.
- **Service (nguyên tử):** thiếu Gem thì ví, `Roster`, mảnh, pity, `Changed`, số lần gọi RNG đều không đổi; RNG ném lỗi thì ví, `Roster`, mảnh, pity đều không đổi (vì roll chạy trước khi trừ tiền); banner lạ hoặc lỗi không đổi gì; nhân vật mới vào `Roster`; trùng thì cộng đúng số mảnh theo độ hiếm; trùng ngay trong một lần quay 10; `persist` gọi đúng một lần cho quay 10 và trả `Saved = false` khi `persist` thất bại mà bộ nhớ vẫn đúng; pity được ghi vào save và còn sau khi nạp lại.
- **Reset:** thêm assert vào `ResetToDefaults_ClearsEveryFieldOfTheSave` rằng `Fragments` và `GachaPity` (đã gán dữ liệu trước reset) rỗng sau reset; `Roster.Changed` được phát. Test `ResetSaveData` hiện có vẫn xanh.
- **ProjectSetup:** banner mẫu hợp lệ và có đủ độ hiếm; `Boot.unity` có `_bannerCatalog` và `_startScene`; scene `Gacha` có `Camera`, `EventSystem` + `InputSystemUIInputModule` (và không có `StandaloneInputModule`), `GachaView` đã gán `_banner`; chạy lại không ghi đè số đã chỉnh.

## 7. Gói triển khai

Chi tiết từng task, model/effort và lệnh ở [kế hoạch](../plans/2026-10-31-gacha.md) (sẽ viết sau khi duyệt). **9 task:**

1. `Core`: `FragmentCount`, `PityEntry`, field mới của `PlayerSave`, `Wallet.CanAfford`, test tương thích save.
2. `Characters`: `Roster.AddFragments`, `Roster.GetFragments`.
3. Asmdef `Gacha`, `GachaBannerDefinition`, `GachaBannerCatalog`, `Validate()`.
4. `GachaRoller` và test (kịch bản + thống kê), kiểm `NextDouble` ghi đè được.
5. `GachaService` và `GachaPullResult`.
6. `GameBootstrap` nối `GachaService` và scene khởi đầu, test Reset.
7. `ProjectSetup`: banner mẫu, catalog, vá `Boot.unity`.
8. UI: asmdef `UI`, `GachaView`, scene `Gacha` (Camera, EventSystem), thử trong Play Mode.
9. Cập nhật docs (`structure.md`, `roadmap.md`, `game-design.md`), lesson nếu có.

## 8. Ngoài phạm vi

- Nâng sao, dùng mảnh, chỉ số theo sao (spec "Phát triển nhân vật", sau Combat). Cho đến lúc đó mảnh chỉ được cộng và hiển thị.
- Màn hình kho đầy đủ; chọn banner; banner giới hạn thời gian; banner có nhân vật nổi bật (kèm **trọng số từng nhân vật**); soft pity; hoàn Gem khi trùng; âm thanh.
- Hiệu ứng, animation quay và nhật ký lịch sử quay: đã đưa vào [spec reveal](2026-10-31-gacha-reveal-design.md) (2026-10-31). Animation nhân vật thật vẫn ngoài phạm vi.

## 9. Câu hỏi còn mở

- Đã xong (2026-10-31): [vision.md](../../vision/vision.md) đã sửa theo đề xuất, với sự cho phép của user. "Ngoài phạm vi" nay ghi "Chỉ số nhân vật theo sao và cấp nhân vật (chưa thiết kế)"; "Trong phạm vi (MVP)" thêm "Nhân vật trùng thành mảnh để nâng sao (tối đa 5★, spec riêng sau Combat)".
- Đã chốt (2026-10-31): nâng sao làm **sau Combat**, vì sao ảnh hưởng chỉ số mà Restaurant và Combat đọc, và tiêu chí "xong" của MVP không cần đến sao. Roadmap ghi theo thứ tự này.
- `TODO(user)`: sao ảnh hưởng chỉ số chiến đấu và quán thế nào, giá mảnh mỗi sao, nhân vật mới có mấy sao, mảnh dư tại 5★ dùng làm gì. Chốt trong spec nâng sao.

## 10. Rủi ro

- **Trừ tiền không nhất quán:** thứ tự kiểm banner → `CanAfford` → roll → `TrySpend` → áp → lưu ở mục 4.3 là hợp đồng; test nguyên tử phải bắt được từng bước hỏng.
- **Pity sai khi quay 10:** bộ đếm tăng theo từng lượt trong cùng vòng lặp và nâng SR diễn ra trong lượt 10; sai ở ranh giới là lỗi dễ mắc nhất.
- **Bộ chọn theo trọng số:** `<` thay vì `<=` và dự phòng làm tròn (mục 4.2); sai thì trọng số 0 vẫn trúng hoặc văng lỗi chỉ ở xác suất rất nhỏ nên khó thấy.
- **Save cũ không đọc được / Reset bỏ sót:** field mới phải có giá trị khởi tạo; reset ghi đè cả `PlayerSave` nên field mới tự được xoá, nhưng phải có assert cho từng field mới.
- **Ghi đè `NextDouble`:** chưa kiểm trong runtime Unity (mục 4.1).
- **Vá scene `Boot.unity`:** tham chiếu giữ qua thay đổi scene có thể thành null (L-002).
- **Banner mẫu có 6 nhân vật, 3 nhân vật R đều là nhân vật khởi đầu:** 70% lượt quay (độ hiếm R) ra trùng ngay từ đầu trong khi mảnh chưa dùng được; sau khi ra SSR duy nhất thì mọi SSR sau đó cũng trùng. Ổn cho thử nghiệm, sẽ khác khi có thêm nhân vật.
- **Thống kê:** dung sai 5σ ở seed cố định không chứng minh thuật toán đúng với mọi seed; test logic bằng RNG kịch bản mới là bằng chứng chính, test thống kê chỉ bắt lỗi trọng số.
- **Offline:** người chơi sửa save hoặc đồng hồ để gian lận gacha là chấp nhận được ([D-003](../../decisions/decisions.md)).
