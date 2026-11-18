using System;
using System.Threading.Tasks;
using BunDauPeo.Characters;
using BunDauPeo.Core;
using BunDauPeo.Rules;
using NUnit.Framework;
using UnityEngine;

namespace BunDauPeo.Tests
{
    // Server giả trong bộ nhớ đã đăng nhập và Bootstrap, cùng cổng lệnh không bao giờ hết giờ: test service chạy lệnh thật
    // qua CommandGate → LocalGameServer → CommandRunner, không chạm mạng hay save thật.
    internal sealed class ServerRig
    {
        private ServerRig(LocalGameServer server)
        {
            Server = server;
            // Đồng hồ hẹn không bao giờ chạy xong: không test nào đợi 10 giây thật.
            Gate = new CommandGate(_ => new TaskCompletionSource<bool>().Task);
        }

        public LocalGameServer Server { get; }
        public CommandGate Gate { get; }
        public ServerState Booted { get; private set; }

        // seed dựng phần server giữ (Gem, nhân vật, mảnh, sao...) của người chơi trước khi Bootstrap.
        public static async Task<ServerRig> CreateAsync(GameData data, Action<PlayerSave> seed = null)
        {
            var rig = new ServerRig(new LocalGameServer(data));
            await rig.Server.SignInAsync();
            var save = new PlayerSave();
            seed?.Invoke(save);
            rig.Server.Seed(JsonUtility.ToJson(save));
            var boot = await rig.Server.BootstrapAsync();
            rig.Booted = boot.Value.State;
            return rig;
        }

        // Cổng không bao giờ hết giờ và server trống: service chỉ để ĐỌC (ca không gửi lệnh); gửi lệnh thì dùng CreateAsync.
        public static StarService IdleStars(PlayerSave save, Roster roster, CharacterCatalog catalog, StarConfig config) =>
            new StarService(save, roster, catalog, config, new LocalGameServer(),
                new CommandGate(_ => new TaskCompletionSource<bool>().Task), _ => true);

        // Máy khớp server lúc mở app: cùng việc OnlineStartup làm sau Bootstrap.
        public void ApplyTo(PlayerSave save) => ServerStateApplier.Apply(save, Booted);
    }
}

namespace BunDauPeo.Tests
{
    internal static class TaskTestExtensions
    {
        // LocalGameServer trả Task đã xong (không đợi gì), nên cả chuỗi service → cổng → server chạy đồng bộ: ca test không
        // cần async. Chỗ nào chờ thật (DelayNext) thì dùng await; Sync() báo ngay nếu Task chưa xong thay vì treo.
        public static T Sync<T>(this Task<T> task)
        {
            Assert.IsTrue(task.IsCompleted, "the command did not finish synchronously; await it instead");
            return task.GetAwaiter().GetResult();
        }

        public static void Sync(this Task task)
        {
            Assert.IsTrue(task.IsCompleted, "the command did not finish synchronously; await it instead");
            task.GetAwaiter().GetResult();
        }
    }
}
