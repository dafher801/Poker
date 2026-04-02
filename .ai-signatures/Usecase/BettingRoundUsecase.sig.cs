// Source: Assets/Scripts/Usecase/BettingRoundUsecase.cs
// BettingRoundUsecase.cs
// 단일 베팅 라운드의 진행을 제어하는 Usecase.
// 사용 방법: var usecase = new BettingRoundUsecase(actionValidator, potManager);
// await usecase.RunBettingRound(state, actionProvider, broadcaster);
// 첫 액션 플레이어 결정, 순환 루프, 액션 검증·적용, 종료 조건 확인,
// 팟 수집·사이드팟 계산을 순서대로 수행한다.

using System.Threading.Tasks;
using TexasHoldem.Entity;
using TexasHoldem.Gateway;

namespace TexasHoldem.Usecase
{
    public class BettingRoundUsecase
    {
        // 생성자: ActionValidator와 PotManager를 주입받는다.
        public BettingRoundUsecase(ActionValidator actionValidator, PotManager potManager) { /* ... */ }

        // 베팅 라운드를 비동기로 실행한다.
        // (1) 첫 액션 플레이어 결정 (PreFlop: UTG, PostFlop: 딜러 다음)
        // (2) 순환 루프로 각 플레이어 액션 수신·적용
        // (3) 종료 조건: Active 1명 이하 또는 모든 Active가 액션 완료 + CurrentBet 동일
        // (4) PotManager.CollectBets, CalculateSidePots 호출
        // (5) broadcaster 이벤트 호출 (OnBettingRoundStarted, OnPlayerActed, OnPotUpdated, OnBettingRoundEnded)
        public async Task RunBettingRound(GameState state, IPlayerActionProvider actionProvider, IGameEventBroadcaster broadcaster) { /* ... */ }
    }
}
