using BunDauPeo.Combat;
using BunDauPeo.UI;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class CombatViewTextTests
    {
        [Test]
        public void UnitHp_CurrentOverMax()
        {
            Assert.AreEqual("640/800", CombatViewText.UnitHp(640, 800));
            Assert.AreEqual("0/800", CombatViewText.UnitHp(0, 800));
        }

        [Test]
        public void SkillButton_ShowsRemainingCooldown()
        {
            Assert.AreEqual("Skill", CombatViewText.SkillButton(0));
            Assert.AreEqual("Skill (2)", CombatViewText.SkillButton(2));
            Assert.AreEqual("Skill (3)", CombatViewText.SkillButton(3));
        }

        [Test]
        public void UltimateFill_RatioOfEffectiveCost()
        {
            Assert.AreEqual(0.5f, CombatViewText.UltimateFill(50, 100, 100), 1e-6f);
            Assert.AreEqual(1f, CombatViewText.UltimateFill(100, 150, 100), 1e-6f, "cost 150 above max 100: a full bar is enough");
            Assert.AreEqual(0.8f, CombatViewText.UltimateFill(80, 150, 100), 1e-6f, "ratio uses the effective cost 100, not 150");
            Assert.AreEqual(0f, CombatViewText.UltimateFill(0, 100, 100), 1e-6f);
            Assert.AreEqual(1f, CombatViewText.UltimateFill(250, 100, 100), 1e-6f, "clamped at 1");
            Assert.AreEqual(0f, CombatViewText.UltimateFill(-5, 100, 100), 1e-6f, "clamped at 0");
        }

        [Test]
        public void UltimateReadyTag_OnlyWhenReady()
        {
            Assert.AreEqual("OK!", CombatViewText.UltimateReadyTag(true));
            Assert.AreEqual("", CombatViewText.UltimateReadyTag(false));
        }

        [Test]
        public void TurnLine_NamesActor()
        {
            Assert.AreEqual("Lượt của Chả Cốm", CombatViewText.TurnLine("Chả Cốm"));
            Assert.AreEqual("Lượt của Ruồi Bàn", CombatViewText.TurnLine("Ruồi Bàn"));
        }

        [Test]
        public void EndBanner_PerState()
        {
            Assert.AreEqual("CLEAR", CombatViewText.EndBanner(BattleState.Won));
            Assert.AreEqual("Thất bại", CombatViewText.EndBanner(BattleState.Lost));
            Assert.AreEqual("", CombatViewText.EndBanner(BattleState.PlayerTurn));
            Assert.AreEqual("", CombatViewText.EndBanner(BattleState.EnemyTurn));
        }

        [Test]
        public void ActionSeconds_FastIsHalf()
        {
            Assert.AreEqual(0.6f, BattleView.ActionSeconds(false), 1e-6f);
            Assert.AreEqual(0.3f, BattleView.ActionSeconds(true), 1e-6f);
            Assert.AreEqual(BattleView.ActionSeconds(false) / 2f, BattleView.ActionSeconds(true), 1e-6f);
        }

        [Test]
        public void WaveLabel_OneBased()
        {
            Assert.AreEqual("Đợt 1/3", CombatViewText.WaveLabel(1, 3));
            Assert.AreEqual("Đợt 3/3", CombatViewText.WaveLabel(3, 3));
            Assert.AreEqual("Đợt 1/1", CombatViewText.WaveLabel(1, 1));
        }

        [Test]
        public void SpeedLabel_PerFlag()
        {
            Assert.AreEqual("x1", CombatViewText.SpeedLabel(false));
            Assert.AreEqual("x2", CombatViewText.SpeedLabel(true));
        }

        [Test]
        public void PauseHpLine_AliveAndFallen()
        {
            // Quái nhanh hơn nên đánh trước: 160 sát thương làm Nem Chua còn 640/800, rồi 5000 sát thương hạ gục.
            foreach (var (attack, expected) in new[] { (160, "Nem Chua 640/800"), (5000, "Nem Chua: đã ngã") })
            {
                var s = CombatTestData.Battle(new[] { CombatTestData.Hero("Nem Chua", hp: 800, speed: 100) },
                    CombatTestData.Stage("s", new[] { CombatTestData.Enemy("e", 100000, attack, 0, 500) }));
                s.EnemyAct();
                Assert.AreEqual(expected, CombatViewText.PauseHpLine(s.Allies[0]), $"attack {attack}");
            }
        }

        [Test]
        public void SurrenderConfirm_SaysStaminaIsNotRefunded()
        {
            StringAssert.Contains("không được hoàn", CombatViewText.SurrenderConfirm());
            Assert.AreEqual("Bỏ cuộc? Trận này tính thua, thể lực không được hoàn.", CombatViewText.SurrenderConfirm());
        }
    }
}
