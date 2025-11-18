# 🔍 Perception & Object Interaction 가이드

## 논문 핵심 메커니즘 구현

Generative Agents 논문의 다음 기능들을 Unity C#으로 구현:
1. **Perception (지각)**: 시야 내 오브젝트/에이전트 감지
2. **Spatial Memory (공간 기억)**: 본 것들의 위치 기억
3. **Object States (오브젝트 상태)**: 오브젝트가 상태를 가지고 NPC가 변경 가능
4. **Pathfinding (길찾기)**: A* 알고리즘으로 자율 이동

---

## 📁 새로 추가된 파일들

```
Assets/npcSimulation/Core/
├── WorldObject.cs           # 상태를 가진 월드 오브젝트
├── PerceptionSystem.cs      # 지각 시스템
├── PathfindingSystem.cs     # A* 길찾기
└── NPCAgent.cs             # 통합 (업데이트됨)
```

---

## 🌍 WorldObject - 상태를 가진 오브젝트

### 개념
논문에서 오브젝트는 단순한 장식이 아닙니다:
- **상태를 가짐**: clean/dirty, on/off, open/closed 등
- **NPC가 감지 가능**: 시야에 들어오면 자동 인식
- **상호작용 가능**: NPC가 상태를 변경할 수 있음

### 설정 방법

#### 1. 오브젝트에 WorldObject 컴포넌트 추가

```
Scene에서:
1. 오브젝트 선택 (예: Lamp)
2. Add Component > WorldObject
3. Collider2D 자동 추가됨 (Perception용)
```

#### 2. Inspector 설정

```
WorldObject Component:
├── Object Name: "따뜻한 램프"
├── Object Type: Light
├── States: (상태 목록)
│   └── State 0:
│       ├── State Name: "power"
│       ├── Initial Value: "off"
│       └── Possible Values: ["off", "on"]
├── Is Interactable: ✓
├── Interaction Range: 1.5
├── Is Visible: ✓
└── Is Obstacle: ☐
```

### 상태 정의 예시

#### 조명 (Light)
```csharp
states:
  - stateName: "power"
    initialValue: "off"
    possibleValues: ["off", "on"]
  - stateName: "brightness"
    initialValue: "normal"
    possibleValues: ["dim", "normal", "bright"]
```

#### 가구 (Furniture)
```csharp
states:
  - stateName: "cleanliness"
    initialValue: "clean"
    possibleValues: ["clean", "dirty"]
  - stateName: "occupied"
    initialValue: "empty"
    possibleValues: ["empty", "occupied"]
```

#### 문 (Door)
```csharp
states:
  - stateName: "open"
    initialValue: "closed"
    possibleValues: ["closed", "open", "locked"]
```

#### 음식 (Food)
```csharp
states:
  - stateName: "temperature"
    initialValue: "cold"
    possibleValues: ["cold", "warm", "hot"]
  - stateName: "freshness"
    initialValue: "fresh"
    possibleValues: ["fresh", "stale", "spoiled"]
```

### 코드에서 사용

```csharp
// 오브젝트 참조 가져오기
WorldObject lamp = GameObject.Find("Lamp").GetComponent<WorldObject>();

// 상태 읽기
string power = lamp.GetState("power");
Debug.Log($"램프 전원: {power}"); // "off"

// 상태 변경
lamp.SetState("power", "on");
// → 비주얼 자동 업데이트 (노란색으로 변경)

// 모든 상태 확인
string allStates = lamp.GetAllStatesAsString();
Debug.Log(allStates); // "power:on, brightness:normal"

// 설명 (NPC가 보는 정보)
string desc = lamp.GetDescription();
Debug.Log(desc); // "따뜻한 램프 (Light) - power:on"
```

---

## 👁️ PerceptionSystem - 지각 시스템

### 개념
NPC가 주변을 "보는" 시스템:
- **시야 범위 내 자동 감지**
- **오브젝트와 다른 NPC 구분**
- **공간 기억 생성** (본 것들의 위치 저장)
- **메모리에 자동 기록**

### NPCAgent에 자동 추가됨

```csharp
// NPCAgent.cs Start()에서 자동 추가
Perception = gameObject.AddComponent<PerceptionSystem>();
```

### Inspector 설정

```
PerceptionSystem Component:
├── Vision Range: 5.0          # 시야 거리
├── Vision Angle: 120          # 시야 각도 (전방)
├── Perception Layer: Default  # 감지할 레이어
├── Detection Interval: 0.5    # 0.5초마다 감지
└── Use 360 Vision: ✓          # 전방향 시야
```

### 시야 모드

#### 전방향 시야 (360°)
```csharp
use360Vision = true;
// NPC 주변 모든 방향 감지
```

#### 전방 시야만
```csharp
use360Vision = false;
visionAngle = 120f;
// NPC가 보는 방향 ±60° 범위만 감지
```

### Scene View에서 시각화

```
Scene View에서 Gizmos 활성화:
- 노란색 원: 시야 범위
- 초록색 선: 감지된 오브젝트
- 청록색 선: 감지된 다른 NPC
```

### 코드 사용 예시

```csharp
NPCAgent npc = GetComponent<NPCAgent>();

// 주변 감지 (자동으로 0.5초마다 실행됨)
npc.Perception.PerceiveEnvironment();

// 감지된 오브젝트 가져오기
List<WorldObject> objects = npc.Perception.GetDetectedObjects();
foreach (var obj in objects)
{
    Debug.Log($"발견: {obj.objectName} - {obj.GetAllStatesAsString()}");
}

// 특정 타입 찾기
WorldObject lamp = npc.Perception.FindNearestObjectOfType(ObjectType.Light);
if (lamp != null)
{
    Debug.Log($"가장 가까운 조명: {lamp.objectName}");
}

// 이름으로 찾기
WorldObject table = npc.Perception.FindObjectByName("테이블");

// 특정 상태를 가진 오브젝트 찾기
List<WorldObject> dirtyObjects = npc.Perception.FindObjectsWithState("cleanliness", "dirty");
Debug.Log($"더러운 오브젝트 {dirtyObjects.Count}개 발견");

// 공간 기억 요약
string memory = npc.Perception.GetSpatialMemorySummary();
Debug.Log($"공간 기억:\n{memory}");
```

### 메모리 자동 기록

Perception이 오브젝트를 발견하면 자동으로:
```
Memory: "'램프'을(를) (5.2, 3.1, 0.0)에서 발견했다. 상태: power:off"
Importance: 3
```

---

## 🗺️ PathfindingSystem - A* 길찾기

### 개념
NPC가 장애물을 피해 목표 지점까지 자동으로 이동:
- **A* 알고리즘** 사용
- **Tilemap 기반** 경로 계산
- **장애물 회피**
- **대각선 이동** 옵션

### 설정

#### 1. Tilemap 준비

```
Hierarchy:
└── Grid
    ├── GroundTilemap     # 걸을 수 있는 바닥
    └── ObstacleTilemap   # 장애물 (벽, 장식 등)
```

#### 2. Inspector 설정

```
PathfindingSystem Component:
├── Walkable Tilemap: GroundTilemap (드래그)
├── Obstacle Tilemap: ObstacleTilemap (드래그)
├── Grid: Grid GameObject (드래그)
├── Move Speed: 3.0
├── Node Size: 1.0
└── Allow Diagonal: ☐  # 대각선 이동 허용 여부
```

### 사용 방법

#### 1. 월드 좌표로 이동

```csharp
NPCAgent npc = GetComponent<NPCAgent>();

// 특정 위치로 이동
Vector3 target = new Vector3(10f, 5f, 0f);
bool success = npc.Pathfinding.MoveTo(target);

if (success)
{
    Debug.Log("경로 찾기 성공! 이동 중...");
}
else
{
    Debug.LogWarning("경로를 찾을 수 없습니다.");
}

// 이동 중인지 확인
if (npc.Pathfinding.IsMoving())
{
    Debug.Log("이동 중...");
}

// 이동 중지
npc.Pathfinding.StopMoving();
```

#### 2. 오브젝트로 이동

```csharp
// 오브젝트 찾기
WorldObject target = npc.Perception.FindNearestObjectOfType(ObjectType.Light);

if (target != null)
{
    // 해당 오브젝트로 이동
    npc.Pathfinding.MoveToObject(target);
}
```

#### 3. 고급: 이동 완료 감지

```csharp
IEnumerator MoveAndWait(Vector3 target)
{
    npc.Pathfinding.MoveTo(target);
    
    // 이동 완료까지 대기
    while (npc.Pathfinding.IsMoving())
    {
        yield return null;
    }
    
    Debug.Log("도착!");
}
```

### Scene View 시각화

```
Scene View에서:
- 파란색 선: 계산된 경로
- 빨간색 구: 목표 지점
```

### 장애물 설정

#### Tilemap 장애물
```
ObstacleTilemap에 타일 배치
→ 자동으로 경로에서 제외됨
```

#### WorldObject 장애물
```csharp
WorldObject wall = GetComponent<WorldObject>();
wall.isObstacle = true;
// → Pathfinding이 자동으로 회피
```

---

## 🎯 통합 예시: NPC의 자율 행동

### 시나리오: "더러운 테이블을 청소하기"

```csharp
IEnumerator CleanDirtyTable()
{
    NPCAgent npc = GetComponent<NPCAgent>();
    
    // 1. 주변 감지
    npc.Perception.PerceiveEnvironment();
    yield return new WaitForSeconds(0.5f);
    
    // 2. 더러운 테이블 찾기
    List<WorldObject> dirtyTables = npc.Perception.FindObjectsWithState("cleanliness", "dirty");
    
    if (dirtyTables.Count == 0)
    {
        Debug.Log("청소할 테이블이 없습니다.");
        yield break;
    }
    
    WorldObject table = dirtyTables[0];
    Debug.Log($"발견: {table.objectName}이(가) 더럽습니다.");
    
    // 3. 테이블로 이동
    Debug.Log("테이블로 이동 중...");
    npc.Pathfinding.MoveToObject(table);
    
    while (npc.Pathfinding.IsMoving())
    {
        yield return null;
    }
    
    // 4. 청소하기 (상태 변경)
    Debug.Log("청소 중...");
    yield return new WaitForSeconds(2f); // 청소 시간
    
    table.SetState("cleanliness", "clean");
    
    // 5. 메모리에 기록
    npc.MemoryMgr.AddMemory(
        MemoryType.Event,
        $"'{table.objectName}'을(를) 청소했다. 이제 깨끗하다.",
        importance: 7,
        npc
    );
    
    Debug.Log("청소 완료!");
}
```

### 시나리오: "어두우면 조명 켜기"

```csharp
IEnumerator TurnOnLightIfDark()
{
    NPCAgent npc = GetComponent<NPCAgent>();
    
    // 1. 주변 조명 찾기
    npc.Perception.PerceiveEnvironment();
    yield return new WaitForSeconds(0.5f);
    
    WorldObject lamp = npc.Perception.FindNearestObjectOfType(ObjectType.Light);
    
    if (lamp == null)
    {
        Debug.Log("조명을 찾을 수 없습니다.");
        yield break;
    }
    
    // 2. 조명 상태 확인
    string power = lamp.GetState("power");
    
    if (power == "on")
    {
        Debug.Log("조명이 이미 켜져 있습니다.");
        yield break;
    }
    
    // 3. 조명으로 이동
    Debug.Log("조명으로 이동 중...");
    npc.Pathfinding.MoveToObject(lamp);
    
    while (npc.Pathfinding.IsMoving())
    {
        yield return null;
    }
    
    // 4. 조명 켜기
    Debug.Log("조명을 켭니다.");
    lamp.SetState("power", "on");
    
    // 5. 메모리 및 감정
    npc.MemoryMgr.AddMemory(
        MemoryType.Event,
        $"'{lamp.objectName}'을(를) 켰다. 이제 밝다.",
        importance: 6,
        npc
    );
    
    npc.UpdateEmotion("편안함");
}
```

### 시나리오: "NPCAgent의 자동 상호작용"

```csharp
// NPCAgent가 직접 제공하는 고급 API
NPCAgent npc = GetComponent<NPCAgent>();

// 자동으로: 감지 → 이동 → 상호작용
npc.InteractWithObject(targetObject, (success) =>
{
    if (success)
    {
        Debug.Log("상호작용 완료!");
    }
});

// 특정 타입 찾아서 이동
npc.FindAndMoveToObjectType(ObjectType.Furniture, (foundObject) =>
{
    if (foundObject != null)
    {
        Debug.Log($"가구 '{foundObject.objectName}'에 도착!");
    }
});
```

---

## 🎨 씬 설정 체크리스트

### 1. Grid & Tilemap
- [ ] Grid GameObject 생성
- [ ] GroundTilemap 생성 (걸을 수 있는 바닥)
- [ ] ObstacleTilemap 생성 (장애물)
- [ ] 바닥 타일 배치
- [ ] 장애물 타일 배치

### 2. WorldObject들
- [ ] 씬에 오브젝트 배치 (Sprite)
- [ ] 각 오브젝트에 WorldObject 컴포넌트 추가
- [ ] Collider2D 확인 (자동 추가됨)
- [ ] 상태(States) 정의
- [ ] Object Type 설정
- [ ] Is Interactable 체크
- [ ] Layer 설정 (Perception이 감지할 수 있도록)

### 3. NPCAgent
- [ ] NPCAgent GameObject 생성
- [ ] NPCAgent 컴포넌트 추가
- [ ] OpenAI API Key 입력
- [ ] PerceptionSystem 자동 추가 확인
- [ ] PathfindingSystem 자동 추가 확인
- [ ] Collider2D 추가 (다른 NPC 감지용)

### 4. PathfindingSystem 설정
- [ ] Walkable Tilemap 연결
- [ ] Obstacle Tilemap 연결
- [ ] Grid 연결
- [ ] Move Speed 조정
- [ ] Allow Diagonal 설정

### 5. PerceptionSystem 설정
- [ ] Vision Range 조정
- [ ] Vision Angle 설정
- [ ] Perception Layer 설정
- [ ] Use 360 Vision 체크

---

## 🐛 문제 해결

### NPC가 오브젝트를 감지하지 못함
```
1. WorldObject에 Collider2D가 있는지 확인
2. WorldObject의 Layer가 PerceptionSystem의 Perception Layer에 포함되는지 확인
3. Is Visible이 체크되어 있는지 확인
4. Vision Range 안에 있는지 확인 (Scene View에서 노란색 원)
```

### 경로를 찾을 수 없음
```
1. GroundTilemap에 바닥 타일이 배치되어 있는지 확인
2. ObstacleTilemap에 불필요한 타일이 없는지 확인
3. Grid가 제대로 연결되어 있는지 확인
4. WorldObject의 isObstacle 설정 확인
5. Scene View에서 파란색 경로 선이 그려지는지 확인
```

### NPC가 오브젝트 상태를 변경하지 못함
```csharp
// 상태 이름이 정확한지 확인
WorldObject obj = GetComponent<WorldObject>();
Debug.Log(obj.GetAllStatesAsString()); // 현재 상태 확인

// 가능한 값인지 확인
ObjectState state = obj.states.Find(s => s.stateName == "power");
Debug.Log(string.Join(", ", state.possibleValues)); // ["off", "on"]
```

### 메모리에 기록되지 않음
```csharp
// NPCAgent가 제대로 초기화되었는지 확인
if (npc.MemoryMgr == null)
{
    Debug.LogError("MemoryManager가 초기화되지 않았습니다!");
}

// OpenAI API Key 확인
if (string.IsNullOrEmpty(npc.openAIKey))
{
    Debug.LogError("OpenAI API Key가 설정되지 않았습니다!");
}
```

---

## 📊 성능 최적화

### Perception
```csharp
// 감지 주기 조정 (덜 중요한 NPC)
perception.detectionInterval = 1.0f; // 1초마다 (기본 0.5초)

// 시야 거리 감소
perception.visionRange = 3f; // 가까운 것만 (기본 5f)
```

### Pathfinding
```csharp
// 대각선 이동 비활성화 (계산 빠름)
pathfinding.allowDiagonal = false;

// 이동 속도 조정
pathfinding.moveSpeed = 5f; // 빠르게
```

### WorldObject
```csharp
// 상호작용 불가능한 장식은 isInteractable = false
// → Perception이 무시함
decorativeObj.isInteractable = false;
```

---

## 🎯 다음 단계

1. **씬에 WorldObject 배치**
2. **NPC 테스트**: Perception이 제대로 감지하는지
3. **경로 테스트**: Pathfinding이 제대로 이동하는지
4. **상호작용 테스트**: 상태 변경이 제대로 되는지
5. **자율 행동 구현**: AutonomousPlanner에 통합

---

## 💡 고급 활용

### 동적 장애물
```csharp
// 문이 열리면 장애물 해제
door.OnStateChanged += (obj, stateName, oldValue, newValue) =>
{
    if (stateName == "open" && newValue == "open")
    {
        door.isObstacle = false;
    }
    else if (newValue == "closed")
    {
        door.isObstacle = true;
    }
};
```

### 멀티 NPC 상호작용
```csharp
// 다른 NPC 감지
List<NPCAgent> nearbyNPCs = npc.Perception.GetDetectedAgents();
foreach (var other in nearbyNPCs)
{
    Debug.Log($"근처에 '{other.Name}'이(가) 있습니다.");
    // 대화 시작 등...
}
```

### 상태 변화 리스너
```csharp
WorldObject lamp = GetComponent<WorldObject>();

lamp.OnStateChanged += (obj, stateName, oldVal, newVal) =>
{
    Debug.Log($"{obj.objectName}의 {stateName}이(가) {oldVal}에서 {newVal}로 변경됨");
    
    // 비주얼 효과, 사운드 재생 등...
    if (stateName == "power" && newVal == "on")
    {
        PlayLightOnSound();
    }
};
```

---

**완료!** 🎉

이제 NPC가:
- ✅ 주변을 보고 (Perception)
- ✅ 기억하고 (Spatial Memory)
- ✅ 이동하고 (Pathfinding)
- ✅ 상호작용할 수 있습니다 (Object States)

**논문의 핵심 메커니즘 구현 완료!** 🚀
