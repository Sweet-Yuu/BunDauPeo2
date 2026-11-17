using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BunDauPeo.Core;
using BunDauPeo.Rules;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class CommandGateTests
    {
        private readonly List<TaskCompletionSource<bool>> _timers = new();
        private CommandGate _gate;

        [SetUp]
        public void SetUp()
        {
            _timers.Clear();
            // Mỗi lượt gửi nhận một đồng hồ giả; test tự cho hết giờ bằng _timers[i].SetResult.
            _gate = new CommandGate(_ =>
            {
                var timer = new TaskCompletionSource<bool>();
                _timers.Add(timer);
                return timer.Task;
            });
        }

        private static OnlineResult<CommandResponse> Reply(CommandCode code) =>
            OnlineResult<CommandResponse>.Success(new CommandResponse { Code = (int)code });

        private static async Task Until(Func<bool> condition)
        {
            for (var i = 0; i < 2000 && !condition(); i++) await Task.Yield();
            Assert.IsTrue(condition(), "timed out waiting for the gate");
        }

        [Test]
        public async Task Gate_SecondRunWhileBusy_Rejected()
        {
            var hang = new TaskCompletionSource<OnlineResult<CommandResponse>>();
            var calls = 0;
            var first = _gate.RunAsync(_ =>
            {
                calls++;
                return hang.Task;
            });
            Assert.IsTrue(_gate.Busy);

            var second = await _gate.RunAsync(_ =>
            {
                calls++;
                return Task.FromResult(Reply(CommandCode.Ok));
            });

            Assert.IsFalse(second.Ok);
            Assert.AreEqual(OnlineStatus.ServerError, second.Status);
            Assert.AreEqual(1, calls, "the second button press sent nothing");

            hang.SetResult(Reply(CommandCode.Ok));
            Assert.IsTrue((await first).Ok);
            Assert.IsFalse(_gate.Busy);
        }

        [Test]
        public async Task Timeout_ShowsLost_RetrySameRequestId()
        {
            var late = new TaskCompletionSource<OnlineResult<CommandResponse>>();
            var ids = new List<string>();
            Action retry = null;
            _gate.Lost += r => retry = r;

            var run = _gate.RunAsync(id =>
            {
                ids.Add(id);
                return ids.Count == 1 ? late.Task : Task.FromResult(Reply(CommandCode.Ok));
            });
            _timers[0].SetResult(true);
            await Until(() => retry != null);
            Assert.IsFalse(run.IsCompleted, "RunAsync stays open while the box is up");

            retry();
            await Until(() => ids.Count == 2);
            Assert.AreEqual(ids[0], ids[1], "retry resends the same RequestId");

            // Câu trả lời muộn của lượt đầu không làm RunAsync xong lần nữa.
            late.SetResult(Reply(CommandCode.NotEnoughGem));
            var result = await run;
            Assert.AreEqual((int)CommandCode.Ok, result.Value.Code);
            Assert.AreEqual(2, ids.Count);
        }

        [Test]
        public async Task ServerError_ShowsLost()
        {
            var lost = 0;
            Action retry = null;
            _gate.Lost += r =>
            {
                lost++;
                retry = r;
            };
            var tries = 0;

            var run = _gate.RunAsync(_ =>
                Task.FromResult(++tries == 1 ? OnlineResult<CommandResponse>.Fail(OnlineStatus.ServerError) : Reply(CommandCode.Ok)));
            await Until(() => retry != null);
            Assert.AreEqual(1, lost);
            retry();
            retry();

            Assert.IsTrue((await run).Ok);
            Assert.AreEqual(2, tries, "pressing Retry twice sends once");
        }

        [Test]
        public async Task OtherDevice_NoState_TooOld_RaiseEvents_NoRetry()
        {
            var events = new List<string>();
            _gate.Lost += _ => events.Add("lost");
            _gate.OtherDevice += () => events.Add("other");
            _gate.NoState += () => events.Add("nostate");
            _gate.TooOld += () => events.Add("tooold");

            var other = await _gate.RunAsync(_ => Task.FromResult(Reply(CommandCode.OtherDevice)));
            var none = await _gate.RunAsync(_ => Task.FromResult(Reply(CommandCode.NoState)));
            var old = await _gate.RunAsync(_ => Task.FromResult(OnlineResult<CommandResponse>.Fail(OnlineStatus.ClientTooOld)));

            CollectionAssert.AreEqual(new[] { "other", "nostate", "tooold" }, events, "none of them offers Retry");
            Assert.AreEqual(OnlineStatus.OtherDevice, other.Status);
            Assert.IsFalse(none.Ok);
            Assert.AreEqual(OnlineStatus.ClientTooOld, old.Status);
            Assert.IsFalse(_gate.Busy);
        }

        [Test]
        public async Task Rejection_ReturnsSuccessWithCode()
        {
            var events = 0;
            _gate.Lost += _ => events++;
            _gate.OtherDevice += () => events++;

            var result = await _gate.RunAsync(_ => Task.FromResult(Reply(CommandCode.NotEnoughGem)));

            Assert.IsTrue(result.Ok, "the service turns the code into its own status");
            Assert.AreEqual((int)CommandCode.NotEnoughGem, result.Value.Code);
            Assert.AreEqual(0, events);
        }

        [Test]
        public async Task WaitingChanged_Sequence()
        {
            var seen = new List<bool>();
            _gate.WaitingChanged += seen.Add;
            Action retry = null;
            _gate.Lost += r => retry = r;
            var tries = 0;

            var run = _gate.RunAsync(_ =>
                Task.FromResult(++tries == 1 ? OnlineResult<CommandResponse>.Fail(OnlineStatus.NetworkError) : Reply(CommandCode.Ok)));
            await Until(() => retry != null);
            CollectionAssert.AreEqual(new[] { true, false }, seen, "on while sending, off while the box is up");

            retry();
            await run;

            CollectionAssert.AreEqual(new[] { true, false, true, false }, seen);
        }
    }
}
