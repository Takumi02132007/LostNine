using System.Collections.Generic;
using UnityEngine;

namespace MessageWindowSystem.Data
{
    [System.Serializable]
    public class DialogueLine
    {
        [Tooltip("Name of the speaker")]
        public string speakerName;
        
        [Tooltip("Portrait to display")]
        public Sprite portrait;
        
        [TextArea(3, 10)]
        [Tooltip("Dialogue text. Supports TMP tags.")]
        public string text;
        
        [Tooltip("Custom actions to trigger (e.g., 'Shake', 'Flash')")]
        public List<string> customActions;
        
        [Tooltip("Typing speed for this line (seconds per character). 0 = use default from Manager")]
        public float typingSpeed = 0f;
    }
}
