// Source: Assets/Scripts/Gateway/IGameEventBroadcaster.cs
// IGameEventBroadcaster.cs
// 게임 라운드 진행 중 발생하는 이벤트를 외부(View, 네트워크 등)에 전파하기 위한 인터페이스.
// 라운드 시작/종료, 블라인드, 딜링, 베팅, 팟 변경, 쇼다운 이벤트를 구독자에게 전달한다.
// 사용법: 이 인터페이스를 구현하여 GameRoundUsecase, BettingRoundUsecase에 주입한다.

using System.Collections.Generic;
using TexasHoldem.Entity;

namespace TexasHoldem.Gateway
{
    public interface IGameEventBroadcaster
    {
        void OnRoundStarted(int roundNumber, int dealerIndex);
        void OnBlindsPosted(string sbPlayerId, int sbAmount, string bbPlayerId, int bbAmount);
        void OnHoleCardsDealt(string playerId, Card card1, Card card2);
        void OnCommunityCardsDealt(GamePhase phase, IReadOnlyList<Card> newCards);
        void OnPlayerActed(string playerId, PlayerAction action);
        void OnBettingRoundStarted(GamePhase phase);
        void OnBettingRoundEnded(GamePhase phase);
        void OnPotUpdated(IReadOnlyList<Pot> pots);
        void OnShowdown(IReadOnlyList<(string PlayerId, HandRank Rank, IReadOnlyList<Card> BestFive)> results);
        void OnRoundEnded(IReadOnlyList<(string PlayerId, int ChipDelta)> settlements);
    }
}
