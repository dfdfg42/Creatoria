using UnityEngine;
using TMPro;
using System;

namespace NPCSimulation.UI
{
    public class InteractionInputUI : MonoBehaviour
    {
        [Header("UI Components")]
        public GameObject inputPanel;
        public TMP_InputField inputField;
        public TextMeshProUGUI promptText;

        private Action<string> onSubmitCallback;

        private void Start()
        {
            CloseUI();

            if (inputField != null)
                inputField.onSubmit.AddListener(OnSubmit);
        }

        // [추가됨] 매 프레임 체크하여 ESC가 눌리면 닫기
        private void Update()
        {
            // 패널이 켜져 있을 때만 체크
            if (inputPanel != null && inputPanel.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    CloseUI();
                }
            }
        }

        public void OpenUI(string prompt, Action<string> callback)
        {
            if (inputPanel != null) inputPanel.SetActive(true);

            if (inputField != null)
            {
                inputField.text = "";
                inputField.ActivateInputField();
            }

            if (promptText != null) promptText.text = prompt;

            onSubmitCallback = callback;

            Time.timeScale = 0f; // 게임 일시정지
        }

        public void CloseUI()
        {
            if (inputPanel != null) inputPanel.SetActive(false);

            // [중요] 창을 닫으면 게임 시간을 다시 흐르게 함
            Time.timeScale = 1f;
        }

        private void OnSubmit(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            onSubmitCallback?.Invoke(text);
            CloseUI();
        }
    }
}
