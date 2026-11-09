using System.Linq;
using BunDauPeo.EditorTools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.Tests
{
    public sealed class RestaurantLobbyArtTests
    {
        [Test]
        public void Lobby_ArtworkIsAssigned_AndDoesNotBlockInput() =>
            RestaurantSceneTests.WithScene(ProjectSetup.RestaurantScenePath, scene =>
            {
                var canvas = RestaurantSceneTests.All<Canvas>(scene).Single(c => !c.overrideSorting);
                var background = canvas.transform.Find("Background").GetComponent<Image>();
                Assert.That(background.sprite, Is.Not.Null, "Lobby must use its illustrated courtyard.");
                Assert.That(background.raycastTarget, Is.False);
                var illustrations = canvas.GetComponentsInChildren<Image>(true)
                    .Where(image => image.name.Contains("Art_") || image.name.StartsWith("CookingStationArt"))
                    .ToArray();
                Assert.That(illustrations.Length, Is.GreaterThanOrEqualTo(20));
                foreach (var image in illustrations)
                {
                    Assert.That(image.sprite, Is.Not.Null, image.name);
                    Assert.That(image.raycastTarget, Is.False, image.name);
                    Assert.That(image.preserveAspect, Is.True, image.name);
                }
            });

        [Test]
        public void ApplyingLobbySkinTwice_DoesNotDuplicateDecorations() =>
            RestaurantSceneTests.WithScene(ProjectSetup.RestaurantScenePath, scene =>
            {
                var canvas = RestaurantSceneTests.All<Canvas>(scene).Single(c => !c.overrideSorting);
                var root = (RectTransform)canvas.transform;
                RestaurantLobbySkin.Apply(root);
                var before = root.GetComponentsInChildren<RectTransform>(true).Length;
                RestaurantLobbySkin.Apply(root);
                Assert.That(root.GetComponentsInChildren<RectTransform>(true).Length, Is.EqualTo(before));
            });
    }
}
