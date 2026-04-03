// LocalPlayerActionDirector.cs
// 로컬 플레이어의 턴 흐름을 중재하는 Director 클래스.
// ActionValidationUsecase로 PlayerActionContext를 생성하고, ActionPanelView를 통해 UI를 표시한 뒤,
// 유저가 버튼을 클릭하면 LocalPlayerInputGateway에 액션을 제출하여 대기를 해제한다.
// View 콜백 구독/해제를 HandleTurnAsync 진입/종료 시 정확히 관리하여 중복 호출을 방지한다.

using System;
using System.Threading;
using System.Threading.Tasks;
using TexasHoldem.Entity;
using TexasHoldem.Gateway;
using TexasHoldem.Usecase;
using TexasHoldem.View;

namespace TexasHoldem.Director
{
    public class LocalPlayerActionDirector
    {
        private readonly ActionValidationUsecase _actionValidationUsecase;
        private readonly LocalPlayerInputGateway _inputGateway;
        private readonly ActionPanelView _actionPanelView;

        public LocalPlayerActionDirector(
            ActionValidationUsecase actionValidationUsecase,
            LocalPlayerInputGateway inputGateway,
            ActionPanelView actionPanelView)
        {
            _actionValidationUsecase = actionValidationUsecase ?? throw new ArgumentNullException(nameof(actionValidationUsecase));
            _inputGateway = inputGateway ?? throw new ArgumentNullException(nameof(inputGateway));
            _actionPanelView = actionPanelView ?? throw new ArgumentNullException(nameof(actionPanelView));
        }

        /// <summary>
        /// 로컬 플레이어의 턴을 처리한다.
        /// UI를 표시하고, 유저 입력을 받아 Gateway에 제출한 뒤, 결과를 반환한다.
        /// </summary>
        /// <param name="state">현재 게임 상태</param>
        /// <param name="playerId">로컬 플레이어 ID</param>
        /// <param name="seatIndex">로컬 플레이어 좌석 인덱스</param>
        /// <param name="ct">취소 토큰</param>
        /// <returns>유저가 선택한 PlayerAction</returns>
        public async Task<PlayerAction> HandleTurnAsync(GameState state, string playerId, int seatIndex, CancellationToken ct)
        {
            // (1) ActionValidationUsecase로 PlayerActionContext 생성
            PlayerActionContext ctx = _actionValidationUsecase.BuildActionContext(state, playerId);

            // (2) View의 OnActionSelected 콜백 구독
            Action<ActionType, int> callback = null;
            callback = (actionType, amount) =>
            {
                PlayerAction action;
                if (actionType == ActionType.Call)
                {
                    action = new PlayerAction(playerId, ActionType.Call, ctx.CurrentBetToCall);
                }
                else if (actionType == ActionType.AllIn)
                {
                    action = new PlayerAction(playerId, ActionType.AllIn, ctx.PlayerChips);
                }
                else
                {
                    action = new PlayerAction(playerId, actionType, amount);
                }

                _inputGateway.SubmitAction(action);
            };

            _actionPanelView.OnActionSelected += callback;

            try
            {
                // (3) ActionPanelView.Show(ctx) 호출하여 UI 표시
                _actionPanelView.Show(ctx);

                // (4) Gateway.RequestActionAsync()를 await하여 결과 수신
                var legalActionsList = new System.Collections.Generic.List<ActionType>(ctx.ValidActions);
                PlayerAction result = await _inputGateway.RequestActionAsync(
                    seatIndex,
                    legalActionsList,
                    ctx.MinRaiseAmount,
                    ctx.MaxRaiseAmount,
                    ctx.CurrentBetToCall,
                    ct);

                return result;
            }
            finally
            {
                // (5) 콜백 해제 및 UI 숨김 (예외·취소 시에도 반드시 실행)
                _actionPanelView.OnActionSelected -= callback;
                _actionPanelView.Hide();
            }
        }
    }
}
