using System;

namespace BunDauPeo.UI
{
    [Flags]
    public enum ResourceSlots
    {
        None = 0,
        Stamina = 1,
        Coin = 2,
        Material = 4,
        Gem = 8,
        All = Stamina | Coin | Material | Gem,
    }

    // Trang nào cần ô riêng trên thanh tài nguyên thì cài; trang không cài tính là None.
    public interface IResourcePage
    {
        ResourceSlots Slots { get; }
    }
}
