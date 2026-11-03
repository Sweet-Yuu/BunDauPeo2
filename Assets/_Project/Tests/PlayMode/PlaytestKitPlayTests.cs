using System.Collections;
using BunDauPeo.Playtest;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BunDauPeo.PlayModeTests
{
    public class PlaytestKitPlayTests
    {
        [UnityTest]
        public IEnumerator PlayModeAssembly_RunsAndSeesPlaytestKit()
        {
            yield return null;
            Assert.IsTrue(Application.isPlaying);
            Assert.AreEqual("UiDriver", typeof(UiDriver).Name);
        }
    }
}
