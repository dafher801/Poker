// HandDirectorScenarioTests.cs
// HandDirector의 통합 시나리오 테스트.
// MockPlayerActionProvider와 TestEventBroadcaster를 주입하여
// 프리플롭 올폴드, 쇼다운 정상 진행, 사이드 팟, 헤즈업, 전원 올인 스킵 시나리오를 검증한다.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using TexasHoldem.Director;
using TexasHoldem.Entity;
using TexasHoldem.Gateway;
using TexasHoldem.Usecase;

namespace TexasHoldem.Tests.EditMode
{
    /// <summary>
    /// TexasHoldem.Director.IGameEventBroadcaster 테스트용 구현체.
    /// Publish 호출 시 모든 이벤트를 내부 리스트에 순서대로 기록한다.
    /// </summary>
    public class TestEventBroadcaster : TexasHoldem.Director.IGameEventBroadcaster
    {
        private readonly List<GameEventBase> _events = new List<GameEventBase>();

        public void Subscribe<T>(Action<T> handler) where T : GameEventBase { }
        public void Unsubscribe<T>(Action<T> handler) where T : GameEventBase { }

        public void Publish<T>(T gameEvent) where T : GameEventBase
        {
            _events.Add(gameEvent);
        }

        public List<T> GetEvents<T>() where T : GameEventBase
        {
            return _events.OfType<T>().ToList();
        }

        public IReadOnlyList<GameEventBase> GetAll()
        {
            return _events.AsReadOnly();
        }

        public int EventCount => _events.Count;
    }

    /// <summary>
    /// IChipLedger 테스트용 구현체.
    /// 좌석별 칩 잔액을 딕셔너리로 관리한다.
    /// </summary>
    public class TestChipLedger : IChipLedger
    {
        private readonly Dictionary<int, int> _chips = new Dictionary<int, int>();

        public void Initialize(int seatIndex, int chips)
        {
            _chips[seatIndex] = chips;
        }

        public int GetChips(int seatIndex)
        {
            return _chips.TryGetValue(seatIndex, out int c) ? c : 0;
        }

        public void DeductChips(int seatIndex, int amount)
        {
            if (!_chips.ContainsKey(seatIndex))
                _chips[seatIndex] = 0;
            _chips[seatIndex] -= amount;
        }

        public void AddChips(int seatIndex, int amount)
        {
            if (!_chips.ContainsKey(seatIndex))
                _chips[seatIndex] = 0;
            _chips[seatIndex] += amount;
        }
    }

    [TestFixture]
    public class HandDirectorScenarioTests
    {
        // ────────────────────────────────────────────────────────────────
        // 헬퍼
        // ────────────────────────────────────────────────────────────────

        private static Card[] BuildFixedDeck(Card[] drawOrder)
        {
            var usedSet = new HashSet<Card>(drawOrder);

            var remaining = new List<Card>();
            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                foreach (Rank rank in Enum.GetValues(typeof(Rank)))
                {
                    var card = new Card(suit, rank);
                    if (!usedSet.Contains(card))
                        remaining.Add(card);
                }
            }

            var deck = new Card[52];
            for (int i = 0; i < remaining.Count; i++)
                deck[i] = remaining[i];

            for (int i = 0; i < drawOrder.Length; i++)
                deck[52 - 1 - i] = drawOrder[i];

            return deck;
        }

        private static List<PlayerData> CreatePlayers(int count, int chips)
        {
            var players = new List<PlayerData>();
            for (int i = 0; i < count; i++)
                players.Add(new PlayerData(i.ToString(), $"Player{i}", chips, i));
            return players;
        }

        private static List<PlayerData> CreatePlayersWithChips(params int[] chipAmounts)
        {
            var players = new List<PlayerData>();
            for (int i = 0; i < chipAmounts.Length; i++)
                players.Add(new PlayerData(i.ToString(), $"Player{i}", chipAmounts[i], i));
            return players;
        }

        private TestChipLedger CreateLedger(List<PlayerData> players)
        {
            var ledger = new TestChipLedger();
            for (int i = 0; i < players.Count; i++)
                ledger.Initialize(i, players[i].Chips);
            return ledger;
        }

        private HandDirector CreateDirector(
            MockPlayerActionProvider actionProvider,
            TestEventBroadcaster broadcaster,
            TestChipLedger ledger,
            IRandomSource randomSource)
        {
            return new HandDirector(
                actionProvider,
                broadcaster,
                ledger,
                randomSource,
                new TurnOrderResolver(),
                new ActionValidator(),
                new RoundEndEvaluator(),
                new PotManager());
        }

        /// <summary>
        /// 4명 기본 덱 배치.
        /// 딜러=0, SB=1, BB=2. 딜 순서: seat1, seat2, seat3, seat0.
        /// 커뮤니티 카드까지 포함하여 쇼다운 시 핸드를 예측할 수 있다.
        /// </summary>
        private Card[] BuildFourPlayerDeck()
        {
            // 홀카드 딜 순서: seat1, seat2, seat3, seat0 × 2라운드
            // 그 후: burn, flop×3, burn, turn, burn, river
            return new Card[]
            {
                // Round 1
                new Card(Suit.Spade, Rank.Two),    // seat 1 card 1
                new Card(Suit.Spade, Rank.Three),  // seat 2 card 1
                new Card(Suit.Spade, Rank.Four),   // seat 3 card 1
                new Card(Suit.Spade, Rank.Five),   // seat 0 card 1
                // Round 2
                new Card(Suit.Heart, Rank.Two),    // seat 1 card 2
                new Card(Suit.Heart, Rank.Three),  // seat 2 card 2
                new Card(Suit.Heart, Rank.Four),   // seat 3 card 2
                new Card(Suit.Heart, Rank.Five),   // seat 0 card 2
                // Burn + Flop
                new Card(Suit.Diamond, Rank.Nine), // burn
                new Card(Suit.Club, Rank.Ace),     // flop 1
                new Card(Suit.Club, Rank.King),    // flop 2
                new Card(Suit.Club, Rank.Queen),   // flop 3
                // Burn + Turn
                new Card(Suit.Diamond, Rank.Eight),// burn
                new Card(Suit.Club, Rank.Jack),    // turn
                // Burn + River
                new Card(Suit.Diamond, Rank.Seven),// burn
                new Card(Suit.Club, Rank.Ten),     // river
            };
        }

        /// <summary>
        /// 3명 사이드팟 시나리오용 덱.
        /// seat0(올인 플레이어)에게 가장 강한 핸드를 부여한다.
        /// 딜러=0, SB=1, BB=2. 딜 순서: seat1, seat2, seat0.
        /// </summary>
        private Card[] BuildThreePlayerSidePotDeck()
        {
            return new Card[]
            {
                // Round 1
                new Card(Suit.Heart, Rank.Two),     // seat 1 card 1
                new Card(Suit.Diamond, Rank.Two),   // seat 2 card 1
                new Card(Suit.Spade, Rank.Ace),     // seat 0 card 1
                // Round 2
                new Card(Suit.Heart, Rank.Three),   // seat 1 card 2
                new Card(Suit.Diamond, Rank.Three),  // seat 2 card 2
                new Card(Suit.Spade, Rank.King),    // seat 0 card 2
                // Burn + Flop
                new Card(Suit.Club, Rank.Nine),     // burn
                new Card(Suit.Spade, Rank.Queen),   // flop 1
                new Card(Suit.Spade, Rank.Jack),    // flop 2
                new Card(Suit.Spade, Rank.Ten),     // flop 3
                // Burn + Turn
                new Card(Suit.Club, Rank.Eight),    // burn
                new Card(Suit.Heart, Rank.Four),    // turn
                // Burn + River
                new Card(Suit.Club, Rank.Seven),    // burn
                new Card(Suit.Heart, Rank.Five),    // river
            };
        }

        // ────────────────────────────────────────────────────────────────
        // 시나리오 1: 프리플롭 올폴드 — BB 제외 전원 폴드 시 BB가 팟 획득
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Scenario1_PreflopAllFold_BBWinsPot()
        {
            // 4명, 1000칩, SB=5, BB=10, 딜러=0
            // SB=1, BB=2
            // PreFlop 액션 순서: seat3(UTG), seat0, seat1(SB), seat2(BB)
            // seat3: Fold, seat0: Fold, seat1: Fold → BB 승리

            var players = CreatePlayers(4, 1000);
            var blinds = new BlindInfo(5, 10);
            var state = new GameState(players, blinds, dealerIndex: 0, currentPlayerIndex: 0);
            var ledger = CreateLedger(players);
            var broadcaster = new TestEventBroadcaster();

            var scripts = new Dictionary<(int seatIndex, int actionSequence), PlayerAction>
            {
                { (3, 0), new PlayerAction("3", ActionType.Fold, 0) },
                { (0, 0), new PlayerAction("0", ActionType.Fold, 0) },
                { (1, 0), new PlayerAction("1", ActionType.Fold, 0) },
            };
            var actionProvider = new MockPlayerActionProvider(scripts);
            var randomSource = new FixedRandomSource(BuildFixedDeck(BuildFourPlayerDeck()));

            var director = CreateDirector(actionProvider, broadcaster, ledger, randomSource);

            var task = director.RunHandAsync(state, "hand-1", CancellationToken.None);
            task.Wait();

            // HandStartedEvent 확인
            var handStarted = broadcaster.GetEvents<HandStartedEvent>();
            Assert.AreEqual(1, handStarted.Count, "HandStartedEvent가 1회 발행되어야 한다.");
            Assert.AreEqual(0, handStarted[0].DealerSeatIndex);

            // BlindPostedEvent 확인
            var blindEvents = broadcaster.GetEvents<BlindPostedEvent>();
            Assert.AreEqual(2, blindEvents.Count, "SB, BB 블라인드 이벤트가 발행되어야 한다.");
            Assert.AreEqual(1, blindEvents[0].SeatIndex, "SB는 seat 1");
            Assert.AreEqual(5, blindEvents[0].Amount);
            Assert.AreEqual(2, blindEvents[1].SeatIndex, "BB는 seat 2");
            Assert.AreEqual(10, blindEvents[1].Amount);

            // PlayerActedEvent — 3명 폴드
            var actedEvents = broadcaster.GetEvents<PlayerActedEvent>();
            Assert.AreEqual(3, actedEvents.Count, "3명이 폴드해야 한다.");
            Assert.IsTrue(actedEvents.All(e => e.ActionType == ActionType.Fold));

            // HandEndedEvent — LastManStanding
            var handEnded = broadcaster.GetEvents<HandEndedEvent>();
            Assert.AreEqual(1, handEnded.Count);
            Assert.AreEqual(HandEndReason.LastManStanding, handEnded[0].Reason);
            Assert.AreEqual(2, handEnded[0].Awards[0].SeatIndex, "BB(seat 2)가 팟 획득");
            Assert.AreEqual(15, handEnded[0].Awards[0].Amount, "팟 = SB(5) + BB(10) = 15");

            // ShowdownResultEvent 미발행
            var showdownEvents = broadcaster.GetEvents<ShowdownResultEvent>();
            Assert.AreEqual(0, showdownEvents.Count, "쇼다운이 발생하면 안 된다.");
        }

        // ────────────────────────────────────────────────────────────────
        // 시나리오 2: 쇼다운까지 정상 진행 — 4페이즈 모두 통과
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Scenario2_FullHandToShowdown_CorrectWinner()
        {
            // 3명, 1000칩, SB=5, BB=10, 딜러=0
            // SB=1, BB=2
            // PreFlop 액션 순서: seat0(UTG), seat1(SB), seat2(BB)
            // 모두 Call/Check로 진행

            var players = CreatePlayers(3, 1000);
            var blinds = new BlindInfo(5, 10);
            var state = new GameState(players, blinds, dealerIndex: 0, currentPlayerIndex: 0);
            var ledger = CreateLedger(players);
            var broadcaster = new TestEventBroadcaster();

            // 3명 딜 순서: seat1, seat2, seat0
            // 덱: seat0에 Ace+King(강), seat1/seat2에 약한 카드
            var drawOrder = new Card[]
            {
                // Round 1
                new Card(Suit.Heart, Rank.Two),    // seat 1 card 1
                new Card(Suit.Diamond, Rank.Two),  // seat 2 card 1
                new Card(Suit.Spade, Rank.Ace),    // seat 0 card 1
                // Round 2
                new Card(Suit.Heart, Rank.Three),  // seat 1 card 2
                new Card(Suit.Diamond, Rank.Three),// seat 2 card 2
                new Card(Suit.Spade, Rank.King),   // seat 0 card 2
                // Burn + Flop
                new Card(Suit.Club, Rank.Nine),    // burn
                new Card(Suit.Spade, Rank.Queen),  // flop 1
                new Card(Suit.Spade, Rank.Jack),   // flop 2
                new Card(Suit.Spade, Rank.Ten),    // flop 3
                // Burn + Turn
                new Card(Suit.Club, Rank.Eight),   // burn
                new Card(Suit.Heart, Rank.Four),   // turn
                // Burn + River
                new Card(Suit.Club, Rank.Seven),   // burn
                new Card(Suit.Heart, Rank.Five),   // river
            };
            var randomSource = new FixedRandomSource(BuildFixedDeck(drawOrder));

            // PreFlop: seat0 Call(10), seat1 Call(5 more to match 10), seat2 Check
            // Flop: seat1 Check, seat2 Check, seat0 Check
            // Turn: seat1 Check, seat2 Check, seat0 Check
            // River: seat1 Check, seat2 Check, seat0 Check
            var scripts = new Dictionary<(int seatIndex, int actionSequence), PlayerAction>
            {
                // PreFlop
                { (0, 0), new PlayerAction("0", ActionType.Call, 10) },
                { (1, 0), new PlayerAction("1", ActionType.Call, 5) },
                { (2, 0), new PlayerAction("2", ActionType.Check, 0) },
                // Flop
                { (1, 1), new PlayerAction("1", ActionType.Check, 0) },
                { (2, 1), new PlayerAction("2", ActionType.Check, 0) },
                { (0, 1), new PlayerAction("0", ActionType.Check, 0) },
                // Turn
                { (1, 2), new PlayerAction("1", ActionType.Check, 0) },
                { (2, 2), new PlayerAction("2", ActionType.Check, 0) },
                { (0, 2), new PlayerAction("0", ActionType.Check, 0) },
                // River
                { (1, 3), new PlayerAction("1", ActionType.Check, 0) },
                { (2, 3), new PlayerAction("2", ActionType.Check, 0) },
                { (0, 3), new PlayerAction("0", ActionType.Check, 0) },
            };
            var actionProvider = new MockPlayerActionProvider(scripts);

            var director = CreateDirector(actionProvider, broadcaster, ledger, randomSource);

            var task = director.RunHandAsync(state, "hand-2", CancellationToken.None);
            task.Wait();

            // 4개 페이즈 PhaseChangedEvent 확인
            var phaseEvents = broadcaster.GetEvents<PhaseChangedEvent>();
            Assert.IsTrue(phaseEvents.Any(e => e.CurrentPhase == RoundPhase.PreFlop));
            Assert.IsTrue(phaseEvents.Any(e => e.CurrentPhase == RoundPhase.Flop));
            Assert.IsTrue(phaseEvents.Any(e => e.CurrentPhase == RoundPhase.Turn));
            Assert.IsTrue(phaseEvents.Any(e => e.CurrentPhase == RoundPhase.River));

            // ShowdownResultEvent 발행 확인
            var showdownEvents = broadcaster.GetEvents<ShowdownResultEvent>();
            Assert.AreEqual(1, showdownEvents.Count, "쇼다운 결과가 발행되어야 한다.");
            Assert.IsTrue(showdownEvents[0].Entries.Count > 0, "쇼다운 엔트리가 있어야 한다.");

            // seat 0이 Royal Flush로 승리
            var winnerEntry = showdownEvents[0].Entries.FirstOrDefault(e => e.IsWinner);
            Assert.IsNotNull(winnerEntry, "승자가 있어야 한다.");
            Assert.AreEqual(0, winnerEntry.SeatIndex, "seat 0(Ace+King → Royal Flush)이 승리해야 한다.");

            // HandEndedEvent — Showdown 사유
            var handEnded = broadcaster.GetEvents<HandEndedEvent>();
            Assert.AreEqual(1, handEnded.Count);
            Assert.AreEqual(HandEndReason.Showdown, handEnded[0].Reason);

            // 총 팟 = 30 (10 × 3). 승자가 전체 팟 획득
            int totalAwarded = handEnded[0].Awards.Sum(a => a.Amount);
            Assert.AreEqual(30, totalAwarded, "총 지급 금액 = 30");
        }

        // ────────────────────────────────────────────────────────────────
        // 시나리오 3: 사이드 팟 발생 — 올인 플레이어 포함 3명
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Scenario3_SidePot_CorrectDistribution()
        {
            // 3명: seat0=100칩(딜러), seat1=1000칩(SB), seat2=1000칩(BB)
            // SB=5, BB=10
            // seat0 올인(100) → seat1 Call → seat2 Call
            // seat0이 최강 핸드 → 메인 팟 획득
            // seat1/seat2 중 나머지 사이드 팟 승자 결정

            var players = CreatePlayersWithChips(100, 1000, 1000);
            var blinds = new BlindInfo(5, 10);
            var state = new GameState(players, blinds, dealerIndex: 0, currentPlayerIndex: 0);
            var ledger = CreateLedger(players);
            var broadcaster = new TestEventBroadcaster();

            var drawOrder = BuildThreePlayerSidePotDeck();
            var randomSource = new FixedRandomSource(BuildFixedDeck(drawOrder));

            // PreFlop 액션 순서 (3명, 딜러=0): UTG=seat0, seat1(SB), seat2(BB)
            // seat0: AllIn(100). 이미 currentBet=0이므로 amount=100
            // seat1: Call — 현재 최고 베팅=100, seat1 currentBet=5, 필요한 콜=95
            // seat2: Call — 현재 최고 베팅=100, seat2 currentBet=10, 필요한 콜=90
            // seat0 올인이지만 seat1/seat2는 Active → Flop/Turn/River 진행 필요
            var scripts = new Dictionary<(int seatIndex, int actionSequence), PlayerAction>
            {
                // PreFlop
                { (0, 0), new PlayerAction("0", ActionType.AllIn, 100) },
                { (1, 0), new PlayerAction("1", ActionType.Call, 95) },
                { (2, 0), new PlayerAction("2", ActionType.Call, 90) },
                // Flop
                { (1, 1), new PlayerAction("1", ActionType.Check, 0) },
                { (2, 1), new PlayerAction("2", ActionType.Check, 0) },
                // Turn
                { (1, 2), new PlayerAction("1", ActionType.Check, 0) },
                { (2, 2), new PlayerAction("2", ActionType.Check, 0) },
                // River
                { (1, 3), new PlayerAction("1", ActionType.Check, 0) },
                { (2, 3), new PlayerAction("2", ActionType.Check, 0) },
            };
            var actionProvider = new MockPlayerActionProvider(scripts);

            var director = CreateDirector(actionProvider, broadcaster, ledger, randomSource);

            var task = director.RunHandAsync(state, "hand-3", CancellationToken.None);
            task.Wait();

            // seat1/seat2가 Active이므로 Flop/Turn/River를 거쳐 정상 쇼다운
            var showdownEvents = broadcaster.GetEvents<ShowdownResultEvent>();
            Assert.AreEqual(1, showdownEvents.Count, "쇼다운이 발생해야 한다.");

            // HandEndedEvent
            var handEnded = broadcaster.GetEvents<HandEndedEvent>();
            Assert.AreEqual(1, handEnded.Count);
            Assert.AreEqual(HandEndReason.Showdown, handEnded[0].Reason);

            // seat0(Royal Flush)이 메인 팟 승자
            var awards = handEnded[0].Awards;
            Assert.IsTrue(awards.Count > 0, "최소 하나의 PotAward가 있어야 한다.");

            // 총 지급 금액 = 총 베팅 금액 (100+100+100 = 300)
            int totalAwarded = awards.Sum(a => a.Amount);
            Assert.AreEqual(300, totalAwarded, "총 지급 금액 = 300");

            // seat0이 최소 메인 팟(300)을 받아야 함
            int seat0Award = awards.Where(a => a.SeatIndex == 0).Sum(a => a.Amount);
            Assert.IsTrue(seat0Award > 0, "seat0(올인 승자)이 팟을 획득해야 한다.");
        }

        // ────────────────────────────────────────────────────────────────
        // 시나리오 4: 헤즈업(2인) 블라인드·액션 순서 특수 규칙
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Scenario4_HeadsUp_CorrectBlindAndActionOrder()
        {
            // 2명, 1000칩, SB=5, BB=10, 딜러=0
            // 헤즈업: 딜러(seat0)=SB, seat1=BB
            // PreFlop 액션 순서: 딜러(seat0) → seat1(BB)
            // PostFlop 액션 순서: seat1(BB) → seat0(딜러)

            var players = CreatePlayers(2, 1000);
            var blinds = new BlindInfo(5, 10);
            var state = new GameState(players, blinds, dealerIndex: 0, currentPlayerIndex: 0);
            var ledger = CreateLedger(players);
            var broadcaster = new TestEventBroadcaster();

            // 2명 딜 순서: seat1, seat0
            var drawOrder = new Card[]
            {
                // Round 1
                new Card(Suit.Heart, Rank.Two),    // seat 1 card 1
                new Card(Suit.Spade, Rank.Ace),    // seat 0 card 1
                // Round 2
                new Card(Suit.Heart, Rank.Three),  // seat 1 card 2
                new Card(Suit.Spade, Rank.King),   // seat 0 card 2
                // Burn + Flop
                new Card(Suit.Club, Rank.Nine),    // burn
                new Card(Suit.Spade, Rank.Queen),  // flop 1
                new Card(Suit.Spade, Rank.Jack),   // flop 2
                new Card(Suit.Spade, Rank.Ten),    // flop 3
                // Burn + Turn
                new Card(Suit.Club, Rank.Eight),   // burn
                new Card(Suit.Heart, Rank.Four),   // turn
                // Burn + River
                new Card(Suit.Club, Rank.Seven),   // burn
                new Card(Suit.Heart, Rank.Five),   // river
            };
            var randomSource = new FixedRandomSource(BuildFixedDeck(drawOrder));

            // PreFlop: seat0(SB/딜러) Call(5 to match 10), seat1(BB) Check
            // Flop: seat1(BB) Check, seat0(딜러) Check
            // Turn: seat1 Check, seat0 Check
            // River: seat1 Check, seat0 Check
            var scripts = new Dictionary<(int seatIndex, int actionSequence), PlayerAction>
            {
                // PreFlop — 딜러(seat0)가 먼저
                { (0, 0), new PlayerAction("0", ActionType.Call, 5) },
                { (1, 0), new PlayerAction("1", ActionType.Check, 0) },
                // Flop — BB(seat1)가 먼저
                { (1, 1), new PlayerAction("1", ActionType.Check, 0) },
                { (0, 1), new PlayerAction("0", ActionType.Check, 0) },
                // Turn
                { (1, 2), new PlayerAction("1", ActionType.Check, 0) },
                { (0, 2), new PlayerAction("0", ActionType.Check, 0) },
                // River
                { (1, 3), new PlayerAction("1", ActionType.Check, 0) },
                { (0, 3), new PlayerAction("0", ActionType.Check, 0) },
            };
            var actionProvider = new MockPlayerActionProvider(scripts);

            var director = CreateDirector(actionProvider, broadcaster, ledger, randomSource);

            var task = director.RunHandAsync(state, "hand-4", CancellationToken.None);
            task.Wait();

            // 블라인드 확인: seat0=SB(5), seat1=BB(10)
            var blindEvents = broadcaster.GetEvents<BlindPostedEvent>();
            Assert.AreEqual(2, blindEvents.Count);
            Assert.AreEqual(0, blindEvents[0].SeatIndex, "헤즈업: 딜러(seat0)=SB");
            Assert.AreEqual(BlindType.Small, blindEvents[0].BlindType);
            Assert.AreEqual(5, blindEvents[0].Amount);
            Assert.AreEqual(1, blindEvents[1].SeatIndex, "헤즈업: seat1=BB");
            Assert.AreEqual(BlindType.Big, blindEvents[1].BlindType);
            Assert.AreEqual(10, blindEvents[1].Amount);

            // PreFlop 액션 순서 확인: seat0(딜러=SB) → seat1(BB)
            var actedEvents = broadcaster.GetEvents<PlayerActedEvent>();
            var preflopActions = actedEvents.Take(2).ToList();
            Assert.AreEqual(0, preflopActions[0].SeatIndex, "PreFlop: 딜러(seat0)가 먼저 액션");
            Assert.AreEqual(1, preflopActions[1].SeatIndex, "PreFlop: BB(seat1)가 두 번째");

            // PostFlop 액션 순서 확인: seat1(BB) → seat0(딜러)
            // Flop 단계의 액션은 3, 4번째
            var flopActions = actedEvents.Skip(2).Take(2).ToList();
            Assert.AreEqual(1, flopActions[0].SeatIndex, "Flop: BB(seat1)가 먼저 액션");
            Assert.AreEqual(0, flopActions[1].SeatIndex, "Flop: 딜러(seat0)가 두 번째");

            // 쇼다운까지 정상 진행
            var handEnded = broadcaster.GetEvents<HandEndedEvent>();
            Assert.AreEqual(1, handEnded.Count);
            Assert.AreEqual(HandEndReason.Showdown, handEnded[0].Reason);

            // 총 팟 = 20 (10 × 2)
            int totalAwarded = handEnded[0].Awards.Sum(a => a.Amount);
            Assert.AreEqual(20, totalAwarded, "총 지급 금액 = 20");
        }

        // ────────────────────────────────────────────────────────────────
        // 시나리오 5: 모든 플레이어 올인 → 잔여 페이즈 스킵 후 쇼다운 직행
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Scenario5_AllPlayersAllIn_SkipToShowdown()
        {
            // 3명, 500칩, SB=5, BB=10, 딜러=0
            // PreFlop: seat0 AllIn(500), seat1 AllIn(495+5=500), seat2 AllIn(490+10=500)
            // 전원 올인 → 남은 페이즈 스킵 → 즉시 쇼다운

            var players = CreatePlayers(3, 500);
            var blinds = new BlindInfo(5, 10);
            var state = new GameState(players, blinds, dealerIndex: 0, currentPlayerIndex: 0);
            var ledger = CreateLedger(players);
            var broadcaster = new TestEventBroadcaster();

            var drawOrder = BuildThreePlayerSidePotDeck();
            var randomSource = new FixedRandomSource(BuildFixedDeck(drawOrder));

            // PreFlop 액션 순서 (3명, 딜러=0): UTG=seat0, seat1(SB), seat2(BB)
            var scripts = new Dictionary<(int seatIndex, int actionSequence), PlayerAction>
            {
                { (0, 0), new PlayerAction("0", ActionType.AllIn, 500) },
                { (1, 0), new PlayerAction("1", ActionType.AllIn, 495) },
                { (2, 0), new PlayerAction("2", ActionType.AllIn, 490) },
            };
            var actionProvider = new MockPlayerActionProvider(scripts);

            var director = CreateDirector(actionProvider, broadcaster, ledger, randomSource);

            var task = director.RunHandAsync(state, "hand-5", CancellationToken.None);
            task.Wait();

            // Flop/Turn/River PhaseChangedEvent가 없어야 한다 (스킵됨)
            var phaseEvents = broadcaster.GetEvents<PhaseChangedEvent>();
            bool hasFlopPhase = phaseEvents.Any(e => e.CurrentPhase == RoundPhase.Flop);
            bool hasTurnPhase = phaseEvents.Any(e => e.CurrentPhase == RoundPhase.Turn);
            bool hasRiverPhase = phaseEvents.Any(e => e.CurrentPhase == RoundPhase.River);
            Assert.IsFalse(hasFlopPhase, "Flop 페이즈가 스킵되어야 한다.");
            Assert.IsFalse(hasTurnPhase, "Turn 페이즈가 스킵되어야 한다.");
            Assert.IsFalse(hasRiverPhase, "River 페이즈가 스킵되어야 한다.");

            // 쇼다운 결과 확인
            var showdownEvents = broadcaster.GetEvents<ShowdownResultEvent>();
            Assert.AreEqual(1, showdownEvents.Count, "쇼다운이 발생해야 한다.");

            // HandEndedEvent — Showdown 사유
            var handEnded = broadcaster.GetEvents<HandEndedEvent>();
            Assert.AreEqual(1, handEnded.Count);
            Assert.AreEqual(HandEndReason.Showdown, handEnded[0].Reason);

            // 총 지급 금액 = 전체 칩 합계 (500 × 3 = 1500)
            int totalAwarded = handEnded[0].Awards.Sum(a => a.Amount);
            Assert.AreEqual(1500, totalAwarded, "총 지급 금액 = 1500 (칩 보존)");

            // 커뮤니티 카드 5장이 딜되었는지 확인 (스킵 시에도 쇼다운에서 나머지 딜)
            Assert.AreEqual(5, state.CommunityCards.Count, "커뮤니티 카드 5장이 있어야 한다.");
        }
    }
}
