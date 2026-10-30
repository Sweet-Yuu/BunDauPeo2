using System;
using System.Collections.Generic;

namespace BunDauPeo.Core
{
    // Chỉ MonoBehaviour view dùng; class logic nhận phụ thuộc qua constructor để test được.
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> Services = new();

        public static void Register<T>(T service)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            Services[typeof(T)] = service;
        }

        public static T Get<T>()
        {
            if (Services.TryGetValue(typeof(T), out var service)) return (T)service;
            throw new InvalidOperationException($"Service {typeof(T).Name} is not registered. Is the Boot scene running?");
        }

        public static void Clear() => Services.Clear();
    }
}
