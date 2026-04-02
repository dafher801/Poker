// Source: Assets/Scripts/Gateway/StubPlayerActionProvider.cs
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

        // SeatIndex별 큐 모드
        public StubPlayerActionProvider(Dictionary<int, Queue<PlayerAction>> actionsBySeat) { /* ... */ }

        // 단일 큐 모드 (기존 호환)
        public StubPlayerActionProvider(IEnumerable<PlayerAction> actions = null) { /* ... */ }

        // 단일 큐 모드에서 액션 추가
        public void EnqueueAction(PlayerAction action) { /* ... */ }

        // SeatIndex별 큐 모드에서 액션 추가
        public void EnqueueAction(int seatIndex, PlayerAction action) { /* ... */ }

        public Task<PlayerAction> RequestActionAsync(
            int seatIndex,
            IReadOnlyList<ActionType> legalActions,
            int minRaise,
            int maxRaise,
            int callAmount,
            CancellationToken ct) { /* ... */ }
    }
}
