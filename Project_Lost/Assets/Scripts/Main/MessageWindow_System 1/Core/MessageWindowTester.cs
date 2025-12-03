using UnityEngine;
using MessageWindowSystem.Data;
using MessageWindowSystem.Core;
using System.Collections.Generic;

namespace MessageWindowSystem.Testing
{
    public class MessageWindowTester : MonoBehaviour
    {
        [Header("Test Settings")]
        public bool playOnStart = true;
        public DialogueScenario testScenario;

        private void Start()
        {
            if (playOnStart)
            {
                if (testScenario != null)
                {
                    MessageWindowManager.Instance.StartScenario(testScenario);
                }
                else
                {
                    // Create a dummy scenario if none provided
                    var dummyScenario = ScriptableObject.CreateInstance<DialogueScenario>();
                    dummyScenario.lines = new List<DialogueLine>
                    {
                        new DialogueLine { speakerName = "System", text = "Hello! This is a test of the <color=yellow>Message Window System</color>." },
                        new DialogueLine { speakerName = "System", text = "It supports <b>Rich Text</b> via TextMeshPro." },
                        new DialogueLine { speakerName = "System", text = "And custom effects like... <shake>Camera Shake!</shake>", customActions = new List<string> { "Shake" } },
                        new DialogueLine { speakerName = "System", text = "Click to continue or skip typing." }
                    };
                    
                    MessageWindowManager.Instance.StartScenario(dummyScenario);
                }
            }
        }
    }
}
