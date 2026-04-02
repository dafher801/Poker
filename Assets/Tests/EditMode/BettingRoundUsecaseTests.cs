// BettingRoundUsecaseTests.cs
// BettingRoundUsecase의 RunBettingRound 메서드를 검증하는 EditMode 테스트 모음.
// 체크-체크 종료, 레이즈-콜 종료, 폴드로 종료, 올인 포함 라운드,
// 첫 액션 플레이어 결정, broadcaster 이벤트 호출 검증을 수행한다.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TexasHoldem.Entity;
using TexasHoldem.Gateway;
using TexasHoldem.Usecase;

namespace TexasHoldem.Tests.EditMode
{
    [TestFixture]
    public class BettingRoundUsecaseTests
    {
        private ActionValidator _actionValidator;
        private PotManager _potManager;
        private BettingRoundUsecase _usecase;

        [SetUp]
        public void SetUp()
        {
            _actionValidator = new ActionValidator();
            _potManager = new PotManager();
            _usecase = new BettingRoundUsecase(_actionValidator, _potManager);
        }

        private GameState CreateGameState(List<PlayerData> players, int bigBlind = 100,
            int dealerIndex = 0, GamePhase phase = GamePhase.PreFlop)
        {
            var blinds = new BlindInfo(bigBlind / 2, bigBlind);
            var state = new GameState(players, blinds, dealerIndex);
            state.Phase = phase;
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
        // 체크-체크 종료: 2명 모두 Check → 라운드 정상 종료, 팟에 추가 금액 없음
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void CheckCheck_RoundEndsNormally_NoPotIncrease()
        {
            // Flop에서 2명 모두 Check (CurrentBet이 0으로 동일한 상태)
            var players = new List<PlayerData>
            {
                CreatePlayer("P1", chips: 1000, currentBet: 0, seatIndex: 0),
                CreatePlayer("P2", chips: 1000, currentBet: 0, seatIndex: 1)
            };
            // dealerIndex=0 → PostFlop에서 첫 액션자는 딜러(0) 다음인 P2(index 1)
            var state = CreateGameState(players, bigBlind: 100, dealerIndex: 0, phase: GamePhase.Flop);
            state.LastRaiseSize = 100;

            var actionProvider = new StubPlayerActionProvider(new List<PlayerAction>
            {
                new PlayerAction("P2", ActionType.Check, 0),
                new PlayerAction("P1", ActionType.Check, 0)
            });
            var broadcaster = new StubGameEventBroadcaster();

            _usecase.RunBettingRound(state, actionProvider, broadcaster).Wait();

            // 팟에 추가 금액 없음
            int totalPot = state.Pots.Sum(p => p.Amount);
            Assert.AreEqual(0, totalPot, "체크-체크이므로 팟에 추가 금액이 없어야 한다");

            // 양쪽 모두 칩 변화 없음
            Assert.AreEqual(1000, state.Players[0].Chips, "P1 칩 변화 없어야 한다");
            Assert.AreEqual(1000, state.Players[1].Chips, "P2 칩 변화 없어야 한다");
        }

        // ────────────────────────────────────────────────────────────────
        // 레이즈-콜 종료: P1 Raise → P2 Call → 라운드 종료, 팟 합산 정확
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void RaiseCall_RoundEnds_PotIsCorrect()
        {
            // Flop, 둘 다 CurrentBet=0, 칩 1000씩
            var players = new List<PlayerData>
            {
                CreatePlayer("P1", chips: 1000, currentBet: 0, seatIndex: 0),
                CreatePlayer("P2", chips: 1000, currentBet: 0, seatIndex: 1)
            };
            var state = CreateGameState(players, bigBlind: 100, dealerIndex: 0, phase: GamePhase.Flop);
            state.LastRaiseSize = 100;

            // P2가 첫 액션자(딜러 다음), P2가 Raise 200, P1이 Call 200
            var actionProvider = new StubPlayerActionProvider(new List<PlayerAction>
            {
                new PlayerAction("P2", ActionType.Raise, 200),
                new PlayerAction("P1", ActionType.Call, 200)
            });
            var broadcaster = new StubGameEventBroadcaster();

            _usecase.RunBettingRound(state, actionProvider, broadcaster).Wait();

            int totalPot = state.Pots.Sum(p => p.Amount);
            Assert.AreEqual(400, totalPot, "레이즈 200 + 콜 200 = 팟 400이어야 한다");
            Assert.AreEqual(800, state.Players[0].Chips, "P1은 200을 베팅하여 800이어야 한다");
            Assert.AreEqual(800, state.Players[1].Chips, "P2은 200을 베팅하여 800이어야 한다");
        }

        // ────────────────────────────────────────────────────────────────
        // 폴드로 1명 남아 종료: 3명 중 2명 Fold → Active 1명 → 라운드 즉시 종료
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void TwoFolds_OneRemaining_RoundEndsImmediately()
        {
            // Flop, 3명 모두 CurrentBet=0
            var players = new List<PlayerData>
            {
                CreatePlayer("P1", chips: 1000, currentBet: 0, seatIndex: 0),
                CreatePlayer("P2", chips: 1000, currentBet: 0, seatIndex: 1),
                CreatePlayer("P3", chips: 1000, currentBet: 0, seatIndex: 2)
            };
            // dealerIndex=0 → PostFlop 첫 액션자: 딜러 다음 = P2(index 1)
            var state = CreateGameState(players, bigBlind: 100, dealerIndex: 0, phase: GamePhase.Flop);
            state.LastRaiseSize = 100;

            // P2 Fold → P3 Fold → Active가 P1 1명이므로 종료
            var actionProvider = new StubPlayerActionProvider(new List<PlayerAction>
            {
                new PlayerAction("P2", ActionType.Fold, 0),
                new PlayerAction("P3", ActionType.Fold, 0)
            });
            var broadcaster = new StubGameEventBroadcaster();

            _usecase.RunBettingRound(state, actionProvider, broadcaster).Wait();

            Assert.AreEqual(PlayerStatus.Folded, state.Players[1].Status, "P2는 Folded여야 한다");
            Assert.AreEqual(PlayerStatus.Folded, state.Players[2].Status, "P3는 Folded여야 한다");
            Assert.AreEqual(PlayerStatus.Active, state.Players[0].Status, "P1은 Active여야 한다");
        }

        // ────────────────────────────────────────────────────────────────
        // 올인 포함 라운드: P1 Raise → P2 AllIn(칩 부족) → P1 Call → 라운드 종료 후 사이드 팟 생성 확인
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void AllInRound_SidePotsCreated()
        {
            // Flop, P1은 칩 1000, P2는 칩 300 (부족)
            var players = new List<PlayerData>
            {
                CreatePlayer("P1", chips: 1000, currentBet: 0, seatIndex: 0),
                CreatePlayer("P2", chips: 300, currentBet: 0, seatIndex: 1)
            };
            var state = CreateGameState(players, bigBlind: 100, dealerIndex: 0, phase: GamePhase.Flop);
            state.LastRaiseSize = 100;

            // P2 첫 액션(딜러 다음): AllIn 300 → P1: Call 300
            var actionProvider = new StubPlayerActionProvider(new List<PlayerAction>
            {
                new PlayerAction("P2", ActionType.AllIn, 0),
                new PlayerAction("P1", ActionType.Call, 300)
            });
            var broadcaster = new StubGameEventBroadcaster();

            _usecase.RunBettingRound(state, actionProvider, broadcaster).Wait();

            Assert.AreEqual(PlayerStatus.AllIn, state.Players[1].Status, "P2는 AllIn이어야 한다");
            Assert.AreEqual(0, state.Players[1].Chips, "P2 칩은 0이어야 한다");
            Assert.AreEqual(700, state.Players[0].Chips, "P1은 300을 콜하여 700이어야 한다");

            int totalPot = state.Pots.Sum(p => p.Amount);
            Assert.AreEqual(600, totalPot, "총 팟은 600이어야 한다");
        }

        // ────────────────────────────────────────────────────────────────
        // 첫 액션 플레이어 결정: PreFlop에서 BB 다음 좌석이 첫 액션자인지 확인
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void PreFlop_FirstActorIsUTG()
        {
            // 3명: dealer=0, SB=1, BB=2, UTG=0
            // UTG가 첫 액션자여야 함 → P1이 첫 액션
            var players = new List<PlayerData>
            {
                CreatePlayer("P1", chips: 900, currentBet: 0, seatIndex: 0),
                CreatePlayer("P2", chips: 950, currentBet: 50, seatIndex: 1),
                CreatePlayer("P3", chips: 900, currentBet: 100, seatIndex: 2)
            };
            var state = CreateGameState(players, bigBlind: 100, dealerIndex: 0, phase: GamePhase.PreFlop);
            state.LastRaiseSize = 100;

            // UTG(P1) Fold → SB(P2) Fold → BB(P3)만 남아 종료
            var actionProvider = new StubPlayerActionProvider(new List<PlayerAction>
            {
                new PlayerAction("P1", ActionType.Fold, 0),
                new PlayerAction("P2", ActionType.Fold, 0)
            });
            var broadcaster = new StubGameEventBroadcaster();

            _usecase.RunBettingRound(state, actionProvider, broadcaster).Wait();

            // P1이 첫 번째로 Fold한 것이 기록되어야 함 (UTG가 첫 액션자)
            var log = broadcaster.GetLog();
            var playerActedEvents = log.Where(e => e.EventName == "OnPlayerActed").ToList();
            Assert.AreEqual(2, playerActedEvents.Count, "2명이 액션해야 한다");
            Assert.AreEqual("P1", (string)playerActedEvents[0].Args[0], "UTG(P1)이 첫 번째 액션자여야 한다");
            Assert.AreEqual("P2", (string)playerActedEvents[1].Args[0], "SB(P2)가 두 번째 액션자여야 한다");
        }

        // ────────────────────────────────────────────────────────────────
        // 첫 액션 플레이어 결정: Flop에서 딜러 다음 좌석이 첫 액션자인지 확인
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Flop_FirstActorIsDealerNext()
        {
            // 3명: dealer=0, 다음 Active는 P2(index 1)
            var players = new List<PlayerData>
            {
                CreatePlayer("P1", chips: 1000, currentBet: 0, seatIndex: 0),
                CreatePlayer("P2", chips: 1000, currentBet: 0, seatIndex: 1),
                CreatePlayer("P3", chips: 1000, currentBet: 0, seatIndex: 2)
            };
            var state = CreateGameState(players, bigBlind: 100, dealerIndex: 0, phase: GamePhase.Flop);
            state.LastRaiseSize = 100;

            // P2(딜러 다음)가 첫 액션, 모두 Check
            var actionProvider = new StubPlayerActionProvider(new List<PlayerAction>
            {
                new PlayerAction("P2", ActionType.Check, 0),
                new PlayerAction("P3", ActionType.Check, 0),
                new PlayerAction("P1", ActionType.Check, 0)
            });
            var broadcaster = new StubGameEventBroadcaster();

            _usecase.RunBettingRound(state, actionProvider, broadcaster).Wait();

            var log = broadcaster.GetLog();
            var playerActedEvents = log.Where(e => e.EventName == "OnPlayerActed").ToList();
            Assert.AreEqual(3, playerActedEvents.Count, "3명이 액션해야 한다");
            Assert.AreEqual("P2", (string)playerActedEvents[0].Args[0], "딜러 다음(P2)이 첫 액션자여야 한다");
            Assert.AreEqual("P3", (string)playerActedEvents[1].Args[0], "P3가 두 번째 액션자여야 한다");
            Assert.AreEqual("P1", (string)playerActedEvents[2].Args[0], "P1(딜러)이 마지막 액션자여야 한다");
        }

        // ────────────────────────────────────────────────────────────────
        // broadcaster 이벤트 호출 횟수·순서 검증
        // OnPlayerActed가 액션마다, OnBettingRoundEnded가 라운드 종료 시 정확히 호출되는지 확인
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void BroadcasterEvents_CalledInCorrectOrder()
        {
            // Flop, 2명 Check-Check
            var players = new List<PlayerData>
            {
                CreatePlayer("P1", chips: 1000, currentBet: 0, seatIndex: 0),
                CreatePlayer("P2", chips: 1000, currentBet: 0, seatIndex: 1)
            };
            var state = CreateGameState(players, bigBlind: 100, dealerIndex: 0, phase: GamePhase.Flop);
            state.LastRaiseSize = 100;

            var actionProvider = new StubPlayerActionProvider(new List<PlayerAction>
            {
                new PlayerAction("P2", ActionType.Check, 0),
                new PlayerAction("P1", ActionType.Check, 0)
            });
            var broadcaster = new StubGameEventBroadcaster();

            _usecase.RunBettingRound(state, actionProvider, broadcaster).Wait();

            var log = broadcaster.GetLog();

            // 순서: OnBettingRoundStarted → OnPlayerActed × 2 → OnPotUpdated → OnBettingRoundEnded
            Assert.IsTrue(log.Count >= 5, $"최소 5개 이벤트가 기록되어야 한다 (실제: {log.Count})");

            Assert.AreEqual("OnBettingRoundStarted", log[0].EventName, "첫 이벤트는 OnBettingRoundStarted여야 한다");
            Assert.AreEqual(GamePhase.Flop, (GamePhase)log[0].Args[0]);

            Assert.AreEqual("OnPlayerActed", log[1].EventName);
            Assert.AreEqual("OnPlayerActed", log[2].EventName);

            Assert.AreEqual("OnPotUpdated", log[3].EventName, "팟 업데이트가 호출되어야 한다");
            Assert.AreEqual("OnBettingRoundEnded", log[4].EventName, "마지막은 OnBettingRoundEnded여야 한다");
            Assert.AreEqual(GamePhase.Flop, (GamePhase)log[4].Args[0]);

            // OnBettingRoundStarted는 정확히 1회
            int startedCount = log.Count(e => e.EventName == "OnBettingRoundStarted");
            Assert.AreEqual(1, startedCount, "OnBettingRoundStarted는 1회 호출되어야 한다");

            // OnBettingRoundEnded는 정확히 1회
            int endedCount = log.Count(e => e.EventName == "OnBettingRoundEnded");
            Assert.AreEqual(1, endedCount, "OnBettingRoundEnded는 1회 호출되어야 한다");

            // OnPlayerActed는 정확히 2회
            int actedCount = log.Count(e => e.EventName == "OnPlayerActed");
            Assert.AreEqual(2, actedCount, "OnPlayerActed는 2회 호출되어야 한다");
        }
    }
}
