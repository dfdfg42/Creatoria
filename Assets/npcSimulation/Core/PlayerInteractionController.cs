using UnityEngine;
using System;
using System.Collections;
using NPCSimulation.Core;
using NPCSimulation.UI;

public class PlayerInteractionController : MonoBehaviour
{
    [Header("Keys")]
    public KeyCode talkKey = KeyCode.E;   // 대화 전용 키
    public KeyCode actionKey = KeyCode.F; // 행동 전용 키

    [Header("Settings")]
    public float interactionRange = 2.5f;
    public string openAIKey = "";

    [Header("UI Reference")]
    public InteractionInputUI inputUI;

    private OpenAIClient llmClient;
    private PlayerStats stats;

    private void Start()
    {
        llmClient = new OpenAIClient(openAIKey);
        stats = GetComponent<PlayerStats>();
    }

    private void Update()
    {
        // UI가 꺼져있을 때만 입력 받음
        if (inputUI != null && inputUI.inputPanel != null && inputUI.inputPanel.activeSelf) return;

        // 1. 대화 키 (F) 입력 -> NPC 찾기
        if (Input.GetKeyDown(talkKey))
        {
            TryTalkToNPC();
        }

        // 2. 행동 키 (E) 입력 -> 사물(Object) 찾기
        if (Input.GetKeyDown(actionKey))
        {
            TryInteractObject();
        }
    }

    // --- 1. 대화 시도 (NPC) ---
    private void TryTalkToNPC()
    {
        NPCAgent nearestNPC = FindNearestNPC();

        if (nearestNPC != null)
        {
            // [로그 추가] 찾은 NPC 이름 출력
            Debug.Log($"🔍 [Interaction] 가장 가까운 NPC 발견: {nearestNPC.Name} (거리: {Vector3.Distance(transform.position, nearestNPC.transform.position):F2}m)");

            // UI 열기: 대화 모드
            inputUI.OpenUI($"[대화] {nearestNPC.Name}에게 할 말:", (playerInput) =>
            {
                StartCoroutine(ProcessDialogue(nearestNPC, playerInput));
            });
        }
        else
        {
            // [로그 추가] 못 찾았을 때
            Debug.Log($"⚠️ [Interaction] 반경 {interactionRange}m 내에 대화할 NPC가 없습니다.");
        }
    }

    // --- 2. 행동 시도 (Object) ---
    private void TryInteractObject()
    {
        WorldObject nearestObj = FindNearestObject();

        if (nearestObj != null)
        {
            // [로그 추가] 찾은 오브젝트 이름 출력
            Debug.Log($"🔍 [Interaction] 가장 가까운 오브젝트 발견: {nearestObj.objectName} (거리: {Vector3.Distance(transform.position, nearestObj.transform.position):F2}m)");

            // UI 열기: 행동 모드
            inputUI.OpenUI($"[행동] {nearestObj.objectName}에게 할 행동:", (playerInput) =>
            {
                StartCoroutine(ProcessObjectAction(nearestObj, playerInput));
            });
        }
        else
        {
            // [로그 추가] 못 찾았을 때
            Debug.Log($"⚠️ [Interaction] 반경 {interactionRange}m 내에 상호작용할 사물이 없습니다.");
        }
    }

    // --- 탐색 로직 ---

    private NPCAgent FindNearestNPC()
    {
        NPCAgent nearest = null;
        float minDst = float.MaxValue;

        var agents = FindObjectsOfType<NPCAgent>();

        foreach (var agent in agents)
        {
            if (agent.gameObject == this.gameObject) continue; // 나 자신 제외

            float dst = Vector3.Distance(transform.position, agent.transform.position);
            if (dst <= interactionRange && dst < minDst)
            {
                minDst = dst;
                nearest = agent;
            }
        }
        return nearest;
    }

    private WorldObject FindNearestObject()
    {
        WorldObject nearest = null;
        float minDst = float.MaxValue;
        Collider2D[] cols = Physics2D.OverlapCircleAll(transform.position, interactionRange);

        foreach (var col in cols)
        {
            var obj = col.GetComponent<WorldObject>();
            if (obj != null)
            {
                float dst = Vector3.Distance(transform.position, obj.transform.position);
                if (dst < minDst)
                {
                    minDst = dst;
                    nearest = obj;
                }
            }
        }
        return nearest;
    }

    // --- 처리 로직 (기존과 동일) ---

    // 1. NPC와 대화 처리
    private IEnumerator ProcessDialogue(NPCAgent npc, string message)
    {
        var myBubble = GetComponentInChildren<ChatBubbleDisplay>();
        if (myBubble != null) myBubble.ShowMessage(message, true);

        bool done = false;
        npc.RespondToPlayer(message, "Player", (response) =>
        {
            done = true;
        });

        while (!done) yield return null;
    }

    // 2. 사물과 행동 처리
    private IEnumerator ProcessObjectAction(WorldObject targetObj, string playerAction)
    {
        Debug.Log($"[Interaction] '{targetObj.objectName}'에게 행동: {playerAction}");

        var myBubble = GetComponentInChildren<ChatBubbleDisplay>();
        if (myBubble != null) myBubble.ShowMessage($"({playerAction})", true);

        string prompt = $@"
You are a Game Master AI. 
[Player Status] Energy:{stats.energy}, Hunger:{stats.hunger}, Hygiene:{stats.hygiene}, Happiness:{stats.happiness}
[Object] {targetObj.objectName} (State: {targetObj.objectState})
[Action] ""{playerAction}""

Goal: Update object state and player stats.
Output JSON only:
{{
  ""newState"": ""string"",
  ""dEnergy"": int,
  ""dHunger"": int,
  ""dHygiene"": int,
  ""dHappiness"": int,
  ""flavorText"": ""Short result description""
}}
";

        yield return llmClient.GetChatCompletion(prompt, (jsonResponse) =>
        {
            HandleActionResponse(targetObj, jsonResponse);
        }, temperature: 0.7f, maxTokens: 150);
    }

    private void HandleActionResponse(WorldObject obj, string jsonResponse)
    {
        try
        {
            jsonResponse = jsonResponse.Replace("```json", "").Replace("```", "").Trim();
            InteractionResult result = JsonUtility.FromJson<InteractionResult>(jsonResponse);

            obj.UpdateState(result.newState);
            stats.ApplyStatChange(result.dEnergy, result.dHunger, result.dHygiene, result.dHappiness);

            var bubble = GetComponentInChildren<ChatBubbleDisplay>();
            if (bubble != null)
            {
                StartCoroutine(ShowResultBubble(bubble, result.flavorText));
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"JSON Error: {e.Message}");
        }
    }

    private IEnumerator ShowResultBubble(ChatBubbleDisplay bubble, string text)
    {
        yield return new WaitForSeconds(1.5f);
        bubble.ShowMessage(text, true);
    }

    [Serializable]
    public class InteractionResult
    {
        public string newState;
        public int dEnergy;
        public int dHunger;
        public int dHygiene;
        public int dHappiness;
        public string flavorText;
    }
}
