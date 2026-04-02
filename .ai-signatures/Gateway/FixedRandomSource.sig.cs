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
        public FixedRandomSource() { /* ... */ }
        public FixedRandomSource(Card[] fixedDeck) { /* ... */ }
        public FixedRandomSource(int[] randomSequence) { /* ... */ }
        public int Next(int minInclusive, int maxExclusive) { /* ... */ }
        public void Shuffle<T>(IList<T> list) { /* ... */ }
    }
}
