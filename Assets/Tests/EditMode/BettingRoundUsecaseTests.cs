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
        private ActionExecutor _actionExecutor;
        private TurnOrderResolver _turnOrderResolver;
        private RoundEndEvaluator _roundEndEvaluator;
        private PotManager _potManager;
        private BettingRoundUsecase _usecase;

        [SetUp]
        public void SetUp()
        {
            _actionValidator = new ActionValidator();
            _actionExecutor = new ActionExecutor();
            _turnOrderResolver = new TurnOrderResolver();
            _roundEndEvaluator = new RoundEndEvaluator();
            _potManager = new PotManager();
            _usecase = new BettingRoundUsecase(_actionValidator, _actionExecutor, _turnOrderResolver, _roundEndEvaluator, _potManager);
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

            var result = _usecase.RunBettingRound(state, actionProvider, broadcaster).Result;

            Assert.AreEqual(BettingRoundResultType.RoundComplete, result.Type, "정상 라운드 종료여야 한다");

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

            var result = _usecase.RunBettingRound(state, actionProvider, broadcaster).Result;

            Assert.AreEqual(PlayerStatus.Folded, state.Players[1].Status, "P2는 Folded여야 한다");
            Assert.AreEqual(PlayerStatus.Folded, state.Players[2].Status, "P3는 Folded여야 한다");
            Assert.AreEqual(PlayerStatus.Active, state.Players[0].Status, "P1은 Active여야 한다");
            Assert.AreEqual(BettingRoundResultType.HandEndedByFold, result.Type, "폴드로 핸드 종료여야 한다");
            Assert.AreEqual(0, result.WinningSeatIndex, "P1(index 0)이 승자여야 한다");
        }

        // ────────────────────────────────────────────────────────────────
        // 올인 포함 라운드: P2 Raise → P3 AllIn(칩 부족) → P1 Call → P2 Call → 라운드 종료 확인
        // 2인 게임에서 AllIn 시 Active가 1명이 되어 즉시 종료되므로 3인으로 테스트한다.
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void AllInRound_PotCollectedCorrectly()
        {
            // Flop, 3명: P1(1000), P2(1000), P3(150, 칩 부족)
            var players = new List<PlayerData>
            {
                CreatePlayer("P1", chips: 1000, currentBet: 0, seatIndex: 0),
                CreatePlayer("P2", chips: 1000, currentBet: 0, seatIndex: 1),
                CreatePlayer("P3", chips: 150, currentBet: 0, seatIndex: 2)
            };
            // dealer=0 → PostFlop 첫 액션자: P2(index 1)
            var state = CreateGameState(players, bigBlind: 100, dealerIndex: 0, phase: GamePhase.Flop);
            state.LastRaiseSize = 100;

            // P2 Raise 300 → P3 AllIn(칩 150) → P1 Call 300 → P2는 이미 액션, 라운드 종료
            var actionProvider = new StubPlayerActionProvider(new List<PlayerAction>
            {
                new PlayerAction("P2", ActionType.Raise, 300),
                new PlayerAction("P3", ActionType.AllIn, 0),
                new PlayerAction("P1", ActionType.Call, 300)
            });
            var broadcaster = new StubGameEventBroadcaster();

            _usecase.RunBettingRound(state, actionProvider, broadcaster).Wait();

            Assert.AreEqual(PlayerStatus.AllIn, state.Players[2].Status, "P3는 AllIn이어야 한다");
            Assert.AreEqual(0, state.Players[2].Chips, "P3 칩은 0이어야 한다");
            Assert.AreEqual(700, state.Players[0].Chips, "P1은 300을 콜하여 700이어야 한다");
            Assert.AreEqual(700, state.Players[1].Chips, "P2는 300을 레이즈하여 700이어야 한다");

            int totalPot = state.Pots.Sum(p => p.Amount);
            Assert.AreEqual(750, totalPot, "총 팟은 300+150+300=750이어야 한다");
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
            var playerActedEvents = broadcaster.GetEvents<PlayerActedEvent>();
            Assert.AreEqual(2, playerActedEvents.Count, "2명이 액션해야 한다");
            Assert.AreEqual(0, playerActedEvents[0].SeatIndex, "UTG(P1, seat 0)이 첫 번째 액션자여야 한다");
            Assert.AreEqual(1, playerActedEvents[1].SeatIndex, "SB(P2, seat 1)가 두 번째 액션자여야 한다");
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

            var playerActedEvents = broadcaster.GetEvents<PlayerActedEvent>();
            Assert.AreEqual(3, playerActedEvents.Count, "3명이 액션해야 한다");
            Assert.AreEqual(1, playerActedEvents[0].SeatIndex, "딜러 다음(P2, seat 1)이 첫 액션자여야 한다");
            Assert.AreEqual(2, playerActedEvents[1].SeatIndex, "P3(seat 2)가 두 번째 액션자여야 한다");
            Assert.AreEqual(0, playerActedEvents[2].SeatIndex, "P1(딜러, seat 0)이 마지막 액션자여야 한다");
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

            // 순서: PlayerActedEvent × 2 → PotUpdatedEvent
            var playerActedEvents = broadcaster.GetEvents<PlayerActedEvent>();
            Assert.AreEqual(2, playerActedEvents.Count, "PlayerActedEvent는 2회 호출되어야 한다");

            var potUpdatedEvents = broadcaster.GetEvents<PotUpdatedEvent>();
            Assert.AreEqual(1, potUpdatedEvents.Count, "PotUpdatedEvent는 1회 호출되어야 한다");

            // 전체 이벤트 순서 확인: PlayerActed → PlayerActed → PotUpdated
            var events = broadcaster.GetEvents();
            Assert.IsTrue(events.Count >= 3, $"최소 3개 이벤트가 기록되어야 한다 (실제: {events.Count})");
            Assert.IsInstanceOf<PlayerActedEvent>(events[0]);
            Assert.IsInstanceOf<PlayerActedEvent>(events[1]);
            Assert.IsInstanceOf<PotUpdatedEvent>(events[2]);
        }
    }
}
