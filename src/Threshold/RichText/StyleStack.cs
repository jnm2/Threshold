using System;
using System.Collections.Generic;
using System.Linq;

namespace Threshold.RichText
{
    internal readonly struct StyleStack<T>
    {
        private readonly List<AppliedValue> appliedValues;

        public T Current => appliedValues.Last().Value;

        public StyleStack(T defaultValue)
        {
            appliedValues = new List<AppliedValue>();
            appliedValues.Add(new AppliedValue(this, defaultValue));
        }

        public IDisposable Apply(T value)
        {
            var appliedValue = new AppliedValue(this, value);
            appliedValues.Add(appliedValue);
            return appliedValue;
        }

        private sealed class AppliedValue : IDisposable
        {
            private readonly StyleStack<T> owner;

            public T Value { get; }

            public AppliedValue(StyleStack<T> owner, T value)
            {
                this.owner = owner;
                Value = value;
            }

            public void Dispose()
            {
                owner.appliedValues.Remove(this);
            }
        }
    }
}
