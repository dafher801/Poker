// HandEvaluation.cs
// 핸드 평가 결과를 담는 불변(immutable) 값 객체.
// HandRank로 족보 등급을, TieBreakers로 동일 족보 간 키커·서브랭크 비교 정보를 제공한다.
// TieBreakers는 높은 값 우선의 내림차순 리스트이며, 외부에서 변경할 수 없다.
// 사용 예: new HandEvaluation(HandRank.OnePair, new List<int> { 13, 9, 7 })

using System.Collections.Generic;

namespace Entity
{
    public class HandEvaluation
    {
        public HandRank Rank { get; }
        public IReadOnlyList<int> TieBreakers { get; }

        public HandEvaluation(HandRank rank, List<int> tieBreakers)
        {
            Rank = rank;
            TieBreakers = new List<int>(tieBreakers).AsReadOnly();
        }
    }
}
