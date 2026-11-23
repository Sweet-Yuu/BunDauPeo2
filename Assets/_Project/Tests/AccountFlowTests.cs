using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BunDauPeo.Core;
using BunDauPeo.Missions;
using BunDauPeo.UI;
using NUnit.Framework;
using UnityEngine;

namespace BunDauPeo.Tests
{
    // Luật các hộp tài khoản (Review Focus 4 của kế hoạch đợt 3): không ForceLink, Huỷ giữ tiến độ, Chuyển về Tiêu đề, mất mạng không nửa vời.
    public class AccountFlowTests
    {
        private const string Strong = "Abcdef1!";

        private LocalAccountDirectory _directory;
        private LocalAccountService _anonymous;
        private LocalAccountService _owner;
        private List<string> _calls;
        private bool _hasProgress;

        [SetUp]
        public void SetUp()
        {
            _directory = new LocalAccountDirectory();
            // Người "b" đã liên kết Google; người "a" (ẩn danh) là máy đang chơi.
            _owner = new LocalAccountService("b", _directory, google: true);
            _owner.LinkGoogleAsync().GetAwaiter().GetResult();
            _anonymous = new LocalAccountService("a", _directory, google: true);
            _calls = new List<string>();
            _hasProgress = true;
        }

        private AccountFlow Flow() => new(_anonymous, new AccountSwitcher(async signIn =>
        {
            _calls.Add("stopSync");
            var result = await signIn();
            if (result.Ok) _calls.Add("shutdown");
            return result;
        }), () => _calls.Add("title"), () => _hasProgress);

        private static AccountStatus Run(Task<AccountResult> task) => task.GetAwaiter().GetResult().Status;

        [Test]
        public void Link_Conflict_AsksSwitch_CancelKeepsProgress()
        {
            var flow = Flow();

            Assert.AreEqual(AccountStatus.Conflict, Run(flow.LinkAsync(AccountMethod.Google, "", "")));

            // Huỷ = không gọi gì thêm: vẫn là người chơi cũ, không session nào bị bỏ.
            Assert.AreEqual("a", _anonymous.PlayerId);
            Assert.IsFalse(_anonymous.IsLinked);
            CollectionAssert.IsEmpty(_calls);
        }

        [Test]
        public void Link_Conflict_Switch_SignsOutThenInThenTitle()
        {
            var flow = Flow();
            Run(flow.LinkAsync(AccountMethod.Google, "", ""));

            Assert.AreEqual(AccountStatus.Ok, Run(flow.SignInAsync(AccountMethod.Google, "", "", confirmedLoss: true)));

            CollectionAssert.AreEqual(new[] { "stopSync", "shutdown", "title" }, _calls);
            Assert.AreEqual("b", _anonymous.PlayerId, "now signed in as the player who owns that identity");
        }

        [Test]
        public void Link_Offline_NoHalfState()
        {
            _anonymous.Offline = true;
            var flow = Flow();

            Assert.AreEqual(AccountStatus.Offline, Run(flow.LinkAsync(AccountMethod.UserPass, "abc", Strong)));
            Assert.AreEqual(AccountStatus.Offline, Run(flow.SignInAsync(AccountMethod.Google, "", "", confirmedLoss: true)));

            Assert.AreEqual("a", _anonymous.PlayerId);
            Assert.IsFalse(_anonymous.IsLinked);
            CollectionAssert.DoesNotContain(_calls, "shutdown", "a failed sign-in keeps the session");
            CollectionAssert.DoesNotContain(_calls, "title");
        }

        [Test]
        public void SignIn_OverAnonymousProgress_RequiresWarnConfirm()
        {
            var flow = Flow();
            Assert.IsTrue(flow.NeedsLoseWarning);

            Assert.AreEqual(AccountStatus.Cancelled, Run(flow.SignInAsync(AccountMethod.Google, "", "", confirmedLoss: false)));
            CollectionAssert.IsEmpty(_calls, "without the confirmation nothing is called");
            Assert.AreEqual("a", _anonymous.PlayerId);

            Assert.AreEqual(AccountStatus.Ok, Run(flow.SignInAsync(AccountMethod.Google, "", "", confirmedLoss: true)));
            Assert.AreEqual("b", _anonymous.PlayerId);
        }

        [Test]
        public void SignIn_NoProgressOrLinked_NoWarning()
        {
            _hasProgress = false;
            Assert.IsFalse(Flow().NeedsLoseWarning, "fresh install has nothing to lose");

            _hasProgress = true;
            Run(_anonymous.LinkUserPassAsync("abc", Strong));
            Assert.IsFalse(Flow().NeedsLoseWarning, "a linked player can sign back in");
            Assert.AreEqual(AccountStatus.Ok, Run(Flow().SignInAsync(AccountMethod.Google, "", "", confirmedLoss: false)));
        }

        [Test]
        public void Link_Success_RaisesChanged_NewLinkMissionClaimable()
        {
            var changed = 0;
            _anonymous.Changed += () => changed++;
            Assert.IsFalse(MissionService.ClientDoneFromSave(new PlayerSave(), _anonymous.IsLinked).Contains("new_link"));

            Assert.AreEqual(AccountStatus.Ok, Run(Flow().LinkAsync(AccountMethod.UserPass, "abc", Strong)));

            Assert.AreEqual(1, changed);
            Assert.IsTrue(MissionService.ClientDoneFromSave(new PlayerSave(), _anonymous.IsLinked).Contains("new_link"));
        }

        [Test]
        public void NeverForceLink()
        {
            var path = Path.Combine(Application.dataPath, "_Project/Online/Scripts/UgsAccountService.cs");
            var source = File.ReadAllText(path);
            StringAssert.DoesNotContain("ForceLink =", source);
            StringAssert.DoesNotContain("ForceLink=", source);
            StringAssert.DoesNotContain("ForceLink:", source);
        }
    }
}
