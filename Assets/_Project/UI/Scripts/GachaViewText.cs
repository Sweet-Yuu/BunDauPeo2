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
            entry.IsNew ? "MỚI" : $"TRÙNG +{entry.FragmentsGained} mảnh";

        public static Color RarityColor(Rarity rarity) => rarity switch
        {
            Rarity.R => new Color(0.62f, 0.62f, 0.66f),
            Rarity.SR => new Color(0.35f, 0.62f, 1f),
            Rarity.SSR => new Color(1f, 0.82f, 0.25f),
            _ => throw new ArgumentOutOfRangeException(nameof(rarity), rarity, null),
        };

        public static string PityLine(int threshold, int pulls) =>
            threshold <= 0 ? "Đảm bảo SSR: tắt" : $"SSR đảm bảo sau: {Math.Max(0, threshold - pulls)} lượt";

        public static string StatusMessage(GachaPullStatus status, bool saved) => status switch
        {
            GachaPullStatus.Success => saved ? "" : "Cảnh báo: chưa lưu được dữ liệu, game sẽ thử lưu lại sau.",
            GachaPullStatus.UnknownBanner => "Không tìm thấy banner này.",
            GachaPullStatus.InvalidBanner => "Banner bị lỗi cấu hình nên không quay được.",
            GachaPullStatus.NotEnoughCurrency => "Không đủ Gem.",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
        };

        // Số lấy từ banner để chỉnh chi phí trong Inspector thì nút không ghi sai giá.
        public static string PullLabel(int count, IReadOnlyList<CurrencyAmount> cost)
        {
            var lines = new string[cost.Count];
            for (var i = 0; i < lines.Length; i++) lines[i] = $"{cost[i].Amount} {cost[i].Type}";
            return $"Quay {count} ({string.Join(" + ", lines)})";
        }
    }
}
