// EntityTests.cs
// Entity 계층 단위 테스트 모음 (EditMode).
// Unity Test Framework(NUnit)을 사용하여 Deck, Card, PlayerData,
// PlayerAction, BlindInfo, GameState의 유효성 규칙을 검증한다.
// Test Runner 창(Window > General > Test Runner)에서 EditMode 탭으로 실행한다.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using TexasHoldem.Entity;
using TexasHoldem.Gateway;

namespace TexasHoldem.Tests.EditMode
{
    [TestFixture]
    public class EntityTests
    {
        // ────────────────────────────────────────────────────────────────
        // Deck
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Deck_CreatedWith52Cards()
        {
            var deck = new Deck();
            Assert.AreEqual(52, deck.Remaining);
        }

        [Test]
        public void Deck_AllCardsAreUnique()
        {
            var deck = new Deck();
            var seen = new HashSet<Card>();

            while (deck.Remaining > 0)
            {
                Card card = deck.Draw();
                Assert.IsTrue(seen.Add(card), $"중복 카드 발견: {card}");
            }

            Assert.AreEqual(52, seen.Count);
        }

        [Test]
        public void Deck_AfterFixedShuffle_CardIntegrityMaintained()
        {
            var deck = new Deck();
            var fixedSource = new FixedRandomSource();
            deck.Shuffle(fixedSource);

            Assert.AreEqual(52, deck.Remaining);

            var seen = new HashSet<Card>();
            while (deck.Remaining > 0)
            {
                Card card = deck.Draw();
                Assert.IsTrue(seen.Add(card), $"셔플 후 중복 카드 발견: {card}");
            }

            Assert.AreEqual(52, seen.Count);
        }

        [Test]
        public void Deck_Draw_DecreasesRemaining()
        {
            var deck = new Deck();
            int before = deck.Remaining;

            deck.Draw();

            Assert.AreEqual(before - 1, deck.Remaining);
        }

        [Test]
        public void Deck_DrawAfterEmpty_ThrowsInvalidOperationException()
        {
            var deck = new Deck();

            for (int i = 0; i < 52; i++)
                deck.Draw();

            Assert.Throws<InvalidOperationException>(() => deck.Draw());
        }

        // ────────────────────────────────────────────────────────────────
        // Card 동등성
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Card_SameSuitAndRank_AreEqual()
        {
            var a = new Card(Suit.Spade, Rank.Ace);
            var b = new Card(Suit.Spade, Rank.Ace);

            Assert.AreEqual(a, b);
            Assert.IsTrue(a == b);
        }

        [Test]
        public void Card_DifferentSuitOrRank_AreNotEqual()
        {
            var a = new Card(Suit.Spade, Rank.Ace);
            var b = new Card(Suit.Heart, Rank.Ace);
            var c = new Card(Suit.Spade, Rank.King);

            Assert.AreNotEqual(a, b);
            Assert.AreNotEqual(a, c);
            Assert.IsTrue(a != b);
            Assert.IsTrue(a != c);
        }

        // ────────────────────────────────────────────────────────────────
        // PlayerData
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void PlayerData_NegativeChips_ThrowsArgumentOutOfRangeException()
        {
            var player = new PlayerData("p1", "Alice", 1000, 0);

            Assert.Throws<ArgumentOutOfRangeException>(() => player.Chips = -1);
        }

        // ────────────────────────────────────────────────────────────────
        // PlayerAction 유효성
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void PlayerAction_FoldWithNonZeroAmount_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new PlayerAction("p1", ActionType.Fold, 10));
        }

        [Test]
        public void PlayerAction_CheckWithNonZeroAmount_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new PlayerAction("p1", ActionType.Check, 5));
        }

        [Test]
        public void PlayerAction_RaiseWithZeroAmount_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new PlayerAction("p1", ActionType.Raise, 0));
        }

        [Test]
        public void PlayerAction_CallWithNegativeAmount_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new PlayerAction("p1", ActionType.Call, -1));
        }

        [Test]
        public void PlayerAction_AllInWithNegativeAmount_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new PlayerAction("p1", ActionType.AllIn, -1));
        }

        // ────────────────────────────────────────────────────────────────
        // BlindInfo 유효성
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void BlindInfo_SmallBlindZero_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new BlindInfo(0, 10));
        }

        [Test]
        public void BlindInfo_BigBlindLessThanSmallBlind_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new BlindInfo(10, 5));
        }

        // ────────────────────────────────────────────────────────────────
        // GameState Players 수 범위
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void GameState_OnePlayer_ThrowsArgumentException()
        {
            var players = new List<PlayerData>
            {
                new PlayerData("p1", "Alice", 1000, 0)
            };
            var blinds = new BlindInfo(10, 20);

            Assert.Throws<ArgumentException>(() => new GameState(players, blinds));
        }

        [Test]
        public void GameState_ElevenPlayers_ThrowsArgumentException()
        {
            var players = new List<PlayerData>();
            for (int i = 0; i < 11; i++)
                players.Add(new PlayerData($"p{i}", $"Player{i}", 1000, i % 10));
            var blinds = new BlindInfo(10, 20);

            Assert.Throws<ArgumentException>(() => new GameState(players, blinds));
        }

        [Test]
        public void GameState_TwoPlayers_CreatesSuccessfully()
        {
            var players = new List<PlayerData>
            {
                new PlayerData("p1", "Alice", 1000, 0),
                new PlayerData("p2", "Bob",   1000, 1)
            };
            var blinds = new BlindInfo(10, 20);

            var state = new GameState(players, blinds);

            Assert.AreEqual(2, state.Players.Count);
        }
    }
}
