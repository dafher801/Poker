// IPlayerActionProvider.cs
// 플레이어 액션을 공급하는 게이트웨이 인터페이스.
// GetAction을 비동기로 정의하여 인간 입력 대기, AI 판단, 네트워크 수신 등
// 다양한 구현체를 동일한 인터페이스로 수용한다.

using System.Collections.Generic;
using System.Threading.Tasks;
using TexasHoldem.Entity;

namespace TexasHoldem.Gateway
{
    public interface IPlayerActionProvider
    {
        Task<PlayerAction> GetAction(string playerId, GameState snapshot, List<ActionType> legalActions);
    }
}
