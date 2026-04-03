// SessionFlowUsecaseTests.cs
// SessionFlowUsecase의 단위 테스트.
// 핸드 결과 반영, 딜러 이동, 세션 종료 판정, 최종 결과 생성을 검증한다.

using System.Collections.Generic;
using NUnit.Framework;
using Poker.Entity;
using Poker.Usecase;
using TexasHoldem.Entity;

namespace Poker.Tests.EditMode
{
    [TestFixture]
    public class SessionFlowUsecaseTests
    {
        private SessionFlowUsecase _usecase;
        private SessionState _state;

        [SetUp]
        public void SetUp()
        {
            _usecase = new SessionFlowUsecase();
            _state = new SessionState(
                new List<string> { "Player0", "Player1", "Player2" },
                1000
            );
        }

        // ──────────────────────────────────
        // ProcessHandResult
        // ──────────────────────────────────

        [Test]
        public void ProcessHandResult_UpdatesWinnerChips()
        {
            var potResult = new PotResult(300, new List<int> { 0 }, 300, 0);
            var roundResult = new RoundResult(new List<PotResult> { potResult }, false);

            _usecase.ProcessHandResult(_state, roundResult);

            Assert.AreEqual(1300, _state.Chips["Player0"]);
        }

        [Test]
        public void ProcessHandResult_SplitPotDistributesEvenly()
        {
            var potResult = new PotResult(200, new List<int> { 0, 1 }, 100, 0);
            var roundResult = new RoundResult(new List<PotResult> { potResult }, false);

            _usecase.ProcessHandResult(_state, roundResult);

            Assert.AreEqual(1100, _state.Chips["Player0"]);
            Assert.AreEqual(1100, _state.Chips["Player1"]);
        }

        [Test]
        public void ProcessHandResult_RemainderGoesToFirstWinner()
        {
            var potResult = new PotResult(301, new List<int> { 1, 2 }, 150, 1);
            var roundResult = new RoundResult(new List<PotResult> { potResult }, false);

            _usecase.ProcessHandResult(_state, roundResult);

            Assert.AreEqual(1151, _state.Chips["Player1"]);
            Assert.AreEqual(1150, _state.Chips["Player2"]);
        }

        [Test]
        public void ProcessHandResult_EliminatesPlayersWithZeroChips()
        {
            _state.SetChips("Player2", 0);
            var potResult = new PotResult(0, new List<int> { 0 }, 0, 0);
            var roundResult = new RoundResult(new List<PotResult> { potResult }, false);

            _usecase.ProcessHandResult(_state, roundResult);

            Assert.IsTrue(_state.Eliminated["Player2"]);
            Assert.IsFalse(_state.Eliminated["Player0"]);
        }

        [Test]
        public void ProcessHandResult_IncrementsHandCount()
        {
            var potResult = new PotResult(100, new List<int> { 0 }, 100, 0);
            var roundResult = new RoundResult(new List<PotResult> { potResult }, false);

            _usecase.ProcessHandResult(_state, roundResult);

            Assert.AreEqual(1, _state.HandCount);
        }

        // ──────────────────────────────────
        // AdvanceDealerButton
        // ──────────────────────────────────

        [Test]
        public void AdvanceDealerButton_MovesToNextPlayer()
        {
            _state.DealerSeatIndex = 0;

            _usecase.AdvanceDealerButton(_state);

            Assert.AreEqual(1, _state.DealerSeatIndex);
        }

        [Test]
        public void AdvanceDealerButton_WrapsAround()
        {
            _state.DealerSeatIndex = 2;

            _usecase.AdvanceDealerButton(_state);

            Assert.AreEqual(0, _state.DealerSeatIndex);
        }

        [Test]
        public void AdvanceDealerButton_SkipsEliminatedPlayers()
        {
            _state.SetChips("Player1", 0);
            _state.EliminatePlayer("Player1");
            _state.DealerSeatIndex = 0;

            _usecase.AdvanceDealerButton(_state);

            Assert.AreEqual(2, _state.DealerSeatIndex);
        }

        // ──────────────────────────────────
        // ShouldEndSession
        // ──────────────────────────────────

        [Test]
        public void ShouldEndSession_FalseWhenMultipleActivePlayers()
        {
            Assert.IsFalse(_usecase.ShouldEndSession(_state, "Player0"));
        }

        [Test]
        public void ShouldEndSession_TrueWhenOnlyOneActivePlayer()
        {
            _state.SetChips("Player1", 0);
            _state.EliminatePlayer("Player1");
            _state.SetChips("Player2", 0);
            _state.EliminatePlayer("Player2");

            Assert.IsTrue(_usecase.ShouldEndSession(_state, "Player0"));
        }

        [Test]
        public void ShouldEndSession_TrueWhenHumanEliminated()
        {
            _state.SetChips("Player0", 0);
            _state.EliminatePlayer("Player0");

            Assert.IsTrue(_usecase.ShouldEndSession(_state, "Player0"));
        }

        [Test]
        public void ShouldEndSession_FalseWhenHumanAliveAndMultipleActive()
        {
            _state.SetChips("Player2", 0);
            _state.EliminatePlayer("Player2");

            Assert.IsFalse(_usecase.ShouldEndSession(_state, "Player0"));
        }

        // ──────────────────────────────────
        // GetSessionResult
        // ──────────────────────────────────

        [Test]
        public void GetSessionResult_WinnerIsPlayerWithMostChips()
        {
            _state.SetChips("Player0", 2000);
            _state.SetChips("Player1", 500);
            _state.SetChips("Player2", 0);
            _state.EliminatePlayer("Player2");

            var result = _usecase.GetSessionResult(_state);

            Assert.AreEqual("Player0", result.WinnerId);
        }

        [Test]
        public void GetSessionResult_RankingsOrderedCorrectly()
        {
            // Player2 탈락 (핸드 1에서), Player1 탈락 (핸드 3에서)
            _state.HandCount = 1;
            _state.SetChips("Player2", 0);
            _state.EliminatePlayer("Player2");

            _state.HandCount = 3;
            _state.SetChips("Player1", 0);
            _state.EliminatePlayer("Player1");

            _state.SetChips("Player0", 3000);

            var result = _usecase.GetSessionResult(_state);

            Assert.AreEqual(3, result.Rankings.Count);
            Assert.AreEqual("Player0", result.Rankings[0].PlayerId);
            Assert.AreEqual(1, result.Rankings[0].Rank);
            Assert.IsNull(result.Rankings[0].EliminatedAtHand);

            // Player1: 나중에 탈락했으므로 2위
            Assert.AreEqual("Player1", result.Rankings[1].PlayerId);
            Assert.AreEqual(2, result.Rankings[1].Rank);
            Assert.AreEqual(3, result.Rankings[1].EliminatedAtHand);

            // Player2: 먼저 탈락했으므로 3위
            Assert.AreEqual("Player2", result.Rankings[2].PlayerId);
            Assert.AreEqual(3, result.Rankings[2].Rank);
            Assert.AreEqual(1, result.Rankings[2].EliminatedAtHand);
        }

        [Test]
        public void GetSessionResult_AllActivePlayersRankedByChips()
        {
            _state.SetChips("Player0", 500);
            _state.SetChips("Player1", 1500);
            _state.SetChips("Player2", 1000);

            var result = _usecase.GetSessionResult(_state);

            Assert.AreEqual("Player1", result.Rankings[0].PlayerId);
            Assert.AreEqual("Player2", result.Rankings[1].PlayerId);
            Assert.AreEqual("Player0", result.Rankings[2].PlayerId);
        }
    }
}
