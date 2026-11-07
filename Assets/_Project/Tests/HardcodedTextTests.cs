using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace BunDauPeo.Tests
{
    // Chữ người chơi thấy phải đi qua UiText. shortcut: chỉ bắt chuỗi có dấu; chữ Việt không dấu hay tiếng Anh viết cứng thì lọt, dựa vào review.
    public class HardcodedTextTests
    {
        private static readonly Regex StringLiteral = new(@"\$?@?""(?:[^""\\]|\\.)*""");
        private static readonly Regex Vietnamese = new("[À-ỹĐđ]");

        [Test]
        public void UiAndBuilders_HaveNoHardcodedVietnamese()
        {
            var root = Path.Combine(Application.dataPath, "_Project");
            var files = Directory.GetFiles(Path.Combine(root, "UI/Scripts"), "*.cs", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(Path.Combine(root, "Editor"), "*SceneBuilder.cs"))
                .Where(path => !Path.GetFileName(path).StartsWith("UiText") && Path.GetFileName(path) != "DevOverlay.cs")
                .ToList();
            Assert.IsNotEmpty(files, "không tìm thấy file nào để quét");

            var violations = files.SelectMany(path => FindViolations(Path.GetFileName(path), File.ReadAllLines(path))).ToList();
            Assert.IsEmpty(violations, "chữ Việt viết cứng, chuyển vào UiText:\n" + string.Join("\n", violations));
        }

        [Test]
        public void Scanner_FindsVietnameseLiteral_IgnoresComment()
        {
            Assert.AreEqual(1, FindViolations("a.cs", new[] { "var a = \"Đóng\";" }).Count);
            Assert.AreEqual(0, FindViolations("a.cs", new[] { "// \"Đóng\"" }).Count);
            Assert.AreEqual(0, FindViolations("a.cs", new[] { "var a = 1; // Đóng \"Đóng\"" }).Count);
            Assert.AreEqual(0, FindViolations("a.cs", new[] { "Debug.LogError(\"Lỗi\");" }).Count);
            Assert.AreEqual(0, FindViolations("a.cs", new[] { "var a = \"Close\";" }).Count);
        }

        internal static List<string> FindViolations(string fileName, string[] lines)
        {
            var result = new List<string>();
            for (var i = 0; i < lines.Length; i++)
            {
                var line = CutComment(lines[i]);
                if (line.Contains("Debug.Log")) continue;
                foreach (Match literal in StringLiteral.Matches(line))
                    if (Vietnamese.IsMatch(literal.Value)) result.Add($"{fileName}:{i + 1}: {literal.Value}");
            }

            return result;
        }

        // Cắt ở "//" đầu tiên nằm ngoài chuỗi (số dấu " đứng trước là chẵn). Thô: bỏ qua \" trong chuỗi.
        private static string CutComment(string line)
        {
            for (var i = line.IndexOf("//"); i >= 0; i = line.IndexOf("//", i + 1))
                if (line.Take(i).Count(c => c == '"') % 2 == 0) return line.Substring(0, i);
            return line;
        }
    }
}
