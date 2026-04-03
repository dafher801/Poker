// ActionPanelBuilder.cs
// Unity 에디터 메뉴에서 Action Panel UI 계층 구조를 자동 생성하는 에디터 스크립트.
// Canvas 하위에 Fold·Check·Call·Raise 버튼, 레이즈 슬라이더, 금액 텍스트를 배치하고
// ActionPanelView 컴포넌트의 직렬화 필드를 자동 연결한다.
// 메뉴: TexasHoldem > Build Action Panel

using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TexasHoldem.Editor
{
    public static class ActionPanelBuilder
    {
        [MenuItem("TexasHoldem/Build Action Panel")]
        public static void BuildActionPanel()
        {
            // ── Canvas 탐색 또는 생성 ──
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                Undo.RegisterCreatedObjectUndo(canvasObj, "Build Action Panel");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();

                CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            Transform canvasTransform = canvas.transform;

            // ── PanelRoot: 하단 중앙 고정 ──
            GameObject panelRoot = CreateUIObject("ActionPanel", canvasTransform);
            Undo.RegisterCreatedObjectUndo(panelRoot, "Build Action Panel");
            RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 20f);
            panelRect.sizeDelta = new Vector2(700f, 180f);

            // 배경 이미지
            Image panelBg = panelRoot.AddComponent<Image>();
            panelBg.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);

            // ── 슬라이더 영역 (상단) ──
            GameObject sliderArea = CreateUIObject("SliderArea", panelRoot.transform);
            RectTransform sliderAreaRect = sliderArea.GetComponent<RectTransform>();
            sliderAreaRect.anchorMin = new Vector2(0f, 0.5f);
            sliderAreaRect.anchorMax = new Vector2(1f, 1f);
            sliderAreaRect.offsetMin = new Vector2(20f, 10f);
            sliderAreaRect.offsetMax = new Vector2(-20f, -10f);

            // Slider Value Label (슬라이더 위)
            GameObject sliderValueLabelObj = CreateUIObject("SliderValueLabel", sliderArea.transform);
            RectTransform sliderValueRect = sliderValueLabelObj.GetComponent<RectTransform>();
            sliderValueRect.anchorMin = new Vector2(0f, 0.55f);
            sliderValueRect.anchorMax = new Vector2(1f, 1f);
            sliderValueRect.offsetMin = Vector2.zero;
            sliderValueRect.offsetMax = Vector2.zero;
            TextMeshProUGUI sliderValueLabel = sliderValueLabelObj.AddComponent<TextMeshProUGUI>();
            sliderValueLabel.text = "0";
            sliderValueLabel.fontSize = 24;
            sliderValueLabel.alignment = TextAlignmentOptions.Center;
            sliderValueLabel.color = Color.white;

            // Raise Slider
            GameObject sliderObj = CreateSlider("RaiseSlider", sliderArea.transform);
            RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0f, 0f);
            sliderRect.anchorMax = new Vector2(1f, 0.5f);
            sliderRect.offsetMin = new Vector2(10f, 5f);
            sliderRect.offsetMax = new Vector2(-10f, -5f);
            Slider raiseSlider = sliderObj.GetComponent<Slider>();
            raiseSlider.wholeNumbers = true;

            // ── 버튼 영역 (하단) ──
            GameObject buttonArea = CreateUIObject("ButtonArea", panelRoot.transform);
            RectTransform buttonAreaRect = buttonArea.GetComponent<RectTransform>();
            buttonAreaRect.anchorMin = new Vector2(0f, 0f);
            buttonAreaRect.anchorMax = new Vector2(1f, 0.5f);
            buttonAreaRect.offsetMin = new Vector2(10f, 10f);
            buttonAreaRect.offsetMax = new Vector2(-10f, -5f);

            HorizontalLayoutGroup hlg = buttonArea.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            hlg.padding = new RectOffset(5, 5, 5, 5);

            // ── 버튼 4개 생성 ──
            Color foldColor = new Color(0.7f, 0.2f, 0.2f, 1f);
            Color checkColor = new Color(0.2f, 0.6f, 0.2f, 1f);
            Color callColor = new Color(0.2f, 0.5f, 0.7f, 1f);
            Color raiseColor = new Color(0.8f, 0.6f, 0.1f, 1f);

            GameObject foldButtonObj = CreateButton("FoldButton", buttonArea.transform, "Fold", foldColor);
            GameObject checkButtonObj = CreateButton("CheckButton", buttonArea.transform, "Check", checkColor);
            GameObject callButtonObj = CreateButton("CallButton", buttonArea.transform, "Call", callColor);
            GameObject raiseButtonObj = CreateButton("RaiseButton", buttonArea.transform, "Raise", raiseColor);

            Button foldButton = foldButtonObj.GetComponent<Button>();
            Button checkButton = checkButtonObj.GetComponent<Button>();
            Button callButton = callButtonObj.GetComponent<Button>();
            Button raiseButton = raiseButtonObj.GetComponent<Button>();

            // Call·Raise 레이블 참조 (버튼 자식의 TMP)
            TextMeshProUGUI callLabel = callButtonObj.GetComponentInChildren<TextMeshProUGUI>();
            TextMeshProUGUI raiseLabel = raiseButtonObj.GetComponentInChildren<TextMeshProUGUI>();

            // ── ActionPanelView 컴포넌트 추가 및 직렬화 필드 연결 ──
            View.ActionPanelView actionPanelView = panelRoot.AddComponent<View.ActionPanelView>();
            SerializedObject so = new SerializedObject(actionPanelView);
            so.FindProperty("_panelRoot").objectReferenceValue = panelRoot;
            so.FindProperty("_foldButton").objectReferenceValue = foldButton;
            so.FindProperty("_checkButton").objectReferenceValue = checkButton;
            so.FindProperty("_callButton").objectReferenceValue = callButton;
            so.FindProperty("_raiseButton").objectReferenceValue = raiseButton;
            so.FindProperty("_callLabel").objectReferenceValue = callLabel;
            so.FindProperty("_raiseLabel").objectReferenceValue = raiseLabel;
            so.FindProperty("_sliderValueLabel").objectReferenceValue = sliderValueLabel;
            so.FindProperty("_raiseSlider").objectReferenceValue = raiseSlider;
            so.ApplyModifiedPropertiesWithoutUndo();

            // 초기 상태: 비활성
            panelRoot.SetActive(false);

            Selection.activeGameObject = panelRoot;
            Debug.Log("[ActionPanelBuilder] Action Panel UI가 생성되었습니다.");
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private static GameObject CreateButton(string name, Transform parent, string label, Color bgColor)
        {
            GameObject btnObj = CreateUIObject(name, parent);

            Image btnImage = btnObj.AddComponent<Image>();
            btnImage.color = bgColor;

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            btn.colors = colors;

            // 텍스트 라벨
            GameObject textObj = CreateUIObject("Label", btnObj.transform);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 28;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Bold;

            return btnObj;
        }

        private static GameObject CreateSlider(string name, Transform parent)
        {
            GameObject sliderObj = CreateUIObject(name, parent);

            // Background
            GameObject background = CreateUIObject("Background", sliderObj.transform);
            RectTransform bgRect = background.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0.25f);
            bgRect.anchorMax = new Vector2(1f, 0.75f);
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            Image bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            // Fill Area
            GameObject fillArea = CreateUIObject("Fill Area", sliderObj.transform);
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRect.offsetMin = new Vector2(5f, 0f);
            fillAreaRect.offsetMax = new Vector2(-15f, 0f);

            GameObject fill = CreateUIObject("Fill", fillArea.transform);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.8f, 0.6f, 0.1f, 1f);

            // Handle Slide Area
            GameObject handleSlideArea = CreateUIObject("Handle Slide Area", sliderObj.transform);
            RectTransform handleSlideRect = handleSlideArea.GetComponent<RectTransform>();
            handleSlideRect.anchorMin = Vector2.zero;
            handleSlideRect.anchorMax = Vector2.one;
            handleSlideRect.offsetMin = new Vector2(10f, 0f);
            handleSlideRect.offsetMax = new Vector2(-10f, 0f);

            GameObject handle = CreateUIObject("Handle", handleSlideArea.transform);
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20f, 0f);
            handleRect.anchorMin = new Vector2(0f, 0f);
            handleRect.anchorMax = new Vector2(0f, 1f);
            Image handleImage = handle.AddComponent<Image>();
            handleImage.color = Color.white;

            // Slider 컴포넌트
            Slider slider = sliderObj.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0;
            slider.maxValue = 1000;
            slider.value = 0;

            return sliderObj;
        }
    }
}
