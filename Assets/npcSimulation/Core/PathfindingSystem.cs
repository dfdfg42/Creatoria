using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

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

        public void MoveToArea(string areaName, Action onComplete)
        {
            // 1. 씬에 있는 모든 WorldArea 중에서 이름이 일치하는 곳을 찾음
            WorldArea targetArea = FindObjectsOfType<WorldArea>()
                .FirstOrDefault(a => a.GetFullName() == areaName || a.areaName == areaName);

            if (targetArea != null)
            {
                // 2. 해당 구역의 위치로 이동 (기존에 작성된 MoveTo 사용)
                // 만약 MoveTo가 코루틴 방식이 아니라면, 여기서 직접 코루틴을 실행해야 합니다.
                // 아래는 MoveTo가 위치를 설정하고 이동을 시작하는 함수라고 가정한 코드입니다.
                MoveTo(targetArea.transform.position);

                // 3. 도착 확인을 위한 코루틴 실행
                StartCoroutine(WaitForArrival(onComplete));
            }
            else
            {
                Debug.LogWarning($"[Pathfinding] '{areaName}' 구역을 찾을 수 없습니다.");
                // 찾지 못했어도 멈추지 않게 콜백은 실행
                onComplete?.Invoke();
            }
        }

        // 도착할 때까지 대기하는 코루틴
        private IEnumerator WaitForArrival(Action onComplete)
        {
            // IsMoving이 true가 될 때까지 잠시 대기 (이동 시작 딜레이 고려)
            yield return new WaitForEndOfFrame();

            // 이동 중이라면 계속 대기
            // (주의: PathfindingSystem에 'IsMoving' 프로퍼티가 있어야 합니다)
            while (IsMoving)
            {
                yield return null;
            }

            // 도착 완료
            onComplete?.Invoke();
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
