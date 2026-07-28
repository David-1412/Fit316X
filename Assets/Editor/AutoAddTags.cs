using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class AutoAddTags
{
    static AutoAddTags()
    {
        AddTag("EchoGhost");
    }

    private static void AddTag(string tag)
    {
        UnityEngine.Object[] asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if ((asset != null) && (asset.Length > 0))
        {
            SerializedObject so = new SerializedObject(asset[0]);
            SerializedProperty tags = so.FindProperty("tags");

            for (int i = 0; i < tags.arraySize; ++i)
            {
                if (tags.GetArrayElementAtIndex(i).stringValue == tag)
                {
                    return; // Tag already exists
                }
            }

            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
            so.ApplyModifiedProperties();
            so.Update();
            Debug.Log($"<color=#00FF00>Automatically added missing '{tag}' tag to Project Settings!</color>");
        }
    }
}
