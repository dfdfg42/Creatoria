# 🏷️ Area 이름 설정 가이드

## ❗ 중요: NPC가 장소를 찾지 못하는 문제 해결

### 문제 상황
```
AI 계획: "집:내방"으로 이동하려고 함
Unity Scene: GameObject "Area", areaName = "myroom"
→ 이름이 달라서 매칭 실패!
```

### ✅ 해결책: 일관된 이름 사용

---

## 방법 1: 영어 이름으로 통일 (권장)

### Unity Scene 설정
```
GameObject: MyRoom
├── WorldArea Component
│   ├── areaName: "myroom"         ← 소문자, 띄어쓰기 없음
│   ├── sectorName: "home"         ← (선택) 큰 구역 이름
│   └── description: "My cozy bedroom"
```

### 이름 규칙
```
✅ 좋은 예:
- myroom
- cafe
- library
- classroom

❌ 나쁜 예:
- 내방 (한글)
- My Room (띄어쓰기)
- MyRoom (대문자 섞임 - 괜찮지만 소문자 추천)
```

---

## 방법 2: Sector:Arena 형식 사용 (논문 방식)

### Unity Scene 설정
```
GameObject: MyRoom
├── WorldArea Component
│   ├── areaName: "bedroom"        ← Arena 이름
│   ├── sectorName: "home"         ← Sector 이름
│   └── GetFullName() → "home:bedroom"
```

### 예시들

#### 집
```
GameObject: Home_Kitchen
├── sectorName: "home"
└── areaName: "kitchen"
→ FullName: "home:kitchen"

GameObject: Home_Bedroom
├── sectorName: "home"
└── areaName: "bedroom"
→ FullName: "home:bedroom"
```

#### 카페
```
GameObject: Cafe_Counter
├── sectorName: "cafe"
└── areaName: "counter"
→ FullName: "cafe:counter"

GameObject: Cafe_Seating
├── sectorName: "cafe"
└── areaName: "seating"
→ FullName: "cafe:seating"
```

---

## 방법 3: 유연한 매칭 활용 (현재 구현됨)

### 현재 시스템이 지원하는 매칭
```csharp
1. 정확한 매칭 (대소문자 구분)
   "cafe:counter" → "cafe:counter" ✅

2. 대소문자 무시 매칭
   "CAFE:COUNTER" → "cafe:counter" ✅
   "Cafe:Counter" → "cafe:counter" ✅

3. areaName만 매칭
   "counter" → "cafe:counter" ✅
   "bedroom" → "home:bedroom" ✅

4. 부분 문자열 매칭
   "cafe" → "cafe:counter" ✅
   "room" → "myroom" ✅
```

### 하지만 주의!
```
❌ 한글 → 영어 자동 번역은 안 됨!
"내방" ≠ "myroom"
"카페" ≠ "cafe"
```

---

## 🎯 권장 설정 방법

### Step 1: Scene에 WorldArea 생성

```
Hierarchy 우클릭 > Create Empty
이름: "MyRoom"

Add Component > WorldArea
```

### Step 2: WorldArea 설정

```
WorldArea Component:
├── Area Name: "myroom"          ← 필수! 소문자 영어
├── Sector Name: "home"          ← (선택) 큰 구역
├── Description: "편안한 내 방"   ← (선택) 설명
├── Area Size: (5, 5)            ← 구역 크기
└── Objects In Area: (자동 감지)
```

### Step 3: 확인

Play 모드에서 Console 확인:
```
[AutonomousPlanner] 사용 가능한 장소 2개: home:myroom, cafe
[NPCMovement] ✅ Found area: 'home:myroom'
```

---

## 🔧 디버깅 팁

### 1. Scene의 모든 장소 확인
```csharp
// Console에서 확인
WorldArea[] areas = FindObjectsOfType<WorldArea>();
foreach (var area in areas)
{
    Debug.Log($"Area: {area.GetFullName()}");
}
```

### 2. NPC가 생성한 계획 확인
```
[AutonomousPlanner] 시간별 일정 8개 생성됨
  📍 07:00 | wake up | home:myroom
  📍 09:00 | study | cafe
```

### 3. 이동 시도 확인
```
[NPCMovement] 🔍 Searching for area: 'home:myroom'
[NPCMovement] ✅ Found area: 'home:myroom'
[NPCMovement] Match type: Exact FullName
```

---

## 📋 체크리스트

Scene 설정 전:
- [ ] WorldArea GameObject 생성
- [ ] areaName을 소문자 영어로 설정
- [ ] (선택) sectorName 설정
- [ ] Area Size 설정
- [ ] GameObject 위치 적절히 배치

Play 모드 확인:
- [ ] Console에 "사용 가능한 장소" 로그 확인
- [ ] NPC 계획에서 장소 이름 확인
- [ ] "Area not found" 에러 없음
- [ ] NPC가 실제로 이동함

---

## 💡 예제: 간단한 맵 만들기

### Scene 구성
```
Hierarchy:
├── Grid
│   ├── GroundTilemap
│   └── ObjectTilemap
├── Areas
│   ├── Home_MyRoom (GameObject)
│   │   └── WorldArea: areaName="myroom", sectorName="home"
│   └── CafeMain (GameObject)
│       └── WorldArea: areaName="cafe", sectorName=""
└── NPCs
    └── NPC_Seoa
        └── NPCAgent
```

### 결과
```
AI 생성 계획:
07:00 | wake up | home:myroom     ← ✅ 찾음!
09:00 | have coffee | cafe        ← ✅ 찾음!

Console:
[AutonomousPlanner] 사용 가능한 장소 2개: home:myroom, cafe
[NPCMovement] ✅ Found area: 'home:myroom'
[NPCAgent] ✅ Arrived at home:myroom, starting activity: wake up
```

---

## 🆘 문제 해결

### "Area not found" 에러
```
문제: [NPCMovement] ❌ Area not found: '집:내방'

해결:
1. WorldArea의 areaName을 "myroom"으로 변경
2. 또는 AI 프롬프트에서 한글 사용 금지
3. Console에서 실제 장소 이름 확인:
   [AutonomousPlanner] 사용 가능한 장소: ...
```

### AI가 이상한 장소 이름 생성
```
문제: AI가 "도서관:열람실" 같은 한글 이름 생성

해결:
1. AutonomousPlanner.cs의 프롬프트 확인
2. "반드시 이 중에서만 선택!" 강조됨
3. temperature 낮추기 (0.3 이하)
```

### NPC가 이동 안 함
```
문제: 계획은 생성되는데 이동 안 함

확인:
1. NPCMovement 컴포넌트 있나?
2. "Already at target location" 로그?
3. PathfindingSystem 설정 확인
```

---

## ✅ 완성!

이제 NPC가 올바른 장소를 찾아서 이동할 수 있습니다! 🎉
