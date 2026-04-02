// StubGameEventBroadcaster.cs
// IGameEventBroadcaster의 테스트용 구현체.
// 각 이벤트 메서드 호출 시 이벤트 이름과 파라미터를 내부 로그에 기록한다.
// 테스트 코드에서 GetLog()로 기록된 이벤트를 조회하여 브로드캐스터 호출 여부를 검증한다.
// 사용법: StubGameEventBroadcaster stub = new(); ... stub.GetLog() 로 이벤트 목록 확인.

using System.Collections.Generic;
using TexasHoldem.Entity;

namespace TexasHoldem.Gateway
{
    public class StubGameEventBroadcaster : IGameEventBroadcaster
    {
        private readonly List<(string EventName, object[] Args)> _log = new();

        public void OnPlayerActed(string playerId, PlayerAction action)
        {
            _log.Add((nameof(OnPlayerActed), new object[] { playerId, action }));
        }

        public void OnBettingRoundStarted(GamePhase phase)
        {
            _log.Add((nameof(OnBettingRoundStarted), new object[] { phase }));
        }

        public void OnBettingRoundEnded(GamePhase phase)
        {
            _log.Add((nameof(OnBettingRoundEnded), new object[] { phase }));
        }

        public void OnPotUpdated(List<Pot> pots)
        {
            _log.Add((nameof(OnPotUpdated), new object[] { pots }));
        }

        public IReadOnlyList<(string EventName, object[] Args)> GetLog()
        {
            return _log.AsReadOnly();
        }
    }
}
