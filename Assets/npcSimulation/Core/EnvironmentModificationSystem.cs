using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Tilemaps;
using NPCSimulation.Core;

namespace NPCSimulation.Environment
{
    /// <summary>
    /// NPC가 AI 이미지 생성을 통해 환경(타일/오브젝트)을 동적으로 생성하는 시스템 (2D 전용)
    /// </summary>
    public class EnvironmentModificationSystem : MonoBehaviour
    {
        [Header("References")]
        public NPCAgent npcAgent;
        public Tilemap groundTilemap;      // 바닥 타일맵
        public Tilemap objectTilemap;      // 오브젝트 타일맵 (위에 배치)
        public Grid grid;                   // Grid 참조 (Tilemap의 부모)
        public Transform spriteObjectContainer; // Tilemap 대신 Sprite로 배치할 경우
        
        [Header("Tilemap Settings")]
        public bool useTilemap = true;      // true: Tilemap 사용, false: Sprite 오브젝트 사용
        public bool useWorldObjectsWithGrid = true; // WorldObject + Grid 스냅 사용
        public int tileSize = 32;           // 타일 크기 (픽셀)
        public int pixelsPerUnit = 32;      // Unity Unit 당 픽셀 수
        
        [Header("Generation Settings")]
        public bool autoRemoveBackground = true;
        public int maxImageSize = 512;      // 다운로드 최대 크기 (메모리 절약)

        // 생성된 타일 관리
        private Dictionary<Vector3Int, TileBase> generatedTiles = new Dictionary<Vector3Int, TileBase>();
        private List<GameObject> generatedObjects = new List<GameObject>();

        private OpenAIClient llmClient;

        private void Start()
        {
            if (npcAgent != null && !string.IsNullOrEmpty(npcAgent.openAIKey))
            {
                llmClient = new OpenAIClient(npcAgent.openAIKey);
            }
            else
            {
                Debug.LogWarning("[EnvironmentModificationSystem] NPC Agent 또는 OpenAI API Key가 설정되지 않았습니다!");
            }

            // Grid 자동 찾기
            if (grid == null && objectTilemap != null)
            {
                grid = objectTilemap.GetComponentInParent<Grid>();
            }

            ValidateSetup();
        }

        /// <summary>
        /// 설정 검증
        /// </summary>
        private void ValidateSetup()
        {
            if (useTilemap)
            {
                if (objectTilemap == null)
                {
                    Debug.LogWarning("[EnvironmentModificationSystem] useTilemap이 true지만 objectTilemap이 설정되지 않았습니다!");
                }
                if (grid == null)
                {
                    Debug.LogWarning("[EnvironmentModificationSystem] Grid가 설정되지 않았습니다. Tilemap의 부모에서 자동으로 찾습니다.");
                }
            }
            else
            {
                if (spriteObjectContainer == null)
                {
                    Debug.LogWarning("[EnvironmentModificationSystem] Sprite 모드지만 spriteObjectContainer가 설정되지 않았습니다!");
                }
            }

            Debug.Log($"[EnvironmentModificationSystem] 초기화 완료 - 모드: {(useTilemap ? "Tilemap" : "Sprite")}, 타일 크기: {tileSize}px");
        }

        /// <summary>
        /// NPC가 환경 변경을 결정하고 실행
        /// </summary>
        public void RequestEnvironmentChange(string context)
        {
            if (npcAgent == null)
            {
                Debug.LogError("[EnvironmentModificationSystem] NPC Agent가 설정되지 않았습니다!");
                return;
            }

            Debug.Log($"[EnvironmentModificationSystem] Evaluating environment change: {context}");

            // 1. NPC에게 환경 변경 필요 여부 판단 요청
            npcAgent.EvaluateEnvironmentChange(context, (decision) =>
            {
                if (decision.isNeeded)
                {
                    Debug.Log($"[EnvironmentModificationSystem] NPC decided to add: {decision.objectName}");
                    
                    // 2. 이미지 생성 및 배치
                    StartCoroutine(GenerateAndPlaceObject(decision));
                }
                else
                {
                    Debug.Log("[EnvironmentModificationSystem] NPC decided no change is needed");
                }
            });
        }

        /// <summary>
        /// 오브젝트 생성 및 배치
        /// </summary>
        private IEnumerator GenerateAndPlaceObject(EnvironmentChangeDecision decision)
        {
            Debug.Log($"[EnvironmentModificationSystem] Generating image: {decision.imagePrompt}");

            // 1. DALL-E로 이미지 생성
            string imageUrl = null;
            yield return llmClient.GenerateImage(decision.imagePrompt, (url) =>
            {
                imageUrl = url;
            }, size: "1024x1024", quality: "standard");

            if (string.IsNullOrEmpty(imageUrl))
            {
                Debug.LogError("[EnvironmentModificationSystem] Failed to generate image");
                yield break;
            }

            Debug.Log($"[EnvironmentModificationSystem] Image generated: {imageUrl}");

            // 2. 이미지 다운로드
            Texture2D texture = null;
            yield return DownloadImage(imageUrl, (downloadedTexture) =>
            {
                texture = downloadedTexture;
            });

            if (texture == null)
            {
                Debug.LogError("[EnvironmentModificationSystem] Failed to download image");
                yield break;
            }

            // 3. 후처리 (리사이즈 및 최적화)
            Texture2D processedTexture = ProcessTexture(texture);

            if (processedTexture == null)
            {
                Debug.LogError("[EnvironmentModificationSystem] Failed to process texture");
                yield break;
            }

            // 4. 씬에 배치
            bool success = false;
            if (useTilemap)
            {
                success = PlaceAsTile(processedTexture, decision);
            }
            else
            {
                success = PlaceAsSpriteObject(processedTexture, decision);
            }

            if (success)
            {
                Debug.Log($"[EnvironmentModificationSystem] Object placed successfully: {decision.objectName}");
            }
            else
            {
                Debug.LogError($"[EnvironmentModificationSystem] Failed to place object: {decision.objectName}");
                yield break;
            }

            // 5. 메모리에 기록
            npcAgent.MemoryMgr.AddMemory(
                MemoryType.Event,
                $"나는 환경을 개선하기 위해 '{decision.objectName}'을(를) 추가했다. 이유: {decision.reason}",
                8,
                this
            );
        }

        /// <summary>
        /// 이미지 다운로드
        /// </summary>
        private IEnumerator DownloadImage(string url, Action<Texture2D> callback)
        {
            using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    Texture2D texture = DownloadHandlerTexture.GetContent(www);
                    callback?.Invoke(texture);
                }
                else
                {
                    Debug.LogError($"[EnvironmentModificationSystem] Download failed: {www.error}");
                    callback?.Invoke(null);
                }
            }
        }

        /// <summary>
        /// 텍스처 후처리 (리사이즈 + 최적화)
        /// </summary>
        private Texture2D ProcessTexture(Texture2D source)
        {
            try
            {
                // 1. 크기 제한 (메모리 절약)
                int width = Mathf.Min(source.width, maxImageSize);
                int height = Mathf.Min(source.height, maxImageSize);

                // 2. 타일 크기로 리사이즈
                Texture2D resized = ResizeTexture(source, tileSize, tileSize);

                // 3. 필터링 설정
                resized.filterMode = FilterMode.Point; // 픽셀 아트에 적합
                resized.wrapMode = TextureWrapMode.Clamp;

                return resized;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EnvironmentModificationSystem] Texture processing failed: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 텍스처 리사이즈
        /// </summary>
        private Texture2D ResizeTexture(Texture2D source, int targetWidth, int targetHeight)
        {
            RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32);
            rt.filterMode = FilterMode.Bilinear;
            
            RenderTexture.active = rt;
            Graphics.Blit(source, rt);
            
            Texture2D result = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
            result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
            result.Apply();
            
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);
            
            return result;
        }

        /// <summary>
        /// Tilemap에 타일로 배치
        /// </summary>
        private bool PlaceAsTile(Texture2D texture, EnvironmentChangeDecision decision)
        {
            if (objectTilemap == null)
            {
                Debug.LogError("[EnvironmentModificationSystem] objectTilemap이 설정되지 않았습니다!");
                return false;
            }

            // 1. Sprite 생성
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit
            );
            sprite.name = decision.objectName;

            // 2. Tile 생성
            Tile tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.name = decision.objectName;
            tile.colliderType = Tile.ColliderType.None; // 필요시 변경

            // 3. 배치 위치 결정
            Vector3 worldPosition = DetermineWorldPosition(decision.positionHint);
            Vector3Int cellPosition = objectTilemap.WorldToCell(worldPosition);

            // 4. 기존 타일 확인
            if (objectTilemap.HasTile(cellPosition))
            {
                Debug.LogWarning($"[EnvironmentModificationSystem] 위치 {cellPosition}에 이미 타일이 있습니다. 덮어씁니다.");
            }

            // 5. 타일 배치
            objectTilemap.SetTile(cellPosition, tile);
            generatedTiles[cellPosition] = tile;

            // 6. Tilemap 새로고침
            objectTilemap.RefreshTile(cellPosition);

            Debug.Log($"[EnvironmentModificationSystem] Tile placed at cell {cellPosition} (world: {worldPosition})");
            return true;
        }

        /// <summary>
        /// Sprite GameObject로 배치 (WorldObject 컴포넌트 포함, Grid 스냅)
        /// </summary>
        private bool PlaceAsSpriteObject(Texture2D texture, EnvironmentChangeDecision decision)
        {
            if (spriteObjectContainer == null)
            {
                Debug.LogError("[EnvironmentModificationSystem] spriteObjectContainer가 설정되지 않았습니다!");
                return false;
            }

            // 1. Sprite 생성
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit
            );
            sprite.name = decision.objectName;

            // 2. GameObject 생성
            GameObject obj = new GameObject($"Generated_{decision.objectName}_{generatedObjects.Count}");
            obj.transform.SetParent(spriteObjectContainer);

            // 3. 위치 설정 (Grid 스냅 적용)
            Vector3 worldPosition = DetermineWorldPosition(decision.positionHint);
            
            // Grid 스냅 적용
            if (useWorldObjectsWithGrid && grid != null)
            {
                Vector3Int cellPosition = grid.WorldToCell(worldPosition);
                worldPosition = grid.GetCellCenterWorld(cellPosition);
                Debug.Log($"[EnvironmentModificationSystem] Snapped to grid: Cell {cellPosition} -> World {worldPosition}");
            }
            
            obj.transform.position = worldPosition;

            // 4. SpriteRenderer 설정
            SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = 10; // 오브젝트가 바닥 위에 표시되도록

            // 5. WorldObject 컴포넌트 추가 (NPC가 인식 가능하도록)
            if (useWorldObjectsWithGrid)
            {
                WorldObject worldObject = obj.AddComponent<WorldObject>();
                worldObject.objectName = decision.objectName;
                worldObject.objectType = MapDecisionToObjectType(decision.objectName);
                //worldObject.isInteractable = true;
                worldObject.isVisible = true;
                worldObject.objectState = $"{decision.reason}.";

                Debug.Log($"[EnvironmentModificationSystem] WorldObject component added: {decision.objectName}");
            }
            
            // 6. Collider 추가 (선택적)
            CircleCollider2D collider = obj.AddComponent<CircleCollider2D>();
            collider.radius = 0.4f;
            collider.isTrigger = true; // NPC가 통과 가능하도록

            // 7. 관리 목록에 추가
            generatedObjects.Add(obj);

            Debug.Log($"[EnvironmentModificationSystem] Sprite object placed at {worldPosition} with WorldObject component");
            return true;
        }

        /// <summary>
        /// Decision의 objectType을 WorldObject.ObjectType으로 매핑
        /// </summary>
        private string MapDecisionToObjectType(string objectType)
        {
            string lowerType = objectType.ToLower();

            if (lowerType.Contains("lamp") || lowerType.Contains("light") || lowerType.Contains("조명") || lowerType.Contains("램프"))
                return "Light"; // 문자열 반환
            else if (lowerType.Contains("table") || lowerType.Contains("desk") || lowerType.Contains("테이블") || lowerType.Contains("책상"))
                return "Furniture";
            else if (lowerType.Contains("chair") || lowerType.Contains("의자"))
                return "Furniture";
            else if (lowerType.Contains("door") || lowerType.Contains("문"))
                return "Door";
            else if (lowerType.Contains("plant") || lowerType.Contains("flower") || lowerType.Contains("식물") || lowerType.Contains("화분"))
                return "Decoration";
            else if (lowerType.Contains("food") || lowerType.Contains("음식"))
                return "Food";
            else if (lowerType.Contains("tool") || lowerType.Contains("도구"))
                return "Tool";
            else
                return "Generic";
        }

        /// <summary>
        /// 위치 힌트를 기반으로 월드 좌표 결정
        /// </summary>
        private Vector3 DetermineWorldPosition(string hint)
        {
            Vector3 basePosition = npcAgent != null ? npcAgent.transform.position : Vector3.zero;
            hint = hint.ToLower();

            Vector3 offset = Vector3.zero;

            if (hint.Contains("corner") || hint.Contains("코너"))
            {
                // 코너에 배치 (맵 크기를 모르므로 NPC 기준 대각선)
                offset = new Vector3(4f, 4f, 0f);
            }
            else if (hint.Contains("center") || hint.Contains("중앙"))
            {
                // 중앙 (맵 중앙을 모르므로 NPC 위치)
                offset = Vector3.zero;
            }
            else if (hint.Contains("left") || hint.Contains("왼쪽"))
            {
                offset = new Vector3(-2f, 0f, 0f);
            }
            else if (hint.Contains("right") || hint.Contains("오른쪽"))
            {
                offset = new Vector3(2f, 0f, 0f);
            }
            else if (hint.Contains("above") || hint.Contains("위"))
            {
                offset = new Vector3(0f, 2f, 0f);
            }
            else if (hint.Contains("below") || hint.Contains("아래"))
            {
                offset = new Vector3(0f, -2f, 0f);
            }
            else if (hint.Contains("near") || hint.Contains("근처"))
            {
                // NPC 근처 랜덤 (그리드에 맞춤)
                float randomX = Mathf.Round(UnityEngine.Random.Range(-3f, 3f));
                float randomY = Mathf.Round(UnityEngine.Random.Range(-3f, 3f));
                offset = new Vector3(randomX, randomY, 0f);
            }
            else
            {
                // 기본값: NPC 오른쪽
                offset = new Vector3(2f, 0f, 0f);
            }

            Vector3 worldPosition = basePosition + offset;

            // 2D이므로 Z는 0으로 고정
            worldPosition.z = 0f;

            // Tilemap 사용 시 그리드에 스냅
            if (useTilemap && grid != null)
            {
                Vector3Int cellPosition = grid.WorldToCell(worldPosition);
                worldPosition = grid.GetCellCenterWorld(cellPosition);
            }

            return worldPosition;
        }

        #region Public API

        /// <summary>
        /// 수동으로 타일 생성 요청
        /// </summary>
        public void ManualGenerateTile(string prompt, string positionHint = "near")
        {
            EnvironmentChangeDecision decision = new EnvironmentChangeDecision
            {
                isNeeded = true,
                objectName = "Manual Object",
                reason = "Manual generation",
                positionHint = positionHint,
                imagePrompt = prompt
            };

            StartCoroutine(GenerateAndPlaceObject(decision));
        }

        /// <summary>
        /// 특정 위치에 타일 생성
        /// </summary>
        public void GenerateTileAtPosition(string prompt, Vector3 worldPosition)
        {
            StartCoroutine(GenerateTileAtPositionCoroutine(prompt, worldPosition));
        }

        private IEnumerator GenerateTileAtPositionCoroutine(string prompt, Vector3 targetPosition)
        {
            // 이미지 생성
            string imageUrl = null;
            yield return llmClient.GenerateImage(prompt, (url) => { imageUrl = url; });

            if (string.IsNullOrEmpty(imageUrl)) yield break;

            // 다운로드
            Texture2D texture = null;
            yield return DownloadImage(imageUrl, (tex) => { texture = tex; });

            if (texture == null) yield break;

            // 처리
            Texture2D processed = ProcessTexture(texture);
            if (processed == null) yield break;

            // Sprite 생성
            Sprite sprite = Sprite.Create(
                processed,
                new Rect(0, 0, processed.width, processed.height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit
            );

            // 배치
            if (useTilemap && objectTilemap != null)
            {
                Vector3Int cellPos = objectTilemap.WorldToCell(targetPosition);
                Tile tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprite;
                objectTilemap.SetTile(cellPos, tile);
                generatedTiles[cellPos] = tile;
            }
            else if (spriteObjectContainer != null)
            {
                GameObject obj = new GameObject("Generated_Tile");
                obj.transform.position = targetPosition;
                obj.transform.SetParent(spriteObjectContainer);
                SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = 10;
                generatedObjects.Add(obj);
            }
        }

        /// <summary>
        /// 생성된 모든 타일/오브젝트 제거
        /// </summary>
        public void ClearGeneratedObjects()
        {
            // Tilemap 타일 제거
            if (objectTilemap != null)
            {
                foreach (var kvp in generatedTiles)
                {
                    objectTilemap.SetTile(kvp.Key, null);
                }
                generatedTiles.Clear();
            }

            // Sprite 오브젝트 제거
            foreach (var obj in generatedObjects)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
            generatedObjects.Clear();

            Debug.Log("[EnvironmentModificationSystem] All generated objects cleared");
        }

        /// <summary>
        /// 특정 위치의 타일 제거
        /// </summary>
        public void RemoveTileAtPosition(Vector3 worldPosition)
        {
            if (useTilemap && objectTilemap != null)
            {
                Vector3Int cellPos = objectTilemap.WorldToCell(worldPosition);
                if (generatedTiles.ContainsKey(cellPos))
                {
                    objectTilemap.SetTile(cellPos, null);
                    generatedTiles.Remove(cellPos);
                    Debug.Log($"[EnvironmentModificationSystem] Tile removed at {cellPos}");
                }
            }
        }

        /// <summary>
        /// 생성된 타일 개수 반환
        /// </summary>
        public int GetGeneratedObjectCount()
        {
            return generatedTiles.Count + generatedObjects.Count;
        }

        #endregion

        #region Debug Helpers

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || npcAgent == null) return;

            // NPC 위치 표시
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(npcAgent.transform.position, 0.3f);

            // 생성된 타일 위치 표시
            if (useTilemap && objectTilemap != null)
            {
                Gizmos.color = Color.green;
                foreach (var kvp in generatedTiles)
                {
                    Vector3 worldPos = objectTilemap.GetCellCenterWorld(kvp.Key);
                    Gizmos.DrawWireCube(worldPos, Vector3.one * 0.5f);
                }
            }

            // Sprite 오브젝트 위치 표시
            Gizmos.color = Color.cyan;
            foreach (var obj in generatedObjects)
            {
                if (obj != null)
                {
                    Gizmos.DrawWireCube(obj.transform.position, Vector3.one * 0.5f);
                }
            }
        }

        #endregion
    }
}
