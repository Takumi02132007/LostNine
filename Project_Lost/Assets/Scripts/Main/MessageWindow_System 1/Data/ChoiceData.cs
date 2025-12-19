using System;
using System.Collections.Generic;
using UnityEngine;

namespace MessageWindowSystem.Data
{
    /// <summary>
    /// Represents a single choice option in dialogue.
    /// </summary>
    [Serializable]
    public class ChoiceData
    {
        [Tooltip("Text displayed on the choice button.")]
        public string choiceText;

        [Tooltip("Scenario to start when this choice is selected (optional).")]
        public DialogueScenario nextScenario;

        [Tooltip("Unique ID for this choice (for logic hooks).")]
        public string choiceId;
    }
}
