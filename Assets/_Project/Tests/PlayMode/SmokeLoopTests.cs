using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using BunDauPeo.Boot;
using BunDauPeo.Characters;
using BunDauPeo.Combat;
using BunDauPeo.Core;
using BunDauPeo.Playtest;
using BunDauPeo.UI;
using NUnit.Framework;
using UnityEngine;
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
            SceneManager.LoadScene("Boot");
            yield return WaitForScene("Restaurant");
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
            SceneManager.LoadScene("Boot");
            yield return WaitForScene("Restaurant");
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
            SceneManager.LoadScene("Boot");
            yield return WaitForScene("Restaurant");
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
            SceneManager.LoadScene("Boot");
            yield return WaitForScene("Restaurant");
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

#if UNITY_EDITOR
        // --- Panel DEV (DevOverlay): chỉ tự dựng lúc Play khởi động khi chưa có service (L-024), nên test dựng tay sau khi Boot xong.

        private IEnumerator BootWithDevOverlay()
        {
            SceneManager.LoadScene("Boot");
            yield return WaitForScene("Restaurant");
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
        public IEnumerator LanguageSwitch_ToEnglish_ReloadsPersistsAndResetFollowsDevice()
        {
            _step = "L1 Boot";
            yield return BootWithDevOverlay();
            var session = Object.FindFirstObjectByType<GameBootstrap>().Session;
            // Máy dev có thể là tiếng Anh (Boot theo máy): ép về tiếng Việt và nạp lại Restaurant để bước sau chứng minh được việc nạp lại.
            Assert.IsTrue(session.SetLanguage(Language.Vi), Where("SetLanguage(Vi)"));
            SceneManager.LoadScene("Restaurant");
            yield return null;
            yield return WaitForScene("Restaurant");
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

            _step = "L4 Reset về theo máy";
            Assert.IsTrue(ServiceLocator.Get<GameResetter>().Reset(), Where("Reset"));
            Assert.AreEqual("", session.Save.Language, Where("Reset phải xoá lựa chọn ngôn ngữ"));
            Assert.AreEqual(GameLanguage.Resolve("", Application.systemLanguage), GameLanguage.Current, Where("sau Reset phải theo máy, không kẹt English"));
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
