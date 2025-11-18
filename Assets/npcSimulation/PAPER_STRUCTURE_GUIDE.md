# The Ville 월드 빌더 - 논문 구조 가이드

## 📋 논문의 실제 구조

### Tilemap 레이어 (논문 the_ville.tmx 기준)
```
논문은 단 2개 레이어만 사용:
1. Bottom Ground Layer - 바닥 타일 (잔디, 땅 등)
2. Object Layer(들) - 건물, 가구, 벽 등 모든 오브젝트
```

### 데이터 구조
```
논문은 GameObject가 아닌 CSV 데이터로 관리:
- world_blocks.csv → 월드 메타데이터
- sector_blocks.csv → 19개 Sector 정보
- arena_blocks.csv → 60+ Arena 정보
- game_object_blocks.csv → 40+ Object 타입
- collision_maze.csv → 충돌 맵 (0=이동가능, 32125=벽)
```

## 🎯 Unity 구현 방식

### 방식 1: 논문 완전 재현 (데이터 기반)
```
✅ 논문과 100% 동일
- WorldArea/WorldObject는 데이터로만 존재
- GameObject는 생성 안함 (또는 비활성화)
- NPC는 데이터를 참조하여 이동

사용 스크립트: TheVilleWorldBuilder_Simple.cs
```

### 방식 2: Unity 친화적 (시각화 포함)
```
⚠️ 논문보다 복잡하지만 Unity에서 편집 쉬움
- WorldArea/WorldObject를 GameObject로 생성
- Scene에서 시각적으로 확인 가능
- 직접 편집 가능

사용 스크립트: TheVilleWorldBuilder.cs (기존)
```

## 🛠️ 추천 설정 (논문 방식)

### 1. Tilemap 설정

```
Hierarchy 구조:
Grid
├── GroundTilemap (Bottom Ground)
└── ObjectTilemap (Buildings, Furniture, etc.)
```

**GroundTilemap 설정:**
- Sorting Layer: Ground (0)
- Order in Layer: 0

**ObjectTilemap 설정:**
- Sorting Layer: Props (2)
- Order in Layer: 0
- Tilemap Collider 2D 추가 (충돌 감지용)

### 2. TheVilleWorldBuilder 설정

**Simple 버전 (논문 방식):**
```csharp
// TheVilleWorldBuilder_Simple.cs 사용
public Tilemap groundTilemap;    // GroundTilemap 할당
public Tilemap objectTilemap;    // ObjectTilemap 할당
public Grid grid;                // Grid 할당
public bool visualizeAreas = false; // false = 데이터만
```

**Full 버전 (시각화):**
```csharp
// TheVilleWorldBuilder.cs 사용
public Tilemap groundTilemap;
public Tilemap wallTilemap;      // 없어도 됨 (선택)
public Tilemap propsTilemap;     // ObjectTilemap과 동일
public bool createGameObjects = true; // GameObject 생성
```

## ❓ Prefab 필요 여부

### WorldArea Prefab
```
❌ 필요 없음 (논문 방식)
  → 데이터만 있으면 됨
  → TheVilleWorldBuilder가 자동 생성

✅ 필요함 (시각화 원할 경우)
  → 커스텀 아이콘/색상 표시
  → Inspector에서 수동 편집
```

### WorldObject Prefab
```
❌ 필요 없음 (논문 방식)
  → GameObject 안만들고 타일로만 표시
  
✅ 필요함 (상호작용 원할 경우)
  → 침대에 누우기, 책상 사용 등
  → 플레이어/NPC 상호작용
```

## 🚀 빠른 시작 (논문 방식)

### Step 1: Tilemap 생성
```
1. Hierarchy 우클릭 → 2D Object → Tilemap → Rectangular
2. 이름 변경: "GroundTilemap"
3. 한번 더: "ObjectTilemap"
```

### Step 2: Builder 컴포넌트 추가
```
1. 빈 GameObject 생성: "WorldBuilder"
2. TheVilleWorldBuilder_Simple.cs 추가
3. Inspector에서:
   - Ground Tilemap: GroundTilemap
   - Object Tilemap: ObjectTilemap
   - Grid: Grid
   - Visualize Areas: false (체크 해제)
```

### Step 3: 월드 생성
```
1. WorldBuilder 선택
2. 우클릭 → "Build The Ville World"
3. Console에서 결과 확인
```

### 결과
```
[TheVilleWorldBuilder] 완료!
  - Sectors: 19
  - Arenas: 60+
  - Objects: 40+
  - 방식: 논문처럼 데이터 구조만
```

## 📊 두 방식 비교

| 항목 | 논문 방식 (Simple) | Unity 방식 (Full) |
|------|-------------------|-------------------|
| Tilemap 레이어 | 2개 (Ground + Object) | 3개 (Ground + Wall + Props) |
| GameObject 생성 | 최소 (또는 없음) | 많음 (모든 Area/Object) |
| Prefab 필요 | ❌ | ✅ |
| 메모리 사용 | 적음 | 많음 |
| 논문 일치도 | 100% | ~80% |
| Unity 편집 | 어려움 | 쉬움 |
| 추천 대상 | 연구/재현 목적 | 게임 개발 |

## 🎯 결론

### 논문 완전 재현이 목표라면:
```
✅ TheVilleWorldBuilder_Simple.cs 사용
✅ 2개 Tilemap만 (Ground + Object)
✅ visualizeAreas = false
❌ Prefab 불필요
❌ GameObject 생성 안함
```

### Unity 게임 개발이 목표라면:
```
✅ TheVilleWorldBuilder.cs 사용
✅ 3개 Tilemap (Ground + Wall + Props)
✅ createGameObjects = true
✅ WorldArea/Object Prefab 생성
✅ Scene에서 직접 편집 가능
```

## 💡 추천

**당신의 경우 (논문 재현):**
```
→ TheVilleWorldBuilder_Simple.cs
→ 2 Tilemap (Ground + Object)
→ No Prefabs needed
→ visualizeAreas = false
```

타일 배치는 Tiled Editor로 하거나, Unity Tile Palette로 수동 배치하세요!
