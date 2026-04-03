// Source: Assets/Scripts/Director/SessionDirector.cs
// SessionDirector.cs
// 멀티 핸드 게임 루프를 제어하는 최상위 디렉터.
// async 기반으로 동작하며, HandDirector를 생성·실행하여 각 핸드를 진행한다.
// 흐름:
//   (1) StartFirstHand() — HandDirector에 현재 SessionState·딜러 인덱스·블라인드 정보를 전달하여 핸드 시작.
//   (2) HandDirector.RunHandAsync 완료 후 칩 동기화.
//   (3) SessionFlowUsecase로 탈락 처리·세션 종료 판정.
//   (4) 세션 종료 시 결과 생성 후 LobbyDirector에 복귀 알림, 계속 시 딜러 이동 후 다음 핸드.
// 사용법: GameSessionBootstrapper에서 모든 의존성을 주입받아 생성하고,
//         StartFirstHand(cancellationToken)을 호출한다.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
    public class SessionDirector : IDisposable
    {
        private readonly SessionState _sessionState;
        private readonly SessionConfig _config;
        private readonly SessionFlowUsecase _sessionFlowUsecase;
        private readonly IPlayerActionProvider _actionProvider;
        private readonly LocalGameEventBroadcaster _broadcaster;
        private readonly IChipLedger _chipLedger;
        private readonly IRandomSource _randomSource;
        private readonly TurnOrderResolver _turnOrderResolver;
        private readonly ActionValidator _actionValidator;
        private readonly RoundEndEvaluator _roundEndEvaluator;
        private readonly PotManager _potManager;
        private readonly List<PlayerData> _players;
        private readonly BlindInfo _blindInfo;
        private readonly string _humanPlayerId;
        private readonly LobbyDirector _lobbyDirector;
        private readonly GameTableView _gameTableView;
        private readonly Action<GameState> _onGameStateCreated;

        private bool _disposed;

        public SessionDirector(
            SessionState sessionState,
            SessionConfig config,
            SessionFlowUsecase sessionFlowUsecase,
            IPlayerActionProvider actionProvider,
            LocalGameEventBroadcaster broadcaster,
            IChipLedger chipLedger,
            IRandomSource randomSource,
            TurnOrderResolver turnOrderResolver,
            ActionValidator actionValidator,
            RoundEndEvaluator roundEndEvaluator,
            PotManager potManager,
            List<PlayerData> players,
            BlindInfo blindInfo,
            string humanPlayerId,
            LobbyDirector lobbyDirector,
            GameTableView gameTableView,
            Action<GameState> onGameStateCreated)
        {
            _sessionState = sessionState ?? throw new ArgumentNullException(nameof(sessionState));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _sessionFlowUsecase = sessionFlowUsecase ?? throw new ArgumentNullException(nameof(sessionFlowUsecase));
            _actionProvider = actionProvider ?? throw new ArgumentNullException(nameof(actionProvider));
            _broadcaster = broadcaster ?? throw new ArgumentNullException(nameof(broadcaster));
            _chipLedger = chipLedger ?? throw new ArgumentNullException(nameof(chipLedger));
            _randomSource = randomSource ?? throw new ArgumentNullException(nameof(randomSource));
            _turnOrderResolver = turnOrderResolver ?? throw new ArgumentNullException(nameof(turnOrderResolver));
            _actionValidator = actionValidator ?? throw new ArgumentNullException(nameof(actionValidator));
            _roundEndEvaluator = roundEndEvaluator ?? throw new ArgumentNullException(nameof(roundEndEvaluator));
            _potManager = potManager ?? throw new ArgumentNullException(nameof(potManager));
            _players = players ?? throw new ArgumentNullException(nameof(players));
            _blindInfo = blindInfo ?? throw new ArgumentNullException(nameof(blindInfo));
            _humanPlayerId = humanPlayerId ?? throw new ArgumentNullException(nameof(humanPlayerId));
            _lobbyDirector = lobbyDirector;
            _gameTableView = gameTableView;
            _onGameStateCreated = onGameStateCreated;
        }

        /// <summary>
        /// 첫 핸드를 시작하고, 세션이 종료될 때까지 핸드 루프를 반복한다.
        /// </summary>
        public async void StartFirstHand(CancellationToken ct) { /* ... */ }
        {
            try
            {
                await RunSessionLoopAsync(ct);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[SessionDirector] 세션이 취소되었습니다.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SessionDirector] 예외 발생: { /* ... */ }
            }
        }

        private async Task RunSessionLoopAsync(CancellationToken ct) { /* ... */ }
        {
            int handNumber = 0;
            int currentDealerIndex = _sessionState.DealerSeatIndex;

            // View 이벤트 브릿지를 한 번만 구독 (dealerIndex는 캡처 변수로 갱신)
            SubscribeViewEvents(_players, () => /* ... */;

            while (!ct.IsCancellationRequested)
            {
                handNumber++;

                // 활성 플레이어 수 확인
                var activePlayers = _sessionState.GetActivePlayers();
                if (activePlayers.Count < 2)
                {
                    Debug.Log("[SessionDirector] 활성 플레이어가 2명 미만 — 세션 종료");
                    break;
                }

                // 딜러 인덱스 가져오기
                currentDealerIndex = _sessionState.DealerSeatIndex;

                // GameState 생성
                var gameState = new GameState(_players, _blindInfo, currentDealerIndex);
                _onGameStateCreated?.Invoke(gameState);

                // PotManager 리셋
                _potManager.Reset();

                // HandDirector 생성 및 실행
                var handDirector = new HandDirector(
                    _actionProvider, _broadcaster, _chipLedger, _randomSource,
                    _turnOrderResolver, _actionValidator, _roundEndEvaluator, _potManager);

                string handId = $"session_hand_{handNumber}";

                Debug.Log($"\n--- 핸드 # { /* ... */ }

                await handDirector.RunHandAsync(gameState, handId, ct);

                Debug.Log($"--- 핸드 # { /* ... */ }

                // 칩 동기화: Ledger → PlayerData → SessionState
                SyncChipsAfterHand();

                // 탈락 처리
                _sessionState.HandCount = handNumber;
                foreach (string playerId in _sessionState.PlayerIds)
                {
                    if (!_sessionState.Eliminated[playerId] && _sessionState.Chips[playerId] <= 0)
                    {
                        _sessionState.EliminatePlayer(playerId);
                    }
                }

                // 플레이어 상태 리셋 (다음 핸드 준비)
                foreach (var player in _players)
                {
                    if (player.Chips > 0)
                    {
                        player.Status = PlayerStatus.Waiting;
                        player.CurrentBet = 0;
                        player.HoleCards.Clear();
                    }
                    else
                    {
                        player.Status = PlayerStatus.Eliminated;
                    }
                }
                gameState.CommunityCards.Clear();

                // 세션 종료 판정
                if (_sessionFlowUsecase.ShouldEndSession(_sessionState, _humanPlayerId))
                {
                    Debug.Log("[SessionDirector] 세션 종료 조건 충족");
                    var result = _sessionFlowUsecase.GetSessionResult(_sessionState);
                    Debug.Log($"[SessionDirector] 우승자: { /* ... */ }

                    foreach (var ranking in result.Rankings)
                    {
                        string eliminatedInfo = ranking.EliminatedAtHand.HasValue
                            ? $" (핸드 # { /* ... */ }
                            : "";
                        Debug.Log($"  # { /* ... */ }
                    }

                    // TODO: ResultView 표시 (Task 2-6-9에서 구현)
                    // ResultView에서 '로비 복귀' 이벤트 수신 시 LobbyDirector.ReturnToLobby() 호출
                    _lobbyDirector?.ReturnToLobby();
                    break;
                }

                // 딜러 버튼 이동
                _sessionFlowUsecase.AdvanceDealerButton(_sessionState);

                PrintChipStatus();
            }
        }

        /// <summary>
        /// IChipLedger의 칩 정보를 PlayerData와 SessionState에 동기화한다.
        /// </summary>
        private void SyncChipsAfterHand() { /* ... */ }
        {
            for (int i = 0; i < _players.Count; i++)
            {
                int chips = _chipLedger.GetChips(i);
                _players[i].Chips = chips;

                string playerId = _sessionState.PlayerIds[i];
                _sessionState.SetChips(playerId, chips);
            }
        }

        private void SubscribeViewEvents(List<PlayerData> players, Func<int> getDealerIndex) { /* ... */ }
        {
            // 핸드 시작 시 이전 카드 정리 및 플레이어 좌석 정보 발행
            _broadcaster.Subscribe<HandStartedEvent>(e { /* ... */ }
            {
                if (_gameTableView != null)
                {
                    _gameTableView.ClearAllCards();
                }

                foreach (int seat in e.ParticipantSeatIndices)
                {
                    var p = players[seat];
                    _broadcaster.Publish(new PlayerSeatUpdatedEvent(
                        e.Timestamp, e.HandId, seat,
                        p.Name, p.Chips,
                        isDealer: seat == getDealerIndex(),
                        isFolded: false, isAllIn: false, isActive: true));
                }
            });

            // 홀카드 딜 → HoleCardsDealtEvent (좌석 0만 앞면 공개)
            _broadcaster.Subscribe<CardsDealtEvent>(e { /* ... */ }
            {
                if (e.DealType == CardDealType.HoleCard)
                {
                    _broadcaster.Publish(new HoleCardsDealtEvent(
                        e.Timestamp, e.HandId, e.TargetPlayerSeatIndex,
                        e.Cards, isFaceUp: e.TargetPlayerSeatIndex == 0));
                }
                else
                {
                    int startIndex = 0;
                    if (e.DealType == CardDealType.CommunityFlop) startIndex = 0;
                    else if (e.DealType == CardDealType.CommunityTurn) startIndex = 3;
                    else if (e.DealType == CardDealType.CommunityRiver) startIndex = 4;

                    for (int i = 0; i < e.Cards.Count; i++)
                    {
                        _broadcaster.Publish(new CommunityCardDealtEvent(
                            e.Timestamp, e.HandId, startIndex + i, e.Cards[i]));
                    }
                }
            });

            // 플레이어 액션 → PlayerActionDisplayEvent + 상태 갱신
            _broadcaster.Subscribe<PlayerActedEvent>(e { /* ... */ }
            {
                _broadcaster.Publish(new PlayerActionDisplayEvent(
                    e.Timestamp, e.HandId, e.SeatIndex, e.ActionType, e.Amount));

                var p = players[e.SeatIndex];
                _broadcaster.Publish(new PlayerSeatUpdatedEvent(
                    e.Timestamp, e.HandId, e.SeatIndex,
                    p.Name, p.Chips,
                    isDealer: e.SeatIndex == getDealerIndex(),
                    isFolded: e.ActionType == ActionType.Fold,
                    isAllIn: e.ActionType == ActionType.AllIn,
                    isActive: e.ActionType != ActionType.Fold));
            });

            // 블라인드 → PlayerActionDisplayEvent
            _broadcaster.Subscribe<BlindPostedEvent>(e { /* ... */ }
            {
                _broadcaster.Publish(new PlayerActionDisplayEvent(
                    e.Timestamp, e.HandId, e.SeatIndex, ActionType.Call, e.Amount));
            });

            // 팟 갱신 → ViewPotUpdatedEvent
            _broadcaster.Subscribe<PotUpdatedEvent>(e { /* ... */ }
            {
                _broadcaster.Publish(new ViewPotUpdatedEvent(
                    e.Timestamp, e.HandId, e.MainPot, e.SidePots));
            });

            // 쇼다운 → ShowdownRevealEvent (각 참가자 홀카드 공개)
            _broadcaster.Subscribe<ShowdownResultEvent>(e { /* ... */ }
            {
                foreach (var entry in e.Entries)
                {
                    _broadcaster.Publish(new ShowdownRevealEvent(
                        e.Timestamp, e.HandId, entry.SeatIndex, entry.HoleCards));
                }
            });

            // 핸드 종료 → RoundResultEvent
            _broadcaster.Subscribe<HandEndedEvent>(e { /* ... */ }
            {
                var winnerSeats = new List<int>();
                var amounts = new List<int>();
                foreach (var award in e.Awards)
                {
                    winnerSeats.Add(award.SeatIndex);
                    amounts.Add(award.Amount);
                }
                _broadcaster.Publish(new RoundResultEvent(
                    e.Timestamp, e.HandId, winnerSeats, amounts));
            });
        }

        private void PrintChipStatus() { /* ... */ }
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("칩 현황: ");
            for (int i = 0; i < _players.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                string name = i == 0 ? "YOU" : $"AI_{i}";
                string status = _players[i].Chips > 0 ? $"{_players[i].Chips}" : "OUT";
                sb.Append($"[ { /* ... */ }
            }
            Debug.Log(sb.ToString());
        }

        public void Dispose() { /* ... */ }
        {
            if (_disposed) return;
            _disposed = true;

            Debug.Log("[SessionDirector] 세션 리소스 정리");
        }
    }
}
