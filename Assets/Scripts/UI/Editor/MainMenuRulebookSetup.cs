#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FourE.UI.Editor
{
    /// <summary>
    /// Crea il prefab del regolamento e lo collega alla scena del menu principale.
    /// </summary>
    public static class MainMenuRulebookSetup
    {
        private const string PrefabPath = "Assets/Prefabs/UI/RulebookPopup.prefab";
        private const string ScenePath = "Assets/Scenes/MainMenu.unity";
        private const string NoteSpritePath = "Assets/Background/Note.png";
        private const string FontPath = "Assets/Fonts/Handwritten.ttf";
        /// <summary>
        /// Genera il popup, aggiunge il pulsante Regolamento e salva la scena.
        /// </summary>
        [MenuItem("4E/Setup Main Menu Rulebook")]
        public static void Setup()
        {
            RulebookPopup popupPrefab = CreatePopupPrefab();
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Transform safeArea = FindSceneTransform(scene, "SafeAreaPanel");
            Transform modePanel = FindSceneTransform(scene, "ModePanel");
            MainMenuController controller = FindSceneComponent<MainMenuController>(scene);

            if (popupPrefab == null || safeArea == null || modePanel == null || controller == null)
            {
                Debug.LogError(
                    $"[4E] Main menu rulebook setup failed. Prefab: {popupPrefab != null}, "
                    + $"SafeArea: {safeArea != null}, ModePanel: {modePanel != null}, Controller: {controller != null}.");
                return;
            }

            Button rulesButton = GetOrCreateRulesButton(modePanel);
            RulebookPopup popup = GetOrCreatePopup(safeArea, popupPrefab);

            SerializedObject controllerObject = new(controller);
            controllerObject.FindProperty("_rulesButton").objectReferenceValue = rulesButton;
            controllerObject.FindProperty("_rulebookPopup").objectReferenceValue = popup;
            controllerObject.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[4E] Main menu rulebook created and connected.");
        }

        /// <summary>
        /// Crea il prefab visuale del regolamento.
        /// </summary>
        /// <returns>Componente del prefab appena salvato.</returns>
        private static RulebookPopup CreatePopupPrefab()
        {
            Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            Sprite noteSprite = AssetDatabase.LoadAssetAtPath<Sprite>(NoteSpritePath);

            GameObject root = CreateUiObject("RulebookPopup", null);
            Stretch(root.GetComponent<RectTransform>());
            Image overlay = root.AddComponent<Image>();
            overlay.color = new Color(0f, 0f, 0f, 0.72f);
            overlay.raycastTarget = true;

            GameObject note = CreateUiObject("Note", root.transform);
            RectTransform noteRect = note.GetComponent<RectTransform>();
            noteRect.anchorMin = new Vector2(0.5f, 0.5f);
            noteRect.anchorMax = new Vector2(0.5f, 0.5f);
            noteRect.sizeDelta = new Vector2(820f, 1040f);
            noteRect.anchoredPosition = Vector2.zero;
            Image noteImage = note.AddComponent<Image>();
            noteImage.sprite = noteSprite;
            noteImage.preserveAspect = true;
            noteImage.raycastTarget = true;

            Text title = CreateText("Title", note.transform, font, "REGOLAMENTO", 54, FontStyle.Bold);
            SetRect(title.rectTransform, new Vector2(0.15f, 0.82f), new Vector2(0.85f, 0.92f));

            Text section = CreateText("Section", note.transform, font, string.Empty, 38, FontStyle.Bold);
            SetRect(section.rectTransform, new Vector2(0.14f, 0.70f), new Vector2(0.86f, 0.81f));

            Text body = CreateText("Body", note.transform, font, string.Empty, 31, FontStyle.Normal);
            body.alignment = TextAnchor.UpperLeft;
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Truncate;
            body.lineSpacing = 1.08f;
            SetRect(body.rectTransform, new Vector2(0.16f, 0.23f), new Vector2(0.84f, 0.70f));

            Text page = CreateText("Page", note.transform, font, string.Empty, 28, FontStyle.Bold);
            SetRect(page.rectTransform, new Vector2(0.39f, 0.12f), new Vector2(0.61f, 0.20f));

            Button previous = CreateButton("PreviousButton", note.transform, font, "<",
                new Vector2(0.16f, 0.10f), new Vector2(0.34f, 0.21f));
            Button next = CreateButton("NextButton", note.transform, font, ">",
                new Vector2(0.66f, 0.10f), new Vector2(0.84f, 0.21f));
            Button close = CreateButton("CloseButton", note.transform, font, "X",
                new Vector2(0.79f, 0.82f), new Vector2(0.89f, 0.91f));

            RulebookPopup popup = root.AddComponent<RulebookPopup>();
            SerializedObject popupObject = new(popup);
            popupObject.FindProperty("_sectionLabel").objectReferenceValue = section;
            popupObject.FindProperty("_bodyLabel").objectReferenceValue = body;
            popupObject.FindProperty("_pageLabel").objectReferenceValue = page;
            popupObject.FindProperty("_previousButton").objectReferenceValue = previous;
            popupObject.FindProperty("_nextButton").objectReferenceValue = next;
            popupObject.FindProperty("_closeButton").objectReferenceValue = close;
            popupObject.ApplyModifiedPropertiesWithoutUndo();

            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            if (savedPrefab == null)
            {
                return null;
            }

            AssetDatabase.ImportAsset(PrefabPath, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<RulebookPopup>(PrefabPath);
        }

        /// <summary>
        /// Recupera il pulsante esistente o ne duplica uno coerente con il menu.
        /// </summary>
        private static Button GetOrCreateRulesButton(Transform modePanel)
        {
            Transform existing = modePanel.Find("RulesButton");
            if (existing != null)
            {
                return existing.GetComponent<Button>();
            }

            Transform source = modePanel.Find("OnlineButton");
            GameObject clone = Object.Instantiate(source.gameObject, modePanel);
            clone.name = "RulesButton";
            Text label = clone.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.text = "Regolamento";
            }

            return clone.GetComponent<Button>();
        }

        /// <summary>
        /// Recupera l'istanza del popup o la crea dal prefab.
        /// </summary>
        private static RulebookPopup GetOrCreatePopup(Transform safeArea, RulebookPopup popupPrefab)
        {
            Transform existing = safeArea.Find("RulebookPopup");
            if (existing != null)
            {
                return existing.GetComponent<RulebookPopup>();
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(popupPrefab.gameObject, safeArea) as GameObject;
            instance.name = "RulebookPopup";
            RectTransform rect = instance.GetComponent<RectTransform>();
            Stretch(rect);
            instance.SetActive(false);
            return instance.GetComponent<RulebookPopup>();
        }

        /// <summary>
        /// Cerca ricorsivamente un oggetto per nome nella scena.
        /// </summary>
        private static Transform FindSceneTransform(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform match = FindDescendant(root.transform, name);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        /// <summary>
        /// Cerca ricorsivamente un discendente per nome.
        /// </summary>
        private static Transform FindDescendant(Transform current, string name)
        {
            if (current.name == name)
            {
                return current;
            }

            foreach (Transform child in current)
            {
                Transform match = FindDescendant(child, name);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        /// <summary>
        /// Cerca un componente, inclusi gli oggetti inattivi, tra le radici della scena.
        /// </summary>
        private static T FindSceneComponent<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }

        /// <summary>
        /// Crea un oggetto UI con RectTransform.
        /// </summary>
        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject gameObject = new(name, typeof(RectTransform));
            gameObject.layer = LayerMask.NameToLayer("UI");
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        /// <summary>
        /// Crea un testo con lo stile manoscritto del progetto.
        /// </summary>
        private static Text CreateText(
            string name,
            Transform parent,
            Font font,
            string content,
            int fontSize,
            FontStyle fontStyle)
        {
            GameObject gameObject = CreateUiObject(name, parent);
            Text text = gameObject.AddComponent<Text>();
            text.font = font;
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = Color.black;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>
        /// Crea un pulsante trasparente con etichetta.
        /// </summary>
        private static Button CreateButton(
            string name,
            Transform parent,
            Font font,
            string label,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            GameObject gameObject = CreateUiObject(name, parent);
            Image image = gameObject.AddComponent<Image>();
            image.color = new Color(1f, 0.9f, 0.35f, 0.55f);
            Button button = gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            SetRect(gameObject.GetComponent<RectTransform>(), anchorMin, anchorMax);

            Text text = CreateText("Label", gameObject.transform, font, label, 42, FontStyle.Bold);
            Stretch(text.rectTransform);
            return button;
        }

        /// <summary>
        /// Imposta ancoraggi e azzera gli offset.
        /// </summary>
        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Estende un RectTransform su tutto il parent.
        /// </summary>
        private static void Stretch(RectTransform rect)
        {
            SetRect(rect, Vector2.zero, Vector2.one);
        }
    }
}
#endif
