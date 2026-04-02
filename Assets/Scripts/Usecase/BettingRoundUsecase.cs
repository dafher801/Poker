// BettingRoundUsecase.cs
// 단일 베팅 라운드의 진행을 제어하는 Usecase.
// 사용 방법: var usecase = new BettingRoundUsecase(actionValidator, potManager);
// await usecase.RunBettingRound(state, actionProvider, broadcaster);
// 첫 액션 플레이어 결정, 순환 루프, 액션 검증·적용, 종료 조건 확인,
// 팟 수집·사이드팟 계산을 순서대로 수행한다.

using System.Collections.Generic;
using System.Threading.Tasks;
using TexasHoldem.Entity;
using TexasHoldem.Gateway;

namespace TexasHoldem.Usecase
{
    public class BettingRoundUsecase
    {
        private readonly ActionValidator _actionValidator;
        private readonly PotManager _potManager;

        public BettingRoundUsecase(ActionValidator actionValidator, PotManager potManager)
        {
            _actionValidator = actionValidator;
            _potManager = potManager;
        }

        // 베팅 라운드를 비동기로 실행한다.
        public async Task RunBettingRound(GameState state, IPlayerActionProvider actionProvider, IGameEventBroadcaster broadcaster)
        {
            broadcaster.OnBettingRoundStarted(state.Phase);

            int playerCount = state.Players.Count;
            int firstActorIndex = GetFirstActorIndex(state);

            // 각 플레이어가 이번 라운드에서 액션했는지 추적
            var hasActed = new bool[playerCount];

            int currentIndex = firstActorIndex;

            while (true)
            {
                PlayerData player = state.Players[currentIndex];

                // Folded 또는 AllIn인 플레이어는 건너뛴다
                if (player.Status == PlayerStatus.Folded || player.Status == PlayerStatus.AllIn)
                {
                    // 건너뛴 플레이어도 액션한 것으로 간주 (종료 조건 판정용)
                    hasActed[currentIndex] = true;

                    if (ShouldEndRound(state, hasActed))
                        break;

                    currentIndex = NextPlayerIndex(currentIndex, playerCount);
                    continue;
                }

                // Active가 아닌 플레이어(Waiting, Eliminated)도 건너뛴다
                if (player.Status != PlayerStatus.Active)
                {
                    hasActed[currentIndex] = true;

                    if (ShouldEndRound(state, hasActed))
                        break;

                    currentIndex = NextPlayerIndex(currentIndex, playerCount);
                    continue;
                }

                // 합법 액션 계산 및 플레이어 액션 수신
                LegalActionSet legalActions = _actionValidator.GetLegalActions(state, player.Id);
                PlayerAction action = await actionProvider.GetAction(player.Id, legalActions);

                // 액션을 GameState에 적용
                ApplyAction(state, player, action);

                broadcaster.OnPlayerActed(player.Id, action);

                hasActed[currentIndex] = true;

                // Raise가 발생하면 다른 플레이어들이 다시 액션할 기회를 가져야 한다
                if (action.Type == ActionType.Raise)
                {
                    ResetHasActedExcept(hasActed, currentIndex, state.Players);
                }

                // AllIn이면서 실질적으로 레이즈에 해당하는 경우에도 리셋
                if (action.Type == ActionType.AllIn && IsAllInRaise(state, player, action))
                {
                    ResetHasActedExcept(hasActed, currentIndex, state.Players);
                }

                if (ShouldEndRound(state, hasActed))
                    break;

                currentIndex = NextPlayerIndex(currentIndex, playerCount);
            }

            // 베팅액 수집 및 사이드 팟 계산
            _potManager.CollectBets(state);
            _potManager.CalculateSidePots(state);

            broadcaster.OnPotUpdated(state.Pots);
            broadcaster.OnBettingRoundEnded(state.Phase);
        }

        // 첫 액션 플레이어의 인덱스를 결정한다.
        // PreFlop: BB 다음 시트(UTG), PostFlop: 딜러 다음 첫 Active 플레이어
        private int GetFirstActorIndex(GameState state)
        {
            int playerCount = state.Players.Count;

            if (state.Phase == GamePhase.PreFlop)
            {
                // BB는 딜러+2 (2인일 때는 딜러+1이 BB)
                int bbIndex;
                if (playerCount == 2)
                {
                    // 헤즈업: 딜러=SB, 상대=BB
                    bbIndex = NextPlayerIndex(state.DealerIndex, playerCount);
                }
                else
                {
                    int sbIndex = NextPlayerIndex(state.DealerIndex, playerCount);
                    bbIndex = NextPlayerIndex(sbIndex, playerCount);
                }

                // UTG = BB 다음
                return NextActivePlayerIndex(NextPlayerIndex(bbIndex, playerCount), state);
            }
            else
            {
                // PostFlop: 딜러 다음 첫 Active 플레이어
                return NextActivePlayerIndex(NextPlayerIndex(state.DealerIndex, playerCount), state);
            }
        }

        // 주어진 인덱스부터 순환하며 Active 또는 AllIn이 아닌 첫 Active 플레이어를 찾는다.
        // Active 플레이어가 없으면 startIndex를 반환한다.
        private int NextActivePlayerIndex(int startIndex, GameState state)
        {
            int playerCount = state.Players.Count;
            int index = startIndex;

            for (int i = 0; i < playerCount; i++)
            {
                PlayerData player = state.Players[index];
                if (player.Status == PlayerStatus.Active)
                    return index;

                index = NextPlayerIndex(index, playerCount);
            }

            return startIndex;
        }

        private int NextPlayerIndex(int current, int playerCount)
        {
            return (current + 1) % playerCount;
        }

        // 액션을 GameState에 적용한다.
        private void ApplyAction(GameState state, PlayerData player, PlayerAction action)
        {
            switch (action.Type)
            {
                case ActionType.Fold:
                    player.Status = PlayerStatus.Folded;
                    break;

                case ActionType.Check:
                    // 변경 없음
                    break;

                case ActionType.Call:
                    int callAmount = action.Amount;
                    player.Chips -= callAmount;
                    player.CurrentBet += callAmount;
                    break;

                case ActionType.Raise:
                    int raiseTotal = action.Amount; // 레이즈 총액
                    int raiseAdditional = raiseTotal - player.CurrentBet;
                    int previousMaxBet = GetMaxBet(state);
                    state.LastRaiseSize = raiseTotal - previousMaxBet;
                    player.Chips -= raiseAdditional;
                    player.CurrentBet = raiseTotal;
                    break;

                case ActionType.AllIn:
                    int allInAmount = player.Chips;
                    player.CurrentBet += allInAmount;
                    player.Chips = 0;
                    player.Status = PlayerStatus.AllIn;
                    break;
            }
        }

        // AllIn이 실질적으로 레이즈에 해당하는지 판정한다.
        // 올인 후 CurrentBet이 기존 최고 베팅액보다 큰 경우 레이즈로 간주한다.
        private bool IsAllInRaise(GameState state, PlayerData allInPlayer, PlayerAction action)
        {
            int maxBetExcludingPlayer = 0;
            foreach (var p in state.Players)
            {
                if (p.Id == allInPlayer.Id) continue;
                if ((p.Status == PlayerStatus.Active || p.Status == PlayerStatus.AllIn)
                    && p.CurrentBet > maxBetExcludingPlayer)
                {
                    maxBetExcludingPlayer = p.CurrentBet;
                }
            }

            return allInPlayer.CurrentBet > maxBetExcludingPlayer;
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

        // 베팅 라운드 종료 조건을 확인한다.
        // (a) Active 플레이어가 1명 이하
        // (b) 모든 Active 플레이어가 최소 1회 액션했고 CurrentBet이 모두 동일
        private bool ShouldEndRound(GameState state, bool[] hasActed)
        {
            int activeCount = 0;
            int maxBet = 0;
            bool allActedAndEqual = true;

            for (int i = 0; i < state.Players.Count; i++)
            {
                PlayerData player = state.Players[i];

                if (player.Status == PlayerStatus.Active)
                {
                    activeCount++;

                    if (!hasActed[i])
                    {
                        allActedAndEqual = false;
                    }

                    if (player.CurrentBet > maxBet)
                        maxBet = player.CurrentBet;
                }
            }

            // (a) Active 플레이어가 1명 이하
            if (activeCount <= 1)
                return true;

            // (b) 모든 Active 플레이어가 액션했고 CurrentBet이 동일한지 확인
            if (!allActedAndEqual)
                return false;

            foreach (var player in state.Players)
            {
                if (player.Status == PlayerStatus.Active && player.CurrentBet != maxBet)
                    return false;
            }

            return true;
        }
    }
}
