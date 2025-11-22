using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NPCSimulation.Core
{
    /// <summary>
    /// 메모리 관리 시스템
    /// </summary>
    public class MemoryManager
    {
        private OpenAIClient llmClient;
        private string npcName;
        private string personaDescription;

        // 메모리 저장소
        private List<Memory> eventMemories = new List<Memory>();
        private List<Memory> thoughtMemories = new List<Memory>();
        private Dictionary<string, List<Memory>> keywordToEvent = new Dictionary<string, List<Memory>>();
        private Dictionary<string, List<Memory>> keywordToThought = new Dictionary<string, List<Memory>>();
        private Dictionary<string, int> keywordStrength = new Dictionary<string, int>();

        // 메모리 룸
        private Queue<Memory> shortTermMemoryRoom = new Queue<Memory>();
        private List<Memory> longTermMemoryRoom = new List<Memory>();

        // 지식 베이스
        private Dictionary<string, Knowledge> knowledgeBase = new Dictionary<string, Knowledge>();

        // 설정
        private int shortTermMaxLength = 20;
        private int summaryWindow = 10;
        private float recencyDecay = 0.99f;
        private float[] scoreWeights = { 1f, 1f, 1f }; // [recency, importance, relevance]

        // 리플렉션
        private int reflectionImportanceSum = 0;
        private int reflectionThreshold = 100;

        public MemoryManager(OpenAIClient llmClient, string npcName, string personaDescription = "")
        {
            this.llmClient = llmClient;
            this.npcName = npcName;
            this.personaDescription = personaDescription;
        }

        #region Add Memory

        /// <summary>
        /// 새로운 메모리 추가
        /// </summary>
        public void AddMemory(MemoryType type, string description, int importance = -1, MonoBehaviour coroutineRunner = null)
        {
            if (importance == -1)
            {
                // 중요도 계산 (비동기)
                if (coroutineRunner != null)
                {
                    coroutineRunner.StartCoroutine(CalculateImportanceAsync(description, type, (calculatedImportance) =>
                    {
                        AddMemoryInternal(type, description, calculatedImportance, coroutineRunner);
                    }));
                }
                else
                {
                    // 동기 호출 시 기본값 사용
                    AddMemoryInternal(type, description, 5, null);
                }
            }
            else
            {
                AddMemoryInternal(type, description, importance, coroutineRunner);
            }
        }

        private void AddMemoryInternal(MemoryType type, string description, int importance, MonoBehaviour coroutineRunner)
        {
            // 임베딩 생성 (비동기)
            if (coroutineRunner != null)
            {
                coroutineRunner.StartCoroutine(llmClient.GetEmbedding(description, (embedding) =>
                {
                    // 키워드 추출 (비동기)
                    coroutineRunner.StartCoroutine(ExtractKeywordsAsync(description, (keywords) =>
                    {
                        CreateAndStoreMemory(type, description, importance, embedding, keywords);
                    }));
                }));
            }
            else
            {
                // 동기 호출
                CreateAndStoreMemory(type, description, importance, null, new List<string>());
            }
        }

        private void CreateAndStoreMemory(MemoryType type, string description, int importance, float[] embedding, List<string> keywords)
        {
            Memory newMemory = new Memory(type, description, importance, embedding, keywords);

            // 메모리 저장
            if (type == MemoryType.Event)
            {
                eventMemories.Add(newMemory);
            }
            else
            {
                thoughtMemories.Add(newMemory);
            }

            // 키워드 인덱싱
            foreach (string keyword in keywords)
            {
                if (type == MemoryType.Event)
                {
                    if (!keywordToEvent.ContainsKey(keyword))
                        keywordToEvent[keyword] = new List<Memory>();
                    keywordToEvent[keyword].Add(newMemory);
                }
                else
                {
                    if (!keywordToThought.ContainsKey(keyword))
                        keywordToThought[keyword] = new List<Memory>();
                    keywordToThought[keyword].Add(newMemory);
                }

                if (!keywordStrength.ContainsKey(keyword))
                    keywordStrength[keyword] = 0;
                keywordStrength[keyword] += importance;
            }

            // 단기 메모리 룸 처리
            if (type == MemoryType.Event)
            {
                shortTermMemoryRoom.Enqueue(newMemory);

                if (shortTermMemoryRoom.Count >= summaryWindow)
                {
                    // TODO: 요약 후 장기 메모리로 이동
                    shortTermMemoryRoom.Clear();
                }
            }

            // 리플렉션 트리거 체크
            reflectionImportanceSum += importance;
            if (reflectionImportanceSum >= reflectionThreshold)
            {
                Debug.Log($"[MemoryManager] Reflection triggered! (sum: {reflectionImportanceSum})");
                // TODO: Reflection 실행
                reflectionImportanceSum = 0;
            }

            Debug.Log($"[MemoryManager] Memory added: {newMemory}");
        }

        #endregion

        #region Retrieve Memory

        /// <summary>
        /// 관련 메모리 검색 (중요도 + 최신성 + 관련성 점수 기반)
        /// </summary>
        public List<Memory> RetrieveRelevantMemories(string query, int topK = 5, MonoBehaviour coroutineRunner = null)
        {
            if (coroutineRunner != null)
            {
                // 비동기 버전은 콜백으로 처리
                // TODO: 구현
            }

            // 동기 버전 (임베딩 없이 키워드 기반)
            List<Memory> allMemories = new List<Memory>();
            allMemories.AddRange(eventMemories);
            allMemories.AddRange(thoughtMemories);

            // 간단한 키워드 매칭
            var rankedMemories = allMemories
                .Select(m => new
                {
                    Memory = m,
                    Score = CalculateRetrievalScore(m, query)
                })
                .OrderByDescending(x => x.Score)
                .Take(topK)
                .Select(x => x.Memory)
                .ToList();

            return rankedMemories;
        }

        private float CalculateRetrievalScore(Memory memory, string query)
        {
            // Recency
            float recencyScore = memory.GetRecencyScore(recencyDecay);

            // Importance (0-10 → 0-1)
            float importanceScore = memory.importance / 10f;

            // Relevance (키워드 매칭으로 근사)
            float relevanceScore = 0f;
            string lowerQuery = query.ToLower();
            foreach (string keyword in memory.keywords)
            {
                if (lowerQuery.Contains(keyword.ToLower()))
                {
                    relevanceScore += 0.3f;
                }
            }
            relevanceScore = Mathf.Clamp01(relevanceScore);

            // 가중 합
            float totalScore = recencyScore * scoreWeights[0] +
                               importanceScore * scoreWeights[1] +
                               relevanceScore * scoreWeights[2];

            return totalScore;
        }

        /// <summary>
        /// 최근 메모리 검색
        /// </summary>
        public List<Memory> RetrieveRecentMemories(int count = 5)
        {
            List<Memory> allMemories = new List<Memory>();
            allMemories.AddRange(eventMemories);
            allMemories.AddRange(thoughtMemories);

            return allMemories
                .OrderByDescending(m => m.timestamp)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// 오늘 생성된 메모리들 가져오기
        /// </summary>
        public List<Memory> GetMemoriesFromToday()
        {
            DateTime today = DateTime.Today;
            List<Memory> allMemories = new List<Memory>();
            allMemories.AddRange(eventMemories);
            allMemories.AddRange(thoughtMemories);

            return allMemories
                .Where(m => m.timestamp.Date == today)
                .OrderByDescending(m => m.timestamp)
                .ToList();
        }

        /// <summary>
        /// 특정 시간 범위의 메모리 가져오기
        /// </summary>
        public List<Memory> GetMemoriesInTimeRange(DateTime startTime, DateTime endTime)
        {
            List<Memory> allMemories = new List<Memory>();
            allMemories.AddRange(eventMemories);
            allMemories.AddRange(thoughtMemories);

            return allMemories
                .Where(m => m.timestamp >= startTime && m.timestamp <= endTime)
                .OrderByDescending(m => m.timestamp)
                .ToList();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 중요도 계산 (비동기)
        /// </summary>
        private System.Collections.IEnumerator CalculateImportanceAsync(string description, MemoryType type, Action<int> callback)
        {
            string memoryCategory = type == MemoryType.Event ? "사건" : "생각";
            
            string prompt = $@"
다음은 '{npcName}'에 대한 간략한 설명입니다.
{personaDescription}

1점에서 10점까지의 척도에서, 다음 {memoryCategory}의 중요도를 평가해 주세요.
- 1점: 일상적이고 평범한 일 (예: 아침 식사)
- 5점: 보통의 흥미로운 일 (예: 친구와 대화)
- 10점: 매우 중요하고 기억할 만한 일 (예: 중요한 결정)

{memoryCategory}: {description}

숫자만 답해주세요 (1-10):";

            yield return llmClient.GetChatCompletion(prompt, (response) =>
            {
                if (int.TryParse(response.Trim(), out int importance))
                {
                    importance = Mathf.Clamp(importance, 1, 10);
                    callback?.Invoke(importance);
                }
                else
                {
                    callback?.Invoke(5); // 기본값
                }
            }, temperature: 0.1f, maxTokens: 10);
        }

        /// <summary>
        /// 키워드 추출 (비동기)
        /// </summary>
        private System.Collections.IEnumerator ExtractKeywordsAsync(string description, Action<List<string>> callback)
        {
            string prompt = $@"
다음 문장에서 가장 중요한 핵심 키워드를 5개 이하로 추출해줘.
문장이 함의하는 '개념'도 포함해줘.
쉼표로 구분해서 명사 형태로 출력해줘.

문장: ""{description}""
키워드:";

            yield return llmClient.GetChatCompletion(prompt, (response) =>
            {
                List<string> keywords = new List<string>();
                string[] parts = response.Split(',');
                foreach (string part in parts)
                {
                    string keyword = part.Trim();
                    if (!string.IsNullOrEmpty(keyword))
                    {
                        keywords.Add(keyword);
                    }
                }
                callback?.Invoke(keywords);
            }, temperature: 0.1f, maxTokens: 50);
        }

        #endregion

        #region Knowledge Base

        /// <summary>
        /// 지식 추가 또는 강화
        /// </summary>
        public void AddOrReinforceKnowledge(string concept, string description, MonoBehaviour coroutineRunner = null)
        {
            if (knowledgeBase.ContainsKey(concept))
            {
                knowledgeBase[concept].Reinforce();
                Debug.Log($"[MemoryManager] Knowledge reinforced: {concept} (count: {knowledgeBase[concept].reinforcementCount})");
            }
            else
            {
                if (coroutineRunner != null)
                {
                    coroutineRunner.StartCoroutine(llmClient.GetEmbedding(description, (embedding) =>
                    {
                        Knowledge newKnowledge = new Knowledge(concept, description, embedding);
                        knowledgeBase[concept] = newKnowledge;
                        Debug.Log($"[MemoryManager] New knowledge learned: {concept}");
                    }));
                }
                else
                {
                    Knowledge newKnowledge = new Knowledge(concept, description, null);
                    knowledgeBase[concept] = newKnowledge;
                }
            }
        }

        #endregion
    }
}
