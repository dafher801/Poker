// ResultViewBuilder.cs
// Unity 에디터 메뉴에서 ResultView UI 계층 구조를 자동 생성하는 에디터 스크립트.
// Canvas 하위에 결과 패널(우승자 헤더, 순위 리스트, 탈락 메시지, 로비 복귀 버튼)을 배치하고
// ResultView 컴포넌트의 직렬화 필드를 자동 연결한다.
// 메뉴: TexasHoldem > Build Result View

using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TexasHoldem.Editor
{
    public static class ResultViewBuilder
    {
        [MenuItem("TexasHoldem/Build Result View")]
        public static void BuildResultView()
        {
            // ── Canvas 탐색 또는 생성 ──
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                Undo.RegisterCreatedObjectUndo(canvasObj, "Build Result View");
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

            // ── ResultPanel: 화면 중앙 ──
            GameObject panelRoot = CreateUIObject("ResultPanel", canvasTransform);
            Undo.RegisterCreatedObjectUndo(panelRoot, "Build Result View");
            RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(600f, 700f);

            Image panelBg = panelRoot.AddComponent<Image>();
            panelBg.color = new Color(0.1f, 0.1f, 0.12f, 0.95f);

            // ── 헤더 영역 (상단) ──
            GameObject headerArea = CreateUIObject("HeaderArea", panelRoot.transform);
            RectTransform headerRect = headerArea.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 0.82f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.offsetMin = new Vector2(20f, 0f);
            headerRect.offsetMax = new Vector2(-20f, -15f);

            // 타이틀
            GameObject titleObj = CreateUIObject("Title", headerArea.transform);
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.6f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "게임 종료";
            titleText.fontSize = 36;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = new Color(1f, 0.85f, 0.3f, 1f);

            // 우승자 이름
            GameObject winnerNameObj = CreateUIObject("WinnerName", headerArea.transform);
            RectTransform winnerNameRect = winnerNameObj.GetComponent<RectTransform>();
            winnerNameRect.anchorMin = new Vector2(0f, 0.2f);
            winnerNameRect.anchorMax = new Vector2(1f, 0.6f);
            winnerNameRect.offsetMin = Vector2.zero;
            winnerNameRect.offsetMax = Vector2.zero;
            TextMeshProUGUI winnerNameText = winnerNameObj.AddComponent<TextMeshProUGUI>();
            winnerNameText.text = "우승자 이름";
            winnerNameText.fontSize = 30;
            winnerNameText.fontStyle = FontStyles.Bold;
            winnerNameText.alignment = TextAlignmentOptions.Center;
            winnerNameText.color = Color.white;

            // 우승자 칩
            GameObject winnerChipsObj = CreateUIObject("WinnerChips", headerArea.transform);
            RectTransform winnerChipsRect = winnerChipsObj.GetComponent<RectTransform>();
            winnerChipsRect.anchorMin = new Vector2(0f, 0f);
            winnerChipsRect.anchorMax = new Vector2(1f, 0.25f);
            winnerChipsRect.offsetMin = Vector2.zero;
            winnerChipsRect.offsetMax = Vector2.zero;
            TextMeshProUGUI winnerChipsText = winnerChipsObj.AddComponent<TextMeshProUGUI>();
            winnerChipsText.text = "0 chips";
            winnerChipsText.fontSize = 22;
            winnerChipsText.alignment = TextAlignmentOptions.Center;
            winnerChipsText.color = new Color(0.7f, 0.9f, 0.7f, 1f);

            // ── 구분선 ──
            GameObject separator = CreateUIObject("Separator", panelRoot.transform);
            RectTransform sepRect = separator.GetComponent<RectTransform>();
            sepRect.anchorMin = new Vector2(0.05f, 0.80f);
            sepRect.anchorMax = new Vector2(0.95f, 0.805f);
            sepRect.offsetMin = Vector2.zero;
            sepRect.offsetMax = Vector2.zero;
            Image sepImage = separator.AddComponent<Image>();
            sepImage.color = new Color(0.4f, 0.4f, 0.4f, 0.6f);

            // ── 순위 리스트 영역 (중앙) ──
            GameObject scrollViewObj = CreateUIObject("RankingScrollView", panelRoot.transform);
            RectTransform scrollRect = scrollViewObj.GetComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0f, 0.18f);
            scrollRect.anchorMax = new Vector2(1f, 0.79f);
            scrollRect.offsetMin = new Vector2(20f, 0f);
            scrollRect.offsetMax = new Vector2(-20f, 0f);

            Image scrollBg = scrollViewObj.AddComponent<Image>();
            scrollBg.color = new Color(0.08f, 0.08f, 0.1f, 0.5f);

            ScrollRect scroll = scrollViewObj.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;

            // Viewport
            GameObject viewport = CreateUIObject("Viewport", scrollViewObj.transform);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewport.AddComponent<Image>().color = Color.white;
            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            // Content (VerticalLayoutGroup)
            GameObject content = CreateUIObject("Content", viewport.transform);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = new Vector2(0f, 0f);
            contentRect.offsetMax = new Vector2(0f, 0f);

            VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 5f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(10, 10, 10, 10);

            ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewportRect;
            scroll.content = contentRect;

            // ── 순위 아이템 프리팹 (비활성 템플릿) ──
            GameObject rankingItemTemplate = CreateUIObject("RankingItemTemplate", content.transform);
            RectTransform itemRect = rankingItemTemplate.GetComponent<RectTransform>();
            itemRect.sizeDelta = new Vector2(0f, 40f);

            Image itemBg = rankingItemTemplate.AddComponent<Image>();
            itemBg.color = new Color(0.15f, 0.15f, 0.18f, 0.7f);

            LayoutElement itemLayout = rankingItemTemplate.AddComponent<LayoutElement>();
            itemLayout.minHeight = 40f;
            itemLayout.preferredHeight = 40f;

            // 순위 텍스트
            GameObject itemTextObj = CreateUIObject("RankingText", rankingItemTemplate.transform);
            RectTransform itemTextRect = itemTextObj.GetComponent<RectTransform>();
            itemTextRect.anchorMin = Vector2.zero;
            itemTextRect.anchorMax = Vector2.one;
            itemTextRect.offsetMin = new Vector2(15f, 0f);
            itemTextRect.offsetMax = new Vector2(-15f, 0f);
            TextMeshProUGUI itemText = itemTextObj.AddComponent<TextMeshProUGUI>();
            itemText.text = "#1  Player  —  1,000 chips";
            itemText.fontSize = 20;
            itemText.alignment = TextAlignmentOptions.MidlineLeft;
            itemText.color = Color.white;

            rankingItemTemplate.SetActive(false);

            // ── 탈락 메시지 (순위 리스트 아래) ──
            GameObject eliminationObj = CreateUIObject("EliminationText", panelRoot.transform);
            RectTransform elimRect = eliminationObj.GetComponent<RectTransform>();
            elimRect.anchorMin = new Vector2(0f, 0.12f);
            elimRect.anchorMax = new Vector2(1f, 0.18f);
            elimRect.offsetMin = new Vector2(20f, 0f);
            elimRect.offsetMax = new Vector2(-20f, 0f);
            TextMeshProUGUI eliminationText = eliminationObj.AddComponent<TextMeshProUGUI>();
            eliminationText.text = "탈락했습니다";
            eliminationText.fontSize = 24;
            eliminationText.fontStyle = FontStyles.Bold;
            eliminationText.alignment = TextAlignmentOptions.Center;
            eliminationText.color = new Color(0.9f, 0.3f, 0.3f, 1f);
            eliminationObj.SetActive(false);

            // ── 로비 복귀 버튼 (하단) ──
            GameObject returnBtnObj = CreateUIObject("ReturnToLobbyButton", panelRoot.transform);
            RectTransform returnBtnRect = returnBtnObj.GetComponent<RectTransform>();
            returnBtnRect.anchorMin = new Vector2(0.2f, 0.02f);
            returnBtnRect.anchorMax = new Vector2(0.8f, 0.10f);
            returnBtnRect.offsetMin = Vector2.zero;
            returnBtnRect.offsetMax = Vector2.zero;

            Image btnBg = returnBtnObj.AddComponent<Image>();
            btnBg.color = new Color(0.2f, 0.5f, 0.7f, 1f);

            Button returnBtn = returnBtnObj.AddComponent<Button>();
            ColorBlock colors = returnBtn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            returnBtn.colors = colors;

            GameObject btnLabelObj = CreateUIObject("Label", returnBtnObj.transform);
            RectTransform btnLabelRect = btnLabelObj.GetComponent<RectTransform>();
            btnLabelRect.anchorMin = Vector2.zero;
            btnLabelRect.anchorMax = Vector2.one;
            btnLabelRect.offsetMin = Vector2.zero;
            btnLabelRect.offsetMax = Vector2.zero;
            TextMeshProUGUI btnLabel = btnLabelObj.AddComponent<TextMeshProUGUI>();
            btnLabel.text = "로비로 돌아가기";
            btnLabel.fontSize = 24;
            btnLabel.fontStyle = FontStyles.Bold;
            btnLabel.alignment = TextAlignmentOptions.Center;
            btnLabel.color = Color.white;

            // ── ResultView 컴포넌트 추가 및 직렬화 필드 연결 ──
            View.ResultView resultView = panelRoot.AddComponent<View.ResultView>();
            SerializedObject so = new SerializedObject(resultView);
            so.FindProperty("_winnerNameText").objectReferenceValue = winnerNameText;
            so.FindProperty("_winnerChipsText").objectReferenceValue = winnerChipsText;
            so.FindProperty("_rankingContainer").objectReferenceValue = content.transform;
            so.FindProperty("_rankingItemPrefab").objectReferenceValue = rankingItemTemplate;
            so.FindProperty("_eliminationText").objectReferenceValue = eliminationText;
            so.FindProperty("_returnToLobbyButton").objectReferenceValue = returnBtn;
            so.ApplyModifiedPropertiesWithoutUndo();

            // 초기 상태: 비활성
            panelRoot.SetActive(false);

            Selection.activeGameObject = panelRoot;
            Debug.Log("[ResultViewBuilder] Result View UI가 생성되었습니다.");
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }
    }
}
