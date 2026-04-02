// DealerRotationTests.cs
// DealerRotation 유틸리티의 단위 테스트.
// 딜러 이동, 블라인드 포지션 결정, Eliminated 플레이어 건너뛰기,
// 헤즈업 특수 규칙 등을 검증한다.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using TexasHoldem.Entity;
using TexasHoldem.Usecase;

namespace TexasHoldem.Tests.EditMode
{
    [TestFixture]
    public class DealerRotationTests
    {
        // ────────────────────────────────────────────────────────────────
        // GetNextDealer
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void GetNextDealer_ReturnsNextActivePlayer()
        {
            var players = CreatePlayers(4, 1000);
            var state = new GameState(players, new BlindInfo(10, 20), dealerIndex: 0);

            int next = DealerRotation.GetNextDealer(state);

            Assert.AreEqual(1, next);
        }

        [Test]
        public void GetNextDealer_SkipsEliminatedPlayers()
        {
            var players = CreatePlayers(4, 1000);
            players[1].Status = PlayerStatus.Eliminated;
            var state = new GameState(players, new BlindInfo(10, 20), dealerIndex: 0);

            int next = DealerRotation.GetNextDealer(state);

            Assert.AreEqual(2, next);
        }

        [Test]
        public void GetNextDealer_WrapsAround()
        {
            var players = CreatePlayers(4, 1000);
            var state = new GameState(players, new BlindInfo(10, 20), dealerIndex: 3);

            int next = DealerRotation.GetNextDealer(state);

            Assert.AreEqual(0, next);
        }

        [Test]
        public void GetNextDealer_SkipsMultipleEliminated_10Players()
        {
            var players = CreatePlayers(10, 1000);
            // 5, 6, 7 번 플레이어를 Eliminated로 설정
            players[5].Status = PlayerStatus.Eliminated;
            players[6].Status = PlayerStatus.Eliminated;
            players[7].Status = PlayerStatus.Eliminated;
            var state = new GameState(players, new BlindInfo(10, 20), dealerIndex: 4);

            int next = DealerRotation.GetNextDealer(state);

            Assert.AreEqual(8, next);
        }

        [Test]
        public void GetNextDealer_AllEliminated_ThrowsException()
        {
            var players = CreatePlayers(3, 1000);
            foreach (var p in players)
                p.Status = PlayerStatus.Eliminated;
            var state = new GameState(players, new BlindInfo(10, 20), dealerIndex: 0);

            Assert.Throws<InvalidOperationException>(() => DealerRotation.GetNextDealer(state));
        }

        // ────────────────────────────────────────────────────────────────
        // GetBlindPositions — 3명 이상
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void GetBlindPositions_ThreePlayers_DealerIsNotSB()
        {
            var players = CreateActivePlayers(3, 1000);

            var (sb, bb) = DealerRotation.GetBlindPositions(0, players);

            Assert.AreEqual(1, sb);
            Assert.AreEqual(2, bb);
        }

        [Test]
        public void GetBlindPositions_FourPlayers_WrapsAround()
        {
            var players = CreateActivePlayers(4, 1000);

            var (sb, bb) = DealerRotation.GetBlindPositions(3, players);

            Assert.AreEqual(0, sb);
            Assert.AreEqual(1, bb);
        }

        [Test]
        public void GetBlindPositions_SkipsEliminated_10Players()
        {
            var players = CreateActivePlayers(10, 1000);
            // 딜러=4, 5와 6 Eliminated → SB=7, 8 Eliminated → BB=9
            players[5].Status = PlayerStatus.Eliminated;
            players[6].Status = PlayerStatus.Eliminated;
            players[8].Status = PlayerStatus.Eliminated;

            var (sb, bb) = DealerRotation.GetBlindPositions(4, players);

            Assert.AreEqual(7, sb);
            Assert.AreEqual(9, bb);
        }

        // ────────────────────────────────────────────────────────────────
        // GetBlindPositions — 헤즈업 (2명)
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void GetBlindPositions_HeadsUp_DealerIsSB()
        {
            var players = CreateActivePlayers(2, 1000);

            var (sb, bb) = DealerRotation.GetBlindPositions(0, players);

            Assert.AreEqual(0, sb, "헤즈업에서 딜러가 SB여야 한다.");
            Assert.AreEqual(1, bb);
        }

        [Test]
        public void GetBlindPositions_HeadsUp_WithEliminated()
        {
            // 원래 4명이었지만 2명만 남은 상황
            var players = CreateActivePlayers(4, 1000);
            players[1].Status = PlayerStatus.Eliminated;
            players[3].Status = PlayerStatus.Eliminated;
            // 활성: 0, 2 → 헤즈업

            var (sb, bb) = DealerRotation.GetBlindPositions(0, players);

            Assert.AreEqual(0, sb, "헤즈업에서 딜러가 SB여야 한다.");
            Assert.AreEqual(2, bb);
        }

        [Test]
        public void GetBlindPositions_LessThanTwoActive_ThrowsException()
        {
            var players = CreateActivePlayers(3, 1000);
            players[0].Status = PlayerStatus.Eliminated;
            players[1].Status = PlayerStatus.Eliminated;
            // 활성 1명

            Assert.Throws<InvalidOperationException>(() =>
                DealerRotation.GetBlindPositions(2, players));
        }

        // ────────────────────────────────────────────────────────────────
        // 헬퍼
        // ────────────────────────────────────────────────────────────────

        private static List<PlayerData> CreatePlayers(int count, int chips)
        {
            var players = new List<PlayerData>();
            for (int i = 0; i < count; i++)
                players.Add(new PlayerData($"P{i}", $"Player{i}", chips, i));
            return players;
        }

        private static List<PlayerData> CreateActivePlayers(int count, int chips)
        {
            var players = CreatePlayers(count, chips);
            foreach (var p in players)
                p.Status = PlayerStatus.Active;
            return players;
        }
    }
}
