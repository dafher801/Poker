// Source: Assets/Scripts/Gateway/StubPlayerActionProvider.cs
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

        // PlayerId별 큐 모드
        public StubPlayerActionProvider(Dictionary<string, Queue<PlayerAction>> actionsByPlayer) { /* ... */ }

        // 단일 큐 모드 (기존 호환)
        public StubPlayerActionProvider(IEnumerable<PlayerAction> actions = null) { /* ... */ }

        // 단일 큐 모드에서 액션 추가
        public void EnqueueAction(PlayerAction action) { /* ... */ }

        // PlayerId별 큐 모드에서 액션 추가
        public void EnqueueAction(string playerId, PlayerAction action) { /* ... */ }

        public Task<PlayerAction> GetAction(string playerId, LegalActionSet legalActions) { /* ... */ }
    }
}
