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

        public static string UpgradeStatus(UpgradeBuyStatus status, bool saved) => status switch
        {
            UpgradeBuyStatus.Success => saved ? "" : "Cảnh báo: chưa lưu được dữ liệu, game sẽ thử lưu lại sau.",
            UpgradeBuyStatus.UnknownUpgrade => "Không tìm thấy nâng cấp này.",
            UpgradeBuyStatus.InvalidUpgrade => "Nâng cấp bị lỗi cấu hình nên không mua được.",
            UpgradeBuyStatus.MaxLevel => "Đã đạt cấp tối đa.",
            UpgradeBuyStatus.NotEnoughCurrency => "Không đủ Coin hoặc Material.",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
        };

        // Phần đầy 0..1 của ngôi sao thứ index (0..4) khi có `stars` sao.
        public static float StarFill(double stars, int index) => (float)Math.Clamp(stars - index, 0.0, 1.0);
    }
}
