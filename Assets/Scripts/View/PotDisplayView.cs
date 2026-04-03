// PotDisplayView.cs
// 메인 팟 및 사이드 팟 금액을 표시하는 MonoBehaviour.
// 테이블 중앙 상단에 배치되며, 메인 팟 라벨과 동적으로 생성되는 사이드 팟 요소들을 관리한다.
// UpdatePot(mainPot, sidePots)으로 팟 정보를 갱신하고, ClearPot()으로 초기화한다.

using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace TexasHoldem.View
{
    public class PotDisplayView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _mainPotLabel;
        [SerializeField] private Transform _sidePotContainer;
        [SerializeField] private GameObject _sidePotPrefab;

        private readonly List<GameObject> _activeSidePots = new List<GameObject>();

        /// <summary>
        /// 메인 팟과 사이드 팟 금액을 갱신한다.
        /// mainPot이 0이면 메인 팟 라벨을 숨긴다.
        /// 사이드 팟 개수에 따라 오브젝트를 동적 생성/삭제한다.
        /// </summary>
        public void UpdatePot(int mainPot, int[] sidePots)
        {
            if (_mainPotLabel != null)
            {
                if (mainPot > 0)
                {
                    _mainPotLabel.gameObject.SetActive(true);
                    _mainPotLabel.text = $"Pot: {mainPot}";
                }
                else
                {
                    _mainPotLabel.gameObject.SetActive(false);
                }
            }

            int requiredCount = sidePots != null ? sidePots.Length : 0;

            // 초과하는 사이드 팟 오브젝트 제거
            while (_activeSidePots.Count > requiredCount)
            {
                int lastIndex = _activeSidePots.Count - 1;
                Destroy(_activeSidePots[lastIndex]);
                _activeSidePots.RemoveAt(lastIndex);
            }

            // 부족한 사이드 팟 오브젝트 생성
            while (_activeSidePots.Count < requiredCount)
            {
                if (_sidePotPrefab != null && _sidePotContainer != null)
                {
                    GameObject sidePotObj = Instantiate(_sidePotPrefab, _sidePotContainer);
                    _activeSidePots.Add(sidePotObj);
                }
            }

            // 사이드 팟 텍스트 갱신
            for (int i = 0; i < requiredCount; i++)
            {
                TextMeshProUGUI label = _activeSidePots[i].GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                {
                    label.text = $"Side Pot: {sidePots[i]}";
                }
            }
        }

        /// <summary>
        /// 모든 팟 표시를 초기화한다.
        /// </summary>
        public void ClearPot()
        {
            if (_mainPotLabel != null)
            {
                _mainPotLabel.text = "";
                _mainPotLabel.gameObject.SetActive(false);
            }

            for (int i = _activeSidePots.Count - 1; i >= 0; i--)
            {
                Destroy(_activeSidePots[i]);
            }
            _activeSidePots.Clear();
        }
    }
}
