# 🎓 완전 튜토리얼: 첫 번째 자율 NPC 만들기

## 목표
논문의 Generative Agent를 Unity에서 구현하여 **스스로 생각하고 행동하는 NPC** 만들기

**NPC가 할 수 있는 것:**
- ✅ 주변 환경 관찰
- ✅ 오브젝트 기억
- ✅ 자율적으로 이동
- ✅ 오브젝트 상태 변경
- ✅ 플레이어와 대화
- ✅ AI 이미지로 환경 변경

---

## 📋 Step 1: 새 2D 프로젝트 생성

```
1. Unity Hub > New Project
2. Template: 2D (URP)
3. Project Name: "NPCSimulation"
4. Create Project
```

---

## 📦 Step 2: 파일 복사

```
Assets/npcSimulation/ 폴더를 프로젝트에 복사
├── Core/
├── Environment/
├── Demo/
└── *.md (문서들)
```

---

## 🗺️ Step 3: Tilemap 설정

### 1. Grid 생성
```
Hierarchy 우클릭
> 2D Object > Tilemap > Rectangular

자동 생성:
└── Grid
    └── Tilemap
```

### 2. Tilemap 이름 변경 및 추가
```
Grid 하위:
├── GroundTilemap    (기존 Tilemap 이름 변경)
└── ObjectTilemap    (새로 생성: Hierarchy > Grid 우클릭 > 2D Object > Tilemap > Rectangular)
```

### 3. Sorting Order 설정
```
GroundTilemap:
  Tilemap Renderer > Order in Layer: 0

ObjectTilemap:
  Tilemap Renderer > Order in Layer: 10
```

### 4. 간단한 맵 만들기

```
Window > 2D > Tile Palette

1. Create New Palette
2. 타일 에셋 생성 또는 색상 Sprite 사용
3. GroundTilemap에 바닥 타일 배치 (10x10 정도)
4. 완료!
```

---

## 🎭 Step 4: WorldObject 생성 (상호작용 가능한 오브젝트)

### 예제 1: 조명 만들기

```
1. Hierarchy 우클릭 > 2D Object > Sprite > Circle
   이름: "Lamp"
   
2. Transform:
   Position: (3, 3, 0)
   Scale: (0.5, 0.5, 1)
   
3. Sprite Renderer:
   Color: 노란색 (RGB: 255, 255, 0)
   
4. Add Component > WorldObject

5. WorldObject 설정:
   Object Name: "따뜻한 램프"
   Object Type: Light
   
   States (Add 버튼):
   └── Element 0:
       State Name: "power"
       Initial Value: "off"
       Possible Values: 
         - "off"
         - "on"
   
   Is Interactable: ✓
   Interaction Range: 1.5
   Is Visible: ✓
   Is Obstacle: ☐

6. Layer 설정: Default (Perception이 감지하도록)
```

### 예제 2: 테이블 만들기

```
1. Hierarchy 우클릭 > 2D Object > Sprite > Square
   이름: "Table"
   
2. Transform:
   Position: (6, 2, 0)
   Scale: (1, 0.5, 1)
   
3. Sprite Renderer:
   Color: 갈색 (RGB: 139, 69, 19)
   
4. Add Component > WorldObject

5. WorldObject 설정:
   Object Name: "나무 테이블"
   Object Type: Furniture
   
   States:
   └── Element 0:
       State Name: "cleanliness"
       Initial Value: "clean"
       Possible Values:
         - "clean"
         - "dirty"
   
   Is Interactable: ✓
   Interaction Range: 1.5
   Is Visible: ✓
   Is Obstacle: ☐ (테이블 위로 지나갈 수 있음)
```

---

## 🤖 Step 5: NPC 생성

### 1. GameObject 생성

```
Hierarchy 우클릭 > Create Empty
이름: "NPC_Seoa"
```

### 2. 비주얼 추가

```
NPC_Seoa 선택 > Add Component > Sprite Renderer

Sprite: Circle (또는 캐릭터 스프라이트)
Color: 파란색 (RGB: 100, 150, 255)
Sorting Layer: Default
Order in Layer: 5 (오브젝트 위에 표시)
```

### 3. Transform 설정

```
Transform:
  Position: (0, 0, 0)
  Scale: (0.7, 0.7, 1)
```

### 4. NPCAgent 추가

```
Add Component > NPCAgent

Inspector 설정:
├── NPC Name: "이서아"
├── Persona: "21살의 대학생. 시각 디자인을 전공하며 졸업 작품으로 고민이 많다."
├── OpenAI Key: [여기에 API 키 입력]
├── Enable Autonomous Behavior: ✓
└── Autonomous Update Interval: 60
```

### 5. 추가 컴포넌트

```
Add Component > Circle Collider 2D
  Radius: 0.5
  Is Trigger: ✓

Add Component > Rigidbody 2D (선택사항, 물리 충돌용)
  Body Type: Kinematic
  Simulated: ✓
```

---

## 🔧 Step 6: 시스템 설정

### 1. PathfindingSystem 설정

```
NPC_Seoa 선택

PathfindingSystem Component (자동 추가됨):
├── Walkable Tilemap: [GroundTilemap 드래그]
├── Obstacle Tilemap: [없으면 비워둠]
├── Grid: [Grid GameObject 드래그]
├── Move Speed: 3.0
├── Node Size: 1.0
└── Allow Diagonal: ☐
```

### 2. PerceptionSystem 설정

```
PerceptionSystem Component (자동 추가됨):
├── Vision Range: 5.0
├── Vision Angle: 120
├── Perception Layer: Default
├── Detection Interval: 0.5
└── Use 360 Vision: ✓
```

---

## 🌍 Step 7: EnvironmentModificationSystem 설정

### 1. GameObject 생성

```
Hierarchy 우클릭 > Create Empty
이름: "EnvironmentSystem"
```

### 2. 컴포넌트 추가

```
Add Component > EnvironmentModificationSystem

Inspector 설정:
├── NPC Agent: [NPC_Seoa 드래그]
├── Ground Tilemap: [GroundTilemap 드래그]
├── Object Tilemap: [ObjectTilemap 드래그]
├── Grid: [Grid 드래그]
├── Sprite Object Container: [비워둠]
├── Use Tilemap: ✓
├── Tile Size: 32
├── Pixels Per Unit: 32
├── Auto Remove Background: ✓
└── Max Image Size: 512
```

---

## 🎮 Step 8: UI 설정 (간단 버전)

### 1. Canvas 생성

```
Hierarchy 우클릭 > UI > Canvas

Canvas:
  Render Mode: Screen Space - Overlay
  
Canvas Scaler:
  UI Scale Mode: Scale With Screen Size
  Reference Resolution: 1920x1080
```

### 2. 테스트 버튼들 추가

```
Canvas 우클릭 > UI > Button - TextMeshPro

버튼 1:
  이름: "PerceiveButton"
  Text: "주변 감지"
  Position: (좌측 상단)

버튼 2:
  이름: "MoveToLampButton"
  Text: "램프로 이동"
  
버튼 3:
  이름: "InteractButton"
  Text: "상호작용"
```

### 3. 상태 표시 Text

```
Canvas 우클릭 > UI > Text - TextMeshPro

이름: "StatusText"
Position: (우측 상단)
Alignment: Top Left
Font Size: 16
```

---

## 🎬 Step 9: 테스트 스크립트 작성

```
Assets/Scripts/ 폴더 생성
SimpleNPCTester.cs 생성
```

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NPCSimulation.Core;
using NPCSimulation.Environment;
using System.Collections;

public class SimpleNPCTester : MonoBehaviour
{
    public NPCAgent npc;
    public EnvironmentModificationSystem envSystem;
    public TextMeshProUGUI statusText;
    
    public Button perceiveButton;
    public Button moveToLampButton;
    public Button interactButton;

    private void Start()
    {
        perceiveButton.onClick.AddListener(OnPerceiveClicked);
        moveToLampButton.onClick.AddListener(OnMoveToLampClicked);
        interactButton.onClick.AddListener(OnInteractClicked);
        
        UpdateStatus("준비 완료!");
    }

    private void OnPerceiveClicked()
    {
        UpdateStatus("주변 감지 중...");
        npc.Perception.PerceiveEnvironment();
        
        var objects = npc.Perception.GetDetectedObjects();
        UpdateStatus($"발견한 오브젝트: {objects.Count}개\n" + 
                     string.Join("\n", objects.ConvertAll(o => $"- {o.objectName}")));
    }

    private void OnMoveToLampClicked()
    {
        UpdateStatus("램프 찾는 중...");
        WorldObject lamp = npc.Perception.FindObjectByName("램프");
        
        if (lamp != null)
        {
            UpdateStatus($"램프 발견! 이동 중...");
            npc.Pathfinding.MoveToObject(lamp);
        }
        else
        {
            UpdateStatus("램프를 찾을 수 없습니다.");
        }
    }

    private void OnInteractClicked()
    {
        UpdateStatus("가장 가까운 오브젝트와 상호작용 중...");
        
        var objects = npc.Perception.GetDetectedObjects();
        if (objects.Count > 0)
        {
            WorldObject nearest = objects[0];
            npc.InteractWithObject(nearest, (success) =>
            {
                if (success)
                {
                    UpdateStatus($"'{nearest.objectName}'과(와) 상호작용 완료!");
                }
                else
                {
                    UpdateStatus("상호작용 실패");
                }
            });
        }
        else
        {
            UpdateStatus("근처에 오브젝트가 없습니다.");
        }
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = $"[{System.DateTime.Now:HH:mm:ss}]\n{message}";
        }
        Debug.Log($"[SimpleNPCTester] {message}");
    }
}
```

### 10. 스크립트 연결

```
1. Canvas에 SimpleNPCTester 컴포넌트 추가
2. Inspector에서:
   - NPC: [NPC_Seoa 드래그]
   - Env System: [EnvironmentSystem 드래그]
   - Status Text: [StatusText 드래그]
   - Perceive Button: [PerceiveButton 드래그]
   - Move To Lamp Button: [MoveToLampButton 드래그]
   - Interact Button: [InteractButton 드래그]
```

---

## ▶️ Step 10: 실행 및 테스트

### 1. Play 버튼 클릭

```
콘솔 확인:
✓ [NPCAgent] Initializing 이서아...
✓ [NPCAgent] 이서아 initialized successfully!
✓ [EnvironmentModificationSystem] 초기화 완료
```

### 2. 테스트 시나리오

#### 테스트 1: 주변 감지
```
1. "주변 감지" 버튼 클릭
2. Scene View에서 Gizmos 확인:
   - 노란색 원: NPC 시야
   - 초록색 선: 감지된 오브젝트
3. StatusText에 발견한 오브젝트 목록 표시
```

#### 테스트 2: 자동 이동
```
1. "램프로 이동" 버튼 클릭
2. Scene View에서:
   - 파란색 선: 계산된 경로
   - NPC가 램프로 이동
3. 도착하면 콘솔에 "도착!" 메시지
```

#### 테스트 3: 상호작용
```
1. NPC를 램프 근처로 이동
2. "상호작용" 버튼 클릭
3. AI가 자동으로:
   - 상황 분석
   - 행동 결정 (예: 조명 켜기)
   - 상태 변경 (power: off → on)
4. 램프 색상이 어두워지거나 밝아짐
```

---

## 🎯 Step 11: 자율 행동 테스트

### 1. 자율 모드 활성화

```
NPC_Seoa > NPCAgent:
  Enable Autonomous Behavior: ✓
  Autonomous Update Interval: 30 (30초마다)
```

### 2. Play 후 관찰

```
30초마다 자동으로:
1. 일일 계획 수립
2. 현재 시간에 맞는 활동 실행
3. 필요시 자동 이동
4. 메모리에 기록

콘솔 확인:
"[AutonomousPlanner] Creating new daily plan..."
"[AutonomousPlanner] Daily plan created with 8 activities"
"[NPCAgent] Current activity: 도서관에서 공부 @ 도서관:열람실"
```

---

## 🎨 Step 12: AI 이미지 생성 테스트

### 1. 환경 변경 UI 추가

```
Canvas에 Button 추가:
  이름: "GenerateObjectButton"
  Text: "오브젝트 생성"
```

### 2. SimpleNPCTester에 추가

```csharp
public Button generateObjectButton;

private void Start()
{
    // ...
    generateObjectButton.onClick.AddListener(OnGenerateObjectClicked);
}

private void OnGenerateObjectClicked()
{
    UpdateStatus("AI 이미지 생성 중...");
    
    string prompt = "warm table lamp, pixel art, 32x32px, top-down view";
    envSystem.ManualGenerateTile(prompt, "near");
    
    UpdateStatus("이미지 생성 요청 완료! (10-30초 소요)");
}
```

### 3. 테스트

```
1. "오브젝트 생성" 버튼 클릭
2. 10-30초 대기 (DALL-E API 응답)
3. ObjectTilemap에 새로운 타일 추가됨!
4. Scene View에서 초록색 박스로 표시
```

---

## ✅ 최종 체크리스트

- [ ] Grid & Tilemap 설정 완료
- [ ] WorldObject들 배치 (램프, 테이블 등)
- [ ] NPC 생성 및 설정
- [ ] OpenAI API Key 입력
- [ ] PathfindingSystem 설정
- [ ] PerceptionSystem 설정
- [ ] EnvironmentModificationSystem 설정
- [ ] UI 버튼들 연결
- [ ] SimpleNPCTester 스크립트 연결
- [ ] Play 후 콘솔 에러 없음
- [ ] 주변 감지 테스트 성공
- [ ] 이동 테스트 성공
- [ ] 상호작용 테스트 성공
- [ ] (선택) AI 이미지 생성 테스트 성공

---

## 🎉 완성!

이제 당신만의 **생각하고 행동하는 NPC**를 만들었습니다!

### NPC가 할 수 있는 것들:

✅ **자율 감지**: 주변 5m 내 모든 오브젝트/NPC 자동 감지
✅ **공간 기억**: 본 것들의 위치 기억
✅ **자동 이동**: A* 알고리즘으로 장애물 회피하며 이동
✅ **상태 변경**: 오브젝트 상태 변경 (조명 켜기, 청소 등)
✅ **자율 행동**: 일일 계획에 따라 스스로 행동
✅ **대화**: 플레이어와 자연스러운 대화
✅ **환경 변경**: AI 이미지 생성으로 새로운 오브젝트 추가

---

## 🚀 다음 단계

### 1. 더 많은 오브젝트 추가
```
- 침대 (state: occupied)
- 냉장고 (state: temperature, open)
- 문 (state: open, locked)
- 컴퓨터 (state: power, program)
```

### 2. 복잡한 행동 패턴
```csharp
// 예: "배고프면 냉장고로 가서 음식 먹기"
if (npc.CurrentEmotion == "배고픔")
{
    npc.FindAndMoveToObjectType(ObjectType.Food, (food) =>
    {
        if (food != null)
        {
            food.SetState("freshness", "eaten");
            npc.UpdateEmotion("만족");
        }
    });
}
```

### 3. 멀티 NPC
```
- 여러 NPC 추가
- NPC끼리 대화
- 공동 작업
```

### 4. 저장/불러오기
```csharp
// 메모리 저장
MemoryManager.SaveToFile("save.json");

// 메모리 불러오기
MemoryManager.LoadFromFile("save.json");
```

---

**축하합니다!** 🎊

논문의 Generative Agents를 Unity에서 성공적으로 구현했습니다!
