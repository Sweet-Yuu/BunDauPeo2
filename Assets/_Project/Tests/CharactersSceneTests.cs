using System.Linq;
using System.Reflection;
using BunDauPeo.Characters;
using BunDauPeo.Core;
using BunDauPeo.EditorTools;
using BunDauPeo.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.Tests
{
    // Panel Nhân vật nằm trong Restaurant.unity; việc nối view và vị trí panel do LobbySceneTests kiểm.
    // Các ca hành vi dựng panel bằng builder trong một canvas tạm; EditMode không tự gọi OnEnable nên gọi qua reflection.
    public class CharactersSceneTests
    {
        private static readonly BindingFlags Any = BindingFlags.NonPublic | BindingFlags.Instance;

        private GameObject _canvas;
        private RectTransform _panel;
        private CharactersView _view;
        private LevelConfig _levelConfig;
        private StarConfig _starConfig;

        [SetUp]
        public void SetUp()
        {
            var catalog = TestData.Catalog(CombatTestData.Hero("a"), CombatTestData.Hero("b"), CombatTestData.Hero("c"));
            var save = new PlayerSave();
            var wallet = new Wallet(save);
            var roster = new Roster(save, catalog);
            foreach (var id in new[] { "a", "b", "c" }) roster.Add(id);
            _levelConfig = ScriptableObject.CreateInstance<LevelConfig>();
            _starConfig = ScriptableObject.CreateInstance<StarConfig>();
            var player = new PlayerLevelService(save, wallet, _levelConfig, () => true);
            ServiceLocator.Clear();
            ServiceLocator.Register(roster);
            ServiceLocator.Register(wallet);
            ServiceLocator.Register(ServerRig.IdleStars(save, roster, catalog, _starConfig));
            ServiceLocator.Register(_starConfig);
            ServiceLocator.Register(_levelConfig);
            ServiceLocator.Register(player);
            ServiceLocator.Register(new CharacterLevelService(save, roster, wallet, catalog, _levelConfig, player, () => true));

            _canvas = new GameObject("TestCanvas", typeof(RectTransform), typeof(Canvas));
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _panel = CharactersSceneBuilder.BuildPanel((RectTransform)_canvas.transform, null, font);
            _panel.gameObject.SetActive(true);
            _view = _panel.GetComponentInChildren<CharactersView>(true);
            typeof(CharactersView).GetMethod("OnEnable", Any).Invoke(_view, null);
        }

        [TearDown]
        public void TearDown()
        {
            typeof(CharactersView).GetMethod("OnDisable", Any).Invoke(_view, null);
            Object.DestroyImmediate(_canvas);
            Object.DestroyImmediate(_levelConfig);
            Object.DestroyImmediate(_starConfig);
            ServiceLocator.Clear();
        }

        private T Find<T>(string name) where T : Component =>
            _panel.GetComponentsInChildren<T>(true).Single(c => c.name == name);

        private GameObject DetailRoot => Find<RectTransform>("DetailRoot").gameObject;
        // Hai Text cùng tên "NameText" (thẻ và panel chi tiết): lấy cái nằm dưới DetailRoot.
        private string DetailNameText() =>
            DetailRoot.GetComponentsInChildren<Text>(true).Single(t => t.name == "NameText").text;

        private void Click(string name) => Find<Button>(name).onClick.Invoke();

        private void ClickCard(int i) => Find<Button>("CharacterCard" + i).onClick.Invoke();

        [Test]
        public void Scene_LocalizedTexts_KeysExistAndNotWrittenByViews() =>
            RestaurantSceneTests.WithScene(ProjectSetup.RestaurantScenePath, SceneTextAsserts.AssertLocalizedTexts);

        [Test]
        public void Scene_ClickingCard_OpensDetailPanelOfThatCharacter()
        {
            Assert.IsFalse(DetailRoot.activeSelf, "starts on the list");

            ClickCard(1);

            Assert.IsTrue(DetailRoot.activeSelf);
            Assert.AreEqual("b", DetailNameText());
        }

        [Test]
        public void Detail_BackButton_ReturnsToList()
        {
            ClickCard(0);
            Click("BackButton");

            Assert.IsFalse(DetailRoot.activeSelf);
        }

        [Test]
        public void Detail_HasLevelAndStarTabs_StarUpButtonOnlyInStarTab()
        {
            ClickCard(0);
            Assert.IsTrue(Find<RectTransform>("LevelGroup").gameObject.activeSelf);
            Assert.IsFalse(Find<Button>("StarUpButton").gameObject.activeInHierarchy, "star up lives in the Star tab");

            Click("StarTabButton");

            Assert.IsFalse(Find<RectTransform>("LevelGroup").gameObject.activeSelf);
            Assert.IsTrue(Find<Button>("StarUpButton").gameObject.activeInHierarchy);
            Assert.IsFalse(Find<Button>("LevelUpButton").gameObject.activeInHierarchy);
        }

        [Test]
        public void Detail_ArrowsSwitchToNeighborCharacter()
        {
            ClickCard(0);

            Click("NextButton");
            Assert.AreEqual("b", DetailNameText());

            Click("PrevButton");
            Click("PrevButton");
            Assert.AreEqual("c", DetailNameText(), "wraps around");
        }
    }
}
