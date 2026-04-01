// CombinationUtil.cs
// 카드 리스트에서 n장을 선택하는 모든 조합을 생성하는 정적 유틸리티 클래스.
// 사용법: CombinationUtil.GetCombinations(cards, 5) — 7장에서 5장 조합 C(7,5)=21가지를 반환한다.
// 입력 카드 수가 choose보다 작으면 빈 리스트를 반환한다.

using System.Collections.Generic;
using TexasHoldem.Entity;

namespace TexasHoldem.Usecase
{
    public static class CombinationUtil
    {
        public static List<List<Card>> GetCombinations(List<Card> cards, int choose)
        {
            var result = new List<List<Card>>();
            if (cards == null || choose <= 0 || cards.Count < choose)
                return result;

            var current = new List<Card>(choose);
            Combine(cards, choose, 0, current, result);
            return result;
        }

        private static void Combine(List<Card> cards, int choose, int start, List<Card> current, List<List<Card>> result)
        {
            if (current.Count == choose)
            {
                result.Add(new List<Card>(current));
                return;
            }

            int remaining = choose - current.Count;
            for (int i = start; i <= cards.Count - remaining; i++)
            {
                current.Add(cards[i]);
                Combine(cards, choose, i + 1, current, result);
                current.RemoveAt(current.Count - 1);
            }
        }
    }
}
