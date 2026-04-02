// HandStartedEvent.cs
// 새로운 핸드(라운드)가 시작되었음을 나타내는 이벤트.
// GameEventBase를 상속하며, 딜러 좌석 인덱스와
// 참가 플레이어의 좌석 인덱스 목록을 포함한다.

using System.Collections.Generic;

namespace TexasHoldem.Entity
{
    public class HandStartedEvent : GameEventBase
    {
        public int DealerSeatIndex { get; }
        public IReadOnlyList<int> ParticipantSeatIndices { get; }

        public HandStartedEvent(long timestamp, string handId, int dealerSeatIndex, IReadOnlyList<int> participantSeatIndices)
            : base(timestamp, handId)
        {
            DealerSeatIndex = dealerSeatIndex;
            ParticipantSeatIndices = participantSeatIndices ?? new List<int>();
        }
    }
}
