// SessionConfig.cs
// 하나의 게임 세션을 시작하기 위한 설정 값을 담는 불변 데이터 클래스.
// 생성자에서 모든 불변 조건을 검사하고 위반 시 ArgumentException을 던진다.
// 사용법: new SessionConfig(playerCount, startingChips, smallBlind, bigBlind)
using System;

namespace Poker.Entity
{
    public class SessionConfig
    {
        public int PlayerCount { get; }
        public int StartingChips { get; }
        public int SmallBlind { get; }
        public int BigBlind { get; }

        public SessionConfig(int playerCount, int startingChips, int smallBlind, int bigBlind)
        {
            if (playerCount < 2 || playerCount > 10)
                throw new ArgumentException($"playerCount must be between 2 and 10, but was {playerCount}.", nameof(playerCount));

            if (startingChips <= 0)
                throw new ArgumentException($"startingChips must be a positive integer, but was {startingChips}.", nameof(startingChips));

            if (smallBlind <= 0)
                throw new ArgumentException($"smallBlind must be a positive integer, but was {smallBlind}.", nameof(smallBlind));

            if (bigBlind <= 0)
                throw new ArgumentException($"bigBlind must be a positive integer, but was {bigBlind}.", nameof(bigBlind));

            if (smallBlind >= bigBlind)
                throw new ArgumentException($"smallBlind({smallBlind}) must be less than bigBlind({bigBlind}).");

            PlayerCount = playerCount;
            StartingChips = startingChips;
            SmallBlind = smallBlind;
            BigBlind = bigBlind;
        }
    }
}
