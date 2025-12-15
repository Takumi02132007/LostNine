using System.Collections.Generic;
using UnityEngine;

namespace MessageWindowSystem.Data
{
    [CreateAssetMenu(fileName = "NewDialogueScenario", menuName = "MessageWindow/Dialogue Scenario")]
    public class DialogueScenario : ScriptableObject
    {
        [Tooltip("Unique ID for this scenario (optional, for lookup)")]
        public string scenarioId;

        [Tooltip("The next scenario to play automatically after this one ends.")]
        public DialogueScenario nextScenario;

        public List<DialogueLine> lines;
    }
}
