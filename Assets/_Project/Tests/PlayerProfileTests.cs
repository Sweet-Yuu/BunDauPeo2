using System;
using BunDauPeo.Core;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class PlayerProfileTests
    {
        private static readonly Func<string> Get = () => "";
        private static readonly Func<string, bool> Set = _ => true;

        [Test]
        public void NullArgs_SetCallsThrough_ChangedAlwaysRaised()
        {
            Assert.Throws<ArgumentNullException>(() => new PlayerProfile(null, Get, Set, Set));
            Assert.Throws<ArgumentNullException>(() => new PlayerProfile(Get, null, Set, Set));
            Assert.Throws<ArgumentNullException>(() => new PlayerProfile(Get, Get, null, Set));
            Assert.Throws<ArgumentNullException>(() => new PlayerProfile(Get, Get, Set, null));

            string name = null, avatar = null;
            var result = true;
            var profile = new PlayerProfile(() => name, () => avatar, v => { name = v; return result; }, v => { avatar = v; return result; });
            var changed = 0;
            profile.Changed += () => changed++;

            Assert.AreEqual("", profile.Name, "getter trả null thì Name là rỗng");
            Assert.AreEqual("", profile.AvatarId, "getter trả null thì AvatarId là rỗng");

            Assert.IsTrue(profile.SetName("An"));
            Assert.AreEqual("An", name, "gọi đúng hàm set tên với đúng giá trị");
            Assert.AreEqual("An", profile.Name, "đọc lại mỗi lần");
            Assert.AreEqual(1, changed);

            result = false;
            Assert.IsFalse(profile.SetName("Bo"), "trả đúng kết quả của hàm set");
            Assert.AreEqual(2, changed, "lưu lỗi vẫn phát Changed");

            result = true;
            Assert.IsTrue(profile.SetAvatar("a"));
            Assert.AreEqual("a", avatar);
            Assert.AreEqual("a", profile.AvatarId);
            Assert.AreEqual(3, changed);
            result = false;
            Assert.IsFalse(profile.SetAvatar("b"));
            Assert.AreEqual("b", avatar, "hàm set avatar, không phải hàm set tên");
            Assert.AreEqual("Bo", name, "set avatar không đụng tên");
            Assert.AreEqual(4, changed);
        }
    }
}
