// GatewayStubTests.cs
// Gateway 계층 구현체 단위 테스트 모음 (EditMode).
// Unity Test Framework(NUnit)을 사용하여 SystemRandomSource, FixedRandomSource,
// InMemoryGameStateRepository, StubPlayerActionProvider, StubGameEventBroadcaster를 검증한다.
// Test Runner 창(Window > General > Test Runner)에서 EditMode 탭으로 실행한다.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TexasHoldem.Entity;
using TexasHoldem.Gateway;

namespace TexasHoldem.Tests.EditMode
{
    [TestFixture]
    public class GatewayStubTests
    {
        // ────────────────────────────────────────────────────────────────
        // SystemRandomSource
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void SystemRandomSource_Shuffle_PreservesAllElements()
        {
            var source = new SystemRandomSource(42);
            var list = new List<int> { 1, 2, 3, 4, 5 };
            var original = new List<int>(list);

            source.Shuffle(list);

            Assert.AreEqual(original.Count, list.Count);
            foreach (int item in original)
                Assert.IsTrue(list.Contains(item), $"원소 {item}이 셔플 후 사라졌습니다.");
        }

        [Test]
        public void SystemRandomSource_Shuffle_ChangesOrder()
        {
            // seed=42로 1~100 리스트를 셔플하면 거의 확실하게 순서가 바뀐다.
            var source = new SystemRandomSource(42);
            var list = Enumerable.Range(1, 100).ToList();
            var original = new List<int>(list);

            source.Shuffle(list);

            Assert.IsFalse(list.SequenceEqual(original), "셔플 후에도 순서가 동일합니다.");
        }

        // ────────────────────────────────────────────────────────────────
        // FixedRandomSource
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void FixedRandomSource_Shuffle_PreservesOrder()
        {
            var source = new FixedRandomSource();
            var list = new List<int> { 10, 20, 30, 40, 50 };
            var original = new List<int>(list);

            source.Shuffle(list);

            Assert.IsTrue(list.SequenceEqual(original), "FixedRandomSource는 순서를 변경하면 안 됩니다.");
        }

        // ────────────────────────────────────────────────────────────────
        // InMemoryGameStateRepository
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void InMemoryGameStateRepository_SaveAndLoad_ReturnsSameState()
        {
            var repo = new InMemoryGameStateRepository();
            var players = new List<PlayerData>
            {
                new PlayerData("p1", "Alice", 1000, 0),
                new PlayerData("p2", "Bob",   1000, 1)
            };
            var blinds = new BlindInfo(10, 20);
            var state = new GameState(players, blinds);

            repo.Save(state);
            GameState loaded = repo.Load();

            Assert.AreSame(state, loaded);
        }

        [Test]
        public void InMemoryGameStateRepository_LoadWithoutSave_ThrowsInvalidOperationException()
        {
            var repo = new InMemoryGameStateRepository();

            Assert.Throws<InvalidOperationException>(() => repo.Load());
        }

        // ────────────────────────────────────────────────────────────────
        // StubPlayerActionProvider
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void StubPlayerActionProvider_RequestActionAsync_ReturnsEnqueuedActionsInOrder()
        {
            var action1 = new PlayerAction("p1", ActionType.Call, 20);
            var action2 = new PlayerAction("p1", ActionType.Fold, 0);
            var provider = new StubPlayerActionProvider(new[] { action1, action2 });

            var result1 = provider.RequestActionAsync(0, new List<ActionType>(), 0, 0, 0, System.Threading.CancellationToken.None).Result;
            var result2 = provider.RequestActionAsync(0, new List<ActionType>(), 0, 0, 0, System.Threading.CancellationToken.None).Result;

            Assert.AreSame(action1, result1);
            Assert.AreSame(action2, result2);
        }

        [Test]
        public void StubPlayerActionProvider_RequestActionAsyncOnEmptyQueue_ThrowsInvalidOperationException()
        {
            var provider = new StubPlayerActionProvider();

            Assert.Throws<InvalidOperationException>(() =>
                provider.RequestActionAsync(0, new List<ActionType>(), 0, 0, 0, System.Threading.CancellationToken.None).GetAwaiter().GetResult());
        }

        [Test]
        public void StubPlayerActionProvider_EnqueueAction_AddsToQueue()
        {
            var provider = new StubPlayerActionProvider();
            var action = new PlayerAction("p1", ActionType.Check, 0);
            provider.EnqueueAction(action);

            var result = provider.RequestActionAsync(0, new List<ActionType>(), 0, 0, 0, System.Threading.CancellationToken.None).Result;

            Assert.AreSame(action, result);
        }

        // ────────────────────────────────────────────────────────────────
        // StubGameEventBroadcaster
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void StubGameEventBroadcaster_Broadcast_RecordsEvent()
        {
            var broadcaster = new StubGameEventBroadcaster();
            var evt = new PlayerActedEvent(DateTime.UtcNow.Ticks, "", 0, ActionType.Raise, 50);

            broadcaster.Broadcast(evt);

            Assert.AreEqual(1, broadcaster.EventCount);
            Assert.AreSame(evt, broadcaster.GetEventAt(0));
        }

        [Test]
        public void StubGameEventBroadcaster_GetEventsGeneric_FiltersCorrectly()
        {
            var broadcaster = new StubGameEventBroadcaster();
            broadcaster.Broadcast(new PlayerActedEvent(DateTime.UtcNow.Ticks, "", 0, ActionType.Check, 0));
            broadcaster.Broadcast(new PotUpdatedEvent(DateTime.UtcNow.Ticks, "", 100, new List<int>()));

            var playerEvents = broadcaster.GetEvents<PlayerActedEvent>();
            Assert.AreEqual(1, playerEvents.Count);
            Assert.AreEqual(ActionType.Check, playerEvents[0].ActionType);
        }

        [Test]
        public void StubGameEventBroadcaster_MultipleEvents_RecordsAllInOrder()
        {
            var broadcaster = new StubGameEventBroadcaster();

            broadcaster.Broadcast(new HandStartedEvent(DateTime.UtcNow.Ticks, "", 0, new List<int> { 0, 1 }));
            broadcaster.Broadcast(new PlayerActedEvent(DateTime.UtcNow.Ticks, "", 0, ActionType.Call, 10));
            broadcaster.Broadcast(new PotUpdatedEvent(DateTime.UtcNow.Ticks, "", 100, new List<int>()));

            Assert.AreEqual(3, broadcaster.EventCount);
            Assert.IsInstanceOf<HandStartedEvent>(broadcaster.GetEventAt(0));
            Assert.IsInstanceOf<PlayerActedEvent>(broadcaster.GetEventAt(1));
            Assert.IsInstanceOf<PotUpdatedEvent>(broadcaster.GetEventAt(2));
        }
    }
}