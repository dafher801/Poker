// GameTableSceneBuilder.cs
// Unity 에디터 메뉴에서 게임 테이블 UI 씬 계층 구조와 PlayerSlot 프리팹을 자동 생성하는 에디터 스크립트.
// 메뉴: TexasHoldem > Build Game Table Scene, TexasHoldem > Create PlayerSlot Prefab

using UnityEditor;
using UnityEngine;
using TMPro;

namespace TexasHoldem.Editor
{
    public static class GameTableSceneBuilder
    {
        private const string PlayerSlotPrefabPath = "Assets/Prefabs/PlayerSlot.prefab";
        private const string SidePotPrefabPath = "Assets/Prefabs/SidePotItem.prefab";
        private const string ChipIconPrefabPath = "Assets/Prefabs/ChipIcon.prefab";

        private const string TableBackgroundSpritePath = "Assets/Resources/Sprites/table_background.png";
        private const string DealerButtonSpritePath = "Assets/Resources/Sprites/dealer_button.png";
        private const string ChipIconSpritePath = "Assets/Resources/Sprites/chip_icon.png";

        [MenuItem("TexasHoldem/Create PlayerSlot Prefab")]
        public static void CreatePlayerSlotPrefab()
        {
            EnsureDirectory("Assets/Prefabs");

            GameObject root = new GameObject("PlayerSlot");

            // 배경 패널
            GameObject bgPanel = CreateUIObject("BackgroundPanel", root.transform);
            SpriteRenderer bgRenderer = bgPanel.AddComponent<SpriteRenderer>();
            bgRenderer.color = new Color(0.2f, 0.2f, 0.2f, 0.6f);
            bgRenderer.sortingOrder = 0;

            // NameLabel (TMP)
            GameObject nameLabel = CreateTMPObject("NameLabel", root.transform, "Player", 24,
                new Vector3(0f, 0.5f, 0f));

            // ChipStackLabel (TMP)
            GameObject chipStackLabel = CreateTMPObject("ChipStackLabel", root.transform, "0", 20,
                new Vector3(0f, 0.2f, 0f));

            // BetAmountLabel (TMP)
            GameObject betAmountLabel = CreateTMPObject("BetAmountLabel", root.transform, "", 18,
                new Vector3(0f, -0.8f, 0f));
            betAmountLabel.SetActive(false);

            // DealerButton
            GameObject dealerButton = new GameObject("DealerButton");
            dealerButton.transform.SetParent(root.transform, false);
            dealerButton.transform.localPosition = new Vector3(0.8f, 0.5f, 0f);
            SpriteRenderer dealerRenderer = dealerButton.AddComponent<SpriteRenderer>();
            dealerRenderer.sortingOrder = 5;
            dealerRenderer.sprite = LoadSprite(DealerButtonSpritePath);
            dealerButton.SetActive(false);

            // FoldOverlay (반투명 회색)
            GameObject foldOverlay = new GameObject("FoldOverlay");
            foldOverlay.transform.SetParent(root.transform, false);
            SpriteRenderer foldRenderer = foldOverlay.AddComponent<SpriteRenderer>();
            foldRenderer.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            foldRenderer.sortingOrder = 10;
            foldOverlay.SetActive(false);

            // AllInBadge
            GameObject allInBadge = CreateTMPObject("AllInBadge", root.transform, "ALL IN", 16,
                new Vector3(0f, -0.2f, 0f));
            allInBadge.SetActive(false);

            // HoleCard0
            GameObject holeCard0 = CreateCardViewObject("HoleCard0", root.transform,
                new Vector3(-0.3f, -0.4f, 0f));

            // HoleCard1
            GameObject holeCard1 = CreateCardViewObject("HoleCard1", root.transform,
                new Vector3(0.3f, -0.4f, 0f));

            // PlayerSlotView 컴포넌트 추가 및 참조 연결
            View.PlayerSlotView slotView = root.AddComponent<View.PlayerSlotView>();
            SerializedObject so = new SerializedObject(slotView);
            so.FindProperty("_nameLabel").objectReferenceValue = nameLabel.GetComponent<TextMeshPro>();
            so.FindProperty("_chipStackLabel").objectReferenceValue = chipStackLabel.GetComponent<TextMeshPro>();
            so.FindProperty("_betAmountLabel").objectReferenceValue = betAmountLabel.GetComponent<TextMeshPro>();
            so.FindProperty("_dealerButtonIcon").objectReferenceValue = dealerButton;
            so.FindProperty("_foldOverlay").objectReferenceValue = foldOverlay;
            so.FindProperty("_allInBadge").objectReferenceValue = allInBadge;

            SerializedProperty holeCardsProp = so.FindProperty("_holeCards");
            holeCardsProp.arraySize = 2;
            holeCardsProp.GetArrayElementAtIndex(0).objectReferenceValue = holeCard0.GetComponent<View.CardView>();
            holeCardsProp.GetArrayElementAtIndex(1).objectReferenceValue = holeCard1.GetComponent<View.CardView>();
            so.ApplyModifiedPropertiesWithoutUndo();

            // 프리팹 저장
            PrefabUtility.SaveAsPrefabAsset(root, PlayerSlotPrefabPath);
            Object.DestroyImmediate(root);

            Debug.Log($"[GameTableSceneBuilder] PlayerSlot 프리팹이 생성되었습니다: {PlayerSlotPrefabPath}");
        }

        [MenuItem("TexasHoldem/Create SidePot Prefab")]
        public static void CreateSidePotPrefab()
        {
            EnsureDirectory("Assets/Prefabs");

            GameObject root = new GameObject("SidePotItem");

            // SidePot 라벨
            TextMeshPro tmp = root.AddComponent<TextMeshPro>();
            tmp.text = "Side Pot: 0";
            tmp.fontSize = 20;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.sortingOrder = 5;

            PrefabUtility.SaveAsPrefabAsset(root, SidePotPrefabPath);
            Object.DestroyImmediate(root);

            Debug.Log($"[GameTableSceneBuilder] SidePotItem 프리팹이 생성되었습니다: {SidePotPrefabPath}");
        }

        [MenuItem("TexasHoldem/Create ChipIcon Prefab")]
        public static void CreateChipIconPrefab()
        {
            EnsureDirectory("Assets/Prefabs");

            GameObject root = new GameObject("ChipIcon");
            SpriteRenderer sr = root.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 20;
            sr.sprite = LoadSprite(ChipIconSpritePath);

            PrefabUtility.SaveAsPrefabAsset(root, ChipIconPrefabPath);
            Object.DestroyImmediate(root);

            Debug.Log($"[GameTableSceneBuilder] ChipIcon 프리팹이 생성되었습니다: {ChipIconPrefabPath}");
        }

        [MenuItem("TexasHoldem/Build Game Table Scene")]
        public static void BuildGameTableScene()
        {
            // 프리팹이 없으면 먼저 생성
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(PlayerSlotPrefabPath))
            {
                CreatePlayerSlotPrefab();
            }
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(SidePotPrefabPath))
            {
                CreateSidePotPrefab();
            }
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(ChipIconPrefabPath))
            {
                CreateChipIconPrefab();
            }

            // ── 루트: GameTableView ──
            GameObject gameTableRoot = new GameObject("GameTableView");
            Undo.RegisterCreatedObjectUndo(gameTableRoot, "Build Game Table Scene");

            // ── TableBackground ──
            GameObject tableBackground = new GameObject("TableBackground");
            tableBackground.transform.SetParent(gameTableRoot.transform, false);
            SpriteRenderer tableBgRenderer = tableBackground.AddComponent<SpriteRenderer>();
            tableBgRenderer.sprite = LoadSprite(TableBackgroundSpritePath);
            tableBgRenderer.sortingOrder = -10;

            // ── TableLayoutManager (테이블 중앙에 빈 오브젝트) ──
            GameObject tableCenterObj = new GameObject("TableCenter");
            tableCenterObj.transform.SetParent(gameTableRoot.transform, false);

            View.TableLayoutManager layoutManager = gameTableRoot.AddComponent<View.TableLayoutManager>();
            SerializedObject layoutSo = new SerializedObject(layoutManager);
            layoutSo.FindProperty("_tableCenter").objectReferenceValue = tableCenterObj.transform;
            layoutSo.FindProperty("_radiusX").floatValue = 5f;
            layoutSo.FindProperty("_radiusY").floatValue = 2.5f;
            layoutSo.FindProperty("_maxSeats").intValue = 10;
            layoutSo.ApplyModifiedPropertiesWithoutUndo();

            // ── PlayerSlotContainer (10개 슬롯 배치 부모) ──
            GameObject playerSlotContainer = new GameObject("PlayerSlotContainer");
            playerSlotContainer.transform.SetParent(gameTableRoot.transform, false);

            // PlayerSlot 프리팹 로드 및 10개 인스턴스 배치
            GameObject slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerSlotPrefabPath);
            View.PlayerSlotView[] playerSlots = new View.PlayerSlotView[10];
            for (int i = 0; i < 10; i++)
            {
                Vector3 pos = layoutManager.GetSeatPosition(i);
                GameObject slotInstance = (GameObject)PrefabUtility.InstantiatePrefab(slotPrefab, playerSlotContainer.transform);
                slotInstance.name = $"PlayerSlot_{i}";
                slotInstance.transform.position = pos;
                playerSlots[i] = slotInstance.GetComponent<View.PlayerSlotView>();
            }

            // ── CommunityCardsArea ──
            GameObject communityCardsArea = new GameObject("CommunityCardsArea");
            communityCardsArea.transform.SetParent(gameTableRoot.transform, false);

            View.CommunityCardsView communityCardsView = communityCardsArea.AddComponent<View.CommunityCardsView>();

            // 5개 카드 슬롯 위치 + CardView
            View.CardView[] communityCardViews = new View.CardView[5];
            Transform[] cardSlotPositions = new Transform[5];
            float cardSpacing = 1.2f;
            float startX = -2.4f;

            for (int i = 0; i < 5; i++)
            {
                // 슬롯 위치 마커
                GameObject slotPos = new GameObject($"CardSlot_{i}");
                slotPos.transform.SetParent(communityCardsArea.transform, false);
                slotPos.transform.localPosition = new Vector3(startX + i * cardSpacing, 0f, 0f);
                cardSlotPositions[i] = slotPos.transform;

                // CardView
                GameObject cardObj = CreateCardViewObject($"CommunityCard_{i}", communityCardsArea.transform,
                    new Vector3(startX + i * cardSpacing, 0f, 0f));
                cardObj.SetActive(false);
                communityCardViews[i] = cardObj.GetComponent<View.CardView>();
            }

            // CommunityCardsView 참조 연결
            SerializedObject commSo = new SerializedObject(communityCardsView);
            SerializedProperty commCardsProp = commSo.FindProperty("_communityCards");
            commCardsProp.arraySize = 5;
            SerializedProperty commSlotsProp = commSo.FindProperty("_cardSlotPositions");
            commSlotsProp.arraySize = 5;
            for (int i = 0; i < 5; i++)
            {
                commCardsProp.GetArrayElementAtIndex(i).objectReferenceValue = communityCardViews[i];
                commSlotsProp.GetArrayElementAtIndex(i).objectReferenceValue = cardSlotPositions[i];
            }
            commSo.ApplyModifiedPropertiesWithoutUndo();

            // ── PotDisplay (중앙 상단) ──
            GameObject potDisplayObj = new GameObject("PotDisplay");
            potDisplayObj.transform.SetParent(gameTableRoot.transform, false);
            potDisplayObj.transform.localPosition = new Vector3(0f, 2f, 0f);

            View.PotDisplayView potDisplayView = potDisplayObj.AddComponent<View.PotDisplayView>();

            // MainPotLabel
            GameObject mainPotLabel = new GameObject("MainPotLabel");
            mainPotLabel.transform.SetParent(potDisplayObj.transform, false);
            TextMeshPro mainPotTmp = mainPotLabel.AddComponent<TextMeshPro>();
            mainPotTmp.text = "Pot: 0";
            mainPotTmp.fontSize = 28;
            mainPotTmp.alignment = TextAlignmentOptions.Center;
            mainPotTmp.sortingOrder = 5;
            mainPotLabel.SetActive(false);

            // SidePotContainer
            GameObject sidePotContainer = new GameObject("SidePotContainer");
            sidePotContainer.transform.SetParent(potDisplayObj.transform, false);
            sidePotContainer.transform.localPosition = new Vector3(0f, -0.5f, 0f);

            // PotDisplayView 참조 연결
            GameObject sidePotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SidePotPrefabPath);
            SerializedObject potSo = new SerializedObject(potDisplayView);
            potSo.FindProperty("_mainPotLabel").objectReferenceValue = mainPotTmp;
            potSo.FindProperty("_sidePotContainer").objectReferenceValue = sidePotContainer.transform;
            potSo.FindProperty("_sidePotPrefab").objectReferenceValue = sidePotPrefab;
            potSo.ApplyModifiedPropertiesWithoutUndo();

            // ── HoleCardsView ──
            GameObject holeCardsObj = new GameObject("HoleCardsView");
            holeCardsObj.transform.SetParent(gameTableRoot.transform, false);
            View.HoleCardsView holeCardsView = holeCardsObj.AddComponent<View.HoleCardsView>();

            // ── GameTableView 컴포넌트 추가 및 참조 연결 ──
            View.GameTableView gameTableView = gameTableRoot.AddComponent<View.GameTableView>();

            GameObject chipIconPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ChipIconPrefabPath);

            SerializedObject tableSo = new SerializedObject(gameTableView);
            tableSo.FindProperty("_layoutManager").objectReferenceValue = layoutManager;
            tableSo.FindProperty("_communityCards").objectReferenceValue = communityCardsView;
            tableSo.FindProperty("_holeCards").objectReferenceValue = holeCardsView;
            tableSo.FindProperty("_potDisplay").objectReferenceValue = potDisplayView;
            tableSo.FindProperty("_chipIconPrefab").objectReferenceValue = chipIconPrefab;

            SerializedProperty slotsProp = tableSo.FindProperty("_playerSlots");
            slotsProp.arraySize = 10;
            for (int i = 0; i < 10; i++)
            {
                slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = playerSlots[i];
            }
            tableSo.ApplyModifiedPropertiesWithoutUndo();

            // 선택하여 확인
            Selection.activeGameObject = gameTableRoot;
            Debug.Log("[GameTableSceneBuilder] 게임 테이블 씬 계층 구조가 생성되었습니다.");
        }

        private static GameObject CreateCardViewObject(string name, Transform parent, Vector3 localPos)
        {
            GameObject cardObj = new GameObject(name);
            cardObj.transform.SetParent(parent, false);
            cardObj.transform.localPosition = localPos;

            // Front Renderer
            GameObject frontObj = new GameObject("Front");
            frontObj.transform.SetParent(cardObj.transform, false);
            SpriteRenderer frontRenderer = frontObj.AddComponent<SpriteRenderer>();
            frontRenderer.sortingOrder = 3;
            frontRenderer.enabled = false;

            // Back Renderer
            GameObject backObj = new GameObject("Back");
            backObj.transform.SetParent(cardObj.transform, false);
            SpriteRenderer backRenderer = backObj.AddComponent<SpriteRenderer>();
            backRenderer.sortingOrder = 3;
            backRenderer.enabled = false;

            // CardView 컴포넌트
            View.CardView cardView = cardObj.AddComponent<View.CardView>();
            SerializedObject so = new SerializedObject(cardView);
            so.FindProperty("_frontRenderer").objectReferenceValue = frontRenderer;
            so.FindProperty("_backRenderer").objectReferenceValue = backRenderer;

            // CardSpriteProvider 자동 탐색
            string[] guids = AssetDatabase.FindAssets("t:CardSpriteProvider");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                View.CardSpriteProvider provider = AssetDatabase.LoadAssetAtPath<View.CardSpriteProvider>(path);
                so.FindProperty("_spriteProvider").objectReferenceValue = provider;
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            return cardObj;
        }

        private static GameObject CreateTMPObject(string name, Transform parent, string text, float fontSize, Vector3 localPos)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;

            TextMeshPro tmp = obj.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.sortingOrder = 5;

            return obj;
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private static Sprite LoadSprite(string assetPath)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                Debug.LogWarning($"[GameTableSceneBuilder] 스프라이트를 찾을 수 없습니다: {assetPath}");
            }
            return sprite;
        }

        private static void EnsureDirectory(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string[] parts = path.Split('/');
                string current = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string next = current + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next))
                    {
                        AssetDatabase.CreateFolder(current, parts[i]);
                    }
                    current = next;
                }
            }
        }
    }
}
