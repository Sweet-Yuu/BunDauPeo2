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

        private GameSession _session;

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

            DontDestroyOnLoad(gameObject);
            var saveService = new SaveService(Path.Combine(Application.persistentDataPath, "save.json"));
            _session = new GameSession(saveService, _catalog, _newGameConfig);

            ServiceLocator.Clear();
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
