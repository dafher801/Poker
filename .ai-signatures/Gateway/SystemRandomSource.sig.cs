// Source: Assets/Scripts/Gateway/SystemRandomSource.cs
// SystemRandomSource.cs
// IRandomSource의 실제 구현체.
// System.Random 기반 Fisher-Yates 알고리즘으로 리스트를 셔플한다.
// 생성자에 seed를 전달하면 재현 가능한 셔플을 수행할 수 있다.

using System;
using System.Collections.Generic;

namespace TexasHoldem.Gateway
{
    public class SystemRandomSource : IRandomSource
    {
        private readonly Random _random;

        public SystemRandomSource() { /* ... */ }
        {
            _random = new Random();
        }

        public SystemRandomSource(int seed) { /* ... */ }
        {
            _random = new Random(seed);
        }

        public void Shuffle<T>(IList<T> list) { /* ... */ }
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }
    }
}
