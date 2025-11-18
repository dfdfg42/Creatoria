# 플레이어 캐릭터 설정 가이드

NPC들과 상호작용할 수 있는 플레이어 캐릭터 시스템입니다.

## 🎮 주요 기능

### PlayerController
- **WASD / 화살표 키** 이동
- **E키** NPC와 대화
- RPG Maker 스타일 스프라이트 애니메이션
- 자동으로 가까운 NPC 감지

### PlayerInteractionManager
- UI 기반 대화 시스템
- 타이핑 효과
- OpenAI API 또는 키워드 기반 응답
- NPC 메모리 연동

## 🛠️ Unity에서 설정하기

### 1. 플레이어 오브젝트 생성

#### 방법 1: 수동 생성
1. Hierarchy에서 우클릭 → `2D Object` → `Sprite` → 이름: "Player"
2. Transform 설정:
   - Position: (0, 0, 0)
   - Scale: (1, 1, 1)

3. 컴포넌트 추가:
   - `PlayerController.cs`
   - `Rigidbody2D` (자동 추가됨)
   - `BoxCollider2D` 또는 `CircleCollider2D`

4. PlayerController 설정:
   - Move Speed: `5`
   - Use Rigidbody: `true` (체크)
   - Interaction Range: `2`
   - NPC Layer: `NPC` 레이어 선택
   - Interaction Key: `E`

#### 방법 2: 프리팹 사용
```
Assets/npcSimulation/Prefabs/Player.prefab (만들어야 함)
```

### 2. 레이어 설정

1. Unity 상단 메뉴: `Edit` → `Project Settings` → `Tags and Layers`
2. Layers에 추가:
   - Layer 6: `Player`
   - Layer 7: `NPC`

3. 플레이어 오브젝트:
   - Layer: `Player`

4. NPC 오브젝트들:
   - Layer: `NPC`

### 3. UI 설정

#### Canvas 생성
1. Hierarchy 우클릭 → `UI` → `Canvas`
2. Canvas 이름: `PlayerUI`
3. Canvas Scaler 설정:
   - UI Scale Mode: `Scale With Screen Size`
   - Reference Resolution: 1920 x 1080

#### 대화 패널 만들기

```
PlayerUI (Canvas)
├── ConversationPanel (Panel)
│   ├── Background (Image) - 반투명 검은색
│   ├── NPCNameText (Text) - NPC 이름
│   ├── DialogueText (Text) - 대화 내용
│   ├── PlayerInputField (Input Field) - 플레이어 입력
│   ├── SendButton (Button) - 전송 버튼
│   └── CloseButton (Button) - 닫기 버튼
└── InteractionIndicator (Panel)
    └── IndicatorText (Text) - "[E] OOO와 대화"
```

**상세 설정:**

**ConversationPanel:**
- Anchor: 중앙 하단
- Size: (800, 300)
- Position: (0, 150, 0)
- Active: false (초기 비활성)

**NPCNameText:**
- Position: 상단 왼쪽
- Font Size: 24
- Color: 노란색
- Text: "NPC 이름"

**DialogueText:**
- Position: 중앙
- Font Size: 18
- Color: 흰색
- Text: "대화 내용이 여기 표시됩니다."
- Alignment: Left, Top

**PlayerInputField:**
- Position: 하단
- Size: (600, 40)
- Placeholder: "메시지를 입력하세요..."

**SendButton:**
- Position: InputField 오른쪽
- Text: "전송"

**CloseButton:**
- Position: 우상단
- Text: "X"

**InteractionIndicator:**
- Anchor: 중앙 상단
- Size: (200, 50)
- Position: (0, -100, 0)
- Active: false (코드에서 자동 제어)

**IndicatorText:**
- Text: "[E] 대화하기"
- Font Size: 16
- Color: 흰색
- Outline: 검은색 (가독성)

### 4. PlayerInteractionManager 설정

1. `PlayerUI` 오브젝트에 `PlayerInteractionManager.cs` 추가
2. Inspector에서 레퍼런스 연결:
   - **Conversation Panel**: ConversationPanel 할당
   - **NPC Name Text**: NPCNameText 할당
   - **Dialogue Text**: DialogueText 할당
   - **Player Input Field**: PlayerInputField 할당
   - **Send Button**: SendButton 할당
   - **Close Button**: CloseButton 할당
   - **Interaction Indicator**: InteractionIndicator 할당
   - **Indicator Text**: IndicatorText 할당
   - **Typing Speed**: 0.05 (타이핑 속도)

### 5. 캐릭터 스프라이트 설정

플레이어 캐릭터 스프라이트 선택 (Cute RPG Character 사용):

**사용 가능한 캐릭터:**
```
Ada, Alex, Amelia, Bob, Bodhi, Bradley, Carlos, Cara, Carmen, 
Eddy, Eli, Finnegan, Gus, Giorgio, Harvey, Hailey, Isabella, 
Jennifer, John, Eddy, Klaus, Latoya, Maria, Mei, Rajiv, Ryan, 
Sam, Tamara, Tom, Wolfgang, Yuriko 등...
```

**PlayerController에서 설정:**
```csharp
// Start()에서 또는 Inspector에서 설정
playerController.SetCharacterSprite("Ada"); // 원하는 캐릭터 이름
```

## 🎯 사용 방법

### 게임 실행 후 조작

1. **이동**: WASD 또는 화살표 키
2. **NPC에게 접근**: 가까이 가면 머리 위에 "[E] OOO와 대화" 표시
3. **대화 시작**: E키 누르기
4. **메시지 전송**: 
   - 텍스트 입력 후 "전송" 버튼 클릭
   - 또는 Enter 키
5. **대화 종료**: 
   - "X" 버튼 클릭
   - 또는 ESC 키

### 코드에서 플레이어 제어

```csharp
// 플레이어 찾기
PlayerController player = FindObjectOfType<PlayerController>();

// 이동 속도 변경
player.SetMoveSpeed(10f);

// 캐릭터 스프라이트 변경
player.SetCharacterSprite("John");

// 현재 상호작용 가능한 NPC 확인
if (player.CanInteract)
{
    NPCAgent targetNPC = player.CurrentTargetNPC;
    Debug.Log($"상호작용 가능: {targetNPC.agentName}");
}
```

```csharp
// 상호작용 매니저
PlayerInteractionManager manager = FindObjectOfType<PlayerInteractionManager>();

// 대화 중인지 확인
if (manager.IsConversationActive)
{
    NPCAgent currentNPC = manager.CurrentNPC;
    Debug.Log($"현재 대화 중: {currentNPC.agentName}");
}

// 강제로 대화 시작 (특정 NPC와)
NPCAgent specificNPC = FindObjectOfType<NPCAgent>();
manager.StartConversation(specificNPC);
```

## 🔧 커스터마이징

### 1. 이동 방식 변경

**물리 기반 (Rigidbody2D):**
```csharp
// Inspector에서
Use Rigidbody: true (체크)

// 장점: 자연스러운 물리 충돌
// 단점: 약간의 미끄러짐
```

**Transform 기반 (직접 이동):**
```csharp
// Inspector에서
Use Rigidbody: false (체크 해제)

// 장점: 정확한 위치 제어
// 단점: 물리 충돌 직접 처리 필요
```

### 2. 상호작용 범위 조정

```csharp
// Inspector에서
Interaction Range: 2 (기본값)

// 더 멀리서 대화: 5
// 가까이서만 대화: 1
```

### 3. 대화 시스템 커스터마이징

**OpenAI API 사용:**
```csharp
// NPC에 OpenAI API 키 설정
npcAgent.openAIKey = "your-api-key";

// PlayerInteractionManager가 자동으로 GPT 응답 생성
```

**키워드 기반 응답 추가:**

`PlayerInteractionManager.cs`의 `GenerateSimpleResponse()` 수정:

```csharp
private string GenerateSimpleResponse(string message)
{
    message = message.ToLower();
    
    // 커스텀 키워드 추가
    if (message.Contains("날씨"))
        return "오늘 날씨 좋네요!";
    
    if (message.Contains("취미"))
        return $"저는 {currentNPC.hobby}를 좋아해요!";
    
    // ... 기존 코드
}
```

### 4. UI 스타일 변경

**타이핑 속도:**
```csharp
// Inspector에서
Typing Speed: 0.05 (기본값)
// 빠르게: 0.01
// 느리게: 0.1
```

**대화창 크기:**
```csharp
// ConversationPanel의 RectTransform
Size: (800, 300) // 기본값
// 크게: (1000, 400)
// 작게: (600, 200)
```

### 5. 애니메이션 커스터마이징

```csharp
// CharacterSpriteManager 설정
CharacterSpriteManager spriteManager = player.GetComponent<CharacterSpriteManager>();

// 애니메이션 속도 조정
spriteManager.animationSpeed = 0.2f; // 기본값 0.15f

// 수동으로 방향 설정
spriteManager.SetDirection(CharacterSpriteManager.Direction.Down);
```

## 🎨 추천 설정

### 카메라 따라가기

```csharp
// 새 스크립트: CameraFollowPlayer.cs
using UnityEngine;

public class CameraFollowPlayer : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 5f;
    public Vector3 offset = new Vector3(0, 0, -10f);
    
    void LateUpdate()
    {
        if (target == null) return;
        
        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;
    }
}
```

Main Camera에 추가:
- `CameraFollowPlayer.cs` 컴포넌트 추가
- Target: Player 오브젝트 할당

### Sorting Layer 설정

1. `Edit` → `Project Settings` → `Tags and Layers` → `Sorting Layers`
2. 추가:
   - 0: Default
   - 1: Background
   - 2: Ground
   - 3: Props
   - 4: Characters
   - 5: UI

3. Player의 SpriteRenderer:
   - Sorting Layer: `Characters`
   - Order in Layer: `10`

4. NPC의 SpriteRenderer:
   - Sorting Layer: `Characters`
   - Order in Layer: `5`

## 🐛 트러블슈팅

### 플레이어가 움직이지 않음
- Rigidbody2D의 Body Type이 `Dynamic`인지 확인
- Constraints의 Freeze Position이 체크되지 않았는지 확인

### NPC와 상호작용이 안됨
- NPC Layer가 올바르게 설정되었는지 확인
- PlayerController의 NPC Layer 설정 확인
- Interaction Range가 충분한지 확인

### 대화창이 안뜸
- ConversationPanel이 처음에 비활성화되어 있는지 확인
- PlayerInteractionManager의 UI 레퍼런스가 모두 연결되었는지 확인

### 스프라이트가 안보임
- CharacterSpriteManager가 올바른 스프라이트를 로드하는지 확인
- Assets/npcSimulation/Characters/ 폴더에 스프라이트가 있는지 확인
- 스프라이트 Import Settings: Sprite Mode = Multiple, Pixels Per Unit = 32

### 타이핑 효과가 안됨
- DialogueText가 올바르게 연결되었는지 확인
- Typing Speed가 0보다 큰지 확인

## 📝 체크리스트

설정 완료 확인:

- [ ] Player 오브젝트 생성
- [ ] PlayerController 컴포넌트 추가
- [ ] Rigidbody2D, Collider2D 설정
- [ ] Layer 설정 (Player, NPC)
- [ ] Canvas 및 UI 생성
- [ ] ConversationPanel 구조 완성
- [ ] PlayerInteractionManager 컴포넌트 추가
- [ ] UI 레퍼런스 모두 연결
- [ ] 캐릭터 스프라이트 로드 확인
- [ ] 카메라 따라가기 설정 (선택)
- [ ] Sorting Layer 설정 (선택)

## 🎉 완료!

이제 플레이어 캐릭터로 The Ville을 돌아다니며 NPC들과 대화할 수 있습니다!

**다음 단계:**
1. The Ville 월드 생성 (TheVilleWorldBuilder)
2. NPC들 배치
3. 플레이어로 게임 시작!
