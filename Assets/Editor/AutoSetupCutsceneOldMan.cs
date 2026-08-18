using UnityEngine;
using UnityEditor;
using UnityEngine.U2D.Animation;

public class AutoSetupCutsceneOldMan
{
    [MenuItem("Tools/Auto-Setup Cutscene Old Man")]
    public static void Setup()
    {
        GameObject oldMan = GameObject.Find("Old Man");
        if (oldMan == null)
        {
            Debug.LogError("Could not find a GameObject named 'Old Man' in the scene.");
            return;
        }

        // 1. Add CutsceneWalkAway
        if (oldMan.GetComponent<CutsceneWalkAway>() == null)
        {
            oldMan.AddComponent<CutsceneWalkAway>();
        }

        // 2. Setup Animator
        Animator anim = oldMan.GetComponent<Animator>();
        if (anim == null)
        {
            anim = oldMan.AddComponent<Animator>();
        }

        string controllerPath = AssetDatabase.GUIDToAssetPath("1ff04f2f419b0a14fb6e342eb3eb78b8");
        RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
        if (controller != null)
        {
            anim.runtimeAnimatorController = controller;
        }
        else
        {
            Debug.LogError("Could not find Player.controller!");
        }

        // 3. Setup SpriteLibrary
        SpriteLibrary lib = oldMan.GetComponent<SpriteLibrary>();
        if (lib == null)
        {
            lib = oldMan.AddComponent<SpriteLibrary>();
        }

        string libPath = AssetDatabase.GUIDToAssetPath("dcb6acd4dfd75e44395a6998745f3d2b");
        SpriteLibraryAsset libAsset = AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(libPath);
        if (libAsset != null)
        {
            lib.spriteLibraryAsset = libAsset;
        }
        else
        {
            Debug.LogError("Could not find OldMan.spriteLib!");
        }

        EditorUtility.SetDirty(oldMan);
        Debug.Log("<color=#00FF00>Successfully implemented Old Man Cutscene, Animator, and SpriteLibrary!</color>");
    }
}
