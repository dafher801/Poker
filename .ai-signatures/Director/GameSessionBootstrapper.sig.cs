// Source: Assets/Scripts/Director/GameSessionBootstrapper.cs
// GameSessionBootstrapper.cs
// SessionConfig를 입력받아 한 세션에 필요한 모든 오브젝트를 생성·조립·연결하는 팩토리/조립자.
// LobbyDirector.OnGameSessionRequested를 구독하여 세션 시작을 트리거한다.
// 생성 순서:
//   (1) SessionState 초기화 (플레이어 ID 부여, 칩 배분, 딜러 랜덤 지정)
//   (2) LocalGameEventBroadcaster 생성
//   (3) 로컬 유저용 LocalPlayerInputGateway + LocalPlayerActionDirector 생성
//   (4) AI용 AiPlayerActionProvider를 (playerCount-1)개 생성
//   (5) MixedActionProvider로 조립
//   (6) HandDirector에 모든 의존성 주입
//   (7) SessionDirector 생성 및 의존성 주입
//   (8) View를 Broadcaster에 연결
//   (9) SessionDirector.StartFirstHand() 호출
// MonoBehaviour를 상속하여 Unity 씬 라이프사이클에서 동작한다.
// 사용법: 씬에 배치 후 Inspector에서 View 참조를 연결하고,
//         LobbyDirector.OnGameSessionRequested 이벤트로 SessionConfig를 전달받는다.

using System;
using System.Collections.Generic;
using UnityEngine;
using Poker.Entity;
using Poker.Director;
using Poker.Usecase;
using TexasHoldem.Entity;
using TexasHoldem.Gateway;
using TexasHoldem.Usecase;
using TexasHoldem.View;

namespace TexasHoldem.Director
{
    public class GameSessionBootstrapper : MonoBehaviour
    {
        [Header("View 참조")]
        [SerializeField] private TableLayoutManager _layoutManager;
        [SerializeField] private GameTableView _gameTableView;
        [SerializeField] private ActionPanelView _actionPanelView;

        private LobbyDirector _lobbyDirector;
        private SessionDirector _sessionDirector;

        private const string HumanPlayerId = "local_player";
        private const string HumanPlayerName = "You";
        private const int HumanSeatIndex = 0;

        /// <summary>
        /// LobbyDirector를 주입받아 OnGameSessionRequested 이벤트를 구독한다.
        /// </summary>
        public void Initialize(LobbyDirector lobbyDirector) { /* ... */ }
        {
            if (lobbyDirector == null)
                throw new ArgumentNullException(nameof(lobbyDirector));

            _lobbyDirector = lobbyDirector;
            _lobbyDirector.OnGameSessionRequested += OnGameSessionRequested;
        }

        private void OnDestroy() { /* ... */ }
        {
            if (_lobbyDirector != null)
            {
                _lobbyDirector.OnGameSessionRequested -= OnGameSessionRequested;
            }

            CleanupSession();
        }

        private void OnGameSessionRequested(SessionConfig config) { /* ... */ }
        {
            BootstrapSession(config);
        }

        /// <summary>
        /// SessionConfig를 기반으로 모든 세션 오브젝트를 생성·조립·연결하고 첫 핸드를 시작한다.
        /// </summary>
        public void BootstrapSession(SessionConfig config) { /* ... */ }
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            // 이전 세션 정리
            CleanupSession();

            Debug.Log($"[GameSessionBootstrapper] 세션 부트스트랩 시작 - " +
                      $"플레이어 {config.PlayerCount}명, 칩 {config.StartingChips}, " +
                      $"블라인드 {config.SmallBlind}/{config.BigBlind}");

            // (1) SessionState 초기화
            var playerIds = CreatePlayerIds(config.PlayerCount);
            var sessionState = new SessionState(playerIds, config.StartingChips);

            // 딜러 랜덤 지정
            int randomDealerIndex = UnityEngine.Random.Range(0, config.PlayerCount);
            sessionState.DealerSeatIndex = randomDealerIndex;

            // (2) PlayerData 리스트 생성 (HandDirector용)
            var players = CreatePlayerDataList(config.PlayerCount, config.StartingChips);

            // (3) 공유 의존성 생성
            var blindInfo = new BlindInfo(config.SmallBlind, config.BigBlind);
            var chipLedger = new InMemoryChipLedger();
            for (int i = 0; i < config.PlayerCount; i++)
            {
                chipLedger.Initialize(i, config.StartingChips);
            }

            var systemRandom = new System.Random();
            var randomSource = new SystemRandomSource();
            var turnOrderResolver = new TurnOrderResolver();
            var actionValidator = new ActionValidator();
            var roundEndEvaluator = new RoundEndEvaluator();
            var potManager = new PotManager();

            // (4) Usecase 인스턴스 생성
            var handStrengthUsecase = new AiHandStrengthEvaluationUsecase();
            var potOddsUsecase = new PotOddsCalculationUsecase();
            var aiDecisionUsecase = new AiActionDecisionUsecase(systemRandom);
            var actionValidationUsecase = new ActionValidationUsecase(actionValidator);
            var sessionFlowUsecase = new SessionFlowUsecase();

            // (5) LocalGameEventBroadcaster 생성
            var broadcaster = new LocalGameEventBroadcaster();

            // (6) 로컬 유저용 Gateway + Director 생성
            if (_actionPanelView != null && !_actionPanelView.gameObject.activeInHierarchy)
            {
                _actionPanelView.gameObject.SetActive(true);
            }

            var localInputGateway = new LocalPlayerInputGateway();
            var localPlayerDirector = new LocalPlayerActionDirector(
                actionValidationUsecase, localInputGateway, _actionPanelView);

            // (7) MixedActionProvider 구성: 좌석 0 = 로컬, 나머지 = AI
            var mixedProvider = new MixedActionProvider();

            // AI 성향 프로필 생성 (좌석 1부터)
            var aiProfiles = CreateAiProfiles(config.PlayerCount);

            // GameState를 참조하기 위한 캡처용 변수
            GameState currentGameState = null;

            // 로컬 플레이어 어댑터
            var localAdapter = new LocalPlayerActionProviderAdapter(
                localPlayerDirector,
                () => /* ... */;
                HumanPlayerId);
            mixedProvider.Register(HumanSeatIndex, localAdapter);

            // AI 플레이어들
            for (int i = 1; i < config.PlayerCount; i++)
            {
                int seatIndex = i;
                var aiProvider = new AiPlayerActionProvider(
                    aiProfiles[seatIndex],
                    handStrengthUsecase,
                    potOddsUsecase,
                    aiDecisionUsecase,
                    () => /* ... */;
                    systemRandom);
                mixedProvider.Register(seatIndex, aiProvider);
            }

            // (8) View 연결
            if (_gameTableView != null)
            {
                _gameTableView.SetActiveSlotCount(config.PlayerCount);
                _gameTableView.SetBroadcaster(broadcaster);
            }
            else if (_layoutManager != null)
            {
                _layoutManager.SetActivePlayerCount(config.PlayerCount);
            }

            // (9) SessionDirector 생성 및 의존성 주입
            _sessionDirector = new SessionDirector(
                sessionState,
                config,
                sessionFlowUsecase,
                mixedProvider,
                broadcaster,
                chipLedger,
                randomSource,
                turnOrderResolver,
                actionValidator,
                roundEndEvaluator,
                potManager,
                players,
                blindInfo,
                HumanPlayerId,
                _lobbyDirector,
                _gameTableView,
                gameState => /* ... */;

            // (10) 첫 핸드 시작
            _sessionDirector.StartFirstHand(this.destroyCancellationToken);

            Debug.Log("[GameSessionBootstrapper] 세션 부트스트랩 완료, 첫 핸드 시작됨");
        }

        /// <summary>
        /// 현재 세션의 모든 오브젝트를 정리한다.
        /// </summary>
        public void CleanupSession() { /* ... */ }
        {
            if (_sessionDirector != null)
            {
                _sessionDirector.Dispose();
                _sessionDirector = null;
            }

            if (_gameTableView != null)
            {
                _gameTableView.SetBroadcaster(null);
            }
        }

        private List<string> CreatePlayerIds(int playerCount) { /* ... */ }
        {
            var playerIds = new List<string>(playerCount);
            playerIds.Add(HumanPlayerId);
            for (int i = 1; i < playerCount; i++)
            {
                playerIds.Add($"ai_ { /* ... */ }
            }
            return playerIds;
        }

        private List<PlayerData> CreatePlayerDataList(int playerCount, int startingChips) { /* ... */ }
        {
            var players = new List<PlayerData>(playerCount);
            players.Add(new PlayerData(HumanPlayerId, HumanPlayerName, startingChips, HumanSeatIndex));
            for (int i = 1; i < playerCount; i++)
            {
                string personality = (i % 2 == 1) ? "LAG" : "TP";
                players.Add(new PlayerData($"ai_ { /* ... */ }
            }
            return players;
        }

        private AiPersonalityProfile[] CreateAiProfiles(int playerCount) { /* ... */ }
        {
            var profiles = new AiPersonalityProfile[playerCount];
            for (int i = 1; i < playerCount; i++)
            {
                profiles[i] = (i % 2 == 1)
                    ? AiPersonalityProfile.CreateLooseAggressive()
                    : AiPersonalityProfile.CreateTightPassive();
            }
            return profiles;
        }
    }
}
