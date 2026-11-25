namespace BunDauPeo.UI
{
    // Khu "Cấp": cấp người chơi, cấp nhân vật, thuốc EXP (spec cấp nhân vật mục 8).
    public static partial class UiText
    {
        private static readonly (string Key, string Vi, string En)[] Level =
        {
            ("level.potion", "Thuốc EXP", "EXP Potion"),
            ("level.player", "Lv {0}", "Lv {0}"),
            ("level.player_exp", "{0}/{1}", "{0}/{1}"),
            ("level.player_max", "Tối đa", "MAX"),
            ("level.up_title", "Lên cấp!", "Level up!"),
            ("level.up_body", "Cấp người chơi: {0}\nTrần cấp nhân vật: {0}\nThưởng: {1} Coin + {2} Thuốc EXP", "Player level: {0}\nCharacter level cap: {0}\nReward: {1} Coin + {2} EXP Potion"),
            ("level.up_ok", "OK", "OK"),
            ("level.exp_gained", "EXP người chơi +{0}", "Player EXP +{0}"),
            ("characters.back", "Quay lại", "Back"),
            ("characters.detail_title", "Nâng cấp nhân vật", "Upgrade Character"),
            ("characters.tab_level", "Cấp", "Level"),
            ("characters.tab_star", "Sao", "Star"),
            ("characters.prev", "‹", "‹"),
            ("characters.next", "›", "›"),
            ("characters.level_line", "Lv {0}/{1}", "Lv {0}/{1}"),
            ("characters.level_now", "Cấp nhân vật  {0} → {1}", "Character level  {0} → {1}"),
            ("characters.level_max", "Cấp nhân vật  {0}", "Character level  {0}"),
            ("characters.level_cap", "Trần cấp (= cấp người chơi)  {0}", "Level cap (= player level)  {0}"),
            ("characters.need_potion", "Thuốc EXP  cần {0} · có {1}", "EXP Potion  need {0} · have {1}"),
            ("characters.need_coin", "Coin  cần {0} · có {1}", "Coin  need {0} · have {1}"),
            ("characters.level_up_btn", "Nâng cấp ({0} thuốc + {1} Coin)", "Level up ({0} potion + {1} Coin)"),
            ("characters.level_reason_player", "Cần nâng cấp người chơi", "Raise your player level first"),
            ("characters.level_reason_potion", "Thiếu thuốc EXP", "Not enough EXP Potion"),
            ("characters.level_reason_coin", "Thiếu Coin", "Not enough Coin"),
            ("characters.level_done", "Đã lên Lv {0}", "Reached Lv {0}"),
            ("characters.level_error", "Không nâng cấp được", "Cannot level up right now"),
        };
    }
}
