using UnityEngine;
using TMPro;
using NPCSimulation.Core;

public class WorldTimeDisplay : MonoBehaviour
{
    public TextMeshProUGUI dateText; // 예: "2024년 1월 1일"
    public TextMeshProUGUI timeText; // 예: "08:30 AM"

    private void Update()
    {
        if (WorldTimeManager.Instance == null) return;

        System.DateTime now = WorldTimeManager.Instance.CurrentTime;

        if (dateText != null)
            dateText.text = now.ToString("yyyy년 MM월 dd일");

        if (timeText != null)
            timeText.text = now.ToString("tt hh:mm"); // 오전/오후 표시
    }
}
