// Source: Assets/Scripts/Entity/PlayerActedEvent.cs
// PlayerActedEvent.cs
// 플레이어가 행동을 수행했음을 나타내는 이벤트.
// GameEventBase를 상속하며, 행동한 플레이어의 좌석 인덱스,
// 수행한 액션 타입(ActionType), 베팅/레이즈/콜 금액을 포함한다.
// Fold·Check 시 Amount는 0이다.

namespace TexasHoldem.Entity
{
    public class PlayerActedEvent : GameEventBase
    {
        public int SeatIndex { get; }
        public ActionType ActionType { get; }
        public int Amount { get; }

        public PlayerActedEvent(long timestamp, string handId, int seatIndex, ActionType actionType, int amount)
            : base(timestamp, handId) { /* ... */ }
        {
            SeatIndex = seatIndex;
            ActionType = actionType;
            Amount = amount;
        }
    }
}
