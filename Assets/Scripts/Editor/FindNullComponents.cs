using UnityEditor;
using UnityEngine;

public class FindNullComponents : MonoBehaviour
{
    [MenuItem("Tools/Find Missing Components in Scene")]
    static void FindMissingReferences()
    {
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        foreach (GameObject go in allObjects)
        {
            Component[] components = go.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    Debug.LogError($"Missing component on GameObject: {go.name}", go);
                }
            }
        }
    }
}
