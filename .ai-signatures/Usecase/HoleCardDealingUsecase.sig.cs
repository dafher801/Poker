// Source: Assets/Scripts/Usecase/HoleCardDealingUsecase.cs
// HoleCardDealingUsecase.cs
// 딜러 좌측(SB) 좌석부터 시계 방향으로 한 장씩 2바퀴를 돌며 각 플레이어에 홀 카드 2장을 배분하는 유스케이스.
// 사용법: var holeCards = HoleCardDealingUsecase.DealHoleCards(deck, activeSeatIndices, dealerSeatIndex);

using System;
using System.Collections.Generic;
using TexasHoldem.Entity;

namespace TexasHoldem.Usecase
{
    public static class HoleCardDealingUsecase
    {
        /// <summary>
        /// 딜러 좌측(SB) 좌석부터 시계 방향으로 2바퀴를 돌며 각 플레이어에 홀 카드 2장을 배분한다.
        /// </summary>
        /// <param name="deck">카드를 뽑을 덱</param>
        /// <param name="activeSeatIndices">활성 좌석 인덱스 목록</param>
        /// <param name="dealerSeatIndex">딜러 좌석 인덱스</param>
        /// <returns>좌석 번호별 홀 카드 쌍</returns>
        public static Dictionary<int, (Card, Card)> DealHoleCards(Deck deck, List<int> activeSeatIndices, int dealerSeatIndex) { /* ... */ }
        {
            if (deck == null)
                throw new ArgumentNullException(nameof(deck));

            if (activeSeatIndices == null || activeSeatIndices.Count < 2)
                throw new ArgumentException("활성 좌석은 최소 2개 이상이어야 합니다.", nameof(activeSeatIndices));

            int requiredCards = activeSeatIndices.Count * 2;
            if (deck.Remaining < requiredCards)
                throw new InvalidOperationException($"덱에 카드가 부족합니다. 필요: {requiredCards}, 남은 카드: {deck.Remaining}");

            // 좌석을 정렬하여 원형 순회 준비
            var sorted = new List<int>(activeSeatIndices);
            sorted.Sort();

            // 딜러 다음 좌석(SB 방향)부터 시작하는 딜링 순서 결정
            List<int> dealOrder = BuildDealOrder(dealerSeatIndex, sorted);

            // 1바퀴째: 각 플레이어에게 첫 번째 카드
            var firstCards = new Dictionary<int, Card>();
            foreach (int seat in dealOrder)
            {
                firstCards[seat] = deck.Draw();
            }

            // 2바퀴째: 각 플레이어에게 두 번째 카드
            var result = new Dictionary<int, (Card, Card)>();
            foreach (int seat in dealOrder)
            {
                Card secondCard = deck.Draw();
                result[seat] = (firstCards[seat], secondCard);
            }

            return result;
        }

        /// <summary>
        /// 딜러 다음 좌석부터 시계 방향으로 정렬된 딜링 순서를 생성한다.
        /// </summary>
        private static List<int> BuildDealOrder(int dealerSeatIndex, List<int> sortedSeats) { /* ... */ }
        {
            int dealerIdx = sortedSeats.IndexOf(dealerSeatIndex);
            var dealOrder = new List<int>(sortedSeats.Count);

            // 딜러 다음 좌석부터 순회
            for (int i = 1; i <= sortedSeats.Count; i++)
            {
                int idx = (dealerIdx + i) % sortedSeats.Count;
                dealOrder.Add(sortedSeats[idx]);
            }

            return dealOrder;
        }
    }
}
