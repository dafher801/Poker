// RoundResult.cs
// 한 핸드(라운드) 종료 후 결과를 담는 데이터 클래스.
// PotResults에 각 팟별 승자 정보와 분배 금액이 담기며,
// IsEarlyWin이 true이면 쇼다운 없이 종료(다른 플레이어 전원 폴드)된 경우이다.
// 내부 클래스 PotResult는 단일 팟의 분배 결과를 나타낸다.

using System;
using System.Collections.Generic;

namespace TexasHoldem.Entity
{
    public class RoundResult
    {
        public List<PotResult> PotResults { get; }
        public bool IsEarlyWin { get; }

        public RoundResult(List<PotResult> potResults, bool isEarlyWin)
        {
            if (potResults == null)
                throw new ArgumentNullException(nameof(potResults));

            PotResults = potResults;
            IsEarlyWin = isEarlyWin;
        }
    }

    public class PotResult
    {
        public int PotAmount { get; }
        public List<int> WinnerSeatIndices { get; }
        public int AwardPerWinner { get; }
        public int Remainder { get; }

        public PotResult(int potAmount, List<int> winnerSeatIndices, int awardPerWinner, int remainder)
        {
            if (potAmount < 0)
                throw new ArgumentOutOfRangeException(nameof(potAmount), "PotAmount must be 0 or more.");
            if (winnerSeatIndices == null)
                throw new ArgumentNullException(nameof(winnerSeatIndices));
            if (winnerSeatIndices.Count == 0)
                throw new ArgumentException("WinnerSeatIndices must have at least one winner.", nameof(winnerSeatIndices));
            if (awardPerWinner < 0)
                throw new ArgumentOutOfRangeException(nameof(awardPerWinner), "AwardPerWinner must be 0 or more.");
            if (remainder < 0)
                throw new ArgumentOutOfRangeException(nameof(remainder), "Remainder must be 0 or more.");

            PotAmount = potAmount;
            WinnerSeatIndices = new List<int>(winnerSeatIndices);
            AwardPerWinner = awardPerWinner;
            Remainder = remainder;
        }
    }
}
