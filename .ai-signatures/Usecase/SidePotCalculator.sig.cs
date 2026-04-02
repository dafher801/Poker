// Source: Assets/Scripts/Usecase/SidePotCalculator.cs
// SidePotCalculator.cs
// 사이드 팟을 생성하고 쇼다운 결과에 따라 팟을 분배하는 유스케이스.
// 사용 방법:
//   var pots = SidePotCalculator.BuildPots(bets);
//   var awards = SidePotCalculator.DistributePots(pots, evaluations, dealerSeatIndex);
// BuildPots는 각 플레이어의 총 베팅 금액을 기준으로 올인 금액 경계마다 팟을 분리한다.
// DistributePots는 각 팟에서 최고 핸드를 가진 eligible 플레이어에게 금액을 분배한다.
// 동순위 시 균등 분할하며 나머지 칩은 딜러 좌측(시계 방향) 첫 번째 승자에게 배분한다.

using System.Collections.Generic;
using TexasHoldem.Entity;

namespace TexasHoldem.Usecase
{
    public static class SidePotCalculator
    {
        /// <summary>
        /// 각 플레이어의 베팅 정보를 기준으로 올인 금액 경계마다 팟을 분리하여 반환한다.
        /// 올인 플레이어가 없으면 단일 메인 팟만 생성된다.
        /// 폴드한 플레이어의 베팅액은 팟 총액에 포함되지만 EligiblePlayerIds에서는 제외된다.
        /// </summary>
        public static List<Pot> BuildPots(List<PlayerBetInfo> bets) { /* ... */ }
        {
            return PotCalculator.CalculatePots(bets);
        }

        /// <summary>
        /// 각 팟에 대해 eligible 플레이어 중 최고 핸드를 가진 플레이어에게 금액을 분배한다.
        /// 동점 시 균등 분할하며, 나머지 칩은 딜러 좌측(시계 방향) 첫 번째 승자에게 지급한다.
        /// </summary>
        /// <param name="pots">분배할 팟 목록</param>
        /// <param name="evaluations">좌석 인덱스 → HandEvaluation 매핑</param>
        /// <param name="dealerSeatIndex">딜러 좌석 인덱스 (나머지 칩 배분 기준)</param>
        /// <returns>각 수상 내역을 담은 PotAward 목록</returns>
        public static List<PotAward> DistributePots(
            List<Pot> pots,
            Dictionary<int, HandEvaluation> evaluations,
            int dealerSeatIndex)
        {
            var awards = new List<PotAward>();

            for (int potIndex = 0; potIndex < pots.Count; potIndex++)
            {
                var pot = pots[potIndex];
                if (pot.Amount == 0 || pot.EligiblePlayerIds.Count == 0)
                    continue;

                var winners = FindWinners(pot.EligiblePlayerIds, evaluations);
                if (winners.Count == 0)
                    continue;

                SortByDealerLeftOrder(winners, dealerSeatIndex);

                string potLabel = potIndex == 0 ? "Main Pot" : $"Side Pot {potIndex}";
                int share = pot.Amount / winners.Count;
                int remainder = pot.Amount % winners.Count;

                foreach (int seatIndex in winners)
                {
                    awards.Add(new PotAward(seatIndex, share, potLabel));
                }

                if (remainder > 0)
                {
                    awards.Add(new PotAward(winners[0], remainder, potLabel));
                }
            }

            return awards;
        }

        /// <summary>
        /// eligible 플레이어 중 최고 핸드를 가진 플레이어(들)의 좌석 인덱스를 반환한다.
        /// </summary>
        private static List<int> FindWinners(
            List<string> eligiblePlayerIds,
            Dictionary<int, HandEvaluation> evaluations)
        {
            HandEvaluation bestEval = null;
            var winners = new List<int>();

            foreach (string playerIdStr in eligiblePlayerIds)
            {
                if (!int.TryParse(playerIdStr, out int seatIndex))
                    continue;

                if (!evaluations.ContainsKey(seatIndex))
                    continue;

                var eval = evaluations[seatIndex];

                if (bestEval == null)
                {
                    bestEval = eval;
                    winners.Add(seatIndex);
                    continue;
                }

                int comparison = HandEvaluator.Compare(eval, bestEval);
                if (comparison > 0)
                {
                    bestEval = eval;
                    winners.Clear();
                    winners.Add(seatIndex);
                }
                else if (comparison == 0)
                {
                    winners.Add(seatIndex);
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
