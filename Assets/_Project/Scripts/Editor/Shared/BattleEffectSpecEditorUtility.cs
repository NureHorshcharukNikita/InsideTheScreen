using UnityEditor;

public static class BattleEffectSpecEditorUtility
{
    public static int CountEmptyConditions(SerializedProperty conditionsProp)
    {
        if (conditionsProp == null || !conditionsProp.isArray)
            return 0;

        int emptyCount = 0;
        for (int i = 0; i < conditionsProp.arraySize; i++)
        {
            SerializedProperty conditionSpecProp = conditionsProp.GetArrayElementAtIndex(i);
            SerializedProperty conditionProp = conditionSpecProp.FindPropertyRelative("condition");

            if (conditionProp.objectReferenceValue == null)
                emptyCount++;
        }

        return emptyCount;
    }
}
