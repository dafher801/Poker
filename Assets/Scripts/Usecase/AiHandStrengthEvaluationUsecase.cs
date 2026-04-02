// AiHandStrengthEvaluationUsecase.cs
// AI 의사결정용 핸드 강도 평가 유즈케이스.
// 홀카드와 커뮤니티 카드를 받아 0(최약)~1(최강)의 정규화된 핸드 강도를 반환한다.
// 프리플랍: 홀카드 조합의 사전 확률 테이블 기반 강도 산출.
// 플랍 이후: HandEvaluator로 메이드 핸드 랭크를 정규화하고, 드로우 가능성 보정값을 가산.
// 외부 의존: HandEvaluator, Card, HandRank, HandEvaluation Entity만 사용.

using System;
using System.Collections.Generic;
using System.Linq;
using TexasHoldem.Entity;

namespace TexasHoldem.Usecase
{
    public class AiHandStrengthEvaluationUsecase
    {
        private const float MaxHandRankValue = 9f; // RoyalFlush
        private const float FlushDrawBonus = 0.08f;
        private const float OpenEndedStraightDrawBonus = 0.06f;
        private const float GutshotStraightDrawBonus = 0.03f;

        /// <summary>
        /// 핸드 강도를 0~1 사이 값으로 평가한다.
        /// </summary>
        /// <param name="holeCards">플레이어의 홀카드 (2장)</param>
        /// <param name="communityCards">커뮤니티 카드 (0~5장)</param>
        /// <returns>0(최약)~1(최강) 정규화된 핸드 강도</returns>
        public float EvaluateStrength(Card[] holeCards, Card[] communityCards)
        {
            if (holeCards == null || holeCards.Length != 2)
                throw new ArgumentException("홀카드는 정확히 2장이어야 합니다.", nameof(holeCards));
            if (communityCards == null)
                communityCards = Array.Empty<Card>();

            // 프리플랍: 커뮤니티 카드 0장
            if (communityCards.Length == 0)
                return EvaluatePreflop(holeCards);

            // 플랍 이후: 메이드 핸드 + 드로우 보정
            return EvaluatePostflop(holeCards, communityCards);
        }

        private float EvaluatePreflop(Card[] holeCards)
        {
            int rank1 = (int)holeCards[0].Rank;
            int rank2 = (int)holeCards[1].Rank;
            bool suited = holeCards[0].Suit == holeCards[1].Suit;
            bool paired = rank1 == rank2;

            int high = Math.Max(rank1, rank2);
            int low = Math.Min(rank1, rank2);

            float strength;

            if (paired)
            {
                // 포켓 페어: AA=1.0, 22≈0.45
                // (high - 2) / 12 → 0~1, 기본 0.45~1.0 범위로 매핑
                strength = 0.45f + ((high - 2) / 12f) * 0.55f;
            }
            else
            {
                // 언페어드: 하이카드 기여 + 연결성(갭) + 수티드 보너스
                float highContrib = (high - 2) / 12f; // 0~1
                float lowContrib = (low - 2) / 12f;   // 0~1
                int gap = high - low;

                // 기본 강도: 하이카드 60%, 로우카드 25% 가중
                strength = highContrib * 0.35f + lowContrib * 0.15f;

                // 연결성 보너스: 갭이 작을수록 스트레이트 가능성
                if (gap == 1) strength += 0.06f;
                else if (gap == 2) strength += 0.03f;
                else if (gap == 3) strength += 0.01f;

                // 수티드 보너스
                if (suited) strength += 0.05f;

                // 에이스 보너스
                if (high == 14) strength += 0.08f;
            }

            return Math.Min(1f, Math.Max(0f, strength));
        }

        private float EvaluatePostflop(Card[] holeCards, Card[] communityCards)
        {
            var allCards = new List<Card>(holeCards);
            allCards.AddRange(communityCards);

            // 메이드 핸드 평가
            var evaluation = HandEvaluator.Evaluate(allCards);
            float rankStrength = (int)evaluation.Rank / MaxHandRankValue;

            // 동일 족보 내 세분화: TieBreakers 기반 보정 (최대 0.1 범위)
            float tieBreakBonus = 0f;
            if (evaluation.TieBreakers.Count > 0)
            {
                // 첫 번째 타이브레이커 (가장 중요한 키커/랭크)를 정규화
                tieBreakBonus = (evaluation.TieBreakers[0] - 2) / 12f * 0.08f;
            }

            float baseStrength = rankStrength + tieBreakBonus;

            // 드로우 보정 (플랍/턴에서만, 리버에서는 불필요)
            float drawBonus = 0f;
            if (communityCards.Length < 5)
            {
                drawBonus = CalculateDrawBonus(holeCards, communityCards);
            }

            return Math.Min(1f, Math.Max(0f, baseStrength + drawBonus));
        }

        private float CalculateDrawBonus(Card[] holeCards, Card[] communityCards)
        {
            var allCards = new List<Card>(holeCards);
            allCards.AddRange(communityCards);

            float bonus = 0f;

            // 플러시 드로우 감지: 같은 수트 4장
            if (HasFlushDraw(allCards))
                bonus += FlushDrawBonus;

            // 스트레이트 드로우 감지
            var straightDrawType = DetectStraightDraw(allCards);
            if (straightDrawType == StraightDrawType.OpenEnded)
                bonus += OpenEndedStraightDrawBonus;
            else if (straightDrawType == StraightDrawType.Gutshot)
                bonus += GutshotStraightDrawBonus;

            return bonus;
        }

        private bool HasFlushDraw(List<Card> cards)
        {
            // 수트별 카드 수 카운트
            var suitCounts = new int[4];
            foreach (var card in cards)
            {
                suitCounts[(int)card.Suit]++;
            }

            // 4장 수트가 있으면 플러시 드로우 (5장이면 이미 플러시)
            foreach (var count in suitCounts)
            {
                if (count == 4) return true;
            }
            return false;
        }

        private StraightDrawType DetectStraightDraw(List<Card> cards)
        {
            // 고유 랭크 추출 및 정렬
            var uniqueRanks = cards
                .Select(c => (int)c.Rank)
                .Distinct()
                .OrderBy(r => r)
                .ToList();

            // 에이스는 1로도 사용 가능
            if (uniqueRanks.Contains(14) && !uniqueRanks.Contains(1))
                uniqueRanks.Insert(0, 1);

            // 연속 5장 윈도우에서 몇 장이 존재하는지 검사
            bool hasOpenEnded = false;
            bool hasGutshot = false;

            for (int windowStart = 1; windowStart <= 10; windowStart++)
            {
                int windowEnd = windowStart + 4;
                int count = 0;
                for (int r = windowStart; r <= windowEnd; r++)
                {
                    if (uniqueRanks.Contains(r)) count++;
                }

                if (count == 4)
                {
                    // 4장 존재: 빈 자리가 양 끝이면 오픈엔디드, 내부면 거셧
                    bool missingLow = !uniqueRanks.Contains(windowStart);
                    bool missingHigh = !uniqueRanks.Contains(windowEnd);

                    if (missingLow || missingHigh)
                        hasOpenEnded = true;
                    else
                        hasGutshot = true;
                }
            }

            if (hasOpenEnded) return StraightDrawType.OpenEnded;
            if (hasGutshot) return StraightDrawType.Gutshot;
            return StraightDrawType.None;
        }

        private enum StraightDrawType
        {
            None,
            Gutshot,
            OpenEnded
        }
    }
}
