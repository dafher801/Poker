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
        private readonly List<Pot> _currentPots;

        // PotCalculator를 사용하여 이번 라운드의 팟을 계산하고, 기존 CurrentPots와 병합한다.
        // EligiblePlayerIds 집합이 동일한 팟끼리 금액을 합산, 동일 집합이 없으면 새 팟 추가.
        public void CollectBets(List<PlayerBetInfo> roundBets) { /* ... */ }

        // 현재 팟 목록의 복사본을 반환한다.
        public List<Pot> GetPots() { /* ... */ }

        // 모든 팟 금액의 합계를 반환한다.
        public int GetTotalPot() { /* ... */ }

        // 새 핸드 시작 시 팟을 초기화한다.
        public void Reset() { /* ... */ }

        // 특정 플레이어를 모든 팟의 EligiblePlayerIds에서 제거한다. (폴드 시 호출용)
        public void RemovePlayer(string playerId) { /* ... */ }

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
