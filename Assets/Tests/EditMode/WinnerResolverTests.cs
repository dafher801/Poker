// WinnerResolverTests.cs
// WinnerResolver의 단위 테스트.
// 조기 종료, 쇼다운, 사이드 팟, 스플릿 팟 시나리오를 검증한다.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TexasHoldem.Entity;
using TexasHoldem.Usecase;

namespace TexasHoldem.Tests.EditMode
{
    [TestFixture]
    public class WinnerResolverTests
    {
        private WinnerResolver _resolver;

        [SetUp]
        public void SetUp()
        {
            _resolver = new WinnerResolver();
        }

        // ────────────────────────────────────────────────────────────────
        // 조기 종료
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Resolve_OneActivePlayer_GetsEntirePot()
        {
            var players = CreatePlayers(4, 1000);
            var state = new GameState(players, new BlindInfo(10, 20));

            // Player[0]만 Active, 나머지 Folded
            players[0].Status = PlayerStatus.Active;
            players[1].Status = PlayerStatus.Folded;
            players[2].Status = PlayerStatus.Folded;
            players[3].Status = PlayerStatus.Folded;

            // 팟 설정
            state.Pots.Add(new Pot(100, new List<string> { "P0", "P1", "P2", "P3" }));

            var result = _resolver.Resolve(state);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("P0", result[0].PlayerId);
            Assert.AreEqual(100, result[0].Amount);
        }

        [Test]
        public void Resolve_OneActivePlayer_MultiplePots_GetsAll()
        {
            var players = CreatePlayers(3, 1000);
            var state = new GameState(players, new BlindInfo(10, 20));

            players[0].Status = PlayerStatus.Active;
            players[1].Status = PlayerStatus.Folded;
            players[2].Status = PlayerStatus.Folded;

            state.Pots.Add(new Pot(150, new List<string> { "P0", "P1", "P2" }));
            state.Pots.Add(new Pot(50, new List<string> { "P0", "P1" }));

            var result = _resolver.Resolve(state);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("P0", result[0].PlayerId);
            Assert.AreEqual(200, result[0].Amount);
        }

        [Test]
        public void Resolve_OneAllInPlayer_RestFolded_GetsEntirePot()
        {
            var players = CreatePlayers(3, 1000);
            var state = new GameState(players, new BlindInfo(10, 20));

            players[0].Status = PlayerStatus.AllIn;
            players[1].Status = PlayerStatus.Folded;
            players[2].Status = PlayerStatus.Folded;

            state.Pots.Add(new Pot(300, new List<string> { "P0", "P1", "P2" }));

            var result = _resolver.Resolve(state);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("P0", result[0].PlayerId);
            Assert.AreEqual(300, result[0].Amount);
        }

        // ────────────────────────────────────────────────────────────────
        // 쇼다운 — 기본
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Resolve_Showdown_BestHandWins()
        {
            var players = CreatePlayers(2, 1000);
            var state = new GameState(players, new BlindInfo(10, 20));

            players[0].Status = PlayerStatus.Active;
            players[1].Status = PlayerStatus.Active;

            // Player[0]: Ace-King → OnePair of Aces
            players[0].AddHoleCard(new Card(Suit.Spade, Rank.Ace));
            players[0].AddHoleCard(new Card(Suit.Heart, Rank.King));

            // Player[1]: Two-Three → nothing special
            players[1].AddHoleCard(new Card(Suit.Diamond, Rank.Two));
            players[1].AddHoleCard(new Card(Suit.Club, Rank.Three));

            // Community: Ace, 7, 8, 9, 10
            state.AddCommunityCard(new Card(Suit.Club, Rank.Ace));
            state.AddCommunityCard(new Card(Suit.Diamond, Rank.Seven));
            state.AddCommunityCard(new Card(Suit.Heart, Rank.Eight));
            state.AddCommunityCard(new Card(Suit.Spade, Rank.Nine));
            state.AddCommunityCard(new Card(Suit.Club, Rank.Ten));

            state.Pots.Add(new Pot(200, new List<string> { "P0", "P1" }));

            var result = _resolver.Resolve(state);

            // Player[0] has pair of Aces, Player[1] has high card
            var p0Payout = result.Where(r => r.PlayerId == "P0").Sum(r => r.Amount);
            Assert.AreEqual(200, p0Payout);
        }

        [Test]
        public void Resolve_Showdown_AllInPlayerCanWin()
        {
            var players = CreatePlayers(2, 1000);
            var state = new GameState(players, new BlindInfo(10, 20));

            players[0].Status = PlayerStatus.AllIn;
            players[1].Status = PlayerStatus.Active;

            // Player[0]: Pocket Aces
            players[0].AddHoleCard(new Card(Suit.Spade, Rank.Ace));
            players[0].AddHoleCard(new Card(Suit.Heart, Rank.Ace));

            // Player[1]: Two-Three
            players[1].AddHoleCard(new Card(Suit.Diamond, Rank.Two));
            players[1].AddHoleCard(new Card(Suit.Club, Rank.Three));

            // Community: 5, 6, 7, 8, 9
            state.AddCommunityCard(new Card(Suit.Club, Rank.Five));
            state.AddCommunityCard(new Card(Suit.Diamond, Rank.Six));
            state.AddCommunityCard(new Card(Suit.Heart, Rank.Seven));
            state.AddCommunityCard(new Card(Suit.Spade, Rank.Eight));
            state.AddCommunityCard(new Card(Suit.Club, Rank.Nine));

            state.Pots.Add(new Pot(400, new List<string> { "P0", "P1" }));

            var result = _resolver.Resolve(state);

            // Both have straight 5-9, but Player[0]'s Ace doesn't help
            // Both have same straight → split pot
            var p0Payout = result.Where(r => r.PlayerId == "P0").Sum(r => r.Amount);
            var p1Payout = result.Where(r => r.PlayerId == "P1").Sum(r => r.Amount);
            Assert.AreEqual(200, p0Payout);
            Assert.AreEqual(200, p1Payout);
        }

        // ────────────────────────────────────────────────────────────────
        // 사이드 팟
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Resolve_SidePot_BestHandWinsMainPot()
        {
            var players = CreatePlayers(3, 1000);
            var state = new GameState(players, new BlindInfo(10, 20));

            players[0].Status = PlayerStatus.AllIn;
            players[1].Status = PlayerStatus.AllIn;
            players[2].Status = PlayerStatus.Active;

            // Player[0]: Pocket Aces (best)
            players[0].AddHoleCard(new Card(Suit.Spade, Rank.Ace));
            players[0].AddHoleCard(new Card(Suit.Heart, Rank.Ace));

            // Player[1]: Pocket Kings (second)
            players[1].AddHoleCard(new Card(Suit.Spade, Rank.King));
            players[1].AddHoleCard(new Card(Suit.Heart, Rank.King));

            // Player[2]: Pocket Twos (worst)
            players[2].AddHoleCard(new Card(Suit.Spade, Rank.Two));
            players[2].AddHoleCard(new Card(Suit.Heart, Rank.Two));

            // Community: 5, 6, 7, 8, Jack (no straights/flushes)
            state.AddCommunityCard(new Card(Suit.Club, Rank.Five));
            state.AddCommunityCard(new Card(Suit.Diamond, Rank.Six));
            state.AddCommunityCard(new Card(Suit.Club, Rank.Seven));
            state.AddCommunityCard(new Card(Suit.Diamond, Rank.Eight));
            state.AddCommunityCard(new Card(Suit.Club, Rank.Jack));

            // Main pot: P0, P1, P2 eligible
            state.Pots.Add(new Pot(300, new List<string> { "P0", "P1", "P2" }));
            // Side pot: P1, P2 eligible
            state.Pots.Add(new Pot(200, new List<string> { "P1", "P2" }));

            var result = _resolver.Resolve(state);

            var p0Payout = result.Where(r => r.PlayerId == "P0").Sum(r => r.Amount);
            var p1Payout = result.Where(r => r.PlayerId == "P1").Sum(r => r.Amount);
            var p2Payout = result.Where(r => r.PlayerId == "P2").Sum(r => r.Amount);

            // P0 wins main pot (300), P1 wins side pot (200)
            Assert.AreEqual(300, p0Payout);
            Assert.AreEqual(200, p1Payout);
            Assert.AreEqual(0, p2Payout);
        }

        // ────────────────────────────────────────────────────────────────
        // 칩 보존
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Resolve_TotalPayoutsEqualTotalPots()
        {
            var players = CreatePlayers(4, 1000);
            var state = new GameState(players, new BlindInfo(10, 20));

            players[0].Status = PlayerStatus.Active;
            players[1].Status = PlayerStatus.Active;
            players[2].Status = PlayerStatus.Folded;
            players[3].Status = PlayerStatus.Folded;

            players[0].AddHoleCard(new Card(Suit.Spade, Rank.Ace));
            players[0].AddHoleCard(new Card(Suit.Heart, Rank.King));

            players[1].AddHoleCard(new Card(Suit.Diamond, Rank.Queen));
            players[1].AddHoleCard(new Card(Suit.Club, Rank.Jack));

            state.AddCommunityCard(new Card(Suit.Club, Rank.Two));
            state.AddCommunityCard(new Card(Suit.Diamond, Rank.Five));
            state.AddCommunityCard(new Card(Suit.Heart, Rank.Eight));
            state.AddCommunityCard(new Card(Suit.Spade, Rank.Nine));
            state.AddCommunityCard(new Card(Suit.Club, Rank.Four));

            state.Pots.Add(new Pot(500, new List<string> { "P0", "P1", "P2", "P3" }));

            var result = _resolver.Resolve(state);

            int totalPayout = result.Sum(r => r.Amount);
            Assert.AreEqual(500, totalPayout);
        }

        // ────────────────────────────────────────────────────────────────
        // 헬퍼
        // ────────────────────────────────────────────────────────────────

        private List<PlayerData> CreatePlayers(int count, int chips)
        {
            var players = new List<PlayerData>();
            for (int i = 0; i < count; i++)
            {
                players.Add(new PlayerData($"P{i}", $"Player{i}", chips, i));
            }
            return players;
        }
    }
}
