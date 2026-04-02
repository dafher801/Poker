// TurnOrderResolverTests.cs
// TurnOrderResolver의 ResolveOrder 메서드를 검증하는 EditMode 테스트 모음.
// PreFlop/PostFlop 순서, 헤즈업 특수 규칙, Folded/AllIn 제외를 확인한다.

using System.Collections.Generic;
using NUnit.Framework;
using TexasHoldem.Entity;
using TexasHoldem.Usecase;

namespace TexasHoldem.Tests.EditMode
{
    [TestFixture]
    public class TurnOrderResolverTests
    {
        private TurnOrderResolver _resolver;

        [SetUp]
        public void SetUp()
        {
            _resolver = new TurnOrderResolver();
        }

        private PlayerData CreatePlayer(string id, int seatIndex, PlayerStatus status = PlayerStatus.Active)
        {
            var player = new PlayerData(id, id, 1000, seatIndex);
            player.Status = status;
            return player;
        }

        // === PreFlop 3인 이상 ===

        [Test]
        public void PreFlop_ThreePlayers_UTGFirst_BBLast()
        {
            // Dealer=0, SB=1, BB=2, UTG=0
            var players = new List<PlayerData>
            {
                CreatePlayer("P0", 0),
                CreatePlayer("P1", 1),
                CreatePlayer("P2", 2)
            };

            var order = _resolver.ResolveOrder(players, dealerIndex: 0, GamePhase.PreFlop);

            // UTG=0, SB=1, BB=2
            Assert.AreEqual(new List<int> { 0, 1, 2 }, order);
        }

        [Test]
        public void PreFlop_FourPlayers_DealerAt1()
        {
            // Dealer=1, SB=2, BB=3, UTG=0
            var players = new List<PlayerData>
            {
                CreatePlayer("P0", 0),
                CreatePlayer("P1", 1),
                CreatePlayer("P2", 2),
                CreatePlayer("P3", 3)
            };

            var order = _resolver.ResolveOrder(players, dealerIndex: 1, GamePhase.PreFlop);

            // UTG=0, Dealer=1, SB=2, BB=3
            Assert.AreEqual(new List<int> { 0, 1, 2, 3 }, order);
        }

        [Test]
        public void PreFlop_SixPlayers_DealerAt2()
        {
            // Dealer=2, SB=3, BB=4, UTG=5
            var players = new List<PlayerData>
            {
                CreatePlayer("P0", 0),
                CreatePlayer("P1", 1),
                CreatePlayer("P2", 2),
                CreatePlayer("P3", 3),
                CreatePlayer("P4", 4),
                CreatePlayer("P5", 5)
            };

            var order = _resolver.ResolveOrder(players, dealerIndex: 2, GamePhase.PreFlop);

            // UTG=5, 0, 1, Dealer=2, SB=3, BB=4
            Assert.AreEqual(new List<int> { 5, 0, 1, 2, 3, 4 }, order);
        }

        // === PostFlop 3인 이상 ===

        [Test]
        public void PostFlop_ThreePlayers_DealerNextFirst()
        {
            // Dealer=0, 다음=1부터 시작
            var players = new List<PlayerData>
            {
                CreatePlayer("P0", 0),
                CreatePlayer("P1", 1),
                CreatePlayer("P2", 2)
            };

            var order = _resolver.ResolveOrder(players, dealerIndex: 0, GamePhase.Flop);

            Assert.AreEqual(new List<int> { 1, 2, 0 }, order);
        }

        [Test]
        public void PostFlop_Turn_DealerAt3()
        {
            var players = new List<PlayerData>
            {
                CreatePlayer("P0", 0),
                CreatePlayer("P1", 1),
                CreatePlayer("P2", 2),
                CreatePlayer("P3", 3)
            };

            var order = _resolver.ResolveOrder(players, dealerIndex: 3, GamePhase.Turn);

            // Dealer=3, 다음=0부터
            Assert.AreEqual(new List<int> { 0, 1, 2, 3 }, order);
        }

        [Test]
        public void PostFlop_River_Works()
        {
            var players = new List<PlayerData>
            {
                CreatePlayer("P0", 0),
                CreatePlayer("P1", 1),
                CreatePlayer("P2", 2)
            };

            var order = _resolver.ResolveOrder(players, dealerIndex: 1, GamePhase.River);

            Assert.AreEqual(new List<int> { 2, 0, 1 }, order);
        }

        // === 헤즈업(2인) 특수 규칙 ===

        [Test]
        public void HeadsUp_PreFlop_DealerSBFirst()
        {
            // 헤즈업 PreFlop: 딜러(=SB)가 먼저
            var players = new List<PlayerData>
            {
                CreatePlayer("P0", 0),
                CreatePlayer("P1", 1)
            };

            var order = _resolver.ResolveOrder(players, dealerIndex: 0, GamePhase.PreFlop);

            // Dealer(SB)=0이 먼저, BB=1이 나중
            Assert.AreEqual(new List<int> { 0, 1 }, order);
        }

        [Test]
        public void HeadsUp_PostFlop_NonDealerFirst()
        {
            // 헤즈업 PostFlop: 딜러가 아닌 쪽이 먼저
            var players = new List<PlayerData>
            {
                CreatePlayer("P0", 0),
                CreatePlayer("P1", 1)
            };

            var order = _resolver.ResolveOrder(players, dealerIndex: 0, GamePhase.Flop);

            // Non-dealer=1이 먼저, Dealer=0이 나중
            Assert.AreEqual(new List<int> { 1, 0 }, order);
        }

        // === Folded/AllIn 제외 ===

        [Test]
        public void FoldedPlayer_Excluded()
        {
            var players = new List<PlayerData>
            {
                CreatePlayer("P0", 0),
                CreatePlayer("P1", 1, PlayerStatus.Folded),
                CreatePlayer("P2", 2),
                CreatePlayer("P3", 3)
            };

            var order = _resolver.ResolveOrder(players, dealerIndex: 0, GamePhase.Flop);

            // P1(Folded) 제외
            Assert.AreEqual(new List<int> { 2, 3, 0 }, order);
        }

        [Test]
        public void AllInPlayer_Excluded()
        {
            var players = new List<PlayerData>
            {
                CreatePlayer("P0", 0),
                CreatePlayer("P1", 1),
                CreatePlayer("P2", 2, PlayerStatus.AllIn),
                CreatePlayer("P3", 3)
            };

            var order = _resolver.ResolveOrder(players, dealerIndex: 0, GamePhase.Flop);

            // P2(AllIn) 제외
            Assert.AreEqual(new List<int> { 1, 3, 0 }, order);
        }

        [Test]
        public void MultipleInactive_Excluded()
        {
            var players = new List<PlayerData>
            {
                CreatePlayer("P0", 0, PlayerStatus.Folded),
                CreatePlayer("P1", 1),
                CreatePlayer("P2", 2, PlayerStatus.AllIn),
                CreatePlayer("P3", 3),
                CreatePlayer("P4", 4, PlayerStatus.Folded)
            };

            var order = _resolver.ResolveOrder(players, dealerIndex: 0, GamePhase.PreFlop);

            // Dealer=0, SB=1, BB=2, UTG=3
            // Active만: P1, P3
            // UTG=3부터: 3, 1
            Assert.AreEqual(new List<int> { 3, 1 }, order);
        }

        [Test]
        public void PreFlop_FoldedPlayersSkipped_OrderPreserved()
        {
            // 6인 중 2명 폴드
            var players = new List<PlayerData>
            {
                CreatePlayer("P0", 0),
                CreatePlayer("P1", 1, PlayerStatus.Folded),
                CreatePlayer("P2", 2),
                CreatePlayer("P3", 3),
                CreatePlayer("P4", 4, PlayerStatus.Folded),
                CreatePlayer("P5", 5)
            };

            var order = _resolver.ResolveOrder(players, dealerIndex: 2, GamePhase.PreFlop);

            // Dealer=2, SB=3, BB=4(Folded), UTG=5
            // 순서: 5, 0, 2, 3 (P1, P4는 Folded라 제외)
            Assert.AreEqual(new List<int> { 5, 0, 2, 3 }, order);
        }

        [Test]
        public void AllPlayersExceptOne_ReturnsOnlyActive()
        {
            var players = new List<PlayerData>
            {
                CreatePlayer("P0", 0, PlayerStatus.Folded),
                CreatePlayer("P1", 1, PlayerStatus.Folded),
                CreatePlayer("P2", 2),
                CreatePlayer("P3", 3, PlayerStatus.AllIn)
            };

            var order = _resolver.ResolveOrder(players, dealerIndex: 0, GamePhase.Flop);

            Assert.AreEqual(new List<int> { 2 }, order);
        }
    }
}
