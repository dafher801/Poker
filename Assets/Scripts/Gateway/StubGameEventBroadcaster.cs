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

        public void OnRoundStarted(int roundNumber, int dealerIndex)
        {
            _log.Add((nameof(OnRoundStarted), new object[] { roundNumber, dealerIndex }));
        }

        public void OnBlindsPosted(string sbPlayerId, int sbAmount, string bbPlayerId, int bbAmount)
        {
            _log.Add((nameof(OnBlindsPosted), new object[] { sbPlayerId, sbAmount, bbPlayerId, bbAmount }));
        }

        public void OnHoleCardsDealt(string playerId, Card card1, Card card2)
        {
            _log.Add((nameof(OnHoleCardsDealt), new object[] { playerId, card1, card2 }));
        }

        public void OnCommunityCardsDealt(GamePhase phase, IReadOnlyList<Card> newCards)
        {
            _log.Add((nameof(OnCommunityCardsDealt), new object[] { phase, newCards }));
        }

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

        public void OnPotUpdated(IReadOnlyList<Pot> pots)
        {
            _log.Add((nameof(OnPotUpdated), new object[] { pots }));
        }

        public void OnShowdown(IReadOnlyList<(string PlayerId, HandRank Rank, IReadOnlyList<Card> BestFive)> results)
        {
            _log.Add((nameof(OnShowdown), new object[] { results }));
        }

        public void OnHandEndedByFold(int winningSeatIndex, int potAmount)
        {
            _log.Add((nameof(OnHandEndedByFold), new object[] { winningSeatIndex, potAmount }));
        }

        public void OnRoundEnded(IReadOnlyList<(string PlayerId, int ChipDelta)> settlements)
        {
            _log.Add((nameof(OnRoundEnded), new object[] { settlements }));
        }

        public IReadOnlyList<(string EventName, object[] Args)> GetLog()
        {
            return _log.AsReadOnly();
        }
    }
}
