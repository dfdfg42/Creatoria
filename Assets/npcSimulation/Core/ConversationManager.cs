using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NPCSimulation.Core
{
    /// <summary>
    /// 대화 관리 시스템
    /// </summary>
    public class ConversationManager
    {
        private OpenAIClient llmClient;
        
        // 대화 버퍼
        private Queue<ConversationTurn> conversationBuffer = new Queue<ConversationTurn>();
        private int bufferSize = 10;
        
        // 대화 요약
        private string conversationSummary = "";
        private DateTime lastSummaryTime = DateTime.MinValue;
        
        public ConversationManager(OpenAIClient llmClient, int bufferSize = 10)
        {
            this.llmClient = llmClient;
            this.bufferSize = bufferSize;
        }

        /// <summary>
        /// 대화 턴 추가
        /// </summary>
        public void AddTurn(string speaker, string message)
        {
            ConversationTurn turn = new ConversationTurn
            {
                speaker = speaker,
                message = message,
                timestamp = DateTime.Now
            };

            conversationBuffer.Enqueue(turn);

            if (conversationBuffer.Count > bufferSize)
            {
                conversationBuffer.Dequeue();
            }

            Debug.Log($"[ConversationManager] {speaker}: {message}");
        }

        /// <summary>
        /// 최근 대화 내역 가져오기
        /// </summary>
        public string GetRecentConversation(int count = 5)
        {
            var recent = conversationBuffer.Reverse().Take(count).Reverse();
            
            string result = "";
            foreach (var turn in recent)
            {
                result += $"{turn.speaker}: {turn.message}\n";
            }
            
            return result;
        }

        /// <summary>
        /// 전체 대화 요약 가져오기
        /// </summary>
        public string GetConversationSummary()
        {
            if (string.IsNullOrEmpty(conversationSummary))
            {
                return "아직 대화가 없습니다.";
            }
            return conversationSummary;
        }

        /// <summary>
        /// 대화 요약 생성 (비동기)
        /// </summary>
        public void GenerateSummary(MonoBehaviour coroutineRunner, Action<string> callback = null)
        {
            if (conversationBuffer.Count < 3)
            {
                callback?.Invoke("대화가 부족합니다.");
                return;
            }

            string recentConversation = GetRecentConversation(conversationBuffer.Count);

            string prompt = $@"
다음 대화 내용을 2-3문장으로 핵심만 요약해주세요:

{recentConversation}

요약:";

            coroutineRunner.StartCoroutine(llmClient.GetChatCompletion(prompt, (summary) =>
            {
                conversationSummary = summary;
                lastSummaryTime = DateTime.Now;
                
                Debug.Log($"[ConversationManager] Summary generated: {summary}");
                callback?.Invoke(summary);
            }, temperature: 0.3f, maxTokens: 150));
        }

        /// <summary>
        /// 대화 버퍼 초기화
        /// </summary>
        public void ClearBuffer()
        {
            conversationBuffer.Clear();
            Debug.Log("[ConversationManager] Buffer cleared");
        }

        /// <summary>
        /// 대화 맥락을 포함한 프롬프트 생성
        /// </summary>
        public string BuildContextualPrompt(string basePrompt, bool includeSummary = true, int recentTurns = 5)
        {
            string context = "";

            if (includeSummary && !string.IsNullOrEmpty(conversationSummary))
            {
                context += $"### 이전 대화 요약 ###\n{conversationSummary}\n\n";
            }

            if (conversationBuffer.Count > 0)
            {
                context += $"### 최근 대화 ###\n{GetRecentConversation(recentTurns)}\n\n";
            }

            return context + basePrompt;
        }
    }

    [Serializable]
    public class ConversationTurn
    {
        public string speaker;
        public string message;
        public DateTime timestamp;

        public override string ToString()
        {
            return $"[{timestamp:HH:mm}] {speaker}: {message}";
        }
    }
}
