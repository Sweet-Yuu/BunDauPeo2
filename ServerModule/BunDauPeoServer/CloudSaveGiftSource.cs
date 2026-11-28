using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BunDauPeo.Rules;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Apis.Extensions;
using Unity.Services.CloudCode.Core;

namespace BunDauPeoServer;

// Custom data `gifts`, khoá `list` (spec đợt 4 mục 4.1): JSON mảng quà do user sửa trên Dashboard, không deploy.
public static class GiftJsonParser
{
    // JSON hỏng hoặc không phải mảng thì ném lỗi (không coi là rỗng). Một quà sai kiểu thì thành null để Validate bỏ riêng quà đó.
    public static List<GiftData> Parse(string json)
    {
        var array = JToken.Parse(json) as JArray ?? throw new JsonException("gifts list must be a JSON array");
        var gifts = new List<GiftData>(array.Count);
        foreach (var item in array)
        {
            try { gifts.Add(item.ToObject<GiftJson>()!.ToGift()); }
            catch (Exception e) when (e is JsonException or InvalidCastException or NullReferenceException) { gifts.Add(null!); }
        }
        return gifts;
    }
}

public class CloudSaveGiftSource : IGiftSource
{
    public const string CustomDataId = "gifts";
    public const string Key = "list";

    readonly IExecutionContext _ctx;
    readonly IGameApiClient _api;

    public CloudSaveGiftSource(IExecutionContext ctx, IGameApiClient api)
    {
        _ctx = ctx;
        _api = api;
    }

    // Đọc lỗi thì ném lỗi (máy thấy ServerError và giữ thư cũ). Chưa có khoá `list` = chưa có quà = rỗng.
    public async Task<IReadOnlyList<GiftData>> LoadAsync()
    {
        var response = await _api.CloudSaveData.GetCustomItemsAsync(
            _ctx, _ctx.ServiceToken, _ctx.ProjectId, CustomDataId, new List<string> { Key });
        var item = response.Data.Results.FirstOrDefault(i => i.Key == Key);
        if (item == null) return new List<GiftData>();
        var json = item.Value as string ?? JsonConvert.SerializeObject(item.Value);
        return GiftJsonParser.Parse(json);
    }
}
