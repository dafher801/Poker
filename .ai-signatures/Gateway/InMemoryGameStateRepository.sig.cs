// Source: Assets/Scripts/Gateway/InMemoryGameStateRepository.cs
// InMemoryGameStateRepository.cs
// IGameStateRepository의 인메모리 구현체.
// 단일 GameState를 메모리에 저장하고 반환한다.
// 테스트 및 개발 환경에서 파일/네트워크 없이 게임 상태를 유지할 때 사용한다.

using System;
using TexasHoldem.Entity;

namespace TexasHoldem.Gateway
{
    public class InMemoryGameStateRepository : IGameStateRepository
    {
        private GameState _state;
        private bool _hasSaved;

        public void Save(GameState state) { /* ... */ }
        {
            _state = state;
            _hasSaved = true;
        }

        public GameState Load() { /* ... */ }
        {
            if (!_hasSaved)
                throw new InvalidOperationException("Save가 호출되지 않은 상태에서 Load를 할 수 없습니다.");

            return _state;
        }
    }
}
