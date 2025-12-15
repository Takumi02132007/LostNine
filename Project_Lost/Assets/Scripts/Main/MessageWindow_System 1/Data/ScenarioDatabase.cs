using System.Collections.Generic;
using UnityEngine;

namespace MessageWindowSystem.Data
{
    /// <summary>
    /// Database to hold references to all DialogueScenarios.
    /// Allows retrieving scenarios by ID.
    /// </summary>
    [CreateAssetMenu(fileName = "ScenarioDatabase", menuName = "MessageWindow/Scenario Database")]
    public class ScenarioDatabase : ScriptableObject
    {
        [Tooltip("List of all scenarios in the game.")]
        public List<DialogueScenario> allScenarios = new List<DialogueScenario>();

        /// <summary>
        /// Cache dictionary for fast lookup.
        /// </summary>
        private Dictionary<string, DialogueScenario> _scenarioMap;

        private void OnEnable()
        {
            BuildMap();
        }

        /// <summary>
        /// Rebuilds the dictionary map from the list.
        /// </summary>
        public void BuildMap()
        {
            _scenarioMap = new Dictionary<string, DialogueScenario>();
            foreach (var scenario in allScenarios)
            {
                if (scenario != null && !string.IsNullOrEmpty(scenario.scenarioId))
                {
                    if (!_scenarioMap.ContainsKey(scenario.scenarioId))
                    {
                        _scenarioMap.Add(scenario.scenarioId, scenario);
                    }
                    else
                    {
                        Debug.LogWarning($"Duplicate Scenario ID found: {scenario.scenarioId} in {scenario.name}");
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves a scenario by its ID.
        /// </summary>
        /// <param name="id">The Scenario ID.</param>
        /// <returns>The DialogueScenario, or null if not found.</returns>
        public DialogueScenario GetScenarioById(string id)
        {
            if (_scenarioMap == null) BuildMap();
            
            if (_scenarioMap.TryGetValue(id, out var scenario))
            {
                return scenario;
            }
            
            Debug.LogWarning($"Scenario with ID '{id}' not found in database.");
            return null;
        }
    }
}
