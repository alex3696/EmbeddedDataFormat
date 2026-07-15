namespace EdfNet.Gen
{
    public struct EdfArrayObjectsEnumerator<T, TEnumerator> : IEdfByteEnumerator
        where T : class, new()
        where TEnumerator : struct, IEdfByteEnumerator<T>
    {
        private readonly Array _array;
        private int _arrayIndex;
        private TEnumerator _currentElementEnum;
        private bool _isElementActive;
        private readonly Func<T, TEnumerator> _factory; // Фабрика для создания без рефлексии
        private readonly int[] _indices;
        private readonly int[] _dims;

        public EdfArrayObjectsEnumerator(Array array, Func<T, TEnumerator> factory)
        {
            _array = array;
            _arrayIndex = 0;
            _isElementActive = false;
            _currentElementEnum = default;
            _factory = factory;

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
                _indices = [];
                _dims = [];
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
        public bool MoveNext(EdfType et = default)
        {
            // 1. Если внутренний автомат структуры еще работает — крутим его
            if (_isElementActive)
            {
                if (_currentElementEnum.MoveNext(et)) return true;

                // Текущий элемент массива закончился
                _isElementActive = false;
                _arrayIndex++;
            }

            // 2. Если весь массив закончился — выходим
            if (_arrayIndex >= _array.Length) return false;

            // 3. Обновляем индексы многомерного массива для следующего шага
            UpdateIndices(_arrayIndex);
            T currentObj = (T?)_array.GetValue(_indices) ?? new T();

            // 4. создаем энумератор через переданную фабрику 
            _currentElementEnum = _factory(currentObj);
            _isElementActive = true;

            // Важнейший шаг: СРАЗУ делаем первый шаг по примитивам нового элемента.
            // Если у элемента есть поля, он вернет true и мы выйдем из MoveNext() на валидный примитив.
            // Если элемент пустой (хотя у нас [EdfBinSerializable] типы всегда имеют поля), цикл пойдет к следующему элементу массива.
            if (_currentElementEnum.MoveNext(et))
                return true;

            _isElementActive = false;
            _arrayIndex++;
            return false;
        }

        public int CurrentIndex => _currentElementEnum.CurrentIndex;
        public PoType CurrentPoType => _currentElementEnum.CurrentPoType;
        public int CurrentPoLen => _currentElementEnum.CurrentPoLen;

        public int Write(Span<byte> destination) => _currentElementEnum.Write(destination);

        public int Read(ReadOnlySpan<byte> src)
        {
            int readLen = _currentElementEnum.Read(src);

            // Записываем обновленный из файла объект обратно в массив без рефлексии и боксинга
            _array.SetValue(_currentElementEnum.Result, _indices);
            return readLen;
        }

    }
}
