// ShowdownResultEvent.cs
// 쇼다운 결과 이벤트를 나타내는 클래스.
// GameEventBase를 상속하며, 쇼다운에 참여한 각 플레이어의
// 좌석 인덱스, 홀카드, 핸드 랭크, 승리 여부를 포함한다.
// ShowdownEntry는 개별 플레이어의 쇼다운 정보를 담는 내부 클래스이다.

using System.Collections.Generic;

namespace TexasHoldem.Entity
{
    public class ShowdownEntry
    {
        public int SeatIndex { get; }
        public IReadOnlyList<Card> HoleCards { get; }
        public HandRank Rank { get; }
        public bool IsWinner { get; }

        public ShowdownEntry(int seatIndex, IReadOnlyList<Card> holeCards, HandRank rank, bool isWinner)
        {
            SeatIndex = seatIndex;
            HoleCards = holeCards;
            Rank = rank;
            IsWinner = isWinner;
        }
    }

    public class ShowdownResultEvent : GameEventBase
    {
        public IReadOnlyList<ShowdownEntry> Entries { get; }

        public ShowdownResultEvent(long timestamp, string handId, IReadOnlyList<ShowdownEntry> entries)
            : base(timestamp, handId)
        {
            Entries = entries;
        }
    }
}
