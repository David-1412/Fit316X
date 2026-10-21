using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tools > Apply Menu Style: writes the code-built menu look (MainMenuStyle / MenuStyle)
/// into the open scene(s), so it shows and can be tweaked in the editor without pressing Play.
/// Open the StartMenu scene or a floor (F1-F5), run it, then save the scene.
/// Running it again is safe: anything already built is reused, not duplicated.
/// </summary>
public static class ApplyMenuStyle
{
    private const string GlowPath = "Assets/Sprites/UI/MenuGlow.png";

    [MenuItem("Tools/Apply Menu Style")]
    private static void Apply()
    {
        EnsureGlowTexture();

        int startMenus = 0, tabMenus = 0;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;
            bool changed = false;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (MainMenuManager menu in root.GetComponentsInChildren<MainMenuManager>(true))
                {
                    Undo.RegisterFullObjectHierarchyUndo(menu.gameObject, "Apply Menu Style");
                    MainMenuStyle.Apply(menu);
                    RecordPrefabOverrides(menu.gameObject);
                    startMenus++;
                    changed = true;
                }

                foreach (TabController tabs in root.GetComponentsInChildren<TabController>(true))
                {
                    GameObject canvas = tabs.transform.root.gameObject;
                    Undo.RegisterFullObjectHierarchyUndo(canvas, "Apply Menu Style");
                    MenuStyle.Apply(tabs);
                    RecordPrefabOverrides(canvas);
                    tabMenus++;
                    changed = true;
                }
            }

            if (changed) EditorSceneManager.MarkSceneDirty(scene);
        }

        string result = startMenus + tabMenus == 0
            ? "No start menu or Tab menu found in the open scene(s). Open StartMenu or a floor scene first."
            : $"Styled {startMenus} start menu(s) and {tabMenus} Tab menu(s).\n\nSave the scene (Ctrl+S) to keep it.";
        EditorUtility.DisplayDialog("Apply Menu Style", result, "OK");
    }

    /// <summary>Changes inside a prefab instance (F2's UICanvas) must be recorded or they are lost.</summary>
    private static void RecordPrefabOverrides(GameObject root)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (!PrefabUtility.IsPartOfPrefabInstance(t.gameObject)) continue;
            PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);
            foreach (Component component in t.GetComponents<Component>())
                if (component != null) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }
    }

    /// <summary>The start menu glow is generated once and saved as an image, so the scene can keep it.</summary>
    private static void EnsureGlowTexture()
    {
        MenuAssets assets = MenuAssets.Load();
        if (assets == null || assets.glowTexture != null) return;

        if (!File.Exists(GlowPath))
        {
            Texture2D generated = MainMenuStyle.MakeGlowTexture(128);
            File.WriteAllBytes(GlowPath, generated.EncodeToPNG());
            Object.DestroyImmediate(generated);
            AssetDatabase.ImportAsset(GlowPath);

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(GlowPath);
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        assets.glowTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(GlowPath);
        EditorUtility.SetDirty(assets);
        AssetDatabase.SaveAssets();
    }
}
