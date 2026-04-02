// GameEventBroadcasterTests.cs
// LocalGameEventBroadcaster의 이벤트 발행·구독·수신 기능을 검증하는 단위 테스트.
// Subscribe/Unsubscribe/Publish 동작, 타입 격리, 복수 핸들러, 예외 격리를 테스트한다.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using TexasHoldem.Director;
using TexasHoldem.Entity;

namespace Poker.EditMode.Tests
{
    [TestFixture]
    public class GameEventBroadcasterTests
    {
        private LocalGameEventBroadcaster _broadcaster;

        [SetUp]
        public void SetUp()
        {
            _broadcaster = new LocalGameEventBroadcaster();
        }

        // ── (1) Subscribe 후 Publish하면 핸들러가 호출되는지 확인 ──

        [Test]
        public void Subscribe_ThenPublish_HandlerIsCalled()
        {
            PhaseChangedEvent received = null;
            _broadcaster.Subscribe<PhaseChangedEvent>(e => received = e);

            var evt = new PhaseChangedEvent(1000L, "hand-1", RoundPhase.PreFlop, RoundPhase.Flop);
            _broadcaster.Publish(evt);

            Assert.IsNotNull(received);
            Assert.AreEqual(RoundPhase.PreFlop, received.PreviousPhase);
            Assert.AreEqual(RoundPhase.Flop, received.CurrentPhase);
        }

        // ── (2) Unsubscribe 후 Publish하면 핸들러가 호출되지 않는지 확인 ──

        [Test]
        public void Unsubscribe_ThenPublish_HandlerIsNotCalled()
        {
            bool called = false;
            Action<PhaseChangedEvent> handler = e => called = true;

            _broadcaster.Subscribe(handler);
            _broadcaster.Unsubscribe(handler);

            _broadcaster.Publish(new PhaseChangedEvent(1000L, "hand-1", RoundPhase.PreFlop, RoundPhase.Flop));

            Assert.IsFalse(called);
        }

        // ── (3) 서로 다른 이벤트 타입 Subscribe 시 해당 타입만 수신 ──

        [Test]
        public void Subscribe_DifferentTypes_OnlyMatchingTypeReceived()
        {
            bool phaseHandlerCalled = false;
            bool potHandlerCalled = false;

            _broadcaster.Subscribe<PhaseChangedEvent>(e => phaseHandlerCalled = true);
            _broadcaster.Subscribe<PotUpdatedEvent>(e => potHandlerCalled = true);

            _broadcaster.Publish(new PhaseChangedEvent(1000L, "hand-1", RoundPhase.PreFlop, RoundPhase.Flop));

            Assert.IsTrue(phaseHandlerCalled);
            Assert.IsFalse(potHandlerCalled);
        }

        // ── (4) 동일 이벤트 타입에 복수 핸들러 등록 시 모두 호출 ──

        [Test]
        public void Subscribe_MultipleHandlers_AllAreCalled()
        {
            int callCount = 0;

            _broadcaster.Subscribe<PhaseChangedEvent>(e => callCount++);
            _broadcaster.Subscribe<PhaseChangedEvent>(e => callCount++);
            _broadcaster.Subscribe<PhaseChangedEvent>(e => callCount++);

            _broadcaster.Publish(new PhaseChangedEvent(1000L, "hand-1", RoundPhase.PreFlop, RoundPhase.Flop));

            Assert.AreEqual(3, callCount);
        }

        // ── (5) 핸들러 내부에서 예외 발생 시 다른 핸들러가 정상 호출 ──

        [Test]
        public void Publish_HandlerThrows_OtherHandlersStillCalled()
        {
            bool firstCalled = false;
            bool thirdCalled = false;

            _broadcaster.Subscribe<PhaseChangedEvent>(e => firstCalled = true);
            _broadcaster.Subscribe<PhaseChangedEvent>(e => { throw new InvalidOperationException("test exception"); });
            _broadcaster.Subscribe<PhaseChangedEvent>(e => thirdCalled = true);

            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException"));
            _broadcaster.Publish(new PhaseChangedEvent(1000L, "hand-1", RoundPhase.PreFlop, RoundPhase.Flop));

            Assert.IsTrue(firstCalled);
            Assert.IsTrue(thirdCalled);
        }

        // ── 7개 이벤트 타입별 Publish-Subscribe 라운드트립 테스트 ──

        [Test]
        public void RoundTrip_PhaseChangedEvent()
        {
            PhaseChangedEvent received = null;
            _broadcaster.Subscribe<PhaseChangedEvent>(e => received = e);

            var evt = new PhaseChangedEvent(100L, "h1", RoundPhase.Flop, RoundPhase.Turn);
            _broadcaster.Publish(evt);

            Assert.AreSame(evt, received);
            Assert.AreEqual(RoundPhase.Flop, received.PreviousPhase);
            Assert.AreEqual(RoundPhase.Turn, received.CurrentPhase);
        }

        [Test]
        public void RoundTrip_CardsDealtEvent()
        {
            CardsDealtEvent received = null;
            _broadcaster.Subscribe<CardsDealtEvent>(e => received = e);

            var cards = new List<Card> { new Card(Suit.Spade, Rank.Ace), new Card(Suit.Heart, Rank.King) };
            var evt = new CardsDealtEvent(100L, "h1", CardDealType.HoleCard, cards, 0);
            _broadcaster.Publish(evt);

            Assert.AreSame(evt, received);
            Assert.AreEqual(CardDealType.HoleCard, received.DealType);
            Assert.AreEqual(2, received.Cards.Count);
            Assert.AreEqual(0, received.TargetPlayerSeatIndex);
        }

        [Test]
        public void RoundTrip_PlayerActedEvent()
        {
            PlayerActedEvent received = null;
            _broadcaster.Subscribe<PlayerActedEvent>(e => received = e);

            var evt = new PlayerActedEvent(100L, "h1", 2, ActionType.Raise, 500);
            _broadcaster.Publish(evt);

            Assert.AreSame(evt, received);
            Assert.AreEqual(2, received.SeatIndex);
            Assert.AreEqual(ActionType.Raise, received.ActionType);
            Assert.AreEqual(500, received.Amount);
        }

        [Test]
        public void RoundTrip_PotUpdatedEvent()
        {
            PotUpdatedEvent received = null;
            _broadcaster.Subscribe<PotUpdatedEvent>(e => received = e);

            var sidePots = new List<int> { 200, 300 };
            var evt = new PotUpdatedEvent(100L, "h1", 1000, sidePots);
            _broadcaster.Publish(evt);

            Assert.AreSame(evt, received);
            Assert.AreEqual(1000, received.MainPot);
            Assert.AreEqual(2, received.SidePots.Count);
        }

        [Test]
        public void RoundTrip_TurnStartedEvent()
        {
            TurnStartedEvent received = null;
            _broadcaster.Subscribe<TurnStartedEvent>(e => received = e);

            var actions = new List<ActionType> { ActionType.Fold, ActionType.Call, ActionType.Raise };
            var evt = new TurnStartedEvent(100L, "h1", 3, actions, 100, 5000, 30f);
            _broadcaster.Publish(evt);

            Assert.AreSame(evt, received);
            Assert.AreEqual(3, received.SeatIndex);
            Assert.AreEqual(3, received.AvailableActions.Count);
            Assert.AreEqual(100, received.MinRaiseAmount);
            Assert.AreEqual(5000, received.MaxRaiseAmount);
            Assert.AreEqual(30f, received.TimeLimit);
        }

        [Test]
        public void RoundTrip_ShowdownResultEvent()
        {
            ShowdownResultEvent received = null;
            _broadcaster.Subscribe<ShowdownResultEvent>(e => received = e);

            var entries = new List<ShowdownEntry>
            {
                new ShowdownEntry(0, new List<Card> { new Card(Suit.Spade, Rank.Ace), new Card(Suit.Heart, Rank.King) }, HandRank.OnePair, true),
                new ShowdownEntry(1, new List<Card> { new Card(Suit.Diamond, Rank.Ten), new Card(Suit.Club, Rank.Nine) }, HandRank.HighCard, false)
            };
            var evt = new ShowdownResultEvent(100L, "h1", entries);
            _broadcaster.Publish(evt);

            Assert.AreSame(evt, received);
            Assert.AreEqual(2, received.Entries.Count);
            Assert.IsTrue(received.Entries[0].IsWinner);
            Assert.IsFalse(received.Entries[1].IsWinner);
        }

        [Test]
        public void RoundTrip_HandEndedEvent()
        {
            HandEndedEvent received = null;
            _broadcaster.Subscribe<HandEndedEvent>(e => received = e);

            var awards = new List<PotAward>
            {
                new PotAward(0, 1500, "Main"),
                new PotAward(2, 500, "Side1")
            };
            var evt = new HandEndedEvent(100L, "h1", awards, HandEndReason.Showdown);
            _broadcaster.Publish(evt);

            Assert.AreSame(evt, received);
            Assert.AreEqual(2, received.Awards.Count);
            Assert.AreEqual(HandEndReason.Showdown, received.Reason);
            Assert.AreEqual("Main", received.Awards[0].PotLabel);
            Assert.AreEqual(1500, received.Awards[0].Amount);
        }
    }
}
