using System;
using System.Collections.Generic;

namespace BunDauPeo.Tests
{
    // Trả lần lượt các giá trị đã định sẵn cho NextDouble, hết thì trả fallback; Calls đếm số lần gọi.
    internal sealed class ScriptedRandom : Random
    {
        private readonly double _fallback;
        private readonly Queue<double> _values;

        public ScriptedRandom(double fallback = 0.0, params double[] values)
        {
            _fallback = fallback;
            _values = new Queue<double>(values);
        }

        public int Calls { get; private set; }

        public override double NextDouble()
        {
            Calls++;
            return _values.Count > 0 ? _values.Dequeue() : _fallback;
        }
    }

    // Trả 0.0 cho đến lần gọi thứ throwOnCall (tính từ 1) thì ném, để thử RNG hỏng giữa chừng một lần quay 10.
    internal sealed class ThrowingRandom : Random
    {
        private readonly int _throwOnCall;
        private int _calls;

        public ThrowingRandom(int throwOnCall = 1) => _throwOnCall = throwOnCall;

        public override double NextDouble() =>
            ++_calls >= _throwOnCall ? throw new InvalidOperationException("scripted RNG failure") : 0.0;
    }
}
