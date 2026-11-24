using BunDauPeo.Rules;
using NUnit.Framework;
using UnityEngine;

namespace BunDauPeo.Tests
{
    public class ServerProtocolTests
    {
        // Số 18 chữ số: double làm tròn mất hai chữ số cuối, nên test bắt được nếu JSON đi qua double.
        const long Ticks = 638_700_000_000_000_123;

        // Online O3 đặt Version 3; Cấp nhân vật không nâng thêm (server cũ thiếu ExpPotion vẫn đọc được).
        [Test]
        public void Version_IsNotRaisedByThisFeature_MinProtocolStays2()
        {
            Assert.AreEqual(3, ServerProtocol.Version);
            Assert.AreEqual(2, ServerProtocol.MinProtocolVersion);
        }

        [Test]
        public void BootstrapResponse_JsonUtilityRoundTrip()
        {
            var json = JsonUtility.ToJson(new BootstrapResponse { ServerUtcTicks = Ticks });

            StringAssert.Contains("\"ServerUtcTicks\":638700000000000123", json);
            Assert.AreEqual(Ticks, JsonUtility.FromJson<BootstrapResponse>(json).ServerUtcTicks);
        }
    }
}
