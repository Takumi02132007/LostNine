using UnityEngine;
using MessageWindowSystem.Data;
using MessageWindowSystem.Core;
using System.Collections.Generic;

namespace MessageWindowSystem.Testing
{
    public class MessageWindowIndexStarter : MonoBehaviour
    {
        [Header("Settings")]
        public bool playOnStart = true;
        
        [Header("Scenario Data")]
        [Tooltip("Database containing all scenarios")]
        public ScenarioDatabase scenarioDatabase;

        [Tooltip("ID of the scenario to start (if Play On Start is true)")]
        public string startScenarioId;

        public bool enableKeywords = false;

        private void Start()
        {
            if (playOnStart)
            {
                StartScenarioById(startScenarioId);
            }
        }

        public void StartScenarioById(string scenarioId)
        {
            if (scenarioDatabase != null)
            {
                var scenario = scenarioDatabase.GetScenarioById(scenarioId);
                if (scenario != null)
                {
                    MessageWindowManager.Instance.StartScenario(scenario, enableKeywords);
                }
                else
                {
                    Debug.LogWarning($"Scenario ID '{scenarioId}' not found in database.");
                }
            }
            else
            {
                Debug.LogError("Scenario Database is not assigned in MessageWindowIndexStarter.");
            }
        }

        // Keep this for legacy support or manual testing via index if needed, but better to use ID.
        // Or we can create a simple test method.
        [ContextMenu("Test Start Scenario")]
        public void TestStart()
        {
            StartScenarioById(startScenarioId);
        }
    }
}
