using System;
using BunDauPeo.Restaurant;
using BunDauPeo.UI;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class CustomerBubbleTests
    {
        [Test]
        public void AllStates_MapToKind()
        {
            // Bảng spec 5.6. Thêm CustomerState mà quên bảng này thì đỏ ở dòng đếm.
            var expected = new (CustomerState State, CustomerBubbleKind Kind)[]
            {
                (CustomerState.WaitingForTable, CustomerBubbleKind.None),
                (CustomerState.WaitingForCook, CustomerBubbleKind.Food),
                (CustomerState.Cooking, CustomerBubbleKind.Food),
                (CustomerState.WaitingForServer, CustomerBubbleKind.Food),
                (CustomerState.Serving, CustomerBubbleKind.Food),
                (CustomerState.Eating, CustomerBubbleKind.None),
                (CustomerState.WaitingForCashier, CustomerBubbleKind.Pay),
                (CustomerState.Paying, CustomerBubbleKind.Pay),
            };

            var states = (CustomerState[])Enum.GetValues(typeof(CustomerState));
            Assert.AreEqual(expected.Length, states.Length, "table must cover every CustomerState");

            foreach (var state in states)
            {
                var row = Array.Find(expected, e => e.State == state);
                Assert.AreEqual(row.Kind, CustomerBubble.For(state), state.ToString());
            }
        }
    }
}
