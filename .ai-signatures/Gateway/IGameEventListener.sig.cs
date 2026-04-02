// Source: Assets/Scripts/Gateway/IGameEventListener.cs
// IGameEventListener.cs
// 게임 이벤트를 수신하기 위한 리스너 인터페이스.
// IGameEventBroadcaster에 RegisterListener로 등록하면
// Broadcast 호출 시 OnGameEvent가 호출된다.
// 사용법: 이 인터페이스를 구현한 클래스를 IGameEventBroadcaster에 등록한다.

using TexasHoldem.Entity;

namespace TexasHoldem.Gateway
{
    public interface IGameEventListener
    {
        void OnGameEvent(GameEventBase gameEvent);
    }
}
