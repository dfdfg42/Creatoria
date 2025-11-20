using UnityEngine;
using UnityEngine.InputSystem;

namespace NPCSimulation.Core
{
    /// <summary>
    /// 플레이어 캐릭터 컨트롤러
    /// WASD/화살표 키로 이동, E키로 NPC와 상호작용
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private bool useRigidbody = true; // true: 물리 기반, false: Transform 기반

        [Header("Interaction")]
        [SerializeField] private float interactionRange = 2f;
        [SerializeField] private LayerMask npcLayer;
        [SerializeField] private KeyCode interactionKey = KeyCode.E;

        [Header("Sprite Animation")]
        [SerializeField] private CharacterSpriteManager spriteManager;

        private Rigidbody2D rb;
        private SpriteRenderer spriteRenderer;
        private Vector2 moveInput;
        private Vector2 lastMoveDirection = Vector2.down;
        
        // 가장 가까운 상호작용 가능한 NPC
        private NPCAgent nearestNPC;
        
        void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            
            // Rigidbody2D 설정
            if (useRigidbody)
            {
                rb.gravityScale = 0f;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }
            
            // 스프라이트 매니저 설정
            if (spriteManager == null)
            {
                spriteManager = GetComponent<CharacterSpriteManager>();
                if (spriteManager == null)
                {
                    spriteManager = gameObject.AddComponent<CharacterSpriteManager>();
                }

                if (spriteManager != null)
                {
                    // 예: NPC 이름이 "이서아"라면 "Resources/Characters/이서아"를 찾음
                    // 혹은 영어 ID가 따로 있다면 그것을 사용 (npcID 등)
                    spriteManager.LoadCharacterSprite("player");
                }
            }
            
            // 기본 스프라이트 로드 (Ada로 설정, 원하는 캐릭터로 변경 가능)
            //spriteManager.LoadCharacterSprite("Ada");
            
            Debug.Log("[PlayerController] 플레이어 초기화 완료");
        }

        void Update()
        {
            // 입력 처리
            HandleInput();
            
            // 가장 가까운 NPC 찾기
            FindNearestNPC();
            
            // 상호작용 키 입력
            if (Input.GetKeyDown(interactionKey))
            {
                Debug.Log($"[PlayerController] E키 입력 감지! nearestNPC = {(nearestNPC != null ? nearestNPC.Name : "null")}");
                
                if (nearestNPC != null)
                {
                    InteractWithNPC(nearestNPC);
                }
                else
                {
                    Debug.LogWarning("[PlayerController] 상호작용할 NPC가 없습니다.");
                }
            }
            
            // 스프라이트 애니메이션 업데이트
            UpdateSpriteAnimation();
        }

        void FixedUpdate()
        {
            if (useRigidbody)
            {
                MoveWithRigidbody();
            }
            else
            {
                MoveWithTransform();
            }
        }

        /// <summary>
        /// 키보드 입력 처리 (WASD, 화살표 키)
        /// </summary>
        private void HandleInput()
        {
            float horizontal = Input.GetAxisRaw("Horizontal"); // A/D, 좌/우
            float vertical = Input.GetAxisRaw("Vertical");     // W/S, 상/하
            
            moveInput = new Vector2(horizontal, vertical).normalized;
            
            // 이동 방향 기록 (애니메이션용)
            if (moveInput != Vector2.zero)
            {
                lastMoveDirection = moveInput;
            }
        }

        /// <summary>
        /// Rigidbody2D로 이동 (물리 기반)
        /// </summary>
        private void MoveWithRigidbody()
        {
            rb.linearVelocity = moveInput * moveSpeed;
        }

        /// <summary>
        /// Transform으로 이동 (직접 위치 변경)
        /// </summary>
        private void MoveWithTransform()
        {
            Vector3 movement = new Vector3(moveInput.x, moveInput.y, 0f);
            transform.position += movement * moveSpeed * Time.deltaTime;
        }

        /// <summary>
        /// 스프라이트 애니메이션 업데이트
        /// </summary>
        private void UpdateSpriteAnimation()
        {
            if (spriteManager == null) return;
            
            bool isMoving = moveInput.magnitude > 0.1f;
            
            if (isMoving)
            {
                // 이동 방향에 따라 스프라이트 방향 설정
                CharacterSpriteManager.Direction dir = GetDirectionFromVector(lastMoveDirection);
                spriteManager.SetDirection(dir);
                
                if (!spriteManager.IsMoving)
                {
                    spriteManager.StartMoving();
                }
            }
            else
            {
                if (spriteManager.IsMoving)
                {
                    spriteManager.StopMoving();
                }
            }
        }

        /// <summary>
        /// Vector2를 캐릭터 방향으로 변환
        /// </summary>
        private CharacterSpriteManager.Direction GetDirectionFromVector(Vector2 dir)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            
            if (angle >= -45f && angle < 45f)
                return CharacterSpriteManager.Direction.Right;
            else if (angle >= 45f && angle < 135f)
                return CharacterSpriteManager.Direction.Up;
            else if (angle >= -135f && angle < -45f)
                return CharacterSpriteManager.Direction.Down;
            else
                return CharacterSpriteManager.Direction.Left;
        }

        /// <summary>
        /// 범위 내 가장 가까운 NPC 찾기
        /// </summary>
        private void FindNearestNPC()
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, interactionRange, npcLayer);
            
            // 디버그: 감지된 Collider 수
            if (colliders.Length > 0 && nearestNPC == null)
            {
                Debug.Log($"[PlayerController] 범위 내 Collider {colliders.Length}개 발견");
            }
            
            float nearestDistance = float.MaxValue;
            NPCAgent nearest = null;
            
            foreach (var collider in colliders)
            {
                // 자기 자신은 제외
                if (collider.gameObject == gameObject)
                {
                    continue;
                }
                
                NPCAgent npc = collider.GetComponent<NPCAgent>();
                if (npc != null)
                {
                    float distance = Vector2.Distance(transform.position, collider.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearest = npc;
                    }
                }
                else
                {
                    // Player가 아닌 경우에만 경고
                    if (!collider.CompareTag("Player"))
                    {
                        Debug.LogWarning($"[PlayerController] Collider {collider.name}에 NPCAgent 컴포넌트 없음");
                    }
                }
            }
            
            // 상태 변경 시 로그
            if (nearestNPC != nearest)
            {
                if (nearest != null)
                    Debug.Log($"[PlayerController] {nearest.Name} 감지 (거리: {Vector2.Distance(transform.position, nearest.transform.position):F2}m)");
                else if (nearestNPC != null)
                    Debug.Log($"[PlayerController] {nearestNPC.Name} 범위 밖으로 이동");
            }
            
            nearestNPC = nearest;
        }

        /// <summary>
        /// NPC와 상호작용
        /// </summary>
        private void InteractWithNPC(NPCAgent npc)
        {
            Debug.Log($"[PlayerController] {npc.Name}와 대화 시작");
            
            // PlayerInteractionManager에게 알림
            PlayerInteractionManager interactionManager = FindAnyObjectByType<PlayerInteractionManager>();
            if (interactionManager != null)
            {
                interactionManager.StartConversation(npc);
            }
            else
            {
                Debug.LogWarning("[PlayerController] PlayerInteractionManager를 찾을 수 없습니다.");
            }
        }

        /// <summary>
        /// 플레이어 캐릭터 스프라이트 변경
        /// </summary>
        public void SetCharacterSprite(string characterName)
        {
            if (spriteManager != null)
            {
                spriteManager.LoadCharacterSprite(characterName);
                Debug.Log($"[PlayerController] 캐릭터 변경: {characterName}");
            }
        }

        /// <summary>
        /// 이동 속도 설정
        /// </summary>
        public void SetMoveSpeed(float speed)
        {
            moveSpeed = speed;
        }

        /// <summary>
        /// 현재 상호작용 가능한 NPC가 있는지
        /// </summary>
        public bool CanInteract => nearestNPC != null;

        /// <summary>
        /// 현재 타겟 NPC
        /// </summary>
        public NPCAgent CurrentTargetNPC => nearestNPC;

        private void OnDrawGizmosSelected()
        {
            // 상호작용 범위 시각화
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactionRange);
            
            // 가장 가까운 NPC 표시
            if (nearestNPC != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, nearestNPC.transform.position);
            }
        }
    }
}
