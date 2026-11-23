using BunDauPeo.Core;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class LocalAccountServiceTests
    {
        private const string Strong = "Abcdef1!";

        private static AccountStatus Run(System.Threading.Tasks.Task<AccountResult> task) => task.GetAwaiter().GetResult().Status;

        [Test]
        public void LinkUserPass_FirstTime_IsLinkedAndNamed()
        {
            var account = new LocalAccountService("a");
            Assert.IsFalse(account.IsLinked);
            var changed = 0;
            account.Changed += () => changed++;

            Assert.AreEqual(AccountStatus.Ok, Run(account.LinkUserPassAsync("abc", Strong)));

            Assert.IsTrue(account.IsLinked);
            CollectionAssert.AreEqual(new[] { "abc" }, account.LinkedNames);
            Assert.AreEqual(1, changed);
        }

        [Test]
        public void LinkUserPass_NameUsedByOtherPlayer_UsernameTaken_CaseInsensitive()
        {
            var directory = new LocalAccountDirectory();
            var first = new LocalAccountService("a", directory);
            var second = new LocalAccountService("b", directory);
            Assert.AreEqual(AccountStatus.Ok, Run(first.LinkUserPassAsync("Abc", Strong)));

            Assert.AreEqual(AccountStatus.UsernameTaken, Run(second.LinkUserPassAsync("aBC", Strong)));
            Assert.IsFalse(second.IsLinked, "a refused link changes nothing");
        }

        [Test]
        public void LinkUserPass_WeakPassword_WeakPassword()
        {
            var account = new LocalAccountService("a");
            foreach (var weak in new[] { "abcdefg1!", "ABCDEFG1!", "Abcdefgh!", "Abcdefg1", "Ab1!", "Abcdef1!Abcdef1!Abcdef1!Abcdef1!" })
                Assert.AreEqual(AccountStatus.WeakPassword, Run(account.LinkUserPassAsync("abc", weak)), weak);
            Assert.IsFalse(account.IsLinked);
        }

        [Test]
        public void SignInUserPass_WrongPasswordOrName_WrongCredentials_KeepsPlayer()
        {
            var directory = new LocalAccountDirectory();
            var owner = new LocalAccountService("a", directory);
            Run(owner.LinkUserPassAsync("abc", Strong));
            var other = new LocalAccountService("b", directory);

            Assert.AreEqual(AccountStatus.WrongCredentials, Run(other.SignInUserPassAsync("abc", "Wrong1234!")));
            Assert.AreEqual(AccountStatus.WrongCredentials, Run(other.SignInUserPassAsync("nobody", Strong)));
            Assert.AreEqual("b", other.PlayerId);

            Assert.AreEqual(AccountStatus.Ok, Run(other.SignInUserPassAsync("ABC", Strong)));
            Assert.AreEqual("a", other.PlayerId);
            CollectionAssert.AreEqual(new[] { "abc" }, other.LinkedNames);
        }

        [Test]
        public void LinkGoogle_SameIdentityTwoPlayers_Conflict_NoForce()
        {
            var directory = new LocalAccountDirectory();
            var first = new LocalAccountService("a", directory, google: true);
            var second = new LocalAccountService("b", directory, google: true);
            Assert.AreEqual(AccountStatus.Ok, Run(first.LinkGoogleAsync()));

            Assert.AreEqual(AccountStatus.Conflict, Run(second.LinkGoogleAsync()));
            Assert.IsFalse(second.IsLinked);
            Assert.IsTrue(first.IsLinked, "the first player keeps the link");
        }

        [Test]
        public void Offline_EverythingReturnsOffline_ChangesNothing()
        {
            var account = new LocalAccountService("a") { Offline = true };
            Assert.AreEqual(AccountStatus.Offline, Run(account.LinkUserPassAsync("abc", Strong)));
            Assert.AreEqual(AccountStatus.Offline, Run(account.SignInUserPassAsync("abc", Strong)));
            Assert.IsFalse(account.IsLinked);
        }

        [Test]
        public void GoogleIsOnlyAvailableWhenAsked()
        {
            CollectionAssert.AreEqual(new[] { AccountMethod.UserPass }, new LocalAccountService().Available);
            CollectionAssert.Contains(new LocalAccountService(google: true).Available, AccountMethod.Google);
            Assert.AreEqual(AccountStatus.Unavailable, Run(new LocalAccountService().LinkGoogleAsync()));
        }

        [Test]
        public void AccountRules_NameAndPassword()
        {
            Assert.IsTrue(AccountRules.ValidName("a.b-c@d_1"));
            Assert.IsFalse(AccountRules.ValidName("ab"), "3 minimum");
            Assert.IsFalse(AccountRules.ValidName(new string('a', 21)), "20 maximum");
            Assert.IsFalse(AccountRules.ValidName("a b c"));
            Assert.IsFalse(AccountRules.ValidName("tên"));
            Assert.IsFalse(AccountRules.ValidName(null));
            Assert.IsTrue(AccountRules.StrongPassword(Strong));
            Assert.IsFalse(AccountRules.StrongPassword(null));
        }
    }
}
