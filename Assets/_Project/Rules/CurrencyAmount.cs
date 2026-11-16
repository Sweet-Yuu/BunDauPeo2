using System;

namespace BunDauPeo.Core
{
    [Serializable]
    public struct CurrencyAmount
    {
        public CurrencyType Type;
        public long Amount;

        public CurrencyAmount(CurrencyType type, long amount)
        {
            Type = type;
            Amount = amount;
        }
    }
}
