using System.Collections.Generic;
using System.Linq;
using BunDauPeo.UI;
using NUnit.Framework;
using UnityEngine;

namespace BunDauPeo.Tests
{
    public class BattleLayoutTests
    {
        // Lưới 3×3 mỗi phe: cột Front/Mid/Back, hàng 280/520/760; Front và Mid ở hàng trên và dưới, Back ở hàng giữa.
        private static readonly Vector2[] AllyCenters = { new(760, 280), new(760, 760), new(540, 280), new(540, 760), new(320, 520) };
        private static readonly Vector2[] EnemyCenters = { new(1180, 280), new(1180, 760), new(1400, 280), new(1400, 760), new(1620, 520) };

        [Test]
        public void AllyRects_MatchSpecCenters()
        {
            for (var slot = 0; slot < AllyCenters.Length; slot++)
            {
                var rect = BattleLayout.AllyRect(slot);
                Assert.AreEqual(AllyCenters[slot], rect.center, $"slot {slot}");
                Assert.AreEqual(new Vector2(130, 130), rect.size, $"slot {slot}");
            }
        }

        [Test]
        public void EnemyRects_MatchSpecCenters()
        {
            for (var slot = 0; slot < EnemyCenters.Length; slot++)
            {
                var rect = BattleLayout.EnemyRect(slot, false);
                Assert.AreEqual(EnemyCenters[slot], rect.center, $"slot {slot}");
                Assert.AreEqual(BattleLayout.UnitSize, rect.size, $"slot {slot}");
            }
        }

        [Test]
        public void BossRect_OnlyAtSlotZero()
        {
            var boss = BattleLayout.EnemyRect(0, true);
            Assert.AreEqual(new Vector2(1180, 400), boss.center);
            Assert.AreEqual(BattleLayout.BossSize, boss.size);
            Assert.AreEqual(new Vector2(280, 320), boss.size);

            for (var slot = 1; slot < EnemyCenters.Length; slot++)
                Assert.AreEqual(BattleLayout.EnemyRect(slot, false), BattleLayout.EnemyRect(slot, true), $"a boss in slot {slot} is drawn at normal size");
        }

        [Test]
        public void NoTwoRectsOverlap_AnyTeamAndWave()
        {
            // bossSlot -1: không boss; 0: boss ô 0 kèm bốn quái thường; 1-4: cờ boss ở ô khác (vẽ cỡ thường).
            for (var bossSlot = -1; bossSlot < EnemyCenters.Length; bossSlot++)
            {
                var rects = new List<(string Name, Rect Rect)>();
                for (var slot = 0; slot < AllyCenters.Length; slot++) rects.Add(($"ally{slot}", BattleLayout.AllyRect(slot)));
                for (var slot = 0; slot < EnemyCenters.Length; slot++) rects.Add(($"enemy{slot}", BattleLayout.EnemyRect(slot, slot == bossSlot)));

                for (var i = 0; i < rects.Count; i++)
                {
                    for (var j = i + 1; j < rects.Count; j++)
                    {
                        var a = rects[i].Rect;
                        var b = rects[j].Rect;
                        var overlapX = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin);
                        var overlapY = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
                        Assert.IsFalse(overlapX > 1f && overlapY > 1f, $"bossSlot {bossSlot}: {rects[i].Name} overlaps {rects[j].Name}");
                    }
                }
            }
        }

        [Test]
        public void AllRects_InsideCanvas_BelowTopBar()
        {
            var canvas = new Rect(0, 0, 1920, 1080);
            var all = new List<(string Name, Rect Rect)> { ("boss", BattleLayout.EnemyRect(0, true)) };
            for (var slot = 0; slot < AllyCenters.Length; slot++) all.Add(($"ally{slot}", BattleLayout.AllyRect(slot)));
            for (var slot = 0; slot < EnemyCenters.Length; slot++) all.Add(($"enemy{slot}", BattleLayout.EnemyRect(slot, false)));

            foreach (var (name, rect) in all)
            {
                Assert.IsTrue(canvas.Contains(rect.min) && canvas.Contains(rect.max), $"{name} {rect} leaves the canvas");
                // Dòng "Lượt của X" ở góc trên trái kết thúc ở y 200.
                Assert.GreaterOrEqual(rect.yMin, 205f, $"{name} reaches into the top bar");
                // Dưới thân còn tên, số HP và hàng buff (~110 px): không chạm mép dưới màn.
                Assert.LessOrEqual(rect.yMax + 110f, 1080f, $"{name} has no room for its labels");
            }
        }

        // User chốt 2026-11-06: lưới 3×3 mỗi phe, trừ boss; giãn dọc để sau còn chỗ hiện buff/debuff dưới mỗi người.
        [Test]
        public void Grid_ThreeColumnsThreeRowsPerSide_RoomForBuffRowBelowEachUnit()
        {
            foreach (var rects in new[] { Enumerable.Range(0, 5).Select(BattleLayout.AllyRect).ToArray(), Enumerable.Range(0, 5).Select(i => BattleLayout.EnemyRect(i, false)).ToArray() })
            {
                Assert.AreEqual(3, rects.Select(r => r.center.x).Distinct().Count(), "three columns per side");
                CollectionAssert.AreEquivalent(new[] { 280f, 520f, 760f }, rects.Select(r => r.center.y).Distinct().ToArray(), "three rows per side");

                for (var i = 0; i < rects.Length; i++)
                for (var j = 0; j < rects.Length; j++)
                {
                    if (i == j || rects[i].center.x != rects[j].center.x || rects[j].center.y <= rects[i].center.y) continue;
                    Assert.GreaterOrEqual(rects[j].yMin - rects[i].yMax, 110f, $"column x={rects[i].center.x}: no room for name, HP and buff row between units");
                }
            }

            // Hai phe đối xứng quanh giữa màn.
            for (var slot = 0; slot < 5; slot++)
                Assert.AreEqual(1940f - BattleLayout.AllyRect(slot).center.x, BattleLayout.EnemyRect(slot, false).center.x, 1f, $"slot {slot} mirrors");
        }

        [Test]
        public void ToAnchored_CentersOnCanvas()
        {
            Assert.AreEqual(Vector2.zero, BattleLayout.ToAnchored(new Vector2(960, 540)));
            Assert.AreEqual(new Vector2(-200, 180), BattleLayout.ToAnchored(new Vector2(760, 360)));
        }
    }
}
