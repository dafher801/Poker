// IRandomSource.cs
// 셔플 기능을 추상화하는 게이트웨이 인터페이스.
// Deck.Shuffle() 에 주입하여 실제 랜덤(SystemRandomSource) 또는
// 테스트용 고정 순서(FixedRandomSource) 구현체로 교체할 수 있다.

using System.Collections.Generic;

namespace TexasHoldem.Gateway
{
    public interface IRandomSource
    {
        void Shuffle<T>(IList<T> list);
    }
}
