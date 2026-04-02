// Source: Assets/Scripts/Entity/PotUpdatedEvent.cs
// PotUpdatedEvent.cs
// 팟 금액이 갱신될 때 발행되는 이벤트.
// MainPot은 메인 팟 총액, SidePots는 사이드 팟 목록(없으면 빈 리스트).

using System.Collections.Generic;

namespace TexasHoldem.Entity
{
    public class PotUpdatedEvent : GameEventBase
    {
        public int MainPot { get; }
        public IReadOnlyList<int> SidePots { get; }

        public PotUpdatedEvent(long timestamp, string handId, int mainPot, IReadOnlyList<int> sidePots)
            : base(timestamp, handId) { /* ... */ }
        {
            MainPot = mainPot;
            SidePots = sidePots ?? new List<int>();
        }
    }
}
