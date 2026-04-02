// Source: Assets/Scripts/Usecase/PotManager.cs
// PotManager.cs
// 게임 전체 수명 동안 팟 상태를 관리하는 클래스.
// 사용 방법:
//   var pm = new PotManager();
//   pm.CollectBets(roundBets);      — PotCalculator로 팟을 계산하고 기존 팟과 병합
//   pm.GetPots();                   — 현재 팟 목록 복사본 반환
//   pm.GetTotalPot();               — 전체 팟 금액 합계 반환
//   pm.RemovePlayer(playerId);      — 모든 팟에서 특정 플레이어 제거
//   pm.Reset();                     — 팟 초기화
// 기존 GameState 기반 메서드(CollectBets(GameState), CalculateSidePots, DistributePots)도 유지.

using System.Collections.Generic;
using System.Linq;
using TexasHoldem.Entity;

namespace TexasHoldem.Usecase
{
    public class PotManager
    {
        private readonly List<Pot> _currentPots = new List<Pot>();
        /// <summary>
        /// PotCalculator를 사용하여 이번 라운드의 팟을 계산하고, 기존 CurrentPots와 병합한다.
        /// 병합 규칙: EligiblePlayerIds 집합이 동일한 팟끼리 금액을 합산하고,
        /// 동일 집합이 없으면 새 팟으로 추가한다.
        /// </summary>
        public void CollectBets(List<PlayerBetInfo> roundBets) { /* ... */ }
        {
            var roundPots = PotCalculator.CalculatePots(roundBets);

            foreach (var roundPot in roundPots)
            {
                bool merged = false;
                foreach (var existingPot in _currentPots)
                {
                    if (AreEligibleSetsEqual(existingPot.EligiblePlayerIds, roundPot.EligiblePlayerIds))
                    {
                        existingPot.AddAmount(roundPot.Amount);
                        merged = true;
                        break;
                    }
                }

                if (!merged)
                {
                    _currentPots.Add(new Pot(roundPot.Amount, new List<string>(roundPot.EligiblePlayerIds)));
                }
            }
        }

        /// <summary>
        /// 현재 팟 목록의 복사본을 반환한다.
        /// </summary>
        public List<Pot> GetPots() { /* ... */ }
        {
            var copy = new List<Pot>();
            foreach (var pot in _currentPots)
            {
                copy.Add(new Pot(pot.Amount, new List<string>(pot.EligiblePlayerIds)));
            }
            return copy;
        }

        /// <summary>
        /// 모든 팟 금액의 합계를 반환한다.
        /// </summary>
        public int GetTotalPot() { /* ... */ }
        {
            int total = 0;
            foreach (var pot in _currentPots)
            {
                total += pot.Amount;
            }
            return total;
        }

        /// <summary>
        /// 새 핸드 시작 시 팟을 초기화한다.
        /// </summary>
        public void Reset() { /* ... */ }
        {
            _currentPots.Clear();
        }

        /// <summary>
        /// 특정 플레이어를 모든 팟의 EligiblePlayerIds에서 제거한다. (폴드 시 호출용)
        /// </summary>
        public void RemovePlayer(string playerId) { /* ... */ }
        {
            foreach (var pot in _currentPots)
            {
                pot.RemovePlayer(playerId);
            }
        }

        private bool AreEligibleSetsEqual(List<string> a, List<string> b) { /* ... */ }
        {
            if (a.Count != b.Count)
                return false;

            var setA = new HashSet<string>(a);
            foreach (var id in b)
            {
                if (!setA.Contains(id))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 모든 플레이어의 CurrentBet을 합산하여 state.Pots에 반영한 뒤
        /// 각 플레이어의 CurrentBet을 0으로 초기화한다.
        /// </summary>
        public void CollectBets(GameState state) { /* ... */ }
        {
            int totalBets = 0;
            foreach (var player in state.Players)
            {
                totalBets += player.CurrentBet;
                player.CurrentBet = 0;
            }

            if (totalBets == 0)
                return;

            if (state.Pots.Count == 0)
            {
                state.Pots.Add(new Pot(totalBets, BuildEligiblePlayerIds(state)));
            }
            else
            {
                state.Pots[0].AddAmount(totalBets);
                // 자격 목록도 갱신: Folded/Eliminated가 아닌 플레이어
                var eligible = BuildEligiblePlayerIds(state);
                state.Pots[0].EligiblePlayerIds.Clear();
                state.Pots[0].EligiblePlayerIds.AddRange(eligible);
            }
        }

        /// <summary>
        /// 올인한 플레이어가 있는 경우 사이드 팟을 생성한다.
        /// 이 메서드는 CollectBets 전에, 각 플레이어의 CurrentBet이 아직 남아 있는 상태에서 호출해야 한다.
        /// 실제로는 CollectBets 대신 이 메서드를 사용하여 베팅액 수집과 사이드 팟 분리를 동시에 처리한다.
        /// </summary>
        public void CalculateSidePots(GameState state) { /* ... */ }
        {
            // 베팅에 참여한 플레이어(CurrentBet > 0)만 추출
            var bettors = new List<PlayerData>();
            foreach (var player in state.Players)
            {
                if (player.CurrentBet > 0)
                    bettors.Add(player);
            }

            if (bettors.Count == 0)
                return;

            // 올인 플레이어가 없으면 단순 CollectBets와 동일
            bool hasAllIn = false;
            foreach (var player in bettors)
            {
                if (player.Status == PlayerStatus.AllIn)
                {
                    hasAllIn = true;
                    break;
                }
            }

            if (!hasAllIn)
            {
                // 단순히 CollectBets 처리
                CollectBets(state);
                return;
            }

            // 베팅액 기준 오름차순 정렬
            bettors.Sort((a, b) => /* ... */;

            // 고유한 베팅 레벨 추출
            var levels = new List<int>();
            foreach (var bettor in bettors)
            {
                if (levels.Count == 0 || levels[levels.Count - 1] != bettor.CurrentBet)
                    levels.Add(bettor.CurrentBet);
            }

            // 기존 팟의 금액을 보존하여 새 팟에 추가
            int existingPotAmount = 0;
            if (state.Pots.Count > 0)
            {
                existingPotAmount = state.Pots[0].Amount;
            }

            var newPots = new List<Pot>();
            int previousLevel = 0;

            for (int i = 0; i < levels.Count; i++)
            {
                int currentLevel = levels[i];
                int contribution = currentLevel - previousLevel;

                // 이 레벨 이상 베팅한 플레이어 수
                int contributors = 0;
                var eligible = new List<string>();
                foreach (var bettor in bettors)
                {
                    if (bettor.CurrentBet >= currentLevel)
                    {
                        contributors++;
                        // Folded 플레이어는 기여는 하지만 수령 자격 없음
                        if (bettor.Status != PlayerStatus.Folded && bettor.Status != PlayerStatus.Eliminated)
                            eligible.Add(bettor.Id);
                    }
                }

                int potAmount = contribution * contributors;

                // 첫 번째 팟에 기존 팟 금액 추가
                if (i == 0)
                    potAmount += existingPotAmount;

                if (potAmount > 0)
                    newPots.Add(new Pot(potAmount, eligible));

                previousLevel = currentLevel;
            }

            // CurrentBet 초기화
            foreach (var player in state.Players)
                player.CurrentBet = 0;

            // state.Pots 교체
            state.Pots.Clear();
            state.Pots.AddRange(newPots);
        }

        /// <summary>
        /// 각 Pot에 대해 EligiblePlayerIds 중 HandEvaluation이 가장 높은 플레이어에게
        /// 해당 Pot.Amount를 지급한다. 동점 시 균등 분배하며 나머지 칩은
        /// 딜러에 가장 가까운(포지션 인덱스가 낮은) 플레이어에게 지급한다.
        /// </summary>
        public List<(string PlayerId, int Amount)> DistributePots(
            List<Pot> pots,
            Dictionary<string, HandEvaluation> evaluations)
        {
            var payoutMap = new Dictionary<string, int>();

            foreach (var pot in pots)
            {
                if (pot.Amount == 0 || pot.EligiblePlayerIds.Count == 0)
                    continue;

                // 이 팟에서 가장 높은 핸드를 가진 플레이어들을 찾기
                var winners = FindWinners(pot.EligiblePlayerIds, evaluations);

                if (winners.Count == 0)
                    continue;

                int share = pot.Amount / winners.Count;
                int remainder = pot.Amount % winners.Count;

                foreach (var winnerId in winners)
                {
                    if (!payoutMap.ContainsKey(winnerId))
                        payoutMap[winnerId] = 0;
                    payoutMap[winnerId] += share;
                }

                // 나머지 칩은 포지션 인덱스가 가장 낮은(딜러에 가장 가까운) 승자에게 지급
                if (remainder > 0)
                {
                    // winners는 이미 정렬되어 있지 않으므로 첫 번째가 가장 가까운 플레이어
                    // FindWinners에서 원래 순서 유지하므로 첫 번째가 포지션이 가장 낮은 플레이어
                    payoutMap[winners[0]] += remainder;
                }
            }

            var result = new List<(string PlayerId, int Amount)>();
            foreach (var kvp in payoutMap)
            {
                result.Add((kvp.Key, kvp.Value));
            }

            return result;
        }

        private List<string> FindWinners(
            List<string> eligiblePlayerIds,
            Dictionary<string, HandEvaluation> evaluations)
        {
            HandEvaluation bestEval = null;
            var winners = new List<string>();

            foreach (var playerId in eligiblePlayerIds)
            {
                if (!evaluations.ContainsKey(playerId))
                    continue;

                var eval = evaluations[playerId];

                if (bestEval == null)
                {
                    bestEval = eval;
                    winners.Add(playerId);
                    continue;
                }

                int comparison = CompareHands(eval, bestEval);
                if (comparison > 0)
                {
                    // 새로운 최강 핸드
                    bestEval = eval;
                    winners.Clear();
                    winners.Add(playerId);
                }
                else if (comparison == 0)
                {
                    // 동점
                    winners.Add(playerId);
                }
            }

            return winners;
        }

        /// <summary>
        /// 두 핸드를 비교한다. a가 더 크면 양수, b가 더 크면 음수, 동점이면 0.
        /// </summary>
        private int CompareHands(HandEvaluation a, HandEvaluation b) { /* ... */ }
        {
            if ((int)a.Rank != (int)b.Rank)
                return ((int)a.Rank).CompareTo((int)b.Rank);

            // Rank가 같으면 TieBreakers 비교
            int count = a.TieBreakers.Count;
            if (b.TieBreakers.Count < count)
                count = b.TieBreakers.Count;

            for (int i = 0; i < count; i++)
            {
                if (a.TieBreakers[i] != b.TieBreakers[i])
                    return a.TieBreakers[i].CompareTo(b.TieBreakers[i]);
            }

            return 0;
        }

        private List<string> BuildEligiblePlayerIds(GameState state) { /* ... */ }
        {
            var eligible = new List<string>();
            foreach (var player in state.Players)
            {
                if (player.Status != PlayerStatus.Folded && player.Status != PlayerStatus.Eliminated)
                    eligible.Add(player.Id);
            }
            return eligible;
        }
    }
}
