// Source: Assets/Scripts/Gateway/FixedRandomSource.cs
// FixedRandomSource.cs
// 통합 테스트용 IRandomSource 구현체.
// 두 가지 모드를 지원한다:
// 1. 고정 덱 모드: Card[] 배열을 받아 Shuffle 호출 시 해당 순서로 덱을 재배치한다.
// 2. 고정 난수 모드: int[] 시퀀스를 받아 Fisher-Yates 셔플의 각 단계에서 사용할 난수를 순차 제공한다.
// Next(int, int) 메서드로 사전 정의된 난수 값을 순서대로 반환하며, 시퀀스 소진 시 예외를 던진다.

using System;
using System.Collections.Generic;
using TexasHoldem.Entity;

namespace TexasHoldem.Gateway
{
    public class FixedRandomSource : IRandomSource
    {
        private readonly Card[] _fixedDeck;
        private readonly int[] _randomSequence;
        private int _sequenceIndex;

        /// <summary>
        /// 무작동 모드. Shuffle 호출 시 리스트를 변경하지 않아 원래 순서를 유지한다.
        /// </summary>
        public FixedRandomSource() { /* ... */ }
        {
            _fixedDeck = null;
            _randomSequence = null;
            _sequenceIndex = 0;
        }

        /// <summary>
        /// 고정 덱 모드. Shuffle 호출 시 리스트를 지정된 카드 순서로 재배치한다.
        /// </summary>
        public FixedRandomSource(Card[] fixedDeck) { /* ... */ }
        {
            _fixedDeck = fixedDeck ?? throw new ArgumentNullException(nameof(fixedDeck));
            _randomSequence = null;
            _sequenceIndex = 0;
        }

        /// <summary>
        /// 고정 난수 모드. Fisher-Yates 셔플의 각 단계에서 사용할 난수 값을 순서대로 제공한다.
        /// </summary>
        public FixedRandomSource(int[] randomSequence) { /* ... */ }
        {
            _randomSequence = randomSequence ?? throw new ArgumentNullException(nameof(randomSequence));
            _fixedDeck = null;
            _sequenceIndex = 0;
        }

        /// <summary>
        /// 사전 정의된 난수 시퀀스에서 다음 값을 반환한다.
        /// 고정 덱 모드에서는 호출할 수 없다.
        /// </summary>
        public int Next(int minInclusive, int maxExclusive) { /* ... */ }
        {
            if (_randomSequence == null)
                throw new InvalidOperationException("고정 덱 모드에서는 Next를 호출할 수 없습니다.");

            if (_sequenceIndex >= _randomSequence.Length)
                throw new InvalidOperationException("난수 시퀀스가 모두 소진되었습니다.");

            return _randomSequence[_sequenceIndex++];
        }

        public void Shuffle<T>(IList<T> list) { /* ... */ }
        {
            if (_fixedDeck != null)
            {
                ShuffleWithFixedDeck(list);
            }
            else if (_randomSequence != null)
            {
                ShuffleWithFisherYates(list);
            }
            // 기본 생성자: 아무것도 하지 않는다. 원래 순서를 유지한다.
        }

        private void ShuffleWithFixedDeck<T>(IList<T> list) { /* ... */ }
        {
            if (typeof(T) != typeof(Card))
                throw new InvalidOperationException("고정 덱 모드는 Card 리스트에서만 사용할 수 있습니다.");

            if (list.Count != _fixedDeck.Length)
                throw new InvalidOperationException(
                    $"리스트 크기( { /* ... */ }

            var cardList = (IList<Card>)list;
            for (int i = 0; i < _fixedDeck.Length; i++)
            {
                cardList[i] = _fixedDeck[i];
            }
        }

        private void ShuffleWithFisherYates<T>(IList<T> list) { /* ... */ }
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Next(0, i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }
    }
}
