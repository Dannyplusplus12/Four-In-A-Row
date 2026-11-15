using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class GameModeSelectionUITool
{
    private const string PanelName = "GameModeSelectionPanel";
    private const string ItemPrefabName = "ModeItemPrefab";
    private const string ScrollViewName = "ModeScrollView";
    private const string ContentName = "Content";

    [MenuItem("Tools/Connect Four/Game Mode/Create Mode Item And Scroll View")]
    private static void CreateModeSelectionUI()
    {
        Transform panel = FindSelectedPanel(Selection.activeGameObject == null ? null : Selection.activeGameObject.transform);
        if (panel == null)
            panel = FindScenePanel();

        if (panel == null)
        {
            EditorUtility.DisplayDialog(
                "Game Mode Selection UI",
                "Could not find GameModeSelectionPanel in the open scene. Select the panel or add GameModeSelectionUI to it.",
                "OK");
            return;
        }

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Create Game Mode Selection UI");

        try
        {
            CreateModeItemPrefab(panel);
            Transform scrollView = CreateScrollView(panel);
            Transform content = scrollView.Find("Viewport/Content");
            GameModeSelectionUI selectionUI = panel.GetComponent<GameModeSelectionUI>();

            if (selectionUI != null)
            {
                SerializedObject serializedSelectionUI = new SerializedObject(selectionUI);
                serializedSelectionUI.FindProperty("modeItemContainer").objectReferenceValue = content;
                serializedSelectionUI.ApplyModifiedProperties();
                EditorUtility.SetDirty(selectionUI);
            }

            Selection.activeGameObject = scrollView.gameObject;
            EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
            Debug.Log("GameModeSelectionUITool: created ModeItemPrefab and a scroll view with a vertical content layout.");
        }
        finally
        {
            Undo.CollapseUndoOperations(undoGroup);
        }
    }

    private static Transform FindSelectedPanel(Transform selected)
    {
        Transform current = selected;
        while (current != null)
        {
            if (current.name == PanelName || current.GetComponent<GameModeSelectionUI>() != null)
                return current;
            current = current.parent;
        }

        return null;
    }

    private static Transform FindScenePanel()
    {
        GameModeSelectionUI[] selectionPanels = UnityEngine.Object.FindObjectsByType<GameModeSelectionUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (selectionPanels.Length > 0)
            return selectionPanels[0].transform;

        GameObject panelObject = GameObject.Find(PanelName);
        return panelObject == null ? null : panelObject.transform;
    }

    private static GameObject CreateModeItemPrefab(Transform panel)
    {
        Transform existing = panel.Find(ItemPrefabName);
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog(
                    "Game Mode Selection UI",
                    ItemPrefabName + " already exists. Replace it?",
                    "Replace",
                    "Cancel"))
                return existing.gameObject;

            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        GameObject item = new GameObject(ItemPrefabName, typeof(RectTransform), typeof(LayoutElement), typeof(Image), typeof(Button), typeof(GameModeItemUI));
        Undo.RegisterCreatedObjectUndo(item, "Create mode item prefab template");
        item.transform.SetParent(panel, false);
        item.SetActive(false);

        RectTransform itemRect = item.GetComponent<RectTransform>();
        itemRect.sizeDelta = new Vector2(0f, 120f);
        LayoutElement itemLayout = item.GetComponent<LayoutElement>();
        itemLayout.minHeight = 120f;
        itemLayout.preferredHeight = 120f;
        itemLayout.flexibleWidth = 1f;

        Image itemBackground = item.GetComponent<Image>();
        itemBackground.color = Color.white;

        Button button = item.GetComponent<Button>();
        button.targetGraphic = itemBackground;

        GameObject titleObject = CreateTextChild(item.transform, "Title", "Mode Title", 24, TextAlignmentOptions.Left);
        GameObject subtitleObject = CreateTextChild(item.transform, "Subtitle", "Rule description", 16, TextAlignmentOptions.Left);
        SetAnchors(titleObject.GetComponent<RectTransform>(), new Vector2(0.06f, 0.52f), new Vector2(0.68f, 0.9f), new Vector2(12f, 0f), new Vector2(-8f, 0f));
        SetAnchors(subtitleObject.GetComponent<RectTransform>(), new Vector2(0.06f, 0.1f), new Vector2(0.68f, 0.5f), new Vector2(12f, 0f), new Vector2(-8f, 0f));

        Image[] stars = new Image[3];
        for (int index = 0; index < stars.Length; index++)
        {
            GameObject starObject = new GameObject("DifficultyStar" + (index + 1), typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(starObject, "Create difficulty star");
            starObject.transform.SetParent(item.transform, false);
            RectTransform starRect = starObject.GetComponent<RectTransform>();
            starRect.anchorMin = new Vector2(0.72f + index * 0.08f, 0.35f);
            starRect.anchorMax = new Vector2(0.78f + index * 0.08f, 0.65f);
            starRect.offsetMin = Vector2.zero;
            starRect.offsetMax = Vector2.zero;
            stars[index] = starObject.GetComponent<Image>();
            stars[index].color = Color.white;
        }

        SerializedObject serializedItem = new SerializedObject(item.GetComponent<GameModeItemUI>());
        serializedItem.FindProperty("titleText").objectReferenceValue = titleObject.GetComponent<TMP_Text>();
        serializedItem.FindProperty("subtitleText").objectReferenceValue = subtitleObject.GetComponent<TMP_Text>();
        SerializedProperty starArray = serializedItem.FindProperty("difficultyStars");
        starArray.arraySize = stars.Length;
        for (int index = 0; index < stars.Length; index++)
            starArray.GetArrayElementAtIndex(index).objectReferenceValue = stars[index];
        serializedItem.FindProperty("selectButton").objectReferenceValue = button;
        serializedItem.ApplyModifiedPropertiesWithoutUndo();

        return item;
    }

    private static Transform CreateScrollView(Transform panel)
    {
        Transform existing = panel.Find(ScrollViewName);
        if (existing != null)
            return existing;

        GameObject scrollViewObject = new GameObject(ScrollViewName, typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        Undo.RegisterCreatedObjectUndo(scrollViewObject, "Create mode scroll view");
        scrollViewObject.transform.SetParent(panel, false);

        RectTransform scrollRect = scrollViewObject.GetComponent<RectTransform>();
        SetAnchors(scrollRect, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.82f), Vector2.zero, Vector2.zero);
        scrollViewObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);

        GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        Undo.RegisterCreatedObjectUndo(viewportObject, "Create mode scroll viewport");
        viewportObject.transform.SetParent(scrollViewObject.transform, false);
        RectTransform viewport = viewportObject.GetComponent<RectTransform>();
        SetAnchors(viewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        GameObject contentObject = new GameObject(ContentName, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        Undo.RegisterCreatedObjectUndo(contentObject, "Create mode scroll content");
        contentObject.transform.SetParent(viewportObject.transform, false);
        RectTransform content = contentObject.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 12f;
        layout.padding = new RectOffset(16, 16, 16, 16);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = scrollViewObject.GetComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;

        return scrollViewObject.transform;
    }

    private static GameObject CreateTextChild(Transform parent, string name, string text, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(textObject, "Create mode item text");
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI textComponent = textObject.GetComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.alignment = alignment;
        textComponent.color = Color.white;
        textComponent.enableWordWrapping = true;
        return textObject;
    }

    private static void SetAnchors(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }
}
