#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Linq;
using BunDauPeo.Characters;
using BunDauPeo.Combat;
using BunDauPeo.Core;
using BunDauPeo.Restaurant;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Nút DEV (trong panel Cài đặt) để thử trong Editor và Development Build; tự dựng bằng code nên không có gì của nó trong bản phát hành.
    // Mở từ Menu → DEV của hub và từ Menu tạm dừng của trận; không còn nút nào ngoài panel.
    // Không [SerializeField] trong #if (lệch bố cục serialize), không OnGUI (bản chỉ dùng Input System không nhận IMGUI).
    public static class DevOverlay
    {
        public const long MaterialGrant = 50;
        public const long GemGrant = 1000;
        public const int AfkRewindHours = 1;
        public const int StaminaGrant = 900;
        public const int WeatherRewindSeconds = 1800;
        private const string ConfirmPrompt = "Xoá TOÀN BỘ dữ liệu đã lưu và đưa game về như mới?\nKhông thể hoàn tác.";
        private const string ResetFailedMessage = "RESET THẤT BẠI: không xoá hay ghi được file save (đang bị khoá?).\nThử lại.";

        // Scene panel DEV còn nạp: nạp lại khi đổi ngôn ngữ, Chiến đấu thử.
        public static readonly string[] SceneNames = { BattleView.RestaurantScene, BattleView.BattleScene };

        private static GameObject _root;
        private static Action _openPanel;
        // Màn mà nút "Chiến đấu thử" dùng; nút "Màn thử" xoay vòng nó.
        private static string _trialStageId;

        // Tắt domain reload thì static còn nguyên sang lần Play sau.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _root = null;
            _openPanel = null;
            _trialStageId = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Build()
        {
            if (_root != null) return;

            CombatService combat;
            try
            {
                ServiceLocator.Get<RestaurantService>();
                combat = ServiceLocator.Get<CombatService>();
            }
            catch (InvalidOperationException)
            {
                // Boot lỗi hoặc không phải Boot: không có service thì không có gì để thử.
                return;
            }

            _root = new GameObject("DevOverlay", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            UnityEngine.Object.DontDestroyOnLoad(_root);
            var canvas = _root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = _root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            // Khớp theo chiều cao như các màn khác, để panel không lấn lên thẻ trận ở màn rộng.
            scaler.matchWidthOrHeight = 1f;

            _trialStageId = NextBattleStageId(combat.Config.Stages, null);
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildSettings(font, combat);
        }

        // Đường duy nhất mở panel: Menu → DEV của hub và Menu tạm dừng của trận.
        public static void OpenSettings() => _openPanel?.Invoke();

        public static string LanguageButtonLabel(Language current) => current == Language.En ? "Language: English" : "Ngôn ngữ: Tiếng Việt";

        // Battle không nạp lại: scene đang chạy trận, nạp lại làm hỏng trận.
        public static bool ShouldReloadScene(string activeScene) => activeScene != BattleView.BattleScene;

        // Trang kín màn của hub đang mở (Chọn đội...) giữ ảnh chụp đội cũ: đóng trước khi lưu hay xoá save, kẻo Huỷ sau đó ghi đội cũ
        // vào save mới (D-005, L-035). Battle không có hub.
        private static void CloseHubPages()
        {
            var hub = UnityEngine.Object.FindFirstObjectByType<HubNav>();
            if (hub != null) hub.CloseOpenPages();
        }

        public static System.Threading.Tasks.Task<OnlineStatus> ResetAll()
        {
            CloseHubPages();
            return ServiceLocator.Get<GameResetter>().ResetAsync();
        }

        // Trả dòng báo cho nút: "" khi đã nạp lại scene.
        public static string SwitchLanguage()
        {
            CloseHubPages();
            var next = GameLanguage.Current == Language.Vi ? Language.En : Language.Vi;
            if (!ServiceLocator.Get<LanguageSwitcher>().Set(next)) return "Chưa lưu được, giữ ngôn ngữ cũ";

            var scene = SceneManager.GetActiveScene().name;
            // shortcut: Battle không vẽ lại khi đổi ngôn ngữ, làm khi hub có nút đổi ngôn ngữ cho người chơi.
            if (!ShouldReloadScene(scene)) return "Ngôn ngữ đổi khi rời trận";

            SceneManager.LoadScene(scene);
            return "";
        }

        // Mọi nút DEV nằm trong panel Cài đặt, chỉ người thử bấm Menu → DEV mới mở; ngoài panel không có nút nào nên không đè nút game (L-025).
        // Panel sống qua Về Tiêu đề (máy khác, Xoá tài khoản) còn service thì dựng lại: lấy service lúc bấm, không giữ bản cũ.
        private static CombatService Combat() => ServiceLocator.Get<CombatService>();

        // Chuyển màn do thanh dưới của hub làm. Reset luôn qua hộp xác nhận (D-005).
        private static void BuildSettings(Font font, CombatService combat)
        {
            // Nền mờ chặn bấm xuyên xuống game khi panel mở.
            var dim = NewRect("SettingsPanel", _root.transform, Vector2.zero, Vector2.zero);
            dim.anchorMin = Vector2.zero;
            dim.anchorMax = Vector2.one;
            dim.offsetMin = dim.offsetMax = Vector2.zero;
            dim.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);

            // Mười một nút xếp hai cột, từ trên xuống, rồi mục Thời tiết (bốn nút + dòng trạng thái) phía trên; hộp xác nhận Reset (cao 170) ở đáy.
            var box = NewRect("Box", dim, Vector2.zero, new Vector2(1000, 900));
            box.anchorMin = box.anchorMax = box.pivot = new Vector2(0.5f, 0.5f);
            box.anchoredPosition = Vector2.zero;
            box.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.16f, 1f);

            AddLabel(box, font, "Cài đặt (DEV)", new Vector2(0, 825), new Vector2(1000, 60), 40);
            AddButton(box, font, "Đóng", new Vector2(820, 825), 160, () => dim.gameObject.SetActive(false));
            BuildWeatherSection(box, font, () => ServiceLocator.Get<RestaurantService>().Weather);

            const float Left = 40, Right = 520, Width = 440;
            // Nhiệm vụ đợt 3: Reset thay cho đợi sang ngày; +Thể lực để thử hộp xác nhận vượt trần. Lệnh DevOnly chỉ log cảnh báo.
            AddButton(box, font, "Reset nhiệm vụ", new Vector2(Left, 555), Width, async () => await ServiceLocator.Get<DevCommands>().ResetMissionsAsync());
            AddButton(box, font, $"+Thể lực {StaminaGrant}", new Vector2(Right, 555), Width,
                async () => await ServiceLocator.Get<DevCommands>().AddStaminaAsync(StaminaGrant));
            AddButton(box, font, "Chiến đấu thử", new Vector2(Left, 475), Width, () =>
            {
                if (StartTrialBattle(Combat())) dim.gameObject.SetActive(false);
            });
            Text stageLabel = null;
            stageLabel = AddButton(box, font, StageLabel(combat), new Vector2(Right, 475), Width, () =>
            {
                _trialStageId = NextBattleStageId(Combat().Config.Stages, _trialStageId);
                stageLabel.text = StageLabel(Combat());
            });
            AddButton(box, font, "Hồi đầy thể lực", new Vector2(Left, 395), Width, async () => await ServiceLocator.Get<DevCommands>().RefillStaminaAsync());
            // Qua ví như mọi nguồn thu khác (không chạm file save), nên Changed bắn và Reset Save Data xoá sạch như thường.
            AddButton(box, font, $"+{MaterialGrant} Material", new Vector2(Right, 395), Width,
                () => ServiceLocator.Get<Wallet>().Add(CurrencyType.Material, MaterialGrant));
            AddButton(box, font, $"+{GemGrant} Gem", new Vector2(Left, 315), Width,
                async () => await ServiceLocator.Get<DevCommands>().AddGemAsync(GemGrant));
            AddButton(box, font, $"Tua AFK +{AfkRewindHours} giờ", new Vector2(Right, 315), Width, RewindAfk);
            // Dòng báo (lỗi lưu, đang ở Battle) hiện ngay trên nhãn nút; mở lại panel thì nhãn trở về ngôn ngữ hiện tại.
            Text languageLabel = null;
            languageLabel = AddButton(box, font, LanguageButtonLabel(GameLanguage.Current), new Vector2(Left, 235), Width, () =>
            {
                var message = SwitchLanguage();
                if (message == "") dim.gameObject.SetActive(false);
                else languageLabel.text = message;
            });
            languageLabel.fontSize = 28;
            languageLabel.transform.parent.name = "LanguageButton";

            var confirm = NewRect("ResetConfirm", box, new Vector2(40, 40), new Vector2(920, 170));
            confirm.gameObject.AddComponent<Image>().color = new Color(0.35f, 0.1f, 0.1f, 1f);
            var prompt = AddLabel(confirm, font, ConfirmPrompt, new Vector2(20, 80), new Vector2(880, 80), 32);
            var yes = AddButton(confirm, font, "Xoá hết", new Vector2(40, 10), 400, async () =>
            {
                // Thất bại (mất mạng, file bị khoá...) thì giữ panel mở và báo rõ, không đóng như thành công.
                var status = await ResetAll();
                if (status == OnlineStatus.Ok)
                {
                    Debug.Log("DevOverlay: Reset Save Data = True");
                    dim.gameObject.SetActive(false);
                    return;
                }

                prompt.text = ResetFailedMessage + $" ({status})";
                prompt.color = new Color(1f, 0.9f, 0.3f);
                Debug.LogError($"DevOverlay: Reset Save Data FAILED ({status}).");
            });
            yes.GetComponentInParent<Image>().color = new Color(0.85f, 0.2f, 0.2f);
            AddButton(confirm, font, "Huỷ", new Vector2(480, 10), 400, () => confirm.gameObject.SetActive(false));
            confirm.gameObject.SetActive(false);

            AddButton(box, font, "Reset dữ liệu", new Vector2(Right, 235), Width, () =>
            {
                prompt.text = ConfirmPrompt;
                prompt.color = Color.white;
                confirm.gameObject.SetActive(true);
            });
            // Quà thử (đợt 4): chỉ chế độ Local (UGS lấy quà từ Dashboard). Ghi gifts.json cạnh save; mở Hộp thư là thấy.
            var giftLabel = AddButton(box, font, "Quà thử", new Vector2(Left, 155), Width, () =>
            {
                if (ServiceLocator.Get<IGameServer>() is LocalGameServer local && !string.IsNullOrEmpty(local.GiftsPath))
                    System.IO.File.WriteAllText(local.GiftsPath, SampleGiftJson(ServiceLocator.Get<ServerTime>().UtcNow));
            });
            var giftButton = giftLabel.transform.parent.gameObject;
            giftButton.name = "SampleGiftButton";
            dim.gameObject.SetActive(false);
            _openPanel = () =>
            {
                giftButton.SetActive(ServiceLocator.Get<IGameServer>() is LocalGameServer);
                languageLabel.text = LanguageButtonLabel(GameLanguage.Current);
                confirm.gameObject.SetActive(false);
                dim.gameObject.SetActive(true);
            };
        }

        // Một quà mẫu hạn 7 ngày (Gem + Coin), cùng hình Dashboard; id theo giờ nên bấm lại ra quà mới nhận được.
        public static string SampleGiftJson(DateTime nowUtc) =>
            "[{\"id\":\"dev_gift_" + nowUtc.ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture) + "\","
            + "\"titleVi\":\"Quà thử\",\"titleEn\":\"Test gift\","
            + "\"rewards\":[{\"type\":\"Gem\",\"amount\":300},{\"type\":\"Coin\",\"amount\":5000}],"
            + "\"startUtc\":\"" + nowUtc.AddDays(-1).ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture) + "\","
            + "\"endUtc\":\"" + nowUtc.AddDays(7).ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture) + "\"}]";

        private static Text AddLabel(RectTransform parent, Font font, string text, Vector2 position, Vector2 size, int fontSize)
        {
            var label = NewRect("Label", parent, position, size).gameObject.AddComponent<Text>();
            label.font = font;
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
        }

        // Đội trống thì thêm tối đa một đội đầy từ người đã sở hữu, theo thứ tự sở hữu; trả số người thêm.
        public static int FillTeamIfEmpty(Roster roster)
        {
            if (roster.GetBattleTeam().Count > 0) return 0;

            var added = 0;
            foreach (var character in roster.GetOwned())
            {
                if (added >= Roster.MaxBattleTeamSize) break;
                if (roster.AddToTeam(character.Id)) added++;
            }

            return added;
        }

        // Màn Battle kế sau currentId, vòng lại; null hoặc không thấy (kể cả màn Story) thì màn Battle đầu tiên.
        public static string NextBattleStageId(StageCatalog catalog, string currentId)
        {
            if (catalog == null) return null;

            var ids = catalog.Items.Where(s => s != null && s.Kind == StageKind.Battle).Select(s => s.Id).ToList();
            if (ids.Count == 0) return null;

            var index = ids.IndexOf(currentId);
            return ids[(index + 1) % ids.Count];
        }

        private static string StageLabel(CombatService combat) =>
            $"Màn thử: {(combat.Config.Stages != null && combat.Config.Stages.TryGet(_trialStageId, out var stage) ? stage.DisplayName : "-")}";

        private static bool StartTrialBattle(CombatService combat)
        {
            FillTeamIfEmpty(ServiceLocator.Get<Roster>());
            var result = combat.StartBattle(_trialStageId);
            Debug.Log($"DevOverlay: StartBattle({_trialStageId}) = {result.Status}");
            if (result.Status != StartBattleStatus.Success) return false;
            SceneManager.LoadScene(BattleView.BattleScene);
            return true;
        }

        // Heartbeat trước để chốt rate hiện tại (game mới chưa có Heartbeat nào thì rate là 0); sau Rewind phải CatchUp ngay,
        // nếu không lần lưu định kỳ kế tiếp sẽ đè mốc.
        private static void RewindAfk()
        {
            var afk = ServiceLocator.Get<RestaurantService>().Afk;
            afk.Heartbeat();
            afk.Rewind(TimeSpan.FromHours(AfkRewindHours));
            afk.CatchUp();
        }

        // Mục Thời tiết (spec Thời tiết 5): dòng trạng thái cập nhật khi mở panel và mỗi giây, bốn nút thử. Lấy service lúc bấm (panel sống qua Về Tiêu đề).
        // ForceRain tự lưu; ba nút còn lại để autosave của quán lưu, như nút Tua AFK.
        public static RectTransform BuildWeatherSection(RectTransform box, Font font, Func<WeatherService> weather)
        {
            var section = NewRect("WeatherSection", box, new Vector2(0, 630), new Vector2(1000, 170));
            var status = NewRect("WeatherStatus", section, new Vector2(40, 70), new Vector2(920, 100)).gameObject.AddComponent<DevWeatherStatus>();
            var text = status.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = 28;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.white;
            text.raycastTarget = false;
            status.Init(text, () => WeatherStatusText(weather()));
            status.Refresh();

            var actions = new (string Label, Action<WeatherService> Run)[]
            {
                ("Gọi mưa", w => w.ForceRain()), ("Hết mưa", w => w.ForceClear()),
                ("Tua +30 phút", w => w.AddContinuous(WeatherRewindSeconds)), ("Quay ngay", w => w.RollNow()),
            };
            for (var i = 0; i < actions.Length; i++)
            {
                var run = actions[i].Run;
                AddButton(section, font, actions[i].Label, new Vector2(40 + i * 235, 0), 215, () =>
                {
                    run(weather());
                    status.Refresh();
                });
            }

            return section;
        }

        public static string WeatherStatusText(WeatherService weather)
        {
            var now = weather.IsRaining ? $"Mưa, còn {CombatViewText.Clock(weather.RainRemainingSeconds)}" : "Quang";
            var chance = (weather.CurrentChance * 100).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
            return $"Đang: {now}\nChơi liên tục: {(int)weather.ContinuousMinutes} phút · Xác suất mỗi lần quay: {chance}% · Lần quay kế: {CombatViewText.Clock(weather.SecondsToNextRoll)}";
        }

        private static RectTransform NewRect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static Text AddButton(RectTransform parent, Font font, string label, Vector2 position, float width, Action onClick)
        {
            var rect = NewRect(label + "Button", parent, position, new Vector2(width, 60));
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.30f, 0.50f, 0.90f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick());

            var textRect = NewRect("Label", rect, Vector2.zero, rect.sizeDelta);
            var text = textRect.gameObject.AddComponent<Text>();
            text.font = font;
            text.text = label;
            text.fontSize = 32;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }
    }
}
#endif
