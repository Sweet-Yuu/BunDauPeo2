using System;
using BunDauPeo.Characters;
using BunDauPeo.UI;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class AvatarRuleTests
    {
        [Test]
        public void Resolve_AndInitial()
        {
            var an = TestData.Character("an");
            TestData.Edit(an, so => so.FindProperty("_displayName").stringValue = "an");
            Func<string, CharacterDefinition> lookup = id => id == "an" ? an : null;
            Func<string, bool> owned = _ => true;
            Func<string, bool> notOwned = _ => false;

            Assert.IsNull(AvatarRule.Resolve("", owned, lookup), "empty id");
            Assert.IsNull(AvatarRule.Resolve(null, owned, lookup), "null id");
            Assert.IsNull(AvatarRule.Resolve("ghost", owned, lookup), "id not in catalog");
            Assert.IsNull(AvatarRule.Resolve("an", notOwned, lookup), "not owned");
            Assert.AreSame(an, AvatarRule.Resolve("an", owned, lookup));

            Assert.AreEqual("A", AvatarRule.Initial(an));
            Assert.AreEqual("", AvatarRule.Initial(null));
        }
    }
}
