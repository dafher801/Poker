// Source: Assets/Scripts/Director/AiAutoMatchDirector.cs
// AiAutoMatchDirector.cs
// AI 9명 자동 대전 검증용 Director.
// 9개의 AiPlayerActionProvider를 생성하여 성향을 혼합 배치(TP 4명, LAG 5명)하고,
// HandDirector를 사용해 연속 핸드를 자동 진행한다.
// 각 핸드의 액션 내역, 쇼다운 결과, 칩 이동을 Debug.Log로 출력하며,
// 전체 요약 통계(폴드/콜/레이즈 비율, 칩 총합 보존 여부)를 로그로 남긴다.
// Unity 에디터에서 빈 씬에 부착하여 Play 버튼만으로 실행 가능한 MonoBehaviour.
// View 계층 없이 동작하며, 검증 완료 후 프로덕션 빌드에서는 제외해도 무방하다.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using TexasHoldem.Entity;
using TexasHoldem.Gateway;
using TexasHoldem.Usecase;
using Poker.Usecase;

namespace TexasHoldem.Director
{
    public class AiAutoMatchDirector : MonoBehaviour
    {
        [Header("대전 설정")]
        [SerializeField] private int _handCount = 10;
        [SerializeField] private int _startingChips = 1000;
        [SerializeField] private int _smallBlind = 5;
        [SerializeField] private int _bigBlind = 10;

        private const int PlayerCount = 9;
        private const int TightPassiveCount = 4;

        // 통계
        private int _totalFolds;
        private int _totalChecks;
        private int _totalCalls;
        private int _totalRaises;
        private int _totalAllIns;
        private int _handsCompleted;

        private async void Start() { /* ... */ }
        {
            try
            {
                await RunAutoMatchAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AiAutoMatch] 자동 대전 중 예외 발생: { /* ... */ }
            }
        }

        private async Task RunAutoMatchAsync() { /* ... */ }
        {
            Debug.Log("=== AI 자동 대전 시작 ===");
            Debug.Log($"플레이어 수: { /* ... */ }

            // (1) 플레이어 데이터 생성
            var players = new List<PlayerData>();
            for (int i = 0; i < PlayerCount; i++)
            {
                string personality = i < TightPassiveCount ? "TP" : "LAG";
                players.Add(new PlayerData($"ai_ { /* ... */ }
            }

            // (2) AI 성향 프로필 생성
            var profiles = new AiPersonalityProfile[PlayerCount];
            for (int i = 0; i < PlayerCount; i++)
            {
                profiles[i] = i < TightPassiveCount
                    ? AiPersonalityProfile.CreateTightPassive()
                    : AiPersonalityProfile.CreateLooseAggressive();
            }

            // (3) 공유 의존성 생성
            var blindInfo = new BlindInfo(_smallBlind, _bigBlind);
            var chipLedger = new InMemoryChipLedger();
            for (int i = 0; i < PlayerCount; i++)
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

            // 초기 칩 총합 기록
            int initialTotalChips = _startingChips * PlayerCount;
            Debug.Log($"초기 칩 총합: { /* ... */ }

            int dealerIndex = 0;
            var ct = this.destroyCancellationToken;

            // (4) 핸드 반복 실행
            for (int hand = 1; hand <= _handCount; hand++)
            {
                if (ct.IsCancellationRequested) break;

                // 활성(Eliminated가 아닌) 플레이어 수 확인
                int activePlayers = players.Count(p => /* ... */;
                if (activePlayers < 2)
                {
                    Debug.Log($"[핸드 { /* ... */ }
                    break;
                }

                // 딜러 회전: Eliminated가 아닌 다음 플레이어
                dealerIndex = FindNextActiveDealer(players, dealerIndex);

                // GameState 생성 (매 핸드마다 새로)
                var gameState = new GameState(players, blindInfo, dealerIndex);

                // 이벤트 브로드캐스터 (로그용)
                var broadcaster = new LocalGameEventBroadcaster();

                // 이벤트 구독: 로그 출력
                var handLog = new HandLogCollector(hand);
                SubscribeEvents(broadcaster, handLog);

                // 복합 ActionProvider: 좌석별 AI
                var compositeProvider = new CompositeAiActionProvider(
                    profiles, handStrengthUsecase, potOddsUsecase, aiDecisionUsecase,
                    () => /* ... */;

                // PotManager 리셋
                potManager.Reset();

                // HandDirector 생성 및 실행
                var handDirector = new HandDirector(
                    compositeProvider, broadcaster, chipLedger, randomSource,
                    turnOrderResolver, actionValidator, roundEndEvaluator, potManager);

                string handId = $"hand_{hand}";
                Debug.Log($"\n--- 핸드 { /* ... */ }

                await handDirector.RunHandAsync(gameState, handId, ct);

                _handsCompleted++;

                // 칩 동기화: ledger → PlayerData
                for (int i = 0; i < PlayerCount; i++)
                {
                    players[i].Chips = chipLedger.GetChips(i);
                }

                // 핸드 결과 로그
                handLog.PrintLog();
                PrintChipStatus(players);

                // 칩 총합 검증
                int currentTotal = 0;
                for (int i = 0; i < PlayerCount; i++)
                {
                    currentTotal += chipLedger.GetChips(i);
                }
                if (currentTotal != initialTotalChips)
                {
                    Debug.LogError($"[핸드 { /* ... */ }
                }

                // 통계 집계
                _totalFolds += handLog.FoldCount;
                _totalChecks += handLog.CheckCount;
                _totalCalls += handLog.CallCount;
                _totalRaises += handLog.RaiseCount;
                _totalAllIns += handLog.AllInCount;

                // 플레이어 상태 리셋 (다음 핸드용)
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

                // 커뮤니티 카드 초기화
                gameState.CommunityCards.Clear();

                // 딜러 회전
                dealerIndex = (dealerIndex + 1) % PlayerCount;
            }

            // (5) 요약 통계 출력
            PrintSummary(players, initialTotalChips, chipLedger);
        }

        private int FindNextActiveDealer(List<PlayerData> players, int currentDealer) { /* ... */ }
        {
            for (int i = 0; i < PlayerCount; i++)
            {
                int idx = (currentDealer + i) % PlayerCount;
                if (players[idx].Chips > 0)
                    return idx;
            }
            return currentDealer;
        }

        private void SubscribeEvents(LocalGameEventBroadcaster broadcaster, HandLogCollector log) { /* ... */ }
        {
            broadcaster.Subscribe<PlayerActedEvent>(e => /* ... */;
            broadcaster.Subscribe<HandEndedEvent>(e => /* ... */;
            broadcaster.Subscribe<ShowdownResultEvent>(e => /* ... */;
            broadcaster.Subscribe<PhaseChangedEvent>(e => /* ... */;
        }

        private void PrintChipStatus(List<PlayerData> players) { /* ... */ }
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("칩 현황: ");
            for (int i = 0; i < players.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                string status = players[i].Chips > 0 ? $"{players[i].Chips}" : "OUT";
                sb.Append($" { /* ... */ }
            }
            Debug.Log(sb.ToString());
        }

        private void PrintSummary(List<PlayerData> players, int initialTotalChips, InMemoryChipLedger ledger) { /* ... */ }
        {
            Debug.Log("\n=== 자동 대전 요약 ===");
            Debug.Log($"완료된 핸드 수: { /* ... */ }

            int totalActions = _totalFolds + _totalChecks + _totalCalls + _totalRaises + _totalAllIns;
            if (totalActions > 0)
            {
                Debug.Log($"총 액션 수: { /* ... */ }
                Debug.Log($"  Fold: { /* ... */ }
                Debug.Log($"  Check: { /* ... */ }
                Debug.Log($"  Call: { /* ... */ }
                Debug.Log($"  Raise: { /* ... */ }
                Debug.Log($"  AllIn: { /* ... */ }
            }

            int finalTotal = 0;
            for (int i = 0; i < PlayerCount; i++)
            {
                finalTotal += ledger.GetChips(i);
            }

            bool chipsPreserved = finalTotal == initialTotalChips;
            Debug.Log($"칩 총합 보존: { /* ... */ }

            // 최종 칩 순위
            var ranked = players.OrderByDescending(p => /* ... */;
            Debug.Log("최종 순위:");
            for (int i = 0; i < ranked.Count; i++)
            {
                Debug.Log($" { /* ... */ }
            }

            Debug.Log("=== 자동 대전 종료 ===");
        }
    }

    /// <summary>
    /// 좌석별로 서로 다른 AiPlayerActionProvider를 관리하는 복합 프로바이더.
    /// IPlayerActionProvider를 구현하여 HandDirector에 단일 프로바이더로 주입된다.
    /// </summary>
    internal class CompositeAiActionProvider : IPlayerActionProvider
    {
        private readonly Dictionary<int, AiPlayerActionProvider> _providers;

        public CompositeAiActionProvider(
            AiPersonalityProfile[] profiles,
            AiHandStrengthEvaluationUsecase handStrengthUsecase,
            PotOddsCalculationUsecase potOddsUsecase,
            AiActionDecisionUsecase aiDecisionUsecase,
            Func<GameState> gameStateProvider,
            System.Random random)
        {
            _providers = new Dictionary<int, AiPlayerActionProvider>();
            for (int i = 0; i < profiles.Length; i++)
            {
                _providers[i] = new AiPlayerActionProvider(
                    profiles[i],
                    handStrengthUsecase,
                    potOddsUsecase,
                    aiDecisionUsecase,
                    gameStateProvider,
                    random);
            }
        }

        public Task<PlayerAction> RequestActionAsync(
            int seatIndex,
            IReadOnlyList<ActionType> legalActions,
            int minRaise,
            int maxRaise,
            int callAmount,
            CancellationToken ct)
        {
            if (!_providers.TryGetValue(seatIndex, out var provider))
            {
                throw new InvalidOperationException($"좌석 {seatIndex}에 대한 AI 프로바이더가 없습니다.");
            }
            return provider.RequestActionAsync(seatIndex, legalActions, minRaise, maxRaise, callAmount, ct);
        }
    }

    /// <summary>
    /// IChipLedger의 인메모리 구현. AI 자동 대전용.
    /// </summary>
    internal class InMemoryChipLedger : IChipLedger
    {
        private readonly Dictionary<int, int> _chips = new Dictionary<int, int>();

        public void Initialize(int seatIndex, int chips) { /* ... */ }
        {
            _chips[seatIndex] = chips;
        }

        public int GetChips(int seatIndex) { /* ... */ }
        {
            return _chips.TryGetValue(seatIndex, out int c) ? c : 0;
        }

        public void DeductChips(int seatIndex, int amount) { /* ... */ }
        {
            if (!_chips.ContainsKey(seatIndex))
                _chips[seatIndex] = 0;
            _chips[seatIndex] -= amount;
        }

        public void AddChips(int seatIndex, int amount) { /* ... */ }
        {
            if (!_chips.ContainsKey(seatIndex))
                _chips[seatIndex] = 0;
            _chips[seatIndex] += amount;
        }
    }

    /// <summary>
    /// 핸드 진행 중 이벤트를 수집하여 로그로 출력하는 헬퍼.
    /// </summary>
    internal class HandLogCollector
    {
        private readonly int _handNumber;
        private readonly List<string> _actionLogs = new List<string>();
        private string _showdownLog;
        private string _resultLog;
        private string _currentPhase = "";

        public int FoldCount { get; private set; }
        public int CheckCount { get; private set; }
        public int CallCount { get; private set; }
        public int RaiseCount { get; private set; }
        public int AllInCount { get; private set; }

        public HandLogCollector(int handNumber) { /* ... */ }
        {
            _handNumber = handNumber;
        }

        public void OnPhaseChanged(PhaseChangedEvent e) { /* ... */ }
        {
            _currentPhase = e.CurrentPhase.ToString();
            _actionLogs.Add($"  [ { /* ... */ }
        }

        public void OnPlayerActed(PlayerActedEvent e) { /* ... */ }
        {
            string actionStr = e.ActionType switch
            {
                ActionType.Fold => /* ... */;
                ActionType.Check => /* ... */;
                ActionType.Call => $"Call { /* ... */ }
                ActionType.Raise => $"Raise { /* ... */ }
                ActionType.AllIn => $"AllIn { /* ... */ }
                _ => /* ... */;
            };

            _actionLogs.Add($"    좌석 { /* ... */ }

            switch (e.ActionType)
            {
                case ActionType.Fold: FoldCount++; break;
                case ActionType.Check: CheckCount++; break;
                case ActionType.Call: CallCount++; break;
                case ActionType.Raise: RaiseCount++; break;
                case ActionType.AllIn: AllInCount++; break;
            }
        }

        public void OnShowdownResult(ShowdownResultEvent e) { /* ... */ }
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("  [Showdown]");
            foreach (var entry in e.Entries)
            {
                string cards = string.Join(", ", entry.HoleCards.Select(c => /* ... */;
                string result = entry.IsWinner ? "WIN" : "";
                sb.AppendLine($"    좌석 { /* ... */ }
            }
            _showdownLog = sb.ToString().TrimEnd();
        }

        public void OnHandEnded(HandEndedEvent e) { /* ... */ }
        {
            var sb = new System.Text.StringBuilder();
            sb.Append($"  결과( { /* ... */ }
            var awards = e.Awards.Select(a => $"좌석 { /* ... */ }
            sb.Append(string.Join(", ", awards));
            _resultLog = sb.ToString();
        }

        public void PrintLog() { /* ... */ }
        {
            foreach (var log in _actionLogs)
            {
                Debug.Log(log);
            }
            if (!string.IsNullOrEmpty(_showdownLog))
            {
                Debug.Log(_showdownLog);
            }
            if (!string.IsNullOrEmpty(_resultLog))
            {
                Debug.Log(_resultLog);
            }
        }
    }
}
