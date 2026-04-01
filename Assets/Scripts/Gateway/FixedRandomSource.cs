// FixedRandomSource.cs
// IRandomSource의 테스트용 구현체.
// Shuffle 호출 시 리스트를 변경하지 않아 원래 순서를 유지한다.
// 테스트에서 카드 순서를 예측 가능하게 만드는 데 사용한다.

using System.Collections.Generic;

namespace TexasHoldem.Gateway
{
    public class FixedRandomSource : IRandomSource
    {
        public void Shuffle<T>(IList<T> list)
        {
            // 의도적으로 아무것도 하지 않는다. 순서를 그대로 유지한다.
        }
    }
}
