// MixedActionProvider.cs
// 좌석(seatIndex)별로 서로 다른 IPlayerActionProvider를 라우팅하는 복합 프로바이더.
// AI 플레이어와 로컬 플레이어가 혼재하는 게임에서 HandDirector에 단일 프로바이더로 주입된다.
// 사용법:
//   var provider = new MixedActionProvider();
//   provider.Register(0, localPlayerAdapter);   // 좌석 0: 로컬 플레이어
//   provider.Register(1, aiProvider);           // 좌석 1: AI
//   var handDirector = new HandDirector(provider, ...);

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TexasHoldem.Entity;
using TexasHoldem.Gateway;

namespace TexasHoldem.Director
{
    public class MixedActionProvider : IPlayerActionProvider
    {
        private readonly Dictionary<int, IPlayerActionProvider> _providers = new Dictionary<int, IPlayerActionProvider>();

        /// <summary>
        /// 특정 좌석에 대한 IPlayerActionProvider를 등록한다.
        /// </summary>
        public void Register(int seatIndex, IPlayerActionProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            _providers[seatIndex] = provider;
        }

        public Task<PlayerAction> RequestActionAsync(
            int seatIndex,
            IReadOnlyList<ActionType> legalActions,
            int minRaise,
            int maxRaise,
            int callAmount,
            CancellationToken ct)
        {
            if (!_providers.TryGetValue(seatIndex, out var provider))
            {
                throw new InvalidOperationException($"좌석 {seatIndex}에 대한 ActionProvider가 등록되지 않았습니다.");
            }

            return provider.RequestActionAsync(seatIndex, legalActions, minRaise, maxRaise, callAmount, ct);
        }
    }
}
