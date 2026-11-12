using System;
using System.Globalization;
using BunDauPeo.Characters;

namespace BunDauPeo.UI
{
    public static class AvatarRule
    {
        // id rỗng, null, không có trong catalog hoặc chưa sở hữu → null.
        public static CharacterDefinition Resolve(string savedId, Func<string, bool> isOwned, Func<string, CharacterDefinition> lookup)
        {
            if (string.IsNullOrEmpty(savedId)) return null;

            var character = lookup(savedId);
            return character != null && isOwned(savedId) ? character : null;
        }

        // Chữ cái đầu của DisplayName (text element đầu, viết hoa bất biến theo ngôn ngữ); null → "".
        public static string Initial(CharacterDefinition character)
        {
            var name = character != null ? character.DisplayName : null;
            return string.IsNullOrEmpty(name) ? "" : StringInfo.GetNextTextElement(name).ToUpperInvariant();
        }
    }
}
