# Spec: Combat

> Ngày: 2026-11-02 · Trạng thái: **đã duyệt** (user, 2026-11-02, gồm nút cốt truyện giữ chỗ) · **sửa 2026-11-03**: luật thể lực mới do user chốt (tối đa 100, mỗi màn đánh 10 kể cả boss, trừ một lần ngay khi vào trận và lưu ngay, không hoàn; thay luật "chỉ trừ khi thắng" đã duyệt hôm trước; nguồn [D-008](../../decisions/decisions.md)) · Phạm vi: trận đánh theo lượt, 5 class và 3 vị trí, quái và đợt (wave), màn chơi (chọn màn, bản đồ, màn cốt truyện giữ chỗ), thể lực, phần thưởng Material và Gem, tiến độ màn, các màn hình để chơi thử trọn vòng.
> Kiến trúc, `Wallet`, `Roster`, `SaveService`, luật phụ thuộc asmdef: [spec nền móng](2026-10-30-foundation-architecture-design.md). Luật chơi: [game-design.md](../../design/game-design.md#chiến-đấu). Mẫu định dạng và cách nối vào `Boot`: [spec Restaurant](2026-11-01-restaurant-design.md). Kế hoạch: [plans/2026-11-02-combat.md](../plans/2026-11-02-combat.md).
> **Đã xong, đây là lịch sử.** Khi spec khác code hoặc [game-design.md](../../design/game-design.md), code và game-design thắng; không sửa spec này để theo kịp.

## 1. Mục tiêu

Người chơi chọn một màn trên bản đồ, xếp đội 1 đến 5 người (nhân vật vào đội thì rời quán), rồi đánh một trận theo lượt: mỗi nhân vật có đánh thường, skill có hồi chiêu và ultimate cần năng lượng; quái đến theo từng đợt. Vào trận thì trừ thể lực ngay (thua cũng không hoàn); thắng thì nhận Material và Gem và mở màn kế tiếp. Có màn hình đầy đủ để thử trọn vòng này khi chưa có UI hub.

### Quyết định đã chốt với user

| Chủ đề | Quyết định | Nguồn |
|---|---|---|
| Lượt | Vòng theo tốc độ: mỗi vòng ai còn sống đánh đúng 1 lần, xếp theo Speed giảm dần; hoà thì phe ta trước, rồi theo ô. Không ngẫu nhiên trong sát thương, không chí mạng | user, 2026-11-02 |
| Đội hình | **Class quyết định vị trí, hệ thống tự xếp.** 5 ô: **Front 2** (Đỡ đòn, Đấu sĩ), **Mid 2** (Hỗ trợ, Xạ thủ), **Back 1** (Pháp sư). Hàng đầy thì người thừa tràn sang hàng kế. Người chơi chỉ chọn ai vào đội, **chưa tự xếp ô** (để dành, không đổi save khi làm sau) | user, 2026-11-02 |
| Vai trò của vị trí | Chỉ ảnh hưởng quái nhắm ai: trọng số front 3, mid 2, back 1 | user, 2026-11-02 |
| Màn hình | Phe ta bên trái, năm nhân vật không đứng chồng lên nhau (kiểu Princess Connect); chọn chiêu kiểu Fate/Grand Order, từng nhân vật đánh theo tốc độ | user, 2026-11-01 |
| Công cụ trong trận | Giữ **AUTO**, **tốc độ x1/x2**, **tạm dừng và bỏ cuộc**. Cắt nút SET, thống kê sát thương, 3 sao | user, 2026-11-02 |
| Độ dài | **1 chương: 5 màn đánh và 2 màn cốt truyện** (7 nút trên bản đồ, mục 3.5) | user, 2026-11-02 |
| Thưởng | Thắng lần đầu: Material + Gem (khởi điểm **30 Material, 150 Gem** ở màn đầu, tăng dần). Đánh lại: Material như cũ, **Gem khoảng 1/5** để cày Gem chậm hơn cày Material. Số nằm trong `StageDefinition` | user, 2026-11-02 |
| Thể lực | Tối đa **100**, hồi 1 điểm mỗi 5 phút theo giờ máy kể cả khi tắt game. Mỗi màn đánh tốn **10**, kể cả boss; màn cốt truyện 0. Trừ **một lần ngay khi vào trận hợp lệ** và lưu ngay; thua, bỏ cuộc, tắt app giữa trận đều **không hoàn**; "Thử lại" là lần vào mới, trừ lại. Không mua bằng Gem. **Luật đổi so với [vision](../../vision/vision.md) và [ui-screens.md](../../references/ui-screens.md)**. Cách vào trận có trả thể lực ở mục 4.5 và 4.7 | user nêu 2026-11-02; **sửa theo user chốt 2026-11-03** (master plan mục 20.7, [D-008](../../decisions/decisions.md)); luật cũ "tối đa 60, boss 15, chỉ trừ khi thắng" bỏ |
| Màn cốt truyện | User muốn có. Vision ghi cốt truyện làm sau MVP và chưa có spec hội thoại, nên bản này chỉ làm **nút giữ chỗ** (mục 3.5, 5.2). Hội thoại thật vẫn là spec sau MVP. **User duyệt cùng spec** | user nêu và duyệt, 2026-11-02 |
| Đội | Tối thiểu 1 người để vào trận, dưới 5 người phải qua hộp xác nhận | user, 2026-11-01 |
| Nhân vật chỉ ở một nơi | Đã có ở `Roster`: `AddToTeam` gỡ khỏi quán, `AssignStaff` gỡ khỏi đội | spec nền móng |

Mọi số cân bằng trong spec là **số khởi điểm đặt trong ScriptableObject**, chỉnh trong Inspector không cần sửa code. Riêng luật đội hình (class nào đứng hàng nào, 2-2-1 ô) nằm trong code, vì `BattleLayout` vẽ đúng 5 ô cố định: đổi số ô trong Inspector sẽ làm hỏng màn hình.

### Vision

Không sửa [vision.md](../../vision/vision.md) (chỉ user sửa). Hai chỗ lệch cần user tự cập nhật hoặc xác nhận khi duyệt spec: (1) thêm **thể lực** vào "Trong phạm vi" (chữ "stamina" ở dòng "không làm" của [ui-screens.md](../../references/ui-screens.md) đã bỏ ngày 2026-11-02); (2) "Cốt truyện và hội thoại" vẫn ngoài MVP, chỉ có nút giữ chỗ.

## 2. Cấu trúc và ranh giới asmdef

```
Core  <-  Characters  <-  Gacha, Restaurant, Combat  <-  UI, Boot
```

| Assembly | Thêm mới / thay đổi |
|---|---|
| `Core` | `StaminaState` (save); field mới của `PlayerSave` |
| `Characters` | `CharacterClass` (enum); `CharacterDefinition` thêm `_class` |
| `Combat` (asmdef mới, chỉ tham chiếu `Core` và `Characters`; đã có folder rỗng) | SO: `CombatConfig`, `EnemyDefinition`, `WaveDefinition` (class `Serializable`), `StageDefinition`, `StageCatalog`. Logic thuần: `BattleUnit`, `DamageCalculator`, `BattleFormation`, `BattleSession`, `AutoPolicy`, `CombatSkillRules`, `StaminaService`, `StageProgressService`, `CombatService` |
| `UI` (thêm tham chiếu `Combat`) | `StageMapView`, `StageInfoPopup`, `TeamSelectPopup`, `StoryPlaceholderPopup`, `BattleView`, `BattleLayout`, `BattleActionPanel`, `BattleResultView`, `BattlePausePopup`, `CombatViewText`; `DevOverlay` thêm nút |
| `Boot` (thêm tham chiếu `Combat`) | `GameBootstrap` nối `CombatService` |
| `Editor` (thêm tham chiếu `Combat`) | `ProjectSetup` sinh dữ liệu mẫu, `StageSelectSceneBuilder`, `BattleSceneBuilder`, vá `Boot.unity` |
| `Tests` | thêm tham chiếu `Combat` |

`Combat` không tham chiếu `Gacha`, `Restaurant`, `Boot`, `UI`. Bản này không sửa file nào của `Restaurant`.

### Rà từng tham chiếu SO với sơ đồ (L-001)

| Từ | Tới | Tầng | Hợp lệ |
|---|---|---|---|
| `CharacterDefinition` (Characters) | `CharacterClass` (Characters) | cùng assembly | có |
| `EnemyDefinition` (Combat) | `SkillDefinition`, `CombatStats` (Characters) | tầng dưới | có |
| `WaveDefinition` (Combat) | `EnemyDefinition` (Combat) | cùng assembly | có |
| `StageDefinition : GameDefinition` (Combat, `GameDefinition` ở Core) | `WaveDefinition`; `CurrencyAmount` (Core) | cùng assembly, tầng dưới | có |
| `StageCatalog : Catalog<StageDefinition>` (Combat) | `StageDefinition` | cùng assembly | có |
| `CombatConfig` (Combat) | `StageCatalog` (Combat) | cùng assembly | có |
| `PlayerSave` (Core) | chỉ chuỗi và số (`ClearedStages`, `StaminaState`) | không trỏ tới Combat | có |
| `StaminaService` (Combat) | `StaminaState` (Core); giờ nhận qua `Func<DateTime>` | tầng dưới | có; `Combat` không được tham chiếu `IClock` ở `Restaurant`, nên chỉ nhận hàm trả giờ |
| `StageProgressService` (Combat) | `Wallet` (Core), `PlayerSave` | tầng dưới | có |

`StageDefinition` nằm cùng `EnemyDefinition` trong `Combat` đúng như [spec nền móng](2026-10-30-foundation-architecture-design.md) mục 3. Hai chỗ **khác** bản nháp ở đó: bỏ trường "màn cần clear trước" (màn mở theo thứ tự trong `StageCatalog`, mục 4.6), và `EnemyDefinition` có một đánh thường và tối đa một skill thay vì "danh sách skill".

## 3. Mô hình dữ liệu

### 3.1 Save (`Core`)

```csharp
[Serializable] public class StaminaState {
    public int  Current;            // chỉ có nghĩa khi AnchorUtcTicks != 0
    public long AnchorUtcTicks;     // 0 = đầy; khác 0 = lúc bắt đầu khoảng hồi đang chạy (đủ một khoảng thì +1)
}

// PlayerSave: thêm hai field, có giá trị khởi tạo để save cũ vẫn đọc được.
public List<string> ClearedStages = new();   // Id màn đã clear (cả màn đánh và màn cốt truyện)
public StaminaState Stamina = new();
```

- `Version` giữ `1`: chỉ thêm field có giá trị khởi tạo. Test dựng JSON save cũ không có hai field này.
- **`AnchorUtcTicks == 0` nghĩa là thể lực đầy** (kể cả khi `Max` thay đổi). Nhờ vậy game mới, save cũ và Reset Save Data tự đầy mà `GameSession.GrantNewGame` không phải đổi gì.
- Trận đang đánh **không được lưu**: tắt app giữa trận là bỏ trận, không có thưởng. Thể lực đã trừ và đã lưu lúc vào trận nên **không lấy lại được** bằng cách tắt app (mục 4.5).

### 3.2 `CharacterClass` và `CharacterDefinition` (`Characters`)

```csharp
public enum CharacterClass { Unset = 0, Tank = 1, Fighter = 2, Support = 3, Marksman = 4, Mage = 5 }   // Đỡ đòn, Đấu sĩ, Hỗ trợ, Xạ thủ, Pháp sư
```

`CharacterDefinition` thêm `[SerializeField] CharacterClass _class` và property `Class`. `Validate()` báo lỗi khi `Unset` (asset cũ thiếu field được Unity điền 0; `ProjectSetup` điền theo bảng mẫu bên dưới, cùng cách điền `_stars`). Giá trị số ghim, chỉ thêm ở cuối.

Class **không có luật riêng** ngoài hai việc: quyết định vị trí ưu tiên (mục 4.1) và để người chơi nhìn vai trò. Đỡ đòn khác Đấu sĩ ở chỉ số và skill trong SO của nhân vật, không phải ở code.

Class cho 6 nhân vật mẫu (số chiến đấu hiện có giữ nguyên): Nem Chua **Đấu sĩ**, Chả Cốm **Xạ thủ**, Đậu Phụ **Hỗ trợ** (ba nhân vật khởi đầu, có sẵn skill Heal), Bún Lá **Đấu sĩ**, Lòng Dồi **Đỡ đòn**, Mắm Tôm **Pháp sư**. Đủ cả 5 class để thử đội 5 người.

### 3.3 SO của `Combat`

| SO | Trường (private `[SerializeField]`, property đọc cùng tên) | Ghi chú |
|---|---|---|
| `EnemyDefinition` (không `Id`, không được save tham chiếu) | `_displayName`, `_stats` (`CombatStats`), `_basicAttack` (`SkillDefinition`, Kind Basic), `_skill` (`SkillDefinition`, Kind Skill, **có thể trống**), `_isBoss` | Quái không có ultimate và không có năng lượng |
| `WaveDefinition` (`[Serializable] class`) | `EnemyDefinition[] _enemies` | 1 đến 5 quái; thứ tự trong mảng là thứ tự ô |
| `StageDefinition : GameDefinition` | `_displayName`, `_kind` (`StageKind { Battle = 0, Story = 1 }`), `_waves` (`WaveDefinition[]`), `_staminaCost`, `_firstClearReward`, `_repeatReward` (`CurrencyAmount[]`) | Màn Story không có wave, không tốn thể lực, không thưởng |
| `StageCatalog : Catalog<StageDefinition>` | | **Thứ tự mảng là thứ tự màn**, mục 4.6 |
| `CombatConfig` | bảng 3.4 | `StageCatalog` nằm trong đây |

`StageDefinition.Validate()` báo: `Id` trống; Battle mà không có wave, wave rỗng hoặc quá 5 quái hoặc có quái null; `_staminaCost < 1` ở Battle hoặc `!= 0` ở Story; Battle thiếu `_firstClearReward`; thưởng có dòng `<= 0` hoặc loại `Coin` (Coin chỉ từ quán, [game-design](../../design/game-design.md#tiền-tệ)); Story mà có wave hoặc thưởng; quái lỗi `Validate()`. `EnemyDefinition.Validate()` báo: HP, Attack, Speed `<= 0`, Defense `< 0`; thiếu đánh thường; skill sai Kind; skill không phải `Damage` (quái chỉ gây sát thương); skill sai luật ở `CombatSkillRules` (dưới).

**`CombatSkillRules`** (static, `Combat`): `Validate(SkillDefinition, SkillKind)` báo `Power <= 0`; `Effect = Damage` mà `Target` không phải `SingleEnemy`/`AllEnemies`; `Effect = Heal` mà `Target` không phải `SingleAlly`/`AllAllies`/`Self`; `Kind = Skill` mà `Cooldown < 1`; `Kind = Ultimate` mà `EnergyCost < 1`. Để ở `Combat` thay vì `SkillDefinition.Validate()` để không làm đỏ các test và asset hiện có của `Characters`; `CombatService.StartBattle` từ chối đội có nhân vật lỗi (mục 4.7) và test kiểm 4 skill mẫu hợp lệ.

### 3.4 `CombatConfig`

| Nhóm | Field | Khởi điểm |
|---|---|---|
| Nhắm mục tiêu | `_frontWeight`, `_midWeight`, `_backWeight` | 3, 2, 1 |
| Năng lượng | `_maxEnergy`, `_energyOnBasic`, `_energyOnSkill`, `_energyOnHit` | 100, 25, 10, 15 |
| Giới hạn | `_maxRounds` | 60 (cả trận; quá thì thua, chặn trận không kết thúc vì hồi máu liên tục) |
| AUTO | `_autoHealBelow` | 0,7 |
| Thể lực | `_maxStamina`, `_staminaRegenSeconds` | 100, 300 (1 điểm mỗi 5 phút, từ 0 lên đầy mất 500 phút, tức 8 giờ 20) |
| Màn | `_stages` (`StageCatalog`) | 7 màn mẫu, mục 3.5 |

`BattlePosition { Front = 0, Mid = 1, Back = 2 }` nằm trong `Combat`. `Validate()` (kèm `OnValidate` chỉ `LogWarning`, L-016: mọi field làm mẫu số, ngưỡng hoặc chỉ số mảng đều có mặt) báo: trọng số `<= 0`; `_maxEnergy < 1`; mỗi mức năng lượng `< 0`; `_maxRounds < 1`; `_autoHealBelow` ngoài `(0, 1]`; `_maxStamina < 1`; `_staminaRegenSeconds <= 0` (chia cho 0); `_stages` null hoặc `Validate()` còn lỗi, hoặc có `_staminaCost > _maxStamina` (màn không bao giờ vào được).

### 3.5 Dữ liệu mẫu

Quái mẫu (số khởi điểm, chọn bằng mô phỏng nhanh ngoài repo để ba nhân vật khởi đầu thắng được màn 1 đến 3 và đội năm người đủ class thắng được boss; cân bằng thật làm sau):

| Quái | HP | Attack | Defense | Speed | Skill |
|---|---|---|---|---|---|
| Ruồi Bàn | 500 | 60 | 30 | 90 | không |
| Chuột Bếp | 900 | 85 | 45 | 100 | không |
| Gián Kho | 1300 | 110 | 60 | 85 | "Cào mạnh": đánh 1 người, hệ số 1,8, hồi chiêu 3 |
| Chuột Chúa (boss) | 7000 | 135 | 70 | 105 | "Gầm": đánh cả phe ta, hệ số 1,0, hồi chiêu 3 |

Chương 1, bảy nút theo thứ tự (Id dạng `st_1_story1`, `st_1_1`, …):

| Nút | Loại | Đợt | Thể lực | Thưởng lần đầu | Thưởng đánh lại |
|---|---|---|---|---|---|
| Cốt truyện 1 | Story | | 0 | không | không |
| 1-1 | Battle | 1: 2 Ruồi | 10 | 30 Material + 150 Gem | 30 Material + 30 Gem |
| 1-2 | Battle | 1: 2 Ruồi; 2: 3 Ruồi | 10 | 40 Material + 150 Gem | 40 Material + 30 Gem |
| Cốt truyện 2 | Story | | 0 | không | không |
| 1-3 | Battle | 1: Ruồi, Chuột; 2: 2 Chuột; 3: Ruồi, 2 Chuột | 10 | 50 Material + 200 Gem | 50 Material + 40 Gem |
| 1-4 | Battle | 1: 2 Chuột; 2: Gián, Chuột; 3: 2 Gián, Chuột | 10 | 60 Material + 200 Gem | 60 Material + 40 Gem |
| 1-5 (boss) | Battle | 1: Chuột, Gián; 2: 2 Gián, Chuột; 3: Chuột Chúa | 10 | 100 Material + 500 Gem | 100 Material + 100 Gem |

Tổng thưởng lần đầu: 280 Material, 1200 Gem (khoảng 13 lượt quay, một lần quay 10 tốn 900 Gem), so với 345 Material của toàn bộ nâng cấp quán. Clear lần đầu cả 5 màn tốn 50 thể lực, nửa thanh thể lực đầy. Màn **1-1 chỉ một đợt** để mốc chơi thử đầu tiên là "một màn, một đợt". Nút Story đặt ở đầu chương và giữa chương; tên là chữ giữ chỗ, user thay khi có cốt truyện.

## 4. Logic

### 4.1 Đội hình (`BattleFormation`, static thuần)

```csharp
public const int FrontSlots = 2, MidSlots = 2, BackSlots = 1;   // tổng bằng Roster.MaxBattleTeamSize, test kiểm
public static BattlePosition PreferredPosition(CharacterClass c);   // switch: Tank, Fighter → Front; Support, Marksman → Mid; Mage → Back
public static IReadOnlyList<FormationSlot> Assign(IReadOnlyList<CharacterDefinition> team);   // FormationSlot { Character, Position, IndexInPosition, SlotIndex }
public static TeamEntryStatus GetEntryStatus(int teamSize);    // Empty (0), NeedsConfirm (1..4), Ready (5), TooMany (>5)
```

`Assign` duyệt đội theo thứ tự trong `Roster.GetBattleTeam()`; mỗi nhân vật vào hàng ưu tiên của class nếu còn ô, nếu đầy thì thử hàng **gần nhất**: Front → Mid → Back, Mid → Front → Back, Back → Mid → Front. Đội đủ 5 người tối đa 5 ô nên luôn có chỗ. `SlotIndex` là số thứ tự chung của phe ta: Front 0, 1; Mid 2, 3; Back 4. Mọi chỗ "ô thấp" của phe ta trong spec dùng số này, để thứ tự lượt và mục tiêu mặc định không phụ thuộc cách cài đặt. Kết quả là hàm của đội, không lưu gì trong save: **thêm "tự xếp ô" sau này cần đổi save**, và đó là lý do để dành. Đội 5 người một class (5 Đấu sĩ) thành Front 2, Mid 2, Back 1: vị trí là hệ quả, không phải lựa chọn.

### 4.2 Công thức (`DamageCalculator`, static thuần)

- Sát thương = `max(1, Round(Attack × Power × 100 / (100 + Defense)))`, `Round` làm tròn nửa lên, tính bằng `double`.
- Hồi máu = `Round(Attack_người_hồi × Power)`, không vượt máu tối đa; sự kiện ghi số máu **thực sự** hồi.
- Không chí mạng, không né, không ngẫu nhiên ở sát thương. Ngẫu nhiên duy nhất của trận là quái chọn mục tiêu (mục 4.4).

### 4.3 `BattleSession`

```csharp
public BattleSession(IReadOnlyList<CharacterDefinition> team, StageDefinition stage, CombatConfig config, System.Random random);

public StageDefinition Stage { get; }      // StageProgressService đọc Id màn từ đây
public BattleState State { get; }          // PlayerTurn, EnemyTurn, Won, Lost
public int WaveIndex, WaveCount, Round { get; }
public BattleUnit Current { get; }         // người sắp hành động
public IReadOnlyList<BattleUnit> Allies, Enemies { get; }    // Enemies: đợt hiện tại
public IReadOnlyList<BattleUnit> RemainingOrder { get; }     // còn lại trong vòng này, để vẽ thanh lượt
public bool EntryPaid { get; internal set; }                 // đã trả thể lực lúc vào; chỉ CombatService.EnterStage đặt. Trận từ StartBattle (dải DEV) luôn false
public bool RewardsClaimed { get; internal set; }            // chỉ StageProgressService đặt

public ActionAvailability GetAvailability(BattleActionKind kind);   // Available, OnCooldown, NotEnoughEnergy, NoSuchSkill, NotYourTurn, Ended
public BattleUnit DefaultTarget(BattleActionKind kind);              // null nếu kỹ năng không cần mục tiêu
public BattleActionResult PlayerAct(BattleActionKind kind, BattleUnit target = null);
public BattleActionResult EnemyAct();                                // chỉ khi State == EnemyTurn
public BattleActionResult Surrender();                               // → Lost, không thưởng
```

`BattleActionKind { Basic, Skill, Ultimate }`. `BattleActionResult { Status (Success, NotYourTurn, Unavailable, InvalidTarget, Ended), Actor, Skill, Hits (danh sách {Target, Amount, IsHeal, Killed}), WaveCleared, NextWaveStarted, StateAfter }`. Trận chạy **đồng bộ và tức thì**; view phát hoạt hình từ danh sách kết quả, nên logic không đợi view và test không cần Play.

**Vòng và lượt:**
1. Bắt đầu đợt: dựng `BattleUnit` quái, sắp mọi người còn sống theo Speed giảm; hoà thì phe ta trước, rồi ô thấp trước. Người chết giữa vòng bị bỏ qua. Một vòng hết thì sắp lại từ đầu (Speed không đổi trong trận nên thứ tự chỉ đổi theo người chết/sinh ra), `Round` tăng.
2. Khi tới lượt một người: hồi chiêu skill giảm 1 nếu còn dương, **trước** khi chọn. Hồi chiêu N nghĩa là dùng ở lượt t thì dùng lại được ở lượt t+N của chính nhân vật (N = 3: dùng ở lượt 1, dùng lại ở lượt 4); lượt đầu skill sẵn sàng. `Cooldown = 1` tức là dùng mỗi lượt.
3. Hành động: **Basic** luôn dùng được, +`_energyOnBasic`. **Skill** dùng được khi hồi chiêu = 0, +`_energyOnSkill`, đặt hồi chiêu = `Cooldown`. **Ultimate** dùng được khi năng lượng ≥ `min(EnergyCost, _maxEnergy)`, trừ đúng chi phí đó, không cộng năng lượng. Năng lượng tối đa `_maxEnergy`, bắt đầu **0** mỗi trận. Mỗi lần một nhân vật phe ta bị trúng đòn (sát thương thực `> 0`) +`_energyOnHit`. Quái không có năng lượng.
4. Mục tiêu: `Damage` + `SingleEnemy` nhắm một quái sống (người chơi chọn, trống thì quái sống đầu tiên theo ô); `AllEnemies` đánh mọi quái sống; `Heal` + `SingleAlly` nhắm một người phe ta **còn sống** (mặc định người có tỉ lệ HP thấp nhất, hoà thì ô thấp); `AllAllies` hồi mọi người sống; `Self`. Không hồi sinh. Mục tiêu chết hoặc sai phe thì `InvalidTarget`, không đổi gì. Chiêu không cần chọn mục tiêu (`AllEnemies`, `AllAllies`, `Self`) thì bỏ qua tham số `target`, để view truyền mục tiêu đang chọn mà không bị từ chối.
5. Sau mỗi hành động: hết quái trong đợt → nếu còn đợt thì **đợt kế bắt đầu** (HP, năng lượng, hồi chiêu giữ nguyên, `NextWaveStarted`; đây là một vòng mới: `Round` tăng 1, sắp lượt lại, người chưa kịp đánh ở vòng cũ không được đánh bù), hết đợt cuối thì `Won`; hết người phe ta, hoặc `Round > _maxRounds`, hoặc `Surrender`, thì `Lost`. Thắng và thua đều **không đổi dữ liệu nào ngoài bộ nhớ của trận**: thể lực trừ lúc vào trận (`CombatService.EnterStage`, mục 4.7), thưởng và lưu do `StageProgressService` (mục 4.6).

### 4.4 Quái và AUTO

- **Quái:** dùng skill nếu có và hồi chiêu = 0, ngược lại đánh thường. Đánh một người thì chọn theo trọng số vị trí của những người **còn sống** (front 3, mid 2, back 1): đúng **một** lần `random.NextDouble()` cho mỗi hành động nhắm một mục tiêu, kể cả khi chỉ còn một người, để số lần gọi RNG là hợp đồng test (`ScriptedRandom` có sẵn). Hành động đánh cả phe ta không gọi RNG.
- **`AutoPolicy.Choose(BattleSession)`** (static thuần, dùng cho nút AUTO): xét Ultimate rồi skill, chọn chiêu đầu tiên dùng được và "đáng dùng"; còn lại đánh thường. "Đáng dùng" là chiêu `Damage`, hoặc chiêu `Heal` mà có người phe ta sống dưới `_autoHealBelow` × HP tối đa (áp dụng cho cả Ultimate hồi máu, để AUTO không đốt năng lượng khi cả đội đầy máu). Mục tiêu là `DefaultTarget`. AUTO tắt khi người chơi bấm lại, hoặc khi trận hết.

### 4.5 Thể lực (`StaminaService`) — luật mới, sửa 2026-11-03 (user chốt, [D-008](../../decisions/decisions.md))

```csharp
public StaminaService(PlayerSave save, CombatConfig config, Func<DateTime> utcNow);   // Boot truyền () => DateTime.UtcNow, test truyền giờ giả
public int Current { get; }               // đã cộng phần hồi theo giờ máy
public int Max { get; }
public double SecondsToNext { get; }      // 0 khi đầy
public bool CanAfford(int cost);
public StaminaSpendStatus TrySpend(int cost, Func<bool> persist);   // Spent, Rejected (cost < 0 hoặc thiếu: không đổi gì), SaveFailed (đã hoàn đúng trạng thái cũ)
public void Refill();                     // chỉ cho dải DEV
```

- **Hồi:** `n = floor((now − Anchor) / _staminaRegenSeconds)`; `Current = min(Max, Current + n)`; `Anchor += n × regen`; đầy thì `Anchor = 0`. Hồi theo **giờ máy kể cả khi tắt game**, tối đa `Max`, nên vắng 8 giờ 20 là đầy. Đồng hồ lùi (`now < Anchor`) thì không hồi và đặt `Anchor = now`; `Current` không bao giờ giảm vì đồng hồ. Chỉnh đồng hồ tiến để gian lận thì chấp nhận, như [AFK](../../design/game-design.md#afk) (game offline, đã có trần).
- **Trừ khi nào:** **một lần, ngay khi vào trận hợp lệ** (`CombatService.EnterStage`, mục 4.7), mỗi màn đánh `_staminaCost` (10, kể cả boss), màn cốt truyện 0. **Thắng, thua, bỏ cuộc hay tắt app giữa trận đều không hoàn.** "Thử lại" sau khi thua là một lần vào mới nên trừ lại. Lý do: trận không khôi phục được, nên nếu chỉ trừ khi thắng thì người chơi thoát ra khi sắp thua để giữ thể lực; trừ lúc vào thì thể lực là giá của một lần thử.
- **`TrySpend(cost, persist)` làm trọn một bước nguyên tử:** (1) hồi phần đến giờ; (2) `cost < 0` hoặc thiếu thì trả `Rejected`, không đổi gì và không gọi `persist`; `cost == 0` thì trả `Spent`, không đổi gì và không gọi `persist`; (3) chụp `Current` và `Anchor` thô, trừ; (4) gọi `persist()` **đúng một lần**; (5) `persist` trả `false` thì **đặt lại đúng `Current` và `Anchor` đã chụp** và trả `SaveFailed`, như chưa từng trừ. Phải lưu ngay khi trừ để tắt app không lấy lại được thể lực; không lưu được thì không cho vào trận, thay vì để thể lực trừ trong bộ nhớ mà đĩa còn nguyên.
- Trừ từ đầy thì `Anchor = now` (bắt đầu hồi); trừ khi chưa đầy thì giữ `Anchor`, phần hồi dở không mất. `Max` giảm trong config thì `Current` bị kẹp lại khi đọc. Đầy thì không tích lượt hồi (`Anchor = 0`), nên vắng lâu cũng chỉ đầy 100.
- Không mua thể lực bằng Gem, không có vật phẩm hồi thể lực (ngoài phạm vi). Reset Save Data ghi đè `PlayerSave` nên thể lực về đầy.

### 4.6 Tiến độ màn và thưởng (`StageProgressService`)

```csharp
public StageProgressService(PlayerSave save, Wallet wallet, StageCatalog catalog, StaminaService stamina, Func<bool> persist);
public StageStatus GetStatus(string id);                      // Unknown, Locked, Available, Cleared
public StageEnterStatus CanEnter(string id, int teamSize);    // chỉ hỏi, không đổi gì: Ready, Unknown, Invalid, WrongKind, Locked, EmptyTeam, NotEnoughStamina
public StageCompleteResult CompleteBattle(BattleSession won); // Status, FirstClear, Rewards, StaminaSpent (thể lực đã trả lúc vào, chỉ để hiện), Saved
public StageCompleteResult CompleteStory(string id);          // Status, Saved
// StageCompleteStatus: Success, NotWon, AlreadyClaimed, AlreadyCleared, Unknown, Invalid, WrongKind, Locked, NotPaid
public event Action Changed;
```

- **Mở màn:** màn thứ `i` trong `StageCatalog` mở khi `i = 0`, **hoặc** màn `i − 1` đã clear, **hoặc** chính nó đã clear (để chèn màn vào giữa catalog sau này không khoá lại người đã đi qua). Id lạ trong `ClearedStages` bị bỏ qua, giữ nguyên trong save.
- **`CanEnter`** kiểm theo thứ tự: tra id → `Validate()` còn lỗi → là màn Battle → mở rồi → có ít nhất 1 người → `StaminaService.CanAfford`. Không đổi gì; view dùng nó để làm mờ nút và hiện lý do, còn việc vào trận thật do `CombatService.EnterStage` (mục 4.7) gọi lại nó.
- **`CompleteBattle`** chỉ trao thưởng cho trận **đã trả thể lực**, không đụng tới thể lực. Thứ tự (hợp đồng, test bắt từng bước): `won.State == Won` → `RewardsClaimed == false` → `won.EntryPaid` (trận từ `StartBattle` của dải DEV thì `NotPaid`, không đổi gì) → tra màn, hợp lệ, đã mở → chọn thưởng (**lần đầu** nếu chưa có trong `ClearedStages`, ngược lại **đánh lại**) → `Wallet.Add` từng dòng → thêm Id vào `ClearedStages` nếu lần đầu → `won.RewardsClaimed = true` → `Changed` → **`persist()` đúng một lần**; `false` thì `Saved = false`, bộ nhớ giữ nguyên (`OnApplicationPause`/`Quit` sẽ lưu lại), view báo "chưa lưu được dữ liệu" như AFK. Gọi lần hai với cùng trận trả `AlreadyClaimed`, không đổi gì (chặn cộng đôi thưởng khi view lỡ gọi hai lần). `StaminaSpent` trong kết quả là `Stage.StaminaCost` của trận đã trả, chỉ để hiện "Thể lực đã dùng".
- **`CompleteStory`**: màn Story đã mở và chưa clear thì thêm Id, `Changed`, `persist()` một lần; đã clear thì `AlreadyCleared`, không đổi gì và không lưu.
- **Khi nào ghi save:** ở `EnterStage` (trừ thể lực, kèm đội hiện tại; đúng một lần), `CompleteBattle` (thưởng và tiến độ; một lần) và `CompleteStory`. Đổi đội (`Roster.AddToTeam`/`RemoveFromTeam`) ghi bộ nhớ như hiện nay, `TeamSelectPopup` gọi `CombatService.Persist()` khi đóng. Trận, đội hình tự xếp, AUTO, tốc độ: không lưu.

### 4.7 `CombatService` và nối dây

`CombatService` (class thuần) gom `Config`, `Stamina`, `Progress`, `Roster` và `Func<bool> persist`, đăng ký vào `ServiceLocator`:

```csharp
public EnterStageResult EnterStage(string stageId);     // ĐƯỜNG VÀO TRẬN THẬT, có trả thể lực (bên dưới)
public StartBattleResult StartBattle(string stageId);   // dựng BattleSession từ Roster.GetBattleTeam() KHÔNG kiểm màn, KHÔNG trừ thể lực, KHÔNG lưu: chỉ dải DEV gọi. Status: Success, UnknownStage, NotBattle, EmptyTeam, InvalidCharacter, InvalidConfig
public BattleSession Current { get; }
public void EndBattle();
public bool Persist();
public void ResetRuntime();
public bool AutoEnabled { get; set; }   // nhớ giữa các trận trong phiên chơi, không lưu
public bool FastEnabled { get; set; }   // x2, như trên
```

**`EnterStage(stageId)`** — mọi màn hình thật (nút Thách đấu, Vào trận, Thử lại) vào trận bằng hàm này; `StartBattle` chỉ còn cho dải DEV (review grep: ngoài `DevOverlay` không ai gọi `StartBattle`). `EnterStageResult { Status, Session, StaminaSpent, Saved }`; `EnterStageStatus { Entered, InvalidConfig, BattleInProgress, Unknown, Invalid, WrongKind, Locked, EmptyTeam, NotEnoughStamina, InvalidCharacter, SaveFailed }`. Thứ tự bước (hợp đồng như `UpgradeService.TryBuy`: **mọi bước có thể từ chối đứng trước bước trừ**; test bắt từng bước):

1. `!IsRunning` → `InvalidConfig`.
2. `Current` đang đánh dở (`State` là `PlayerTurn`/`EnemyTurn`) → `BattleInProgress`, để bấm Thách đấu hai lần liền không trừ hai lần. Trận đã `Won`/`Lost` thì không chặn (Thử lại).
3. `Progress.CanEnter(stageId, đội.Count)`: `Unknown`, `Invalid`, `WrongKind`, `Locked`, `EmptyTeam`, `NotEnoughStamina` (ánh xạ 1:1 sang `EnterStageStatus`).
4. Nhân vật nào trong đội không qua `CharacterDefinition.Validate()` hoặc `CombatSkillRules` → `InvalidCharacter`.
5. Dựng `BattleSession` (chưa gán `Current`; dùng chung hàm dựng với `StartBattle`).
6. `Stamina.TrySpend(Stage.StaminaCost, persist)` (mục 4.5): trừ, lưu đúng một lần. `SaveFailed` thì thể lực đã được hoàn đúng như cũ, `Current` giữ nguyên, không có trận, trả `SaveFailed`; view báo "chưa lưu được dữ liệu, chưa vào trận". (`Rejected` không thể xảy ra sau bước 3, nhưng nếu xảy ra thì trả `NotEnoughStamina`.)
7. `session.EntryPaid = true`, `Current = session`, trả `Entered` kèm `StaminaSpent` và `Saved = true`.

Kết quả: không bao giờ có trận chạy mà thể lực chưa trừ và chưa lưu, và không bao giờ trừ thể lực mà không có trận. Lần lưu ở bước 6 cũng ghi luôn đội hiện tại. Thua rồi bấm Thử lại là gọi `EnterStage` lần nữa: kiểm lại từ đầu, trừ lại.

`StartBattle` **không** kiểm mở màn, không kiểm hay trừ thể lực, không lưu (để dải DEV có thể vào thẳng); nó từ chối khi có nhân vật không qua `CharacterDefinition.Validate()` hoặc `CombatSkillRules`. Trận của nó có `EntryPaid == false` nên `CompleteBattle` trả `NotPaid`: thắng cũng không có thưởng. `ResetRuntime()` huỷ trận đang chạy (`Current = null`), nối vào `GameSession.ResetCompleted` như `RestaurantService` ([D-005](../../decisions/decisions.md)); `ClearedStages` và `Stamina` nằm trong `PlayerSave` nên Reset ghi đè sẵn, test kiểm cả hai.

**Cấu hình lỗi:** `CombatConfig.Validate()` còn lỗi thì `IsRunning = false`, `Debug.LogError` một lần liệt kê lỗi, `StartBattle` trả `InvalidConfig`; Gacha và quán vẫn chạy (cùng khuôn `RestaurantService`).

`GameBootstrap`: thêm `[SerializeField] CombatConfig _combatConfig` (kiểm null như các field khác), tạo `StaminaService` (truyền `() => DateTime.UtcNow`), `StageProgressService`, `CombatService`, đăng ký vào `ServiceLocator`, nối `ResetCompleted`. Quán **vẫn chạy khi đang đánh trận**: `RestaurantRunner` nằm trên GameObject `DontDestroyOnLoad` và tick bằng `unscaledDeltaTime`; **tốc độ x2 của trận không dùng `Time.timeScale`** (chỉ nhân tốc độ hoạt hình trong view) để không đụng tới quán. Ở màn đánh `RestaurantView` đã tắt nên `SetPresent(false)`: không sinh sự cố, không tính thời gian có mặt.

## 5. UI

Màn ngang, `CanvasScaler` tham chiếu 1920×1080 khớp theo chiều cao. **Mọi hình trong Canvas là component `Image`** (hình vuông đặc không gán sprite, hoặc `UISprite`/`Knob` có sẵn), không `Raw Image`, không thêm file ảnh; chữ dùng `Text` uGUI font dựng sẵn; EventSystem dùng `InputSystemUIInputModule`; nạp skill `unity:ui-ugui` khi làm UI ([unity-workflow.md](../../guides/unity-workflow.md#ui-và-hình-tạm)). Ảnh chỉ để tham khảo bố cục và luồng, không lấy luật: không lấy nguyên tố, trang bị, EXP người chơi, thống kê sát thương, 3 sao, nút SET, "auto tiến màn".

### 5.1 Hai scene mới

`StageSelect.unity` (bản đồ, popup màn, popup chọn đội, màn cốt truyện giữ chỗ) và `Battle.unity` (trận, tạm dừng, kết quả). Cả hai do builder sinh một lần, có rồi thì giữ nguyên, đọc lại bằng test (L-002); đổi bố cục sau playtest thì dựng lại bằng hai mục menu `Rebuild StageSelect Scene`, `Rebuild Battle Scene` (cùng khuôn `RebuildRestaurantScene`), không sửa tay `.unity`. Build Settings thứ tự `Boot`, `Gacha`, `Restaurant`, `StageSelect`, `Battle`. `_startScene` của Boot không đổi (vẫn Quán); dải DEV thêm nút để tới `StageSelect` (hub thật là việc sau).

### 5.2 Bản đồ màn (`StageMapView`, theo `quest-stage-map.jpg`, `quest-stage-map-small-en.jpg`)

Nền phẳng một màu. Trái trên: nút Quay lại (về Quán) và "Chương 1". Phải trên: thẻ **Thể lực x/100 · +1 sau m:ss**, Material, Gem. Bảy nút nối nhau bằng đoạn thẳng (hình chữ nhật xoay) rải theo đường gấp khúc; nút tròn (`Knob`) ghi "1-1"… hoặc "Truyện"; trạng thái bằng màu và hình: **khoá** (xám, có hình ổ khoá dựng từ `Image`), **mở** (sáng, tam giác "NEXT" nhấp nháy trên màn mở đầu tiên chưa clear), **đã clear** (xanh, dấu tích). Bấm nút khoá thì không làm gì (hiện dòng "Clear màn trước để mở"). Bấm nút Battle đã mở thì mở `StageInfoPopup`; bấm nút Story đã mở thì mở `StoryPlaceholderPopup`.

- **`StageInfoPopup`** (theo `quest-stage-info-popup.jpg`): tên màn, các đợt (mỗi đợt một hàng ô vuông quái, quái boss lớn hơn), thưởng (đang ở lần đầu thì hiện thưởng lần đầu kèm nhãn "Lần đầu", đã clear thì hiện thưởng đánh lại), thể lực "42 → 32" kèm dòng "Trừ ngay khi vào trận; thua hoặc bỏ cuộc không hoàn", nút Hủy và Thách đấu. Thiếu thể lực thì nút mờ kèm dòng báo thời gian tới khi đủ. Thách đấu gọi `EnterStage` (mục 4.7) và chỉ nạp scene `Battle` khi trả `Entered`; ngược lại hiện lý do (gồm "chưa lưu được dữ liệu, chưa vào trận" khi `SaveFailed`).
- **`StoryPlaceholderPopup`**: tên màn, dòng "Chưa có nội dung cốt truyện", nút Tiếp tục gọi `CompleteStory`. Khi có spec cốt truyện chỉ thay popup này bằng hội thoại ([story-dialogue-en.webp](../../references/ui-screens.md)); không đổi catalog hay save.

### 5.3 Chọn đội (`TeamSelectPopup`, theo `quest-team-select-popup.jpg`)

Lưới thẻ nhân vật đã có (tên, class bằng nhãn màu, sao, độ hiếm), bấm để vào/ra đội. Dòng thẻ phía dưới "Thành viên hiện tại" hiện **đội đã tự xếp** thành Front / Mid / Back theo `BattleFormation.Assign`, để người chơi thấy mỗi người đứng đâu. Nhân vật đang làm quán ghi "Quán: <vai>"; bấm vào thì hỏi xác nhận trong popup ("X đang làm <vai>, chuyển vào đội chiến đấu?"), giống `StaffPanel`. Bố cục lưới, chip, nút: spec [layout Mota](2026-11-06-layout-mota-design.md) mục 4.3 T1 (16 thẻ mỗi trang). Nút Hủy và **Vào trận**: đội trống thì nút mờ; dưới 5 người thì hiện **hộp xác nhận** "Đội mới có N người, vào trận?" (Có / Không); thể lực chỉ bị trừ khi bấm Có (hoặc Vào trận khi đủ 5 người), trong `EnterStage`, không phải lúc mở hộp. Không có bộ lọc nguyên tố, EX trang bị, "tổng chiến lực".

### 5.4 Màn trận (`BattleView`, `BattleLayout`, `BattleActionPanel`; theo `battle-wave.jpg`, `battle-wave-small.jpg`, `battle-skill-select-en.png`, `battle-boss.png`)

Bố cục chốt lại với user ngày 2026-11-06 (spec [layout Mota](2026-11-06-layout-mota-design.md) mục 4.3 B1, B2; toạ độ ở đó thắng bản phác này):

```
 [Đợt 1/3]                                                           [x1] [AUTO] [Menu]
 [Lượt: ● ● ● ○ ○ ○]
 Lượt của <tên>                                                     (quái ô 2)
            Back          Mid            Front       Front   (quái ô 0)  (quái ô 4)
                       ally   ally       ally           (hàng trên, giữa, dưới: lưới 3×3 mỗi phe)
              ally                                                            ( Đánh ) ( Skill ③ ) ( Ultimate ◔ )
                       ally   ally
```

- **Phe ta bên trái, không chồng nhau** (yêu cầu user): `BattleLayout` là hàm tĩnh thuần cho toạ độ **tâm hộp** khởi điểm, trong một vùng 1920×1080 gốc trái trên **neo giữa Canvas**, để máy màn dài hơn 16:9 không dồn cả trận sang trái. Mỗi phe là **lưới 3 cột × 3 hàng** (user chốt 2026-11-06, trừ boss); hàng ở y = 280, 520, 760 (cách 240 để dưới mỗi người còn chỗ cho tên, số HP và một hàng buff/debuff sau này). Cột phe ta: Front x = 760 (hàng trên và dưới), Mid x = 540 (hàng trên và dưới), Back x = 320 (hàng giữa). Quái đối xứng: ô 0 và 1 x = 1180, ô 2 và 3 x = 1400, ô 4 x = 1620. Người rộng 130, cao 130; boss rộng 280, cao 320, đứng ô 0 tại (1180, 400). Hộp của hai ô bất kỳ không giao nhau, kể cả boss đứng cùng quái thường; **test kiểm không đè nhau** ở đủ mọi tổ hợp đội và đợt.
- **Hình tạm:** phe ta là `Knob` tô màu theo class (Đỡ đòn xám xanh, Đấu sĩ đỏ cam, Hỗ trợ lục, Xạ thủ vàng, Pháp sư tím) kèm tên; quái là hình vuông đỏ sẫm kèm tên, boss lớn hơn. Người chết mờ dần rồi ẩn. Chỉ số HP trên đầu mỗi người là một thanh `Image` filled; cả hai phe có thêm số HP "hiện/tối đa" dưới tên.
- **Không còn hàng thẻ dưới màn** (user bỏ 2026-11-06): HP của cả hai phe là số "hiện/tối đa" dưới tên mỗi người; chiêu hồi máu chọn đồng minh bằng cách chạm người trên sân. Năm thẻ (`BattleCardView`) chỉ còn trong popup tạm dừng. Thanh năng lượng ultimate chỉ hiện ở vòng quanh nút Ultimate của người đang tới lượt.
- **Bảng chiêu kiểu FGO**: góc phải dưới, ba nút tròn của **người đang tới lượt**: **Đánh**, **Skill** (hiện số lượt hồi chiêu còn lại, mờ khi chưa dùng được), **Ultimate** (vòng năng lượng đầy dần, mờ khi chưa đủ). Chạm một quái để đặt mục tiêu (mũi tên tam giác trên đầu); mặc định quái sống đầu tiên. Skill hồi máu chọn đồng minh bằng cách chạm thẻ hoặc người; mặc định người thấp máu nhất. Tới lượt quái thì bảng khoá, chữ "Lượt của <tên>".
- **Thanh lượt** góc trên trái, dưới chữ "Đợt n/m": các hình nhỏ theo `RemainingOrder`, người hiện tại sáng; dòng "Lượt của <tên>" ngay dưới thanh.
- **Hoạt hình** (chỉ trong view, phát từ `BattleActionResult`): người ra đòn nhích về phía mục tiêu rồi về, mục tiêu nháy, số sát thương/hồi máu bay lên (bể đối tượng tái dùng), thanh HP chạy; mỗi đòn khoảng 0,6 giây, **x2 chia đôi thời gian**, không dùng `Time.timeScale`. Đợt mới: banner "Đợt 2/3" 1 giây.
- **AUTO**: nút bật/tắt; khi bật, tới lượt phe ta thì gọi `AutoPolicy` rồi phát hoạt hình như người chơi bấm. **Tốc độ**: nút xoay vòng x1 ↔ x2. **Menu** (góc trên phải, cạnh AUTO và x1) mở `BattlePausePopup` (theo `battle-pause-popup.jpg`): tên màn, năm thẻ nhân vật (HP, năng lượng; chỉ xem), Tiếp tục, **Cài đặt** (mở panel DEV, chỉ Editor và Development Build; ở màn trận nút Cài đặt góc dưới phải được ẩn), **Bỏ cuộc** (hỏi lại một lần với dòng "Trận này tính thua, thể lực không được hoàn", rồi `Surrender`, tính thua, không thưởng, không hoàn thể lực). Mở popup thì trận dừng; AUTO và tốc độ nằm ở `CombatService` (mục 4.7).
- Vào scene mà `CombatService.Current` trống (mở Battle.unity trực tiếp) thì hiện "Chưa có trận" và nút về, **không** tự dựng trận.

### 5.5 Kết quả (`BattleResultView`, theo `battle-result-clear-en.webp`, `battle-result-rewards-mvp-en.webp`, `battle-result-failed-en.webp`)

Bố cục hộp kết quả: spec [layout Mota](2026-11-06-layout-mota-design.md) mục 4.2 K1, K2.

- **Thắng:** banner "CLEAR", danh sách thưởng (ô vuông + số, nhãn "Lần đầu" nếu là lần đầu), thể lực đã dùng lúc vào; nếu `Saved = false` thì dòng "chưa lưu được dữ liệu" (cùng câu với AFK). Nút **Xác nhận** về bản đồ, màn kế tiếp vừa mở được đánh dấu "NEXT". `CompleteBattle` gọi **một lần** khi hiện màn này. `Status` khác `Success` (chỉ gặp ở trận vào thẳng từ dải DEV, `NotPaid`) thì vẫn hiện "CLEAR" kèm dòng "Trận thử, không có thưởng". Không có MVP, thống kê, 3 sao.
- **Thua:** banner "Thất bại", gợi ý một dòng ("Thử vào đủ 5 người, hoặc đánh lại màn trước để lấy Gem") và dòng "Đã trừ N thể lực khi vào, không hoàn" (trận DEV không có dòng này), nút **Thử lại** và **Về bản đồ**. Thử lại gọi `EnterStage` cùng màn (kiểm lại từ đầu, **trừ thể lực lần nữa**); nút mờ kèm lý do khi `CanEnter` không `Ready` (thiếu thể lực thì kèm "Đủ thể lực sau m:ss"). Trận DEV thua thì Thử lại gọi `StartBattle` như cũ. Bỏ cuộc cũng ra màn này.

### 5.6 Dải DEV

`DevOverlay` (chỉ Editor và Development Build) thêm: **Màn chơi** (sang `StageSelect`), **Chiến đấu thử** (đội trống thì thêm tối đa 5 nhân vật đã sở hữu vào đội; `StartBattle` màn đang chọn, **bỏ qua** kiểm mở màn và **không trừ thể lực**, không thưởng; có từ Task 6, trước khi có bản đồ), **Màn thử** (xoay vòng qua các màn Battle, mặc định màn đầu tiên, để thử 1-2 đến 1-5 ở playtest 2) và **Hồi đầy thể lực**. Hub sau này xoá dải này.

## 6. Test

EditMode, cùng assembly `BunDauPeo.Tests`; số cụ thể ở kế hoạch. Mọi test logic tiền, thể lực, lượt có đầu vào mà cài đặt sai cho kết quả khác (L-006), có ca "handler ném lỗi" cho event mới (L-003), dùng lặp trong một method thay `[TestCase]` (L-012), thời gian ở biên nửa bước (L-008).

- **Save và tương thích:** JSON cũ không có `ClearedStages`, `Stamina` đọc được và về giá trị khởi tạo, field cũ còn nguyên; round-trip qua `SaveService`; `ResetToDefaults` ghi đè cả hai; `ResetCompleted` huỷ `CombatService.Current`.
- **`CharacterClass`, `CharacterDefinition`:** `Unset` bị báo; asset mẫu đủ 5 class và hợp lệ.
- **`CombatConfig`, SO:** từng lỗi `Validate()` mục 3.3 và 3.4 có một ca (kể cả `_staminaRegenSeconds = 0`, `_maxStamina` nhỏ hơn thể lực một màn); mặc định `_maxStamina` 100; cấu hình và 7 màn mẫu hợp lệ, mọi màn Battle tốn 10; thưởng có `Coin` bị báo; 4 skill mẫu qua `CombatSkillRules`.
- **`BattleFormation`:** từng class vào đúng hàng; tổng ba hằng số ô bằng `Roster.MaxBattleTeamSize`; `SlotIndex` đúng Front 0, 1, Mid 2, 3, Back 4; hàng đầy thì tràn đúng thứ tự gần nhất; 5 Đấu sĩ ra 2-2-1; đội 1 đến 4 người; đội đúng thứ tự `GetBattleTeam`; `GetEntryStatus` ở 0, 1, 4, 5, 6.
- **`DamageCalculator`:** công thức ở nhiều (Attack, Power, Defense) với số tay tính được; làm tròn nửa lên; tối thiểu 1; hồi máu không vượt máu tối đa và ghi số thực hồi.
- **`BattleSession`:** thứ tự theo Speed, hoà phe ta trước rồi ô thấp; người chết bỏ qua; thắng và thua một đợt; hồi chiêu đúng mốc (dùng ở lượt 1, chưa dùng lại ở lượt 3, được ở lượt 4 với N = 3) và `Cooldown = 1`; năng lượng cộng đúng từng nguồn, kẹp ở tối đa, ultimate trừ đúng và không cộng; ultimate bị từ chối khi thiếu một điểm; mục tiêu sai phe, đã chết, null; AoE; hồi máu không hồi sinh; quái dùng skill khi sẵn sàng và đánh thường khi chưa; số lần gọi RNG đúng hợp đồng (một lần mỗi hành động nhắm một người, kể cả còn một người, không lần nào cho AoE); trọng số 3:2:1 qua `ScriptedRandom` ở ba biên; qua đợt giữ HP, năng lượng, hồi chiêu, `Round` tăng 1 và người chưa đánh không được đánh bù; chiêu không cần mục tiêu bỏ qua `target` truyền vào; `_maxRounds` thì thua; `Surrender`; hành động khi đã kết thúc hoặc sai lượt trả đúng trạng thái và **không đổi gì**.
- **`AutoPolicy`:** ultimate trước, skill khi sẵn sàng, đánh thường khi không; skill và ultimate hồi máu chỉ khi có người dưới ngưỡng (ngay tại ngưỡng thì không); mục tiêu mặc định.
- **`StaminaService` (đồng hồ giả):** đầy khi `Anchor = 0`; hồi đúng theo giờ (chưa đủ một khoảng thì chưa tăng, ở nửa khoảng liền kề); kẹp ở `Max`; vắng dài thì đầy; `TrySpend` từ đầy đặt `Anchor`, khi chưa đầy giữ `Anchor`; `persist` gọi đúng một lần và **sau** khi đã trừ; thiếu thì `Rejected`, không đổi gì và không gọi `persist`; `persist` thất bại thì `SaveFailed` và `Current`, `Anchor` về đúng như trước (cả từ đầy lẫn khi chưa đầy); đồng hồ lùi không hồi và `Current` không giảm; `Max` giảm thì kẹp; `Refill`.
- **`StageProgressService`:** mở màn theo thứ tự và theo quy tắc "đã clear thì luôn mở"; `CanEnter` từng trạng thái theo đúng thứ tự kiểm; `CompleteBattle`: thưởng lần đầu rồi đánh lại, **không đụng thể lực**, trận chưa trả thể lực (`StartBattle`) thì `NotPaid` và không đổi gì (ví, màn, số lần `persist`, `Changed`), `persist` đúng một lần, `Saved = false` khi `persist` thất bại mà bộ nhớ đúng, gọi hai lần cùng trận trả `AlreadyClaimed`, trận chưa thắng, màn chưa mở, handler `Changed` ném lỗi không chặn lưu; `Status` khác `Success` thì không đổi gì; `CompleteStory` không thưởng, không tốn thể lực, lần hai không đổi gì.
- **`CombatService`:** từng `Status` của `StartBattle`; nhân vật lỗi chặn trận; `ResetRuntime`; cấu hình lỗi thì `IsRunning = false`. **`EnterStage`:** vào thành công trừ đúng `StaminaCost` và `persist` đúng một lần, lúc `persist` chạy thể lực đã trừ trong save; **từng lý do từ chối** (màn lạ, màn lỗi, không phải Battle, màn khoá, đội trống, thiếu thể lực) không trừ thể lực, không lưu, giữ nguyên `Current`; **nhân vật lỗi trong đội, đủ thể lực** thì `InvalidCharacter` và thể lực nguyên (không mất oan); cấu hình lỗi; lưu thất bại thì thể lực về đúng như cũ và không có trận; trận đang đánh dở thì `BattleInProgress`, không trừ hai lần; thua rồi vào lại thì trừ lại; trừ đúng `StaminaCost` của màn; trận từ `StartBattle` không trừ, không lưu, `EntryPaid` false.
- **`BattleLayout`, `CombatViewText`:** không đè nhau ở mọi tổ hợp đội và đợt; chữ thời gian hồi thể lực, nhãn hồi chiêu, dòng "trừ ngay, không hoàn", dòng lý do `EnterStage` thất bại; hàm tĩnh thuần có test.
- **`ProjectSetup`:** dữ liệu mẫu hợp lệ; chạy lại không ghi đè số đã chỉnh; asset đã sinh theo luật thể lực cũ (`_maxStamina` 60, thể lực boss 15) được chuyển một lần sang 100 và 10, còn số đã sửa tay thì giữ; `Boot.unity` có `_combatConfig`; hai scene mới có `Camera`, `EventSystem` + `InputSystemUIInputModule`, không `RawImage`, đúng thứ tự Build Settings.

## 7. Gói triển khai

Chi tiết từng task, model/effort, lệnh, số test cộng dồn ở [kế hoạch](../plans/2026-11-02-combat.md). Dự kiến **15 task, chia 4 session, 2 mốc playtest sớm**. Làm trận chơi thử được **trước** thể lực, màn chơi và thưởng, vì cảm giác đánh chỉ chỉnh được khi bấm thử, và phần sau không chạm vào lõi trận.

**Giai đoạn A: nền và trận chơi thử được (một màn, một đợt, chơi tay).** Session 1 (logic thuần): 1. `CharacterClass`, asmdef, `CombatConfig`. 2. SO quái, đợt, màn, catalog và dữ liệu mẫu. 3. `DamageCalculator`, `BattleUnit`, `BattleFormation`. 4. `BattleSession`: vòng lượt, đánh thường, quái, nhiều đợt, thắng/thua. Session 2: 5. `BattleSession`: skill, ultimate, hồi chiêu, năng lượng, mục tiêu, hồi máu, AoE, quái dùng skill. 6. `CombatService`, nối `Boot`, dải DEV (Chiến đấu thử). 7. UI: `BattleLayout`, `BattleView`, scene `Battle`. 8. UI: bảng chiêu kiểu FGO, thẻ 5 người, chọn mục tiêu, banner thắng/thua. **Dừng: playtest 1** (đánh màn 1-1 từ dải DEV, mọi hành động, chỉ chơi tay).

**Giai đoạn B: trận đầy đủ.** Session 3: 9. `AutoPolicy`, giới hạn vòng, kiểm lại nhiều đợt. 10. UI: thanh lượt, đợt, AUTO, tốc độ, tạm dừng, bỏ cuộc. 11. Core: `StaminaState`, `ClearedStages`, `StaminaService` (đổi mặc định sang 100 và 10 mỗi màn). **Dừng: playtest 2** (màn 1-2 đến 1-5 từ dải DEV, AUTO, x2, tạm dừng/bỏ cuộc; thể lực chưa có giao diện).

**Giai đoạn C: màn chơi, thể lực, thưởng, lưu.** Session 4: 12. `StageProgressService`, đường vào trận `CombatService.EnterStage`, nối vào Reset. 13. UI: bản đồ, popup màn, màn cốt truyện giữ chỗ. 14. UI: chọn đội, hộp xác nhận, màn kết quả, nối trọn vòng. 15. Cập nhật docs. Rồi review toàn nhánh. **Dừng: playtest 3** (trọn vòng: bản đồ → đội → trận → thưởng → màn mở → thoát và mở lại vẫn giữ tiến độ và thể lực).

Mỗi ranh giới session là chỗ test xanh đã push; không chia giữa một task.

## 8. Ngoài phạm vi

- **Hội thoại và cốt truyện thật** (chỉ có nút giữ chỗ), nhiều chương, độ khó Hard, các chế độ phụ trên [quest-mode-select.jpg](../../references/ui-screens.md).
- **Tự xếp ô thủ công** (để dành, đổi save khi làm), kéo thả đội hình.
- Buff, debuff, nguyên tố, trang bị, chí mạng, né, khiêu khích (đã chốt trong [vision](../../vision/vision.md) hoặc [spec nền móng](2026-10-30-foundation-architecture-design.md): `SkillDefinition` chỉ một hiệu ứng).
- Mua hoặc hồi thể lực bằng Gem, vật phẩm thể lực, "quét nhanh/skip", tự tiến màn tiếp theo.
- Thống kê sát thương, MVP, 3 sao, nút SET, khôi phục trận giữa chừng.
- Cấp và nâng sao ảnh hưởng chỉ số (spec Nâng sao), chỉ số theo sao.
- Hình, hoạt hình, âm thanh thật; cân bằng thật ([mục 9](#9-câu-hỏi-còn-mở)).
- Kiểm trên Android thật (đã hoãn ở [roadmap](../../roadmap.md)).

## 9. Câu hỏi còn mở

- Đã duyệt 2026-11-02: nút cốt truyện giữ chỗ. Thể lực duyệt 2026-11-02 rồi **sửa 2026-11-03 theo user chốt** (số ở mục 4.5: tối đa 100, 1 mỗi 5 phút, 10 mỗi màn kể cả boss, trừ một lần khi vào trận và lưu ngay, không hoàn, không mua), ghi ở [D-008](../../decisions/decisions.md). `TODO(user)`: tự cập nhật [vision.md](../../vision/vision.md) cho khớp (agent không sửa file này, xem mục 11).
- `TODO(user)`: tên thật của hai màn cốt truyện, tên quái, tên màn.
- `TODO(user)`: art cuối (nhân vật, quái, nền) khi có; bản này dùng hình tạm.
- Số cân bằng (quái, thưởng, chỉ số nhân vật theo class, 1/5 Gem đánh lại) chỉ là số khởi điểm. **Đã thấy trước:** ba nhân vật khởi đầu (đều R) thắng 1-1 đến 1-3 nhưng thua 1-4 và boss; cần gacha hoặc đủ 5 người, đúng ý "đội mạnh hay quán đông". Chỉ số mẫu của 6 nhân vật chưa phản ánh class (Đỡ đòn mẫu không có HP nổi bật); chỉnh khi cân bằng.
- **Kinh tế cần xem ở playtest 3 (tính lại 2026-11-03 theo luật mới):** thể lực hồi 288 điểm/ngày, tức 28,8 lượt đánh mỗi ngày, với điều kiện vào ít nhất một lần mỗi 8 giờ 20 để thanh 100 không bị đầy mà phí phần hồi. Mọi màn cùng giá 10, nên cày đúng màn 1-5 (đánh lại cho 100 Material + 100 Gem) cho tối đa khoảng **2.880 Gem và 2.880 Material mỗi ngày**: hơn 3 lần quay 10 mỗi ngày (so với hơn 2 ở luật cũ, vì boss rẻ đi từ 15 xuống 10) và gấp 8 lần tổng 345 Material của mọi nâng cấp quán. Cày 1-1 chỉ được 30 + 30 cho cùng 10 thể lực (864 mỗi ngày), nên **không còn lý do chơi màn dễ ngoài lần đầu**, vì giá thể lực không khác nhau giữa các màn. Ngày đầu có thêm 100 thể lực khởi đầu (10 lượt, khoảng 1.000 Gem nếu toàn boss). Thua vẫn mất 10, nên đội yếu cày boss không thắng được sẽ tốn thể lực mà không có thưởng; số thưởng đánh lại theo màn là chỗ chỉnh nếu muốn khuyến khích cày màn vừa sức. Toàn là số trong SO, chỉnh sau không đổi code.
- Chạy thử mô phỏng ở đây là ước lượng bằng một script ngoài repo với cùng công thức, không thay cho test và playtest.

## 10. Rủi ro

- **Thưởng nhận hai lần:** `CompleteBattle` đổi ví và tiến độ trong một bước rồi lưu một lần; cờ `RewardsClaimed` chặn gọi hai lần; test bắt từng bước hỏng và `persist` thất bại.
- **Mất thể lực oan (trừ rồi mới thấy từ chối):** `EnterStage` đặt mọi kiểm có thể từ chối trước `TrySpend`; test riêng cho từng lý do, nhất là nhân vật lỗi trong đội và cấu hình lỗi (hai lỗi nằm ngoài `CanEnter`, dễ bị quên).
- **Trừ thể lực mà không có trận, hoặc có trận mà thể lực chưa lưu:** `TrySpend` trừ, lưu một lần, `SaveFailed` thì hoàn đúng `Current` và `Anchor` đã chụp; `Current` chỉ gán sau khi trừ và lưu xong. Test bắt cả hai hướng.
- **Trừ hai lần vì bấm đôi:** `BattleInProgress` chặn khi trận trước còn dở; view còn tắt nút Thách đấu ngay khi bấm.
- **Đường vào trận miễn phí lọt ra màn thật:** `StartBattle` không trừ thể lực; review grep chỉ `DevOverlay` được gọi nó; `CompleteBattle` từ chối trận chưa trả (`NotPaid`) nên có lọt cũng không có thưởng.
- **Save thể lực lệch khi đồng hồ chạy lùi hoặc `Max` đổi:** quy ước `Anchor = 0` là đầy, kẹp theo `Max` khi đọc, đồng hồ lùi không hồi và không trừ.
- **Asset đã sinh theo luật cũ (60, boss 15):** Setup không ghi đè số đã sinh, nên Task 11 có bước chuyển một lần giá trị cũ nguyên văn sang 100 và 10, có test.
- **Thêm `_class` làm đỏ test hay asset hiện có:** `Validate()` mới báo `Unset`; test dựng `CharacterDefinition` bằng code (như `RestaurantTestData`, `CharacterDefinitionTests`) phải đặt class. `grep` mọi chỗ dựng nhân vật trước khi sửa; `ProjectSetup` điền class cho 6 asset mẫu.
- **Trận không kết thúc:** hồi máu liên tục chặn sát thương; `_maxRounds` chặn và có test.
- **Không chồng nhau trên màn hình:** `BattleLayout` có test hình học; thử bằng ảnh chụp thật theo L-010 và L-011 (đọc `Screen.width/height`, chụp qua camera `targetTexture`, không tin ảnh `capture game_view`).
- **Tốc độ x2 làm đứng quán:** không dùng `Time.timeScale`; test không có chỗ nào đặt nó (grep trong review).
- **Reset bỏ sót trạng thái:** hai field mới nằm trong `PlayerSave`; `Current` của `CombatService` xoá ở `ResetRuntime`; test kiểm từng thứ (D-005).
- **Lượt thử Play ghi vào save thật:** sao lưu `save.json*` vào thư mục mới ngoài repo trước khi Play, chép lại sau khi đóng Editor, không dùng Reset Save Data để dọn (nó xoá cả `.bak`); trả `ProjectSettings.asset` nếu đổi (L-004, L-007, L-014). Cách làm cụ thể ở mục "Thử Play an toàn" của kế hoạch.
- **Cân bằng sai làm boss không thắng nổi:** số mẫu chọn bằng mô phỏng; sai thì chỉnh SO, không sửa code.

## 11. Việc cần người khác làm (agent không đụng)

- **User:** cập nhật [vision.md](../../vision/vision.md) (thể lực vào phạm vi; cốt truyện vẫn sau MVP, chỉ giữ chỗ).
- **Quyết định thể lực:** đã ghi **D-008** ([decisions.md](../../decisions/decisions.md), 2026-11-03) cùng lần sửa spec này. (D-007 là hướng kiếm tiền.)
- **Agent, sau khi user duyệt spec (đã làm 2026-11-02, cùng kế hoạch):** sửa dòng stamina ở [ui-screens.md](../../references/ui-screens.md), cập nhật [roadmap.md](../../roadmap.md), [game-design.md](../../design/game-design.md) (mục Nhân vật, Chiến đấu, thêm Thể lực và Màn chơi) và [structure.md](../../project/structure.md) (asmdef `Combat`, scene mới).
