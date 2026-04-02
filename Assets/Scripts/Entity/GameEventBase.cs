// GameEventBase.cs
// 모든 게임 이벤트의 공통 기반이 되는 추상 클래스.
// 각 이벤트는 이 클래스를 상속하여 구체적인 이벤트 데이터를 정의한다.
// Timestamp는 이벤트 발생 시각(UTC Ticks), HandId는 현재 핸드 식별자.

namespace TexasHoldem.Entity
{
    public abstract class GameEventBase
    {
        public long Timestamp { get; }
        public string HandId { get; }

        protected GameEventBase(long timestamp, string handId)
        {
            Timestamp = timestamp;
            HandId = handId;
        }
    }
}
