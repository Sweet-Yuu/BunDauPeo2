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

        public static void SafeInvoke<T1, T2>(this Action<T1, T2> action, T1 first, T2 second)
        {
            if (action == null) return;
            foreach (var handler in action.GetInvocationList())
            {
                try
                {
                    ((Action<T1, T2>)handler)(first, second);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
    }
}
