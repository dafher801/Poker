// Source: Assets/Scripts/Entity/Pot.cs
// Pot.cs
// 포커 게임의 단일 팟(상금 풀)을 나타내는 엔티티.
// Amount는 0 이상이어야 하며, EligiblePlayerIds는 중복 없는 플레이어 ID 목록이다.
// AddAmount(int)로 팟 금액을 증가시키고, AddEligiblePlayer(string)로 자격 플레이어를 추가한다.
// RemovePlayer(string)로 특정 플레이어를 자격 목록에서 제거한다.

using System;
using System.Collections.Generic;

namespace TexasHoldem.Entity
{
    public class Pot
    {
        public int Amount { get; private set; }
        public List<string> EligiblePlayerIds { get; }

        public Pot(int amount, List<string> eligiblePlayerIds) { /* ... */ }
        public Pot() : this(0, new List<string>()) { /* ... */ }

        // 팟 금액을 증가시킨다. 음수 불가.
        public void AddAmount(int value) { /* ... */ }

        // 자격 플레이어를 추가한다. 중복 ID는 무시된다.
        public void AddEligiblePlayer(string playerId) { /* ... */ }

        // 특정 플레이어를 자격 목록에서 제거한다.
        public void RemovePlayer(string playerId) { /* ... */ }
    }
}
