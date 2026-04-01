// HandEvaluatorTest.cs
// HandEvaluator의 족보 판정, 최선 핸드 선택, 비교 로직을 검증하는 EditMode 테스트.
// Unity Test Framework(NUnit) — Test Runner > EditMode 탭에서 실행한다.

using System.Collections.Generic;
using NUnit.Framework;
using TexasHoldem.Entity;
using TexasHoldem.Usecase;

namespace TexasHoldem.Tests.EditMode
{
    [TestFixture]
    public class HandEvaluatorTest
    {
        // ────────────────────────────────────────────────────────────────
        // 헬퍼
        // ────────────────────────────────────────────────────────────────

        private static Card C(Suit s, Rank r) => new Card(s, r);

        // ────────────────────────────────────────────────────────────────
        // (1) 10가지 족보 각각 정확한 판정
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Evaluate_HighCard()
        {
            var hand = new List<Card>
            {
                C(Suit.Spade,   Rank.Two),
                C(Suit.Heart,   Rank.Four),
                C(Suit.Diamond, Rank.Six),
                C(Suit.Club,    Rank.Eight),
                C(Suit.Spade,   Rank.King)
            };
            var result = HandEvaluator.Evaluate(hand);
            Assert.AreEqual(HandRank.HighCard, result.Rank);
        }

        [Test]
        public void Evaluate_OnePair()
        {
            var hand = new List<Card>
            {
                C(Suit.Spade,   Rank.Ace),
                C(Suit.Heart,   Rank.Ace),
                C(Suit.Diamond, Rank.Two),
                C(Suit.Club,    Rank.Four),
                C(Suit.Spade,   Rank.Six)
            };
            var result = HandEvaluator.Evaluate(hand);
            Assert.AreEqual(HandRank.OnePair, result.Rank);
        }

        [Test]
        public void Evaluate_TwoPair()
        {
            var hand = new List<Card>
            {
                C(Suit.Spade,   Rank.Ace),
                C(Suit.Heart,   Rank.Ace),
                C(Suit.Diamond, Rank.King),
                C(Suit.Club,    Rank.King),
                C(Suit.Spade,   Rank.Two)
            };
            var result = HandEvaluator.Evaluate(hand);
            Assert.AreEqual(HandRank.TwoPair, result.Rank);
        }

        [Test]
        public void Evaluate_ThreeOfAKind()
        {
            var hand = new List<Card>
            {
                C(Suit.Spade,   Rank.Seven),
                C(Suit.Heart,   Rank.Seven),
                C(Suit.Diamond, Rank.Seven),
                C(Suit.Club,    Rank.Two),
                C(Suit.Spade,   Rank.Three)
            };
            var result = HandEvaluator.Evaluate(hand);
            Assert.AreEqual(HandRank.ThreeOfAKind, result.Rank);
        }

        [Test]
        public void Evaluate_Straight()
        {
            var hand = new List<Card>
            {
                C(Suit.Spade,   Rank.Five),
                C(Suit.Heart,   Rank.Six),
                C(Suit.Diamond, Rank.Seven),
                C(Suit.Club,    Rank.Eight),
                C(Suit.Spade,   Rank.Nine)
            };
            var result = HandEvaluator.Evaluate(hand);
            Assert.AreEqual(HandRank.Straight, result.Rank);
        }

        [Test]
        public void Evaluate_Flush()
        {
            var hand = new List<Card>
            {
                C(Suit.Heart, Rank.Two),
                C(Suit.Heart, Rank.Four),
                C(Suit.Heart, Rank.Six),
                C(Suit.Heart, Rank.Eight),
                C(Suit.Heart, Rank.King)
            };
            var result = HandEvaluator.Evaluate(hand);
            Assert.AreEqual(HandRank.Flush, result.Rank);
        }

        [Test]
        public void Evaluate_FullHouse()
        {
            var hand = new List<Card>
            {
                C(Suit.Spade,   Rank.King),
                C(Suit.Heart,   Rank.King),
                C(Suit.Diamond, Rank.King),
                C(Suit.Club,    Rank.Ace),
                C(Suit.Spade,   Rank.Ace)
            };
            var result = HandEvaluator.Evaluate(hand);
            Assert.AreEqual(HandRank.FullHouse, result.Rank);
        }

        [Test]
        public void Evaluate_FourOfAKind()
        {
            var hand = new List<Card>
            {
                C(Suit.Spade,   Rank.Nine),
                C(Suit.Heart,   Rank.Nine),
                C(Suit.Diamond, Rank.Nine),
                C(Suit.Club,    Rank.Nine),
                C(Suit.Spade,   Rank.Two)
            };
            var result = HandEvaluator.Evaluate(hand);
            Assert.AreEqual(HandRank.FourOfAKind, result.Rank);
        }

        [Test]
        public void Evaluate_StraightFlush()
        {
            var hand = new List<Card>
            {
                C(Suit.Club, Rank.Five),
                C(Suit.Club, Rank.Six),
                C(Suit.Club, Rank.Seven),
                C(Suit.Club, Rank.Eight),
                C(Suit.Club, Rank.Nine)
            };
            var result = HandEvaluator.Evaluate(hand);
            Assert.AreEqual(HandRank.StraightFlush, result.Rank);
        }

        [Test]
        public void Evaluate_RoyalFlush()
        {
            var hand = new List<Card>
            {
                C(Suit.Diamond, Rank.Ten),
                C(Suit.Diamond, Rank.Jack),
                C(Suit.Diamond, Rank.Queen),
                C(Suit.Diamond, Rank.King),
                C(Suit.Diamond, Rank.Ace)
            };
            var result = HandEvaluator.Evaluate(hand);
            Assert.AreEqual(HandRank.RoyalFlush, result.Rank);
        }

        // ────────────────────────────────────────────────────────────────
        // (2) Ace-low 스트레이트 (wheel): A-2-3-4-5 → TieBreakers[0] == 5
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Evaluate_AceLowStraight_HighCardIs5()
        {
            var hand = new List<Card>
            {
                C(Suit.Spade,   Rank.Ace),
                C(Suit.Heart,   Rank.Two),
                C(Suit.Diamond, Rank.Three),
                C(Suit.Club,    Rank.Four),
                C(Suit.Spade,   Rank.Five)
            };
            var result = HandEvaluator.Evaluate(hand);
            Assert.AreEqual(HandRank.Straight,  result.Rank);
            Assert.AreEqual(5, result.TieBreakers[0]);
        }

        // ────────────────────────────────────────────────────────────────
        // (3) 7장에서 최선 5장 선택 — FullHouse vs OnePair 동시 가능
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Evaluate_7Cards_SelectsBestHand_FullHouse()
        {
            // K K K A A + 2 3 → FullHouse(K full of A) 선택되어야 함
            var cards = new List<Card>
            {
                C(Suit.Spade,   Rank.King),
                C(Suit.Heart,   Rank.King),
                C(Suit.Diamond, Rank.King),
                C(Suit.Club,    Rank.Ace),
                C(Suit.Spade,   Rank.Ace),
                C(Suit.Heart,   Rank.Two),
                C(Suit.Diamond, Rank.Three)
            };
            var result = HandEvaluator.Evaluate(cards);
            Assert.AreEqual(HandRank.FullHouse, result.Rank);
        }

        // ────────────────────────────────────────────────────────────────
        // (4) 동일 족보 키커 비교
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Compare_OnePair_HigherPairWins()
        {
            // K 페어 vs Q 페어 → K 페어 승
            var handK = HandEvaluator.Evaluate(new List<Card>
            {
                C(Suit.Spade,   Rank.King),
                C(Suit.Heart,   Rank.King),
                C(Suit.Diamond, Rank.Two),
                C(Suit.Club,    Rank.Three),
                C(Suit.Spade,   Rank.Four)
            });
            var handQ = HandEvaluator.Evaluate(new List<Card>
            {
                C(Suit.Spade,   Rank.Queen),
                C(Suit.Heart,   Rank.Queen),
                C(Suit.Diamond, Rank.Two),
                C(Suit.Club,    Rank.Three),
                C(Suit.Spade,   Rank.Four)
            });
            Assert.Greater(HandEvaluator.Compare(handK, handQ), 0);
        }

        [Test]
        public void Compare_TwoPair_HigherSecondPairWins()
        {
            // AA-KK vs AA-QQ → AA-KK 승
            var handKK = HandEvaluator.Evaluate(new List<Card>
            {
                C(Suit.Spade,   Rank.Ace),
                C(Suit.Heart,   Rank.Ace),
                C(Suit.Diamond, Rank.King),
                C(Suit.Club,    Rank.King),
                C(Suit.Spade,   Rank.Two)
            });
            var handQQ = HandEvaluator.Evaluate(new List<Card>
            {
                C(Suit.Spade,   Rank.Ace),
                C(Suit.Heart,   Rank.Ace),
                C(Suit.Diamond, Rank.Queen),
                C(Suit.Club,    Rank.Queen),
                C(Suit.Spade,   Rank.Two)
            });
            Assert.Greater(HandEvaluator.Compare(handKK, handQQ), 0);
        }

        // ────────────────────────────────────────────────────────────────
        // (5) 완전 동점 — 수트만 다른 동일 5장
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Compare_IdenticalRanks_DifferentSuits_ReturnsTie()
        {
            var handA = HandEvaluator.Evaluate(new List<Card>
            {
                C(Suit.Spade,   Rank.Ace),
                C(Suit.Spade,   Rank.King),
                C(Suit.Spade,   Rank.Queen),
                C(Suit.Spade,   Rank.Jack),
                C(Suit.Spade,   Rank.Nine)
            });
            var handB = HandEvaluator.Evaluate(new List<Card>
            {
                C(Suit.Heart,   Rank.Ace),
                C(Suit.Heart,   Rank.King),
                C(Suit.Heart,   Rank.Queen),
                C(Suit.Heart,   Rank.Jack),
                C(Suit.Heart,   Rank.Nine)
            });
            // 둘 다 Flush이지만 TieBreakers 동일 → Compare == 0
            Assert.AreEqual(0, HandEvaluator.Compare(handA, handB));
        }
    }
}
