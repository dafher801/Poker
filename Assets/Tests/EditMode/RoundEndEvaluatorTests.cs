// RoundEndEvaluatorTests.cs
// RoundEndEvaluator의 IsBettingRoundComplete, IsOnlyOnePlayerRemaining, ShouldSkipToShowdown을 검증하는 EditMode 테스트.

using System.Collections.Generic;
using NUnit.Framework;
using TexasHoldem.Entity;
using TexasHoldem.Usecase;

namespace TexasHoldem.Tests.EditMode
{
    [TestFixture]
    public class RoundEndEvaluatorTests
    {
        private RoundEndEvaluator _evaluator;

        [SetUp]
        public void SetUp()
        {
            _evaluator = new RoundEndEvaluator();
        }

        private PlayerData CreatePlayer(string id, int seatIndex, PlayerStatus status, int chips = 1000, int currentBet = 0)
        {
            var player = new PlayerData(id, id, chips, seatIndex);
            player.Status = status;
            player.CurrentBet = currentBet;
            return player;
        }

        // === IsBettingRoundComplete ===

        [Test]
        public void IsBettingRoundComplete_AllActedAndBetsEqual_ReturnsTrue()
        {
            var players = new List<PlayerData>
            {
                CreatePlayer("p0", 0, PlayerStatus.Active, currentBet: 100),
                CreatePlayer("p1", 1, PlayerStatus.Active, currentBet: 100),
                CreatePlayer("p2", 2, PlayerStatus.Active, currentBet: 100)
            };
            var hasActed = new bool[] { true, true, true };

            Assert.IsTrue(_evaluator.IsBettingRoundComplete(players, 100, hasActed));
        }

        [Test]
        public void IsBettingRoundComplete_PlayerNotActed_ReturnsFalse()
        {
            var players = new List<PlayerData>
            {
                CreatePlayer("p0", 0, PlayerStatus.Active, currentBet: 100),
                CreatePlayer("p1", 1, PlayerStatus.Active, currentBet: 100),
                CreatePlayer("p2", 2, PlayerStatus.Active, currentBet: 0)
            };
            var hasActed = new bool[] { true, true, false };

            Assert.IsFalse(_evaluator.IsBettingRoundComplete(players, 100, hasActed));
        }

        [Test]
        public void IsBettingRoundComplete_BetsNotEqual_ReturnsFalse()
        {
            var players = new List<PlayerData>
            {
                CreatePlayer("p0", 0, PlayerStatus.Active, currentBet: 100),
                CreatePlayer("p1", 1, PlayerStatus.Active, currentBet: 50),
                CreatePlayer("p2", 2, PlayerStatus.Active, currentBet: 100)
            };
            var hasActed = new bool[] { true, true, true };

            Assert.IsFalse(_evaluator.IsBettingRoundComplete(players, 100, hasActed));
        }

        [Test]
        public void IsBettingRoundComplete_FoldedAndAllInPlayersIgnored()
        {
            var players = new List<PlayerData>
            {
                CreatePlayer("p0", 0, PlayerStatus.Active, currentBet: 200),
                CreatePlayer("p1", 1, PlayerStatus.Folded, currentBet: 50),
                CreatePlayer("p2", 2, PlayerStatus.AllIn, currentBet: 150),
                CreatePlayer("p3", 3, PlayerStatus.Active, currentBet: 200)
            };
            var hasActed = new bool[] { true, true, true, true };

            Assert.IsTrue(_evaluator.IsBettingRoundComplete(players, 200, hasActed));
        }

        [Test]
        public void IsBettingRoundComplete_NoActivePlayers_ReturnsTrue()
        {
            var players = new List<PlayerData>
            {
                CreatePlayer("p0", 0, PlayerStatus.AllIn, currentBet: 500),
                CreatePlayer("p1", 1, PlayerStatus.Folded, currentBet: 0)
            };
            var hasActed = new bool[] { true, true };

            Assert.IsTrue(_evaluator.IsBettingRoundComplete(players, 500, hasActed));
        }

        [Test]
        public void IsBettingRoundComplete_PreFlopBBOption_NotActedReturnsFalse()
        {
            // BB가 아직 행동하지 않은 경우 (다른 사람이 모두 콜)
            var players = new List<PlayerData>
            {
                CreatePlayer("p0", 0, PlayerStatus.Active, currentBet: 100),
                CreatePlayer("p1", 1, PlayerStatus.Active, currentBet: 100),
                CreatePlayer("p2", 2, PlayerStatus.Active, currentBet: 100)
            };
            var hasActed = new bool[] { true, true, false };

            Assert.IsFalse(_evaluator.IsBettingRoundComplete(players, 100, hasActed));
        }

        // === IsOnlyOnePlayerRemaining ===

        [Test]
        public void IsOnlyOnePlayerRemaining_OneActive_ReturnsTrue()
        {
            var players = new List<PlayerData>
            {
                CreatePlayer("p0", 0, PlayerStatus.Active),
                CreatePlayer("p1", 1, PlayerStatus.Folded),
                CreatePlayer("p2", 2, PlayerStatus.Folded)
            };

            Assert.IsTrue(_evaluator.IsOnlyOnePlayerRemaining(players, out int winner));
            Assert.AreEqual(0, winner);
        }

        [Test]
        public void IsOnlyOnePlayerRemaining_TwoActive_ReturnsFalse()
        {
            var players = new List<PlayerData>
            {
                CreatePlayer("p0", 0, PlayerStatus.Active),
                CreatePlayer("p1", 1, PlayerStatus.Active),
                CreatePlayer("p2", 2, PlayerStatus.Folded)
            };

            Assert.IsFalse(_evaluator.IsOnlyOnePlayerRemaining(players, out int winner));
            Assert.AreEqual(-1, winner);
        }

        [Test]
        public void IsOnlyOnePlayerRemaining_OneActiveOneAllIn_ReturnsFalse()
        {
            // AllIn도 Folded가 아니므로 2명 남은 것으로 판정
            var players = new List<PlayerData>
            {
                CreatePlayer("p0", 0, PlayerStatus.Active),
                CreatePlayer("p1", 1, PlayerStatus.AllIn),
                CreatePlayer("p2", 2, PlayerStatus.Folded)
            };

            Assert.IsFalse(_evaluator.IsOnlyOnePlayerRemaining(players, out int winner));
            Assert.AreEqual(-1, winner);
        }

        [Test]
        public void IsOnlyOnePlayerRemaining_AllFoldedExceptAllIn_ReturnsTrue()
        {
            var players = new List<PlayerData>
            {
                CreatePlayer("p0", 0, PlayerStatus.AllIn),
                CreatePlayer("p1", 1, PlayerStatus.Folded),
                CreatePlayer("p2", 2, PlayerStatus.Folded)
            };

            Assert.IsTrue(_evaluator.IsOnlyOnePlayerRemaining(players, out int winner));
            Assert.AreEqual(0, winner);
        }

        [Test]
        public void IsOnlyOnePlayerRemaining_EliminatedPlayerExcluded()
        {
            var players = new List<PlayerData>
            {
                CreatePlayer("p0", 0, PlayerStatus.Active),
                CreatePlayer("p1", 1, PlayerStatus.Folded),
                CreatePlayer("p2", 2, PlayerStatus.Eliminated, chips: 0)
            };

            Assert.IsTrue(_evaluator.IsOnlyOnePlayerRemaining(players, out int winner));
            Assert.AreEqual(0, winner);
        }

        [Test]
        public void IsOnlyOnePlayerRemaining_MultipleAllIn_ReturnsFalse()
        {
            // AllIn 여럿이면 핸드 종료가 아님 (쇼다운으로 가야 함)
            var players = new List<PlayerData>
            {
                CreatePlayer("p0", 0, PlayerStatus.AllIn),
                CreatePlayer("p1", 1, PlayerStatus.AllIn),
                CreatePlayer("p2", 2, PlayerStatus.Folded)
            };

            Assert.IsFalse(_evaluator.IsOnlyOnePlayerRemaining(players, out int winner));
            Assert.AreEqual(-1, winner);
        }

        // === ShouldSkipToShowdown ===

        [Test]
        public void ShouldSkipToShowdown_AllAllIn_ReturnsTrue()
        {
            var players = new List<PlayerData>
            {
                CreatePlayer("p0", 0, PlayerStatus.AllIn),
                CreatePlayer("p1", 1, PlayerStatus.AllIn),
                CreatePlayer("p2", 2, PlayerStatus.Folded)
            };

            Assert.IsTrue(_evaluator.ShouldSkipToShowdown(players));
        }

        [Test]
        public void ShouldSkipToShowdown_OneActiveRestAllIn_ReturnsTrue()
        {
            var players = new List<PlayerData>
            {
                CreatePlayer("p0", 0, PlayerStatus.Active),
                CreatePlayer("p1", 1, PlayerStatus.AllIn),
                CreatePlayer("p2", 2, PlayerStatus.AllIn),
                CreatePlayer("p3", 3, PlayerStatus.Folded)
            };

            Assert.IsTrue(_evaluator.ShouldSkipToShowdown(players));
        }

        [Test]
        public void ShouldSkipToShowdown_TwoActive_ReturnsFalse()
        {
            var players = new List<PlayerData>
            {
                CreatePlayer("p0", 0, PlayerStatus.Active),
                CreatePlayer("p1", 1, PlayerStatus.Active),
                CreatePlayer("p2", 2, PlayerStatus.Folded)
            };

            Assert.IsFalse(_evaluator.ShouldSkipToShowdown(players));
        }

        [Test]
        public void ShouldSkipToShowdown_TwoActiveOneAllIn_ReturnsFalse()
        {
            var players = new List<PlayerData>
            {
                CreatePlayer("p0", 0, PlayerStatus.Active),
                CreatePlayer("p1", 1, PlayerStatus.Active),
                CreatePlayer("p2", 2, PlayerStatus.AllIn)
            };

            Assert.IsFalse(_evaluator.ShouldSkipToShowdown(players));
        }

        [Test]
        public void ShouldSkipToShowdown_OneActiveNoAllIn_ReturnsFalse()
        {
            // AllIn이 없으면 스킵 불필요 (IsOnlyOnePlayerRemaining이 처리)
            var players = new List<PlayerData>
            {
                CreatePlayer("p0", 0, PlayerStatus.Active),
                CreatePlayer("p1", 1, PlayerStatus.Folded),
                CreatePlayer("p2", 2, PlayerStatus.Folded)
            };

            Assert.IsFalse(_evaluator.ShouldSkipToShowdown(players));
        }

        [Test]
        public void ShouldSkipToShowdown_OneActiveOneAllIn_ReturnsTrue()
        {
            // 헤즈업에서 한 명 올인, 한 명 Active → 더 이상 베팅 의미 없음
            var players = new List<PlayerData>
            {
                CreatePlayer("p0", 0, PlayerStatus.Active),
                CreatePlayer("p1", 1, PlayerStatus.AllIn)
            };

            Assert.IsTrue(_evaluator.ShouldSkipToShowdown(players));
        }
    }
}
