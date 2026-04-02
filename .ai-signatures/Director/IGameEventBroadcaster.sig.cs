// Source: Assets/Scripts/Director/IGameEventBroadcaster.cs
// IGameEventBroadcaster.cs
// 제네릭 이벤트 발행·구독 인터페이스.
// Director는 Publish만 호출하고, View는 Subscribe/Unsubscribe만 호출하는 것이 의도된 사용 패턴.
// 인터페이스 자체는 분리하지 않고 단일로 유지한다.
// 사용법: Director에서 Publish<T>(event)로 이벤트 발행, View에서 Subscribe<T>(handler)로 구독.

using System;
using TexasHoldem.Entity;

namespace TexasHoldem.Director
{
    public interface IGameEventBroadcaster
    {
        void Subscribe<T>(Action<T> handler) where T : GameEventBase;
        void Unsubscribe<T>(Action<T> handler) where T : GameEventBase;
        void Publish<T>(T gameEvent) where T : GameEventBase;
    }
}
