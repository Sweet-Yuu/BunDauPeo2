using System.Collections.Generic;

namespace BunDauPeo.UI
{
    // Mũi tên ‹ › của panel chi tiết: nhân vật kế trước/sau trong danh sách, đi vòng tròn.
    public static class CharacterDetailNav
    {
        public static string Neighbor(IReadOnlyList<string> ids, string currentId, int step)
        {
            if (ids == null || ids.Count == 0) return "";
            var index = -1;
            for (var i = 0; i < ids.Count; i++)
                if (ids[i] == currentId) index = i;
            if (index < 0) return ids[0];
            return ids[((index + step) % ids.Count + ids.Count) % ids.Count];
        }
    }
}
