using System;
using BunDauPeo.Core;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class RestaurantBarPrefTests
    {
        [Test]
        public void RestaurantBarPref_ReadsGet_SetPassesValueAndReturnsResult()
        {
            var open = true;
            bool? received = null;
            var pref = new RestaurantBarPref(() => open, value => { received = value; return false; });

            Assert.IsTrue(pref.IsOpen);
            open = false;
            Assert.IsFalse(pref.IsOpen, "đọc lại mỗi lần, không nhớ giá trị cũ");
            Assert.IsFalse(pref.Set(true), "lưu thất bại phải báo false");
            Assert.AreEqual(true, received);
            Assert.Throws<ArgumentNullException>(() => new RestaurantBarPref(null, _ => true));
            Assert.Throws<ArgumentNullException>(() => new RestaurantBarPref(() => true, null));
        }
    }
}
