using UnityEngine;

public static class GlobalHelper
{
    public static string GenerateUniqueID(GameObject obj)
    {
        return $"{obj.name}_{obj.transform.position.x}_{obj.transform.position.y}"; //Chest_3_4
    }
}
