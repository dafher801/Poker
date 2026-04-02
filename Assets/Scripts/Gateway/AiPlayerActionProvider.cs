// AiPlayerActionProvider.cs
// IPlayerActionProvider를 구현하는 AI 플레이어 Gateway.
// 생성자에서 AI 성향 프로필과 각 Usecase를 주입받아, 현재 게임 상태를 기반으로
// 핸드 강도·팟 오즈를 산출하고 AiActionDecisionUsecase로 Fold/Check/Call/Raise를 결정한다.
// 자연스러운 플레이 속도를 위해 0.5~2.0초의 인위적 딜레이를 포함한다.
// MonoBehaviour를 상속하지 않는다.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TexasHoldem.Entity;
using TexasHoldem.Usecase;
using Poker.Usecase;

namespace TexasHoldem.Gateway
{
    public class AiPlayerActionProvider : IPlayerActionProvider
    {
        private readonly AiPersonalityProfile _personality;
        private readonly AiHandStrengthEvaluationUsecase _handStrengthUsecase;
        private readonly PotOddsCalculationUsecase _potOddsUsecase;
        private readonly AiActionDecisionUsecase _actionDecisionUsecase;
        private readonly Func<GameState> _gameStateProvider;
        private readonly Random _random;

        /// <summary>
        /// AI 플레이어 액션 프로바이더를 생성한다.
        /// </summary>
        /// <param name="personality">AI 성향 프로필</param>
        /// <param name="handStrengthUsecase">핸드 강도 평가 유즈케이스</param>
        /// <param name="potOddsUsecase">팟 오즈 계산 유즈케이스</param>
        /// <param name="actionDecisionUsecase">액션 결정 유즈케이스</param>
        /// <param name="gameStateProvider">현재 게임 상태를 반환하는 팩토리. 호출 시점의 최신 상태를 반환해야 한다.</param>
        /// <param name="random">딜레이 랜덤 생성기. null이면 새 인스턴스를 생성한다.</param>
        public AiPlayerActionProvider(
            AiPersonalityProfile personality,
            AiHandStrengthEvaluationUsecase handStrengthUsecase,
            PotOddsCalculationUsecase potOddsUsecase,
            AiActionDecisionUsecase actionDecisionUsecase,
            Func<GameState> gameStateProvider,
            Random random = null)
        {
            _personality = personality ?? throw new ArgumentNullException(nameof(personality));
            _handStrengthUsecase = handStrengthUsecase ?? throw new ArgumentNullException(nameof(handStrengthUsecase));
            _potOddsUsecase = potOddsUsecase ?? throw new ArgumentNullException(nameof(potOddsUsecase));
            _actionDecisionUsecase = actionDecisionUsecase ?? throw new ArgumentNullException(nameof(actionDecisionUsecase));
            _gameStateProvider = gameStateProvider ?? throw new ArgumentNullException(nameof(gameStateProvider));
            _random = random ?? new Random();
        }

        public async Task<PlayerAction> RequestActionAsync(
            int seatIndex,
            IReadOnlyList<ActionType> legalActions,
            int minRaise,
            int maxRaise,
            int callAmount,
            CancellationToken ct)
        {
            // 인위적 딜레이 (0.5~2.0초)
            int delayMs = 500 + _random.Next(1501);
            await Task.Delay(delayMs, ct);

            // 현재 게임 상태 읽기
            var gameState = _gameStateProvider();
            var player = gameState.Players.FirstOrDefault(p => p.SeatIndex == seatIndex);
            if (player == null)
                throw new InvalidOperationException($"좌석 {seatIndex}에 해당하는 플레이어를 찾을 수 없습니다.");

            // (1) 핸드 강도 산출
            var holeCards = player.HoleCards.ToArray();
            var communityCards = gameState.CommunityCards.ToArray();
            float handStrength = _handStrengthUsecase.EvaluateStrength(holeCards, communityCards);

            // (2) 팟 오즈 산출
            float potOdds = _potOddsUsecase.CalculatePotOdds(gameState.Pots.Sum(p => p.Amount), callAmount);

            // 포지션 계산: 딜러 이후 순서에서 해당 좌석의 위치 (0=얼리, max=레이트)
            int positionIndex = CalculatePositionIndex(seatIndex, gameState);
            int activePlayers = gameState.Players.Count(p => p.Status == PlayerStatus.Active);

            // (3) AiDecisionContext 조립
            var context = new AiDecisionContext(
                handStrength: handStrength,
                potOdds: potOdds,
                potSize: gameState.Pots.Sum(p => p.Amount),
                amountToCall: callAmount,
                playerChips: player.Chips,
                minRaise: minRaise,
                maxRaise: maxRaise,
                positionIndex: positionIndex,
                totalPlayers: activePlayers,
                personality: _personality
            );

            // (4) 액션 결정
            var decision = _actionDecisionUsecase.Decide(context);

            // 결정된 액션이 합법 액션 목록에 포함되는지 검증하고, 아니면 폴백
            var action = ResolveAction(player.Id, decision, legalActions, callAmount);

            return action;
        }

        /// <summary>
        /// 딜러 이후 순서에서 해당 좌석의 포지션 인덱스를 계산한다.
        /// 0 = 얼리 포지션, max = 레이트 포지션.
        /// </summary>
        private int CalculatePositionIndex(int seatIndex, GameState gameState)
        {
            var activeSeatIndices = gameState.Players
                .Where(p => p.Status == PlayerStatus.Active)
                .Select(p => p.SeatIndex)
                .OrderBy(s => s)
                .ToList();

            if (activeSeatIndices.Count == 0)
                return 0;

            int dealerSeat = gameState.Players[gameState.DealerIndex].SeatIndex;

            // 딜러 이후 순서로 정렬
            var ordered = new List<int>();
            int dealerPos = activeSeatIndices.IndexOf(dealerSeat);
            if (dealerPos < 0)
            {
                // 딜러가 폴드 등으로 비활성이면, 딜러 좌석 이후 첫 활성 좌석부터 시작
                dealerPos = 0;
                for (int i = 0; i < activeSeatIndices.Count; i++)
                {
                    if (activeSeatIndices[i] > dealerSeat)
                    {
                        dealerPos = i;
                        break;
                    }
                }
            }
            else
            {
                // 딜러 다음부터 시작
                dealerPos = (dealerPos + 1) % activeSeatIndices.Count;
            }

            for (int i = 0; i < activeSeatIndices.Count; i++)
            {
                int idx = (dealerPos + i) % activeSeatIndices.Count;
                ordered.Add(activeSeatIndices[idx]);
            }

            int position = ordered.IndexOf(seatIndex);
            return position >= 0 ? position : 0;
        }

        /// <summary>
        /// AI가 결정한 액션이 합법 액션 목록에 포함되지 않는 경우 폴백 처리한다.
        /// </summary>
        private PlayerAction ResolveAction(
            string playerId,
            AiDecisionResult decision,
            IReadOnlyList<ActionType> legalActions,
            int callAmount)
        {
            var actionType = decision.Action;
            int amount = decision.RaiseAmount;

            // 결정된 액션이 합법이면 그대로 사용
            if (legalActions.Contains(actionType))
            {
                return CreatePlayerAction(playerId, actionType, amount, callAmount);
            }

            // 합법이 아닌 경우 폴백: Raise→Call→Check→Fold 순으로 시도
            if (actionType == ActionType.Raise)
            {
                if (legalActions.Contains(ActionType.Call))
                    return CreatePlayerAction(playerId, ActionType.Call, 0, callAmount);
                if (legalActions.Contains(ActionType.Check))
                    return CreatePlayerAction(playerId, ActionType.Check, 0, 0);
            }
            else if (actionType == ActionType.Call)
            {
                if (legalActions.Contains(ActionType.Check))
                    return CreatePlayerAction(playerId, ActionType.Check, 0, 0);
            }
            else if (actionType == ActionType.Check)
            {
                if (legalActions.Contains(ActionType.Call))
                    return CreatePlayerAction(playerId, ActionType.Call, 0, callAmount);
            }

            // 최후의 폴백: Fold
            if (legalActions.Contains(ActionType.Fold))
                return CreatePlayerAction(playerId, ActionType.Fold, 0, 0);

            // Fold도 없으면 첫 번째 합법 액션
            return CreatePlayerAction(playerId, legalActions[0], 0, callAmount);
        }

        private PlayerAction CreatePlayerAction(string playerId, ActionType actionType, int raiseAmount, int callAmount)
        {
            switch (actionType)
            {
                case ActionType.Fold:
                    return new PlayerAction(playerId, ActionType.Fold, 0);
                case ActionType.Check:
                    return new PlayerAction(playerId, ActionType.Check, 0);
                case ActionType.Call:
                    return new PlayerAction(playerId, ActionType.Call, callAmount);
                case ActionType.Raise:
                    return new PlayerAction(playerId, ActionType.Raise, raiseAmount);
                default:
                    return new PlayerAction(playerId, ActionType.Fold, 0);
            }
        }
    }
}
