// Source: Assets/Scripts/Director/MockEventPublisher.cs
// MockEventPublisher.cs
// 테스트용 Mock 이벤트를 순차 발행하는 MonoBehaviour.
// IGameEventBroadcaster를 구현하며, Start 시 코루틴으로 포커 한 핸드 시나리오를 자동 실행한다.
// 씬에 배치하면 4명 착석 → 딜러 지정 → 홀카드 딜 → 프리플롭 베팅 → 팟 갱신 →
// 플롭 → 턴 → 리버 → 쇼다운 → 라운드 결과 순서로 이벤트를 발행하여 View를 육안 검증할 수 있다.
// 내부적으로 LocalGameEventBroadcaster에 위임하여 구독/발행을 처리한다.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TexasHoldem.Entity;

namespace TexasHoldem.Director
{
    public class MockEventPublisher : MonoBehaviour, IGameEventBroadcaster
    {
        private readonly LocalGameEventBroadcaster _broadcaster = new LocalGameEventBroadcaster();

        private const string MockHandId = "MOCK-HAND-001";

        public void Subscribe<T>(Action<T> handler) where T : GameEventBase { /* ... */ }
        {
            _broadcaster.Subscribe(handler);
        }

        public void Unsubscribe<T>(Action<T> handler) where T : GameEventBase { /* ... */ }
        {
            _broadcaster.Unsubscribe(handler);
        }

        public void Publish<T>(T gameEvent) where T : GameEventBase { /* ... */ }
        {
            _broadcaster.Publish(gameEvent);
        }

        private void Start() { /* ... */ }
        {
            StartCoroutine(RunMockScenario());
        }

        private long Now() => /* ... */;

        private IEnumerator RunMockScenario() { /* ... */ }
        {
            yield return new WaitForSeconds(0.5f);

            // (1) 4명 플레이어 착석 (seat 0, 1, 2, 3)
            string[] playerNames = { /* ... */ }
            int[] chipStacks = { 1000, 1200, 800, 1500 };

            for (int i = 0; i < 4; i++)
            {
                Publish(new PlayerSeatUpdatedEvent(
                    Now(), MockHandId, i,
                    playerNames[i], chipStacks[i],
                    isDealer: false, isFolded: false, isAllIn: false, isActive: true));
                yield return new WaitForSeconds(0.3f);
            }

            // (2) 딜러 버튼 지정 (seat 2)
            Publish(new PlayerSeatUpdatedEvent(
                Now(), MockHandId, 2,
                playerNames[2], chipStacks[2],
                isDealer: true, isFolded: false, isAllIn: false, isActive: true));
            yield return new WaitForSeconds(0.5f);

            // (3) 홀카드 딜 (각 플레이어에 2장)
            Card[][] holeCards =
            {
                new[] { /* ... */ }
                new[] { /* ... */ }
                new[] { /* ... */ }
                new[] { /* ... */ }
            };

            for (int i = 0; i < 4; i++)
            {
                Publish(new HoleCardsDealtEvent(
                    Now(), MockHandId, i,
                    new List<Card>(holeCards[i]),
                    isFaceUp: i == 0));
                yield return new WaitForSeconds(0.2f);
            }

            yield return new WaitForSeconds(1.0f);

            // (4) 프리플롭 베팅 액션
            // Charlie(seat 3) Raise 100
            Publish(new PlayerActionDisplayEvent(
                Now(), MockHandId, 3, ActionType.Raise, 100));
            yield return new WaitForSeconds(0.8f);

            // Player(seat 0) Call 100
            Publish(new PlayerActionDisplayEvent(
                Now(), MockHandId, 0, ActionType.Call, 100));
            yield return new WaitForSeconds(0.8f);

            // Alice(seat 1) Fold
            Publish(new PlayerActionDisplayEvent(
                Now(), MockHandId, 1, ActionType.Fold, 0));
            Publish(new PlayerSeatUpdatedEvent(
                Now(), MockHandId, 1,
                playerNames[1], chipStacks[1],
                isDealer: false, isFolded: true, isAllIn: false, isActive: false));
            yield return new WaitForSeconds(0.8f);

            // Bob(seat 2) Call 100
            Publish(new PlayerActionDisplayEvent(
                Now(), MockHandId, 2, ActionType.Call, 100));
            yield return new WaitForSeconds(0.8f);

            // (5) 팟 갱신
            Publish(new ViewPotUpdatedEvent(
                Now(), MockHandId, 300, new List<int>()));
            yield return new WaitForSeconds(1.0f);

            // (6) 플롭 3장 딜
            Card[] flopCards =
            {
                new Card(Suit.Heart, Rank.Ace),
                new Card(Suit.Diamond, Rank.Five),
                new Card(Suit.Club, Rank.Three)
            };

            for (int i = 0; i < 3; i++)
            {
                Publish(new CommunityCardDealtEvent(
                    Now(), MockHandId, i, flopCards[i]));
                yield return new WaitForSeconds(0.3f);
            }

            yield return new WaitForSeconds(1.5f);

            // (7) 턴 1장 딜
            Publish(new CommunityCardDealtEvent(
                Now(), MockHandId, 3, new Card(Suit.Spade, Rank.King)));
            yield return new WaitForSeconds(1.5f);

            // (8) 리버 1장 딜
            Publish(new CommunityCardDealtEvent(
                Now(), MockHandId, 4, new Card(Suit.Diamond, Rank.Two)));
            yield return new WaitForSeconds(1.5f);

            // (9) 쇼다운 (남은 플레이어 홀카드 공개: seat 2, 3)
            Publish(new ShowdownRevealEvent(
                Now(), MockHandId, 2,
                new List<Card>(holeCards[2])));
            yield return new WaitForSeconds(0.5f);

            Publish(new ShowdownRevealEvent(
                Now(), MockHandId, 3,
                new List<Card>(holeCards[3])));
            yield return new WaitForSeconds(1.5f);

            // (10) 라운드 결과 (seat 0 승리 - Ace 투페어)
            Publish(new RoundResultEvent(
                Now(), MockHandId,
                new List<int> { 0 },
                new List<int> { 300 }));

            Debug.Log("[MockEventPublisher] Mock scenario completed.");
        }
    }
}
