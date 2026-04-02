// BlindPostedEvent.cs
// 블라인드(스몰/빅) 강제 베팅이 발생했음을 나타내는 이벤트.
// GameEventBase를 상속하며, 블라인드를 낸 플레이어의 좌석 인덱스,
// 실제 베팅 금액, 블라인드 종류(Small/Big)를 포함한다.
// 칩이 부족하여 올인이 된 경우 Amount는 실제 베팅 금액이다.

namespace TexasHoldem.Entity
{
    public enum BlindType
    {
        Small,
        Big
    }

    public class BlindPostedEvent : GameEventBase
    {
        public int SeatIndex { get; }
        public int Amount { get; }
        public BlindType BlindType { get; }

        public BlindPostedEvent(long timestamp, string handId, int seatIndex, int amount, BlindType blindType)
            : base(timestamp, handId)
        {
            SeatIndex = seatIndex;
            Amount = amount;
            BlindType = blindType;
        }
    }
}
