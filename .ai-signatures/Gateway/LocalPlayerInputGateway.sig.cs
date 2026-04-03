// Source: Assets/Scripts/Gateway/LocalPlayerInputGateway.cs
// LocalPlayerInputGateway.cs
// IPlayerActionProvider를 구현하는 로컬 유저 입력 Gateway.
// RequestActionAsync 호출 시 TaskCompletionSource로 대기하며,
// 외부(View 콜백 등)에서 SubmitAction을 호출하면 대기를 해제하여 선택된 액션을 반환한다.
// CancellationToken을 지원하여 타임아웃·게임 중단 시 취소 가능하다.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TexasHoldem.Entity;

namespace TexasHoldem.Gateway
{
    public class LocalPlayerInputGateway : IPlayerActionProvider
    {
        private TaskCompletionSource<PlayerAction> _tcs;

        public async Task<PlayerAction> RequestActionAsync(
            int seatIndex,
            IReadOnlyList<ActionType> legalActions,
            int minRaise,
            int maxRaise,
            int callAmount,
            CancellationToken ct)
        {
            _tcs = new TaskCompletionSource<PlayerAction>();

            using (ct.Register(() { /* ... */ }
            {
                return await _tcs.Task;
            }
        }

        /// <summary>
        /// 외부에서 유저가 선택한 액션을 전달하여 RequestActionAsync의 대기를 해제한다.
        /// </summary>
        public void SubmitAction(PlayerAction action) { /* ... */ }
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            if (_tcs == null || _tcs.Task.IsCompleted)
                throw new InvalidOperationException("No pending action request.");

            _tcs.TrySetResult(action);
        }
    }
}
