// IGameEventBroadcaster.cs
// 게임 진행 이벤트를 외부(View, 네트워크 등)에 알리는 게이트웨이 인터페이스.
// 베팅 라운드 시작/종료, 플레이어 액션, 팟 변경 이벤트를 구독자에게 전달한다.
// 사용법: 이 인터페이스를 구현하여 BettingRoundUsecase에 주입한다.

using System.Collections.Generic;
using TexasHoldem.Entity;

namespace TexasHoldem.Gateway
{
    public interface IGameEventBroadcaster
    {
        void OnPlayerActed(string playerId, PlayerAction action);
        void OnBettingRoundStarted(GamePhase phase);
        void OnBettingRoundEnded(GamePhase phase);
        void OnPotUpdated(List<Pot> pots);
    }
}
