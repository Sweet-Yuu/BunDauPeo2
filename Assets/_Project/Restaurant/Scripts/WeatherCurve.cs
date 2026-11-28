namespace BunDauPeo.Restaurant
{
    // Xác suất mỗi lần quay mưa theo số phút chơi liên tục (spec Thời tiết mục 3). Thuần, không trạng thái.
    public static class WeatherCurve
    {
        public static double Chance(double minutes, RestaurantConfig config)
        {
            if (!(minutes >= config.GraceMinutes)) return 0;
            if (minutes < config.SlowStartMinutes) return config.BaseChance;
            if (minutes < config.FastStartMinutes)
            {
                var k = (minutes - config.SlowStartMinutes) / (config.FastStartMinutes - config.SlowStartMinutes);
                return config.BaseChance + (config.FastStartChance - config.BaseChance) * k;
            }

            // Khoảng quay hỏng (<= 0) đã bị Validate báo; ở đây chỉ tránh NaN nếu ai gọi thẳng.
            var steps = config.RollIntervalMinutes > 0 ? System.Math.Floor((minutes - config.FastStartMinutes) / config.RollIntervalMinutes) : 0;
            return System.Math.Min(config.MaxChance, config.FastStartChance + config.FastStepChance * steps);
        }
    }
}
