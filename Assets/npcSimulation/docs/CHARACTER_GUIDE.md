# 🎭 AI 캐릭터 생성 시스템 가이드

## 🎯 기능 개요

**DALL-E AI로 캐릭터를 동적으로 생성하고 씬에 배치하는 시스템**

### ✨ 주요 기능
- ✅ **AI 이미지 생성**: DALL-E로 캐릭터 스프라이트 생성
- ✅ **NPC 주도 생성**: 메인 NPC가 필요 여부 판단
- ✅ **자동 배치**: 생성된 캐릭터를 씬에 자동 배치
- ✅ **캐릭터 데이터**: 이름, 역할, 성격 등 저장
- ✅ **간단한 애니메이션**: Idle 바운스 효과
- ✅ **상호작용 준비**: 플레이어와 대화 가능 (확장 가능)

---

## 📦 컴포넌트 구조

```
CharacterGenerationSystem.cs     # 캐릭터 생성 및 관리
GeneratedCharacter.cs            # 캐릭터 데이터 컴포넌트
SimpleCharacterAnimation.cs      # 간단한 애니메이션
```

---

## 🚀 빠른 시작

### 1. 씬 설정

```
Hierarchy:
├── NPCAgent (메인 NPC)
├── CharacterSystem (CharacterGenerationSystem.cs)
└── GeneratedCharacters (Empty GameObject)
```

### 2. CharacterGenerationSystem 설정

**Inspector**:
```
Main NPC Agent: [NPCAgent 드래그]
Character Container: [GeneratedCharacters 드래그]

Character Settings:
  Character Sprite Size: 128        # 캐릭터 이미지 크기
  Pixels Per Unit: 32
  Character Scale: (1.5, 1.5)       # 실제 씬에서 크기

Animation Settings:
  Enable Simple Animation: ✓
  Idle Bounce Speed: 1
  Idle Bounce Height: 0.1

AI Prompt Settings:
  Default Style: "pixel art character"
  View Angle: "front view, full body"
```

---

## 💬 사용 방법

### 방법 1: NPC가 자동 판단

```csharp
CharacterGenerationSystem charSystem = GetComponent<CharacterGenerationSystem>();

// NPC가 상황을 보고 캐릭터 필요 여부 판단
charSystem.RequestCharacterGeneration("혼자 있기 외로워요");

// NPC AI가:
// 1. 상황 분석
// 2. 캐릭터 필요 여부 결정
// 3. 캐릭터 정보 생성 (이름, 외모, 성격 등)
// 4. DALL-E로 이미지 생성
// 5. 씬에 자동 배치
```

### 방법 2: 수동 생성

```csharp
charSystem.ManualGenerateCharacter(
    characterName: "김민수",
    description: "20대 남성, 안경을 쓴 개발자",
    imagePrompt: "young male programmer with glasses, pixel art character, front view",
    role: "프로그래머",
    personality: "내향적이지만 친절함"
);
```

### 방법 3: UI에서 생성

```
1. F3 키로 Character Panel 열기
2. Context Input: "친구가 필요해요"
3. "평가" 버튼 클릭
   → NPC가 자동으로 캐릭터 생성

또는

1. Character Name: "이서아"
2. Character Desc: "20대 여성, 예술가"
3. "생성" 버튼 클릭
   → 즉시 캐릭터 생성
```

---

## 🎨 프롬프트 예시

### 기본 픽셀 아트
```
young male programmer with glasses, hoodie and jeans, 
pixel art character, front view, full body, white background
```

### 판타지 스타일
```
elven warrior with silver armor, 
pixel art RPG character, front view, full body, 
transparent background
```

### 현대 캐주얼
```
teenage girl with backpack, casual clothes, 
pixel art character sprite, front facing, 
white background
```

### 직업별
```
# 의사
doctor in white coat with stethoscope, 
pixel art character, front view, standing pose

# 경찰
police officer in uniform, 
pixel art style, front view, full body

# 학생
high school student with uniform, 
pixel art character, frontal view
```

---

## 🎮 캐릭터 관리

### 캐릭터 찾기
```csharp
GeneratedCharacter character = charSystem.FindCharacterByName("김민수");

if (character != null)
{
    Debug.Log(character.GetInfo());
}
```

### 모든 캐릭터 가져오기
```csharp
List<GeneratedCharacter> allCharacters = charSystem.GetAllCharacters();

foreach (var character in allCharacters)
{
    Debug.Log($"{character.characterName}: {character.role}");
}
```

### 캐릭터 제거
```csharp
// 특정 캐릭터 제거
charSystem.RemoveCharacter(character);

// 모든 캐릭터 제거
charSystem.ClearAllCharacters();
```

### 개수 확인
```csharp
int count = charSystem.GetCharacterCount();
Debug.Log($"현재 캐릭터 수: {count}");
```

---

## 🔧 GeneratedCharacter 컴포넌트

생성된 각 캐릭터에는 `GeneratedCharacter` 컴포넌트가 자동으로 추가됩니다.

### 데이터
```csharp
public class GeneratedCharacter : MonoBehaviour
{
    public string characterName;        // 이름
    public string description;          // 외모 설명
    public string role;                 // 역할/직업
    public string personality;          // 성격
    
    public bool hasAI;                  // AI 대화 시스템 활성화
    public bool isInteractable;         // 상호작용 가능
    public float interactionRadius;     // 상호작용 거리
    
    public bool canMove;                // 이동 가능
    public float moveSpeed;
    public float wanderRadius;          // 배회 반경
}
```

### 상호작용
```csharp
GeneratedCharacter character = GetComponent<GeneratedCharacter>();

// 정보 출력
string info = character.GetInfo();

// 플레이어와 상호작용
character.Interact(player);
```

---

## 🎬 애니메이션

`SimpleCharacterAnimation` 컴포넌트가 자동 추가되어 기본 애니메이션 제공:

### Idle 바운스
```csharp
public float bounceSpeed = 1f;      # 바운스 속도
public float bounceHeight = 0.1f;   # 바운스 높이
```

### 호흡 효과
```csharp
public bool enableBreathing = true;
public float breathingSpeed = 2f;
public float breathingScale = 0.05f;
```

### 커스터마이징
```csharp
SimpleCharacterAnimation anim = character.GetComponent<SimpleCharacterAnimation>();
anim.bounceSpeed = 2f;
anim.bounceHeight = 0.2f;
anim.enableBreathing = false;
```

---

## 🔌 AI 대화 시스템 연결 (확장)

### 단계 1: GeneratedCharacter에 AI 활성화

```csharp
GeneratedCharacter character = /* ... */;
character.hasAI = true;
character.openAIKey = "your-api-key";
```

### 단계 2: NPCAgent 추가

```csharp
NPCAgent aiAgent = character.gameObject.AddComponent<NPCAgent>();
aiAgent.npcName = character.characterName;
aiAgent.persona = $"{character.description}. {character.personality}";
aiAgent.openAIKey = character.openAIKey;
```

### 단계 3: 대화 시작

```csharp
aiAgent.RespondToPlayer("안녕하세요!", "Player", (response) =>
{
    Debug.Log($"{character.characterName}: {response}");
});
```

---

## 🎯 고급 기능

### 1. 특정 위치에 생성

```csharp
// CharacterGenerationSystem.cs 확장
public void GenerateCharacterAtPosition(string name, string prompt, Vector3 position)
{
    StartCoroutine(GenerateAtPositionCoroutine(name, prompt, position));
}
```

### 2. 캐릭터 그룹 생성

```csharp
IEnumerator GenerateCharacterGroup(List<string> names)
{
    foreach (string name in names)
    {
        charSystem.ManualGenerateCharacter(
            name, 
            $"{name}의 설명", 
            $"{name} pixel art character"
        );
        yield return new WaitForSeconds(5f); // API 제한 고려
    }
}
```

### 3. 캐릭터 이동 시스템

```csharp
public class CharacterMovement : MonoBehaviour
{
    private GeneratedCharacter character;
    private Vector3 targetPosition;
    
    void Update()
    {
        if (character.canMove)
        {
            // 배회 로직
            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                targetPosition = GetRandomPosition();
            }
            
            transform.position = Vector3.MoveTowards(
                transform.position, 
                targetPosition, 
                character.moveSpeed * Time.deltaTime
            );
        }
    }
}
```

### 4. 캐릭터 간 상호작용

```csharp
// 두 캐릭터가 서로 인사
void CharactersGreet(GeneratedCharacter char1, GeneratedCharacter char2)
{
    Debug.Log($"{char1.characterName}: 안녕하세요 {char2.characterName}님!");
    Debug.Log($"{char2.characterName}: 반갑습니다 {char1.characterName}님!");
}
```

---

## 🎨 UI 설정

### Character Panel 레이아웃

```
CharacterPanel (Canvas)
├── Background (Image)
├── Title (TextMeshPro): "캐릭터 생성"
├── ContextSection
│   ├── Label: "상황 설명"
│   ├── ContextInput (TMP Input Field)
│   └── EvaluateButton (Button): "평가"
├── ManualSection
│   ├── Label: "수동 생성"
│   ├── NameInput (TMP Input Field): Placeholder "이름"
│   ├── DescInput (TMP Input Field): Placeholder "외모 설명"
│   └── GenerateButton (Button): "생성"
└── CharacterList (TextMeshPro): 생성된 캐릭터 목록
```

---

## 🐛 문제 해결

### 캐릭터가 안 보임
```
1. Sorting Order 확인 (20 이상)
2. Character Container 위치 확인
3. Camera의 Orthographic Size 확인
4. Scene View에서 Gizmos로 위치 확인 (마젠타 구)
```

### 캐릭터가 너무 작거나 큼
```
Character Scale 조정:
- 작게: (0.8, 0.8)
- 중간: (1.5, 1.5)
- 크게: (2.5, 2.5)

또는 Pixels Per Unit 조정:
- 큰 캐릭터: 16
- 기본: 32
- 작은 캐릭터: 64
```

### 캐릭터가 겹침
```csharp
// 스폰 위치 분산 조정
float distance = UnityEngine.Random.Range(4f, 8f); // 더 넓게
```

### 애니메이션이 동기화됨
```
각 캐릭터의 timeOffset이 자동으로 랜덤 설정됨
문제가 있다면:
anim.timeOffset = Random.Range(0f, 10f);
```

---

## 📊 성능 최적화

### 1. 캐릭터 수 제한
```csharp
void Start()
{
    maxCharacters = 10; // 최대 10명
}

// 생성 전 확인
if (charSystem.GetCharacterCount() >= maxCharacters)
{
    charSystem.RemoveCharacter(charSystem.GetAllCharacters()[0]); // 가장 오래된 캐릭터 제거
}
```

### 2. 텍스처 메모리
```
Character Sprite Size:
- 모바일: 64
- PC: 128
- 고품질: 256
```

### 3. 오브젝트 풀링
```csharp
// 재사용을 위한 비활성화
character.gameObject.SetActive(false);

// 나중에 재활성화
character.gameObject.SetActive(true);
```

---

## ✅ 체크리스트

- [ ] CharacterGenerationSystem GameObject 생성
- [ ] Main NPC Agent 연결
- [ ] Character Container 생성 및 연결
- [ ] Character Settings 설정
- [ ] UI Panel 생성 및 연결 (선택사항)
- [ ] OpenAI API 키 확인
- [ ] Play 후 캐릭터 생성 테스트

---

## 🎉 완료!

이제 **NPC가 필요에 따라 새로운 캐릭터를 동적으로 생성**할 수 있습니다!

### 가능한 시나리오:
- 🎭 **동료 생성**: "외로워요" → 친구 캐릭터 생성
- 🛍️ **상점 주인**: "물건을 사고 싶어요" → 상인 캐릭터 생성
- 🏫 **선생님**: "공부를 가르쳐줄 사람이 필요해요" → 교사 캐릭터 생성
- 👥 **파티 멤버**: "모험을 함께할 동료가 필요해요" → 전사, 마법사 생성
- 🎪 **군중**: "파티를 열자!" → 여러 캐릭터 생성

**Happy Character Creating!** 🎮✨
