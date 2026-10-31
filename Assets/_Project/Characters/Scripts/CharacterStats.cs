using System;

namespace BunDauPeo.Characters
{
    // Nằm ở Characters để quái của Combat dùng lại.
    [Serializable]
    public struct CombatStats
    {
        public int HP;
        public int Attack;
        public int Defense;
        public int Speed;
    }

    [Serializable]
    public struct RestaurantStats
    {
        public int Cooking;
        public int Serving;
        public int Cashier;
    }
}
