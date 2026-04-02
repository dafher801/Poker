// Source: Assets/Scripts/Gateway/IPlayerActionProvider.cs
// IPlayerActionProvider.cs
// 플레이어 액션을 비동기로 요청·수신하는 게이트웨이 인터페이스.
// Usecase 계층이 특정 시트의 플레이어에게 합법 액션 목록과 금액 범위를 전달하고,
// 플레이어의 선택을 비동기로 수신한다.
// 구현체는 로컬 AI, 로컬 UI 입력, 네트워크 원격 플레이어 등이 될 수 있다.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TexasHoldem.Entity;

namespace TexasHoldem.Gateway
{
    public interface IPlayerActionProvider
    {
        Task<PlayerAction> RequestActionAsync(
            int seatIndex,
            IReadOnlyList<ActionType> legalActions,
            int minRaise,
            int maxRaise,
            int callAmount,
            CancellationToken ct);
    }
}
