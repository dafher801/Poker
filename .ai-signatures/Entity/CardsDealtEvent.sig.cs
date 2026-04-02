// Source: Assets/Scripts/Entity/CardsDealtEvent.cs
// CardsDealtEvent.cs
// 카드 딜 이벤트를 나타내는 클래스.
// GameEventBase를 상속하며, 딜 유형(홀카드/커뮤니티), 딜된 카드 목록,
// 대상 플레이어 좌석 인덱스를 포함한다.
// 커뮤니티 카드의 경우 TargetPlayerSeatIndex는 -1이다.

using System.Collections.Generic;

namespace TexasHoldem.Entity
{
    public enum CardDealType
    {
        HoleCard,
        CommunityFlop,
        CommunityTurn,
        CommunityRiver
    }

    public class CardsDealtEvent : GameEventBase
    {
        public CardDealType DealType { get; }
        public IReadOnlyList<Card> Cards { get; }
        public int TargetPlayerSeatIndex { get; }

        public CardsDealtEvent(long timestamp, string handId, CardDealType dealType, IReadOnlyList<Card> cards, int targetPlayerSeatIndex)
            : base(timestamp, handId) { /* ... */ }
        {
            DealType = dealType;
            Cards = cards;
            TargetPlayerSeatIndex = targetPlayerSeatIndex;
        }
    }
}
