using UnityEngine;
using UnityEngine.Rendering;

public class CharacterSorting : MonoBehaviour
{
    [SerializeField] private string sortingLayerName = "NPC";
    [SerializeField] private int yMultiplier = 100;

    private SortingGroup sortingGroup;
    private SpriteRenderer[] spriteRenderers;
    private int tieBreaker;

    private void Awake()
    {
        Setup();
    }

    private void OnValidate()
    {
        Setup();
        ApplySortingLayerToChildren();
        UpdateSorting();
    }

    private void LateUpdate()
    {
        UpdateSorting();
    }

    private void Setup()
    {
        sortingGroup = GetComponent<SortingGroup>();
        if (sortingGroup == null)
            sortingGroup = gameObject.AddComponent<SortingGroup>();

        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        if (tieBreaker == 0)
            tieBreaker = Random.Range(-2, 3);
    }

    private void ApplySortingLayerToChildren()
    {
        if (spriteRenderers == null) return;

        foreach (var sr in spriteRenderers)
        {
            if (sr != null)
                sr.sortingLayerName = sortingLayerName;
        }

        if (sortingGroup != null)
            sortingGroup.sortingLayerName = sortingLayerName;
    }

    private void UpdateSorting()
    {
        if (sortingGroup == null)
            return;

        int baseOrder = -(int)(transform.position.y * yMultiplier) + tieBreaker;
        sortingGroup.sortingOrder = baseOrder;
    }
}