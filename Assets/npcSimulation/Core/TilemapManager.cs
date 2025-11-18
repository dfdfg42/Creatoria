using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

namespace NPCSimulation.Core
{
    /// <summary>
    /// Tilemap 관리 시스템
    /// Cute RPG 타일셋을 사용한 맵 관리
    /// </summary>
    public class TilemapManager : MonoBehaviour
    {
        [Header("Tilemap 레퍼런스")]
        public Tilemap groundTilemap;      // 지면 (바닥)
        public Tilemap propsTilemap;       // 오브젝트 (나무, 건물 등)
        public Tilemap waterTilemap;       // 물
        public Tilemap pathTilemap;        // 길

        [Header("Tile 에셋")]
        public TileBase[] groundTiles;
        public TileBase[] propTiles;
        public TileBase[] waterTiles;
        public TileBase[] pathTiles;

        // 위치별 타일 정보 저장
        private Dictionary<Vector3Int, LocationInfo> locationMap = new Dictionary<Vector3Int, LocationInfo>();

        private void Start()
        {
            InitializeLocationMap();
        }

        /// <summary>
        /// 위치별 정보 초기화 (타일 위치와 게임 내 장소 매핑)
        /// ⚠️ Unity Scene에 있는 WorldArea만 사용하도록 변경됨
        /// </summary>
        private void InitializeLocationMap()
        {
            // 이제 더 이상 하드코딩된 장소를 사용하지 않음
            // WorldArea 컴포넌트를 가진 GameObject들을 자동으로 감지
            Debug.Log("[TilemapManager] Scene의 WorldArea를 자동으로 감지합니다.");
        }

        /// <summary>
        /// 특정 영역을 장소로 등록
        /// </summary>
        private void AddLocationArea(Vector3Int min, Vector3Int max, string locationName)
        {
            for (int x = min.x; x <= max.x; x++)
            {
                for (int y = min.y; y <= max.y; y++)
                {
                    Vector3Int pos = new Vector3Int(x, y, 0);
                    locationMap[pos] = new LocationInfo
                    {
                        locationName = locationName,
                        isWalkable = true
                    };
                }
            }
            
            Debug.Log($"[TilemapManager] 장소 등록: {locationName} ({min} ~ {max})");
        }

        /// <summary>
        /// 월드 좌표를 타일 좌표로 변환
        /// </summary>
        public Vector3Int WorldToCell(Vector3 worldPosition)
        {
            if (groundTilemap != null)
                return groundTilemap.WorldToCell(worldPosition);
            return Vector3Int.zero;
        }

        /// <summary>
        /// 타일 좌표를 월드 좌표로 변환
        /// </summary>
        public Vector3 CellToWorld(Vector3Int cellPosition)
        {
            if (groundTilemap != null)
                return groundTilemap.GetCellCenterWorld(cellPosition);
            return Vector3.zero;
        }

        /// <summary>
        /// 특정 위치의 장소 이름 가져오기
        /// </summary>
        public string GetLocationNameAt(Vector3 worldPosition)
        {
            Vector3Int cellPos = WorldToCell(worldPosition);
            
            if (locationMap.ContainsKey(cellPos))
                return locationMap[cellPos].locationName;
            
            return "알 수 없는 장소";
        }

        /// <summary>
        /// 특정 타일이 이동 가능한지 확인
        /// </summary>
        public bool IsWalkable(Vector3Int cellPosition)
        {
            // 물 타일이 있으면 이동 불가
            if (waterTilemap != null && waterTilemap.HasTile(cellPosition))
                return false;

            // 특정 오브젝트 타일이 있으면 이동 불가 (벽, 나무 등)
            if (propsTilemap != null && propsTilemap.HasTile(cellPosition))
            {
                // TODO: 오브젝트별로 이동 가능 여부 판단
                return false;
            }

            // locationMap에 정보가 있으면 그것 사용
            if (locationMap.ContainsKey(cellPosition))
                return locationMap[cellPosition].isWalkable;

            // 기본적으로 이동 가능
            return true;
        }

        /// <summary>
        /// 장소 이름으로 대표 위치 찾기
        /// </summary>
        public Vector3 GetLocationCenter(string locationName)
        {
            Vector3 sum = Vector3.zero;
            int count = 0;

            foreach (var kvp in locationMap)
            {
                if (kvp.Value.locationName == locationName)
                {
                    sum += CellToWorld(kvp.Key);
                    count++;
                }
            }

            if (count > 0)
                return sum / count;

            Debug.LogWarning($"[TilemapManager] 장소를 찾을 수 없음: {locationName}");
            return Vector3.zero;
        }

        /// <summary>
        /// 모든 등록된 장소 이름 가져오기 (Unity Scene에 있는 WorldArea만)
        /// </summary>
        public List<string> GetAllLocationNames()
        {
            HashSet<string> locations = new HashSet<string>();
            
            // Scene의 모든 WorldArea를 찾아서 장소 목록 생성
            WorldArea[] allAreas = FindObjectsOfType<WorldArea>();
            foreach (var area in allAreas)
            {
                string fullName = area.GetFullName();
                if (!string.IsNullOrEmpty(fullName))
                {
                    locations.Add(fullName);
                }
            }

            Debug.Log($"[TilemapManager] Scene에서 {locations.Count}개의 장소를 찾았습니다: {string.Join(", ", locations)}");
            return new List<string>(locations);
        }

        /// <summary>
        /// 장소 정보 구조체
        /// </summary>
        [System.Serializable]
        private class LocationInfo
        {
            public string locationName;
            public bool isWalkable;
        }

        #region 에디터 도우미

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // 에디터에서 장소 영역 시각화
            if (locationMap == null || locationMap.Count == 0)
                return;

            Dictionary<string, Color> locationColors = new Dictionary<string, Color>
            {
                { "집:침실", Color.blue },
                { "집:부엌", Color.cyan },
                { "대학교:강의실", Color.red },
                { "대학교:중앙광장", Color.yellow },
                { "도서관:열람실", Color.green },
                { "카페:휴게실", Color.magenta }
            };

            foreach (var kvp in locationMap)
            {
                Color color = locationColors.ContainsKey(kvp.Value.locationName) 
                    ? locationColors[kvp.Value.locationName] 
                    : Color.white;
                
                color.a = 0.3f;
                Gizmos.color = color;

                Vector3 worldPos = CellToWorld(kvp.Key);
                Gizmos.DrawCube(worldPos, Vector3.one * 0.5f);
            }
        }
#endif

        #endregion
    }
}
