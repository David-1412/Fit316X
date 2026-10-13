using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class NPC : MonoBehaviour, IInteractable
{
    public NPCDialogue dialogueData;
    private DialogueBox dialogueUI;
    private int dialogueIndex;
    private bool isTyping, isDialogueActive, isChoosing;

    // Frame of the last start/choice, so the same key press or click doesn't also skip a line
    private int inputBlockedFrame = -1;

    private enum QuestState { NotStarted, InProgress, Completed }
    private QuestState questState = QuestState.NotStarted;

    [Header("Events")]
    public UnityEvent onDialogueEnd;

    private void Update()
    {
        // Space, Enter, E or click progresses dialogue while it's active
        if (!isDialogueActive || isChoosing || Time.frameCount <= inputBlockedFrame) return;

        if (DialogueBox.AdvancePressed())
        {
            NextLine();
        }
    }

    public bool CanInteract()
    {
        return !isDialogueActive;
    }

    public void Interact()
    {
        // If no dialogue data or the game is paused and no dialogue is active
        if (dialogueData == null || (PauseController.IsGamePaused && !isDialogueActive))
            return;

        if (isDialogueActive)
        {
            NextLine();
        }
        else
        {
            StartDialogue();
        }
    }

    void StartDialogue()
    {
        dialogueUI = DialogueBox.Instance;

        //Sync with quest data
        SyncQuestState();

        //Set dialogue line based on questState
        if(questState == QuestState.NotStarted)
        {
            dialogueIndex = 0;
        }
        else if(questState == QuestState.InProgress)
        {
            dialogueIndex = dialogueData.questInProgressIndex;
        }
        else if(questState == QuestState.Completed)
        {
            dialogueIndex = dialogueData.questCompletedIndex;
        }

        isDialogueActive = true;
        isChoosing = false;
        inputBlockedFrame = Time.frameCount;

        dialogueUI.Open(dialogueData.npcName, dialogueData.npcPortrait);
        PauseController.SetPause(true);

        DisplayCurrentLine();
    }

    private void SyncQuestState()
    {
        if (dialogueData.quest == null) return;

        string questID = dialogueData.quest.questID;

        //Future update add completing quest and handing in!
        if(QuestController.Instance.IsQuestCompleted(questID) || QuestController.Instance.IsQuestHandedIn(questID))
        {
            questState = QuestState.Completed;
        }
        else if (QuestController.Instance.IsQuestActive(questID))
        {
            questState = QuestState.InProgress;
        }
        else
        {
            questState = QuestState.NotStarted;
        }
    }

    void NextLine()
    {
        if (isTyping)
        {
            // Skip typing animation and show the full line
            StopAllCoroutines();
            dialogueUI.ShowFullLine();
            isTyping = false;
            return;
        }

        //Clear Choices
        dialogueUI.ClearChoices();

        //Check endDialogueLines
        if(dialogueData.endDialogueLines != null && dialogueData.endDialogueLines.Length > dialogueIndex && dialogueData.endDialogueLines[dialogueIndex])
        {
            EndDialogue();
            return;
        }

        //Check if choices & display
        if (dialogueData.choices != null)
        {
            foreach(DialogueChoice dialogueChoice in dialogueData.choices)
            {
                if(dialogueChoice != null && dialogueChoice.dialogueIndex == dialogueIndex)
                {
                    DisplayChoices(dialogueChoice);
                    return;
                }
            }
        }

        if(dialogueData.dialogueLines != null && ++dialogueIndex < dialogueData.dialogueLines.Length)
        {
            //If another line, type next line
            DisplayCurrentLine();
        }
        else
        {
            EndDialogue();
        }
    }

    IEnumerator TypeLine()
    {
        isTyping = true;

        // Same typewriter and per-letter voice sound as the Floor 5 cutscene
        yield return dialogueUI.TypeLine(dialogueData.dialogueLines[dialogueIndex],
                                         dialogueData.typingSpeed,
                                         dialogueData.voiceSound,
                                         dialogueData.voicePitch);

        isTyping = false;
        inputBlockedFrame = Time.frameCount; // a press that skipped typing shouldn't also advance

        if(dialogueData.autoProgressLines != null && dialogueData.autoProgressLines.Length > dialogueIndex && dialogueData.autoProgressLines[dialogueIndex])
        {
            yield return new WaitForSeconds(dialogueData.autoProgressDelay);
            NextLine();
        }
    }

    void DisplayChoices(DialogueChoice choice)
    {
        isChoosing = true;
        dialogueUI.ShowChoices(choice.choices, i =>
        {
            int nextIndex = choice.nextDialogueIndexes[i];
            bool givesQuest = choice.givesQuest.Length > i && choice.givesQuest[i];
            ChooseOption(nextIndex, givesQuest);
        });
    }

    void ChooseOption(int nextIndex, bool givesQuest)
    {
        if (givesQuest)
        {
            QuestController.Instance.AcceptQuest(dialogueData.quest);
            questState = QuestState.InProgress;
        }

        isChoosing = false;
        inputBlockedFrame = Time.frameCount; // the click/Enter that picked the choice
        dialogueIndex = nextIndex;
        dialogueUI.ClearChoices();
        DisplayCurrentLine();
    }

    void DisplayCurrentLine()
    {
        StopAllCoroutines();
        StartCoroutine(TypeLine());
    }

    public void EndDialogue()
    {
        if(questState == QuestState.Completed && dialogueData != null && dialogueData.quest != null && QuestController.Instance != null && !QuestController.Instance.IsQuestHandedIn(dialogueData.quest.questID))
        {
            HandleQuestCompletion(dialogueData.quest);
        }

        StopAllCoroutines();
        isDialogueActive = false;
        isTyping = false;
        isChoosing = false;
        if (dialogueUI != null) dialogueUI.Close();

        PauseController.SetPause(false);

        // Trigger any cutscenes or events hooked up in the inspector!
        onDialogueEnd?.Invoke();
    }

    void HandleQuestCompletion(Quest quest)
    {
        RewardsController.Instance.GiveQuestReward(quest);
        QuestController.Instance.HandInQuest(quest.questID);
    }
}
