// Source: Assets/Scripts/Usecase/GameRoundUsecase.cs
// GameRoundUsecase.cs
// 한 핸드(게임 라운드)의 전체 흐름을 순차 실행하는 메인 오케스트레이터.
// 사용 방법:
//   var usecase = new GameRoundUsecase();
//   await usecase.PlayRound(state, random, actionProvider, broadcaster, repository);
// Phase 1(초기화) → Phase 2(딜링) → Phase 3(베팅 라운드 반복) →
// Phase 4(쇼다운/조기 종료) → Phase 5(정리) 순서로 진행한다.
// 내부에서 DealerRotation, BettingRoundUsecase, WinnerResolver, HandEvaluator를 조합하여 사용한다.

using System.Collections.Generic;
using System.Threading.Tasks;
using TexasHoldem.Entity;
using TexasHoldem.Gateway;

namespace TexasHoldem.Usecase
{
    public class GameRoundUsecase
    {
        // 생성자: ActionValidator, PotManager, BettingRoundUsecase, WinnerResolver를 내부에서 생성한다.
        public GameRoundUsecase() { /* ... */ }

        // 한 핸드의 전체 흐름을 비동기로 실행한다.
        // Phase 1: 딜러 이동, 블라인드 포스팅
        // Phase 2: 덱 셔플, 홀카드 딜링
        // Phase 3: PreFlop/Flop/Turn/River 베팅 라운드
        // Phase 4: 쇼다운 또는 조기 종료, 칩 분배
        // Phase 5: 상태 초기화, repository에 저장
        public async Task PlayRound(
            GameState state,
            IRandomSource random,
            IPlayerActionProvider actionProvider,
            IGameEventBroadcaster broadcaster,
            IGameStateRepository repository) { /* ... */ }
    }
}
