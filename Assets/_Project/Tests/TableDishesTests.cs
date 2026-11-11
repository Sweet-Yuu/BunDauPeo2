using BunDauPeo.Restaurant;
using BunDauPeo.UI;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class TableDishesTests
    {
        private static CustomerSnapshot Customer(int id, CustomerState state, int table) =>
            new CustomerSnapshot(id, null, null, state, table, 0f);

        [Test]
        public void OnlyEatingCustomersMarkTheirTable()
        {
            var customers = new[]
            {
                Customer(1, CustomerState.Eating, 2),
                Customer(2, CustomerState.WaitingForServer, 5),
                Customer(3, CustomerState.Eating, -1),
                Customer(4, CustomerState.Eating, 9),
            };

            var dishes = TableDishes.Occupied(customers, 8);

            Assert.AreEqual(8, dishes.Length);
            for (var i = 0; i < dishes.Length; i++)
            {
                Assert.AreEqual(i == 2, dishes[i], $"table {i}");
            }
        }
    }
}
