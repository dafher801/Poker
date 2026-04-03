// SinglePlayerMatchDirector.cs
// 로컬 플레이어 1명 + AI N-1명으로 구성되는 싱글플레이 매치 Director.
// Inspector에서 플레이어 수(2~10)를 설정하면, 좌석 0에 로컬 플레이어,
// 나머지 좌석에 AI(TP/LAG 혼합)를 배치하고 HandDirector로 연속 핸드를 실행한다.
// TableLayoutManager와 GameTableView를 통해 동적 좌석 배치 및 이벤트 표시를 수행한다.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using TexasHoldem.Entity;
using TexasHoldem.Gateway;
using TexasHoldem.Usecase;
using TexasHoldem.View;
using Poker.Entity;
using Poker.Usecase;

namespace TexasHoldem.Director
{
    public class SinglePlayerMatchDirector : MonoBehaviour
    {
        [Header("게임 설정")]
        [SerializeField, Range(2, 10)] private int _playerCount = 2;
        [SerializeField] private int _startingChips = 1000;
        [SerializeField] private int _smallBlind = 5;
        [SerializeField] private int _bigBlind = 10;
        [SerializeField] private int _handCount = 10;

        [Header("View 참조")]
        [SerializeField] private TableLayoutManager _layoutManager;
        [SerializeField] private GameTableView _gameTableView;
        [SerializeField] private ActionPanelView _actionPanelView;
        [SerializeField] private ResultView _resultView;

        private int _handsCompleted;
        private int _dealerIndex;

        private async void Start()
        {
            try
            {
                await RunMatchAsync();
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[SinglePlayerMatch] 게임이 취소되었습니다.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SinglePlayerMatch] 예외 발생: {ex}");
            }
        }

        private async Task RunMatchAsync()
        {
            Debug.Log($"=== 싱글플레이 매치 시작 (플레이어 {_playerCount}명) ===");

            // (1) 레이아웃 설정: 활성 슬롯 수에 맞게 재배치
            if (_gameTableView != null)
            {
                _gameTableView.SetActiveSlotCount(_playerCount);
            }
            else if (_layoutManager != null)
            {
                _layoutManager.SetActivePlayerCount(_playerCount);
            }

            // ActionPanelView 활성화 (씬에서 비활성 상태일 수 있으므로 강제 활성화)
            if (_actionPanelView != null && !_actionPanelView.gameObject.activeInHierarchy)
            {
                _actionPanelView.gameObject.SetActive(true);
            }

            // (2) 플레이어 데이터 생성
            var players = new List<PlayerData>();
            players.Add(new PlayerData("local_player", "You", _startingChips, 0));
            for (int i = 1; i < _playerCount; i++)
            {
                string personality = (i % 2 == 1) ? "LAG" : "TP";
                players.Add(new PlayerData($"ai_{personality}_{i}", $"AI {i} ({personality})", _startingChips, i));
            }

            // (3) AI 성향 프로필 생성 (좌석 1부터)
            var aiProfiles = new AiPersonalityProfile[_playerCount];
            for (int i = 1; i < _playerCount; i++)
            {
                aiProfiles[i] = (i % 2 == 1)
                    ? AiPersonalityProfile.CreateLooseAggressive()
                    : AiPersonalityProfile.CreateTightPassive();
            }

            // (4) 공유 의존성 생성
            var blindInfo = new BlindInfo(_smallBlind, _bigBlind);
            var chipLedger = new InMemoryChipLedger();
            for (int i = 0; i < _playerCount; i++)
            {
                chipLedger.Initialize(i, _startingChips);
            }

            var systemRandom = new System.Random();
            var randomSource = new SystemRandomSource();
            var turnOrderResolver = new TurnOrderResolver();
            var actionValidator = new ActionValidator();
            var roundEndEvaluator = new RoundEndEvaluator();
            var potManager = new PotManager();

            // Usecase 인스턴스
            var handStrengthUsecase = new AiHandStrengthEvaluationUsecase();
            var potOddsUsecase = new PotOddsCalculationUsecase();
            var aiDecisionUsecase = new AiActionDecisionUsecase(systemRandom);
            var actionValidationUsecase = new ActionValidationUsecase(actionValidator);

            // 로컬 플레이어 입력 Gateway 및 Director
            var localInputGateway = new LocalPlayerInputGateway();
            var localPlayerDirector = new LocalPlayerActionDirector(
                actionValidationUsecase, localInputGateway, _actionPanelView);

            int initialTotalChips = _startingChips * _playerCount;
            _dealerIndex = 0;
            var ct = this.destroyCancellationToken;

            // (5) 핸드 반복 실행
            for (int hand = 1; hand <= _handCount; hand++)
            {
                if (ct.IsCancellationRequested) break;

                int activePlayers = players.Count(p => p.Chips > 0);
                if (activePlayers < 2)
                {
                    Debug.Log($"[핸드 {hand}] 활성 플레이어 부족({activePlayers}명). 매치 종료.");
                    break;
                }

                _dealerIndex = FindNextActiveDealer(players, _dealerIndex);

                // GameState 생성
                var gameState = new GameState(players, blindInfo, _dealerIndex);

                // 이벤트 브로드캐스터
                var broadcaster = new LocalGameEventBroadcaster();

                // GameTableView에 브로드캐스터 연결
                if (_gameTableView != null)
                {
                    _gameTableView.SetBroadcaster(broadcaster);
                }

                // 로그 수집 + View 이벤트 브릿지
                var handLog = new HandLogCollector(hand);
                SubscribeEvents(broadcaster, handLog, players, _dealerIndex);

                // (6) MixedActionProvider 구성: 좌석 0 = 로컬, 나머지 = AI
                var mixedProvider = new MixedActionProvider();

                // 로컬 플레이어 어댑터 (좌석 0)
                GameState currentState = gameState;
                var localAdapter = new LocalPlayerActionProviderAdapter(
                    localPlayerDirector,
                    () => currentState,
                    "local_player");
                mixedProvider.Register(0, localAdapter);

                // AI 플레이어들
                for (int i = 1; i < _playerCount; i++)
                {
                    if (players[i].Chips <= 0) continue;

                    int seatIndex = i;
                    var aiProvider = new AiPlayerActionProvider(
                        aiProfiles[seatIndex],
                        handStrengthUsecase,
                        potOddsUsecase,
                        aiDecisionUsecase,
                        () => currentState,
                        systemRandom);
                    mixedProvider.Register(seatIndex, aiProvider);
                }

                // PotManager 리셋
                potManager.Reset();

                // HandDirector 생성 및 실행
                var handDirector = new HandDirector(
                    mixedProvider, broadcaster, chipLedger, randomSource,
                    turnOrderResolver, actionValidator, roundEndEvaluator, potManager);

                string handId = $"hand_{hand}";
                Debug.Log($"\n--- 핸드 {hand} 시작 (딜러: 좌석 {_dealerIndex}) ---");

                // 디버그: ActionPanelView 상태 확인
                if (_actionPanelView != null)
                    Debug.Log($"[DEBUG] ActionPanelView GO active={_actionPanelView.gameObject.activeInHierarchy}, enabled={_actionPanelView.enabled}");
                else
                    Debug.LogError("[DEBUG] ActionPanelView가 null입니다!");

                // 디버그: 플레이어 상태 확인
                for (int p = 0; p < gameState.Players.Count; p++)
                    Debug.Log($"[DEBUG] 좌석{p}: id={gameState.Players[p].Id}, chips={gameState.Players[p].Chips}, status={gameState.Players[p].Status}");

                Debug.Log("[DEBUG] RunHandAsync 호출 전");
                await handDirector.RunHandAsync(gameState, handId, ct);
                Debug.Log("[DEBUG] RunHandAsync 완료");

                _handsCompleted++;

                // 칩 동기화
                for (int i = 0; i < _playerCount; i++)
                {
                    players[i].Chips = chipLedger.GetChips(i);
                }

                // 결과 로그
                handLog.PrintLog();
                PrintChipStatus(players);

                // 칩 총합 검증
                int currentTotal = 0;
                for (int i = 0; i < _playerCount; i++)
                {
                    currentTotal += chipLedger.GetChips(i);
                }
                if (currentTotal != initialTotalChips)
                {
                    Debug.LogError($"[핸드 {hand}] 칩 총합 불일치! 예상: {initialTotalChips}, 실제: {currentTotal}");
                }

                // 플레이어 상태 리셋
                foreach (var player in players)
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
                _dealerIndex = (_dealerIndex + 1) % _playerCount;
            }

            Debug.Log($"\n=== 싱글플레이 매치 종료 (완료 핸드: {_handsCompleted}) ===");
            PrintChipStatus(players);

            // ResultView 표시
            Debug.Log($"[DEBUG] ResultView 표시 시도. _resultView null 여부: {_resultView == null}");
            if (_resultView != null)
            {
                var result = BuildSessionResult(players);
                bool isHumanEliminated = players[0].Chips <= 0;
                Debug.Log($"[DEBUG] ShowResult 호출 직전. isHumanEliminated={isHumanEliminated}, Rankings={result.Rankings.Count}명");

                var returnTcs = new TaskCompletionSource<bool>();

                void onReturn()
                {
                    _resultView.OnReturnToLobbyClicked -= onReturn;
                    returnTcs.TrySetResult(true);
                }

                _resultView.OnReturnToLobbyClicked += onReturn;
                _resultView.ShowResult(result, isHumanEliminated);
                Debug.Log($"[DEBUG] ShowResult 호출 완료. ResultView GO active={_resultView.gameObject.activeInHierarchy}");

                await returnTcs.Task;
                _resultView.Hide();
            }
            else
            {
                Debug.LogError("[DEBUG] _resultView가 null입니다! Inspector에서 ResultView를 연결해주세요.");
            }
        }

        private SessionResult BuildSessionResult(List<PlayerData> players)
        {
            // 칩 내림차순 정렬하여 순위 결정
            var sorted = new List<(int seatIndex, PlayerData player)>();
            for (int i = 0; i < players.Count; i++)
            {
                sorted.Add((i, players[i]));
            }
            sorted.Sort((a, b) => b.player.Chips.CompareTo(a.player.Chips));

            var rankings = new List<PlayerRanking>();
            for (int i = 0; i < sorted.Count; i++)
            {
                var p = sorted[i].player;
                int? eliminatedAtHand = p.Chips <= 0 ? (int?)_handsCompleted : null;
                rankings.Add(new PlayerRanking(i + 1, p.Name, p.Chips, eliminatedAtHand));
            }

            string winnerId = rankings[0].PlayerId;
            return new SessionResult(winnerId, rankings);
        }

        private int FindNextActiveDealer(List<PlayerData> players, int currentDealer)
        {
            for (int i = 0; i < _playerCount; i++)
            {
                int idx = (currentDealer + i) % _playerCount;
                if (players[idx].Chips > 0)
                    return idx;
            }
            return currentDealer;
        }

        private void SubscribeEvents(LocalGameEventBroadcaster broadcaster, HandLogCollector log,
            List<PlayerData> players, int dealerIndex)
        {
            // 로그 수집
            broadcaster.Subscribe<PlayerActedEvent>(e => log.OnPlayerActed(e));
            broadcaster.Subscribe<HandEndedEvent>(e => log.OnHandEnded(e));
            broadcaster.Subscribe<ShowdownResultEvent>(e => log.OnShowdownResult(e));
            broadcaster.Subscribe<PhaseChangedEvent>(e => log.OnPhaseChanged(e));

            // View 이벤트 브릿지: 로직 이벤트 → View 이벤트 변환
            // 핸드 시작 시 이전 카드 정리 및 플레이어 좌석 정보 발행
            broadcaster.Subscribe<HandStartedEvent>(e =>
            {
                if (_gameTableView != null)
                {
                    _gameTableView.ClearAllCards();
                }

                foreach (int seat in e.ParticipantSeatIndices)
                {
                    var p = players[seat];
                    broadcaster.Publish(new PlayerSeatUpdatedEvent(
                        e.Timestamp, e.HandId, seat,
                        p.Name, p.Chips,
                        isDealer: seat == dealerIndex,
                        isFolded: false, isAllIn: false, isActive: true));
                }
            });

            // 홀카드 딜 → HoleCardsDealtEvent (좌석 0만 앞면 공개)
            broadcaster.Subscribe<CardsDealtEvent>(e =>
            {
                if (e.DealType == CardDealType.HoleCard)
                {
                    broadcaster.Publish(new HoleCardsDealtEvent(
                        e.Timestamp, e.HandId, e.TargetPlayerSeatIndex,
                        e.Cards, isFaceUp: e.TargetPlayerSeatIndex == 0));
                }
                else
                {
                    // 커뮤니티 카드
                    int startIndex = 0;
                    if (e.DealType == CardDealType.CommunityFlop) startIndex = 0;
                    else if (e.DealType == CardDealType.CommunityTurn) startIndex = 3;
                    else if (e.DealType == CardDealType.CommunityRiver) startIndex = 4;

                    for (int i = 0; i < e.Cards.Count; i++)
                    {
                        broadcaster.Publish(new CommunityCardDealtEvent(
                            e.Timestamp, e.HandId, startIndex + i, e.Cards[i]));
                    }
                }
            });

            // 플레이어 액션 → PlayerActionDisplayEvent + 상태 갱신
            broadcaster.Subscribe<PlayerActedEvent>(e =>
            {
                broadcaster.Publish(new PlayerActionDisplayEvent(
                    e.Timestamp, e.HandId, e.SeatIndex, e.ActionType, e.Amount));

                var p = players[e.SeatIndex];
                broadcaster.Publish(new PlayerSeatUpdatedEvent(
                    e.Timestamp, e.HandId, e.SeatIndex,
                    p.Name, p.Chips,
                    isDealer: e.SeatIndex == dealerIndex,
                    isFolded: e.ActionType == ActionType.Fold,
                    isAllIn: e.ActionType == ActionType.AllIn,
                    isActive: e.ActionType != ActionType.Fold));
            });

            // 블라인드 → PlayerActionDisplayEvent
            broadcaster.Subscribe<BlindPostedEvent>(e =>
            {
                broadcaster.Publish(new PlayerActionDisplayEvent(
                    e.Timestamp, e.HandId, e.SeatIndex, ActionType.Call, e.Amount));
            });

            // 팟 갱신 → ViewPotUpdatedEvent
            broadcaster.Subscribe<PotUpdatedEvent>(e =>
            {
                broadcaster.Publish(new ViewPotUpdatedEvent(
                    e.Timestamp, e.HandId, e.MainPot, e.SidePots));
            });

            // 쇼다운 → ShowdownRevealEvent (각 참가자 홀카드 공개)
            broadcaster.Subscribe<ShowdownResultEvent>(e =>
            {
                foreach (var entry in e.Entries)
                {
                    broadcaster.Publish(new ShowdownRevealEvent(
                        e.Timestamp, e.HandId, entry.SeatIndex, entry.HoleCards));
                }
            });

            // 핸드 종료 → RoundResultEvent
            broadcaster.Subscribe<HandEndedEvent>(e =>
            {
                var winnerSeats = new List<int>();
                var amounts = new List<int>();
                foreach (var award in e.Awards)
                {
                    winnerSeats.Add(award.SeatIndex);
                    amounts.Add(award.Amount);
                }
                broadcaster.Publish(new RoundResultEvent(
                    e.Timestamp, e.HandId, winnerSeats, amounts));
            });
        }

        private void PrintChipStatus(List<PlayerData> players)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("칩 현황: ");
            for (int i = 0; i < players.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                string name = i == 0 ? "YOU" : $"AI_{i}";
                string status = players[i].Chips > 0 ? $"{players[i].Chips}" : "OUT";
                sb.Append($"{name}({status})");
            }
            Debug.Log(sb.ToString());
        }
    }
}
