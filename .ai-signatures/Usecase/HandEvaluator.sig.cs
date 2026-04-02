// Source: Assets/Scripts/Usecase/HandEvaluator.cs
// HandEvaluator.cs
// 5장(또는 6~7장)의 카드 조합에서 텍사스 홀덤 족보를 판정하는 정적 유틸리티 클래스.
// EvaluateFive : 정확히 5장을 받아 HandEvaluation을 반환하는 핵심 내부 메서드.
// Evaluate     : 5~7장을 받아 가능한 모든 5장 조합 중 최선의 핸드를 반환한다 (Task 1-2-5).
// Compare      : 두 HandEvaluation을 비교하여 양수/음수/0을 반환한다 (Task 1-2-6).

using System;
using System.Collections.Generic;
using System.Linq;
using TexasHoldem.Entity;

namespace TexasHoldem.Usecase
{
    public static class HandEvaluator
    {
        // ----------------------------------------------------------------
        // Task 1-2-4: 5장 핸드 분류 핵심 메서드
        // ----------------------------------------------------------------

        private static HandEvaluation EvaluateFive(List<Card> five) { /* ... */ }
        {
            // 랭크를 정수로 변환 후 내림차순 정렬
            var ranks = five.Select(c => /* ... */;

            // Flush 판정 — 5장 모두 같은 Suit
            bool isFlush = five.All(c => /* ... */;

            // Straight 판정
            bool isStraight = false;
            int straightHigh = 0;

            // 일반 스트레이트: 5장 랭크가 모두 다르고 최고-최저 차이가 4
            if (ranks.Distinct().Count() == 5 && ranks[0] - ranks[4] == 4)
            {
                isStraight = true;
                straightHigh = ranks[0];
            }
            // Ace-low 스트레이트 (wheel): A-2-3-4-5 → 하이카드 5
            else if (ranks[0] == 14 && ranks[1] == 5 && ranks[2] == 4 && ranks[3] == 3 && ranks[4] == 2)
            {
                isStraight = true;
                straightHigh = 5;
            }

            // ---- 높은 족보부터 순차 검사 ----

            // RoyalFlush — Flush + A-high Straight
            if (isFlush && isStraight && straightHigh == 14)
                return new HandEvaluation(HandRank.RoyalFlush, new List<int> { 14 });

            // StraightFlush
            if (isFlush && isStraight)
                return new HandEvaluation(HandRank.StraightFlush, new List<int> { straightHigh });

            // 그룹화: 카드 수 내림차순 → 랭크 내림차순
            var groups = ranks
                .GroupBy(r => /* ... */;
                .OrderByDescending(g => /* ... */;
                .ThenByDescending(g => /* ... */;
                .ToList();

            // FourOfAKind — [쿼드 랭크, 키커]
            if (groups[0].Count() == 4)
            {
                int quad   = groups[0].Key;
                int kicker = groups[1].Key;
                return new HandEvaluation(HandRank.FourOfAKind, new List<int> { quad, kicker });
            }

            // FullHouse — [트리플 랭크, 페어 랭크]
            if (groups[0].Count() == 3 && groups[1].Count() == 2)
            {
                int triple = groups[0].Key;
                int pair   = groups[1].Key;
                return new HandEvaluation(HandRank.FullHouse, new List<int> { triple, pair });
            }

            // Flush — [5장 랭크 내림차순]
            if (isFlush)
                return new HandEvaluation(HandRank.Flush, new List<int>(ranks));

            // Straight — [스트레이트 하이카드]
            if (isStraight)
                return new HandEvaluation(HandRank.Straight, new List<int> { straightHigh });

            // ThreeOfAKind — [트리플 랭크, 나머지 내림차순]
            if (groups[0].Count() == 3)
            {
                int triple  = groups[0].Key;
                var kickers = ranks.Where(r => /* ... */;
                var tbs     = new List<int> { triple };
                tbs.AddRange(kickers);
                return new HandEvaluation(HandRank.ThreeOfAKind, tbs);
            }

            // TwoPair — [높은 페어, 낮은 페어, 키커]
            if (groups[0].Count() == 2 && groups[1].Count() == 2)
            {
                // groups는 랭크 내림차순으로 정렬되어 있으므로 groups[0].Key >= groups[1].Key
                int highPair = groups[0].Key;
                int lowPair  = groups[1].Key;
                int kicker   = groups[2].Key;
                return new HandEvaluation(HandRank.TwoPair, new List<int> { highPair, lowPair, kicker });
            }

            // OnePair — [페어 랭크, 나머지 내림차순]
            if (groups[0].Count() == 2)
            {
                int pair    = groups[0].Key;
                var kickers = ranks.Where(r => /* ... */;
                var tbs     = new List<int> { pair };
                tbs.AddRange(kickers);
                return new HandEvaluation(HandRank.OnePair, tbs);
            }

            // HighCard — [5장 랭크 내림차순]
            return new HandEvaluation(HandRank.HighCard, new List<int>(ranks));
        }

        // ----------------------------------------------------------------
        // Task 1-2-5: 5~7장 중 최선 핸드 선택
        // ----------------------------------------------------------------

        /// <summary>
        /// 5~7장의 카드를 받아 가능한 모든 5장 조합 중 가장 높은 HandEvaluation을 반환한다.
        /// 5장 미만이거나 7장 초과이면 ArgumentException을 던진다.
        /// </summary>
        public static HandEvaluation Evaluate(List<Card> cards) { /* ... */ }
        {
            if (cards == null || cards.Count < 5 || cards.Count > 7)
                throw new ArgumentException("카드 수는 5~7장이어야 합니다.", nameof(cards));

            if (cards.Count == 5)
                return EvaluateFive(cards);

            // 6~7장: 모든 5장 조합 중 최선 선택
            var combinations = CombinationUtil.GetCombinations(cards, 5);
            HandEvaluation best = null;
            foreach (var combo in combinations)
            {
                var eval = EvaluateFive(combo);
                if (best == null || Compare(eval, best) > 0)
                    best = eval;
            }
            return best;
        }

        // ----------------------------------------------------------------
        // Task 1-2-6: 두 HandEvaluation 비교
        // ----------------------------------------------------------------

        /// <summary>
        /// a와 b를 비교한다.
        /// 반환 값: 양수 → a 승, 음수 → b 승, 0 → 무승부(스플릿 팟).
        /// </summary>
        public static int Compare(HandEvaluation a, HandEvaluation b) { /* ... */ }
        {
            // (1) 족보 등급 비교
            int rankDiff = (int)a.Rank - (int)b.Rank;
            if (rankDiff != 0) return rankDiff;

            // (2) TieBreakers 순차 비교
            for (int i = 0; i < a.TieBreakers.Count && i < b.TieBreakers.Count; i++)
            {
                int diff = a.TieBreakers[i] - b.TieBreakers[i];
                if (diff != 0) return diff;
            }

            // (3) 완전 동점
            return 0;
        }
    }
}
