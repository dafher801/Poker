// WinnerResolver.cs
// 쇼다운 또는 조기 종료 시 최종 승자와 배분 금액을 결정하는 클래스.
// 사용 방법:
//   var resolver = new WinnerResolver();
//   List<(string PlayerId, int Amount)> payouts = resolver.Resolve(state);
// GameState를 읽기 전용으로 접근하며, 상태를 변경하지 않는다.
// 조기 종료(Active 1명) 시 전체 팟을 해당 플레이어에게 수여한다.
// 쇼다운 시 HandEvaluator로 각 플레이어의 핸드를 평가하고 PotManager.DistributePots로 분배한다.

using System.Collections.Generic;
using System.Linq;
using TexasHoldem.Entity;

namespace TexasHoldem.Usecase
{
    public class WinnerResolver
    {
        /// <summary>
        /// 현재 GameState를 기반으로 각 팟의 승자와 배분 금액을 결정하여 반환한다.
        /// Active/AllIn 플레이어가 1명이면 조기 종료, 2명 이상이면 쇼다운 판정을 수행한다.
        /// </summary>
        public List<(string PlayerId, int Amount)> Resolve(GameState state)
        {
            var activePlayers = state.Players
                .Where(p => p.Status == PlayerStatus.Active || p.Status == PlayerStatus.AllIn)
                .ToList();

            // 조기 종료: Active/AllIn 플레이어가 1명뿐이면 전체 팟을 수여
            if (activePlayers.Count == 1)
            {
                int totalPot = state.Pots.Sum(p => p.Amount);
                return new List<(string PlayerId, int Amount)>
                {
                    (activePlayers[0].Id, totalPot)
                };
            }

            // 쇼다운: 각 Active/AllIn 플레이어의 핸드를 평가
            var evaluations = new Dictionary<string, HandEvaluation>();
            foreach (var player in activePlayers)
            {
                var allCards = new List<Card>(state.CommunityCards);
                allCards.AddRange(player.HoleCards);
                evaluations[player.Id] = HandEvaluator.Evaluate(allCards);
            }

            // PotManager를 사용하여 각 팟별 승자에게 분배
            var potManager = new PotManager();
            return potManager.DistributePots(state.Pots, evaluations);
        }
    }
}
