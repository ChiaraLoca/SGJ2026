#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace FourE.UI.Editor
{
    /// <summary>
    /// Setup automatico della scena di test effetti.
    /// Crea la gerarchia completa con Canvas, UI controller e view comandanti.
    /// </summary>
    public static class EffectTestSceneSetup
    {
        [MenuItem("4E/Setup Scena Test Effetti")]
        public static void SetupEffectTestScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Canvas root
            var canvasGO = new GameObject("Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var canvasScaler = canvasGO.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1080, 1920);

            var graphicRaycaster = canvasGO.AddComponent<GraphicRaycaster>();

            // SafeAreaPanel (inner panel)
            var safeAreaGO = new GameObject("SafeAreaPanel");
            safeAreaGO.transform.SetParent(canvasGO.transform, false);
            var safeAreaRect = safeAreaGO.AddComponent<RectTransform>();
            safeAreaRect.anchorMin = Vector2.zero;
            safeAreaRect.anchorMax = Vector2.one;
            safeAreaRect.offsetMin = Vector2.zero;
            safeAreaRect.offsetMax = Vector2.zero;

            var safeAreaController = safeAreaGO.AddComponent<SafeAreaController>();
            safeAreaController.SetPanel(safeAreaRect);

            // --- TOP PANEL: Controls (dropdown, toggle, button)
            var topPanelGO = new GameObject("TopPanel");
            topPanelGO.transform.SetParent(safeAreaGO.transform, false);
            var topRect = topPanelGO.AddComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0, 1);
            topRect.anchorMax = new Vector2(1, 1);
            topRect.offsetMin = new Vector2(0, -200);
            topRect.offsetMax = new Vector2(0, 0);
            var layoutGroup = topPanelGO.AddComponent<VerticalLayoutGroup>();
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.spacing = 10;
            layoutGroup.padding = new RectOffset(10, 10, 10, 10);

            // Card Dropdown
            var cardDropdownGO = CreateDropdown("CardDropdown", topPanelGO.transform);
            var cardDropdown = cardDropdownGO.GetComponent<Dropdown>();

            // Player Dropdown
            var playerDropdownGO = CreateDropdown("PlayerDropdown", topPanelGO.transform);
            var playerDropdown = playerDropdownGO.GetComponent<Dropdown>();

            // Target Dropdown
            var targetDropdownGO = CreateDropdown("TargetDropdown", topPanelGO.transform);
            var targetDropdown = targetDropdownGO.GetComponent<Dropdown>();

            // Buttons row
            var buttonsRowGO = new GameObject("ButtonsRow");
            buttonsRowGO.transform.SetParent(topPanelGO.transform, false);
            var buttonsRowRect = buttonsRowGO.AddComponent<RectTransform>();
            var buttonsHorizontal = buttonsRowGO.AddComponent<HorizontalLayoutGroup>();
            buttonsHorizontal.childForceExpandWidth = true;
            buttonsHorizontal.spacing = 5;

            var applyButtonGO = CreateButton("ApplyButton", "Apply Effect", buttonsRowGO.transform);
            var applyButton = applyButtonGO.GetComponent<Button>();

            var resetButtonGO = CreateButton("ResetButton", "Reset", buttonsRowGO.transform);
            var resetButton = resetButtonGO.GetComponent<Button>();

            // Passive Toggle
            var passivesToggleGO = CreateToggle("PassivesToggle", "Passive abilitate", topPanelGO.transform);
            var passivesToggle = passivesToggleGO.GetComponent<Toggle>();

            // --- MIDDLE PANEL: Player States (2 columns)
            var middlePanelGO = new GameObject("MiddlePanel");
            middlePanelGO.transform.SetParent(safeAreaGO.transform, false);
            var middleRect = middlePanelGO.AddComponent<RectTransform>();
            middleRect.anchorMin = new Vector2(0, 0.2f);
            middleRect.anchorMax = new Vector2(1, 1);
            middleRect.offsetMin = Vector2.zero;
            middleRect.offsetMax = Vector2.zero;
            var gridLayout = middlePanelGO.AddComponent<GridLayoutGroup>();
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 2;
            gridLayout.cellSize = new Vector2(500, 300);
            gridLayout.spacing = new Vector2(10, 10);
            gridLayout.padding = new RectOffset(10, 10, 10, 10);

            // Player 0 - Commander 0
            var p0c0GO = new GameObject("Player0_Commander0");
            p0c0GO.transform.SetParent(middlePanelGO.transform, false);
            var p0c0Rect = p0c0GO.AddComponent<RectTransform>();
            p0c0Rect.sizeDelta = new Vector2(500, 300);
            var p0c0View = p0c0GO.AddComponent<CommanderTestView>();

            // Player 0 - Commander 1
            var p0c1GO = new GameObject("Player0_Commander1");
            p0c1GO.transform.SetParent(middlePanelGO.transform, false);
            var p0c1Rect = p0c1GO.AddComponent<RectTransform>();
            p0c1Rect.sizeDelta = new Vector2(500, 300);
            var p0c1View = p0c1GO.AddComponent<CommanderTestView>();

            // Player 1 - Commander 0
            var p1c0GO = new GameObject("Player1_Commander0");
            p1c0GO.transform.SetParent(middlePanelGO.transform, false);
            var p1c0Rect = p1c0GO.AddComponent<RectTransform>();
            p1c0Rect.sizeDelta = new Vector2(500, 300);
            var p1c0View = p1c0GO.AddComponent<CommanderTestView>();

            // Player 1 - Commander 1
            var p1c1GO = new GameObject("Player1_Commander1");
            p1c1GO.transform.SetParent(middlePanelGO.transform, false);
            var p1c1Rect = p1c1GO.AddComponent<RectTransform>();
            p1c1Rect.sizeDelta = new Vector2(500, 300);
            var p1c1View = p1c1GO.AddComponent<CommanderTestView>();

            // Creazione testi per le view
            CreateCommanderViewTexts(p0c0GO);
            CreateCommanderViewTexts(p0c1GO);
            CreateCommanderViewTexts(p1c0GO);
            CreateCommanderViewTexts(p1c1GO);

            // --- BOTTOM PANEL: Log
            var bottomPanelGO = new GameObject("BottomPanel");
            bottomPanelGO.transform.SetParent(safeAreaGO.transform, false);
            var bottomRect = bottomPanelGO.AddComponent<RectTransform>();
            bottomRect.anchorMin = new Vector2(0, 0);
            bottomRect.anchorMax = new Vector2(1, 0.2f);
            bottomRect.offsetMin = Vector2.zero;
            bottomRect.offsetMax = Vector2.zero;

            var logBG = bottomPanelGO.AddComponent<Image>();
            logBG.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            var logTextGO = new GameObject("LogText");
            logTextGO.transform.SetParent(bottomPanelGO.transform, false);
            var logTextRect = logTextGO.AddComponent<RectTransform>();
            logTextRect.anchorMin = Vector2.zero;
            logTextRect.anchorMax = Vector2.one;
            logTextRect.offsetMin = new Vector2(10, 10);
            logTextRect.offsetMax = new Vector2(-10, -10);
            var logText = logTextGO.AddComponent<Text>();
            logText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            logText.text = "Log output here...";
            logText.alignment = TextAnchor.UpperLeft;
            logText.fontSize = 14;
            logText.color = Color.white;

            // Carica GameConfig e GameContent per la scena di test
            var gameConfigPath = "Assets/ScriptableObjects/Config/GameConfig.asset";
            var gameContentPath = "Assets/ScriptableObjects/Config/GameContent.asset";
            var gameConfig = AssetDatabase.LoadAssetAtPath<FourE.Config.GameConfigSO>(gameConfigPath);
            var gameContent = AssetDatabase.LoadAssetAtPath<FourE.Config.GameContentSO>(gameContentPath);

            if (gameConfig == null || gameContent == null)
            {
                Debug.LogError("GameConfig o GameContent non trovati. Assicurati che gli asset esistano.");
                return;
            }

            // --- Manager
            var managerGO = new GameObject("EffectTestManager");
            managerGO.transform.SetParent(canvasGO.transform, false);
            var manager = managerGO.AddComponent<EffectTestSceneManager>();

            // --- UI Controller
            var uiControllerGO = new GameObject("UIController");
            uiControllerGO.transform.SetParent(canvasGO.transform, false);
            var uiController = uiControllerGO.AddComponent<EffectTestUIController>();

            // Setup references via public methods
            manager.SetCommanderViews(new[] { p0c0View, p0c1View, p1c0View, p1c1View });
            manager.SetUIController(uiController);
            uiController.SetUIReferences(cardDropdown, playerDropdown, targetDropdown, passivesToggle, applyButton, resetButton, logText);

            // Save scene
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/EffectTest.unity");
            EditorSceneManager.OpenScene("Assets/Scenes/EffectTest.unity");

            Debug.Log("Scena EffectTest creata con successo!");
        }

        private static GameObject CreateDropdown(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 40);

            var dropdown = go.AddComponent<Dropdown>();
            dropdown.targetGraphic = go.AddComponent<Image>();
            dropdown.captionText = CreateChildText(go, "Label");
            dropdown.itemText = CreateChildText(go, "Item");

            var scrollViewGO = new GameObject("Template");
            scrollViewGO.transform.SetParent(go.transform, false);
            scrollViewGO.AddComponent<CanvasGroup>();

            return go;
        }

        private static GameObject CreateButton(string name, string label, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 40);

            var image = go.AddComponent<Image>();
            image.color = new Color(0.2f, 0.6f, 1f, 1f);

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(go.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var text = textGO.AddComponent<Text>();
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;

            return go;
        }

        private static GameObject CreateToggle(string name, string label, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 40);

            var toggle = go.AddComponent<Toggle>();

            var checkmarkGO = new GameObject("Checkmark");
            checkmarkGO.transform.SetParent(go.transform, false);
            var checkmarkImage = checkmarkGO.AddComponent<Image>();
            checkmarkImage.color = Color.white;
            toggle.graphic = checkmarkImage;

            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            var labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.2f, 0);
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var labelText = labelGO.AddComponent<Text>();
            labelText.text = label;
            labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.color = Color.white;

            return go;
        }

        private static Text CreateChildText(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.color = Color.white;
            return text;
        }

        private static void CreateCommanderViewTexts(GameObject parent)
        {
            var view = parent.GetComponent<CommanderTestView>();

            // Name
            var nameGO = new GameObject("NameText");
            nameGO.transform.SetParent(parent.transform, false);
            var nameRect = nameGO.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 1);
            nameRect.anchorMax = new Vector2(1, 1);
            nameRect.offsetMin = new Vector2(5, -30);
            nameRect.offsetMax = new Vector2(-5, 0);
            var nameText = nameGO.AddComponent<Text>();
            nameText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            nameText.text = "Commander";
            nameText.alignment = TextAnchor.MiddleCenter;
            nameText.color = Color.white;
            nameText.fontSize = 20;

            // Notes
            var notesGO = new GameObject("NotesText");
            notesGO.transform.SetParent(parent.transform, false);
            var notesRect = notesGO.AddComponent<RectTransform>();
            notesRect.anchorMin = new Vector2(0, 0.7f);
            notesRect.anchorMax = new Vector2(1, 0.85f);
            notesRect.offsetMin = new Vector2(5, 0);
            notesRect.offsetMax = new Vector2(-5, 0);
            var notesText = notesGO.AddComponent<Text>();
            notesText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            notesText.text = "Note: 0";
            notesText.alignment = TextAnchor.MiddleCenter;
            notesText.color = Color.yellow;

            // Debuff
            var debuffGO = new GameObject("DebuffText");
            debuffGO.transform.SetParent(parent.transform, false);
            var debuffRect = debuffGO.AddComponent<RectTransform>();
            debuffRect.anchorMin = new Vector2(0, 0.45f);
            debuffRect.anchorMax = new Vector2(1, 0.60f);
            debuffRect.offsetMin = new Vector2(5, 0);
            debuffRect.offsetMax = new Vector2(-5, 0);
            var debuffText = debuffGO.AddComponent<Text>();
            debuffText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            debuffText.text = "Debuff: Nessuno";
            debuffText.alignment = TextAnchor.MiddleCenter;
            debuffText.color = Color.green;
            debuffText.fontSize = 12;

            // Buff
            var buffGO = new GameObject("BuffText");
            buffGO.transform.SetParent(parent.transform, false);
            var buffRect = buffGO.AddComponent<RectTransform>();
            buffRect.anchorMin = new Vector2(0, 0.25f);
            buffRect.anchorMax = new Vector2(1, 0.40f);
            buffRect.offsetMin = new Vector2(5, 0);
            buffRect.offsetMax = new Vector2(-5, 0);
            var buffText = buffGO.AddComponent<Text>();
            buffText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            buffText.text = "Buff: Nessuno";
            buffText.alignment = TextAnchor.MiddleCenter;
            buffText.color = Color.gray;
            buffText.fontSize = 12;

            // Secondary
            var secondaryGO = new GameObject("SecondaryText");
            secondaryGO.transform.SetParent(parent.transform, false);
            var secondaryRect = secondaryGO.AddComponent<RectTransform>();
            secondaryRect.anchorMin = new Vector2(0, 0.05f);
            secondaryRect.anchorMax = new Vector2(1, 0.20f);
            secondaryRect.offsetMin = new Vector2(5, 0);
            secondaryRect.offsetMax = new Vector2(-5, 0);
            var secondaryText = secondaryGO.AddComponent<Text>();
            secondaryText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            secondaryText.text = "✗ Passiva Secondaria";
            secondaryText.alignment = TextAnchor.MiddleCenter;
            secondaryText.color = Color.gray;
            secondaryText.fontSize = 12;

            // Assign to view via public method
            view.SetTextReferences(nameText, notesText, debuffText, buffText, secondaryText);
        }
    }
}
#endif
