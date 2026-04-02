// MockPlayerActionProvider.cs
// 테스트용 IPlayerActionProvider 구현체.
// Dictionary<(int seatIndex, int actionSequence), PlayerAction> 형태로
// 스크립트된 액션 시퀀스를 생성자에서 주입받는다.
// RequestActionAsync 호출 시 해당 좌석의 다음 스크립트된 액션을 반환하며,
// 스크립트에 없는 요청이 오면 InvalidOperationException을 던진다.
// 플레이어별 호출 카운터를 관리하여 동일 좌석의 n번째 호출에 올바른 액션을 반환한다.
// 선택적으로 대기 시간(delayMs)을 지정하여 비동기 대기를 시뮬레이션할 수 있다.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TexasHoldem.Entity;

namespace TexasHoldem.Gateway
{
    public class MockPlayerActionProvider : IPlayerActionProvider
    {
        private readonly Dictionary<(int seatIndex, int actionSequence), PlayerAction> _scriptedActions;
        private readonly Dictionary<int, int> _callCounters = new Dictionary<int, int>();
        private readonly int _delayMs;

        public MockPlayerActionProvider(
            Dictionary<(int seatIndex, int actionSequence), PlayerAction> scriptedActions,
            int delayMs = 0)
        {
            _scriptedActions = scriptedActions ?? throw new ArgumentNullException(nameof(scriptedActions));
            _delayMs = delayMs;
        }

        public async Task<PlayerAction> RequestActionAsync(
            int seatIndex,
            IReadOnlyList<ActionType> legalActions,
            int minRaise,
            int maxRaise,
            int callAmount,
            CancellationToken ct)
        {
            if (_delayMs > 0)
            {
                await Task.Delay(_delayMs, ct);
            }

            if (!_callCounters.TryGetValue(seatIndex, out int count))
            {
                count = 0;
            }

            var key = (seatIndex, count);
            if (!_scriptedActions.TryGetValue(key, out PlayerAction action))
            {
                throw new InvalidOperationException(
                    $"MockPlayerActionProvider: 좌석 {seatIndex}의 {count}번째 액션이 스크립트에 없습니다.");
            }

            _callCounters[seatIndex] = count + 1;
            return action;
        }
    }
}
