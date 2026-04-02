// GameRoundTests_Basic.cs
// GameRoundUsecase의 통합 테스트.
// 시나리오 1: 4명 플레이어가 모든 스트리트에서 Call/Check → 쇼다운 도달, 최고 핸드 승리, 칩 합계 불변.
// 시나리오 2: UTG Raise 후 나머지 3명 Fold → 조기 종료, 커뮤니티 카드 없음, OnShowdown 미호출, 칩 합계 불변.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TexasHoldem.Entity;
using TexasHoldem.Gateway;
using TexasHoldem.Usecase;

namespace TexasHoldem.Tests.EditMode
{
    [TestFixture]
    public class GameRoundTests_Basic
    {
        // ────────────────────────────────────────────────────────────────
        // 헬퍼
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// 지정 카드 13장을 Draw 순서대로 배치한 52장 고정 덱을 생성한다.
        /// drawOrder[0]이 첫 번째 Draw() 결과가 된다.
        /// Deck.Draw()는 리스트 끝에서 꺼내므로 drawOrder를 역순으로 배치한다.
        /// </summary>
        private static Card[] BuildFixedDeck(Card[] drawOrder)
        {
            var usedSet = new HashSet<Card>(drawOrder);

            // 사용되지 않은 카드 수집
            var remaining = new List<Card>();
            foreach (Suit suit in System.Enum.GetValues(typeof(Suit)))
            {
                foreach (Rank rank in System.Enum.GetValues(typeof(Rank)))
                {
                    var card = new Card(suit, rank);
                    if (!usedSet.Contains(card))
                        remaining.Add(card);
                }
            }

            // fixedDeck: [남은 카드들 ... , drawOrder 역순]
            // Draw()는 끝에서 꺼내므로, drawOrder의 첫 번째가 맨 끝에 위치해야 한다.
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
                players.Add(new PlayerData($"P{i}", $"Player{i}", chips, i));
            return players;
        }

        // ────────────────────────────────────────────────────────────────
        // 시나리오 1 — 기본 쇼다운
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Scenario1_BasicShowdown_WinnerTakesPot_ChipsSumPreserved()
        {
            // 4명, 1000칩, 블라인드 10/20, 초기 dealerIndex=0
            var players = CreatePlayers(4, 1000);
            var blinds = new BlindInfo(10, 20);
            var state = new GameState(players, blinds, dealerIndex: 0);

            // GetNextDealer: 0 → 1. Dealer=1, SB=2, BB=3
            // PreFlop 첫 액션: P0(UTG), 이후 P1, P2, P3

            // 고정 덱 설정
            // 홀카드: P0=A♠A♥, P1=K♠4♥, P2=8♦8♣, P3=10♠10♥
            // 커뮤니티: 2♠ 3♦ 7♥ 9♣ J♣
            // P0이 AA로 최고 핸드

            var drawOrder = new Card[]
            {
                // 홀카드 (P0 card1, P0 card2, P1 card1, P1 card2, P2 card1, P2 card2, P3 card1, P3 card2)
                new Card(Suit.Spade, Rank.Ace),     // P0 hole 1
                new Card(Suit.Heart, Rank.Ace),     // P0 hole 2
                new Card(Suit.Spade, Rank.King),    // P1 hole 1
                new Card(Suit.Heart, Rank.Four),    // P1 hole 2
                new Card(Suit.Diamond, Rank.Eight), // P2 hole 1
                new Card(Suit.Club, Rank.Eight),    // P2 hole 2
                new Card(Suit.Spade, Rank.Ten),     // P3 hole 1
                new Card(Suit.Heart, Rank.Ten),     // P3 hole 2
                // 커뮤니티 (Flop1, Flop2, Flop3, Turn, River)
                new Card(Suit.Spade, Rank.Two),     // Flop 1
                new Card(Suit.Diamond, Rank.Three), // Flop 2
                new Card(Suit.Heart, Rank.Seven),   // Flop 3
                new Card(Suit.Club, Rank.Nine),     // Turn
                new Card(Suit.Club, Rank.Jack),     // River
            };

            var fixedDeck = BuildFixedDeck(drawOrder);
            var random = new FixedRandomSource(fixedDeck);

            // 액션 설정 (PlayerId별 큐)
            // PreFlop: P0 Call(20), P1 Call(20), P2 Call(10), P3 Check
            // Flop/Turn/River: 전원 Check (PostFlop 순서: P2, P3, P0, P1)
            var actions = new Dictionary<int, Queue<PlayerAction>>
            {
                {
                    0, new Queue<PlayerAction>(new[]
                    {
                        new PlayerAction("P0", ActionType.Call, 20),  // PreFlop
                        new PlayerAction("P0", ActionType.Check, 0),  // Flop
                        new PlayerAction("P0", ActionType.Check, 0),  // Turn
                        new PlayerAction("P0", ActionType.Check, 0),  // River
                    })
                },
                {
                    1, new Queue<PlayerAction>(new[]
                    {
                        new PlayerAction("P1", ActionType.Call, 20),  // PreFlop
                        new PlayerAction("P1", ActionType.Check, 0),  // Flop
                        new PlayerAction("P1", ActionType.Check, 0),  // Turn
                        new PlayerAction("P1", ActionType.Check, 0),  // River
                    })
                },
                {
                    2, new Queue<PlayerAction>(new[]
                    {
                        new PlayerAction("P2", ActionType.Call, 10),  // PreFlop (SB already 10)
                        new PlayerAction("P2", ActionType.Check, 0),  // Flop
                        new PlayerAction("P2", ActionType.Check, 0),  // Turn
                        new PlayerAction("P2", ActionType.Check, 0),  // River
                    })
                },
                {
                    3, new Queue<PlayerAction>(new[]
                    {
                        new PlayerAction("P3", ActionType.Check, 0),  // PreFlop (BB, no raise)
                        new PlayerAction("P3", ActionType.Check, 0),  // Flop
                        new PlayerAction("P3", ActionType.Check, 0),  // Turn
                        new PlayerAction("P3", ActionType.Check, 0),  // River
                    })
                }
            };

            var actionProvider = new StubPlayerActionProvider(actions);
            var broadcaster = new StubGameEventBroadcaster();
            var repository = new InMemoryGameStateRepository();

            // 실행
            var usecase = new GameRoundUsecase();
            usecase.PlayRound(state, random, actionProvider, broadcaster, repository).Wait();

            // 검증 1: 쇼다운 도달 — ShowdownResultEvent가 발행되었는지
            bool showdownCalled = broadcaster.GetEvents<ShowdownResultEvent>().Count > 0;
            Assert.IsTrue(showdownCalled, "쇼다운 이벤트가 호출되어야 한다.");

            // 검증 2: P0(AA)이 팟을 수령 — P0의 칩이 가장 많아야 한다
            // 팟 = 20 * 4 = 80. P0 chips = 1000 - 20 + 80 = 1060
            Assert.AreEqual(1060, state.Players[0].Chips,
                "P0(AA)이 메인 팟 80을 수령하여 1060칩이어야 한다.");

            // 검증 3: 4명 칩 합계 = 4000 (칩 보존 법칙)
            int totalChips = state.Players.Sum(p => p.Chips);
            Assert.AreEqual(4000, totalChips,
                "전체 칩 합계가 4000으로 보존되어야 한다.");

            // 검증 4: 라운드 종료 후 상태 정리 — 모든 플레이어 Waiting, HoleCards 비어 있음
            foreach (var player in state.Players)
            {
                Assert.AreEqual(PlayerStatus.Waiting, player.Status,
                    $"{player.Id}의 상태가 Waiting이어야 한다.");
                Assert.AreEqual(0, player.HoleCards.Count,
                    $"{player.Id}의 홀카드가 비어 있어야 한다.");
                Assert.AreEqual(0, player.CurrentBet,
                    $"{player.Id}의 CurrentBet이 0이어야 한다.");
            }

            // 검증 5: 커뮤니티 카드 초기화
            Assert.AreEqual(0, state.CommunityCards.Count,
                "커뮤니티 카드가 초기화되어야 한다.");

            // 검증 6: 팟 초기화
            Assert.AreEqual(0, state.Pots.Count,
                "팟이 초기화되어야 한다.");
        }

        // ────────────────────────────────────────────────────────────────
        // 시나리오 2 — 조기 종료
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Scenario2_EarlyTermination_RaiserWinsBlinds_NoShowdown()
        {
            // 4명, 1000칩, 블라인드 10/20, 초기 dealerIndex=0
            var players = CreatePlayers(4, 1000);
            var blinds = new BlindInfo(10, 20);
            var state = new GameState(players, blinds, dealerIndex: 0);

            // GetNextDealer: 0 → 1. Dealer=1, SB=2, BB=3
            // PreFlop 첫 액션: P0(UTG)

            // 덱은 아무 순서나 가능 (커뮤니티 카드가 딜링되지 않으므로)
            // 하지만 홀카드는 딜링되므로 유효한 52장 덱 필요
            var random = new FixedRandomSource(); // 무작동 모드: 기본 순서 유지

            // 액션: P0 Raise(100), P1 Fold, P2 Fold, P3 Fold
            var actions = new Dictionary<int, Queue<PlayerAction>>
            {
                {
                    0, new Queue<PlayerAction>(new[]
                    {
                        new PlayerAction("P0", ActionType.Raise, 100)
                    })
                },
                {
                    1, new Queue<PlayerAction>(new[]
                    {
                        new PlayerAction("P1", ActionType.Fold, 0)
                    })
                },
                {
                    2, new Queue<PlayerAction>(new[]
                    {
                        new PlayerAction("P2", ActionType.Fold, 0)
                    })
                },
                {
                    3, new Queue<PlayerAction>(new[]
                    {
                        new PlayerAction("P3", ActionType.Fold, 0)
                    })
                }
            };

            var actionProvider = new StubPlayerActionProvider(actions);
            var broadcaster = new StubGameEventBroadcaster();
            var repository = new InMemoryGameStateRepository();

            // 실행
            var usecase = new GameRoundUsecase();
            usecase.PlayRound(state, random, actionProvider, broadcaster, repository).Wait();

            var events = broadcaster.GetEvents();

            // 검증 1: OnShowdown 이벤트가 호출되지 않아야 한다
            bool showdownCalled = events.Any(e => e is ShowdownResultEvent);
            Assert.IsFalse(showdownCalled, "조기 종료 시 OnShowdown이 호출되지 않아야 한다.");

            // 검증 2: 커뮤니티 카드가 딜링되지 않았어야 한다 (OnCommunityCardsDealt 미호출)
            bool communityDealt = events.Any(e => e is CardsDealtEvent cde && cde.DealType != CardDealType.HoleCard);
            Assert.IsFalse(communityDealt, "조기 종료 시 커뮤니티 카드가 딜링되지 않아야 한다.");

            // 검증 3: P0이 팟(SB+BB+자신의 베팅에서 돌려받는 금액)을 수령
            // P2(SB)=10, P3(BB)=20, P0(Raise)=100 → 팟=130 → P0에게 130 지급
            // P0 최종: 1000 - 100 + 130 = 1030
            Assert.AreEqual(1030, state.Players[0].Chips,
                "P0이 팟 130을 수령하여 1030칩이어야 한다.");

            // 검증 4: 나머지 플레이어 칩 확인
            Assert.AreEqual(1000, state.Players[1].Chips,
                "P1은 베팅하지 않았으므로 1000칩이어야 한다.");
            Assert.AreEqual(990, state.Players[2].Chips,
                "P2(SB)는 10을 잃어 990칩이어야 한다.");
            Assert.AreEqual(980, state.Players[3].Chips,
                "P3(BB)는 20을 잃어 980칩이어야 한다.");

            // 검증 5: 4명 칩 합계 = 4000 (칩 보존 법칙)
            int totalChips = state.Players.Sum(p => p.Chips);
            Assert.AreEqual(4000, totalChips,
                "전체 칩 합계가 4000으로 보존되어야 한다.");

            // 검증 6: 라운드 종료 후 상태 정리
            foreach (var player in state.Players)
            {
                Assert.AreEqual(PlayerStatus.Waiting, player.Status,
                    $"{player.Id}의 상태가 Waiting이어야 한다.");
                Assert.AreEqual(0, player.HoleCards.Count,
                    $"{player.Id}의 홀카드가 비어 있어야 한다.");
                Assert.AreEqual(0, player.CurrentBet,
                    $"{player.Id}의 CurrentBet이 0이어야 한다.");
            }

            // 검증 7: OnRoundEnded 이벤트가 호출되었는지
            bool roundEnded = events.Any(e => e is HandEndedEvent);
            Assert.IsTrue(roundEnded, "OnRoundEnded 이벤트가 호출되어야 한다.");
        }
    }
}
