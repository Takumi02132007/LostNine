using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;
using DG.Tweening;
using MessageWindowSystem.Data;
using Main.UIMoves;

namespace MessageWindowSystem.Core
{
    /// <summary>
    /// Manages the dialogue window, keyword interactions, and typing effects.
    /// </summary>
    public class MessageWindowManager : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        #region Serialized Fields

        [Header("UI References")]
        [SerializeField] private TMP_Text speakerNameText;
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private Image portraitImage;
        [SerializeField] private GameObject windowRoot;

        [Header("Typing Settings")]
        [SerializeField] private float typingSpeed = 0.05f;

        [Header("Name Slide Animation")]
        [SerializeField] private bool animateName = true;
        [SerializeField] private float nameSlideDistance = 600f;
        [SerializeField] private float nameSlideDuration = 0.35f;
        [SerializeField] private Ease nameSlideEase = Ease.OutCubic;

        [Header("Portrait Animation")]
        [SerializeField] private bool portraitJumpOnText = true;
        [SerializeField] private float portraitJumpHeight = 50f;
        [SerializeField] private float portraitJumpDuration = 0.3f;
        [SerializeField] private Ease portraitJumpEase = Ease.OutBounce;

        [Header("Skip Mode")]
        [SerializeField] private bool enableSkipMode = true;
        [SerializeField] private Key skipKey = Key.LeftCtrl;
        [SerializeField] private float skipTypingSpeed = 0.001f;

        [Header("Keyword Charge")]
        [SerializeField] private float chargeDuration = 1.0f;
        [SerializeField] private bool _isKeywordEnabled = true;

        [Header("Choice Buttons")]
        [Tooltip("Pre-attached choice buttons (max 4 typically).")]
        [SerializeField] private Button[] choiceButtons;
        [SerializeField] private TMP_Text[] choiceButtonTexts;

        #endregion

        #region Public Properties

        public static MessageWindowManager Instance { get; private set; }
        public bool IsKeywordEnabled => _isKeywordEnabled;
        public event Action<string> OnKeywordClicked;

        #endregion

        #region Private Fields

        private readonly List<(string speaker, string text)> _log = new();
        private readonly Queue<DialogueLine> _linesQueue = new();
        
        private DialogueScenario _currentScenarioData;
        private DialogueLine _currentLine;
        
        private Coroutine _typingCoroutine;
        private Coroutine _chargeCoroutine;
        
        private Vector2 _nameOriginalAnchored;
        private string _previousSpeakerName;
        private string _chargingLinkID;
        
        private bool _isTyping;
        private bool _isWindowActive;
        private bool _isCharging;
        private bool _shouldBlockNext;
        private bool _nameOriginalCaptured;
        private bool _isWaitingForChoice;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            if (windowRoot) windowRoot.SetActive(false);

            if (speakerNameText != null)
            {
                _nameOriginalAnchored = speakerNameText.rectTransform.anchoredPosition;
                _nameOriginalCaptured = true;
            }

            HideAllChoices();
        }

        #endregion

        #region Public API

        public void SetKeywordEnabled(bool enable) => _isKeywordEnabled = enable;

        public void StartScenario(DialogueScenario scenario)
        {
            if (scenario == null) return;

            // Reset choice state
            _isWaitingForChoice = false;
            HideAllChoices();

            _isKeywordEnabled = scenario.enableKeywords;
            BakePersistentColors(scenario);

            _linesQueue.Clear();
            foreach (var line in scenario.lines)
                _linesQueue.Enqueue(line);

            _currentScenarioData = scenario;

            if (windowRoot) windowRoot.SetActive(true);
            _isWindowActive = true;
            DisplayNextLine();
        }

        public void StartScenario(DialogueScenario scenario, bool enableKeywords)
        {
            SetKeywordEnabled(enableKeywords);
            StartScenario(scenario);
        }

        public void Next()
        {
            if (_isWaitingForChoice) return;
            if (_shouldBlockNext)
            {
                _shouldBlockNext = false;
                return;
            }
            SkipOrInteract();
        }

        public IReadOnlyList<(string speaker, string text)> GetLog() => _log;

        /// <summary>
        /// Called by choice buttons when selected.
        /// </summary>
        public void OnChoiceSelected(int index)
        {
            if (!_isWaitingForChoice || _currentLine?.choices == null) return;
            if (index < 0 || index >= _currentLine.choices.Count) return;

            var choice = _currentLine.choices[index];
            _isWaitingForChoice = false;
            HideAllChoices();

            if (choice.nextScenario != null)
            {
                StartScenario(choice.nextScenario);
            }
            else
            {
                DisplayNextLine();
            }
        }

        #endregion

        #region Keyword Link Methods

        public void SetLinkColor(string id, string colorHex)
        {
            if (dialogueText == null || string.IsNullOrEmpty(id) || _currentScenarioData == null) return;

            bool currentLineChanged = false;
            string pattern = BuildLinkPattern(id);

            foreach (var line in _currentScenarioData.lines)
            {
                if (string.IsNullOrEmpty(line.text) || !Regex.IsMatch(line.text, pattern)) continue;

                string newText = Regex.Replace(line.text, pattern, m =>
                {
                    string stripped = StripColorTags(m.Groups[1].Value);
                    return $"<a href=\"{id}\"><color={colorHex}>{stripped}</color></a>";
                }, RegexOptions.Singleline);

                if (line.text != newText)
                {
                    line.text = newText;
                    if (line == _currentLine) currentLineChanged = true;
                }
            }

            if (currentLineChanged) RefreshDialogueText();
        }

        public void ResetKeywordState(string id)
        {
            if (string.IsNullOrEmpty(id)) return;

            ClueManager.Instance?.ResetKeywordStatus(id);

            if (_currentScenarioData == null) return;

            bool currentLineChanged = false;
            string pattern = BuildLinkPattern(id);

            foreach (var line in _currentScenarioData.lines)
            {
                if (string.IsNullOrEmpty(line.text) || !Regex.IsMatch(line.text, pattern)) continue;

                string newText = Regex.Replace(line.text, pattern, m =>
                {
                    string stripped = StripColorTags(m.Groups[1].Value);
                    return $"<a href=\"{id}\">{stripped}</a>";
                }, RegexOptions.Singleline);

                if (line.text != newText)
                {
                    line.text = newText;
                    if (line == _currentLine) currentLineChanged = true;
                }
            }

            if (currentLineChanged) RefreshDialogueText();
        }

        public void ShakeLinkVisual(string id)
        {
            dialogueText?.GetComponent<RectTransform>()?.DOShakeAnchorPos(0.35f, new Vector2(8f, 0f), 10, 90f);
        }

        public void StartKeywordConversation(string id)
        {
            var ds = Resources.Load<DialogueScenario>($"KeywordConversations/{id}");
            if (ds != null) StartScenario(ds);
        }

        #endregion

        #region Pointer Handlers (Keyword Charge)

        public void OnPointerDown(PointerEventData eventData)
        {
            _shouldBlockNext = false;

            if (!_isWindowActive || _isTyping || !_isKeywordEnabled) return;

            Camera uiCamera = GetUICamera();
            int linkIndex = TMP_TextUtilities.FindIntersectingLink(dialogueText, eventData.position, uiCamera);

            if (linkIndex == -1) return;

            _shouldBlockNext = true;
            _chargingLinkID = dialogueText.textInfo.linkInfo[linkIndex].GetLinkID();
            _isCharging = true;
            _chargeCoroutine = StartCoroutine(ChargeRoutine(linkIndex, _chargingLinkID));
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_isCharging) CancelCharge();
        }

        private void CancelCharge()
        {
            _isCharging = false;
            if (_chargeCoroutine != null) StopCoroutine(_chargeCoroutine);

            if (!string.IsNullOrEmpty(_chargingLinkID))
                dialogueText.ForceMeshUpdate();

            EffectManager.Instance?.StopChargeSE();
            _chargingLinkID = null;
        }

        private IEnumerator ChargeRoutine(int linkIndex, string linkID)
        {
            EffectManager.Instance?.PlayChargeSE();

            var linkInfo = dialogueText.textInfo.linkInfo[linkIndex];
            int startCharIdx = linkInfo.linkTextfirstCharacterIndex;
            int charCount = linkInfo.linkTextLength;

            var originalColors = new Color32[charCount];
            var originalVertices = new Vector3[charCount][];

            CacheCharacterData(startCharIdx, charCount, originalColors, originalVertices);

            Color32 targetColor = new Color32(255, 215, 0, 255);
            const float maxScale = 1.5f;

            float timer = 0f;
            while (timer < chargeDuration)
            {
                timer += Time.deltaTime;
                float progress = timer / chargeDuration;
                float easedProgress = DOVirtual.EasedValue(0f, 1f, progress, Ease.OutQuad);
                float scale = Mathf.Lerp(1f, maxScale, easedProgress);

                ApplyChargeVisuals(startCharIdx, charCount, originalColors, originalVertices, targetColor, progress, scale);
                yield return null;
            }

            RestoreVertices(startCharIdx, charCount, originalVertices);

            _isCharging = false;
            EffectManager.Instance?.StopChargeSE();
            EffectManager.Instance?.PlayDevelopmentEffect();

            OnKeywordClicked?.Invoke(linkID);
            ClueManager.Instance?.ProcessKeywordClick(linkID);
        }

        #endregion

        #region Dialogue Flow

        private void SkipOrInteract()
        {
            if (_isCharging) return;

            if (_isTyping)
            {
                if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
                _isTyping = false;
                dialogueText.text = _currentLine.text;
                dialogueText.maxVisibleCharacters = _currentLine.text.Length;
                return;
            }

            DisplayNextLine();
        }

        private void DisplayNextLine()
        {
            if (_linesQueue.Count == 0)
            {
                // Check for loop
                if (_currentScenarioData != null && _currentScenarioData.loopScenario)
                {
                    StartScenario(_currentScenarioData);
                    return;
                }

                // Check for next scenario chain
                if (_currentScenarioData?.nextScenario != null)
                {
                    StartScenario(_currentScenarioData.nextScenario);
                    return;
                }

                EndScenario();
                return;
            }

            _currentLine = _linesQueue.Dequeue();
            string speakerName = _currentLine.speakerName ?? string.Empty;

            if (speakerNameText) speakerNameText.text = speakerName;
            _log.Add((speakerName, _currentLine.text));

            UpdatePortrait();
            PlayEffects();
            AnimateSpeakerName(speakerName);

            _previousSpeakerName = speakerName;

            if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
            float speed = _currentLine.typingSpeed > 0 ? _currentLine.typingSpeed : typingSpeed;
            _typingCoroutine = StartCoroutine(TypeText(_currentLine.text, speed));
        }

        private void EndScenario()
        {
            _isWindowActive = false;
            if (windowRoot) windowRoot.SetActive(false);

            // Call ToggleComu if configured
            if (_currentScenarioData != null && _currentScenarioData.toggleComuOnEnd)
            {
                var comuManager = FindAnyObjectByType<ComuStartandEndManager>();
                comuManager?.ToggleComu();
            }
        }

        private IEnumerator TypeText(string text, float speed)
        {
            _isTyping = true;
            dialogueText.text = text;
            dialogueText.maxVisibleCharacters = 0;
            dialogueText.ForceMeshUpdate();

            int total = dialogueText.textInfo.characterCount;
            for (int i = 0; i <= total; i++)
            {
                float step = (enableSkipMode && Keyboard.current?[skipKey].isPressed == true) ? skipTypingSpeed : speed;
                dialogueText.maxVisibleCharacters = i;
                yield return new WaitForSeconds(step);
            }

            _isTyping = false;

            // Show choices if this line has any
            if (_currentLine?.choices != null && _currentLine.choices.Count > 0)
            {
                Debug.Log($"[MWM] Showing {_currentLine.choices.Count} choices");
                ShowChoices(_currentLine.choices);
            }
            else
            {
                Debug.Log($"[MWM] No choices for this line. choices={_currentLine?.choices}, count={_currentLine?.choices?.Count ?? 0}");
            }
        }

        #endregion

        #region Choice Methods

        private void ShowChoices(List<ChoiceData> choices)
        {
            _isWaitingForChoice = true;

            for (int i = 0; i < choiceButtons.Length; i++)
            {
                if (i < choices.Count)
                {
                    choiceButtons[i].gameObject.SetActive(true);
                    if (choiceButtonTexts != null && i < choiceButtonTexts.Length)
                        choiceButtonTexts[i].text = choices[i].choiceText;

                    int index = i;
                    choiceButtons[i].onClick.RemoveAllListeners();
                    choiceButtons[i].onClick.AddListener(() => OnChoiceSelected(index));
                }
                else
                {
                    choiceButtons[i].gameObject.SetActive(false);
                }
            }
        }

        private void HideAllChoices()
        {
            if (choiceButtons == null) return;
            foreach (var btn in choiceButtons)
                btn?.gameObject.SetActive(false);
        }

        #endregion

        #region Helper Methods

        private void BakePersistentColors(DialogueScenario scenario)
        {
            if (ClueManager.Instance == null || scenario == null) return;

            foreach (var line in scenario.lines)
            {
                if (string.IsNullOrEmpty(line.text)) continue;

                line.text = Regex.Replace(line.text, @"<a\s+href\s*=\s*""(.*?)""\s*>(.*?)</a>", m =>
                {
                    string id = m.Groups[1].Value;
                    string stripped = StripColorTags(m.Groups[2].Value);
                    string colorTag = ClueManager.Instance.IsClicked(id) ? "<color=#888888>" : "";
                    string closeTag = ClueManager.Instance.IsClicked(id) ? "</color>" : "";
                    return $"<a href=\"{id}\">{colorTag}{stripped}{closeTag}</a>";
                }, RegexOptions.Singleline);
            }
        }

        private void UpdatePortrait()
        {
            if (portraitImage == null) return;

            if (_currentLine.portrait != null)
            {
                portraitImage.sprite = _currentLine.portrait;
                portraitImage.gameObject.SetActive(true);
                if (portraitJumpOnText) PlayPortraitJump();
            }
            else
            {
                portraitImage.gameObject.SetActive(false);
            }
        }

        private void PlayEffects()
        {
            if (_currentLine.effects == null || EffectManager.Instance == null) return;
            foreach (var effect in _currentLine.effects)
                EffectManager.Instance.PlayEffect(effect);
        }

        private void AnimateSpeakerName(string newName)
        {
            if (!animateName || speakerNameText == null) return;
            if (string.Equals(newName, _previousSpeakerName, StringComparison.Ordinal)) return;

            var rt = speakerNameText.rectTransform;
            if (!_nameOriginalCaptured) { _nameOriginalAnchored = rt.anchoredPosition; _nameOriginalCaptured = true; }

            bool fromRight = _currentLine.nameSlideDirection switch
            {
                NameSlideDirection.Left => false,
                NameSlideDirection.Right => true,
                _ => false
            };

            float dir = fromRight ? 1f : -1f;
            rt.anchoredPosition = _nameOriginalAnchored + new Vector2(dir * nameSlideDistance, 0f);

            MoveWithEasing.MoveToAnchored(speakerNameText.gameObject, _nameOriginalAnchored, new MoveWithEasing.MoveOptions
            {
                duration = nameSlideDuration,
                ease = nameSlideEase,
                shakeOnComplete = false,
                endAlpha = 1f
            });
        }

        private void PlayPortraitJump()
        {
            var rect = portraitImage.GetComponent<RectTransform>();
            if (rect == null) return;

            var original = rect.anchoredPosition;
            var jumpTarget = original + Vector2.up * portraitJumpHeight;

            rect.DOAnchorPos(jumpTarget, portraitJumpDuration * 0.5f).SetEase(Ease.OutQuad);
            rect.DOAnchorPos(original, portraitJumpDuration * 0.5f).SetEase(portraitJumpEase).SetDelay(portraitJumpDuration * 0.5f);
        }

        private Camera GetUICamera()
        {
            var canvas = dialogueText.GetComponentInParent<Canvas>();
            return canvas.renderMode == RenderMode.ScreenSpaceCamera ? canvas.worldCamera : null;
        }

        private void RefreshDialogueText()
        {
            dialogueText.text = _currentLine.text;
            dialogueText.ForceMeshUpdate();
        }

        private static string BuildLinkPattern(string id) => $@"<a\s+href\s*=\s*""{Regex.Escape(id)}""\s*>(.*?)</a>";
        private static string StripColorTags(string content) => Regex.Replace(content, "</?color[^>]*>", "");

        private void CacheCharacterData(int start, int count, Color32[] colors, Vector3[][] vertices)
        {
            for (int i = 0; i < count; i++)
            {
                var charInfo = dialogueText.textInfo.characterInfo[start + i];
                var mesh = dialogueText.textInfo.meshInfo[charInfo.materialReferenceIndex];
                int vi = charInfo.vertexIndex;

                colors[i] = mesh.colors32[vi];
                vertices[i] = new[] { mesh.vertices[vi], mesh.vertices[vi + 1], mesh.vertices[vi + 2], mesh.vertices[vi + 3] };
            }
        }

        private void ApplyChargeVisuals(int start, int count, Color32[] origColors, Vector3[][] origVerts, Color32 target, float progress, float scale)
        {
            for (int i = 0; i < count; i++)
            {
                var charInfo = dialogueText.textInfo.characterInfo[start + i];
                if (!charInfo.isVisible) continue;

                var mesh = dialogueText.textInfo.meshInfo[charInfo.materialReferenceIndex];
                int vi = charInfo.vertexIndex;

                var c = Color32.Lerp(origColors[i], target, progress);
                mesh.colors32[vi] = mesh.colors32[vi + 1] = mesh.colors32[vi + 2] = mesh.colors32[vi + 3] = c;

                Vector3 center = (origVerts[i][0] + origVerts[i][2]) / 2;
                for (int v = 0; v < 4; v++)
                    mesh.vertices[vi + v] = center + (origVerts[i][v] - center) * scale;
            }

            dialogueText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32 | TMP_VertexDataUpdateFlags.Vertices);
        }

        private void RestoreVertices(int start, int count, Vector3[][] origVerts)
        {
            for (int i = 0; i < count; i++)
            {
                var charInfo = dialogueText.textInfo.characterInfo[start + i];
                if (!charInfo.isVisible) continue;

                var mesh = dialogueText.textInfo.meshInfo[charInfo.materialReferenceIndex];
                int vi = charInfo.vertexIndex;

                for (int v = 0; v < 4; v++)
                    mesh.vertices[vi + v] = origVerts[i][v];
            }
            dialogueText.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
        }

        #endregion
    }
}