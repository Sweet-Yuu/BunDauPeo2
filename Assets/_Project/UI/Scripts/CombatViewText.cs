using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BunDauPeo.Characters;
using BunDauPeo.Combat;
using BunDauPeo.Core;
using UnityEngine;

namespace BunDauPeo.UI
{
    // Chữ của màn trận: hàm tĩnh thuần để test không cần scene.
    public static class CombatViewText
    {
        public static string UnitHp(int hp, int maxHp) => $"{hp}/{maxHp}";

        public static string SkillButton(int cooldown) => cooldown > 0 ? $"Skill ({cooldown})" : "Skill";

        // Giá vượt năng lượng tối đa thì chỉ cần đầy thanh, nên chia cho giá hiệu dụng (cùng luật BattleSession).
        public static float UltimateFill(int energy, int energyCost, int maxEnergy)
        {
            var effective = Mathf.Min(energyCost, maxEnergy);
            return effective <= 0 ? 0f : Mathf.Clamp01((float)energy / effective);
        }

        public static string UltimateReadyTag(bool ready) => ready ? "OK!" : "";

        public static string TurnLine(string name) => UiText.Format("combat.turn", name);

        public static string WaveLabel(int waveIndex, int waveCount) => UiText.Format("combat.wave", waveIndex, waveCount);

        public static string SpeedLabel(bool fast) => fast ? "x2" : "x1";

        // Trận DEV chưa trừ thể lực nhưng dùng cùng một câu (chỉ là bản thử).
        public static string SurrenderConfirm() => UiText.Get("combat.surrender_confirm");

        // Giây làm tròn lên: còn 0,2 giây vẫn ghi 0:01 chứ không ghi 0:00 khi thể lực chưa lên.
        public static string StaminaCard(int current, int max, double secondsToNext) =>
            current >= max ? UiText.Format("combat.stamina_full", current, max) : UiText.Format("combat.stamina_next", current, max, Clock(secondsToNext));

        // Thiếu thể lực thì không ghi số âm.
        public static string StaminaChange(int current, int cost) =>
            current >= cost ? UiText.Format("combat.stamina_change", current, current - cost) : UiText.Format("combat.stamina_short", current, cost);

        public static string EnterWarning(int cost) => UiText.Format("combat.enter_warning", cost);

        public static string EnterFailedLine(EnterStageStatus status) => status switch
        {
            EnterStageStatus.NotEnoughStamina => UiText.Get("combat.enter_no_stamina"),
            EnterStageStatus.Locked => LockedHint(),
            EnterStageStatus.SaveFailed => UiText.Get("combat.enter_save_failed"),
            EnterStageStatus.BattleInProgress => UiText.Get("combat.enter_in_battle"),
            EnterStageStatus.InvalidConfig => InvalidConfigLine(),
            EnterStageStatus.EmptyTeam => UiText.Get("combat.enter_empty_team"),
            EnterStageStatus.InvalidCharacter => UiText.Get("combat.enter_invalid_character"),
            _ => UiText.Get("combat.enter_unknown"),
        };

        // Tên các lý do từ chối của CanEnter trùng tên ở EnterStageStatus; viết rõ từng nhánh để đổi tên một enum thì lỗi biên dịch.
        public static string EnterFailedLine(StageEnterStatus status) => EnterFailedLine(status switch
        {
            StageEnterStatus.Locked => EnterStageStatus.Locked,
            StageEnterStatus.EmptyTeam => EnterStageStatus.EmptyTeam,
            StageEnterStatus.NotEnoughStamina => EnterStageStatus.NotEnoughStamina,
            StageEnterStatus.Invalid => EnterStageStatus.Invalid,
            StageEnterStatus.WrongKind => EnterStageStatus.WrongKind,
            _ => EnterStageStatus.Unknown,
        });

        // Chỉ trận chưa trả thể lực (NotPaid, từ dải DEV) là "trận thử"; trận đã trả mà không nhận được thưởng thì báo thẳng.
        public static string NoRewardLine(StageCompleteStatus status) =>
            status == StageCompleteStatus.NotPaid ? TrialLine() : UiText.Get("combat.no_reward");

        // Số giây tới khi đủ `cost`: phần hồi đang chạy dở, rồi các lần hồi tròn còn lại.
        public static double SecondsUntilAffordable(int current, int cost, double secondsToNext, int regenSeconds) =>
            current >= cost ? 0d : secondsToNext + (cost - current - 1) * (double)regenSeconds;

        public static string WaitLine(double seconds) => UiText.Format("combat.wait", Clock(seconds));

        public static string RewardHeader(bool firstClear) => UiText.Get(firstClear ? "combat.reward_first" : "combat.reward");

        public static Color CurrencyColor(CurrencyType type) => type switch
        {
            CurrencyType.Coin => new Color(0.95f, 0.80f, 0.25f),
            CurrencyType.Material => new Color(0.65f, 0.45f, 0.28f),
            _ => new Color(0.35f, 0.65f, 0.95f),
        };

        public static string CurrencyCell(CurrencyAmount line) =>
            $"{line.Amount.ToString(CultureInfo.InvariantCulture)} {line.Type}";

        public static string NodeLabel(StageDefinition stage) => stage.Kind == StageKind.Story ? UiText.Get("combat.node_story") : stage.DisplayName;

        public static string LockedHint() => UiText.Get("combat.locked_hint");

        public static string StoryEmpty() => UiText.Get("combat.story_empty");

        public static string InvalidConfigLine() => UiText.Get("combat.invalid_config");

        // m:ss, từ một giờ trở lên h:mm:ss.
        private static string Clock(double seconds)
        {
            var total = (long)Math.Ceiling(Math.Max(0d, seconds));
            var h = total / 3600;
            var m = total % 3600 / 60;
            var sec = total % 60;
            return h > 0 ? $"{h}:{m:00}:{sec:00}" : $"{m}:{sec:00}";
        }

        public static string ConfirmSmallTeam(int count) => UiText.Format("combat.confirm_small_team", count);

        public static string ConfirmFromStaff(string name, StaffRole role) =>
            UiText.Format("combat.confirm_from_staff", name, StaffPanelRows.RoleName(role));

        public static string StaffTag(StaffRole role) => UiText.Format("combat.staff_tag", StaffPanelRows.RoleName(role));

        public static string ClassName(CharacterClass cls) => cls switch
        {
            CharacterClass.Tank => UiText.Get("combat.class_tank"),
            CharacterClass.Fighter => UiText.Get("combat.class_fighter"),
            CharacterClass.Support => UiText.Get("combat.class_support"),
            CharacterClass.Marksman => UiText.Get("combat.class_marksman"),
            CharacterClass.Mage => UiText.Get("combat.class_mage"),
            _ => "",
        };

        public static string PositionName(BattlePosition position) => position switch
        {
            BattlePosition.Front => UiText.Get("combat.position_front"),
            BattlePosition.Mid => UiText.Get("combat.position_mid"),
            _ => UiText.Get("combat.position_back"),
        };

        public static string TeamFull() => UiText.Format("combat.team_full", Roster.MaxBattleTeamSize);

        public static string StaminaUsed(int cost) => UiText.Format("combat.stamina_used", cost);

        public static string TrialLine() => UiText.Get("combat.trial");

        public static string FailHint() => UiText.Get("combat.fail_hint");

        public static string FailStamina(int cost) => UiText.Format("combat.fail_stamina", cost);

        public static string EndBanner(BattleState state) => state switch
        {
            BattleState.Won => UiText.Get("combat.end_won"),
            BattleState.Lost => UiText.Get("combat.end_lost"),
            _ => "",
        };
    }
}
