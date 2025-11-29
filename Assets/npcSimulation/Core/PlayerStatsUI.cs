using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NPCSimulation.Core;

public class PlayerStatsUI : MonoBehaviour
{
    [Header("UI Sliders")]
    public Slider energySlider;
    public Slider hungerSlider;
    public Slider hygieneSlider;
    public Slider happinessSlider;

    [Header("Value Texts (Optional)")]
    public TextMeshProUGUI energyText;
    public TextMeshProUGUI hungerText;

    private void Start()
    {
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnStatsChanged += UpdateUI;
            UpdateUI(); // 초기화
        }
    }

    private void UpdateUI()
    {
        var stats = PlayerStats.Instance;
        if (stats == null) return;

        if (energySlider) energySlider.value = stats.energy / 100f;
        if (hungerSlider) hungerSlider.value = stats.hunger / 100f;
        if (hygieneSlider) hygieneSlider.value = stats.hygiene / 100f;
        if (happinessSlider) happinessSlider.value = stats.happiness / 100f;

        if (energyText) energyText.text = $"{stats.energy}%";
        if (hungerText) hungerText.text = $"{stats.hunger}%";
    }
}
