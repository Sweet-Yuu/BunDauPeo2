using System;
using System.Collections.Generic;
using System.IO;
using BunDauPeo.EditorTools;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class AndroidSigningTests
    {
        private const string Secret = "s3cr3t-value";

        [Test]
        public void Signing_FromEnvironment_Cases()
        {
            var root = Path.Combine(Path.GetTempPath(), "bdp_proj");
            var outside = Path.Combine(Path.GetTempPath(), "bdp_sign_test.keystore");
            var inside = Path.Combine(root, "k.keystore");
            Directory.CreateDirectory(root);
            File.WriteAllText(outside, "x");
            File.WriteAllText(inside, "x");
            try
            {
                Func<string, string> Env(Dictionary<string, string> d) => k => d.TryGetValue(k, out var v) ? v : null;
                Dictionary<string, string> Full() => new Dictionary<string, string>
                {
                    [AndroidSigning.KeystorePathVar] = outside, [AndroidSigning.KeystorePassVar] = Secret,
                    [AndroidSigning.KeyAliasVar] = "alias", [AndroidSigning.KeyPassVar] = Secret,
                };

                CollectionAssert.IsEmpty(AndroidSigning.Problems(Env(Full()), root), "valid");

                foreach (var name in new[] { AndroidSigning.KeystorePathVar, AndroidSigning.KeystorePassVar, AndroidSigning.KeyAliasVar, AndroidSigning.KeyPassVar })
                {
                    var d = Full();
                    d.Remove(name);
                    var problems = AndroidSigning.Problems(Env(d), root);
                    Assert.AreEqual(1, problems.Count, name);
                    StringAssert.Contains(name, problems[0]);
                    StringAssert.DoesNotContain(Secret, problems[0]);
                }

                var missing = Full();
                missing[AndroidSigning.KeystorePathVar] = Path.Combine(Path.GetTempPath(), "bdp_nope.keystore");
                Assert.AreEqual(1, AndroidSigning.Problems(Env(missing), root).Count, "missing file");

                var inRepo = Full();
                inRepo[AndroidSigning.KeystorePathVar] = inside;
                var inProblems = AndroidSigning.Problems(Env(inRepo), root);
                Assert.AreEqual(1, inProblems.Count, "inside project");
                StringAssert.Contains("trong thư mục project", inProblems[0]);
            }
            finally
            {
                File.Delete(outside);
                Directory.Delete(root, true);
            }
        }
    }
}
