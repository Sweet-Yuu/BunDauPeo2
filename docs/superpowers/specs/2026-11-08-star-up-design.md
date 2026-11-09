# Spec: Nâng sao nhân vật

> Ngày: 2026-11-08 · Trạng thái: **đã duyệt** (user, 2026-11-08, cả thiết kế lẫn bản viết) · Phạm vi: tiêu mảnh để nâng sao (tối đa 5★), chỉ số trận theo sao, save sao, màn "Nhân vật" tạm, hiện ★ hiện tại ở thẻ chọn đội và màn lộ Gacha.
> Nguồn: [roadmap](../../roadmap.md) (dòng "Nâng sao nhân vật"), [game-design](../../design/game-design.md#nhân-vật) (mảnh, sao khởi điểm), [spec Gacha](2026-10-31-gacha-design.md) (mảnh trong `Roster`), [spec Combat](2026-11-02-combat-design.md) (chỉ số trong trận), [D-005](../../decisions/decisions.md) (save, reset), [D-012](../../decisions/decisions.md) (chữ qua `UiText`). Kế hoạch: [plans/2026-11-08-star-up.md](../plans/2026-11-08-star-up.md).
> **Đã xong, đây là lịch sử.** Khi spec khác code, [game-design](../../design/game-design.md#nhân-vật) hoặc [code-style.md](../../guides/code-style.md), những nơi đó thắng; không sửa spec này để theo kịp. Khác biệt đã biết: `TryStarUp` từ chối thêm khi giá ≤ 0 (`InvalidConfig`, sửa N1 sau review); PlayMode cuối nhánh là 16 test chứ không phải 14.

## 1. Mục tiêu

Mảnh từ Gacha có chỗ tiêu: người chơi nâng sao một nhân vật đã sở hữu, mỗi lần 1 sao, tối đa 5★. Nhân vật được nâng sao mạnh hơn trong trận.

Thành công khi:
1. Ở màn Nhân vật, chọn một nhân vật đủ mảnh rồi bấm Nâng sao: ★ tăng 1, mảnh giảm đúng giá, chỉ số trận trên panel tăng đúng công thức.
2. Vào trận với nhân vật đó: HP tối đa, Tấn công, Thủ của nó trong trận là số đã tăng; Tốc độ không đổi.
3. Tắt game rồi mở lại: sao và mảnh còn nguyên. Save cũ (chưa có dữ liệu sao) nạp được, mọi nhân vật đứng ở sao khởi điểm.
4. Reset Save Data xoá cả sao đã nâng lẫn mảnh: nhân vật khởi đầu quay về sao khởi điểm, mảnh về 0.
5. Thiếu mảnh hoặc đã 5★ thì nút mờ; gọi thẳng service cũng không đổi gì.
6. Mọi chữ mới đi qua `UiText`, có bản tiếng Anh; `HardcodedTextTests` xanh.

### Quyết định đã chốt với user (2026-11-08)

| Chủ đề | Quyết định |
|---|---|
| Hướng game | Sao **tăng chỉ số** (đưa vào MVP). [Vision](../../vision/vision.md) bỏ "Chỉ số nhân vật theo sao" khỏi "Ngoài phạm vi", chỉ còn "cấp nhân vật" (mục 8) |
| Giá mảnh | Theo **sao đích**, không theo độ hiếm: lên 2★ **2**, 3★ **4**, 4★ **6**, 5★ **8** mảnh (*khởi điểm*, trong SO). R từ 1★ lên 5★ cần 20 mảnh, SR từ 2★ cần 18, SSR từ 3★ cần 14 |
| Ảnh hưởng chỉ số | Mỗi sao **đã nâng** (trên sao khởi điểm) cộng **10%** chỉ số gốc vào **HP, Tấn công, Thủ** (*khởi điểm*, trong SO). Tốc độ, chỉ số quán, thu nhập AFK không đổi |
| Mảnh dư ở 5★ | Vẫn được cộng như bây giờ, để dành. Màn Nhân vật hiện số mảnh; chưa có chỗ tiêu |
| Chỗ nâng sao | Màn mới **Nhân vật** (`Characters.unity`), mở từ panel Cài đặt DEV như các màn khác. Tab Nhân vật của UI hub sau này dùng lại màn này |
| Công cụ thử | **Không** thêm nút DEV cộng mảnh. Playtest bằng cách quay Gacha (nút DEV "+1000 Gem" đã có) |

## 2. Hiện trạng (kiểm trong code ngày 2026-11-08, chỉ đọc)

- `CharacterDefinition` (`Characters`): `_stars` 1..5 là sao khởi điểm (mẫu R 1, SR 2, SSR 3, `ProjectSetup` điền), `Validate()` báo lỗi ngoài 1..5. `CombatStats {HP, Attack, Defense, Speed}`, `RestaurantStats {Cooking, Serving, Cashier}` nằm trong `CharacterStats.cs`.
- `Roster`: `AddFragments(id, count)`, `GetFragments(id)`; mảnh lưu ở `PlayerSave.Fragments` (`FragmentCount {CharacterId, long Count}`). Chưa có hàm tiêu mảnh.
- `GachaBannerDefinition._duplicateFragments` R/SR/SSR đều 1. Banner mẫu 6 nhân vật (3 R là nhân vật khởi đầu, 2 SR, 1 SSR); mỗi lượt quay, mỗi R ra mảnh khoảng 23%, mỗi SR 12,5%, SSR khoảng 5,4% (tính cả đảm bảo 50 lượt).
- Combat: `BattleUnit.ForAlly(FormationSlot)` chép thẳng `character.CombatStats`. `BattleSession(team, stage, config, random)` có 13 chỗ gọi (chủ yếu test). `CombatService` dựng `BattleSession` ở `EnterStage` và `StartBattle`, có 2 chỗ gọi constructor (`GameBootstrap`, `CombatTestData`).
- Sao đang hiện ở: `TeamCardView` (`character.Stars`), `GachaRevealView` (`character.Stars`).
- Chuyển màn hiện chỉ có ở panel Cài đặt DEV (`DevOverlay.SceneNames`: Quán, Gacha, Màn chơi, Chiến đấu). Mỗi màn dựng bằng một `*SceneBuilder` trong `Editor/`, `ProjectSetup` tạo scene khi chưa có và xếp Build Settings.
- Gem có được: thắng lần đầu cả 5 màn đánh là 1.200 Gem (khoảng 1 lần quay 10, 900 Gem); đánh lại 30–100 Gem mỗi lần.

## 3. Ngoài phạm vi (cố ý không làm)

- Cấp nhân vật, EXP, trang bị, kỹ năng mạnh lên theo sao.
- Sao ảnh hưởng chỉ số quán hay AFK.
- Chỗ tiêu mảnh dư (kho, cửa hàng đổi mảnh, đổi sang tiền tệ).
- Nâng nhiều sao một lần, hạ sao, hoàn mảnh.
- Hoạt hình nâng sao, âm thanh.
- Nút DEV cộng mảnh (user bỏ, mục 1).
- Màn kho mảnh riêng; UI hub.

## 4. Các phương án đã xét

| | A. `StarConfig` + `StarService` mới (**chọn**) | B. Giá trong `CharacterCatalog`, luật trong `Roster` | C. Bọc `OwnedCharacter` (định nghĩa + sao) |
|---|---|---|---|
| Cách làm | SO và service mới trong `Characters`; Combat nhận hàm "chỉ số của nhân vật" | Không thêm SO, không thêm field ở Boot | `Roster.GetBattleTeam()` trả kiểu mới, mọi chỗ gọi đổi theo |
| Sửa test cũ | Ít: tham số `BattleSession` tuỳ chọn, `Roster` không đổi constructor | Ít | Nhiều: Combat, UI, 20+ test |
| Ranh giới | Giống `GachaService` (service giữ luật, `Roster` giữ dữ liệu nhân vật) | `Roster` phình, catalog lẫn số kinh tế | Gọn về lâu dài |

Chọn A vì nhỏ nhất mà vẫn tách bạch luật với dữ liệu.

## 5. Thiết kế

### 5.1 Save (Core)

- File mới `Core/Scripts/StarUpEntry.cs`: `[Serializable] public struct StarUpEntry { public string CharacterId; public int Count; }`.
- `PlayerSave` thêm `public List<StarUpEntry> StarUps = new();`. `Count` là **số lần đã nâng**, không phải số sao tuyệt đối, để đổi sao khởi điểm trong SO sau này không lấy mất sao người chơi đã trả mảnh.
- **Save cũ:** JsonUtility để field thiếu ở giá trị khởi tạo, tức danh sách rỗng: mọi nhân vật ở sao khởi điểm ([D-005](../../decisions/decisions.md) luật 2). Có test nạp JSON cũ.
- **Reset:** `ResetToDefaults()` ghi đè bằng `new PlayerSave()` nên `StarUps` và `Fragments` đều rỗng; `Roster.NotifyChanged()` đã có làm view vẽ lại. `StarService` không giữ trạng thái chạy (chỉ đọc save), nên không cần nghe `ResetCompleted` (D-005 luật 3).
- Id lạ (nhân vật đã bị xoá khỏi catalog) được giữ nguyên trong save, như `Roster`. `Count` âm (save bị sửa tay) tính là 0.

### 5.2 `StarConfig` (SO, `Characters`)

```csharp
[CreateAssetMenu(menuName = "BunDauPeo/Characters/Star Config")]
public class StarConfig : ScriptableObject
{
    public const int MaxStars = 5;
    // Giá lên 2★, 3★, 4★, 5★: phần tử i là giá để lên (i + 2)★.
    [SerializeField] private int[] _costs = { 2, 4, 6, 8 };
    // % chỉ số gốc cộng thêm cho mỗi sao đã nâng (HP, Attack, Defense).
    [SerializeField] private int _statPercentPerStar = 10;

    public int CostToReach(int targetStars);   // 2..5; ngoài khoảng thì ArgumentOutOfRange
    public int StatPercentPerStar { get; }
    public List<string> Validate();            // _costs null hoặc độ dài != 4; phần tử < 1; _statPercentPerStar < 0
}
```

`OnValidate` chỉ `LogWarning` như các SO khác (L-016). Asset mẫu `Assets/_Project/Characters/Data/StarConfig.asset` (hằng `ProjectSetup.StarConfigPath`, cạnh `CharacterCatalog.asset`) do `ProjectSetup` tạo khi chưa có, không ghi đè số đã chỉnh.

### 5.3 `StarService` (`Characters`)

```csharp
public sealed class StarService
{
    public StarService(PlayerSave save, Roster roster, CharacterCatalog catalog, StarConfig config, Func<bool> persist);

    public bool IsConfigValid { get; }                       // config.Validate() rỗng, tính một lần ở constructor
    public int GetStarUps(string id);                        // số sao đã nâng có hiệu lực: clamp(Count, 0, MaxStars - sao khởi điểm); Id lạ hoặc null → 0
    public int GetStars(string id);                          // sao khởi điểm + GetStarUps; Id lạ → 0
    public int GetNextCost(string id);                       // giá lên sao kế; 0 khi đã 5★, Id lạ hoặc config lỗi
    public CombatStats GetCombatStats(CharacterDefinition character);  // null → ArgumentNullException
    public StarUpResult TryStarUp(string id);
}

public enum StarUpStatus { Success, InvalidConfig, Unknown, NotOwned, MaxStars, NotEnoughFragments }
public struct StarUpResult { public StarUpStatus Status; public int StarsAfter; public bool Saved; }
```

**Chỉ số theo sao** (`GetCombatStats`), tính bằng số nguyên `long` để không tràn:
- `HP`, `Attack`, `Defense` = `(gốc × (100 + StatPercentPerStar × GetStarUps) + 50) / 100` (làm tròn nửa lên; chỉ số gốc không âm theo `Validate` của Combat).
- `Speed` = gốc.
- Ví dụ Mắm Tôm (SR, 2★, HP 1000, Atk 110, Def 50) lên 3★: HP 1100, Atk 121, Def 55. Nem Chua (R, 1★, HP 800, Atk 80, Def 40) lên 5★: HP 1120, Atk 112, Def 56.
- Config lỗi thì trả chỉ số gốc (trận vẫn chạy được, lỗi đã báo qua `Validate`).

**`TryStarUp(id)`**, mọi bước có thể từ chối đứng trước bước đổi (như `UpgradeService.TryBuy`):
1. `!IsConfigValid` → `InvalidConfig`.
2. Id không có trong catalog → `Unknown`.
3. `!roster.IsOwned(id)` → `NotOwned`.
4. `GetStars(id) >= MaxStars` → `MaxStars`.
5. `roster.GetFragments(id) < GetNextCost(id)` → `NotEnoughFragments`.
6. Tăng `Count` của dòng `StarUps` (chưa có thì thêm dòng `Count = 1`; dòng có `Count` âm thì đặt về 1).
7. `roster.SpendFragments(id, cost)`: phát `Roster.Changed` một lần, sau khi cả sao lẫn mảnh đã đổi, nên view nghe sự kiện thấy trạng thái nhất quán.
8. `persist()` đúng một lần. `false` thì `Saved = false`, bộ nhớ giữ nguyên (lần lưu sau sẽ ghi), view báo "chưa lưu được dữ liệu" như Gacha.

Từ chối ở bước 1–5 thì save, mảnh, `Changed`, `persist` đều không đổi; `StarsAfter` = sao hiện tại.

### 5.4 `Roster.SpendFragments` (`Characters`)

`public bool SpendFragments(string id, long count)`: `false` và không đổi gì khi `count <= 0`, id `null`, hoặc mảnh đang có `< count`; thành công thì trừ, phát `Changed`. Để `public` như mọi hàm sửa khác của `Roster` (assembly `Characters` chưa có `InternalsVisibleTo`); ngoài `StarService` và test thì không ai gọi (review grep).

### 5.5 Combat

- `BattleSession` thêm tham số cuối tuỳ chọn `Func<CharacterDefinition, CombatStats> statsOf = null`; `null` thì dùng `character.CombatStats`. 13 chỗ gọi cũ không phải sửa.
- `BattleUnit.ForAlly(FormationSlot slot, CombatStats stats)`: dùng `stats` thay cho `character.CombatStats`. Quái không đổi.
- `CombatService` thêm tham số constructor `StarService stars` (bắt buộc, đặt sau `roster`), truyền `stars.GetCombatStats` vào cả hai chỗ dựng `BattleSession` (`EnterStage`, `StartBattle`). Sửa 2 chỗ gọi: `GameBootstrap`, `CombatTestData`.
- `Combat` đã tham chiếu `Characters` nên không đổi asmdef.

### 5.6 Boot và ProjectSetup

- `GameBootstrap` thêm `[SerializeField] StarConfig _starConfig`; null thì `Debug.LogError` kèm tên field rồi `return`, như `_catalog`. Sau khi tạo session: `var stars = new StarService(_session.Save, _session.Roster, _catalog, _starConfig, _session.Persist)`, đăng ký vào `ServiceLocator`, truyền vào `CombatService`.
- `ProjectSetup`: tạo `StarConfig.asset` khi chưa có; **vá** `Boot.unity` gán `_starConfig` khi trống (L-002: test đọc lại tham chiếu từ file); tạo `Characters.unity` bằng `CharactersSceneBuilder` khi chưa có; menu `Tools/BunDauPeo/Rebuild Characters Scene`; thêm scene vào Build Settings (sau các màn hiện có).

### 5.7 Màn Nhân vật (`Characters.unity`, `UI`)

Bố cục theo `Mota/character-list-en.png` (lưới thẻ có sao) và `Mota/character-upgrade.png` (nhân vật đang chọn, khung chỉ số). Chỉ lấy bố cục; không lấy tab Lv/trang bị/kỹ năng.

```
┌─────────────────────────────────────────────────────────────┐
│ Nhân vật                                          [Cài đặt] │
│ ┌──────┐┌──────┐┌──────┐   │  Mắm Tôm   SR  ★★★☆☆          │
│ │Nem   ││Chả   ││Đậu   │   │  HP        1000 → 1100         │
│ │R ★   ││R ★★  ││R ★   │   │  Tấn công   110 → 121          │
│ │3/4   ││1/6   ││0/2   │   │  Thủ         50 → 55           │
│ └──────┘└──────┘└──────┘   │  Tốc độ     110                │
│ ┌──────┐                   │  Mảnh: 5 / 6                   │
│ │Mắm   │                   │  [ Nâng lên 4★ (6 mảnh) ]      │
│ │SR ★★★│                   │  dòng trạng thái               │
│ └──────┘                   │                                │
└─────────────────────────────────────────────────────────────┘
```

- **Lưới trái:** nhân vật đã sở hữu, theo thứ tự sở hữu. Thẻ: ô chân dung màu theo độ hiếm (R xám, SR xanh, SSR vàng như Gacha), tên, độ hiếm, ★ hiện tại, "mảnh / giá" (5★ thì "Tối đa"). Thẻ đang chọn có viền vàng. Mở màn thì chọn sẵn thẻ đầu.
- **Panel phải:** tên, độ hiếm, ★ hiện tại trên 5 (★ và ☆); bốn dòng chỉ số. HP, Tấn công, Thủ ghi "hiện tại → sau khi nâng"; Tốc độ chỉ ghi một số. Đã 5★ thì mỗi dòng chỉ một số. Dòng "Mảnh: có / giá" (5★: "Mảnh: N").
- **Nút Nâng sao:** "Nâng lên N★ (G mảnh)". Mờ (`interactable = false`) khi thiếu mảnh hoặc 5★ (5★ ghi "Đã tối đa"). Bấm thì gọi `TryStarUp`; vẽ lại lưới và panel từ trạng thái sau khi trả về. Dòng trạng thái: `Success` + `Saved` → "Đã lên N★"; `Saved = false` → "Chưa lưu được dữ liệu"; status khác `Success` (chỉ gặp khi gọi lệch) → câu báo tương ứng.
- View nghe `Roster.Changed` để vẽ lại (Gacha hay Reset ở panel DEV làm đổi mảnh và sao).
- Hàm thuần cho chữ và hàng (`CharacterListRows`, `CharactersViewText`) để test không cần scene, như `TeamSelectRows`, `GachaViewText`.
- `CharactersView` lấy `Roster` và `StarService` từ `ServiceLocator`.
- Chữ trong `UI/Scripts/Text/UiText.Characters.cs` (key `characters.*`), nhãn cố định do builder dựng gắn `LocalizedText` (D-012). Bảng Combat chưa có tên chỉ số nên thêm `characters.stat_hp`, `stat_attack`, `stat_defense`, `stat_speed`.
- `DevOverlay`: thêm `CharactersScene = "Characters"` vào `SceneNames` và một nút chuyển màn "Nhân vật". Lưới đã kín nên thêm hàng dưới cùng: "Nhân vật" bên trái, "Reset dữ liệu" thu về nửa phải của cùng hàng (kế hoạch Task 6). Bố trí theo luật chống đè nút DEV (L-025, L-027): nút Cài đặt của màn không nằm dưới nút của màn.

### 5.8 Hiện ★ hiện tại ở chỗ khác

- `TeamSelectRow` thêm `int Stars`; `TeamSelectRows.Build(roster, stars)` lấy từ `StarService.GetStars`. `TeamCardView` vẽ `Row.Stars` thay cho `character.Stars`.
- `GachaRevealView`: ★ của nhân vật đang lộ đọc `StarService.GetStars(id)` (lấy từ `ServiceLocator`) sau khi lượt quay đã áp. Nhân vật mới thì bằng sao khởi điểm; nhân vật trùng thì ra sao hiện tại.
- `CharacterDefinition.Stars` giữ nguyên nghĩa "sao khởi điểm"; comment trong file đổi thành "sao khởi điểm; sao hiện tại ở `StarService.GetStars`".

## 6. Luồng chạy

```
Boot: GameSession nạp save → StarService(save, roster, catalog, starConfig, persist) → ServiceLocator → CombatService(…, stars, …)
Nâng: CharactersView → StarService.TryStarUp → StarUps.Count++ → Roster.SpendFragments (Changed) → persist → vẽ lại
Trận: CombatService.EnterStage → new BattleSession(team, stage, config, random, stars.GetCombatStats) → BattleUnit.ForAlly(slot, stats)
Reset: ResetToDefaults → new PlayerSave() (StarUps, Fragments rỗng) → Roster.NotifyChanged → view vẽ lại
```

## 7. Test

**EditMode** (ước tính, kế hoạch chốt số):

| Test | Bắt lỗi gì |
|---|---|
| `StarConfigTests` | Asset mẫu hợp lệ; `Validate` báo `_costs` null, sai độ dài, phần tử < 1, `%` âm; `CostToReach` đúng 2/4/6/8 và ném lỗi ngoài 2..5 |
| `StarServiceTests` | `GetStars` = khởi điểm khi chưa nâng; Count âm tính 0; Count vượt mức bị chặn ở 5★; Id lạ trả 0. `TryStarUp`: thành công trừ đúng giá, tăng 1 sao, `Changed` một lần, `persist` một lần; lần lượt từng lý do từ chối không đổi save, mảnh, `Changed`, `persist`; nâng liên tiếp từ 1★ lên 5★ tốn tổng 20 mảnh, lần thứ năm `MaxStars`; `persist` false thì `Saved = false` mà bộ nhớ vẫn đã nâng. `GetCombatStats`: đúng ví dụ mục 5.3, làm tròn nửa lên (một ca số lẻ), Speed không đổi, config lỗi trả gốc |
| `RosterTests` (thêm ca) | `SpendFragments`: trừ và phát `Changed`; `count <= 0` hoặc thiếu thì `false`, không đổi |
| `SaveCompatibilityTests` (thêm ca) | Save cũ không có `StarUps` đọc ra danh sách rỗng, mọi nhân vật ở sao khởi điểm |
| `GameSessionTests` (thêm assert vào test reset hiện có) | Sau reset `StarUps` và `Fragments` rỗng, nhân vật khởi đầu ở sao khởi điểm |
| `BattleSessionTests` / `BattleUnitTests` (thêm ca) | Có `statsOf` thì HP tối đa, Attack, Defense của phe ta theo hàm đó; không có thì theo SO (ca cũ giữ nguyên) |
| `CombatServiceTests` (thêm ca) | `EnterStage` với nhân vật đã nâng sao dựng trận có chỉ số đã tăng |
| `TeamSelectRowsTests` (thêm ca) | `Stars` của hàng là sao hiện tại |
| `CharacterListRowsTests`, `CharactersViewTextTests` | Hàng theo thứ tự sở hữu; chữ "mảnh / giá", "Tối đa", nhãn nút, dòng chỉ số "a → b", cả bản tiếng Anh |
| `CharactersSceneTests` | Đọc lại từ file scene: có `Camera`, `EventSystem` + `InputSystemUIInputModule`, `CharactersView`; mọi `LocalizedText` có key trong `UiText` |
| `ProjectSetupTests` (thêm ca) | `StarConfig.asset` tồn tại và hợp lệ; `Boot.unity` có `_starConfig`; Build Settings có `Characters`; chạy lại không ghi đè số đã chỉnh |
| `RestaurantSceneTests.DevOverlay_SceneTargetsAreInBuildSettings` (có sẵn) | Tự bắt `Characters` có trong `SceneNames` mà thiếu trong Build Settings; không sửa test |
| `HardcodedTextTests` | Tự quét `CharactersView`, `CharactersSceneBuilder`; không sửa test |

**PlayMode** (save tạm, theo `SmokeLoopTests`): Boot → cấp mảnh qua `Roster.AddFragments` → mở màn Nhân vật → bấm nút Nâng sao bằng `UiDriver` → `StarService.GetStars` tăng 1, mảnh giảm đúng giá → nạp lại save tạm vẫn giữ.

**Kiểm bằng mắt (lần dừng cho user):** chụp màn Nhân vật bằng `Capture1080("su-characters.png")` (L-026) ở tiếng Việt và tiếng Anh, có một thẻ 5★ và một thẻ thiếu mảnh. Playtest: DEV "+1000 Gem" → quay 10 → màn Nhân vật nâng một nhân vật R → vào màn 1-1 thấy HP và sát thương tăng → tắt, mở lại vẫn giữ sao → Reset dữ liệu thì về sao khởi điểm, mảnh 0.

## 8. Docs phải sửa

- Đã sửa 2026-11-08, user cho phép: [vision.md](../../vision/vision.md): mục "Ngoài phạm vi" đổi "Chỉ số nhân vật theo sao và cấp nhân vật (chưa thiết kế)" thành "Cấp nhân vật (chưa thiết kế)"; mục "Trong phạm vi" dòng mảnh ghi "nâng sao tăng HP, Tấn công, Thủ".
- Đã sửa 2026-11-08: [game-design.md](../../design/game-design.md#nhân-vật): dòng 27–29 ghi luật nâng sao của mục 1.
- Khi xong ([structure.md](../../project/structure.md), [roadmap.md](../../roadmap.md)): `StarConfig`, `StarService`, màn Nhân vật, số test; dòng UI hub ghi "tab Nhân vật dùng màn `Characters`".

## 9. Rủi ro

| Rủi ro | Cách giữ |
|---|---|
| Mảnh về chậm (1 mảnh mỗi lần trùng) nên người chơi ít khi nâng được | Số nằm trong SO (`StarConfig`, `_duplicateFragments` của banner); chỉnh khi cân bằng, không đổi code |
| Nhân vật nâng sao làm trận dễ hơn dự tính | Chỉ tăng 10%/sao, tối đa +40% (R); Tốc độ không đổi nên thứ tự lượt giữ nguyên |
| Đổi sao khởi điểm trong SO sau khi người chơi đã nâng | Save lưu số lần nâng; vượt 5★ thì bị chặn, chỉ số theo số sao có hiệu lực |
| Màn mới quên chữ tiếng Anh | `UiText` + `HardcodedTextTests` (D-012) |
| Nút Nhân vật trong panel DEV đè nút của màn | Luật chống đè (L-025, L-027) và test đè nút hiện có |
