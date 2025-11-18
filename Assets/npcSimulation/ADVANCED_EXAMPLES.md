# 🎓 고급 예제 시나리오

## 📚 목차
1. [감정 기반 행동](#감정-기반-행동)
2. [복잡한 일과 만들기](#복잡한-일과-만들기)
3. [멀티 NPC 상호작용](#멀티-npc-상호작용)
4. [동적 환경 변화 반응](#동적-환경-변화-반응)
5. [커스텀 상태 머신](#커스텀-상태-머신)

---

## 1. 감정 기반 행동

### 시나리오: 날씨에 따라 기분이 변하는 NPC

```csharp
using UnityEngine;
using NPCSimulation.Core;
using System.Collections;

public class EmotionalNPC : MonoBehaviour
{
    public NPCAgent agent;
    public float moodCheckInterval = 120f; // 2분마다 기분 체크
    
    private enum Mood { Happy, Sad, Tired, Energetic, Anxious }
    private Mood currentMood = Mood.Happy;
    
    private void Start()
    {
        StartCoroutine(MoodUpdateRoutine());
    }
    
    private IEnumerator MoodUpdateRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(moodCheckInterval);
            UpdateMood();
            ReactToMood();
        }
    }
    
    private void UpdateMood()
    {
        // 최근 기억에서 감정 추출
        var recentMemories = agent.Memory.GetRecentMemories(20);
        
        int positiveCount = 0;
        int negativeCount = 0;
        
        foreach (var memory in recentMemories)
        {
            if (memory.description.Contains("즐거운") || 
                memory.description.Contains("행복한") ||
                memory.description.Contains("성공"))
            {
                positiveCount++;
            }
            else if (memory.description.Contains("슬픈") || 
                     memory.description.Contains("실패") ||
                     memory.description.Contains("피곤"))
            {
                negativeCount++;
            }
        }
        
        // 기분 결정
        if (positiveCount > negativeCount + 3)
        {
            currentMood = Mood.Happy;
        }
        else if (negativeCount > positiveCount + 3)
        {
            currentMood = Mood.Sad;
        }
        else if (recentMemories.Count > 15)
        {
            currentMood = Mood.Tired;
        }
        else
        {
            currentMood = Mood.Energetic;
        }
        
        Debug.Log($"[EmotionalNPC] {agent.NPCName}의 기분: {currentMood}");
    }
    
    private void ReactToMood()
    {
        switch (currentMood)
        {
            case Mood.Happy:
                // 행복하면 친구에게 가기
                StartCoroutine(FindAndMeetFriend());
                break;
                
            case Mood.Sad:
                // 슬프면 조용한 곳으로 이동
                StartCoroutine(FindQuietPlace());
                break;
                
            case Mood.Tired:
                // 피곤하면 휴식
                StartCoroutine(TakeRest());
                break;
                
            case Mood.Energetic:
                // 활기차면 새로운 활동
                StartCoroutine(ExploreEnvironment());
                break;
                
            case Mood.Anxious:
                // 불안하면 안전한 곳으로
                StartCoroutine(SeekSafety());
                break;
        }
    }
    
    private IEnumerator FindAndMeetFriend()
    {
        Debug.Log($"[EmotionalNPC] {agent.NPCName}가 친구를 찾습니다.");
        
        var nearbyAgents = agent.Perception.GetNearbyAgents();
        if (nearbyAgents.Count > 0)
        {
            NPCAgent friend = nearbyAgents[0];
            agent.Pathfinding.MoveTo(friend.transform.position);
            
            yield return new WaitUntil(() => 
                Vector2.Distance(transform.position, friend.transform.position) < 2f);
            
            // 대화 시작
            yield return StartCoroutine(agent.RespondToPlayer(
                $"안녕! 오늘 기분이 정말 좋아. 같이 시간 보낼래?"));
        }
    }
    
    private IEnumerator FindQuietPlace()
    {
        Debug.Log($"[EmotionalNPC] {agent.NPCName}가 조용한 곳을 찾습니다.");
        
        // 사람이 없는 곳 찾기
        float searchRadius = 10f;
        Vector2 quietPlace = FindEmptyArea(searchRadius);
        
        agent.Pathfinding.MoveTo(quietPlace);
        yield return new WaitUntil(() => agent.Pathfinding.IsMoving == false);
        
        // 메모리에 기록
        agent.Memory.AddMemory(
            Memory.MemoryType.Reflection,
            "조용한 곳에서 혼자 시간을 보내며 생각을 정리했다.",
            6
        );
    }
    
    private IEnumerator TakeRest()
    {
        Debug.Log($"[EmotionalNPC] {agent.NPCName}가 휴식을 취합니다.");
        
        // 의자나 침대 찾기
        WorldObject restSpot = agent.Perception.FindObjectByType(WorldObject.ObjectType.Furniture);
        
        if (restSpot != null)
        {
            agent.Pathfinding.MoveToObject(restSpot);
            yield return new WaitUntil(() => agent.Pathfinding.IsMoving == false);
            
            // 휴식 상태 유지 (30초)
            yield return new WaitForSeconds(30f);
            
            agent.Memory.AddMemory(
                Memory.MemoryType.Event,
                $"{restSpot.objectName}에서 휴식을 취해 피로가 풀렸다.",
                5
            );
            
            // 기분 개선
            currentMood = Mood.Happy;
        }
    }
    
    private IEnumerator ExploreEnvironment()
    {
        Debug.Log($"[EmotionalNPC] {agent.NPCName}가 주변을 탐험합니다.");
        
        for (int i = 0; i < 3; i++)
        {
            Vector2 randomPoint = GetRandomPointInRadius(8f);
            agent.Pathfinding.MoveTo(randomPoint);
            
            yield return new WaitUntil(() => agent.Pathfinding.IsMoving == false);
            
            // 주변 감지
            agent.Perception.PerceiveEnvironment();
            yield return new WaitForSeconds(2f);
        }
        
        agent.Memory.AddMemory(
            Memory.MemoryType.Event,
            "새로운 장소들을 탐험하며 흥미로운 것들을 발견했다.",
            6
        );
    }
    
    private IEnumerator SeekSafety()
    {
        Debug.Log($"[EmotionalNPC] {agent.NPCName}가 안전한 곳으로 이동합니다.");
        
        // 집이나 안전한 장소로 이동
        WorldObject safePlace = agent.Perception.FindObjectByName("집") ?? 
                                agent.Perception.FindObjectByName("방");
        
        if (safePlace != null)
        {
            agent.Pathfinding.MoveToObject(safePlace);
            yield return new WaitUntil(() => agent.Pathfinding.IsMoving == false);
            
            agent.Memory.AddMemory(
                Memory.MemoryType.Reflection,
                "안전한 곳으로 와서 마음이 진정되었다.",
                5
            );
            
            currentMood = Mood.Happy;
        }
    }
    
    // 유틸리티
    private Vector2 FindEmptyArea(float radius)
    {
        for (int i = 0; i < 10; i++)
        {
            Vector2 candidate = GetRandomPointInRadius(radius);
            var agents = Physics2D.OverlapCircleAll(candidate, 3f);
            
            if (agents.Length == 0)
            {
                return candidate;
            }
        }
        return transform.position;
    }
    
    private Vector2 GetRandomPointInRadius(float radius)
    {
        Vector2 randomDir = Random.insideUnitCircle * radius;
        return (Vector2)transform.position + randomDir;
    }
}
```

---

## 2. 복잡한 일과 만들기

### 시나리오: 대학생의 하루

```csharp
using UnityEngine;
using NPCSimulation.Core;
using System.Collections.Generic;
using System;

public class StudentSchedule : MonoBehaviour
{
    public NPCAgent student;
    public Transform home;
    public Transform university;
    public Transform cafe;
    public Transform library;
    
    private Dictionary<string, Action> scheduleActions;
    
    private void Start()
    {
        InitializeSchedule();
        StartCoroutine(DailyRoutine());
    }
    
    private void InitializeSchedule()
    {
        scheduleActions = new Dictionary<string, Action>
        {
            { "07:00", WakeUp },
            { "08:00", GoToUniversity },
            { "09:00", AttendClass },
            { "12:00", LunchBreak },
            { "13:00", StudyAtLibrary },
            { "16:00", WorkOnProject },
            { "18:00", MeetFriends },
            { "20:00", GoHome },
            { "21:00", RelaxAndReflect },
            { "23:00", Sleep }
        };
    }
    
    private IEnumerator DailyRoutine()
    {
        while (true)
        {
            string currentTime = DateTime.Now.ToString("HH:00");
            
            if (scheduleActions.ContainsKey(currentTime))
            {
                Debug.Log($"[StudentSchedule] 시간: {currentTime} - 활동 시작");
                scheduleActions[currentTime].Invoke();
            }
            
            // 1분마다 체크
            yield return new WaitForSeconds(60f);
        }
    }
    
    // === 활동 메서드들 ===
    
    private void WakeUp()
    {
        StartCoroutine(WakeUpRoutine());
    }
    
    private IEnumerator WakeUpRoutine()
    {
        Debug.Log($"{student.NPCName} 기상 중...");
        
        // 침대에서 일어나기
        WorldObject bed = student.Perception.FindObjectByType(WorldObject.ObjectType.Furniture);
        if (bed != null)
        {
            yield return StartCoroutine(student.InteractWithObjectCoroutine(bed, null));
        }
        
        // 아침 일과
        yield return new WaitForSeconds(5f);
        
        student.Memory.AddMemory(
            Memory.MemoryType.Event,
            "상쾌하게 아침에 일어났다. 오늘도 좋은 하루가 될 것 같다.",
            5
        );
    }
    
    private void GoToUniversity()
    {
        StartCoroutine(TravelTo(university, "대학교"));
    }
    
    private void AttendClass()
    {
        StartCoroutine(AttendClassRoutine());
    }
    
    private IEnumerator AttendClassRoutine()
    {
        Debug.Log($"{student.NPCName} 수업 참여 중...");
        
        // 3시간 수업 (실제로는 30초로 시뮬레이션)
        yield return new WaitForSeconds(30f);
        
        // 수업 내용 메모리에 기록
        string[] classTopics = {
            "타이포그래피의 기본 원칙",
            "색채 이론과 심리학",
            "UI/UX 디자인 트렌드",
            "그리드 시스템의 활용"
        };
        
        string topic = classTopics[UnityEngine.Random.Range(0, classTopics.Length)];
        
        student.Memory.AddMemory(
            Memory.MemoryType.Knowledge,
            $"시각디자인 수업에서 '{topic}'에 대해 배웠다.",
            7
        );
    }
    
    private void LunchBreak()
    {
        StartCoroutine(LunchBreakRoutine());
    }
    
    private IEnumerator LunchBreakRoutine()
    {
        Debug.Log($"{student.NPCName} 점심 식사 중...");
        
        // 카페로 이동
        yield return StartCoroutine(TravelTo(cafe, "카페"));
        
        // 음식 주문
        WorldObject counter = student.Perception.FindObjectByType(WorldObject.ObjectType.Furniture);
        if (counter != null)
        {
            yield return StartCoroutine(student.InteractWithObjectCoroutine(counter, null));
        }
        
        // 식사 (20초)
        yield return new WaitForSeconds(20f);
        
        student.Memory.AddMemory(
            Memory.MemoryType.Event,
            "카페에서 샌드위치와 라떼를 먹으며 잠시 휴식을 취했다.",
            4
        );
    }
    
    private void StudyAtLibrary()
    {
        StartCoroutine(StudyRoutine());
    }
    
    private IEnumerator StudyRoutine()
    {
        Debug.Log($"{student.NPCName} 도서관에서 공부 중...");
        
        // 도서관 이동
        yield return StartCoroutine(TravelTo(library, "도서관"));
        
        // 공부 (1시간 = 60초)
        yield return new WaitForSeconds(60f);
        
        // 메모리 추가
        student.Memory.AddMemory(
            Memory.MemoryType.Reflection,
            "도서관에서 집중해서 공부했다. 졸업 작품 아이디어가 조금씩 구체화되고 있다.",
            6
        );
    }
    
    private void WorkOnProject()
    {
        StartCoroutine(ProjectWorkRoutine());
    }
    
    private IEnumerator ProjectWorkRoutine()
    {
        Debug.Log($"{student.NPCName} 프로젝트 작업 중...");
        
        // 컴퓨터 찾기
        WorldObject computer = student.Perception.FindObjectByName("컴퓨터");
        
        if (computer != null)
        {
            student.Pathfinding.MoveToObject(computer);
            yield return new WaitUntil(() => student.Pathfinding.IsMoving == false);
            
            // 컴퓨터 켜기
            if (computer.GetState("power") == "off")
            {
                yield return StartCoroutine(student.InteractWithObjectCoroutine(computer, null));
            }
            
            // 작업 (2시간 = 120초)
            yield return new WaitForSeconds(120f);
            
            student.Memory.AddMemory(
                Memory.MemoryType.Event,
                "졸업 작품 포트폴리오 작업을 진행했다. 몇 가지 디자인 시안을 완성했다.",
                8
            );
        }
    }
    
    private void MeetFriends()
    {
        StartCoroutine(SocialRoutine());
    }
    
    private IEnumerator SocialRoutine()
    {
        Debug.Log($"{student.NPCName} 친구들과 만남...");
        
        // 카페로 이동
        yield return StartCoroutine(TravelTo(cafe, "카페"));
        
        // 근처 다른 NPC 찾기
        var nearbyAgents = student.Perception.GetNearbyAgents();
        
        if (nearbyAgents.Count > 0)
        {
            NPCAgent friend = nearbyAgents[0];
            
            // 대화
            yield return StartCoroutine(student.RespondToPlayer(
                "오늘 하루 어땠어? 나는 수업도 듣고 프로젝트 작업도 해서 바빴어."));
            
            yield return new WaitForSeconds(3f);
            
            student.Memory.AddMemory(
                Memory.MemoryType.Social,
                $"{friend.NPCName}와 카페에서 만나 이야기를 나눴다. 즐거운 시간이었다.",
                7
            );
        }
    }
    
    private void GoHome()
    {
        StartCoroutine(TravelTo(home, "집"));
    }
    
    private void RelaxAndReflect()
    {
        StartCoroutine(ReflectRoutine());
    }
    
    private IEnumerator ReflectRoutine()
    {
        Debug.Log($"{student.NPCName} 하루 정리 중...");
        
        // 침대나 소파로 이동
        WorldObject furniture = student.Perception.FindObjectByType(WorldObject.ObjectType.Furniture);
        
        if (furniture != null)
        {
            student.Pathfinding.MoveToObject(furniture);
            yield return new WaitUntil(() => student.Pathfinding.IsMoving == false);
        }
        
        // 하루 반성
        var todayMemories = student.Memory.GetMemoriesFromToday();
        
        string reflection = "오늘은 ";
        if (todayMemories.Count > 15)
        {
            reflection += "정말 바쁜 하루였다. 많은 일을 했지만 보람찼다.";
        }
        else if (todayMemories.Count > 10)
        {
            reflection += "적당히 활동적인 하루였다.";
        }
        else
        {
            reflection += "조금 여유로운 하루였다. 내일은 더 열심히 해야겠다.";
        }
        
        student.Memory.AddMemory(
            Memory.MemoryType.Reflection,
            reflection,
            9
        );
        
        yield return new WaitForSeconds(10f);
    }
    
    private void Sleep()
    {
        StartCoroutine(SleepRoutine());
    }
    
    private IEnumerator SleepRoutine()
    {
        Debug.Log($"{student.NPCName} 취침 중...");
        
        WorldObject bed = student.Perception.FindObjectByType(WorldObject.ObjectType.Furniture);
        
        if (bed != null)
        {
            student.Pathfinding.MoveToObject(bed);
            yield return new WaitUntil(() => student.Pathfinding.IsMoving == false);
            
            // 침대 상태 변경
            if (bed.GetState("occupied") == "empty")
            {
                yield return StartCoroutine(student.InteractWithObjectCoroutine(bed, null));
            }
        }
        
        student.Memory.AddMemory(
            Memory.MemoryType.Event,
            "피곤한 하루를 마치고 잠자리에 들었다.",
            4
        );
        
        // 수면 (실제로는 30초)
        yield return new WaitForSeconds(30f);
    }
    
    // === 유틸리티 ===
    
    private IEnumerator TravelTo(Transform destination, string locationName)
    {
        Debug.Log($"{student.NPCName} → {locationName}으로 이동 중...");
        
        student.Pathfinding.MoveTo(destination.position);
        
        yield return new WaitUntil(() => student.Pathfinding.IsMoving == false);
        
        Debug.Log($"{student.NPCName} {locationName}에 도착!");
        
        student.Memory.AddMemory(
            Memory.MemoryType.Event,
            $"{locationName}에 도착했다.",
            3
        );
    }
}
```

**사용법:**
```
1. Empty GameObject 생성: "StudentScheduleManager"
2. StudentSchedule 컴포넌트 추가
3. Inspector에서:
   - Student: [NPC 드래그]
   - Home: [집 Transform]
   - University: [대학교 Transform]
   - Cafe: [카페 Transform]
   - Library: [도서관 Transform]
```

---

## 3. 멀티 NPC 상호작용

### 시나리오: 여러 NPC가 협력하여 작업

```csharp
using UnityEngine;
using NPCSimulation.Core;
using System.Collections.Generic;
using System.Linq;

public class CollaborativeTask : MonoBehaviour
{
    [System.Serializable]
    public class TaskAssignment
    {
        public NPCAgent agent;
        public string taskDescription;
        public WorldObject targetObject;
        public bool isCompleted;
    }
    
    public List<NPCAgent> teamMembers;
    public List<TaskAssignment> tasks = new List<TaskAssignment>();
    
    public string projectGoal = "방 청소하기";
    
    private void Start()
    {
        AssignTasks();
        StartCoroutine(MonitorProgress());
    }
    
    private void AssignTasks()
    {
        Debug.Log($"[CollaborativeTask] '{projectGoal}' 프로젝트 시작!");
        
        // 모든 WorldObject 찾기
        WorldObject[] allObjects = FindObjectsOfType<WorldObject>();
        var dirtyObjects = allObjects.Where(obj => obj.GetState("cleanliness") == "dirty").ToList();
        
        // 각 NPC에게 작업 할당
        for (int i = 0; i < teamMembers.Count && i < dirtyObjects.Count; i++)
        {
            TaskAssignment task = new TaskAssignment
            {
                agent = teamMembers[i],
                taskDescription = $"{dirtyObjects[i].objectName} 청소하기",
                targetObject = dirtyObjects[i],
                isCompleted = false
            };
            
            tasks.Add(task);
            
            // NPC에게 작업 알림
            StartCoroutine(AssignTaskToAgent(task));
        }
    }
    
    private IEnumerator AssignTaskToAgent(TaskAssignment task)
    {
        Debug.Log($"[CollaborativeTask] {task.agent.NPCName}에게 '{task.taskDescription}' 할당");
        
        // NPC 메모리에 추가
        task.agent.Memory.AddMemory(
            Memory.MemoryType.Plan,
            $"팀 프로젝트: {task.taskDescription}",
            8
        );
        
        // 오브젝트로 이동
        task.agent.Pathfinding.MoveToObject(task.targetObject);
        
        yield return new WaitUntil(() => task.agent.Pathfinding.IsMoving == false);
        
        // 상호작용
        yield return StartCoroutine(task.agent.InteractWithObjectCoroutine(
            task.targetObject,
            (success) =>
            {
                if (success)
                {
                    task.isCompleted = true;
                    Debug.Log($"[CollaborativeTask] {task.agent.NPCName}가 '{task.taskDescription}' 완료!");
                    
                    // 팀원들에게 완료 알림
                    BroadcastCompletion(task);
                }
            }
        ));
    }
    
    private void BroadcastCompletion(TaskAssignment completedTask)
    {
        foreach (var member in teamMembers)
        {
            if (member != completedTask.agent)
            {
                member.Memory.AddMemory(
                    Memory.MemoryType.Social,
                    $"{completedTask.agent.NPCName}가 {completedTask.taskDescription}를 완료했다.",
                    5
                );
            }
        }
    }
    
    private IEnumerator MonitorProgress()
    {
        while (true)
        {
            yield return new WaitForSeconds(10f);
            
            int completedCount = tasks.Count(t => t.isCompleted);
            int totalCount = tasks.Count;
            
            Debug.Log($"[CollaborativeTask] 진행률: {completedCount}/{totalCount}");
            
            if (completedCount == totalCount && totalCount > 0)
            {
                OnProjectComplete();
                break;
            }
        }
    }
    
    private void OnProjectComplete()
    {
        Debug.Log($"[CollaborativeTask] 프로젝트 '{projectGoal}' 완료!");
        
        // 모든 팀원에게 완료 알림
        foreach (var member in teamMembers)
        {
            member.Memory.AddMemory(
                Memory.MemoryType.Reflection,
                $"팀과 함께 '{projectGoal}'를 성공적으로 완료했다. 협력이 중요하다는 걸 깨달았다.",
                9
            );
        }
        
        // 축하 이벤트
        StartCoroutine(CelebrationEvent());
    }
    
    private IEnumerator CelebrationEvent()
    {
        Debug.Log("[CollaborativeTask] 축하 이벤트 시작!");
        
        // 모든 NPC를 중앙으로 모으기
        Vector2 meetingPoint = CalculateCenterPoint();
        
        foreach (var member in teamMembers)
        {
            member.Pathfinding.MoveTo(meetingPoint);
        }
        
        // 모두 도착할 때까지 대기
        yield return new WaitForSeconds(10f);
        
        // 대화
        for (int i = 0; i < teamMembers.Count; i++)
        {
            yield return StartCoroutine(teamMembers[i].RespondToPlayer(
                i == 0 ? "다들 수고했어! 정말 잘 해냈어." :
                i == 1 ? "협력해서 하니까 훨씬 빨리 끝났네!" :
                "다음에도 같이 하자!"
            ));
            
            yield return new WaitForSeconds(2f);
        }
    }
    
    private Vector2 CalculateCenterPoint()
    {
        Vector2 sum = Vector2.zero;
        foreach (var member in teamMembers)
        {
            sum += (Vector2)member.transform.position;
        }
        return sum / teamMembers.Count;
    }
}
```

---

## 4. 동적 환경 변화 반응

### 시나리오: 화재 발생 시 대피

```csharp
using UnityEngine;
using NPCSimulation.Core;
using System.Collections;

public class EmergencyResponse : MonoBehaviour
{
    public NPCAgent[] npcs;
    public Transform[] exitPoints;
    public GameObject firePrefab;
    
    private bool emergencyActive = false;
    
    public void TriggerFire(Vector2 location)
    {
        if (emergencyActive) return;
        
        StartCoroutine(FireEmergency(location));
    }
    
    private IEnumerator FireEmergency(Vector2 fireLocation)
    {
        emergencyActive = true;
        
        Debug.Log("[EmergencyResponse] 화재 발생!");
        
        // 화재 시각 효과
        GameObject fire = Instantiate(firePrefab, fireLocation, Quaternion.identity);
        
        // 모든 NPC에게 긴급 상황 알림
        foreach (var npc in npcs)
        {
            StartCoroutine(NPCRespondToFire(npc, fireLocation));
        }
        
        // 30초 후 화재 진압
        yield return new WaitForSeconds(30f);
        
        Destroy(fire);
        emergencyActive = false;
        
        Debug.Log("[EmergencyResponse] 화재 진압 완료");
        
        // 복구
        foreach (var npc in npcs)
        {
            npc.Memory.AddMemory(
                Memory.MemoryType.Event,
                "화재가 진압되었다. 안전하게 대피했던 것이 다행이다.",
                9
            );
        }
    }
    
    private IEnumerator NPCRespondToFire(NPCAgent npc, Vector2 fireLocation)
    {
        // 1. 감지
        float distance = Vector2.Distance(npc.transform.position, fireLocation);
        
        if (distance < 10f)
        {
            Debug.Log($"[EmergencyResponse] {npc.NPCName}가 화재를 감지!");
            
            npc.Memory.AddMemory(
                Memory.MemoryType.Event,
                "화재가 발생했다! 빨리 대피해야 한다!",
                10
            );
            
            // 2. 패닉 상태
            yield return new WaitForSeconds(1f);
            
            // 3. 가장 가까운 출구 찾기
            Transform nearestExit = FindNearestExit(npc.transform.position);
            
            if (nearestExit != null)
            {
                Debug.Log($"[EmergencyResponse] {npc.NPCName} → 출구로 대피 중");
                
                // 빠른 속도로 이동
                float originalSpeed = npc.Pathfinding.moveSpeed;
                npc.Pathfinding.moveSpeed = originalSpeed * 1.5f;
                
                npc.Pathfinding.MoveTo(nearestExit.position);
                
                yield return new WaitUntil(() => npc.Pathfinding.IsMoving == false);
                
                // 속도 복구
                npc.Pathfinding.moveSpeed = originalSpeed;
                
                Debug.Log($"[EmergencyResponse] {npc.NPCName} 안전하게 대피 완료!");
                
                npc.Memory.AddMemory(
                    Memory.MemoryType.Event,
                    "출구를 통해 안전하게 대피했다. 정말 무서웠다.",
                    10
                );
            }
        }
    }
    
    private Transform FindNearestExit(Vector3 position)
    {
        Transform nearest = null;
        float minDistance = float.MaxValue;
        
        foreach (var exit in exitPoints)
        {
            float distance = Vector3.Distance(position, exit.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = exit;
            }
        }
        
        return nearest;
    }
}
```

**테스트:**
```csharp
// 다른 스크립트에서 화재 발생
EmergencyResponse emergency = FindObjectOfType<EmergencyResponse>();
emergency.TriggerFire(new Vector2(5, 5));
```

---

## 5. 커스텀 상태 머신

### 시나리오: 상점 주인 NPC

```csharp
using UnityEngine;
using NPCSimulation.Core;
using System.Collections;
using System.Collections.Generic;

public class ShopkeeperAI : MonoBehaviour
{
    public NPCAgent shopkeeper;
    public Transform counter;
    public Transform storage;
    public List<WorldObject> merchandise;
    
    private enum ShopState
    {
        Opening,      // 가게 오픈 준비
        Serving,      // 손님 응대
        Restocking,   // 재고 정리
        Closing,      // 가게 마감
        Resting       // 휴식
    }
    
    private ShopState currentState = ShopState.Opening;
    private float stateTimer = 0f;
    
    private void Start()
    {
        StartCoroutine(ShopRoutine());
    }
    
    private IEnumerator ShopRoutine()
    {
        while (true)
        {
            switch (currentState)
            {
                case ShopState.Opening:
                    yield return StartCoroutine(OpeningRoutine());
                    break;
                    
                case ShopState.Serving:
                    yield return StartCoroutine(ServingRoutine());
                    break;
                    
                case ShopState.Restocking:
                    yield return StartCoroutine(RestockingRoutine());
                    break;
                    
                case ShopState.Closing:
                    yield return StartCoroutine(ClosingRoutine());
                    break;
                    
                case ShopState.Resting:
                    yield return StartCoroutine(RestingRoutine());
                    break;
            }
            
            yield return null;
        }
    }
    
    private IEnumerator OpeningRoutine()
    {
        Debug.Log("[ShopkeeperAI] 가게 오픈 준비 중...");
        
        // 카운터로 이동
        shopkeeper.Pathfinding.MoveTo(counter.position);
        yield return new WaitUntil(() => shopkeeper.Pathfinding.IsMoving == false);
        
        // 조명 켜기
        WorldObject lights = shopkeeper.Perception.FindObjectByType(WorldObject.ObjectType.Light);
        if (lights != null && lights.GetState("power") == "off")
        {
            yield return StartCoroutine(shopkeeper.InteractWithObjectCoroutine(lights, null));
        }
        
        // 상품 진열
        foreach (var item in merchandise)
        {
            if (item.GetState("visibility") == "hidden")
            {
                item.SetState("visibility", "visible");
                yield return new WaitForSeconds(1f);
            }
        }
        
        shopkeeper.Memory.AddMemory(
            Memory.MemoryType.Event,
            "가게 오픈 준비를 완료했다. 손님을 맞이할 준비가 되었다.",
            5
        );
        
        // 다음 상태
        currentState = ShopState.Serving;
        stateTimer = 0f;
    }
    
    private IEnumerator ServingRoutine()
    {
        Debug.Log("[ShopkeeperAI] 손님 응대 중...");
        
        // 카운터에서 대기
        if (Vector2.Distance(shopkeeper.transform.position, counter.position) > 1f)
        {
            shopkeeper.Pathfinding.MoveTo(counter.position);
            yield return new WaitUntil(() => shopkeeper.Pathfinding.IsMoving == false);
        }
        
        // 근처 손님 확인
        var customers = shopkeeper.Perception.GetNearbyAgents();
        
        if (customers.Count > 0)
        {
            NPCAgent customer = customers[0];
            
            // 인사
            yield return StartCoroutine(shopkeeper.RespondToPlayer(
                "어서오세요! 무엇을 도와드릴까요?"));
            
            yield return new WaitForSeconds(3f);
            
            // 응대
            yield return StartCoroutine(shopkeeper.RespondToPlayer(
                "원하시는 물건이 있으시면 말씀해주세요."));
            
            shopkeeper.Memory.AddMemory(
                Memory.MemoryType.Social,
                $"{customer.NPCName} 손님을 응대했다.",
                6
            );
            
            yield return new WaitForSeconds(5f);
        }
        
        // 10분마다 재고 확인
        stateTimer += Time.deltaTime;
        if (stateTimer > 600f) // 10분
        {
            currentState = ShopState.Restocking;
            stateTimer = 0f;
        }
        
        // 저녁 6시면 마감
        if (System.DateTime.Now.Hour >= 18)
        {
            currentState = ShopState.Closing;
        }
        
        yield return new WaitForSeconds(5f);
    }
    
    private IEnumerator RestockingRoutine()
    {
        Debug.Log("[ShopkeeperAI] 재고 정리 중...");
        
        // 창고로 이동
        shopkeeper.Pathfinding.MoveTo(storage.position);
        yield return new WaitUntil(() => shopkeeper.Pathfinding.IsMoving == false);
        
        // 재고 확인 (5초)
        yield return new WaitForSeconds(5f);
        
        // 상품 재정렬
        foreach (var item in merchandise)
        {
            if (item.GetState("arrangement") == "messy")
            {
                item.SetState("arrangement", "organized");
                yield return new WaitForSeconds(2f);
            }
        }
        
        shopkeeper.Memory.AddMemory(
            Memory.MemoryType.Event,
            "재고를 확인하고 상품을 재정렬했다.",
            5
        );
        
        // 카운터로 복귀
        shopkeeper.Pathfinding.MoveTo(counter.position);
        yield return new WaitUntil(() => shopkeeper.Pathfinding.IsMoving == false);
        
        currentState = ShopState.Serving;
    }
    
    private IEnumerator ClosingRoutine()
    {
        Debug.Log("[ShopkeeperAI] 가게 마감 중...");
        
        // 조명 끄기
        WorldObject lights = shopkeeper.Perception.FindObjectByType(WorldObject.ObjectType.Light);
        if (lights != null && lights.GetState("power") == "on")
        {
            yield return StartCoroutine(shopkeeper.InteractWithObjectCoroutine(lights, null));
        }
        
        // 상품 덮기
        foreach (var item in merchandise)
        {
            item.SetState("visibility", "hidden");
            yield return new WaitForSeconds(0.5f);
        }
        
        // 매출 정리 (시뮬레이션)
        yield return new WaitForSeconds(5f);
        
        shopkeeper.Memory.AddMemory(
            Memory.MemoryType.Reflection,
            "오늘 하루 장사를 마쳤다. 손님들이 만족해했으면 좋겠다.",
            7
        );
        
        currentState = ShopState.Resting;
    }
    
    private IEnumerator RestingRoutine()
    {
        Debug.Log("[ShopkeeperAI] 휴식 중...");
        
        // 휴게실로 이동
        yield return new WaitForSeconds(3f);
        
        // 하루 정리
        var todayMemories = shopkeeper.Memory.GetMemoriesFromToday();
        int customerCount = todayMemories.FindAll(m => 
            m.type == Memory.MemoryType.Social).Count;
        
        shopkeeper.Memory.AddMemory(
            Memory.MemoryType.Reflection,
            $"오늘 {customerCount}명의 손님을 맞이했다. 내일도 열심히 해야겠다.",
            8
        );
        
        // 다음날 준비
        yield return new WaitForSeconds(30f); // 실제로는 밤 시간
        
        currentState = ShopState.Opening;
    }
}
```

---

## 🎯 활용 팁

### 1. 성능 최적화
```csharp
// 감지 빈도 조절
npc.Perception.detectionInterval = 1.0f; // 멀리 있는 NPC
npc.Perception.detectionInterval = 0.2f; // 중요한 NPC

// 메모리 관리
npc.Memory.maxMemories = 100; // 오래된 기억 자동 삭제
```

### 2. 디버깅
```csharp
// Scene View에서 Gizmos로 시각화
private void OnDrawGizmos()
{
    if (npc != null)
    {
        // 시야 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, npc.Perception.visionRange);
        
        // 경로 표시
        if (npc.Pathfinding.currentPath != null)
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < npc.Pathfinding.currentPath.Count - 1; i++)
            {
                Gizmos.DrawLine(npc.Pathfinding.currentPath[i], 
                                npc.Pathfinding.currentPath[i + 1]);
            }
        }
    }
}
```

### 3. 이벤트 시스템
```csharp
// 커스텀 이벤트
public class NPCEvent : UnityEvent<NPCAgent, string> { }
public NPCEvent OnTaskComplete = new NPCEvent();

// 사용
OnTaskComplete.AddListener((agent, task) =>
{
    Debug.Log($"{agent.NPCName}가 {task}를 완료했습니다!");
});
```

---

이제 복잡한 시나리오도 구현할 수 있습니다! 🎉
