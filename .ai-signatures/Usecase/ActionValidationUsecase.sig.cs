// Source: Assets/Scripts/Usecase/ActionValidationUsecase.cs
// ActionValidationUsecase.cs
// 현재 게임 상태와 플레이어 ID를 받아 PlayerActionContext를 생성·반환하는 유스케이스.
// 내부적으로 ActionValidator를 사용하여 합법 액션 세트를 계산한 뒤,
// View가 사용할 수 있는 PlayerActionContext로 변환한다.

using System.Collections.Generic;
using TexasHoldem.Entity;

namespace TexasHoldem.Usecase
{
    public class ActionValidationUsecase
    {
        private readonly ActionValidator _actionValidator;

        public ActionValidationUsecase(ActionValidator actionValidator) { /* ... */ }
        {
            _actionValidator = actionValidator;
        }

        /// <summary>
        /// 현재 GameState와 playerId로부터 PlayerActionContext를 생성하여 반환한다.
        /// </summary>
        public PlayerActionContext BuildActionContext(GameState state, string playerId) { /* ... */ }
        {
            LegalActionSet legalSet = _actionValidator.GetLegalActions(state, playerId);

            PlayerData player = null;
            foreach (var p in state.Players)
            {
                if (p.Id == playerId)
                {
                    player = p;
                    break;
                }
            }

            int playerChips = player != null ? player.Chips : 0;

            int potTotal = 0;
            foreach (var pot in state.Pots)
            {
                potTotal += pot.Amount;
            }
            // 아직 팟에 합산되지 않은 현재 라운드 베팅도 포함
            foreach (var p in state.Players)
            {
                potTotal += p.CurrentBet;
            }

            var validActions = new HashSet<ActionType>(legalSet.AvailableActions);

            return new PlayerActionContext(
                currentBetToCall: legalSet.CallAmount,
                minRaiseAmount: legalSet.MinRaiseAmount,
                maxRaiseAmount: legalSet.MaxRaiseAmount,
                playerChips: playerChips,
                potTotal: potTotal,
                validActions: validActions
            );
        }
    }
}
