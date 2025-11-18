using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace NPCSimulation.Core
{
    /// <summary>
    /// 플레이어와 NPC 간의 대화/상호작용 관리
    /// UI 표시 및 대화 로직 처리
    /// </summary>
    public class PlayerInteractionManager : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject conversationPanel;
        public TextMeshProUGUI npcNameText;
        public TextMeshProUGUI dialogueText;
        public TMP_InputField playerInputField;
        public Button sendButton;
        public Button closeButton;
        
        [Header("Interaction Indicator")]
        public GameObject interactionIndicator; // E키 표시 UI
        public TextMeshProUGUI indicatorText;
        
        [Header("Settings")]
        public float typingSpeed = 0.05f;
        
        private NPCAgent currentNPC;
        private PlayerController playerController;
        private bool isConversationActive = false;
        private Coroutine typingCoroutine;
        
        void Start()
        {
            playerController = FindAnyObjectByType<PlayerController>();
            
            Debug.Log($"[PlayerInteractionManager] 초기화 시작");
            Debug.Log($"  - PlayerController: {(playerController != null ? "찾음" : "없음")}");
            Debug.Log($"  - ConversationPanel: {(conversationPanel != null ? "연결됨" : "없음")}");
            Debug.Log($"  - NPCNameText: {(npcNameText != null ? "연결됨" : "없음")}");
            Debug.Log($"  - DialogueText: {(dialogueText != null ? "연결됨" : "없음")}");
            Debug.Log($"  - PlayerInputField: {(playerInputField != null ? "연결됨" : "없음")}");
            Debug.Log($"  - InteractionIndicator: {(interactionIndicator != null ? "연결됨" : "없음")}");
            
            // UI 초기 상태 설정
            if (conversationPanel != null)
                conversationPanel.SetActive(false);
            
            if (interactionIndicator != null)
                interactionIndicator.SetActive(false);
            
            // 버튼 이벤트 연결
            if (sendButton != null)
                sendButton.onClick.AddListener(OnSendMessage);
            
            if (closeButton != null)
                closeButton.onClick.AddListener(OnCloseConversation);
            
            // InputField 엔터키 이벤트
            if (playerInputField != null)
            {
                playerInputField.onSubmit.AddListener(OnPlayerInputSubmit);
            }
        }

        void Update()
        {
            // 상호작용 인디케이터 업데이트
            UpdateInteractionIndicator();
            
            // ESC로 대화 종료
            if (isConversationActive && Input.GetKeyDown(KeyCode.Escape))
            {
                OnCloseConversation();
            }
        }

        /// <summary>
        /// 상호작용 가능 표시 업데이트
        /// </summary>
        private void UpdateInteractionIndicator()
        {
            if (playerController == null || interactionIndicator == null) return;
            
            bool canInteract = playerController.CanInteract && !isConversationActive;
            interactionIndicator.SetActive(canInteract);
            
            if (canInteract && playerController.CurrentTargetNPC != null)
            {
                // NPC 위치에 인디케이터 표시
                Vector3 npcScreenPos = Camera.main.WorldToScreenPoint(
                    playerController.CurrentTargetNPC.transform.position + Vector3.up * 1f
                );
                interactionIndicator.transform.position = npcScreenPos;
                
                if (indicatorText != null)
                {
                    indicatorText.text = $"[E] {playerController.CurrentTargetNPC.Name}와 대화";
                }
            }
        }

        /// <summary>
        /// NPC와 대화 시작
        /// </summary>
        public void StartConversation(NPCAgent npc)
        {
            if (npc == null)
            {
                Debug.LogError("[PlayerInteractionManager] StartConversation: NPC가 null입니다!");
                return;
            }
            
            Debug.Log($"[PlayerInteractionManager] StartConversation 호출됨 - NPC: {npc.Name}");
            
            currentNPC = npc;
            isConversationActive = true;
            
            // UI 표시
            if (conversationPanel != null)
            {
                conversationPanel.SetActive(true);
                Debug.Log($"[PlayerInteractionManager] conversationPanel 활성화");
            }
            else
            {
                Debug.LogError("[PlayerInteractionManager] conversationPanel이 null입니다! Inspector에서 연결하세요.");
            }
            
            if (npcNameText != null)
                npcNameText.text = npc.Name;
            
            // 대화 시작 - AI 인사말 생성
            if (dialogueText != null)
                dialogueText.text = "...";
            
            // AI로 인사말 생성
            npc.RespondToPlayer("[대화 시작]", "Player", (greeting) =>
            {
                ShowNPCDialogue(greeting);
            });
            
            // 입력 필드 포커스
            if (playerInputField != null)
            {
                playerInputField.text = "";
                playerInputField.Select();
                playerInputField.ActivateInputField();
            }
            
            Debug.Log($"[PlayerInteractionManager] {npc.Name}와 대화 시작 완료");
        }

        /// <summary>
        /// NPC 대화 표시 (타이핑 효과)
        /// </summary>
        private void ShowNPCDialogue(string text)
        {
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);
            
            typingCoroutine = StartCoroutine(TypeText(text));
        }

        /// <summary>
        /// 텍스트 타이핑 효과
        /// </summary>
        private IEnumerator TypeText(string text)
        {
            if (dialogueText == null) yield break;
            
            dialogueText.text = "";
            
            foreach (char c in text)
            {
                dialogueText.text += c;
                yield return new WaitForSeconds(typingSpeed);
            }
        }

        /// <summary>
        /// 플레이어 메시지 전송
        /// </summary>
        private void OnSendMessage()
        {
            if (playerInputField == null || currentNPC == null) return;
            
            string playerMessage = playerInputField.text.Trim();
            if (string.IsNullOrEmpty(playerMessage)) return;
            
            Debug.Log($"[PlayerInteractionManager] 플레이어: {playerMessage}");
            
            // 입력 필드 초기화
            playerInputField.text = "";
            
            // 버튼 비활성화 (응답 대기 중)
            if (sendButton != null)
                sendButton.interactable = false;
            
            // 로딩 표시
            if (dialogueText != null)
                dialogueText.text = "...";
            
            // NPC에게 메시지 전달 및 응답 받기 (DemoController 방식)
            currentNPC.RespondToPlayer(playerMessage, "Player", (response) =>
            {
                // 응답 표시
                ShowNPCDialogue(response);
                
                // 버튼 다시 활성화
                if (sendButton != null)
                    sendButton.interactable = true;
                
                // 입력 필드 포커스
                if (playerInputField != null)
                {
                    playerInputField.Select();
                    playerInputField.ActivateInputField();
                }
            });
        }

        /// <summary>
        /// 엔터키로 메시지 전송
        /// </summary>
        private void OnPlayerInputSubmit(string text)
        {
            OnSendMessage();
            
            // 다시 포커스
            if (playerInputField != null)
            {
                playerInputField.Select();
                playerInputField.ActivateInputField();
            }
        }

        /// <summary>
        /// 대화 종료
        /// </summary>
        private void OnCloseConversation()
        {
            isConversationActive = false;
            currentNPC = null;
            
            if (conversationPanel != null)
                conversationPanel.SetActive(false);
            
            Debug.Log("[PlayerInteractionManager] 대화 종료");
        }

        /// <summary>
        /// 현재 대화 중인지
        /// </summary>
        public bool IsConversationActive => isConversationActive;
        
        /// <summary>
        /// 현재 대화 중인 NPC
        /// </summary>
        public NPCAgent CurrentNPC => currentNPC;
    }
}
