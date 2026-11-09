namespace BunDauPeo.UI
{
    // Trang kín màn nằm trên một ô của hub. Close() làm đúng việc nút Quay lại của trang làm.
    public interface IHubPage
    {
        bool IsOpen { get; }
        void Close();
    }
}
