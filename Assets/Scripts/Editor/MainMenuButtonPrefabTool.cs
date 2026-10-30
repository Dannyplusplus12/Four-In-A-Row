using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class MainMenuButtonPrefabTool
{
    private const string PrefabFolder = "Assets/Prefabs/MainMenuButtons";
    private const string PrefabPath = "Assets/Prefabs/PinkButton.prefab";
    private const string SinglePlayerChoicePanelPrefabPath = "Assets/Prefabs/SinglePlayerChoicePanel.prefab";
    private const string PanelName = "MainMenuPanel";
    private const string SelectPanelName = "SelectMenuPanel";
    private const string SinglePlayerChoicePanelName = "SinglePlayerChoicePanel";
    private const string ButtonsRootName = "Buttons";

    private static readonly string[] RequiredButtonNames =
    {
        "OnePlayerButton",
        "TwoPlayerButton",
        "StatsButton",
        "OptionsButton"
    };

    private static readonly string[] SelectPanelButtonNames =
    {
        "BackButton",
        "StartButton"
    };

    private static readonly string[] SinglePlayerChoiceButtonNames =
    {
        "ResumeButton",
        "NewGameButton",
        "StatsButton",
        "MainMenuButton"
    };

    [MenuItem("Tools/Connect Four/Main Menu/Create 4 Buttons From Pink Prefab")]
    private static void ExtractButtons()
    {
        Transform buttonsRoot = FindButtonsRoot(Selection.activeTransform);
        if (buttonsRoot == null)
        {
            EditorUtility.DisplayDialog(
                "Main Menu Buttons",
                "Select MainMenuPanel or its Buttons child before running this tool.",
                "OK");
            return;
        }

        List<Button> buttons = GetRequiredButtons(buttonsRoot);
        if (buttons == null)
            return;

        if (!EditorUtility.DisplayDialog(
                "Create Main Menu Button Prefabs",
                "This will create one PinkButton prefab and replace the four scene buttons with instances while preserving each button's own data.",
                "Continue",
                "Cancel"))
            return;

        EnsureFolderExists(PrefabFolder);
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Extract Main Menu Buttons As Prefabs");

        try
        {
            GameObject prefabAsset = CreateTemplatePrefab(buttons[0].gameObject);
            for (int index = 0; index < buttons.Count; index++)
                ReplaceWithPrefab(buttons[index].gameObject, prefabAsset);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(buttonsRoot.gameObject.scene);
            Selection.activeGameObject = buttonsRoot.gameObject;
            Debug.Log("MainMenuButtonPrefabTool: created one PinkButton prefab and replaced all four MainMenuPanel buttons.");
        }
        finally
        {
            Undo.CollapseUndoOperations(undoGroup);
        }
    }

    [MenuItem("Tools/Connect Four/Main Menu/Create 4 Buttons From Pink Prefab", true)]
    private static bool ValidateExtractButtons()
    {
        return FindButtonsRoot(Selection.activeTransform) != null;
    }

    [MenuItem("Tools/Connect Four/Select Menu/Create Back And Start From Pink Prefab")]
    private static void ReplaceSelectPanelButtons()
    {
        Transform buttonsRoot = FindButtonsRoot(Selection.activeTransform, SelectPanelName);
        if (buttonsRoot == null)
        {
            EditorUtility.DisplayDialog(
                "Select Menu Buttons",
                "Select SelectMenuPanel or its Buttons child before running this tool.",
                "OK");
            return;
        }

        List<Button> buttons = GetRequiredButtons(buttonsRoot, SelectPanelButtonNames);
        if (buttons == null)
            return;

        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefabAsset == null)
        {
            EditorUtility.DisplayDialog(
                "Select Menu Buttons",
                "Could not find the shared prefab at " + PrefabPath + ".",
                "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "Create Select Menu Button Instances",
                "This will replace BackButton and StartButton with instances of PinkButton while preserving their individual text, size, layout, and click events.",
                "Continue",
                "Cancel"))
            return;

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Create Select Menu Buttons From Pink Prefab");
        try
        {
            for (int index = 0; index < buttons.Count; index++)
                ReplaceWithPrefab(buttons[index].gameObject, prefabAsset);

            EditorSceneManager.MarkSceneDirty(buttonsRoot.gameObject.scene);
            Selection.activeGameObject = buttonsRoot.gameObject;
            Debug.Log("MainMenuButtonPrefabTool: replaced SelectMenuPanel BackButton and StartButton from PinkButton.prefab.");
        }
        finally
        {
            Undo.CollapseUndoOperations(undoGroup);
        }
    }

    [MenuItem("Tools/Connect Four/Select Menu/Create Back And Start From Pink Prefab", true)]
    private static bool ValidateReplaceSelectPanelButtons()
    {
        return FindButtonsRoot(Selection.activeTransform, SelectPanelName) != null;
    }

    [MenuItem("Tools/Connect Four/Single Player Choice/Create Panel Prefab")]
    private static void CreateSinglePlayerChoicePanelPrefab()
    {
        Transform panelTransform = FindSelectedPanel(Selection.activeTransform, SinglePlayerChoicePanelName);
        if (panelTransform == null)
        {
            EditorUtility.DisplayDialog(
                "Single Player Choice Panel",
                "Select SinglePlayerChoicePanel before running this tool.",
                "OK");
            return;
        }

        Transform buttonsRoot = FindDirectChild(panelTransform, ButtonsRootName);
        if (buttonsRoot == null || GetRequiredButtons(buttonsRoot, SinglePlayerChoiceButtonNames) == null)
            return;

        GameObject pinkButtonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (pinkButtonPrefab == null)
        {
            EditorUtility.DisplayDialog(
                "Single Player Choice Panel",
                "Could not find the shared prefab at " + PrefabPath + ".",
                "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "Create Single Player Choice Panel Prefab",
                "This will replace the four panel buttons with PinkButton instances, create one reusable panel prefab, and preserve the panel's scene references.",
                "Continue",
                "Cancel"))
            return;

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Create Single Player Choice Panel Prefab");
        try
        {
            List<Button> buttons = GetRequiredButtons(buttonsRoot, SinglePlayerChoiceButtonNames);
            for (int index = 0; index < buttons.Count; index++)
                ReplaceWithPrefab(buttons[index].gameObject, pinkButtonPrefab);

            GameObject panelPrefab = CreatePrefabAsset(panelTransform.gameObject, SinglePlayerChoicePanelPrefabPath, SinglePlayerChoicePanelName);
            ReplacePanelWithPrefab(panelTransform.gameObject, panelPrefab);

            AssetDatabase.SaveAssets();
            Selection.activeGameObject = GameObject.Find(SinglePlayerChoicePanelName);
            Debug.Log("MainMenuButtonPrefabTool: created SinglePlayerChoicePanel.prefab with four PinkButton instances.");
        }
        finally
        {
            Undo.CollapseUndoOperations(undoGroup);
        }
    }

    [MenuItem("Tools/Connect Four/Single Player Choice/Create Panel Prefab", true)]
    private static bool ValidateCreateSinglePlayerChoicePanelPrefab()
    {
        return FindSelectedPanel(Selection.activeTransform, SinglePlayerChoicePanelName) != null;
    }

    private static Transform FindButtonsRoot(Transform selected)
    {
        return FindButtonsRoot(selected, PanelName);
    }

    private static Transform FindButtonsRoot(Transform selected, string panelName)
    {
        if (selected == null)
            return null;

        if (selected.name == ButtonsRootName && FindAncestor(selected, panelName) != null)
            return selected;

        Transform panel = selected.name == panelName ? selected : FindAncestor(selected, panelName);
        return panel == null ? null : FindDirectChild(panel, ButtonsRootName);
    }

    private static Transform FindSelectedPanel(Transform selected, string panelName)
    {
        if (selected == null)
            return null;
        return selected.name == panelName ? selected : FindAncestor(selected, panelName);
    }

    private static List<Button> GetRequiredButtons(Transform buttonsRoot)
    {
        return GetRequiredButtons(buttonsRoot, RequiredButtonNames);
    }

    private static List<Button> GetRequiredButtons(Transform buttonsRoot, string[] buttonNames)
    {
        var result = new List<Button>(buttonNames.Length);
        for (int index = 0; index < buttonNames.Length; index++)
        {
            Transform child = FindDirectChild(buttonsRoot, buttonNames[index]);
            Button button = child == null ? null : child.GetComponent<Button>();
            if (button == null)
            {
                EditorUtility.DisplayDialog(
                    "Main Menu Buttons",
                    "Could not find a Button component on direct child '" + buttonNames[index] + "'.",
                    "OK");
                return null;
            }

            result.Add(button);
        }

        return result;
    }

    private static GameObject CreateTemplatePrefab(GameObject sourceObject)
    {
        GameObject prefabSource = UnityEngine.Object.Instantiate(sourceObject);
        prefabSource.name = "PinkButton";

        Button templateButton = prefabSource.GetComponent<Button>();
        if (templateButton == null)
            throw new InvalidOperationException("The selected source button has no Button component.");

        SerializedObject templateSerializedObject = new SerializedObject(templateButton);
        SerializedProperty clickProperty = templateSerializedObject.FindProperty("m_OnClick");
        clickProperty.FindPropertyRelative("m_PersistentCalls.m_Calls").ClearArray();
        templateSerializedObject.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(prefabSource, PrefabPath);
        UnityEngine.Object.DestroyImmediate(prefabSource);
        if (prefabAsset == null)
            throw new InvalidOperationException("Could not create prefab at " + PrefabPath);

        return prefabAsset;
    }

    private static GameObject CreatePrefabAsset(GameObject sourceObject, string prefabPath, string prefabName)
    {
        GameObject prefabSource = UnityEngine.Object.Instantiate(sourceObject);
        prefabSource.name = prefabName;
        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(prefabSource, prefabPath);
        UnityEngine.Object.DestroyImmediate(prefabSource);
        if (prefabAsset == null)
            throw new InvalidOperationException("Could not create prefab at " + prefabPath);
        return prefabAsset;
    }

    private static void ReplaceWithPrefab(GameObject sourceObject, GameObject prefabAsset)
    {
        Transform sourceTransform = sourceObject.transform;
        Transform parent = sourceTransform.parent;
        int siblingIndex = sourceTransform.GetSiblingIndex();
        string objectName = sourceObject.name;

        GameObject replacement = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset, parent);
        if (replacement == null)
            throw new InvalidOperationException("Could not instantiate prefab at " + PrefabPath);

        Undo.RegisterCreatedObjectUndo(replacement, "Create button prefab instance");
        replacement.transform.SetSiblingIndex(siblingIndex);
        CopyHierarchyData(sourceObject, replacement);
        replacement.name = objectName;
        replacement.SetActive(sourceObject.activeSelf);

        Button replacementButton = replacement.GetComponent<Button>();
        if (replacementButton == null)
            throw new InvalidOperationException("Prefab has no Button component at " + PrefabPath);

        Undo.DestroyObjectImmediate(sourceObject);
        PrefabUtility.RecordPrefabInstancePropertyModifications(replacement);
    }

    private static void ReplacePanelWithPrefab(GameObject sourceObject, GameObject prefabAsset)
    {
        Transform sourceTransform = sourceObject.transform;
        Transform parent = sourceTransform.parent;
        int siblingIndex = sourceTransform.GetSiblingIndex();
        string objectName = sourceObject.name;

        GameObject replacement = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset, parent);
        if (replacement == null)
            throw new InvalidOperationException("Could not instantiate prefab at " + SinglePlayerChoicePanelPrefabPath);

        Undo.RegisterCreatedObjectUndo(replacement, "Create panel prefab instance");
        replacement.transform.SetSiblingIndex(siblingIndex);
        CopyHierarchyData(sourceObject, replacement);
        replacement.name = objectName;
        replacement.SetActive(sourceObject.activeSelf);
        UpdateSceneReferences(sourceObject, replacement);
        Undo.DestroyObjectImmediate(sourceObject);
        PrefabUtility.RecordPrefabInstancePropertyModifications(replacement);
    }

    private static void UpdateSceneReferences(GameObject oldObject, GameObject newObject)
    {
        GameObject[] sceneObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int objectIndex = 0; objectIndex < sceneObjects.Length; objectIndex++)
        {
            Component[] components = sceneObjects[objectIndex].GetComponents<Component>();
            for (int componentIndex = 0; componentIndex < components.Length; componentIndex++)
            {
                Component component = components[componentIndex];
                if (component == null)
                    continue;

                SerializedObject serializedObject = new SerializedObject(component);
                SerializedProperty iterator = serializedObject.GetIterator();
                bool changed = false;
                bool enterChildren = true;
                while (iterator.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    if (iterator.propertyType == SerializedPropertyType.ObjectReference && iterator.objectReferenceValue == oldObject)
                    {
                        iterator.objectReferenceValue = newObject;
                        changed = true;
                    }
                }

                if (changed)
                {
                    serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(component);
                }
            }
        }
    }

    private static void CopyHierarchyData(GameObject sourceRoot, GameObject targetRoot)
    {
        Transform[] sourceTransforms = sourceRoot.GetComponentsInChildren<Transform>(true);
        for (int index = 0; index < sourceTransforms.Length; index++)
        {
            Transform sourceTransform = sourceTransforms[index];
            string relativePath = GetRelativePath(sourceRoot.transform, sourceTransform);
            Transform targetTransform = string.IsNullOrEmpty(relativePath)
                ? targetRoot.transform
                : targetRoot.transform.Find(relativePath);
            if (targetTransform == null)
                throw new InvalidOperationException("Prefab hierarchy is missing child '" + relativePath + "'.");

            CopyComponentData(sourceRoot.transform, targetRoot.transform, sourceTransform, targetTransform);
        }
    }

    private static void CopyComponentData(
        Transform sourceRoot,
        Transform targetRoot,
        Transform sourceTransform,
        Transform targetTransform)
    {
        Component[] sourceComponents = sourceTransform.GetComponents<Component>();
        Component[] targetComponents = targetTransform.GetComponents<Component>();
        for (int index = 0; index < sourceComponents.Length; index++)
        {
            Component sourceComponent = sourceComponents[index];
            if (sourceComponent == null)
                continue;

            Component targetComponent = FindMatchingComponent(sourceComponent, targetComponents, sourceComponents, index);
            if (targetComponent == null)
                continue;

            SerializedObject sourceSerializedObject = new SerializedObject(sourceComponent);
            SerializedObject targetSerializedObject = new SerializedObject(targetComponent);
            SerializedProperty iterator = sourceSerializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.name == "m_Script" || iterator.name == "m_GameObject" || iterator.name == "m_Father" || iterator.name == "m_Children")
                    continue;

                SerializedProperty targetProperty = targetSerializedObject.FindProperty(iterator.propertyPath);
                if (targetProperty == null)
                    continue;

                targetSerializedObject.CopyFromSerializedProperty(iterator);
                if (iterator.propertyType == SerializedPropertyType.ObjectReference)
                {
                    UnityEngine.Object mappedReference = MapLocalReference(
                        sourceRoot,
                        iterator.objectReferenceValue,
                        targetRoot);
                    if (mappedReference != null)
                        targetProperty.objectReferenceValue = mappedReference;
                }
            }

            targetSerializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static Component FindMatchingComponent(
        Component sourceComponent,
        Component[] targetComponents,
        Component[] sourceComponents,
        int sourceIndex)
    {
        Type componentType = sourceComponent == null ? null : sourceComponent.GetType();
        if (componentType == null)
            return null;

        int occurrence = 0;
        for (int index = 0; index < sourceIndex; index++)
            if (sourceComponents[index] != null && sourceComponents[index].GetType() == componentType)
                occurrence++;

        for (int index = 0; index < targetComponents.Length; index++)
        {
            if (targetComponents[index] == null || targetComponents[index].GetType() != componentType)
                continue;
            if (occurrence-- == 0)
                return targetComponents[index];
        }

        return null;
    }

    private static UnityEngine.Object MapLocalReference(
        Transform sourceRoot,
        UnityEngine.Object sourceReference,
        Transform targetRoot)
    {
        if (sourceReference is not Component sourceComponent || !sourceComponent.transform.IsChildOf(sourceRoot))
            return sourceReference;

        string relativePath = GetRelativePath(sourceRoot, sourceComponent.transform);
        Transform targetTransform = string.IsNullOrEmpty(relativePath)
            ? targetRoot
            : targetRoot.Find(relativePath);
        if (targetTransform == null)
            return sourceReference;

        Component[] targetComponents = targetTransform.GetComponents(sourceComponent.GetType());
        Component[] sourceComponents = sourceComponent.transform.GetComponents(sourceComponent.GetType());
        for (int index = 0; index < sourceComponents.Length; index++)
            if (sourceComponents[index] == sourceComponent)
                return index < targetComponents.Length ? targetComponents[index] : sourceReference;

        return sourceReference;
    }

    private static string GetRelativePath(Transform root, Transform target)
    {
        if (root == target)
            return string.Empty;

        var path = new List<string>();
        Transform current = target;
        while (current != null && current != root)
        {
            path.Insert(0, current.name);
            current = current.parent;
        }

        return current == root ? string.Join("/", path) : string.Empty;
    }

    private static Transform FindAncestor(Transform current, string name)
    {
        while (current != null)
        {
            if (current.name == name)
                return current;
            current = current.parent;
        }

        return null;
    }

    private static Transform FindDirectChild(Transform parent, string name)
    {
        for (int index = 0; index < parent.childCount; index++)
        {
            Transform child = parent.GetChild(index);
            if (child.name == name)
                return child;
        }

        return null;
    }

    private static void EnsureFolderExists(string folderPath)
    {
        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int index = 1; index < parts.Length; index++)
        {
            string next = current + "/" + parts[index];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[index]);
            current = next;
        }
    }
}