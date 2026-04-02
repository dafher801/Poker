// Source: Assets/Scripts/View/TableLayoutManager.cs
// TableLayoutManager.cs
// 타원형 테이블 위에 최대 10개 플레이어 슬롯 위치를 계산·관리하는 MonoBehaviour.
// 타원 방정식 (radiusX * cos(θ), radiusY * sin(θ))으로 좌석 위치를 계산하며,
// 로컬 플레이어 좌석(seatIndex 0)이 화면 하단 중앙(θ = 270°)에 오도록 오프셋한다.
// InitializeSlots()으로 PlayerSlotView 프리팹을 maxSeats개 인스턴스화하여 각 위치에 배치한다.

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

        public int MaxSeats => /* ... */;

        /// <summary>
        /// 해당 좌석의 월드 좌표를 반환한다.
        /// seatIndex 0은 하단 중앙(270°)에 위치하며, 반시계 방향으로 배치된다.
        /// </summary>
        public Vector3 GetSeatPosition(int seatIndex) { /* ... */ }
        {
            float angleDegrees = StartAngleDegrees + (360f / _maxSeats) * seatIndex;
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
        public Vector3 GetBetChipPosition(int seatIndex) { /* ... */ }
        {
            Vector3 seatPos = GetSeatPosition(seatIndex);
            Vector3 center = _tableCenter != null ? _tableCenter.position : transform.position;

            return Vector3.Lerp(seatPos, center, BetChipLerpFactor);
        }

        /// <summary>
        /// PlayerSlotView 프리팹을 maxSeats개 인스턴스화하여 각 좌석 위치에 배치한다.
        /// </summary>
        public GameObject[] InitializeSlots(GameObject prefab) { /* ... */ }
        {
            GameObject[] slots = new GameObject[_maxSeats];

            for (int i = 0; i < _maxSeats; i++)
            {
                Vector3 position = GetSeatPosition(i);
                GameObject slot = Instantiate(prefab, position, Quaternion.identity, transform);
                slot.name = $"PlayerSlot_{i}";
                slots[i] = slot;
            }

            return slots;
        }
    }
}
