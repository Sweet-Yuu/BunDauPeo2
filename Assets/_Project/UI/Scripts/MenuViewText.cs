namespace BunDauPeo.UI
{
    // Chữ của Menu hub nằm ở đây để test được mà không cần dựng scene.
    public static class MenuViewText
    {
        public static string LanguageLabel() => UiText.Get("hub.language");
        public static string LanguageNotSaved() => UiText.Get("hub.language_not_saved");
        public static string ResetFailed() => UiText.Get("hub.reset_failed");
        public static string ComingSoon() => UiText.Get("hub.coming_soon");
    }
}
