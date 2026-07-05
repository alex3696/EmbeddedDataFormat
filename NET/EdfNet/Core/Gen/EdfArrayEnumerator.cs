using EdfNet.Core.Gen;
using System;

namespace EdfNet.Core
{
    public struct EdfObjectArrayEnumerator<T, TEnumerator> : IEdfByteEnumerator
        where T : struct
        where TEnumerator : struct, IEdfByteEnumerator
    {
        private readonly Array _array;
        private int _arrayIndex;
        private readonly int _totalElements;
        private TEnumerator _currentElementEnum;
        private bool _isElementActive;
        private readonly int _baseIndexOffset;

        private readonly int[] _indices;
        private readonly int[] _dims;

        public EdfObjectArrayEnumerator(Array array, int baseIndexOffset)
        {
            _array = array;
            _arrayIndex = 0;
            _totalElements = array?.Length ?? 0;
            _isElementActive = false;
            _currentElementEnum = default;
            _baseIndexOffset = baseIndexOffset;

            if (array != null)
            {
                _indices = new int[array.Rank];
                _dims = new int[array.Rank];
                for (int i = 0; i < array.Rank; i++)
                {
                    _dims[i] = array.GetLength(i);
                }
            }
            else
            {
                _indices = Array.Empty<int>();
                _dims = Array.Empty<int>();
            }
        }

        private void UpdateIndices(int flatIndex)
        {
            int remainder = flatIndex;
            for (int i = _dims.Length - 1; i >= 0; i--)
            {
                _indices[i] = remainder % _dims[i];
                remainder /= _dims[i];
            }
        }

        public bool MoveNext()
        {
            if (_isElementActive)
            {
                if (_currentElementEnum.MoveNext()) return true;
                _isElementActive = false;
                _arrayIndex++;
            }

            if (_arrayIndex >= _totalElements) return false;

            UpdateIndices(_arrayIndex);
            T currentObj = (T)_array.GetValue(_indices);

            // ИСПРАВЛЕНИЕ 1: Безопасное создание обобщенного энумератора TEnumerator.
            // .NET очень эффективно оптимизирует Activator.CreateInstance для структур 
            // с конструктором, вызывая его без аллокаций в куче.
            _currentElementEnum = (TEnumerator)Activator.CreateInstance(typeof(TEnumerator), currentObj);

            _isElementActive = true;
            return _currentElementEnum.MoveNext();
        }

        public int CurrentIndex => _baseIndexOffset + _currentElementEnum.CurrentIndex;
        public PoType CurrentPoType => _currentElementEnum.CurrentPoType;
        public int CurrentPoLen => _currentElementEnum.CurrentPoLen;

        public int Write(Span<byte> destination)
        {
            return _currentElementEnum.Write(destination);
        }

        public int Read(ReadOnlySpan<byte> src)
        {
            int readLen = _currentElementEnum.Read(src);

            UpdateIndices(_arrayIndex);

            // ИСПРАВЛЕНИЕ 2: Теперь .Result доступен напрямую из интерфейса IEdfByteEnumerator!
            // Извлеченный object автоматически приводится к типу ячейки массива внутри SetValue.
            _array.SetValue(_currentElementEnum.Result, _indices);

            return readLen;
        }

        // Реализация свойства Result для самого EdfObjectArrayEnumerator
        // Возвращает весь массив целиком, когда обход завершен
        public object Result => _array;
    }
}
