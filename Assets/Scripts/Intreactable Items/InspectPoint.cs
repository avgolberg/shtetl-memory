using UnityEngine;

//Something the player looks at closely: one or more pages in the info popup (GraveInfoUI).
//Each page can unlock a journal stage, complete a Custom quest objective and start a quest (for example a new lead).
//Works on its own with a trigger collider, or as the contents a MiniGameItem reveals once it is solved.
public class InspectPoint : MonoBehaviour, IInteractable
{
    [System.Serializable]
    public class Page
    {
        public Sprite image;
        public string title;
        [TextArea] public string text;
        [TextArea] public string imageText; //Optional: printed on the image (e.g. an invitation's wording)
        public string journalTopicId; //Optional
        public int journalStage;
        public string questObjectiveId; //Optional: Custom objective completed when the page is seen
        public Quest startsQuest; //Optional
    }

    [SerializeField] private Page[] pages;

    public bool CanInteract()
    {
        return pages != null && pages.Length > 0 && GraveInfoUI.Instance != null && !GraveInfoUI.Instance.IsOpen;
    }

    public void Interact()
    {
        if (!CanInteract()) return;

        ShowPage(0);
    }

    private void ShowPage(int index)
    {
        ApplyEffects(pages[index]);

        bool hasNext = index + 1 < pages.Length;
        GraveInfoUI.Instance.Show(
            pages[index].image,
            pages[index].title,
            pages[index].text,
            hasNext ? () => ShowPage(index + 1) : null,
            hasNext ? "Next" : null,
            hasNext ? () => ApplyRemaining(index + 1) : null,
            pages[index].imageText
        );
    }

    //Closing early must not lose progress: the unseen pages still count
    private void ApplyRemaining(int fromIndex)
    {
        for (int i = fromIndex; i < pages.Length; i++)
        {
            ApplyEffects(pages[i]);
        }
    }

    private static void ApplyEffects(Page page)
    {
        if (!string.IsNullOrEmpty(page.journalTopicId))
            JournalController.Instance.UnlockOrUpdateTopic(page.journalTopicId, page.journalStage);

        if (!string.IsNullOrEmpty(page.questObjectiveId))
            QuestController.Instance.CompleteObjective(page.questObjectiveId);

        Quest quest = page.startsQuest;
        if (quest != null && !QuestController.Instance.IsQuestActive(quest.questID) &&
            !QuestController.Instance.IsQuestHandedIn(quest.questID))
        {
            QuestController.Instance.AcceptQuest(quest);
        }
    }
}
