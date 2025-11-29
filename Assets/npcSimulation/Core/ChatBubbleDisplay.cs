using UnityEngine;
using TMPro;
using System.Collections;

public class ChatBubbleDisplay : MonoBehaviour
{
    [Header("UI Components")]
    public TextMeshPro chatText;      // 대화 내용을 띄울 텍스트
    public GameObject bubbleVisual;   // 말풍선 배경 (선택사항, 없으면 텍스트만)

    [Header("Settings")]
    public Vector3 offset = new Vector3(0, 1.8f, 0); // 상태창(Status)보다 조금 더 위에 띄움
    public float displayDuration = 4.0f; // 말풍선 유지 시간
    public Color normalColor = Color.white;
    public Color playerTalkColor = new Color(0.6f, 1f, 0.6f); // 플레이어와의 대화는 연두색

    private Coroutine hideCoroutine;
    private Transform parentTransform;

    private void Start()
    {
        // 부모(NPC) 찾기
        parentTransform = transform.parent;

        // 시작할 때는 숨기기
        if (chatText != null) chatText.text = "";
        if (bubbleVisual != null) bubbleVisual.SetActive(false);
    }

    private void LateUpdate()
    {
        if (parentTransform == null) return;

        // 위치 고정 & 좌우 반전 방지 (NPC가 뒤집혀도 글자는 똑바로)
        transform.position = parentTransform.position + offset;

        if (parentTransform.localScale.x < 0)
            transform.localScale = new Vector3(-1, 1, 1);
        else
            transform.localScale = new Vector3(1, 1, 1);
    }

    /// <summary>
    /// 대화 메시지 출력 함수
    /// </summary>
    public void ShowMessage(string message, bool isToPlayer = false)
    {
        gameObject.SetActive(true);
        if (bubbleVisual != null) bubbleVisual.SetActive(true);

        // 텍스트 설정
        chatText.text = message;
        chatText.color = isToPlayer ? playerTalkColor : normalColor;

        // 기존에 꺼지는 타이머가 돌고 있었다면 취소하고 새로 시작
        if (hideCoroutine != null) StopCoroutine(hideCoroutine);
        hideCoroutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);

        chatText.text = "";
        if (bubbleVisual != null) bubbleVisual.SetActive(false);
    }
}
