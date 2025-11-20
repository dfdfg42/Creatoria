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


        [Header("Settings")]
        public string playerName = "Player";

        private bool isWaitingForResponse = false;

        private void Start()
        {
            SetupUI();
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



    }
        
}
