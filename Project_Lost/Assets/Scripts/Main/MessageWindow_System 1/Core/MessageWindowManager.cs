using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using MessageWindowSystem.Data;
using UnityEngine.InputSystem;
using Main.UIMoves;
using DG.Tweening;
using UnityEngine.EventSystems;
using System.Text.RegularExpressions;

namespace MessageWindowSystem.Core
{
    public class MessageWindowManager : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("UI References")]
        [Tooltip("Text component for the speaker's name.")]
        [SerializeField] private TMP_Text speakerNameText;
        [Tooltip("Text component for the dialogue content.")]
        [SerializeField] private TMP_Text dialogueText;
        [Tooltip("Image component for the character's portrait.")]
        [SerializeField] private Image portraitImage;
        [Tooltip("Root GameObject of the message window (to show/hide).")]
        [SerializeField] private GameObject windowRoot;

        [Header("Settings")]
        [Tooltip("Default typing speed (seconds per character).")]
        [SerializeField] private float typingSpeed = 0.05f;

        [Header("Name Slide In")]
        [Tooltip("Enable sliding animation for the speaker's name.")]
        [SerializeField] private bool animateName = true;
        private bool slideFromRight = false;
        [Tooltip("Distance for the name slide animation.")]
        [SerializeField] private float nameSlideDistance = 600f;
        [Tooltip("Duration of the name slide animation.")]
        [SerializeField] private float nameSlideDuration = 0.35f;
        [SerializeField] private DG.Tweening.Ease nameSlideEase = DG.Tweening.Ease.OutCubic;

        [Header("Portrait Jump")]
        [Tooltip("Enable jumping animation for the portrait when text appears.")]
        [SerializeField] private bool portraitJumpOnText = true;
        [SerializeField] private float portraitJumpHeight = 50f;
        [SerializeField] private float portraitJumpDuration = 0.3f;
        [SerializeField] private DG.Tweening.Ease portraitJumpEase = DG.Tweening.Ease.OutBounce;

        [Header("Skip Mode")]
        [Tooltip("Enable skipping typing by holding a key.")]
        [SerializeField] private bool enableSkipMode = true;
        [SerializeField] private Key skipKey = Key.LeftCtrl;
        [SerializeField] private float skipTypingSpeed = 0.001f;

        // Log of conversation history
        private List<(string speaker, string text)> _log = new();

        private Queue<DialogueLine> _linesQueue = new Queue<DialogueLine>();
        private DialogueLine _currentLine;
        private bool _isTyping;
        private Coroutine _typingCoroutine;
        private Vector2 _nameOriginalAnchored;
        private bool _nameOriginalCaptured = false;
        private string _previousSpeakerName = null;
        private Vector3 _portraitOriginalPosition;
        private bool _isWindowActive = false;
        [SerializeField] private ComuStartandEndManager comuStartandEndManager;

        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static MessageWindowManager Instance { get; private set; }

        // ★修正: 外部に通知するのは、クリックされたリンクのID（文字列）
        public event Action<string> OnKeywordClicked; 
        
        // キーワードのクリック有効フラグ（外部から制御）
        // キーワードのクリック有効フラグ（外部から制御）
        private bool _isKeywordEnabled = true;
        public bool IsKeywordEnabled => _isKeywordEnabled;

        // 会話開始時などにキーワード反応を切り替えるための公開API
        public void SetKeywordEnabled(bool enable)
        {
            _isKeywordEnabled = enable;
        }
        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            if (windowRoot) windowRoot.SetActive(false);

            if (speakerNameText != null)
            {
                var rt = speakerNameText.rectTransform;
                if (rt != null)
                {
                    _nameOriginalAnchored = rt.anchoredPosition;
                    _nameOriginalCaptured = true;
                }
            }

            if (portraitImage != null)
            {
                _portraitOriginalPosition = portraitImage.transform.position;
            }

            // Force enable keywords by default
            _isKeywordEnabled = true;
        }

        /// <summary>
        /// Starts a new dialogue scenario.
        /// </summary>
        /// <param name="scenario">The scenario data to play.</param>
        public void StartScenario(DialogueScenario scenario)
        {
            if (scenario == null) return;

            // Reset state
            _linesQueue.Clear();
            foreach (var line in scenario.lines)
                _linesQueue.Enqueue(line);

            // Store current scenario to check for 'NextScenario' later
            _currentScenarioData = scenario; 

            if (windowRoot) windowRoot.SetActive(true);
            _isWindowActive = true;
            DisplayNextLine();
        }

        private DialogueScenario _currentScenarioData; // Added field to track current scenario
        
        // --- Hold Interaction Variables ---
        private Coroutine _chargeCoroutine;
        private bool _isCharging;
        private string _chargingLinkID;
        [Tooltip("Time required to hold the link to develop it.")]
        [SerializeField] private float chargeDuration = 1.0f;
        
        private void Update()
        {
            // Update logic if needed for constant checking, currently mostly event driven
            // Check for mouse hover outside of pointer down if needed for "Hint"
            if (_isWindowActive && !_isTyping && _isKeywordEnabled)
            {
               // TODO: Hover optimization if needed. PointerEnter/Exit on TMPro links is tricky without custom components.
               // For now, PointerDown handles the interaction start.
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            Debug.Log($"OnPointerDown Detected. Active: {_isWindowActive}, Typing: {_isTyping}, KeywordsEnabled: {_isKeywordEnabled}");

            if (!_isWindowActive || _isTyping || !_isKeywordEnabled) 
            {
               if (!_isKeywordEnabled) Debug.Log("Keyword interaction ignored: Keywords are disabled.");
               return;
            }
            
            // Detect link under pointer
            int linkIndex = TMP_TextUtilities.FindIntersectingLink(dialogueText, eventData.position, null); // Pass null for camera if Overlay or handle correctly
            
            // Fix: TMP_TextUtilities needs correct camera reference
            Camera uiCamera = null;
            var canvas = dialogueText.GetComponentInParent<Canvas>();
            if (canvas.renderMode == RenderMode.ScreenSpaceCamera) uiCamera = canvas.worldCamera;
            
            linkIndex = TMP_TextUtilities.FindIntersectingLink(dialogueText, eventData.position, uiCamera);

            if (linkIndex != -1)
            {
                TMP_LinkInfo linkInfo = dialogueText.textInfo.linkInfo[linkIndex];
                string linkID = linkInfo.GetLinkID();
                
                // Start Charge
                _chargingLinkID = linkID;
                _isCharging = true;
                _chargeCoroutine = StartCoroutine(ChargeRoutine(linkIndex, linkID));
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            // Cancel Charge
            if (_isCharging)
            {
                CancelCharge();
            }
        }

        private void CancelCharge()
        {
            _isCharging = false;
            if (_chargeCoroutine != null) StopCoroutine(_chargeCoroutine);
            
            // Reset Visuals
             if (!string.IsNullOrEmpty(_chargingLinkID))
             {
                 // Reset color logic if needed or restore original color
                 // Just refreshing text to clear partial effects might be simple
                 dialogueText.ForceMeshUpdate(); 
             }

            if (EffectManager.Instance) EffectManager.Instance.StopChargeSE();
            _chargingLinkID = null;
        }

        private IEnumerator ChargeRoutine(int linkIndex, string linkID)
        {
            if (EffectManager.Instance) EffectManager.Instance.PlayChargeSE();

            // Get link info to manipulate vertices
            TMP_LinkInfo linkInfo = dialogueText.textInfo.linkInfo[linkIndex];
            int startCharIdx = linkInfo.linkTextfirstCharacterIndex;
            int charCount = linkInfo.linkTextLength;

            // Cache original colors and vertices to restore later if cancelled
            Color32[] originalColors = new Color32[charCount];
            Vector3[][] originalVerticesByChar = new Vector3[charCount][];

            for (int i = 0; i < charCount; i++)
            {
                int charIdx = startCharIdx + i;
                int materialIndex = dialogueText.textInfo.characterInfo[charIdx].materialReferenceIndex;
                int vertexIndex = dialogueText.textInfo.characterInfo[charIdx].vertexIndex;
                
                // Cache Colors
                originalColors[i] = dialogueText.textInfo.meshInfo[materialIndex].colors32[vertexIndex];

                // Cache Vertices (4 per char)
                originalVerticesByChar[i] = new Vector3[4];
                var srcVertices = dialogueText.textInfo.meshInfo[materialIndex].vertices;
                originalVerticesByChar[i][0] = srcVertices[vertexIndex + 0];
                originalVerticesByChar[i][1] = srcVertices[vertexIndex + 1];
                originalVerticesByChar[i][2] = srcVertices[vertexIndex + 2];
                originalVerticesByChar[i][3] = srcVertices[vertexIndex + 3];
            }

            Color32 targetColor = new Color32(255, 215, 0, 255); // Gold
            float maxScale = 1.5f; // Max expansion
            
            float timer = 0f;
            while (timer < chargeDuration)
            {
                timer += Time.deltaTime;
                float progress = timer / chargeDuration;

                // Use DOTween's helper for easing
                float easedProgress = DOVirtual.EasedValue(0f, 1f, progress, Ease.OutQuad);
                float currentScale = Mathf.Lerp(1.0f, maxScale, easedProgress);

                for (int i = 0; i < charCount; i++)
                {
                    int charIdx = startCharIdx + i;
                    // Skip if character is not being rendered (e.g. space)
                    if (!dialogueText.textInfo.characterInfo[charIdx].isVisible) continue;

                    int materialIndex = dialogueText.textInfo.characterInfo[charIdx].materialReferenceIndex;
                    int vertexIndex = dialogueText.textInfo.characterInfo[charIdx].vertexIndex;

                    Color32[] destinationColors = dialogueText.textInfo.meshInfo[materialIndex].colors32;
                    Vector3[] destinationVertices = dialogueText.textInfo.meshInfo[materialIndex].vertices;

                    // 1. Apply Color
                    Color32 c = Color32.Lerp(originalColors[i], targetColor, progress);
                    destinationColors[vertexIndex + 0] = c;
                    destinationColors[vertexIndex + 1] = c;
                    destinationColors[vertexIndex + 2] = c;
                    destinationColors[vertexIndex + 3] = c;

                    // 2. Apply Scale
                    // Calculate center of the character based on original vertices
                    Vector3 center = (originalVerticesByChar[i][0] + originalVerticesByChar[i][2]) / 2;
                    
                    for (int v = 0; v < 4; v++)
                    {
                        Vector3 originalPos = originalVerticesByChar[i][v];
                        Vector3 dir = originalPos - center;
                        destinationVertices[vertexIndex + v] = center + (dir * currentScale);
                    }
                }

                // Apply changes
                dialogueText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
                dialogueText.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);

                yield return null;
            }

            // Snap back size (Restore vertices to original positions)
            for (int i = 0; i < charCount; i++)
            {
                int charIdx = startCharIdx + i;
                if (!dialogueText.textInfo.characterInfo[charIdx].isVisible) continue;

                int materialIndex = dialogueText.textInfo.characterInfo[charIdx].materialReferenceIndex;
                int vertexIndex = dialogueText.textInfo.characterInfo[charIdx].vertexIndex;
                
                Vector3[] destinationVertices = dialogueText.textInfo.meshInfo[materialIndex].vertices;
                
                // Restore from cache
                destinationVertices[vertexIndex + 0] = originalVerticesByChar[i][0];
                destinationVertices[vertexIndex + 1] = originalVerticesByChar[i][1];
                destinationVertices[vertexIndex + 2] = originalVerticesByChar[i][2];
                destinationVertices[vertexIndex + 3] = originalVerticesByChar[i][3];
            }
            dialogueText.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);

            // Complete
            _isCharging = false;
            if (EffectManager.Instance) EffectManager.Instance.StopChargeSE();
            
            // Development Effect
            if (EffectManager.Instance) 
            {
                EffectManager.Instance.PlayDevelopmentEffect(() => {
                     // On Effect Complete
                });
            }

            // Data Logic
            OnKeywordClicked?.Invoke(linkID);
            if (global::ClueManager.Instance != null)
            {
                global::ClueManager.Instance.ProcessKeywordClick(linkID);
            }
            
            Debug.Log($"Keyword Developed: {linkID}");
        }

        // 既存の StartScenario にキーワードの有効フラグを渡すオーバーロード
        public void StartScenario(DialogueScenario scenario, bool enableKeywords)
        {
            SetKeywordEnabled(enableKeywords);
            StartScenario(scenario);
        }
        private void EndScenario()
        {
            Debug.Log("Dialogue scenario ended.");
            _isWindowActive = false;
            if (windowRoot) windowRoot.SetActive(false);
        
            //if(comuStartandEndManager == null) return;
            //comuStartandEndManager.ComuEnd();
        }

        // OnClick からのみ呼ばれる専用関数
        /// <summary>
        /// Proceeds to the next line or skips the current typing effect.
        /// Should be called by UI buttons or input events.
        /// </summary>
        public void Next()
        {
            SkipOrInteract();
        }

        private void SkipOrInteract()
        {
            // Keywordを長押し中は進まない
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

        /// <summary>
        /// Dequeues the next line and updates the UI.
        /// Ends the scenario if no lines remain.
        /// </summary>
        private void DisplayNextLine()
        {
            if (_linesQueue.Count == 0)
            {
                // Check if there is a next scenario to chain per current data
                if (_currentScenarioData != null && _currentScenarioData.nextScenario != null)
                {
                    Debug.Log($"Chaining to next scenario: {_currentScenarioData.nextScenario.name}");
                    StartScenario(_currentScenarioData.nextScenario);
                    return;
                }

                EndScenario();
                return;
            }

            _currentLine = _linesQueue.Dequeue();

            string newSpeakerName = _currentLine.speakerName ?? string.Empty;
            if (speakerNameText) speakerNameText.text = newSpeakerName;

            _log.Add((newSpeakerName, _currentLine.text));

            if (portraitImage)
            {
                if (_currentLine.portrait != null)
                {
                    portraitImage.sprite = _currentLine.portrait;
                    portraitImage.gameObject.SetActive(true);

                    if (portraitJumpOnText)
                        PlayPortraitJump();
                }
                else
                {
                    portraitImage.gameObject.SetActive(false);
                }
            }

            if (_currentLine.effects != null)
            {
                foreach (var effectData in _currentLine.effects)
                {
                    if (EffectManager.Instance) EffectManager.Instance.PlayEffect(effectData); 
                }
            }

            if (animateName && speakerNameText != null)
            {
                if (!string.Equals(newSpeakerName, _previousSpeakerName, StringComparison.Ordinal))
                {
                    var rt = speakerNameText.rectTransform;
                    if (rt != null)
                    {
                        if (!_nameOriginalCaptured)
                        {
                            _nameOriginalAnchored = rt.anchoredPosition;
                            _nameOriginalCaptured = true;
                        }

                        bool useSlideFromRight = slideFromRight;
                        switch (_currentLine.nameSlideDirection)
                        {
                            case NameSlideDirection.Left: useSlideFromRight = false; break;
                            case NameSlideDirection.Right: useSlideFromRight = true; break;
                        }

                        float dir = useSlideFromRight ? 1f : -1f;
                        var startPos = _nameOriginalAnchored + new Vector2(dir * nameSlideDistance, 0f);
                        rt.anchoredPosition = startPos;

                        var opts = new MoveWithEasing.MoveOptions
                        {
                            duration = nameSlideDuration,
                            ease = nameSlideEase,
                            shakeOnComplete = false,
                            endAlpha = 1f
                        };

                        MoveWithEasing.MoveToAnchored(speakerNameText.gameObject, _nameOriginalAnchored, opts);
                    }
                }
            }

            _previousSpeakerName = newSpeakerName;

            if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
            float speed = _currentLine.typingSpeed > 0 ? _currentLine.typingSpeed : typingSpeed;
            
            // Apply persistent colors (grey out clicked keywords)
            string finalText = ApplyPersistentColors(_currentLine.text);
            
            _typingCoroutine = StartCoroutine(TypeText(finalText, speed));
        }

        private IEnumerator TypeText(string text, float speed)
        {
            _isTyping = true;
            dialogueText.text = text;
            dialogueText.maxVisibleCharacters = 0;

            dialogueText.ForceMeshUpdate();
            int totalVisibleCharacters = dialogueText.textInfo.characterCount;

            for (int i = 0; i <= totalVisibleCharacters; i++)
            {
                float step = (enableSkipMode && Keyboard.current != null && Keyboard.current[skipKey].isPressed)
                    ? skipTypingSpeed
                    : speed;

                dialogueText.maxVisibleCharacters = i;
                yield return new WaitForSeconds(step);
            }

            _isTyping = false;
        }

        private void PlayPortraitJump()
        {
            if (portraitImage == null) return;

            var portraitRect = portraitImage.GetComponent<RectTransform>();
            if (portraitRect == null) return;

            var originalAnchoredPos = portraitRect.anchoredPosition;
            var jumpPos = originalAnchoredPos + Vector2.up * portraitJumpHeight;

            DOTween.To(
                () => portraitRect.anchoredPosition,
                x => portraitRect.anchoredPosition = x,
                jumpPos,
                portraitJumpDuration * 0.5f
            ).SetEase(Ease.OutQuad);

            DOTween.To(
                () => portraitRect.anchoredPosition,
                x => portraitRect.anchoredPosition = x,
                originalAnchoredPos,
                portraitJumpDuration * 0.5f
            ).SetDelay(portraitJumpDuration * 0.5f)
             .SetEase(portraitJumpEase);
        }

        // キーワードリンクの色を変更する（テキスト内の <a href="id">...</a> の内部を色付け）
        public void SetLinkColor(string id, string colorHex)
        {
            if (dialogueText == null || string.IsNullOrEmpty(id)) return;

            try
            {
                // 1. まずターゲットリンクの中身を取得
                string pattern = $"<a\\s+href\\s*=\\s*\"{Regex.Escape(id)}\"\\s*>(.*?)</a>";
                
                // 2. 既存のカラータグがあれば除去して、新しい色で包む
                // Note: ネストを防ぐため、一旦タグの中身から <color> タグを外す（簡易的）
                string newText = Regex.Replace(dialogueText.text, pattern, (match) =>
                {
                    string content = match.Groups[1].Value;
                    string stripped = Regex.Replace(content, "</?color[^>]*>", ""); 
                    return $"<a href=\"{id}\"><color={colorHex}>{stripped}</color></a>";
                }, RegexOptions.Singleline);

                dialogueText.text = newText;
                dialogueText.ForceMeshUpdate();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"SetLinkColor error for id={id}: {ex.Message}");
            }
        }

        // キーワードの状態（既読・色）を完全にリセットする
        public void ResetKeywordState(string id)
        {
            if (string.IsNullOrEmpty(id)) return;

            // 1. データ上のリセット
            if (global::ClueManager.Instance != null)
            {
                global::ClueManager.Instance.ResetKeywordStatus(id);
            }

            // 2. ビジュアル（テキスト色）のリセット
            if (dialogueText != null)
            {
                try
                {
                    string pattern = $"<a\\s+href\\s*=\\s*\"{Regex.Escape(id)}\"\\s*>(.*?)</a>";
                    string newText = Regex.Replace(dialogueText.text, pattern, (match) =>
                    {
                        string content = match.Groups[1].Value;
                        string stripped = Regex.Replace(content, "</?color[^>]*>", "");
                        return $"<a href=\"{id}\">{stripped}</a>";
                    }, RegexOptions.Singleline);

                    dialogueText.text = newText;
                    dialogueText.ForceMeshUpdate();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"ResetKeywordState error for id={id}: {ex.Message}");
                }
            }
        }
        
        // テキスト内の全リンクをチェックし、既読（Clicked）状態なら色を適用する
        private string ApplyPersistentColors(string text)
        {
            if (global::ClueManager.Instance == null) return text;

            // Extract all link IDs from the text? Or simpler: 
            // Since we don't know which IDs are in the text without parsing,
            // we can iterate matches of <a> tags.
            
            return Regex.Replace(text, "<a\\s+href\\s*=\\s*\"(.*?)\"\\s*>(.*?)</a>", (match) =>
            {
                string id = match.Groups[1].Value;
                string content = match.Groups[2].Value;

                if (global::ClueManager.Instance.IsClicked(id))
                {
                     // Strip potential old colors and apply grey
                     string stripped = Regex.Replace(content, "</?color[^>]*>", "");
                     return $"<a href=\"{id}\"><color=#888888>{stripped}</color></a>";
                }
                return match.Value; // No change
            }, RegexOptions.Singleline);
        }

        // 簡易的にテキストを震わせる（UI 全体を短時間だけ揺らす）
        public void ShakeLinkVisual(string id)
        {
            if (dialogueText == null) return;
            var rt = dialogueText.GetComponent<RectTransform>();
            if (rt == null) return;

            rt.DOShakeAnchorPos(0.35f, new Vector2(8f, 0f), 10, 90f);
        }

        // キーワードに関連する特別会話を開始する（Resources に DialogueScenario を配置している場合にロード）
        public void StartKeywordConversation(string id)
        {
            Debug.Log($"StartKeywordConversation requested: {id}");
            // 例: Resources/KeywordConversations/{id}.asset
            var ds = Resources.Load<MessageWindowSystem.Data.DialogueScenario>($"KeywordConversations/{id}");
            if (ds != null)
            {
                Debug.Log($"Found keyword conversation asset for {id}, starting scenario.");
                StartScenario(ds);
            }
            else
            {
                Debug.Log($"No keyword conversation asset found for {id}.");
            }
        }

        public IReadOnlyList<(string speaker, string text)> GetLog()
        {
            return _log;
        }
    }
}