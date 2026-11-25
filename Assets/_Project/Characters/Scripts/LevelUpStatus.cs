namespace BunDauPeo.Characters
{
    // Giá trị ghim vì có thể đi vào test và log; chỉ thêm ở cuối.
    public enum LevelUpStatus { Success, InvalidConfig, Unknown, NotOwned, AtPlayerLevel, NotEnoughPotion, NotEnoughCoin }

    public struct LevelUpResult
    {
        public LevelUpStatus Status;
        public int LevelAfter;
        public bool Saved;
    }
}
