using System;
using System.Collections.Generic;
using UnityEngine;

namespace NPCSimulation.Core
{
    /// <summary>
    /// 메모리 데이터 구조
    /// </summary>
    [Serializable]
    public class Memory
    {
        public string id;
        public MemoryType type;
        public string description;
        public int importance;
        public DateTime timestamp;
        public float[] embedding;
        public List<string> keywords;
        public List<string> evidenceIds; // Reflection용

        public Memory(MemoryType type, string description, int importance, float[] embedding = null, List<string> keywords = null, List<string> evidenceIds = null)
        {
            this.id = Guid.NewGuid().ToString();
            this.type = type;
            this.description = description;
            this.importance = importance;
            this.timestamp = DateTime.Now;
            this.embedding = embedding;
            this.keywords = keywords ?? new List<string>();
            this.evidenceIds = evidenceIds ?? new List<string>();
        }

        /// <summary>
        /// 메모리의 최신성 점수 계산 (시간이 지날수록 감소)
        /// </summary>
        public float GetRecencyScore(float decayRate = 0.99f)
        {
            TimeSpan elapsed = DateTime.Now - timestamp;
            int hoursElapsed = (int)elapsed.TotalHours;
            return Mathf.Pow(decayRate, hoursElapsed);
        }

        /// <summary>
        /// 두 메모리 간 유사도 계산 (코사인 유사도)
        /// </summary>
        public float CalculateSimilarity(Memory other)
        {
            if (embedding == null || other.embedding == null || embedding.Length != other.embedding.Length)
                return 0f;

            float dotProduct = 0f;
            float normA = 0f;
            float normB = 0f;

            for (int i = 0; i < embedding.Length; i++)
            {
                dotProduct += embedding[i] * other.embedding[i];
                normA += embedding[i] * embedding[i];
                normB += other.embedding[i] * other.embedding[i];
            }

            if (normA == 0f || normB == 0f)
                return 0f;

            return dotProduct / (Mathf.Sqrt(normA) * Mathf.Sqrt(normB));
        }

        public override string ToString()
        {
            return $"[{type}] {description} (importance: {importance}, timestamp: {timestamp:yyyy-MM-dd HH:mm})";
        }
    }

    public enum MemoryType
    {
        Event,      // 경험한 사건
        Thought,    // 내면의 생각
        Reflection  // 고차원적 추상
    }

    /// <summary>
    /// 지식 데이터 구조
    /// </summary>
    [Serializable]
    public class Knowledge
    {
        public string concept;
        public string description;
        public float[] embedding;
        public DateTime learnedAt;
        public int reinforcementCount;

        public Knowledge(string concept, string description, float[] embedding = null)
        {
            this.concept = concept;
            this.description = description;
            this.embedding = embedding;
            this.learnedAt = DateTime.Now;
            this.reinforcementCount = 1;
        }

        public void Reinforce()
        {
            reinforcementCount++;
        }
    }

    /// <summary>
    /// 일일 계획 아이템 (논문의 Action 개념)
    /// </summary>
    [Serializable]
    public class PlanItem
    {
        public int startHour;
        public int duration;
        public string activity;          // 활동 설명
        public string location;          // Arena (WorldArea)
        public string targetObject;      // GameObject 이름 (선택적)
        public string emoji;

        public PlanItem(int startHour, int duration, string activity, string location, string targetObject = null, string emoji = "📍")
        {
            this.startHour = startHour;
            this.duration = duration;
            this.activity = activity;
            this.location = location;
            this.targetObject = targetObject;  // null이면 나중에 AI가 결정
            this.emoji = emoji;
        }

        public override string ToString()
        {
            string objStr = !string.IsNullOrEmpty(targetObject) ? $" ({targetObject})" : "";
            return $"{startHour:D2}:00 ({duration}h) - {activity} @ {location}{objStr} {emoji}";
        }
    }
}
