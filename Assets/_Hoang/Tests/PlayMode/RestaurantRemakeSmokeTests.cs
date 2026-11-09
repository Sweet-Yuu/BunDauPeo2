#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using BunDauPeo.Core;
using BunDauPeo.Playtest;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Hoang.Remake.Tests
{
    public sealed class RestaurantRemakeSmokeTests
    {
        private string _directory;
        private Scene _scene;
        private bool _refused;
        private const string ScenePath = "Assets/_Hoang/Scenes/RestaurantSystems.unity";

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _refused = RemakeBootstrap.Active != null || UnityEngine.Object.FindFirstObjectByType<BunDauPeo.Boot.GameBootstrap>() != null;
            Assert.That(_refused, Is.False, "Close the Editor and run this test with unity test --mode PlayMode to protect the real save.");
            _directory = Path.Combine(Path.GetTempPath(), "HoangSmoke-" + Guid.NewGuid());
            ServiceLocator.Register(new PreviousSessionMarker());
            RemakeBootstrap.SavePathOverride = Path.Combine(_directory, "save.json");
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Additive));
            _scene = SceneManager.GetSceneByPath(ScenePath);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_refused) yield break;
            if (_scene.IsValid() && _scene.isLoaded) yield return SceneManager.UnloadSceneAsync(_scene);
            RemakeBootstrap.SavePathOverride = null;
            UITooltipSystem.HideTooltip();
            if (_directory != null && Directory.Exists(_directory)) Directory.Delete(_directory, true);
        }

        [UnityTest]
        public IEnumerator SceneUI_DailyShopSkillsPowerRestockResetAndTypedReload()
        {
            var bootstrap = RemakeBootstrap.Active;
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(bootstrap.Context, Is.Not.Null);
            Assert.Throws<InvalidOperationException>(() => ServiceLocator.Get<PreviousSessionMarker>());
            var shell = _scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<RemakeShell>(true)).Single();
            var context = bootstrap.Context;
            var initialGold = context.Wallet.Get(CurrencyType.Coin);
            var initialDiamond = context.Wallet.Get(CurrencyType.Gem);
            Assert.That(shell.SkillTree.gameObject.activeInHierarchy, Is.True);
            shell.ShowPage(2);
            yield return null;
            var claim = Button(shell.Shop.transform, "ClaimRewards");
            Assert.That(claim.interactable, Is.True);
            claim.onClick.Invoke();
            Assert.That(context.Wallet.Get(CurrencyType.Coin), Is.EqualTo(initialGold + 100));
            Assert.That(context.Wallet.Get(CurrencyType.Gem), Is.EqualTo(initialDiamond + 100));
            Assert.That(claim.interactable, Is.False);
            claim.onClick.Invoke();
            Assert.That(context.Wallet.Get(CurrencyType.Coin), Is.EqualTo(initialGold + 100));
            Button(shell.Shop.transform, "WatchAd").onClick.Invoke();
            Button(shell.Shop.transform, "CompleteAd").onClick.Invoke();
            Button(shell.Shop.transform, "CompleteAd").onClick.Invoke();
            Assert.That(context.Wallet.Get(CurrencyType.Gem), Is.EqualTo(initialDiamond + 150));
            var buy = shell.Shop.GetComponentsInChildren<Button>(true).First(button => button.name == "BuyGold");
            buy.onClick.Invoke();
            Assert.That(context.Wallet.Get(CurrencyType.Coin), Is.EqualTo(initialGold + 1100));

            shell.ShowPage(0);
            Button(shell.SkillTree.transform, "Skill_cooking").onClick.Invoke();
            Assert.That(bootstrap.Skills.GetLevel("cooking"), Is.EqualTo(1));
            shell.ShowPage(1);
            yield return null;
            var grid = shell.FacilityGrid;
            grid.Select("generator");
            grid.PlaceAt(GridPoint(grid.GridRoot, 0, 0), null);
            grid.Select("stove");
            grid.PlaceAt(GridPoint(grid.GridRoot, 2, 0), null);
            Assert.That(grid.PlacedViewCount, Is.EqualTo(2));
            Assert.That(bootstrap.Restaurant.Profile.CookingSpeedMultiplier, Is.EqualTo(1.25f).Within(0.0001f));
            Assert.That(bootstrap.Facilities.CurrentPowerUsed, Is.EqualTo(35));
            Assert.That(bootstrap.Skills.TryUpgrade("cooking"), Is.EqualTo(SkillUpgradeStatus.Success));
            Assert.That(bootstrap.Skills.TryUpgrade("ingredients"), Is.EqualTo(SkillUpgradeStatus.Success));
            Assert.That(bootstrap.Restaurant.IngredientPackPrice, Is.EqualTo(96));
            var beforeRestock = context.Wallet.Get(CurrencyType.Coin);
            Assert.That(bootstrap.Restaurant.BuyIngredients(), Is.True);
            Assert.That(context.Wallet.Get(CurrencyType.Coin), Is.EqualTo(beforeRestock - 96));
            Assert.That(context.Wallet.Get(CurrencyType.Material), Is.EqualTo(10));
            var loaded = new RemakeSaveStore(RemakeBootstrap.SavePath).Load();
            Assert.That(loaded.ShopDaily.DailyRewardDay, Is.GreaterThan(0));
            Assert.That(loaded.Facilities.PlacedItems.Count, Is.EqualTo(2));
            Assert.That(loaded.Skills.Slots.Single(slot => slot.SkillId == "cooking").CurrentLevel, Is.EqualTo(2));

            var shots = Path.GetFullPath(Path.Combine(Application.dataPath, "_Hoang", "Validation~", "shots"));
            for (var page = 0; page < 3; page++)
            {
                shell.ShowPage(page);
                yield return null;
                foreach (var art in shell.GetComponentsInChildren<Image>(true).Where(image => image.name.Contains("Artwork") || image.name == "RestaurantEmblem"))
                {
                    Assert.That(art.sprite, Is.Not.Null, art.name);
                    Assert.That(art.raycastTarget, Is.False, art.name + " must not steal pointer events");
                }
                Assert.That(ScreenCapture1080.Capture("remake-" + page + ".png", shots), Does.Not.StartWith("ERROR:"));
            }
            var goldBeforeCancel = context.Wallet.Get(CurrencyType.Coin);
            Button(shell.transform, "SettingsButton").onClick.Invoke();
            Button(shell.transform, "RequestReset").onClick.Invoke();
            Assert.That(context.Wallet.Get(CurrencyType.Coin), Is.EqualTo(goldBeforeCancel));
            Button(shell.transform, "CancelReset").onClick.Invoke();
            Assert.That(bootstrap.Skills.GetLevel("cooking"), Is.EqualTo(2));
            Button(shell.transform, "RequestReset").onClick.Invoke();
            Button(shell.transform, "ConfirmReset").onClick.Invoke();
            Assert.That(context.Save.ShopDaily.DailyRewardDay, Is.Zero);
            Assert.That(bootstrap.Skills.GetLevel("cooking"), Is.Zero);
            Assert.That(bootstrap.Facilities.Grid.PlacedItems, Is.Empty);
            Assert.That(context.Wallet.Get(CurrencyType.Coin), Is.EqualTo(initialGold));
            Assert.That(bootstrap.Restaurant.Profile.CookingSpeedMultiplier, Is.EqualTo(1));
        }

        private static Button Button(Transform root, string name) => root.GetComponentsInChildren<Button>(true).Single(button => button.name == name);
        private static Vector2 GridPoint(RectTransform grid, int x, int y)
        {
            var rect = grid.rect;
            var local = new Vector3(rect.xMin + (x + 0.5f) / 11 * rect.width, rect.yMin + (y + 0.5f) / 11 * rect.height);
            return RectTransformUtility.WorldToScreenPoint(null, grid.TransformPoint(local));
        }
        private sealed class PreviousSessionMarker { }
    }

}
#endif
