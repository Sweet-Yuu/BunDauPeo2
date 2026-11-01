using System;
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

        // Phần đầy 0..1 của ngôi sao thứ index (0..4) khi có `stars` sao.
        public static float StarFill(double stars, int index) => (float)Math.Clamp(stars - index, 0.0, 1.0);
    }
}
