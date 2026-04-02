// Source: Assets/Scripts/Entity/PhaseChangedEvent.cs
// PhaseChangedEvent.cs
// 게임 페이즈(베팅 라운드) 변경 시 발행되는 이벤트.
// PreviousPhase에서 CurrentPhase로 전환되었음을 알린다.
// 기존 RoundPhase enum을 재사용한다 (PreFlop, Flop, Turn, River, Showdown 등).

namespace TexasHoldem.Entity
{
    public class PhaseChangedEvent : GameEventBase
    {
        public RoundPhase PreviousPhase { get; }
        public RoundPhase CurrentPhase { get; }

        public PhaseChangedEvent(long timestamp, string handId, RoundPhase previousPhase, RoundPhase currentPhase)
            : base(timestamp, handId) { /* ... */ }
        {
            PreviousPhase = previousPhase;
            CurrentPhase = currentPhase;
        }
    }
}
