// GameRoundTests_Pots.cs
// GameRoundUsecase의 통합 테스트.
// 시나리오 3: 3명 플레이어, 칩 불균등, AllIn으로 사이드 팟 생성 → 메인 팟/사이드 팟 별도 승자 배분, 칩 합계 불변.
// 시나리오 4: 2명 플레이어, 동일 족보(보드 로열 플러시) → 스플릿 팟, 칩 합계 불변.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TexasHoldem.Entity;
using TexasHoldem.Gateway;
using TexasHoldem.Usecase;

namespace TexasHoldem.Tests.EditMode
{
    [TestFixture]
    public class GameRoundTests_Pots
    {
        // ────────────────────────────────────────────────────────────────
        // 헬퍼
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// 지정 카드를 Draw 순서대로 배치한 52장 고정 덱을 생성한다.
        /// drawOrder[0]이 첫 번째 Draw() 결과가 된다.
        /// Deck.Draw()는 리스트 끝에서 꺼내므로 drawOrder를 역순으로 배치한다.
        /// </summary>
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
        // 시나리오 3 — 사이드 팟
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Scenario3_SidePot_MainPotToShortStack_SidePotToSecond_ChipsPreserved()
        {
            // 3명, 칩: P0(A)=100, P1(B)=200, P2(C)=500, 블라인드 10/20, 초기 dealerIndex=0
            // GetNextDealer: 0 → 1. Dealer=P1, SB=P2, BB=P0
            var players = new List<PlayerData>
            {
                new PlayerData("P0", "PlayerA", 100, 0),
                new PlayerData("P1", "PlayerB", 200, 1),
                new PlayerData("P2", "PlayerC", 500, 2)
            };
            var blinds = new BlindInfo(10, 20);
            var state = new GameState(players, blinds, dealerIndex: 0);

            // 고정 덱 설정
            // 홀카드: P0=K♠K♥, P1=9♣9♥, P2=4♠5♦
            // 커뮤니티: 2♠ 3♦ 7♥ K♣ 9♠
            // P0: Three Kings (최고), P1: Three Nines (두 번째), P2: High card (최하위)
            var drawOrder = new Card[]
            {
                // 홀카드 (P0 card1, P0 card2, P1 card1, P1 card2, P2 card1, P2 card2)
                new Card(Suit.Spade, Rank.King),    // P0 hole 1
                new Card(Suit.Heart, Rank.King),    // P0 hole 2
                new Card(Suit.Club, Rank.Nine),     // P1 hole 1
                new Card(Suit.Heart, Rank.Nine),    // P1 hole 2
                new Card(Suit.Spade, Rank.Four),    // P2 hole 1
                new Card(Suit.Diamond, Rank.Five),  // P2 hole 2
                // 커뮤니티 (Flop1, Flop2, Flop3, Turn, River)
                new Card(Suit.Spade, Rank.Two),     // Flop 1
                new Card(Suit.Diamond, Rank.Three), // Flop 2
                new Card(Suit.Heart, Rank.Seven),   // Flop 3
                new Card(Suit.Club, Rank.King),     // Turn
                new Card(Suit.Spade, Rank.Nine),    // River
            };

            var fixedDeck = BuildFixedDeck(drawOrder);
            var random = new FixedRandomSource(fixedDeck);

            // 블라인드 후 상태: P2(SB)=10 posted (chips 490), P0(BB)=20 posted (chips 80)
            // PreFlop 순서: P1(UTG) → P2 → P0
            // P1: AllIn(200칩 전부) → currentBet=200, 레이즈에 해당(200>20)
            // P2: Call(190) → currentBet=200 (10+190)
            // P0: AllIn(80칩 전부) → currentBet=100 (20+80), 레이즈 아님(100<200)
            // Flop/Turn/River: P2만 Active → Check
            var actions = new Dictionary<string, Queue<PlayerAction>>
            {
                {
                    "P0", new Queue<PlayerAction>(new[]
                    {
                        new PlayerAction("P0", ActionType.AllIn, 0),   // PreFlop — 80칩 올인
                    })
                },
                {
                    "P1", new Queue<PlayerAction>(new[]
                    {
                        new PlayerAction("P1", ActionType.AllIn, 0),   // PreFlop — 200칩 올인
                    })
                },
                {
                    "P2", new Queue<PlayerAction>(new[]
                    {
                        new PlayerAction("P2", ActionType.Call, 190),   // PreFlop — Call to match 200
                        new PlayerAction("P2", ActionType.Check, 0),   // Flop
                        new PlayerAction("P2", ActionType.Check, 0),   // Turn
                        new PlayerAction("P2", ActionType.Check, 0),   // River
                    })
                }
            };

            var actionProvider = new StubPlayerActionProvider(actions);
            var broadcaster = new StubGameEventBroadcaster();
            var repository = new InMemoryGameStateRepository();

            // 실행
            var usecase = new GameRoundUsecase();
            usecase.PlayRound(state, random, actionProvider, broadcaster, repository).Wait();

            // 검증 1: 쇼다운 도달
            var log = broadcaster.GetLog();
            bool showdownCalled = log.Any(e => e.EventName == "OnShowdown");
            Assert.IsTrue(showdownCalled, "쇼다운 이벤트가 호출되어야 한다.");

            // 검증 2: 메인 팟(100×3=300) → P0(A) 수령
            // P0 최종 칩: 0 + 300 = 300
            Assert.AreEqual(300, state.Players[0].Chips,
                "P0(Three Kings)이 메인 팟 300을 수령하여 300칩이어야 한다.");

            // 검증 3: 사이드 팟((200-100)×2=200) → P1(B) 수령
            // P1 최종 칩: 0 + 200 = 200
            Assert.AreEqual(200, state.Players[1].Chips,
                "P1(Three Nines)이 사이드 팟 200을 수령하여 200칩이어야 한다.");

            // 검증 4: P2(C) 칩 = 500 - 200 = 300
            Assert.AreEqual(300, state.Players[2].Chips,
                "P2는 팟을 받지 못하고 300칩이어야 한다.");

            // 검증 5: 칩 합계 = 800 불변 (100+200+500)
            int totalChips = state.Players.Sum(p => p.Chips);
            Assert.AreEqual(800, totalChips,
                "전체 칩 합계가 800으로 보존되어야 한다.");

            // 검증 6: 라운드 종료 후 상태 정리
            foreach (var player in state.Players)
            {
                Assert.AreEqual(0, player.HoleCards.Count,
                    $"{player.Id}의 홀카드가 비어 있어야 한다.");
                Assert.AreEqual(0, player.CurrentBet,
                    $"{player.Id}의 CurrentBet이 0이어야 한다.");
            }

            // 검증 7: 커뮤니티 카드 및 팟 초기화
            Assert.AreEqual(0, state.CommunityCards.Count,
                "커뮤니티 카드가 초기화되어야 한다.");
            Assert.AreEqual(0, state.Pots.Count,
                "팟이 초기화되어야 한다.");
        }

        // ────────────────────────────────────────────────────────────────
        // 시나리오 4 — 스플릿 팟
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Scenario4_SplitPot_IdenticalHand_PotSplitEvenly_ChipsPreserved()
        {
            // 2명, 칩 각 500, 블라인드 10/20, 초기 dealerIndex=0
            // GetNextDealer: 0 → 1. 헤즈업: Dealer=P1(SB), P0(BB)
            var players = new List<PlayerData>
            {
                new PlayerData("P0", "Player0", 500, 0),
                new PlayerData("P1", "Player1", 500, 1)
            };
            var blinds = new BlindInfo(10, 20);
            var state = new GameState(players, blinds, dealerIndex: 0);

            // 고정 덱 설정
            // 홀카드: P0=2♥3♥, P1=4♥5♥ (둘 다 보드보다 약한 카드)
            // 커뮤니티: A♠ K♠ Q♠ J♠ T♠ (보드 로열 플러시)
            // 양측 최선 핸드 = 보드의 Royal Flush → 동점
            var drawOrder = new Card[]
            {
                // 홀카드
                new Card(Suit.Heart, Rank.Two),     // P0 hole 1
                new Card(Suit.Heart, Rank.Three),   // P0 hole 2
                new Card(Suit.Heart, Rank.Four),    // P1 hole 1
                new Card(Suit.Heart, Rank.Five),    // P1 hole 2
                // 커뮤니티
                new Card(Suit.Spade, Rank.Ace),     // Flop 1
                new Card(Suit.Spade, Rank.King),    // Flop 2
                new Card(Suit.Spade, Rank.Queen),   // Flop 3
                new Card(Suit.Spade, Rank.Jack),    // Turn
                new Card(Suit.Spade, Rank.Ten),     // River
            };

            var fixedDeck = BuildFixedDeck(drawOrder);
            var random = new FixedRandomSource(fixedDeck);

            // 블라인드 후: P1(SB) currentBet=10 (chips 490), P0(BB) currentBet=20 (chips 480)
            // PreFlop: P1(SB/Dealer, 헤즈업 선액션) Call(10) → currentBet=20, P0 Check
            // Flop: P0 Check, P1 Check
            // Turn: P0 Check, P1 Check
            // River: P0 Check, P1 Check
            var actions = new Dictionary<string, Queue<PlayerAction>>
            {
                {
                    "P0", new Queue<PlayerAction>(new[]
                    {
                        new PlayerAction("P0", ActionType.Check, 0),   // PreFlop (BB, no raise)
                        new PlayerAction("P0", ActionType.Check, 0),   // Flop
                        new PlayerAction("P0", ActionType.Check, 0),   // Turn
                        new PlayerAction("P0", ActionType.Check, 0),   // River
                    })
                },
                {
                    "P1", new Queue<PlayerAction>(new[]
                    {
                        new PlayerAction("P1", ActionType.Call, 10),    // PreFlop (SB→Call 10 to match BB)
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

            // 검증 1: 쇼다운 도달
            bool showdownCalled = log.Any(e => e.EventName == "OnShowdown");
            Assert.IsTrue(showdownCalled, "쇼다운 이벤트가 호출되어야 한다.");

            // 검증 2: 팟 = 40 (20+20), 정확히 2등분 → 각 20칩 수령
            // P0: 480 + 20 = 500, P1: 480 + 20 = 500
            Assert.AreEqual(500, state.Players[0].Chips,
                "P0이 스플릿 팟 20을 수령하여 500칩이어야 한다.");
            Assert.AreEqual(500, state.Players[1].Chips,
                "P1이 스플릿 팟 20을 수령하여 500칩이어야 한다.");

            // 검증 3: 칩 합계 = 1000 불변
            int totalChips = state.Players.Sum(p => p.Chips);
            Assert.AreEqual(1000, totalChips,
                "전체 칩 합계가 1000으로 보존되어야 한다.");

            // 검증 4: 라운드 종료 후 상태 정리
            foreach (var player in state.Players)
            {
                Assert.AreEqual(PlayerStatus.Waiting, player.Status,
                    $"{player.Id}의 상태가 Waiting이어야 한다.");
                Assert.AreEqual(0, player.HoleCards.Count,
                    $"{player.Id}의 홀카드가 비어 있어야 한다.");
                Assert.AreEqual(0, player.CurrentBet,
                    $"{player.Id}의 CurrentBet이 0이어야 한다.");
            }

            // 검증 5: 커뮤니티 카드 및 팟 초기화
            Assert.AreEqual(0, state.CommunityCards.Count,
                "커뮤니티 카드가 초기화되어야 한다.");
            Assert.AreEqual(0, state.Pots.Count,
                "팟이 초기화되어야 한다.");
        }
    }
}
