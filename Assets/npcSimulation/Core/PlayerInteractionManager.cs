using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using NPCSimulation.Core;

namespace NPCSimulation.Core
{
    /// <summary>
    /// 플레이어와 NPC 간의 대화 UI 매니저
    /// (PlayerInteractionController와 별개로, 대화 전용 UI 패널을 관리하는 경우 사용)
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
        // [수정] PlayerController 의존성 제거됨
        private bool isConversationActive = false;
        private Coroutine typingCoroutine;

        void Start()
        {
            // 초기화 로그
            Debug.Log($"[PlayerInteractionManager] 초기화 시작");

            // UI 초기 상태 설정
            if (conversationPanel != null)
                conversationPanel.SetActive(false);

            // [수정] 인디케이터는 이제 PlayerInteractionController 방식과 겹치므로 기본적으로 끕니다.
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
            // [수정] UpdateInteractionIndicator() 삭제됨
            // 이유: PlayerController에서 감지 로직이 사라졌고, 
            // 이제 PlayerInteractionController가 독립적으로 입력을 처리하기 때문입니다.

            // ESC로 대화 종료
            if (isConversationActive && Input.GetKeyDown(KeyCode.Escape))
            {
                OnCloseConversation();
            }
        }

        /// <summary>
        /// NPC와 대화 시작 (외부에서 호출)
        /// </summary>
        public void StartConversation(NPCAgent npc)
        {
            if (npc == null)
            {
                Debug.LogError("[PlayerInteractionManager] StartConversation: NPC가 null입니다!");
                return;
            }

            Debug.Log($"[PlayerInteractionManager] {npc.Name}와 대화 UI 열기");

            currentNPC = npc;
            isConversationActive = true;

            // UI 표시
            if (conversationPanel != null)
            {
                conversationPanel.SetActive(true);
            }

            if (npcNameText != null)
                npcNameText.text = npc.Name;

            // 입력 필드 포커스
            if (playerInputField != null)
            {
                playerInputField.text = "";
                playerInputField.Select();
                playerInputField.ActivateInputField();
            }
        }

        /// <summary>
        /// NPC 대화 표시 (타이핑 효과)
        /// </summary>
        public void ShowNPCDialogue(string text)
        {
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);

            typingCoroutine = StartCoroutine(TypeText(text));
        }

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

        private void OnSendMessage()
        {
            if (playerInputField == null || currentNPC == null) return;

            string playerMessage = playerInputField.text.Trim();
            if (string.IsNullOrEmpty(playerMessage)) return;

            Debug.Log($"[PlayerInteractionManager] 플레이어: {playerMessage}");

            playerInputField.text = "";

            if (sendButton != null)
                sendButton.interactable = false;

            if (dialogueText != null)
                dialogueText.text = "...";

            // NPC에게 메시지 전달 및 응답 받기
            currentNPC.RespondToPlayer(playerMessage, "Player", (response) =>
            {
                // 응답 표시
                ShowNPCDialogue(response);

                if (sendButton != null)
                    sendButton.interactable = true;

                if (playerInputField != null)
                {
                    playerInputField.Select();
                    playerInputField.ActivateInputField();
                }
            });
        }

        private void OnPlayerInputSubmit(string text)
        {
            OnSendMessage();

            if (playerInputField != null)
            {
                playerInputField.Select();
                playerInputField.ActivateInputField();
            }
        }

        public void OnCloseConversation()
        {
            isConversationActive = false;
            currentNPC = null;

            if (conversationPanel != null)
                conversationPanel.SetActive(false);

            Debug.Log("[PlayerInteractionManager] 대화 종료");
        }

        public bool IsConversationActive => isConversationActive;
        public NPCAgent CurrentNPC => currentNPC;
    }
}
