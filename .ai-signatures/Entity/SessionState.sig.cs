// Source: Assets/Scripts/Entity/SessionState.cs
// SessionState.cs
// 멀티 핸드 세션의 진행 상태를 추적하는 뮤터블 엔티티.
// SessionConfig를 기반으로 초기화하며, 핸드 간 칩 갱신·탈락 처리·딜러 이동 등을 관리한다.
// 사용법: new SessionState(playerIds, startingChips) 로 생성 후
//         EliminatePlayer, GetActivePlayers, IsSessionOver 등으로 세션 상태를 조회·변경한다.
using System;
using System.Collections.Generic;
using System.Linq;

namespace Poker.Entity
{
    public class SessionState
    {
        private readonly List<string> _playerIds;
        private readonly Dictionary<string, int> _chips;
        private readonly Dictionary<string, bool> _eliminated;
        private readonly Dictionary<string, int> _eliminatedAtHand;

        public int DealerSeatIndex { get; set; }
        public int HandCount { get; set; }

        public IReadOnlyList<string> PlayerIds => /* ... */;
        public IReadOnlyDictionary<string, int> Chips => /* ... */;
        public IReadOnlyDictionary<string, bool> Eliminated => /* ... */;
        public IReadOnlyDictionary<string, int> EliminatedAtHand => /* ... */;

        public SessionState(List<string> playerIds, int startingChips) { /* ... */ }
        {
            if (playerIds == null || playerIds.Count < 2)
                throw new ArgumentException("playerIds must contain at least 2 players.", nameof(playerIds));

            if (startingChips <= 0)
                throw new ArgumentException("startingChips must be a positive integer.", nameof(startingChips));

            _playerIds = new List<string>(playerIds);
            _chips = new Dictionary<string, int>();
            _eliminated = new Dictionary<string, bool>();
            _eliminatedAtHand = new Dictionary<string, int>();

            foreach (var id in _playerIds)
            {
                _chips[id] = startingChips;
                _eliminated[id] = false;
            }

            DealerSeatIndex = 0;
            HandCount = 0;
        }

        public void SetChips(string playerId, int amount) { /* ... */ }
        {
            if (!_chips.ContainsKey(playerId))
                throw new ArgumentException($"Unknown player: {playerId}", nameof(playerId));

            _chips[playerId] = amount;
        }

        public void EliminatePlayer(string playerId) { /* ... */ }
        {
            if (!_eliminated.ContainsKey(playerId))
                throw new ArgumentException($"Unknown player: {playerId}", nameof(playerId));

            if (_chips[playerId] <= 0)
            {
                _eliminated[playerId] = true;
                _eliminatedAtHand[playerId] = HandCount;
            }
        }

        public List<string> GetActivePlayers() { /* ... */ }
        {
            return _playerIds.Where(id => !_eliminated[id]).ToList();
        }

        public bool IsSessionOver() { /* ... */ }
        {
            return GetActivePlayers().Count <= 1;
        }
    }
}
