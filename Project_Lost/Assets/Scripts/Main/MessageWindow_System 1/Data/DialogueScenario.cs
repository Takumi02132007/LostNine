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

        [Tooltip("Whether keywords are interactive in this scenario.")]
        public bool enableKeywords = true;

        [Tooltip("If true, this scenario will restart from the beginning when it ends.")]
        public bool loopScenario = false;

        [Tooltip("If true, calls ComuStartandEndManager.ToggleComu() when this scenario ends.")]
        public bool toggleComuOnEnd = false;

        public List<DialogueLine> lines;
    }
}
