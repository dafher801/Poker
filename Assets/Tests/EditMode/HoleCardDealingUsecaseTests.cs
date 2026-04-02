// HoleCardDealingUsecaseTests.cs
// HoleCardDealingUsecase의 단위 테스트.
// 딜링 순서, 카드 배분 개수, 예외 처리를 검증한다.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TexasHoldem.Entity;
using TexasHoldem.Gateway;
using TexasHoldem.Usecase;

namespace TexasHoldem.Tests.EditMode
{
    [TestFixture]
    public class HoleCardDealingUsecaseTests
    {
        private Deck CreateShuffledDeck()
        {
            var deck = new Deck();
            deck.Shuffle(new SystemRandomSource());
            return deck;
        }

        [Test]
        public void DealHoleCards_EachPlayerGets2Cards()
        {
            var deck = CreateShuffledDeck();
            var seats = new List<int> { 0, 2, 5 };

            var result = HoleCardDealingUsecase.DealHoleCards(deck, seats, 0);

            Assert.AreEqual(3, result.Count);
            foreach (int seat in seats)
            {
                Assert.IsTrue(result.ContainsKey(seat));
            }
        }

        [Test]
        public void DealHoleCards_ConsumesCorrectNumberOfCards()
        {
            var deck = CreateShuffledDeck();
            var seats = new List<int> { 1, 3, 7, 9 };

            HoleCardDealingUsecase.DealHoleCards(deck, seats, 1);

            // 4명 × 2장 = 8장 소비
            Assert.AreEqual(52 - 8, deck.Remaining);
        }

        [Test]
        public void DealHoleCards_AllCardsAreUnique()
        {
            var deck = CreateShuffledDeck();
            var seats = new List<int> { 0, 1, 2, 3, 4 };

            var result = HoleCardDealingUsecase.DealHoleCards(deck, seats, 0);

            var allCards = new List<Card>();
            foreach (var pair in result.Values)
            {
                allCards.Add(pair.Item1);
                allCards.Add(pair.Item2);
            }

            Assert.AreEqual(allCards.Count, new HashSet<Card>(allCards).Count,
                "모든 딜링된 카드는 고유해야 합니다.");
        }

        [Test]
        public void DealHoleCards_DealOrderStartsFromDealerNext()
        {
            // 덱을 셔플하지 않아 카드 순서가 예측 가능하도록 함
            var deck = new Deck();
            var seats = new List<int> { 0, 3, 6 };
            int dealerSeat = 3;

            // 딜링 순서: 딜러(3) 다음 → 6 → 0 → (2바퀴) 6 → 0 → 3 순서는 아님
            // 딜러 다음부터 시계방향: 6, 0, 3
            var result = HoleCardDealingUsecase.DealHoleCards(deck, seats, dealerSeat);

            // 3명에게 2장씩 = 6장 소비
            Assert.AreEqual(52 - 6, deck.Remaining);
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.ContainsKey(0));
            Assert.IsTrue(result.ContainsKey(3));
            Assert.IsTrue(result.ContainsKey(6));
        }

        [Test]
        public void DealHoleCards_HeadsUp_DealerNextIsOpponent()
        {
            var deck = new Deck();
            var seats = new List<int> { 2, 7 };
            int dealerSeat = 2;

            // 딜러(2) 다음 → 7, 그 다음 → 2
            var result = HoleCardDealingUsecase.DealHoleCards(deck, seats, dealerSeat);

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(52 - 4, deck.Remaining);
        }

        [Test]
        public void DealHoleCards_NullDeck_ThrowsArgumentNullException()
        {
            var seats = new List<int> { 0, 1 };

            Assert.Throws<ArgumentNullException>(() =>
                HoleCardDealingUsecase.DealHoleCards(null, seats, 0));
        }

        [Test]
        public void DealHoleCards_LessThan2Seats_ThrowsArgumentException()
        {
            var deck = CreateShuffledDeck();

            Assert.Throws<ArgumentException>(() =>
                HoleCardDealingUsecase.DealHoleCards(deck, new List<int> { 0 }, 0));
        }

        [Test]
        public void DealHoleCards_NullSeats_ThrowsArgumentException()
        {
            var deck = CreateShuffledDeck();

            Assert.Throws<ArgumentException>(() =>
                HoleCardDealingUsecase.DealHoleCards(deck, null, 0));
        }

        [Test]
        public void DealHoleCards_InsufficientCards_ThrowsInvalidOperationException()
        {
            var deck = new Deck();
            // 50장을 미리 뽑아서 2장만 남김
            for (int i = 0; i < 50; i++)
                deck.Draw();

            var seats = new List<int> { 0, 1, 2 }; // 3명 × 2 = 6장 필요

            Assert.Throws<InvalidOperationException>(() =>
                HoleCardDealingUsecase.DealHoleCards(deck, seats, 0));
        }

        [Test]
        public void DealHoleCards_FullTable10Players()
        {
            var deck = CreateShuffledDeck();
            var seats = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            var result = HoleCardDealingUsecase.DealHoleCards(deck, seats, 0);

            Assert.AreEqual(10, result.Count);
            Assert.AreEqual(52 - 20, deck.Remaining);
        }
    }
}
