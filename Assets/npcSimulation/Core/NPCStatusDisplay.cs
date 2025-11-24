using UnityEngine;
using TMPro; // TextMeshPro 필수
using NPCSimulation.Core;

public class NPCStatusDisplay : MonoBehaviour
{
    [Header("UI Components")]
    public TextMeshPro statusText; // 머리 위에 띄울 텍스트 컴포넌트

    [Header("Settings")]
    public Vector3 offset = new Vector3(0, 0.3f, 0); // 머리 위 높이 조절
    public bool preventFlip = true; // 캐릭터가 뒤집혀도 글자는 정방향 유지

    private NPCAgent agent;
    private Transform parentTransform;

    private void Start()
    {
        // 부모 오브젝트에서 NPCAgent 찾기
        agent = GetComponentInParent<NPCAgent>();
        parentTransform = agent.transform;

        if (statusText == null)
            statusText = GetComponent<TextMeshPro>();
    }

    private void Update()
    {
        if (agent == null || statusText == null) return;

        UpdatePosition();
        UpdateText();
    }

    private void UpdatePosition()
    {
        // 텍스트 위치 고정 (NPC 머리 위)
        transform.position = parentTransform.position + offset;

        // [중요] 캐릭터가 좌우 반전(Scale X = -1) 되어도 글자는 뒤집히지 않게 처리
        if (preventFlip)
        {
            if (parentTransform.localScale.x < 0)
                transform.localScale = new Vector3(-1, 1, 1); // 부모가 반전되면 나도 반전해서 상쇄
            else
                transform.localScale = new Vector3(1, 1, 1);
        }
    }

    private void UpdateText()
    {
        // [수정 1] Planner가 아직 생성되지 않았으면(초기화 중이면) 아무것도 하지 않고 리턴
        if (agent.Planner == null)
        {
            statusText.text = "초기화 중...";
            statusText.fontSize = 2f;
            statusText.color = Color.gray;
            return;
        }

        // [수정 2] WorldTimeManager가 없을 경우 대비
        if (WorldTimeManager.Instance == null) return;

        string displayText = "";

        // 1순위: 현재 실행 중인 세부 행동 (예: "🥕 당근 썰기")
        if (agent.Planner.CurrentSubAction != null)
        {
            var sub = agent.Planner.CurrentSubAction;
            displayText = $"{sub.emoji} {sub.description}";
            statusText.color = Color.yellow;
            statusText.fontSize = 3f;
        }
        // 2순위: 큰 계획 (예: "🍳 점심 준비하기")
        else
        {
            var currentHighLevel = agent.Planner.GetCurrentActivity(WorldTimeManager.Instance.CurrentTime);

            if (currentHighLevel != null)
            {
                displayText = $"{currentHighLevel.emoji} {currentHighLevel.activity}";
                statusText.color = Color.gray;
                statusText.fontSize = 3f;
            }
            else
            {
                displayText = "💤 휴식 중";
                statusText.color = Color.gray;
                statusText.fontSize = 3f;
            }
        }

        // 텍스트 적용
        statusText.text = displayText;
    }
}
