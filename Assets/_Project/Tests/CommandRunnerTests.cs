using System;
using BunDauPeo.Rules;
using NUnit.Framework;
using UnityEngine;

namespace BunDauPeo.Tests
{
    public class CommandRunnerTests
    {
        static ServerState Stored() => new ServerState { ActiveSessionId = "sess", Gem = 100, Revision = 3 };

        static ServerRequest Req(string id, int version = ServerProtocol.Version, string session = "sess") =>
            new ServerRequest { ProtocolVersion = version, SessionId = session, RequestId = id };

        static string Json(object o) => JsonUtility.ToJson(o);

        [Serializable]
        class Payload { public int N = 5; }

        [Test]
        public void TooOld_Throws_ForZeroAndOne()
        {
            foreach (var v in new[] { 0, 1 })
            {
                var calls = 0;
                var ex = Assert.Throws<ClientTooOldException>(() =>
                    CommandRunner.Run(Stored(), Req("r", v), s => { calls++; return HandlerOutcome.Ok(s); }, Json));
                StringAssert.Contains("ClientTooOld", ex.Message);
                Assert.AreEqual(0, calls, "v=" + v);
            }
        }

        [Test]
        public void NoState_OtherDevice_DoNotCallHandler()
        {
            var calls = 0;
            Func<ServerState, HandlerOutcome> handler = s => { calls++; return HandlerOutcome.Ok(s); };

            var noState = CommandRunner.Run(null, Req("r"), handler, Json);
            Assert.AreEqual((int)CommandCode.NoState, noState.Response.Code);
            Assert.IsNull(noState.ToWrite);

            var other = CommandRunner.Run(Stored(), Req("r", session: "other"), handler, Json);
            Assert.AreEqual((int)CommandCode.OtherDevice, other.Response.Code);
            Assert.IsNull(other.ToWrite);
            Assert.IsNull(other.Response.State);
            Assert.AreEqual(0, calls);
        }

        [Test]
        public void SameRequestId_ReturnsStoredResult_NoWrite()
        {
            var calls = 0;
            Func<ServerState, HandlerOutcome> handler = s => { calls++; s.Gem -= 10; return HandlerOutcome.Ok(s, new Payload()); };

            var first = CommandRunner.Run(Stored(), Req("r1"), handler, Json);
            Assert.AreEqual("r1", first.ToWrite.LastRequestId);
            Assert.AreEqual(90, first.ToWrite.Gem);

            var second = CommandRunner.Run(first.ToWrite, Req("r1"), handler, Json);
            Assert.AreEqual(first.Response.ResultJson, second.Response.ResultJson);
            Assert.AreEqual((int)CommandCode.Ok, second.Response.Code);
            Assert.IsNull(second.ToWrite);
            Assert.AreEqual(90, second.Response.State.Gem, "Gem không trừ lần hai");
            Assert.AreEqual(1, calls);
        }

        [Test]
        public void EmptyRequestId_NeverCountsAsReplay()
        {
            // State mới có LastRequestId rỗng: RequestId rỗng không được khớp nhầm.
            var calls = 0;
            CommandRunner.Run(Stored(), Req(""), s => { calls++; return HandlerOutcome.Ok(s); }, Json);
            Assert.AreEqual(1, calls);
        }

        [Test]
        public void Reject_LeavesStoredUntouched()
        {
            var stored = Stored();
            var run = CommandRunner.Run(stored, Req("r"),
                s => { s.Gem = 0; s.Revision = 99; return HandlerOutcome.Reject(CommandCode.NotEnoughGem); }, Json);
            Assert.AreEqual((int)CommandCode.NotEnoughGem, run.Response.Code);
            Assert.IsNull(run.Response.State);
            Assert.AreEqual("", run.Response.ResultJson);
            Assert.IsNull(run.ToWrite);
            Assert.AreEqual(100, stored.Gem);
            Assert.AreEqual(3, stored.Revision);
        }

        [Test]
        public void Ok_IncrementsRevision_StoresResult()
        {
            var stored = Stored();
            var run = CommandRunner.Run(stored, Req("r9"), s => HandlerOutcome.Ok(s, new Payload()), Json);
            Assert.AreEqual(4, run.ToWrite.Revision);
            Assert.AreEqual("r9", run.ToWrite.LastRequestId);
            Assert.AreEqual(Json(new Payload()), run.ToWrite.LastResultJson);
            Assert.AreEqual(run.ToWrite.LastResultJson, run.Response.ResultJson);
            Assert.AreSame(run.ToWrite, run.Response.State);
            Assert.AreEqual(3, stored.Revision, "bản gốc không bị đổi");

            var noPayload = CommandRunner.Run(stored, Req("r10"), s => HandlerOutcome.Ok(s), Json);
            Assert.AreEqual("", noPayload.ToWrite.LastResultJson);
        }

        [Test]
        public void DataHash_KnownVector()
        {
            Assert.AreEqual("cbf29ce484222325", DataHash.Of(""));
            Assert.AreEqual("af63dc4c8601ec8c", DataHash.Of("a"));
        }
    }
}
