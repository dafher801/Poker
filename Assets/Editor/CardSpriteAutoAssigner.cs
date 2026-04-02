#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public static class CardSpriteAutoAssigner
{
    [MenuItem("Tools/Assign Card Fronts")]
    public static void AssignCardFronts()
    {
        const string providerPath = "Assets/Resources/Data/CardSpriteProvider.asset";
        const string spriteFolderPath = "Assets/Resources/Sprites/Cards/png/face/";

        var provider = AssetDatabase.LoadAssetAtPath<ScriptableObject>(providerPath);
        if (provider == null)
        {
            Debug.LogError($"CardSpriteProvider not found at {providerPath}");
            return;
        }

        var so = new SerializedObject(provider);
        var cardFrontsProp = so.FindProperty("_cardFronts");
        if (cardFrontsProp == null)
        {
            Debug.LogError("_cardFronts field not found in CardSpriteProvider");
            return;
        }

        string[] suits = { "S", "H", "D", "C" };
        string[] ranks = { "A", "2", "3", "4", "5", "6", "7", "8", "9", "T", "J", "Q", "K" };

        var sprites = new List<Sprite>();

        foreach (var suit in suits)
        {
            foreach (var rank in ranks)
            {
                string assetName = $"{rank}{suit}@1x";
                string assetPath = $"{spriteFolderPath}{assetName}.png";
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);

                if (sprite == null)
                {
                    Debug.LogWarning($"Sprite not found: {assetPath}");
                }
                sprites.Add(sprite);
            }
        }

        cardFrontsProp.arraySize = sprites.Count;
        for (int i = 0; i < sprites.Count; i++)
        {
            cardFrontsProp.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(provider);
        AssetDatabase.SaveAssets();

        Debug.Log($"Done! {sprites.Count} sprites assigned. Check Console for any warnings.");
    }
}
#endif