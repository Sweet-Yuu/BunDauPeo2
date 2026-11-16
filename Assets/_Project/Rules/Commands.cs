#nullable disable
using System;

namespace BunDauPeo.Rules
{
    // Số theo thứ tự này, không đổi về sau (JsonUtility và Newtonsoft đều ghi enum dạng số).
    public enum CommandCode
    {
        Ok, Invalid, NotEnoughGem, Locked, WrongKind, EmptyTeam, NotOwned, NotEnoughStamina,
        NotEnoughFragments, MaxStars, AlreadyCleared, TicketMismatch, OtherDevice, NoState, DevOnly,
    }

    [Serializable]
    public class ServerRequest
    {
        public int ProtocolVersion;
        public string SessionId = "";
        public string RequestId = "";
    }

    [Serializable]
    public class CommandResponse
    {
        public int Code;
        public long ServerUtcTicks;
        public ServerState State;
        public string ResultJson = "";
    }

    public class HandlerOutcome
    {
        public CommandCode Code;
        public ServerState State;
        public object Payload;

        public static HandlerOutcome Ok(ServerState state, object payload = null) =>
            new HandlerOutcome { Code = CommandCode.Ok, State = state, Payload = payload };

        public static HandlerOutcome Reject(CommandCode code) => new HandlerOutcome { Code = code };
    }

    public class ServerContext
    {
        public DateTime NowUtc;
        public Random Random;
        public Func<string> NewId;
    }

    public class ClientTooOldException : Exception
    {
        public ClientTooOldException(int version)
            : base($"{ServerProtocol.ClientTooOldTag}: protocol {version} < {ServerProtocol.MinProtocolVersion}") { }
    }
}
