namespace BunDauPeo.UI
{
    public static partial class UiText
    {
        private static readonly (string Key, string Vi, string En)[] Characters =
        {
            ("characters.title", "Nhân vật", "Characters"),
            ("characters.stat_hp", "HP", "HP"),
            ("characters.stat_attack", "Tấn công", "Attack"),
            ("characters.stat_defense", "Thủ", "Defense"),
            ("characters.stat_speed", "Tốc độ", "Speed"),
            ("characters.max", "Tối đa", "Max"),
            ("characters.fragments", "Mảnh: {0} / {1}", "Fragments: {0} / {1}"),
            ("characters.fragments_max", "Mảnh: {0}", "Fragments: {0}"),
            ("characters.star_up", "Nâng lên {0}★ ({1} mảnh)", "Upgrade to {0}★ ({1} frags)"),
            ("characters.star_up_max", "Đã tối đa", "Maxed"),
            ("characters.done", "Đã lên {0}★", "Reached {0}★"),
            ("characters.not_saved", "Chưa lưu được dữ liệu", "Could not save your data"),
            ("characters.not_enough", "Không đủ mảnh", "Not enough fragments"),
            ("characters.error", "Không nâng sao được", "Cannot upgrade right now"),
        };
    }
}
