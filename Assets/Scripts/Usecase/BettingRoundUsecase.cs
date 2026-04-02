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
    // RoundComplete: 정상적으로 라운드가 종료됨.
    // HandEndedByFold: 1명만 남아 즉시 핸드 종료.
    public struct BettingRoundResult
    {
        public BettingRoundResultType Type { get; }
        public int WinningSeatIndex { get; }

        private BettingRoundResult(BettingRoundResultType type, int winningSeatIndex)
        {
            Type = type;
            WinningSeatIndex = winningSeatIndex;
        }

        public static BettingRoundResult RoundComplete()
        {
            return new BettingRoundResult(BettingRoundResultType.RoundComplete, -1);
        }

        public static BettingRoundResult HandEndedByFold(int winningSeatIndex)
        {
            return new BettingRoundResult(BettingRoundResultType.HandEndedByFold, winningSeatIndex);
        }
    }

    public enum BettingRoundResultType
    {
        RoundComplete,
        HandEndedByFold
    }

    public class BettingRoundUsecase
    {
        private readonly ActionValidator _actionValidator;
        private readonly ActionExecutor _actionExecutor;
        private readonly TurnOrderResolver _turnOrderResolver;
        private readonly RoundEndEvaluator _roundEndEvaluator;
        private readonly PotManager _potManager;

        public BettingRoundUsecase(
            ActionValidator actionValidator,
            ActionExecutor actionExecutor,
            TurnOrderResolver turnOrderResolver,
            RoundEndEvaluator roundEndEvaluator,
            PotManager potManager)
        {
            _actionValidator = actionValidator;
            _actionExecutor = actionExecutor;
            _turnOrderResolver = turnOrderResolver;
            _roundEndEvaluator = roundEndEvaluator;
            _potManager = potManager;
        }

        // 베팅 라운드를 비동기로 실행한다.
        // (1) TurnOrderResolver로 초기 액션 순서 생성
        // (2) 순환 루프로 각 플레이어 액션 수신·적용
        // (3) 종료 조건: 폴드로 1명만 남음 또는 모든 Active가 액션 완료 + CurrentBet 동일
        // (4) PotManager.CalculateSidePots 호출
        // (5) BettingRoundResult 반환
        public async Task<BettingRoundResult> RunBettingRound(
            GameState state,
            IPlayerActionProvider actionProvider,
            IGameEventBroadcaster broadcaster)
        {
            broadcaster.OnBettingRoundStarted(state.Phase);

            int playerCount = state.Players.Count;

            // TurnOrderResolver로 초기 액션 순서 결정
            List<int> initialOrder = _turnOrderResolver.ResolveOrder(state.Players, state.DealerIndex, state.Phase);

            // 초기 순서의 첫 번째 플레이어부터 시작
            int currentIndex = initialOrder.Count > 0 ? initialOrder[0] : 0;

            // 각 플레이어가 이번 라운드에서 액션했는지 추적
            var hasActed = new bool[playerCount];

            while (true)
            {
                PlayerData player = state.Players[currentIndex];

                // Folded 또는 AllIn인 플레이어는 건너뛴다
                if (player.Status == PlayerStatus.Folded || player.Status == PlayerStatus.AllIn)
                {
                    hasActed[currentIndex] = true;

                    if (ShouldEndLoop(state, hasActed))
                        break;

                    currentIndex = (currentIndex + 1) % playerCount;
                    continue;
                }

                // Active가 아닌 플레이어(Waiting, Eliminated)도 건너뛴다
                if (player.Status != PlayerStatus.Active)
                {
                    hasActed[currentIndex] = true;

                    if (ShouldEndLoop(state, hasActed))
                        break;

                    currentIndex = (currentIndex + 1) % playerCount;
                    continue;
                }

                // ActionValidator로 합법 액션 산출
                LegalActionSet legalActions = _actionValidator.GetLegalActions(state, player.Id);

                // IPlayerActionProvider로 액션 수신
                PlayerAction action = await actionProvider.RequestActionAsync(
                    currentIndex,
                    legalActions.AvailableActions.AsReadOnly(),
                    legalActions.MinRaiseAmount,
                    legalActions.MaxRaiseAmount,
                    legalActions.CallAmount,
                    CancellationToken.None);

                // 레이즈 판정을 위해 액션 적용 전 최고 베팅액 기록
                int maxBetBefore = GetMaxBet(state);

                // ActionExecutor로 상태 반영
                _actionExecutor.Execute(state, action);

                broadcaster.OnPlayerActed(player.Id, action);

                hasActed[currentIndex] = true;

                // 폴드로 1명만 남았는지 확인
                if (_roundEndEvaluator.IsOnlyOnePlayerRemaining(state.Players, out int winningSeatIndex))
                {
                    // ActionExecutorが既にPotに加算済みなのでクリアしてからCalculateSidePotsで再構築
                    state.Pots.Clear();
                    _potManager.CalculateSidePots(state);

                    int totalPot = 0;
                    foreach (var pot in state.Pots)
                        totalPot += pot.Amount;

                    broadcaster.OnHandEndedByFold(winningSeatIndex, totalPot);
                    broadcaster.OnPotUpdated(state.Pots);
                    broadcaster.OnBettingRoundEnded(state.Phase);

                    return BettingRoundResult.HandEndedByFold(winningSeatIndex);
                }

                // Raise 또는 AllIn이 실질적 레이즈인 경우 다른 Active 플레이어 hasActed 리셋
                bool isRaise = action.Type == ActionType.Raise;
                bool isAllInRaise = action.Type == ActionType.AllIn && player.CurrentBet > maxBetBefore;

                if (isRaise || isAllInRaise)
                {
                    ResetHasActedExcept(hasActed, currentIndex, state.Players);
                }

                if (ShouldEndLoop(state, hasActed))
                    break;

                currentIndex = (currentIndex + 1) % playerCount;
            }

            // ActionExecutor가 이미 Pots에 금액을 추가했으므로 초기화 후 CurrentBet 기반으로 재구성
            state.Pots.Clear();
            _potManager.CalculateSidePots(state);

            broadcaster.OnPotUpdated(state.Pots);
            broadcaster.OnBettingRoundEnded(state.Phase);

            return BettingRoundResult.RoundComplete();
        }

        // 현재 라운드의 최고 베팅액을 반환한다.
        private int GetMaxBet(GameState state)
        {
            int maxBet = 0;
            foreach (var p in state.Players)
            {
                if ((p.Status == PlayerStatus.Active || p.Status == PlayerStatus.AllIn)
                    && p.CurrentBet > maxBet)
                {
                    maxBet = p.CurrentBet;
                }
            }
            return maxBet;
        }

        // 특정 인덱스를 제외한 Active 플레이어의 hasActed를 false로 리셋한다.
        private void ResetHasActedExcept(bool[] hasActed, int exceptIndex, List<PlayerData> players)
        {
            for (int i = 0; i < hasActed.Length; i++)
            {
                if (i == exceptIndex) continue;
                if (players[i].Status == PlayerStatus.Active)
                {
                    hasActed[i] = false;
                }
            }
        }

        // 베팅 라운드 루프 종료 여부를 판정한다.
        // RoundEndEvaluator를 활용하여 모든 Active 플레이어의 액션 완료 + 베팅액 동일 여부를 확인한다.
        private bool ShouldEndLoop(GameState state, bool[] hasActed)
        {
            int highestBet = GetMaxBet(state);
            return _roundEndEvaluator.IsBettingRoundComplete(state.Players, highestBet, hasActed);
        }
    }
}
