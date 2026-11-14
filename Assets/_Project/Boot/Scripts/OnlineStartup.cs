using System;
using System.Threading.Tasks;
using BunDauPeo.Characters;
using BunDauPeo.Combat;
using BunDauPeo.Core;
using BunDauPeo.Gacha;
using BunDauPeo.Restaurant;
using UnityEngine;

namespace BunDauPeo.Boot
{
    // Đăng nhập → Bootstrap (giờ server) → tải save → dựng session → đăng ký service (spec 5.4).
    // Mỗi bước thành công mới ghi vào trường của nó, nên Thử lại chạy tiếp từ bước lỗi và lệnh lỗi không đổi gì.
    public sealed class OnlineStartup : IGameStarter
    {
        private readonly IGameServer _server;
        private readonly ServerTime _time;
        private readonly string _savePath;
        private readonly CharacterCatalog _catalog;
        private readonly NewGameConfig _newGame;
        private readonly GachaBannerCatalog _banners;
        private readonly RestaurantConfig _restaurantConfig;
        private readonly CombatConfig _combatConfig;
        private readonly StarConfig _starConfig;
        private readonly GameObject _host;

        private bool _signedIn;
        private bool _bootstrapped;
        private bool _saveLoaded;
        private PlayerSave _serverSave;
        private Task<OnlineStatus> _running;
        private RestaurantRunner _runner;

        public OnlineStartup(IGameServer server, ServerTime time, string savePath, CharacterCatalog catalog,
            NewGameConfig newGame, GachaBannerCatalog banners, RestaurantConfig restaurantConfig,
            CombatConfig combatConfig, StarConfig starConfig, GameObject host)
        {
            _server = server ?? throw new ArgumentNullException(nameof(server));
            _time = time ?? throw new ArgumentNullException(nameof(time));
            _savePath = savePath ?? throw new ArgumentNullException(nameof(savePath));
            _catalog = catalog;
            _newGame = newGame;
            _banners = banners;
            _restaurantConfig = restaurantConfig;
            _combatConfig = combatConfig;
            _starConfig = starConfig;
            _host = host != null ? host : throw new ArgumentNullException(nameof(host));
        }

        public GameSession Session { get; private set; }
        public RestaurantService Restaurant { get; private set; }
        public CloudSync Sync { get; private set; }
        public bool IsStarted => Session != null;

        // Ba service sống qua Shutdown: Tiêu đề cần chúng để dựng lại session.
        public void RegisterBase()
        {
            ServiceLocator.Register<IGameStarter>(this);
            ServiceLocator.Register(_server);
            ServiceLocator.Register(_time);
        }

        public Task<OnlineStatus> StartAsync()
        {
            if (IsStarted) return Task.FromResult(OnlineStatus.Ok);
            if (_running == null || _running.IsCompleted) _running = RunAsync();
            return _running;
        }

        // Đăng nhập giữ nguyên (cùng người chơi); Bootstrap và tải save chạy lại để lấy bản mới trên server.
        public void Shutdown()
        {
            TearDownGame();
            _bootstrapped = false;
            _saveLoaded = false;
            _serverSave = null;
        }

        private void TearDownGame()
        {
            if (_runner != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(_runner);
                else UnityEngine.Object.DestroyImmediate(_runner);
            }

            // Session cũ bị bỏ cùng các handler đã gắn vào nó; ResetCompleted không còn ai bắn.
            Sync?.Stop();
            _runner = null;
            Session = null;
            Restaurant = null;
            Sync = null;
            ServiceLocator.Clear();
            RegisterBase();
        }

        // JSON hỏng không phải "chưa có save": tạo game mới lúc đó sẽ đè save thật trên server.
        public static bool TryParseSave(string json, out PlayerSave save)
        {
            save = null;
            if (string.IsNullOrWhiteSpace(json)) return false;
            try
            {
                save = JsonUtility.FromJson<PlayerSave>(json);
            }
            catch (ArgumentException)
            {
                return false;
            }

            return save != null;
        }

        private async Task<OnlineStatus> RunAsync()
        {
            if (!_signedIn)
            {
                var status = await _server.SignInAsync();
                if (status != OnlineStatus.Ok) return status;
                _signedIn = true;
            }

            if (!_bootstrapped)
            {
                var boot = await _server.BootstrapAsync();
                if (!boot.Ok) return boot.Status;
                _time.Sync(boot.Value.ServerUtcTicks);
                _bootstrapped = true;
            }

            if (!_saveLoaded)
            {
                var load = await _server.LoadSaveAsync();
                if (!load.Ok) return load.Status;
                PlayerSave save = null;
                if (load.Value != null && !TryParseSave(load.Value, out save))
                {
                    Debug.LogError($"{nameof(OnlineStartup)}: server save is not valid JSON, game not started");
                    return OnlineStatus.ServerError;
                }

                _serverSave = save;
                _saveLoaded = true;
            }

            try
            {
                Build();
            }
            catch (Exception e)
            {
                // Không để nửa số service đã đăng ký; lần Thử lại dựng lại từ bản đã tải.
                Debug.LogException(e);
                TearDownGame();
                return OnlineStatus.ServerError;
            }

            return OnlineStatus.Ok;
        }

        private void Build()
        {
            var session = new GameSession(new SaveService(_savePath), _catalog, _newGame, _serverSave);
            ServiceLocator.Register(session.Wallet);
            ServiceLocator.Register(session.Roster);
            ServiceLocator.Register(new GameResetter(session.ResetToDefaults));
            session.ApplyLanguage();
            ServiceLocator.Register(new LanguageSwitcher(session.SetLanguage));
            ServiceLocator.Register(new RestaurantBarPref(() => session.Save.RestaurantBarOpen, session.SetRestaurantBarOpen));
            ServiceLocator.Register(new PlayerProfile(() => session.Save.PlayerName, () => session.Save.AvatarCharacterId,
                session.SetPlayerName, session.SetAvatar));
            ServiceLocator.Register(new GachaService(session.Save, session.Wallet, session.Roster, _catalog,
                _banners, new System.Random(), session.Persist));

            var restaurant = new RestaurantService(_restaurantConfig, session.Save, session.Wallet, session.Roster,
                new System.Random(), new ServerClock(_time), session.Persist);
            _runner = _host.AddComponent<RestaurantRunner>();
            _runner.Init(restaurant);
            ServiceLocator.Register(restaurant);
            session.ResetCompleted += restaurant.ResetRuntime;

            var stars = new StarService(session.Save, session.Roster, _catalog, _starConfig, session.Persist);
            ServiceLocator.Register(stars);

            var stamina = new StaminaService(session.Save, _combatConfig, () => _time.UtcNow);
            var progress = new StageProgressService(session.Save, session.Wallet, _combatConfig.Stages, stamina, session.Persist);
            var combat = new CombatService(_combatConfig, session.Roster, stars, stamina, progress, new System.Random(), session.Persist);
            ServiceLocator.Register(combat);
            session.ResetCompleted += combat.ResetRuntime;

            var sync = new CloudSync(_server, () => JsonUtility.ToJson(session.Save), () => session.Save.SaveRevision++);
            session.Persisted += sync.MarkDirty;
            // Game mới đã ghi bản đệm trong constructor, trước khi có người nghe Persisted: tự đánh dấu để server có save.
            if (session.IsNewGame) sync.MarkDirty();

            Session = session;
            Restaurant = restaurant;
            Sync = sync;
        }
    }
}
