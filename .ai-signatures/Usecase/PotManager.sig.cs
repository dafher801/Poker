// Source: Assets/Scripts/Usecase/PotManager.cs
// PotManager.cs
// 베팅 라운드 종료 후 베팅액을 수집하고, 사이드 팟을 계산하며, 승자에게 팟을 분배한다.
// 사용 방법:
//   var pm = new PotManager();
//   pm.CollectBets(state);          — 모든 플레이어의 CurrentBet을 팟에 합산하고 0으로 초기화
//   pm.CalculateSidePots(state);    — 올인 플레이어가 있을 경우 사이드 팟을 분리
//   var payouts = pm.DistributePots(pots, evaluations); — 각 팟의 승자에게 금액 분배

using System.Collections.Generic;
using TexasHoldem.Entity;

namespace TexasHoldem.Usecase
{
    public class PotManager
    {
        // 모든 플레이어의 CurrentBet을 합산하여 state.Pots에 반영한 뒤 각 플레이어의 CurrentBet을 0으로 초기화한다.
        public void CollectBets(GameState state) { /* ... */ }

        // 올인한 플레이어가 있는 경우 사이드 팟을 생성한다.
        // 올인 없으면 단일 메인 팟 유지. CurrentBet 초기화 포함.
        public void CalculateSidePots(GameState state) { /* ... */ }

        // 각 Pot에 대해 EligiblePlayerIds 중 HandEvaluation이 가장 높은 플레이어에게 해당 Pot.Amount를 지급.
        // 동점 시 균등 분배, 나머지 칩은 포지션 인덱스가 낮은 플레이어에게 지급.
        public List<(string PlayerId, int Amount)> DistributePots(
            List<Pot> pots,
            Dictionary<string, HandEvaluation> evaluations) { /* ... */ }
    }
}
