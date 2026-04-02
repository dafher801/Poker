// GameRoundTests_DealerFlow.cs
// GameRoundUsecase의 통합 테스트.
// 시나리오 5: 2명 헤즈업 — 딜러=SB 규칙 검증, 프리플롭 SB 선액션, 포스트플롭 BB 선액션.
// 시나리오 6: 3명 연속 2라운드 — 딜러 인덱스 이동 확인, 칩 합계 불변.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TexasHoldem.Entity;
using TexasHoldem.Gateway;
using TexasHoldem.Usecase;

namespace TexasHoldem.Tests.EditMode
{
    [TestFixture]
    public class GameRoundTests_DealerFlow
    {
        // ────────────────────────────────────────────────────────────────
        // 헬퍼
        // ────────────────────────────────────────────────────────────────

        private static Card[] BuildFixedDeck(Card[] drawOrder)
        {
            var usedSet = new HashSet<Card>(drawOrder);

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

            var deck = new Card[52];
            for (int i = 0; i < remaining.Count; i++)
                deck[i] = remaining[i];

            for (int i = 0; i < drawOrder.Length; i++)
                deck[52 - 1 - i] = drawOrder[i];

            return deck;
        }

        // ────────────────────────────────────────────────────────────────
        // 시나리오 5 — 헤즈업 블라인드
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Scenario5_HeadsUpBlinds_DealerIsSB_PreFlopSBActsFirst_PostFlopBBActsFirst()
        {
            // 2명, 칩 각 500, 블라인드 10/20
            // 초기 dealerIndex=1 → GetNextDealer: 1 → 0. Dealer=P0
            // 헤즈업: P0(Dealer)=SB(10), P1=BB(20)
            var players = new List<PlayerData>
            {
                new PlayerData("P0", "Player0", 500, 0),
                new PlayerData("P1", "Player1", 500, 1)
            };
            var blinds = new BlindInfo(10, 20);
            var state = new GameState(players, blinds, dealerIndex: 1);

            // 고정 덱 설정
            // 홀카드: P0=A♠K♠, P1=2♥3♥
            // 커뮤니티: 7♣ 8♦ 9♣ J♦ Q♦
            // P0: A-high straight없음, AK high → P0이 AK로 승리
            var drawOrder = new Card[]
            {
                // 홀카드
                new Card(Suit.Spade, Rank.Ace),      // P0 hole 1
                new Card(Suit.Spade, Rank.King),     // P0 hole 2
                new Card(Suit.Heart, Rank.Two),      // P1 hole 1
                new Card(Suit.Heart, Rank.Three),    // P1 hole 2
                // 커뮤니티
                new Card(Suit.Club, Rank.Seven),     // Flop 1
                new Card(Suit.Diamond, Rank.Eight),  // Flop 2
                new Card(Suit.Club, Rank.Nine),      // Flop 3
                new Card(Suit.Diamond, Rank.Jack),   // Turn
                new Card(Suit.Diamond, Rank.Queen),  // River
            };

            var fixedDeck = BuildFixedDeck(drawOrder);
            var random = new FixedRandomSource(fixedDeck);

            // 블라인드 후: P0(SB) chips=490, currentBet=10 / P1(BB) chips=480, currentBet=20
            // PreFlop: 헤즈업에서 SB(=Dealer=P0)가 선액션
            //   P0 Call(10) → currentBet=20
            //   P1 Check
            // PostFlop (Flop/Turn/River): BB(=비딜러=P1)가 선액션
            //   P1 Check, P0 Check
            var actions = new Dictionary<int, Queue<PlayerAction>>
            {
                {
                    0, new Queue<PlayerAction>(new[]
                    {
                        new PlayerAction("P0", ActionType.Call, 10),    // PreFlop (SB → Call to match BB)
                        new PlayerAction("P0", ActionType.Check, 0),   // Flop
                        new PlayerAction("P0", ActionType.Check, 0),   // Turn
                        new PlayerAction("P0", ActionType.Check, 0),   // River
                    })
                },
                {
                    1, new Queue<PlayerAction>(new[]
                    {
                        new PlayerAction("P1", ActionType.Check, 0),   // PreFlop (BB, no raise → Check)
                        new PlayerAction("P1", ActionType.Check, 0),   // Flop
                        new PlayerAction("P1", ActionType.Check, 0),   // Turn
                        new PlayerAction("P1", ActionType.Check, 0),   // River
                    })
                }
            };

            var actionProvider = new StubPlayerActionProvider(actions);
            var broadcaster = new StubGameEventBroadcaster();
            var repository = new InMemoryGameStateRepository();

            // 실행
            var usecase = new GameRoundUsecase();
            usecase.PlayRound(state, random, actionProvider, broadcaster, repository).Wait();

            var log = broadcaster.GetLog();

            // 검증 1: OnBlindsPosted — P0이 SB(10), P1이 BB(20)
            var blindsEvent = log.FirstOrDefault(e => e.EventName == "OnBlindsPosted");
            Assert.IsNotNull(blindsEvent, "OnBlindsPosted 이벤트가 호출되어야 한다.");
            Assert.AreEqual("P0", blindsEvent.Args[0], "SB 플레이어는 P0이어야 한다.");
            Assert.AreEqual(10, blindsEvent.Args[1], "SB 금액은 10이어야 한다.");
            Assert.AreEqual("P1", blindsEvent.Args[2], "BB 플레이어는 P1이어야 한다.");
            Assert.AreEqual(20, blindsEvent.Args[3], "BB 금액은 20이어야 한다.");

            // 검증 2: 프리플롭 첫 액션이 P0(SB=딜러)인지 확인
            var playerActedEvents = log.Where(e => e.EventName == "OnPlayerActed").ToList();
            Assert.IsTrue(playerActedEvents.Count > 0, "OnPlayerActed 이벤트가 최소 1개 이상 있어야 한다.");
            Assert.AreEqual("P0", playerActedEvents[0].Args[0],
                "프리플롭 첫 액션 차례는 P0(SB=딜러)이어야 한다.");

            // 검증 3: 포스트플롭(Flop) 첫 액션이 P1(BB=비딜러)인지 확인
            // PreFlop에서 P0 Call, P1 Check → 2개 이벤트 후 Flop 시작
            // Flop 첫 액션은 playerActedEvents[2] (0-indexed)
            Assert.IsTrue(playerActedEvents.Count >= 3,
                "Flop까지 최소 3개의 OnPlayerActed 이벤트가 있어야 한다.");
            Assert.AreEqual("P1", playerActedEvents[2].Args[0],
                "포스트플롭 첫 액션 차례는 P1(BB=비딜러)이어야 한다.");

            // 검증 4: 쇼다운 도달
            bool showdownCalled = log.Any(e => e.EventName == "OnShowdown");
            Assert.IsTrue(showdownCalled, "쇼다운 이벤트가 호출되어야 한다.");

            // 검증 5: 칩 합계 = 1000 불변
            int totalChips = state.Players.Sum(p => p.Chips);
            Assert.AreEqual(1000, totalChips,
                "전체 칩 합계가 1000으로 보존되어야 한다.");

            // 검증 6: 라운드 종료 후 상태 정리
            foreach (var player in state.Players)
            {
                Assert.AreEqual(0, player.HoleCards.Count,
                    $"{player.Id}의 홀카드가 비어 있어야 한다.");
                Assert.AreEqual(0, player.CurrentBet,
                    $"{player.Id}의 CurrentBet이 0이어야 한다.");
            }

            Assert.AreEqual(0, state.CommunityCards.Count,
                "커뮤니티 카드가 초기화되어야 한다.");
            Assert.AreEqual(0, state.Pots.Count,
                "팟이 초기화되어야 한다.");
        }

        // ────────────────────────────────────────────────────────────────
        // 시나리오 6 — 연속 라운드
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Scenario6_ConsecutiveRounds_DealerRotatesCorrectly_ChipsPreserved()
        {
            // 3명, 칩 각 1000, 블라인드 10/20
            // 초기 dealerIndex=2 → 1차 라운드 GetNextDealer: 2 → 0
            // 1차: Dealer=0, SB=1, BB=2
            var players = new List<PlayerData>
            {
                new PlayerData("P0", "Player0", 1000, 0),
                new PlayerData("P1", "Player1", 1000, 1),
                new PlayerData("P2", "Player2", 1000, 2)
            };
            var blinds = new BlindInfo(10, 20);
            var state = new GameState(players, blinds, dealerIndex: 2);

            // ── 1차 라운드 ──
            // Dealer=0, SB=1, BB=2
            // PreFlop 순서: P0(UTG=Dealer, 3인에서 Dealer가 UTG) → P1 → P2
            // P0 Fold, P1 Call(10) (SB→BB 매칭), P2 Check (BB)
            // PostFlop 순서: P1(SB) → P2(BB) — P0 folded
            // Flop/Turn/River: P1 Check, P2 Check

            // 고정 덱 (1차 라운드): P1이 승리
            // P0=2♣3♣, P1=A♠A♥, P2=4♦5♦
            // 커뮤니티: 7♠ 8♠ J♣ Q♥ K♦
            var drawOrder1 = new Card[]
            {
                new Card(Suit.Club, Rank.Two),       // P0 hole 1
                new Card(Suit.Club, Rank.Three),     // P0 hole 2
                new Card(Suit.Spade, Rank.Ace),      // P1 hole 1
                new Card(Suit.Heart, Rank.Ace),      // P1 hole 2
                new Card(Suit.Diamond, Rank.Four),   // P2 hole 1
                new Card(Suit.Diamond, Rank.Five),   // P2 hole 2
                new Card(Suit.Spade, Rank.Seven),    // Flop 1
                new Card(Suit.Spade, Rank.Eight),    // Flop 2
                new Card(Suit.Club, Rank.Jack),      // Flop 3
                new Card(Suit.Heart, Rank.Queen),    // Turn
                new Card(Suit.Diamond, Rank.King),   // River
            };

            var fixedDeck1 = BuildFixedDeck(drawOrder1);
            var random1 = new FixedRandomSource(fixedDeck1);

            var actions1 = new Dictionary<int, Queue<PlayerAction>>
            {
                {
                    0, new Queue<PlayerAction>(new[]
                    {
                        new PlayerAction("P0", ActionType.Fold, 0),      // PreFlop
                    })
                },
                {
                    1, new Queue<PlayerAction>(new[]
                    {
                        new PlayerAction("P1", ActionType.Call, 10),     // PreFlop (SB → Call 10)
                        new PlayerAction("P1", ActionType.Check, 0),    // Flop
                        new PlayerAction("P1", ActionType.Check, 0),    // Turn
                        new PlayerAction("P1", ActionType.Check, 0),    // River
                    })
                },
                {
                    2, new Queue<PlayerAction>(new[]
                    {
                        new PlayerAction("P2", ActionType.Check, 0),    // PreFlop (BB)
                        new PlayerAction("P2", ActionType.Check, 0),    // Flop
                        new PlayerAction("P2", ActionType.Check, 0),    // Turn
                        new PlayerAction("P2", ActionType.Check, 0),    // River
                    })
                }
            };

            var actionProvider1 = new StubPlayerActionProvider(actions1);
            var broadcaster1 = new StubGameEventBroadcaster();
            var repository = new InMemoryGameStateRepository();

            var usecase = new GameRoundUsecase();
            usecase.PlayRound(state, random1, actionProvider1, broadcaster1, repository).Wait();

            // 1차 라운드 검증: 딜러=0
            Assert.AreEqual(0, state.DealerIndex,
                "1차 라운드 딜러 인덱스가 0이어야 한다.");

            // 1차 라운드: P1(AA) 승리. 팟 = 20+20 = 40
            // P0: 1000 (Fold, 베팅 없음), P1: 1000-20+40 = 1020, P2: 1000-20 = 980
            int totalAfterRound1 = state.Players.Sum(p => p.Chips);
            Assert.AreEqual(3000, totalAfterRound1,
                "1차 라운드 후 칩 합계가 3000이어야 한다.");

            // ── 2차 라운드 ──
            // GetNextDealer: 0 → 1. Dealer=1, SB=2, BB=0
            // PreFlop 순서: P1(UTG=Dealer) → P2 → P0
            // 간단히 조기 종료: P1 Raise, P2 Fold, P0 Fold

            var random2 = new FixedRandomSource(); // 기본 덱 순서 (조기 종료이므로 커뮤니티 불필요)

            var actions2 = new Dictionary<int, Queue<PlayerAction>>
            {
                {
                    0, new Queue<PlayerAction>(new[]
                    {
                        new PlayerAction("P0", ActionType.Fold, 0),
                    })
                },
                {
                    1, new Queue<PlayerAction>(new[]
                    {
                        new PlayerAction("P1", ActionType.Raise, 100),
                    })
                },
                {
                    2, new Queue<PlayerAction>(new[]
                    {
                        new PlayerAction("P2", ActionType.Fold, 0),
                    })
                }
            };

            var actionProvider2 = new StubPlayerActionProvider(actions2);
            var broadcaster2 = new StubGameEventBroadcaster();

            usecase.PlayRound(state, random2, actionProvider2, broadcaster2, repository).Wait();

            // 2차 라운드 검증: 딜러=1
            Assert.AreEqual(1, state.DealerIndex,
                "2차 라운드 딜러 인덱스가 1이어야 한다.");

            // 2차 라운드 블라인드 검증: SB=P2, BB=P0
            var log2 = broadcaster2.GetLog();
            var blindsEvent2 = log2.FirstOrDefault(e => e.EventName == "OnBlindsPosted");
            Assert.IsNotNull(blindsEvent2, "2차 라운드 OnBlindsPosted 이벤트가 호출되어야 한다.");
            Assert.AreEqual("P2", blindsEvent2.Args[0], "2차 라운드 SB 플레이어는 P2이어야 한다.");
            Assert.AreEqual("P0", blindsEvent2.Args[2], "2차 라운드 BB 플레이어는 P0이어야 한다.");

            // 2차 라운드 후 칩 합계 = 3000 불변
            int totalAfterRound2 = state.Players.Sum(p => p.Chips);
            Assert.AreEqual(3000, totalAfterRound2,
                "2차 라운드 후 칩 합계가 3000으로 보존되어야 한다.");

            // 라운드 종료 후 상태 정리 검증
            foreach (var player in state.Players)
            {
                Assert.AreEqual(0, player.HoleCards.Count,
                    $"{player.Id}의 홀카드가 비어 있어야 한다.");
                Assert.AreEqual(0, player.CurrentBet,
                    $"{player.Id}의 CurrentBet이 0이어야 한다.");
            }

            Assert.AreEqual(0, state.CommunityCards.Count,
                "커뮤니티 카드가 초기화되어야 한다.");
            Assert.AreEqual(0, state.Pots.Count,
                "팟이 초기화되어야 한다.");
        }
    }
}
