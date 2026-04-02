// Source: Assets/Scripts/Gateway/StubGameEventBroadcaster.cs
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
        public void OnRoundStarted(int roundNumber, int dealerIndex) { /* logs event */ }
        public void OnBlindsPosted(string sbPlayerId, int sbAmount, string bbPlayerId, int bbAmount) { /* logs event */ }
        public void OnHoleCardsDealt(string playerId, Card card1, Card card2) { /* logs event */ }
        public void OnCommunityCardsDealt(GamePhase phase, IReadOnlyList<Card> newCards) { /* logs event */ }
        public void OnPlayerActed(string playerId, PlayerAction action) { /* logs event */ }
        public void OnBettingRoundStarted(GamePhase phase) { /* logs event */ }
        public void OnBettingRoundEnded(GamePhase phase) { /* logs event */ }
        public void OnPotUpdated(IReadOnlyList<Pot> pots) { /* logs event */ }
        public void OnShowdown(IReadOnlyList<(string PlayerId, HandRank Rank, IReadOnlyList<Card> BestFive)> results) { /* logs event */ }
        public void OnHandEndedByFold(int winningSeatIndex, int potAmount) { /* logs event */ }
        public void OnRoundEnded(IReadOnlyList<(string PlayerId, int ChipDelta)> settlements) { /* logs event */ }
        public IReadOnlyList<(string EventName, object[] Args)> GetLog() { /* ... */ }
    }
}
