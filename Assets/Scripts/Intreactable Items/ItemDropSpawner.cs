using UnityEngine;

public static class ItemDropSpawner
{
    public static void SpawnItem(GameObject itemPrefab, GameObject source, float dropDistance = 5f, float verticalOffset = 0.2f, float collectionDelay = 0.3f)
    {
        if (itemPrefab == null || source == null) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("Player not found for WorldDropSpawner");
            return;
        }

        Vector3 spawnPos = GetSpawnPosition(source, player, dropDistance);
        GameObject droppedItem = Object.Instantiate(itemPrefab, spawnPos, Quaternion.identity);

        Item item = droppedItem.GetComponent<Item>();
        if (item != null)
        {
            item.SetTrackCollection(false);
            item.EnableCollection(collectionDelay);
        }

        BounceEffect bounce = droppedItem.GetComponent<BounceEffect>();
        if (bounce != null)
        {
            bounce.StartBounce();
        }
    }

    private static Vector3 GetSpawnPosition(GameObject source, GameObject player, float dropDistance)
    {
        Transform playerTransform = player.transform;

        Vector3 basePos = GetGroundSpawnBase(source, playerTransform);
        Vector3 horizontalDir = basePos.x >= playerTransform.position.x ? Vector3.right : Vector3.left;

        if (Mathf.Abs(basePos.x - playerTransform.position.x) < 0.1f)
        {
            horizontalDir = Random.value > 0.5f ? Vector3.right : Vector3.left;
        }

        return basePos + horizontalDir * dropDistance;
    }

    private static Vector3 GetGroundSpawnBase(GameObject source, Transform playerTransform)
    {
        Collider2D col = source.GetComponent<Collider2D>();
        if (col != null)
        {
            return new Vector3(source.transform.position.x, playerTransform.position.y, source.transform.position.z);
        }

        SpriteRenderer sr = source.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            return new Vector3(source.transform.position.x, playerTransform.position.y, source.transform.position.z);
        }

        return source.transform.position;
    }
}
