using System;
using System.Net.Http;
using System.Reflection;
using BunDauPeo.Core;
using BunDauPeo.Online;
using NUnit.Framework;
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
    }
}
