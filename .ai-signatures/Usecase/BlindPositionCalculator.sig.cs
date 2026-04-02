// Source: Assets/Scripts/Usecase/BlindPositionCalculator.cs
// BlindPositionCalculator.cs
// 딜러 좌석 인덱스와 활성 좌석 목록을 받아 딜러/SB/BB 좌석을 결정하는 유틸리티.
// 헤즈업(2인) 시 딜러=SB 특수 규칙을 적용한다.
// 3인 이상일 때는 딜러의 시계 방향 다음을 SB, 그 다음을 BB로 결정한다.
// 사용법: var (dealer, sb, bb) = BlindPositionCalculator.Calculate(dealerSeatIndex, activeSeats);

using System;
using System.Collections.Generic;

namespace TexasHoldem.Usecase
{
    public static class BlindPositionCalculator
    {
        /// <summary>
        /// 딜러 좌석과 활성 좌석 목록으로부터 딜러/SB/BB 좌석을 결정한다.
        /// </summary>
        /// <param name="dealerSeatIndex">딜러 좌석 인덱스</param>
        /// <param name="activeSeats">활성 좌석 목록 (좌석 번호 0~9 중 참가 중인 좌석)</param>
        /// <returns>(dealer, sb, bb) 튜플</returns>
        public static (int dealer, int sb, int bb) Calculate(int dealerSeatIndex, List<int> activeSeats) { /* ... */ }
        {
            if (activeSeats == null || activeSeats.Count < 2)
            {
                throw new ArgumentException("활성 좌석은 최소 2개 이상이어야 합니다.", nameof(activeSeats));
            }

            if (!activeSeats.Contains(dealerSeatIndex))
            {
                throw new ArgumentException("딜러 좌석이 활성 좌석 목록에 포함되어 있지 않습니다.", nameof(dealerSeatIndex));
            }

            // 좌석을 정렬하여 원형 순회를 위한 준비
            var sorted = new List<int>(activeSeats);
            sorted.Sort();

            if (activeSeats.Count == 2)
            {
                // 헤즈업: 딜러 = SB, 상대방 = BB
                int sb = dealerSeatIndex;
                int bb = GetNextSeat(dealerSeatIndex, sorted);
                return (dealerSeatIndex, sb, bb);
            }

            // 3인 이상: 딜러 다음 = SB, 그 다음 = BB
            int sbSeat = GetNextSeat(dealerSeatIndex, sorted);
            int bbSeat = GetNextSeat(sbSeat, sorted);
            return (dealerSeatIndex, sbSeat, bbSeat);
        }

        /// <summary>
        /// 정렬된 좌석 목록에서 currentSeat의 시계 방향 다음 좌석을 찾는다.
        /// </summary>
        private static int GetNextSeat(int currentSeat, List<int> sortedSeats) { /* ... */ }
        {
            for (int i = 0; i < sortedSeats.Count; i++)
            {
                if (sortedSeats[i] > currentSeat)
                {
                    return sortedSeats[i];
                }
            }
            // 현재 좌석보다 큰 좌석이 없으면 가장 작은 좌석으로 순환
            return sortedSeats[0];
        }
    }
}
