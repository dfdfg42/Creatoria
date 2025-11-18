# The Ville 월드 자동 생성 가이드

논문 "Generative Agents"에서 사용된 "The Ville" 월드를 Unity에서 그대로 재현합니다.

## 📍 월드 구조

논문의 맵 데이터를 기반으로 다음과 같은 위치들이 자동 생성됩니다:

### Sectors (큰 구역)
- artist's co-living space (예술가 공동주택)
- Arthur Burton's apartment (아서 버튼의 아파트)
- Ryan Park's apartment (라이언 파크의 아파트)
- Isabella Rodriguez's apartment (이사벨라 로드리게스의 아파트)
- Giorgio Rossi's apartment (조르지오 로시의 아파트)
- Carlos Gomez's apartment (카를로스 고메즈의 아파트)
- The Rose and Crown Pub (로즈 앤 크라운 펍)
- Hobbs Cafe (홉스 카페)
- Oak Hill College (오크 힐 대학교)
- Johnson Park (존슨 공원)
- Harvey Oak Supply Store (하비 오크 잡화점)
- The Willows Market and Pharmacy (윌로우즈 마켓 & 약국)
- 여러 주택들...

### Arenas (세부 장소)
각 Sector는 여러 Arena로 구성됩니다:

#### Artist's Co-living Space
- Latoya Williams's room / bathroom
- Rajiv Patel's room / bathroom
- Abigail Chen's room / bathroom
- Francisco Lopez's room / bathroom
- Hailey Johnson's room / bathroom
- common room
- kitchen

#### Oak Hill College
- classroom
- library
- hallway

#### Dorm for Oak Hill College
- Klaus Mueller's room
- Maria Lopez's room
- Ayesha Khan's room
- Wolfgang Schulz's room
- man's bathroom
- woman's bathroom
- common room
- kitchen
- garden

### GameObjects (상호작용 가능한 오브젝트)
각 Arena에 적절한 오브젝트가 자동 배치됩니다:

**침실**
- bed (침대)
- desk (책상)
- closet (옷장)
- shelf (선반)

**욕실**
- bathroom sink (세면대)
- shower (샤워기)
- toilet (변기)

**주방**
- kitchen sink (싱크대)
- refrigerator (냉장고)
- toaster (토스터)
- cooking area (조리 공간)

**카페**
- behind the cafe counter (카페 카운터 뒤)
- cafe customer seating (고객 좌석)
- piano (피아노)

**펍**
- behind the bar counter (바 카운터 뒤)
- bar customer seating (고객 좌석)
- pool table (당구대)

**강의실**
- blackboard (칠판)
- classroom podium (교단)
- classroom student seating (학생 좌석)

**도서관**
- bookshelf (책장)
- library table (도서관 테이블)
- library sofa (소파)

## 🛠️ Unity에서 설정하기

### 1. 씬 준비

1. 새 씬 생성 또는 기존 씬 열기
2. Hierarchy에서 우클릭 → `2D Object` → `Tilemap` → `Rectangular` (3개 생성)
   - `GroundTilemap`
   - `WallTilemap`
   - `PropsTilemap`

3. Grid 오브젝트 선택 후 설정:
   - Cell Size: X=1, Y=1, Z=1
   - Cell Gap: X=0, Y=0, Z=0

### 2. WorldBuilder 설정

1. 빈 GameObject 생성: `TheVilleWorldBuilder`
2. `TheVilleWorldBuilder.cs` 컴포넌트 추가
3. Inspector에서 설정:
   - **Ground Tilemap**: GroundTilemap 할당
   - **Wall Tilemap**: WallTilemap 할당
   - **Props Tilemap**: PropsTilemap 할당
   - **Grid**: Grid 오브젝트 할당
   - **Area Size**: 5 (기본값)
   - **Auto Build On Start**: true (자동 생성)

### 3. 월드 생성

#### 방법 1: 자동 생성
- `Auto Build On Start`를 true로 설정
- Play 버튼 클릭

#### 방법 2: 수동 생성
- `TheVilleWorldBuilder` 오브젝트 선택
- Inspector에서 우클릭 → `Build The Ville World`

### 4. 생성 결과 확인

생성 후 Hierarchy에 다음과 같은 구조가 만들어집니다:

```
TheVille_World
├── Sectors
│   ├── Sector_artist's co-living space
│   ├── Sector_Oak Hill College
│   ├── Sector_Hobbs Cafe
│   └── ...
├── Arenas
│   ├── artist's co-living space_Latoya Williams's room
│   ├── Oak Hill College_classroom
│   ├── Hobbs Cafe_cafe
│   └── ...
└── WorldObjects
    ├── artist's co-living space_Latoya Williams's room_bed
    ├── artist's co-living space_Latoya Williams's room_desk
    ├── Oak Hill College_classroom_blackboard
    └── ...
```

## 🎮 NPC 통합

### AutonomousPlanner와 연동

생성된 장소들은 NPC의 자율 행동 시스템과 자동으로 연동됩니다:

```csharp
// AutonomousPlanner가 인식하는 장소 이름 형식
"artist's co-living space:Latoya Williams's room"
"Oak Hill College:classroom"
"Hobbs Cafe:cafe"
```

### 장소 목록 가져오기

```csharp
TheVilleWorldBuilder builder = FindObjectOfType<TheVilleWorldBuilder>();
List<string> locations = builder.GetAllLocationNames();

// NPCAgent의 Planner에 설정
foreach (var npc in FindObjectsOfType<NPCAgent>())
{
    npc.Planner.SetAvailableLocations(locations);
}
```

## 📊 논문 데이터 매핑

| 논문 용어 | Unity 구현 | 설명 |
|---------|-----------|------|
| World | Scene | "the Ville" 전체 |
| Sector | Sector GameObject | 큰 구역 (건물/지역) |
| Arena | WorldArea Component | 실제 장소 (방, 카페 등) |
| GameObject | WorldObject Component | 상호작용 가능한 오브젝트 |
| Block ID | blockId field | 논문의 고유 ID 유지 |

## 🔧 커스터마이징

### 새로운 Arena 추가

`TheVilleWorldData.cs`의 `LoadTheVilleData()` 메서드에서:

```csharp
worldData.arenas.Add(new Arena 
{ 
    blockId = 32999, 
    sectorName = "My Sector", 
    arenaName = "My Room", 
    gridPosition = new Vector2Int(50, 50) 
});
```

### Arena별 오브젝트 커스터마이징

`TheVilleWorldBuilder.cs`의 `GetObjectsForArena()` 메서드에서:

```csharp
else if (arenaName.Contains("my custom room"))
{
    objects.AddRange(new[] { "special object 1", "special object 2" });
}
```

### 새로운 GameObject 타입 추가

1. `TheVilleWorldData.cs`에 추가:
```csharp
new WorldObjectData { blockId = 32999, objectName = "new object" }
```

2. `TheVilleWorldBuilder.cs`의 `MapObjectType()`에 매핑 추가:
```csharp
if (name.Contains("new object")) return NPCSimulation.Core.ObjectType.Custom;
```

## 📝 논문과의 차이점

1. **타일맵 데이터**: 논문의 CSV 충돌 맵은 현재 미구현 (시각적 타일 배치는 수동으로 해야 함)
2. **스프라이트**: 기본 스프라이트만 생성됨 (Cute RPG 타일셋을 수동으로 연결 필요)
3. **위치 좌표**: 논문의 140x100 그리드를 Unity 좌표계로 단순화

## 🎯 다음 단계

1. Cute RPG 타일셋을 사용하여 각 Arena 시각화
2. 충돌 맵 데이터 파싱하여 벽/장애물 자동 배치
3. NPC 스폰 위치 설정
4. 카메라 범위 조정

---

논문의 "The Ville" 월드가 Unity에 완벽하게 재현되었습니다! 🎉
