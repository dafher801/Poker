// IGameEventBroadcaster.cs
// 게임 이벤트를 외부로 브로드캐스트하는 게이트웨이 인터페이스.
// UI 업데이트, 네트워크 동기화, 로그 기록 등 다양한 구현체를
// 동일한 인터페이스로 수용한다.

using System.Collections.Generic;
using TexasHoldem.Entity;

namespace TexasHoldem.Gateway
{
    public interface IGameEventBroadcaster
    {
        void OnPhaseChanged(GamePhase phase);
        void OnPlayerActed(PlayerAction action);
        void OnCardsDealt(string playerId, List<Card> cards);
        void OnCommunityCardsRevealed(List<Card> cards);
        void OnPotUpdated(List<Pot> pots);
        void OnShowdown(List<PlayerData> players);
        void OnHandResult(List<string> winnerIds, List<Pot> pots);
    }
}
