// PotManagerTests.cs
// PotManager의 CollectBets, CalculateSidePots, DistributePots 메서드를 검증하는 EditMode 테스트 모음.
// 베팅 수집, 사이드 팟 분리, 승자 분배가 올바르게 동작하는지 확인한다.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TexasHoldem.Entity;
using TexasHoldem.Usecase;

namespace TexasHoldem.Tests.EditMode
{
    [TestFixture]
    public class PotManagerTests
    {
        private PotManager _potManager;

        [SetUp]
        public void SetUp()
        {
            _potManager = new PotManager();
        }

        private GameState CreateGameState(List<PlayerData> players, int bigBlind = 100, int dealerIndex = 0)
        {
            var blinds = new BlindInfo(bigBlind / 2, bigBlind);
            var state = new GameState(players, blinds, dealerIndex);
            return state;
        }

        private PlayerData CreatePlayer(string id, int chips, int currentBet = 0,
            PlayerStatus status = PlayerStatus.Active, int seatIndex = 0)
        {
            var player = new PlayerData(id, id, chips, seatIndex)
            {
                CurrentBet = currentBet,
                Status = status
            };
            return player;
        }

        // ────────────────────────────────────────────────────────────────
        // CollectBets: 3명이 각각 100씩 베팅 → 팟 300, 모든 CurrentBet 0으로 초기화
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void CollectBets_ThreePlayersEach100_PotIs300AndBetsReset()
        {
            var players = new List<PlayerData>
            {
                CreatePlayer("P1", chips: 900, currentBet: 100, seatIndex: 0),
                CreatePlayer("P2", chips: 900, currentBet: 100, seatIndex: 1),
                CreatePlayer("P3", chips: 900, currentBet: 100, seatIndex: 2)
            };
            var state = CreateGameState(players);

            _potManager.CollectBets(state);

            Assert.AreEqual(1, state.Pots.Count, "팟은 1개여야 한다");
            Assert.AreEqual(300, state.Pots[0].Amount, "팟 금액은 300이어야 한다");
            foreach (var player in state.Players)
            {
                Assert.AreEqual(0, player.CurrentBet, $"{player.Id}의 CurrentBet은 0이어야 한다");
            }
        }

        // ────────────────────────────────────────────────────────────────
        // CalculateSidePots 올인 없음: 단일 메인 팟 유지
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void CalculateSidePots_NoAllIn_SingleMainPot()
        {
            var players = new List<PlayerData>
            {
                CreatePlayer("P1", chips: 900, currentBet: 100, seatIndex: 0),
                CreatePlayer("P2", chips: 900, currentBet: 100, seatIndex: 1),
                CreatePlayer("P3", chips: 900, currentBet: 100, seatIndex: 2)
            };
            var state = CreateGameState(players);

            _potManager.CalculateSidePots(state);

            Assert.AreEqual(1, state.Pots.Count, "올인이 없으면 단일 메인 팟이어야 한다");
            Assert.AreEqual(300, state.Pots[0].Amount);
            foreach (var player in state.Players)
            {
                Assert.AreEqual(0, player.CurrentBet, $"{player.Id}의 CurrentBet은 0이어야 한다");
            }
        }

        // ────────────────────────────────────────────────────────────────
        // CalculateSidePots 3명 서로 다른 올인: A=50, B=150, C=300
        // 메인 팟(50×3=150, 자격 A·B·C), 사이드 팟1(100×2=200, 자격 B·C),
        // 사이드 팟2(150×1=150, 자격 C)
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void CalculateSidePots_ThreeDifferentAllIns_CreatesCorrectPots()
        {
            var players = new List<PlayerData>
            {
                CreatePlayer("A", chips: 0, currentBet: 50, status: PlayerStatus.AllIn, seatIndex: 0),
                CreatePlayer("B", chips: 0, currentBet: 150, status: PlayerStatus.AllIn, seatIndex: 1),
                CreatePlayer("C", chips: 0, currentBet: 300, status: PlayerStatus.AllIn, seatIndex: 2)
            };
            var state = CreateGameState(players);

            _potManager.CalculateSidePots(state);

            Assert.AreEqual(3, state.Pots.Count, "3개의 팟이 생성되어야 한다");

            // 메인 팟: 50 × 3 = 150, 자격 A·B·C
            Assert.AreEqual(150, state.Pots[0].Amount, "메인 팟은 150이어야 한다");
            Assert.AreEqual(3, state.Pots[0].EligiblePlayerIds.Count);
            Assert.Contains("A", state.Pots[0].EligiblePlayerIds);
            Assert.Contains("B", state.Pots[0].EligiblePlayerIds);
            Assert.Contains("C", state.Pots[0].EligiblePlayerIds);

            // 사이드 팟 1: (150-50) × 2 = 200, 자격 B·C
            Assert.AreEqual(200, state.Pots[1].Amount, "사이드 팟 1은 200이어야 한다");
            Assert.AreEqual(2, state.Pots[1].EligiblePlayerIds.Count);
            Assert.Contains("B", state.Pots[1].EligiblePlayerIds);
            Assert.Contains("C", state.Pots[1].EligiblePlayerIds);

            // 사이드 팟 2: (300-150) × 1 = 150, 자격 C
            Assert.AreEqual(150, state.Pots[2].Amount, "사이드 팟 2는 150이어야 한다");
            Assert.AreEqual(1, state.Pots[2].EligiblePlayerIds.Count);
            Assert.Contains("C", state.Pots[2].EligiblePlayerIds);
        }

        // ────────────────────────────────────────────────────────────────
        // DistributePots 메인 팟 단일 승자: 정확한 금액 지급
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void DistributePots_SingleWinner_GetsFullAmount()
        {
            var pots = new List<Pot>
            {
                new Pot(300, new List<string> { "P1", "P2", "P3" })
            };
            var evaluations = new Dictionary<string, HandEvaluation>
            {
                { "P1", new HandEvaluation(HandRank.OnePair, new List<int> { 10, 9, 7 }) },
                { "P2", new HandEvaluation(HandRank.Flush, new List<int> { 13, 10, 9, 7, 4 }) },
                { "P3", new HandEvaluation(HandRank.HighCard, new List<int> { 14, 10, 8, 5, 3 }) }
            };

            var payouts = _potManager.DistributePots(pots, evaluations);

            Assert.AreEqual(1, payouts.Count, "승자는 1명이어야 한다");
            Assert.AreEqual("P2", payouts[0].PlayerId);
            Assert.AreEqual(300, payouts[0].Amount, "승자는 팟 전체 금액을 받아야 한다");
        }

        // ────────────────────────────────────────────────────────────────
        // DistributePots 사이드 팟 포함: 각 팟별 별도 승자 지급
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void DistributePots_WithSidePots_EachPotPaidToItsWinner()
        {
            // 메인 팟: A가 승리, 사이드 팟: B가 승리
            var pots = new List<Pot>
            {
                new Pot(150, new List<string> { "A", "B", "C" }),
                new Pot(200, new List<string> { "B", "C" })
            };
            var evaluations = new Dictionary<string, HandEvaluation>
            {
                // A가 가장 강함 (메인 팟에서 승리)
                { "A", new HandEvaluation(HandRank.Flush, new List<int> { 14, 13, 10, 9, 7 }) },
                // B가 C보다 강함 (사이드 팟에서 승리)
                { "B", new HandEvaluation(HandRank.TwoPair, new List<int> { 13, 10, 9 }) },
                { "C", new HandEvaluation(HandRank.OnePair, new List<int> { 10, 9, 7 }) }
            };

            var payouts = _potManager.DistributePots(pots, evaluations);

            var payoutDict = payouts.ToDictionary(p => p.PlayerId, p => p.Amount);
            Assert.AreEqual(150, payoutDict["A"], "A는 메인 팟 150을 받아야 한다");
            Assert.AreEqual(200, payoutDict["B"], "B는 사이드 팟 200을 받아야 한다");
            Assert.IsFalse(payoutDict.ContainsKey("C"), "C는 분배받지 못해야 한다");
        }

        // ────────────────────────────────────────────────────────────────
        // DistributePots 동점(스플릿 팟): 균등 분배 및 나머지 칩 딜러 근접 플레이어 지급
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void DistributePots_Tie_SplitEvenlyWithRemainderToFirstPlayer()
        {
            // 팟 301을 3명이 동점으로 나눔 → 100씩, 나머지 1칩은 첫 번째 플레이어에게
            var pots = new List<Pot>
            {
                new Pot(301, new List<string> { "P1", "P2", "P3" })
            };
            var evaluations = new Dictionary<string, HandEvaluation>
            {
                { "P1", new HandEvaluation(HandRank.OnePair, new List<int> { 10, 9, 7 }) },
                { "P2", new HandEvaluation(HandRank.OnePair, new List<int> { 10, 9, 7 }) },
                { "P3", new HandEvaluation(HandRank.OnePair, new List<int> { 10, 9, 7 }) }
            };

            var payouts = _potManager.DistributePots(pots, evaluations);

            var payoutDict = payouts.ToDictionary(p => p.PlayerId, p => p.Amount);
            Assert.AreEqual(3, payoutDict.Count, "동점 시 3명 모두 분배받아야 한다");

            int total = payoutDict.Values.Sum();
            Assert.AreEqual(301, total, "총 분배 금액은 팟 금액과 같아야 한다");

            // P1이 포지션 인덱스가 가장 낮으므로 나머지 1칩 추가
            Assert.AreEqual(101, payoutDict["P1"], "P1(첫 번째)은 100 + 나머지 1 = 101을 받아야 한다");
            Assert.AreEqual(100, payoutDict["P2"], "P2는 균등 분배 100을 받아야 한다");
            Assert.AreEqual(100, payoutDict["P3"], "P3는 균등 분배 100을 받아야 한다");
        }
    }
}
