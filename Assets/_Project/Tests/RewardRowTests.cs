using BunDauPeo.Core;
using BunDauPeo.EditorTools;
using BunDauPeo.UI;
using NUnit.Framework;
using UnityEngine;

namespace BunDauPeo.Tests
{
    public class RewardRowTests
    {
        private GameObject _canvas;
        private RewardRow _row;

        [SetUp]
        public void SetUp()
        {
            _canvas = new GameObject("TestCanvas", typeof(RectTransform), typeof(Canvas));
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _row = GachaSceneBuilder.BuildRewardRow((RectTransform)_canvas.transform, "Row", font, new Vector2(0, 1), Vector2.zero);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_canvas);

        private static CurrencyAmount[] Lines(params (CurrencyType, long)[] items)
        {
            var result = new CurrencyAmount[items.Length];
            for (var i = 0; i < items.Length; i++) result[i] = new CurrencyAmount(items[i].Item1, items[i].Item2);
            return result;
        }

        private Transform Cell(int i) => _row.transform.Find($"Cell{i}");

        [Test]
        public void RewardRow_Show_TwoLines_TwoCellsWithAmountsAndColors()
        {
            Assert.IsTrue(_row.HasAllReferences());
            _row.Show(Lines((CurrencyType.Material, 30), (CurrencyType.Gem, 150)), "Lần đầu");

            Assert.IsTrue(_row.gameObject.activeSelf);
            Assert.IsTrue(Cell(0).gameObject.activeSelf);
            Assert.IsTrue(Cell(1).gameObject.activeSelf);
            Assert.IsFalse(Cell(2).gameObject.activeSelf);
            Assert.IsFalse(Cell(3).gameObject.activeSelf);
            Assert.AreEqual("150 Gem", Cell(1).GetComponentInChildren<UnityEngine.UI.Text>(true).text);
            Assert.AreEqual(CombatViewText.CurrencyColor(CurrencyType.Material), Cell(0).GetComponent<UnityEngine.UI.Image>().color);
            Assert.AreEqual(CombatViewText.CurrencyColor(CurrencyType.Gem), Cell(1).GetComponent<UnityEngine.UI.Image>().color);
            Assert.AreEqual("Lần đầu", _row.transform.Find("Label").GetComponent<UnityEngine.UI.Text>().text);
        }

        [Test]
        public void RewardRow_Show_FiveLines_ShowsFirstFour()
        {
            _row.Show(Lines((CurrencyType.Coin, 1), (CurrencyType.Gem, 2), (CurrencyType.Material, 3), (CurrencyType.Coin, 4), (CurrencyType.Gem, 5)), "");

            for (var i = 0; i < RewardRow.MaxCells; i++) Assert.IsTrue(Cell(i).gameObject.activeSelf, $"cell {i}");
            Assert.AreEqual("4 Coin", Cell(3).GetComponentInChildren<UnityEngine.UI.Text>(true).text, "fifth line is dropped, not the fourth");
        }

        [Test]
        public void RewardRow_Show_NoLines_OnlyLabel_ThenHideTurnsOff()
        {
            _row.Show(Lines((CurrencyType.Gem, 9)), "x");
            _row.Show(Lines(), "Không có");

            Assert.IsTrue(_row.gameObject.activeSelf);
            for (var i = 0; i < RewardRow.MaxCells; i++) Assert.IsFalse(Cell(i).gameObject.activeSelf, $"cell {i} must clear after an empty Show");

            _row.Hide();
            Assert.IsFalse(_row.gameObject.activeSelf);
        }
    }
}
