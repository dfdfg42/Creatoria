using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NPCSimulation.Core
{
    /// <summary>
    /// 논문의 Area 개념 - 게임 세계의 구역
    /// 이제 BoxCollider2D를 기준으로 영역을 판단합니다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))] // 콜라이더 필수
    public class WorldArea : MonoBehaviour
    {
        [Header("Area Info")]
        public string areaName = "Unknown Area";
        public string sectorName = "";
        [TextArea(2, 4)]
        public string description = "";

        [Header("Parent Area")]
        public WorldArea parentArea;

        // [삭제됨] public Vector2 areaSize; -> 이제 콜라이더가 대신함

        [Header("Objects in Area")]
        public List<WorldObject> objectsInArea = new List<WorldObject>();

        // 진입 지점
        private Transform entryPoint;
        private Collider2D areaCollider; // 캐싱용

        private void Awake()
        {
            areaCollider = GetComponent<Collider2D>();
            // 트리거로 설정 (물리 충돌 방지하고 영역 감지만 함)
            if (areaCollider != null) areaCollider.isTrigger = true;
        }

        private void Start()
        {
            RefreshObjectsInArea();
            Debug.Log($"[WorldArea] {GetFullName()} initialized with {objectsInArea.Count} objects.");
        }

        /// <summary>
        /// 구역 내 오브젝트 목록 갱신
        /// </summary>
        public void RefreshObjectsInArea()
        {
            objectsInArea.Clear();

            // 씬의 모든 WorldObject를 가져와서 내 구역 안에 있는지 확인
            WorldObject[] allObjects = FindObjectsOfType<WorldObject>();
            foreach (var obj in allObjects)
            {
                // 내 콜라이더 안에 해당 오브젝트의 위치가 포함되는지 확인
                if (ContainsPosition(obj.transform.position))
                {
                    objectsInArea.Add(obj);
                }
            }

            Debug.Log($"[WorldArea] {areaName}: Found {objectsInArea.Count} objects");
        }

        /// <summary>
        /// 위치가 이 구역 안에 있는지 확인 (Physics2D 사용)
        /// </summary>
        public bool ContainsPosition(Vector3 position)
        {
            if (areaCollider == null) areaCollider = GetComponent<Collider2D>();

            // Unity 내장 함수: 점이 콜라이더 내부에 있는지 판별 (회전도 고려됨)
            return areaCollider.OverlapPoint(position);
        }

        public Vector3 GetEntryPoint()
        {
            if (entryPoint != null) return entryPoint.position;
            // 콜라이더의 중심(Bounds Center) 반환
            return areaCollider != null ? areaCollider.bounds.center : transform.position;
        }

        public string GetFullName()
        {
            if (!string.IsNullOrEmpty(sectorName)) return $"{sectorName}:{areaName}";
            if (parentArea != null) return $"{parentArea.areaName}:{areaName}";
            return areaName;
        }

        public WorldObject FindObjectOfType(string type)
        {
            foreach (var obj in objectsInArea)
            {
                if (obj.objectType.Contains(type) || type.Contains(obj.objectType))
                    return obj;
            }
            return null;
        }

        public WorldObject FindObjectByName(string objectName)
        {
            foreach (var obj in objectsInArea)
            {
                if (obj.objectName.Contains(objectName))
                    return obj;
            }
            return null;
        }

        public string GetAreaDescription()
        {
            string desc = $"{GetFullName()}";
            if (!string.IsNullOrEmpty(description)) desc += $": {description}";
            if (objectsInArea.Count > 0) desc += $"\n오브젝트: {string.Join(", ", objectsInArea.ConvertAll(o => o.objectName))}";
            return desc;
        }

        // OnDrawGizmos 수정: 콜라이더는 유니티가 알아서 그려주므로 이름만 표시
        private void OnDrawGizmos()
        {
#if UNITY_EDITOR
            // 씬 뷰에서 이름 잘 보이게 표시
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.cyan;
            style.fontSize = 15;
            style.alignment = TextAnchor.MiddleCenter;

            UnityEditor.Handles.Label(transform.position, areaName, style);
#endif
        }
    }
}
