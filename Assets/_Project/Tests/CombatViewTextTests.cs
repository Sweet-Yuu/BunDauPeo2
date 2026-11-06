using BunDauPeo.Characters;
using BunDauPeo.Combat;
using BunDauPeo.Core;
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
        public void SurrenderConfirm_SaysStaminaIsNotRefunded()
        {
            StringAssert.Contains("không được hoàn", CombatViewText.SurrenderConfirm());
            Assert.AreEqual("Bỏ cuộc? Trận này tính thua, thể lực không được hoàn.", CombatViewText.SurrenderConfirm());
        }

        [Test]
        public void StaminaCard_ShowsNextTick_RoundsUp()
        {
            Assert.AreEqual("Thể lực 42/100 · +1 sau 4:05", CombatViewText.StaminaCard(42, 100, 244.3));
            Assert.AreEqual("Thể lực 99/100 · +1 sau 0:01", CombatViewText.StaminaCard(99, 100, 0.2));
            Assert.AreEqual("Thể lực 42/100 · +1 sau 4:05", CombatViewText.StaminaCard(42, 100, 245.0), "an exact second is not rounded up again");
            Assert.AreEqual("Thể lực 100/100", CombatViewText.StaminaCard(100, 100, 0), "full bar has no countdown");
        }

        [Test]
        public void StaminaChange_ShowsBeforeAfter()
        {
            Assert.AreEqual("Thể lực 42 → 32", CombatViewText.StaminaChange(42, 10));
            Assert.AreEqual("Thể lực 100 → 75", CombatViewText.StaminaChange(100, 25));
            Assert.AreEqual("Thể lực 10 → 0", CombatViewText.StaminaChange(10, 10));
            Assert.AreEqual("Thể lực 7, cần 10", CombatViewText.StaminaChange(7, 10), "never a negative result");
        }

        [Test]
        public void EnterWarning_SaysSpentUpFrontAndNoRefund()
        {
            var text = CombatViewText.EnterWarning(10);
            StringAssert.Contains("10", text);
            StringAssert.Contains("không hoàn", text);
            StringAssert.Contains("25", CombatViewText.EnterWarning(25), "the cost comes from the argument");
        }

        [Test]
        public void EnterFailedLine_EveryNonEnteredStatus_HasDistinctText()
        {
            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (EnterStageStatus status in System.Enum.GetValues(typeof(EnterStageStatus)))
            {
                if (status == EnterStageStatus.Entered) continue;
                var line = CombatViewText.EnterFailedLine(status);
                Assert.IsNotEmpty(line, $"status = {status}");
                if (status != EnterStageStatus.Unknown && status != EnterStageStatus.Invalid && status != EnterStageStatus.WrongKind)
                    Assert.IsTrue(seen.Add(line), $"status = {status}: text '{line}' is shared");
            }

            StringAssert.Contains("chưa vào trận", CombatViewText.EnterFailedLine(EnterStageStatus.SaveFailed));
            StringAssert.Contains("thể lực", CombatViewText.EnterFailedLine(EnterStageStatus.NotEnoughStamina));
            Assert.AreEqual(CombatViewText.LockedHint(), CombatViewText.EnterFailedLine(EnterStageStatus.Locked));
            Assert.AreEqual(CombatViewText.InvalidConfigLine(), CombatViewText.EnterFailedLine(EnterStageStatus.InvalidConfig));
        }

        [Test]
        public void SecondsUntilAffordable_CountsWholeIntervals()
        {
            Assert.AreEqual(720d, CombatViewText.SecondsUntilAffordable(7, 10, 120, 300), 0.001, "120 s for the next point, then 2 more intervals");
            Assert.AreEqual(120d, CombatViewText.SecondsUntilAffordable(9, 10, 120, 300), 0.001);
            Assert.AreEqual(0d, CombatViewText.SecondsUntilAffordable(10, 10, 120, 300));
            Assert.AreEqual(0d, CombatViewText.SecondsUntilAffordable(80, 10, 0, 300));
        }

        [Test]
        public void WaitLine_MinutesAndHours()
        {
            Assert.AreEqual("Đủ thể lực sau 12:00", CombatViewText.WaitLine(720));
            Assert.AreEqual("Đủ thể lực sau 1:15:00", CombatViewText.WaitLine(4500));
            Assert.AreEqual("Đủ thể lực sau 0:01", CombatViewText.WaitLine(0.4));
        }

        [Test]
        public void RewardHeader_FirstClearOrRepeat()
        {
            Assert.AreEqual("Thưởng lần đầu", CombatViewText.RewardHeader(true));
            Assert.AreEqual("Thưởng", CombatViewText.RewardHeader(false));
        }

        [Test]
        public void NodeLabel_BattleAndStory()
        {
            var battle = CombatTestData.Stage("b", new[] { CombatTestData.Enemy() });
            TestData.Edit(battle, so => so.FindProperty("_displayName").stringValue = "1-1");
            Assert.AreEqual("1-1", CombatViewText.NodeLabel(battle));
            var story = CombatTestData.Story("s");
            TestData.Edit(story, so => so.FindProperty("_displayName").stringValue = "Cốt truyện 1");
            Assert.AreEqual("Truyện", CombatViewText.NodeLabel(story));
        }

        [Test]
        public void StoryEmpty_AndLockedHint_Text()
        {
            Assert.AreEqual("Chưa có nội dung cốt truyện", CombatViewText.StoryEmpty());
            Assert.AreEqual("Clear màn trước để mở", CombatViewText.LockedHint());
            Assert.AreEqual("Cấu hình trận lỗi", CombatViewText.InvalidConfigLine());
        }

        [Test]
        public void ConfirmSmallTeam_ShowsCount()
        {
            Assert.AreEqual("Đội mới có 3 người, vào trận?", CombatViewText.ConfirmSmallTeam(3));
            Assert.AreEqual("Đội mới có 1 người, vào trận?", CombatViewText.ConfirmSmallTeam(1));
        }

        [Test]
        public void ConfirmFromStaff_NamesRole()
        {
            Assert.AreEqual("Nem Chua đang làm Đầu bếp, chuyển vào đội chiến đấu?", CombatViewText.ConfirmFromStaff("Nem Chua", StaffRole.Cook));
            StringAssert.Contains("Thu ngân", CombatViewText.ConfirmFromStaff("x", StaffRole.Cashier));
            StringAssert.Contains("Phục vụ", CombatViewText.ConfirmFromStaff("x", StaffRole.Server));
        }

        [Test]
        public void StaffTag_AndClassName()
        {
            Assert.AreEqual("Quán: Đầu bếp", CombatViewText.StaffTag(StaffRole.Cook));
            Assert.AreEqual("Quán: Thu ngân", CombatViewText.StaffTag(StaffRole.Cashier));
            Assert.AreEqual("Đỡ đòn", CombatViewText.ClassName(CharacterClass.Tank));
            Assert.AreEqual("Đấu sĩ", CombatViewText.ClassName(CharacterClass.Fighter));
            Assert.AreEqual("Hỗ trợ", CombatViewText.ClassName(CharacterClass.Support));
            Assert.AreEqual("Xạ thủ", CombatViewText.ClassName(CharacterClass.Marksman));
            Assert.AreEqual("Pháp sư", CombatViewText.ClassName(CharacterClass.Mage));
            Assert.AreEqual("", CombatViewText.ClassName(CharacterClass.Unset));
            Assert.AreEqual("Hàng trước", CombatViewText.PositionName(BattlePosition.Front));
            Assert.AreEqual("Hàng giữa", CombatViewText.PositionName(BattlePosition.Mid));
            Assert.AreEqual("Hàng sau", CombatViewText.PositionName(BattlePosition.Back));
            Assert.AreEqual("Đội đã đủ 5 người", CombatViewText.TeamFull());
        }

        [Test]
        public void CurrencyColor_DistinctPerCurrency()
        {
            var coin = CombatViewText.CurrencyColor(CurrencyType.Coin);
            var material = CombatViewText.CurrencyColor(CurrencyType.Material);
            var gem = CombatViewText.CurrencyColor(CurrencyType.Gem);
            Assert.AreNotEqual(coin, material);
            Assert.AreNotEqual(coin, gem);
            Assert.AreNotEqual(material, gem);
        }

        [Test]
        public void CurrencyCell_ShowsAmountAndName()
        {
            Assert.AreEqual("30 Material", CombatViewText.CurrencyCell(new CurrencyAmount(CurrencyType.Material, 30)));
            Assert.AreEqual("150 Gem", CombatViewText.CurrencyCell(new CurrencyAmount(CurrencyType.Gem, 150)));
            Assert.AreEqual("500 Coin", CombatViewText.CurrencyCell(new CurrencyAmount(CurrencyType.Coin, 500)));
        }

        [Test]
        public void NoRewardLine_TrialOnlyForNotPaid()
        {
            Assert.AreEqual(CombatViewText.TrialLine(), CombatViewText.NoRewardLine(StageCompleteStatus.NotPaid));
            foreach (StageCompleteStatus status in System.Enum.GetValues(typeof(StageCompleteStatus)))
            {
                if (status == StageCompleteStatus.Success || status == StageCompleteStatus.NotPaid) continue;
                var line = CombatViewText.NoRewardLine(status);
                Assert.IsNotEmpty(line, $"status = {status}");
                Assert.AreNotEqual(CombatViewText.TrialLine(), line, $"status = {status}: a paid battle is not a trial");
            }
        }

        [Test]
        public void EnterFailedLine_ForCanEnterStatus_MatchesEnterStageStatus()
        {
            foreach (StageEnterStatus status in System.Enum.GetValues(typeof(StageEnterStatus)))
            {
                if (status == StageEnterStatus.Ready) continue;
                var expected = (EnterStageStatus)System.Enum.Parse(typeof(EnterStageStatus), status.ToString());
                Assert.AreEqual(CombatViewText.EnterFailedLine(expected), CombatViewText.EnterFailedLine(status), $"status = {status}");
            }
        }

        [Test]
        public void StaminaUsed_Text()
        {
            Assert.AreEqual("Thể lực đã dùng: 10", CombatViewText.StaminaUsed(10));
            Assert.AreEqual("Thể lực đã dùng: 25", CombatViewText.StaminaUsed(25));
        }

        [Test]
        public void TrialAndFailLines_Text()
        {
            Assert.AreEqual("Trận thử, không có thưởng", CombatViewText.TrialLine());
            Assert.AreEqual("Thử vào đủ 5 người, hoặc đánh lại màn trước để lấy Gem", CombatViewText.FailHint());
            Assert.AreEqual("Đã trừ 10 thể lực khi vào, không hoàn", CombatViewText.FailStamina(10));
            StringAssert.Contains("10", CombatViewText.FailStamina(10));
            StringAssert.Contains("không hoàn", CombatViewText.FailStamina(10));
            StringAssert.Contains("25", CombatViewText.FailStamina(25));
        }
    }
}
