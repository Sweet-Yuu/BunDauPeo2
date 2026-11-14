using BunDauPeo.Rules;
using NUnit.Framework;
using UnityEngine;

namespace BunDauPeo.Tests
{
    public class ServerProtocolTests
    {
        // Số 18 chữ số: double làm tròn mất hai chữ số cuối, nên test bắt được nếu JSON đi qua double.
        const long Ticks = 638_700_000_000_000_123;

        [Test]
        public void BootstrapResponse_JsonUtilityRoundTrip()
        {
            var json = JsonUtility.ToJson(new BootstrapResponse { ServerUtcTicks = Ticks });

            StringAssert.Contains("\"ServerUtcTicks\":638700000000000123", json);
            Assert.AreEqual(Ticks, JsonUtility.FromJson<BootstrapResponse>(json).ServerUtcTicks);
        }
    }
}
