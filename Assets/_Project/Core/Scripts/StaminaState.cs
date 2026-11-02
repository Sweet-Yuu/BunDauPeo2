using System;

namespace BunDauPeo.Core
{
    // Anchor = 0 nghĩa là đầy (không có gì đang hồi); khác 0 là mốc UTC của điểm hồi kế tiếp tính từ đó. StaminaService là chỗ duy nhất đọc và ghi.
    [Serializable]
    public class StaminaState
    {
        public int Current;
        public long AnchorUtcTicks;
    }
}
