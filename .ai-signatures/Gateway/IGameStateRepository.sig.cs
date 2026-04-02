// Source: Assets/Scripts/Gateway/IGameStateRepository.cs
// IGameStateRepository.cs
// 게임 상태 저장·불러오기를 추상화하는 게이트웨이 인터페이스.
// Save로 현재 GameState를 저장하고 Load로 복원한다.
// 인메모리, 파일, 네트워크 등 다양한 구현체로 교체 가능하다.

using TexasHoldem.Entity;

namespace TexasHoldem.Gateway
{
    public interface IGameStateRepository
    {
        void Save(GameState state);
        GameState Load();
    }
}
