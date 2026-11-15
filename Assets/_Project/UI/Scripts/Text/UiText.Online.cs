namespace BunDauPeo.UI
{
    public static partial class UiText
    {
        // Online đợt 1 (spec 5.14): bước kết nối ở Tiêu đề và hộp mạng (OnlineOverlay).
        private static readonly (string Key, string Vi, string En)[] Online =
        {
            ("online.connecting", "Đang kết nối…", "Connecting…"),
            ("online.connect_failed_title", "Không kết nối được", "Can't connect"),
            ("online.connect_failed_body", "Cần có mạng để chơi. Kiểm tra mạng rồi thử lại.", "An internet connection is required. Check your connection and try again."),
            ("online.lost_title", "Mất kết nối", "Connection lost"),
            ("online.lost_body", "Đang chờ kết nối lại với máy chủ.", "Waiting to reconnect to the server."),
            ("online.retry", "Thử lại", "Retry"),
            ("online.other_device_title", "Tài khoản đang dùng ở máy khác", "Account in use on another device"),
            ("online.other_device_body", "Tài khoản này vừa đăng nhập ở máy khác. Về màn Tiêu đề để tải lại.", "This account just signed in on another device. Return to the title screen to reload."),
            ("online.to_title", "Về màn Tiêu đề", "Back to title"),
        };
    }
}
