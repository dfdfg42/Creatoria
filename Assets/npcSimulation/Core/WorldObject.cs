using UnityEngine;

namespace NPCSimulation.Core
{
    [RequireComponent(typeof(Collider2D))]
    public class WorldObject : MonoBehaviour
    {
        [Header("Object Info")]
        public string objectName = "New Object";
        public string objectType = "Generic";

        [Header("State (Generative)")]
        [TextArea(3, 10)]
        [Tooltip("오브젝트의 현재 상태를 묘사하는 자연어 문장")]
        public string objectState = "idle"; // 예: "brewing coffee", "turned off"
        [Header("Interaction")]
        public bool isInteractable = true;
        public float interactionRange = 1.5f;

        // [복구] 시스템에서 필요한 필수 변수
        public bool isVisible = true;

        private void OnValidate()
        {
            if (!string.IsNullOrEmpty(objectName) && gameObject.name != objectName)
            {
                gameObject.name = objectName;
            }
        }

        private void Start()
        {
            // 시작 시 크기 자동 조절
            RefreshVisuals();
        }

        /// <summary>
        /// AI에게 보여줄 전체 설명 가져오기
        /// </summary>
        public string GetDescription()
        {
            return $"{objectName} ({objectType}) is currently {objectState}";
        }

        /// <summary>
        /// LLM이 생성한 새로운 상태로 업데이트
        /// </summary>
        public void UpdateState(string newStateDescription)
        {
            string oldState = objectState;
            objectState = newStateDescription;
            Debug.Log($"[WorldObject] {objectName} State Updated: '{oldState}' -> '{newStateDescription}'");
        }

        /// <summary>
        /// [복구] 스프라이트 변경 시 콜라이더 크기 재설정
        /// </summary>
        public void RefreshVisuals()
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            BoxCollider2D col = GetComponent<BoxCollider2D>();

            if (sr != null && sr.sprite != null && col != null)
            {
                if (sr.drawMode == SpriteDrawMode.Simple)
                    col.size = sr.sprite.bounds.size;
                else
                    col.size = sr.size;

                col.offset = Vector2.zero;
            }
        }

        private void OnDrawGizmos()
        {
            if (!isInteractable) return;
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, interactionRange);
        }
    }
}