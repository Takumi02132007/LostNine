using System.Collections.Generic;
using UnityEngine;

namespace MessageWindowSystem.Data
{
    [CreateAssetMenu(fileName = "NewDialogueScenario", menuName = "MessageWindow/Dialogue Scenario")]
    public class DialogueScenario : ScriptableObject
    {
        public List<DialogueLine> lines;
    }
}
