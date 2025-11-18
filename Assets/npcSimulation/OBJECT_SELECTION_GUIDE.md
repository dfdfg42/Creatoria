# 🎯 GameObject 자동 선택 시스템 가이드

## 📋 논문의 위치 결정 방식

### **논문 "Generative Agents"의 3단계 시스템**

```python
# 1단계: Sector 선택
act_sector = generate_action_sector(act_desp, persona, maze)
# 예: "sleeping" → "dolores double studio"

# 2단계: Arena 선택
act_arena = generate_action_arena(act_desp, persona, maze, act_world, act_sector)
# 예: "sleeping" + "dolores double studio" → "bedroom 2"

# 3단계: GameObject 선택
act_game_object = generate_action_game_object(act_desp, act_address, persona, maze)
# 예: "sleeping" + "dolores double studio:bedroom 2" → "bed"
```

### **최종 주소 형식**
```
the_ville:dolores double studio:bedroom 2:bed
└─world  └─────sector──────────  └─arena─  └object
```

---

## 🎮 Unity 구현

### **변경된 PlanItem 구조**

```csharp
public class PlanItem
{
    public int startHour;        // 시작 시간
    public int duration;         // 지속 시간
    public string activity;      // 활동 설명 (예: "sleeping")
    public string location;      // Arena (예: "home:bedroom")
    public string targetObject;  // 🆕 GameObject (예: "bed")
    public string emoji;
}
```

---

## 🔄 자동 선택 프로세스

### **1. 계획 생성 시 (AutonomousPlanner)**

```csharp
// AI가 일정 생성
07:00 | wake up | home:bedroom
09:00 | study | cafe
12:00 | have lunch | cafe

// targetObject는 null (나중에 결정)
```

### **2. 실행 시 (NPCAgent.AutonomousUpdate)**

```csharp
// Step 1: 목표 장소로 이동
movement.MoveToArea("home:bedroom", () => {
    
    // Step 2: 도착 후 GameObject 자동 선택
    SelectAndInteractWithObject(currentActivity, currentArea);
});
```

### **3. GameObject 선택 (SelectAndInteractWithObject)**

```csharp
// Step 3-1: 현재 Area의 오브젝트 목록 수집
Available Objects in home:bedroom:
- bed (Furniture)
- desk (Furniture)
- lamp (Light)

// Step 3-2: AI에게 선택 요청
Prompt: "Which object should be used for 'wake up'?"
AI Response: "bed"

// Step 3-3: 선택된 오브젝트와 상호작용
InteractWithObject(bed, callback);

// Step 3-4: 계획에 저장 (다음번 재사용)
activity.targetObject = "bed"
```

---

## 💡 예제 시나리오

### **시나리오: "아침 일과"**

#### **계획 생성 (07:00)**
```
PlanItem {
    startHour: 7,
    activity: "wake up",
    location: "home:bedroom",
    targetObject: null  ← 아직 미정
}
```

#### **실행 (07:00 도착)**
```
1. [NPCMovement] Moving to: home:bedroom
2. [NPCMovement] ✅ Found area: 'home:bedroom'
3. [NPCAgent] ✅ Arrived at home:bedroom
4. [NPCAgent] 🔍 Selecting object for activity: wake up
5. [NPCAgent] AI selected object: bed
6. [NPCAgent] 🎯 Found object: bed
7. [NPCAgent] Interacting with bed...
8. [NPCAgent] ✅ Interaction success!
```

#### **메모리 기록**
```
[Event] 나는 home:bedroom에 도착해서 'wake up' 활동을 시작했다.
[Event] 'wake up' 활동을 위해 'bed'을(를) 사용했다.
```

#### **다음번 (같은 활동)**
```
PlanItem {
    startHour: 7,
    activity: "wake up",
    location: "home:bedroom",
    targetObject: "bed"  ← 이미 저장됨
}

→ AI 선택 스킵, 바로 "bed" 사용!
```

---

## 🛠️ Scene 설정

### **1. WorldArea 설정**

```
GameObject: Bedroom
├── WorldArea Component
│   ├── areaName: "bedroom"
│   └── sectorName: "home"
└── Children (오브젝트들)
    ├── Bed
    │   └── WorldObject: objectName="bed", objectType=Furniture
    ├── Desk
    │   └── WorldObject: objectName="desk", objectType=Furniture
    └── Lamp
        └── WorldObject: objectName="lamp", objectType=Light
```

### **2. WorldObject 설정**

```
GameObject: Bed
├── Transform: Position inside Bedroom area
├── Sprite Renderer: (침대 이미지)
└── WorldObject Component
    ├── Object Name: "bed"
    ├── Object Type: Furniture
    ├── States:
    │   └── occupied: "false" / "true"
    ├── Is Interactable: ✓
    └── Interaction Range: 1.5
```

---

## 🔍 AI 선택 로직

### **프롬프트 구조**

```
### Current Activity ###
sleeping

### Available Objects in home:bedroom ###
bed (Furniture), desk (Furniture), lamp (Light)

### Task ###
Which object should be used for this activity?
Answer with ONLY the object name (without type).
If no object is needed, answer "none".

Answer:
```

### **AI 응답 예시**

| Activity | Objects | AI Choice | 이유 |
|----------|---------|-----------|------|
| sleeping | bed, desk, lamp | **bed** | 자는 곳은 침대 |
| studying | bed, desk, lamp | **desk** | 공부는 책상에서 |
| turn on light | bed, desk, lamp | **lamp** | 조명 켜기는 램프 |
| relaxing | sofa, TV, table | **sofa** | 휴식은 소파에서 |
| none needed | door, wall, floor | **none** | 오브젝트 불필요 |

---

## ⚙️ 최적화 옵션

### **Option 1: 활동별 오브젝트 타입 매핑**

```csharp
// 논문처럼 활동과 오브젝트 타입을 미리 정의
Dictionary<string, ObjectType> activityToObjectType = new Dictionary<string, ObjectType>
{
    {"sleeping", ObjectType.Furniture},  // bed
    {"cooking", ObjectType.Appliance},   // stove
    {"reading", ObjectType.Furniture},   // chair
    {"washing", ObjectType.Appliance},   // sink
};

// AI 호출 없이 바로 선택
ObjectType targetType = activityToObjectType[activity];
WorldObject obj = currentArea.FindObjectOfType(targetType);
```

**장점:**
- ✅ 빠름 (AI 호출 없음)
- ✅ 비용 절약
- ✅ 예측 가능

**단점:**
- ❌ 유연성 낮음
- ❌ 새로운 활동마다 수동 추가 필요

---

### **Option 2: AI 선택 + 캐싱**

```csharp
// 현재 구현 방식
// 첫 실행: AI 선택 → activity.targetObject에 저장
// 다음 실행: 저장된 값 재사용

if (string.IsNullOrEmpty(activity.targetObject))
{
    // AI 선택 (첫 실행만)
    SelectAndInteractWithObject(activity, currentArea);
}
else
{
    // 저장된 오브젝트 재사용 (다음번)
    WorldObject obj = currentArea.FindObjectByName(activity.targetObject);
    InteractWithObject(obj, null);
}
```

**장점:**
- ✅ 첫 실행만 AI 사용
- ✅ 유연함 (새로운 활동 자동 처리)
- ✅ 논문에 가장 가까움

**단점:**
- ❌ 첫 실행 시 약간 느림

---

### **Option 3: 하이브리드**

```csharp
// 기본 규칙 + AI fallback
ObjectType? knownType = GetKnownObjectType(activity);

if (knownType.HasValue)
{
    // 알려진 활동 → 규칙 기반
    WorldObject obj = currentArea.FindObjectOfType(knownType.Value);
    if (obj != null)
    {
        InteractWithObject(obj, null);
        return;
    }
}

// 모르는 활동 → AI 선택
SelectAndInteractWithObject(activity, currentArea);
```

**장점:**
- ✅ 일반적인 활동은 빠름
- ✅ 새로운 활동도 처리 가능

**단점:**
- ❌ 구현 복잡도 증가

---

## 🎯 권장 설정 (현재 구현)

```
✅ AI 자동 선택 + 캐싱 (Option 2)

이유:
1. 논문과 가장 유사
2. 확장성 높음
3. 메모리 효율적
4. 코드 단순
```

---

## 📊 비교: 논문 vs Unity

| 항목 | 논문 (Python) | Unity 구현 |
|------|---------------|------------|
| **Sector 선택** | AI (GPT) | Scene의 WorldArea |
| **Arena 선택** | AI (GPT) | Scene의 WorldArea |
| **GameObject 선택** | AI (GPT) | 🆕 AI (GPT) + 캐싱 |
| **재선택** | 매번 | 첫 실행만 |
| **캐싱** | ❌ | ✅ |
| **메모리 사용** | 높음 | 낮음 |

---

## 🐛 문제 해결

### "AI가 오브젝트를 못 찾아요"

```
문제: AI가 "lamp" 선택했는데 실제는 "Lamp"로 저장됨

해결: 대소문자 무시 매칭 (이미 구현됨)
```

```csharp
if (obj.objectName.ToLower().Contains(selectedObjectName) || 
    selectedObjectName.Contains(obj.objectName.ToLower()))
{
    targetObject = obj;
}
```

---

### "오브젝트가 없는 Area에서 에러"

```
문제: Area에 오브젝트가 하나도 없음

해결: 조기 종료
```

```csharp
if (currentArea == null || currentArea.objectsInArea.Count == 0)
{
    Debug.Log("No objects in current area");
    yield break;  // 조기 종료
}
```

---

### "AI가 이상한 오브젝트 선택"

```
문제: "sleeping"인데 "desk" 선택

해결:
1. temperature 낮추기 (0.3 → 0.1)
2. 프롬프트 개선
3. 예시 추가
```

```csharp
string prompt = $@"
### Examples ###
sleeping → bed
studying → desk
cooking → stove

### Current Activity ###
{activity.activity}

### Available Objects ###
{objectsStr}

Answer (object name only):
";
```

---

## ✅ 완성!

이제 NPC가:
1. ✅ 계획 수립 (Area까지)
2. ✅ 목표 Area로 이동
3. ✅ 도착 후 적절한 GameObject 자동 선택
4. ✅ 선택한 GameObject와 상호작용
5. ✅ 다음번에 재사용 (캐싱)

**논문의 3단계 위치 결정 시스템을 완벽히 구현했습니다!** 🎉
