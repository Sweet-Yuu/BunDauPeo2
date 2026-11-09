using BunDauPeo.EditorTools;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    // Panel Nhân vật nằm trong Restaurant.unity; việc nối view và vị trí panel do LobbySceneTests kiểm.
    public class CharactersSceneTests
    {
        [Test]
        public void Scene_LocalizedTexts_KeysExistAndNotWrittenByViews() =>
            RestaurantSceneTests.WithScene(ProjectSetup.RestaurantScenePath, SceneTextAsserts.AssertLocalizedTexts);
    }
}
