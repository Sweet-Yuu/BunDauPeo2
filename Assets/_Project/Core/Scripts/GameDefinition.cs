using UnityEngine;

namespace BunDauPeo.Core
{
    // Chỉ loại SO được save tham chiếu mới kế thừa lớp này; Id là khoá nối từ save về SO.
    public abstract class GameDefinition : ScriptableObject
    {
        [SerializeField] private string _id;

        public string Id => _id;
    }
}
