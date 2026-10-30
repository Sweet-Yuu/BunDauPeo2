# Spec: Nền móng kiến trúc + ScriptableObject

> Ngày: 2026-10-30 · Trạng thái: **đã duyệt** · Phạm vi: **nền móng**, chưa có gameplay.
> Luật chơi nằm ở [game-design.md](../../design/game-design.md), kế hoạch ở [plans/2026-10-30-foundation.md](../plans/2026-10-30-foundation.md).

## 1. Mục tiêu và bối cảnh

BunDauPeo là game RPG gacha 2D, turn-based, kết hợp quản lý quán bún đậu mắm tôm. Mobile, màn ngang, UI hub kiểu Princess Connect Re:Dive (ảnh trong `Mota/`).

Vòng lặp chính:
1. Khách đến quán ngẫu nhiên, ăn và trả tiền (Coin).
2. Nhân vật gacha làm nhân viên quán (nấu, phục vụ, thu ngân) hoặc đi chiến đấu.
3. Đánh màn chơi cho Material và Gem.
4. Coin và Material dùng mua nâng cấp tăng thu nhập quán. Gem dùng quay gacha để có nhân vật mới.

Spec này chốt **kiến trúc tổng và cấu trúc thư mục**, và triển khai **nền móng cùng SO nhân vật**. Logic chạy của gacha, quán, combat, UI hub thuộc các spec riêng sau.

### Quyết định đã chốt với user

| Chủ đề | Quyết định |
|---|---|
| Quản lý quán | Tự chạy + có hình: khách tự đến, nhân vật xếp vào vị trí và làm theo chỉ số. Logic tách khỏi view |
| Liên kết quán và chiến đấu | Nhân vật nằm trong đội chiến đấu thì **không** làm quán, dù đang đánh hay không |
| Phạm vi lần này | Nền móng + SO của `Characters`, chưa có gameplay |
| Kiến trúc | Folder theo tính năng, mỗi tính năng một asmdef, chung lõi `Core`. Tính năng không gọi nhau; dùng chung qua `Core` và `Characters`. UI nghe `event` C# của service |
| Combat | Turn-based, mỗi nhân vật 1 đánh thường, 1 skill, 1 ultimate |
| Placeholder | Sprite có sẵn của Unity (`UI/Skin/UISprite.psd`, `UI/Skin/Knob.psd`), không thêm file ảnh |
| Đội và kinh tế | Đội chiến đấu tối đa 5 người, đối thủ là nhiều quái theo wave. Ba loại tiền: Coin (quán), Gem (gacha), Material (chiến đấu) |
| Nền tảng | Mobile màn ngang, offline hoàn toàn, không nạp tiền thật. Nếu sau này có IAP thì save cục bộ và roll gacha trên máy không còn phù hợp, cần server |
| Thời gian quán chạy | Quán vẫn kiếm tiền trong lúc đánh trận. Khi tắt game, quán chuyển sang chế độ AFK |
| AFK | Tối đa 12 giờ, thu nhập thấp hơn, phải thu thập mới tích tiếp. Luật ở [game-design.md](../../design/game-design.md#afk). **Triển khai trong spec Restaurant**, vì thu nhập AFK tính từ thu nhập quán |

## 2. Cấu trúc thư mục và assembly

```
Assets/_Project/
  Core/          BunDauPeo.Core
    Scripts/     ServiceLocator, Wallet, SaveService, PlayerSave, CurrencyType, CurrencyAmount,
                 StaffRole, GameDefinition, Catalog<T>
  Characters/    BunDauPeo.Characters
  Gacha/         (asmdef BunDauPeo.Gacha tạo cùng script đầu tiên)
  Restaurant/    (asmdef BunDauPeo.Restaurant; gồm cả nâng cấp quán)
  Combat/        (asmdef BunDauPeo.Combat; gồm cả màn chơi và quái)
  UI/            (asmdef BunDauPeo.UI; khung hub, thanh điều hướng dưới)
  Boot/          BunDauPeo.Boot      GameBootstrap, NewGameConfig
  Editor/        BunDauPeo.Editor    chỉ chạy trong Editor: menu sinh asset mẫu, scene khởi động khi Play
  Scenes/        Boot.unity
  Tests/         BunDauPeo.Tests     EditMode
```

- `Core`, `Characters`, `Gacha`, `Restaurant`, `Combat`, `UI`, `Boot` đều có các folder con `Scripts/`, `Data/` (asset SO), `Art/`, `Animations/`, `Prefabs/`. Folder con còn rỗng giữ bằng `.gitkeep`.
- Tính năng chưa có code thì chưa tạo asmdef, vì Unity cảnh báo asmdef không có script. File `.asmdef` được tạo cùng script đầu tiên của tính năng đó.

### Luật phụ thuộc (do asmdef thực thi)

```
Core  <-  Characters  <-  Gacha, Restaurant, Combat  <-  UI, Boot
```

- `Core` không phụ thuộc assembly nào của dự án.
- `Characters` chỉ phụ thuộc `Core`. Tách riêng vì Gacha, Restaurant, Combat đều cần định nghĩa nhân vật.
- `Gacha`, `Restaurant`, `Combat` chỉ phụ thuộc `Core` và `Characters`, **không phụ thuộc lẫn nhau**.
- `UI` và `Boot` là tầng trên cùng, được tham chiếu mọi assembly tính năng.
- `Editor` được tham chiếu mọi assembly và chỉ build trong Editor.
- `Tests` tham chiếu `Core`, `Characters`, `Boot`, `Editor`, chạy ở chế độ Editor.

Nguyên tắc đặt nội dung: một SO chỉ được trỏ tới SO cùng assembly hoặc ở tầng dưới. Vì vậy `StageDefinition` nằm cùng `EnemyDefinition` trong Combat. Nâng cấp đều tác động lên quán nên `UpgradeDefinition` nằm trong Restaurant.

### Khác với quy ước cũ
`docs/project/structure.md` hiện đề xuất `Assets/_Project/Scripts/<Tính năng>/`. Cấu trúc ở trên thay thế quy ước đó (mục đó chưa từng được user duyệt). Khi triển khai phải cập nhật `structure.md`.

## 3. Mô hình dữ liệu

Hai loại, không trộn:
- **Definition**: ScriptableObject, chỉnh trong Inspector, chỉ đọc lúc chạy.
- **State**: class C# thuần `[Serializable]`, ghi vào save JSON.

### 3.1 Kiểu dùng chung ở `Core`

- `GameDefinition : ScriptableObject` có `string Id`. Chỉ loại SO được save tham chiếu mới kế thừa nó: nhân vật, banner, nâng cấp, màn chơi. SO khác (skill, món, khách, quái) không cần `Id`.
- `Catalog<T> : ScriptableObject where T : GameDefinition` giữ `T[]`, có `TryGet(id, out T)`. Mỗi loại được save tham chiếu có một catalog cụ thể (`CharacterCatalog`…). Không dùng thư mục `Resources`. `OnValidate` báo Id trống hoặc trùng.
- `CurrencyType { Coin, Gem, Material }`.
- `CurrencyAmount { CurrencyType Type; long Amount; }` dùng cho chi phí, phần thưởng và số dư trong save.
- `StaffRole { Cook, Server, Cashier }`. Nằm ở `Core` vì `PlayerSave` cần.
- `Wallet(PlayerSave)`:
  - `Get(type)`, `Add(type, amount)`, `TrySpend(params CurrencyAmount[] costs)`.
  - `TrySpend` trừ tất cả hoặc không trừ gì; các dòng cùng loại tiền được cộng gộp trước khi kiểm tra.
  - Số âm bị từ chối; số dư không bao giờ âm.
  - Có `event Action Changed`.
- `ServiceLocator`: `Register<T>`, `Get<T>`. `Get` báo lỗi rõ ràng nếu service chưa đăng ký. Chỉ MonoBehaviour view được dùng nó; class logic nhận phụ thuộc qua constructor để test được.

### 3.2 SO của `Characters` (triển khai lần này)

| SO | Trường |
|---|---|
| `CharacterDefinition : GameDefinition` | DisplayName, Rarity (R/SR/SSR), Portrait, BattleSprite, `CombatStats`, `RestaurantStats`, `SkillDefinition BasicAttack`, `SkillDefinition Skill`, `SkillDefinition Ultimate` |
| `SkillDefinition` | DisplayName, Icon, Kind (Basic/Skill/Ultimate), Target (SingleEnemy/AllEnemies/SingleAlly/AllAllies/Self), Effect (Damage/Heal), Power (hệ số), Cooldown (dùng khi Kind = Skill), EnergyCost (dùng khi Kind = Ultimate), AnimationTrigger (string) |
| `CharacterCatalog : Catalog<CharacterDefinition>` | danh sách nhân vật |

- `CombatStats { HP, Attack, Defense, Speed }`. Nằm ở `Characters` để quái của Combat dùng lại.
- `RestaurantStats { Cooking, Serving, Cashier }`.
- `CharacterDefinition.Validate()` trả danh sách lỗi; `OnValidate` chỉ gọi và log nó. Lỗi gồm: ô skill trống, hoặc skill sai loại (`BasicAttack.Kind` phải là Basic, `Skill.Kind` là Skill, `Ultimate.Kind` là Ultimate).

### 3.3 SO của các tính năng sau (bản nháp, chưa triển khai)

Dùng để kiểm tra ranh giới kiến trúc. Spec của từng tính năng sẽ chốt lại.

| SO | Thuộc | Trường dự kiến |
|---|---|---|
| `GachaBannerDefinition` | Gacha | DisplayName, Art, chi phí 1 lượt và 10 lượt, tỉ lệ theo độ hiếm, danh sách nhân vật kèm trọng số, ngưỡng đảm bảo (0 = tắt) |
| `DishDefinition` | Restaurant | DisplayName, Icon, BasePrice, CookTime |
| `CustomerDefinition` | Restaurant | DisplayName, Sprite, SpawnWeight, PatienceSeconds, PayMultiplier |
| `RestaurantConfig` | Restaurant | BaseTables, khoảng thời gian khách đến, danh sách món và khách |
| `UpgradeDefinition` | Restaurant | DisplayName, Icon, loại hiệu ứng (thu nhập, tốc độ khách, số bàn, tốc độ nấu), giá trị mỗi cấp, danh sách cấp (mỗi cấp là một `CurrencyAmount[]`, số cấp tối đa = độ dài danh sách), nâng cấp cần có trước |
| `EnemyDefinition` | Combat | DisplayName, Sprite, `CombatStats`, danh sách skill |
| `StageDefinition` | Combat | DisplayName, các wave (mỗi wave một danh sách quái), `CurrencyAmount[]` phần thưởng, màn cần clear trước |

### 3.4 State (save)

`PlayerSave` nằm ở `Core`, chỉ chứa Id, số và enum của `Core`, không chứa kiểu của tính năng. Dùng `List` các kiểu `[Serializable]`, vì `JsonUtility` không hỗ trợ `Dictionary`.

Trường triển khai lần này:
- `Version` (int).
- `Balances`: danh sách `CurrencyAmount`.
- `OwnedCharacterIds`: danh sách Id.
- `BattleTeam`: tối đa 5 Id.
- `Staff`: danh sách cặp Id và `StaffRole`.

Spec sau thêm trường cấp nâng cấp và AFK (Restaurant), tiến độ màn (Combat), bộ đếm đảm bảo (Gacha). Trường mới có giá trị khởi tạo trong class nên save cũ vẫn đọc được.

### 3.5 `Roster` và luật "đi đánh thì vắng quán"

`Roster(PlayerSave, CharacterCatalog)` nằm ở `Characters`, là nơi duy nhất sửa danh sách nhân vật, đội và nhân viên. Có `event Action Changed`.

- `Add(id)`: Id đã có thì không đổi và trả `false`. Xử lý nhân vật trùng thuộc spec Gacha.
- Thêm vào đội: tự gỡ khỏi `Staff`. Trả `false` nếu đội đã đủ 5 hoặc chưa sở hữu nhân vật.
- Gán nhân viên: tự gỡ khỏi `BattleTeam`. Trả `false` nếu chưa sở hữu. Số vị trí tối đa do Restaurant quyết định.
- Id không còn trong catalog: bỏ qua khi trả về danh sách và cảnh báo, nhưng **giữ nguyên trong save** để khôi phục được khi SO quay lại.

Restaurant và Combat chỉ hỏi `Roster`, không gọi nhau.

### 3.6 Save

`SaveService(string path)` dùng `JsonUtility`. Đường dẫn truyền vào để test không đụng save thật; `GameBootstrap` truyền `Application.persistentDataPath/save.json`.

**Ghi:** ghi ra file tạm, rồi thay file chính và giữ bản trước làm `.bak`. Tắt app giữa chừng không làm hỏng file chính.

**Đọc:** thử file chính, hỏng thì thử `.bak` và cảnh báo. Không còn file đọc được thì sao chép file chính sang `.corrupt` rồi trả `null`, vì game mới sẽ ghi đè file chính.

**Thời điểm lưu:**
- Khi app vào nền và khi thoát (`GameBootstrap` qua `OnApplicationPause`, `OnApplicationQuit`).
- Ngay sau mỗi thao tác tiêu tiền lớn: quay gacha, mua nâng cấp, kết thúc trận (các spec sau phải tuân theo).
- Tiền khách trả không lưu theo từng khách.

`Version` chưa có migration; thêm khi lần đầu đổi schema theo cách không tương thích.

## 4. Luồng chạy

### 4.1 Khởi động

`GameBootstrap` (MonoBehaviour trong scene `Boot`, `DontDestroyOnLoad`, có tham chiếu `CharacterCatalog` và `NewGameConfig`) tạo `GameSession` (class thuần ở `Boot`, test được):
1. `SaveService.Load`. Nếu `null` thì tạo `PlayerSave` mới, áp `NewGameConfig` và lưu ngay, để mở lại không được cấp lại đồ ban đầu.
2. Tạo `Wallet`, `Roster`, đăng ký vào `ServiceLocator`.
3. Từ các spec sau: đăng ký service của tính năng và mở scene hub. Lần này dừng ở bước 2.

`NewGameConfig` (SO ở `Boot`): `CurrencyAmount[] StartingCurrency`, `CharacterDefinition[] StarterCharacters`.

Script Editor đặt `EditorSceneManager.playModeStartScene` thành `Boot`, nên bấm Play ở scene nào cũng khởi động đúng. Build Settings đặt `Boot` đầu danh sách.

### 4.2 Giao tiếp

Không có event bus chung. Tính năng không cần nghe nhau, vì việc dùng chung đều đi qua `Wallet`, `Roster` và `PlayerSave`. UI được tham chiếu tính năng nên nghe trực tiếp `event` C# của service.

Thêm event bus khi xuất hiện một tính năng thật sự phải nghe tính năng ngang hàng.

### 4.3 Các luồng chính (thiết kế, chưa triển khai trong spec này)

| Luồng | Đường đi |
|---|---|
| Gacha | `GachaService.Pull` → `Wallet.TrySpend` → roll theo banner → `Roster.Add` → lưu → trả kết quả cho UI |
| Quán | `RestaurantSimulation.Tick(dt)` sinh khách, lấy nhân viên từ `Roster`, khách trả tiền thì `Wallet.Add(Coin)`. View nghe event của simulation để vẽ. `Tick` chạy ở đối tượng sống xuyên scene, không gắn với view, để quán vẫn chạy khi đánh trận |
| Combat | `BattleSession` xếp lượt theo Speed, mỗi lượt chọn Basic/Skill/Ultimate. Thắng thì `Wallet.Add(phần thưởng)`, ghi tiến độ màn, lưu, báo UI |
| Nâng cấp | `UpgradeService.TryBuy` (Restaurant) kiểm điều kiện, `Wallet.TrySpend(chi phí cấp)`, tăng cấp, lưu. Simulation đọc cấp trực tiếp |

### 4.4 Logic và view

Logic là class C# thuần, không phải MonoBehaviour. MonoBehaviour chỉ làm view. Dữ liệu cấu hình luôn ở SO (khớp `docs/guides/code-style.md`).

## 5. Test

Assembly `BunDauPeo.Tests` (EditMode, Test Framework 1.6.0):
- `Wallet`: `TrySpend` nhiều loại tiền thiếu một loại thì không trừ gì; các dòng cùng loại được cộng gộp; số âm bị từ chối; `Changed` được phát.
- `Roster`: luật loại trừ đội và nhân viên; giới hạn 5; `Add` trùng trả `false`; Id lạ bị bỏ qua khi trả về nhưng vẫn còn trong save.
- `SaveService` (đường dẫn trong thư mục tạm): ghi rồi đọc lại đúng; file chính hỏng thì đọc `.bak`; cả hai hỏng thì trả `null` và còn bản `.corrupt`; file tạm sót lại không ảnh hưởng.
- `CharacterDefinition.Validate()`: skill sai loại hoặc thiếu ô bị báo.
- `Catalog`: `TryGet` đúng khi có và khi không có Id.
- `GameSession`: game mới được cấp đồ ban đầu; mở lại không cấp lại.
- `ProjectSetup`: chạy lại không ghi đè asset đã chỉnh; asset mẫu hợp lệ.

Test tỉ lệ roll gacha thuộc spec Gacha.

## 6. Gói triển khai của spec này

1. Cây thư mục theo mục 2, asmdef cho `Core`, `Characters`, `Boot`, `Editor`, `Tests`.
2. Code `Core` theo mục 3.1 và 3.4, cùng `SaveService` theo mục 3.6.
3. `Characters`: `CharacterDefinition`, `SkillDefinition`, `CharacterCatalog`, `CombatStats`, `RestaurantStats`, `Roster`.
4. Menu Editor `Tools/BunDauPeo/Setup Project` (chạy được ở batch mode) sinh asset mẫu: 6 nhân vật (đủ độ hiếm) dùng chung vài skill mẫu đủ 3 loại, `CharacterCatalog`, `NewGameConfig`; sprite trỏ tới sprite có sẵn của Unity. Asset đã tồn tại thì giữ nguyên, không ghi đè số bạn đã chỉnh.
5. Cùng menu đó tạo scene `Boot` nếu chưa có, đặt `Boot` đầu Build Settings, và Player Settings chỉ cho phép màn ngang, đặt company `Sweet-Yuu` và product `BunDauPeo` (đường dẫn save phụ thuộc hai tên này).
6. `GameSession`, `GameBootstrap`, `NewGameConfig`, `ServiceLocator` theo mục 3.1 và 4.1, script `playModeStartScene`.
7. Test theo mục 5.
8. Cập nhật docs khi xong: phần "Hiện tại" của `docs/project/structure.md`, `docs/roadmap.md`.

### Ngoài phạm vi

- Logic chạy và SO của gacha, quán, nâng cấp, combat, màn chơi; UI hub.
- Cấp nhân vật và tăng chỉ số theo cấp: chưa có cơ chế nào tăng cấp, thêm khi thiết kế phát triển nhân vật.
- Buff, debuff, skill nhiều hiệu ứng: `SkillDefinition` chỉ có một hiệu ứng, spec Combat sẽ mở rộng.
- AFK: luật đã chốt, triển khai cùng spec Restaurant.
- Animation thật, hệ nguyên tố, trang bị, đổi nhân vật trùng thành tài nguyên.

### Thứ tự spec tiếp theo (đề xuất)

Gacha → Restaurant (gồm nâng cấp và AFK) → Combat (gồm màn chơi) → UI hub. UI riêng của từng tính năng làm trong spec của tính năng đó ở mức tối thiểu để thử được.

## 7. Rủi ro

- Asset mẫu, scene `Boot` và Player Settings được tạo qua Unity ở batch mode. Batch mode không chạy được khi project đang mở trong Editor; lúc đó dùng menu trong Editor.
- Sáu nhân vật mẫu dùng chung vài sprite có sẵn nên trông giống nhau; phân biệt bằng tên cho đến khi có ảnh thật.
- Nếu sau này đổi quyết định "offline, không IAP" thì ảnh hưởng tới `Wallet`, gacha và save.
- Nếu sau này nâng cấp cần điều kiện clear màn: Restaurant đọc tiến độ màn từ `PlayerSave` ở `Core`, không cần phụ thuộc Combat.
- Không có DI container; `ServiceLocator` đủ cho quy mô hiện tại. Nâng lên DI khi số service vượt khả năng quản lý bằng tay.
