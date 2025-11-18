using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NPCSimulation.Core;
using NPCSimulation.Environment;

namespace NPCSimulation.Demo
{
    /// <summary>
    /// NPC 시스템 데모/테스트 컨트롤러
    /// </summary>
    public class NPCDemoController : MonoBehaviour
    {
        [Header("References")]
        public NPCAgent npcAgent;
        public EnvironmentModificationSystem environmentSystem;
        //public CharacterGenerationSystem characterSystem;

        [Header("UI - Chat")]
        public GameObject chatPanel;
        public TMP_InputField chatInputField;
        public Button sendButton;
        public TextMeshProUGUI chatHistoryText;
        public TextMeshProUGUI npcStatusText;

        [Header("UI - Environment")]
        public GameObject environmentPanel;
        public TMP_InputField environmentContextInput;
        public Button evaluateEnvironmentButton;
        public Button manualGenerateButton;
        public TMP_InputField manualPromptInput;

        [Header("UI - Character")]
        public GameObject characterPanel;
        public TMP_InputField characterContextInput;
        public Button evaluateCharacterButton;
        public TMP_InputField characterNameInput;
        public TMP_InputField characterDescInput;
        public Button manualGenerateCharacterButton;
        public TextMeshProUGUI characterListText;

        [Header("Settings")]
        public string playerName = "Player";

        private bool isWaitingForResponse = false;

        private void Start()
        {
            SetupUI();
            UpdateNPCStatus();
        }

        private void SetupUI()
        {
            // 채팅 버튼
            if (sendButton != null)
            {
                sendButton.onClick.AddListener(OnSendMessage);
            }

            // Enter 키로 메시지 전송
            if (chatInputField != null)
            {
                chatInputField.onSubmit.AddListener((text) => OnSendMessage());
            }

            // 환경 변경 버튼
            if (evaluateEnvironmentButton != null)
            {
                evaluateEnvironmentButton.onClick.AddListener(OnEvaluateEnvironment);
            }

            // 수동 생성 버튼
            if (manualGenerateButton != null)
            {
                manualGenerateButton.onClick.AddListener(OnManualGenerate);
            }

            // 캐릭터 평가 버튼
            if (evaluateCharacterButton != null)
            {
                evaluateCharacterButton.onClick.AddListener(OnEvaluateCharacter);
            }

            // 수동 캐릭터 생성 버튼
            if (manualGenerateCharacterButton != null)
            {
                manualGenerateCharacterButton.onClick.AddListener(OnManualGenerateCharacter);
            }

            // 초기 메시지
            AppendChatMessage("System", "NPC와 대화를 시작하세요!");
        }

        #region Chat

        private void OnSendMessage()
        {
            if (isWaitingForResponse)
            {
                Debug.Log("[NPCDemoController] 응답 대기 중...");
                return;
            }

            string message = chatInputField.text.Trim();
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            // UI 업데이트
            AppendChatMessage(playerName, message);
            chatInputField.text = "";
            isWaitingForResponse = true;
            sendButton.interactable = false;

            // NPC에게 메시지 전송
            npcAgent.RespondToPlayer(message, playerName, (response) =>
            {
                AppendChatMessage(npcAgent.Name, response);
                isWaitingForResponse = false;
                sendButton.interactable = true;
                UpdateNPCStatus();
            });
        }

        private void AppendChatMessage(string speaker, string message)
        {
            if (chatHistoryText != null)
            {
                string timestamp = System.DateTime.Now.ToString("HH:mm");
                chatHistoryText.text += $"\n[{timestamp}] <b>{speaker}</b>: {message}";

                // 스크롤 다운
                Canvas.ForceUpdateCanvases();
            }
        }

        #endregion

        #region Environment

        private void OnEvaluateEnvironment()
        {
            if (environmentSystem == null)
            {
                Debug.LogError("[NPCDemoController] EnvironmentModificationSystem이 설정되지 않았습니다!");
                return;
            }

            string context = environmentContextInput.text.Trim();
            if (string.IsNullOrEmpty(context))
            {
                context = "현재 환경을 평가해주세요";
            }

            Debug.Log($"[NPCDemoController] Requesting environment change: {context}");
            environmentSystem.RequestEnvironmentChange(context);

            // UI 피드백
            AppendChatMessage("System", $"환경 평가 중... (Context: {context})");
        }

        private void OnManualGenerate()
        {
            if (environmentSystem == null)
            {
                Debug.LogError("[NPCDemoController] EnvironmentModificationSystem이 설정되지 않았습니다!");
                return;
            }

            string prompt = manualPromptInput.text.Trim();
            if (string.IsNullOrEmpty(prompt))
            {
                prompt = "cozy warm lamp, pixel art, 32x32px, top-down view, isolated object, white background";
            }

            Debug.Log($"[NPCDemoController] Manual tile generation: {prompt}");
            environmentSystem.ManualGenerateTile(prompt, "near");

            // UI 피드백
            AppendChatMessage("System", $"타일 생성 중... (Prompt: {prompt})");
        }

        #endregion

        #region Character

        private void OnEvaluateCharacter()
        {
            // CharacterGenerationSystem이 구현될 때까지 비활성화
            Debug.LogWarning("[NPCDemoController] CharacterGenerationSystem이 아직 구현되지 않았습니다!");
            AppendChatMessage("System", "캐릭터 생성 기능이 아직 구현되지 않았습니다.");
            
            /*
            if (characterSystem == null)
            {
                Debug.LogError("[NPCDemoController] CharacterGenerationSystem이 설정되지 않았습니다!");
                return;
            }

            string context = characterContextInput.text.Trim();
            if (string.IsNullOrEmpty(context))
            {
                context = "새로운 캐릭터가 필요한가요?";
            }

            Debug.Log($"[NPCDemoController] Requesting character generation: {context}");
            characterSystem.RequestCharacterGeneration(context);

            // UI 피드백
            AppendChatMessage("System", $"캐릭터 평가 중... (Context: {context})");
            */
        }

        private void OnManualGenerateCharacter()
        {
            // CharacterGenerationSystem이 구현될 때까지 비활성화
            Debug.LogWarning("[NPCDemoController] CharacterGenerationSystem이 아직 구현되지 않았습니다!");
            AppendChatMessage("System", "캐릭터 생성 기능이 아직 구현되지 않았습니다.");
            
            /*
            if (characterSystem == null)
            {
                Debug.LogError("[NPCDemoController] CharacterGenerationSystem이 설정되지 않았습니다!");
                return;
            }

            string name = characterNameInput.text.Trim();
            string desc = characterDescInput.text.Trim();

            if (string.IsNullOrEmpty(name))
            {
                name = "김철수";
            }

            if (string.IsNullOrEmpty(desc))
            {
                desc = "20대 남성, 캐주얼 복장";
            }

            // 프롬프트 자동 생성
            string prompt = $"{desc}, pixel art character, front view, full body, white background";

            Debug.Log($"[NPCDemoController] Manual character generation: {name} - {prompt}");
            characterSystem.ManualGenerateCharacter(name, desc, prompt);

            // UI 피드백
            AppendChatMessage("System", $"캐릭터 생성 중... ({name})");
            
            // 입력 필드 초기화
            characterNameInput.text = "";
            characterDescInput.text = "";
            */
        }

        private void UpdateCharacterList()
        {
            if (characterListText != null)
            {
                characterListText.text = "캐릭터 생성 기능이 아직 구현되지 않았습니다.";
            }
            
            /*
            if (characterListText != null && characterSystem != null)
            {
                var characters = characterSystem.GetAllCharacters();
                
                if (characters.Count == 0)
                {
                    characterListText.text = "생성된 캐릭터 없음";
                }
                else
                {
                    string list = $"<b>생성된 캐릭터 ({characters.Count}명)</b>\n";
                    foreach (var character in characters)
                    {
                        list += $"\n• {character.characterName} ({character.role})";
                    }
                    characterListText.text = list;
                }
            }
            */
        }

        #endregion

        #region Status Update

        private void UpdateNPCStatus()
        {
            if (npcStatusText != null && npcAgent != null)
            {
                npcStatusText.text = $@"
<b>NPC: {npcAgent.Name}</b>
위치: {npcAgent.CurrentLocation}
감정: {npcAgent.CurrentEmotion}
상황: {npcAgent.CurrentSituation}
목표: {npcAgent.CurrentGoal}
대화 중: {(npcAgent.IsInteractingWithPlayer ? "예" : "아니오")}
";
            }
        }

        #endregion

        #region Keyboard Shortcuts

        private void Update()
        {
            // F1: 채팅 패널 토글
            if (Input.GetKeyDown(KeyCode.F1))
            {
                if (chatPanel != null)
                {
                    chatPanel.SetActive(!chatPanel.activeSelf);
                }
            }

            // F2: 환경 패널 토글
            if (Input.GetKeyDown(KeyCode.F2))
            {
                if (environmentPanel != null)
                {
                    environmentPanel.SetActive(!environmentPanel.activeSelf);
                }
            }

            // F3: 캐릭터 패널 토글
            if (Input.GetKeyDown(KeyCode.F3))
            {
                if (characterPanel != null)
                {
                    characterPanel.SetActive(!characterPanel.activeSelf);
                }
            }

            // F5: 상태 업데이트
            if (Input.GetKeyDown(KeyCode.F5))
            {
                UpdateNPCStatus();
                UpdateCharacterList();
            }

            // ESC: 대화 종료
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (npcAgent.IsInteractingWithPlayer)
                {
                    npcAgent.EndInteractionWithPlayer();
                    AppendChatMessage("System", "대화를 종료했습니다.");
                    UpdateNPCStatus();
                }
            }
        }

        #endregion
    }
}
