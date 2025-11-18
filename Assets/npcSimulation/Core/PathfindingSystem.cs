using System.Collections.Generic;
using UnityEngine;

namespace NPCSimulation.Core
{
    /// <summary>
    /// 간단한 길찾기 시스템
    /// - 직선 이동만 지원 (타일맵 사용 안 함)
    /// - WorldArea 기반
    /// </summary>
    public class PathfindingSystem : MonoBehaviour
    {
        [Header("Settings")]
        public float moveSpeed = 3f;

        // 현재 경로
        private List<Vector3> currentPath = new List<Vector3>();
        private int currentPathIndex = 0;
        private bool isMoving = false;

        private NPCAgent owner;

        private void Start()
        {
            owner = GetComponent<NPCAgent>();
        }

        private void Update()
        {
            if (isMoving && currentPath.Count > 0)
            {
                FollowPath();
            }
        }

        /// <summary>
        /// 목표 위치로 이동 (간단한 직선 경로)
        /// </summary>
        public bool MoveTo(Vector3 targetWorldPosition)
        {
            // 간단한 직선 경로 생성
            currentPath = new List<Vector3> { targetWorldPosition };
            currentPathIndex = 0;
            isMoving = true;

            Debug.Log($"[Pathfinding] {owner?.Name} moving to {targetWorldPosition}");
            return true;
        }

        /// <summary>
        /// WorldObject로 이동
        /// </summary>
        public bool MoveToObject(WorldObject target)
        {
            if (target == null)
            {
                Debug.LogWarning("[Pathfinding] Target object is null");
                return false;
            }

            return MoveTo(target.transform.position);
        }

        /// <summary>
        /// 경로 따라가기
        /// </summary>
        private void FollowPath()
        {
            if (currentPathIndex >= currentPath.Count)
            {
                // 목적지 도착
                isMoving = false;
                OnDestinationReached();
                return;
            }

            Vector3 targetPosition = currentPath[currentPathIndex];
            
            // 이동
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );

            // 목표 지점 도달 확인
            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                currentPathIndex++;
            }
        }

        /// <summary>
        /// 목적지 도착 이벤트
        /// </summary>
        private void OnDestinationReached()
        {
            Debug.Log($"[Pathfinding] {owner?.Name} reached destination");

            if (owner != null && currentPath.Count > 0)
            {
                owner.MemoryMgr.AddMemory(
                    MemoryType.Event,
                    $"목적지에 도착했다.",
                    importance: 4,
                    owner
                );
            }

            currentPath.Clear();
        }

        /// <summary>
        /// 이동 중지
        /// </summary>
        public void StopMoving()
        {
            isMoving = false;
            currentPath.Clear();
            currentPathIndex = 0;
        }

        /// <summary>
        /// 현재 이동 중인지
        /// </summary>
        public bool IsMoving => isMoving;

        /// <summary>
        /// 현재 경로
        /// </summary>
        public List<Vector3> CurrentPath => currentPath;

        // Gizmo로 경로 시각화
        private void OnDrawGizmos()
        {
            if (currentPath == null || currentPath.Count == 0)
                return;

            // 경로 선
            Gizmos.color = Color.blue;
            Vector3 start = transform.position;
            foreach (var point in currentPath)
            {
                Gizmos.DrawLine(start, point);
                start = point;
            }

            // 목표 지점
            if (currentPath.Count > 0)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(currentPath[currentPath.Count - 1], 0.3f);
            }
        }
    }
}
