// ActionValidatorTests.cs
// ActionValidator의 GetLegalActions 메서드를 검증하는 EditMode 테스트 모음.
// 다양한 베팅 상황에서 합법 액션, 콜 금액, 레이즈 범위가 올바른지 확인한다.

using System.Collections.Generic;
using NUnit.Framework;
using TexasHoldem.Entity;
using TexasHoldem.Usecase;

namespace TexasHoldem.Tests.EditMode
{
    [TestFixture]
    public class ActionValidatorTests
    {
        private ActionValidator _validator;

        [SetUp]
        public void SetUp()
        {
            _validator = new ActionValidator();
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
        // 아무도 베팅하지 않은 상태에서 Check와 Raise가 가능한지 확인
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void NoBet_CheckAndRaiseAvailable()
        {
            var players = new List<PlayerData>
            {
                CreatePlayer("P1", chips: 1000, currentBet: 0, seatIndex: 0),
                CreatePlayer("P2", chips: 1000, currentBet: 0, seatIndex: 1)
            };
            var state = CreateGameState(players);

            var result = _validator.GetLegalActions(state, "P1");

            Assert.Contains(ActionType.Check, result.AvailableActions);
            Assert.Contains(ActionType.Raise, result.AvailableActions);
            Assert.Contains(ActionType.Fold, result.AvailableActions);
            Assert.Contains(ActionType.AllIn, result.AvailableActions);
            Assert.AreEqual(0, result.CallAmount);
        }

        // ────────────────────────────────────────────────────────────────
        // 앞 플레이어가 레이즈한 뒤 Fold, Call, Raise(리레이즈)가 가능한지 확인
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void AfterRaise_FoldCallRaiseAvailable()
        {
            var players = new List<PlayerData>
            {
                CreatePlayer("P1", chips: 700, currentBet: 300, seatIndex: 0),
                CreatePlayer("P2", chips: 1000, currentBet: 0, seatIndex: 1)
            };
            var state = CreateGameState(players);
            state.LastRaiseSize = 200; // BB 100에서 300으로 레이즈 → 레이즈 크기 200

            var result = _validator.GetLegalActions(state, "P2");

            Assert.Contains(ActionType.Fold, result.AvailableActions);
            Assert.Contains(ActionType.Call, result.AvailableActions);
            Assert.Contains(ActionType.Raise, result.AvailableActions);
            Assert.AreEqual(300, result.CallAmount);
        }

        // ────────────────────────────────────────────────────────────────
        // Call 금액이 남은 칩보다 클 때 Call 대신 AllIn만 표시되는지 확인
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void CallAmountExceedsChips_OnlyAllInAvailable()
        {
            var players = new List<PlayerData>
            {
                CreatePlayer("P1", chips: 700, currentBet: 300, seatIndex: 0),
                CreatePlayer("P2", chips: 200, currentBet: 0, seatIndex: 1)
            };
            var state = CreateGameState(players);
            state.LastRaiseSize = 200;

            var result = _validator.GetLegalActions(state, "P2");

            Assert.Contains(ActionType.Fold, result.AvailableActions);
            Assert.Contains(ActionType.AllIn, result.AvailableActions);
            Assert.IsFalse(result.AvailableActions.Contains(ActionType.Call),
                "칩이 콜 금액보다 적거나 같으면 Call은 불가능해야 한다");
        }

        // ────────────────────────────────────────────────────────────────
        // 최소 레이즈 금액이 직전 레이즈 크기 이상인지 확인
        // BB=100, 첫 레이즈 300 → 최소 리레이즈 총액 500
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void MinRaiseAmount_IsCurrentMaxBetPlusLastRaiseSize()
        {
            var players = new List<PlayerData>
            {
                CreatePlayer("P1", chips: 700, currentBet: 300, seatIndex: 0),
                CreatePlayer("P2", chips: 1000, currentBet: 0, seatIndex: 1)
            };
            var state = CreateGameState(players);
            // BB=100, 첫 레이즈가 300이면 LastRaiseSize = 300 - 100 = 200
            state.LastRaiseSize = 200;

            var result = _validator.GetLegalActions(state, "P2");

            // 최소 레이즈 총액 = maxBet(300) + LastRaiseSize(200) = 500
            Assert.AreEqual(500, result.MinRaiseAmount);
            // 최대 레이즈 총액 = 칩(1000) + CurrentBet(0) = 1000
            Assert.AreEqual(1000, result.MaxRaiseAmount);
        }

        // ────────────────────────────────────────────────────────────────
        // 칩이 최소 레이즈 미만이지만 콜 이상인 경우 Raise 불가, Call·AllIn 가능 확인
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void ChipsBelowMinRaiseButAboveCall_NoRaise_CallAndAllInAvailable()
        {
            var players = new List<PlayerData>
            {
                CreatePlayer("P1", chips: 700, currentBet: 300, seatIndex: 0),
                CreatePlayer("P2", chips: 400, currentBet: 0, seatIndex: 1)
            };
            var state = CreateGameState(players);
            state.LastRaiseSize = 200;
            // 최소 레이즈 총액 = 300 + 200 = 500
            // P2의 총 칩 = 400 + 0 = 400 < 500 → Raise 불가
            // 콜 금액 = 300, P2 칩 = 400 > 300 → Call 가능

            var result = _validator.GetLegalActions(state, "P2");

            Assert.Contains(ActionType.Fold, result.AvailableActions);
            Assert.Contains(ActionType.Call, result.AvailableActions);
            Assert.Contains(ActionType.AllIn, result.AvailableActions);
            Assert.IsFalse(result.AvailableActions.Contains(ActionType.Raise),
                "칩이 최소 레이즈 미만이면 Raise는 불가능해야 한다");
        }

        // ────────────────────────────────────────────────────────────────
        // 올인 상태 플레이어에게는 빈 액션 목록 반환 확인
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void AllInPlayer_ReturnsEmptyActions()
        {
            var players = new List<PlayerData>
            {
                CreatePlayer("P1", chips: 0, currentBet: 500, status: PlayerStatus.AllIn, seatIndex: 0),
                CreatePlayer("P2", chips: 1000, currentBet: 0, seatIndex: 1)
            };
            var state = CreateGameState(players);

            var result = _validator.GetLegalActions(state, "P1");

            Assert.IsEmpty(result.AvailableActions, "AllIn 상태 플레이어는 빈 액션 목록을 받아야 한다");
            Assert.AreEqual(0, result.CallAmount);
            Assert.AreEqual(0, result.MinRaiseAmount);
            Assert.AreEqual(0, result.MaxRaiseAmount);
        }
    }
}
