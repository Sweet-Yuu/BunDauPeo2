using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace BunDauPeo.EditorTools
{
    // Ký bản phát hành bằng biến môi trường. Bí mật chỉ nằm trong bộ nhớ lúc build, không ghi ra file hay log.
    public static class AndroidSigning
    {
        public const string KeystorePathVar = "BDP_KEYSTORE_PATH";
        public const string KeystorePassVar = "BDP_KEYSTORE_PASS";
        public const string KeyAliasVar = "BDP_KEY_ALIAS";
        public const string KeyPassVar = "BDP_KEY_PASS";

        // Chỉ ghi TÊN biến thiếu, không bao giờ ghi giá trị.
        public static List<string> Problems(Func<string, string> env, string projectRoot)
        {
            var p = new List<string>();
            foreach (var v in new[] { KeystorePathVar, KeystorePassVar, KeyAliasVar, KeyPassVar })
                if (string.IsNullOrEmpty(env(v))) p.Add($"Thiếu biến môi trường {v}.");

            var path = env(KeystorePathVar);
            if (string.IsNullOrEmpty(path)) return p;
            if (!File.Exists(path))
                p.Add($"{KeystorePathVar} trỏ tới file không tồn tại.");
            var full = Path.GetFullPath(path).Replace(Path.DirectorySeparatorChar, '/');
            var root = Path.GetFullPath(projectRoot).Replace(Path.DirectorySeparatorChar, '/').TrimEnd('/') + "/";
            if (full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                p.Add($"{KeystorePathVar} nằm trong thư mục project; khoá phải để ngoài repo.");
            return p;
        }

        public sealed class Saved
        {
            internal bool Use;
            internal string Name, Pass, Alias, AliasPass;
        }

        public static Saved Apply(Func<string, string> env)
        {
            var saved = new Saved
            {
                Use = PlayerSettings.Android.useCustomKeystore,
                Name = PlayerSettings.Android.keystoreName,
                Pass = PlayerSettings.Android.keystorePass,
                Alias = PlayerSettings.Android.keyaliasName,
                AliasPass = PlayerSettings.Android.keyaliasPass,
            };
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = env(KeystorePathVar);
            PlayerSettings.Android.keystorePass = env(KeystorePassVar);
            PlayerSettings.Android.keyaliasName = env(KeyAliasVar);
            PlayerSettings.Android.keyaliasPass = env(KeyPassVar);
            return saved;
        }

        public static void Restore(Saved s)
        {
            PlayerSettings.Android.useCustomKeystore = s.Use;
            // Setter của Unity ghi '{inproject}: ' vào ProjectSettings.asset khi tên trống (kể cả null); trả thẳng qua SerializedObject.
            var so = new SerializedObject(Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings"));
            so.FindProperty("AndroidKeystoreName").stringValue = s.Name ?? "";
            so.ApplyModifiedPropertiesWithoutUndo();
            PlayerSettings.Android.keystorePass = s.Pass;
            PlayerSettings.Android.keyaliasName = s.Alias;
            PlayerSettings.Android.keyaliasPass = s.AliasPass;
        }
    }
}
