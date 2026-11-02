namespace BunDauPeo.Characters
{
    public enum Rarity { R, SR, SSR }

    // Giá trị số được lưu trong asset: chỉ thêm ở cuối. Đỡ đòn, Đấu sĩ, Hỗ trợ, Xạ thủ, Pháp sư.
    public enum CharacterClass { Unset = 0, Tank = 1, Fighter = 2, Support = 3, Marksman = 4, Mage = 5 }

    public enum SkillKind { Basic, Skill, Ultimate }

    public enum SkillTarget { SingleEnemy, AllEnemies, SingleAlly, AllAllies, Self }

    public enum SkillEffect { Damage, Heal }
}
