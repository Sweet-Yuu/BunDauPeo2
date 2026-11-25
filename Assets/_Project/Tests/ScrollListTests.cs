using System.Linq;
using BunDauPeo.EditorTools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.Tests
{
    // L-045: mọi danh sách cuộn của Lobby nhận kéo và lăn chuột từ bất kỳ điểm nào trong khung (đọc lại từ file scene, L-002).
    public class ScrollListTests
    {
        [Test]
        public void EveryListScroll_HasRaycastableSurface_AndWheelSensitivity()
        {
            RestaurantSceneTests.WithScene(ProjectSetup.RestaurantScenePath, scene =>
            {
                var lists = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<ScrollRect>(true))
                    .Where(s => s.name != "WorldScroll").ToList();
                // Hộp Lịch sử Gacha, lưới avatar, danh sách Nhân viên, trang Nhiệm vụ: nếu thiếu thì thêm vào đây là việc của builder, không phải của test.
                Assert.GreaterOrEqual(lists.Count, 4, "expected the four list scrolls");
                foreach (var scroll in lists)
                {
                    var surface = scroll.GetComponent<Graphic>();
                    Assert.IsNotNull(surface, $"{scroll.name}: no Graphic on the ScrollRect, drags on empty space and rows are lost");
                    Assert.IsTrue(surface.raycastTarget, $"{scroll.name}: surface must be a raycast target");
                    Assert.IsFalse(surface.canvasRenderer.cullTransparentMesh, $"{scroll.name}: a culled transparent mesh is skipped by raycasts");
                    Assert.Greater(surface.color.a, 0f, $"{scroll.name}: alpha 0 is culled");
                    Assert.GreaterOrEqual(scroll.scrollSensitivity, 20f, $"{scroll.name}: wheel ticks are tiny with the Input System");
                }
            });
        }
    }
}
