using System;
using System.Collections.Generic;
using System.Globalization;
using BunDauPeo.Core;
using BunDauPeo.Rules;

namespace BunDauPeo.UI
{
    // Chữ của thư quà: giờ hạn theo UTC+7 (tính từ giờ server, không dùng giờ máy), dòng thưởng gộp theo loại.
    public static class GiftFormat
    {
        private const int VietnamHours = 7;

        public static string Short(long utcTicks) => Local(utcTicks).ToString("dd/MM HH:mm", CultureInfo.InvariantCulture);

        public static string Long(long utcTicks) => Local(utcTicks).ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);

        // Kẹp để endUtc sát năm 9999 (hoặc ticks ngoài khoảng) không làm ném lỗi khi vẽ thư.
        private static DateTime Local(long utcTicks)
        {
            var max = DateTime.MaxValue.Ticks - TimeSpan.TicksPerHour * VietnamHours;
            return new DateTime(Math.Clamp(utcTicks, 0, max), DateTimeKind.Utc).AddHours(VietnamHours);
        }

        // Số có dấu phân cách nghìn theo ngôn ngữ: "5.000" (Vi) hoặc "5,000" (En).
        public static string Amount(long amount)
        {
            var text = amount.ToString("N0", CultureInfo.InvariantCulture);
            return GameLanguage.Current == Language.Vi ? text.Replace(',', '.') : text;
        }

        // Gộp theo loại, thứ tự Gem, Coin, Material; bỏ loại tổng 0 hoặc loại lạ.
        public static List<string> Lines(IEnumerable<GiftReward> rewards)
        {
            long gem = 0, coin = 0, material = 0;
            foreach (var r in rewards)
            {
                if (r == null) continue;
                if (r.Type == GiftRules.Gem) gem += r.Amount;
                else if (r.Type == GiftRules.Coin) coin += r.Amount;
                else if (r.Type == GiftRules.Material) material += r.Amount;
            }

            return Lines(gem, coin, material);
        }

        public static List<string> Lines(long gem, long coin, long material)
        {
            var lines = new List<string>(3);
            if (gem > 0) lines.Add("Gem " + Amount(gem));
            if (coin > 0) lines.Add("Coin " + Amount(coin));
            if (material > 0) lines.Add("Material " + Amount(material));
            return lines;
        }
    }
}
