using System;
using UnityEngine;

namespace BunDauPeo.Core
{
    public static class EventExtensions
    {
        // Sự kiện bắn sau khi trạng thái đã đổi xong, nên handler lỗi chỉ được log; nếu để nó ném ra thì
        // người gọi (vd. gacha sau TrySpend) bỏ dở việc còn lại và handler phía sau không được gọi.
        public static void SafeInvoke(this Action action)
        {
            if (action == null) return;
            foreach (var handler in action.GetInvocationList())
            {
                try
                {
                    ((Action)handler)();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
    }
}
