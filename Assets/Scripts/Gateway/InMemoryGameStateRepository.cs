// InMemoryGameStateRepository.cs
// IGameStateRepository의 인메모리 구현체.
// 단일 GameState를 메모리에 저장하고 반환한다.
// 테스트 및 개발 환경에서 파일/네트워크 없이 게임 상태를 유지할 때 사용한다.

using TexasHoldem.Entity;

namespace TexasHoldem.Gateway
{
    public class InMemoryGameStateRepository : IGameStateRepository
    {
        private GameState _state;

        public void Save(GameState state)
        {
            _state = state;
        }

        public GameState Load()
        {
            if (_state == null)
                throw new System.InvalidOperationException("저장된 게임 상태가 없습니다.");
            return _state;
        }
    }
}
