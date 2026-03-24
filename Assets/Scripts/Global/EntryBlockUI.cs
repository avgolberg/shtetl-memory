using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EntryBlockUI : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Transform linesContainer;
    [SerializeField] private GameObject linePrefab;

    public void Setup(string title, List<string> lines)
    {
        titleText.text = title;

        foreach (Transform child in linesContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (string line in lines)
        {
            GameObject lineGO = Instantiate(linePrefab, linesContainer);
            TMP_Text lineText = lineGO.GetComponent<TMP_Text>();
            lineText.text = line;
        }
    }
}