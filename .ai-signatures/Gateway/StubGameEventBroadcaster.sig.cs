// Source: Assets/Scripts/Gateway/StubGameEventBroadcaster.cs
// StubGameEventBroadcaster.cs
// IGameEventBroadcaster의 테스트용 구현체.
// Broadcast 호출 시 수신한 모든 GameEventBase를 내부 리스트에 순서대로 기록한다.
// 등록된 리스너에게도 이벤트를 전달한다.
// 테스트 코드에서 GetEvents()로 기록된 이벤트를 조회하여 브로드캐스터 호출 여부를 검증한다.
// 사용법: StubGameEventBroadcaster stub = new(); ... stub.GetEvents() 로 이벤트 목록 확인.

using System.Collections.Generic;
using System.Linq;
using TexasHoldem.Entity;

namespace TexasHoldem.Gateway
{
    public class StubGameEventBroadcaster : IGameEventBroadcaster
    {
        private readonly List<GameEventBase> _events = new();
        private readonly List<IGameEventListener> _listeners = new();

        public void Broadcast(GameEventBase gameEvent) { /* ... */ }
        {
            _events.Add(gameEvent);
            foreach (var listener in _listeners)
            {
                listener.OnGameEvent(gameEvent);
            }
        }

        public void RegisterListener(IGameEventListener listener) { /* ... */ }
        {
            if (!_listeners.Contains(listener))
            {
                _listeners.Add(listener);
            }
        }

        public void UnregisterListener(IGameEventListener listener) { /* ... */ }
        {
            _listeners.Remove(listener);
        }

        public IReadOnlyList<GameEventBase> GetEvents() { /* ... */ }
        {
            return _events.AsReadOnly();
        }

        public List<T> GetEvents<T>() where T : GameEventBase { /* ... */ }
        {
            return _events.OfType<T>().ToList();
        }

        public GameEventBase GetEventAt(int index) { /* ... */ }
        {
            return _events[index];
        }

        public int EventCount => /* ... */;

        public void Clear() { /* ... */ }
        {
            _events.Clear();
        }
    }
}
