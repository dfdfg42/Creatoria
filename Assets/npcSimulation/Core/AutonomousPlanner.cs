using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NPCSimulation.Core
{
    /// <summary>
    /// 자율 행동 계획 시스템
    /// </summary>
    public class AutonomousPlanner
    {
        private NPCAgent npcAgent;
        private OpenAIClient llmClient;

        // 계획 상태
        public List<string> DailyRequirements { get; private set; } = new List<string>();
        public List<PlanItem> DailySchedule { get; private set; } = new List<PlanItem>();
        public int CurrentActivityIndex { get; private set; } = 0;
        
        private DateTime? lastPlanningDate = null;
        private int wakeUpHour = 7;
        
        // 계획 생성 진행 중 플래그
        public bool IsPlanningInProgress { get; private set; } = false;
        public bool IsPlanReady { get; private set; } = false;

        public AutonomousPlanner(NPCAgent npcAgent, OpenAIClient llmClient)
        {
            this.npcAgent = npcAgent;
            this.llmClient = llmClient;
        }

        /// <summary>
        /// 재계획이 필요한지 확인
        /// </summary>
        public bool ShouldReplan(DateTime currentTime)
        {
            DateTime currentDate = currentTime.Date;

            if (lastPlanningDate == null || 
                lastPlanningDate.Value != currentDate || 
                DailySchedule.Count == 0)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 새로운 일일 계획 생성
        /// </summary>
        public void CreateNewDailyPlan(DateTime currentTime, MonoBehaviour coroutineRunner)
        {
            if (IsPlanningInProgress)
            {
                Debug.Log($"[AutonomousPlanner] 이미 계획 생성 중입니다. 건너뜁니다.");
                return;
            }
            
            Debug.Log($"[AutonomousPlanner] Creating new daily plan for {currentTime:yyyy-MM-dd}");
            
            IsPlanningInProgress = true;
            IsPlanReady = false;
            coroutineRunner.StartCoroutine(CreateDailyPlanCoroutine(currentTime));
        }

        private IEnumerator CreateDailyPlanCoroutine(DateTime currentTime)
        {
            Debug.Log($"[AutonomousPlanner] 📅 일일 계획 생성 시작 - {currentTime:yyyy-MM-dd}");
            
            // 1. 기상 시간 결정
            bool wakeUpHourDone = false;
            yield return GenerateWakeUpHour((hour) =>
            {
                wakeUpHour = hour;
                Debug.Log($"[AutonomousPlanner] ⏰ 기상 시간: {hour}시");
                wakeUpHourDone = true;
            });
            while (!wakeUpHourDone) yield return null;

            // 2. 일일 요구사항 생성
            bool requirementsDone = false;
            yield return GenerateDailyRequirements((requirements) =>
            {
                DailyRequirements = requirements;
                Debug.Log($"[AutonomousPlanner] 📋 일일 목표 {requirements.Count}개 생성됨");
                requirementsDone = true;
            });
            while (!requirementsDone) yield return null;

            // 3. 시간별 스케줄 생성
            bool scheduleDone = false;
            yield return GenerateHourlySchedule((schedule) =>
            {
                DailySchedule = schedule;
                CurrentActivityIndex = 0;
                Debug.Log($"[AutonomousPlanner] 🗓️ 시간별 일정 {schedule.Count}개 생성됨");
                scheduleDone = true;
            });
            while (!scheduleDone) yield return null;

            lastPlanningDate = currentTime.Date;
            
            // 계획 생성 완료 플래그 설정
            IsPlanningInProgress = false;
            IsPlanReady = true;

            Debug.Log($"[AutonomousPlanner] ✅ 일일 계획 생성 완료! 총 {DailySchedule.Count}개 활동:");
            foreach (var plan in DailySchedule)
            {
                Debug.Log($"  📍 {plan}");
            }
        }

        /// <summary>
        /// 기상 시간 생성
        /// </summary>
        private IEnumerator GenerateWakeUpHour(Action<int> callback)
        {
            string prompt = $@"
다음은 NPC의 정보입니다:
{npcAgent.Persona}

이 NPC의 성격과 생활 패턴을 고려할 때, 보통 몇 시에 일어날까요?
6시에서 10시 사이의 숫자 하나로만 답해주세요.

예시: 8
";

            yield return llmClient.GetChatCompletion(prompt, (response) =>
            {
                if (int.TryParse(response.Trim(), out int hour))
                {
                    hour = Mathf.Clamp(hour, 6, 10);
                    callback?.Invoke(hour);
                }
                else
                {
                    callback?.Invoke(7); // 기본값
                }
            }, temperature: 0.1f, maxTokens: 10);
        }

        /// <summary>
        /// 일일 요구사항 생성
        /// </summary>
        private IEnumerator GenerateDailyRequirements(Action<List<string>> callback)
        {
            // 최근 기억 가져오기
            List<Memory> recentMemories = npcAgent.MemoryMgr.RetrieveRecentMemories(5);
            string memorySummary = string.Join("\n", recentMemories.Select(m => $"- {m.description}"));

            string conversationSummary = npcAgent.ConversationMgr.GetConversationSummary();

            string prompt = $@"
당신은 {npcAgent.Name}의 하루 계획을 세우는 AI입니다.

### NPC 기본 정보 ###
{npcAgent.Persona}

### 최근 기억 및 경험 ###
{memorySummary}

### 최근 대화 요약 ###
{conversationSummary}

### 지시사항 ###
위 정보를 바탕으로 {npcAgent.Name}의 하루 목표와 해야 할 일들을 4-6개의 주요 활동으로 나열해주세요.
각 활동은 NPC의 성격, 전공, 최근 경험을 반영해야 합니다.

형식: 각 줄마다 한 가지 활동만 작성
예시:
졸업 작품 아이디어 구상하기
수학 과제 완료하기
친구와 카페에서 대화하기
충분한 휴식 취하기

응답:
";

            yield return llmClient.GetChatCompletion(prompt, (response) =>
            {
                List<string> activities = new List<string>();
                string[] lines = response.Split('\n');
                
                foreach (string line in lines)
                {
                    string trimmed = line.Trim();
                    if (!string.IsNullOrEmpty(trimmed) && 
                        !trimmed.StartsWith("#") && 
                        !trimmed.StartsWith("###"))
                    {
                        // 앞의 숫자나 특수문자 제거
                        string cleaned = System.Text.RegularExpressions.Regex.Replace(trimmed, @"^[\d\.\-\*\•]\s*", "");
                        if (!string.IsNullOrEmpty(cleaned))
                        {
                            activities.Add(cleaned);
                        }
                    }
                }

                if (activities.Count == 0)
                {
                    activities = new List<string>
                    {
                        "하루를 계획적으로 보내기",
                        "학업에 집중하기",
                        "적절한 휴식 취하기",
                        "사회적 관계 유지하기"
                    };
                }

                callback?.Invoke(activities);
            }, temperature: 0.4f, maxTokens: 200);
        }

        /// <summary>
        /// Unity Scene에 존재하는 실제 장소 목록 가져오기
        /// WorldArea 컴포넌트를 가진 GameObject만 사용
        /// </summary>
        private List<string> GetAvailableLocations()
        {
            List<string> locations = new List<string>();
            
            // WorldArea 컴포넌트 직접 찾기
            WorldArea[] allAreas = UnityEngine.Object.FindObjectsOfType<WorldArea>();
            foreach (var area in allAreas)
            {
                string fullName = area.GetFullName();
                if (!string.IsNullOrEmpty(fullName) && !locations.Contains(fullName))
                {
                    locations.Add(fullName);
                }
            }
            
            // 장소가 하나도 없으면 기본 장소 사용 (안전장치)
            if (locations.Count == 0)
            {
                Debug.LogWarning("[AutonomousPlanner] Scene에 WorldArea가 없습니다! 기본 장소를 사용합니다.");
                locations.Add("알 수 없는 장소");
            }
            
            Debug.Log($"[AutonomousPlanner] 사용 가능한 장소 {locations.Count}개: {string.Join(", ", locations)}");
            return locations;
        }

        /// <summary>
        /// 시간별 스케줄 생성
        /// </summary>
        private IEnumerator GenerateHourlySchedule(Action<List<PlanItem>> callback)
        {
            string requirementsStr = string.Join("\n", DailyRequirements.Select(r => $"- {r}"));
            
            // ⭐ Unity Scene에 실제로 존재하는 장소만 가져오기
            List<string> availableLocations = GetAvailableLocations();
            string locationsStr = string.Join(", ", availableLocations);

            string prompt = $@"
다음은 {npcAgent.Name}의 하루 목표입니다:
{requirementsStr}

### NPC 정보 ###
{npcAgent.Persona}

### ⚠️ 사용 가능한 장소 (반드시 이 중에서만 선택!) ###
{locationsStr}

### ⚠️ 중요 규칙 ###
1. 장소는 위 목록에서 정확히 복사해서 사용하세요
2. 장소 이름을 번역하거나 변경하지 마세요
3. 콜론(:)과 대소문자를 정확히 맞추세요

### 지시사항 ###
{wakeUpHour}시에 일어나서 하루 종일(24시간) 동안의 활동을 시간 순서대로 계획해주세요.

형식: 시간 | 활동 | 장소
예시 (위의 실제 장소 목록 사용):
07:00 | wake up | {availableLocations[0]}
09:00 | study | {(availableLocations.Count > 1 ? availableLocations[1] : availableLocations[0])}

응답:
";

            yield return llmClient.GetChatCompletion(prompt, (response) =>
            {
                List<PlanItem> schedule = ParseScheduleResponse(response);
                callback?.Invoke(schedule);
            }, temperature: 0.5f, maxTokens: 500);
        }

        /// <summary>
        /// LLM 응답을 PlanItem 리스트로 파싱
        /// </summary>
        private List<PlanItem> ParseScheduleResponse(string response)
        {
            List<PlanItem> schedule = new List<PlanItem>();
            string[] lines = response.Split('\n');

            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                    continue;

                // 형식: "07:00 | 기상 및 아침 식사 | 집:부엌"
                string[] parts = trimmed.Split('|');
                if (parts.Length >= 3)
                {
                    // 시간 파싱
                    string timeStr = parts[0].Trim();
                    if (timeStr.Contains(":"))
                    {
                        string hourStr = timeStr.Split(':')[0];
                        if (int.TryParse(hourStr, out int hour))
                        {
                            string activity = parts[1].Trim();
                            string location = parts[2].Trim();

                            // 지속 시간 계산 (다음 활동까지)
                            int duration = 1;

                            PlanItem item = new PlanItem(hour, duration, activity, location);
                            schedule.Add(item);
                        }
                    }
                }
            }

            // 지속 시간 계산
            for (int i = 0; i < schedule.Count - 1; i++)
            {
                int nextHour = schedule[i + 1].startHour;
                int currentHour = schedule[i].startHour;
                schedule[i].duration = nextHour - currentHour;
            }

            // 마지막 활동은 다음날 첫 활동까지
            if (schedule.Count > 0)
            {
                int lastIndex = schedule.Count - 1;
                int firstHour = schedule[0].startHour;
                int lastHour = schedule[lastIndex].startHour;
                schedule[lastIndex].duration = (24 - lastHour) + firstHour;
            }

            return schedule;
        }

        /// <summary>
        /// 현재 시간에 해당하는 활동 가져오기
        /// </summary>
        public PlanItem GetCurrentActivity(DateTime currentTime)
        {
            int currentHour = currentTime.Hour;
            
            Debug.Log($"[AutonomousPlanner] 현재 시간 {currentHour}시에 해당하는 활동 검색 중... (총 {DailySchedule.Count}개 일정)");

            foreach (var plan in DailySchedule)
            {
                int endHour = (plan.startHour + plan.duration) % 24;
                
                // 하루를 넘어가는 경우 처리
                bool isInRange;
                if (endHour <= plan.startHour) // 자정을 넘어가는 경우
                {
                    isInRange = currentHour >= plan.startHour || currentHour < endHour;
                }
                else
                {
                    isInRange = currentHour >= plan.startHour && currentHour < endHour;
                }
                
                if (isInRange)
                {
                    Debug.Log($"[AutonomousPlanner] ✅ 찾음! {plan}");
                    return plan;
                }
            }

            Debug.LogWarning($"[AutonomousPlanner] ⚠️ {currentHour}시에 해당하는 활동을 찾지 못했습니다.");
            return null;
        }
    }
}
