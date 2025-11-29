using UnityEngine;

namespace NPCSimulation.Core
{
    /// <summary>
    /// 플레이어 캐릭터 컨트롤러
    /// 역할: 이동(WASD) 및 스프라이트 애니메이션 제어만 담당
    /// (상호작용은 PlayerInteractionController가 담당함)
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private bool useRigidbody = true; // true: 물리 기반, false: Transform 기반

        [Header("Sprite Animation")]
        [SerializeField] private CharacterSpriteManager spriteManager;

        private Rigidbody2D rb;
        private SpriteRenderer spriteRenderer;
        private Vector2 moveInput;
        private Vector2 lastMoveDirection = Vector2.down;

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
                    // 기본 캐릭터 스프라이트 로드 (필요시 변경)
                    spriteManager.LoadCharacterSprite("Player");
                }
            }

            Debug.Log("[PlayerController] 플레이어 이동 시스템 초기화 완료");
        }

        void Update()
        {
            // 입력 처리
            HandleInput();

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
            // UI 입력 중(채팅 등)일 때는 이동 막기 (선택사항 - 필요시 주석 해제)
            // if (Time.timeScale == 0) { moveInput = Vector2.zero; return; }

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
            // Unity 6 (2023.3+) 부터 velocity -> linearVelocity로 변경됨
            // 구버전 Unity라면 rb.velocity = moveInput * moveSpeed; 사용
            rb.velocity = moveInput * moveSpeed;
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
    }
}
