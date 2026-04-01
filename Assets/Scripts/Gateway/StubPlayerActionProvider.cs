// StubPlayerActionProvider.cs
// 테스트 전용 IPlayerActionProvider 구현체.
// 생성자 또는 EnqueueAction으로 PlayerAction을 Queue에 미리 적재하고,
// GetAction 호출 시 큐에서 순서대로 반환한다. 큐가 비면 InvalidOperationException을 던진다.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TexasHoldem.Entity;

namespace TexasHoldem.Gateway
{
    public class StubPlayerActionProvider : IPlayerActionProvider
    {
        private readonly Queue<PlayerAction> _queue = new Queue<PlayerAction>();

        public StubPlayerActionProvider(IEnumerable<PlayerAction> actions = null)
        {
            if (actions != null)
            {
                foreach (var action in actions)
                    _queue.Enqueue(action);
            }
        }

        public void EnqueueAction(PlayerAction action)
        {
            _queue.Enqueue(action);
        }

        public Task<PlayerAction> GetAction(string playerId, GameState snapshot, List<ActionType> legalActions)
        {
            if (_queue.Count == 0)
                throw new InvalidOperationException("StubPlayerActionProvider: 큐에 남은 액션이 없습니다.");

            return Task.FromResult(_queue.Dequeue());
        }
    }
}
