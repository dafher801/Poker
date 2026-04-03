// ResultView.cs
// 게임 종료 결과 화면 MonoBehaviour View.
// SessionDirector로부터 세션 결과 DTO(SessionResult)를 받아 표시한다.
// 구성 요소:
//   (1) 우승자 이름·칩 수를 강조 표시하는 헤더 영역.
//   (2) 전체 플레이어 최종 순위 리스트 (VerticalLayoutGroup) — 각 항목에 순위·이름·최종 칩·탈락 핸드 번호 표시.
//   (3) 유저가 탈락하여 종료된 경우 '탈락했습니다' 메시지 추가 표시.
//   (4) '로비로 돌아가기' Button — 클릭 시 OnReturnToLobbyClicked 이벤트 발행.
// 게임 진행 중에는 비활성(SetActive(false)) 상태를 유지한다.
// 사용법: SessionDirector에서 ShowResult(sessionResult, isHumanEliminated)를 호출한다.

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Poker.Entity;

namespace TexasHoldem.View
{
    public class ResultView : MonoBehaviour
    {
        [Header("Winner Header")]
        [SerializeField] private TMP_Text _winnerNameText;
        [SerializeField] private TMP_Text _winnerChipsText;

        [Header("Rankings")]
        [SerializeField] private Transform _rankingContainer;
        [SerializeField] private GameObject _rankingItemPrefab;

        [Header("Elimination Message")]
        [SerializeField] private TMP_Text _eliminationText;

        [Header("Buttons")]
        [SerializeField] private Button _returnToLobbyButton;

        public event Action OnReturnToLobbyClicked;

        private void Awake()
        {
            _returnToLobbyButton.onClick.AddListener(HandleReturnToLobbyClicked);
        }

        private void OnDestroy()
        {
            _returnToLobbyButton.onClick.RemoveListener(HandleReturnToLobbyClicked);
        }

        /// <summary>
        /// 세션 결과를 표시한다.
        /// </summary>
        /// <param name="result">세션 결과 DTO</param>
        /// <param name="isHumanEliminated">유저가 탈락하여 종료된 경우 true</param>
        public void ShowResult(SessionResult result, bool isHumanEliminated)
        {
            Debug.Log($"[DEBUG][ResultView] ShowResult 진입. result null={result == null}, isHumanEliminated={isHumanEliminated}");

            if (result == null)
                throw new ArgumentNullException(nameof(result));

            ClearRankings();

            // 우승자 헤더
            var winnerRanking = result.Rankings[0];
            _winnerNameText.text = winnerRanking.PlayerId;
            _winnerChipsText.text = $"{winnerRanking.FinalChips:#,0} chips";

            // 순위 리스트
            foreach (var ranking in result.Rankings)
            {
                CreateRankingItem(ranking);
            }

            // 탈락 메시지
            if (_eliminationText != null)
            {
                _eliminationText.gameObject.SetActive(isHumanEliminated);
                if (isHumanEliminated)
                {
                    _eliminationText.text = "You have been eliminated";
                }
            }

            Debug.Log($"[DEBUG][ResultView] SetActive(true) 호출 직전. 현재 active={gameObject.activeInHierarchy}, parent active={transform.parent?.gameObject.activeInHierarchy}");
            gameObject.SetActive(true);
            Debug.Log($"[DEBUG][ResultView] SetActive(true) 호출 완료. 현재 active={gameObject.activeInHierarchy}");
        }

        /// <summary>
        /// 결과 화면을 숨긴다.
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void CreateRankingItem(PlayerRanking ranking)
        {
            if (_rankingItemPrefab == null || _rankingContainer == null)
                return;

            var item = Instantiate(_rankingItemPrefab, _rankingContainer);
            item.SetActive(true);

            // 순위 항목 텍스트 구성
            var texts = item.GetComponentsInChildren<TMP_Text>();
            if (texts.Length >= 1)
            {
                string eliminatedInfo = ranking.EliminatedAtHand.HasValue
                    ? $"  (Eliminated at hand #{ranking.EliminatedAtHand.Value})"
                    : "";
                texts[0].text = $"#{ranking.Rank}  {ranking.PlayerId}  —  {ranking.FinalChips:#,0} chips{eliminatedInfo}";
            }
        }

        private void ClearRankings()
        {
            if (_rankingContainer == null)
                return;

            for (int i = _rankingContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_rankingContainer.GetChild(i).gameObject);
            }
        }

        private void HandleReturnToLobbyClicked()
        {
            OnReturnToLobbyClicked?.Invoke();
        }
    }
}
