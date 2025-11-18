using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NPCSimulation.Core
{
    /// <summary>
    /// 논문 기반 NPC 이동 시스템
    /// - Area 단위 이동
    /// - 간단한 경로 찾기
    /// - 충돌 회피
    /// </summary>
    public class NPCMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float moveSpeed = 2f;
        public float rotationSpeed = 5f;
        public float stoppingDistance = 0.1f;
        
        [Header("Current State")]
        public bool isMoving = false;
        public WorldArea currentArea;
        public Vector3 targetPosition;
        
        private NPCAgent npcAgent;
        private CharacterSpriteManager spriteManager;
        private List<Vector3> currentPath = new List<Vector3>();
        private int currentPathIndex = 0;
        private Vector3 lastPosition;
        
        private void Start()
        {
            npcAgent = GetComponent<NPCAgent>();
            spriteManager = GetComponent<CharacterSpriteManager>();
            
            // 현재 위치에서 가장 가까운 Area 찾기
            FindCurrentArea();
            
            lastPosition = transform.position;
        }
        
        private void Update()
        {
            if (isMoving && currentPath.Count > 0)
            {
                MoveAlongPath();
            }
            
            // 스프라이트 애니메이션 업데이트
            UpdateSpriteAnimation();
        }
        
        /// <summary>
        /// 스프라이트 애니메이션 업데이트
        /// </summary>
        private void UpdateSpriteAnimation()
        {
            if (spriteManager == null) return;
            
            Vector3 movement = transform.position - lastPosition;
            
            if (movement.magnitude > 0.01f)
            {
                // 이동 중
                spriteManager.SetDirection(new Vector2(movement.x, movement.y));
                spriteManager.StartMoving();
            }
            else
            {
                // 정지
                spriteManager.StopMoving();
            }
            
            lastPosition = transform.position;
        }
        
        /// <summary>
        /// 현재 위치한 Area 찾기
        /// </summary>
        public void FindCurrentArea()
        {
            WorldArea[] allAreas = FindObjectsOfType<WorldArea>();
            
            foreach (var area in allAreas)
            {
                if (area.ContainsPosition(transform.position))
                {
                    currentArea = area;
                    Debug.Log($"[NPCMovement] {npcAgent.Name} is in {area.GetFullName()}");
                    return;
                }
            }
            
            Debug.LogWarning($"[NPCMovement] {npcAgent.Name} is not in any defined area");
        }
        
        /// <summary>
        /// Area 이름으로 이동 (논문 스타일)
        /// 유연한 매칭: 대소문자 무시, 부분 일치 지원
        /// </summary>
        public void MoveToArea(string areaFullName, System.Action onArrived = null)
        {
            Debug.Log($"[NPCMovement] 🔍 Searching for area: '{areaFullName}'");
            
            WorldArea[] allAreas = FindObjectsOfType<WorldArea>();
            WorldArea targetArea = FindAreaByName(allAreas, areaFullName);
            
            if (targetArea == null)
            {
                Debug.LogWarning($"[NPCMovement] ❌ Area not found: '{areaFullName}'");
                Debug.LogWarning($"[NPCMovement] Available areas: {string.Join(", ", System.Array.ConvertAll(allAreas, a => a.GetFullName()))}");
                onArrived?.Invoke(); // 실패해도 콜백 호출
                return;
            }
            
            Debug.Log($"[NPCMovement] ✅ Found area: '{targetArea.GetFullName()}'");
            
            // 목표 지점으로 이동
            Vector3 destination = targetArea.GetEntryPoint();
            MoveToPosition(destination, () =>
            {
                currentArea = targetArea;
                Debug.Log($"[NPCMovement] {npcAgent.Name} arrived at {targetArea.GetFullName()}");
                onArrived?.Invoke();
            });
        }
        
        /// <summary>
        /// 유연한 Area 검색
        /// 1. 정확한 FullName 매칭 (대소문자 구분)
        /// 2. 정확한 FullName 매칭 (대소문자 무시)
        /// 3. areaName 매칭 (대소문자 무시)
        /// 4. 부분 문자열 매칭 (대소문자 무시)
        /// </summary>
        private WorldArea FindAreaByName(WorldArea[] allAreas, string searchName)
        {
            if (allAreas == null || allAreas.Length == 0)
                return null;
            
            searchName = searchName.Trim();
            
            // 1단계: 정확한 FullName 매칭 (대소문자 구분)
            foreach (var area in allAreas)
            {
                if (area.GetFullName() == searchName)
                {
                    Debug.Log($"[NPCMovement] Match type: Exact FullName");
                    return area;
                }
            }
            
            // 2단계: 정확한 FullName 매칭 (대소문자 무시)
            foreach (var area in allAreas)
            {
                if (area.GetFullName().Equals(searchName, System.StringComparison.OrdinalIgnoreCase))
                {
                    Debug.Log($"[NPCMovement] Match type: Case-insensitive FullName");
                    return area;
                }
            }
            
            // 3단계: areaName만 매칭 (대소문자 무시)
            // "집:부엌" → "부엌"으로 파싱
            string[] parts = searchName.Split(':');
            string areaNameOnly = parts.Length > 1 ? parts[1] : parts[0];
            
            foreach (var area in allAreas)
            {
                if (area.areaName.Equals(areaNameOnly, System.StringComparison.OrdinalIgnoreCase))
                {
                    Debug.Log($"[NPCMovement] Match type: Case-insensitive areaName");
                    return area;
                }
            }
            
            // 4단계: 부분 문자열 매칭 (대소문자 무시)
            string lowerSearch = searchName.ToLower();
            foreach (var area in allAreas)
            {
                string lowerFullName = area.GetFullName().ToLower();
                string lowerAreaName = area.areaName.ToLower();
                
                if (lowerFullName.Contains(lowerSearch) || lowerSearch.Contains(lowerFullName))
                {
                    Debug.Log($"[NPCMovement] Match type: Partial FullName");
                    return area;
                }
                
                if (lowerAreaName.Contains(lowerSearch) || lowerSearch.Contains(lowerAreaName))
                {
                    Debug.Log($"[NPCMovement] Match type: Partial areaName");
                    return area;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// 특정 위치로 이동
        /// </summary>
        public void MoveToPosition(Vector3 position, System.Action onArrived = null)
        {
            targetPosition = position;
            
            // 간단한 직선 경로 생성 (나중에 A*로 업그레이드 가능)
            currentPath = new List<Vector3> { position };
            currentPathIndex = 0;
            isMoving = true;
            
            StartCoroutine(MoveCoroutine(onArrived));
        }
        
        /// <summary>
        /// 특정 오브젝트로 이동
        /// </summary>
        public void MoveToObject(WorldObject targetObject, System.Action onArrived = null)
        {
            if (targetObject == null)
            {
                Debug.LogWarning("[NPCMovement] Target object is null");
                onArrived?.Invoke();
                return;
            }
            
            Vector3 targetPos = targetObject.transform.position;
            
            // 상호작용 범위만큼 떨어진 위치로 이동
            Vector3 direction = (transform.position - targetPos).normalized;
            Vector3 stopPosition = targetPos + direction * (targetObject.interactionRange * 0.8f);
            
            MoveToPosition(stopPosition, onArrived);
        }
        
        /// <summary>
        /// 경로를 따라 이동
        /// </summary>
        private void MoveAlongPath()
        {
            if (currentPathIndex >= currentPath.Count)
            {
                isMoving = false;
                return;
            }
            
            Vector3 targetPoint = currentPath[currentPathIndex];
            Vector3 direction = (targetPoint - transform.position).normalized;
            
            // 이동
            transform.position += direction * moveSpeed * Time.deltaTime;
            
            // 2D에서는 Z축 회전만
            if (direction != Vector3.zero)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                Quaternion targetRotation = Quaternion.Euler(0, 0, angle - 90f);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
            
            // 목표 지점 도달 확인
            float distance = Vector3.Distance(transform.position, targetPoint);
            if (distance < stoppingDistance)
            {
                currentPathIndex++;
            }
        }
        
        /// <summary>
        /// 이동 코루틴
        /// </summary>
        private IEnumerator MoveCoroutine(System.Action onArrived)
        {
            while (isMoving && currentPathIndex < currentPath.Count)
            {
                yield return null;
            }
            
            isMoving = false;
            onArrived?.Invoke();
        }
        
        /// <summary>
        /// 이동 중단
        /// </summary>
        public void StopMoving()
        {
            isMoving = false;
            currentPath.Clear();
            currentPathIndex = 0;
            
            if (spriteManager != null)
            {
                spriteManager.StopMoving();
            }
        }
        
        /// <summary>
        /// 현재 Area의 특정 오브젝트로 이동
        /// </summary>
        public void MoveToObjectInCurrentArea(string objectName, System.Action onArrived = null)
        {
            if (currentArea == null)
            {
                Debug.LogWarning("[NPCMovement] Not in any area");
                onArrived?.Invoke();
                return;
            }
            
            WorldObject targetObject = currentArea.FindObjectByName(objectName);
            if (targetObject != null)
            {
                MoveToObject(targetObject, onArrived);
            }
            else
            {
                Debug.LogWarning($"[NPCMovement] Object '{objectName}' not found in {currentArea.GetFullName()}");
                onArrived?.Invoke();
            }
        }
        
        private void OnDrawGizmos()
        {
            // 현재 경로 시각화
            if (currentPath != null && currentPath.Count > 0)
            {
                Gizmos.color = Color.yellow;
                
                for (int i = 0; i < currentPath.Count - 1; i++)
                {
                    Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
                }
                
                // 목표 지점
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(currentPath[currentPath.Count - 1], 0.3f);
            }
            
            // 현재 위치
            if (isMoving)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position, 0.2f);
            }
        }
    }
}
