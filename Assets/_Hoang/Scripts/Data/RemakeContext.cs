using System;
using BunDauPeo.Core;
using UnityEngine;

namespace Hoang.Remake
{
    public sealed class RemakeContext
    {
        private readonly Func<HoangSaveData, bool> _write;
        private readonly Action<HoangSaveData> _defaults;
        private readonly Func<HoangSaveData, bool> _resetWrite;
        private bool _committing;

        public HoangSaveData Save { get; }
        public Wallet Wallet { get; }
        public event Action Changed;
        public event Action ResetCompleted;

        public RemakeContext(HoangSaveData save, Func<HoangSaveData, bool> write,
            Action<HoangSaveData> defaults = null, Func<HoangSaveData, bool> resetWrite = null)
        {
            Save = save ?? throw new ArgumentNullException(nameof(save));
            _write = write ?? throw new ArgumentNullException(nameof(write));
            _defaults = defaults;
            _resetWrite = resetWrite ?? write;
            Wallet = new Wallet(save);
        }

        public bool Commit(Action<HoangSaveData> mutation)
        {
            if (mutation == null) throw new ArgumentNullException(nameof(mutation));
            if (_committing) return false;
            _committing = true;
            try
            {
                var candidate = Clone(Save);
                mutation(candidate);
                if (!_write(candidate)) return false;
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(candidate), Save);
            }
            finally { _committing = false; }
            Wallet.NotifyChanged();
            Changed.SafeInvoke();
            return true;
        }

        public bool Persist() => !_committing && _write(Clone(Save));

        // Chỉ UI đã hỏi xác nhận mới gọi. Repository chuẩn bị candidate trước khi thay save.
        public bool Reset()
        {
            if (_committing) return false;
            var candidate = new HoangSaveData();
            _defaults?.Invoke(candidate);
            if (!_resetWrite(candidate)) return false;
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(candidate), Save);
            ResetCompleted.SafeInvoke();
            Wallet.NotifyChanged();
            Changed.SafeInvoke();
            return true;
        }

        public static HoangSaveData Clone(HoangSaveData save) =>
            JsonUtility.FromJson<HoangSaveData>(JsonUtility.ToJson(save));
    }
}
