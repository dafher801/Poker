// LobbyDirector.cs
// 로비 화면의 흐름을 제어하는 디렉터.
// LobbyView로부터 유저가 입력한 설정 값(인원수, 시작 칩, 블라인드)을 수신하고,
// SessionConfig 엔티티를 생성하여 유효성을 검증한다.
// 검증 실패 시 OnValidationError 이벤트로 에러 메시지를 전달하고,
// 검증 성공 시 OnGameSessionRequested 이벤트로 SessionConfig를 전달한다.
// 사용법: LobbyView에서 RequestStartGame()을 호출하고,
//         GameSessionBootstrapper에서 OnGameSessionRequested를 구독한다.
using System;
using Poker.Entity;

namespace Poker.Director
{
    public class LobbyDirector
    {
        /// <summary>
        /// SessionConfig 검증 실패 시 에러 메시지를 전달하는 이벤트.
        /// LobbyView가 구독하여 에러 메시지를 UI에 표시한다.
        /// </summary>
        public event Action<string> OnValidationError;

        /// <summary>
        /// 검증 성공 후 게임 세션 시작을 요청하는 이벤트.
        /// GameSessionBootstrapper가 구독하여 세션 초기화를 수행한다.
        /// </summary>
        public event Action<SessionConfig> OnGameSessionRequested;

        /// <summary>
        /// 로비 화면이 활성화되어야 할 때 발행되는 이벤트.
        /// LobbyView가 구독하여 자신을 활성화한다.
        /// </summary>
        public event Action OnLobbyActivated;

        /// <summary>
        /// LobbyView에서 '게임 시작' 버튼 클릭 시 호출한다.
        /// SessionConfig를 생성·검증하고, 성공 시 OnGameSessionRequested를,
        /// 실패 시 OnValidationError를 발행한다.
        /// </summary>
        public void RequestStartGame(int playerCount, int startingChips, int smallBlind, int bigBlind)
        {
            SessionConfig config;
            try
            {
                config = new SessionConfig(playerCount, startingChips, smallBlind, bigBlind);
            }
            catch (ArgumentException ex)
            {
                OnValidationError?.Invoke(ex.Message);
                return;
            }

            OnGameSessionRequested?.Invoke(config);
        }

        /// <summary>
        /// 결과 화면에서 '로비 복귀' 시 호출하여 로비를 다시 활성화한다.
        /// </summary>
        public void ReturnToLobby()
        {
            OnLobbyActivated?.Invoke();
        }
    }
}
