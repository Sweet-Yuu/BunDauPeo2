using BunDauPeo.UI;

namespace BunDauPeo.Tests
{
    // Trang giả cho HubNavTests: đếm số lần Close. Không phải MonoBehaviour vì assembly Tests chỉ có ở Editor.
    internal sealed class FakeHubPage : IHubPage
    {
        public bool Open;
        public int Closed;

        public bool IsOpen => Open;

        public void Close()
        {
            Open = false;
            Closed++;
        }
    }
}
