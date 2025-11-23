using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NPCSimulation.Core
{
    /// <summary>
    /// 논문의 Area 개념 - 게임 세계의 구역
    /// 예: "침실", "거실", "주방" 등
    /// </summary>
    public class WorldArea : MonoBehaviour
    {
        [Header("Area Info")]
        public string areaName = "Unknown Area";
        public string sectorName = ""; // The Ville의 Sector 이름
        [TextArea(2, 4)]
        public string description = "";
        
        [Header("Parent Area")]
        public WorldArea parentArea; // 예: "거실"의 부모는 "집"
        
        [Header("Area Bounds")]
        public Vector2 areaSize = new Vector2(5f, 5f);
        
        [Header("Objects in Area")]
        public List<WorldObject> objectsInArea = new List<WorldObject>();
        
        // 진입 지점 (NPC가 이 구역으로 들어올 때 목표 지점)
        private Transform entryPoint;
        
        private void Start()
        {
            // 자동으로 구역 내 오브젝트 탐지
            RefreshObjectsInArea();
            Debug.Log(
                $"[WorldArea] {GetFullName()} has {objectsInArea.Count} objects: " +
                string.Join(", ", objectsInArea.Select(o => o?.objectName))
            );
        }
        
        /// <summary>
        /// 구역 내 오브젝트 목록 갱신
        /// </summary>
        public void RefreshObjectsInArea()
        {
            objectsInArea.Clear();
            
            // 구역 범위 내의 모든 WorldObject 찾기
            WorldObject[] allObjects = FindObjectsOfType<WorldObject>();
            foreach (var obj in allObjects)
            {
                if (ContainsPosition(obj.transform.position))
                {
                    objectsInArea.Add(obj);
                }
            }
            
            Debug.Log($"[WorldArea] {areaName}: Found {objectsInArea.Count} objects");
        }
        
        /// <summary>
        /// 위치가 이 구역 안에 있는지 확인
        /// </summary>
        public bool ContainsPosition(Vector3 position)
        {
            Vector3 localPos = transform.InverseTransformPoint(position);
            
            return Mathf.Abs(localPos.x) <= areaSize.x / 2f &&
                   Mathf.Abs(localPos.y) <= areaSize.y / 2f;
        }
        
        /// <summary>
        /// 진입 지점 가져오기 (없으면 중심)
        /// </summary>
        public Vector3 GetEntryPoint()
        {
            if (entryPoint != null)
                return entryPoint.position;
            
            return transform.position;
        }
        
        /// <summary>
        /// 구역의 전체 이름 (계층 포함)
        /// 예: "집:거실" 또는 "artist's co-living space:Latoya Williams's room"
        /// </summary>
        public string GetFullName()
        {
            // The Ville 형식: sector:arena
            if (!string.IsNullOrEmpty(sectorName))
            {
                return $"{sectorName}:{areaName}";
            }
            
            // 기존 parentArea 형식
            if (parentArea != null)
            {
                return $"{parentArea.areaName}:{areaName}";
            }
            
            // sectorName도 parentArea도 없으면 areaName만 반환
            return areaName;
        }

        /// <summary>
        /// 특정 오브젝트 타입 찾기
        /// </summary>
        public WorldObject FindObjectOfType(string type)
        {
            foreach (var obj in objectsInArea)
            {
                // 문자열 포함 여부로 체크하면 더 유연함 (예: "Light" 검색 시 "Desk Light"도 찾음)
                if (obj.objectType.Contains(type) || type.Contains(obj.objectType))
                    return obj;
            }
            return null;
        }

        /// <summary>
        /// 이름으로 오브젝트 찾기
        /// </summary>
        public WorldObject FindObjectByName(string objectName)
        {
            foreach (var obj in objectsInArea)
            {
                if (obj.objectName.Contains(objectName))
                    return obj;
            }
            return null;
        }
        
        /// <summary>
        /// 구역 설명 (NPC가 인식하는 정보)
        /// </summary>
        public string GetAreaDescription()
        {
            string desc = $"{GetFullName()}";
            
            if (!string.IsNullOrEmpty(description))
            {
                desc += $": {description}";
            }
            
            if (objectsInArea.Count > 0)
            {
                desc += $"\n오브젝트: {string.Join(", ", objectsInArea.ConvertAll(o => o.objectName))}";
            }
            
            return desc;
        }
        
        private void OnDrawGizmos()
        {
            // 구역 범위 시각화
            Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(areaSize.x, areaSize.y, 0.1f));
            
            // 이름 표시
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position, areaName);
            #endif
        }
    }
}
