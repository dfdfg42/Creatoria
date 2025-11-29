using UnityEngine;
using TMPro; // [필수] TextMeshPro
using System.Collections;
using NPCSimulation.Core;

public class WorldObject : MonoBehaviour
{
    [Header("Basic Info")]
    public string objectName = "Object";
    public string objectType = "Furniture";
    public string objectState = "Default";

    [Header("Proximity Settings")]
    public float infoShowDistance = 3.0f; // 이 거리 안에 들어오면 정보 표시
    private Transform playerTransform;

    [Header("Interaction")]
    public float interactionRange = 1.5f;
    public bool isVisible = true;

    // [UI Settings - 인스펙터에서 연결]
    [Header("UI References")]
    public TextMeshPro popupText;    // 상태 변화 알림용 (노란색)
    public TextMeshPro infoText;     // 마우스 오버 정보용 (흰색)

    [Header("UI Config")]
    public float popupDuration = 2.0f;
    public Vector3 popupOffset = new Vector3(0, 1.0f, 0);

    private Coroutine popupCoroutine;
    private Vector3 originalPopupPos;

    private void Start()
    {
        // 시작 시 UI 숨기기 및 위치 저장
        if (popupText != null)
        {
            // 🔵 부모를 이 오브젝트로 고정
            popupText.transform.SetParent(transform, false);

            // 🔵 부모 기준 위쪽으로 올려두기 (popupOffset 사용)
            popupText.transform.localPosition = popupOffset;

            popupText.gameObject.SetActive(false);
            originalPopupPos = popupText.transform.localPosition;
        }

        if (infoText != null)
        {
            // 🔵 infoText도 부모를 이 오브젝트로 고정
            infoText.transform.SetParent(transform, false);

            // 🔵 같은 위치 쓰고 싶으면 popupOffset 재사용, 따로 하고 싶으면 다른 offset 변수 만들어도 됨
            infoText.transform.localPosition = popupOffset;

            infoText.gameObject.SetActive(false);
        }

        var player = FindObjectOfType<PlayerStats>();
        if (player != null) playerTransform = player.transform;
    }

    private void Update()
    {
        // [추가] 플레이어와의 거리 체크하여 UI 표시
        if (playerTransform != null && infoText != null)
        {
            float dist = Vector3.Distance(transform.position, playerTransform.position);

            // 마우스가 올라가 있지 않을 때만 거리 기반 작동 (마우스 오버가 우선순위)
            bool isMouseOver = false; // 실제로는 OnMouseEnter에서 플래그 관리 필요할 수 있음

            if (dist <= infoShowDistance)
            {
                // 가까이 있으면 켜기
                if (!infoText.gameObject.activeSelf)
                {
                    infoText.gameObject.SetActive(true);
                    UpdateInfoText(); // 텍스트 갱신
                }
            }
            else
            {
                // 멀어지면 끄기 (단, 팝업 텍스트나 마우스 오버 상태가 아닐 때)
                if (infoText.gameObject.activeSelf)
                {
                    // 여기서는 간단히 끄지만, 마우스 오버 로직과 겹치면 플래그 관리 필요
                    infoText.gameObject.SetActive(false);
                }
            }
        }
    }

    /// <summary>
    /// 외부(NPC)에서 상태를 변경할 때 호출
    /// </summary>
    public void UpdateState(string newState)
    {
        if (objectState == newState) return;

        objectState = newState;
        Debug.Log($"[WorldObject] {objectName} state updated to: {objectState}");

        // 1. 상태 변화 알림 띄우기
        if (popupText != null)
        {
            if (popupCoroutine != null) StopCoroutine(popupCoroutine);
            popupCoroutine = StartCoroutine(ShowPopupRoutine(newState));
        }

        // 2. 정보창 갱신 (켜져 있다면)
        if (infoText != null && infoText.gameObject.activeSelf)
        {
            UpdateInfoText();
        }

        RefreshVisuals();
    }

    private IEnumerator ShowPopupRoutine(string text)
    {
        popupText.gameObject.SetActive(true);
        popupText.text = text;
        popupText.color = Color.yellow; // 강조색

        // 위치 초기화 후 시작
        popupText.transform.localPosition = originalPopupPos;

        float timer = 0f;
        while (timer < popupDuration)
        {
            timer += Time.deltaTime;
            // 위로 둥실 떠오르는 효과
            popupText.transform.position += Vector3.up * Time.deltaTime * 0.5f;
            yield return null;
        }

        popupText.gameObject.SetActive(false);
    }

    private void UpdateInfoText()
    {
        if (infoText != null)
        {
            infoText.text = $"{objectName}\n<size=70%>({objectState})</size>";
        }
    }

    // --- 마우스 이벤트 ---

    private void OnMouseEnter()
    {
        // UI에 가려져 있으면 무시
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

        if (infoText != null)
        {
            infoText.gameObject.SetActive(true);
            UpdateInfoText();
        }
    }

    private void OnMouseExit()
    {
        if (infoText != null)
        {
            infoText.gameObject.SetActive(false);
        }
    }

    public void RefreshVisuals() { }

    public string GetDescription()
    {
        return $"{objectName} is currently {objectState}.";
    }
}
