using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking; // [필수] 이미지 다운로드용
using System.Collections;       // [필수] 코루틴용
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using NPCSimulation.Core;

public class WorldEditorManager : MonoBehaviour
{
    public static WorldEditorManager Instance { get; private set; }

    [Header("OpenAI Settings")]
    [Tooltip("이미지 생성을 위한 API Key를 여기에 입력하세요")]
    public string openAIKey = ""; // [중요] 인스펙터에서 키를 입력해야 합니다!

    [Header("UI Panels")]
    public GameObject worldEditorPanel;
    public GameObject areaEditorPanel;
    public GameObject objectEditorPanel;

    [Header("Mode Buttons")]
    public Button toggleAreaEditButton;
    public Button toggleObjectEditButton;
    public Button toggleDefaultModeButton;

    [Header("Area UI (TMP)")]
    public TMP_InputField areaNameInputField;
    public TMP_InputField areaSectorInputField;
    public Button createAreaButton;
    public Button deleteAreaButton;
    public Transform areaListContent;
    public GameObject areaListItemPrefab;

    [Header("Object UI (TMP)")]
    public TMP_InputField objectNameInputField;
    public TMP_InputField objectTypeInputField; // [추가] 타입 입력 필드
    public TMP_InputField objectStateInputField;
    public Image objectSpritePreview;
    public TMP_Dropdown objectSpriteDropdown;
    public TMP_InputField imagePromptInputField;
    public Button generateImageButton;
    public Button createObjectButton;
    public Button deleteObjectButton;
    public Transform objectListContent;
    public GameObject objectListItemPrefab;


    [Header("Resources & Prefabs")]
    public string spritesFolderPath = "Sprites/WorldObjects";
    public GameObject resizeHandlePrefab;
    public LayerMask clickableLayer;

    [Header("Settings")]
    public Color selectedColor = Color.yellow;
    public Color normalColor = Color.white;
    public bool showModeOutlines = true;

    [Header("Grid Settings")]
    public bool useGridSnap = true;      // 그리드 스냅 사용 여부
    public float gridSize = 1.0f;        // 그리드 한 칸의 크기 (보통 1 또는 0.5)
    public bool snapSizeToGrid = true;   // 크기 조절도 그리드에 맞출지 여부

    // --- 상태 변수 ---
    public enum EditorMode { Default, AreaEdit, ObjectEdit }
    public enum ManipulationMode { None, Move, Resize_TopLeft, Resize_TopRight, Resize_BottomLeft, Resize_BottomRight }

    private EditorMode currentMode = EditorMode.Default;
    private ManipulationMode currentManipulation = ManipulationMode.None;

    private WorldArea selectedArea;
    private WorldObject selectedObject;

    private List<Sprite> availableSprites = new List<Sprite>();
    private Sprite selectedNewSprite = null;

    // 드래그/리사이징 관련
    private Vector3 startMousePos;
    private Vector3 startObjectPos;
    private Bounds startBounds;
    private List<GameObject> activeHandles = new List<GameObject>();

    // 캐싱용 리스트
    private List<WorldArea> cachedAreas = new List<WorldArea>();
    private List<WorldObject> cachedObjects = new List<WorldObject>();

    // OpenAI 클라이언트 (이미지 생성용)
    private OpenAIClient editorClient;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void Start()
    {
        LoadAvailableSprites();
        InitUI();
        SetEditorMode(EditorMode.Default);
    }

    private void Update()
    {
        DrawEditorModeOutlines();

        if (currentMode == EditorMode.Default) return;

        HandleInput();
    }

    // --- 초기화 및 UI 설정 ---
    private void LoadAvailableSprites()
    {
        availableSprites = Resources.LoadAll<Sprite>(spritesFolderPath).ToList();
        Debug.Log($"[WorldEditor] '{spritesFolderPath}'에서 스프라이트 {availableSprites.Count}개를 로드했습니다.");

        objectSpriteDropdown.ClearOptions();
        List<string> options = new List<string> { "None" };
        options.AddRange(availableSprites.Select(s => s.name));
        objectSpriteDropdown.AddOptions(options);

        objectSpriteDropdown.onValueChanged.AddListener(OnSpriteDropdownChanged);
    }

    private void InitUI()
    {
        // 모드 버튼
        toggleAreaEditButton.onClick.AddListener(() => SetEditorMode(EditorMode.AreaEdit));
        toggleObjectEditButton.onClick.AddListener(() => SetEditorMode(EditorMode.ObjectEdit));
        toggleDefaultModeButton.onClick.AddListener(() => SetEditorMode(EditorMode.Default));

        // 생성/삭제 버튼
        createAreaButton.onClick.AddListener(OnCreateAreaClicked);
        deleteAreaButton.onClick.AddListener(OnDeleteAreaClicked);
        createObjectButton.onClick.AddListener(OnCreateObjectClicked);
        deleteObjectButton.onClick.AddListener(OnDeleteObjectClicked);
        generateImageButton.onClick.AddListener(OnGenerateImageClicked);

        // [추가] 입력 필드 실시간 반영 이벤트
        if (objectStateInputField != null)
        {
            objectStateInputField.onEndEdit.AddListener((val) => {
                if (selectedObject != null) selectedObject.UpdateState(val); // 상태 업데이트
            });
        }
        if (objectNameInputField != null)
            objectNameInputField.onEndEdit.AddListener((val) => { if (selectedObject != null) selectedObject.objectName = val; });

        if (objectTypeInputField != null)
            objectTypeInputField.onEndEdit.AddListener((val) => { if (selectedObject != null) selectedObject.objectType = val; });

        if (areaNameInputField != null)
            areaNameInputField.onEndEdit.AddListener((val) => { if (selectedArea != null) selectedArea.areaName = val; });

        if (areaSectorInputField != null)
            areaSectorInputField.onEndEdit.AddListener((val) => { if (selectedArea != null) selectedArea.sectorName = val; });

        RefreshLists();
    }

    // --- 모드 관리 ---
    public void SetEditorMode(EditorMode mode)
    {
        Debug.Log($"[WorldEditor] 모드 변경: {currentMode} -> {mode}");
        currentMode = mode;
        DeselectAll();

        if (worldEditorPanel != null) worldEditorPanel.SetActive(true);
        if (areaEditorPanel != null) areaEditorPanel.SetActive(mode == EditorMode.AreaEdit);
        if (objectEditorPanel != null) objectEditorPanel.SetActive(mode == EditorMode.ObjectEdit);

        var player = FindAnyObjectByType<PlayerController>();
        if (player != null) player.enabled = (mode == EditorMode.Default);

        RefreshLists();
    }

    private void RefreshLists()
    {
        cachedAreas.Clear();
        cachedObjects.Clear();

        // Area 목록 갱신
        foreach (Transform t in areaListContent) Destroy(t.gameObject);
        var areas = FindObjectsOfType<WorldArea>();
        cachedAreas.AddRange(areas);

        foreach (var area in areas)
        {
            var item = Instantiate(areaListItemPrefab, areaListContent);
            var tmpText = item.GetComponentInChildren<TextMeshProUGUI>();
            if (tmpText != null) tmpText.text = area.areaName;
            else item.GetComponentInChildren<Text>().text = area.areaName;
            item.GetComponent<Button>().onClick.AddListener(() => SelectTarget(area.GetComponent<Collider2D>()));
        }

        // Object 목록 갱신
        foreach (Transform t in objectListContent) Destroy(t.gameObject);
        var objects = FindObjectsOfType<WorldObject>();
        cachedObjects.AddRange(objects);

        foreach (var obj in objects)
        {
            var item = Instantiate(objectListItemPrefab, objectListContent);
            var tmpText = item.GetComponentInChildren<TextMeshProUGUI>();
            if (tmpText != null) tmpText.text = obj.objectName;
            else item.GetComponentInChildren<Text>().text = obj.objectName;
            item.GetComponent<Button>().onClick.AddListener(() => SelectTarget(obj.GetComponent<Collider2D>()));
        }
    }

    // --- 시각적 피드백 ---
    private void DrawEditorModeOutlines()
    {
        if (!showModeOutlines) return;

        if (currentMode == EditorMode.AreaEdit)
        {
            foreach (var area in cachedAreas)
                if (area != null) DrawBounds(area.GetComponent<BoxCollider2D>().bounds, Color.green);
        }
        else if (currentMode == EditorMode.ObjectEdit)
        {
            foreach (var obj in cachedObjects)
                if (obj != null)
                {
                    var col = obj.GetComponent<Collider2D>();
                    if (col != null) DrawBounds(col.bounds, Color.cyan);
                }
        }
    }

    private void DrawBounds(Bounds b, Color c)
    {
        Vector3 p1 = new Vector3(b.min.x, b.min.y, 0);
        Vector3 p2 = new Vector3(b.max.x, b.min.y, 0);
        Vector3 p3 = new Vector3(b.max.x, b.max.y, 0);
        Vector3 p4 = new Vector3(b.min.x, b.max.y, 0);
        Debug.DrawLine(p1, p2, c); Debug.DrawLine(p2, p3, c); Debug.DrawLine(p3, p4, c); Debug.DrawLine(p4, p1, c);
    }

    // --- 입력 처리 ---
    private void HandleInput()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;

        Vector3 mousePos = GetMouseWorldPos();

        if (Input.GetMouseButtonDown(0))
        {
            startMousePos = mousePos;

            // 1. 핸들 클릭
            GameObject hitHandle = CheckHandleClick(mousePos);
            if (hitHandle != null)
            {
                var handleScript = hitHandle.GetComponent<EditorResizeHandle>();
                if (handleScript != null)
                {
                    currentManipulation = handleScript.handleType;
                    if (selectedArea != null) startBounds = selectedArea.GetComponent<BoxCollider2D>().bounds;
                    else if (selectedObject != null) startBounds = selectedObject.GetComponent<Collider2D>().bounds;
                    return;
                }
            }

            // 2. 객체 클릭
            Collider2D hitCol = Physics2D.OverlapPoint(mousePos, clickableLayer);
            if (hitCol != null)
            {
                if (createAreaButton.interactable && createObjectButton.interactable)
                {
                    SelectTarget(hitCol);
                    if (selectedArea != null || selectedObject != null)
                    {
                        currentManipulation = ManipulationMode.Move;
                        startObjectPos = (selectedArea != null) ? selectedArea.transform.position : selectedObject.transform.position;
                    }
                    return;
                }
            }
            else
            {
                if (createAreaButton.interactable && createObjectButton.interactable) DeselectAll();
            }
        }

        if (Input.GetMouseButton(0))
        {
            if (currentManipulation == ManipulationMode.Move) MoveTarget(mousePos);
            else if (currentManipulation != ManipulationMode.None) ResizeTarget(mousePos);
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (currentMode == EditorMode.AreaEdit && !createAreaButton.interactable) FinishCreatingArea(startMousePos, mousePos);
            else if (currentMode == EditorMode.ObjectEdit && !createObjectButton.interactable) FinishCreatingObject(mousePos);
            currentManipulation = ManipulationMode.None;
        }
    }

    // --- 이동 및 리사이징 ---
    private void MoveTarget(Vector3 currentMousePos)
    {
        // 1. 원래 이동 로직 (델타 값 계산)
        Vector3 delta = currentMousePos - startMousePos;
        Vector3 targetPos = startObjectPos + delta;

        // 2. [수정] 스냅 적용
        if (useGridSnap)
        {
            targetPos = GetSnappedPosition(targetPos);
        }

        if (selectedArea != null)
        {
            selectedArea.transform.position = targetPos;
            UpdateHandles(selectedArea.GetComponent<BoxCollider2D>().bounds);
        }
        else if (selectedObject != null)
        {
            selectedObject.transform.position = targetPos;
            UpdateHandles(selectedObject.GetComponent<Collider2D>().bounds);
        }
    }

    private void ResizeTarget(Vector3 currentMousePos)
    {
        Bounds newBounds = CalculateNewBounds(startBounds, currentManipulation, currentMousePos);

        if (selectedArea != null)
        {
            ApplyBoundsToTransform(selectedArea.transform, selectedArea.GetComponent<BoxCollider2D>(), newBounds);
            UpdateHandles(newBounds);
        }
        else if (selectedObject != null)
        {
            ApplyBoundsToTransform(selectedObject.transform, selectedObject.GetComponent<BoxCollider2D>(), newBounds);
            UpdateHandles(newBounds);
        }
    }

    private Bounds CalculateNewBounds(Bounds initial, ManipulationMode mode, Vector3 mousePos)
    {
        Vector3 min = initial.min;
        Vector3 max = initial.max;

        switch (mode)
        {
            case ManipulationMode.Resize_TopLeft: min.x = mousePos.x; max.y = mousePos.y; break;
            case ManipulationMode.Resize_TopRight: max.x = mousePos.x; max.y = mousePos.y; break;
            case ManipulationMode.Resize_BottomLeft: min.x = mousePos.x; min.y = mousePos.y; break;
            case ManipulationMode.Resize_BottomRight: max.x = mousePos.x; min.y = mousePos.y; break;
        }

        Vector3 center = (min + max) * 0.5f;
        Vector3 size = new Vector3(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y), 0);
        return new Bounds(center, size);
    }

    private void ApplyBoundsToTransform(Transform targetTr, BoxCollider2D col, Bounds bounds)
    {
        targetTr.position = bounds.center;
        if (col != null) col.size = bounds.size;

        if (targetTr.TryGetComponent<SpriteRenderer>(out SpriteRenderer sr))
        {
            sr.size = bounds.size; // Sliced 모드 가정
        }
    }

    // --- 선택 및 핸들 관리 ---
    private void SelectTarget(Collider2D col)
    {
        DeselectAll();
        var area = col.GetComponent<WorldArea>();
        var obj = col.GetComponent<WorldObject>();

        if (currentMode == EditorMode.AreaEdit && area != null)
        {
            selectedArea = area;
            if (areaNameInputField != null) areaNameInputField.text = area.areaName;
            if (areaSectorInputField != null) areaSectorInputField.text = area.sectorName;
            Highlight(area.transform, true);
            CreateHandles(col.bounds);
        }
        else if (currentMode == EditorMode.ObjectEdit && obj != null)
        {
            selectedObject = obj;
            if (objectNameInputField != null) objectNameInputField.text = obj.objectName;
            if (objectTypeInputField != null) objectTypeInputField.text = obj.objectType;

            // [수정] State 불러오기
            if (objectStateInputField != null) objectStateInputField.text = obj.objectState;

            Highlight(obj.transform, true);
            CreateHandles(col.bounds);
        }
    }

   

    private void DeselectAll()
    {
        if (selectedArea != null) Highlight(selectedArea.transform, false);
        if (selectedObject != null) Highlight(selectedObject.transform, false);
        selectedArea = null; selectedObject = null; ClearHandles();

        if (areaNameInputField != null) areaNameInputField.text = "";
        if (areaSectorInputField != null) areaSectorInputField.text = "";
        if (objectNameInputField != null) objectNameInputField.text = "";
        if (objectTypeInputField != null) objectTypeInputField.text = "";
        
        // [수정] State UI 초기화
        if (objectStateInputField != null) objectStateInputField.text = "";
    }

    private void Highlight(Transform target, bool isSelected)
    {
        if (target.TryGetComponent<SpriteRenderer>(out SpriteRenderer sr))
        {
            sr.color = isSelected ? selectedColor : normalColor;
        }
    }

    private void CreateHandles(Bounds bounds)
    {
        ClearHandles();
        SpawnHandle(new Vector3(bounds.min.x, bounds.max.y), ManipulationMode.Resize_TopLeft);
        SpawnHandle(new Vector3(bounds.max.x, bounds.max.y), ManipulationMode.Resize_TopRight);
        SpawnHandle(new Vector3(bounds.min.x, bounds.min.y), ManipulationMode.Resize_BottomLeft);
        SpawnHandle(new Vector3(bounds.max.x, bounds.min.y), ManipulationMode.Resize_BottomRight);
    }

    private void SpawnHandle(Vector3 pos, ManipulationMode mode)
    {
        GameObject handle = Instantiate(resizeHandlePrefab, transform);
        handle.transform.position = pos;
        var handleScript = handle.GetComponent<EditorResizeHandle>();
        if (handleScript != null) handleScript.handleType = mode;
        activeHandles.Add(handle);
    }

    private void UpdateHandles(Bounds bounds)
    {
        if (activeHandles.Count < 4) return;
        activeHandles[0].transform.position = new Vector3(bounds.min.x, bounds.max.y);
        activeHandles[1].transform.position = new Vector3(bounds.max.x, bounds.max.y);
        activeHandles[2].transform.position = new Vector3(bounds.min.x, bounds.min.y);
        activeHandles[3].transform.position = new Vector3(bounds.max.x, bounds.min.y);
    }

    private void ClearHandles()
    {
        foreach (var h in activeHandles) Destroy(h);
        activeHandles.Clear();
    }

    private GameObject CheckHandleClick(Vector3 mousePos)
    {
        Collider2D hit = Physics2D.OverlapPoint(mousePos);
        if (hit != null && hit.GetComponent<EditorResizeHandle>() != null) return hit.gameObject;
        return null;
    }

    // --- 생성 로직 ---
    private void OnCreateAreaClicked()
    {
        createAreaButton.interactable = false;
        DeselectAll();
        Debug.Log("[WorldEditor] Area 생성 모드 시작");
    }

    private void FinishCreatingArea(Vector3 start, Vector3 end)
    {
        createAreaButton.interactable = true;

        GameObject go = new GameObject("New Area");
        Vector3 center = (start + end) * 0.5f;
        Vector3 size = new Vector3(Mathf.Abs(start.x - end.x), Mathf.Abs(start.y - end.y), 0);
        go.transform.position = center;

        var area = go.AddComponent<WorldArea>();
        area.areaName = "New Area";

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = size;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateColorSprite(new Color(0, 1, 0, 0.3f));
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = size;

        go.layer = LayerMask.NameToLayer("Clickable");

        RefreshLists();
        SelectTarget(col);
        Debug.Log($"[WorldEditor] Area 생성 완료: {go.name}");
    }

    private void OnCreateObjectClicked()
    {
        createObjectButton.interactable = false;
        DeselectAll();
        Debug.Log("[WorldEditor] Object 생성 모드 시작");
    }

    private void FinishCreatingObject(Vector3 pos)
    {
        createObjectButton.interactable = true;

        GameObject go = new GameObject("New Object");
        go.transform.position = pos;

        // [수정] 순서 중요: Collider -> WorldObject
        var col = go.AddComponent<BoxCollider2D>();

        var obj = go.AddComponent<WorldObject>();
        obj.objectName = "New Object";
        obj.objectType = "Generic";

        var sr = go.AddComponent<SpriteRenderer>();
        if (selectedNewSprite != null) sr.sprite = selectedNewSprite;
        else sr.sprite = CreateColorSprite(Color.white);

        sr.drawMode = SpriteDrawMode.Sliced;
        col.size = sr.bounds.size;
        go.layer = LayerMask.NameToLayer("Clickable");

        RefreshLists();
        SelectTarget(col); // 생성 후 바로 선택하여 속성 추가 가능하게 함
        Debug.Log($"[WorldEditor] Object 생성 완료: {go.name}");
    }

    private void OnDeleteAreaClicked()
    {
        if (selectedArea != null)
        {
            Destroy(selectedArea.gameObject);
            DeselectAll();
            RefreshLists();
        }
    }

    private void OnDeleteObjectClicked()
    {
        if (selectedObject != null)
        {
            Destroy(selectedObject.gameObject);
            DeselectAll();
            RefreshLists();
        }
    }

    // --- 기타 ---
    private void OnSpriteDropdownChanged(int index)
    {
        if (index > 0 && index <= availableSprites.Count)
        {
            selectedNewSprite = availableSprites[index - 1];
            objectSpritePreview.sprite = selectedNewSprite;

            if (selectedObject != null)
            {
                selectedObject.GetComponent<SpriteRenderer>().sprite = selectedNewSprite;
                selectedObject.objectName = selectedNewSprite.name;
                selectedObject.RefreshVisuals();
            }
        }
    }


    private Vector3 GetMouseWorldPos()
    {
        Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        pos.z = 0;
        return pos;
    }

    private Sprite CreateColorSprite(Color color)
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100);
    }

    // ============================================================
    // [새로 구현된 이미지 생성 부분]
    // ============================================================

    private void OnGenerateImageClicked()
    {
        string prompt = imagePromptInputField.text.Trim();
        if (string.IsNullOrEmpty(prompt))
        {
            Debug.LogWarning("이미지 생성을 위한 프롬프트를 입력해주세요.");
            return;
        }

        if (editorClient == null)
        {
            // 키가 없으면 런타임에 찾거나 새로 생성 시도
            if (!string.IsNullOrEmpty(openAIKey))
                editorClient = new OpenAIClient(openAIKey);
            else
            {
                Debug.LogError("OpenAI API Key가 설정되지 않았습니다. WorldEditorManager의 Inspector를 확인해주세요.");
                return;
            }
        }

        // UI 비활성화 (중복 클릭 방지)
        generateImageButton.interactable = false;
        Debug.Log($"[WorldEditor] 이미지 생성 요청 중... Prompt: {prompt}");

        // 픽셀 아트 스타일 자동 추가 (선택 사항)
        string fullPrompt = prompt + ", pixel art style, top-down view, white background, game asset";

        StartCoroutine(editorClient.GenerateImage(fullPrompt, (url) =>
        {
            if (!string.IsNullOrEmpty(url))
            {
                Debug.Log($"[WorldEditor] 이미지 URL 수신 완료. 다운로드 시작...");
                StartCoroutine(DownloadAndCreateSprite(url));
            }
            else
            {
                Debug.LogError("[WorldEditor] 이미지 생성 실패 (URL 없음)");
                generateImageButton.interactable = true;
            }
        }, size: "1024x1024", quality: "standard"));
    }

    private IEnumerator DownloadAndCreateSprite(string url)
    {
        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(www);

                // [중요] 픽셀 아트용 필터 설정
                texture.filterMode = FilterMode.Point;

                // 스프라이트 생성 (중심점 0.5, 0.5 / 32 PPU)
                // DALL-E는 1024px이므로 PPU를 조절하거나 텍스처를 리사이징해야 하지만,
                // 일단 그대로 생성 후 에디터에서 크기를 줄이는 방식을 사용합니다.
                Sprite newSprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100 // PPU (기본 100)
                );
                newSprite.name = "Generated_" + System.DateTime.Now.ToString("HHmmss");

                // 생성된 스프라이트 선택
                selectedNewSprite = newSprite;
                objectSpritePreview.sprite = newSprite;

                // 만약 현재 선택된 오브젝트가 있다면 바로 적용
                if (selectedObject != null)
                {
                    var sr = selectedObject.GetComponent<SpriteRenderer>();
                    sr.sprite = newSprite;
                    sr.drawMode = SpriteDrawMode.Sliced;
                    // 크기 재조정 (콜라이더 포함)
                    selectedObject.RefreshVisuals();
                }

                Debug.Log("[WorldEditor] 이미지 생성 및 적용 완료!");
            }
            else
            {
                Debug.LogError($"[WorldEditor] 이미지 다운로드 실패: {www.error}");
            }

            generateImageButton.interactable = true;
        }
    }

    // 입력받은 위치를 가장 가까운 그리드 좌표로 반환
    private Vector3 GetSnappedPosition(Vector3 originalPos)
    {
        if (!useGridSnap) return originalPos;

        float x = Mathf.Round(originalPos.x / gridSize) * gridSize;
        float y = Mathf.Round(originalPos.y / gridSize) * gridSize;

        // 타일 중심에 맞추고 싶다면 아래처럼 오프셋이 필요할 수 있습니다. 
        // (예: 타일이 (0,0)이 아니라 (0.5, 0.5)가 중심인 경우)
        // return new Vector3(x + (gridSize * 0.5f), y + (gridSize * 0.5f), 0);

        return new Vector3(x, y, 0);
    }

    // 입력받은 크기(마우스 좌표)를 그리드 단위로 반환 (리사이징용)
    private Vector3 GetSnappedMousePosForResize(Vector3 mousePos)
    {
        if (!useGridSnap || !snapSizeToGrid) return mousePos;

        float x = Mathf.Round(mousePos.x / gridSize) * gridSize;
        float y = Mathf.Round(mousePos.y / gridSize) * gridSize;

        return new Vector3(x, y, 0);
    }
}
