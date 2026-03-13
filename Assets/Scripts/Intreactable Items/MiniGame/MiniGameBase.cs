using UnityEngine;

public abstract class MiniGameBase : MonoBehaviour
{
    protected MiniGameItem ownerItem;

    public virtual void Open(MiniGameItem item)
    {
        ownerItem = item;
        gameObject.SetActive(true);
    }

    public virtual void Close()
    {
        Destroy(gameObject);
    }

    protected void Complete()
    {
        if (ownerItem != null)
        {
            ownerItem.CompleteMiniGame();
        }

        Close();
    }
}

