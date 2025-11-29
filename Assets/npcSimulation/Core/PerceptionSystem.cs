using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NPCSimulation.Core
{
    /// <summary>
    /// 지각 시스템 (논문의 Perception)
    /// - 시야 내 오브젝트/에이전트 감지
    /// - 공간 기억 생성
    /// </summary>
    public class PerceptionSystem : MonoBehaviour
    {
        [Header("Vision Settings")]
        public float visionRange = 5f;          // 시야 거리
        public float visionAngle = 120f;        // 시야 각도 (전방)
        public LayerMask perceptionLayer;       // 감지할 레이어
        
        [Header("Detection Settings")]
        public float detectionInterval = 0.5f;  // 감지 주기 (초)
        public bool use360Vision = true;        // true: 전방향, false: 전방만

        // 감지된 오브젝트들
        private List<WorldObject> detectedObjects = new List<WorldObject>();
        private List<NPCAgent> detectedAgents = new List<NPCAgent>();
        
        // 공간 기억 (위치 → 오브젝트)
        private Dictionary<Vector3, WorldObject> spatialMemory = new Dictionary<Vector3, WorldObject>();

        private NPCAgent owner;
        private float lastDetectionTime = 0f;

        private void Start()
        {
            owner = GetComponent<NPCAgent>();
        }

        private void Update()
        {
            // 주기적으로 감지
            if (Time.time - lastDetectionTime >= detectionInterval)
            {
                PerceiveEnvironment();

                lastDetectionTime = Time.time;
            }
        }

        /// <summary>
        /// 환경 지각
        /// </summary>
        public void PerceiveEnvironment()
        {
            detectedObjects.Clear();
            detectedAgents.Clear();

            // 시야 범위 내 모든 콜라이더 검색
            Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, visionRange, perceptionLayer);

            foreach (Collider2D col in hitColliders)
            {
                if (col.gameObject == gameObject) continue; // 자기 자신 제외

                Vector3 directionToTarget = (col.transform.position - transform.position).normalized;
                float distanceToTarget = Vector3.Distance(transform.position, col.transform.position);

                // 시야각 체크 (전방향이 아닌 경우)
                if (!use360Vision)
                {
                    float angleToTarget = Vector3.Angle(transform.right, directionToTarget); // 2D는 right가 앞
                    if (angleToTarget > visionAngle / 2f)
                    {
                        continue; // 시야각 밖
                    }
                }

                // WorldObject 감지 부분 수정
                WorldObject obj = col.GetComponent<WorldObject>();
                if (obj != null && obj.isVisible) // [복구된 isVisible 사용]
                {
                    detectedObjects.Add(obj);

                    Vector3 roundedPos = RoundPosition(obj.transform.position);
                    if (!spatialMemory.ContainsKey(roundedPos))
                    {
                        spatialMemory[roundedPos] = obj;
                    }

                    if (owner != null)
                    {
                        // [수정] GetAllStatesAsString() -> objectState 사용
                        string memoryDescription = $"'{obj.objectName}'을(를) {obj.transform.position}에서 발견했다. 상태: {obj.objectState}";
                        owner.MemoryMgr.AddMemory(
                            MemoryType.Event,
                            memoryDescription,
                            importance: 3,
                            owner
                        );
                    }
                }

                // 다른 NPC 감지
                NPCAgent agent = col.GetComponent<NPCAgent>();
                if (agent != null && agent != owner)
                {
                    detectedAgents.Add(agent);

                    // 메모리에 기록
                    if (owner != null)
                    {
                        string memoryDescription = $"'{agent.Name}'을(를) {agent.transform.position}에서 만났다.";
                        owner.MemoryMgr.AddMemory(
                            MemoryType.Event,
                            memoryDescription,
                            importance: 5,
                            owner
                        );
                    }
                }
            }

            if (detectedObjects.Count > 0 || detectedAgents.Count > 0)
            {
                Debug.Log($"[Perception] {owner?.Name} detected: {detectedObjects.Count} objects, {detectedAgents.Count} agents");
            }
        }

        /// <summary>
        /// 특정 오브젝트 타입 검색
        /// </summary>
        public WorldObject FindNearestObjectOfType(string type)
        {
            var objectsOfType = detectedObjects.Where(o => o.objectType.Equals(type, System.StringComparison.OrdinalIgnoreCase)).ToList();

            if (objectsOfType.Count == 0)
            {
                objectsOfType = spatialMemory.Values.Where(o => o.objectType.Equals(type, System.StringComparison.OrdinalIgnoreCase)).ToList();
            }
            if (objectsOfType.Count == 0)
                return null;

            // 가장 가까운 것 반환
            return objectsOfType.OrderBy(o => Vector3.Distance(transform.position, o.transform.position)).First();
        }

        // PerceptionSystem.cs 추가
        public NPCAgent FindNearestAgent(float range = 10f)
        {
            var allAgents = FindObjectsOfType<NPCAgent>();
            NPCAgent nearest = null;
            float minDst = range;

            foreach (var agent in allAgents)
            {
                if (agent == GetComponent<NPCAgent>()) continue; // 나 자신은 제외

                float dst = Vector3.Distance(transform.position, agent.transform.position);
                if (dst < minDst)
                {
                    minDst = dst;
                    nearest = agent;
                }
            }
            return nearest;
        }

        /// <summary>
        /// 이름으로 오브젝트 검색
        /// </summary>
        public WorldObject FindObjectByName(string name)
        {
            // 현재 감지된 것 중 검색
            WorldObject obj = detectedObjects.FirstOrDefault(o => o.objectName.ToLower().Contains(name.ToLower()));
            
            // 없으면 공간 기억에서 검색
            if (obj == null)
            {
                obj = spatialMemory.Values.FirstOrDefault(o => o.objectName.ToLower().Contains(name.ToLower()));
            }

            return obj;
        }

        /// <summary>
        /// [수정] 특정 상태 키워드를 포함한 오브젝트 검색
        /// 예: FindObjectsWithState("burning")
        /// </summary>
        public List<WorldObject> FindObjectsWithState(string stateKeyword)
        {
            return detectedObjects
                .Where(o => o.objectState.Contains(stateKeyword))
                .ToList();
        }

        /// <summary>
        /// 위치 반올림 (공간 기억용)
        /// </summary>
        private Vector3 RoundPosition(Vector3 pos)
        {
            return new Vector3(
                Mathf.Round(pos.x),
                Mathf.Round(pos.y),
                Mathf.Round(pos.z)
            );
        }

        /// <summary>
        /// 감지된 오브젝트 목록 반환
        /// </summary>
        public List<WorldObject> GetDetectedObjects() => detectedObjects;

        /// <summary>
        /// 감지된 에이전트 목록 반환
        /// </summary>
        public List<NPCAgent> GetDetectedAgents() => detectedAgents;




        /// <summary>
        /// 공간 기억 요약
        /// </summary>
        public string GetSpatialMemorySummary()
        {
            List<string> summary = new List<string>();
            foreach (var kvp in spatialMemory)
            {
                summary.Add($"- {kvp.Value.objectName} @ {kvp.Key}");
            }
            return string.Join("\n", summary);
        }

        private void OnDrawGizmos()
        {
            // 시야 범위 표시
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, visionRange);

            // 감지된 오브젝트 표시
            Gizmos.color = Color.green;
            foreach (var obj in detectedObjects)
            {
                if (obj != null)
                {
                    Gizmos.DrawLine(transform.position, obj.transform.position);
                }
            }

            // 감지된 에이전트 표시
            Gizmos.color = Color.cyan;
            foreach (var agent in detectedAgents)
            {
                if (agent != null)
                {
                    Gizmos.DrawLine(transform.position, agent.transform.position);
                }
            }
        }
    }
}
