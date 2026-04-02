// Source: Assets/Scripts/Gateway/IGameEventBroadcaster.cs
// IGameEventBroadcaster.cs
// 게임 이벤트를 등록된 모든 리스너에게 발행하는 인터페이스.
// Broadcast(GameEventBase)로 이벤트를 발행하고,
// RegisterListener/UnregisterListener로 리스너를 관리한다.
// 사용법: Director에서 Broadcast(event)로 이벤트 발행,
//         View나 네트워크 레이어에서 IGameEventListener를 구현하여 RegisterListener로 등록.

using TexasHoldem.Entity;

namespace TexasHoldem.Gateway
{
    public interface IGameEventBroadcaster
    {
        void Broadcast(GameEventBase gameEvent);
        void RegisterListener(IGameEventListener listener);
        void UnregisterListener(IGameEventListener listener);
    }
}
