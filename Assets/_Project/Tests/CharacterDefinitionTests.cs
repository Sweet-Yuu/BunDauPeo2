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
    }
}
