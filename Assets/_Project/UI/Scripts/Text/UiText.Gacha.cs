namespace BunDauPeo.UI
{
    public static partial class UiText
    {
        private static readonly (string Key, string Vi, string En)[] Gacha =
        {
            ("gacha.new", "MỚI", "NEW"),
            ("gacha.duplicate", "TRÙNG +{0} mảnh", "DUPLICATE +{0} fragments"),
            ("gacha.pity_off", "Đảm bảo SSR: tắt", "SSR guarantee: off"),
            ("gacha.pity_left", "SSR đảm bảo sau: {0} lượt", "SSR guaranteed in: {0} pulls"),
            ("gacha.status_unsaved", "Cảnh báo: chưa lưu được dữ liệu, game sẽ thử lưu lại sau.", "Warning: could not save your data, the game will try again later."),
            ("gacha.status_unknown_banner", "Không tìm thấy banner này.", "This banner was not found."),
            ("gacha.status_invalid_banner", "Banner bị lỗi cấu hình nên không quay được.", "This banner is misconfigured, so you cannot pull."),
            ("gacha.status_not_enough", "Không đủ Gem.", "Not enough Gem."),
            ("gacha.pull_label", "Quay {0}\n{1}", "Pull {0}\n{1}"),
            ("gacha.roll_again_cost", "Quay tiếp ({0})", "Pull again ({0})"),
            ("gacha.need_gem", "Không đủ Gem (cần {0})", "Not enough Gem (need {0})"),
            ("gacha.history_line", "{0} · Quay {1} · {2}", "{0} · Pull {1} · {2}"),
            ("gacha.history_empty", "Chưa có lượt quay nào.", "No pulls yet."),
            ("gacha.history", "Lịch sử", "History"),
            ("gacha.total_title", "Kết quả quay", "Pull results"),
            ("gacha.skip", "Bỏ qua", "Skip"),
            ("gacha.roll_again_title", "Quay tiếp?", "Pull again?"),
            ("gacha.roll_again", "Quay tiếp", "Pull again"),
            ("gacha.history_title", "Lịch sử quay", "Pull history"),
        };
    }
}
