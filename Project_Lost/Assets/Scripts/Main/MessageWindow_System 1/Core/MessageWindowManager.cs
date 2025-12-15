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
    public class MessageWindowManager : MonoBehaviour
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
        private bool _isKeywordEnabled = false;
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
        
        private void Update()
        {
            if (!_isWindowActive) return;

            // Detect mouse click for TMP links
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                HandleMouseClick();
            }
        }

        private void HandleMouseClick()
        {
            // タイピング中はリンク検出をスキップ
            if (!_isWindowActive || _isTyping || dialogueText == null) return;

            // マウス位置を取得
            Vector2 mousePos = Mouse.current.position.ReadValue();

            // UI カメラを取得（Canvas の RenderMode に応じて）
            Camera uiCamera = null;
            var canvasGroup = dialogueText.GetComponentInParent<Canvas>();
            if (canvasGroup != null)
            {
                if (canvasGroup.renderMode == RenderMode.ScreenSpaceCamera)
                    uiCamera = canvasGroup.worldCamera;
                else if (canvasGroup.renderMode == RenderMode.ScreenSpaceOverlay)
                    uiCamera = null; // Overlay の場合は null
            }

            // クリック位置に交差するリンクがないか確認
            int linkIndex = TMP_TextUtilities.FindIntersectingLink(dialogueText, mousePos, uiCamera);

            if (linkIndex != -1)
            {
                // リンク情報を取得
                TMP_LinkInfo linkInfo = dialogueText.textInfo.linkInfo[linkIndex];
                string linkID = linkInfo.GetLinkID();

                // キーワード反応のON/OFFによって挙動を変える
                if (_isKeywordEnabled)
                {
                    // キーワード有効状態: 通常のクリックイベントを発火し、ClueManager に処理させる
                    OnKeywordClicked?.Invoke(linkID);
                    // 既存の ClueManager サブスクライブが無くなっている可能性があるため、直接呼び出す
                    if (global::ClueManager.Instance != null)
                    {
                        global::ClueManager.Instance.ProcessKeywordClick(linkID);
                    }
                }
                else
                {
                    // キーワード無効（発見演出のみ）: ClueManager に発見を通知
                    if (global::ClueManager.Instance != null)
                    {
                        global::ClueManager.Instance.DiscoverKeyword(linkID);
                    }

                    // 視覚演出（暫定）
                    SetLinkColor(linkID, "#FFFF00");
                    ShakeLinkVisual(linkID);
                }

                Debug.Log($"リンククリック検出: {linkID} (keywordEnabled={_isKeywordEnabled})");
            }
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
            _typingCoroutine = StartCoroutine(TypeText(_currentLine.text, speed));
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
                string pattern = $"<a\\s+href\\s*=\\s*\"{Regex.Escape(id)}\"\\s*>(.*?)</a>";
                string replacement = $"<a href=\"{id}\"><color={colorHex}>$1</color></a>";
                string newText = Regex.Replace(dialogueText.text, pattern, replacement, RegexOptions.Singleline);
                dialogueText.text = newText;
                dialogueText.ForceMeshUpdate();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"SetLinkColor error for id={id}: {ex.Message}");
            }
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