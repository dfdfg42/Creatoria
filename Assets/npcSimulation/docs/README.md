# NPC Simulation - Unity C# 포팅

Python 기반의 Generative Agents NPC 시스템을 Unity C#으로 완전히 포팅한 버전입니다.

## 🎯 특징

- ✅ **완전한 Unity 통합**: Python 서버 없이 Unity 내에서 모든 것 처리
- ✅ **메모리 시스템**: 단기/장기 메모리, 키워드 인덱싱, 리플렉션
- ✅ **자율 행동**: 일일 계획 수립 및 시간별 스케줄 관리
- ✅ **대화 관리**: 맥락 이해 및 자연스러운 응답
- ✅ **환경 변경**: AI 이미지 생성을 통한 동적 타일/오브젝트 생성

## 📁 프로젝트 구조

```
Assets/npcSimulation/
├── Core/
│   ├── Memory.cs                    # 메모리 데이터 구조
│   ├── OpenAIClient.cs              # OpenAI API 클라이언트
│   ├── MemoryManager.cs             # 메모리 관리 시스템
│   ├── ConversationManager.cs       # 대화 관리 시스템
│   ├── AutonomousPlanner.cs         # 자율 행동 계획
│   └── NPCAgent.cs                  # 메인 NPC 에이전트
├── Environment/
│   └── EnvironmentModificationSystem.cs  # 환경 변경 시스템
└── Demo/
    └── NPCDemoController.cs         # 데모 컨트롤러
```

## 🚀 빠른 시작

### 1. OpenAI API 키 설정

NPCAgent 컴포넌트의 Inspector에서 `openAIKey` 필드에 API 키를 입력하세요.

### 2. 씬 설정

```
Hierarchy:
├── NPCAgent (NPCAgent.cs)
├── EnvironmentSystem (EnvironmentModificationSystem.cs)
├── DemoController (NPCDemoController.cs)
├── Tilemap (Tilemap 컴포넌트)
└── Canvas (UI)
```

### 3. 컴포넌트 연결

**NPCAgent**:
- `npcName`: NPC 이름
- `persona`: NPC 성격/설정
- `openAIKey`: OpenAI API 키

**EnvironmentModificationSystem**:
- `npcAgent`: NPCAgent 참조
- `targetTilemap`: 타일을 배치할 Tilemap
- `objectContainer`: 오브젝트의 부모 Transform

**NPCDemoController**:
- `npcAgent`: NPCAgent 참조
- `environmentSystem`: EnvironmentModificationSystem 참조
- UI 요소들 연결

## 💬 사용 방법

### 1. 플레이어와 대화

```csharp
NPCAgent npc = GetComponent<NPCAgent>();

npc.RespondToPlayer("안녕하세요!", "Player", (response) => {
    Debug.Log($"NPC: {response}");
});
```

### 2. 환경 변경 요청

```csharp
EnvironmentModificationSystem envSystem = GetComponent<EnvironmentModificationSystem>();

// NPC가 판단하도록
envSystem.RequestEnvironmentChange("여기 너무 어두워");

// 또는 수동으로
envSystem.ManualGenerateTile("cozy lamp, pixel art, 32x32", "corner");
```

### 3. 자율 행동

NPCAgent의 `enableAutonomousBehavior`를 true로 설정하면 자동으로:
- 일일 계획 수립
- 시간별 행동 실행
- 주기적 상태 업데이트

## 🎮 키보드 단축키

- **F1**: 채팅 패널 토글
- **F2**: 환경 패널 토글
- **F5**: 상태 업데이트
- **ESC**: 대화 종료
- **Enter**: 메시지 전송

## 📊 시스템 아키텍처

```
NPCAgent
├── MemoryManager
│   ├── 단기 메모리
│   ├── 장기 메모리
│   └── 지식 베이스
├── ConversationManager
│   ├── 대화 버퍼
│   └── 대화 요약
└── AutonomousPlanner
    ├── 일일 요구사항
    └── 시간별 스케줄

OpenAIClient
├── Chat Completion (GPT-4)
├── Embedding (text-embedding-ada-002)
└── Image Generation (DALL-E 3)

EnvironmentModificationSystem
├── 이미지 생성
├── 후처리
└── 배치
```

## 🔧 커스터마이징

### 메모리 설정

```csharp
MemoryManager memoryMgr = new MemoryManager(llmClient, "NPC이름", "페르소나");

// 메모리 추가
memoryMgr.AddMemory(MemoryType.Event, "이벤트 설명", importance: 8, this);

// 메모리 검색
List<Memory> memories = memoryMgr.RetrieveRelevantMemories("쿼리", topK: 5);
```

### 계획 수립

```csharp
AutonomousPlanner planner = new AutonomousPlanner(npcAgent, llmClient);

// 사용 가능한 위치 설정
planner.AvailableLocations = new List<string> {
    "집:침실", "도서관:열람실", "카페:휴게실"
};

// 계획 생성
planner.CreateNewDailyPlan(DateTime.Now, this);
```

## 🎨 이미지 생성 프롬프트 예시

```csharp
// 픽셀 아트 스타일
"cozy warm lamp, pixel art, 32x32px, top-down view, isolated object, white background"

// 현실적인 스타일
"wooden table, photorealistic, high quality, top-down view, isolated, neutral background"

// 판타지 스타일
"magical crystal, fantasy art, glowing, 64x64px, transparent background"
```

## ⚠️ 주의사항

1. **OpenAI API 비용**: DALL-E 3 호출은 비용이 발생합니다
2. **비동기 처리**: 모든 AI 호출은 Coroutine으로 처리됩니다
3. **메모리 관리**: 큰 이미지는 메모리를 많이 사용할 수 있습니다

## 🐛 디버깅

```csharp
// 메모리 상태 확인
Debug.Log($"Event memories: {memoryMgr.eventMemories.Count}");
Debug.Log($"Thought memories: {memoryMgr.thoughtMemories.Count}");

// 현재 계획 확인
PlanItem current = planner.GetCurrentActivity(DateTime.Now);
Debug.Log($"Current activity: {current?.activity}");

// 대화 이력 확인
string history = conversationMgr.GetRecentConversation(5);
Debug.Log(history);
```

## 📝 TODO

- [ ] 배경 제거 기능 (rembg C# 포팅 또는 외부 서비스 사용)
- [ ] 리플렉션 시스템 완성
- [ ] 메모리 영속화 (JSON 저장/로드)
- [ ] 멀티 NPC 상호작용
- [ ] 감정 시스템 확장

## 📚 참고

- [Generative Agents 논문](https://arxiv.org/abs/2304.03442)
- [OpenAI API 문서](https://platform.openai.com/docs)
- Python 원본: `npcSimulation/` 디렉토리

## 🤝 기여

이슈나 개선 제안은 언제든 환영합니다!
