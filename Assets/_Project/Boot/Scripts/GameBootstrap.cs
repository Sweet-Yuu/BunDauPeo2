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
        [SerializeField] private StarConfig _starConfig;
        [SerializeField] private string _startScene;

        // Chỉ bản khởi động thành công mới là "active"; nạp lại scene Boot sẽ sinh bản thứ hai và hai session cùng ghi đè save của nhau.
        private static GameBootstrap _active;

        private OnlineStartup _startup;

        // Dùng chung cho game và công cụ Editor để cả hai luôn trỏ tới cùng một file save.
        public static string SavePath => SavePathOverride ?? Path.Combine(Application.persistentDataPath, "save.json");

        // Không đặt lại trong ResetStatics: tắt domain reload thì giá trị sống tiếp, nên người đặt phải tự trả về null.
        public static string SavePathOverride { get; set; }

        // Test đặt server giả ở đây và tự trả về null như SavePathOverride; null = Boot tự chọn server.
        public static System.Func<IGameServer> ServerFactory { get; set; }

        // Menu Tools/BunDauPeo/Online Mode ghi vào đây (EditorPrefs); bản build bỏ qua.
        public static bool UseUgsInEditor { get; set; }

        // null cho tới khi Tiêu đề dựng xong session từ server.
        public GameSession Session => _startup?.Session;

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

            if (_starConfig == null)
            {
                Debug.LogError($"{nameof(GameBootstrap)}: field {nameof(_starConfig)} is not assigned.", this);
                return;
            }

            if (_active != null && _active != this)
            {
                Destroy(gameObject);
                return;
            }

            // Xoá service của lần Play trước; session chỉ dựng sau khi đăng nhập (Tiêu đề gọi IGameStarter.StartAsync).
            ServiceLocator.Clear();
            var cached = ReadCache();
            GameLanguage.Current = GameLanguage.Resolve(cached?.Language ?? "");

            var server = ServerFactory != null
                ? ServerFactory()
                : ServerChoice.Create(Application.isEditor, UseUgsInEditor, Debug.isDebugBuild);
            if (server == null)
            {
                // Editor chế độ Local: bản đệm làm dữ liệu "server" để tiến độ còn qua các lần Play.
                var local = new LocalGameServer();
                if (cached != null) local.Seed(JsonUtility.ToJson(cached));
                server = local;
            }

            _startup = new OnlineStartup(server, new ServerTime(), SavePath, _catalog, _newGameConfig, _bannerCatalog,
                _restaurantConfig, _combatConfig, _starConfig, gameObject);
            _startup.RegisterBase();

            _active = this;
            DontDestroyOnLoad(gameObject);

            // Scene khởi đầu do dữ liệu quyết định để spec hub sau này đổi scene mà không sửa code.
            if (!string.IsNullOrEmpty(_startScene)) SceneManager.LoadScene(_startScene);
        }

        // Bản đệm chỉ để hiện ngôn ngữ đã lưu ở Tiêu đề; đọc lỗi thì tiếng Anh, không chặn khởi động.
        private PlayerSave ReadCache()
        {
            try
            {
                return new SaveService(SavePath).Load();
            }
            catch (System.Exception e) when (e is IOException || e is System.UnauthorizedAccessException)
            {
                Debug.LogWarning($"{nameof(GameBootstrap)}: cannot read save cache: {e.Message}", this);
                return null;
            }
        }

        private void Update() => _startup?.Sync?.Tick(Time.unscaledDeltaTime);

        // Heartbeat TRƯỚC Persist, gọi tường minh vì Unity không đảm bảo thứ tự giữa các MonoBehaviour (spec 10).
        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveAndFlush();
            else _startup?.Restaurant?.OnAppResume();
        }

        private void OnApplicationQuit() => SaveAndFlush();

        // Đẩy ngay, không đợi gộp lượt: hệ điều hành có thể giết app sau khi vào nền (spec 5.5).
        private void SaveAndFlush()
        {
            _startup?.Restaurant?.OnAppPause();
            _startup?.Session?.Persist();
            _ = _startup?.Sync?.FlushAsync();
        }
    }
}
