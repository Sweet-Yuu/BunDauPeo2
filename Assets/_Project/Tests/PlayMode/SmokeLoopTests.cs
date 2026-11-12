using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using BunDauPeo.Boot;
using BunDauPeo.Characters;
using BunDauPeo.Combat;
using BunDauPeo.Core;
using BunDauPeo.Playtest;
using BunDauPeo.Restaurant;
using BunDauPeo.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BunDauPeo.PlayModeTests
{
    // Vòng Combat trọn vẹn qua UI thật: Boot → bản đồ → đội → trận AUTO → thưởng → nạp lại save. Chạy trên save tạm.
    // Cùng fixture (chung save tạm và guard) có thêm các test panel DEV: mở từ Menu của hub, luồng xác nhận Reset, Reset thất bại.
    public class SmokeLoopTests
    {
        private const float SceneTimeout = 30f;
        private const float ButtonTimeout = 15f;
        private const float BattleTimeout = 60f;

        private string _dir;
        private string _savePath;
        private string _realSaveHash;
        private string _step = "SetUp";
        private bool _refused;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Chạy từ Test Runner trong Editor không có -runTests nên Boot đã chạy với save thật (L-023): dừng trước khi đụng gì.
            _refused = Object.FindFirstObjectByType<GameBootstrap>() != null;
            Assert.IsFalse(_refused, "Đã có GameBootstrap chạy với save thật (Play từ Boot trong Editor). Chạy smoke test bằng `unity test --mode PlayMode` (Editor đóng), không chạy từ Test Runner.");
            _dir = Path.Combine(Path.GetTempPath(), "bdp-smoke-" + Path.GetRandomFileName());
            _savePath = Path.Combine(_dir, "save.json");
            _realSaveHash = HashRealSave();
            yield return null; // Play đã chạy hẳn rồi mới đặt override và nạp Boot
            GameBootstrap.SavePathOverride = _savePath;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_refused) yield break; // bản Boot đang chạy là của người chơi, không phải của test: không huỷ, không xoá
            GameBootstrap.SavePathOverride = null;
            GameLanguage.Current = Language.Vi;
            var bootstrap = Object.FindFirstObjectByType<GameBootstrap>();
            if (bootstrap != null) Object.Destroy(bootstrap.gameObject); // DontDestroyOnLoad nên không tự mất
            var overlay = GameObject.Find("DevOverlay"); // test panel DEV dựng tay, cũng DontDestroyOnLoad
            if (overlay != null) Object.Destroy(overlay);
            ServiceLocator.Clear();
            yield return null;
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
            Assert.AreEqual(_realSaveHash, HashRealSave(), "save thật đã bị đổi");
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator FullLoop_NewGame_Stage1_AutoWin_RewardsAndSaveReload()
        {
            _step = "1 nạp Boot";
            yield return BootToLobby();
            var session = Object.FindFirstObjectByType<GameBootstrap>().Session;
            Assert.IsNotNull(session, Where("GameBootstrap không có Session"));
            Assert.IsTrue(session.IsNewGame, Where("save tạm phải là game mới"));
            var newGame = LoadNewGameConfig();
            foreach (CurrencyType type in Enum.GetValues(typeof(CurrencyType)))
                Assert.AreEqual(newGame.StartingCurrency.Where(l => l.Type == type).Sum(l => l.Amount), session.Wallet.Get(type),
                    Where($"ví {type} lúc mới"));

            var combat = ServiceLocator.Get<CombatService>();
            // "Màn 1" là màn Battle đầu tiên; các màn Truyện đứng trước nó phải qua trước (màn sau chỉ mở khi màn trước đã clear).
            var stages = combat.Config.Stages.Items;
            var battleIndex = stages.Select((st, i) => (st, i)).First(x => x.st.Kind == StageKind.Battle).i;
            var stage1 = stages[battleIndex];
            var stage2 = stages[battleIndex + 1];
            var walletBefore = Enum.GetValues(typeof(CurrencyType)).Cast<CurrencyType>().ToDictionary(t => t, t => session.Wallet.Get(t));
            var staminaBefore = combat.Stamina.Current; // đọc một lần để chốt số thô (L-021)
            Assert.AreEqual(combat.Stamina.Max, staminaBefore, Where("game mới phải đầy thể lực"));

            // Bản đồ là panel của Lobby: bấm ô Màn chơi của thanh dưới.
            _step = "2 vào bản đồ";
            yield return ClickWhenReady("NavTabStages_PH_320x120");
            yield return ClickWhenReady("ModeMain_PH_820x560");

            _step = "3 chọn màn 1 và đội";
            for (var i = 0; i < battleIndex; i++)
            {
                yield return ClickWhenReady("Node" + i);
                yield return ClickWhenReady("ContinueButton");
                Assert.AreEqual(StageStatus.Cleared, combat.Progress.GetStatus(stages[i].Id), Where($"màn Truyện {i} chưa clear"));
            }
            yield return ClickWhenReady("Node" + battleIndex);
            yield return ClickWhenReady("ChallengeButton");
            var teamSize = Math.Min(session.Roster.GetOwned().Count, Roster.MaxBattleTeamSize);
            Assert.Greater(teamSize, 0, Where("không có nhân vật khởi đầu"));
            for (var i = 0; i < teamSize; i++) yield return ClickWhenReady("TeamCard" + i);
            Assert.AreEqual(teamSize, session.Roster.GetBattleTeam().Count, Where("đội sau khi chọn thẻ"));

            _step = "4 vào trận";
            yield return ClickWhenReady("StartButton");
            if (BattleFormation.GetEntryStatus(teamSize) == TeamEntryStatus.NeedsConfirm)
                yield return ClickWhenReady("SmallConfirmYesButton");
            yield return WaitForScene(BattleView.BattleScene);

            _step = "5 bật x2 và AUTO";
            Assert.IsFalse(combat.AutoEnabled, Where("AUTO phải tắt khi vào trận"));
            yield return ClickWhenReady("SpeedButton");
            yield return ClickWhenReady("AutoButton");
            Assert.IsTrue(combat.FastEnabled, Where("x2 chưa bật"));
            Assert.IsTrue(combat.AutoEnabled, Where("AUTO chưa bật"));

            _step = "6 chờ kết quả trận";
            var deadline = Time.realtimeSinceStartup + BattleTimeout;
            while (combat.Current == null || (combat.Current.State != BattleState.Won && combat.Current.State != BattleState.Lost))
            {
                if (Time.realtimeSinceStartup > deadline)
                    Assert.Fail(Where($"{BattleTimeout}s mà trận chưa xong (state {combat.Current?.State}). Nút: {UiDriver.DescribeVisibleButtons()}"));
                yield return null;
            }
            Assert.AreEqual(BattleState.Won, combat.Current.State, Where("thua màn 1 bằng AUTO: lỗi cân bằng thật"));
            yield return WaitForButtonStep("ConfirmButton"); // màn kết quả đã dựng nghĩa là thưởng đã trả (BattleResultView.Show)

            _step = "7 kiểm kết quả";
            Assert.AreEqual(staminaBefore - stage1.StaminaCost, combat.Stamina.Current, Where("thể lực sau trận"));
            foreach (var line in stage1.FirstClearReward)
                Assert.AreEqual(walletBefore[line.Type] + line.Amount, session.Wallet.Get(line.Type), Where($"thưởng lần đầu {line.Type}"));
            Assert.AreEqual(StageStatus.Cleared, combat.Progress.GetStatus(stage1.Id), Where("màn 1"));
            Assert.AreEqual(StageStatus.Available, combat.Progress.GetStatus(stage2.Id), Where("màn 2 phải mở"));
            Assert.Contains(stage1.Id, session.Save.ClearedStages.ToList());
            var shot = ScreenCapture1080.Capture("smoke-result.png");
            Assert.IsFalse(shot.StartsWith("ERROR:"), Where(shot));
            Assert.IsTrue(File.Exists(shot), Where("ảnh chụp không có: " + shot));
            Debug.Log("Smoke test screenshot: " + shot);

            _step = "8 về bản đồ";
            yield return ClickWhenReady("ConfirmButton");
            yield return WaitForScene(BattleView.RestaurantScene);

            // So từng trường chứ không so cả JSON: quán còn chạy ngầm và đổi Afk/DailyPresence trong bộ nhớ sau lần lưu cuối.
            _step = "9 nạp lại save";
            var loaded = new SaveService(_savePath).Load();
            Assert.IsNotNull(loaded, Where("không đọc lại được save tạm"));
            var loadedWallet = new Wallet(loaded);
            foreach (CurrencyType type in Enum.GetValues(typeof(CurrencyType)))
                Assert.AreEqual(session.Wallet.Get(type), loadedWallet.Get(type), Where($"ví {type} trong save"));
            CollectionAssert.AreEqual(session.Save.ClearedStages, loaded.ClearedStages, Where("ClearedStages trong save"));
            CollectionAssert.AreEqual(session.Save.OwnedCharacterIds, loaded.OwnedCharacterIds, Where("nhân vật trong save"));
            CollectionAssert.AreEqual(session.Save.BattleTeam, loaded.BattleTeam, Where("đội trong save"));
            Assert.AreEqual(staminaBefore - stage1.StaminaCost, loaded.Stamina.Current, Where("thể lực trong save"));
        }

        // Nâng sao qua màn Nhân vật thật: trừ mảnh, tăng sao, vào save; Reset xoá sạch và màn đang mở vẽ lại (Review Focus 1).
        [UnityTest, Timeout(180000)]
        public IEnumerator Characters_StarUp_SavesAndResetClears()
        {
            _step = "C1 Boot";
            yield return BootToLobby();
            var roster = ServiceLocator.Get<Roster>();
            var stars = ServiceLocator.Get<StarService>();
            var starter = roster.GetOwned()[0].Id;
            var startStars = stars.GetStars(starter);
            var cost = stars.GetNextCost(starter);
            Assert.Greater(cost, 0, Where("nhân vật đầu đã tối đa sao, không thử nâng được"));

            _step = "C2 cấp mảnh, mở màn Nhân vật";
            roster.AddFragments(starter, cost);
            yield return ClickWhenReady("NavTabCharacters_PH_320x120");

            _step = "C3 bấm thẻ đầu và nút nâng";
            yield return ClickWhenReady("CharacterCard0");
            Assert.IsTrue(StarUpButton().interactable, Where("đủ mảnh mà nút nâng bị tắt"));
            yield return ClickWhenReady("StarUpButton");
            Assert.AreEqual(startStars + 1, stars.GetStars(starter), Where("sao sau khi nâng"));
            Assert.AreEqual(0, roster.GetFragments(starter), Where("mảnh phải bị trừ đúng giá"));
            Assert.IsFalse(StarUpButton().interactable, Where("hết mảnh mà nút nâng vẫn bấm được"));

            _step = "C4 nạp lại save";
            var loaded = new SaveService(_savePath).Load();
            Assert.IsNotNull(loaded, Where("không đọc lại được save tạm"));
            Assert.IsTrue(loaded.StarUps.Any(e => e.CharacterId == starter && e.Count == 1), Where("StarUps không vào save"));

            _step = "C5 Reset";
            var card = GameObject.Find("CharacterCard0");
            var info = card.transform.Find("InfoText").GetComponent<Text>();
            StringAssert.Contains(new string('★', startStars + 1), info.text, Where("thẻ phải hiện sao mới trước khi Reset"));
            Assert.IsTrue(ServiceLocator.Get<GameResetter>().Reset(), Where("Reset thất bại"));
            Assert.AreEqual(startStars, stars.GetStars(starter), Where("sao sau Reset"));
            Assert.AreEqual(0, roster.GetFragments(starter), Where("mảnh sau Reset"));
            yield return null;
            StringAssert.DoesNotContain(new string('★', startStars + 1), info.text, Where("thẻ vẫn hiện sao cũ sau Reset"));
            Assert.AreEqual("", GameObject.Find("StarUpStatus").GetComponent<Text>().text, Where("dòng trạng thái còn chữ cũ sau Reset"));
        }

        private static Button StarUpButton() => GameObject.Find("StarUpButton").GetComponent<Button>();

        // Nút nâng chỉ bấm được khi đủ mảnh và chưa 5★: đi từ không có mảnh tới 5★ bằng đúng số mảnh mỗi bậc.
        [UnityTest, Timeout(180000)]
        public IEnumerator Characters_StarUpButton_FollowsFragmentsUntilMaxStars()
        {
            _step = "M1 Boot, mở màn Nhân vật";
            yield return BootToLobby();
            var roster = ServiceLocator.Get<Roster>();
            var stars = ServiceLocator.Get<StarService>();
            var starter = roster.GetOwned()[0].Id;
            Assert.AreEqual(0, roster.GetFragments(starter), Where("nhân vật đầu phải chưa có mảnh"));
            yield return ClickWhenReady("NavTabCharacters_PH_320x120");
            yield return ClickWhenReady("CharacterCard0");
            Assert.IsFalse(StarUpButton().interactable, Where("không có mảnh mà nút nâng bấm được"));

            _step = "M2 nâng từng bậc";
            while (stars.GetNextCost(starter) > 0)
            {
                var before = stars.GetStars(starter);
                roster.AddFragments(starter, stars.GetNextCost(starter) - 1);
                yield return null;
                Assert.IsFalse(StarUpButton().interactable, Where($"thiếu 1 mảnh ở {before}★ mà nút nâng bấm được"));
                roster.AddFragments(starter, 1);
                yield return null;
                yield return ClickWhenReady("StarUpButton");
                Assert.AreEqual(before + 1, stars.GetStars(starter), Where($"sao sau khi nâng từ {before}★"));
            }

            _step = "M3 đã tối đa";
            Assert.AreEqual(StarConfig.MaxStars, stars.GetStars(starter), Where("phải tới 5★"));
            Assert.IsFalse(StarUpButton().interactable, Where("đã 5★ mà nút nâng bấm được"));
        }

        // Lưu lỗi thì dòng trạng thái phải báo "chưa lưu", không báo thành công (view không tự viết chữ "Đã lên").
        [UnityTest, Timeout(180000)]
        public IEnumerator Characters_StarUp_PersistFails_ShowsNotSaved()
        {
            _step = "P1 Boot, cấp mảnh";
            yield return BootToLobby();
            var session = Object.FindFirstObjectByType<GameBootstrap>().Session;
            var roster = ServiceLocator.Get<Roster>();
            var stars = ServiceLocator.Get<StarService>();
            var starter = roster.GetOwned()[0].Id;
            var startStars = stars.GetStars(starter);
            roster.AddFragments(starter, stars.GetNextCost(starter));
            yield return ClickWhenReady("NavTabCharacters_PH_320x120");
            yield return ClickWhenReady("CharacterCard0");

            _step = "P2 khoá file save rồi nâng";
            Assert.IsTrue(session.Persist(), Where("ghi save tạm"));
            var locked = new FileStream(_savePath, FileMode.Open, FileAccess.Read, FileShare.None);
            try
            {
                LogAssert.Expect(LogType.Error, new Regex("Save failed"));
                yield return ClickWhenReady("StarUpButton");
            }
            finally
            {
                locked.Dispose();
            }

            Assert.AreEqual(UiText.Get("characters.not_saved"), GameObject.Find("StarUpStatus").GetComponent<Text>().text,
                Where("lưu lỗi mà dòng trạng thái không báo chưa lưu"));
            Assert.AreEqual(startStars + 1, stars.GetStars(starter), Where("lưu lỗi vẫn phải áp dụng trong bộ nhớ"));
        }

        // --- Menu của hub (spec UI hub 5.6, 5.7): Reset, Ngôn ngữ, đường về sau trận.

        // Hub của lần nạp Restaurant mới: HubNav cũ bị huỷ cuối khung nạp, nên chờ tới khi tìm thấy cái khác.
        private IEnumerator WaitForNewHub(HubNav old)
        {
            var deadline = Time.realtimeSinceStartup + SceneTimeout;
            while (true)
            {
                var hub = Object.FindFirstObjectByType<HubNav>();
                if (hub != null && hub != old) yield break;
                if (Time.realtimeSinceStartup > deadline) Assert.Fail(Where($"{SceneTimeout}s chờ Restaurant nạp lại. Nút: {UiDriver.DescribeVisibleButtons()}"));
                yield return null;
            }
        }

        // Mở bản đồ, qua các màn Truyện đứng trước, bấm màn Battle đầu tiên rồi Thách đấu: dừng ở popup Chọn đội.
        private IEnumerator OpenTeamSelectOfFirstBattle(CombatService combat)
        {
            yield return ClickWhenReady("NavTabStages_PH_320x120");
            yield return ClickWhenReady("ModeMain_PH_820x560");
            var stages = combat.Config.Stages.Items;
            var battleIndex = stages.Select((st, i) => (st, i)).First(x => x.st.Kind == StageKind.Battle).i;
            for (var i = 0; i < battleIndex; i++)
            {
                yield return ClickWhenReady("Node" + i);
                yield return ClickWhenReady("ContinueButton");
            }

            yield return ClickWhenReady("Node" + battleIndex);
            yield return ClickWhenReady("ChallengeButton");
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator Hub_Menu_Reset_CancelKeeps_ConfirmClears()
        {
            _step = "R1 Boot";
            yield return BootToLobby();
            var session = Object.FindFirstObjectByType<GameBootstrap>().Session;
            var roster = session.Roster;
            var starter = roster.GetOwned()[0].Id;
            var newGems = session.Wallet.Get(CurrencyType.Gem);
            var newTeam = roster.GetBattleTeam().Select(c => c.Id).ToList();
            var newFragments = roster.GetFragments(starter);

            _step = "R2 chơi: +Gem, +mảnh, đổi đội";
            session.Wallet.Add(CurrencyType.Gem, 1000);
            roster.AddFragments(starter, 7);
            if (roster.GetBattleTeam().Count == 0) roster.AddToTeam(starter);
            else roster.RemoveFromTeam(starter);
            Assert.IsTrue(session.Persist(), Where("ghi save tạm"));
            var played = session.Wallet.Get(CurrencyType.Gem);

            _step = "R3 Gacha → Menu → Reset → Huỷ";
            yield return ClickWhenReady("NavTabGacha_PH_320x120");
            yield return ClickWhenReady("NavTabMenu_PH_320x120");
            yield return ClickWhenReady("MenuResetBtn_PH_700x100");
            Assert.AreEqual(played, session.Wallet.Get(CurrencyType.Gem), Where("mới bấm Reset dữ liệu mà đã reset: bỏ qua bước xác nhận (D-005)"));
            Assert.AreEqual(1, UiDriver.FindButtons("ResetYesBtn_PH_340x110").Count, Where("hộp xác nhận Reset phải hiện"));
            yield return ClickWhenReady("ResetNoBtn_PH_340x110");
            Assert.AreEqual(played, session.Wallet.Get(CurrencyType.Gem), Where("Huỷ mà vẫn reset"));
            Assert.AreEqual(0, UiDriver.FindButtons("ResetYesBtn_PH_340x110").Count, Where("Huỷ phải đóng hộp xác nhận"));
            Assert.IsTrue(File.Exists(_savePath), Where("Huỷ mà file save mất"));
            Assert.AreEqual(1, UiDriver.FindButtons("MenuResetBtn_PH_700x100").Count, Where("Huỷ xong Menu vẫn mở"));

            _step = "R4 Reset → Xoá hết";
            yield return ClickWhenReady("MenuResetBtn_PH_700x100");
            var oldHub = Object.FindFirstObjectByType<HubNav>();
            yield return ClickWhenReady("ResetYesBtn_PH_340x110");
            yield return WaitForNewHub(oldHub);

            _step = "R5 kiểm game mới";
            Assert.AreEqual(newGems, session.Wallet.Get(CurrencyType.Gem), Where("Gem sau Xoá hết"));
            CollectionAssert.AreEqual(newTeam, roster.GetBattleTeam().Select(c => c.Id).ToList(), Where("đội sau Xoá hết"));
            Assert.AreEqual(newFragments, roster.GetFragments(starter), Where("mảnh sau Xoá hết"));
            var hub = Object.FindFirstObjectByType<HubNav>();
            Assert.AreEqual(HubTab.Restaurant, hub.State.Current, Where("sau Xoá hết phải về Lobby"));
            Assert.IsFalse(hub.State.MenuOpen, Where("sau Xoá hết Menu không được mở"));
            foreach (var panel in new[] { "CharactersPanel_PH_1920x1080", "ModeSelectPanel_PH_1920x1080", "StageMapPanel_PH_1920x1080", "GachaPanel_PH_1920x1080", "MenuPanel_PH_1920x1080" })
                Assert.IsNull(GameObject.Find(panel), Where($"{panel} còn bật sau Xoá hết"));
            Assert.IsFalse(HubPending.TryTake(out _, out _), Where("Xoá hết không được để lại ô chờ"));
            var loaded = new SaveService(_savePath).Load();
            Assert.IsNotNull(loaded, Where("sau Reset phải có file save mới"));
            Assert.AreEqual(newGems, new Wallet(loaded).Get(CurrencyType.Gem), Where("Gem trong file save sau Reset"));
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator Hub_Menu_Language_KeepsTabAndPersists()
        {
            _step = "G1 Boot (Việt)";
            yield return BootToLobby();
            var session = Object.FindFirstObjectByType<GameBootstrap>().Session;
            var combat = ServiceLocator.Get<CombatService>();
            // Máy dev có thể là tiếng Anh: ép về tiếng Việt rồi nạp lại để nhãn bắt đầu bằng tiếng Việt.
            Assert.IsTrue(session.SetLanguage(Language.Vi), Where("SetLanguage(Vi)"));
            SceneManager.LoadScene("Restaurant");
            yield return null;
            yield return WaitForScene("Restaurant");

            // Review Focus 2: đội chọn dở bị trả về như lúc mở Chọn đội, cả trong bộ nhớ lẫn trong file save (lưu ngôn ngữ ghi cả save).
            _step = "G2 Màn chơi → Chọn đội (dở)";
            var teamBefore = session.Roster.GetBattleTeam().Select(c => c.Id).ToList();
            yield return OpenTeamSelectOfFirstBattle(combat);
            yield return ClickWhenReady("TeamCard0");
            CollectionAssert.AreNotEqual(teamBefore, session.Roster.GetBattleTeam().Select(c => c.Id).ToList(), Where("bấm TeamCard0 phải đổi đội, nếu không ca này không thử gì"));

            _step = "G3 Menu → Ngôn ngữ";
            yield return ClickWhenReady("NavTabMenu_PH_320x120");
            Assert.AreEqual("Ngôn ngữ: Tiếng Việt", GameObject.Find("MenuLanguageBtn_PH_700x100").GetComponentInChildren<Text>().text, Where("nhãn Ngôn ngữ trước khi đổi"));
            var oldHub = Object.FindFirstObjectByType<HubNav>();
            yield return ClickWhenReady("MenuLanguageBtn_PH_700x100");
            yield return WaitForNewHub(oldHub);
            yield return null; // LocalizedText.OnEnable của scene mới

            _step = "G4 kiểm sau khi nạp lại";
            var hub = Object.FindFirstObjectByType<HubNav>();
            Assert.AreEqual(Language.En, GameLanguage.Current, Where("GameLanguage"));
            Assert.AreEqual(HubTab.Stages, hub.State.Current, Where("đổi ngôn ngữ phải ở lại ô Màn chơi"));
            Assert.IsTrue(hub.State.MenuOpen, Where("đổi ngôn ngữ xong Menu phải còn mở"));
            Assert.AreEqual("Language: English", GameObject.Find("MenuLanguageBtn_PH_700x100").GetComponentInChildren<Text>().text, Where("nhãn Ngôn ngữ"));
            Assert.AreEqual("Restaurant", GameObject.Find("NavTabRestaurant_PH_320x120").GetComponentInChildren<Text>().text, Where("nhãn ô Quán phải theo tiếng Anh"));
            CollectionAssert.AreEqual(teamBefore, session.Roster.GetBattleTeam().Select(c => c.Id).ToList(), Where("đội chọn dở phải về như lúc mở Chọn đội"));
            Assert.AreEqual(0, Object.FindObjectsByType<TeamSelectPopup>(FindObjectsInactive.Include, FindObjectsSortMode.None).Count(p => p.IsOpen), Where("Chọn đội còn mở sau khi nạp lại"));

            _step = "G5 nạp lại save tạm";
            var loaded = new SaveService(_savePath).Load();
            Assert.IsNotNull(loaded, Where("không đọc lại được save tạm"));
            Assert.AreEqual("en", loaded.Language, Where("Language trong save"));
            CollectionAssert.AreEqual(teamBefore, loaded.BattleTeam, Where("đội trong save"));
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator Hub_BattleBack_OpensStageMap()
        {
            _step = "B1 Boot, vào trận màn Battle đầu";
            yield return BootToLobby();
            var session = Object.FindFirstObjectByType<GameBootstrap>().Session;
            var combat = ServiceLocator.Get<CombatService>();
            yield return OpenTeamSelectOfFirstBattle(combat);
            var teamSize = Math.Min(session.Roster.GetOwned().Count, Roster.MaxBattleTeamSize);
            for (var i = 0; i < teamSize; i++) yield return ClickWhenReady("TeamCard" + i);
            yield return ClickWhenReady("StartButton");
            if (BattleFormation.GetEntryStatus(teamSize) == TeamEntryStatus.NeedsConfirm)
                yield return ClickWhenReady("SmallConfirmYesButton");
            yield return WaitForScene(BattleView.BattleScene);

            _step = "B2 Menu tạm dừng → Bỏ cuộc → Quay lại";
            yield return WaitForButtonStep("MenuButton");
            yield return ClickWhenReady("MenuButton");
            yield return ClickWhenReady("SurrenderButton");
            yield return ClickWhenReady("ConfirmYesButton");
            yield return ClickWhenReady("BackButton");

            _step = "B3 về Lobby với bản đồ mở sẵn";
            yield return WaitForScene(BattleView.RestaurantScene);
            var hub = Object.FindFirstObjectByType<HubNav>();
            Assert.IsNotNull(hub, Where("Restaurant nạp mà không có HubNav"));
            Assert.AreEqual(HubTab.Stages, hub.State.Current, Where("về từ trận phải ở ô Màn chơi"));
            Assert.IsFalse(hub.State.MenuOpen, Where("về từ trận không mở Menu"));
            Assert.IsNotNull(GameObject.Find("StageMapPanel_PH_1920x1080"), Where("bản đồ phải bật sẵn"));
        }

        // Đợt C, Chọn chế độ (spec 5.2): ô Màn chơi → Chọn chế độ; ô khoá ra "Sắp có"; Chương chính → Bản đồ → Quay lại; về từ trận mở sẵn Bản đồ.
        [UnityTest, Timeout(180000)]
        public IEnumerator Hub_ModeSelect_MapBackAndBattleReturn()
        {
            const string modePanel = "ModeSelectPanel_PH_1920x1080", mapPanel = "StageMapPanel_PH_1920x1080";
            _step = "M1 Boot, ô Màn chơi → Chọn chế độ";
            yield return BootToLobby();
            yield return ClickWhenReady("NavTabStages_PH_320x120");
            Assert.IsTrue(FindAny(modePanel).activeSelf, Where("ô Màn chơi phải mở Chọn chế độ"));
            Assert.IsFalse(FindAny(mapPanel).activeSelf, Where("ô Màn chơi không được mở thẳng Bản đồ"));

            _step = "M2 bốn ô khoá → Sắp có";
            foreach (var name in new[] { "ModeLockedHard_PH_440x260", "ModeLockedEvent_PH_440x260", "ModeLockedChallenge_PH_440x260", "ModeLockedArena_PH_440x260" })
            {
                FindAny("ComingSoonToast").SetActive(false);
                yield return ClickWhenReady(name);
                Assert.IsTrue(FindAny("ComingSoonToast").activeSelf, Where($"{name} phải hiện dòng Sắp có"));
                Assert.IsFalse(FindAny(mapPanel).activeSelf, Where($"{name} không được mở Bản đồ"));
            }

            _step = "M3 Chương chính → Bản đồ, chỉ Thể lực";
            yield return ClickWhenReady("ModeMain_PH_820x560");
            Assert.IsTrue(FindAny(mapPanel).activeSelf, Where("Chương chính phải mở Bản đồ"));
            yield return null;
            Assert.AreEqual("Stamina", ShownSlots(), Where("Bản đồ chỉ hiện Thể lực"));

            _step = "M4 Quay lại → Chọn chế độ";
            yield return ClickWhenReady("BackButton");
            Assert.IsFalse(FindAny(mapPanel).activeSelf, Where("Quay lại phải đóng Bản đồ"));
            Assert.IsTrue(FindAny(modePanel).activeSelf, Where("Quay lại phải về Chọn chế độ"));

            _step = "M5 Bản đồ → ô Quán → ô Màn chơi: luôn về Chọn chế độ";
            yield return ClickWhenReady("ModeMain_PH_820x560");
            yield return ClickWhenReady("NavTabRestaurant_PH_320x120");
            Assert.IsFalse(FindAny(mapPanel).activeSelf, Where("ô Quán phải đóng Bản đồ"));
            yield return ClickWhenReady("NavTabStages_PH_320x120");
            Assert.IsTrue(FindAny(modePanel).activeSelf);
            Assert.IsFalse(FindAny(mapPanel).activeSelf, Where("ô Màn chơi từ Quán phải về Chọn chế độ, không phải Bản đồ"));
            yield return ClickWhenReady("ModeMain_PH_820x560");
            yield return ClickWhenReady("NavTabStages_PH_320x120");
            Assert.IsFalse(FindAny(mapPanel).activeSelf, Where("bấm ô Màn chơi khi đang ở Bản đồ phải về Chọn chế độ"));

            _step = "M6 về từ trận mở sẵn Bản đồ";
            var oldHub = Object.FindFirstObjectByType<HubNav>();
            HubNav.OpenOnNextLoad(HubTab.Stages, false, stageMap: true);
            SceneManager.LoadScene("Restaurant");
            yield return WaitForNewHub(oldHub);
            yield return null;
            Assert.AreEqual(HubTab.Stages, Object.FindFirstObjectByType<HubNav>().State.Current, Where("ô chờ phải mở ô Màn chơi"));
            Assert.IsTrue(FindAny(mapPanel).activeSelf, Where("ô chờ có stageMap phải mở Bản đồ"));
            Assert.IsTrue(FindAny(modePanel).activeSelf, Where("Chọn chế độ còn bật dưới Bản đồ"));
            yield return ClickWhenReady("BackButton");
            Assert.IsFalse(FindAny(mapPanel).activeSelf, Where("Quay lại sau khi về từ trận"));
        }

        // Đợt C, nút Thông báo (spec 5.3): chấm theo việc, hộp 5 dòng đúng thứ tự, mỗi dòng tới đúng chỗ, qua raycast thật (L-036).
        [UnityTest, Timeout(300000)]
        public IEnumerator Hub_Notify_DotAndRowsGoToRightPlace()
        {
            const string notify = "NotifyBtn_PH_110x100", dot = "NotifyDot_PH_28x28", row = "NotifyRow_PH_600x100", popup = "NotifyPopup";
            _step = "N1 Boot, game mới đủ người: không có việc";
            yield return BootToLobby();
            var session = Object.FindFirstObjectByType<GameBootstrap>().Session;
            var restaurant = ServiceLocator.Get<RestaurantService>();
            var combat = ServiceLocator.Get<CombatService>();
            var stars = ServiceLocator.Get<StarService>();
            var roster = session.Roster;
            var hub = Object.FindFirstObjectByType<HubNav>();
            foreach (var role in new[] { StaffRole.Cook, StaffRole.Server, StaffRole.Cashier })
                if (restaurant.Profile.Workers(role).Count == 0)
                {
                    // Người đã có vai thì TryAssign chuyển vai (vai cũ lại thiếu): chỉ xếp người chưa có vai nào.
                    var free = roster.GetOwned().Where(c => new[] { StaffRole.Cook, StaffRole.Server, StaffRole.Cashier }.All(r => restaurant.Profile.Workers(r).All(w => w.CharacterId != c.Id)));
                    Assert.IsTrue(free.Any(c => restaurant.Staffing.TryAssign(c.Id, role) == StaffAssignResult.Success), Where($"không xếp được ai vào vai {role}"));
                }
            // Game mới có thể lực đầy nên dòng "Thể lực đầy" hiện sẵn (đúng luật spec); tiêu hao 1 để bắt đầu từ trạng thái không có việc.
            Assert.AreEqual(StaminaSpendStatus.Spent, combat.Stamina.TrySpend(1, () => true), Where("tiêu hao 1 thể lực"));
            session.Save.Afk.PendingTheoretical = 0;
            yield return new WaitForSecondsRealtime(0.7f);
            Assert.IsFalse(FindAny(dot).activeSelf, Where($"không có việc mà chấm vẫn bật (sự cố {restaurant.Incidents.Active.HasValue}, thiếu {restaurant.Estimate.MissingRole}, AFK {restaurant.Afk.PreviewCollect()}, " +
                                                          $"thể lực {combat.Stamina.Current}/{combat.Stamina.Max}, đủ mảnh {CharacterListRows.Build(roster, stars).Count(r => r.NextCost > 0 && r.Fragments >= r.NextCost)})"));

            yield return ClickWhenReady(notify);
            Assert.IsTrue(FindAny(popup).activeSelf, Where("bấm nút mà hộp không mở"));
            Assert.IsTrue(FindAny("NotifyEmpty").activeSelf, Where("không có việc phải ghi dòng trống"));
            Assert.IsTrue(FindAny("LobbyHud").activeSelf, Where("hộp nổi không được đổi bố cục Lobby"));
            yield return ClickWhenReady("NotifyDim");
            Assert.IsFalse(FindAny(popup).activeSelf, Where("bấm nền mờ phải đóng hộp"));

            _step = "N2 ép đủ 5 việc";
            var config = restaurant.Config;
            var min = typeof(RestaurantConfig).GetField("_incidentSpawnMin", BindingFlags.NonPublic | BindingFlags.Instance);
            var max = typeof(RestaurantConfig).GetField("_incidentSpawnMax", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(min, Where("RestaurantConfig đổi tên _incidentSpawnMin"));
            Assert.IsNotNull(max, Where("RestaurantConfig đổi tên _incidentSpawnMax"));
            var oldMin = min.GetValue(config);
            var oldMax = max.GetValue(config);
            min.SetValue(config, 1f);
            max.SetValue(config, 2f);
            try
            {
                restaurant.Incidents.Reset();
                var deadline = Time.realtimeSinceStartup + 20f;
                while (restaurant.Incidents.Active == null)
                {
                    if (Time.realtimeSinceStartup > deadline) Assert.Fail(Where("20s mà không có sự cố nào sinh"));
                    yield return null;
                }
            }
            finally
            {
                min.SetValue(config, oldMin);
                max.SetValue(config, oldMax);
            }

            foreach (var worker in restaurant.Profile.Workers(StaffRole.Cashier).ToList()) restaurant.Staffing.Remove(worker.CharacterId);
            session.Save.Afk.PendingTheoretical = 1000;
            session.Save.Afk.AccumulatedSeconds = 3600;
            combat.Stamina.Refill();
            var ready = CharacterListRows.Build(roster, stars).First(r => r.NextCost > 0);
            roster.AddFragments(ready.Character.Id, Math.Max(0L, ready.NextCost - ready.Fragments));
            var starUpId = CharacterListRows.Build(roster, stars).First(r => r.NextCost > 0 && r.Fragments >= r.NextCost).Character.Id;
            yield return new WaitForSecondsRealtime(0.7f);
            Assert.IsTrue(FindAny(dot).activeSelf, Where("có đủ việc mà chấm không bật"));

            _step = "N3 hộp có 5 dòng đúng thứ tự";
            yield return ClickWhenReady(notify);
            var rows = UiDriver.FindButtons(row).OrderBy(b => b.transform.GetSiblingIndex()).ToList();
            Assert.AreEqual(5, rows.Count, Where("hộp phải có đủ 5 dòng"));
            Assert.IsFalse(FindAny("NotifyEmpty").activeSelf, Where("có việc mà dòng trống vẫn bật"));
            var texts = rows.Select(b => b.GetComponentInChildren<Text>().text).ToList();
            Assert.AreEqual(UiText.Get("notify.incident"), texts[0], Where("dòng 1"));
            Assert.AreEqual(UiText.Format("notify.staff_missing", StaffPanelRows.RoleName(StaffRole.Cashier)), texts[1], Where("dòng 2"));
            StringAssert.Contains(restaurant.Afk.PreviewCollect().ToString(), texts[2], Where("dòng AFK phải ghi số Coin"));
            Assert.AreEqual(UiText.Get("notify.stamina_full"), texts[3], Where("dòng 4"));
            StringAssert.Contains(UiText.Format("notify.star_up", CharacterListRows.Build(roster, stars).Count(r => r.NextCost > 0 && r.Fragments >= r.NextCost)), texts[4], Where("dòng 5"));
            yield return ClickWhenReady("NotifyDim");

            // Mỗi lần đứng ở ô Gacha rồi mới bấm: đích phải tự kéo về ô của nó.
            for (var i = 0; i < 5; i++)
            {
                _step = $"N4.{i} bấm dòng {i + 1}";
                yield return ClickWhenReady("NavTabGacha_PH_320x120");
                yield return ClickWhenReady(notify);
                var current = UiDriver.FindButtons(row).OrderBy(b => b.transform.GetSiblingIndex()).ToList();
                Assert.AreEqual(5, current.Count, Where($"mở lại hộp lần {i + 1}"));
                var result = UiDriver.Click(current[i]);
                Assert.AreEqual(ClickStatus.Clicked, result.Status, Where(result.ToString()));
                yield return null;
                yield return null;
                Assert.IsFalse(FindAny(popup).activeSelf, Where($"dòng {i + 1} phải đóng hộp"));
                switch (i)
                {
                    case 0:
                        Assert.AreEqual(HubTab.Restaurant, hub.State.Current, Where("Sự cố phải về ô Quán"));
                        var onScreen = RectTransformUtility.WorldToScreenPoint(null, FindAny("IncidentBubble").transform.position);
                        Assert.IsTrue(new Rect(0, 0, Screen.width, Screen.height).Contains(onScreen), Where($"'!' ở {onScreen} ngoài khung màn {Screen.width}x{Screen.height}"));
                        break;
                    case 1:
                        Assert.AreEqual(HubTab.Restaurant, hub.State.Current);
                        Assert.IsTrue(FindAny("StaffPanelRoot").activeSelf, Where("Thiếu người phải mở trang Nhân viên"));
                        break;
                    case 2:
                        Assert.AreEqual(HubTab.Restaurant, hub.State.Current);
                        Assert.IsTrue(FindAny("AfkPopupRoot").activeSelf, Where("Coin AFK phải mở popup thu AFK"));
                        yield return ClickWhenReady("LaterButton");
                        break;
                    case 3:
                        Assert.AreEqual(HubTab.Stages, hub.State.Current, Where("Thể lực đầy phải tới ô Màn chơi"));
                        Assert.IsTrue(FindAny("ModeSelectPanel_PH_1920x1080").activeSelf, Where("Thể lực đầy phải mở Chọn chế độ"));
                        Assert.IsFalse(FindAny("StageMapPanel_PH_1920x1080").activeSelf, Where("Thể lực đầy không được mở thẳng Bản đồ"));
                        break;
                    case 4:
                        Assert.AreEqual(HubTab.Characters, hub.State.Current, Where("Đủ mảnh phải tới ô Nhân vật"));
                        var selected = typeof(CharactersView).GetField("_selectedId", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Object.FindFirstObjectByType<CharactersView>());
                        Assert.AreEqual(starUpId, selected, Where("Nhân vật phải chọn sẵn người đủ mảnh đầu tiên"));
                        break;
                }
            }

            _step = "N5 Menu mở: nút vẫn bấm được, bấm dòng thì Menu đóng";
            yield return ClickWhenReady("NavTabMenu_PH_320x120");
            Assert.IsTrue(FindAny("MenuPanel_PH_1920x1080").activeSelf);
            yield return ClickWhenReady(notify);
            Assert.IsTrue(FindAny(popup).activeSelf, Where("hộp phải mở được trên Menu"));
            var menuRows = UiDriver.FindButtons(row).OrderBy(b => b.transform.GetSiblingIndex()).ToList();
            var menuResult = UiDriver.Click(menuRows[3]);
            Assert.AreEqual(ClickStatus.Clicked, menuResult.Status, Where(menuResult.ToString()));
            yield return null;
            Assert.IsFalse(FindAny("MenuPanel_PH_1920x1080").activeSelf, Where("bấm dòng phải đóng Menu"));
            Assert.AreEqual(HubTab.Stages, hub.State.Current);

            _step = "N6 việc hết ngay trước khi bấm dòng (Review Focus 2)";
            yield return ClickWhenReady("NavTabRestaurant_PH_320x120");
            yield return ClickWhenReady(notify);
            var stale = UiDriver.FindButtons(row).OrderBy(b => b.transform.GetSiblingIndex()).ToList();
            Assert.IsTrue(restaurant.Incidents.Active.HasValue, Where("sự cố phải còn để thử huỷ"));
            restaurant.Incidents.Cancel();
            var staleResult = UiDriver.Click(stale[0]);
            Assert.AreEqual(ClickStatus.Clicked, staleResult.Status, Where(staleResult.ToString()));
            yield return null;
            Assert.AreEqual(HubTab.Restaurant, hub.State.Current, Where("sự cố đã huỷ vẫn phải về ô Quán"));
            Assert.IsFalse(FindAny(popup).activeSelf, Where("hộp phải đóng"));

            _step = "N7 Gacha quay: nút ẩn cùng thanh";
            session.Wallet.Add(CurrencyType.Gem, 5000);
            yield return ClickWhenReady("NavTabGacha_PH_320x120");
            Assert.IsTrue(FindAny(notify).activeInHierarchy, Where("trang roll phải có nút Thông báo"));
            yield return ClickWhenReady("TenButton");
            yield return null;
            Assert.IsFalse(FindAny(notify).activeInHierarchy, Where("đang lộ nhân vật mà nút Thông báo còn hiện"));
            typeof(GachaView).GetMethod("Dismiss", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(Object.FindFirstObjectByType<GachaView>(), null);
            yield return null;
            Assert.IsTrue(FindAny(notify).activeInHierarchy, Where("về trang roll mà nút vẫn ẩn"));
        }

        // --- Vòng ô của hub, thanh Gacha, Reset lỗi trong Menu, ảnh chụp cho playtest (spec UI hub 7).

        private static readonly string[] HubPanels = { "CharactersPanel_PH_1920x1080", "ModeSelectPanel_PH_1920x1080", "GachaPanel_PH_1920x1080" };

        // GameObject.Find bỏ qua object đang tắt, mà ở đây cần đọc cả trạng thái tắt.
        private static GameObject FindAny(string name) =>
            Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(t => t.name == name).gameObject;

        private void AssertOnlyPanelOpen(string open)
        {
            foreach (var panel in HubPanels)
                Assert.AreEqual(panel == open, FindAny(panel).activeSelf, Where($"{panel} phải {(panel == open ? "bật" : "tắt")}"));
        }

        [UnityTest, Timeout(240000)]
        public IEnumerator Hub_TabLoop_OpensEachPanel()
        {
            _step = "T1 Boot";
            yield return BootToLobby();
            var restaurant = ServiceLocator.Get<RestaurantService>();
            var hub = Object.FindFirstObjectByType<HubNav>();
            AssertOnlyPanelOpen(null);
            Assert.IsTrue(restaurant.IsPresent, Where("vào Lobby mà quán không có mặt"));

            _step = "T2 Nhân vật, Màn chơi, Gacha";
            foreach (var (tab, panel, expected) in new[]
                     {
                         ("NavTabCharacters_PH_320x120", HubPanels[0], HubTab.Characters),
                         ("NavTabStages_PH_320x120", HubPanels[1], HubTab.Stages),
                         ("NavTabGacha_PH_320x120", HubPanels[2], HubTab.Gacha),
                     })
            {
                yield return ClickWhenReady(tab);
                AssertOnlyPanelOpen(panel);
                Assert.AreEqual(expected, hub.State.Current, Where($"{tab} không đổi ô hiện tại"));
                Assert.IsTrue(restaurant.IsPresent, Where($"mở {panel} mà quán rời đi: sự cố và chuyên cần sẽ dừng"));
            }

            _step = "T3 Cốt truyện (khoá) → Sắp có, không đổi ô";
            yield return ClickWhenReady("NavTabStory_PH_320x120");
            Assert.IsTrue(FindAny("ComingSoonToast").activeSelf, Where("ô Cốt truyện phải hiện dòng Sắp có"));
            Assert.AreEqual(HubTab.Gacha, hub.State.Current, Where("ô Cốt truyện không được đổi ô hiện tại"));
            AssertOnlyPanelOpen(HubPanels[2]);

            _step = "T4 Menu mở rồi Quán";
            yield return ClickWhenReady("NavTabMenu_PH_320x120");
            Assert.IsTrue(FindAny("MenuPanel_PH_1920x1080").activeSelf, Where("ô Menu phải mở Menu"));
            yield return ClickWhenReady("NavTabRestaurant_PH_320x120");
            AssertOnlyPanelOpen(null);
            Assert.IsFalse(FindAny("MenuPanel_PH_1920x1080").activeSelf, Where("bấm ô Quán phải đóng Menu"));
            Assert.AreEqual(HubTab.Restaurant, hub.State.Current);
            Assert.IsTrue(restaurant.IsPresent);

            // Review Focus 1: trang của quán đang mở mà bấm ô khác thì trang phải đóng qua IHubPage.Close() và trả SetSpawnBlocked,
            // nếu không sự cố không bao giờ sinh lại.
            _step = "T5 Nhân viên → bấm Gacha";
            yield return OpenRestaurantBar();
            yield return ClickWhenReady("StaffButton");
            Assert.IsTrue(FindAny("StaffPanelRoot").activeSelf, Where("StaffButton không mở trang Nhân viên"));
            yield return ClickWhenReady("NavTabGacha_PH_320x120");
            Assert.IsFalse(FindAny("StaffPanelRoot").activeSelf, Where("bấm ô khác mà trang Nhân viên còn mở"));
            yield return ClickWhenReady("NavTabRestaurant_PH_320x120");

            // Giờ thật như các ca chờ khác, nhưng rút bộ đếm sinh xuống 1–2 giây (mặc định 60–120) rồi trả lại; Reset xoá bộ đếm đã đặt.
            _step = "T6 sự cố vẫn sinh sau khi đóng trang";
            var config = restaurant.Config;
            var min = typeof(RestaurantConfig).GetField("_incidentSpawnMin", BindingFlags.NonPublic | BindingFlags.Instance);
            var max = typeof(RestaurantConfig).GetField("_incidentSpawnMax", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(min, Where("RestaurantConfig đổi tên _incidentSpawnMin"));
            Assert.IsNotNull(max, Where("RestaurantConfig đổi tên _incidentSpawnMax"));
            var oldMin = min.GetValue(config);
            var oldMax = max.GetValue(config);
            min.SetValue(config, 1f);
            max.SetValue(config, 2f);
            try
            {
                restaurant.Incidents.Reset();
                var deadline = Time.realtimeSinceStartup + 20f;
                while (restaurant.Incidents.Active == null)
                {
                    if (Time.realtimeSinceStartup > deadline) Assert.Fail(Where("20s sau khi đóng trang mà không có sự cố nào sinh: SetSpawnBlocked còn kẹt"));
                    yield return null;
                }
            }
            finally
            {
                min.SetValue(config, oldMin);
                max.SetValue(config, oldMax);
            }
        }

        private static readonly string[] ResourceSlotNames = { "Stamina", "Coin", "Material", "Gem" };

        // Các ô tài nguyên đang hiện (tính cả cha bị tắt), ví dụ "Stamina,Coin". Rỗng khi cả thanh tắt.
        private static string ShownSlots() =>
            string.Join(",", ResourceSlotNames.Where(n => FindAny($"Res{n}_PH_280x76").activeInHierarchy));

        // Nút trùng tên (ResPlus_PH_56x56 có 3 cái) thì UiDriver báo Ambiguous: bấm thẳng bằng sự kiện click.
        private static void ClickDirect(Button button) =>
            ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);

        // Thanh quán mặc định đóng (spec UI hub B 5.5): mở nó trước khi bấm Nhân viên / Nâng cấp.
        private IEnumerator OpenRestaurantBar()
        {
            if (!FindAny("RestaurantBar_PH_180x460").activeSelf) yield return ClickWhenReady("RestaurantBarToggle_PH_60x160");
        }

        // Spec UI hub B mục 7: thanh tài nguyên đổi theo ô và theo trang đang mở; avatar, Shop, nút + báo Sắp có; banner mở Gacha.
        [UnityTest, Timeout(240000)]
        public IEnumerator Hub_ResourceBar_FollowsTabsAndPages()
        {
            _step = "B1 Boot, Lobby";
            yield return BootToLobby();
            var hub = Object.FindFirstObjectByType<HubNav>();
            var session = Object.FindFirstObjectByType<GameBootstrap>().Session;
            session.Wallet.Add(CurrencyType.Gem, 5000);
            var bar = (RectTransform)FindAny("ResourceBar_PH_1120x76").transform;
            yield return null;
            Assert.AreEqual("Stamina,Coin,Material,Gem", ShownSlots(), Where("Lobby phải hiện đủ 4 ô"));
            Assert.AreEqual(0f, bar.anchorMin.x, Where("Lobby: thanh neo trái, cạnh avatar"));
            Assert.IsTrue(FindAny("LobbyHud").activeSelf, Where("Lobby phải bật LobbyHud"));
            Assert.AreEqual($"Gem {session.Wallet.Get(CurrencyType.Gem)}", FindAny("ResGem_PH_280x76").GetComponentInChildren<Text>().text, Where("ô Gem phải ghi số trong ví"));

            _step = "B2 Nhân vật → Coin";
            yield return ClickWhenReady("NavTabCharacters_PH_320x120");
            yield return null;
            Assert.AreEqual("Coin", ShownSlots(), Where("Nhân vật chỉ hiện Coin"));
            Assert.AreEqual(1f, bar.anchorMin.x, Where("ngoài Lobby thanh neo trên-phải"));
            Assert.IsFalse(FindAny("LobbyHud").activeSelf, Where("rời Lobby phải tắt LobbyHud"));

            _step = "B3 Màn chơi → Thể lực, Popup màn và Chọn đội vẫn Thể lực";
            yield return ClickWhenReady("NavTabStages_PH_320x120");
            yield return null;
            Assert.AreEqual("Stamina", ShownSlots(), Where("Chọn chế độ chỉ hiện Thể lực"));
            yield return ClickWhenReady("ModeMain_PH_820x560");
            yield return null;
            Assert.AreEqual("Stamina", ShownSlots(), Where("Bản đồ chỉ hiện Thể lực"));
            var stages = ServiceLocator.Get<CombatService>().Config.Stages.Items;
            var battleIndex = stages.Select((st, i) => (st, i)).First(x => x.st.Kind == StageKind.Battle).i;
            for (var i = 0; i < battleIndex; i++)
            {
                yield return ClickWhenReady("Node" + i);
                yield return ClickWhenReady("ContinueButton");
            }

            yield return ClickWhenReady("Node" + battleIndex);
            yield return null;
            Assert.AreEqual("Stamina", ShownSlots(), Where("mở Popup màn vẫn hiện Thể lực"));
            yield return ClickWhenReady("ChallengeButton");
            yield return null;
            Assert.AreEqual("Stamina", ShownSlots(), Where("Chọn đội vẫn hiện Thể lực"));

            _step = "B4 Gacha → Gem, quay → thanh ẩn cùng thanh dưới, về trang roll → hiện lại";
            yield return ClickWhenReady("NavTabGacha_PH_320x120");
            yield return null;
            Assert.AreEqual("Gem", ShownSlots(), Where("trang roll chỉ hiện Gem"));
            yield return ClickWhenReady("TenButton");
            yield return null;
            Assert.IsFalse(FindAny("NavBar_PH_1920x120").activeSelf, Where("đang lộ mà thanh dưới còn hiện"));
            Assert.IsFalse(FindAny("ResourceBar_PH_1120x76").activeInHierarchy, Where("đang lộ mà thanh tài nguyên còn hiện"));
            typeof(GachaView).GetMethod("Dismiss", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(Object.FindFirstObjectByType<GachaView>(), null);
            yield return null;
            yield return null;
            Assert.AreEqual("Gem", ShownSlots(), Where("về trang roll thanh tài nguyên phải hiện lại"));

            _step = "B5 Quán → Nâng cấp chỉ Material, Nhân viên không hiện";
            yield return ClickWhenReady("NavTabRestaurant_PH_320x120");
            yield return null;
            Assert.AreEqual("Stamina,Coin,Material,Gem", ShownSlots(), Where("về Quán phải hiện lại đủ 4 ô"));
            Assert.IsTrue(FindAny("LobbyHud").activeSelf, Where("về Quán phải bật lại LobbyHud"));
            yield return OpenRestaurantBar();
            yield return ClickWhenReady("UpgradeButton");
            yield return null;
            Assert.AreEqual("Material", ShownSlots(), Where("trang Nâng cấp chỉ hiện Material"));
            Assert.IsFalse(FindAny("LobbyHud").activeSelf, Where("mở trang của quán phải tắt LobbyHud"));
            yield return ClickWhenReady("NavTabRestaurant_PH_320x120");
            yield return null;
            Assert.IsTrue(FindAny("LobbyHud").activeSelf, Where("đóng trang phải bật lại LobbyHud"));
            yield return ClickWhenReady("StaffButton");
            yield return null;
            Assert.AreEqual("", ShownSlots(), Where("trang Nhân viên không hiện ô nào"));
            yield return ClickWhenReady("NavTabRestaurant_PH_320x120");

            _step = "B6 Menu → thanh tắt";
            yield return ClickWhenReady("NavTabMenu_PH_320x120");
            yield return null;
            Assert.AreEqual("", ShownSlots(), Where("Menu không hiện ô nào"));
            yield return ClickWhenReady("NavTabRestaurant_PH_320x120");
            yield return null;

            _step = "B7 avatar, Shop, nút + → Sắp có";
            var toast = FindAny("ComingSoonToast");
            foreach (var name in new[] { "Avatar_PH_140x140", "HubRoundBtnShop_PH_110x110" })
            {
                toast.SetActive(false);
                yield return ClickWhenReady(name);
                Assert.IsTrue(toast.activeSelf, Where($"{name} phải hiện Sắp có"));
            }

            var plus = bar.GetComponentsInChildren<Button>(false).Where(b => b.name == "ResPlus_PH_56x56").ToArray();
            Assert.AreEqual(3, plus.Length, Where("Lobby có 3 nút + (Thể lực, Coin, Gem)"));
            foreach (var button in plus)
            {
                toast.SetActive(false);
                ClickDirect(button);
                Assert.IsTrue(toast.activeSelf, Where($"nút + trong {button.transform.parent.name} phải hiện Sắp có"));
            }

            _step = "B8 banner → ô Gacha";
            yield return ClickWhenReady("BannerGacha_PH_360x200");
            Assert.AreEqual(HubTab.Gacha, hub.State.Current, Where("banner phải mở ô Gacha"));
        }

        // Spec UI hub B 5.5: thanh quán mặc định đóng, nhớ qua nạp lại và qua save, Reset về đóng; chấm báo Coin AFK chờ khi thanh đóng.
        [UnityTest, Timeout(180000)]
        public IEnumerator Hub_RestaurantBar_RemembersAcrossReload()
        {
            _step = "K1 Boot, game mới: thanh đóng";
            yield return BootToLobby();
            var session = Object.FindFirstObjectByType<GameBootstrap>().Session;
            Assert.IsFalse(FindAny("RestaurantBar_PH_180x460").activeSelf, Where("game mới phải đóng thanh quán"));
            Assert.IsFalse(session.Save.RestaurantBarOpen, Where("game mới: cờ trong save phải false"));

            _step = "K2 bấm toggle → mở";
            yield return ClickWhenReady("RestaurantBarToggle_PH_60x160");
            Assert.IsTrue(FindAny("RestaurantBar_PH_180x460").activeSelf, Where("bấm toggle phải mở thanh"));
            Assert.IsTrue(session.Save.RestaurantBarOpen, Where("mở thanh phải ghi cờ vào save"));

            _step = "K3 nạp lại Restaurant và nạp lại save → vẫn mở";
            var oldHub = Object.FindFirstObjectByType<HubNav>();
            SceneManager.LoadScene("Restaurant");
            yield return WaitForNewHub(oldHub);
            yield return null;
            Assert.IsTrue(FindAny("RestaurantBar_PH_180x460").activeSelf, Where("nạp lại scene thanh phải vẫn mở"));
            Assert.IsTrue(new SaveService(_savePath).Load().RestaurantBarOpen, Where("file save phải giữ cờ mở"));

            _step = "K4 chấm: thanh đóng + Coin AFK chờ → bật; mở thanh → tắt";
            session.Save.Afk.PendingTheoretical = 1000;
            yield return ClickWhenReady("RestaurantBarToggle_PH_60x160");
            yield return null;
            Assert.IsFalse(FindAny("RestaurantBar_PH_180x460").activeSelf, Where("bấm toggle lần hai phải đóng thanh"));
            Assert.IsTrue(FindAny("RestaurantBarDot_PH_24x24").activeSelf, Where("thanh đóng mà có Coin AFK chờ thì chấm phải bật"));
            yield return ClickWhenReady("RestaurantBarToggle_PH_60x160");
            yield return null;
            Assert.IsFalse(FindAny("RestaurantBarDot_PH_24x24").activeSelf, Where("thanh mở thì chấm phải tắt"));

            _step = "K5 Menu → Reset → Xoá hết → thanh đóng";
            yield return ClickWhenReady("NavTabMenu_PH_320x120");
            yield return ClickWhenReady("MenuResetBtn_PH_700x100");
            oldHub = Object.FindFirstObjectByType<HubNav>();
            yield return ClickWhenReady("ResetYesBtn_PH_340x110");
            yield return WaitForNewHub(oldHub);
            yield return null;
            Assert.IsFalse(FindAny("RestaurantBar_PH_180x460").activeSelf, Where("sau Xoá hết thanh quán phải đóng"));
            Assert.IsFalse(session.Save.RestaurantBarOpen, Where("sau Xoá hết cờ phải false"));
            Assert.IsFalse(new SaveService(_savePath).Load().RestaurantBarOpen, Where("file save sau Reset phải false"));
        }

        // Mô phỏng module nhập như PointerInputModule (L-036): raycast thật tại điểm nhấn, pointerDown lên đối tượng trúng, khi kéo mà đối tượng nhấn
        // không phải đối tượng kéo thì pointerUp và huỷ click, rồi thả. `under` là thứ người chơi định chạm: raycast không trúng nó nghĩa là nó bị che.
        private static IEnumerator Swipe(Vector2 from, Vector2 delta, Transform under)
        {
            var es = EventSystem.current;
            var e = new PointerEventData(es) { button = PointerEventData.InputButton.Left, position = from, pressPosition = from };
            var hits = new List<RaycastResult>();
            // Canvas dựng mesh ở khung sau khi scene nạp: đợi tối đa 60 khung cho tới khi raycast trúng.
            for (var wait = 0; wait < 60; wait++)
            {
                es.RaycastAll(e, hits);
                if (hits.Count > 0) break;
                yield return null;
            }

            Assert.IsNotEmpty(hits, $"swipe starts on nothing at {from} (screen {Screen.width}x{Screen.height})");
            var over = hits[0].gameObject;
            Assert.IsTrue(over.transform.IsChildOf(under), $"swipe at {from} (screen {Screen.width}x{Screen.height}) lands on {over.name}, not {under.name}: covered");
            e.pointerCurrentRaycast = e.pointerPressRaycast = hits[0];
            e.rawPointerPress = over;
            e.pointerPress = ExecuteEvents.ExecuteHierarchy(over, e, ExecuteEvents.pointerDownHandler) ?? ExecuteEvents.GetEventHandler<IPointerClickHandler>(over);
            e.eligibleForClick = true;
            e.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(over);
            Assert.IsNotNull(e.pointerDrag, $"{over.name} has no drag handler up its parents");
            ExecuteEvents.Execute(e.pointerDrag, e, ExecuteEvents.initializePotentialDrag);
            ExecuteEvents.Execute(e.pointerDrag, e, ExecuteEvents.beginDragHandler);
            e.dragging = true;
            const int steps = 10;
            for (var i = 1; i <= steps; i++)
            {
                var next = from + delta * (i / (float)steps);
                e.delta = next - e.position;
                e.position = next;
                if (e.pointerPress != e.pointerDrag)
                {
                    if (e.pointerPress != null) ExecuteEvents.Execute(e.pointerPress, e, ExecuteEvents.pointerUpHandler);
                    e.eligibleForClick = false;
                    e.pointerPress = null;
                    e.rawPointerPress = null;
                }

                ExecuteEvents.Execute(e.pointerDrag, e, ExecuteEvents.dragHandler);
                yield return null;
            }

            if (e.pointerPress != null) ExecuteEvents.Execute(e.pointerPress, e, ExecuteEvents.pointerUpHandler);
            if (e.eligibleForClick && e.pointerPress != null && e.pointerPress == ExecuteEvents.GetEventHandler<IPointerClickHandler>(over))
                ExecuteEvents.Execute(e.pointerPress, e, ExecuteEvents.pointerClickHandler);
            ExecuteEvents.Execute(e.pointerDrag, e, ExecuteEvents.endDragHandler);
        }

        // Spec UI hub B 5.2, Review Focus 1: vuốt dời quán và kẹp ở mép; kéo bắt đầu ngay trên "!" vẫn dời quán mà không xử lý sự cố; bấm không kéo thì xử lý.
        [UnityTest, Timeout(180000)]
        public IEnumerator Hub_WorldDrag_PansAndClamps_IncidentStillClicks()
        {
            _step = "D1 Boot, Lobby, kéo 300 px sang trái";
            yield return BootToLobby();
            var restaurant = ServiceLocator.Get<RestaurantService>();
            var scroll = FindAny("WorldScroll").GetComponent<ScrollRect>();
            var world = scroll.content;
            Assert.AreEqual(0f, world.anchoredPosition.x, 0.5f, Where("mở Lobby phải ở giữa"));
            var centre = RectTransformUtility.WorldToScreenPoint(null, scroll.transform.position);
            yield return Swipe(centre, new Vector2(-300f, 0f), scroll.transform);
            yield return null;
            Assert.Less(world.anchoredPosition.x, -1f, Where("vuốt sang trái mà quán không dời"));

            _step = "D2 kéo 5000 px: dừng ở mép";
            yield return Swipe(centre, new Vector2(-5000f, 0f), scroll.transform);
            yield return null;
            yield return null;
            Assert.AreEqual(1f, scroll.horizontalNormalizedPosition, 0.01f, Where("kéo quá mép phải dừng ở mép"));
            yield return Swipe(centre, new Vector2(5000f, 5000f), scroll.transform);
            yield return null;
            yield return null;
            Assert.AreEqual(0f, scroll.horizontalNormalizedPosition, 0.01f, Where("kéo quá mép trái"));
            Assert.AreEqual(0f, scroll.verticalNormalizedPosition, 0.01f, Where("vuốt lên quá mép thì dừng ở mép dưới"));
            scroll.StopMovement();
            world.anchoredPosition = Vector2.zero;
            yield return null;

            _step = "D3 ép một sự cố, có người xử lý";
            var config = restaurant.Config;
            var min = typeof(RestaurantConfig).GetField("_incidentSpawnMin", BindingFlags.NonPublic | BindingFlags.Instance);
            var max = typeof(RestaurantConfig).GetField("_incidentSpawnMax", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(min, Where("RestaurantConfig đổi tên _incidentSpawnMin"));
            Assert.IsNotNull(max, Where("RestaurantConfig đổi tên _incidentSpawnMax"));
            var oldMin = min.GetValue(config);
            var oldMax = max.GetValue(config);
            min.SetValue(config, 1f);
            max.SetValue(config, 2f);
            try
            {
                // Vai Thu ngân: "!" của nó đứng gần HUD trái nhất, chỗ banner từng che (N1). Sinh lại tới khi trúng vai này (1/3 mỗi lần).
                for (var attempt = 0; attempt < 30 && !(restaurant.Incidents.Active.HasValue && restaurant.Incidents.Active.Value.Role == StaffRole.Cashier); attempt++)
                {
                    restaurant.Incidents.Reset();
                    var deadline = Time.realtimeSinceStartup + 20f;
                    while (restaurant.Incidents.Active == null)
                    {
                        if (Time.realtimeSinceStartup > deadline) Assert.Fail(Where("20s mà không có sự cố nào sinh"));
                        yield return null;
                    }
                }

                Assert.IsTrue(restaurant.Incidents.Active.HasValue && restaurant.Incidents.Active.Value.Role == StaffRole.Cashier, Where("30 lần sinh mà không có sự cố Thu ngân"));
                var role = restaurant.Incidents.Active.Value.Role;
                if (!restaurant.Incidents.Active.Value.CanResolve)
                {
                    var assigned = ServiceLocator.Get<Roster>().GetOwned().Any(c => restaurant.Staffing.TryAssign(c.Id, role) == StaffAssignResult.Success);
                    Assert.IsTrue(assigned, Where($"không xếp được ai vào vai {role} để xử lý sự cố"));
                }

                yield return null;
                yield return null;
                var bubble = FindAny("IncidentBubble");
                Assert.IsTrue(bubble.activeInHierarchy, Where("sự cố có mà bong bóng không hiện"));

                _step = "D4 kéo bắt đầu trên '!': quán dời, sự cố còn";
                var onBubble = RectTransformUtility.WorldToScreenPoint(null, bubble.transform.position);
                var before = world.anchoredPosition.x;
                yield return Swipe(onBubble, new Vector2(-300f, 0f), bubble.transform);
                yield return null;
                Assert.Less(world.anchoredPosition.x, before - 1f, Where("kéo từ '!' mà quán không dời"));
                Assert.IsTrue(restaurant.Incidents.Active.HasValue, Where("kéo từ '!' đã xử lý sự cố (đáng lẽ chỉ dời quán)"));

                _step = "D5 bấm '!' không kéo: sự cố được xử lý";
                scroll.StopMovement();
                world.anchoredPosition = Vector2.zero;
                yield return null;
                yield return null;
                // Qua raycast tại tâm nút (màn test 640×480): HUD phủ "!" thì UiDriver báo Blocked và test đỏ (L-036).
                yield return ClickWhenReady("IncidentBubble");
                Assert.IsFalse(restaurant.Incidents.Active.HasValue, Where("bấm '!' mà sự cố không được xử lý"));
            }
            finally
            {
                min.SetValue(config, oldMin);
                max.SetValue(config, oldMax);
            }
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator Hub_GachaPull_HidesBar_UntilBackOnRollPage()
        {
            _step = "H1 Boot, Gacha";
            yield return BootToLobby();
            var session = Object.FindFirstObjectByType<GameBootstrap>().Session;
            session.Wallet.Add(CurrencyType.Gem, 5000);
            yield return ClickWhenReady("NavTabGacha_PH_320x120");
            Assert.IsTrue(FindAny("NavBar_PH_1920x120").activeSelf, Where("thanh phải hiện ở trang roll"));

            _step = "H2 Quay 10 → thanh ẩn";
            yield return ClickWhenReady("TenButton");
            yield return null;
            Assert.IsFalse(FindAny("NavBar_PH_1920x120").activeSelf, Where("đang lộ nhân vật mà thanh còn hiện"));

            _step = "H3 về trang roll → thanh hiện lại";
            // Đường tới đây (cutscene, chạm từng thẻ, trang tổng) cần chuột thật; gọi thẳng hàm Thôi của GachaView, nơi trả lượt quay.
            typeof(GachaView).GetMethod("Dismiss", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(Object.FindFirstObjectByType<GachaView>(), null);
            yield return null;
            Assert.IsTrue(FindAny("NavBar_PH_1920x120").activeSelf, Where("về trang roll mà thanh vẫn ẩn"));
        }

        // Reset lỗi trong Menu (ResetFailed): file save bị khoá nên xoá thất bại. Hộp đóng, dòng trạng thái báo lỗi và không hứa
        // "dữ liệu giữ nguyên"; Menu còn mở; thử lại sau khi nhả khoá thì reset được.
        [UnityTest, Timeout(180000)]
        public IEnumerator Hub_Menu_ResetFails_ShowsError_KeepsMenu_RetryWorks()
        {
            _step = "F1 Boot, +Gem";
            yield return BootToLobby();
            var session = Object.FindFirstObjectByType<GameBootstrap>().Session;
            var startGems = session.Wallet.Get(CurrencyType.Gem);
            session.Wallet.Add(CurrencyType.Gem, 1000);
            Assert.IsTrue(session.Persist(), Where("ghi save tạm"));
            var played = session.Wallet.Get(CurrencyType.Gem);

            _step = "F2 Menu → Reset → Xoá hết khi file bị khoá";
            yield return ClickWhenReady("NavTabMenu_PH_320x120");
            yield return ClickWhenReady("MenuResetBtn_PH_700x100");
            var locked = new FileStream(_savePath, FileMode.Open, FileAccess.Read, FileShare.None); // File.Delete sẽ ném IOException
            try
            {
                LogAssert.Expect(LogType.Error, new Regex("Reset failed"));
                LogAssert.Expect(LogType.Error, new Regex("MenuView: Reset Save Data FAILED"));
                yield return ClickWhenReady("ResetYesBtn_PH_340x110");
                Assert.AreEqual(played, session.Wallet.Get(CurrencyType.Gem), Where("reset thất bại mà bộ nhớ vẫn đổi"));
                Assert.AreEqual(0, UiDriver.FindButtons("ResetYesBtn_PH_340x110").Count, Where("reset thất bại thì hộp xác nhận phải đóng"));
                Assert.AreEqual(1, UiDriver.FindButtons("MenuResetBtn_PH_700x100").Count, Where("reset thất bại thì Menu phải còn mở"));
                var status = GameObject.Find("MenuStatus").GetComponent<Text>().text;
                Assert.AreEqual(MenuViewText.ResetFailed(), status, Where("dòng trạng thái phải báo Reset chưa xong"));
                StringAssert.DoesNotContain("giữ nguyên", status, Where("câu lỗi không được hứa dữ liệu giữ nguyên"));
            }
            finally
            {
                locked.Dispose();
            }

            _step = "F3 thử lại sau khi nhả khoá";
            yield return ClickWhenReady("MenuResetBtn_PH_700x100");
            var oldHub = Object.FindFirstObjectByType<HubNav>();
            yield return ClickWhenReady("ResetYesBtn_PH_340x110");
            yield return WaitForNewHub(oldHub);
            Assert.AreEqual(startGems, session.Wallet.Get(CurrencyType.Gem), Where("thử lại phải reset được"));
        }

        // N1: Reset lỗi thì Menu còn mở, Chọn đội cũng còn mở bên dưới nếu Reset không đóng nó; Hủy sau đó sẽ ghi lại đội cũ vào
        // save vừa bị ghi đè. Reset phải đóng trang trước (đội chọn dở về như lúc mở) rồi mới chạy.
        [UnityTest, Timeout(180000)]
        public IEnumerator Hub_Menu_Reset_ClosesOpenTeamSelect_BeforeResetting()
        {
            _step = "C1 Boot, Chọn đội (dở)";
            yield return BootToLobby();
            var session = Object.FindFirstObjectByType<GameBootstrap>().Session;
            var combat = ServiceLocator.Get<CombatService>();
            var teamBefore = session.Roster.GetBattleTeam().Select(c => c.Id).ToList();
            yield return OpenTeamSelectOfFirstBattle(combat);
            yield return ClickWhenReady("TeamCard0");
            CollectionAssert.AreNotEqual(teamBefore, session.Roster.GetBattleTeam().Select(c => c.Id).ToList(), Where("bấm TeamCard0 phải đổi đội, nếu không ca này không thử gì"));

            _step = "C2 Menu → Reset → Xoá hết khi file bị khoá";
            yield return ClickWhenReady("NavTabMenu_PH_320x120");
            yield return ClickWhenReady("MenuResetBtn_PH_700x100");
            var locked = new FileStream(_savePath, FileMode.Open, FileAccess.Read, FileShare.None);
            try
            {
                LogAssert.Expect(LogType.Error, new Regex("Save failed")); // Hủy lưu đội đã trả về, file đang khoá
                LogAssert.Expect(LogType.Error, new Regex("Reset failed"));
                LogAssert.Expect(LogType.Error, new Regex("MenuView: Reset Save Data FAILED"));
                yield return ClickWhenReady("ResetYesBtn_PH_340x110");
                Assert.AreEqual(0, Object.FindObjectsByType<TeamSelectPopup>(FindObjectsInactive.Include, FindObjectsSortMode.None).Count(p => p.IsOpen), Where("Reset phải đóng Chọn đội trước khi chạy"));
                CollectionAssert.AreEqual(teamBefore, session.Roster.GetBattleTeam().Select(c => c.Id).ToList(), Where("đội chọn dở phải về như lúc mở"));
            }
            finally
            {
                locked.Dispose();
            }
        }

        // Ảnh cho lần dừng playtest đợt B: mọi panel và trang con ở tiếng Việt rồi tiếng Anh, ra D:/bdp_BunDauPeo/shots/hubb-<màn>[-en].png
        // (tên hubb- để không ghi đè ảnh hub- của đợt A).
        // Đi cả đường đóng trang bằng ô khác (Chọn đội, Lịch sử quay), nên cũng là smoke test của IHubPage.
        private IEnumerator Shot(string screen, string suffix)
        {
            yield return null;
            yield return null;
            var path = ScreenCapture1080.Capture($"hubb-{screen}{suffix}.png");
            Assert.IsFalse(path.StartsWith("ERROR", StringComparison.Ordinal), Where(path));
        }

        private IEnumerator ShootAllPages(string suffix)
        {
            var combat = ServiceLocator.Get<CombatService>();
            _step = "S Lobby" + suffix;
            // Cờ thanh quán nằm trong save nên lượt tiếng Anh thừa hưởng lượt tiếng Việt: đóng trước để ảnh Lobby thanh đóng đúng.
            if (FindAny("RestaurantBar_PH_180x460").activeSelf) yield return ClickWhenReady("RestaurantBarToggle_PH_60x160");
            yield return Shot("lobby", suffix);

            var scroll = FindAny("WorldScroll").GetComponent<ScrollRect>();
            var centre = RectTransformUtility.WorldToScreenPoint(null, scroll.transform.position);
            yield return Swipe(centre, new Vector2(-Screen.width * 0.3f, -Screen.height * 0.2f), scroll.transform);
            scroll.StopMovement();
            yield return Shot("lobby-dragged", suffix);
            scroll.content.anchoredPosition = Vector2.zero;

            yield return OpenRestaurantBar();
            yield return Shot("lobby-bar", suffix);
            yield return ClickWhenReady("NavTabCharacters_PH_320x120");
            yield return Shot("characters", suffix);
            yield return ClickWhenReady("NavTabStages_PH_320x120");
            yield return Shot("modes", suffix);
            yield return ClickWhenReady("ModeMain_PH_820x560");
            yield return Shot("stages", suffix);

            var stages = combat.Config.Stages.Items;
            var battleIndex = stages.Select((st, i) => (st, i)).First(x => x.st.Kind == StageKind.Battle).i;
            for (var i = 0; i < battleIndex; i++)
            {
                yield return ClickWhenReady("Node" + i);
                if (i == 0) yield return Shot("story", suffix);
                yield return ClickWhenReady("ContinueButton");
            }

            yield return ClickWhenReady("Node" + battleIndex);
            yield return Shot("stage-info", suffix);
            yield return ClickWhenReady("ChallengeButton");
            yield return Shot("team", suffix);

            _step = "S Gacha" + suffix;
            yield return ClickWhenReady("NavTabGacha_PH_320x120");
            yield return Shot("gacha", suffix);
            yield return ClickWhenReady("HistoryButton");
            yield return Shot("gacha-history", suffix);
            // Lịch sử quay là hộp vẽ trên thanh: đóng bằng nút Đóng của hộp rồi mới bấm được thanh.
            yield return ClickWhenReady("CloseButton");

            _step = "S Quán" + suffix;
            yield return ClickWhenReady("NavTabRestaurant_PH_320x120");
            yield return OpenRestaurantBar();
            yield return ClickWhenReady("StaffButton");
            yield return Shot("staff", suffix);
            yield return ClickWhenReady("NavTabRestaurant_PH_320x120");
            yield return ClickWhenReady("UpgradeButton");
            yield return Shot("upgrade", suffix);
            yield return ClickWhenReady("NavTabRestaurant_PH_320x120");

            _step = "S Menu" + suffix;
            yield return ClickWhenReady("NavTabMenu_PH_320x120");
            yield return Shot("menu", suffix);
            yield return ClickWhenReady("MenuResetBtn_PH_700x100");
            yield return Shot("reset", suffix);
            yield return ClickWhenReady("ResetNoBtn_PH_340x110");
            yield return ClickWhenReady("NavTabRestaurant_PH_320x120");
        }

        // Game mới chưa có nhân viên nên Lobby trống người: thêm người từ catalog, xếp đủ ba vai, rồi chờ (tối đa 45 giây) tới khi có khách đang ăn
        // để ảnh thấy người đi, đĩa món và bóng trạng thái. Chạy trên save tạm của fixture, không đụng save thật.
        private IEnumerator SeedRestaurantForShots(GameBootstrap bootstrap)
        {
            var catalog = (CharacterCatalog)typeof(GameBootstrap).GetField("_catalog", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(bootstrap);
            var roster = bootstrap.Session.Roster;
            foreach (var character in catalog.Items)
                if (roster.GetOwned().Count < 6) roster.Add(character.Id);

            var restaurant = ServiceLocator.Get<RestaurantService>();
            var roles = new[] { StaffRole.Cook, StaffRole.Server, StaffRole.Cashier };
            var owned = roster.GetOwned();
            for (var i = 0; i < owned.Count; i++) restaurant.Staffing.TryAssign(owned[i].Id, roles[i % roles.Length]);

            var deadline = Time.realtimeSinceStartup + 45f;
            while (Time.realtimeSinceStartup < deadline && !restaurant.Simulation.Customers.Any(c => c.State == CustomerState.Eating)) yield return null;
        }

        [UnityTest, Timeout(360000)]
        public IEnumerator Hub_Screenshots_VietnameseAndEnglish()
        {
            _step = "S0 Boot (Việt)";
            yield return BootToLobby();
            var bootstrap = Object.FindFirstObjectByType<GameBootstrap>();
            var session = bootstrap.Session;
            yield return SeedRestaurantForShots(bootstrap);
            Assert.IsTrue(session.SetLanguage(Language.Vi), Where("SetLanguage(Vi)"));
            var oldHub = Object.FindFirstObjectByType<HubNav>();
            SceneManager.LoadScene("Restaurant");
            yield return WaitForNewHub(oldHub);
            yield return null;
            yield return ShootAllPages("");

            _step = "S0 đổi English";
            Assert.IsTrue(session.SetLanguage(Language.En), Where("SetLanguage(En)"));
            oldHub = Object.FindFirstObjectByType<HubNav>();
            SceneManager.LoadScene("Restaurant");
            yield return WaitForNewHub(oldHub);
            yield return null;
            yield return ShootAllPages("-en");
        }

#if UNITY_EDITOR
        // --- Panel DEV (DevOverlay): chỉ tự dựng lúc Play khởi động khi chưa có service (L-024), nên test dựng tay sau khi Boot xong.

        private IEnumerator BootWithDevOverlay()
        {
            yield return BootToLobby();
            DevOverlay.Build();
            Assert.IsNotNull(GameObject.Find("DevOverlay"), Where("DevOverlay không dựng được"));
        }

        // Panel DEV mở từ Menu → DEV (nút "Cài đặt" ở góc đã bỏ).
        private IEnumerator OpenDevPanel()
        {
            yield return ClickWhenReady("NavTabMenu_PH_320x120");
            yield return ClickWhenReady("MenuDevBtn_PH_700x100");
        }

        // Mọi nút trong panel Cài đặt (kể cả nút của hộp xác nhận đang tắt) không đè nhau (L-027): thêm nút DEV mới sai chỗ thì đỏ.
        private void AssertDevPanelButtonsDoNotOverlap()
        {
            var buttons = GameObject.Find("DevOverlay").transform.Find("SettingsPanel").GetComponentsInChildren<Button>(true);
            Assert.Greater(buttons.Length, 0, Where("panel Cài đặt không có nút nào"));
            for (var i = 0; i < buttons.Length; i++)
                for (var j = i + 1; j < buttons.Length; j++)
                    Assert.IsFalse(ScreenRect(buttons[i]).Overlaps(ScreenRect(buttons[j])),
                        Where($"nút DEV '{buttons[i].name}' {ScreenRect(buttons[i])} đè '{buttons[j].name}' {ScreenRect(buttons[j])}"));
        }

        private static Rect ScreenRect(Button button)
        {
            var corners = new Vector3[4];
            ((RectTransform)button.transform).GetWorldCorners(corners);
            var canvas = button.GetComponentInParent<Canvas>(true).rootCanvas;
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var a = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            var b = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator DevOverlay_OpensFromMenu_AndFromBattlePause()
        {
            _step = "D1 Boot";
            yield return BootWithDevOverlay();
            Assert.AreEqual(0, UiDriver.FindButtons("Chiến đấu thử").Count, Where("panel DEV không được mở sẵn"));

            _step = "D2 Menu → DEV mở panel, Đóng trả về Menu";
            yield return OpenDevPanel();
            Assert.AreEqual(1, UiDriver.FindButtons("Chiến đấu thử").Count, Where("Menu → DEV phải mở panel"));
            AssertDevPanelButtonsDoNotOverlap();
            yield return ClickWhenReady("ĐóngButton");
            Assert.AreEqual(0, UiDriver.FindButtons("Chiến đấu thử").Count, Where("Đóng phải tắt panel DEV"));
            Assert.AreEqual(1, UiDriver.FindButtons("MenuDevBtn_PH_700x100").Count, Where("Menu vẫn mở sau khi đóng panel DEV"));

            _step = "D3 trận, Menu tạm dừng → Cài đặt";
            yield return ClickWhenReady("MenuDevBtn_PH_700x100");
            yield return ClickWhenReady("Chiến đấu thử");
            yield return WaitForScene(BattleView.BattleScene);
            yield return WaitForButtonStep("MenuButton");
            yield return ClickWhenReady("MenuButton");
            yield return ClickWhenReady("SettingsButton");
            Assert.AreEqual(1, UiDriver.FindButtons("Chiến đấu thử").Count, Where("Cài đặt trong Menu tạm dừng phải mở panel DEV"));
            yield return ClickWhenReady("ĐóngButton");
            yield return ClickWhenReady("ResumeButton");
        }

        private static IEnumerable<Text> AllTexts() => Object.FindObjectsByType<Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        [UnityTest, Timeout(180000)]
        public IEnumerator LanguageSwitch_ToEnglish_ReloadsPersistsAndResetGoesEnglish()
        {
            _step = "L1 Boot";
            yield return BootWithDevOverlay();
            var session = Object.FindFirstObjectByType<GameBootstrap>().Session;
            // Boot mặc định tiếng Anh: ép về tiếng Việt và nạp lại Restaurant để bước sau chứng minh được việc nạp lại.
            Assert.IsTrue(session.SetLanguage(Language.Vi), Where("SetLanguage(Vi)"));
            SceneManager.LoadScene("Restaurant");
            yield return null;
            yield return WaitForScene("Restaurant");
            // Nút Nhân viên nằm trong thanh quán (mặc định đóng): mở thanh, và thanh nhớ trạng thái qua lần nạp lại bên dưới.
            yield return ClickWhenReady("RestaurantBarToggle_PH_60x160");
            yield return WaitForButtonStep("StaffButton");
            Assert.AreEqual("Nhân viên", GameObject.Find("StaffButton").GetComponentInChildren<Text>().text, Where("nhãn trước khi đổi"));

            _step = "L2 bấm LanguageButton";
            yield return OpenDevPanel();
            yield return ClickWhenReady("LanguageButton");

            _step = "L3 chờ scene nạp lại";
            var expected = UiText.Get("restaurant.staff");
            var deadline = Time.realtimeSinceStartup + SceneTimeout;
            string label = null;
            while (label != expected)
            {
                label = GameObject.Find("StaffButton")?.GetComponentInChildren<Text>()?.text;
                if (Time.realtimeSinceStartup > deadline) Assert.Fail(Where($"nút Nhân viên đang là '{label}', cần '{expected}'"));
                yield return null;
            }

            Assert.AreEqual(Language.En, GameLanguage.Current, Where("Current"));
            Assert.AreEqual("en", session.Save.Language, Where("save"));

            _step = "L4 Reset về tiếng Anh";
            Assert.IsTrue(ServiceLocator.Get<GameResetter>().Reset(), Where("Reset"));
            Assert.AreEqual("", session.Save.Language, Where("Reset phải xoá lựa chọn ngôn ngữ"));
            Assert.AreEqual(Language.En, GameLanguage.Current, Where("sau Reset phải là tiếng Anh"));
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator DevOverlay_Reset_NeedsConfirmation_CancelKeepsData()
        {
            _step = "D1 Boot";
            yield return BootWithDevOverlay();
            var wallet = Object.FindFirstObjectByType<GameBootstrap>().Session.Wallet;
            var startGems = wallet.Get(CurrencyType.Gem);

            _step = "D2 +Gem";
            yield return OpenDevPanel();
            yield return ClickWhenReady($"+{DevOverlay.GemGrant} Gem");
            var played = startGems + DevOverlay.GemGrant;
            Assert.AreEqual(played, wallet.Get(CurrencyType.Gem), Where("Gem sau khi cộng"));

            _step = "D3 bấm Reset dữ liệu";
            yield return ClickWhenReady("Reset dữ liệuButton");
            Assert.AreEqual(played, wallet.Get(CurrencyType.Gem), Where("mới bấm Reset dữ liệu mà đã reset: bỏ qua bước xác nhận (D-005)"));
            Assert.AreEqual(1, UiDriver.FindButtons("Xoá hết").Count, Where("hộp xác nhận phải hiện"));

            _step = "D4 Huỷ";
            yield return ClickWhenReady("Huỷ");
            Assert.AreEqual(played, wallet.Get(CurrencyType.Gem), Where("Huỷ mà vẫn reset"));
            Assert.AreEqual(0, UiDriver.FindButtons("Xoá hết").Count, Where("Huỷ phải đóng hộp xác nhận"));

            _step = "D5 Xoá hết";
            yield return ClickWhenReady("Reset dữ liệuButton");
            Assert.AreEqual(played, wallet.Get(CurrencyType.Gem), Where("bấm Reset lần hai mà đã reset"));
            yield return ClickWhenReady("Xoá hết");
            Assert.AreEqual(startGems, wallet.Get(CurrencyType.Gem), Where("Xoá hết phải đưa Gem về lúc mới"));
            Assert.AreEqual(0, UiDriver.FindButtons("Reset dữ liệuButton").Count, Where("reset thành công thì panel phải đóng"));
        }

        // Id có trong catalog mà game mới không cấp: cho ca "ảnh chụp đội mang người không còn sở hữu sau Reset".
        private static string UnownedCharacterId(GameBootstrap bootstrap)
        {
            var catalog = (CharacterCatalog)typeof(GameBootstrap).GetField("_catalog", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(bootstrap);
            return catalog.Items.First(c => !bootstrap.Session.Roster.IsOwned(c.Id)).Id;
        }

        // N1 đường DEV: Reset ở panel DEV không nạp lại scene, nên Chọn đội mở dở phải được đóng (trả đội) trước khi xoá;
        // nếu không, Hủy sau đó ghi người đã mất vào save mới (D-005).
        [UnityTest, Timeout(180000)]
        public IEnumerator DevOverlay_Reset_ClosesOpenTeamSelect_NoStaleTeamAfterCancel()
        {
            _step = "N1 Boot, thêm người ngoài game mới vào đội, mở Chọn đội";
            yield return BootWithDevOverlay();
            var bootstrap = Object.FindFirstObjectByType<GameBootstrap>();
            var session = bootstrap.Session;
            var extra = UnownedCharacterId(bootstrap);
            Assert.IsTrue(session.Roster.Add(extra), Where("thêm người"));
            Assert.IsTrue(session.Roster.AddToTeam(extra), Where("đưa vào đội"));
            yield return OpenTeamSelectOfFirstBattle(ServiceLocator.Get<CombatService>());

            _step = "N2 DEV → Reset → Xoá hết";
            yield return OpenDevPanel();
            yield return ClickWhenReady("Reset dữ liệuButton");
            yield return ClickWhenReady("Xoá hết");
            Assert.IsFalse(session.Roster.IsOwned(extra), Where("Reset phải xoá người thêm"));

            _step = "N3 Hủy (nếu popup còn mở)";
            var popups = Object.FindObjectsByType<TeamSelectPopup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.AreEqual(0, popups.Count(p => p.IsOpen), Where("Reset phải đóng Chọn đội trước khi xoá"));
            foreach (var popup in popups) popup.Cancel();
            CollectionAssert.DoesNotContain(session.Save.BattleTeam, extra, Where("đội sau Hủy mang người không còn sở hữu"));
        }

        // Góp ý 1: đổi ngôn ngữ ở DEV lưu cả save, nên đội chọn dở phải được trả về trước đó, như đường Menu.
        [UnityTest, Timeout(180000)]
        public IEnumerator DevOverlay_Language_ClosesOpenTeamSelect_BeforeSaving()
        {
            _step = "M1 Boot (Việt), Chọn đội (dở)";
            yield return BootWithDevOverlay();
            var session = Object.FindFirstObjectByType<GameBootstrap>().Session;
            Assert.IsTrue(session.SetLanguage(Language.Vi), Where("SetLanguage(Vi)"));
            SceneManager.LoadScene("Restaurant");
            yield return null;
            yield return WaitForScene("Restaurant");
            var teamBefore = session.Roster.GetBattleTeam().Select(c => c.Id).ToList();
            yield return OpenTeamSelectOfFirstBattle(ServiceLocator.Get<CombatService>());
            yield return ClickWhenReady("TeamCard0");
            CollectionAssert.AreNotEqual(teamBefore, session.Roster.GetBattleTeam().Select(c => c.Id).ToList(), Where("bấm TeamCard0 phải đổi đội, nếu không ca này không thử gì"));

            _step = "M2 DEV → Ngôn ngữ";
            yield return OpenDevPanel();
            var oldHub = Object.FindFirstObjectByType<HubNav>();
            yield return ClickWhenReady("LanguageButton");
            yield return WaitForNewHub(oldHub);

            _step = "M3 kiểm file save";
            var loaded = new SaveService(_savePath).Load();
            Assert.IsNotNull(loaded, Where("không đọc lại được save tạm"));
            Assert.AreEqual("en", loaded.Language, Where("Language trong save"));
            CollectionAssert.AreEqual(teamBefore, loaded.BattleTeam, Where("đội trong file save phải là đội lúc mở Chọn đội"));
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator DevOverlay_ResetFails_KeepsPanelOpen_ShowsError_KeepsData()
        {
            _step = "D1 Boot";
            yield return BootWithDevOverlay();
            var session = Object.FindFirstObjectByType<GameBootstrap>().Session;
            var wallet = session.Wallet;
            var startGems = wallet.Get(CurrencyType.Gem);

            _step = "D2 +Gem và khoá file save";
            yield return OpenDevPanel();
            yield return ClickWhenReady($"+{DevOverlay.GemGrant} Gem");
            var played = startGems + DevOverlay.GemGrant;
            Assert.IsTrue(session.Persist(), Where("ghi save tạm"));
            var locked = new FileStream(_savePath, FileMode.Open, FileAccess.Read, FileShare.None); // File.Delete sẽ ném IOException
            try
            {
                _step = "D3 Xoá hết khi file bị khoá";
                yield return ClickWhenReady("Reset dữ liệuButton");
                LogAssert.Expect(LogType.Error, new Regex("Reset failed"));
                LogAssert.Expect(LogType.Error, new Regex("DevOverlay: Reset Save Data FAILED"));
                yield return ClickWhenReady("Xoá hết");
                Assert.AreEqual(played, wallet.Get(CurrencyType.Gem), Where("reset thất bại mà bộ nhớ vẫn đổi"));
                Assert.AreEqual(1, UiDriver.FindButtons("Xoá hết").Count, Where("reset thất bại thì không được đóng panel như thành công"));
                Assert.IsTrue(AllTexts().Any(t => t.text.Contains("RESET THẤT BẠI")), Where("phải báo lỗi trên panel"));
                Assert.IsFalse(AllTexts().Any(t => t.text.Contains("giữ nguyên")), Where("câu lỗi không được hứa dữ liệu giữ nguyên (ResetToDefaults có thể đã ghi đè bộ nhớ)"));
            }
            finally
            {
                locked.Dispose();
            }

            _step = "D4 thử lại sau khi nhả khoá";
            yield return ClickWhenReady("Xoá hết");
            Assert.AreEqual(startGems, wallet.Get(CurrencyType.Gem), Where("thử lại phải reset được"));
            Assert.AreEqual(0, UiDriver.FindButtons("Reset dữ liệuButton").Count, Where("reset thành công thì panel phải đóng"));
        }
#endif

        private string Where(string message) => $"[bước {_step}] {message}";

        private IEnumerator WaitForScene(string name)
        {
            var deadline = Time.realtimeSinceStartup + SceneTimeout;
            while (SceneManager.GetActiveScene().name != name)
            {
                if (Time.realtimeSinceStartup > deadline)
                    Assert.Fail(Where($"{SceneTimeout}s chờ scene '{name}', đang ở '{SceneManager.GetActiveScene().name}'. Nút: {UiDriver.DescribeVisibleButtons()}"));
                yield return null;
            }
            yield return null; // để Awake/Start/Update đầu của scene chạy
        }

        // Mở app: Boot nạp Tiêu đề; chạm TapArea qua raycast thật (L-036) rồi mới vào Lobby.
        private IEnumerator BootToLobby()
        {
            SceneManager.LoadScene("Boot");
            yield return WaitForScene(TitleView.TitleScene);
            yield return ClickWhenReady("TapArea");
            yield return WaitForScene("Restaurant");
        }

        // Review Focus 3: chạm Tiêu đề hai lần liền thì Restaurant chỉ nạp một lần; đổi ngôn ngữ nạp thẳng Restaurant, không qua Tiêu đề.
        [UnityTest, Timeout(180000)]
        public IEnumerator Title_TapEntersLobby()
        {
            _step = "T1 Boot → Tiêu đề";
            SceneManager.LoadScene("Boot");
            yield return WaitForScene(TitleView.TitleScene);
            Assert.AreEqual(0, Object.FindObjectsByType<HubNav>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, Where("Tiêu đề chưa có Lobby"));
            Assert.AreEqual("Touch to start", GameObject.Find("TapHint").GetComponent<Text>().text, Where("game mới: tiếng Anh"));
            StringAssert.Contains(Application.version, GameObject.Find("VersionText").GetComponent<Text>().text, Where("số phiên bản"));

            _step = "T1b ảnh Tiêu đề";
            yield return null;
            yield return null;
            var shotEn = ScreenCapture1080.Capture("hubc-title-en.png");
            Assert.IsFalse(shotEn.StartsWith("ERROR", StringComparison.Ordinal), Where(shotEn));
            GameLanguage.Current = Language.Vi;
            foreach (var label in Object.FindObjectsByType<LocalizedText>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                label.enabled = false;
                label.enabled = true;
            }

            yield return null;
            yield return null;
            var shotVi = ScreenCapture1080.Capture("hubc-title.png");
            Assert.IsFalse(shotVi.StartsWith("ERROR", StringComparison.Ordinal), Where(shotVi));
            GameLanguage.Current = Language.En;

            _step = "T2 chạm hai lần";
            var loads = new List<string>();
            void OnLoaded(Scene scene, LoadSceneMode mode) => loads.Add(scene.name);
            SceneManager.sceneLoaded += OnLoaded;
            try
            {
                yield return WaitForButtonStep("TapArea");
                Assert.AreEqual(ClickStatus.Clicked, UiDriver.Click("TapArea").Status, Where("chạm lần 1"));
                Assert.AreEqual(ClickStatus.Clicked, UiDriver.Click("TapArea").Status, Where("chạm lần 2 cùng khung"));
                yield return null;
                var third = UiDriver.Click("TapArea");
                Assert.IsTrue(third.Status == ClickStatus.Clicked || third.Status == ClickStatus.NotFound, Where($"chạm lần 3 khung kế: {third}"));
                yield return WaitForScene("Restaurant");
                for (var i = 0; i < 5; i++) yield return null;

                Assert.AreEqual(1, loads.Count(n => n == "Restaurant"), Where($"Restaurant nạp {loads.Count(n => n == "Restaurant")} lần: {string.Join(",", loads)}"));
                Assert.AreEqual(1, Object.FindObjectsByType<HubNav>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, Where("đúng một HubNav"));

                _step = "T3 đổi ngôn ngữ không qua Tiêu đề";
                loads.Clear();
                var oldHub = Object.FindFirstObjectByType<HubNav>();
                yield return ClickWhenReady("NavTabMenu_PH_320x120");
                yield return ClickWhenReady("MenuLanguageBtn_PH_700x100");
                yield return WaitForNewHub(oldHub);
                CollectionAssert.DoesNotContain(loads, TitleView.TitleScene, Where("đổi ngôn ngữ không được nạp Tiêu đề"));
                Assert.AreEqual("Restaurant", SceneManager.GetActiveScene().name, Where("vẫn ở Lobby"));
            }
            finally
            {
                SceneManager.sceneLoaded -= OnLoaded;
            }
        }

        private IEnumerator WaitForButtonStep(string name)
        {
            var inner = UiDriver.WaitForButton(name, ButtonTimeout);
            while (true)
            {
                try
                {
                    if (!inner.MoveNext()) yield break;
                }
                catch (TimeoutException e)
                {
                    Assert.Fail(Where(e.Message));
                }
                yield return inner.Current;
            }
        }

        private IEnumerator ClickWhenReady(string name)
        {
            yield return WaitForButtonStep(name);
            var result = UiDriver.Click(name);
            Assert.AreEqual(ClickStatus.Clicked, result.Status, Where(result.ToString()));
            yield return null; // cho UI xử lý cú bấm
        }

        private static NewGameConfig LoadNewGameConfig()
        {
#if UNITY_EDITOR
            var guid = UnityEditor.AssetDatabase.FindAssets("t:NewGameConfig").Single();
            return UnityEditor.AssetDatabase.LoadAssetAtPath<NewGameConfig>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
#else
            throw new NotSupportedException("Smoke test chỉ chạy trong Editor.");
#endif
        }

        // Băm mọi file save.json* thật (không có thì băm rỗng) để TearDown biết test có chạm vào save thật không.
        private static string HashRealSave()
        {
            var dir = Application.persistentDataPath;
            var files = Directory.Exists(dir)
                ? Directory.GetFiles(dir).Where(f =>
                {
                    var n = Path.GetFileName(f);
                    return n == "save.json" || n.StartsWith("save.json.", StringComparison.Ordinal);
                }).OrderBy(f => f, StringComparer.Ordinal).ToArray()
                : new string[0];
            using var sha = SHA256.Create();
            foreach (var f in files)
            {
                var name = System.Text.Encoding.UTF8.GetBytes(Path.GetFileName(f));
                sha.TransformBlock(name, 0, name.Length, null, 0);
                var bytes = File.ReadAllBytes(f);
                sha.TransformBlock(bytes, 0, bytes.Length, null, 0);
            }
            sha.TransformFinalBlock(new byte[0], 0, 0);
            return BitConverter.ToString(sha.Hash);
        }
    }
}
