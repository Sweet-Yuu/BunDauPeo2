using System.Collections.Generic;
using BunDauPeo.UI;
using NUnit.Framework;
using UnityEngine;

namespace BunDauPeo.Tests
{
    public class BattleLayoutTests
    {
        private static readonly Vector2[] AllyCenters = { new(760, 360), new(760, 600), new(540, 300), new(540, 540), new(320, 450) };
        private static readonly Vector2[] EnemyCenters = { new(1180, 360), new(1180, 600), new(1400, 300), new(1400, 540), new(1620, 450) };

        [Test]
        public void AllyRects_MatchSpecCenters()
        {
            for (var slot = 0; slot < AllyCenters.Length; slot++)
            {
                var rect = BattleLayout.AllyRect(slot);
                Assert.AreEqual(AllyCenters[slot], rect.center, $"slot {slot}");
                Assert.AreEqual(new Vector2(140, 170), rect.size, $"slot {slot}");
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
            Assert.AreEqual(new Vector2(1180, 340), boss.center);
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
        public void AllRects_InsideCanvas_AboveCardArea()
        {
            var canvas = new Rect(0, 0, 1920, 1080);
            var all = new List<(string Name, Rect Rect)> { ("boss", BattleLayout.EnemyRect(0, true)) };
            for (var slot = 0; slot < AllyCenters.Length; slot++) all.Add(($"ally{slot}", BattleLayout.AllyRect(slot)));
            for (var slot = 0; slot < EnemyCenters.Length; slot++) all.Add(($"enemy{slot}", BattleLayout.EnemyRect(slot, false)));

            foreach (var (name, rect) in all)
            {
                Assert.IsTrue(canvas.Contains(rect.min) && canvas.Contains(rect.max), $"{name} {rect} leaves the canvas");
                Assert.LessOrEqual(rect.yMax, BattleLayout.CardAreaTop, $"{name} reaches into the card area");
            }

            Assert.AreEqual(820f, BattleLayout.CardAreaTop);
            Assert.AreEqual(980f, BattleLayout.CardAreaBottom);
        }

        [Test]
        public void ToAnchored_CentersOnCanvas()
        {
            Assert.AreEqual(Vector2.zero, BattleLayout.ToAnchored(new Vector2(960, 540)));
            Assert.AreEqual(new Vector2(-200, 180), BattleLayout.ToAnchored(new Vector2(760, 360)));
        }
    }
}
