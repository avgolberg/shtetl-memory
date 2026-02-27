using UnityEngine;

public class RewardsController : MonoBehaviour
{
    public static RewardsController Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void GiveQuestReward(Quest quest)
    {
        if (quest?.questRewards == null) return;

        foreach (var reward in quest.questRewards)
        {
            switch (reward.type)
            {
                case RewardType.Item:
                    GiveItemReward(reward.rewardID, reward.amount);
                    break;
                case RewardType.Portal:
                    GiveTeleportReward(reward.rewardID);
                    break;
                case RewardType.Custom:
                    break;
            }
        }
    }

    public void GiveItemReward(int itemID, int amount)
    {
        var itemPrefab = FindAnyObjectByType<ItemDictionary>()?.GetItemPrefab(itemID);

        if (itemPrefab == null) return;

        for (int i = 0; i < amount; i++)
        {
            InventoryController.Instance.AddItem(itemPrefab);
            itemPrefab.GetComponent<Item>().ShowPopUp();
        }
    }

    public void GiveTeleportReward(int teleportID)
    {
        var teleportPrefab = FindAnyObjectByType<TransitionDictionary>()?.GetTransitionPrefab(teleportID);

        if (teleportPrefab == null) return;

        teleportPrefab.GetComponent<MapTransition>().ChangeSprite();
        
    }
}
