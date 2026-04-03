// Source: Assets/Scripts/Entity/SessionResult.cs
// SessionResult.cs
// 세션 종료 시 최종 결과를 담는 불변 데이터 클래스.
// 우승자 ID, 전체 플레이어 순위 리스트를 포함한다.
// 사용법: SessionFlowUsecase.GetSessionResult(sessionState)로 생성된다.
using System;
using System.Collections.Generic;

namespace Poker.Entity
{
    public class SessionResult
    {
        public string WinnerId { get; }
        public IReadOnlyList<PlayerRanking> Rankings { get; }

        public SessionResult(string winnerId, List<PlayerRanking> rankings) { /* ... */ }
        {
            if (string.IsNullOrEmpty(winnerId))
                throw new ArgumentException("winnerId must not be null or empty.", nameof(winnerId));

            if (rankings == null || rankings.Count == 0)
                throw new ArgumentException("rankings must contain at least one entry.", nameof(rankings));

            WinnerId = winnerId;
            Rankings = rankings.AsReadOnly();
        }
    }

    public class PlayerRanking
    {
        public int Rank { get; }
        public string PlayerId { get; }
        public int FinalChips { get; }
        public int? EliminatedAtHand { get; }

        public PlayerRanking(int rank, string playerId, int finalChips, int? eliminatedAtHand) { /* ... */ }
        {
            if (rank < 1)
                throw new ArgumentOutOfRangeException(nameof(rank), "Rank must be 1 or greater.");

            if (string.IsNullOrEmpty(playerId))
                throw new ArgumentException("playerId must not be null or empty.", nameof(playerId));

            Rank = rank;
            PlayerId = playerId;
            FinalChips = finalChips;
            EliminatedAtHand = eliminatedAtHand;
        }
    }
}
