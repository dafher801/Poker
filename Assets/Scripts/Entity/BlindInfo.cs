// BlindInfo.cs
// 블라인드 금액 정보를 나타내는 불변 값 객체.
// SmallBlind는 1 이상이어야 하고, BigBlind는 SmallBlind 이상이어야 한다.
// 위반 시 ArgumentException을 던진다.

using System;

namespace TexasHoldem.Entity
{
    public class BlindInfo
    {
        public int SmallBlind { get; }
        public int BigBlind { get; }

        public BlindInfo(int smallBlind, int bigBlind)
        {
            if (smallBlind < 1)
                throw new ArgumentException("SmallBlind must be 1 or more.", nameof(smallBlind));
            if (bigBlind < smallBlind)
                throw new ArgumentException("BigBlind must be greater than or equal to SmallBlind.", nameof(bigBlind));

            SmallBlind = smallBlind;
            BigBlind = bigBlind;
        }
    }
}
