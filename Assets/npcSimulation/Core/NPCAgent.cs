using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        public float autonomousUpdateInterval = 60f; // 60초마다 업데이트

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
                // 예: NPC 이름이 "이서아"라면 "Resources/Characters/이서아"를 찾음
                // 혹은 영어 ID가 따로 있다면 그것을 사용 (npcID 등)
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

        /// <summary>
        /// 자율 행동 업데이트
        /// </summary>
        private void AutonomousUpdate()
        {
            if (IsInteractingWithPlayer)
            {
                Debug.Log("[NPCAgent] Skipping autonomous update (interacting with player)");
                return;
            }

            Debug.Log("[NPCAgent] Performing autonomous update...");

            // 현재 시간 기준으로 계획 확인
            DateTime currentTime = DateTime.Now;

            // 재계획 필요 여부 확인
            if (Planner.ShouldReplan(currentTime))
            {
                Debug.Log("[NPCAgent] 새로운 일일 계획이 필요합니다. 계획 생성을 시작합니다...");
                Planner.CreateNewDailyPlan(currentTime, this);
                return; // 계획이 생성되는 동안은 이동하지 않음
            }
            
            // 계획 생성 중이면 이동하지 않음
            if (Planner.IsPlanningInProgress)
            {
                Debug.Log("[NPCAgent] 계획 생성 중입니다. 완료될 때까지 대기합니다...");
                return;
            }
            
            // 계획이 준비되지 않았으면 이동하지 않음
            if (!Planner.IsPlanReady)
            {
                Debug.Log("[NPCAgent] 계획이 아직 준비되지 않았습니다.");
                return;
            }

            // 현재 활동 확인
            PlanItem currentActivity = Planner.GetCurrentActivity(currentTime);
            if (currentActivity != null)
            {
                Debug.Log($"[NPCAgent] 📅 Current activity: {currentActivity.activity} @ {currentActivity.location}");
                
                // 이미 목표 장소에 있는지 확인
                NPCMovement movement = GetComponent<NPCMovement>();
                if (movement != null && movement.currentArea != null)
                {
                    string currentAreaName = movement.currentArea.GetFullName();
                    if (currentAreaName.Equals(currentActivity.location, System.StringComparison.OrdinalIgnoreCase) ||
                        movement.currentArea.areaName.Equals(currentActivity.location, System.StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.Log($"[NPCAgent] ✅ Already at target location: {currentAreaName}");
                        
                        // 위치 업데이트
                        CurrentLocation = currentActivity.location;
                        
                        // 🆕 GameObject 선택 및 상호작용 (논문 방식)
                        if (string.IsNullOrEmpty(currentActivity.targetObject))
                        {
                            // AI가 활동에 맞는 오브젝트 자동 선택
                            StartCoroutine(SelectAndInteractWithObject(currentActivity, movement.currentArea));
                        }
                        else
                        {
                            // 계획에 이미 오브젝트가 지정되어 있으면 바로 사용
                            WorldObject targetObj = movement.currentArea.FindObjectByName(currentActivity.targetObject);
                            if (targetObj != null)
                            {
                                InteractWithObject(targetObj, (success) =>
                                {
                                    Debug.Log($"[NPCAgent] Interaction with {targetObj.objectName}: {(success ? "Success" : "Failed")}");
                                });
                            }
                        }
                        
                        // 메모리에 활동 기록 (첫 진입 시만)
                        if (!string.IsNullOrEmpty(currentActivity.activity))
                        {
                            MemoryMgr.AddMemory(
                                MemoryType.Event,
                                $"나는 {currentActivity.location}에서 '{currentActivity.activity}' 활동을 하고 있다.",
                                6,
                                this
                            );
                        }
                        return;
                    }
                }
                
                // 목표 장소로 이동
                Debug.Log($"[NPCAgent] 🚶 Moving to: {currentActivity.location}");
                CurrentLocation = currentActivity.location;
                
                if (movement != null)
                {
                    movement.MoveToArea(currentActivity.location, () =>
                    {
                        Debug.Log($"[NPCAgent] ✅ Arrived at {currentActivity.location}, starting activity: {currentActivity.activity}");
                        
                        // 도착 후 GameObject 선택 및 상호작용
                        if (string.IsNullOrEmpty(currentActivity.targetObject))
                        {
                            StartCoroutine(SelectAndInteractWithObject(currentActivity, movement.currentArea));
                        }
                        else
                        {
                            WorldObject targetObj = movement.currentArea.FindObjectByName(currentActivity.targetObject);
                            if (targetObj != null)
                            {
                                InteractWithObject(targetObj, null);
                            }
                        }
                        
                        // 메모리에 활동 기록
                        MemoryMgr.AddMemory(
                            MemoryType.Event,
                            $"나는 {currentActivity.location}에 도착해서 '{currentActivity.activity}' 활동을 시작했다.",
                            7,
                            this
                        );
                    });
                }
                else
                {
                    Debug.LogWarning("[NPCAgent] ⚠️ NPCMovement component not found!");
                }
            }
            else
            {
                Debug.LogWarning($"[NPCAgent] ⚠️ No activity found for current time: {currentTime:HH:mm}");
            }
        }
        
        /// <summary>
        /// 활동에 맞는 GameObject를 AI로 선택하고 상호작용 (논문 방식)
        /// </summary>
        private IEnumerator SelectAndInteractWithObject(PlanItem activity, WorldArea currentArea)
        {
            if (currentArea == null || currentArea.objectsInArea.Count == 0)
            {
                Debug.Log($"[NPCAgent] No objects in {currentArea?.GetFullName() ?? "current area"}");
                yield break;
            }
            
            Debug.Log($"[NPCAgent] 🔍 Selecting object for activity: {activity.activity}");
            
            // 현재 Area의 오브젝트 목록
            List<string> availableObjects = new List<string>();
            foreach (var obj in currentArea.objectsInArea)
            {
                availableObjects.Add($"{obj.objectName} ({obj.objectType})");
            }
            string objectsStr = string.Join(", ", availableObjects);
            
            // AI에게 오브젝트 선택 요청
            string prompt = $@"
### Current Activity ###
{activity.activity}

### Available Objects in {currentArea.GetFullName()} ###
{objectsStr}

### Task ###
Which object should be used for this activity?
Answer with ONLY the object name (without type).
If no object is needed, answer ""none"".

Answer:";

            string selectedObjectName = "";
            yield return llmClient.GetChatCompletion(prompt, (response) =>
            {
                selectedObjectName = response.Trim().ToLower();
                Debug.Log($"[NPCAgent] AI selected object: {selectedObjectName}");
            }, temperature: 0.3f, maxTokens: 30);
            
            if (selectedObjectName == "none" || string.IsNullOrEmpty(selectedObjectName))
            {
                Debug.Log("[NPCAgent] No object needed for this activity");
                yield break;
            }
            
            // 선택된 오브젝트 찾기
            WorldObject targetObject = null;
            foreach (var obj in currentArea.objectsInArea)
            {
                if (obj.objectName.ToLower().Contains(selectedObjectName) || 
                    selectedObjectName.Contains(obj.objectName.ToLower()))
                {
                    targetObject = obj;
                    break;
                }
            }
            
            if (targetObject != null)
            {
                Debug.Log($"[NPCAgent] 🎯 Found object: {targetObject.objectName}");
                
                // 오브젝트와 상호작용
                InteractWithObject(targetObject, (success) =>
                {
                    if (success)
                    {
                        // 계획에 오브젝트 저장 (다음번에 다시 선택 안 하도록)
                        activity.targetObject = targetObject.objectName;
                        
                        MemoryMgr.AddMemory(
                            MemoryType.Event,
                            $"'{activity.activity}' 활동을 위해 '{targetObject.objectName}'을(를) 사용했다.",
                            6,
                            this
                        );
                    }
                });
            }
            else
            {
                Debug.LogWarning($"[NPCAgent] Could not find object matching: {selectedObjectName}");
            }
        }

        #endregion

        #region Emotion

        /// <summary>
        /// 감정 상태 업데이트
        /// </summary>
        public void UpdateEmotion(string newEmotion)
        {
            string oldEmotion = CurrentEmotion;
            CurrentEmotion = newEmotion;

            Debug.Log($"[NPCAgent] Emotion changed: {oldEmotion} → {newEmotion}");

            MemoryMgr.AddMemory(MemoryType.Thought, $"나의 감정이 '{oldEmotion}'에서 '{newEmotion}'로 변했다.", 6, this);
        }

        #endregion

        #region Object Interaction

        /// <summary>
        /// 오브젝트와 상호작용
        /// </summary>
        public void InteractWithObject(WorldObject targetObject, Action<bool> callback)
        {
            StartCoroutine(GenerativeInteractionCoroutine(targetObject, callback));
        }

        private IEnumerator GenerativeInteractionCoroutine(WorldObject targetObject, Action<bool> callback)
        {
            if (targetObject == null) { callback?.Invoke(false); yield break; }

            // 1. 이동 (기존과 동일)
            float distance = Vector3.Distance(transform.position, targetObject.transform.position);
            if (distance > targetObject.interactionRange)
            {
                Pathfinding.MoveTo(targetObject.transform.position);
                while (Pathfinding.IsMoving) yield return null;
            }

            Debug.Log($"[NPCAgent] {Name} interacting with {targetObject.objectName}...");

            // 2. [STEP 1] 행동 결정하기
            // "이 물건으로 무엇을 할 것인가?"
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

            // 3. [STEP 2] 상태 변화 추론하기 (논문의 Generative Sandbox 핵심!)
            // "내가 이 행동을 하면, 물건의 상태는 어떻게 변하는가?"
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
                // 따옴표나 불필요한 설명 제거
                newState = newState.Replace("The object is now ", "").Replace("\"", "").Replace(".", "");
            }, temperature: 0.3f, maxTokens: 30);

            Debug.Log($"[NPCAgent] New State Inferred: {newState}");

            // 4. 오브젝트 상태 업데이트 (실제 반영)
            targetObject.UpdateState(newState);

            // 5. 기억 저장
            MemoryMgr.AddMemory(
                MemoryType.Event,
                $"{targetObject.objectName}에 대해 행동했다: '{actionDescription}'. 결과 상태: '{newState}'",
                6,
                this
            );

            callback?.Invoke(true);
        }
        
        /// <summary>
        /// 특정 타입의 오브젝트 찾아서 이동
        /// </summary>
        public void FindAndMoveToObjectType(string type, Action<WorldObject> callback)
        {
            StartCoroutine(FindAndMoveToObjectTypeCoroutine(type, callback));
        }

        private IEnumerator FindAndMoveToObjectTypeCoroutine(string type, Action<WorldObject> callback)
        {
            // Perception으로 주변 감지
            Perception.PerceiveEnvironment();
            yield return new WaitForSeconds(0.5f);

            // 해당 타입 오브젝트 검색
            WorldObject target = Perception.FindNearestObjectOfType(type);

            if (target == null)
            {
                Debug.LogWarning($"[NPCAgent] Cannot find object of type {type}");
                callback?.Invoke(null);
                yield break;
            }

            Debug.Log($"[NPCAgent] Found {target.objectName}, moving...");

            // 이동
            bool success = Pathfinding.MoveToObject(target);
            if (!success)
            {
                callback?.Invoke(null);
                yield break;
            }

            // 이동 완료 대기
            while (Pathfinding.IsMoving)
            {
                yield return null;
            }

            callback?.Invoke(target);
        }

        #endregion

        #region Environment Modification

        /// <summary>
        /// 환경 변경이 필요한지 판단
        /// </summary>
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
                // 응답 파싱
                string[] lines = response.Split('\n');
                foreach (string line in lines)
                {
                    if (line.Contains("필요 여부:"))
                    {
                        decision.isNeeded = line.Contains("예");
                    }
                    else if (line.Contains("오브젝트:"))
                    {
                        decision.objectName = line.Split(':')[1].Trim();
                    }
                    else if (line.Contains("이유:"))
                    {
                        decision.reason = line.Split(':')[1].Trim();
                    }
                    else if (line.Contains("위치 힌트:"))
                    {
                        decision.positionHint = line.Split(':')[1].Trim();
                    }
                    else if (line.Contains("프롬프트:"))
                    {
                        decision.imagePrompt = line.Split(':')[1].Trim();
                    }
                }

                Debug.Log($"[NPCAgent] Environment change decision: {decision}");
                callback?.Invoke(decision);
            }, temperature: 0.6f, maxTokens: 300);
        }

        #endregion
    }

    /// <summary>
    /// 환경 변경 결정 데이터
    /// </summary>
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
