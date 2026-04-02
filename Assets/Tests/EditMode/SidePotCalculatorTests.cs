// SidePotCalculatorTests.cs
// SidePotCalculator의 단위 테스트 모음 (EditMode).
// BuildPots: 올인 경계 기준 팟 분리, 단일 팟, 복수 올인, 폴드 플레이어 처리를 검증한다.
// DistributePots: 단독 승자, 동점 균등 분배, 나머지 칩 딜러 좌측 배분을 검증한다.

using System.Collections.Generic;
using NUnit.Framework;
using TexasHoldem.Entity;
using TexasHoldem.Usecase;

namespace TexasHoldem.Tests.EditMode
{
    [TestFixture]
    public class SidePotCalculatorTests
    {
        // ────────────────────────────────────────────────────────────────
        // BuildPots
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void BuildPots_NoBets_ReturnsEmpty()
        {
            var bets = new List<PlayerBetInfo>();
            var pots = SidePotCalculator.BuildPots(bets);
            Assert.AreEqual(0, pots.Count);
        }

        [Test]
        public void BuildPots_NoAllIn_SinglePot()
        {
            var bets = new List<PlayerBetInfo>
            {
                new PlayerBetInfo("0", 100, false, false),
                new PlayerBetInfo("1", 100, false, false),
                new PlayerBetInfo("2", 100, false, false)
            };

            var pots = SidePotCalculator.BuildPots(bets);

            Assert.AreEqual(1, pots.Count);
            Assert.AreEqual(300, pots[0].Amount);
            Assert.AreEqual(3, pots[0].EligiblePlayerIds.Count);
        }

        [Test]
        public void BuildPots_OneAllIn_CreatesSidePot()
        {
            // Player 0: 50 올인, Player 1: 100, Player 2: 100
            var bets = new List<PlayerBetInfo>
            {
                new PlayerBetInfo("0", 50, true, false),
                new PlayerBetInfo("1", 100, false, false),
                new PlayerBetInfo("2", 100, false, false)
            };

            var pots = SidePotCalculator.BuildPots(bets);

            Assert.AreEqual(2, pots.Count);

            // 메인 팟: 50 * 3 = 150, 3명 eligible
            Assert.AreEqual(150, pots[0].Amount);
            Assert.AreEqual(3, pots[0].EligiblePlayerIds.Count);

            // 사이드 팟: 50 * 2 = 100, 2명 eligible (Player 1, 2)
            Assert.AreEqual(100, pots[1].Amount);
            Assert.AreEqual(2, pots[1].EligiblePlayerIds.Count);
            Assert.IsFalse(pots[1].EligiblePlayerIds.Contains("0"));
        }

        [Test]
        public void BuildPots_TwoAllIns_ThreePots()
        {
            // Player 0: 30 올인, Player 1: 70 올인, Player 2: 100
            var bets = new List<PlayerBetInfo>
            {
                new PlayerBetInfo("0", 30, true, false),
                new PlayerBetInfo("1", 70, true, false),
                new PlayerBetInfo("2", 100, false, false)
            };

            var pots = SidePotCalculator.BuildPots(bets);

            Assert.AreEqual(3, pots.Count);

            // 메인 팟: 30 * 3 = 90
            Assert.AreEqual(90, pots[0].Amount);
            Assert.AreEqual(3, pots[0].EligiblePlayerIds.Count);

            // 사이드 팟 1: 40 * 2 = 80
            Assert.AreEqual(80, pots[1].Amount);
            Assert.AreEqual(2, pots[1].EligiblePlayerIds.Count);

            // 사이드 팟 2: 30 * 1 = 30
            Assert.AreEqual(30, pots[2].Amount);
            Assert.AreEqual(1, pots[2].EligiblePlayerIds.Count);
            Assert.IsTrue(pots[2].EligiblePlayerIds.Contains("2"));
        }

        [Test]
        public void BuildPots_FoldedPlayer_IncludedInAmountButNotEligible()
        {
            // Player 0: 50 폴드, Player 1: 100, Player 2: 100
            var bets = new List<PlayerBetInfo>
            {
                new PlayerBetInfo("0", 50, false, true),
                new PlayerBetInfo("1", 100, false, false),
                new PlayerBetInfo("2", 100, false, false)
            };

            var pots = SidePotCalculator.BuildPots(bets);

            // 올인 없으므로 단일 팟
            Assert.AreEqual(1, pots.Count);
            Assert.AreEqual(250, pots[0].Amount);
            // 폴드한 Player 0은 eligible 아님
            Assert.AreEqual(2, pots[0].EligiblePlayerIds.Count);
            Assert.IsFalse(pots[0].EligiblePlayerIds.Contains("0"));
        }

        [Test]
        public void BuildPots_AllInAndFolded_CorrectPots()
        {
            // Player 0: 30 올인, Player 1: 50 폴드, Player 2: 100
            var bets = new List<PlayerBetInfo>
            {
                new PlayerBetInfo("0", 30, true, false),
                new PlayerBetInfo("1", 50, false, true),
                new PlayerBetInfo("2", 100, false, false)
            };

            var pots = SidePotCalculator.BuildPots(bets);

            // 메인 팟: 30 * 3 = 90, eligible: 0, 2 (1은 폴드)
            Assert.AreEqual(90, pots[0].Amount);
            Assert.IsTrue(pots[0].EligiblePlayerIds.Contains("0"));
            Assert.IsFalse(pots[0].EligiblePlayerIds.Contains("1"));
            Assert.IsTrue(pots[0].EligiblePlayerIds.Contains("2"));

            // 사이드 팟에 폴드 플레이어의 잔액도 포함
            int totalAmount = 0;
            foreach (var pot in pots)
                totalAmount += pot.Amount;
            Assert.AreEqual(180, totalAmount); // 30 + 50 + 100
        }

        // ────────────────────────────────────────────────────────────────
        // DistributePots
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void DistributePots_SingleWinner_GetsFullPot()
        {
            var pots = new List<Pot>
            {
                new Pot(300, new List<string> { "0", "1", "2" })
            };

            var evaluations = new Dictionary<int, HandEvaluation>
            {
                { 0, new HandEvaluation(HandRank.OnePair, new List<int> { 10, 8, 7, 5 }) },
                { 1, new HandEvaluation(HandRank.TwoPair, new List<int> { 12, 9, 5 }) },
                { 2, new HandEvaluation(HandRank.HighCard, new List<int> { 14, 10, 8, 7, 5 }) }
            };

            var awards = SidePotCalculator.DistributePots(pots, evaluations, 0);

            int totalForWinner = 0;
            foreach (var award in awards)
            {
                Assert.AreEqual(1, award.SeatIndex);
                totalForWinner += award.Amount;
            }
            Assert.AreEqual(300, totalForWinner);
        }

        [Test]
        public void DistributePots_TiedWinners_EvenSplit()
        {
            var pots = new List<Pot>
            {
                new Pot(200, new List<string> { "0", "1" })
            };

            var evaluations = new Dictionary<int, HandEvaluation>
            {
                { 0, new HandEvaluation(HandRank.OnePair, new List<int> { 10, 8, 7, 5 }) },
                { 1, new HandEvaluation(HandRank.OnePair, new List<int> { 10, 8, 7, 5 }) }
            };

            var awards = SidePotCalculator.DistributePots(pots, evaluations, 5);

            // 200 / 2 = 100 each, no remainder
            Assert.AreEqual(2, awards.Count);
            Assert.AreEqual(100, awards[0].Amount);
            Assert.AreEqual(100, awards[1].Amount);
        }

        [Test]
        public void DistributePots_TiedWinners_RemainderGoesToDealerLeft()
        {
            var pots = new List<Pot>
            {
                new Pot(301, new List<string> { "2", "5", "8" })
            };

            var evaluations = new Dictionary<int, HandEvaluation>
            {
                { 2, new HandEvaluation(HandRank.Flush, new List<int> { 13, 10, 8, 7, 5 }) },
                { 5, new HandEvaluation(HandRank.Flush, new List<int> { 13, 10, 8, 7, 5 }) },
                { 8, new HandEvaluation(HandRank.HighCard, new List<int> { 14, 10, 8, 7, 5 }) }
            };

            // Dealer at seat 0, so dealer-left order: 1,2,3,4,5,...
            // Winners are seat 2 and 5. Seat 2 is closer to dealer left.
            var awards = SidePotCalculator.DistributePots(pots, evaluations, 0);

            // 301 / 2 = 150 each + 1 remainder to seat closer to dealer left (seat 2)
            int totalSeat2 = 0;
            int totalSeat5 = 0;
            foreach (var award in awards)
            {
                if (award.SeatIndex == 2) totalSeat2 += award.Amount;
                if (award.SeatIndex == 5) totalSeat5 += award.Amount;
            }
            Assert.AreEqual(151, totalSeat2);
            Assert.AreEqual(150, totalSeat5);
        }

        [Test]
        public void DistributePots_MultiplePots_CorrectLabels()
        {
            var pots = new List<Pot>
            {
                new Pot(150, new List<string> { "0", "1", "2" }),
                new Pot(100, new List<string> { "1", "2" })
            };

            var evaluations = new Dictionary<int, HandEvaluation>
            {
                { 0, new HandEvaluation(HandRank.ThreeOfAKind, new List<int> { 10, 8, 7 }) },
                { 1, new HandEvaluation(HandRank.OnePair, new List<int> { 12, 9, 5, 3 }) },
                { 2, new HandEvaluation(HandRank.HighCard, new List<int> { 14, 10, 8, 7, 5 }) }
            };

            var awards = SidePotCalculator.DistributePots(pots, evaluations, 0);

            // Main pot (150): Player 0 wins (ThreeOfAKind)
            // Side pot 1 (100): Player 1 wins (OnePair > HighCard)
            bool hasMainPot = false;
            bool hasSidePot = false;
            foreach (var award in awards)
            {
                if (award.PotLabel == "Main Pot")
                {
                    hasMainPot = true;
                    Assert.AreEqual(0, award.SeatIndex);
                    Assert.AreEqual(150, award.Amount);
                }
                if (award.PotLabel == "Side Pot 1")
                {
                    hasSidePot = true;
                    Assert.AreEqual(1, award.SeatIndex);
                    Assert.AreEqual(100, award.Amount);
                }
            }
            Assert.IsTrue(hasMainPot);
            Assert.IsTrue(hasSidePot);
        }

        [Test]
        public void DistributePots_EmptyPot_Skipped()
        {
            var pots = new List<Pot>
            {
                new Pot(0, new List<string> { "0", "1" }),
                new Pot(100, new List<string> { "0", "1" })
            };

            var evaluations = new Dictionary<int, HandEvaluation>
            {
                { 0, new HandEvaluation(HandRank.OnePair, new List<int> { 10 }) },
                { 1, new HandEvaluation(HandRank.HighCard, new List<int> { 14 }) }
            };

            var awards = SidePotCalculator.DistributePots(pots, evaluations, 0);

            Assert.AreEqual(1, awards.Count);
            Assert.AreEqual(0, awards[0].SeatIndex);
            Assert.AreEqual(100, awards[0].Amount);
        }

        [Test]
        public void DistributePots_NoEvaluationForEligible_Skipped()
        {
            var pots = new List<Pot>
            {
                new Pot(100, new List<string> { "0", "1" })
            };

            // Only player 1 has evaluation
            var evaluations = new Dictionary<int, HandEvaluation>
            {
                { 1, new HandEvaluation(HandRank.HighCard, new List<int> { 14 }) }
            };

            var awards = SidePotCalculator.DistributePots(pots, evaluations, 0);

            Assert.AreEqual(1, awards.Count);
            Assert.AreEqual(1, awards[0].SeatIndex);
            Assert.AreEqual(100, awards[0].Amount);
        }
    }
}
