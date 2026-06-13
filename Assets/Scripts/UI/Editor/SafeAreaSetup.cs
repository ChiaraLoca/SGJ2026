using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace FourE.UI.Editor
{
    /// <summary>
    /// Strumento editor che installa la Safe Area sul Canvas principale della scena attiva.
    /// Crea un SafeAreaPanel figlio del Canvas, vi sposta tutti i figli esistenti e
    /// aggiunge il componente <see cref="SafeAreaController"/> al Canvas con il pannello collegato.
    /// Se il pannello esiste già, non modifica nulla.
    /// </summary>
    internal static class SafeAreaSetup
    {
        private const string SafeAreaPanelName = "SafeAreaPanel";

        [MenuItem("4E/Setup Safe Area nella scena attiva")]
        private static void Run()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Safe Area Setup", "Nessun Canvas trovato nella scena.", "OK");
                return;
            }

            // Cerca il Canvas root (radice, non figlio di un altro Canvas).
            Canvas[] allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (Canvas c in allCanvases)
            {
                if (c.isRootCanvas) { canvas = c; break; }
            }

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();

            // Se il SafeAreaPanel esiste già non fare nulla.
            Transform existing = canvasRect.Find(SafeAreaPanelName);
            if (existing != null)
            {
                EditorUtility.DisplayDialog("Safe Area Setup",
                    $"'{SafeAreaPanelName}' esiste già su '{canvas.name}'. Nessuna modifica.", "OK");
                return;
            }

            Undo.SetCurrentGroupName("Setup Safe Area");
            int undoGroup = Undo.GetCurrentGroup();

            // Crea il pannello.
            GameObject panelGO = new GameObject(SafeAreaPanelName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(panelGO, "Crea SafeAreaPanel");
            panelGO.layer = canvas.gameObject.layer;

            RectTransform panelRect = panelGO.GetComponent<RectTransform>();
            Undo.SetTransformParent(panelRect, canvasRect, "Aggiungi SafeAreaPanel al Canvas");
            panelRect.SetAsFirstSibling();
            StretchFull(panelRect);

            // Sposta tutti i figli pre-esistenti dentro il pannello (in ordine).
            // Raccoglili prima del ciclo perché il parent cambierà durante lo spostamento.
            Transform[] children = new Transform[canvasRect.childCount];
            for (int i = 0; i < canvasRect.childCount; i++)
                children[i] = canvasRect.GetChild(i);

            foreach (Transform child in children)
            {
                if (child == panelRect) continue;
                Undo.SetTransformParent(child, panelRect, "Sposta in SafeAreaPanel");
            }

            // Aggiunge SafeAreaController al Canvas (se non già presente).
            SafeAreaController controller = canvas.GetComponent<SafeAreaController>();
            if (controller == null)
                controller = Undo.AddComponent<SafeAreaController>(canvas.gameObject);

            // Collega il pannello tramite SerializedObject per rispettare Undo/serializzazione.
            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("_panel").objectReferenceValue = panelRect;
            so.ApplyModifiedProperties();

            Undo.CollapseUndoOperations(undoGroup);

            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            EditorUtility.DisplayDialog("Safe Area Setup",
                $"Fatto!\n\nCanvas: '{canvas.name}'\nPannello: '{SafeAreaPanelName}'\n\nSalva la scena per mantenere le modifiche (Ctrl+S).", "OK");
        }

        /// <summary>
        /// Imposta il RectTransform a stretch completo (anchor 0,0 → 1,1, offset 0).
        /// </summary>
        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
