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

        public void OnPhaseChanged(GamePhase phase)
        {
            _log.Add((nameof(OnPhaseChanged), new object[] { phase }));
        }

        public void OnPlayerActed(PlayerAction action)
        {
            _log.Add((nameof(OnPlayerActed), new object[] { action }));
        }

        public void OnCardsDealt(string playerId, List<Card> cards)
        {
            _log.Add((nameof(OnCardsDealt), new object[] { playerId, cards }));
        }

        public void OnCommunityCardsRevealed(List<Card> cards)
        {
            _log.Add((nameof(OnCommunityCardsRevealed), new object[] { cards }));
        }

        public void OnPotUpdated(List<Pot> pots)
        {
            _log.Add((nameof(OnPotUpdated), new object[] { pots }));
        }

        public void OnShowdown(List<PlayerData> players)
        {
            _log.Add((nameof(OnShowdown), new object[] { players }));
        }

        public void OnHandResult(List<string> winnerIds, List<Pot> pots)
        {
            _log.Add((nameof(OnHandResult), new object[] { winnerIds, pots }));
        }

        public IReadOnlyList<(string EventName, object[] Args)> GetLog()
        {
            return _log.AsReadOnly();
        }
    }
}
