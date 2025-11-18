# Cute RPG Tileset 설정 가이드

논문에서 사용했던 Cute RPG 타일셋과 캐릭터 스프라이트가 Unity로 이식되었습니다.

## 📂 파일 위치

- **타일셋**: `Assets/npcSimulation/Tilesets/`
  - 45개의 타일셋 PNG 파일
  - CuteRPG_Field, CuteRPG_Interior, CuteRPG_Village 등
  - v3 타일셋 (grassland-grass, paths, props, water)

- **캐릭터**: `Assets/npcSimulation/Characters/`
  - 90개의 캐릭터 스프라이트 파일
  - !Character_RM_001.png ~ 053.png (NPC 캐릭터)
  - 적, 애니메이션, 오브젝트 스프라이트

## 🎮 Unity에서 설정하기

### 1. Tilemap 설정

#### 1-1. Tilemap 생성
1. Hierarchy에서 우클릭 → `2D Object` → `Tilemap` → `Rectangular`
2. 4개의 레이어 생성:
   - `Ground` (지면)
   - `Props` (오브젝트)
   - `Water` (물)
   - `Paths` (길)

#### 1-2. Tile Palette 생성
1. `Window` → `2D` → `Tile Palette`
2. `Create New Palette` 클릭
3. 각 타일셋마다 팔레트 생성:
   - `CuteRPG_Field_Palette`
   - `CuteRPG_Interior_Palette`
   - `CuteRPG_Village_Palette`

#### 1-3. 타일 이미지 설정
1. `Assets/npcSimulation/Tilesets/` 폴더의 PNG 파일 선택
2. Inspector에서:
   - `Texture Type`: `Sprite (2D and UI)`
   - `Sprite Mode`: `Multiple`
   - `Pixels Per Unit`: `32` (RPG Maker 타일 기준)
   - `Filter Mode`: `Point (no filter)` (픽셀 아트 스타일 유지)
   - `Compression`: `None`
3. `Apply` 클릭
4. `Sprite Editor` 버튼 클릭 → `Slice` → `Grid By Cell Size`
   - Cell Size: X=32, Y=32
   - `Slice` 클릭

#### 1-4. TilemapManager 연결
1. 빈 GameObject 생성: `TilemapManager`
2. `TilemapManager.cs` 컴포넌트 추가
3. Inspector에서 Tilemap 레퍼런스 연결:
   - `Ground Tilemap`
   - `Props Tilemap`
   - `Water Tilemap`
   - `Path Tilemap`

### 2. 캐릭터 스프라이트 설정

#### 2-1. 캐릭터 이미지 설정
1. `Assets/npcSimulation/Characters/` 폴더의 PNG 파일 선택
2. Inspector에서:
   - `Texture Type`: `Sprite (2D and UI)`
   - `Sprite Mode`: `Multiple`
   - `Pixels Per Unit`: `32`
   - `Filter Mode`: `Point (no filter)`
3. `Apply` 클릭
4. `Sprite Editor` 버튼 클릭 → `Slice` → `Grid By Cell Count`
   - Column: 3, Row: 4 (RPG Maker 캐릭터 기준)
   - `Slice` 클릭

#### 2-2. NPC GameObject 설정
1. NPC GameObject에 `CharacterSpriteManager.cs` 컴포넌트 추가
2. Inspector에서:
   - `Character Sprites`: 사용할 캐릭터 스프라이트 시트 할당
   - `Animation Speed`: 0.2 (기본값)

#### 2-3. 코드에서 캐릭터 로드
```csharp
// NPCAgent.cs의 Start() 또는 InitializeAgent()에서
CharacterSpriteManager spriteManager = GetComponent<CharacterSpriteManager>();
spriteManager.LoadCharacterSprite("이서아"); // 또는 다른 NPC ID
```

### 3. 장소 매핑 설정

`TilemapManager.cs`의 `InitializeLocationMap()` 메서드에서 타일 위치와 장소 이름을 매핑합니다:

```csharp
// 집 영역
AddLocationArea(new Vector3Int(-10, -10, 0), new Vector3Int(-5, -5, 0), "집:침실");
AddLocationArea(new Vector3Int(-10, -15, 0), new Vector3Int(-5, -11, 0), "집:부엌");

// 대학교 영역
AddLocationArea(new Vector3Int(5, 5, 0), new Vector3Int(15, 15, 0), "대학교:강의실");
AddLocationArea(new Vector3Int(0, 0, 0), new Vector3Int(4, 4, 0), "대학교:중앙광장");
```

### 4. 권장 타일 사용

#### 실내 (집, 카페, 도서관)
- `CuteRPG_Interior_B.png` - 바닥 타일
- `CuteRPG_Interior_C.png` - 가구와 장식
- `CuteRPG_Houses_RPGMaker_*.png` - 집 내부

#### 실외 (대학교, 마을)
- `CuteRPG_Field_B.png` - 풀밭 바닥
- `CuteRPG_Village_*.png` - 마을 건물과 오브젝트
- `tileset-grassland-*.png` - v3 스타일 풀밭

#### 특수 타일
- `CuteRPG_Field_A1.png` - 물 애니메이션 타일
- `CuteRPG_*_Doors*.png` - 문 애니메이션

## 🎨 캐릭터 매핑

논문에서 사용한 캐릭터들:

- **이서아**: `!Character_RM_001.png` (여대생, 디자인 전공)
- **플레이어**: `!Character_RM_002.png` 등 선택 가능
- **추가 NPC**: `!Character_RM_003~053.png`

## 🔧 스크립트 통합

### NPCAgent 초기화 시
```csharp
// CharacterSpriteManager와 TilemapManager가 자동으로 연동됨
CharacterSpriteManager spriteManager = GetComponent<CharacterSpriteManager>();
TilemapManager tilemapManager = FindObjectOfType<TilemapManager>();
```

### 이동 시 애니메이션 자동 처리
`NPCMovement.cs`가 자동으로 `CharacterSpriteManager`를 통해:
- 이동 방향에 따른 스프라이트 변경
- 걷기 애니메이션 재생
- 정지 시 Idle 포즈 표시

## 📝 참고사항

- **타일 크기**: 32x32 픽셀 (RPG Maker VX Ace 표준)
- **캐릭터 크기**: 32x32 픽셀 (3x4 프레임 구성)
- **Sorting Layer**: Ground < Paths < Props < Characters 순서로 설정
- **Physics**: 2D Collider를 Props Tilemap에 추가하여 충돌 처리

## 🎯 Scene 구성 예시

```
Scene
├── Grid
│   ├── Ground Tilemap (Sorting Layer: Ground, Order: 0)
│   ├── Paths Tilemap (Sorting Layer: Ground, Order: 1)
│   ├── Water Tilemap (Sorting Layer: Ground, Order: 2)
│   └── Props Tilemap (Sorting Layer: Props, Order: 0, Tilemap Collider 2D)
├── TilemapManager (TilemapManager.cs)
├── NPCs
│   └── NPC_Seoa
│       ├── NPCAgent.cs
│       ├── NPCMovement.cs
│       ├── CharacterSpriteManager.cs (Sorting Layer: Characters, Order: 0)
│       └── CircleCollider2D
└── Main Camera
```

---

이제 논문에서 사용했던 비주얼 스타일 그대로 Unity에서 구현할 수 있습니다! 🎉
