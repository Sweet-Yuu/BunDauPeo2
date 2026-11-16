#nullable disable
using System;

namespace BunDauPeo.Rules
{
    public class CommandRun
    {
        public CommandResponse Response;
        public ServerState ToWrite;   // null = không ghi gì
    }

    // Bước 1–6 của spec con 6.1, không dính kho lưu: người gọi đọc stored (có write lock) và ghi ToWrite.
    public static class CommandRunner
    {
        public static CommandRun Run(ServerState stored, ServerRequest request,
            Func<ServerState, HandlerOutcome> handler, Func<object, string> toJson)
        {
            if (request.ProtocolVersion < ServerProtocol.MinProtocolVersion)
                throw new ClientTooOldException(request.ProtocolVersion);
            if (stored == null) return Reject(CommandCode.NoState);
            if (request.SessionId != stored.ActiveSessionId) return Reject(CommandCode.OtherDevice);

            // RequestId rỗng không bao giờ là "lệnh đã chạy": state mới có LastRequestId rỗng.
            if (request.RequestId.Length > 0 && request.RequestId == stored.LastRequestId)
                return new CommandRun { Response = new CommandResponse { Code = (int)CommandCode.Ok, State = stored, ResultJson = stored.LastResultJson } };

            var outcome = handler(stored.Clone());
            if (outcome.Code != CommandCode.Ok) return Reject(outcome.Code);

            var state = outcome.State;
            state.Revision++;
            state.LastRequestId = request.RequestId;
            state.LastResultJson = outcome.Payload == null ? "" : toJson(outcome.Payload);
            return new CommandRun
            {
                Response = new CommandResponse { Code = (int)CommandCode.Ok, State = state, ResultJson = state.LastResultJson },
                ToWrite = state,
            };
        }

        static CommandRun Reject(CommandCode code) =>
            new CommandRun { Response = new CommandResponse { Code = (int)code } };
    }
}
