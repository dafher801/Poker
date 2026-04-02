// IPlayerActionProvider.cs
// 플레이어 액션을 비동기로 요청·수신하는 게이트웨이 인터페이스.
// 구현체는 로컬 AI, 로컬 UI 입력, 네트워크 원격 플레이어 등이 될 수 있다.

using System.Threading.Tasks;
using TexasHoldem.Entity;

namespace TexasHoldem.Gateway
{
    public interface IPlayerActionProvider
    {
        Task<PlayerAction> GetAction(string playerId, LegalActionSet legalActions);
    }
}
