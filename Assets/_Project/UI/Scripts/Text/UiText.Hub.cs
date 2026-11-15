namespace BunDauPeo.UI
{
    public static partial class UiText
    {
        private static readonly (string Key, string Vi, string En)[] Hub =
        {
            ("hub.tab_restaurant", "Quán", "Restaurant"),
            ("hub.tab_characters", "Nhân vật", "Characters"),
            ("hub.tab_story", "Cốt truyện", "Story"),
            ("hub.tab_stages", "Màn chơi", "Quests"),
            ("hub.tab_gacha", "Gacha", "Gacha"),
            ("hub.tab_menu", "Menu", "Menu"),
            ("hub.coming_soon", "Sắp có", "Coming soon"),
            ("hub.menu_title", "Menu", "Menu"),
            ("hub.language", "Ngôn ngữ: Tiếng Việt", "Language: English"),
            ("hub.language_not_saved", "Chưa lưu được, giữ ngôn ngữ cũ", "Couldn't save. Language not changed"),
            ("hub.reset", "Reset dữ liệu", "Reset data"),
            ("hub.reset_confirm", "Xoá TOÀN BỘ dữ liệu đã lưu và đưa game về như mới?\nKhông thể hoàn tác.\nXoá cả dữ liệu trên máy chủ. Tài khoản và liên kết vẫn giữ.",
                "Delete ALL saved data and start over?\nThis can't be undone.\nAlso deletes your data on the server. Your account and links are kept."),
            ("hub.reset_yes", "Xoá hết", "Delete all"),
            ("hub.reset_failed", "Reset chưa xong: không ghi được file save. Thử lại.", "Reset didn't finish: couldn't write the save file. Try again."),
            ("hub.dev", "DEV", "DEV"),
            ("menu.delete_account", "Xoá tài khoản", "Delete account"),
            ("menu.delete_account_confirm", "Xoá vĩnh viễn tài khoản và mọi dữ liệu, kể cả liên kết Google. Không lấy lại được.",
                "Permanently delete your account and all data, including linked Google account. This cannot be undone."),
            ("menu.delete_yes", "Xoá", "Delete"),
            ("hub.back", "Quay lại", "Back"),
            ("hub.banner_gacha", "Gacha\nQuay ngay", "Gacha\nPull now"),
            ("hub.shop", "Shop", "Shop"),
            ("hub.res_stamina_next", "+1 sau {0}", "+1 in {0}"),
            ("hub.notify", "Báo", "Alerts"),
            ("notify.title", "Việc cần làm", "To do"),
            ("notify.empty", "Không có việc cần làm", "Nothing to do"),
            ("notify.incident", "Quán có sự cố đang chờ", "An incident needs you"),
            ("notify.staff_missing", "Thiếu {0}, quán đứng yên", "No {0}, the shop is stopped"),
            ("notify.afk", "Có {0} Coin AFK chờ thu", "{0} AFK Coin to collect"),
            ("notify.stamina_full", "Thể lực đã đầy", "Stamina is full"),
            ("notify.star_up", "{0} nhân vật đủ mảnh nâng sao", "Ready to star up: {0}"),
            ("profile.title", "Hồ sơ", "Profile"),
            ("profile.default_name", "Người chơi", "Player"),
            ("profile.name_hint", "Tối đa 12 ký tự", "Up to 12 characters"),
            ("profile.change_avatar", "Đổi avatar", "Change avatar"),
            ("profile.not_saved", "Chưa lưu được, sẽ thử lại", "Not saved yet, will retry"),
            ("profile.player_id", "ID: {0}", "ID: {0}"),
            ("profile.copy", "Sao chép", "Copy"),
            ("profile.copied", "Đã chép", "Copied"),
        };
    }
}
