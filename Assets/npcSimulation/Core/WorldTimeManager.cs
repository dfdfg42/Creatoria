using System;
using UnityEngine;

namespace NPCSimulation.Core
{
    public class WorldTimeManager : MonoBehaviour
    {
        public static WorldTimeManager Instance { get; private set; }

        [Header("Time Settings")]
        [Tooltip("현실 1초당 게임 시간 몇 초가 흐를지 (60 = 현실 1초가 게임 1분)")]
        public float timeScale = 60f;

        [Header("Start Date")]
        public int startYear = 2024;
        public int startMonth = 1;
        public int startDay = 1;
        public int startHour = 8; // 아침 8시 시작

        // 실제 게임 내 현재 시간 (모든 NPC는 이걸 봐야 함)
        public DateTime CurrentTime { get; private set; }

        public bool IsPaused { get; set; } = false;

        private void Awake()
        {
            if (Instance != null && Instance != this) Destroy(gameObject);
            else Instance = this;

            // 게임 시작 시간 설정
            CurrentTime = new DateTime(startYear, startMonth, startDay, startHour, 0, 0);
        }

        private void Update()
        {
            if (IsPaused) return;

            // 현실 시간 흐름(Time.deltaTime) * 배율(timeScale) 만큼 게임 시간 추가
            double secondsToAdd = Time.deltaTime * timeScale;
            CurrentTime = CurrentTime.AddSeconds(secondsToAdd);
        }

        // 디버그용: 강제로 시간 넘기기
        public void AdvanceTime(int hours)
        {
            CurrentTime = CurrentTime.AddHours(hours);
        }
    }
}
