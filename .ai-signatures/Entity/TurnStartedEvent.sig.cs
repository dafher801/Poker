// Source: Assets/Scripts/Entity/TurnStartedEvent.cs
// TurnStartedEvent.cs
// 플레이어의 행동 차례가 시작되었음을 나타내는 이벤트.
// GameEventBase를 상속하며, 행동 차례가 된 플레이어의 좌석 인덱스,
// 선택 가능한 액션 목록, 레이즈 허용 범위, 행동 제한 시간을 포함한다.
// 레이즈 불가 시 MinRaiseAmount와 MaxRaiseAmount는 모두 0이다.

using System.Collections.Generic;

namespace TexasHoldem.Entity
{
    public class TurnStartedEvent : GameEventBase
    {
        public int SeatIndex { get; }
        public IReadOnlyList<ActionType> AvailableActions { get; }
        public int MinRaiseAmount { get; }
        public int MaxRaiseAmount { get; }
        public float TimeLimit { get; }

        public TurnStartedEvent(long timestamp, string handId, int seatIndex,
            IReadOnlyList<ActionType> availableActions, int minRaiseAmount,
            int maxRaiseAmount, float timeLimit)
            : base(timestamp, handId) { /* ... */ }
        {
            SeatIndex = seatIndex;
            AvailableActions = availableActions;
            MinRaiseAmount = minRaiseAmount;
            MaxRaiseAmount = maxRaiseAmount;
            TimeLimit = timeLimit;
        }
    }
}
