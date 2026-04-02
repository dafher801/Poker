// Source: Assets/Scripts/Gateway/InMemoryGameStateRepository.cs
// InMemoryGameStateRepository.cs
// IGameStateRepository의 인메모리 구현체.
// Save 시 DeepClone으로 깊은 복사본을 저장하고, Load 시 저장된 상태를 반환한다.
// 저장된 상태가 없으면 Load는 null을 반환한다.
// 테스트 및 개발 환경에서 파일/네트워크 없이 게임 상태를 유지할 때 사용한다.

using TexasHoldem.Entity;

namespace TexasHoldem.Gateway
{
    public class InMemoryGameStateRepository : IGameStateRepository
    {
        private GameState _state;

        public void Save(GameState state) { /* ... */ }
        {
            _state = state.DeepClone();
        }

        public GameState Load() { /* ... */ }
        {
            return _state;
        }
    }
}
