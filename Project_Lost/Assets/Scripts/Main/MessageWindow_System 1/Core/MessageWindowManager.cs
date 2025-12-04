using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using MessageWindowSystem.Data;
using UnityEngine.InputSystem;
using Main.UIMoves;

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
        [Space]
        [Header("Name Slide In")]
        [SerializeField] private bool animateName = true;
        [SerializeField] private bool slideFromRight = false;
        [SerializeField] private float nameSlideDistance = 600f;
        [SerializeField] private float nameSlideDuration = 0.35f;
        [SerializeField] private DG.Tweening.Ease nameSlideEase = DG.Tweening.Ease.OutCubic;

        private Queue<DialogueLine> _linesQueue = new Queue<DialogueLine>();
        private DialogueLine _currentLine;
        private bool _isTyping;
        private Coroutine _typingCoroutine;
        private Vector2 _nameOriginalAnchored;
        private bool _nameOriginalCaptured = false;
        private string _previousSpeakerName = null;
        
        // State tracking
        private bool _isWindowActive = false;

        public static MessageWindowManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            if (windowRoot) windowRoot.SetActive(false);
            // キャッシュしておく: speakerNameText の anchoredPosition（存在する場合）
            if (speakerNameText != null)
            {
                var rt = speakerNameText.rectTransform;
                if (rt != null)
                {
                    _nameOriginalAnchored = rt.anchoredPosition;
                    _nameOriginalCaptured = true;
                }
            }
        }

        public void StartScenario(DialogueScenario scenario)
        {
            if (scenario == null) return;

            _linesQueue.Clear();
            foreach (var line in scenario.lines)
            {
                _linesQueue.Enqueue(line);
            }

            if (windowRoot) windowRoot.SetActive(true);
            _isWindowActive = true;
            DisplayNextLine();
        }

        private void Update()
        {
            if (!_isWindowActive) return;

            // Input Handling (New Input System check)
            bool inputDetected = false;
            
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) inputDetected = true;
            if (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame)) inputDetected = true;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) inputDetected = true;

            if (inputDetected)
            {
                OnInteract();
            }
        }

        private void OnInteract()
        {
            if (_isTyping)
            {
                // Skip typing
                if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
                _isTyping = false;
                dialogueText.text = _currentLine.text;
                dialogueText.maxVisibleCharacters = _currentLine.text.Length;
            }
            else
            {
                // Next line
                DisplayNextLine();
            }
        }

        private void DisplayNextLine()
        {
            if (_linesQueue.Count == 0)
            {
                EndScenario();
                return;
            }

            _currentLine = _linesQueue.Dequeue();

            // Update UI
            string newSpeakerName = _currentLine.speakerName ?? string.Empty;
            if (speakerNameText) speakerNameText.text = newSpeakerName;
            
            if (portraitImage)
            {
                if (_currentLine.portrait != null)
                {
                    portraitImage.sprite = _currentLine.portrait;
                    portraitImage.gameObject.SetActive(true);
                }
                else
                {
                    portraitImage.gameObject.SetActive(false);
                }
            }

            // Trigger Effects
            if (_currentLine.customActions != null)
            {
                foreach (var action in _currentLine.customActions)
                {
                    if (EffectManager.Instance) EffectManager.Instance.PlayEffect(action);
                }
            }

            // 名前表示を画面外からスライドインさせる（前の名前と同じならスキップ）
            if (animateName && speakerNameText != null)
            {
                // 前の名前と同じならアニメーションをスキップ
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

                            // スタート位置を画面外（左右）に設定してから移動
                            // ラインごとの指定があればそれを優先して方向を決定
                            bool useSlideFromRight = slideFromRight;
                            if (_currentLine != null)
                            {
                                switch (_currentLine.nameSlideDirection)
                                {
                                    case NameSlideDirection.Left:
                                        useSlideFromRight = false;
                                        break;
                                    case NameSlideDirection.Right:
                                        useSlideFromRight = true;
                                        break;
                                    case NameSlideDirection.Default:
                                    default:
                                        // Manager の設定を使う
                                        break;
                                }
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

            // 前回のスピーカー名を更新
            _previousSpeakerName = newSpeakerName;

            // Start Typing
            if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
            float speed = _currentLine.typingSpeed > 0 ? _currentLine.typingSpeed : typingSpeed;
            _typingCoroutine = StartCoroutine(TypeText(_currentLine.text, speed));
        }

        private IEnumerator TypeText(string text, float speed)
        {
            _isTyping = true;
            dialogueText.text = text;
            dialogueText.maxVisibleCharacters = 0;

            // Need to parse TMP text info to get character count correctly if tags are used?
            // TMP handles maxVisibleCharacters by character index, ignoring tags.
            // However, we need to wait for the mesh to update to get accurate info.
            
            dialogueText.ForceMeshUpdate();
            int totalVisibleCharacters = dialogueText.textInfo.characterCount; // This excludes tags

            for (int i = 0; i <= totalVisibleCharacters; i++)
            {
                dialogueText.maxVisibleCharacters = i;
                yield return new WaitForSeconds(speed);
            }

            _isTyping = false;
        }

        private void EndScenario()
        {
            _isWindowActive = false;
            if (windowRoot) windowRoot.SetActive(false);
            Debug.Log("Scenario Ended");
        }
    }
}
