using BunDauPeo.Rules;
using BunDauPeoServer;
using Newtonsoft.Json;

namespace BunDauPeoServer.Tests;

public class ServerModuleTests
{
    [Test]
    public void Bootstrap_ReturnsGivenUtc()
    {
        var utc = new DateTime(2026, 11, 13, 10, 5, 7, DateTimeKind.Utc).AddTicks(123);

        Assert.That(ServerModule.BootstrapAt(utc).ServerUtcTicks, Is.EqualTo(utc.Ticks));
    }

    [Test]
    public async Task ResetKeys_ListsEveryDefaultKey()
    {
        // Kho giả trả tối đa một khoá mỗi lần liệt kê, như khi Cloud Save chia trang.
        var store = new List<string> { "save", "x" };
        var deleted = new List<string>();

        await ServerModule.DeleteAllKeysAsync(
            () => Task.FromResult<IReadOnlyList<string>>(store.Take(1).ToList()),
            key => { store.Remove(key); deleted.Add(key); return Task.CompletedTask; });

        Assert.That(deleted, Is.EqualTo(new[] { "save", "x" }));
        Assert.That(store, Is.Empty);
    }

    [Test]
    public void BootstrapResponse_NewtonsoftJson_MatchesJsonUtilityShape()
    {
        // Cloud Code trả kết quả qua Newtonsoft.Json; chuỗi phải giống JsonUtility ở ServerProtocolTests.
        var json = JsonConvert.SerializeObject(new BootstrapResponse { ServerUtcTicks = 638_700_000_000_000_123 });

        Assert.That(json, Is.EqualTo("{\"ServerUtcTicks\":638700000000000123}"));
        Assert.That(JsonConvert.DeserializeObject<BootstrapResponse>(json)!.ServerUtcTicks, Is.EqualTo(638_700_000_000_000_123));
    }
}
