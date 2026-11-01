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
            StaffRole.Cook => "Thiếu đầu bếp, quán không kiếm tiền",
            StaffRole.Server => "Thiếu nhân viên phục vụ, quán không kiếm tiền",
            StaffRole.Cashier => "Thiếu thu ngân, quán không kiếm tiền",
            // Dấu phẩy cố định, không theo culture máy.
            _ => $"≈ {estimate.CoinsPerSecond.ToString("0.0", CultureInfo.InvariantCulture).Replace('.', ',')} Coin/giây",
        };

        public static string CounterLine(int served, int abandoned) => $"Phục vụ {served} · Bỏ về {abandoned}";

        public static string AfkButton(long coins) => $"Thu AFK\n({coins.ToString(CultureInfo.InvariantCulture)} Coin)";

        public static string QualityLine(double total) => $"Chất lượng {Math.Round(total).ToString(CultureInfo.InvariantCulture)}/100";

        public static string IncidentLabel(ActiveIncident incident) => incident.Definition.DisplayName;

        // Chỉ nhắc xếp người khi vai trống; có người thì "!" bấm được ngay.
        public static string IncidentHint(ActiveIncident incident)
        {
            if (incident.CanResolve) return "";
            var role = incident.Role switch
            {
                StaffRole.Cook => "Đầu bếp",
                StaffRole.Server => "Phục vụ",
                _ => "Thu ngân",
            };
            return $"Cần xếp người vào vai {role}";
        }

        public static string InvalidConfigLine() => "Cấu hình quán lỗi";

        // Chi phí của cấp kế tiếp; null khi đã tối đa.
        public static CurrencyAmount[] NextCost(UpgradeDefinition def, int level) =>
            level >= def.MaxLevel ? null : def.Levels[Math.Max(0, level)].Cost;

        // Giá là của cấp kế tiếp, không phải cấp đang có.
        public static string UpgradeButton(UpgradeDefinition def, int level)
        {
            var cost = NextCost(def, level);
            if (cost == null) return $"{def.DisplayName}  {level}/{def.MaxLevel}\nTối đa";

            var lines = new List<string>();
            foreach (var line in cost)
                if (line.Amount > 0) lines.Add($"{line.Amount} {line.Type}");
            return $"{def.DisplayName}  {level}/{def.MaxLevel}\n{string.Join(" + ", lines)}";
        }

        // Dùng chung cho mọi thao tác đã đổi dữ liệu trong bộ nhớ mà persist thất bại (mua nâng cấp, thu AFK).
        public static string SaveWarning(bool saved) => saved ? "" : "Cảnh báo: chưa lưu được dữ liệu, game sẽ thử lưu lại sau.";

        public static string UpgradeStatus(UpgradeBuyStatus status, bool saved) => status switch
        {
            UpgradeBuyStatus.Success => SaveWarning(saved),
            UpgradeBuyStatus.UnknownUpgrade => "Không tìm thấy nâng cấp này.",
            UpgradeBuyStatus.InvalidUpgrade => "Nâng cấp bị lỗi cấu hình nên không mua được.",
            UpgradeBuyStatus.MaxLevel => "Đã đạt cấp tối đa.",
            UpgradeBuyStatus.NotEnoughCurrency => "Không đủ Coin hoặc Material.",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
        };

        // Phần đầy 0..1 của ngôi sao thứ index (0..4) khi có `stars` sao.
        public static float StarFill(double stars, int index) => (float)Math.Clamp(stars - index, 0.0, 1.0);

        // Phút lẻ cắt bỏ, không làm tròn lên.
        public static string AbsenceText(double seconds)
        {
            var minutes = (long)(Math.Max(0.0, seconds) / 60.0);
            return minutes >= 60 ? $"{minutes / 60} giờ {minutes % 60} phút" : $"{minutes} phút";
        }

        public static string FullCapLine(bool isFull) => isFull ? "Đã đầy, hãy thu thập để tích tiếp" : "";

        public static string TipLine(double dailyGoalSeconds) =>
            $"Mẹo: vào quán đủ {(long)(dailyGoalSeconds / 60.0)} phút mỗi ngày để tăng chất lượng.";

        // Năm dòng của AfkPopup; số Coin lý thuyết bỏ phần lẻ như PreviewCollect.
        public static string AfkSummary(double absenceSeconds, double theoreticalCoins, double quality, double factor, long coins) =>
            $"Bạn đã vắng: {AbsenceText(absenceSeconds)}\n"
            + $"Thu nhập chờ (lý thuyết): {Whole((long)Math.Floor(theoreticalCoins))} Coin\n"
            + $"Chất lượng quán: {Math.Round(quality).ToString(CultureInfo.InvariantCulture)}/100 · {Comma(quality / 20.0, "0.0")} sao\n"
            + $"{FactorLine(factor)}\n"
            + $"Bạn nhận được: {Whole(coins)} Coin";

        // Thay phần tóm tắt khi thu xong mà chưa lưu được: Coin đã vào ví, popup ở lại để báo.
        public static string AfkCollectedLine(long coins) => $"Đã nhận {Whole(coins)} Coin";

        public static string FactorLine(double factor) => $"Hệ số AFK ×{Comma(factor, "0.00")}";

        public static string AttendanceLine(double points, double max) => $"Chuyên cần {Points(points)}/{Points(max)}";

        public static string IncidentPointsLine(double points, double max) => $"Xử lý sự cố {Points(points)}/{Points(max)}";

        // Nhãn dưới ô ngày; daysAgo = 1 là hôm qua.
        public static string DayCellLabel(int daysAgo) => daysAgo == 1 ? "Hôm qua" : $"{daysAgo} ngày";

        // Phần đầy 0..1 của ô ngày; `credit` từ RestaurantQuality.DayCredits.
        public static float DayCellFill(float credit) => Math.Clamp(credit, 0f, 1f);

        public static string TodayLine(double seconds, double goalSeconds) =>
            $"Hôm nay: {(long)(Math.Max(0.0, seconds) / 60.0)}/{(long)(goalSeconds / 60.0)} phút (tính vào ngày mai)";

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

            return $"Kịp {fast} · Trễ {late} · Bỏ lỡ {missed} · Chưa có {window - fast - late - missed}";
        }

        private static string Points(double value) => Math.Round(value).ToString(CultureInfo.InvariantCulture);

        private static string Whole(long value) => value.ToString(CultureInfo.InvariantCulture);

        // Dấu phẩy cố định, không theo culture máy (như IncomeLine).
        private static string Comma(double value, string format) => value.ToString(format, CultureInfo.InvariantCulture).Replace('.', ',');
    }
}
