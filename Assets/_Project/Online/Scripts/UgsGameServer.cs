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

namespace BunDauPeo.Online
{
    // Server thật: Authentication ẩn danh, Cloud Save (khoá save, lớp Default), module Cloud Code BunDauPeoServer.
    // Không ném ra ngoài: mọi ngoại lệ đổi thành OnlineStatus qua UgsErrors.
    public sealed class UgsGameServer : IGameServer
    {
        private const string Log = "[Online]";

        // Write lock của khoá save lần đọc/ghi gần nhất; null = chưa có khoá trên server.
        private string _writeLock;

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
                ServerProtocol.Module, ServerProtocol.Bootstrap, new Dictionary<string, object>());
            if (response == null) throw new InvalidOperationException("Bootstrap returned no body");
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

        public Task<OnlineStatus> ResetPlayerAsync() => Run("reset player", async () =>
        {
            await CallResetAsync();
            Debug.Log($"{Log} reset player ok");
        });

        public Task<OnlineStatus> DeleteAccountAsync() => Run("delete account", async () =>
        {
            await CallResetAsync();
            await AuthenticationService.Instance.DeleteAccountAsync();
            // Xoá token phiên để lần mở sau là người chơi mới (spec 5.4).
            AuthenticationService.Instance.SignOut(true);
            Debug.Log($"{Log} delete account ok");
        });

        private async Task CallResetAsync()
        {
            await CloudCodeService.Instance.CallModuleEndpointAsync(ServerProtocol.Module, ServerProtocol.ResetPlayer,
                new Dictionary<string, object>());
            _writeLock = null;
        }

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
            var status = UgsErrors.Map(e);
            Debug.LogWarning($"{Log} {step} failed ({status}): {e.GetType().Name} {e.Message}");
            return status;
        }
    }
}
