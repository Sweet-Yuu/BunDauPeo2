using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BunDauPeo.Rules;
using Newtonsoft.Json;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Apis.Extensions;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudCode.Shared;
using Unity.Services.CloudSave.Model;

namespace BunDauPeoServer;

// Ghi bằng write lock cũ trong khi người khác đã ghi: đọc lại rồi chạy lại (CommandRunner nhận ra RequestId trùng).
public class LockConflictException : Exception
{
}

public interface IStateStore
{
    // state = null khi chưa có khoá "state"; writeLock = null khi chưa có.
    Task<(ServerState? state, string? writeLock)> LoadAsync();

    // Trả write lock mới. Ném LockConflictException khi writeLock lệch.
    Task<string?> SaveAsync(ServerState state, string? writeLock);

    // Xoá mọi khoá Default và Protected của người chơi (Reset / Xoá tài khoản).
    Task DeleteAllAsync();
}

public static class StateJson
{
    // Newtonsoft ghi field công khai và enum số, cùng hình với JsonUtility (ServerState_NewtonsoftShape_MatchesJsonUtility).
    public static string To(object value) => JsonConvert.SerializeObject(value);

    public static ServerState? ToState(string json) => JsonConvert.DeserializeObject<ServerState>(json);
}

// Khoá "state" lớp Protected: người chơi không đọc/ghi được, chỉ module (service token).
public class CloudSaveStateStore : IStateStore
{
    // Chặn vòng lặp vô tận nếu Cloud Save cứ trả lại khoá đã xoá.
    const int MaxDeleteRounds = 50;

    readonly IExecutionContext _ctx;
    readonly IGameApiClient _api;
    readonly string _playerId;

    public CloudSaveStateStore(IExecutionContext ctx, IGameApiClient api)
    {
        _ctx = ctx;
        _api = api;
        _playerId = ctx.PlayerId ?? throw new InvalidOperationException("Command needs a signed-in player");
    }

    public async Task<(ServerState? state, string? writeLock)> LoadAsync()
    {
        var response = await _api.CloudSaveData.GetProtectedItemsAsync(
            _ctx, _ctx.ServiceToken, _ctx.ProjectId, _playerId, new List<string> { ServerProtocol.StateKey });
        var item = response.Data.Results.FirstOrDefault(i => i.Key == ServerProtocol.StateKey);
        if (item == null) return (null, null);
        var json = item.Value as string ?? item.Value.ToString()!;
        return (StateJson.ToState(json), item.WriteLock);
    }

    public async Task<string?> SaveAsync(ServerState state, string? writeLock)
    {
        try
        {
            var response = await _api.CloudSaveData.SetProtectedItemAsync(
                _ctx, _ctx.ServiceToken, _ctx.ProjectId, _playerId,
                new SetItemBody(ServerProtocol.StateKey, StateJson.To(state), writeLock));
            return response.Data.WriteLock;
        }
        catch (ApiException e) when (e.Response?.StatusCode == HttpStatusCode.Conflict)
        {
            throw new LockConflictException();
        }
    }

    public async Task DeleteAllAsync()
    {
        await DeleteAllKeysAsync(
            async () => (await _api.CloudSaveData.GetKeysAsync(_ctx, _ctx.AccessToken, _ctx.ProjectId, _playerId))
                .Data.Results.Select(k => k.Key).ToList(),
            key => _api.CloudSaveData.DeleteItemAsync(_ctx, _ctx.AccessToken, _ctx.ProjectId, _playerId, key));
        await DeleteAllKeysAsync(
            async () => (await _api.CloudSaveData.GetProtectedKeysAsync(_ctx, _ctx.ServiceToken, _ctx.ProjectId, _playerId))
                .Data.Results.Select(k => k.Key).ToList(),
            key => _api.CloudSaveData.DeleteProtectedItemAsync(_ctx, _ctx.ServiceToken, _ctx.ProjectId, _playerId, key, null));
    }

    // Liệt kê lại sau mỗi lượt xoá thay vì đọc link trang sau: kho chia trang vẫn xoá hết.
    public static async Task DeleteAllKeysAsync(Func<Task<IReadOnlyList<string>>> listKeys, Func<string, Task> delete)
    {
        for (var round = 0; round < MaxDeleteRounds; round++)
        {
            var keys = await listKeys();
            if (keys.Count == 0) return;
            foreach (var key in keys) await delete(key);
        }
        throw new InvalidOperationException($"ResetPlayer: keys still present after {MaxDeleteRounds} rounds");
    }
}
