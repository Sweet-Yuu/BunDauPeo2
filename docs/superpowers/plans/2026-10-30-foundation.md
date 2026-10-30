# Nền móng kiến trúc + SO nhân vật: Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans (recommended for this plan, see [agent-routing.md](../../guides/agent-routing.md)) or superpowers:subagent-driven-development to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Dựng cây thư mục, các assembly, `Core`, SO nhân vật, `Roster`, save, scene `Boot` và asset mẫu; chưa có gameplay.

**Architecture:** Folder theo tính năng, mỗi tính năng một assembly, tầng `Core <- Characters <- (Gacha, Restaurant, Combat) <- (UI, Boot)`. Logic là class C# thuần nhận phụ thuộc qua constructor, test bằng EditMode. Asset, scene và Player Settings do một script Editor sinh ra, không sửa tay YAML.

**Tech Stack:** Unity 6000.3.15f1, C#, `JsonUtility`, Test Framework 1.6.0, CLI `unity` 1.0.0-beta.15.

**Spec:** [docs/superpowers/specs/2026-10-30-foundation-architecture-design.md](../specs/2026-10-30-foundation-architecture-design.md)

## Global Constraints

- Không sửa/xoá tay `.meta`; không đụng `Library/`, `Logs/`, `UserSettings/`, `Temp/`. Không sửa tay `.unity`, `.asset`, `.prefab`.
- **Commit:** user đã cho phép (2026-10-30): commit sau mỗi task trên nhánh `feature/foundation`, **không push**. Chỉ `git add` file của task đó, không dùng `git add -A` sau commit nền.
- Code: `PascalCase` cho class/method/property; field private `_camelCase`; field chỉnh trong Inspector dùng `[SerializeField] private`. Struct và class thuần dữ liệu được lưu JSON (`CurrencyAmount`, `PlayerSave`, `CombatStats`…) dùng field `public`.
- Comment chỉ giải thích vì sao. Tên code tiếng Anh, docs tiếng Việt.
- Không thêm package. Không dùng `Resources`.
- Namespace trùng tên assembly, riêng assembly `BunDauPeo.Editor` dùng namespace `BunDauPeo.EditorTools`.
- `OnValidate` và cảnh báo dùng `Debug.LogWarning`, không dùng `LogError`, vì `LogError` làm test đỏ.
- Sau mỗi task: Console không có lỗi compile hoặc warning mới từ code dự án.

### Lệnh dùng chung

**Chạy test** (Editor phải đóng, nếu không Unity báo project đang bị khoá):

```bash
unity test . --mode EditMode --output ../bdp-tests.xml --timeout 600 -- -logFile ../bdp-tests.log
```

Exit `0` là đạt, `6` là test đỏ hoặc lỗi compile. Khi đỏ, đọc lỗi bằng:

```bash
grep -E "error CS|Failed|Exception" ../bdp-tests.log | head -30
```

File `../bdp-tests.*` nằm ngoài repo, xoá khi xong đợt. Nếu Editor đang mở: chạy trong Test Runner (`Window > General > Test Runner`).

Trong Unity, một test tham chiếu class chưa tồn tại làm cả assembly không compile. Bước "test đỏ" vì thế là lỗi `error CS0246`, không phải assert fail.

## Review Focus

1. **Tắt app giữa lúc ghi save, còn sót file `.tmp`:** lần mở sau vẫn đọc được save đúng. Test ở Task 2.
2. **Chuyển nhân viên vào đội đã đủ 5:** bị từ chối và nhân vật vẫn làm ở quán, không bị mất khỏi cả hai nơi. Test ở Task 4.
3. **Mở lại game:** không được cấp lại tiền và nhân vật ban đầu. Test ở Task 5.
4. **Chi phí có dòng trùng loại hoặc số âm:** không trừ sai, không cộng thêm tiền. Test ở Task 1.
5. **Chạy lại Setup Project sau khi đã chỉnh số:** số đã chỉnh được giữ nguyên. Test ở Task 6.

## Điều hướng agent

Quy tắc chung ở [agent-routing.md](../../guides/agent-routing.md).

| Task | Model | Effort |
|---|---|---|
| 1. Core, Wallet | Sonnet 5.5 | medium |
| 2. SaveService | Sonnet 5.5 | high |
| 3. SO nhân vật, Catalog | Sonnet 5.5 | medium |
| 4. Roster | Sonnet 5.5 | medium |
| 5. Boot | Sonnet 5.5 | medium |
| 6. ProjectSetup, docs | Sonnet 5.5 | medium |
| Review toàn nhánh | Opus 5.5 | xhigh (nhánh đụng ví và save) |

## Trước khi bắt đầu

- [x] Repo chưa có commit nào. User đã đồng ý (2026-10-30): commit nền hiện tại lên `main` (`chore: initial project and docs`), rồi tạo nhánh `feature/foundation` từ đó trước Task 1, để reviewer so `main..feature/foundation`. Xem `git status` trước khi `git add`: chỉ đưa file nguồn (`Assets/`, `Packages/`, `ProjectSettings/`, `Mota/`, `docs/`, `CLAUDE.md`, `.gitignore`, `.gitattributes`, `.vsconfig`), không đưa `Library/`, `Logs/`, `UserSettings/`, `Temp/`, `*.sln`, `*.csproj`. Không push.
- [x] Đọc `docs/guides/code-style.md` và `docs/guides/unity-workflow.md`.

---

### Task 1: Cây thư mục, assembly Core và Tests, Wallet

**Files:**
- Create: `Assets/_Project/{Core,Characters,Gacha,Restaurant,Combat,UI,Boot}/{Scripts,Data,Art,Animations,Prefabs}/.gitkeep`
- Create: `Assets/_Project/Scenes/.gitkeep`
- Create: `Assets/_Project/Core/Scripts/BunDauPeo.Core.asmdef`
- Create: `Assets/_Project/Core/Scripts/CurrencyType.cs`, `CurrencyAmount.cs`, `StaffRole.cs`, `PlayerSave.cs`, `Wallet.cs`
- Create: `Assets/_Project/Tests/BunDauPeo.Tests.asmdef`
- Test: `Assets/_Project/Tests/WalletTests.cs`

**Interfaces:**
- Produces, namespace `BunDauPeo.Core`:
  - `enum CurrencyType { Coin, Gem, Material }`, `enum StaffRole { Cook, Server, Cashier }`
  - `[Serializable] struct CurrencyAmount { public CurrencyType Type; public long Amount; public CurrencyAmount(CurrencyType type, long amount) }`
  - `[Serializable] struct StaffAssignment { public string CharacterId; public StaffRole Role; }`
  - `[Serializable] class PlayerSave { public int Version = 1; public List<CurrencyAmount> Balances = new(); public List<string> OwnedCharacterIds = new(); public List<string> BattleTeam = new(); public List<StaffAssignment> Staff = new(); }`
  - `sealed class Wallet { Wallet(PlayerSave save); long Get(CurrencyType type); void Add(CurrencyType type, long amount); bool TrySpend(params CurrencyAmount[] costs); event Action Changed; }`

- [x] **Step 1: Tạo cây thư mục và hai asmdef**

`BunDauPeo.Core.asmdef`: `{"name": "BunDauPeo.Core", "rootNamespace": "BunDauPeo.Core", "autoReferenced": true}`

`BunDauPeo.Tests.asmdef`:
```json
{
  "name": "BunDauPeo.Tests",
  "rootNamespace": "BunDauPeo.Tests",
  "references": ["BunDauPeo.Core", "UnityEngine.TestRunner", "UnityEditor.TestRunner"],
  "includePlatforms": ["Editor"],
  "overrideReferences": true,
  "precompiledReferences": ["nunit.framework.dll"],
  "autoReferenced": false,
  "defineConstraints": ["UNITY_INCLUDE_TESTS"]
}
```

- [x] **Step 2: Viết test đỏ `WalletTests.cs`**

```csharp
private PlayerSave _save; private Wallet _wallet;
[SetUp] public void SetUp() { _save = new PlayerSave(); _wallet = new Wallet(_save); }

[Test] public void Add_IncreasesBalance_AndRaisesChanged() {
    var raised = 0; _wallet.Changed += () => raised++;
    _wallet.Add(CurrencyType.Coin, 100);
    Assert.AreEqual(100, _wallet.Get(CurrencyType.Coin));
    Assert.AreEqual(1, raised);
}
[Test] public void Add_Negative_Throws_AndKeepsBalance() {
    _wallet.Add(CurrencyType.Coin, 100);
    Assert.Throws<ArgumentOutOfRangeException>(() => _wallet.Add(CurrencyType.Coin, -1));
    Assert.AreEqual(100, _wallet.Get(CurrencyType.Coin));
}
[Test] public void TrySpend_Enough_Deducts() {
    _wallet.Add(CurrencyType.Coin, 100);
    Assert.IsTrue(_wallet.TrySpend(new CurrencyAmount(CurrencyType.Coin, 30)));
    Assert.AreEqual(70, _wallet.Get(CurrencyType.Coin));
}
[Test] public void TrySpend_MissingOneCurrency_DeductsNothing() {
    _wallet.Add(CurrencyType.Coin, 100);
    var raised = 0; _wallet.Changed += () => raised++;
    Assert.IsFalse(_wallet.TrySpend(new CurrencyAmount(CurrencyType.Coin, 50), new CurrencyAmount(CurrencyType.Material, 1)));
    Assert.AreEqual(100, _wallet.Get(CurrencyType.Coin));
    Assert.AreEqual(0, raised);
}
[Test] public void TrySpend_SameCurrencyLines_AreSummed() {
    _wallet.Add(CurrencyType.Coin, 100);
    Assert.IsFalse(_wallet.TrySpend(new CurrencyAmount(CurrencyType.Coin, 60), new CurrencyAmount(CurrencyType.Coin, 60)));
    Assert.AreEqual(100, _wallet.Get(CurrencyType.Coin));
}
[Test] public void TrySpend_NegativeLine_ReturnsFalse_AndAddsNothing() {
    _wallet.Add(CurrencyType.Coin, 100);
    Assert.IsFalse(_wallet.TrySpend(new CurrencyAmount(CurrencyType.Coin, -50)));
    Assert.AreEqual(100, _wallet.Get(CurrencyType.Coin));
}
[Test] public void TrySpend_NoCosts_ReturnsTrue_WithoutChanged() {
    var raised = 0; _wallet.Changed += () => raised++;
    Assert.IsTrue(_wallet.TrySpend());
    Assert.AreEqual(0, raised);
}
[Test] public void Balance_IsStoredInSave() {
    _wallet.Add(CurrencyType.Gem, 5);
    Assert.AreEqual(5, new Wallet(_save).Get(CurrencyType.Gem));
}
```

- [x] **Step 3: Chạy test, kỳ vọng đỏ**

Chạy lệnh test chung. Kỳ vọng: exit `6`, log có `error CS0246` với `PlayerSave` hoặc `Wallet`.

- [x] **Step 4: Viết các kiểu dữ liệu và `Wallet`**

`Wallet` đọc và ghi trực tiếp `save.Balances`, không giữ bản sao, để lưu game luôn thấy số mới nhất. `TrySpend` kiểm tra hết các dòng trước (số âm thì `false`, cộng gộp theo loại) rồi mới trừ. `Add(type, 0)` không phát `Changed`.

- [x] **Step 5: Chạy test, kỳ vọng xanh**

Kỳ vọng: exit `0`, `../bdp-tests.xml` có `failed="0"` và `total="8"`.

- [x] **Step 6: Commit** `feat: add project skeleton, Core data types and Wallet`

---

### Task 2: SaveService

**Files:**
- Create: `Assets/_Project/Core/Scripts/SaveService.cs`
- Test: `Assets/_Project/Tests/SaveServiceTests.cs`

**Interfaces:**
- Consumes: `PlayerSave` (Task 1).
- Produces: `sealed class SaveService { SaveService(string path); PlayerSave Load(); bool Save(PlayerSave save); }`. `Load` trả `null` khi không có save đọc được. File phụ: `path + ".tmp"`, `".bak"`, `".corrupt"`.

- [ ] **Step 1: Viết test đỏ**

`SetUp` tạo thư mục `Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())`, `_path` là `save.json` trong đó, `_service = new SaveService(_path)`. `TearDown` xoá thư mục. `Make(coin)` tạo `PlayerSave` có một dòng `Balances`.

```csharp
[Test] public void SaveThenLoad_RoundTripsAllFields() {
    var save = new PlayerSave();
    save.Balances.Add(new CurrencyAmount(CurrencyType.Gem, 5));
    save.OwnedCharacterIds.Add("a");
    save.BattleTeam.Add("a");
    save.Staff.Add(new StaffAssignment { CharacterId = "b", Role = StaffRole.Cashier });
    Assert.IsTrue(_service.Save(save));
    var loaded = _service.Load();
    Assert.AreEqual(5, loaded.Balances[0].Amount);
    Assert.AreEqual(CurrencyType.Gem, loaded.Balances[0].Type);
    Assert.AreEqual("a", loaded.OwnedCharacterIds[0]);
    Assert.AreEqual("a", loaded.BattleTeam[0]);
    Assert.AreEqual(StaffRole.Cashier, loaded.Staff[0].Role);
}
[Test] public void Load_NoFile_ReturnsNull() => Assert.IsNull(_service.Load());
[Test] public void Save_CreatesMissingDirectory() =>
    Assert.IsTrue(new SaveService(Path.Combine(_dir, "sub", "save.json")).Save(new PlayerSave()));
[Test] public void Load_CorruptMain_FallsBackToPreviousSave() {
    _service.Save(Make(1)); _service.Save(Make(2));
    File.WriteAllText(_path, "{broken");
    Assert.AreEqual(1, _service.Load().Balances[0].Amount);
}
[Test] public void Load_EmptyMain_FallsBackToPreviousSave() {
    _service.Save(Make(1)); _service.Save(Make(2));
    File.WriteAllText(_path, "");
    Assert.AreEqual(1, _service.Load().Balances[0].Amount);
}
[Test] public void Load_BothCorrupt_ReturnsNull_AndKeepsCopy() {
    File.WriteAllText(_path, "{broken");
    File.WriteAllText(_path + ".bak", "{broken");
    Assert.IsNull(_service.Load());
    Assert.AreEqual("{broken", File.ReadAllText(_path + ".corrupt"));
}
[Test] public void LeftoverTmp_DoesNotAffectLoadOrSave() {
    _service.Save(Make(1));
    File.WriteAllText(_path + ".tmp", "{half-written");
    Assert.AreEqual(1, _service.Load().Balances[0].Amount);
    Assert.IsTrue(_service.Save(Make(2)));
    Assert.AreEqual(2, _service.Load().Balances[0].Amount);
}
[Test] public void Save_WhenTargetUnwritable_ReturnsFalse() {
    Directory.CreateDirectory(_path); // đường dẫn file là một thư mục
    LogAssert.Expect(LogType.Error, new Regex("Save failed"));
    Assert.IsFalse(_service.Save(new PlayerSave()));
}
```

- [ ] **Step 2: Chạy test, kỳ vọng đỏ** với `error CS0246` về `SaveService`.

- [ ] **Step 3: Viết `SaveService`**

- `Save`: tạo thư mục nếu thiếu, ghi `path.tmp`, rồi `File.Replace(tmp, path, path + ".bak")` nếu file chính đã có, không thì `File.Move(tmp, path)`. Bắt `IOException` và `UnauthorizedAccessException`, `Debug.LogError("Save failed: ...")`, trả `false`.
- `Load`: đọc `path` rồi đến `.bak`. `JsonUtility.FromJson` ném `ArgumentException` khi JSON hỏng và trả `null` khi rỗng, cả hai đều là không đọc được. Dùng `.bak` thì `LogWarning`. Cả hai hỏng và file chính có tồn tại thì `File.Copy(path, path + ".corrupt", true)` và `LogWarning`.

- [ ] **Step 4: Chạy test, kỳ vọng xanh** với `failed="0"`, `total="16"`.

- [ ] **Step 5: Commit** `feat: add SaveService with atomic write and backup recovery`

---

### Task 3: SO nhân vật và Catalog

**Files:**
- Create: `Assets/_Project/Core/Scripts/GameDefinition.cs`, `Catalog.cs`
- Create: `Assets/_Project/Characters/Scripts/BunDauPeo.Characters.asmdef` (references `BunDauPeo.Core`)
- Create: `Assets/_Project/Characters/Scripts/CharacterEnums.cs`, `CharacterStats.cs`, `SkillDefinition.cs`, `CharacterDefinition.cs`, `CharacterCatalog.cs`
- Modify: `Assets/_Project/Tests/BunDauPeo.Tests.asmdef` (thêm `BunDauPeo.Characters`)
- Test: `Assets/_Project/Tests/TestData.cs`, `CharacterDefinitionTests.cs`, `CatalogTests.cs`

**Interfaces:**
- Produces, namespace `BunDauPeo.Core`:
  - `abstract class GameDefinition : ScriptableObject { [SerializeField] string _id; public string Id }`
  - `abstract class Catalog<T> : ScriptableObject where T : GameDefinition { [SerializeField] T[] _items; IReadOnlyList<T> Items; bool TryGet(string id, out T definition); List<string> Validate(); }`
- Produces, namespace `BunDauPeo.Characters`:
  - `enum Rarity { R, SR, SSR }`, `enum SkillKind { Basic, Skill, Ultimate }`, `enum SkillTarget { SingleEnemy, AllEnemies, SingleAlly, AllAllies, Self }`, `enum SkillEffect { Damage, Heal }`
  - `[Serializable] struct CombatStats { public int HP, Attack, Defense, Speed; }`, `[Serializable] struct RestaurantStats { public int Cooking, Serving, Cashier; }`
  - `SkillDefinition : ScriptableObject`, field `_displayName, _icon, _kind, _target, _effect, _power (float), _cooldown (int), _energyCost (int), _animationTrigger`, mỗi field có property đọc cùng tên PascalCase.
  - `CharacterDefinition : GameDefinition`, field `_displayName, _rarity, _portrait, _battleSprite, _combatStats, _restaurantStats, _basicAttack, _skill, _ultimate`, property đọc tương ứng, và `List<string> Validate()`.
  - `CharacterCatalog : Catalog<CharacterDefinition>`.
  - Các SO có `[CreateAssetMenu(menuName = "BunDauPeo/Characters/...")]`.
- Produces, test helper `internal static class TestData` (dùng `SerializedObject` và `ApplyModifiedPropertiesWithoutUndo`, không thêm setter vào code game):
  - `SkillDefinition Skill(SkillKind kind)`
  - `CharacterDefinition Character(string id)`: có đủ 3 skill đúng loại
  - `CharacterCatalog Catalog(params CharacterDefinition[] items)`
  - `void SetRef(UnityEngine.Object target, string property, UnityEngine.Object value)`

- [ ] **Step 1: Viết test đỏ**

```csharp
// CharacterDefinitionTests
[Test] public void Validate_Complete_NoErrors() => CollectionAssert.IsEmpty(TestData.Character("a").Validate());
[Test] public void Validate_MissingSkill_Reported() {
    var c = TestData.Character("a");
    TestData.SetRef(c, "_ultimate", null);
    Assert.That(c.Validate(), Has.Some.Contains("Ultimate"));
}
[Test] public void Validate_WrongKind_Reported() {
    var c = TestData.Character("a");
    TestData.SetRef(c, "_basicAttack", TestData.Skill(SkillKind.Ultimate));
    Assert.That(c.Validate(), Has.Some.Contains("BasicAttack"));
}

// CatalogTests
[Test] public void TryGet_Found() {
    var a = TestData.Character("a");
    Assert.IsTrue(TestData.Catalog(a).TryGet("a", out var found));
    Assert.AreSame(a, found);
}
[Test] public void TryGet_Missing_ReturnsFalse() => Assert.IsFalse(TestData.Catalog().TryGet("x", out _));
[Test] public void TryGet_NullId_ReturnsFalse() => Assert.IsFalse(TestData.Catalog(TestData.Character("a")).TryGet(null, out _));
[Test] public void Validate_DuplicateId_Reported() =>
    Assert.AreEqual(1, TestData.Catalog(TestData.Character("a"), TestData.Character("a")).Validate().Count);
[Test] public void Validate_BlankId_Reported() => Assert.AreEqual(1, TestData.Catalog(TestData.Character("")).Validate().Count);
[Test] public void NullEntry_DoesNotThrow() {
    var catalog = TestData.Catalog(new CharacterDefinition[] { null });
    Assert.IsFalse(catalog.TryGet("a", out _));
    Assert.AreEqual(1, catalog.Validate().Count);
}
```

- [ ] **Step 2: Chạy test, kỳ vọng đỏ** với `error CS0246`.

- [ ] **Step 3: Viết các class**

`Validate` của `CharacterDefinition` trả chuỗi bắt đầu bằng tên ô (`"BasicAttack: ..."`). `OnValidate` của nhân vật và catalog chỉ gọi `Validate()` và `Debug.LogWarning` từng dòng, kèm `this` làm context. `TryGet` quét mảng tuyến tính.

- [ ] **Step 4: Chạy test, kỳ vọng xanh** với `failed="0"`, `total="25"`.

- [ ] **Step 5: Commit** `feat: add character ScriptableObjects and Catalog`

---

### Task 4: Roster

**Files:**
- Create: `Assets/_Project/Characters/Scripts/Roster.cs`
- Test: `Assets/_Project/Tests/RosterTests.cs`

**Interfaces:**
- Consumes: `PlayerSave`, `StaffRole`, `StaffAssignment`, `CharacterCatalog`, `TestData`.
- Produces: `sealed class Roster` trong `BunDauPeo.Characters`:
  ```csharp
  public const int MaxBattleTeamSize = 5;
  public Roster(PlayerSave save, CharacterCatalog catalog);
  public event Action Changed;
  public bool IsOwned(string id);
  public bool Add(string id);
  public bool AddToTeam(string id);
  public bool RemoveFromTeam(string id);
  public bool AssignStaff(string id, StaffRole role);
  public bool RemoveStaff(string id);
  public IReadOnlyList<CharacterDefinition> GetOwned();
  public IReadOnlyList<CharacterDefinition> GetBattleTeam();
  public IReadOnlyList<CharacterDefinition> GetStaff(StaffRole role);
  ```

- [ ] **Step 1: Viết test đỏ**

`SetUp` tạo catalog 7 nhân vật `"c0"`..`"c6"`, `_save`, `_roster`, và `Add` tất cả.

```csharp
[Test] public void Add_Duplicate_ReturnsFalse() => Assert.IsFalse(_roster.Add("c0"));
[Test] public void Add_NotInCatalog_ReturnsFalse() => Assert.IsFalse(_roster.Add("ghost"));
[Test] public void AddToTeam_NotOwned_ReturnsFalse() {
    var roster = new Roster(new PlayerSave(), _catalog);
    Assert.IsFalse(roster.AddToTeam("c0"));
}
[Test] public void AddToTeam_RemovesFromStaff() {
    _roster.AssignStaff("c0", StaffRole.Cook);
    Assert.IsTrue(_roster.AddToTeam("c0"));
    CollectionAssert.IsEmpty(_roster.GetStaff(StaffRole.Cook));
    Assert.AreEqual("c0", _roster.GetBattleTeam()[0].Id);
}
[Test] public void AssignStaff_RemovesFromTeam() {
    _roster.AddToTeam("c0");
    Assert.IsTrue(_roster.AssignStaff("c0", StaffRole.Cook));
    CollectionAssert.IsEmpty(_roster.GetBattleTeam());
}
[Test] public void AddToTeam_Full_RejectedAndStaysAtRestaurant() {
    for (var i = 1; i <= 5; i++) _roster.AddToTeam("c" + i);
    _roster.AssignStaff("c0", StaffRole.Cook);
    Assert.IsFalse(_roster.AddToTeam("c0"));
    Assert.AreEqual(5, _roster.GetBattleTeam().Count);
    Assert.AreEqual("c0", _roster.GetStaff(StaffRole.Cook)[0].Id);
}
[Test] public void AddToTeam_AlreadyInTeam_ReturnsFalse() {
    _roster.AddToTeam("c0");
    Assert.IsFalse(_roster.AddToTeam("c0"));
    Assert.AreEqual(1, _roster.GetBattleTeam().Count);
}
[Test] public void AssignStaff_NewRole_ReplacesOldRole() {
    _roster.AssignStaff("c0", StaffRole.Cook);
    Assert.IsTrue(_roster.AssignStaff("c0", StaffRole.Cashier));
    CollectionAssert.IsEmpty(_roster.GetStaff(StaffRole.Cook));
    Assert.AreEqual(1, _roster.GetStaff(StaffRole.Cashier).Count);
    Assert.AreEqual(1, _save.Staff.Count);
}
[Test] public void AssignStaff_SameRole_ReturnsFalse_WithoutChanged() {
    _roster.AssignStaff("c0", StaffRole.Cook);
    var raised = 0; _roster.Changed += () => raised++;
    Assert.IsFalse(_roster.AssignStaff("c0", StaffRole.Cook));
    Assert.AreEqual(0, raised);
}
[Test] public void UnknownId_SkippedInLists_ButKeptInSave() {
    var save = new PlayerSave();
    save.OwnedCharacterIds.Add("ghost");
    save.BattleTeam.Add("ghost");
    var roster = new Roster(save, _catalog);
    CollectionAssert.IsEmpty(roster.GetOwned());
    CollectionAssert.IsEmpty(roster.GetBattleTeam());
    Assert.Contains("ghost", save.OwnedCharacterIds);
    Assert.Contains("ghost", save.BattleTeam);
}
[Test] public void OwnedGhost_CannotJoinTeam() {
    var save = new PlayerSave();
    save.OwnedCharacterIds.Add("ghost");
    Assert.IsFalse(new Roster(save, _catalog).AddToTeam("ghost"));
}
[Test] public void Changed_RaisedOnRealChangeOnly() {
    var raised = 0; _roster.Changed += () => raised++;
    _roster.AddToTeam("c0");
    _roster.RemoveFromTeam("c0");
    _roster.RemoveFromTeam("c0");
    Assert.AreEqual(2, raised);
}
```

- [ ] **Step 2: Chạy test, kỳ vọng đỏ** với `error CS0246` về `Roster`.

- [ ] **Step 3: Viết `Roster`**

Thao tác trực tiếp trên `PlayerSave`, cùng cách với `Wallet`. `AddToTeam` kiểm tra sở hữu, có trong catalog, chưa trong đội, đội chưa đủ, **rồi mới** gỡ khỏi `Staff`. Constructor `LogWarning` một lần cho mỗi Id không có trong catalog, kể cả ở `BattleTeam` và `Staff`. Các getter tạo danh sách mới và bỏ qua Id lạ mà không log.

- [ ] **Step 4: Chạy test, kỳ vọng xanh** với `failed="0"`, `total="37"`.

- [ ] **Step 5: Commit** `feat: add Roster with team and staff exclusion`

---

### Task 5: Boot

**Files:**
- Create: `Assets/_Project/Core/Scripts/ServiceLocator.cs`
- Create: `Assets/_Project/Boot/Scripts/BunDauPeo.Boot.asmdef` (references `BunDauPeo.Core`, `BunDauPeo.Characters`)
- Create: `Assets/_Project/Boot/Scripts/NewGameConfig.cs`, `GameSession.cs`, `GameBootstrap.cs`
- Modify: `Assets/_Project/Tests/BunDauPeo.Tests.asmdef` (thêm `BunDauPeo.Boot`), `TestData.cs`
- Test: `Assets/_Project/Tests/ServiceLocatorTests.cs`, `GameSessionTests.cs`

**Interfaces:**
- Consumes: `SaveService`, `Wallet`, `Roster`, `CharacterCatalog`.
- Produces:
  - `BunDauPeo.Core.ServiceLocator`: `static void Register<T>(T service)`, `static T Get<T>()` (thiếu thì `InvalidOperationException` nêu tên kiểu), `static void Clear()`.
  - `BunDauPeo.Boot.NewGameConfig : ScriptableObject`, field `_startingCurrency` (`CurrencyAmount[]`), `_starterCharacters` (`CharacterDefinition[]`), property đọc tương ứng.
  - `BunDauPeo.Boot.GameSession`: `GameSession(SaveService saveService, CharacterCatalog catalog, NewGameConfig newGame)`; `PlayerSave Save`, `Wallet Wallet`, `Roster Roster`, `bool IsNewGame`, `bool Persist()`.
  - `BunDauPeo.Boot.GameBootstrap : MonoBehaviour`, field `_catalog`, `_newGameConfig`.
  - `TestData.Config(CurrencyAmount[] currency, params CharacterDefinition[] starters)`.

- [ ] **Step 1: Viết test đỏ**

```csharp
// ServiceLocatorTests: TearDown gọi ServiceLocator.Clear()
[Test] public void Get_Registered_ReturnsService() {
    var save = new PlayerSave();
    ServiceLocator.Register(save);
    Assert.AreSame(save, ServiceLocator.Get<PlayerSave>());
}
[Test] public void Get_Missing_Throws_NamingType() {
    var ex = Assert.Throws<InvalidOperationException>(() => ServiceLocator.Get<PlayerSave>());
    StringAssert.Contains("PlayerSave", ex.Message);
}

// GameSessionTests: dùng đường dẫn thư mục tạm như Task 2, catalog có "a", config cho 500 Coin và starter "a"
[Test] public void NewGame_GrantsStarter_AndWritesFile() {
    var session = new GameSession(_service, _catalog, _config);
    Assert.IsTrue(session.IsNewGame);
    Assert.AreEqual(500, session.Wallet.Get(CurrencyType.Coin));
    Assert.IsTrue(session.Roster.IsOwned("a"));
    Assert.IsTrue(File.Exists(_path));
}
[Test] public void Reopen_DoesNotGrantStarterAgain() {
    var first = new GameSession(_service, _catalog, _config);
    first.Wallet.TrySpend(new CurrencyAmount(CurrencyType.Coin, 100));
    first.Persist();
    var second = new GameSession(_service, _catalog, _config);
    Assert.IsFalse(second.IsNewGame);
    Assert.AreEqual(400, second.Wallet.Get(CurrencyType.Coin));
}
[Test] public void CorruptSave_StartsNewGame_AndKeepsCopy() {
    File.WriteAllText(_path, "{broken");
    var session = new GameSession(_service, _catalog, _config);
    Assert.IsTrue(session.IsNewGame);
    Assert.IsTrue(File.Exists(_path + ".corrupt"));
}
[Test] public void ConfigWithBadEntries_DoesNotThrow() {
    var config = TestData.Config(new[] { new CurrencyAmount(CurrencyType.Coin, -5) }, new CharacterDefinition[] { null });
    var session = new GameSession(_service, _catalog, config);
    Assert.AreEqual(0, session.Wallet.Get(CurrencyType.Coin));
}
```

- [ ] **Step 2: Chạy test, kỳ vọng đỏ** với `error CS0246`.

- [ ] **Step 3: Viết `ServiceLocator`, `NewGameConfig`, `GameSession`**

`GameSession`: `Load()` trả `null` thì là game mới. Tạo `Wallet`, `Roster`. Nếu game mới: cộng các dòng tiền có `Amount > 0`, `Roster.Add` các nhân vật khác `null`, rồi `Persist()`.

- [ ] **Step 4: Viết `GameBootstrap`**

`Awake`: `_catalog` hoặc `_newGameConfig` null thì `Debug.LogError` kèm tên field và `return`. Nếu đủ: `DontDestroyOnLoad(gameObject)`, tạo `GameSession` với `Path.Combine(Application.persistentDataPath, "save.json")`, `ServiceLocator.Clear()`, `Register(session.Wallet)`, `Register(session.Roster)`. `OnApplicationPause(true)` và `OnApplicationQuit` gọi `Persist()`, bỏ qua nếu session null.

- [ ] **Step 5: Chạy test, kỳ vọng xanh** với `failed="0"`, `total="43"`.

- [ ] **Step 6: Commit** `feat: add GameSession and GameBootstrap`

---

### Task 6: ProjectSetup, asset mẫu, scene Boot, docs

**Files:**
- Create: `Assets/_Project/Editor/BunDauPeo.Editor.asmdef` (references `BunDauPeo.Core`, `BunDauPeo.Characters`, `BunDauPeo.Boot`; `includePlatforms: ["Editor"]`; `rootNamespace: BunDauPeo.EditorTools`)
- Create: `Assets/_Project/Editor/ProjectSetup.cs`, `BootScenePlayMode.cs`
- Modify: `Assets/_Project/Tests/BunDauPeo.Tests.asmdef` (thêm `BunDauPeo.Editor`)
- Generate (bởi `ProjectSetup.Run`): `Assets/_Project/Characters/Data/Skills/*.asset`, `Characters/Data/Characters/*.asset`, `Characters/Data/CharacterCatalog.asset`, `Boot/Data/NewGameConfig.asset`, `Scenes/Boot.unity`, `ProjectSettings/*`
- Modify: `docs/project/structure.md`, `docs/roadmap.md`
- Test: `Assets/_Project/Tests/ProjectSetupTests.cs`

**Interfaces:**
- Produces, namespace `BunDauPeo.EditorTools`:
  - `public static class ProjectSetup { public const string BootScenePath = "Assets/_Project/Scenes/Boot.unity"; public const string CatalogPath = "Assets/_Project/Characters/Data/CharacterCatalog.asset"; public const string NewGameConfigPath = "Assets/_Project/Boot/Data/NewGameConfig.asset"; [MenuItem("Tools/BunDauPeo/Setup Project")] public static void Run(); public static T GetOrCreateAsset<T>(string path, Action<T> init) where T : ScriptableObject; }`

**Dữ liệu mẫu** (chỉ là số khởi điểm, chỉnh trong Inspector):

| Skill (file `Skill_*.asset`) | Kind | Target | Effect | Power | Cooldown | Energy |
|---|---|---|---|---|---|---|
| BasicStrike | Basic | SingleEnemy | Damage | 1.0 | 0 | 0 |
| PowerStrike | Skill | SingleEnemy | Damage | 1.8 | 3 | 0 |
| Heal | Skill | SingleAlly | Heal | 1.2 | 4 | 0 |
| Finisher | Ultimate | AllEnemies | Damage | 2.5 | 0 | 100 |

| Id | Tên | Rarity | HP / Atk / Def / Spd | Nấu / Phục vụ / Thu ngân | Skill |
|---|---|---|---|---|---|
| `char_nem_chua` | Nem Chua | R | 800 / 80 / 40 / 100 | 30 / 20 / 10 | PowerStrike |
| `char_cha_com` | Chả Cốm | R | 900 / 70 / 50 / 95 | 20 / 30 / 10 | PowerStrike |
| `char_dau_phu` | Đậu Phụ | R | 1000 / 60 / 60 / 90 | 10 / 20 / 30 | Heal |
| `char_mam_tom` | Mắm Tôm | SR | 1000 / 110 / 50 / 110 | 50 / 30 / 20 | PowerStrike |
| `char_long_doi` | Lòng Dồi | SR | 1200 / 90 / 70 / 100 | 30 / 50 / 20 | Heal |
| `char_bun_la` | Bún Lá | SSR | 1400 / 130 / 80 / 120 | 60 / 50 / 50 | PowerStrike |

Mọi nhân vật dùng `BasicStrike` và `Finisher`. Portrait dùng `AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd")`, BattleSprite và icon skill dùng `"UI/Skin/Knob.psd"`.

`NewGameConfig`: 500 Coin, 1500 Gem; starter là `char_nem_chua`, `char_cha_com`, `char_dau_phu`.

- [ ] **Step 1: Viết test đỏ**

```csharp
private const string TempAsset = "Assets/_Project/Tests/_TempSetupTest.asset";
[TearDown] public void TearDown() => AssetDatabase.DeleteAsset(TempAsset);

[Test] public void GetOrCreateAsset_Existing_KeepsItAndSkipsInit() {
    var first = ProjectSetup.GetOrCreateAsset<CharacterCatalog>(TempAsset, _ => { });
    var called = false;
    var second = ProjectSetup.GetOrCreateAsset<CharacterCatalog>(TempAsset, _ => called = true);
    Assert.AreSame(first, second);
    Assert.IsFalse(called);
}
[Test] public void SampleCatalog_IsValid_AndCoversAllRarities() {
    var catalog = AssetDatabase.LoadAssetAtPath<CharacterCatalog>(ProjectSetup.CatalogPath);
    Assert.IsNotNull(catalog);
    Assert.GreaterOrEqual(catalog.Items.Count, 6);
    CollectionAssert.IsEmpty(catalog.Validate());
    foreach (var c in catalog.Items) {
        CollectionAssert.IsEmpty(c.Validate());
        Assert.IsNotNull(c.Portrait);
    }
    foreach (Rarity r in Enum.GetValues(typeof(Rarity)))
        Assert.IsTrue(catalog.Items.Any(c => c.Rarity == r), r.ToString());
}
[Test] public void NewGameConfig_StartersAreInCatalog() {
    var catalog = AssetDatabase.LoadAssetAtPath<CharacterCatalog>(ProjectSetup.CatalogPath);
    var config = AssetDatabase.LoadAssetAtPath<NewGameConfig>(ProjectSetup.NewGameConfigPath);
    Assert.IsNotEmpty(config.StarterCharacters);
    foreach (var c in config.StarterCharacters) Assert.IsTrue(catalog.TryGet(c.Id, out _), c.Id);
}
[Test] public void BootScene_IsFirstInBuild_AndBootstrapIsWired() {
    Assert.AreEqual(ProjectSetup.BootScenePath, EditorBuildSettings.scenes[0].path);
    var scene = EditorSceneManager.OpenScene(ProjectSetup.BootScenePath, OpenSceneMode.Additive);
    try {
        var bootstrap = scene.GetRootGameObjects().Select(g => g.GetComponent<GameBootstrap>()).Single(b => b != null);
        var so = new SerializedObject(bootstrap);
        Assert.IsNotNull(so.FindProperty("_catalog").objectReferenceValue);
        Assert.IsNotNull(so.FindProperty("_newGameConfig").objectReferenceValue);
    } finally { EditorSceneManager.CloseScene(scene, true); }
}
[Test] public void PlayerSettings_LandscapeOnly() {
    Assert.AreEqual(UIOrientation.AutoRotation, PlayerSettings.defaultInterfaceOrientation);
    Assert.IsFalse(PlayerSettings.allowedAutorotateToPortrait);
    Assert.IsFalse(PlayerSettings.allowedAutorotateToPortraitUpsideDown);
    Assert.IsTrue(PlayerSettings.allowedAutorotateToLandscapeLeft);
    Assert.IsTrue(PlayerSettings.allowedAutorotateToLandscapeRight);
}
[Test] public void PlayerSettings_Identity() {
    // Đường dẫn save phụ thuộc hai tên này; đổi sau khi có save thật thì save cũ bị bỏ lại.
    Assert.AreEqual("Sweet-Yuu", PlayerSettings.companyName);
    Assert.AreEqual("BunDauPeo", PlayerSettings.productName);
}
```

- [ ] **Step 2: Chạy test, kỳ vọng đỏ** với `error CS0246` về `ProjectSetup`.

- [ ] **Step 3: Viết `ProjectSetup`**

- `GetOrCreateAsset`: có asset thì trả về, không gọi `init`. Không có thì `CreateInstance`, `init`, đặt tên, `AssetDatabase.CreateAsset`.
- Thư mục đích đã có từ Task 1; `Skills/` và `Characters/` tạo mới bằng `AssetDatabase.CreateFolder`.
- Gán field riêng tư qua `SerializedObject`, giống `TestData`.
- Scene: chỉ tạo khi chưa có. Dùng `EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive)`, thêm GameObject `GameBootstrap`, gán hai tham chiếu, `SaveScene`, `CloseScene(scene, true)`. Additive để không đụng scene đang mở.
- Build Settings: đưa Boot lên đầu, giữ scene còn lại, không tạo trùng.
- Player Settings theo test `PlayerSettings_LandscapeOnly` và `PlayerSettings_Identity` (`companyName = "Sweet-Yuu"`, `productName = "BunDauPeo"`, user chốt 2026-10-30). Không đụng Bundle Identifier.
- Kết thúc bằng `AssetDatabase.SaveAssets()`.

- [ ] **Step 4: Viết `BootScenePlayMode`**

`[InitializeOnLoadMethod]`, qua `EditorApplication.delayCall` (tránh load asset trong lúc import), đặt `EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ProjectSetup.BootScenePath)`. Chưa có scene thì giá trị là `null`, Play vẫn bình thường.

- [ ] **Step 5: Chạy Setup ở batch mode**

```bash
unity run . --timeout 600 -- -executeMethod BunDauPeo.EditorTools.ProjectSetup.Run -logFile ../bdp-setup.log
```

Kỳ vọng: exit `0`. Editor đang mở thì dùng menu `Tools/BunDauPeo/Setup Project`.

- [ ] **Step 6: Chạy test, kỳ vọng xanh** với `failed="0"`, `total="49"`.

- [ ] **Step 7: Chạy lại Setup, xác nhận không phá gì**

Chạy lại lệnh Step 5 rồi lệnh test. Kỳ vọng: vẫn `failed="0"`, và `git status` không có thay đổi mới ở `Characters/Data` (nếu đã commit trước đó).

- [ ] **Step 8: Thử trong Editor**

```bash
unity open .
```

Bấm Play từ `SampleScene`. Kỳ vọng: scene `Boot` chạy, Console không có lỗi từ code dự án, `Application.persistentDataPath/save.json` xuất hiện. Dừng và Play lại: số tiền không tăng. Tìm đường dẫn bằng cách đọc `Application.persistentDataPath` trong Console hoặc `unity command eval` nếu Editor kết nối được. Không tự kiểm tra được thì nhờ user và ghi rõ trong báo cáo.

- [ ] **Step 9: Cập nhật docs**

- `docs/project/structure.md`, mục "Hiện tại": ghi cây thư mục đã dựng.
- `docs/roadmap.md`: chuyển mục "Nền móng" sang "Đã xong" kèm ngày; "Đang làm" để trống.
- Lesson mới nếu có lỗi phát sinh trong đợt này.

- [ ] **Step 10: Commit** `feat: add ProjectSetup, sample data and Boot scene`

---

## Hoàn tất đợt

- [ ] Xoá `../bdp-tests.*` và `../bdp-setup.log`.
- [ ] Gọi một subagent mới (Opus 5.5, effort xhigh, theo `docs/guides/agent-routing.md`) review toàn nhánh theo spec, tập trung `Wallet`, `SaveService`, `Roster`, và mục Review Focus.
- [ ] Sửa các lỗi review tìm ra, chạy lại test, báo cáo user.
