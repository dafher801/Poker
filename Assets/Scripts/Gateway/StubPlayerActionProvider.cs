// StubPlayerActionProvider.cs
// 통합 테스트 전용 IPlayerActionProvider 구현체.
// 두 가지 모드를 지원한다:
// 1. SeatIndex별 큐 모드: Dictionary<int, Queue<PlayerAction>>을 받아 시트별로 액션을 관리.
// 2. 단일 큐 모드: IEnumerable<PlayerAction>을 받아 호출 순서대로 반환.
// RequestActionAsync 호출 시 큐가 비었거나 SeatIndex가 없으면 InvalidOperationException을 던진다.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TexasHoldem.Entity;

namespace TexasHoldem.Gateway
{
    public class StubPlayerActionProvider : IPlayerActionProvider
    {
        private readonly Dictionary<int, Queue<PlayerAction>> _seatQueues;
        private readonly Queue<PlayerAction> _sharedQueue;
        private readonly bool _useSeatQueues;

        public StubPlayerActionProvider(Dictionary<int, Queue<PlayerAction>> actionsBySeat)
        {
            _seatQueues = actionsBySeat ?? throw new ArgumentNullException(nameof(actionsBySeat));
            _useSeatQueues = true;
        }

        public StubPlayerActionProvider(IEnumerable<PlayerAction> actions = null)
        {
            _sharedQueue = new Queue<PlayerAction>();
            _useSeatQueues = false;

            if (actions != null)
            {
                foreach (var action in actions)
                    _sharedQueue.Enqueue(action);
            }
        }

        public void EnqueueAction(PlayerAction action)
        {
            if (_useSeatQueues)
                throw new InvalidOperationException(
                    "SeatIndex별 큐 모드에서는 EnqueueAction(int, PlayerAction)을 사용하세요.");

            _sharedQueue.Enqueue(action);
        }

        public void EnqueueAction(int seatIndex, PlayerAction action)
        {
            if (!_useSeatQueues)
                throw new InvalidOperationException(
                    "단일 큐 모드에서는 EnqueueAction(PlayerAction)을 사용하세요.");

            if (!_seatQueues.ContainsKey(seatIndex))
                _seatQueues[seatIndex] = new Queue<PlayerAction>();

            _seatQueues[seatIndex].Enqueue(action);
        }

        public Task<PlayerAction> RequestActionAsync(
            int seatIndex,
            IReadOnlyList<ActionType> legalActions,
            int minRaise,
            int maxRaise,
            int callAmount,
            CancellationToken ct)
        {
            if (_useSeatQueues)
            {
                if (!_seatQueues.TryGetValue(seatIndex, out var queue) || queue.Count == 0)
                    throw new InvalidOperationException(
                        $"StubPlayerActionProvider: SeatIndex '{seatIndex}'에 대한 남은 액션이 없습니다.");

                return Task.FromResult(queue.Dequeue());
            }
            else
            {
                if (_sharedQueue.Count == 0)
                    throw new InvalidOperationException(
                        "StubPlayerActionProvider: 큐에 남은 액션이 없습니다.");

                return Task.FromResult(_sharedQueue.Dequeue());
            }
        }
    }
}
