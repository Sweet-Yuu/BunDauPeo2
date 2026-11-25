using System;
using BunDauPeo.Characters;

namespace BunDauPeo.UI
{
    // Mọi chọn chữ của màn Nhân vật nằm ở đây để test được mà không cần dựng scene.
    public static class CharactersViewText
    {
        public static string CardFragments(CharacterListRow row) =>
            row.IsMax ? UiText.Get("characters.max") : $"{row.Fragments}/{row.NextCost}";

        public static string StarsLine(int stars)
        {
            var filled = Math.Clamp(stars, 0, StarConfig.MaxStars);
            return new string('★', filled) + new string('☆', StarConfig.MaxStars - filled);
        }

        // statKey: hp, attack, defense, speed. Speed không đổi khi nâng sao nên gọi với isMax = true.
        public static string StatLine(string statKey, int now, int next, bool isMax)
        {
            var label = UiText.Get("characters.stat_" + statKey);
            return isMax ? $"{label}  {now}" : $"{label}  {now} → {next}";
        }

        public static string FragmentsLine(CharacterListRow row) =>
            row.IsMax ? UiText.Format("characters.fragments_max", row.Fragments) : UiText.Format("characters.fragments", row.Fragments, row.NextCost);

        public static string ButtonLabel(CharacterListRow row) =>
            row.IsMax ? UiText.Get("characters.star_up_max") : UiText.Format("characters.star_up", row.Stars + 1, row.NextCost);

        public static string LevelLine(int level, int max) => UiText.Format("characters.level_line", level, max);

        public static string UpgradeButtonLabel(int potion, int coin) => UiText.Format("characters.level_up_btn", potion, coin);

        // Lý do nút Nâng cấp mờ; trạng thái khác (thành công, lỗi) không có lý do.
        public static string UpgradeDisabledReason(LevelUpStatus s) => s switch
        {
            LevelUpStatus.AtPlayerLevel => UiText.Get("characters.level_reason_player"),
            LevelUpStatus.NotEnoughPotion => UiText.Get("characters.level_reason_potion"),
            LevelUpStatus.NotEnoughCoin => UiText.Get("characters.level_reason_coin"),
            _ => "",
        };

        public static string LevelStatusText(LevelUpResult result) => result.Status switch
        {
            LevelUpStatus.Success => result.Saved ? UiText.Format("characters.level_done", result.LevelAfter) : UiText.Get("characters.not_saved"),
            LevelUpStatus.AtPlayerLevel or LevelUpStatus.NotEnoughPotion or LevelUpStatus.NotEnoughCoin => UpgradeDisabledReason(result.Status),
            _ => UiText.Get("characters.level_error"),
        };

        public static string StatusText(StarUpResult result) => result.Status switch
        {
            StarUpStatus.Success => result.Saved ? UiText.Format("characters.done", result.StarsAfter) : UiText.Get("characters.not_saved"),
            StarUpStatus.NotEnoughFragments => UiText.Get("characters.not_enough"),
            StarUpStatus.MaxStars => UiText.Get("characters.star_up_max"),
            StarUpStatus.Offline => "",
            _ => UiText.Get("characters.error"),
        };
    }
}
