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
            // Online đợt 2 (spec con 7.1–7.3): màn chờ lệnh server và hộp bản cũ.
            ("online.sending", "Đang gửi…", "Sending…"),
            ("online.too_old_title", "Cần cập nhật", "Update required"),
            ("online.too_old_body", "Bản game này đã cũ.\nHãy cập nhật lên bản mới nhất để chơi tiếp.", "This version is out of date.\nPlease update to the latest version to keep playing."),
            // Online đợt 3 (spec con 8.3, 8.7): liên kết và đăng nhập tài khoản.
            ("account.row", "Tài khoản", "Account"),
            ("account.not_linked", "Chưa liên kết", "Not linked"),
            ("account.linked", "Đã liên kết: {0}", "Linked: {0}"),
            ("account.link_btn", "Liên kết tài khoản", "Link account"),
            ("account.signin_btn", "Đăng nhập bằng tài khoản", "Sign in with an account"),
            ("account.choose_link", "Liên kết tài khoản", "Link account"),
            ("account.choose_signin", "Đăng nhập bằng tài khoản", "Sign in with an account"),
            ("account.google", "Google", "Google"),
            ("account.userpass", "Tên đăng nhập và mật khẩu", "Username and password"),
            ("account.username", "Tên đăng nhập", "Username"),
            ("account.password", "Mật khẩu", "Password"),
            ("account.create", "Tạo tài khoản", "Create account"),
            ("account.signin", "Đăng nhập", "Sign in"),
            ("account.err_taken", "Tên đã được dùng", "Username already taken"),
            ("account.err_weak", "Mật khẩu chưa đủ mạnh", "Password is too weak"),
            ("account.err_wrong", "Sai tên đăng nhập hoặc mật khẩu", "Wrong username or password"),
            // Thêm ngoài spec 8.7: luật tên (kiểm phía máy) và lỗi không rõ.
            ("account.err_name", "Tên 3–20 ký tự: chữ, số, . - @ _", "Username: 3–20 letters, digits, . - @ _"),
            ("account.err_other", "Không dùng được lúc này", "Not available right now"),
            ("account.hint_pass", "Mật khẩu 8–30 ký tự, có chữ hoa, chữ thường, số và ký hiệu", "Password: 8–30 characters with upper and lower case, a digit and a symbol"),
            ("account.warn_lose", "Tiến độ hiện tại (chưa liên kết) sẽ mất.", "Your current progress (not linked) will be lost."),
            ("account.conflict_title", "Tài khoản này đã có tiến độ khác", "This account already has other progress"),
            ("account.conflict_body", "Chuyển sang tiến độ đó? Tiến độ hiện tại (chưa liên kết) sẽ mất.",
                "Switch to that progress? Your current progress (not linked) will be lost."),
            ("account.switch", "Chuyển", "Switch"),
            ("account.confirm_title", "Mất tiến độ hiện tại?", "Lose current progress?"),
        };
    }
}
