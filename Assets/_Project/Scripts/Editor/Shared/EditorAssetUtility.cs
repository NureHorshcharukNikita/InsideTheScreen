using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class EditorAssetUtility
{
    public static T CreateAsset<T>(string folderPath, string assetName) where T : ScriptableObject
    {
        return (T)CreateAsset(typeof(T), folderPath, assetName);
    }

    public static ScriptableObject CreateAsset(Type type, string folderPath, string assetName)
    {
        EnsureFolderExists(folderPath);
        string safeName = string.IsNullOrWhiteSpace(assetName) ? type.Name : assetName.Trim();
        string path = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/{safeName}.asset");
        ScriptableObject asset = ScriptableObject.CreateInstance(type);
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
    }

    public static List<T> LoadAssets<T>() where T : UnityEngine.Object
    {
        return AssetDatabase.FindAssets($"t:{typeof(T).Name}")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<T>)
            .Where(asset => asset != null)
            .OrderBy(asset => asset.name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static List<Type> LoadConcreteTypes<T>()
    {
        return TypeCache.GetTypesDerivedFrom<T>()
            .Where(type => !type.IsAbstract)
            .OrderBy(type => type.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static bool ContainsIgnoreCase(string source, string query)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(query))
            return false;

        return source.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static void EnsureFolderExists(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
