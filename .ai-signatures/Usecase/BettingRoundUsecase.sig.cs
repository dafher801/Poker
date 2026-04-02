// Source: Assets/Scripts/Usecase/BettingRoundUsecase.cs
// BettingRoundUsecase.cs
// 단일 베팅 라운드의 진행을 제어하는 Usecase.
// 사용 방법: var usecase = new BettingRoundUsecase(actionValidator, actionExecutor, turnOrderResolver, roundEndEvaluator, potManager);
// var result = await usecase.RunBettingRound(state, actionProvider, broadcaster);
// TurnOrderResolver로 첫 액션 플레이어 결정, 순환 루프, ActionValidator로 합법 액션 산출,
// ActionExecutor로 액션 적용, RoundEndEvaluator로 종료 조건 확인,
// PotManager로 팟 수집·사이드팟 계산을 순서대로 수행한다.
// BettingRoundResult로 라운드 완료 또는 폴드 승리를 반환한다.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TexasHoldem.Entity;
using TexasHoldem.Gateway;

namespace TexasHoldem.Usecase
{
    // 베팅 라운드의 결과를 나타내는 구조체.
    public struct BettingRoundResult
    {
        public BettingRoundResultType Type { get; }
        public int WinningSeatIndex { get; }

        public static BettingRoundResult RoundComplete() { /* ... */ }
        public static BettingRoundResult HandEndedByFold(int winningSeatIndex) { /* ... */ }
    }

    public enum BettingRoundResultType
    {
        RoundComplete,
        HandEndedByFold
    }

    public class BettingRoundUsecase
    {
        // 생성자: ActionValidator, ActionExecutor, TurnOrderResolver, RoundEndEvaluator, PotManager를 주입받는다.
        public BettingRoundUsecase(
            ActionValidator actionValidator,
            ActionExecutor actionExecutor,
            TurnOrderResolver turnOrderResolver,
            RoundEndEvaluator roundEndEvaluator,
            PotManager potManager) { /* ... */ }

        // 베팅 라운드를 비동기로 실행한다.
        // (1) TurnOrderResolver로 첫 액션 플레이어 결정
        // (2) 순환 루프로 각 플레이어 액션 수신·적용
        // (3) 종료 조건: Active 1명 이하(HandEndedByFold) 또는 모든 Active가 액션 완료 + CurrentBet 동일(RoundComplete)
        // (4) PotManager.CalculateSidePots 호출
        // (5) broadcaster 이벤트 호출 (OnBettingRoundStarted, OnPlayerActed, OnPotUpdated, OnBettingRoundEnded, OnHandEndedByFold)
        public async Task<BettingRoundResult> RunBettingRound(GameState state, IPlayerActionProvider actionProvider, IGameEventBroadcaster broadcaster) { /* ... */ }
    }
}
