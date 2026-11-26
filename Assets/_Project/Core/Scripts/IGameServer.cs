using System.Collections.Generic;
using System.Threading.Tasks;
using BunDauPeo.Rules;

namespace BunDauPeo.Core
{
    // Lệnh lỗi thì không đổi gì phía server; người gọi cũng không đổi gì trong bộ nhớ (spec 5.2).
    // Lệnh game (GachaPull…) trả Ok kèm CommandResponse.Code: Code khác Ok là server từ chối theo luật, không phải lỗi mạng.
    public interface IGameServer
    {
        // "" khi chưa đăng nhập.
        string PlayerId { get; }

        // Đã đăng nhập thì trả Ok ngay, không tạo người chơi mới.
        Task<OnlineStatus> SignInAsync();

        // Gửi ServerProtocol.Version; nhớ SessionId trả về cho mọi lệnh sau.
        Task<OnlineResult<BootstrapResponse>> BootstrapAsync();

        // JSON khoá save, null nếu chưa có; nhớ write lock cho lần đẩy sau.
        Task<OnlineResult<string>> LoadSaveAsync();

        // Xung đột write lock (máy khác vừa ghi, hoặc lượt trước của máy này đã ghi mà mất trả lời) → OtherDevice; CloudSync phân biệt hai ca.
        Task<OnlineStatus> UploadSaveAsync(string json);

        // Mã phiên đang giữ trong khoá state (Protected); null khi chưa có state.
        Task<OnlineResult<string>> LoadActiveSessionIdAsync();

        // Xoá dữ liệu trên server, giữ tài khoản và ID; trả state game mới. Phiên khác đang giữ → OtherDevice.
        Task<OnlineResult<ServerState>> ResetPlayerAsync();

        // Xoá dữ liệu và tài khoản, xoá token phiên; PlayerId về "".
        Task<OnlineStatus> DeleteAccountAsync();

        // Cùng requestId thì server chạy một lần và trả lại kết quả đã ghi (spec con 6.1).
        Task<OnlineResult<CommandResponse>> GachaPullAsync(string requestId, string bannerId, int count);
        Task<OnlineResult<CommandResponse>> EnterStageAsync(string requestId, string stageId, IReadOnlyList<string> team);
        Task<OnlineResult<CommandResponse>> CompleteStageAsync(string requestId, string ticket, bool won);
        Task<OnlineResult<CommandResponse>> CompleteStoryAsync(string requestId, string stageId);
        Task<OnlineResult<CommandResponse>> StarUpAsync(string requestId, string characterId);
        Task<OnlineResult<CommandResponse>> DevAddGemAsync(string requestId, long amount);
        Task<OnlineResult<CommandResponse>> DevRefillStaminaAsync(string requestId);

        // Nhiệm vụ và quay miễn phí (đợt 3). clientDone: id việc tân thủ do máy báo (spec đợt 3 mục 6.1).
        Task<OnlineResult<CommandResponse>> ClaimMissionAsync(string requestId, string missionId, IReadOnlyList<string> clientDone);
        Task<OnlineResult<CommandResponse>> ClaimAllMissionsAsync(string requestId, int tab, IReadOnlyList<string> clientDone);
        Task<OnlineResult<CommandResponse>> GachaFreePullAsync(string requestId, string bannerId);
        Task<OnlineResult<CommandResponse>> DevResetMissionsAsync(string requestId);
        Task<OnlineResult<CommandResponse>> DevAddStaminaAsync(string requestId, int amount);

        // Quà từ server (đợt 4). ListGifts không cần requestId và không đổi state; Claim* như ClaimMission.
        Task<OnlineResult<CommandResponse>> ListGiftsAsync(string lang);
        Task<OnlineResult<CommandResponse>> ClaimGiftAsync(string requestId, string giftId);
        Task<OnlineResult<CommandResponse>> ClaimAllGiftsAsync(string requestId);
    }
}
