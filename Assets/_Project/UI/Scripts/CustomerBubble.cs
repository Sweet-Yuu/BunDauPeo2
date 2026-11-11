using BunDauPeo.Restaurant;

namespace BunDauPeo.UI
{
    public enum CustomerBubbleKind { None, Food, Pay }

    // Bóng trạng thái trên đầu khách (spec 5.6).
    public static class CustomerBubble
    {
        // WaitingForTable và Eating (và trạng thái mới) rơi vào nhánh mặc định None; AllStates_MapToKind bắt khi thêm CustomerState mà quên bảng.
        public static CustomerBubbleKind For(CustomerState state) => state switch
        {
            CustomerState.WaitingForCook => CustomerBubbleKind.Food,
            CustomerState.Cooking => CustomerBubbleKind.Food,
            CustomerState.WaitingForServer => CustomerBubbleKind.Food,
            CustomerState.Serving => CustomerBubbleKind.Food,
            CustomerState.WaitingForCashier => CustomerBubbleKind.Pay,
            CustomerState.Paying => CustomerBubbleKind.Pay,
            _ => CustomerBubbleKind.None,
        };
    }
}
