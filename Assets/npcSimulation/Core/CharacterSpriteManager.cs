using UnityEngine;

namespace NPCSimulation.Core
{
    /// <summary>
    /// 가장 단순한 형태의 스프라이트 애니메이터
    /// 인스펙터에 할당된 스프라이트를 사용합니다.
    /// </summary>
    public class CharacterSpriteManager : MonoBehaviour
    {
        [Header("★ 여기에 스프라이트 12개를 순서대로 넣으세요")]
        // Unity 에디터에서 이 배열에 이미지를 드래그해서 넣으세요!
        // 순서: 하(0-2), 좌(3-5), 우(6-8), 상(9-11)
        public Sprite[] characterSprites;

        [Header("설정")]
        public float animationSpeed = 0.2f;

        private SpriteRenderer spriteRenderer;
        private int currentFrame = 0;
        private float frameTimer = 0f;
        private Direction currentDirection = Direction.Down;
        private bool isMoving = false;

        public bool IsMoving => isMoving;

        // 3x4 (12장) 표준 인덱스
        private readonly int[] downFrames = { 0, 1, 2 };
        private readonly int[] leftFrames = { 3, 4, 5 };
        private readonly int[] rightFrames = { 6, 7, 8 };
        private readonly int[] upFrames = { 9, 10, 11 };

        public enum Direction { Down, Left, Right, Up }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        private void Start()
        {
            // 시작할 때 정면(Down) 정지(1번) 모습 보여주기
            SetDirection(Direction.Down);


        }

        private void Update()
        {
            if (isMoving)
            {
                AnimateMovement();
            }
            else
            {
                // 움직이지 않을 때도 방향 유지
                SetIdleSprite(currentDirection);
            }
        }

        // --- 애니메이션 로직 ---

        public void SetDirection(Direction direction)
        {
            currentDirection = direction;
            if (!isMoving) SetIdleSprite(direction);
        }

        public void SetDirection(Vector2 movementVector)
        {
            if (movementVector == Vector2.zero) return;

            if (Mathf.Abs(movementVector.x) > Mathf.Abs(movementVector.y))
                currentDirection = movementVector.x > 0 ? Direction.Right : Direction.Left;
            else
                currentDirection = movementVector.y > 0 ? Direction.Up : Direction.Down;

            if (!isMoving) SetIdleSprite(currentDirection);
        }

        public void StartMoving()
        {
            if (!isMoving)
            {
                isMoving = true;
                currentFrame = 0;
                frameTimer = 0f;
            }
        }

        public void StopMoving()
        {
            isMoving = false;
            SetIdleSprite(currentDirection);
        }

        private void AnimateMovement()
        {
            frameTimer += Time.deltaTime;
            if (frameTimer >= animationSpeed)
            {
                frameTimer = 0f;
                currentFrame = (currentFrame + 1) % 3;
                UpdateSprite();
            }
        }

        private void SetIdleSprite(Direction direction)
        {
            currentFrame = 1; // 걷기 동작 중 가운데(정지) 프레임
            UpdateSprite();
        }

        private void UpdateSprite()
        {
            if (characterSprites == null || characterSprites.Length == 0) return;

            int[] frames = GetFramesForDirection(currentDirection);
            if (currentFrame < frames.Length)
            {
                int spriteIndex = frames[currentFrame];
                if (spriteIndex < characterSprites.Length)
                {
                    spriteRenderer.sprite = characterSprites[spriteIndex];
                }
            }
        }

        private int[] GetFramesForDirection(Direction direction)
        {
            switch (direction)
            {
                case Direction.Down: return downFrames;
                case Direction.Left: return leftFrames;
                case Direction.Right: return rightFrames;
                case Direction.Up: return upFrames;
                default: return downFrames;
            }
        }

        // ★ 중요: 다른 스크립트(NPCAgent, PlayerController)에서 
        // 이 함수를 호출해도 에러가 안 나도록 빈 껍데기만 남겨둠
        public void LoadCharacterSprite(string name)
        {
            // 아무것도 안 함 (인스펙터 설정 우선)
        }
    }
}