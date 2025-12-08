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

namespace MessageWindowSystem.Core
{
    public class MessageWindowManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text speakerNameText;
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private Image portraitImage;
        [SerializeField] private GameObject windowRoot;

        [Header("Settings")]
        [SerializeField] private float typingSpeed = 0.05f;

        [Header("Name Slide In")]
        [SerializeField] private bool animateName = true;
        [SerializeField] private bool slideFromRight = false;
        [SerializeField] private float nameSlideDistance = 600f;
        [SerializeField] private float nameSlideDuration = 0.35f;
        [SerializeField] private DG.Tweening.Ease nameSlideEase = DG.Tweening.Ease.OutCubic;

        [Header("Portrait Jump")]
        [SerializeField] private bool portraitJumpOnText = true;
        [SerializeField] private float portraitJumpHeight = 50f;
        [SerializeField] private float portraitJumpDuration = 0.3f;
        [SerializeField] private DG.Tweening.Ease portraitJumpEase = DG.Tweening.Ease.OutBounce;

        [Header("Skip Mode")]
        [SerializeField] private bool enableSkipMode = true;
        [SerializeField] private Key skipKey = Key.LeftCtrl;
        [SerializeField] private float skipTypingSpeed = 0.001f;

        // Log
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

        public static MessageWindowManager Instance { get; private set; }

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

        public void StartScenario(DialogueScenario scenario)
        {
            if (scenario == null) return;

            _linesQueue.Clear();
            foreach (var line in scenario.lines)
                _linesQueue.Enqueue(line);

            if (windowRoot) windowRoot.SetActive(true);
            _isWindowActive = true;
            DisplayNextLine();
        }

        // ← Updateでの入力検出は廃止（ボタン操作のみ進行）
        private void Update()
        {
            if (!_isWindowActive) return;
        }

        // OnClick からのみ呼ばれる専用関数
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

        private void DisplayNextLine()
        {
            if (_linesQueue.Count == 0)
            {
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

            if (_currentLine.customActions != null)
            {
                foreach (var action in _currentLine.customActions)
                {
                    if (EffectManager.Instance) EffectManager.Instance.PlayEffect(action);
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

        private void EndScenario()
        {
            _isWindowActive = false;
            if (windowRoot) windowRoot.SetActive(false);
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

        public IReadOnlyList<(string speaker, string text)> GetLog()
        {
            return _log;
        }
    }
}
