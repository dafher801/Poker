// Source: Assets/Scripts/View/ActionPanelView.cs
// ActionPanelView.cs
// 로컬 플레이어의 액션 선택 UI를 담당하는 View 클래스.
// Fold, Check, Call, Raise 버튼과 레이즈 금액 슬라이더를 관리한다.
// Show(PlayerActionContext)로 패널을 활성화하고, 유저가 버튼을 클릭하면
// OnActionSelected 콜백을 통해 선택된 액션 타입과 금액을 외부에 전달한다.
// Hide()로 패널을 비활성화한다.

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TexasHoldem.Entity;

namespace TexasHoldem.View
{
    public class ActionPanelView : MonoBehaviour
    {
        [Header("Panel Root")]
        [SerializeField] private GameObject _panelRoot;

        [Header("Buttons")]
        [SerializeField] private Button _foldButton;
        [SerializeField] private Button _checkButton;
        [SerializeField] private Button _callButton;
        [SerializeField] private Button _raiseButton;

        [Header("Labels")]
        [SerializeField] private TMP_Text _callLabel;
        [SerializeField] private TMP_Text _raiseLabel;
        [SerializeField] private TMP_Text _sliderValueLabel;

        [Header("Slider")]
        [SerializeField] private Slider _raiseSlider;

        /// <summary>
        /// 유저가 액션을 선택했을 때 발생하는 콜백.
        /// 파라미터: (ActionType type, int amount)
        /// </summary>
        public Action<ActionType, int> OnActionSelected;

        private int _currentRaiseValue;

        private void Awake() { /* ... */ }
        {
            Debug.Log("[ActionPanelView] Awake - 버튼 리스너 등록 시작");
            _foldButton.onClick.AddListener(OnFoldClicked);
            _checkButton.onClick.AddListener(OnCheckClicked);
            _callButton.onClick.AddListener(OnCallClicked);
            _raiseButton.onClick.AddListener(OnRaiseClicked);
            _raiseSlider.onValueChanged.AddListener(OnSliderValueChanged);

            _panelRoot.SetActive(false);
        }

        /// <summary>
        /// 액션 패널을 활성화하고, PlayerActionContext에 따라 버튼/슬라이더를 설정한다.
        /// </summary>
        public void Show(PlayerActionContext ctx) { /* ... */ }
        {
            _foldButton.interactable = ctx.ValidActions.Contains(ActionType.Fold);
            _checkButton.interactable = ctx.ValidActions.Contains(ActionType.Check);
            _callButton.interactable = ctx.ValidActions.Contains(ActionType.Call);

            bool canRaise = ctx.ValidActions.Contains(ActionType.Raise);
            _raiseButton.interactable = canRaise;
            _raiseSlider.interactable = canRaise;

            // Check 버튼: Call이 불가능하고 Check가 가능하면 표시, 아니면 숨김
            _checkButton.gameObject.SetActive(ctx.ValidActions.Contains(ActionType.Check));
            _callButton.gameObject.SetActive(!ctx.ValidActions.Contains(ActionType.Check));

            // Call 레이블 갱신
            if (ctx.ValidActions.Contains(ActionType.Call))
            {
                _callLabel.text = $"Call {ctx.CurrentBetToCall}";
            }
            else if (ctx.ValidActions.Contains(ActionType.AllIn))
            {
                _callButton.gameObject.SetActive(true);
                _callButton.interactable = true;
                _callLabel.text = $"All-In {ctx.PlayerChips}";
            }

            // 슬라이더 범위 설정
            if (canRaise)
            {
                _raiseSlider.minValue = ctx.MinRaiseAmount;
                _raiseSlider.maxValue = ctx.MaxRaiseAmount;
                _raiseSlider.wholeNumbers = true;
                _raiseSlider.value = ctx.MinRaiseAmount;
                _currentRaiseValue = ctx.MinRaiseAmount;
                UpdateRaiseLabels(ctx.MinRaiseAmount);
            }
            else
            {
                _raiseSlider.minValue = 0;
                _raiseSlider.maxValue = 0;
                _raiseSlider.value = 0;
                _currentRaiseValue = 0;
                UpdateRaiseLabels(0);
            }

            _panelRoot.SetActive(true);
        }

        /// <summary>
        /// 액션 패널을 비활성화한다.
        /// </summary>
        public void Hide() { /* ... */ }
        {
            _panelRoot.SetActive(false);
        }

        private void OnFoldClicked() { /* ... */ }
        {
            Debug.Log("[ActionPanelView] Fold 버튼 클릭됨");
            OnActionSelected?.Invoke(ActionType.Fold, 0);
        }

        private void OnCheckClicked() { /* ... */ }
        {
            Debug.Log("[ActionPanelView] Check 버튼 클릭됨");
            OnActionSelected?.Invoke(ActionType.Check, 0);
        }

        private void OnCallClicked() { /* ... */ }
        {
            Debug.Log("[ActionPanelView] Call 버튼 클릭됨");
            OnActionSelected?.Invoke(ActionType.Call, 0);
        }

        private void OnRaiseClicked() { /* ... */ }
        {
            Debug.Log($"[ActionPanelView] Raise 버튼 클릭됨 (amount: { /* ... */ }
            OnActionSelected?.Invoke(ActionType.Raise, _currentRaiseValue);
        }

        private void OnSliderValueChanged(float value) { /* ... */ }
        {
            Debug.Log($"[ActionPanelView] 슬라이더 값 변경: { /* ... */ }
            _currentRaiseValue = Mathf.RoundToInt(value);
            UpdateRaiseLabels(_currentRaiseValue);
        }

        private void UpdateRaiseLabels(int value) { /* ... */ }
        {
            _raiseLabel.text = $"Raise to {value}";
            _sliderValueLabel.text = value.ToString();
        }
    }
}
