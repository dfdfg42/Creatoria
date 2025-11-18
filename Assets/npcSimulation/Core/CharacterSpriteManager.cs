using UnityEngine;
using System.Collections.Generic;

namespace NPCSimulation.Core
{
    /// <summary>
    /// 캐릭터 스프라이트 관리 시스템
    /// Cute RPG 캐릭터 스프라이트를 사용한 NPC 비주얼 관리
    /// </summary>
    public class CharacterSpriteManager : MonoBehaviour
    {
        [Header("스프라이트 시트")]
        public Sprite[] characterSprites; // 모든 캐릭터 스프라이트

        [Header("애니메이션 설정")]
        public float animationSpeed = 0.2f;
        
        private SpriteRenderer spriteRenderer;
        private int currentFrame = 0;
        private float frameTimer = 0f;
        private Direction currentDirection = Direction.Down;
        private bool isMoving = false;

        // 공개 프로퍼티
        public bool IsMoving => isMoving;

        // 캐릭터 ID별 스프라이트 시트 인덱스
        private Dictionary<string, int> characterSpriteIndex = new Dictionary<string, int>();

        // 방향별 프레임 인덱스 (RPG Maker 스타일: 4x3 그리드)
        private readonly int[] downFrames = { 0, 1, 2 };   // 아래
        private readonly int[] leftFrames = { 12, 13, 14 }; // 왼쪽
        private readonly int[] rightFrames = { 24, 25, 26 }; // 오른쪽
        private readonly int[] upFrames = { 36, 37, 38 };   // 위

        public enum Direction
        {
            Down = 0,
            Left = 1,
            Right = 2,
            Up = 3
        }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            InitializeCharacterMapping();
        }

        private void Update()
        {
            if (isMoving)
            {
                AnimateMovement();
            }
        }

        /// <summary>
        /// 캐릭터 ID와 스프라이트 시트 매핑 초기화
        /// </summary>
        private void InitializeCharacterMapping()
        {
            // 기본 캐릭터들 매핑
            characterSpriteIndex["이서아"] = 1;   // !Character_RM_001.png
            characterSpriteIndex["플레이어"] = 0;  // 플레이어용
            
            // 추가 캐릭터들은 필요에 따라 추가
            for (int i = 0; i < 53; i++)
            {
                characterSpriteIndex[$"NPC_{i}"] = i;
            }
        }

        /// <summary>
        /// 특정 캐릭터 스프라이트 시트 로드
        /// </summary>
        public void LoadCharacterSprite(string characterId)
        {
            if (!characterSpriteIndex.ContainsKey(characterId))
            {
                Debug.LogWarning($"[CharacterSpriteManager] 캐릭터 ID를 찾을 수 없음: {characterId}");
                characterId = "NPC_0"; // 기본 캐릭터 사용
            }

            int spriteIndex = characterSpriteIndex[characterId];
            
            // Resources 폴더에서 로드 (또는 직접 할당된 경우 사용)
            if (characterSprites != null && characterSprites.Length > spriteIndex)
            {
                SetIdleSprite(Direction.Down);
                Debug.Log($"[CharacterSpriteManager] 캐릭터 로드됨: {characterId} (Index: {spriteIndex})");
            }
            else
            {
                Debug.LogError($"[CharacterSpriteManager] 스프라이트를 로드할 수 없음: {characterId}");
            }
        }

        /// <summary>
        /// 방향 설정
        /// </summary>
        public void SetDirection(Direction direction)
        {
            currentDirection = direction;
            
            if (!isMoving)
            {
                SetIdleSprite(direction);
            }
        }

        /// <summary>
        /// 방향 설정 (Vector2로)
        /// </summary>
        public void SetDirection(Vector2 movementVector)
        {
            if (Mathf.Abs(movementVector.x) > Mathf.Abs(movementVector.y))
            {
                // 좌우 이동
                currentDirection = movementVector.x > 0 ? Direction.Right : Direction.Left;
            }
            else
            {
                // 상하 이동
                currentDirection = movementVector.y > 0 ? Direction.Up : Direction.Down;
            }

            if (!isMoving)
            {
                SetIdleSprite(currentDirection);
            }
        }

        /// <summary>
        /// 이동 시작
        /// </summary>
        public void StartMoving()
        {
            isMoving = true;
            currentFrame = 0;
            frameTimer = 0f;
        }

        /// <summary>
        /// 이동 정지
        /// </summary>
        public void StopMoving()
        {
            isMoving = false;
            SetIdleSprite(currentDirection);
        }

        /// <summary>
        /// 이동 애니메이션
        /// </summary>
        private void AnimateMovement()
        {
            frameTimer += Time.deltaTime;

            if (frameTimer >= animationSpeed)
            {
                frameTimer = 0f;
                currentFrame = (currentFrame + 1) % 3; // 0, 1, 2 순환

                int[] frames = GetFramesForDirection(currentDirection);
                if (characterSprites != null && frames[currentFrame] < characterSprites.Length)
                {
                    spriteRenderer.sprite = characterSprites[frames[currentFrame]];
                }
            }
        }

        /// <summary>
        /// 정지 상태 스프라이트 설정
        /// </summary>
        private void SetIdleSprite(Direction direction)
        {
            int[] frames = GetFramesForDirection(direction);
            
            if (characterSprites != null && frames[1] < characterSprites.Length)
            {
                spriteRenderer.sprite = characterSprites[frames[1]]; // 중간 프레임 (정지 포즈)
            }
        }

        /// <summary>
        /// 방향에 따른 프레임 배열 가져오기
        /// </summary>
        private int[] GetFramesForDirection(Direction direction)
        {
            switch (direction)
            {
                case Direction.Down:
                    return downFrames;
                case Direction.Left:
                    return leftFrames;
                case Direction.Right:
                    return rightFrames;
                case Direction.Up:
                    return upFrames;
                default:
                    return downFrames;
            }
        }

        /// <summary>
        /// 특정 프레임 직접 설정 (디버깅용)
        /// </summary>
        public void SetFrame(int frameIndex)
        {
            if (characterSprites != null && frameIndex < characterSprites.Length)
            {
                spriteRenderer.sprite = characterSprites[frameIndex];
            }
        }

        /// <summary>
        /// 투명도 설정
        /// </summary>
        public void SetAlpha(float alpha)
        {
            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = Mathf.Clamp01(alpha);
                spriteRenderer.color = color;
            }
        }

        /// <summary>
        /// 스프라이트 색상 변경 (상태 표시용)
        /// </summary>
        public void SetTint(Color tint)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = tint;
            }
        }
    }
}
