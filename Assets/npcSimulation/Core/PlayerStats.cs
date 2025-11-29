using UnityEngine;
using System;

// [중요] 네임스페이스가 Controller와 같아야 서로 알아봅니다.
namespace NPCSimulation.Core
{
    public class PlayerStats : MonoBehaviour
    {
        public static PlayerStats Instance { get; private set; }

        [Header("Player Parameters (0-100)")]
        [Range(0, 100)] public int energy = 80;
        [Range(0, 100)] public int hunger = 20;
        [Range(0, 100)] public int hygiene = 80;
        [Range(0, 100)] public int happiness = 50;

        public event Action OnStatsChanged;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            OnStatsChanged?.Invoke();
        }

        public void ApplyStatChange(int dEnergy, int dHunger, int dHygiene, int dHappiness)
        {
            energy = Mathf.Clamp(energy + dEnergy, 0, 100);
            hunger = Mathf.Clamp(hunger + dHunger, 0, 100);
            hygiene = Mathf.Clamp(hygiene + dHygiene, 0, 100);
            happiness = Mathf.Clamp(happiness + dHappiness, 0, 100);

            Debug.Log($"[Stats] Energy:{energy}, Hunger:{hunger}, Hygiene:{hygiene}, Happiness:{happiness}");

            OnStatsChanged?.Invoke();
        }
    }
}
