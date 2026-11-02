using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

        public static string TurnLine(string name) => $"Lượt của {name}";

        public static string WaveLabel(int waveIndex, int waveCount) => $"Đợt {waveIndex}/{waveCount}";

        public static string SpeedLabel(bool fast) => fast ? "x2" : "x1";

        public static string PauseHpLine(BattleUnit unit) =>
            unit.IsAlive ? $"{unit.DisplayName} {UnitHp(unit.Hp, unit.MaxHp)}" : $"{unit.DisplayName}: đã ngã";

        // Trận DEV chưa trừ thể lực nhưng dùng cùng một câu (chỉ là bản thử).
        public static string SurrenderConfirm() => "Bỏ cuộc? Trận này tính thua, thể lực không được hoàn.";

        // Giây làm tròn lên: còn 0,2 giây vẫn ghi 0:01 chứ không ghi 0:00 khi thể lực chưa lên.
        public static string StaminaCard(int current, int max, double secondsToNext) =>
            current >= max ? $"Thể lực {current}/{max}" : $"Thể lực {current}/{max} · +1 sau {Clock(secondsToNext)}";

        // Thiếu thể lực thì không ghi số âm.
        public static string StaminaChange(int current, int cost) =>
            current >= cost ? $"Thể lực {current} → {current - cost}" : $"Thể lực {current}, cần {cost}";

        public static string EnterWarning(int cost) => $"Trừ ngay {cost} thể lực khi vào trận; thua hoặc bỏ cuộc không hoàn.";

        public static string EnterFailedLine(EnterStageStatus status) => status switch
        {
            EnterStageStatus.NotEnoughStamina => "Chưa đủ thể lực",
            EnterStageStatus.Locked => LockedHint(),
            EnterStageStatus.SaveFailed => "Chưa lưu được dữ liệu, chưa vào trận",
            EnterStageStatus.BattleInProgress => "Đang có trận",
            EnterStageStatus.InvalidConfig => InvalidConfigLine(),
            EnterStageStatus.EmptyTeam => "Đội chưa có ai",
            EnterStageStatus.InvalidCharacter => "Đội có nhân vật lỗi",
            _ => "Không vào được màn này",
        };

        // Số giây tới khi đủ `cost`: phần hồi đang chạy dở, rồi các lần hồi tròn còn lại.
        public static double SecondsUntilAffordable(int current, int cost, double secondsToNext, int regenSeconds) =>
            current >= cost ? 0d : secondsToNext + (cost - current - 1) * (double)regenSeconds;

        public static string WaitLine(double seconds) => $"Đủ thể lực sau {Clock(seconds)}";

        public static string RewardHeader(bool firstClear) => firstClear ? "Thưởng lần đầu" : "Thưởng";

        public static string RewardLine(IReadOnlyList<CurrencyAmount> lines) =>
            string.Join(" · ", lines.Select(l => $"{l.Amount.ToString(CultureInfo.InvariantCulture)} {l.Type}"));

        public static string NodeLabel(StageDefinition stage) => stage.Kind == StageKind.Story ? "Truyện" : stage.DisplayName;

        public static string LockedHint() => "Clear màn trước để mở";

        public static string StoryEmpty() => "Chưa có nội dung cốt truyện";

        public static string InvalidConfigLine() => "Cấu hình trận lỗi";

        // m:ss, từ một giờ trở lên h:mm:ss.
        private static string Clock(double seconds)
        {
            var total = (long)Math.Ceiling(Math.Max(0d, seconds));
            var h = total / 3600;
            var m = total % 3600 / 60;
            var sec = total % 60;
            return h > 0 ? $"{h}:{m:00}:{sec:00}" : $"{m}:{sec:00}";
        }

        public static string EndBanner(BattleState state) => state switch
        {
            BattleState.Won => "CLEAR",
            BattleState.Lost => "Thất bại",
            _ => "",
        };
    }
}
