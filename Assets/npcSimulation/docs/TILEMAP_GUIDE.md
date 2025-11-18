# 2D Tilemap 환경 변경 시스템 가이드

## 🎨 개선된 기능

### ✅ 수정 사항
1. **Tilemap 전용 최적화**: 2D 프로젝트에 맞게 완전히 재작성
2. **듀얼 모드**: Tilemap 또는 Sprite GameObject 선택 가능
3. **Grid 스냅**: 타일이 정확히 그리드에 맞춰짐
4. **타일 관리**: 생성/삭제/추적 기능 추가
5. **레이어 분리**: 바닥/오브젝트 Tilemap 분리

---

## 🏗️ Hierarchy 구조

```
Scene
├── Grid
│   ├── GroundTilemap          # 바닥 레이어 (Sorting Order: 0)
│   └── ObjectTilemap          # 오브젝트 레이어 (Sorting Order: 10)
├── NPCAgent
├── EnvironmentSystem
├── GeneratedObjects (Empty)    # Sprite 모드용
└── Canvas (UI)
```

---

## 📋 단계별 설정

### 1. Grid & Tilemap 생성

```
1. Hierarchy 우클릭 > 2D Object > Tilemap > Rectangular
   → "Grid" GameObject 자동 생성됨

2. Grid 하위에 두 개의 Tilemap 생성:
   - GroundTilemap (바닥)
   - ObjectTilemap (오브젝트/장식)
```

**Grid 설정**:
```
Grid Component:
  Cell Size: X=1, Y=1, Z=1
  Cell Gap: X=0, Y=0, Z=0
  Cell Layout: Rectangle
  Cell Swizzle: XYZ
```

**GroundTilemap 설정**:
```
Tilemap Renderer:
  Sorting Layer: Default
  Order in Layer: 0
  
Tilemap Collider 2D: (선택사항)
  Used By Composite: ✓
```

**ObjectTilemap 설정**:
```
Tilemap Renderer:
  Sorting Layer: Default
  Order in Layer: 10  ← 바닥 위에 표시
  
Tilemap Collider 2D: (필요시)
```

---

### 2. EnvironmentModificationSystem 설정

**Inspector 설정**:

#### References
```
NPC Agent: [NPCAgent GameObject 드래그]
Ground Tilemap: [GroundTilemap 드래그]
Object Tilemap: [ObjectTilemap 드래그]  ← 여기에 생성됨!
Grid: [Grid GameObject 드래그] (자동 감지됨)
Sprite Object Container: [GeneratedObjects 드래그] (Sprite 모드용)
```

#### Tilemap Settings
```
Use Tilemap: ✓                # Tilemap 모드 사용
Tile Size: 32                  # 생성할 타일 크기 (픽셀)
Pixels Per Unit: 32            # Unity Unit당 픽셀 (기본 32)
```

#### Generation Settings
```
Auto Remove Background: ✓      # 배경 자동 제거 (향후 구현)
Max Image Size: 512            # 메모리 절약용 최대 크기
```

---

## 🎮 사용 방법

### 방법 1: NPC 주도 환경 변경

```csharp
EnvironmentModificationSystem env = GetComponent<EnvironmentModificationSystem>();

// NPC가 상황을 평가하고 필요한 오브젝트 결정
env.RequestEnvironmentChange("여기 너무 어두워요");

// NPC AI가:
// 1. 상황 분석
// 2. 필요한 오브젝트 결정 (예: 램프)
// 3. DALL-E로 이미지 생성
// 4. ObjectTilemap에 자동 배치
```

### 방법 2: 수동 타일 생성

```csharp
// 위치 힌트로 생성
env.ManualGenerateTile("cozy lamp, pixel art, 32x32", "near");

// 정확한 위치에 생성
Vector3 position = new Vector3(5f, 3f, 0f);
env.GenerateTileAtPosition("wooden table, pixel art, top-down", position);
```

### 방법 3: UI에서 생성 (NPCDemoController 사용)

```
1. F2 키로 Environment Panel 열기
2. Context Input에 입력: "밝은 조명이 필요해"
3. "평가" 버튼 클릭
4. NPC가 자동으로 생성 및 배치
```

---

## 🔧 Tilemap vs Sprite 모드

### Tilemap 모드 (권장)
```
Use Tilemap: ✓

장점:
✅ Grid에 정확히 정렬
✅ Tilemap 에디터 도구 사용 가능
✅ 대량 타일 처리 최적화
✅ 충돌 감지 통합 (Tilemap Collider)
✅ 메모리 효율적

단점:
❌ 타일 단위로만 배치 가능
❌ 회전/스케일 제한적
```

### Sprite 모드
```
Use Tilemap: ☐

장점:
✅ 자유로운 위치 배치
✅ 개별 오브젝트 조작 가능
✅ 애니메이션 적용 가능
✅ 회전/스케일 자유

단점:
❌ 많은 오브젝트 시 성능 저하
❌ 수동 정렬 필요
❌ GameObject 오버헤드
```

---

## 📐 좌표 시스템 이해

### World Position vs Cell Position

```csharp
// World Position (Unity 월드 좌표)
Vector3 worldPos = new Vector3(5.5f, 3.2f, 0f);

// Cell Position (Tilemap 셀 좌표)
Vector3Int cellPos = objectTilemap.WorldToCell(worldPos);
// → (5, 3, 0)

// Cell 중심 월드 좌표
Vector3 centerPos = grid.GetCellCenterWorld(cellPos);
// → (5.5f, 3.5f, 0f) - 정확히 타일 중심
```

### 위치 힌트 시스템

| 힌트 | 설명 | 오프셋 |
|-----|------|-------|
| `"near"` / `"근처"` | NPC 근처 랜덤 | ±3 타일 |
| `"left"` / `"왼쪽"` | NPC 왼쪽 | (-2, 0) |
| `"right"` / `"오른쪽"` | NPC 오른쪽 | (+2, 0) |
| `"above"` / `"위"` | NPC 위쪽 | (0, +2) |
| `"below"` / `"아래"` | NPC 아래쪽 | (0, -2) |
| `"corner"` / `"코너"` | 대각선 | (+4, +4) |
| `"center"` / `"중앙"` | NPC 위치 | (0, 0) |

---

## 🎨 타일 스타일 프롬프트

### 픽셀 아트 (32x32)
```
"cozy warm lamp, pixel art, 32x32px, top-down view, isolated object, white background"
"wooden chair, pixel art style, 32x32 pixels, bird's eye view, single item, plain bg"
```

### 픽셀 아트 (64x64, 큰 오브젝트)
```
"large oak tree, pixel art, 64x64px, top-down view, detailed, transparent background"
```

### 아이소메트릭
```
"stone fountain, isometric pixel art, 32x32px, 45 degree angle, white background"
```

### 판타지 스타일
```
"magical crystal, fantasy pixel art, 32x32, glowing effect, top-down, transparent bg"
```

---

## 🐛 문제 해결

### 타일이 안 보임
```
1. ObjectTilemap의 Sorting Order 확인 (10 이상)
2. Camera의 Orthographic Size 확인
3. Tilemap Renderer가 활성화되어 있는지 확인
4. Scene View에서 Gizmos 활성화 (초록/cyan 박스로 표시됨)
```

### 타일이 그리드에 안 맞음
```
1. Grid 설정: Cell Size = (1, 1, 1)
2. Pixels Per Unit = 32 (타일 크기와 일치)
3. Grid GameObject가 연결되었는지 확인
```

### 타일이 겹침
```
// 특정 위치 타일 제거
env.RemoveTileAtPosition(worldPosition);

// 모든 생성 타일 제거
env.ClearGeneratedObjects();
```

### 성능 이슈
```
1. Max Image Size를 512로 제한 (Inspector)
2. Use Tilemap 모드 사용 (Sprite보다 빠름)
3. 생성된 타일 주기적 정리:
   if (env.GetGeneratedObjectCount() > 100) {
       env.ClearGeneratedObjects();
   }
```

---

## 🎯 고급 기능

### 1. 특정 레이어에 배치

```csharp
// EnvironmentModificationSystem.cs 수정
public Tilemap decorationTilemap;  // 장식용
public Tilemap furnitureTilemap;   // 가구용

// 배치 시 레이어 선택
PlaceAsTile(texture, decision, furnitureTilemap);
```

### 2. 타일 애니메이션

```csharp
// AnimatedTile 사용
AnimatedTile animTile = ScriptableObject.CreateInstance<AnimatedTile>();
animTile.animationSpeed = 1f;
animTile.animatedSprites = new Sprite[] { sprite1, sprite2, sprite3 };
```

### 3. 충돌 감지 활성화

```csharp
// Tile 생성 시
tile.colliderType = Tile.ColliderType.Sprite;

// Tilemap에 Collider 추가
objectTilemap.gameObject.AddComponent<TilemapCollider2D>();
```

### 4. 대량 생성

```csharp
IEnumerator GenerateMultipleTiles(List<string> prompts)
{
    foreach (string prompt in prompts)
    {
        env.ManualGenerateTile(prompt, "near");
        yield return new WaitForSeconds(2f); // API 제한 고려
    }
}
```

---

## 📊 성능 최적화

### Tilemap Renderer 최적화
```
Tilemap Renderer:
  Mode: Chunk  # Individual보다 빠름
  Detect Chunk Culling Bounds: Auto
  Chunk Culling Bounds: (적절히 설정)
```

### 메모리 최적화
```csharp
// 사용하지 않는 텍스처 정리
Destroy(sourceTexture);

// 오래된 타일 제거
if (generatedTiles.Count > maxTiles)
{
    var oldest = generatedTiles.First();
    RemoveTileAtPosition(oldest.Key);
}
```

---

## ✅ 체크리스트

- [ ] Grid > GroundTilemap, ObjectTilemap 생성
- [ ] Sorting Order 설정 (Ground=0, Object=10)
- [ ] EnvironmentSystem에 모든 참조 연결
- [ ] Use Tilemap 체크
- [ ] Pixels Per Unit = Tile Size
- [ ] NPC Agent에 OpenAI API 키 입력
- [ ] Scene View에서 Gizmos 활성화
- [ ] Play 후 타일 생성 테스트

---

## 🎉 완료!

이제 NPC가 **2D Tilemap 환경을 동적으로 변경**할 수 있습니다!

- ✅ 자동 그리드 정렬
- ✅ 레이어 분리 (바닥/오브젝트)
- ✅ 타일 추적 및 관리
- ✅ 디버그 시각화 (Gizmos)
- ✅ 메모리 최적화

**Happy Coding!** 🎮✨
