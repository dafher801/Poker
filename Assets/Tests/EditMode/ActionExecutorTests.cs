// ActionExecutorTests.cs
// ActionExecutor의 Execute 메서드를 검증하는 EditMode 테스트 모음.
// 각 액션 타입(Fold, Check, Call, Raise, AllIn)별로 상태 변경이 올바른지 확인한다.

using System.Collections.Generic;
using NUnit.Framework;
using TexasHoldem.Entity;
using TexasHoldem.Usecase;

namespace TexasHoldem.Tests.EditMode
{
    [TestFixture]
    public class ActionExecutorTests
    {
        private ActionExecutor _executor;

        [SetUp]
        public void SetUp()
        {
            _executor = new ActionExecutor();
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
        // Fold
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Fold_SetsStatusToFolded()
        {
            var players = new List<PlayerData>
            {
                CreatePlayer("P1", chips: 1000, seatIndex: 0),
                CreatePlayer("P2", chips: 1000, seatIndex: 1)
            };
            var state = CreateGameState(players);
            var action = new PlayerAction("P1", ActionType.Fold, 0);

            _executor.Execute(state, action);

            Assert.AreEqual(PlayerStatus.Folded, players[0].Status);
            Assert.AreEqual(1000, players[0].Chips);
        }

        // ────────────────────────────────────────────────────────────────
        // Check
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Check_NoStateChange()
        {
            var players = new List<PlayerData>
            {
                CreatePlayer("P1", chips: 1000, seatIndex: 0),
                CreatePlayer("P2", chips: 1000, seatIndex: 1)
            };
            var state = CreateGameState(players);
            var action = new PlayerAction("P1", ActionType.Check, 0);

            _executor.Execute(state, action);

            Assert.AreEqual(PlayerStatus.Active, players[0].Status);
            Assert.AreEqual(1000, players[0].Chips);
            Assert.AreEqual(0, players[0].CurrentBet);
        }

        // ────────────────────────────────────────────────────────────────
        // Call
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Call_DeductsChipsAndUpdatesCurrentBet()
        {
            var players = new List<PlayerData>
            {
                CreatePlayer("P1", chips: 1000, currentBet: 0, seatIndex: 0),
                CreatePlayer("P2", chips: 800, currentBet: 200, seatIndex: 1)
            };
            var state = CreateGameState(players);
            // P2가 200을 베팅한 상태에서 P1이 콜
            var action = new PlayerAction("P1", ActionType.Call, 200);

            _executor.Execute(state, action);

            Assert.AreEqual(800, players[0].Chips);
            Assert.AreEqual(200, players[0].CurrentBet);
            Assert.AreEqual(PlayerStatus.Active, players[0].Status);
        }

        [Test]
        public void Call_AddsToPot()
        {
            var players = new List<PlayerData>
            {
                CreatePlayer("P1", chips: 1000, currentBet: 0, seatIndex: 0),
                CreatePlayer("P2", chips: 800, currentBet: 200, seatIndex: 1)
            };
            var state = CreateGameState(players);
            var action = new PlayerAction("P1", ActionType.Call, 200);

            _executor.Execute(state, action);

            Assert.AreEqual(1, state.Pots.Count);
            Assert.AreEqual(200, state.Pots[0].Amount);
        }

        [Test]
        public void Call_AfterPartialBet_OnlyDeductsDifference()
        {
            var players = new List<PlayerData>
            {
                CreatePlayer("P1", chips: 900, currentBet: 100, seatIndex: 0),
                CreatePlayer("P2", chips: 700, currentBet: 300, seatIndex: 1)
            };
            var state = CreateGameState(players);
            // P1이 이미 100 베팅, P2가 300 → P1이 콜하면 200 추가
            var action = new PlayerAction("P1", ActionType.Call, 200);

            _executor.Execute(state, action);

            Assert.AreEqual(700, players[0].Chips);
            Assert.AreEqual(300, players[0].CurrentBet);
        }

        // ────────────────────────────────────────────────────────────────
        // Raise
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Raise_DeductsChipsAndUpdatesState()
        {
            var players = new List<PlayerData>
            {
                CreatePlayer("P1", chips: 1000, currentBet: 0, seatIndex: 0),
                CreatePlayer("P2", chips: 800, currentBet: 200, seatIndex: 1)
            };
            var state = CreateGameState(players);
            // P1이 400으로 레이즈 (현재 최고 200 → 400)
            var action = new PlayerAction("P1", ActionType.Raise, 400);

            _executor.Execute(state, action);

            Assert.AreEqual(600, players[0].Chips);
            Assert.AreEqual(400, players[0].CurrentBet);
        }

        [Test]
        public void Raise_UpdatesLastRaiseSize()
        {
            var players = new List<PlayerData>
            {
                CreatePlayer("P1", chips: 1000, currentBet: 0, seatIndex: 0),
                CreatePlayer("P2", chips: 800, currentBet: 200, seatIndex: 1)
            };
            var state = CreateGameState(players);
            var action = new PlayerAction("P1", ActionType.Raise, 400);

            _executor.Execute(state, action);

            // LastRaiseSize = 400 (레이즈 총액) - 200 (이전 최고 베팅) = 200
            Assert.AreEqual(200, state.LastRaiseSize);
        }

        [Test]
        public void Raise_AddsToPot()
        {
            var players = new List<PlayerData>
            {
                CreatePlayer("P1", chips: 1000, currentBet: 0, seatIndex: 0),
                CreatePlayer("P2", chips: 800, currentBet: 200, seatIndex: 1)
            };
            var state = CreateGameState(players);
            var action = new PlayerAction("P1", ActionType.Raise, 400);

            _executor.Execute(state, action);

            Assert.AreEqual(1, state.Pots.Count);
            Assert.AreEqual(400, state.Pots[0].Amount);
        }

        // ────────────────────────────────────────────────────────────────
        // AllIn
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void AllIn_SetsChipsToZeroAndStatusAllIn()
        {
            var players = new List<PlayerData>
            {
                CreatePlayer("P1", chips: 500, currentBet: 0, seatIndex: 0),
                CreatePlayer("P2", chips: 800, currentBet: 200, seatIndex: 1)
            };
            var state = CreateGameState(players);
            var action = new PlayerAction("P1", ActionType.AllIn, 500);

            _executor.Execute(state, action);

            Assert.AreEqual(0, players[0].Chips);
            Assert.AreEqual(500, players[0].CurrentBet);
            Assert.AreEqual(PlayerStatus.AllIn, players[0].Status);
        }

        [Test]
        public void AllIn_ExceedsHighestBet_UpdatesLastRaiseSize()
        {
            var players = new List<PlayerData>
            {
                CreatePlayer("P1", chips: 500, currentBet: 0, seatIndex: 0),
                CreatePlayer("P2", chips: 800, currentBet: 200, seatIndex: 1)
            };
            var state = CreateGameState(players);
            var action = new PlayerAction("P1", ActionType.AllIn, 500);

            _executor.Execute(state, action);

            // 올인 500 > 최고 베팅 200, raiseSize = 300 >= LastRaiseSize(100) → 갱신
            Assert.AreEqual(300, state.LastRaiseSize);
        }

        [Test]
        public void AllIn_ShortAllIn_DoesNotUpdateLastRaiseSize()
        {
            var players = new List<PlayerData>
            {
                CreatePlayer("P1", chips: 50, currentBet: 0, seatIndex: 0),
                CreatePlayer("P2", chips: 800, currentBet: 200, seatIndex: 1)
            };
            var state = CreateGameState(players);
            state.LastRaiseSize = 200;
            // P1이 50칩 올인 → 총 베팅 50, 최고 베팅 200보다 낮으므로 LastRaiseSize 미갱신
            var action = new PlayerAction("P1", ActionType.AllIn, 50);

            _executor.Execute(state, action);

            Assert.AreEqual(200, state.LastRaiseSize);
        }

        [Test]
        public void AllIn_AddsToPot()
        {
            var players = new List<PlayerData>
            {
                CreatePlayer("P1", chips: 500, currentBet: 0, seatIndex: 0),
                CreatePlayer("P2", chips: 800, currentBet: 200, seatIndex: 1)
            };
            var state = CreateGameState(players);
            var action = new PlayerAction("P1", ActionType.AllIn, 500);

            _executor.Execute(state, action);

            Assert.AreEqual(1, state.Pots.Count);
            Assert.AreEqual(500, state.Pots[0].Amount);
        }

        // ────────────────────────────────────────────────────────────────
        // 에러 케이스
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Execute_NullState_ThrowsArgumentNullException()
        {
            var action = new PlayerAction("P1", ActionType.Fold, 0);
            Assert.Throws<System.ArgumentNullException>(() => _executor.Execute(null, action));
        }

        [Test]
        public void Execute_NullAction_ThrowsArgumentNullException()
        {
            var players = new List<PlayerData>
            {
                CreatePlayer("P1", chips: 1000, seatIndex: 0),
                CreatePlayer("P2", chips: 1000, seatIndex: 1)
            };
            var state = CreateGameState(players);
            Assert.Throws<System.ArgumentNullException>(() => _executor.Execute(state, null));
        }

        [Test]
        public void Execute_UnknownPlayer_ThrowsArgumentException()
        {
            var players = new List<PlayerData>
            {
                CreatePlayer("P1", chips: 1000, seatIndex: 0),
                CreatePlayer("P2", chips: 1000, seatIndex: 1)
            };
            var state = CreateGameState(players);
            var action = new PlayerAction("UNKNOWN", ActionType.Fold, 0);
            Assert.Throws<System.ArgumentException>(() => _executor.Execute(state, action));
        }

        // ────────────────────────────────────────────────────────────────
        // 연속 액션 시나리오
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void MultipleActions_PotAccumulates()
        {
            var players = new List<PlayerData>
            {
                CreatePlayer("P1", chips: 1000, currentBet: 0, seatIndex: 0),
                CreatePlayer("P2", chips: 1000, currentBet: 0, seatIndex: 1)
            };
            var state = CreateGameState(players);

            // P1 레이즈 200
            _executor.Execute(state, new PlayerAction("P1", ActionType.Raise, 200));
            // P2 콜 200
            _executor.Execute(state, new PlayerAction("P2", ActionType.Call, 200));

            Assert.AreEqual(800, players[0].Chips);
            Assert.AreEqual(800, players[1].Chips);
            Assert.AreEqual(400, state.Pots[0].Amount);
        }
    }
}
