using System;
using System.Collections.Generic;
using System.Globalization;
using BunDauPeo.Core;
using BunDauPeo.Restaurant;

namespace BunDauPeo.UI
{
    // Mọi chọn chữ và số của màn quán nằm ở đây để test được; view chỉ gán kết quả vào Text.
    public static class RestaurantViewText
    {
        public static string IncomeLine(IncomeEstimate estimate) => estimate.MissingRole switch
        {
            StaffRole.Cook => UiText.Get("restaurant.income_missing_cook"),
            StaffRole.Server => UiText.Get("restaurant.income_missing_server"),
            StaffRole.Cashier => UiText.Get("restaurant.income_missing_cashier"),
            // Dấu thập phân theo ngôn ngữ (UiText.Decimal), không theo culture máy.
            _ => UiText.Format("restaurant.income_rate", UiText.Decimal(estimate.CoinsPerSecond, "0.0")),
        };

        public static string CounterLine(int served, int abandoned) => UiText.Format("restaurant.counter", served, abandoned);

        public static string AfkButton(long coins) => UiText.Format("restaurant.afk_button", Whole(coins));

        public static string QualityLine(double total) => UiText.Format("restaurant.quality_line", Points(total));

        public static string IncidentLabel(ActiveIncident incident) => incident.Definition.DisplayName;

        // Chỉ nhắc xếp người khi vai trống; có người thì "!" bấm được ngay.
        public static string IncidentHint(ActiveIncident incident)
        {
            if (incident.CanResolve) return "";
            return UiText.Format("restaurant.incident_hint", StaffPanelRows.RoleName(incident.Role));
        }

        public static string InvalidConfigLine() => UiText.Get("restaurant.invalid_config");

        // Chi phí của cấp kế tiếp; null khi đã tối đa.
        public static CurrencyAmount[] NextCost(UpgradeDefinition def, int level) =>
            level >= def.MaxLevel ? null : def.Levels[Math.Max(0, level)].Cost;

        // Giá là của cấp kế tiếp, không phải cấp đang có.
        public static string UpgradeButton(UpgradeDefinition def, int level)
        {
            var cost = NextCost(def, level);
            if (cost == null) return $"{def.DisplayName}  {level}/{def.MaxLevel}\n{UiText.Get("restaurant.upgrade_max")}";

            var lines = new List<string>();
            foreach (var line in cost)
                if (line.Amount > 0) lines.Add($"{line.Amount} {line.Type}");
            return $"{def.DisplayName}  {level}/{def.MaxLevel}\n{string.Join(" + ", lines)}";
        }

        // Dùng chung cho mọi thao tác đã đổi dữ liệu trong bộ nhớ mà persist thất bại (mua nâng cấp, thu AFK).
        public static string SaveWarning(bool saved) => saved ? "" : UiText.Get("restaurant.save_warning");

        public static string UpgradeStatus(UpgradeBuyStatus status, bool saved) => status switch
        {
            UpgradeBuyStatus.Success => SaveWarning(saved),
            UpgradeBuyStatus.UnknownUpgrade => UiText.Get("restaurant.upgrade_unknown"),
            UpgradeBuyStatus.InvalidUpgrade => UiText.Get("restaurant.upgrade_invalid"),
            UpgradeBuyStatus.MaxLevel => UiText.Get("restaurant.upgrade_max_level"),
            UpgradeBuyStatus.NotEnoughCurrency => UiText.Get("restaurant.upgrade_not_enough"),
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
        };

        // Phần đầy 0..1 của ngôi sao thứ index (0..4) khi có `stars` sao.
        public static float StarFill(double stars, int index) => (float)Math.Clamp(stars - index, 0.0, 1.0);

        // Phút lẻ cắt bỏ, không làm tròn lên.
        public static string AbsenceText(double seconds)
        {
            var minutes = (long)(Math.Max(0.0, seconds) / 60.0);
            return minutes >= 60 ? UiText.Format("restaurant.absence_hours", minutes / 60, minutes % 60) : UiText.Format("restaurant.absence_minutes", minutes);
        }

        public static string FullCapLine(bool isFull) => isFull ? UiText.Get("restaurant.full_cap") : "";

        public static string TipLine(double dailyGoalSeconds) =>
            UiText.Format("restaurant.tip", (long)(dailyGoalSeconds / 60.0));

        // Năm dòng của AfkPopup; số Coin lý thuyết bỏ phần lẻ như PreviewCollect.
        public static string AfkSummary(double absenceSeconds, double theoreticalCoins, double quality, double factor, long coins) =>
            UiText.Format("restaurant.afk_absent", AbsenceText(absenceSeconds)) + "\n"
            + UiText.Format("restaurant.afk_pending", Whole((long)Math.Floor(theoreticalCoins))) + "\n"
            + UiText.Format("restaurant.afk_quality", Points(quality), UiText.Decimal(quality / 20.0, "0.0")) + "\n"
            + FactorLine(factor) + "\n"
            + UiText.Format("restaurant.afk_receive", Whole(coins));

        // Thay phần tóm tắt khi thu xong mà chưa lưu được: Coin đã vào ví, popup ở lại để báo.
        public static string AfkCollectedLine(long coins) => UiText.Format("restaurant.afk_collected", Whole(coins));

        public static string FactorLine(double factor) => UiText.Format("restaurant.afk_factor", UiText.Decimal(factor, "0.00"));

        public static string AttendanceLine(double points, double max) => UiText.Format("restaurant.attendance", Points(points), Points(max));

        public static string IncidentPointsLine(double points, double max) => UiText.Format("restaurant.incident_points", Points(points), Points(max));

        // Nhãn dưới ô ngày; daysAgo = 1 là hôm qua.
        public static string DayCellLabel(int daysAgo) => daysAgo == 1 ? UiText.Get("restaurant.day_yesterday") : UiText.Format("restaurant.day_ago", daysAgo);

        // Phần đầy 0..1 của ô ngày; `credit` từ RestaurantQuality.DayCredits.
        public static float DayCellFill(float credit) => Math.Clamp(credit, 0f, 1f);

        public static string TodayLine(double seconds, double goalSeconds) =>
            UiText.Format("restaurant.today", (long)(Math.Max(0.0, seconds) / 60.0), (long)(goalSeconds / 60.0));

        // Chỉ 10 sự cố mới nhất; ô còn thiếu tính "Chưa có". Điểm 0/1/2 = Bỏ lỡ/Trễ/Kịp (IncidentOutcome).
        public static string IncidentBreakdown(IReadOnlyList<int> scores)
        {
            const int window = 10;
            int fast = 0, late = 0, missed = 0;
            for (var i = Math.Max(0, scores.Count - window); i < scores.Count; i++)
            {
                switch (scores[i])
                {
                    case (int)IncidentOutcome.Fast: fast++; break;
                    case (int)IncidentOutcome.Late: late++; break;
                    default: missed++; break;
                }
            }

            return UiText.Format("restaurant.incident_breakdown", fast, late, missed, window - fast - late - missed);
        }

        private static string Points(double value) => Math.Round(value).ToString(CultureInfo.InvariantCulture);

        private static string Whole(long value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
