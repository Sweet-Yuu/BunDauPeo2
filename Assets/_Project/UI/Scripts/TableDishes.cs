using System.Collections.Generic;
using BunDauPeo.Restaurant;

namespace BunDauPeo.UI
{
    public static class TableDishes
    {
        // Bàn i true khi có khách đang Eating ở bàn i; chỉ số ngoài [0, tableCount) bị bỏ qua.
        public static bool[] Occupied(IReadOnlyList<CustomerSnapshot> customers, int tableCount)
        {
            var result = new bool[tableCount < 0 ? 0 : tableCount];
            for (var i = 0; i < customers.Count; i++)
            {
                var c = customers[i];
                if (c.State == CustomerState.Eating && c.TableIndex >= 0 && c.TableIndex < result.Length)
                {
                    result[c.TableIndex] = true;
                }
            }

            return result;
        }
    }
}
