using BunDauPeo.Rules;
using BunDauPeoServer;
using Newtonsoft.Json;

namespace BunDauPeoServer.Tests;

// Kho giả trong bộ nhớ: đếm write lock như Cloud Save, cho phép chèn xung đột và ghi của "máy khác".
public class FakeStore : IStateStore
{
    public ServerState? State;
    public int Lock;
    public int Saves;
    public int DeleteAllCalls;
    public int ConflictsToInject;
    public Action? OnConflict;   // chạy khi chèn xung đột: giả lập lượt khác ghi trước

    public Task<(ServerState? state, string? writeLock)> LoadAsync() =>
        Task.FromResult((State?.Clone(), State == null ? null : Lock.ToString()));

    public Task<string?> SaveAsync(ServerState state, string? writeLock)
    {
        if (ConflictsToInject > 0)
        {
            ConflictsToInject--;
            OnConflict?.Invoke();
            throw new LockConflictException();
        }
        if (State != null && writeLock != Lock.ToString()) throw new LockConflictException();
        State = state.Clone();
        Saves++;
        Lock++;
        return Task.FromResult<string?>(Lock.ToString());
    }

    public Task DeleteAllAsync()
    {
        DeleteAllCalls++;
        State = null;
        return Task.CompletedTask;
    }
}

public class ServerModuleTests
{
    static readonly DateTime Now = new(2026, 11, 16, 10, 0, 0, DateTimeKind.Utc);

    static ServerRequest Req(string id, int version = ServerProtocol.Version, string session = "sess") =>
        new() { ProtocolVersion = version, SessionId = session, RequestId = id };

    static FakeStore StoreWithGem(long gem) =>
        new() { State = new ServerState { ActiveSessionId = "sess", Gem = gem } };

    static HandlerOutcome Spend10(ServerState s)
    {
        s.Gem -= 10;
        return HandlerOutcome.Ok(s);
    }

    [Test]
    public void GameData_EmbeddedLoads_HashMatchesFile()
    {
        var file = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "../../../../BunDauPeoServer/GameData.json"));

        Assert.That(GameDataSource.Json, Is.EqualTo(file));
        Assert.That(GameDataSource.Hash, Is.EqualTo(DataHash.Of(file)));
        Assert.That(GameDataSource.Data.Banners, Is.Not.Empty);
    }

    [Test]
    public void Bootstrap_VersionMissingOrOld_Throws_ClientTooOld()
    {
        foreach (var v in new[] { 0, 1 })
        {
            var store = new FakeStore();
            var ex = Assert.ThrowsAsync<ClientTooOldException>(() => ServerModule.BootstrapAsync(store, v, () => Now));
            Assert.That(ex!.Message, Does.StartWith(ServerProtocol.ClientTooOldTag));
            Assert.That(store.Saves, Is.EqualTo(0), "v=" + v);
        }
    }

    [Test]
    public async Task Bootstrap_NewGame_ThenKeepsStateWithNewSession()
    {
        var store = new FakeStore();

        var first = await ServerModule.BootstrapAsync(store, ServerProtocol.Version, () => Now);
        store.State!.Gem += 7;
        var second = await ServerModule.BootstrapAsync(store, ServerProtocol.Version, () => Now);

        Assert.That(first.State!.Gem, Is.EqualTo(GameDataSource.Data.NewGame.StartingGem));
        Assert.That(first.DataHash, Is.EqualTo(GameDataSource.Hash));
        Assert.That(second.State!.Gem, Is.EqualTo(GameDataSource.Data.NewGame.StartingGem + 7));
        Assert.That(second.SessionId, Is.Not.EqualTo(first.SessionId).And.Not.Empty);
        Assert.That(second.State.Revision, Is.EqualTo(first.State.Revision + 1));
        Assert.That(second.ServerUtcTicks, Is.EqualTo(Now.Ticks));
    }

    [Test]
    public async Task Execute_LockConflict_RetriesThenReplays()
    {
        // Lượt đầu ghi bị xung đột, nhưng giữa chừng lượt "cùng RequestId" của chính máy đã ghi xong: đọc lại thấy trùng, trả kết quả cũ.
        var store = StoreWithGem(100);
        var calls = 0;
        store.ConflictsToInject = 1;
        store.OnConflict = () =>
        {
            var landed = store.State!.Clone();
            landed.Gem -= 10;
            landed.Revision++;
            landed.LastRequestId = "r1";
            landed.LastResultJson = "{\"x\":1}";
            store.State = landed;
            store.Lock++;
        };

        var response = await ServerModule.Execute(store, Req("r1"), s => { calls++; return Spend10(s); }, () => Now);

        Assert.That(response.Code, Is.EqualTo((int)CommandCode.Ok));
        Assert.That(response.ResultJson, Is.EqualTo("{\"x\":1}"));
        Assert.That(store.State!.Gem, Is.EqualTo(90), "Gem trừ đúng một lần");
        Assert.That(store.Saves, Is.EqualTo(0), "lượt chạy lại không ghi nữa");
        Assert.That(calls, Is.EqualTo(1));
    }

    [Test]
    public async Task Execute_OneConflict_NoLandedWrite_RetriesAndSucceeds()
    {
        var store = StoreWithGem(100);
        store.ConflictsToInject = 1;

        var response = await ServerModule.Execute(store, Req("r1"), Spend10, () => Now);

        Assert.That(response.Code, Is.EqualTo((int)CommandCode.Ok));
        Assert.That(store.State!.Gem, Is.EqualTo(90));
        Assert.That(store.Saves, Is.EqualTo(1));
    }

    [Test]
    public void Execute_ThreeConflicts_Throws()
    {
        var store = StoreWithGem(100);
        store.ConflictsToInject = 3;

        Assert.ThrowsAsync<InvalidOperationException>(() => ServerModule.Execute(store, Req("r1"), Spend10, () => Now));
        Assert.That(store.State!.Gem, Is.EqualTo(100));
    }

    [Test]
    public async Task ResetPlayer_DeletesDefaultAndProtected_RecreateKeepsSession()
    {
        var store = StoreWithGem(5);
        store.State!.OwnedCharacterIds.Add("x");

        var response = await ServerModule.ResetAsync(store, ServerProtocol.Version, "sess", true, GameDataSource.Data, () => Now);

        Assert.That(store.DeleteAllCalls, Is.EqualTo(1));
        Assert.That(store.State!.ActiveSessionId, Is.EqualTo("sess"));
        Assert.That(store.State.Gem, Is.EqualTo(GameDataSource.Data.NewGame.StartingGem));
        Assert.That(store.State.OwnedCharacterIds, Is.EqualTo(GameDataSource.Data.NewGame.StarterCharacterIds));
        Assert.That(response.State!.ActiveSessionId, Is.EqualTo("sess"));
    }

    [Test]
    public async Task ResetPlayer_RecreateFalse_LeavesNoState()
    {
        var store = StoreWithGem(5);

        var response = await ServerModule.ResetAsync(store, ServerProtocol.Version, "sess", false, GameDataSource.Data, () => Now);

        Assert.That(store.DeleteAllCalls, Is.EqualTo(1));
        Assert.That(store.State, Is.Null);
        Assert.That(response.State, Is.Null);
        Assert.That(response.Code, Is.EqualTo((int)CommandCode.Ok));
    }

    [Test]
    public async Task ResetPlayer_OtherSession_RefusedWithoutDeleting()
    {
        var store = StoreWithGem(5);

        var response = await ServerModule.ResetAsync(store, ServerProtocol.Version, "other", true, GameDataSource.Data, () => Now);

        Assert.That(response.Code, Is.EqualTo((int)CommandCode.OtherDevice));
        Assert.That(store.DeleteAllCalls, Is.EqualTo(0));
        Assert.That(store.State!.Gem, Is.EqualTo(5));
    }

    [Test]
    public async Task ResetPlayer_NoStateYet_StillDeletes()
    {
        var store = new FakeStore();

        await ServerModule.ResetAsync(store, ServerProtocol.Version, "sess", true, GameDataSource.Data, () => Now);

        Assert.That(store.DeleteAllCalls, Is.EqualTo(1));
        Assert.That(store.State!.ActiveSessionId, Is.EqualTo("sess"));
    }

    [Test]
    public async Task DevAddGem_OutsideDevelopment_DevOnly()
    {
        foreach (var env in new string?[] { "production", null, "Development" })
        {
            var store = StoreWithGem(100);
            var response = await ServerModule.ExecuteDev(store, env, Req("r1"), Spend10, () => Now);

            Assert.That(response.Code, Is.EqualTo((int)CommandCode.DevOnly), env);
            Assert.That(store.State!.Gem, Is.EqualTo(100), env);
        }

        var dev = StoreWithGem(100);
        var ok = await ServerModule.ExecuteDev(dev, "development", Req("r1"), Spend10, () => Now);
        Assert.That(ok.Code, Is.EqualTo((int)CommandCode.Ok));
        Assert.That(dev.State!.Gem, Is.EqualTo(90));
    }

    [Test]
    public async Task ResetKeys_ListsEveryKey()
    {
        // Kho giả trả tối đa một khoá mỗi lần liệt kê, như khi Cloud Save chia trang.
        var keys = new List<string> { "save", "x" };
        var deleted = new List<string>();

        await CloudSaveStateStore.DeleteAllKeysAsync(
            () => Task.FromResult<IReadOnlyList<string>>(keys.Take(1).ToList()),
            key => { keys.Remove(key); deleted.Add(key); return Task.CompletedTask; });

        Assert.That(deleted, Is.EqualTo(new[] { "save", "x" }));
        Assert.That(keys, Is.Empty);
    }

    [Test]
    public void ServerState_NewtonsoftShape_MatchesJsonUtility()
    {
        // Chuỗi JsonUtility ghi sẵn (cùng state mẫu với ServerStateTests.Json_Shape...): hai phía phải cho đúng chuỗi này.
        var state = new ServerState
        {
            Revision = 2,
            Gem = 9_000_000_000,
            OwnedCharacterIds = { "a" },
            Fragments = { new BunDauPeo.Core.FragmentCount { CharacterId = "a", Count = 3 } },
            StarUps = { new BunDauPeo.Core.StarUpEntry { CharacterId = "a", Count = 1 } },
            GachaPity = { new BunDauPeo.Core.PityEntry { BannerId = "b", Pulls = 4 } },
            GachaHistory = { new BunDauPeo.Core.GachaHistoryEntry { Utc = 5, BannerId = "b", Count = 1, CharacterIds = new[] { "a" } } },
            Stamina = new BunDauPeo.Core.StaminaState { Current = 40, AnchorUtcTicks = 638_700_000_000_000_123 },
            ClearedStages = { "s1" },
            PendingTicket = "t",
            PendingStageId = "s2",
            ActiveSessionId = "sess",
            LastRequestId = "r",
            LastResultJson = "{}",
        };

        Assert.That(JsonConvert.SerializeObject(state), Is.EqualTo(ServerStateJsonSample.Expected));
        Assert.That(JsonConvert.DeserializeObject<ServerState>(ServerStateJsonSample.Expected)!.Stamina.AnchorUtcTicks,
            Is.EqualTo(638_700_000_000_000_123));
    }
}

// Giá trị này cũng nằm trong Assets/_Project/Tests/ServerStateTests.cs (Json_Shape_MatchesNewtonsoftSample): đổi một nơi phải đổi cả hai.
public static class ServerStateJsonSample
{
    public const string Expected =
        "{\"Version\":1,\"Revision\":2,\"Gem\":9000000000,\"OwnedCharacterIds\":[\"a\"],"
        + "\"Fragments\":[{\"CharacterId\":\"a\",\"Count\":3}],\"StarUps\":[{\"CharacterId\":\"a\",\"Count\":1}],"
        + "\"GachaPity\":[{\"BannerId\":\"b\",\"Pulls\":4}],"
        + "\"GachaHistory\":[{\"Utc\":5,\"BannerId\":\"b\",\"Count\":1,\"CharacterIds\":[\"a\"]}],"
        + "\"Stamina\":{\"Current\":40,\"AnchorUtcTicks\":638700000000000123},\"ClearedStages\":[\"s1\"],"
        + "\"PendingTicket\":\"t\",\"PendingStageId\":\"s2\",\"ActiveSessionId\":\"sess\",\"LastRequestId\":\"r\",\"LastResultJson\":\"{}\"}";
}
