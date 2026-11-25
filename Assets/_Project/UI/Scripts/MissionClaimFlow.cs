using System;
using System.Threading.Tasks;
using BunDauPeo.Missions;

namespace BunDauPeo.UI
{
    // Nhận thưởng từ trang: nếu thể lực sẽ mất thì hỏi trước, Huỷ thì không gửi lệnh gì (null).
    public sealed class MissionClaimFlow
    {
        private readonly MissionService _service;
        private readonly Func<StaminaPreview, Task<bool>> _ask;

        public MissionClaimFlow(MissionService service, Func<StaminaPreview, Task<bool>> ask)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _ask = ask ?? throw new ArgumentNullException(nameof(ask));
        }

        // idOrNull = null là Nhận hết cả tab.
        public async Task<MissionClaimResult?> ClaimAsync(int tab, string idOrNull)
        {
            var preview = _service.PreviewStamina(tab, idOrNull);
            if (preview.Wasted > 0 && !await _ask(preview)) return null;
            return idOrNull == null ? await _service.ClaimAllAsync(tab) : await _service.ClaimAsync(idOrNull);
        }
    }
}
