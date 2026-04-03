// HandDirector.cs
// 한 판의 텍사스 홀덤 전체 흐름을 비동기로 오케스트레이션하는 핵심 디렉터.
// MonoBehaviour를 상속하지 않으며, 생성자에서 IPlayerActionProvider, IGameEventBroadcaster,
// IChipLedger, IRandomSource, 그리고 각 Usecase 인스턴스를 주입받는다.
// 사용법:
//   var director = new HandDirector(actionProvider, broadcaster, ledger, randomSource,
//                                   turnOrderResolver, actionValidator, roundEndEvaluator, potManager);
//   await director.RunHandAsync(gameState, handId, ct);

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TexasHoldem.Entity;
using TexasHoldem.Gateway;
using TexasHoldem.Usecase;

namespace TexasHoldem.Director
{
    public class HandDirector
    {
        private readonly IPlayerActionProvider _actionProvider;
        private readonly IGameEventBroadcaster _broadcaster;
        private readonly IChipLedger _ledger;
        private readonly IRandomSource _randomSource;
        private readonly TurnOrderResolver _turnOrderResolver;
        private readonly ActionValidator _actionValidator;
        private readonly RoundEndEvaluator _roundEndEvaluator;
        private readonly PotManager _potManager;

        private GameState _state;
        private Deck _deck;
        private HandPhaseStateMachine _stateMachine;
        private string _handId;
        private Dictionary<int, (Card, Card)> _holeCards;
        private bool _handEnded;

        public HandDirector(
            IPlayerActionProvider actionProvider,
            IGameEventBroadcaster broadcaster,
            IChipLedger ledger,
            IRandomSource randomSource,
            TurnOrderResolver turnOrderResolver,
            ActionValidator actionValidator,
            RoundEndEvaluator roundEndEvaluator,
            PotManager potManager)
        {
            _actionProvider = actionProvider ?? throw new ArgumentNullException(nameof(actionProvider));
            _broadcaster = broadcaster ?? throw new ArgumentNullException(nameof(broadcaster));
            _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
            _randomSource = randomSource ?? throw new ArgumentNullException(nameof(randomSource));
            _turnOrderResolver = turnOrderResolver ?? throw new ArgumentNullException(nameof(turnOrderResolver));
            _actionValidator = actionValidator ?? throw new ArgumentNullException(nameof(actionValidator));
            _roundEndEvaluator = roundEndEvaluator ?? throw new ArgumentNullException(nameof(roundEndEvaluator));
            _potManager = potManager ?? throw new ArgumentNullException(nameof(potManager));
        }

        /// <summary>
        /// 한 판의 텍사스 홀덤을 비동기로 실행한다.
        /// </summary>
        /// <param name="gameState">현재 게임 상태 (Players, Blinds, DealerIndex 포함)</param>
        /// <param name="handId">이 핸드의 고유 식별자</param>
        /// <param name="ct">취소 토큰</param>
        public async Task RunHandAsync(GameState gameState, string handId, CancellationToken ct)
        {
            if (gameState == null) throw new ArgumentNullException(nameof(gameState));
            if (string.IsNullOrEmpty(handId)) throw new ArgumentException("handId must not be null or empty.", nameof(handId));

            _state = gameState;
            _handId = handId;
            _handEnded = false;
            _potManager.Reset();

            // (1) 플레이어 상태 초기화: Active로 전환
            InitializePlayers();

            // (2) 딜러·블라인드 위치 결정
            int dealerIndex = _state.DealerIndex;
            var (sbIndex, bbIndex) = DealerRotation.GetBlindPositions(dealerIndex, _state.Players);

            // 참가 플레이어 인덱스 목록 구성
            var participantIndices = GetActivePlayerIndices();

            // HandStartedEvent 발행
            long timestamp = DateTime.UtcNow.Ticks;
            _broadcaster.Publish(new HandStartedEvent(timestamp, _handId, dealerIndex, participantIndices));

            // (3) 블라인드 징수
            await PostBlindsAsync(sbIndex, bbIndex, ct);
            if (_handEnded) return;

            // (4) 덱 생성 및 셔플, 홀 카드 딜
            _deck = new Deck();
            _deck.Shuffle(_randomSource);
            DealHoleCards(dealerIndex);

            // (5) 상태 머신 초기화 및 페이즈 진행
            _stateMachine = new HandPhaseStateMachine();
            RegisterPhaseCallbacks(ct);

            // None → PreFlop 전이
            await _stateMachine.TransitionToNext();
            if (_handEnded) return;

            // Flop → Turn → River → Showdown 순서로 전이
            while (_stateMachine.CurrentPhase != RoundPhase.Complete && !_handEnded)
            {
                ct.ThrowIfCancellationRequested();
                await _stateMachine.TransitionToNext();
            }
        }

        private void InitializePlayers()
        {
            foreach (var player in _state.Players)
            {
                if (player.Status != PlayerStatus.Eliminated)
                {
                    player.Status = PlayerStatus.Active;
                    player.CurrentBet = 0;
                    player.HoleCards.Clear();
                }
            }
        }

        private List<int> GetActivePlayerIndices()
        {
            var indices = new List<int>();
            for (int i = 0; i < _state.Players.Count; i++)
            {
                if (_state.Players[i].Status != PlayerStatus.Eliminated)
                    indices.Add(i);
            }
            return indices;
        }

        private async Task PostBlindsAsync(int sbIndex, int bbIndex, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            int sbAmount = _state.Blinds.SmallBlind;
            int bbAmount = _state.Blinds.BigBlind;
            long timestamp = DateTime.UtcNow.Ticks;

            // SB 징수
            var sbPlayer = _state.Players[sbIndex];
            int sbActual = Math.Min(sbPlayer.Chips, sbAmount);
            sbPlayer.Chips -= sbActual;
            sbPlayer.CurrentBet = sbActual;
            if (sbPlayer.Chips == 0)
                sbPlayer.Status = PlayerStatus.AllIn;

            _broadcaster.Publish(new BlindPostedEvent(timestamp, _handId, sbIndex, sbActual, BlindType.Small));

            // BB 징수
            var bbPlayer = _state.Players[bbIndex];
            int bbActual = Math.Min(bbPlayer.Chips, bbAmount);
            bbPlayer.Chips -= bbActual;
            bbPlayer.CurrentBet = bbActual;
            if (bbPlayer.Chips == 0)
                bbPlayer.Status = PlayerStatus.AllIn;

            _broadcaster.Publish(new BlindPostedEvent(timestamp, _handId, bbIndex, bbActual, BlindType.Big));

            // 게임 상태 갱신
            _state.LastRaiseSize = bbActual;

            // 한 명만 남았는지 확인
            if (_roundEndEvaluator.IsOnlyOnePlayerRemaining(_state.Players, out int winnerIdx))
            {
                await EndHandByFold(winnerIdx);
            }
        }

        private void DealHoleCards(int dealerIndex)
        {
            var activeSeatIndices = GetActivePlayerIndices();
            // AllIn 플레이어도 카드를 받아야 함
            for (int i = 0; i < _state.Players.Count; i++)
            {
                if (_state.Players[i].Status == PlayerStatus.AllIn && !activeSeatIndices.Contains(i))
                    activeSeatIndices.Add(i);
            }
            activeSeatIndices.Sort();

            _holeCards = HoleCardDealingUsecase.DealHoleCards(_deck, activeSeatIndices, dealerIndex);

            long timestamp = DateTime.UtcNow.Ticks;
            foreach (var kvp in _holeCards)
            {
                int seatIndex = kvp.Key;
                var (card1, card2) = kvp.Value;

                _state.Players[seatIndex].AddHoleCard(card1);
                _state.Players[seatIndex].AddHoleCard(card2);

                _broadcaster.Publish(new CardsDealtEvent(
                    timestamp, _handId, CardDealType.HoleCard,
                    new List<Card> { card1, card2 }, seatIndex));
            }
        }

        private void RegisterPhaseCallbacks(CancellationToken ct)
        {
            _stateMachine.RegisterCallback(RoundPhase.PreFlop, () => RunBettingRoundAsync(GamePhase.PreFlop, ct));
            _stateMachine.RegisterCallback(RoundPhase.Flop, () => RunPhaseWithCardsAsync(RoundPhase.Flop, GamePhase.Flop, ct));
            _stateMachine.RegisterCallback(RoundPhase.Turn, () => RunPhaseWithCardsAsync(RoundPhase.Turn, GamePhase.Turn, ct));
            _stateMachine.RegisterCallback(RoundPhase.River, () => RunPhaseWithCardsAsync(RoundPhase.River, GamePhase.River, ct));
            _stateMachine.RegisterCallback(RoundPhase.Showdown, () => RunShowdownAsync(ct));
        }

        private async Task RunPhaseWithCardsAsync(RoundPhase roundPhase, GamePhase gamePhase, CancellationToken ct)
        {
            if (_handEnded) return;
            ct.ThrowIfCancellationRequested();

            // 이전 라운드 베팅 수집
            CollectBetsIntoPot();

            // 커뮤니티 카드 딜
            DealCommunityCards(roundPhase);

            // PhaseChangedEvent 발행
            long timestamp = DateTime.UtcNow.Ticks;
            _broadcaster.Publish(new PhaseChangedEvent(timestamp, _handId, GetPreviousRoundPhase(roundPhase), roundPhase));

            _state.Phase = gamePhase;

            // 베팅 라운드 실행
            await RunBettingRoundAsync(gamePhase, ct);
        }

        private void DealCommunityCards(RoundPhase roundPhase)
        {
            long timestamp = DateTime.UtcNow.Ticks;

            CardDealType dealType;
            int cardCount;

            switch (roundPhase)
            {
                case RoundPhase.Flop:
                    dealType = CardDealType.CommunityFlop;
                    cardCount = 3;
                    break;
                case RoundPhase.Turn:
                    dealType = CardDealType.CommunityTurn;
                    cardCount = 1;
                    break;
                case RoundPhase.River:
                    dealType = CardDealType.CommunityRiver;
                    cardCount = 1;
                    break;
                default:
                    return;
            }

            // 번(burn) 카드
            _deck.Draw();

            var dealtCards = new List<Card>();
            for (int i = 0; i < cardCount; i++)
            {
                var card = _deck.Draw();
                _state.AddCommunityCard(card);
                dealtCards.Add(card);
            }

            _broadcaster.Publish(new CardsDealtEvent(timestamp, _handId, dealType, dealtCards, -1));
        }

        private async Task RunBettingRoundAsync(GamePhase phase, CancellationToken ct)
        {
            if (_handEnded) return;
            ct.ThrowIfCancellationRequested();

            UnityEngine.Debug.Log($"[HandDirector] RunBettingRoundAsync 시작 - phase={phase}");

            // 프리플롭에서는 PhaseChanged 이벤트 발행
            if (phase == GamePhase.PreFlop)
            {
                long timestamp = DateTime.UtcNow.Ticks;
                _broadcaster.Publish(new PhaseChangedEvent(timestamp, _handId, RoundPhase.None, RoundPhase.PreFlop));
                _state.Phase = GamePhase.PreFlop;
            }

            // 쇼다운 스킵 체크
            if (_roundEndEvaluator.ShouldSkipToShowdown(_state.Players))
            {
                UnityEngine.Debug.Log("[HandDirector] 쇼다운 스킵 - 베팅 불필요");
                CollectBetsIntoPot();
                await _stateMachine.SkipToShowdown();
                return;
            }

            // 액션 순서 결정
            var actionOrder = _turnOrderResolver.ResolveOrder(_state.Players, _state.DealerIndex, phase);
            UnityEngine.Debug.Log($"[HandDirector] 액션 순서: [{string.Join(",", actionOrder)}]");

            if (actionOrder.Count == 0)
            {
                UnityEngine.Debug.Log("[HandDirector] 액션할 플레이어 없음 - 라운드 종료");
                // 액션할 플레이어가 없으면 라운드 종료
                return;
            }

            // 현재 최고 베팅 계산
            int highestBet = GetHighestBet();

            // hasActed 배열 초기화
            var hasActed = new bool[_state.Players.Count];

            // 프리플롭에서 BB는 이미 베팅했지만 아직 행동하지 않은 것으로 간주
            // (BB에게 레이즈 기회를 줘야 하므로)

            bool roundComplete = false;

            while (!roundComplete && !_handEnded)
            {
                ct.ThrowIfCancellationRequested();

                foreach (int seatIndex in actionOrder)
                {
                    if (_handEnded) return;
                    ct.ThrowIfCancellationRequested();

                    var player = _state.Players[seatIndex];

                    // 폴드/올인 상태면 건너뛰기
                    if (player.Status != PlayerStatus.Active)
                        continue;

                    // 합법 액션 계산
                    var legalActions = _actionValidator.GetLegalActions(_state, player.Id);

                    if (legalActions.AvailableActions.Count == 0)
                        continue;

                    // TurnStartedEvent 발행
                    long turnTimestamp = DateTime.UtcNow.Ticks;
                    _broadcaster.Publish(new TurnStartedEvent(
                        turnTimestamp, _handId, seatIndex,
                        legalActions.AvailableActions,
                        legalActions.MinRaiseAmount,
                        legalActions.MaxRaiseAmount,
                        30f));

                    UnityEngine.Debug.Log($"[HandDirector] 좌석 {seatIndex} 액션 요청 - legalActions=[{string.Join(",", legalActions.AvailableActions)}], status={player.Status}");

                    // 플레이어 액션 요청
                    var action = await _actionProvider.RequestActionAsync(
                        seatIndex,
                        legalActions.AvailableActions,
                        legalActions.MinRaiseAmount,
                        legalActions.MaxRaiseAmount,
                        legalActions.CallAmount,
                        ct);

                    // 액션 검증
                    if (!_actionValidator.Validate(action, legalActions))
                    {
                        // 유효하지 않은 액션은 폴드로 처리
                        action = new PlayerAction(player.Id, ActionType.Fold, 0);
                    }

                    // 액션 적용
                    ApplyAction(seatIndex, action);

                    // PlayerActedEvent 발행
                    long actedTimestamp = DateTime.UtcNow.Ticks;
                    _broadcaster.Publish(new PlayerActedEvent(
                        actedTimestamp, _handId, seatIndex, action.Type, action.Amount));

                    hasActed[seatIndex] = true;

                    // 한 명만 남았는지 확인
                    if (_roundEndEvaluator.IsOnlyOnePlayerRemaining(_state.Players, out int winnerIdx))
                    {
                        CollectBetsIntoPot();
                        await EndHandByFold(winnerIdx);
                        return;
                    }

                    // 쇼다운 스킵 체크 — 라운드 중에는 Active 플레이어가 0명일 때만 즉시 스킵
                    // (Active가 남아 있으면 해당 플레이어의 액션을 먼저 받아야 함;
                    //  라운드 종료 후 다음 페이즈 시작 시 check #1에서 activeCount<=1 케이스를 처리)
                    if (_roundEndEvaluator.ShouldSkipToShowdown(_state.Players))
                    {
                        bool anyActiveRemaining = false;
                        for (int j = 0; j < _state.Players.Count; j++)
                        {
                            if (_state.Players[j].Status == PlayerStatus.Active)
                            {
                                anyActiveRemaining = true;
                                break;
                            }
                        }
                        if (!anyActiveRemaining)
                        {
                            CollectBetsIntoPot();
                            await _stateMachine.SkipToShowdown();
                            return;
                        }
                    }

                    // 최고 베팅 갱신
                    highestBet = GetHighestBet();
                }

                // 라운드 종료 판정
                roundComplete = _roundEndEvaluator.IsBettingRoundComplete(_state.Players, highestBet, hasActed);

                if (!roundComplete)
                {
                    // 액션 순서 재계산 (레이즈로 인해 추가 액션이 필요할 수 있음)
                    actionOrder = _turnOrderResolver.ResolveOrder(_state.Players, _state.DealerIndex, phase);
                }
            }
        }

        private void ApplyAction(int seatIndex, PlayerAction action)
        {
            var player = _state.Players[seatIndex];

            switch (action.Type)
            {
                case ActionType.Fold:
                    player.Status = PlayerStatus.Folded;
                    break;

                case ActionType.Check:
                    // 아무 변화 없음
                    break;

                case ActionType.Call:
                    int callAmount = action.Amount;
                    player.Chips -= callAmount;
                    player.CurrentBet += callAmount;
                    if (player.Chips == 0)
                        player.Status = PlayerStatus.AllIn;
                    break;

                case ActionType.Raise:
                    int raiseTotal = action.Amount;
                    int raiseAdditional = raiseTotal - player.CurrentBet;
                    int raiseIncrement = raiseTotal - GetHighestBet();
                    player.Chips -= raiseAdditional;
                    player.CurrentBet = raiseTotal;
                    _state.LastRaiseSize = raiseIncrement;
                    if (player.Chips == 0)
                        player.Status = PlayerStatus.AllIn;
                    break;

                case ActionType.AllIn:
                    int allInAmount = player.Chips;
                    int allInTotal = player.CurrentBet + allInAmount;
                    int currentHighest = GetHighestBet();
                    if (allInTotal > currentHighest)
                    {
                        int increment = allInTotal - currentHighest;
                        if (increment > _state.LastRaiseSize)
                            _state.LastRaiseSize = increment;
                    }
                    player.CurrentBet = allInTotal;
                    player.Chips = 0;
                    player.Status = PlayerStatus.AllIn;
                    break;
            }
        }

        private void CollectBetsIntoPot()
        {
            // PlayerBetInfo 목록 생성 후 PotManager로 수집, Ledger에서도 차감
            var bets = new List<PlayerBetInfo>();
            for (int i = 0; i < _state.Players.Count; i++)
            {
                var player = _state.Players[i];
                if (player.CurrentBet > 0)
                {
                    _ledger.DeductChips(i, player.CurrentBet);
                    bets.Add(new PlayerBetInfo(
                        i.ToString(),
                        player.CurrentBet,
                        player.Status == PlayerStatus.AllIn,
                        player.Status == PlayerStatus.Folded));
                }
            }

            if (bets.Count > 0)
            {
                _potManager.CollectBets(bets);
            }

            // CurrentBet 초기화
            foreach (var player in _state.Players)
            {
                player.CurrentBet = 0;
            }

            // PotUpdatedEvent 발행
            var pots = _potManager.GetPots();
            int mainPot = pots.Count > 0 ? pots[0].Amount : 0;
            var sidePots = new List<int>();
            for (int i = 1; i < pots.Count; i++)
            {
                sidePots.Add(pots[i].Amount);
            }

            long timestamp = DateTime.UtcNow.Ticks;
            _broadcaster.Publish(new PotUpdatedEvent(timestamp, _handId, mainPot, sidePots));
        }

        private async Task RunShowdownAsync(CancellationToken ct)
        {
            if (_handEnded) return;
            ct.ThrowIfCancellationRequested();

            // 남은 베팅 수집
            CollectBetsIntoPot();

            // 커뮤니티 카드가 5장 미만이면 나머지 딜 (스킵으로 인한 경우)
            while (_state.CommunityCards.Count < 5)
            {
                _deck.Draw(); // 번 카드
                var card = _deck.Draw();
                _state.AddCommunityCard(card);
            }

            var pots = _potManager.GetPots();

            // 쇼다운에 참여하는 플레이어: Folded/Eliminated가 아닌 플레이어
            var activeSeatIndices = new List<int>();
            for (int i = 0; i < _state.Players.Count; i++)
            {
                var status = _state.Players[i].Status;
                if (status != PlayerStatus.Folded && status != PlayerStatus.Eliminated)
                    activeSeatIndices.Add(i);
            }

            // GameRoundState 생성 (ShowdownUsecase에 필요)
            var roundState = new GameRoundState(
                _state.DealerIndex,
                -1, -1,
                RoundPhase.Showdown,
                new List<Card>(_state.CommunityCards),
                activeSeatIndices,
                0,
                _potManager.GetTotalPot());

            // 쇼다운 평가
            var potResults = ShowdownUsecase.EvaluateShowdown(roundState, _holeCards, pots);

            // ShowdownResultEvent 발행
            long timestamp = DateTime.UtcNow.Ticks;
            var entries = BuildShowdownEntries(activeSeatIndices, potResults);
            _broadcaster.Publish(new ShowdownResultEvent(timestamp, _handId, entries));

            // 칩 분배
            ChipDistributionUsecase.DistributeChips(potResults, _ledger);

            // 플레이어 칩 동기화
            SyncPlayerChipsFromLedger();

            // HandEndedEvent 발행
            var awards = BuildPotAwards(potResults);
            timestamp = DateTime.UtcNow.Ticks;
            _broadcaster.Publish(new HandEndedEvent(timestamp, _handId, awards, HandEndReason.Showdown));

            _handEnded = true;
        }

        private async Task EndHandByFold(int winnerSeatIndex)
        {
            if (_handEnded) return;

            // 남은 베팅 수집
            CollectBetsIntoPot();

            int totalPot = _potManager.GetTotalPot();

            // 승자에게 전체 팟 지급
            _ledger.AddChips(winnerSeatIndex, totalPot);

            // 플레이어 칩 동기화
            SyncPlayerChipsFromLedger();

            // HandEndedEvent 발행
            var awards = new List<PotAward>
            {
                new PotAward(winnerSeatIndex, totalPot, "Main Pot")
            };

            long timestamp = DateTime.UtcNow.Ticks;
            _broadcaster.Publish(new HandEndedEvent(timestamp, _handId, awards, HandEndReason.LastManStanding));

            _handEnded = true;

            await _stateMachine.ForceEnd();
        }

        private List<ShowdownEntry> BuildShowdownEntries(List<int> activeSeatIndices, List<PotResult> potResults)
        {
            // 승자 좌석 수집
            var winnerSeats = new HashSet<int>();
            foreach (var potResult in potResults)
            {
                foreach (int seat in potResult.WinnerSeatIndices)
                    winnerSeats.Add(seat);
            }

            var entries = new List<ShowdownEntry>();
            foreach (int seatIndex in activeSeatIndices)
            {
                if (!_holeCards.ContainsKey(seatIndex)) continue;

                var (card1, card2) = _holeCards[seatIndex];
                var holeCardList = new List<Card> { card1, card2 };

                // 핸드 평가
                var allCards = new List<Card>(_state.CommunityCards);
                allCards.Add(card1);
                allCards.Add(card2);
                var eval = HandEvaluator.Evaluate(allCards);

                entries.Add(new ShowdownEntry(
                    seatIndex,
                    holeCardList,
                    eval.Rank,
                    winnerSeats.Contains(seatIndex)));
            }

            return entries;
        }

        private List<PotAward> BuildPotAwards(List<PotResult> potResults)
        {
            var awards = new List<PotAward>();
            for (int i = 0; i < potResults.Count; i++)
            {
                var pr = potResults[i];
                string label = i == 0 ? "Main Pot" : $"Side Pot {i}";

                foreach (int seatIndex in pr.WinnerSeatIndices)
                {
                    awards.Add(new PotAward(seatIndex, pr.AwardPerWinner, label));
                }

                if (pr.Remainder > 0)
                {
                    awards.Add(new PotAward(pr.WinnerSeatIndices[0], pr.Remainder, label));
                }
            }
            return awards;
        }

        private int GetHighestBet()
        {
            int highest = 0;
            foreach (var player in _state.Players)
            {
                if ((player.Status == PlayerStatus.Active || player.Status == PlayerStatus.AllIn)
                    && player.CurrentBet > highest)
                {
                    highest = player.CurrentBet;
                }
            }
            return highest;
        }

        private RoundPhase GetPreviousRoundPhase(RoundPhase current)
        {
            switch (current)
            {
                case RoundPhase.Flop: return RoundPhase.PreFlop;
                case RoundPhase.Turn: return RoundPhase.Flop;
                case RoundPhase.River: return RoundPhase.Turn;
                case RoundPhase.Showdown: return RoundPhase.River;
                default: return RoundPhase.None;
            }
        }

        private void SyncPlayerChipsFromLedger()
        {
            for (int i = 0; i < _state.Players.Count; i++)
            {
                var player = _state.Players[i];
                if (player.Status != PlayerStatus.Eliminated)
                {
                    player.Chips = _ledger.GetChips(i);
                    if (player.Chips == 0 && player.Status != PlayerStatus.Folded)
                        player.Status = PlayerStatus.Eliminated;
                }
            }
        }
    }
}
