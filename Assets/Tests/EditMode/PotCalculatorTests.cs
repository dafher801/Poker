// PotCalculatorTests.cs
// PotCalculator 유스케이스의 단위 테스트 모음 (EditMode).
// 올인 없는 단순 케이스, 1명 올인 사이드 팟, 복수 올인 복합 사이드 팟,
// 폴드 플레이어 처리, 전원 동일 금액 올인 시나리오를 검증한다.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TexasHoldem.Entity;
using TexasHoldem.Usecase;

namespace TexasHoldem.Tests.EditMode
{
    [TestFixture]
    public class PotCalculatorTests
    {
        // ────────────────────────────────────────────────────────────────
        // (1) 올인 없이 3명이 동일 금액 베팅 → 단일 메인 팟
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void CalculatePots_NoAllIn_ThreePlayersEqualBet_SingleMainPot()
        {
            var bets = new List<PlayerBetInfo>
            {
                new PlayerBetInfo("P1", 100, false, false),
                new PlayerBetInfo("P2", 100, false, false),
                new PlayerBetInfo("P3", 100, false, false)
            };

            List<Pot> pots = PotCalculator.CalculatePots(bets);

            Assert.AreEqual(1, pots.Count);
            Assert.AreEqual(300, pots[0].Amount);
            Assert.AreEqual(3, pots[0].EligiblePlayerIds.Count);
            Assert.IsTrue(pots[0].EligiblePlayerIds.Contains("P1"));
            Assert.IsTrue(pots[0].EligiblePlayerIds.Contains("P2"));
            Assert.IsTrue(pots[0].EligiblePlayerIds.Contains("P3"));
        }

        // ────────────────────────────────────────────────────────────────
        // (2) 1명 올인(낮은 금액) + 2명 콜 → 메인 팟 + 사이드 팟
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void CalculatePots_OneAllIn_TwoCalls_MainAndSidePot()
        {
            // P1: 올인 50, P2: 콜 100, P3: 콜 100
            var bets = new List<PlayerBetInfo>
            {
                new PlayerBetInfo("P1", 50, true, false),
                new PlayerBetInfo("P2", 100, false, false),
                new PlayerBetInfo("P3", 100, false, false)
            };

            List<Pot> pots = PotCalculator.CalculatePots(bets);

            Assert.AreEqual(2, pots.Count);

            // 메인 팟: 50 * 3 = 150, P1/P2/P3 참여
            Pot mainPot = pots[0];
            Assert.AreEqual(150, mainPot.Amount);
            Assert.AreEqual(3, mainPot.EligiblePlayerIds.Count);
            Assert.IsTrue(mainPot.EligiblePlayerIds.Contains("P1"));
            Assert.IsTrue(mainPot.EligiblePlayerIds.Contains("P2"));
            Assert.IsTrue(mainPot.EligiblePlayerIds.Contains("P3"));

            // 사이드 팟: 50 * 2 = 100, P2/P3만 참여
            Pot sidePot = pots[1];
            Assert.AreEqual(100, sidePot.Amount);
            Assert.AreEqual(2, sidePot.EligiblePlayerIds.Count);
            Assert.IsTrue(sidePot.EligiblePlayerIds.Contains("P2"));
            Assert.IsTrue(sidePot.EligiblePlayerIds.Contains("P3"));
            Assert.IsFalse(sidePot.EligiblePlayerIds.Contains("P1"));
        }

        // ────────────────────────────────────────────────────────────────
        // (3) 3명 서로 다른 금액으로 올인 → 메인 팟 + 복수 사이드 팟
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void CalculatePots_ThreeAllIns_DifferentAmounts_MultipleSidePots()
        {
            // P1: 올인 30, P2: 올인 70, P3: 올인 150
            var bets = new List<PlayerBetInfo>
            {
                new PlayerBetInfo("P1", 30, true, false),
                new PlayerBetInfo("P2", 70, true, false),
                new PlayerBetInfo("P3", 150, true, false)
            };

            List<Pot> pots = PotCalculator.CalculatePots(bets);

            Assert.AreEqual(3, pots.Count);

            // 메인 팟: 30 * 3 = 90, P1/P2/P3
            Assert.AreEqual(90, pots[0].Amount);
            Assert.AreEqual(3, pots[0].EligiblePlayerIds.Count);
            Assert.IsTrue(pots[0].EligiblePlayerIds.Contains("P1"));
            Assert.IsTrue(pots[0].EligiblePlayerIds.Contains("P2"));
            Assert.IsTrue(pots[0].EligiblePlayerIds.Contains("P3"));

            // 사이드 팟 1: (70-30) * 2 = 80, P2/P3
            Assert.AreEqual(80, pots[1].Amount);
            Assert.AreEqual(2, pots[1].EligiblePlayerIds.Count);
            Assert.IsTrue(pots[1].EligiblePlayerIds.Contains("P2"));
            Assert.IsTrue(pots[1].EligiblePlayerIds.Contains("P3"));
            Assert.IsFalse(pots[1].EligiblePlayerIds.Contains("P1"));

            // 사이드 팟 2: (150-70) * 1 = 80, P3만
            Assert.AreEqual(80, pots[2].Amount);
            Assert.AreEqual(1, pots[2].EligiblePlayerIds.Count);
            Assert.IsTrue(pots[2].EligiblePlayerIds.Contains("P3"));
        }

        // ────────────────────────────────────────────────────────────────
        // (4) 폴드 플레이어: 베팅액은 팟에 포함, EligiblePlayerIds에서 제외
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void CalculatePots_FoldedPlayer_BetIncludedButNotEligible()
        {
            // P1: 폴드(50 베팅 후 폴드), P2: 콜 100, P3: 콜 100
            var bets = new List<PlayerBetInfo>
            {
                new PlayerBetInfo("P1", 50, false, true),
                new PlayerBetInfo("P2", 100, false, false),
                new PlayerBetInfo("P3", 100, false, false)
            };

            List<Pot> pots = PotCalculator.CalculatePots(bets);

            // 총 금액: 50 + 100 + 100 = 250
            int totalAmount = pots.Sum(p => p.Amount);
            Assert.AreEqual(250, totalAmount);

            // P1은 어떤 팟의 EligiblePlayerIds에도 포함되지 않아야 한다
            foreach (var pot in pots)
            {
                Assert.IsFalse(pot.EligiblePlayerIds.Contains("P1"),
                    "폴드한 플레이어 P1은 EligiblePlayerIds에 포함되면 안 된다.");
            }

            // P2, P3는 참여 자격이 있어야 한다
            bool p2Eligible = pots.Any(p => p.EligiblePlayerIds.Contains("P2"));
            bool p3Eligible = pots.Any(p => p.EligiblePlayerIds.Contains("P3"));
            Assert.IsTrue(p2Eligible);
            Assert.IsTrue(p3Eligible);
        }

        // ────────────────────────────────────────────────────────────────
        // (5) 전원 동일 금액 올인 → 단일 팟
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void CalculatePots_AllPlayersAllInSameAmount_SinglePot()
        {
            var bets = new List<PlayerBetInfo>
            {
                new PlayerBetInfo("P1", 200, true, false),
                new PlayerBetInfo("P2", 200, true, false),
                new PlayerBetInfo("P3", 200, true, false)
            };

            List<Pot> pots = PotCalculator.CalculatePots(bets);

            Assert.AreEqual(1, pots.Count);
            Assert.AreEqual(600, pots[0].Amount);
            Assert.AreEqual(3, pots[0].EligiblePlayerIds.Count);
            Assert.IsTrue(pots[0].EligiblePlayerIds.Contains("P1"));
            Assert.IsTrue(pots[0].EligiblePlayerIds.Contains("P2"));
            Assert.IsTrue(pots[0].EligiblePlayerIds.Contains("P3"));
        }
    }
}
