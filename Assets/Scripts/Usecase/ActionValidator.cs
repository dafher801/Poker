// ActionValidator.cs
// 특정 플레이어가 현재 GameState에서 취할 수 있는 합법 액션을 계산한다.
// 사용 방법: ActionValidator.GetLegalActions(state, playerId)를 호출하면
// LegalActionSet(수행 가능 액션, 콜 금액, 최소/최대 레이즈 총액)을 반환한다.
// 플레이어가 Active 상태가 아니면 빈 LegalActionSet을 반환한다.

using System.Collections.Generic;
using TexasHoldem.Entity;

namespace TexasHoldem.Usecase
{
    public class ActionValidator
    {
        // playerId에 해당하는 플레이어의 합법 액션 세트를 반환한다.
        public LegalActionSet GetLegalActions(GameState state, string playerId)
        {
            PlayerData player = null;
            foreach (var p in state.Players)
            {
                if (p.Id == playerId)
                {
                    player = p;
                    break;
                }
            }

            // Active 상태가 아니면 빈 액션 세트 반환
            if (player == null || player.Status != PlayerStatus.Active)
            {
                return new LegalActionSet
                {
                    AvailableActions = new List<ActionType>(),
                    CallAmount = 0,
                    MinRaiseAmount = 0,
                    MaxRaiseAmount = 0
                };
            }

            // 현재 라운드 최고 베팅액 (Active 또는 AllIn 플레이어 기준)
            int maxBet = 0;
            foreach (var p in state.Players)
            {
                if ((p.Status == PlayerStatus.Active || p.Status == PlayerStatus.AllIn)
                    && p.CurrentBet > maxBet)
                {
                    maxBet = p.CurrentBet;
                }
            }

            var available = new List<ActionType>();

            // Fold는 항상 가능
            available.Add(ActionType.Fold);

            int callAmount = maxBet - player.CurrentBet;

            if (callAmount == 0)
            {
                // 최고 베팅액과 자신의 베팅액이 같으면 Check 가능
                available.Add(ActionType.Check);
            }
            else
            {
                // 콜 금액이 남은 칩보다 크거나 같으면 Call 불가, AllIn만 가능
                if (callAmount < player.Chips)
                {
                    available.Add(ActionType.Call);
                }
            }

            // Raise 가능 여부 확인
            // 최소 레이즈 총액 = 현재 최고 베팅액 + max(LastRaiseSize, BigBlind)
            int raiseIncrement = state.LastRaiseSize > state.Blinds.BigBlind
                ? state.LastRaiseSize
                : state.Blinds.BigBlind;
            int minRaiseTotal = maxBet + raiseIncrement;
            int playerTotalChips = player.Chips + player.CurrentBet; // 올인 시 낼 수 있는 최대 총액

            if (playerTotalChips >= minRaiseTotal)
            {
                available.Add(ActionType.Raise);
            }

            // AllIn: 칩이 1 이상이면 항상 가능
            if (player.Chips >= 1)
            {
                available.Add(ActionType.AllIn);
            }

            return new LegalActionSet
            {
                AvailableActions = available,
                CallAmount = callAmount > 0 ? callAmount : 0,
                MinRaiseAmount = minRaiseTotal,
                MaxRaiseAmount = playerTotalChips
            };
        }
    }
}
