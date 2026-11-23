using System;
using System.Net.Http;
using System.Reflection;
using BunDauPeo.Core;
using BunDauPeo.Online;
using NUnit.Framework;
using Unity.Services.Authentication;
using Unity.Services.CloudCode;
using Unity.Services.CloudSave;
using Unity.Services.Core;

namespace BunDauPeo.Tests
{
    public class UgsErrorsTests
    {
        // Constructor của CloudSaveException là internal (bản 3.4.2): dựng bằng reflection để thử đúng kiểu SDK ném ra.
        private static CloudSaveException CloudSave(CloudSaveExceptionReason reason, int code) =>
            (CloudSaveException)typeof(CloudSaveException).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null,
                    new[] { typeof(CloudSaveExceptionReason), typeof(int), typeof(string), typeof(Exception) }, null)
                .Invoke(new object[] { reason, code, "test", null });

        [Test]
        public void Map_NetworkConflictOther()
        {
            Assert.AreEqual(OnlineStatus.NetworkError, UgsErrors.Map(new RequestFailedException(CommonErrorCodes.TransportError, "x")));
            Assert.AreEqual(OnlineStatus.NetworkError, UgsErrors.Map(new RequestFailedException(CommonErrorCodes.Timeout, "x")));
            Assert.AreEqual(OnlineStatus.NetworkError, UgsErrors.Map(new TimeoutException()));
            Assert.AreEqual(OnlineStatus.NetworkError, UgsErrors.Map(new HttpRequestException()));
            Assert.AreEqual(OnlineStatus.NetworkError,
                UgsErrors.Map(CloudSave(CloudSaveExceptionReason.NoInternetConnection, CommonErrorCodes.TransportError)));

            Assert.AreEqual(OnlineStatus.OtherDevice, UgsErrors.Map(CloudSave(CloudSaveExceptionReason.Conflict, 7_000)));

            Assert.AreEqual(OnlineStatus.ServerError, UgsErrors.Map(new InvalidOperationException()));
            Assert.AreEqual(OnlineStatus.ServerError, UgsErrors.Map(new RequestFailedException(10_000, "unauthorized")));
            Assert.AreEqual(OnlineStatus.ServerError, UgsErrors.Map(CloudSave(CloudSaveExceptionReason.InvalidArgument, 7_000)));
        }

        [Test]
        public void Map_WrappedClientTooOld_IsClientTooOld()
        {
            const string tooOld = "ScriptError (422): ... ClientTooOld: client 0 < 2 ...";
            Assert.AreEqual(OnlineStatus.ClientTooOld, UgsErrors.Map(CloudCode(tooOld)));
            Assert.AreEqual(OnlineStatus.ServerError, UgsErrors.Map(CloudCode("ScriptError (422): something else")));
            Assert.AreEqual(OnlineStatus.ServerError, UgsErrors.Map(new InvalidOperationException(tooOld)),
                "only a Cloud Code error carries the tag");
        }

        private static CloudCodeException CloudCode(string message) =>
            (CloudCodeException)typeof(CloudCodeException).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null,
                    new[] { typeof(CloudCodeExceptionReason), typeof(int), typeof(string), typeof(Exception) }, null)
                .Invoke(new object[] { CloudCodeExceptionReason.Unknown, 9_000, message, null });

        [Test]
        public void MapAccount_ConflictCancelledOffline()
        {
            Assert.AreEqual(AccountStatus.Conflict, UgsErrors.MapAccount(new RequestFailedException(AuthenticationErrorCodes.AccountAlreadyLinked, "x"), AccountMethod.Google, false));
            Assert.AreEqual(AccountStatus.UsernameTaken, UgsErrors.MapAccount(new RequestFailedException(AuthenticationErrorCodes.AccountAlreadyLinked, "x"), AccountMethod.UserPass, false));
            Assert.AreEqual(AccountStatus.Cancelled, UgsErrors.MapAccount(new OperationCanceledException(), AccountMethod.Google, false));
            Assert.AreEqual(AccountStatus.Offline, UgsErrors.MapAccount(new RequestFailedException(CommonErrorCodes.TransportError, "x"), AccountMethod.Google, true));
            Assert.AreEqual(AccountStatus.WrongCredentials, UgsErrors.MapAccount(new RequestFailedException(AuthenticationErrorCodes.InvalidParameters, "x"), AccountMethod.UserPass, true));
            Assert.AreEqual(AccountStatus.WeakPassword, UgsErrors.MapAccount(new RequestFailedException(AuthenticationErrorCodes.InvalidParameters, "x"), AccountMethod.UserPass, false));
            Assert.AreEqual(AccountStatus.WrongCredentials, UgsErrors.MapAccount(new RequestFailedException(CommonErrorCodes.Unknown, "Invalid username or password"), AccountMethod.UserPass, true));
            const string off = "usernamepassword external id provider is not available: PERMISSION_DENIED";
            Assert.AreEqual(AccountStatus.Unavailable, UgsErrors.MapAccount(new RequestFailedException(AuthenticationErrorCodes.InvalidParameters, off), AccountMethod.UserPass, false));
            Assert.AreEqual(AccountStatus.Unavailable, UgsErrors.MapAccount(new RequestFailedException(AuthenticationErrorCodes.InvalidParameters, off), AccountMethod.UserPass, true));
        }

        private sealed class FakeGoogle : IGoogleTokenProvider
        {
            public System.Threading.Tasks.Task<string> GetIdTokenAsync() => System.Threading.Tasks.Task.FromResult("token");
        }

        [Test]
        public void Available_ExcludesGoogle_WithoutProvider_IncludesWithOne()
        {
            CollectionAssert.AreEqual(new[] { AccountMethod.UserPass }, new BunDauPeo.Online.UgsAccountService("development").Available);
            CollectionAssert.Contains(new BunDauPeo.Online.UgsAccountService("development", new FakeGoogle()).Available, AccountMethod.Google);
            Assert.AreEqual(AccountStatus.Unavailable,
                new BunDauPeo.Online.UgsAccountService("development").LinkGoogleAsync().GetAwaiter().GetResult().Status);
        }
    }
}
