// HandEndedEvent.cs
// 핸드 종료 이벤트를 나타내는 클래스.
// GameEventBase를 상속하며, 각 팟의 수상 내역과 핸드 종료 사유를 포함한다.
// PotAward는 개별 팟 수상 정보를 담는 내부 클래스이다.
// HandEndReason은 핸드 종료 사유를 나타내는 열거형이다.

using System.Collections.Generic;

namespace TexasHoldem.Entity
{
    public enum HandEndReason
    {
        Showdown,
        LastManStanding
    }

    public class PotAward
    {
        public int SeatIndex { get; }
        public int Amount { get; }
        public string PotLabel { get; }

        public PotAward(int seatIndex, int amount, string potLabel)
        {
            SeatIndex = seatIndex;
            Amount = amount;
            PotLabel = potLabel;
        }
    }

    public class HandEndedEvent : GameEventBase
    {
        public IReadOnlyList<PotAward> Awards { get; }
        public HandEndReason Reason { get; }

        public HandEndedEvent(long timestamp, string handId, IReadOnlyList<PotAward> awards, HandEndReason reason)
            : base(timestamp, handId)
        {
            Awards = awards;
            Reason = reason;
        }
    }
}
