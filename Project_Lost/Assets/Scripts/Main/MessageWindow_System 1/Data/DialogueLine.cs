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

        [Tooltip("Direction the speaker name should slide in from for this line. Default = use Manager setting.")]
        public NameSlideDirection nameSlideDirection = NameSlideDirection.Default;

        [Tooltip("Audio clip to play when this line is displayed")]
        public AudioClip voiceClip;
    }

    public enum NameSlideDirection
    {
        Default,
        Left,
        Right
    }
}
