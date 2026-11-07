using System;
using System.Collections.Generic;
using BunDauPeo.Characters;
using BunDauPeo.Core;
using BunDauPeo.Gacha;
using UnityEngine;

namespace BunDauPeo.UI
{
    // Mọi chọn chữ và màu của màn Gacha nằm ở đây để test được mà không cần dựng scene.
    public static class GachaViewText
    {
        public static string Describe(GachaPullEntry entry) =>
            entry.IsNew ? UiText.Get("gacha.new") : UiText.Format("gacha.duplicate", entry.FragmentsGained);

        public static Color RarityColor(Rarity rarity) => rarity switch
        {
            Rarity.R => new Color(0.62f, 0.62f, 0.66f),
            Rarity.SR => new Color(0.35f, 0.62f, 1f),
            Rarity.SSR => new Color(1f, 0.82f, 0.25f),
            _ => throw new ArgumentOutOfRangeException(nameof(rarity), rarity, null),
        };

        // Tối thiểu 1: bộ đếm đã chạm hoặc vượt ngưỡng (hạ ngưỡng sau khi đã quay) thì lượt tới vẫn là lượt ép SSR.
        public static string PityLine(int threshold, int pulls) =>
            threshold <= 0 ? UiText.Get("gacha.pity_off") : UiText.Format("gacha.pity_left", Math.Max(1, threshold - pulls));

        public static string StatusMessage(GachaPullStatus status, bool saved) => status switch
        {
            GachaPullStatus.Success => saved ? "" : UiText.Get("gacha.status_unsaved"),
            GachaPullStatus.UnknownBanner => UiText.Get("gacha.status_unknown_banner"),
            GachaPullStatus.InvalidBanner => UiText.Get("gacha.status_invalid_banner"),
            GachaPullStatus.NotEnoughCurrency => UiText.Get("gacha.status_not_enough"),
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
        };

        // Số lấy từ banner để chỉnh chi phí trong Inspector thì nút không ghi sai giá.
        public static string PullLabel(int count, IReadOnlyList<CurrencyAmount> cost) => UiText.Format("gacha.pull_label", count, CostText(cost));

        public static string RollAgainLabel(IReadOnlyList<CurrencyAmount> cost) => UiText.Format("gacha.roll_again_cost", CostText(cost));

        public static string InsufficientGemText(long needed) => UiText.Format("gacha.need_gem", needed);

        // Điều kiện duy nhất để bật nút Quay tiếp: đủ tiền cho đúng lượt vừa quay.
        public static bool CanRollAgain(Wallet wallet, CurrencyAmount[] cost) => wallet.CanAfford(cost);

        public static string StarsText(int stars) => new string('★', stars);

        // Ví dụ: "14:02 31/10 · Quay 10 · Nem Chua, Lòng Dồi, ..."
        public static string HistoryLine(GachaHistoryEntry entry, Func<string, string> nameOf)
        {
            var time = DateTimeOffset.FromUnixTimeSeconds(entry.Utc).ToLocalTime().ToString("HH:mm dd/MM");
            var names = new List<string>(entry.CharacterIds.Length);
            foreach (var id in entry.CharacterIds) names.Add(nameOf(id));
            return UiText.Format("gacha.history_line", time, entry.Count, string.Join(", ", names));
        }

        private static string CostText(IReadOnlyList<CurrencyAmount> cost)
        {
            var lines = new string[cost.Count];
            for (var i = 0; i < lines.Length; i++) lines[i] = $"{cost[i].Amount} {cost[i].Type}";
            return string.Join(" + ", lines);
        }
    }
}
