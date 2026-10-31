using System.IO;
using BunDauPeo.Characters;
using BunDauPeo.Core;
using UnityEngine;

namespace BunDauPeo.Boot
{
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private CharacterCatalog _catalog;
        [SerializeField] private NewGameConfig _newGameConfig;

        // Chỉ bản khởi động thành công mới là "active"; nạp lại scene Boot sẽ sinh bản thứ hai và hai session cùng ghi đè save của nhau.
        private static GameBootstrap _active;

        private GameSession _session;

        // Dùng chung cho game và công cụ Editor để cả hai luôn trỏ tới cùng một file save.
        public static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

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
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) _session?.Persist();
        }

        private void OnApplicationQuit() => _session?.Persist();
    }
}
