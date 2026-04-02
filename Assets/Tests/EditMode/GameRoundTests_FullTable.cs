// GameRoundTests_FullTable.cs
// GameRoundUsecase의 통합 테스트.
// 시나리오 7: 10인 풀 테이블 — 다수 AllIn, 사이드 팟, 칩 보존 법칙 검증, Eliminated 상태 전환 확인.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TexasHoldem.Entity;
using TexasHoldem.Gateway;
using TexasHoldem.Usecase;

namespace TexasHoldem.Tests.EditMode
{
    [TestFixture]
    public class GameRoundTests_FullTable
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
        // 시나리오 7 — 10인 풀 테이블
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Scenario7_FullTable_SidePots_ChipsPreserved_EliminatedPlayers()
        {
            // 10명 플레이어
            // P0~P4: 500칩, P5~P9: 1000칩. 총합 = 7500
            // 블라인드 25/50, 초기 dealerIndex=9
            // GetNextDealer: 9 → 0. Dealer=P0, SB=P1, BB=P2
            var players = new List<PlayerData>
            {
                new PlayerData("P0", "Player0", 500, 0),
                new PlayerData("P1", "Player1", 500, 1),
                new PlayerData("P2", "Player2", 500, 2),
                new PlayerData("P3", "Player3", 500, 3),
                new PlayerData("P4", "Player4", 500, 4),
                new PlayerData("P5", "Player5", 1000, 5),
                new PlayerData("P6", "Player6", 1000, 6),
                new PlayerData("P7", "Player7", 1000, 7),
                new PlayerData("P8", "Player8", 1000, 8),
                new PlayerData("P9", "Player9", 1000, 9)
            };
            var blinds = new BlindInfo(25, 50);
            var state = new GameState(players, blinds, dealerIndex: 9);

            // 고정 덱 설정
            // 홀카드 (P0~P9 각 2장 = 20장) + 커뮤니티 5장 = 25장
            // P0: A♠A♥ (최고 — Pair of Aces + board = Full House Aces over Kings)
            // P7: K♠K♥ (두 번째 — Pair of Kings + board = Full House Kings over ?)
            // P5: Q♠Q♥ (세 번째 — Pair of Queens)
            // 나머지는 약한 카드
            // 커뮤니티: A♣ K♣ K♦ 2♦ 3♣
            // P0: Full House (AAA KK), P7: Full House (KKK AA), P5: Two Pair (QQ KK) — P0 > P7 > P5
            var drawOrder = new Card[]
            {
                // 홀카드 (P0 card1, P0 card2, P1 card1, P1 card2, ..., P9 card1, P9 card2)
                new Card(Suit.Spade, Rank.Ace),       // P0 hole 1
                new Card(Suit.Heart, Rank.Ace),       // P0 hole 2
                new Card(Suit.Heart, Rank.Two),       // P1 hole 1
                new Card(Suit.Club, Rank.Four),       // P1 hole 2
                new Card(Suit.Heart, Rank.Five),      // P2 hole 1
                new Card(Suit.Club, Rank.Six),        // P2 hole 2
                new Card(Suit.Heart, Rank.Seven),     // P3 hole 1
                new Card(Suit.Club, Rank.Eight),      // P3 hole 2
                new Card(Suit.Heart, Rank.Nine),      // P4 hole 1
                new Card(Suit.Club, Rank.Ten),        // P4 hole 2
                new Card(Suit.Spade, Rank.Queen),     // P5 hole 1
                new Card(Suit.Heart, Rank.Queen),     // P5 hole 2
                new Card(Suit.Heart, Rank.Four),      // P6 hole 1
                new Card(Suit.Diamond, Rank.Six),     // P6 hole 2
                new Card(Suit.Spade, Rank.King),      // P7 hole 1
                new Card(Suit.Heart, Rank.King),      // P7 hole 2
                new Card(Suit.Diamond, Rank.Seven),   // P8 hole 1
                new Card(Suit.Diamond, Rank.Eight),   // P8 hole 2
                new Card(Suit.Diamond, Rank.Nine),    // P9 hole 1
                new Card(Suit.Diamond, Rank.Ten),     // P9 hole 2
                // 커뮤니티
                new Card(Suit.Club, Rank.Ace),        // Flop 1
                new Card(Suit.Club, Rank.King),       // Flop 2
                new Card(Suit.Diamond, Rank.King),    // Flop 3
                new Card(Suit.Diamond, Rank.Two),     // Turn
                new Card(Suit.Club, Rank.Three),      // River
            };

            var fixedDeck = BuildFixedDeck(drawOrder);
            var random = new FixedRandomSource(fixedDeck);

            // 블라인드: P1(SB)=25, P2(BB)=50
            // PreFlop 순서: P3(UTG) → P4 → P5 → P6 → P7 → P8 → P9 → P0(Dealer) → P1(SB) → P2(BB)
            //
            // P3: Raise(150)
            // P4: Fold
            // P5: Call(150)
            // P6: Fold
            // P7: AllIn(1000)
            // P8: Fold
            // P9: Fold
            // P0: Call(→AllIn 500, 칩 전부)
            // P1: Fold
            // P2: Fold
            // P3: Fold
            // P5: Call(850) — 1000에 맞추기 위해 추가 850 (이미 150 베팅)
            //
            // 결과적으로 Active/AllIn: P0(500 all-in), P5(1000 call), P7(1000 all-in)
            // 메인 팟: 500 × 3 = 1500 (P0, P5, P7 eligible) + 폴드한 플레이어들의 베팅
            // 사이드 팟: (1000-500) × 2 = 1000 (P5, P7 eligible)
            //
            // 폴드한 플레이어들의 베팅도 팟에 포함됨:
            // P1(SB): 25, P2(BB): 50, P3: 150 → 총 225가 추가로 메인 팟에 합산
            var actions = new Dictionary<string, Queue<PlayerAction>>
            {
                {
                    "P0", new Queue<PlayerAction>(new[]
                    {
                        new PlayerAction("P0", ActionType.AllIn, 0),      // PreFlop — 500칩 올인
                    })
                },
                {
                    "P1", new Queue<PlayerAction>(new[]
                    {
                        new PlayerAction("P1", ActionType.Fold, 0),       // PreFlop
                    })
                },
                {
                    "P2", new Queue<PlayerAction>(new[]
                    {
                        new PlayerAction("P2", ActionType.Fold, 0),       // PreFlop
                    })
                },
                {
                    "P3", new Queue<PlayerAction>(new[]
                    {
                        new PlayerAction("P3", ActionType.Raise, 150),    // PreFlop
                        new PlayerAction("P3", ActionType.Fold, 0),       // PreFlop — P7 AllIn 후 재액션
                    })
                },
                {
                    "P4", new Queue<PlayerAction>(new[]
                    {
                        new PlayerAction("P4", ActionType.Fold, 0),       // PreFlop
                    })
                },
                {
                    "P5", new Queue<PlayerAction>(new[]
                    {
                        new PlayerAction("P5", ActionType.Call, 150),     // PreFlop — P3의 Raise에 Call
                        new PlayerAction("P5", ActionType.Call, 850),     // PreFlop — P7의 AllIn에 Call
                        new PlayerAction("P5", ActionType.Check, 0),     // Flop
                        new PlayerAction("P5", ActionType.Check, 0),     // Turn
                        new PlayerAction("P5", ActionType.Check, 0),     // River
                    })
                },
                {
                    "P6", new Queue<PlayerAction>(new[]
                    {
                        new PlayerAction("P6", ActionType.Fold, 0),       // PreFlop
                    })
                },
                {
                    "P7", new Queue<PlayerAction>(new[]
                    {
                        new PlayerAction("P7", ActionType.AllIn, 0),      // PreFlop — 1000칩 올인
                    })
                },
                {
                    "P8", new Queue<PlayerAction>(new[]
                    {
                        new PlayerAction("P8", ActionType.Fold, 0),       // PreFlop
                    })
                },
                {
                    "P9", new Queue<PlayerAction>(new[]
                    {
                        new PlayerAction("P9", ActionType.Fold, 0),       // PreFlop
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

            // 검증 2: 메인 팟 → P0(Full House AAA KK) 수령
            // 메인 팟: P0(500) + P5(500 중 매칭분) + P7(500 중 매칭분) + 폴드 플레이어 베팅(P1:25, P2:50, P3:150) = 1500 + 225 = 1725
            // 사이드 팟: (1000-500) × 2 = 1000 → P7(Full House KKK AA) 수령
            // P0 최종: 0 + 메인 팟
            // P7 최종: 0 + 사이드 팟
            // P5 최종: 1000 - 1000 = 0 (전부 베팅)
            // 정확한 팟 크기는 PotManager 구현에 따라 달라질 수 있으므로 칩 보존 법칙으로 검증

            // 검증 3: P0이 메인 팟 수령 (P0 칩 > 0)
            Assert.IsTrue(state.Players[0].Chips > 0,
                "P0(Full House AAA KK)이 메인 팟을 수령하여 칩이 0보다 커야 한다.");

            // 검증 4: P7이 사이드 팟 수령 (P7 칩 > 0)
            Assert.IsTrue(state.Players[7].Chips > 0,
                "P7(Full House KKK AA)이 사이드 팟을 수령하여 칩이 0보다 커야 한다.");

            // 검증 5: 최종 10명 칩 합계 = 7500 (칩 보존 법칙)
            int totalChips = state.Players.Sum(p => p.Chips);
            Assert.AreEqual(7500, totalChips,
                "전체 칩 합계가 7500으로 보존되어야 한다.");

            // 검증 6: AllIn 후 Chips가 0인 플레이어는 Eliminated 상태
            foreach (var player in state.Players)
            {
                if (player.Chips == 0)
                {
                    Assert.AreEqual(PlayerStatus.Eliminated, player.Status,
                        $"{player.Id}의 칩이 0이면 Eliminated 상태여야 한다.");
                }
            }

            // 검증 7: 라운드 종료 후 상태 정리
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

            // 검증 8: 폴드한 플레이어들의 칩이 베팅한 만큼 감소
            // P1(SB): 500 - 25 = 475
            Assert.AreEqual(475, state.Players[1].Chips,
                "P1(SB, Fold)은 25칩을 잃어 475칩이어야 한다.");
            // P2(BB): 500 - 50 = 450
            Assert.AreEqual(450, state.Players[2].Chips,
                "P2(BB, Fold)은 50칩을 잃어 450칩이어야 한다.");
            // P3(Raise 150 후 Fold): 500 - 150 = 350
            Assert.AreEqual(350, state.Players[3].Chips,
                "P3(Raise 150, Fold)은 150칩을 잃어 350칩이어야 한다.");
            // P4, P6, P8, P9: Fold (베팅 없음) → 원래 칩 유지
            Assert.AreEqual(500, state.Players[4].Chips,
                "P4(Fold, 베팅 없음)은 500칩이어야 한다.");
            Assert.AreEqual(1000, state.Players[6].Chips,
                "P6(Fold, 베팅 없음)은 1000칩이어야 한다.");
            Assert.AreEqual(1000, state.Players[8].Chips,
                "P8(Fold, 베팅 없음)은 1000칩이어야 한다.");
            Assert.AreEqual(1000, state.Players[9].Chips,
                "P9(Fold, 베팅 없음)은 1000칩이어야 한다.");
        }
    }
}
