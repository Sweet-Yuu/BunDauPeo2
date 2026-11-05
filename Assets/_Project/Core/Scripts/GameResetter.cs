using System;

namespace BunDauPeo.Core
{
    // Cầu nối để UI gọi Reset mà không phụ thuộc Boot (UI và Boot là hai assembly ngang hàng, xem structure.md).
    public sealed class GameResetter
    {
        private readonly Func<bool> _reset;

        public GameResetter(Func<bool> reset) => _reset = reset ?? throw new ArgumentNullException(nameof(reset));

        // Chỉ gọi sau khi người chơi đã xác nhận (D-005).
        public bool Reset() => _reset();
    }
}
