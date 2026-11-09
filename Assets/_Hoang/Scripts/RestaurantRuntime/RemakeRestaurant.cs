using System;
using BunDauPeo.Characters;
using BunDauPeo.Core;
using BunDauPeo.Restaurant;

namespace Hoang.Remake.RestaurantRuntime
{
    public sealed class RemakeRestaurant : IDisposable
    {
        private readonly RestaurantConfig _config;
        private readonly RemakeContext _context;
        private readonly Roster _roster;
        private double _saveTimer;

        public RestaurantSimulation Simulation { get; }
        public RestaurantProfile Profile { get; private set; }
        public IncomeEstimate Estimate { get; private set; }
        public bool LastSaveSucceeded { get; private set; } = true;
        public int Reputation => (int)Math.Min(100L, 50L + Profile.CleanlinessBonus);
        public long IngredientPackPrice => (long)decimal.Ceiling(100m * (1m - (decimal)Profile.IngredientCostReduction));

        public RemakeRestaurant(RestaurantConfig config, RemakeContext context, Roster roster, System.Random random)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            var errors = config.Validate();
            if (errors.Count > 0) throw new ArgumentException(string.Join("; ", errors), nameof(config));
            Simulation = new RestaurantSimulation(config, random);
            RefreshProfile();
            Simulation.CustomerPaid += OnPaid;
            RestaurantBuffBridge.OnBuffsChanged += RefreshProfile;
            _roster.Changed += RefreshProfile;
            _context.ResetCompleted += OnReset;
        }

        public void Tick(float seconds)
        {
            Simulation.Tick(seconds);
            _saveTimer += seconds;
            if (_saveTimer < _config.AutosaveSeconds) return;
            _saveTimer = 0;
            LastSaveSucceeded = _context.Persist();
        }

        public bool BuyIngredients()
        {
            var price = IngredientPackPrice;
            if (_context.Wallet.Get(CurrencyType.Coin) < price
                || _context.Wallet.Get(CurrencyType.Material) > long.MaxValue - 10) return false;
            return _context.Commit(candidate =>
            {
                var wallet = new Wallet(candidate);
                wallet.TrySpend(new CurrencyAmount(CurrencyType.Coin, price));
                wallet.Add(CurrencyType.Material, 10);
            });
        }

        public void Dispose()
        {
            Simulation.CustomerPaid -= OnPaid;
            RestaurantBuffBridge.OnBuffsChanged -= RefreshProfile;
            _roster.Changed -= RefreshProfile;
            _context.ResetCompleted -= OnReset;
        }

        private void OnPaid(int customerId, long coins) => _context.Wallet.Add(CurrencyType.Coin, coins);

        private void OnReset()
        {
            Simulation.Reset();
            _saveTimer = 0;
        }

        private void RefreshProfile()
        {
            Profile = RestaurantProfile.Build(_config, _context.Save, _roster).WithBuffs(
                RestaurantBuffBridge.GetTotalCookingSpeedMultiplier(), RestaurantBuffBridge.GetTotalRevenueMultiplier(),
                RestaurantBuffBridge.GetCustomerPatienceMultiplier(), RestaurantBuffBridge.GetTotalIngredientCostReduction(),
                RestaurantBuffBridge.GetTotalCleanlinessBonus());
            Simulation.SetProfile(Profile);
            Estimate = IncomeCalculator.Estimate(_config, Profile);
        }
    }
}
