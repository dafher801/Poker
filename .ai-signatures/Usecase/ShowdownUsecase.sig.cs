// Source: Assets/Scripts/Usecase/ShowdownUsecase.cs
// ShowdownUsecase.cs
// 쇼다운 시 각 팟별 승자를 결정하는 유스케이스.
// 사용 방법:
//   var potResults = ShowdownUsecase.EvaluateShowdown(state, holeCards, pots);
// 각 Pot의 EligiblePlayerIds 중 활성 플레이어들의 핸드를 HandEvaluator로 평가하고,
// 최고 핸드를 가진 플레이어(들)에게 팟을 분배한다.
// 동점 시 균등 분배하며, 나머지 칩은 딜러 좌측 첫 번째 승자에게 지급한다.

using System;
using System.Collections.Generic;
using TexasHoldem.Entity;

namespace TexasHoldem.Usecase
{
    public static class ShowdownUsecase
    {
        /// <summary>
        /// 각 팟에 대해 자격이 있는 활성 플레이어들의 핸드를 평가하고 승자를 결정한다.
        /// 동점 시 팟을 균등 분배하며, 나누어떨어지지 않는 나머지는
        /// 딜러 좌측 첫 번째 승자에게 지급한다.
        /// </summary>
        public static List<PotResult> EvaluateShowdown(
            GameRoundState state,
            Dictionary<int, (Card, Card)> holeCards,
            List<Pot> pots)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (holeCards == null)
                throw new ArgumentNullException(nameof(holeCards));
            if (pots == null)
                throw new ArgumentNullException(nameof(pots));

            var activeSeatSet = new HashSet<int>(state.ActiveSeatIndices);
            var potResults = new List<PotResult>();

            foreach (var pot in pots)
            {
                if (pot.Amount == 0 || pot.EligiblePlayerIds.Count == 0)
                    continue;

                // 이 팟에 자격이 있고 활성 상태인 플레이어들의 핸드를 평가
                var evaluations = new Dictionary<int, HandEvaluation>();
                foreach (var playerIdStr in pot.EligiblePlayerIds)
                {
                    if (!int.TryParse(playerIdStr, out int seatIndex))
                        continue;

                    if (!activeSeatSet.Contains(seatIndex))
                        continue;

                    if (!holeCards.ContainsKey(seatIndex))
                        continue;

                    var (card1, card2) = holeCards[seatIndex];
                    var allCards = new List<Card>(state.CommunityCards);
                    allCards.Add(card1);
                    allCards.Add(card2);

                    evaluations[seatIndex] = HandEvaluator.Evaluate(allCards);
                }

                if (evaluations.Count == 0)
                    continue;

                // 최고 핸드를 가진 승자들 결정
                var winnerSeats = FindWinners(evaluations);

                // 딜러 좌측 순서로 승자 정렬 (나머지 칩 지급 우선순위)
                SortByDealerLeftOrder(winnerSeats, state.DealerSeatIndex);

                int awardPerWinner = pot.Amount / winnerSeats.Count;
                int remainder = pot.Amount % winnerSeats.Count;

                potResults.Add(new PotResult(
                    pot.Amount,
                    winnerSeats,
                    awardPerWinner,
                    remainder));
            }

            return potResults;
        }

        /// <summary>
        /// 평가 결과에서 최고 핸드를 가진 플레이어(들)의 좌석 인덱스 목록을 반환한다.
        /// </summary>
        private static List<int> FindWinners(Dictionary<int, HandEvaluation> evaluations) { /* ... */ }
        {
            HandEvaluation bestEval = null;
            var winners = new List<int>();

            foreach (var kvp in evaluations)
            {
                if (bestEval == null)
                {
                    bestEval = kvp.Value;
                    winners.Add(kvp.Key);
                    continue;
                }

                int comparison = HandEvaluator.Compare(kvp.Value, bestEval);
                if (comparison > 0)
                {
                    bestEval = kvp.Value;
                    winners.Clear();
                    winners.Add(kvp.Key);
                }
                else if (comparison == 0)
                {
                    winners.Add(kvp.Key);
                }
            }

            return winners;
        }

        /// <summary>
        /// 승자 목록을 딜러 좌측(시계 방향) 순서로 정렬한다.
        /// 딜러 다음 좌석부터 순회하여 가장 먼저 만나는 승자가 나머지 칩을 받는다.
        /// </summary>
        private static void SortByDealerLeftOrder(List<int> seats, int dealerSeatIndex) { /* ... */ }
        {
            seats.Sort((a, b) { /* ... */ }
            {
                int orderA = (a - dealerSeatIndex - 1 + 10) % 10;
                int orderB = (b - dealerSeatIndex - 1 + 10) % 10;
                return orderA.CompareTo(orderB);
            });
        }
    }
}
