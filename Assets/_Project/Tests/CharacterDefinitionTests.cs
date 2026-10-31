using BunDauPeo.Characters;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class CharacterDefinitionTests
    {
        [Test]
        public void Validate_Complete_NoErrors() => CollectionAssert.IsEmpty(TestData.Character("a").Validate());

        [Test]
        public void Validate_MissingSkill_Reported()
        {
            var c = TestData.Character("a");
            TestData.SetRef(c, "_ultimate", null);
            Assert.That(c.Validate(), Has.Some.Contains("Ultimate"));
        }

        [Test]
        public void Validate_WrongKind_Reported()
        {
            var c = TestData.Character("a");
            TestData.SetRef(c, "_basicAttack", TestData.Skill(SkillKind.Ultimate));
            Assert.That(c.Validate(), Has.Some.Contains("BasicAttack"));
        }

        [Test]
        public void Validate_StarsOutOfRange_Reported()
        {
            foreach (var stars in new[] { 0, 6 })
            {
                var c = TestData.Character("a");
                TestData.Edit(c, so => so.FindProperty("_stars").intValue = stars);
                Assert.That(c.Validate(), Has.Some.Contains("Stars"), $"stars {stars}");
            }
        }

        [Test]
        public void Validate_StarsInRange_NoStarError()
        {
            foreach (var stars in new[] { 1, 5 })
            {
                var c = TestData.Character("a");
                TestData.Edit(c, so => so.FindProperty("_stars").intValue = stars);
                Assert.That(c.Validate(), Has.None.Contains("Stars"), $"stars {stars}");
            }
        }

        [Test]
        public void Validate_UnsetStars_Reported()
        {
            var c = TestData.Character("a");
            TestData.Edit(c, so => so.FindProperty("_stars").intValue = 0);
            Assert.That(c.Validate(), Has.Some.Contains("Stars"));
        }
    }
}
