// Source: Assets/Scripts/Director/GameViewEventData.cs
// GameViewEventData.cs
// View 계층이 구독할 게임 이벤트 데이터 구조체 모음.
// 각 이벤트는 GameEventBase를 상속하며, 불변(immutable) 데이터만 보유한다.
// Director가 게임 진행 상태를 View에 전달할 때 사용하는 View 전용 이벤트들이다.
// PlayerSeatUpdatedEvent, HoleCardsDealtEvent, CommunityCardDealtEvent,
// PlayerActionDisplayEvent, ViewPotUpdatedEvent, ShowdownRevealEvent,
// RoundResultEvent 총 7종의 이벤트를 포함한다.

using System.Collections.Generic;
using TexasHoldem.Entity;

namespace TexasHoldem.Director
{
    /// <summary>
    /// 플레이어 좌석 정보가 갱신되었을 때 발행되는 이벤트.
    /// 이름, 칩 스택, 딜러 여부, 폴드/올인/활성 상태를 포함한다.
    /// </summary>
    public class PlayerSeatUpdatedEvent : GameEventBase
    {
        public int SeatIndex { get; }
        public string PlayerName { get; }
        public int ChipStack { get; }
        public bool IsDealer { get; }
        public bool IsFolded { get; }
        public bool IsAllIn { get; }
        public bool IsActive { get; }

        public PlayerSeatUpdatedEvent(long timestamp, string handId, int seatIndex,
            string playerName, int chipStack, bool isDealer, bool isFolded, bool isAllIn, bool isActive)
            : base(timestamp, handId) { /* ... */ }
        {
            SeatIndex = seatIndex;
            PlayerName = playerName;
            ChipStack = chipStack;
            IsDealer = isDealer;
            IsFolded = isFolded;
            IsAllIn = isAllIn;
            IsActive = isActive;
        }
    }

    /// <summary>
    /// 홀카드가 딜되었을 때 발행되는 이벤트.
    /// 대상 좌석, 카드 목록, 앞면 공개 여부를 포함한다.
    /// </summary>
    public class HoleCardsDealtEvent : GameEventBase
    {
        public int SeatIndex { get; }
        public IReadOnlyList<Card> Cards { get; }
        public bool IsFaceUp { get; }

        public HoleCardsDealtEvent(long timestamp, string handId, int seatIndex,
            IReadOnlyList<Card> cards, bool isFaceUp)
            : base(timestamp, handId) { /* ... */ }
        {
            SeatIndex = seatIndex;
            Cards = cards;
            IsFaceUp = isFaceUp;
        }
    }

    /// <summary>
    /// 커뮤니티 카드 한 장이 딜되었을 때 발행되는 이벤트.
    /// 카드 인덱스(0~4)와 카드 데이터를 포함한다.
    /// </summary>
    public class CommunityCardDealtEvent : GameEventBase
    {
        public int CardIndex { get; }
        public Card Card { get; }

        public CommunityCardDealtEvent(long timestamp, string handId, int cardIndex, Card card)
            : base(timestamp, handId) { /* ... */ }
        {
            CardIndex = cardIndex;
            Card = card;
        }
    }

    /// <summary>
    /// 플레이어 액션을 View에 표시하기 위해 발행되는 이벤트.
    /// 좌석 인덱스, 액션 타입, 베팅 금액을 포함한다.
    /// </summary>
    public class PlayerActionDisplayEvent : GameEventBase
    {
        public int SeatIndex { get; }
        public ActionType ActionType { get; }
        public int BetAmount { get; }

        public PlayerActionDisplayEvent(long timestamp, string handId, int seatIndex,
            ActionType actionType, int betAmount)
            : base(timestamp, handId) { /* ... */ }
        {
            SeatIndex = seatIndex;
            ActionType = actionType;
            BetAmount = betAmount;
        }
    }

    /// <summary>
    /// View용 팟 갱신 이벤트.
    /// 메인 팟 금액과 사이드 팟 금액 배열을 포함한다.
    /// Entity의 PotUpdatedEvent와 구분하기 위해 ViewPotUpdatedEvent로 명명한다.
    /// </summary>
    public class ViewPotUpdatedEvent : GameEventBase
    {
        public int MainPot { get; }
        public IReadOnlyList<int> SidePots { get; }

        public ViewPotUpdatedEvent(long timestamp, string handId, int mainPot, IReadOnlyList<int> sidePots)
            : base(timestamp, handId) { /* ... */ }
        {
            MainPot = mainPot;
            SidePots = sidePots ?? new List<int>();
        }
    }

    /// <summary>
    /// 쇼다운 시 특정 좌석의 홀카드를 공개하는 이벤트.
    /// 좌석 인덱스와 공개할 카드 목록을 포함한다.
    /// </summary>
    public class ShowdownRevealEvent : GameEventBase
    {
        public int SeatIndex { get; }
        public IReadOnlyList<Card> Cards { get; }

        public ShowdownRevealEvent(long timestamp, string handId, int seatIndex, IReadOnlyList<Card> cards)
            : base(timestamp, handId) { /* ... */ }
        {
            SeatIndex = seatIndex;
            Cards = cards;
        }
    }

    /// <summary>
    /// 라운드 결과 이벤트.
    /// 승리한 좌석 인덱스 목록과 각 승자가 획득한 금액 목록을 포함한다.
    /// WinningSeatIndices[i]가 Amounts[i]만큼 획득한다.
    /// </summary>
    public class RoundResultEvent : GameEventBase
    {
        public IReadOnlyList<int> WinningSeatIndices { get; }
        public IReadOnlyList<int> Amounts { get; }

        public RoundResultEvent(long timestamp, string handId, IReadOnlyList<int> winningSeatIndices,
            IReadOnlyList<int> amounts)
            : base(timestamp, handId) { /* ... */ }
        {
            WinningSeatIndices = winningSeatIndices;
            Amounts = amounts;
        }
    }
}
