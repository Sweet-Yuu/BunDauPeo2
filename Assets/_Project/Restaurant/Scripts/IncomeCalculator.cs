using System;
using BunDauPeo.Core;

namespace BunDauPeo.Restaurant
{
    public readonly struct IncomeEstimate
    {
        public IncomeEstimate(double coinsPerSecond, StaffRole? missingRole)
        {
            CoinsPerSecond = coinsPerSecond;
            MissingRole = missingRole;
        }

        public double CoinsPerSecond { get; }

        // Vai đầu tiên (Cook, Server, Cashier) không có ai; khi đó CoinsPerSecond là 0.
        public StaffRole? MissingRole { get; }
    }

    // Cận trên của mô phỏng (spec 4.2): người nhanh nhất mỗi vai, không tính khách hết kiên nhẫn hay ngẫu nhiên.
    public static class IncomeCalculator
    {
        public static IncomeEstimate Estimate(RestaurantConfig config, RestaurantProfile profile)
        {
            var roles = new[] { StaffRole.Cook, StaffRole.Server, StaffRole.Cashier };
            foreach (var role in roles)
                if (profile.Workers(role).Count == 0) return new IncomeEstimate(0, role);

            var totalPrice = 0.0;
            var totalCook = 0.0;
            foreach (var dish in config.Dishes)
            {
                totalPrice += dish.BasePrice;
                totalCook += dish.CookSeconds;
            }

            var baseTimes = new[] { totalCook / config.Dishes.Count, config.ServeSeconds, config.CashierSeconds };

            var throughput = 1.0 / (config.BaseArrivalInterval * profile.ArrivalIntervalMultiplier);
            var tableCycle = (double)config.EatSeconds;
            for (var i = 0; i < roles.Length; i++)
            {
                var capacity = 0.0;
                var fastest = int.MinValue;
                foreach (var worker in profile.Workers(roles[i]))
                {
                    capacity += (100.0 + worker.Stat) / (100.0 * baseTimes[i]);
                    fastest = Math.Max(fastest, worker.Stat);
                }

                throughput = Math.Min(throughput, capacity);
                tableCycle += baseTimes[i] * 100.0 / (100.0 + fastest);
            }

            throughput = Math.Min(throughput, profile.Tables / tableCycle);

            var weights = 0.0;
            var weightedPay = 0.0;
            foreach (var customer in config.Customers)
            {
                weights += customer.SpawnWeight;
                weightedPay += customer.SpawnWeight * customer.PayMultiplier;
            }

            var moneyPerCustomer = totalPrice / config.Dishes.Count * (weightedPay / weights) * profile.PriceMultiplier;
            return new IncomeEstimate(throughput * moneyPerCustomer, null);
        }
    }
}
