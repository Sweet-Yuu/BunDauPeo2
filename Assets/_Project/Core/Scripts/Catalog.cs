using System;
using System.Collections.Generic;
using UnityEngine;

namespace BunDauPeo.Core
{
    // Thay cho Resources: save chỉ giữ Id, catalog là nơi tra Id ra SO.
    public abstract class Catalog<T> : ScriptableObject where T : GameDefinition
    {
        [SerializeField] private T[] _items = Array.Empty<T>();

        public IReadOnlyList<T> Items => _items;

        // shortcut: quét tuyến tính, đủ cho vài trăm mục; thêm Dictionary nếu catalog lớn hơn nhiều.
        public bool TryGet(string id, out T definition)
        {
            if (!string.IsNullOrEmpty(id))
            {
                foreach (var item in _items)
                {
                    if (item != null && item.Id == id)
                    {
                        definition = item;
                        return true;
                    }
                }
            }

            definition = null;
            return false;
        }

        public List<string> Validate()
        {
            var errors = new List<string>();
            var seen = new HashSet<string>();
            for (var i = 0; i < _items.Length; i++)
            {
                var item = _items[i];
                if (item == null)
                    errors.Add($"Items[{i}]: missing reference");
                else if (string.IsNullOrWhiteSpace(item.Id))
                    errors.Add($"Items[{i}] ({item.name}): blank Id");
                else if (!seen.Add(item.Id))
                    errors.Add($"Items[{i}] ({item.name}): duplicate Id '{item.Id}'");
            }

            return errors;
        }

        private void OnValidate()
        {
            foreach (var error in Validate()) Debug.LogWarning(error, this);
        }
    }
}
