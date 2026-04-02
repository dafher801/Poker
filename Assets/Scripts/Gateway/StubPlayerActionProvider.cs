// StubPlayerActionProvider.cs
// 통합 테스트 전용 IPlayerActionProvider 구현체.
// 두 가지 모드를 지원한다:
// 1. PlayerId별 큐 모드: Dictionary<string, Queue<PlayerAction>>을 받아 PlayerId별로 액션을 관리.
// 2. 단일 큐 모드: IEnumerable<PlayerAction>을 받아 호출 순서대로 반환.
// GetAction 호출 시 큐가 비었거나 PlayerId가 없으면 InvalidOperationException을 던진다.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TexasHoldem.Entity;

namespace TexasHoldem.Gateway
{
    public class StubPlayerActionProvider : IPlayerActionProvider
    {
        private readonly Dictionary<string, Queue<PlayerAction>> _playerQueues;
        private readonly Queue<PlayerAction> _sharedQueue;
        private readonly bool _usePlayerQueues;

        public StubPlayerActionProvider(Dictionary<string, Queue<PlayerAction>> actionsByPlayer)
        {
            _playerQueues = actionsByPlayer ?? throw new ArgumentNullException(nameof(actionsByPlayer));
            _usePlayerQueues = true;
        }

        public StubPlayerActionProvider(IEnumerable<PlayerAction> actions = null)
        {
            _sharedQueue = new Queue<PlayerAction>();
            _usePlayerQueues = false;

            if (actions != null)
            {
                foreach (var action in actions)
                    _sharedQueue.Enqueue(action);
            }
        }

        public void EnqueueAction(PlayerAction action)
        {
            if (_usePlayerQueues)
                throw new InvalidOperationException(
                    "PlayerId별 큐 모드에서는 EnqueueAction(string, PlayerAction)을 사용하세요.");

            _sharedQueue.Enqueue(action);
        }

        public void EnqueueAction(string playerId, PlayerAction action)
        {
            if (!_usePlayerQueues)
                throw new InvalidOperationException(
                    "단일 큐 모드에서는 EnqueueAction(PlayerAction)을 사용하세요.");

            if (!_playerQueues.ContainsKey(playerId))
                _playerQueues[playerId] = new Queue<PlayerAction>();

            _playerQueues[playerId].Enqueue(action);
        }

        public Task<PlayerAction> GetAction(string playerId, LegalActionSet legalActions)
        {
            if (_usePlayerQueues)
            {
                if (!_playerQueues.TryGetValue(playerId, out var queue) || queue.Count == 0)
                    throw new InvalidOperationException(
                        $"StubPlayerActionProvider: PlayerId '{playerId}'에 대한 남은 액션이 없습니다.");

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
