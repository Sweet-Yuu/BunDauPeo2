using BunDauPeo.UI;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class WalkPoseTests
    {
        [Test]
        public void Zero_IsIdle_AndLiftInRange()
        {
            Assert.AreEqual(WalkPose.Idle, WalkPose.At(0f));

            for (var i = 0; i <= 200; i++)
            {
                var s = i * 0.01f;
                var pose = WalkPose.At(s);
                Assert.GreaterOrEqual(pose.Lift, 0f, $"lift at {s}");
                Assert.LessOrEqual(pose.Lift, WalkPose.LiftPx + 0.0001f, $"lift at {s}");
                Assert.LessOrEqual(System.Math.Abs(pose.Angle), WalkPose.SwayDegrees + 0.0001f, $"angle at {s}");
            }
        }

        [Test]
        public void ConsecutiveSteps_SwayOppositeSides()
        {
            var first = WalkPose.At(0.25f);
            var second = WalkPose.At(0.75f);

            Assert.Greater(first.Angle, 0f, "mid of step 1");
            Assert.Less(second.Angle, 0f, "mid of step 2");
            Assert.AreEqual(WalkPose.LiftPx, first.Lift, 0.01f);
            Assert.AreEqual(WalkPose.LiftPx, second.Lift, 0.01f);
        }
    }
}
