#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using FourE.UI;

namespace FourE.UI.Editor
{
    /// <summary>
    /// Crea il prefab <c>GameOutcomePanel</c> in <c>Assets/Prefabs/UI/</c>.
    /// Dopo la compilazione, eseguire dal menu <b>4E → Crea prefab GameOutcomePanel</b>.
    /// </summary>
    public static class GameOutcomePanelSetup
    {
        private const string PrefabPath = "Assets/Prefabs/UI/GameOutcomePanel.prefab";

        [MenuItem("4E/Crea prefab GameOutcomePanel")]
        public static void CreatePrefab()
        {
            // --- Overlay: sfondo semi-trasparente full-screen ---
            GameObject root = new GameObject("GameOutcomePanel");
            RectTransform rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Image overlay = root.AddComponent<Image>();
            overlay.color = new Color(0f, 0f, 0f, 0.78f);
            overlay.raycastTarget = true;

            // --- Box centrale ---
            GameObject box = CreateChild("Box", root.transform);
            RectTransform boxRect = box.GetComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.08f, 0.28f);
            boxRect.anchorMax = new Vector2(0.92f, 0.72f);
            boxRect.offsetMin = Vector2.zero;
            boxRect.offsetMax = Vector2.zero;

            Image boxBg = box.AddComponent<Image>();
            boxBg.color = new Color(0.12f, 0.14f, 0.18f, 1f);

            // --- Titolo ---
            Text titleText = CreateText("TitleLabel", box.transform,
                new Vector2(0.05f, 0.58f), new Vector2(0.95f, 0.92f),
                "VITTORIA!", 52, FontStyle.Bold, Color.white);

            // --- Sottotitolo ---
            Text subtitleText = CreateText("SubtitleLabel", box.transform,
                new Vector2(0.05f, 0.38f), new Vector2(0.95f, 0.60f),
                "Ottimo lavoro!", 30, FontStyle.Normal, new Color(0.8f, 0.8f, 0.8f, 1f));

            // --- Pulsante "Torna al menu" ---
            GameObject btnGo = CreateChild("MainMenuButton", box.transform);
            RectTransform btnRect = btnGo.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.18f, 0.06f);
            btnRect.anchorMax = new Vector2(0.82f, 0.30f);
            btnRect.offsetMin = Vector2.zero;
            btnRect.offsetMax = Vector2.zero;

            Image btnBg = btnGo.AddComponent<Image>();
            btnBg.color = new Color(0.18f, 0.48f, 0.88f, 1f);

            Button btn = btnGo.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = new Color(0.18f, 0.48f, 0.88f, 1f);
            cb.highlightedColor = new Color(0.28f, 0.60f, 1f, 1f);
            cb.pressedColor = new Color(0.12f, 0.36f, 0.72f, 1f);
            btn.colors = cb;
            btn.targetGraphic = btnBg;

            CreateText("Label", btnGo.transform,
                Vector2.zero, Vector2.one,
                "Torna al menu", 26, FontStyle.Bold, Color.white);

            // --- Componente GameOutcomePanel con riferimenti ---
            GameOutcomePanel panel = root.AddComponent<GameOutcomePanel>();
            SerializedObject so = new SerializedObject(panel);
            so.FindProperty("_titleLabel").objectReferenceValue = titleText;
            so.FindProperty("_subtitleLabel").objectReferenceValue = subtitleText;
            so.FindProperty("_mainMenuButton").objectReferenceValue = btn;
            so.ApplyModifiedPropertiesWithoutUndo();

            // --- Salva come prefab ---
            bool saved;
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out saved);
            Object.DestroyImmediate(root);

            if (saved)
            {
                AssetDatabase.Refresh();
                Debug.Log($"[4E] Prefab creato: {PrefabPath}");
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(PrefabPath));
            }
            else
            {
                Debug.LogError("[4E] Creazione prefab GameOutcomePanel fallita.");
            }
        }

        private static GameObject CreateChild(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private static Text CreateText(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            string content,
            int fontSize,
            FontStyle style,
            Color color)
        {
            GameObject go = CreateChild(name, parent);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Text text = go.AddComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.resizeTextForBestFit = false;
            return text;
        }
    }
}
#endif
