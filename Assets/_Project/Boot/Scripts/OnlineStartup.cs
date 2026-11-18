using System;
using System.Threading.Tasks;
using BunDauPeo.Characters;
using BunDauPeo.Combat;
using BunDauPeo.Core;
using BunDauPeo.Gacha;
using BunDauPeo.Restaurant;
using BunDauPeo.Rules;
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
        private readonly GameData _data;
        private readonly CommandGate _gate = new(CommandGate.RealtimeDelay);
        private readonly GameObject _host;

        private bool _signedIn;
        private bool _bootstrapped;
        private bool _saveLoaded;
        private PlayerSave _serverSave;
        private BootstrapResponse _bootstrap;
        private Task<OnlineStatus> _running;
        private RestaurantRunner _runner;

        public OnlineStartup(IGameServer server, ServerTime time, string savePath, CharacterCatalog catalog,
            NewGameConfig newGame, GachaBannerCatalog banners, RestaurantConfig restaurantConfig,
            CombatConfig combatConfig, StarConfig starConfig, GameData data, GameObject host)
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
            // Cổng sống suốt app nên nối một lần ở đây: lệnh game báo state mất cũng về Tiêu đề, như đẩy save.
            _gate.NoState += () => NoState.SafeInvoke();
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _host = host != null ? host : throw new ArgumentNullException(nameof(host));
        }

        public GameSession Session { get; private set; }
        public RestaurantService Restaurant { get; private set; }
        public CloudSync Sync { get; private set; }
        public CommandGate Gate => _gate;
        public bool IsStarted => Session != null;

        public event Action<OnlineStatus> ConnectionProblem;
        public event Action ConnectionRestored;
        public event Action NoState;

        // Ba service sống qua Shutdown: Tiêu đề cần chúng để dựng lại session.
        public void RegisterBase()
        {
            ServiceLocator.Register<IGameStarter>(this);
            ServiceLocator.Register(_server);
            ServiceLocator.Register(_time);
            ServiceLocator.Register(_data);
            ServiceLocator.Register(_gate);
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
            _bootstrap = null;
        }

        private void TearDownGame()
        {
            if (_runner != null)
            {
                // Destroy chờ cuối khung: tắt ngay để quán không ghi bản đệm sau khi Xoá tài khoản đã xoá nó.
                _runner.enabled = false;
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
                if (boot.Value.State == null)
                {
                    Debug.LogError($"{nameof(OnlineStartup)}: bootstrap returned no state, game not started");
                    return OnlineStatus.ServerError;
                }

                _time.Sync(boot.Value.ServerUtcTicks);
                var expected = DataHash.Of(GameDataBuilder.ToJson(_data));
                // Không chặn: server khác bản dữ liệu chỉ lệch số liệu, còn lệnh vẫn chạy đúng luật của server.
                if (boot.Value.DataHash != expected)
                    Debug.LogWarning($"{nameof(OnlineStartup)}: server data hash {boot.Value.DataHash} differs from this build {expected}");
                _bootstrap = boot.Value;
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

        // Xoá tài khoản (spec 5.8): server xoá dữ liệu và tài khoản trước; lỗi thì không xoá gì trên máy và đẩy lại save.
        private async Task<OnlineStatus> DeleteAccountAsync(CloudSync sync)
        {
            await sync.StopAsync();
            var status = await _server.DeleteAccountAsync();
            if (status != OnlineStatus.Ok)
            {
                // UGS xoá dữ liệu rồi mới xoá tài khoản: bước sau lỗi thì server có thể đã trống, nên đẩy lại save trong bộ nhớ ngay.
                sync.Resume();
                sync.MarkDirty();
                return status;
            }

            _signedIn = false;
            Shutdown();
            // Tài khoản đã mất nên bản đệm chỉ còn là rác; xoá lỗi thì lần sau vẫn là game mới (server không có save), chỉ cần log.
            new SaveService(_savePath).DeleteAll();
            // Như cài mới: Tiêu đề tiếng Anh tới khi người chơi chọn (D-012).
            GameLanguage.Current = GameLanguage.Resolve("");
            return OnlineStatus.Ok;
        }

        private void Build()
        {
            var session = new GameSession(new SaveService(_savePath), _catalog, _newGame, _serverSave);
            // Server là bản chính của Gem, nhân vật, mảnh… (spec con 4.3); save đợt 1 còn đội/nhân viên/avatar trỏ tới nhân vật server không biết.
            session.ApplyServerState(_bootstrap.State);
            var pruned = ServerStateApplier.PruneUnowned(session.Save);
            if (pruned) session.Persist();
            ServiceLocator.Register(session.Wallet);
            ServiceLocator.Register(session.Roster);
            session.ApplyLanguage();
            ServiceLocator.Register(new LanguageSwitcher(session.SetLanguage));
            ServiceLocator.Register(new RestaurantBarPref(() => session.Save.RestaurantBarOpen, session.SetRestaurantBarOpen));
            ServiceLocator.Register(new PlayerProfile(() => session.Save.PlayerName, () => session.Save.AvatarCharacterId,
                session.SetPlayerName, session.SetAvatar));
            ServiceLocator.Register(new DevCommands(_server, _gate, session.ApplyServerState));
            ServiceLocator.Register(new GachaService(session.Save, session.Wallet, _catalog, _banners,
                _server, _gate, session.ApplyServerState));

            var restaurant = new RestaurantService(_restaurantConfig, session.Save, session.Wallet, session.Roster,
                new System.Random(), new ServerClock(_time), session.Persist);
            _runner = _host.AddComponent<RestaurantRunner>();
            _runner.Init(restaurant);
            ServiceLocator.Register(restaurant);
            session.ResetCompleted += restaurant.ResetRuntime;

            var stars = new StarService(session.Save, session.Roster, _catalog, _starConfig,
                _server, _gate, session.ApplyServerState);
            ServiceLocator.Register(stars);

            var stamina = new StaminaService(session.Save, _combatConfig, () => _time.UtcNow);
            var progress = new StageProgressService(session.Save, session.Wallet, _combatConfig.Stages, stamina,
                _server, _gate, session.ApplyServerState);
            var combat = new CombatService(_combatConfig, session.Roster, stars, stamina, progress, new System.Random(),
                session.Persist, _server, _gate, session.ApplyServerState);
            ServiceLocator.Register(combat);
            session.ResetCompleted += combat.ResetRuntime;

            var sessionId = _bootstrap.SessionId;
            var sync = new CloudSync(_server, () => ServerStateApplier.ToUploadJson(session.Save), () => session.Save.SaveRevision++,
                () => sessionId);
            session.Persisted += sync.MarkDirty;
            // Game mới đã ghi bản đệm trong constructor, trước khi có người nghe Persisted: tự đánh dấu để server có save.
            if (session.IsNewGame || pruned) sync.MarkDirty();
            sync.Lost += () => ConnectionProblem.SafeInvoke(OnlineStatus.NetworkError);
            sync.Restored += () => ConnectionRestored.SafeInvoke();
            sync.OtherDevice += () => ConnectionProblem.SafeInvoke(OnlineStatus.OtherDevice);
            sync.NoState += () => NoState.SafeInvoke();

            // Hộp Mất kết nối: Thử lại đẩy ngay qua đây.
            ServiceLocator.Register(sync);
            ServiceLocator.Register(new GameResetter(() => session.ResetToDefaultsAsync(_server, sync)));
            ServiceLocator.Register(new AccountDeleter(() => DeleteAccountAsync(sync)));

            Session = session;
            Restaurant = restaurant;
            Sync = sync;
        }
    }
}
