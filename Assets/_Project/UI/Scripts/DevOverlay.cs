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
    // Dải DEV để thử trong Editor và Development Build; tự dựng bằng code nên không có gì của nó trong bản phát hành.
    // Hub sau này thay dải này. Không [SerializeField] trong #if (lệch bố cục serialize), không OnGUI (bản chỉ dùng Input System không nhận IMGUI).
    public static class DevOverlay
    {
        public const string RestaurantScene = "Restaurant";
        public const string GachaScene = "Gacha";
        public const long MaterialGrant = 50;
        public const long GemGrant = 1000;
        public const int AfkRewindHours = 1;

        public static readonly string[] SceneNames = { RestaurantScene, GachaScene, BattleView.StageSelectScene, BattleView.BattleScene };

        private static GameObject _root;
        // Màn mà nút "Chiến đấu thử" dùng; nút "Màn thử" xoay vòng nó.
        private static string _trialStageId;

        // Tắt domain reload thì static còn nguyên sang lần Play sau.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _root = null;
            _trialStageId = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Build()
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
            // Khớp theo chiều cao như các màn khác, để dải không lấn lên thẻ trận ở màn rộng.
            scaler.matchWidthOrHeight = 1f;

            _trialStageId = NextBattleStageId(combat.Config.Stages, null);
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var strip = NewRect("Strip", _root.transform, new Vector2(16, 16), new Vector2(1856, 76));
            var stripImage = strip.gameObject.AddComponent<Image>();
            stripImage.color = new Color(0f, 0f, 0f, 0.6f);
            stripImage.raycastTarget = false;
            AddButton(strip, font, "Quán", new Vector2(8, 8), 168, () => SceneManager.LoadScene(RestaurantScene));
            AddButton(strip, font, "Gacha", new Vector2(184, 8), 168, () => SceneManager.LoadScene(GachaScene));
            // Qua ví như mọi nguồn thu khác (không chạm file save), nên Changed bắn và Reset Save Data xoá sạch như thường.
            AddButton(strip, font, $"+{MaterialGrant} Material", new Vector2(360, 8), 240,
                () => ServiceLocator.Get<Wallet>().Add(CurrencyType.Material, MaterialGrant));
            AddButton(strip, font, $"Tua AFK +{AfkRewindHours} giờ", new Vector2(608, 8), 330, RewindAfk);
            AddButton(strip, font, "Chiến đấu thử", new Vector2(946, 8), 230, () => StartTrialBattle(combat));
            Text stageLabel = null;
            stageLabel = AddButton(strip, font, StageLabel(combat), new Vector2(1184, 8), 230, () =>
            {
                _trialStageId = NextBattleStageId(combat.Config.Stages, _trialStageId);
                stageLabel.text = StageLabel(combat);
            });
            AddButton(strip, font, "Hồi đầy thể lực", new Vector2(1422, 8), 250, combat.Stamina.Refill);
            AddButton(strip, font, "Màn chơi", new Vector2(1680, 8), 168, () => SceneManager.LoadScene(BattleView.StageSelectScene));
            BuildSettings(font);
        }

        // Qua ví như +Material: không chạm file save, Changed bắn và Reset Save Data xoá sạch như thường.
        public static void GrantGems(Wallet wallet) => wallet.Add(CurrencyType.Gem, GemGrant);

        // Nút Cài đặt ở góc trên phải (dải DEV nằm dưới, đã đầy). Reset luôn qua hộp xác nhận (D-005).
        private static void BuildSettings(Font font)
        {
            var open = AddButton((RectTransform)_root.transform, font, "Cài đặt", Vector2.zero, 200, () => { });
            var openRect = (RectTransform)open.transform.parent;
            openRect.anchorMin = openRect.anchorMax = openRect.pivot = Vector2.one;
            openRect.anchoredPosition = new Vector2(-16, -16);

            // Nền mờ chặn bấm xuyên xuống game khi panel mở.
            var dim = NewRect("SettingsPanel", _root.transform, Vector2.zero, Vector2.zero);
            dim.anchorMin = Vector2.zero;
            dim.anchorMax = Vector2.one;
            dim.offsetMin = dim.offsetMax = Vector2.zero;
            dim.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);

            var box = NewRect("Box", dim, Vector2.zero, new Vector2(900, 560));
            box.anchorMin = box.anchorMax = box.pivot = new Vector2(0.5f, 0.5f);
            box.anchoredPosition = Vector2.zero;
            box.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.16f, 1f);

            AddLabel(box, font, "Cài đặt (DEV)", new Vector2(0, 485), new Vector2(900, 60), 40);
            AddButton(box, font, $"+{GemGrant} Gem", new Vector2(50, 360), 800,
                () => GrantGems(ServiceLocator.Get<Wallet>()));

            var confirm = NewRect("ResetConfirm", box, new Vector2(50, 40), new Vector2(800, 220));
            confirm.gameObject.AddComponent<Image>().color = new Color(0.35f, 0.1f, 0.1f, 1f);
            AddLabel(confirm, font, "Xoá TOÀN BỘ dữ liệu đã lưu và đưa game về như mới?\nKhông thể hoàn tác.",
                new Vector2(20, 110), new Vector2(760, 100), 32);
            var yes = AddButton(confirm, font, "Xoá hết", new Vector2(40, 20), 340, () =>
            {
                Debug.Log($"DevOverlay: Reset Save Data = {ServiceLocator.Get<GameResetter>().Reset()}");
                dim.gameObject.SetActive(false);
            });
            yes.GetComponentInParent<Image>().color = new Color(0.85f, 0.2f, 0.2f);
            AddButton(confirm, font, "Huỷ", new Vector2(420, 20), 340, () => confirm.gameObject.SetActive(false));
            confirm.gameObject.SetActive(false);

            AddButton(box, font, "Reset dữ liệu", new Vector2(50, 270), 800, () => confirm.gameObject.SetActive(true));
            AddButton(box, font, "Đóng", new Vector2(690, 485), 180, () => dim.gameObject.SetActive(false));
            dim.gameObject.SetActive(false);
            open.GetComponentInParent<Button>().onClick.AddListener(() =>
            {
                confirm.gameObject.SetActive(false);
                dim.gameObject.SetActive(true);
            });
        }

        private static void AddLabel(RectTransform parent, Font font, string text, Vector2 position, Vector2 size, int fontSize)
        {
            var label = NewRect("Label", parent, position, size).gameObject.AddComponent<Text>();
            label.font = font;
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
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

        private static void StartTrialBattle(CombatService combat)
        {
            FillTeamIfEmpty(ServiceLocator.Get<Roster>());
            var result = combat.StartBattle(_trialStageId);
            Debug.Log($"DevOverlay: StartBattle({_trialStageId}) = {result.Status}");
            if (result.Status == StartBattleStatus.Success) SceneManager.LoadScene(BattleView.BattleScene);
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
