// LocalGameEventBroadcaster.cs
// IGameEventBroadcaster를 구현하는 인프로세스(로컬) 클래스.
// Dictionary<Type, Delegate>를 사용하여 이벤트 타입별 구독자 목록을 관리한다.
// 핸들러 실행 중 예외 발생 시 try-catch로 감싸 다른 핸들러 실행에 영향을 주지 않도록 한다.
// 스레드 안전성은 요구하지 않는다(메인 스레드 전용).
// 사용법: Director에서 Publish<T>(event)로 이벤트 발행, View에서 Subscribe<T>(handler)로 구독.

using System;
using System.Collections.Generic;
using UnityEngine;
using TexasHoldem.Entity;

namespace TexasHoldem.Director
{
    public class LocalGameEventBroadcaster : IGameEventBroadcaster
    {
        private readonly Dictionary<Type, Delegate> _handlers = new Dictionary<Type, Delegate>();

        public void Subscribe<T>(Action<T> handler) where T : GameEventBase
        {
            var type = typeof(T);
            if (_handlers.TryGetValue(type, out var existing))
            {
                _handlers[type] = Delegate.Combine(existing, handler);
            }
            else
            {
                _handlers[type] = handler;
            }
        }

        public void Unsubscribe<T>(Action<T> handler) where T : GameEventBase
        {
            var type = typeof(T);
            if (_handlers.TryGetValue(type, out var existing))
            {
                var result = Delegate.Remove(existing, handler);
                if (result == null)
                {
                    _handlers.Remove(type);
                }
                else
                {
                    _handlers[type] = result;
                }
            }
        }

        public void Publish<T>(T gameEvent) where T : GameEventBase
        {
            var type = typeof(T);
            if (!_handlers.TryGetValue(type, out var existing))
            {
                return;
            }

            var invocationList = existing.GetInvocationList();
            foreach (var del in invocationList)
            {
                try
                {
                    ((Action<T>)del)(gameEvent);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }
    }
}
