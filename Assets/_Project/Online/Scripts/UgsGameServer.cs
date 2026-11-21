using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BunDauPeo.Core;
using BunDauPeo.Rules;
using Unity.Services.Authentication;
using Unity.Services.CloudCode;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;
using PlayerLoadOptions = Unity.Services.CloudSave.Models.Data.Player.LoadOptions;
using PlayerSaveOptions = Unity.Services.CloudSave.Models.Data.Player.SaveOptions;
using ProtectedRead = Unity.Services.CloudSave.Models.Data.Player.ProtectedReadAccessClassOptions;

namespace BunDauPeo.Online
{
    // Server thật: Authentication ẩn danh, Cloud Save (khoá save, lớp Default), module Cloud Code BunDauPeoServer.
    // Không ném ra ngoài: mọi ngoại lệ đổi thành OnlineStatus qua UgsErrors.
    public sealed class UgsGameServer : IGameServer
    {
        private const string Log = "[Online]";

        // Write lock của khoá save lần đọc/ghi gần nhất; null = chưa có khoá trên server.
        private string _writeLock;

        // Mã phiên Bootstrap cấp; mọi lệnh gửi kèm để server biết máy nào đang giữ phiên.
        private string _sessionId = "";

        public UgsGameServer(string environment) =>
            Environment = string.IsNullOrEmpty(environment) ? throw new ArgumentException("environment is required", nameof(environment)) : environment;

        public string Environment { get; }

        public string PlayerId =>
            UnityServices.State == ServicesInitializationState.Initialized && AuthenticationService.Instance.IsSignedIn
                ? AuthenticationService.Instance.PlayerId ?? ""
                : "";

        public Task<OnlineStatus> SignInAsync() => Run("sign in", async () =>
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync(new InitializationOptions().SetEnvironmentName(Environment));
            // SDK tự dùng token phiên đã cất trong PlayerPrefs (spec 5.4), nên cùng máy là cùng người chơi.
            if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log($"{Log} signed in as {PlayerId} ({Environment})");
        });

        public Task<OnlineResult<BootstrapResponse>> BootstrapAsync() => Run("bootstrap", async () =>
        {
            var response = await CloudCodeService.Instance.CallModuleEndpointAsync<BootstrapResponse>(
                ServerProtocol.Module, ServerProtocol.Bootstrap, new Dictionary<string, object> { { "protocolVersion", ServerProtocol.Version } });
            if (response == null) throw new InvalidOperationException("Bootstrap returned no body");
            _sessionId = response.SessionId;
            Debug.Log($"{Log} bootstrap ok, server UTC {new DateTime(response.ServerUtcTicks, DateTimeKind.Utc):O}");
            return response;
        });

        public Task<OnlineResult<string>> LoadSaveAsync() => Run("load save", async () =>
        {
            var items = await CloudSaveService.Instance.Data.Player.LoadAsync(
                new HashSet<string> { ServerProtocol.SaveKey }, new PlayerLoadOptions());
            if (!items.TryGetValue(ServerProtocol.SaveKey, out var item))
            {
                _writeLock = null;
                Debug.Log($"{Log} load save: no save on server");
                return null;
            }

            _writeLock = item.WriteLock;
            Debug.Log($"{Log} load save ok");
            return item.Value.GetAsString();
        });

        public Task<OnlineStatus> UploadSaveAsync(string json) => Run("upload save", async () =>
        {
            var locks = await CloudSaveService.Instance.Data.Player.SaveAsync(
                new Dictionary<string, SaveItem> { { ServerProtocol.SaveKey, new SaveItem(json, _writeLock) } },
                new PlayerSaveOptions());
            _writeLock = locks[ServerProtocol.SaveKey];
            Debug.Log($"{Log} upload save ok");
        });

        // Khoá state lớp Protected: người chơi đọc được, chỉ module ghi được.
        public Task<OnlineResult<string>> LoadActiveSessionIdAsync() => Run("load session id", async () =>
        {
            var items = await CloudSaveService.Instance.Data.Player.LoadAsync(
                new HashSet<string> { ServerProtocol.StateKey }, new PlayerLoadOptions(new ProtectedRead()));
            if (!items.TryGetValue(ServerProtocol.StateKey, out var item)) return null;
            return JsonUtility.FromJson<ServerState>(item.Value.GetAsString()).ActiveSessionId;
        });

        public async Task<OnlineResult<ServerState>> ResetPlayerAsync()
        {
            var result = await Run("reset player", () => CallResetAsync(true));
            if (!result.Ok) return OnlineResult<ServerState>.Fail(result.Status);
            return OnlineResult<ServerState>.Success(result.Value.State);
        }

        public Task<OnlineStatus> DeleteAccountAsync() => Run("delete account", async () =>
        {
            await CallResetAsync(false);
            await AuthenticationService.Instance.DeleteAccountAsync();
            // Xoá token phiên để lần mở sau là người chơi mới (spec 5.4).
            AuthenticationService.Instance.SignOut(true);
            Debug.Log($"{Log} delete account ok");
        });

        // Server từ chối (OtherDevice…) thì ném để Run đổi thành OnlineStatus; không xoá tài khoản khi chưa xoá được dữ liệu.
        private async Task<CommandResponse> CallResetAsync(bool recreate)
        {
            var response = await CloudCodeService.Instance.CallModuleEndpointAsync<CommandResponse>(
                ServerProtocol.Module, ServerProtocol.ResetPlayer, Args(null, ("recreate", recreate)));
            if (response == null) throw new InvalidOperationException("ResetPlayer returned no body");
            if (response.Code == (int)CommandCode.OtherDevice) throw new OtherDeviceException();
            _writeLock = null;
            Debug.Log($"{Log} reset player ok");
            return response;
        }

        public Task<OnlineResult<CommandResponse>> GachaPullAsync(string requestId, string bannerId, int count) =>
            Command(ServerProtocol.GachaPull, requestId, ("bannerId", bannerId), ("count", count));

        public Task<OnlineResult<CommandResponse>> EnterStageAsync(string requestId, string stageId, IReadOnlyList<string> team) =>
            Command(ServerProtocol.EnterStage, requestId, ("stageId", stageId), ("team", new List<string>(team ?? new List<string>())));

        public Task<OnlineResult<CommandResponse>> CompleteStageAsync(string requestId, string ticket, bool won) =>
            Command(ServerProtocol.CompleteStage, requestId, ("ticket", ticket), ("won", won));

        public Task<OnlineResult<CommandResponse>> CompleteStoryAsync(string requestId, string stageId) =>
            Command(ServerProtocol.CompleteStory, requestId, ("stageId", stageId));

        public Task<OnlineResult<CommandResponse>> StarUpAsync(string requestId, string characterId) =>
            Command(ServerProtocol.StarUp, requestId, ("characterId", characterId));

        public Task<OnlineResult<CommandResponse>> DevAddGemAsync(string requestId, long amount) =>
            Command(ServerProtocol.DevAddGem, requestId, ("amount", amount));

        public Task<OnlineResult<CommandResponse>> DevRefillStaminaAsync(string requestId) =>
            Command(ServerProtocol.DevRefillStamina, requestId);

        private Task<OnlineResult<CommandResponse>> Command(string name, string requestId, params (string key, object value)[] args) =>
            Run(name, async () =>
            {
                var response = await CloudCodeService.Instance.CallModuleEndpointAsync<CommandResponse>(
                    ServerProtocol.Module, name, Args(requestId, args));
                return response ?? throw new InvalidOperationException($"{name} returned no body");
            });

        private Dictionary<string, object> Args(string requestId, params (string key, object value)[] extra)
        {
            var args = new Dictionary<string, object>
            {
                { "protocolVersion", ServerProtocol.Version },
                { "sessionId", _sessionId },
            };
            if (requestId != null) args["requestId"] = requestId;
            foreach (var (key, value) in extra) args[key] = value;
            return args;
        }

        private sealed class OtherDeviceException : Exception { }

        private static async Task<OnlineStatus> Run(string step, Func<Task> action)
        {
            try
            {
                await action();
                return OnlineStatus.Ok;
            }
            catch (Exception e)
            {
                return Fail(step, e);
            }
        }

        private static async Task<OnlineResult<T>> Run<T>(string step, Func<Task<T>> action)
        {
            try
            {
                return OnlineResult<T>.Success(await action());
            }
            catch (Exception e)
            {
                return OnlineResult<T>.Fail(Fail(step, e));
            }
        }

        private static OnlineStatus Fail(string step, Exception e)
        {
            var status = e is OtherDeviceException ? OnlineStatus.OtherDevice : UgsErrors.Map(e);
            Debug.LogWarning($"{Log} {step} failed ({status}): {e.GetType().Name} {e.Message}");
            return status;
        }
    }
}
