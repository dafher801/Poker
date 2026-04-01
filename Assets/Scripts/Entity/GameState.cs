// GameState.cs
// 텍사스 홀덤 게임 한 핸드의 전체 상태를 담는 Entity.
// Players(2~10명), CommunityCards(최대 5장), Pots, Phase, DealerIndex,
// Blinds, CurrentPlayerIndex를 관리하며 유효성 위반 시 예외를 던진다.
// DeepClone()으로 현재 상태의 독립된 복사본을 생성할 수 있다.

using System;
using System.Collections.Generic;

namespace TexasHoldem.Entity
{
    public class GameState
    {
        public List<PlayerData> Players { get; }
        public List<Card> CommunityCards { get; }
        public List<Pot> Pots { get; }
        public GamePhase Phase { get; set; }
        public BlindInfo Blinds { get; }

        private int _dealerIndex;
        public int DealerIndex
        {
            get => _dealerIndex;
            set
            {
                if (value < 0 || value >= Players.Count)
                    throw new ArgumentOutOfRangeException(nameof(value), "DealerIndex must be within Players range.");
                _dealerIndex = value;
            }
        }

        private int _currentPlayerIndex;
        public int CurrentPlayerIndex
        {
            get => _currentPlayerIndex;
            set
            {
                if (value < 0 || value >= Players.Count)
                    throw new ArgumentOutOfRangeException(nameof(value), "CurrentPlayerIndex must be within Players range.");
                _currentPlayerIndex = value;
            }
        }

        public GameState(List<PlayerData> players, BlindInfo blinds, int dealerIndex = 0, int currentPlayerIndex = 0)
        {
            if (players == null)
                throw new ArgumentNullException(nameof(players));
            if (players.Count < 2 || players.Count > 10)
                throw new ArgumentException("Players count must be between 2 and 10.", nameof(players));
            if (blinds == null)
                throw new ArgumentNullException(nameof(blinds));
            if (dealerIndex < 0 || dealerIndex >= players.Count)
                throw new ArgumentOutOfRangeException(nameof(dealerIndex), "DealerIndex must be within Players range.");
            if (currentPlayerIndex < 0 || currentPlayerIndex >= players.Count)
                throw new ArgumentOutOfRangeException(nameof(currentPlayerIndex), "CurrentPlayerIndex must be within Players range.");

            Players = players;
            Blinds = blinds;
            CommunityCards = new List<Card>();
            Pots = new List<Pot>();
            Phase = GamePhase.PreFlop;
            _dealerIndex = dealerIndex;
            _currentPlayerIndex = currentPlayerIndex;
        }

        // 복사 생성자 — DeepClone 내부에서 사용
        private GameState(GameState source)
        {
            Blinds = source.Blinds;
            Phase = source.Phase;
            _dealerIndex = source._dealerIndex;
            _currentPlayerIndex = source._currentPlayerIndex;

            Players = new List<PlayerData>(source.Players.Count);
            foreach (var p in source.Players)
            {
                var copy = new PlayerData(p.Id, p.Name, p.Chips, p.SeatIndex)
                {
                    CurrentBet = p.CurrentBet,
                    Status = p.Status
                };
                foreach (var card in p.HoleCards)
                    copy.AddHoleCard(card);
                Players.Add(copy);
            }

            CommunityCards = new List<Card>(source.CommunityCards);

            Pots = new List<Pot>(source.Pots.Count);
            foreach (var pot in source.Pots)
                Pots.Add(new Pot(pot.Amount, new List<string>(pot.EligiblePlayerIds)));
        }

        // CommunityCards에 카드를 추가한다. 최대 5장까지만 허용한다.
        public void AddCommunityCard(Card card)
        {
            if (CommunityCards.Count >= 5)
                throw new InvalidOperationException("CommunityCards cannot exceed 5 cards.");
            CommunityCards.Add(card);
        }

        // 현재 상태의 독립된 깊은 복사본을 반환한다.
        public GameState DeepClone()
        {
            return new GameState(this);
        }
    }
}
