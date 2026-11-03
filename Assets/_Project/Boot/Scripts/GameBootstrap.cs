using System.IO;
using BunDauPeo.Characters;
using BunDauPeo.Combat;
using BunDauPeo.Core;
using BunDauPeo.Gacha;
using BunDauPeo.Restaurant;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BunDauPeo.Boot
{
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private CharacterCatalog _catalog;
        [SerializeField] private NewGameConfig _newGameConfig;
        [SerializeField] private GachaBannerCatalog _bannerCatalog;
        [SerializeField] private RestaurantConfig _restaurantConfig;
        [SerializeField] private CombatConfig _combatConfig;
        [SerializeField] private string _startScene;

        // Chỉ bản khởi động thành công mới là "active"; nạp lại scene Boot sẽ sinh bản thứ hai và hai session cùng ghi đè save của nhau.
        private static GameBootstrap _active;

        private GameSession _session;
        private RestaurantService _restaurant;

        // Dùng chung cho game và công cụ Editor để cả hai luôn trỏ tới cùng một file save.
        public static string SavePath => SavePathOverride ?? Path.Combine(Application.persistentDataPath, "save.json");

        // Không đặt lại trong ResetStatics: tắt domain reload thì giá trị sống tiếp, nên người đặt phải tự trả về null.
        public static string SavePathOverride { get; set; }

        public GameSession Session => _session;

        // Tắt domain reload thì static còn nguyên sang lần Play sau.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _active = null;

        private void Awake()
        {
            if (_catalog == null)
            {
                Debug.LogError($"{nameof(GameBootstrap)}: field {nameof(_catalog)} is not assigned.", this);
                return;
            }

            if (_newGameConfig == null)
            {
                Debug.LogError($"{nameof(GameBootstrap)}: field {nameof(_newGameConfig)} is not assigned.", this);
                return;
            }

            if (_bannerCatalog == null)
            {
                Debug.LogError($"{nameof(GameBootstrap)}: field {nameof(_bannerCatalog)} is not assigned.", this);
                return;
            }

            if (_restaurantConfig == null)
            {
                Debug.LogError($"{nameof(GameBootstrap)}: field {nameof(_restaurantConfig)} is not assigned.", this);
                return;
            }

            if (_combatConfig == null)
            {
                Debug.LogError($"{nameof(GameBootstrap)}: field {nameof(_combatConfig)} is not assigned.", this);
                return;
            }

            if (_active != null && _active != this)
            {
                Destroy(gameObject);
                return;
            }

            // Xoá trước khi tạo session: nếu bước sau lỗi thì không để service của lần Play trước còn đó.
            ServiceLocator.Clear();
            var saveService = new SaveService(SavePath);
            try
            {
                _session = new GameSession(saveService, _catalog, _newGameConfig);
            }
            catch (System.Exception e) when (e is IOException || e is System.UnauthorizedAccessException)
            {
                // Không có session thì không đăng ký service và không ghi gì, để save thật còn nguyên cho lần mở sau.
                Debug.LogError($"{nameof(GameBootstrap)}: cannot read save, game not started: {e.Message}", this);
                return;
            }

            _active = this;
            DontDestroyOnLoad(gameObject);
            ServiceLocator.Register(_session.Wallet);
            ServiceLocator.Register(_session.Roster);
            ServiceLocator.Register(new GameResetter(_session.ResetToDefaults));
            ServiceLocator.Register(new GachaService(_session.Save, _session.Wallet, _session.Roster, _catalog,
                _bannerCatalog, new System.Random(), _session.Persist));

            _restaurant = new RestaurantService(_restaurantConfig, _session.Save, _session.Wallet, _session.Roster,
                new System.Random(), new SystemClock(), _session.Persist);
            gameObject.AddComponent<RestaurantRunner>().Init(_restaurant);
            ServiceLocator.Register(_restaurant);
            _session.ResetCompleted += _restaurant.ResetRuntime;

            var stamina = new StaminaService(_session.Save, _combatConfig, () => System.DateTime.UtcNow);
            var progress = new StageProgressService(_session.Save, _session.Wallet, _combatConfig.Stages, stamina, _session.Persist);
            var combat = new CombatService(_combatConfig, _session.Roster, stamina, progress, new System.Random(), _session.Persist);
            ServiceLocator.Register(combat);
            _session.ResetCompleted += combat.ResetRuntime;

            // Scene khởi đầu do dữ liệu quyết định để spec hub sau này đổi scene mà không sửa code.
            if (!string.IsNullOrEmpty(_startScene)) SceneManager.LoadScene(_startScene);
        }

        // Heartbeat TRƯỚC Persist, gọi tường minh vì Unity không đảm bảo thứ tự giữa các MonoBehaviour (spec 10).
        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                _restaurant?.OnAppPause();
                _session?.Persist();
            }
            else
            {
                _restaurant?.OnAppResume();
            }
        }

        private void OnApplicationQuit()
        {
            _restaurant?.OnAppPause();
            _session?.Persist();
        }
    }
}
