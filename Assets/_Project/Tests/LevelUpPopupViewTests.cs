using System.Reflection;
using BunDauPeo.Characters;
using BunDauPeo.Core;
using BunDauPeo.EditorTools;
using BunDauPeo.UI;
using NUnit.Framework;
using UnityEngine;

namespace BunDauPeo.Tests
{
    // Review 1: lên cấp lúc vào trận (scene Lobby bị huỷ để nạp Battle) vẫn phải báo khi về Lobby.
    public class LevelUpPopupViewTests
    {
        private static readonly BindingFlags Any = BindingFlags.NonPublic | BindingFlags.Instance;

        private LevelConfig _config;
        private PlayerSave _save;
        private PlayerLevelService _player;
        private GameObject _canvas;
        private LevelUpPopupView _popup;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<LevelConfig>();
            _save = new PlayerSave();
            var wallet = new Wallet(_save);
            _player = new PlayerLevelService(_save, wallet, _config, () => true);
            ServiceLocator.Clear();
            ServiceLocator.Register(wallet);
            ServiceLocator.Register(_config);
            ServiceLocator.Register(_player);
            _popup = null;
        }

        [TearDown]
        public void TearDown()
        {
            Leave();
            Object.DestroyImmediate(_config);
            ServiceLocator.Clear();
        }

        // "Nạp Lobby": dựng popup mới và bật.
        private void EnterLobby()
        {
            _canvas = new GameObject("TestCanvas", typeof(RectTransform), typeof(Canvas));
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _popup = CharactersSceneBuilder.BuildLevelUpPopup((RectTransform)_canvas.transform, null, font);
            typeof(LevelUpPopupView).GetMethod("OnEnable", Any).Invoke(_popup, null);
        }

        // "Rời Lobby" (sang Battle): tắt và huỷ.
        private void Leave()
        {
            if (_popup == null) return;
            typeof(LevelUpPopupView).GetMethod("OnDisable", Any).Invoke(_popup, null);
            Object.DestroyImmediate(_canvas);
            _popup = null;
        }

        [Test]
        public void LevelUpWhileLeaving_PopupShowsWhenBackInLobby_UntilClosed()
        {
            EnterLobby();
            Assert.IsFalse(_popup.IsOpen, "baseline: nothing to announce");

            _save.PlayerExp = _config.ExpToNext(1);
            Leave(); // lên cấp xảy ra lúc vào trận: scene bị huỷ trước khi người chơi kịp thấy popup
            EnterLobby();

            Assert.IsTrue(_popup.IsOpen, "level-up announced on return");
            _popup.Close();
            Leave();
            EnterLobby();
            Assert.IsFalse(_popup.IsOpen, "closed once, not announced again");
        }

        [Test]
        public void LevelDrop_ClosesPopup()
        {
            EnterLobby();
            _save.PlayerExp = _config.ExpToNext(1);
            ServiceLocator.Get<Wallet>().NotifyChanged();
            Assert.IsTrue(_popup.IsOpen);

            _save.PlayerExp = 0; // Reset
            ServiceLocator.Get<Wallet>().NotifyChanged();

            Assert.IsFalse(_popup.IsOpen);
        }
    }
}
