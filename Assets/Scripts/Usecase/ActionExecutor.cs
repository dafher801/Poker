// ActionExecutor.cs
// 플레이어 액션(Fold, Check, Call, Raise, AllIn)을 GameState에 적용한다.
// 사용 방법: ActionExecutor.Execute(state, action)을 호출하면
// 해당 플레이어의 Chips, CurrentBet, Status와 GameState의 Pot, LastRaiseSize가 갱신된다.
// ActionValidator로 합법성이 검증된 액션만 전달되어야 한다.

using System;
using System.Linq;
using TexasHoldem.Entity;

namespace TexasHoldem.Usecase
{
    public class ActionExecutor
    {
        /// <summary>
        /// 플레이어 액션을 GameState에 적용하여 상태를 갱신한다.
        /// </summary>
        public void Execute(GameState state, PlayerAction action)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            PlayerData player = null;
            foreach (var p in state.Players)
            {
                if (p.Id == action.PlayerId)
                {
                    player = p;
                    break;
                }
            }

            if (player == null)
                throw new ArgumentException($"Player '{action.PlayerId}' not found in GameState.", nameof(action));

            switch (action.Type)
            {
                case ActionType.Fold:
                    ExecuteFold(player);
                    break;
                case ActionType.Check:
                    // 상태 변경 없음 (합법성은 ActionValidator 책임)
                    break;
                case ActionType.Call:
                    ExecuteCall(state, player);
                    break;
                case ActionType.Raise:
                    ExecuteRaise(state, player, action.Amount);
                    break;
                case ActionType.AllIn:
                    ExecuteAllIn(state, player);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), $"Unknown ActionType: {action.Type}");
            }
        }

        private void ExecuteFold(PlayerData player)
        {
            player.Status = PlayerStatus.Folded;
        }

        private void ExecuteCall(GameState state, PlayerData player)
        {
            int highestBet = GetHighestBet(state);
            int callAmount = highestBet - player.CurrentBet;

            if (callAmount <= 0)
                return;

            player.Chips -= callAmount;
            player.CurrentBet += callAmount;
            AddToPot(state, callAmount);
        }

        private void ExecuteRaise(GameState state, PlayerData player, int raiseTotal)
        {
            // raiseTotal은 레이즈 후 플레이어의 목표 총 베팅액
            int highestBet = GetHighestBet(state);
            int additionalChips = raiseTotal - player.CurrentBet;

            player.Chips -= additionalChips;
            player.CurrentBet = raiseTotal;
            state.LastRaiseSize = raiseTotal - highestBet;
            AddToPot(state, additionalChips);
        }

        private void ExecuteAllIn(GameState state, PlayerData player)
        {
            int allInAmount = player.Chips;
            int newTotalBet = player.CurrentBet + allInAmount;
            int highestBet = GetHighestBet(state);

            // 올인 금액이 현재 최고 베팅액을 넘으면 LastRaiseSize 갱신
            if (newTotalBet > highestBet)
            {
                int raiseSize = newTotalBet - highestBet;
                // 최소 레이즈 크기 이상일 때만 LastRaiseSize 갱신 (숏 올인은 갱신하지 않음)
                if (raiseSize >= state.LastRaiseSize)
                {
                    state.LastRaiseSize = raiseSize;
                }
            }

            player.CurrentBet += allInAmount;
            player.Chips = 0;
            player.Status = PlayerStatus.AllIn;
            AddToPot(state, allInAmount);
        }

        private int GetHighestBet(GameState state)
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

        private void AddToPot(GameState state, int amount)
        {
            if (amount <= 0)
                return;

            if (state.Pots.Count == 0)
            {
                state.Pots.Add(new Pot(amount, state.Players
                    .FindAll(p => p.Status != PlayerStatus.Folded && p.Status != PlayerStatus.Eliminated)
                    .ConvertAll(p => p.Id)));
            }
            else
            {
                state.Pots[0].AddAmount(amount);
            }
        }
    }
}
