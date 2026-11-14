using BunDauPeo.Boot;
using BunDauPeo.Online;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class ServerChoiceTests
    {
        private static string EnvironmentOf(bool isEditor, bool useUgsInEditor, bool isDevelopmentBuild) =>
            ((UgsGameServer)ServerChoice.Create(isEditor, useUgsInEditor, isDevelopmentBuild)).Environment;

        // Chỉ dựng đối tượng, không gọi mạng.
        [Test]
        public void EditorDefaultsLocal_BuildsUseUgs()
        {
            Assert.IsNull(ServerChoice.Create(true, false, true), "Editor in Local mode uses LocalGameServer");
            Assert.IsNull(ServerChoice.Create(true, false, false));
            Assert.AreEqual(ServerChoice.Development, EnvironmentOf(true, true, false), "Editor in UGS mode always uses development");

            foreach (var useUgs in new[] { false, true })
            {
                Assert.AreEqual(ServerChoice.Development, EnvironmentOf(false, useUgs, true), $"dev build, useUgs = {useUgs}");
                Assert.AreEqual(ServerChoice.Production, EnvironmentOf(false, useUgs, false), $"release build, useUgs = {useUgs}");
            }

            Assert.AreEqual("development", ServerChoice.Development);
            Assert.AreEqual("production", ServerChoice.Production);
        }
    }
}
