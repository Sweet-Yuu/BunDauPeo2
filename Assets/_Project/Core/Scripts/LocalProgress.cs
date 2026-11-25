using System;

namespace BunDauPeo.Core
{
    // Máy này đã có tiến độ (bản đệm save) chưa? Tiêu đề dùng để cảnh báo trước khi đăng nhập tài khoản khác.
    public sealed class LocalProgress
    {
        private readonly Func<bool> _exists;

        public LocalProgress(Func<bool> exists) => _exists = exists ?? throw new ArgumentNullException(nameof(exists));

        public bool Exists => _exists();
    }
}
