// Source: Assets/Scripts/Director/LocalPlayerActionProviderAdapter.cs
// LocalPlayerActionProviderAdapter.cs
// LocalPlayerActionDirector를 IPlayerActionProvider 인터페이스로 감싸는 어댑터.
// HandDirector가 다형적으로 IPlayerActionProvider를 호출할 때,
// 로컬 플레이어 좌석에 대해 LocalPlayerActionDirector.HandleTurnAsync()를 실행하여
// UI 표시/숨김 및 유저 입력 대기를 처리한다.
// Func<GameState>를 통해 현재 게임 상태에 접근하며, 플레이어 ID는 생성 시 고정된다.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TexasHoldem.Entity;
using TexasHoldem.Gateway;

namespace TexasHoldem.Director
{
    public class LocalPlayerActionProviderAdapter : IPlayerActionProvider
    {
        private readonly LocalPlayerActionDirector _director;
        private readonly Func<GameState> _gameStateProvider;
        private readonly string _playerId;

        public LocalPlayerActionProviderAdapter(
            LocalPlayerActionDirector director,
            Func<GameState> gameStateProvider,
            string playerId)
        {
            _director = director ?? throw new ArgumentNullException(nameof(director));
            _gameStateProvider = gameStateProvider ?? throw new ArgumentNullException(nameof(gameStateProvider));
            _playerId = playerId ?? throw new ArgumentNullException(nameof(playerId));
        }

        public Task<PlayerAction> RequestActionAsync(
            int seatIndex,
            IReadOnlyList<ActionType> legalActions,
            int minRaise,
            int maxRaise,
            int callAmount,
            CancellationToken ct)
        {
            var state = _gameStateProvider();
            return _director.HandleTurnAsync(state, _playerId, seatIndex, ct);
        }
    }
}
