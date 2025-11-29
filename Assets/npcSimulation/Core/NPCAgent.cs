using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace NPCSimulation.Core
{
    /// <summary>
    /// 자율 행동 NPC 에이전트
    /// </summary>
    public class NPCAgent : MonoBehaviour
    {
        [Header("NPC 정보")]
        public string npcName = "이서아";
        [TextArea(3, 10)]
        public string persona = "21살의 대학생. 시각 디자인을 전공하며 졸업 작품으로 고민이 많다. 성격은 내향적이지만 친근하고, 도움을 요청받으면 기꺼이 도와준다.";

        [Header("OpenAI 설정")]
        public string openAIKey = "";

        [Header("자율 행동 설정")]
        public bool enableAutonomousBehavior = true;
        public float autonomousUpdateInterval = 5f; // 60초마다 업데이트

        [Header("UI References")]
        public ChatBubbleDisplay chatBubble; // [추가] 인스펙터에서 연결

        // 컴포넌트들
        private OpenAIClient llmClient;
        public MemoryManager MemoryMgr { get; private set; }
        public ConversationManager ConversationMgr { get; private set; }
        public AutonomousPlanner Planner { get; private set; }
        public PerceptionSystem Perception { get; private set; }
        public PathfindingSystem Pathfinding { get; private set; }

        // 상태 정보
        public string CurrentSituation { get; private set; } = "일상 생활 중";
        public string CurrentEmotion { get; private set; } = "평온함";
        public string CurrentGoal { get; private set; } = "하루 일과를 보내며 필요시 플레이어와 상호작용하기";
        public string CurrentLocation { get; private set; } = ""; // 초기화는 Start에서

        // 플레이어 상호작용 상태
        public bool IsInteractingWithPlayer { get; private set; } = false;

        public bool IsTalking { get; private set; } = false;

        // 리플렉션
        private int reflectionImportanceSum = 0;
        private int reflectionThreshold = 100;

        // 공개 프로퍼티
        public string Name => npcName;
        public string Persona => persona;

        private void Start()
        {
            InitializeAgent();
        }

        /// <summary>
        /// 에이전트 초기화
        /// </summary>
        private void InitializeAgent()
        {
            Debug.Log($"[NPCAgent] Initializing {npcName}...");

            // OpenAI 클라이언트 초기화
            if (string.IsNullOrEmpty(openAIKey))
            {
                Debug.LogError("[NPCAgent] OpenAI API Key가 설정되지 않았습니다!");
                return;
            }

            llmClient = new OpenAIClient(openAIKey);

            // 컴포넌트 초기화
            MemoryMgr = new MemoryManager(llmClient, npcName, persona);
            ConversationMgr = new ConversationManager(llmClient);
            Planner = new AutonomousPlanner(this, llmClient);

            // Perception & Pathfinding (컴포넌트로 추가되어야 함)
            Perception = GetComponent<PerceptionSystem>();
            if (Perception == null)
            {
                Perception = gameObject.AddComponent<PerceptionSystem>();
            }

            Pathfinding = GetComponent<PathfindingSystem>();
            if (Pathfinding == null)
            {
                Pathfinding = gameObject.AddComponent<PathfindingSystem>();
            }

            // 초기 기억 설정
            InitializeMemories();

            // 현재 위치 초기화 (가장 가까운 WorldArea 찾기)
            InitializeCurrentLocation();

            // 자율 행동 시작
            if (enableAutonomousBehavior)
            {
                InvokeRepeating(nameof(AutonomousUpdate), 5f, autonomousUpdateInterval);
            }

            Debug.Log($"[NPCAgent] {npcName} initialized successfully!");

            // [추가] 내 이름에 맞는 스프라이트 찾아 입기
            var spriteMgr = GetComponent<CharacterSpriteManager>();
            if (spriteMgr != null)
            {
                spriteMgr.LoadCharacterSprite(npcName);
            }
        }

        /// <summary>
        /// 현재 위치 초기화 - 가장 가까운 WorldArea 찾기
        /// </summary>
        private void InitializeCurrentLocation()
        {
            WorldArea[] allAreas = FindObjectsOfType<WorldArea>();
            if (allAreas.Length == 0)
            {
                CurrentLocation = "알 수 없는 장소";
                Debug.LogWarning("[NPCAgent] Scene에 WorldArea가 없습니다!");
                return;
            }

            // 가장 가까운 Area 찾기
            WorldArea nearestArea = allAreas[0];
            float minDistance = Vector3.Distance(transform.position, nearestArea.transform.position);

            foreach (var area in allAreas)
            {
                float distance = Vector3.Distance(transform.position, area.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestArea = area;
                }
            }

            CurrentLocation = nearestArea.GetFullName();
            Debug.Log($"[NPCAgent] 시작 위치: {CurrentLocation}");
        }

        /// <summary>
        /// 초기 기억 설정
        /// </summary>
        private void InitializeMemories()
        {
            MemoryMgr.AddMemory(MemoryType.Event, $"나의 이름은 '{npcName}'이다.", 10, this);
            MemoryMgr.AddMemory(MemoryType.Event, $"나의 성격 및 설정: '{persona}'", 10, this);
            MemoryMgr.AddMemory(MemoryType.Thought, $"[목표] 나의 현재 목표는 '{CurrentGoal}'이다.", 9, this);
        }

        #region Player Interaction

        /// <summary>
        /// 플레이어 메시지에 응답
        /// </summary>
        public void RespondToPlayer(string playerMessage, string playerName, Action<string> callback)
        {
            StartCoroutine(GenerateResponseCoroutine(playerMessage, playerName, callback));
        }

        private IEnumerator GenerateResponseCoroutine(string playerMessage, string playerName, Action<string> callback)
        {
            Debug.Log($"[NPCAgent] Player message: {playerMessage}");

            // 1. 플레이어 메시지를 메모리에 추가
            MemoryMgr.AddMemory(MemoryType.Event, $"{playerName}이(가) 말했다: '{playerMessage}'", 7, this);

            // 2. 대화 버퍼에 추가
            ConversationMgr.AddTurn(playerName, playerMessage);

            // 3. 관련 메모리 검색
            var relevantMemories = MemoryMgr.RetrieveRelevantMemories(playerMessage, 5);
            string memoryContext = string.Join("\n", System.Linq.Enumerable.Select(relevantMemories, m => $"- {m.description}"));

            // 4. 응답 생성
            string prompt = ConversationMgr.BuildContextualPrompt($@"
### NPC 정보 ###
이름: {npcName}
성격: {persona}
현재 상황: {CurrentSituation}
현재 감정: {CurrentEmotion}
현재 목표: {CurrentGoal}

### 관련 기억 ###
{memoryContext}

### 지시사항 ###
당신은 {npcName}입니다. {playerName}의 메시지에 자연스럽게 응답하세요.
- 당신의 성격과 현재 상황을 반영하세요
- 이전 대화 맥락을 고려하세요
- 50단어 이내로 간결하게 답하세요

{playerName}의 메시지: ""{playerMessage}""

{npcName}의 응답:");

            string response = "";
            yield return llmClient.GetChatCompletion(prompt, (generatedResponse) =>
            {
                response = generatedResponse;
            }, temperature: 0.7f, maxTokens: 150);

            if (chatBubble != null)
            {
                chatBubble.ShowMessage(response, isToPlayer: true);
            }

            // 5. 응답을 대화 버퍼에 추가
            ConversationMgr.AddTurn(npcName, response);

            // 6. 응답을 메모리에 추가
            MemoryMgr.AddMemory(MemoryType.Event, $"나는 {playerName}에게 말했다: '{response}'", 6, this);

            Debug.Log($"[NPCAgent] Response: {response}");
            callback?.Invoke(response);
        }

        /// <summary>
        /// 플레이어와 상호작용 시작
        /// </summary>
        public void StartInteractionWithPlayer()
        {
            IsInteractingWithPlayer = true;
            Debug.Log("[NPCAgent] Started interaction with player");
        }

        /// <summary>
        /// 플레이어와 상호작용 종료
        /// </summary>
        public void EndInteractionWithPlayer()
        {
            IsInteractingWithPlayer = false;
            Debug.Log("[NPCAgent] Ended interaction with player");

            // 대화 요약 생성
            ConversationMgr.GenerateSummary(this, (summary) =>
            {
                MemoryMgr.AddMemory(MemoryType.Thought, $"플레이어와의 대화 요약: {summary}", 8, this);
            });
        }

        #endregion

        #region Autonomous Behavior

        // 상태 변수 추가
        private bool isDecomposing = false;
        private float currentActionTimer = 0f;

        private void AutonomousUpdate()
        {
            if (IsInteractingWithPlayer || Planner.IsPlanningInProgress || isDecomposing) return;

            // [추가] 주변 에이전트 체크
            CheckForOtherAgents();

            DateTime currentTime = WorldTimeManager.Instance.CurrentTime;

            // 1. [High-Level] 일일 계획 갱신 체크
            if (Planner.ShouldReplan(currentTime))
            {
                Planner.CreateNewDailyPlan(currentTime, this);
                return;
            }
            if (!Planner.IsPlanReady) return;

            // 2. [Sub-Level] 현재 실행 중인 세부 행동(Sub-Action)이 있는지 확인
            if (currentActionTimer > 0)
            {
                currentActionTimer -= autonomousUpdateInterval;
                return;
            }

            // 3. 다음 세부 행동 가져오기
            SubPlanItem nextSubAction = Planner.GetNextSubAction();

            if (nextSubAction != null)
            {
                ExecuteSubAction(nextSubAction);
            }
            else
            {
                // 큐가 비었음 -> 현재 시간의 High-Level 계획을 가져와서 분해 요청
                PlanItem currentActivity = Planner.GetCurrentActivity(currentTime);
                if (currentActivity != null)
                {
                    if (CurrentLocation != currentActivity.location)
                    {
                        MoveToLocation(currentActivity.location);
                    }
                    else
                    {
                        isDecomposing = true;
                        Planner.DecomposeCurrentActivity(currentActivity, this, () =>
                        {
                            isDecomposing = false;
                        });
                    }
                }
            }
        }

        // [추가] 주변에 다른 에이전트가 있는지 확인하고 말을 걸지 결정하는 함수
        private void CheckForOtherAgents()
        {
            if (IsTalking || Planner.IsPlanningInProgress) return;

            // Perception을 통해 가장 가까운 에이전트 찾기
            NPCAgent otherAgent = Perception.FindNearestAgent(3.0f); // 3미터 이내

            if (otherAgent != null && !otherAgent.IsTalking)
            {
                // 리액션 평가
                Planner.EvaluateReaction($"I saw {otherAgent.Name} standing nearby.", this, (shouldReact, actionDesc) =>
                {
                    string action = actionDesc.ToLower();

                    if (shouldReact && (
                        action.Contains("talk") ||
                        action.Contains("chat") ||
                        action.Contains("greet") ||
                        action.Contains("say") ||      // 말하다
                        action.Contains("ask") ||      // 묻다
                        action.Contains("tell") ||     // 말해주다
                        action.Contains("discuss") ||  // 논의하다 (졸업작품 예시에서 나온 것)
                        action.Contains("invite") ||   // 초대하다
                        action.Contains("wave") ||     // 손흔들기 (보통 인사 후 대화로 이어짐)
                        action.Contains("approach")    // 다가가다
                    ))
                    {
                        StartCoroutine(InitiateConversation(otherAgent));
                    }

                    //if (shouldReact && (actionDesc.ToLower().Contains("talk") || actionDesc.ToLower().Contains("chat") || actionDesc.ToLower().Contains("greet")))
                    //{
                    //    StartCoroutine(InitiateConversation(otherAgent));
                    //}
                });
            }
        }

        private void ExecuteSubAction(SubPlanItem subItem)
        {
            Debug.Log($"[NPCAgent] ▶ Performing Sub-Action: {subItem.emoji} {subItem.description} ({subItem.durationMin}m)");

            currentActionTimer = subItem.durationMin * 1.0f;

            MemoryMgr.AddMemory(MemoryType.Event, $"나는 {CurrentLocation}에서 '{subItem.description}' 행동을 시작했다.", 4, this);

            if (!string.IsNullOrEmpty(subItem.targetObject))
            {
                var currentArea = FindObjectsOfType<WorldArea>().FirstOrDefault(a => a.GetFullName() == CurrentLocation);
                if (currentArea != null)
                {
                    var targetObj = currentArea.FindObjectByName(subItem.targetObject);
                    if (targetObj != null)
                    {
                        InteractWithObject(targetObj, null);
                    }
                }
            }
        }

        private void MoveToLocation(string locationName)
        {
            Debug.Log($"[NPCAgent] 🚶 Moving to location: {locationName}");
            CurrentLocation = locationName;

            if (Pathfinding != null)
            {
                Pathfinding.MoveToArea(locationName, () => {
                    Debug.Log($"[NPCAgent] Arrived at {locationName}");
                });
            }
        }

        #endregion

        #region Reacting (Perception Integration)

        public void OnPerceiveEnvironment(string observation)
        {
            if (IsInteractingWithPlayer || Planner.IsPlanningInProgress) return;

            Planner.EvaluateReaction(observation, this, (shouldReact, newActionDesc) =>
            {
                if (shouldReact)
                {
                    InterruptAndReact(newActionDesc);
                }
            });
        }

        private void InterruptAndReact(string newActionDesc)
        {
            Debug.LogWarning($"[NPCAgent] ⚡ INTERRUPTED! New Goal: {newActionDesc}");

            StopAllCoroutines();
            isDecomposing = false;
            currentActionTimer = 0;

            Planner.CurrentSubQueue.Clear();

            var reactionItem = new SubPlanItem(newActionDesc, 10, null, "❗");
            Planner.CurrentSubQueue.Enqueue(reactionItem);

            MemoryMgr.AddMemory(MemoryType.Event, $"갑작스러운 상황 발생: 상황을 인지하고 '{newActionDesc}' 행동을 하기로 결정했다.", 8, this);
        }

        #endregion

        #region Emotion

        public void UpdateEmotion(string newEmotion)
        {
            string oldEmotion = CurrentEmotion;
            CurrentEmotion = newEmotion;
            Debug.Log($"[NPCAgent] Emotion changed: {oldEmotion} → {newEmotion}");
            MemoryMgr.AddMemory(MemoryType.Thought, $"나의 감정이 '{oldEmotion}'에서 '{newEmotion}'로 변했다.", 6, this);
        }

        #endregion

        #region Object Interaction

        public void InteractWithObject(WorldObject targetObject, Action<bool> callback)
        {
            StartCoroutine(GenerativeInteractionCoroutine(targetObject, callback));
        }

        private IEnumerator GenerativeInteractionCoroutine(WorldObject targetObject, Action<bool> callback)
        {
            if (targetObject == null) { callback?.Invoke(false); yield break; }

            float distance = Vector3.Distance(transform.position, targetObject.transform.position);
            if (distance > targetObject.interactionRange)
            {
                Pathfinding.MoveTo(targetObject.transform.position);
                while (Pathfinding.IsMoving) yield return null;
            }

            Debug.Log($"[NPCAgent] {Name} interacting with {targetObject.objectName}...");

            string actionPrompt = $@"
You are {npcName}.
Current Situation: {CurrentSituation}
Target Object: {targetObject.GetDescription()}

What do you want to do with this object?
Answer in one short sentence (e.g., ""I turn on the coffee machine"", ""I eat the apple"").
Action:";

            string actionDescription = "";
            yield return llmClient.GetChatCompletion(actionPrompt, (response) => {
                actionDescription = response.Trim();
            }, temperature: 0.7f, maxTokens: 50);

            Debug.Log($"[NPCAgent] Action Decided: {actionDescription}");

            string statePrompt = $@"
Agent Action: {actionDescription}
Object: {targetObject.objectName}
Current State: {targetObject.objectState}

Describe the NEW state of the object after this action.
Answer in one short phrase (e.g., ""brewing coffee"", ""eaten and empty"", ""turned on"").
New State:";

            string newState = "";
            yield return llmClient.GetChatCompletion(statePrompt, (response) => {
                newState = response.Trim();
                newState = newState.Replace("The object is now ", "").Replace("\"", "").Replace(".", "");
            }, temperature: 0.3f, maxTokens: 30);

            Debug.Log($"[NPCAgent] New State Inferred: {newState}");

            targetObject.UpdateState(newState);

            MemoryMgr.AddMemory(
                MemoryType.Event,
                $"{targetObject.objectName}에 대해 행동했다: '{actionDescription}'. 결과 상태: '{newState}'",
                6,
                this
            );

            callback?.Invoke(true);
        }

        public void FindAndMoveToObjectType(string type, Action<WorldObject> callback)
        {
            StartCoroutine(FindAndMoveToObjectTypeCoroutine(type, callback));
        }

        private IEnumerator FindAndMoveToObjectTypeCoroutine(string type, Action<WorldObject> callback)
        {
            Perception.PerceiveEnvironment();
            yield return new WaitForSeconds(0.5f);

            WorldObject target = Perception.FindNearestObjectOfType(type);

            if (target == null)
            {
                Debug.LogWarning($"[NPCAgent] Cannot find object of type {type}");
                callback?.Invoke(null);
                yield break;
            }

            Debug.Log($"[NPCAgent] Found {target.objectName}, moving...");

            bool success = Pathfinding.MoveToObject(target);
            if (!success)
            {
                callback?.Invoke(null);
                yield break;
            }

            while (Pathfinding.IsMoving)
            {
                yield return null;
            }

            callback?.Invoke(target);
        }

        #endregion

        #region npcInteraction

        public void ReceiveMessageFromAgent(NPCAgent sender, string message)
        {
            Debug.Log($"[{npcName}] 듣기: {sender.Name} -> \"{message}\"");
            MemoryMgr.AddMemory(MemoryType.Event, $"{sender.Name}가 나에게 말했다: \"{message}\"", 5, this);

            if (!IsTalking)
            {
                StopCurrentAction();
                IsTalking = true;
            }

            StartCoroutine(GenerateReply(sender, message));
        }

        private IEnumerator GenerateReply(NPCAgent partner, string partnerMessage)
        {
            if (partnerMessage.Contains("Bye") || partnerMessage.Contains("가볼게"))
            {
                EndConversation();
                yield break;
            }

            string prompt = $@"
You are {npcName}. You are talking to {partner.Name}.
Your Persona: {persona}
Partner's Persona: {partner.Persona}
Current Context: {CurrentSituation}

{partner.Name} said: ""{partnerMessage}""

Reply to {partner.Name}. 
If you want to end the conversation, include ""Bye"".
Keep it short (1 sentence).

Reply:";

            string reply = "";
            yield return llmClient.GetChatCompletion(prompt, (res) => reply = res.Trim(), 0.7f, 100);

            if (chatBubble != null)
            {
                chatBubble.ShowMessage(reply, isToPlayer: false);
            }

            Debug.Log($"[{npcName}] 말하기: \"{reply}\"");
            partner.ReceiveMessageFromAgent(this, reply);

            MemoryMgr.AddMemory(MemoryType.Event, $"나는 {partner.Name}에게 말했다: \"{reply}\"", 5, this);
        }

        public void EndConversation()
        {
            IsTalking = false;
            Debug.Log($"[{npcName}] 대화 종료.");
        }

        private IEnumerator InitiateConversation(NPCAgent target)
        {
            IsTalking = true;
            StopCurrentAction();
            target.StopCurrentAction();

            string prompt = $@"
You decide to talk to {target.Name}.
Your Persona: {persona}
Target's Persona: {target.Persona}

What is your first sentence to start the conversation?
(e.g., Hello, How are you?, Did you see the news?)

Sentence:";

            string firstMsg = "";
            yield return llmClient.GetChatCompletion(prompt, (res) => firstMsg = res.Trim(), 0.7f, 100);

            Debug.Log($"[{npcName}] 대화 시작: \"{firstMsg}\"");

            if (chatBubble != null)
            {
                chatBubble.ShowMessage(firstMsg, isToPlayer: false);
            }

            MemoryMgr.AddMemory(MemoryType.Event, $"{target.Name}에게 말을 걸었다: \"{firstMsg}\"", 5, this);

            target.ReceiveMessageFromAgent(this, firstMsg);
        }

        public void StopCurrentAction()
        {
            StopAllCoroutines();
            // Pathfinding System에 Stop 기능이 있다고 가정 (없으면 에러날 수 있으니 확인 필요)
            if (Pathfinding != null) Pathfinding.StopAllCoroutines();
            isDecomposing = false;
            Planner.CurrentSubQueue.Clear();
        }

        #endregion

        #region Environment Modification

        public void EvaluateEnvironmentChange(string context, Action<EnvironmentChangeDecision> callback)
        {
            StartCoroutine(EvaluateEnvironmentChangeCoroutine(context, callback));
        }

        private IEnumerator EvaluateEnvironmentChangeCoroutine(string context, Action<EnvironmentChangeDecision> callback)
        {
            string prompt = $@"
당신은 {npcName}입니다. 현재 상황: {context}

환경을 개선하기 위해 추가할 오브젝트를 제안하세요.

응답 형식:
필요 여부: [예/아니오]
오브젝트: [이름]
이유: [왜 필요한지]
위치 힌트: [어디에 배치할지]
프롬프트: [이미지 생성용 프롬프트]

예시:
필요 여부: 예
오브젝트: 따뜻한 램프
이유: 카페가 어두워서 분위기를 밝게 만들고 싶다
위치 힌트: 코너
프롬프트: cozy warm table lamp, pixel art, 32x32px, top-down view, isolated object, white background
";

            EnvironmentChangeDecision decision = new EnvironmentChangeDecision();

            yield return llmClient.GetChatCompletion(prompt, (response) =>
            {
                string[] lines = response.Split('\n');
                foreach (string line in lines)
                {
                    if (line.Contains("필요 여부:")) decision.isNeeded = line.Contains("예");
                    else if (line.Contains("오브젝트:")) decision.objectName = line.Split(':')[1].Trim();
                    else if (line.Contains("이유:")) decision.reason = line.Split(':')[1].Trim();
                    else if (line.Contains("위치 힌트:")) decision.positionHint = line.Split(':')[1].Trim();
                    else if (line.Contains("프롬프트:")) decision.imagePrompt = line.Split(':')[1].Trim();
                }

                Debug.Log($"[NPCAgent] Environment change decision: {decision}");
                callback?.Invoke(decision);
            }, temperature: 0.6f, maxTokens: 300);
        }

        #endregion
    }

    [Serializable]
    public class EnvironmentChangeDecision
    {
        public bool isNeeded;
        public string objectName;
        public string reason;
        public string positionHint;
        public string imagePrompt;

        public override string ToString()
        {
            return $"[{(isNeeded ? "필요" : "불필요")}] {objectName} @ {positionHint} - {reason}";
        }
    }
}
