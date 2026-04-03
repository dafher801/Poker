// TableLayoutManager.cs
// 타원형 테이블 위에 플레이어 슬롯 위치를 계산·관리하는 MonoBehaviour.
// 타원 방정식 (radiusX * cos(θ), radiusY * sin(θ))으로 좌석 위치를 계산하며,
// 로컬 플레이어 좌석(seatIndex 0)이 화면 하단 중앙(θ = 270°)에 오도록 오프셋한다.
// SetActivePlayerCount()로 실제 참가 인원수를 설정하면 해당 수만큼 균등 배치된다.
// InitializeSlots()으로 PlayerSlotView 프리팹을 인스턴스화하여 각 위치에 배치한다.

using UnityEngine;

namespace TexasHoldem.View
{
    public class TableLayoutManager : MonoBehaviour
    {
        [Tooltip("타원 X축 반지름")]
        [SerializeField] private float _radiusX = 5f;

        [Tooltip("타원 Y축 반지름")]
        [SerializeField] private float _radiusY = 2.5f;

        [Tooltip("최대 좌석 수")]
        [SerializeField] private int _maxSeats = 10;

        [Tooltip("테이블 중앙 위치")]
        [SerializeField] private Transform _tableCenter;

        private const float StartAngleDegrees = 270f;
        private const float BetChipLerpFactor = 0.4f;

        private int _activePlayerCount;

        public int MaxSeats => _maxSeats;

        /// <summary>
        /// 실제 참가하는 플레이어 수를 설정한다.
        /// 이후 GetSeatPosition()은 이 수에 맞춰 균등 배치한다.
        /// 0 이하이면 _maxSeats를 사용한다.
        /// </summary>
        public void SetActivePlayerCount(int count)
        {
            _activePlayerCount = Mathf.Clamp(count, 2, _maxSeats);
        }

        /// <summary>
        /// 현재 활성 플레이어 수를 반환한다. 설정되지 않았으면 _maxSeats.
        /// </summary>
        public int ActivePlayerCount => _activePlayerCount > 0 ? _activePlayerCount : _maxSeats;

        /// <summary>
        /// 해당 좌석의 월드 좌표를 반환한다.
        /// seatIndex 0은 하단 중앙(270°)에 위치하며, 반시계 방향으로 배치된다.
        /// 활성 플레이어 수에 맞춰 균등 배치된다.
        /// </summary>
        public Vector3 GetSeatPosition(int seatIndex)
        {
            int divisor = _activePlayerCount > 0 ? _activePlayerCount : _maxSeats;
            float angleDegrees = StartAngleDegrees + (360f / divisor) * seatIndex;
            float angleRadians = angleDegrees * Mathf.Deg2Rad;

            Vector3 center = _tableCenter != null ? _tableCenter.position : transform.position;
            float x = center.x + _radiusX * Mathf.Cos(angleRadians);
            float y = center.y + _radiusY * Mathf.Sin(angleRadians);

            return new Vector3(x, y, center.z);
        }

        /// <summary>
        /// 좌석과 테이블 중앙 사이 40% 지점의 월드 좌표를 반환한다.
        /// 플레이어 베팅 칩 표시 위치로 사용한다.
        /// </summary>
        public Vector3 GetBetChipPosition(int seatIndex)
        {
            Vector3 seatPos = GetSeatPosition(seatIndex);
            Vector3 center = _tableCenter != null ? _tableCenter.position : transform.position;

            return Vector3.Lerp(seatPos, center, BetChipLerpFactor);
        }

        /// <summary>
        /// PlayerSlotView 프리팹을 ActivePlayerCount개 인스턴스화하여 각 좌석 위치에 배치한다.
        /// </summary>
        public GameObject[] InitializeSlots(GameObject prefab)
        {
            int count = ActivePlayerCount;
            GameObject[] slots = new GameObject[count];

            for (int i = 0; i < count; i++)
            {
                Vector3 position = GetSeatPosition(i);
                GameObject slot = Instantiate(prefab, position, Quaternion.identity, transform);
                slot.name = $"PlayerSlot_{i}";
                slots[i] = slot;
            }

            return slots;
        }

        /// <summary>
        /// 이미 생성된 슬롯들의 위치를 현재 ActivePlayerCount에 맞게 재배치한다.
        /// 활성 수 이내의 슬롯은 활성화하고 위치를 갱신, 나머지는 비활성화한다.
        /// </summary>
        public void RepositionSlots(GameObject[] slots)
        {
            if (slots == null) return;

            int count = ActivePlayerCount;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) continue;

                if (i < count)
                {
                    slots[i].SetActive(true);
                    slots[i].transform.position = GetSeatPosition(i);
                }
                else
                {
                    slots[i].SetActive(false);
                }
            }
        }
    }
}
