using System.Linq;
using BunDauPeo.UI;
using Hoang.Remake.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hoang.Remake.Tests
{
    public sealed class RestaurantArtAssetTests
    {
        [Test]
        public void AllGameplayDefinitionsHaveDistinctIllustrations()
        {
            var skills = AssetDatabase.FindAssets("t:SkillSO", new[] { "Assets/_Hoang/Data/Skills" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<SkillSO>(AssetDatabase.GUIDToAssetPath(guid))).ToArray();
            var facilities = AssetDatabase.FindAssets("t:FacilityItemSO", new[] { "Assets/_Hoang/Data/Facilities" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<FacilityItemSO>(AssetDatabase.GUIDToAssetPath(guid))).ToArray();
            Assert.That(skills.Length, Is.EqualTo(9));
            Assert.That(facilities.Length, Is.EqualTo(7));
            foreach (var skill in skills) Assert.That(skill.SkillIcon, Is.Not.Null, skill.Id);
            foreach (var facility in facilities) Assert.That(facility.Icon, Is.Not.Null, facility.Id);
            Assert.That(skills.Select(skill => skill.SkillIcon).Distinct().Count(), Is.EqualTo(skills.Length));
            Assert.That(facilities.Select(facility => facility.Icon).Distinct().Count(), Is.EqualTo(facilities.Length));
        }

        [Test]
        public void SharedArtHasAllNamedSprites_AndNineSliceFrame()
        {
            foreach (var id in RestaurantArtSetup.SkillFacilityIds.Concat(RestaurantArtSetup.RewardIds))
            {
                var sprite = RestaurantArt.Icon(id);
                Assert.That(sprite, Is.Not.Null, id);
                Assert.That(sprite.rect.width, Is.GreaterThan(128), id);
                Assert.That(sprite.rect.height, Is.GreaterThan(128), id);
            }
            Assert.That(RestaurantArt.Garden, Is.Not.Null);
            var frame = Resources.Load<Sprite>(RestaurantArt.ResourceRoot + "frame");
            Assert.That(frame, Is.Not.Null);
            Assert.That(frame.border, Is.EqualTo(new Vector4(16, 16, 16, 16)));
        }
    }
}
