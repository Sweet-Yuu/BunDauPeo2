using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BunDauPeo.Rules;
using Newtonsoft.Json;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Apis.Extensions;
using Unity.Services.CloudCode.Core;

namespace BunDauPeoServer;

// GameData.json (xuất từ ScriptableObject bằng Tools/BunDauPeo/Export Server Data) nhúng vào module, nạp một lần.
public static class GameDataSource
{
    static readonly Lazy<(GameData data, string json)> Loaded = new(Load);

    public static GameData Data => Loaded.Value.data;
    public static string Json => Loaded.Value.json;
    public static string Hash => DataHash.Of(Json);

    static (GameData, string) Load()
    {
        var assembly = typeof(GameDataSource).Assembly;
        var name = assembly.GetManifestResourceNames().Single(n => n.EndsWith("GameData.json", StringComparison.Ordinal));
        using var reader = new StreamReader(assembly.GetManifestResourceStream(name)!);
        var json = reader.ReadToEnd();
        return (JsonConvert.DeserializeObject<GameData>(json)!, json);
    }
}

public class ServerModule
{
    // Ghi xung đột write lock (hai lượt cùng lúc): đọc lại và chạy lại tối đa 3 lần, rồi máy thấy ServerError.
    const int MaxAttempts = 3;
    const string DevelopmentEnvironment = "development";

    // ---- Lệnh (spec con 6.2) ----

    [CloudCodeFunction(ServerProtocol.Bootstrap)]
    public Task<BootstrapResponse> Bootstrap(IExecutionContext ctx, IGameApiClient api, int protocolVersion = 0) =>
        BootstrapAsync(new CloudSaveStateStore(ctx, api), protocolVersion, () => DateTime.UtcNow);

    [CloudCodeFunction(ServerProtocol.ResetPlayer)]
    public Task<CommandResponse> ResetPlayer(IExecutionContext ctx, IGameApiClient api,
        int protocolVersion, string sessionId, bool recreate) =>
        ResetAsync(new CloudSaveStateStore(ctx, api), protocolVersion, sessionId, recreate, GameDataSource.Data, () => DateTime.UtcNow);

    [CloudCodeFunction(ServerProtocol.GachaPull)]
    public Task<CommandResponse> GachaPull(IExecutionContext ctx, IGameApiClient api,
        int protocolVersion, string sessionId, string requestId, string bannerId, int count) =>
        Command(ctx, api, protocolVersion, sessionId, requestId,
            (s, c) => ServerHandlers.GachaPull(s, GameDataSource.Data, c, bannerId, count));

    [CloudCodeFunction(ServerProtocol.EnterStage)]
    public Task<CommandResponse> EnterStage(IExecutionContext ctx, IGameApiClient api,
        int protocolVersion, string sessionId, string requestId, string stageId, List<string> team) =>
        Command(ctx, api, protocolVersion, sessionId, requestId,
            (s, c) => ServerHandlers.EnterStage(s, GameDataSource.Data, c, stageId, team ?? new List<string>()));

    [CloudCodeFunction(ServerProtocol.CompleteStage)]
    public Task<CommandResponse> CompleteStage(IExecutionContext ctx, IGameApiClient api,
        int protocolVersion, string sessionId, string requestId, string ticket, bool won) =>
        Command(ctx, api, protocolVersion, sessionId, requestId,
            (s, c) => ServerHandlers.CompleteStage(s, GameDataSource.Data, c, ticket, won));

    [CloudCodeFunction(ServerProtocol.CompleteStory)]
    public Task<CommandResponse> CompleteStory(IExecutionContext ctx, IGameApiClient api,
        int protocolVersion, string sessionId, string requestId, string stageId) =>
        Command(ctx, api, protocolVersion, sessionId, requestId,
            (s, c) => ServerHandlers.CompleteStory(s, GameDataSource.Data, stageId));

    [CloudCodeFunction(ServerProtocol.StarUp)]
    public Task<CommandResponse> StarUp(IExecutionContext ctx, IGameApiClient api,
        int protocolVersion, string sessionId, string requestId, string characterId) =>
        Command(ctx, api, protocolVersion, sessionId, requestId,
            (s, c) => ServerHandlers.StarUp(s, GameDataSource.Data, characterId));

    [CloudCodeFunction(ServerProtocol.DevAddGem)]
    public Task<CommandResponse> DevAddGem(IExecutionContext ctx, IGameApiClient api,
        int protocolVersion, string sessionId, string requestId, long amount) =>
        Command(ctx, api, protocolVersion, sessionId, requestId,
            (s, c) => ServerHandlers.DevAddGem(s, amount), ctx.EnvironmentName);

    [CloudCodeFunction(ServerProtocol.DevRefillStamina)]
    public Task<CommandResponse> DevRefillStamina(IExecutionContext ctx, IGameApiClient api,
        int protocolVersion, string sessionId, string requestId) =>
        Command(ctx, api, protocolVersion, sessionId, requestId,
            (s, c) => ServerHandlers.DevRefillStamina(s, GameDataSource.Data), ctx.EnvironmentName);

    [CloudCodeFunction(ServerProtocol.ClaimMission)]
    public Task<CommandResponse> ClaimMission(IExecutionContext ctx, IGameApiClient api,
        int protocolVersion, string sessionId, string requestId, string missionId, List<string> clientDone) =>
        Command(ctx, api, protocolVersion, sessionId, requestId,
            (s, c) => ServerHandlers.ClaimMission(s, GameDataSource.Data, c, missionId, clientDone ?? new List<string>()));

    [CloudCodeFunction(ServerProtocol.ClaimAllMissions)]
    public Task<CommandResponse> ClaimAllMissions(IExecutionContext ctx, IGameApiClient api,
        int protocolVersion, string sessionId, string requestId, int tab, List<string> clientDone) =>
        Command(ctx, api, protocolVersion, sessionId, requestId,
            (s, c) => ServerHandlers.ClaimAllMissions(s, GameDataSource.Data, c, tab, clientDone ?? new List<string>()));

    [CloudCodeFunction(ServerProtocol.GachaFreePull)]
    public Task<CommandResponse> GachaFreePull(IExecutionContext ctx, IGameApiClient api,
        int protocolVersion, string sessionId, string requestId, string bannerId) =>
        Command(ctx, api, protocolVersion, sessionId, requestId,
            (s, c) => ServerHandlers.GachaFreePull(s, GameDataSource.Data, c, bannerId));

    [CloudCodeFunction(ServerProtocol.DevResetMissions)]
    public Task<CommandResponse> DevResetMissions(IExecutionContext ctx, IGameApiClient api,
        int protocolVersion, string sessionId, string requestId) =>
        Command(ctx, api, protocolVersion, sessionId, requestId,
            (s, c) => ServerHandlers.DevResetMissions(s), ctx.EnvironmentName);

    [CloudCodeFunction(ServerProtocol.DevAddStamina)]
    public Task<CommandResponse> DevAddStamina(IExecutionContext ctx, IGameApiClient api,
        int protocolVersion, string sessionId, string requestId, int amount) =>
        Command(ctx, api, protocolVersion, sessionId, requestId,
            (s, c) => ServerHandlers.DevAddStamina(s, GameDataSource.Data, c, amount), ctx.EnvironmentName);

    [CloudCodeFunction(ServerProtocol.ListGifts)]
    public async Task<CommandResponse> ListGifts(IExecutionContext ctx, IGameApiClient api,
        int protocolVersion, string sessionId, string lang = "vi") =>
        await ListGiftsAsync(new CloudSaveStateStore(ctx, api), new CloudSaveGiftSource(ctx, api),
            protocolVersion, sessionId, lang ?? "vi", () => DateTime.UtcNow);

    [CloudCodeFunction(ServerProtocol.ClaimGift)]
    public async Task<CommandResponse> ClaimGift(IExecutionContext ctx, IGameApiClient api,
        int protocolVersion, string sessionId, string requestId, string giftId)
    {
        var gifts = await new CloudSaveGiftSource(ctx, api).LoadAsync();
        return await Command(ctx, api, protocolVersion, sessionId, requestId,
            (s, c) => ServerHandlers.ClaimGift(s, gifts, c, giftId));
    }

    [CloudCodeFunction(ServerProtocol.ClaimAllGifts)]
    public async Task<CommandResponse> ClaimAllGifts(IExecutionContext ctx, IGameApiClient api,
        int protocolVersion, string sessionId, string requestId)
    {
        var gifts = await new CloudSaveGiftSource(ctx, api).LoadAsync();
        return await Command(ctx, api, protocolVersion, sessionId, requestId,
            (s, c) => ServerHandlers.ClaimAllGifts(s, gifts, c));
    }

    // Chỉ đọc: không ghi state, không cần requestId. Đọc quà lỗi thì lỗi nổi lên, không thành danh sách rỗng.
    public static async Task<CommandResponse> ListGiftsAsync(IStateStore store, IGiftSource source,
        int protocolVersion, string sessionId, string lang, Func<DateTime> now)
    {
        var gifts = await source.LoadAsync();
        var (state, _) = await store.LoadAsync();
        var request = new ServerRequest { ProtocolVersion = protocolVersion, SessionId = sessionId ?? "" };
        var ctx = NewContext(now());
        var response = CommandRunner.RunReadOnly(state, request, s => ServerHandlers.ListGifts(s, gifts, ctx, lang), ToJson);
        response.ServerUtcTicks = now().Ticks;
        return response;
    }

    // ---- Phần chạy được không cần Cloud Code (dotnet test dùng kho giả) ----

    static Task<CommandResponse> Command(IExecutionContext ctx, IGameApiClient api,
        int protocolVersion, string sessionId, string requestId, Func<ServerState, ServerContext, HandlerOutcome> handler,
        string? devEnvironment = null)
    {
        var store = new CloudSaveStateStore(ctx, api);
        var request = new ServerRequest { ProtocolVersion = protocolVersion, SessionId = sessionId ?? "", RequestId = requestId ?? "" };
        var serverCtx = NewContext(DateTime.UtcNow);
        Func<ServerState, HandlerOutcome> run = s => handler(s, serverCtx);
        return devEnvironment == null
            ? Execute(store, request, run, () => serverCtx.NowUtc)
            : ExecuteDev(store, devEnvironment, request, run, () => serverCtx.NowUtc);
    }

    public static ServerContext NewContext(DateTime utcNow) => new()
    {
        NowUtc = utcNow,
        Random = new Random(),
        NewId = () => Guid.NewGuid().ToString("N"),
    };

    public static string ToJson(object value) => StateJson.To(value);

    // Bước 1–6 của spec con 6.1 quanh kho: CommandRunner.Run, ghi ToWrite bằng write lock, xung đột thì đọc lại và chạy lại.
    public static async Task<CommandResponse> Execute(IStateStore store, ServerRequest request,
        Func<ServerState, HandlerOutcome> handler, Func<DateTime> now)
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var (state, writeLock) = await store.LoadAsync();
            var run = CommandRunner.Run(state, request, handler, ToJson);
            if (run.ToWrite != null)
            {
                try { await store.SaveAsync(run.ToWrite, writeLock); }
                catch (LockConflictException) { continue; }
            }
            run.Response.ServerUtcTicks = now().Ticks;
            return run.Response;
        }
        throw new InvalidOperationException($"State write lock still conflicting after {MaxAttempts} attempts");
    }

    // Lệnh Dev* chỉ chạy ở môi trường development; môi trường không đọc được thì từ chối.
    public static async Task<CommandResponse> ExecuteDev(IStateStore store, string? environmentName, ServerRequest request,
        Func<ServerState, HandlerOutcome> handler, Func<DateTime> now)
    {
        if (request.ProtocolVersion < ServerProtocol.MinProtocolVersion) throw new ClientTooOldException(request.ProtocolVersion);
        if (!string.Equals(environmentName, DevelopmentEnvironment, StringComparison.Ordinal))
            return new CommandResponse { Code = (int)CommandCode.DevOnly, ServerUtcTicks = now().Ticks };
        return await Execute(store, request, handler, now);
    }

    public static async Task<BootstrapResponse> BootstrapAsync(IStateStore store, int protocolVersion, Func<DateTime> now)
    {
        if (protocolVersion < ServerProtocol.MinProtocolVersion) throw new ClientTooOldException(protocolVersion);
        var ctx = NewContext(now());
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var (stored, writeLock) = await store.LoadAsync();
            var outcome = ServerHandlers.Bootstrap(stored?.Clone(), GameDataSource.Data, ctx);
            var state = outcome.State;
            try { await store.SaveAsync(state, writeLock); }
            catch (LockConflictException) { continue; }
            return new BootstrapResponse
            {
                ServerUtcTicks = now().Ticks,
                SessionId = state.ActiveSessionId,
                DataHash = GameDataSource.Hash,
                State = state,
            };
        }
        throw new InvalidOperationException($"State write lock still conflicting after {MaxAttempts} attempts");
    }

    // Bước 1 và 3 của 6.1 (khi có state); bỏ bước 2 (chưa có state vẫn xoá được) và bước 4 (Reset lại lần nữa vô hại).
    public static async Task<CommandResponse> ResetAsync(IStateStore store, int protocolVersion, string sessionId, bool recreate,
        GameData data, Func<DateTime> now)
    {
        if (protocolVersion < ServerProtocol.MinProtocolVersion) throw new ClientTooOldException(protocolVersion);
        var (stored, _) = await store.LoadAsync();
        if (stored != null && sessionId != stored.ActiveSessionId)
            return new CommandResponse { Code = (int)CommandCode.OtherDevice, ServerUtcTicks = now().Ticks };

        await store.DeleteAllAsync();
        var response = new CommandResponse { ServerUtcTicks = now().Ticks };
        if (recreate)
        {
            var fresh = ServerHandlers.ResetState(data, sessionId);
            await store.SaveAsync(fresh, null);
            response.State = fresh;
        }
        return response;
    }
}

public class ModuleSetup : ICloudCodeSetup
{
    public void Setup(ICloudCodeConfig config) => config.AddGameApiClient();
}
