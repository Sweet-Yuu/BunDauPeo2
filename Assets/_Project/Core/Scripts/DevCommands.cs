using System;
using System.Threading.Tasks;
using BunDauPeo.Rules;
using UnityEngine;

namespace BunDauPeo.Core
{
    // Lệnh DEV chạy trên server (Gem và thể lực do server giữ, spec con 6.2): panel DEV gọi qua đây, không đổi Gem trên máy.
    // Server bản phát hành từ chối bằng DevOnly; ở đây chỉ log cảnh báo, không đổi gì.
    public sealed class DevCommands
    {
        private readonly IGameServer _server;
        private readonly CommandGate _gate;
        private readonly Func<ServerState, bool> _apply;

        public DevCommands(IGameServer server, CommandGate gate, Func<ServerState, bool> apply)
        {
            _server = server ?? throw new ArgumentNullException(nameof(server));
            _gate = gate ?? throw new ArgumentNullException(nameof(gate));
            _apply = apply ?? throw new ArgumentNullException(nameof(apply));
        }

        // true khi server đã chạy lệnh và state mới đã áp.
        public Task<bool> AddGemAsync(long amount) => RunAsync("DevAddGem", id => _server.DevAddGemAsync(id, amount));

        public Task<bool> RefillStaminaAsync() => RunAsync("DevRefillStamina", id => _server.DevRefillStaminaAsync(id));

        public Task<bool> ResetMissionsAsync() => RunAsync("DevResetMissions", id => _server.DevResetMissionsAsync(id));

        public Task<bool> AddStaminaAsync(int amount) => RunAsync("DevAddStamina", id => _server.DevAddStaminaAsync(id, amount));

        private async Task<bool> RunAsync(string name, Func<string, Task<OnlineResult<CommandResponse>>> call)
        {
            var result = await _gate.RunAsync(call);
            // Lỗi mạng cuối cùng: hộp mạng đã báo, không đổi gì.
            if (!result.Ok) return false;

            var response = result.Value;
            var code = (CommandCode)response.Code;
            if (code != CommandCode.Ok || response.State == null)
            {
                Debug.LogWarning($"{nameof(DevCommands)}: {name} refused by the server ({code})");
                return false;
            }

            _apply(response.State);
            return true;
        }
    }
}
