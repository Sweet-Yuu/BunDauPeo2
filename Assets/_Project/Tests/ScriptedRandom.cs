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

    internal sealed class ThrowingRandom : Random
    {
        public override double NextDouble() => throw new InvalidOperationException("scripted RNG failure");
    }
}
