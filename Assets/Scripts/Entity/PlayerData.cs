// PlayerData.cs
// 텍사스 홀덤 플레이어 한 명의 상태를 담는 Entity.
// Id, Name, Chips, HoleCards, CurrentBet, Status, SeatIndex를 관리하며
// 유효성 위반 시 예외를 발생시킨다.

using System;
using System.Collections.Generic;

namespace TexasHoldem.Entity
{
    public class PlayerData
    {
        public string Id { get; set; }
        public string Name { get; set; }

        private int _chips;
        public int Chips
        {
            get => _chips;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Chips cannot be negative.");
                _chips = value;
            }
        }

        public List<Card> HoleCards { get; } = new List<Card>();

        private int _currentBet;
        public int CurrentBet
        {
            get => _currentBet;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "CurrentBet cannot be negative.");
                _currentBet = value;
            }
        }

        public PlayerStatus Status { get; set; }

        private int _seatIndex;
        public int SeatIndex
        {
            get => _seatIndex;
            set
            {
                if (value < 0 || value > 9)
                    throw new ArgumentOutOfRangeException(nameof(value), "SeatIndex must be between 0 and 9.");
                _seatIndex = value;
            }
        }

        public PlayerData(string id, string name, int chips, int seatIndex)
        {
            Id = id;
            Name = name;
            Chips = chips;
            SeatIndex = seatIndex;
            Status = PlayerStatus.Waiting;
        }

        // HoleCards에 카드를 추가한다. 최대 2장까지만 허용한다.
        public void AddHoleCard(Card card)
        {
            if (HoleCards.Count >= 2)
                throw new InvalidOperationException("A player can hold at most 2 hole cards.");
            HoleCards.Add(card);
        }
    }
}
