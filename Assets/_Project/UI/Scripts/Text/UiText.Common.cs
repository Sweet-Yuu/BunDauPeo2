namespace BunDauPeo.UI
{
    // Chữ dùng chung nhiều khu. Khu nào thiếu chữ chung thì đặt key trong file khu của mình, không thêm vào đây.
    public static partial class UiText
    {
        private static readonly (string Key, string Vi, string En)[] Common =
        {
            ("common.close", "Đóng", "Close"),
            ("common.cancel", "Hủy", "Cancel"),
            ("common.continue", "Tiếp tục", "Continue"),
            ("common.no", "Không", "No"),
            ("common.not_now", "Thôi", "Not now"),
            ("common.move", "Chuyển", "Move"),
            ("common.role_cook", "Đầu bếp", "Cook"),
            ("common.role_server", "Phục vụ", "Server"),
            ("common.role_cashier", "Thu ngân", "Cashier"),
        };
    }
}
