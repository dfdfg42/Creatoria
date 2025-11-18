# Unity 씬 설정 가이드

## 🎬 씬 구성

### 1. 새 씬 생성

1. Unity에서 `File > New Scene > 2D`
2. 씬 저장: `Scenes/NPCDemo.unity`

---

## 📦 GameObject 생성

### A. Grid & Tilemap 설정

```
Hierarchy 우클릭 > 2D Object > Tilemap > Rectangular

Grid
└── Tilemap (이름: EnvironmentTilemap)
```

**Tilemap Renderer 설정**:
- Sorting Layer: Default
- Order in Layer: 0

---

### B. NPC Agent 생성

```
Hierarchy 우클릭 > Create Empty

이름: NPCAgent_Seoa
```

**컴포넌트 추가**:
1. `Add Component > NPCAgent`
2. `Add Component > Sprite Renderer` (NPC 비주얼)
3. `Add Component > Circle Collider 2D`

**NPCAgent 설정**:
```
NPC Name: 이서아
Persona: 21살의 대학생. 시각 디자인을 전공하며 졸업 작품으로 고민이 많다. 
         성격은 내향적이지만 친근하고, 도움을 요청받으면 기꺼이 도와준다.
OpenAI Key: [여기에 API 키 입력]
Enable Autonomous Behavior: ✓
Autonomous Update Interval: 60
```

---

### C. Environment System 생성

```
Hierarchy 우클릭 > Create Empty

이름: EnvironmentSystem
```

**컴포넌트 추가**:
- `Add Component > EnvironmentModificationSystem`

**설정**:
```
NPC Agent: NPCAgent_Seoa 드래그
Target Tilemap: EnvironmentTilemap 드래그
Object Container: (새로 생성) "GeneratedObjects" Empty GameObject
Pixels Per Unit: 32
Auto Remove Background: ✓
```

---

### D. UI 캔버스 생성

```
Hierarchy 우클릭 > UI > Canvas

Canvas
├── ChatPanel
│   ├── Background (Image)
│   ├── ChatHistory (Scroll View)
│   │   └── ChatHistoryText (TextMeshPro)
│   ├── InputPanel
│   │   ├── ChatInputField (TMP Input Field)
│   │   └── SendButton (Button)
│   └── NPCStatusText (TextMeshPro)
└── EnvironmentPanel
    ├── Background (Image)
    ├── ContextInput (TMP Input Field)
    ├── EvaluateButton (Button)
    ├── ManualPromptInput (TMP Input Field)
    └── ManualGenerateButton (Button)
```

**Canvas 설정**:
- Render Mode: Screen Space - Overlay
- Canvas Scaler > UI Scale Mode: Scale With Screen Size
- Reference Resolution: 1920x1080

#### ChatPanel 레이아웃:
```
Position: Anchored to Left
Rect Transform:
  Anchor: Top Left
  Pivot: (0, 1)
  Position: (20, -20)
  Size: (400, 600)
```

#### EnvironmentPanel 레이아웃:
```
Position: Anchored to Right
Rect Transform:
  Anchor: Top Right
  Pivot: (1, 1)
  Position: (-20, -20)
  Size: (400, 400)
```

---

### E. Demo Controller 생성

```
Hierarchy 우클릭 > Create Empty

이름: DemoController
```

**컴포넌트 추가**:
- `Add Component > NPCDemoController`

**연결**:
```
NPC Agent: NPCAgent_Seoa
Environment System: EnvironmentSystem

Chat Panel: ChatPanel GameObject
Chat Input Field: InputPanel/ChatInputField
Send Button: InputPanel/SendButton
Chat History Text: ChatHistory/Viewport/Content/ChatHistoryText
NPC Status Text: NPCStatusText

Environment Panel: EnvironmentPanel
Environment Context Input: ContextInput
Evaluate Environment Button: EvaluateButton
Manual Prompt Input: ManualPromptInput
Manual Generate Button: ManualGenerateButton

Player Name: Player
```

---

## 🎨 UI 스타일링

### ChatPanel Background
```
Image Component:
  Source Image: UI/Skin/Background
  Color: rgba(0, 0, 0, 200) # 반투명 검정
```

### ChatHistoryText
```
TextMeshPro:
  Font Size: 16
  Color: White
  Alignment: Top Left
  Wrapping: Enabled
  Overflow: Overflow
```

### Buttons
```
Button (SendButton, EvaluateButton, etc.):
  Colors:
    Normal: rgba(50, 150, 255, 255)
    Highlighted: rgba(70, 170, 255, 255)
    Pressed: rgba(30, 130, 235, 255)
  
  TextMeshPro (Child):
    Text: "전송" / "평가" / "생성"
    Font Size: 18
    Color: White
    Alignment: Center
```

---

## 🎮 플레이어 캐릭터 (선택사항)

```
Hierarchy 우클릭 > 2D Object > Sprite > Square

이름: Player
```

**컴포넌트**:
1. Sprite Renderer (파란색)
2. Circle Collider 2D
3. Rigidbody 2D (Body Type: Dynamic)
4. 간단한 이동 스크립트:

```csharp
using UnityEngine;

public class SimplePlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        rb.velocity = new Vector2(h, v) * moveSpeed;
    }
}
```

---

## 📷 카메라 설정

**Main Camera**:
```
Transform:
  Position: (0, 0, -10)

Camera:
  Projection: Orthographic
  Size: 5
  Background: 회색 또는 하늘색
```

---

## ✅ 최종 체크리스트

- [ ] NPCAgent에 OpenAI API 키 입력
- [ ] 모든 GameObject 연결 확인
- [ ] UI 요소들이 Canvas 안에 있는지 확인
- [ ] Tilemap이 제대로 보이는지 확인
- [ ] Play 버튼 눌러서 콘솔에 에러 없는지 확인

---

## 🎯 테스트 시나리오

### 1. 기본 대화 테스트
```
1. Play 버튼 클릭
2. ChatInputField에 "안녕하세요" 입력
3. Enter 또는 Send 버튼 클릭
4. NPC 응답 확인
```

### 2. 환경 변경 테스트
```
1. F2 키로 Environment Panel 열기
2. Context Input에 "여기 너무 어두워" 입력
3. "평가" 버튼 클릭
4. 콘솔에서 NPC 결정 확인
5. 타일이 생성되는지 확인 (DALL-E API 호출 시간 소요)
```

### 3. 수동 타일 생성 테스트
```
1. Manual Prompt Input에 다음 입력:
   "wooden chair, pixel art, 32x32, top-down view"
2. "생성" 버튼 클릭
3. Tilemap에 타일 추가 확인
```

---

## 🐛 문제 해결

### "OpenAI API Key가 설정되지 않았습니다"
- NPCAgent Inspector에서 openAIKey 필드 확인

### UI가 보이지 않음
- Canvas가 Overlay 모드인지 확인
- EventSystem GameObject가 있는지 확인 (UI 생성 시 자동 생성됨)

### 타일이 생성되지 않음
- 콘솔 로그 확인 (DALL-E API 응답 시간은 10-30초 소요)
- Tilemap 참조가 올바른지 확인

### NPC가 응답하지 않음
- 콘솔에서 OpenAI API 에러 확인
- API 키 유효성 확인
- 네트워크 연결 확인

---

## 💡 추가 개선 아이디어

1. **NPC 애니메이션**: Animator Controller로 대화/이동 애니메이션
2. **말풍선**: WorldSpace Canvas로 NPC 위 말풍선 표시
3. **미니맵**: 환경 전체를 보여주는 미니맵
4. **타임라인**: 시간 경과 시각화
5. **메모리 뷰어**: NPC 기억을 시각적으로 표시

---

완료! 🎉
