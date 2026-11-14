using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BunDauPeo.Rules;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Apis.Extensions;
using Unity.Services.CloudCode.Core;

namespace BunDauPeoServer;

public class ServerModule
{
    // Chặn vòng lặp vô tận nếu Cloud Save cứ trả lại khoá đã xoá.
    const int MaxDeleteRounds = 50;

    [CloudCodeFunction(ServerProtocol.Bootstrap)]
    public Task<BootstrapResponse> Bootstrap(IExecutionContext ctx) => Task.FromResult(BootstrapAt(DateTime.UtcNow));

    // Xoá mọi khoá Default của người chơi; đợt 2 thêm khoá Protected (spec 5.8).
    [CloudCodeFunction(ServerProtocol.ResetPlayer)]
    public Task ResetPlayer(IExecutionContext ctx, IGameApiClient api)
    {
        var playerId = ctx.PlayerId ?? throw new InvalidOperationException("ResetPlayer needs a signed-in player");
        return DeleteAllKeysAsync(
            async () => (await api.CloudSaveData.GetKeysAsync(ctx, ctx.AccessToken, ctx.ProjectId, playerId))
                .Data.Results.Select(k => k.Key).ToList(),
            key => api.CloudSaveData.DeleteItemAsync(ctx, ctx.AccessToken, ctx.ProjectId, playerId, key));
    }

    public static BootstrapResponse BootstrapAt(DateTime utcNow) => new() { ServerUtcTicks = utcNow.Ticks };

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

public class ModuleSetup : ICloudCodeSetup
{
    public void Setup(ICloudCodeConfig config) => config.AddGameApiClient();
}
