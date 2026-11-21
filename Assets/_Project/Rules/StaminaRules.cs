#nullable disable
using System;
using BunDauPeo.Core;

namespace BunDauPeo.Rules
{
    // Thể lực hồi theo giờ truyền vào (server dùng giờ server). Anchor = 0 nghĩa là đầy. Cùng logic StaminaService cũ, trên số long ticks.
    public static class StaminaRules
    {
        // regenTicks <= 0 (config hồi lỗi) thì không bao giờ hồi, khỏi chia cho 0 (L-016).
        public static void Settle(StaminaState state, long nowTicks, int max, long regenTicks)
        {
            max = Math.Max(0, max);
            // Anchor 0 = không đang hồi: thiếu so với Max thì coi là đầy, vượt Max (nhận thêm) thì giữ nguyên.
            if (state.AnchorUtcTicks == 0)
            {
                state.Current = Math.Max(state.Current, max);
                return;
            }

            var current = Math.Max(0, state.Current);
            if (current >= max)
            {
                state.Current = current;
                state.AnchorUtcTicks = 0;
                return;
            }

            if (nowTicks < state.AnchorUtcTicks)
            {
                // Đồng hồ lùi: không hồi và không trừ, chỉ dời mốc về hiện tại.
                state.AnchorUtcTicks = nowTicks;
            }
            else if (regenTicks > 0)
            {
                var points = (nowTicks - state.AnchorUtcTicks) / regenTicks;
                current = (int)Math.Min(max, current + points);
                state.AnchorUtcTicks += points * regenTicks;
            }

            if (current >= max)
            {
                current = max;
                state.AnchorUtcTicks = 0;
            }

            state.Current = current;
        }

        // Chốt phần hồi rồi trừ. Từ chối (cost âm hoặc thiếu) thì chỉ còn phần chốt, không trừ gì.
        public static bool TrySpend(StaminaState state, int cost, long nowTicks, int max, long regenTicks)
        {
            Settle(state, nowTicks, max, regenTicks);
            if (cost < 0) return false;
            if (cost == 0) return true;
            if (state.Current < cost) return false;

            state.Current -= cost;
            // Rơi xuống dưới Max từ đầy thì bắt đầu hồi từ bây giờ; còn ≥ Max (vượt trần) thì chưa hồi; đang hồi thì giữ mốc.
            if (state.AnchorUtcTicks == 0 && state.Current < max) state.AnchorUtcTicks = nowTicks;
            return true;
        }

        // Cộng thể lực thưởng, tối đa tới cap (có thể > max). Trả phần thực cộng; mất = amount - granted.
        public static int Add(StaminaState state, int amount, long nowTicks, int max, long regenTicks, int cap)
        {
            Settle(state, nowTicks, max, regenTicks);
            if (amount <= 0) return 0;
            var before = state.Current;
            var after = (int)Math.Max(before, Math.Min((long)cap, (long)before + amount));
            state.Current = after;
            if (after >= max) state.AnchorUtcTicks = 0;
            return after - before;
        }
    }
}
