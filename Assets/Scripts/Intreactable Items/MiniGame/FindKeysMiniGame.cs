using UnityEngine;

public class FindKeysMiniGame : MiniGameBase
{
    [SerializeField] private int targetKeysCount = 3;
    private int foundKeys;

    public void FoundKey()
    {
        foundKeys++;

        if (foundKeys >= targetKeysCount)
        {
            Complete();
        }
    }
}

