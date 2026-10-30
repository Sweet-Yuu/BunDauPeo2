using BunDauPeo.Characters;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class CatalogTests
    {
        [Test]
        public void TryGet_Found()
        {
            var a = TestData.Character("a");
            Assert.IsTrue(TestData.Catalog(a).TryGet("a", out var found));
            Assert.AreSame(a, found);
        }

        [Test]
        public void TryGet_Missing_ReturnsFalse() => Assert.IsFalse(TestData.Catalog().TryGet("x", out _));

        [Test]
        public void TryGet_NullId_ReturnsFalse() =>
            Assert.IsFalse(TestData.Catalog(TestData.Character("a")).TryGet(null, out _));

        [Test]
        public void Validate_DuplicateId_Reported() =>
            Assert.AreEqual(1, TestData.Catalog(TestData.Character("a"), TestData.Character("a")).Validate().Count);

        [Test]
        public void Validate_BlankId_Reported() =>
            Assert.AreEqual(1, TestData.Catalog(TestData.Character("")).Validate().Count);

        [Test]
        public void NullEntry_DoesNotThrow()
        {
            var catalog = TestData.Catalog(new CharacterDefinition[] { null });
            Assert.IsFalse(catalog.TryGet("a", out _));
            Assert.AreEqual(1, catalog.Validate().Count);
        }
    }
}
