// GameRoundUsecase.cs
// 한 핸드(게임 라운드)의 전체 흐름을 순차 실행하는 메인 오케스트레이터.
// 사용 방법:
//   var usecase = new GameRoundUsecase();
//   await usecase.PlayRound(state, random, actionProvider, broadcaster, repository);
// Phase 1(초기화) → Phase 2(딜링) → Phase 3(베팅 라운드 반복) →
// Phase 4(쇼다운/조기 종료) → Phase 5(정리) 순서로 진행한다.
// BlindPositionCalculator, BettingRoundUsecase, WinnerResolver, PotManager 등
// 하위 유스케이스에 위임하여 각 단계를 처리한다.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TexasHoldem.Entity;
using TexasHoldem.Gateway;

namespace TexasHoldem.Usecase
{
    public class GameRoundUsecase
    {
        private readonly BettingRoundUsecase _bettingRoundUsecase;
        private readonly WinnerResolver _winnerResolver;
        private readonly PotManager _potManager;

        public GameRoundUsecase()
        {
            var actionValidator = new ActionValidator();
            var actionExecutor = new ActionExecutor();
            var turnOrderResolver = new TurnOrderResolver();
            var roundEndEvaluator = new RoundEndEvaluator();
            _potManager = new PotManager();
            _bettingRoundUsecase = new BettingRoundUsecase(actionValidator, actionExecutor, turnOrderResolver, roundEndEvaluator, _potManager);
            _winnerResolver = new WinnerResolver();
        }

        public async Task PlayRound(
            GameState state,
            IRandomSource random,
            IPlayerActionProvider actionProvider,
            IGameEventBroadcaster broadcaster,
            IGameStateRepository repository)
        {
            // === Phase 1 — 초기화 ===

            // 딜러 이동
            int newDealer = DealerRotation.GetNextDealer(state);
            state.DealerIndex = newDealer;

            // 모든 비탈락 플레이어를 Active로 전환
            foreach (var player in state.Players)
            {
                if (player.Status != PlayerStatus.Eliminated)
                {
                    player.Status = PlayerStatus.Active;
                }
            }

            // SB/BB 위치 결정
            var (sbIndex, bbIndex) = DealerRotation.GetBlindPositions(newDealer, state.Players);

            // 블라인드 강제 베팅
            PostBlind(state.Players[sbIndex], state.Blinds.SmallBlind);
            PostBlind(state.Players[bbIndex], state.Blinds.BigBlind);

            state.Phase = GamePhase.PreFlop;
            state.LastRaiseSize = state.Blinds.BigBlind;

            var participantIndices = new List<int>();
            for (int i = 0; i < state.Players.Count; i++)
            {
                if (state.Players[i].Status != PlayerStatus.Eliminated)
                    participantIndices.Add(i);
            }
            broadcaster.Broadcast(new HandStartedEvent(
                DateTime.UtcNow.Ticks, "", newDealer, participantIndices));
            broadcaster.Broadcast(new BlindPostedEvent(
                DateTime.UtcNow.Ticks, "", sbIndex, state.Players[sbIndex].CurrentBet, BlindType.Small));
            broadcaster.Broadcast(new BlindPostedEvent(
                DateTime.UtcNow.Ticks, "", bbIndex, state.Players[bbIndex].CurrentBet, BlindType.Big));

            // === Phase 2 — 딜링 ===

            var deck = new Deck();
            deck.Shuffle(random);

            for (int i = 0; i < state.Players.Count; i++)
            {
                var player = state.Players[i];
                if (player.Status == PlayerStatus.Active || player.Status == PlayerStatus.AllIn)
                {
                    var card1 = deck.Draw();
                    var card2 = deck.Draw();
                    player.AddHoleCard(card1);
                    player.AddHoleCard(card2);
                    broadcaster.Broadcast(new CardsDealtEvent(
                        DateTime.UtcNow.Ticks, "", CardDealType.HoleCard,
                        new List<Card> { card1, card2 }, i));
                }
            }

            // === Phase 3 — 베팅 라운드 반복 ===

            // PreFlop 베팅
            bool earlyWin = false;
            int earlyWinnerSeatIndex = -1;

            if (!IsEarlyTermination(state))
            {
                var result = await _bettingRoundUsecase.RunBettingRound(state, actionProvider, broadcaster);
                if (result.Type == BettingRoundResultType.HandEndedByFold)
                {
                    earlyWin = true;
                    earlyWinnerSeatIndex = result.WinningSeatIndex;
                }
            }

            // Flop
            if (!earlyWin && !IsEarlyTermination(state))
            {
                state.Phase = GamePhase.Flop;
                var flopCards = new List<Card> { deck.Draw(), deck.Draw(), deck.Draw() };
                foreach (var card in flopCards)
                {
                    state.AddCommunityCard(card);
                }
                broadcaster.Broadcast(new CardsDealtEvent(
                    DateTime.UtcNow.Ticks, "", CardDealType.CommunityFlop, flopCards, -1));

                ResetCurrentBetsForNewStreet(state);
                var result = await _bettingRoundUsecase.RunBettingRound(state, actionProvider, broadcaster);
                if (result.Type == BettingRoundResultType.HandEndedByFold)
                {
                    earlyWin = true;
                    earlyWinnerSeatIndex = result.WinningSeatIndex;
                }
            }

            // Turn
            if (!earlyWin && !IsEarlyTermination(state))
            {
                state.Phase = GamePhase.Turn;
                var turnCard = deck.Draw();
                state.AddCommunityCard(turnCard);
                broadcaster.Broadcast(new CardsDealtEvent(
                    DateTime.UtcNow.Ticks, "", CardDealType.CommunityTurn, new List<Card> { turnCard }, -1));

                ResetCurrentBetsForNewStreet(state);
                var result = await _bettingRoundUsecase.RunBettingRound(state, actionProvider, broadcaster);
                if (result.Type == BettingRoundResultType.HandEndedByFold)
                {
                    earlyWin = true;
                    earlyWinnerSeatIndex = result.WinningSeatIndex;
                }
            }

            // River
            if (!earlyWin && !IsEarlyTermination(state))
            {
                state.Phase = GamePhase.River;
                var riverCard = deck.Draw();
                state.AddCommunityCard(riverCard);
                broadcaster.Broadcast(new CardsDealtEvent(
                    DateTime.UtcNow.Ticks, "", CardDealType.CommunityRiver, new List<Card> { riverCard }, -1));

                ResetCurrentBetsForNewStreet(state);
                var result = await _bettingRoundUsecase.RunBettingRound(state, actionProvider, broadcaster);
                if (result.Type == BettingRoundResultType.HandEndedByFold)
                {
                    earlyWin = true;
                    earlyWinnerSeatIndex = result.WinningSeatIndex;
                }
            }

            // === Phase 4 — 쇼다운 / 조기 종료 ===

            state.Phase = GamePhase.Showdown;

            var payouts = _winnerResolver.Resolve(state);

            // 쇼다운 이벤트 (Active/AllIn 플레이어가 2명 이상인 경우)
            int activeOrAllInCount = state.Players.Count(p =>
                p.Status == PlayerStatus.Active || p.Status == PlayerStatus.AllIn);

            if (activeOrAllInCount >= 2)
            {
                var showdownEntries = new List<ShowdownEntry>();
                for (int i = 0; i < state.Players.Count; i++)
                {
                    var player = state.Players[i];
                    if (player.Status == PlayerStatus.Active || player.Status == PlayerStatus.AllIn)
                    {
                        var allCards = new List<Card>(player.HoleCards);
                        allCards.AddRange(state.CommunityCards);

                        var bestFive = FindBestFiveCards(allCards);
                        var eval = HandEvaluator.Evaluate(new List<Card>(bestFive));
                        showdownEntries.Add(new ShowdownEntry(
                            i, player.HoleCards.AsReadOnly(), eval.Rank, false));
                    }
                }
                broadcaster.Broadcast(new ShowdownResultEvent(
                    DateTime.UtcNow.Ticks, "", showdownEntries));
            }

            // 칩 지급
            foreach (var (playerId, amount) in payouts)
            {
                var player = state.Players.First(p => p.Id == playerId);
                player.Chips += amount;
            }

            // 정산 이벤트
            var awards = new List<PotAward>();
            foreach (var (playerId, amount) in payouts)
            {
                int seatIdx = state.Players.FindIndex(p => p.Id == playerId);
                awards.Add(new PotAward(seatIdx, amount, "Main"));
            }
            broadcaster.Broadcast(new HandEndedEvent(
                DateTime.UtcNow.Ticks, "", awards,
                earlyWin ? HandEndReason.LastManStanding : HandEndReason.Showdown));

            // === Phase 5 — 정리 ===

            foreach (var player in state.Players)
            {
                player.HoleCards.Clear();
                player.CurrentBet = 0;

                if (player.Status != PlayerStatus.Eliminated)
                {
                    if (player.Chips == 0)
                    {
                        player.Status = PlayerStatus.Eliminated;
                    }
                    else
                    {
                        player.Status = PlayerStatus.Waiting;
                    }
                }
            }

            state.CommunityCards.Clear();
            state.Pots.Clear();
            state.LastRaiseSize = state.Blinds.BigBlind;

            repository.Save(state);
        }

        private static void PostBlind(PlayerData player, int blindAmount)
        {
            if (player.Chips <= blindAmount)
            {
                // 칩이 블라인드보다 적으면 보유 칩 전액 베팅 후 AllIn
                player.CurrentBet = player.Chips;
                player.Chips = 0;
                player.Status = PlayerStatus.AllIn;
            }
            else
            {
                player.CurrentBet = blindAmount;
                player.Chips -= blindAmount;
            }
        }

        private static bool IsEarlyTermination(GameState state)
        {
            int remainingCount = state.Players.Count(p =>
                p.Status == PlayerStatus.Active || p.Status == PlayerStatus.AllIn);
            return remainingCount <= 1;
        }

        private static void ResetCurrentBetsForNewStreet(GameState state)
        {
            state.LastRaiseSize = state.Blinds.BigBlind;
        }

        private static IReadOnlyList<Card> FindBestFiveCards(List<Card> allCards)
        {
            var combinations = CombinationUtil.GetCombinations(allCards, 5);
            List<Card> bestCombo = null;
            HandEvaluation bestEval = null;

            foreach (var combo in combinations)
            {
                var eval = HandEvaluator.Evaluate(combo);
                if (bestEval == null || HandEvaluator.Compare(eval, bestEval) > 0)
                {
                    bestEval = eval;
                    bestCombo = combo;
                }
            }

            return bestCombo;
        }
    }
}
