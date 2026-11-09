using BunDauPeo.Characters;
using BunDauPeo.Core;
using BunDauPeo.UI;
using NUnit.Framework;
using UnityEngine;

namespace BunDauPeo.Tests
{
    public class CharacterListRowsTests
    {
        private PlayerSave _save;
        private Roster _roster;
        private StarService _stars;

        [SetUp]
        public void SetUp()
        {
            var a = CombatTestData.Hero("a", hp: 1000, attack: 100, defense: 50, speed: 110);
            var b = CombatTestData.Hero("b");
            var catalog = TestData.Catalog(a, b);
            _save = new PlayerSave();
            _roster = new Roster(_save, catalog);
            _roster.Add("a");
            _roster.Add("b");
            _stars = new StarService(_save, _roster, catalog, ScriptableObject.CreateInstance<StarConfig>(), () => true);
        }

        [Test]
        public void Build_OwnedOrder_AndValues()
        {
            _roster.AddFragments("a", 3);

            var rows = CharacterListRows.Build(_roster, _stars);

            Assert.AreEqual("a", rows[0].Character.Id);
            Assert.AreEqual("b", rows[1].Character.Id);
            Assert.AreEqual(_stars.GetStars("a"), rows[0].Stars);
            Assert.AreEqual(3, rows[0].Fragments);
            Assert.AreEqual(2, rows[0].NextCost);
            Assert.IsTrue(rows[0].CanStarUp);
            Assert.IsFalse(rows[0].IsMax);
            Assert.AreEqual(1000, rows[0].Now.HP);
            Assert.AreEqual(1100, rows[0].Next.HP);
            Assert.AreEqual(110, rows[0].Next.Speed);
        }

        [Test]
        public void Build_ShortOrMax_CannotStarUp()
        {
            _roster.AddFragments("a", 1);
            Assert.IsFalse(CharacterListRows.Build(_roster, _stars)[0].CanStarUp, "1 fragment < cost 2");

            _save.StarUps.Add(new StarUpEntry { CharacterId = "b", Count = 9 });
            _roster.AddFragments("b", 50);
            var max = CharacterListRows.Build(_roster, _stars)[1];
            Assert.AreEqual(5, max.Stars);
            Assert.IsTrue(max.IsMax);
            Assert.AreEqual(0, max.NextCost);
            Assert.IsFalse(max.CanStarUp, "plenty of fragments but already 5 stars");
            Assert.AreEqual(max.Now.HP, max.Next.HP);
        }
    }
}
