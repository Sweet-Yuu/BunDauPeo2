using System;
using BunDauPeo.Core;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class ServiceLocatorTests
    {
        [TearDown]
        public void TearDown() => ServiceLocator.Clear();

        [Test]
        public void Get_Registered_ReturnsService()
        {
            var save = new PlayerSave();
            ServiceLocator.Register(save);
            Assert.AreSame(save, ServiceLocator.Get<PlayerSave>());
        }

        [Test]
        public void Get_Missing_Throws_NamingType()
        {
            var ex = Assert.Throws<InvalidOperationException>(() => ServiceLocator.Get<PlayerSave>());
            StringAssert.Contains("PlayerSave", ex.Message);
        }
    }
}
