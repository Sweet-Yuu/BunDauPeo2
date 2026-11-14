using System.Threading.Tasks;
using BunDauPeo.Core;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class LocalGameServerTests
    {
        [Test]
        public async Task SignIn_StableId_UntilDeleteAccount()
        {
            var server = new LocalGameServer();
            Assert.AreEqual("", server.PlayerId);

            Assert.AreEqual(OnlineStatus.Ok, await server.SignInAsync());
            var first = server.PlayerId;
            Assert.AreEqual(OnlineStatus.Ok, await server.SignInAsync());
            Assert.AreNotEqual("", first);
            Assert.AreEqual(first, server.PlayerId, "signing in again must not create a new player");

            Assert.AreEqual(OnlineStatus.Ok, await server.DeleteAccountAsync());
            Assert.AreEqual("", server.PlayerId);
            await server.SignInAsync();
            Assert.AreNotEqual("", server.PlayerId);
            Assert.AreNotEqual(first, server.PlayerId);
        }

        [Test]
        public async Task LoadUpload_RoundTrip_AndFailNextChangesNothing()
        {
            var server = new LocalGameServer();
            await server.SignInAsync();

            var empty = await server.LoadSaveAsync();
            Assert.IsTrue(empty.Ok);
            Assert.IsNull(empty.Value);

            Assert.AreEqual(OnlineStatus.Ok, await server.UploadSaveAsync("a"));
            Assert.AreEqual("a", (await server.LoadSaveAsync()).Value);

            server.FailNext(OnlineStatus.NetworkError);
            Assert.AreEqual(OnlineStatus.NetworkError, await server.UploadSaveAsync("b"));
            Assert.AreEqual("a", (await server.LoadSaveAsync()).Value, "failed upload must not change data");

            // Lỗi chỉ cho đúng số lệnh đã hẹn; lệnh sau chạy bình thường.
            server.FailNext(OnlineStatus.ServerError, 2);
            Assert.AreEqual(OnlineStatus.ServerError, (await server.LoadSaveAsync()).Status);
            Assert.AreEqual(OnlineStatus.ServerError, (await server.BootstrapAsync()).Status);
            Assert.IsTrue((await server.LoadSaveAsync()).Ok);

            server.SimulateOtherDevice();
            Assert.AreEqual(OnlineStatus.OtherDevice, await server.UploadSaveAsync("c"));
            Assert.AreEqual("a", server.StoredSave);
        }

        [Test]
        public async Task ResetPlayer_ClearsSave_KeepsId()
        {
            var server = new LocalGameServer();
            await server.SignInAsync();
            var id = server.PlayerId;
            await server.UploadSaveAsync("{\"Version\":1}");

            Assert.AreEqual(OnlineStatus.Ok, await server.ResetPlayerAsync());

            Assert.IsNull((await server.LoadSaveAsync()).Value);
            Assert.AreEqual(id, server.PlayerId);
        }
    }
}
