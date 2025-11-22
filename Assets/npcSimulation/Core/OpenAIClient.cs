using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace NPCSimulation.Core
{
    /// <summary>
    /// OpenAI API 클라이언트
    /// </summary>
    public class OpenAIClient
    {
        private string apiKey = "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";
    private string chatModel = "gpt-4o-mini";
    private string embeddingModel = "text-embedding-3-small";
        private string imageModel = "dall-e-3";
    private const int embeddingMaxRetries = 5;
    private const float embeddingInitialBackoffSeconds = 1.5f;
    private const float embeddingMaxBackoffSeconds = 15f;
    private const float embeddingCooldownSeconds = 0.75f;
    private static double nextEmbeddingAvailableTime = 0d;
    
    // Chat Completion Rate Limiting
    private const float chatCooldownSeconds = 1.0f;
    private const int chatMaxRetries = 3; // 429 에러 시 재시도 횟수
    private const float chatRetryBackoffSeconds = 2.0f; // 재시도 대기 시간
    private static double nextChatAvailableTime = 0d;

        public OpenAIClient(string apiKey)
        {
            this.apiKey = apiKey;
        }

        #region Chat Completion

        [Serializable]
        private class ChatRequest
        {
            public string model;
            public List<ChatMessage> messages;
            public float temperature;
            public int max_tokens;
        }

        [Serializable]
        private class ChatMessage
        {
            public string role;
            public string content;
        }

        [Serializable]
        private class ChatResponse
        {
            public List<Choice> choices;
        }

        [Serializable]
        private class Choice
        {
            public ChatMessage message;
        }

        /// <summary>
        /// GPT-4에게 텍스트 생성 요청
        /// Rate Limiting + Retry 로직 포함
        /// </summary>
        public IEnumerator GetChatCompletion(string prompt, Action<string> callback, float temperature = 0.7f, int maxTokens = 500)
        {
            int attempt = 0;
            
            while (attempt < chatMaxRetries)
            {
                // Rate Limiting: API 호출 간격 조절
                while (true)
                {
                    double now = Time.realtimeSinceStartupAsDouble;
                    if (now >= nextChatAvailableTime)
                    {
                        nextChatAvailableTime = now + chatCooldownSeconds;
                        break;
                    }

                    double waitSeconds = Math.Min(chatCooldownSeconds, nextChatAvailableTime - now);
                    yield return new WaitForSeconds((float)waitSeconds);
                }

                // 로그 제거: Debug.Log 호출 중 메시지 제거

                string url = "https://api.openai.com/v1/chat/completions";

                ChatRequest request = new ChatRequest
                {
                    model = chatModel,
                    messages = new List<ChatMessage>
                    {
                        new ChatMessage { role = "user", content = prompt }
                    },
                    temperature = temperature,
                    max_tokens = maxTokens
                };

                string jsonData = JsonUtility.ToJson(request);
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

                using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
                {
                    www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    www.downloadHandler = new DownloadHandlerBuffer();
                    www.SetRequestHeader("Content-Type", "application/json");
                    www.SetRequestHeader("Authorization", $"Bearer {apiKey}");

                    yield return www.SendWebRequest();

                    if (www.result == UnityWebRequest.Result.Success)
                    {
                        //Debug.Log($"[OpenAI] Chat API 응답 성공!");
                        ChatResponse response = JsonUtility.FromJson<ChatResponse>(www.downloadHandler.text);
                        if (response.choices != null && response.choices.Count > 0)
                        {
                            callback?.Invoke(response.choices[0].message.content.Trim());
                            yield break; // 성공 시 종료
                        }
                        else
                        {
                            callback?.Invoke("ERROR: No response from GPT");
                            yield break;
                        }
                    }
                    
                    // 429 에러 처리
                    if (www.responseCode == 429)
                    {
                        attempt++;
                        if (attempt < chatMaxRetries)
                        {
                            float retryWait = chatRetryBackoffSeconds * attempt; // 지수 백오프
                            Debug.LogWarning($"[OpenAI] Rate Limit (429). {retryWait}초 후 재시도합니다. (시도 {attempt}/{chatMaxRetries})");
                            yield return new WaitForSeconds(retryWait);
                            continue; // 재시도
                        }
                        else
                        {
                            Debug.LogError($"[OpenAI] Rate Limit 초과. 최대 재시도 횟수({chatMaxRetries})를 초과했습니다.");
                            callback?.Invoke("ERROR: 요청이 너무 많습니다. 잠시 후 다시 시도해주세요.");
                            yield break;
                        }
                    }
                    else
                    {
                        // 다른 에러
                        Debug.LogError($"[OpenAI] API Error (code {www.responseCode}): {www.error}");
                        callback?.Invoke($"ERROR: {www.error}");
                        yield break;
                    }
                }
            }
            
            // 최대 재시도 초과 (여기 도달하면 안 되지만 안전장치)
            callback?.Invoke("ERROR: 요청 처리 실패");
        }

        #endregion

        #region Embedding

        [Serializable]
        private class EmbeddingRequest
        {
            public string model;
            public string input;
        }

        [Serializable]
        private class EmbeddingResponse
        {
            public List<EmbeddingData> data;
        }

        [Serializable]
        private class EmbeddingData
        {
            public float[] embedding;
        }

        /// <summary>
        /// 텍스트를 벡터로 변환 (임베딩)
        /// </summary>
        public IEnumerator GetEmbedding(string text, Action<float[]> callback)
        {
            string url = "https://api.openai.com/v1/embeddings";

            EmbeddingRequest request = new EmbeddingRequest
            {
                model = embeddingModel,
                input = text
            };

            string jsonData = JsonUtility.ToJson(request);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

            // 글로벌 쿨다운으로 호출 간 최소 간격 확보
            while (true)
            {
                double now = Time.realtimeSinceStartupAsDouble;
                if (now >= nextEmbeddingAvailableTime)
                {
                    nextEmbeddingAvailableTime = now + embeddingCooldownSeconds;
                    break;
                }

                double waitSeconds = Math.Min(embeddingCooldownSeconds, nextEmbeddingAvailableTime - now);
                yield return new WaitForSeconds((float)waitSeconds);
            }

            int attempt = 0;
            while (attempt < embeddingMaxRetries)
            {
                using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
                {
                    www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    www.downloadHandler = new DownloadHandlerBuffer();
                    www.SetRequestHeader("Content-Type", "application/json");
                    www.SetRequestHeader("Authorization", $"Bearer {apiKey}");

                    yield return www.SendWebRequest();

                    if (www.result == UnityWebRequest.Result.Success)
                    {
                        EmbeddingResponse response = JsonUtility.FromJson<EmbeddingResponse>(www.downloadHandler.text);
                        if (response.data != null && response.data.Count > 0)
                        {
                            callback?.Invoke(response.data[0].embedding);
                            yield break;
                        }

                        Debug.LogWarning("OpenAI Embedding Warning: 빈 응답을 수신했습니다.");
                        callback?.Invoke(null);
                        yield break;
                    }

                    bool isRateLimited = www.responseCode == 429;
                    if (isRateLimited && attempt < embeddingMaxRetries - 1)
                    {
                        float waitSeconds = Mathf.Min(embeddingMaxBackoffSeconds,
                            embeddingInitialBackoffSeconds * Mathf.Pow(2f, attempt));
                        Debug.LogWarning($"OpenAI Embedding Rate Limit (429). {waitSeconds:F1}초 후 재시도합니다. (시도 {attempt + 1}/{embeddingMaxRetries})");
                        yield return new WaitForSeconds(waitSeconds);
                        attempt++;
                        continue;
                    }

                    Debug.LogError($"OpenAI Embedding Error (code {www.responseCode}): {www.error}");
                    callback?.Invoke(null);
                    yield break;
                }
            }

            Debug.LogError("OpenAI Embedding Error: 최대 재시도 횟수를 초과했습니다.");
            callback?.Invoke(null);
        }

        #endregion

        #region Image Generation

        [Serializable]
        private class ImageRequest
        {
            public string model;
            public string prompt;
            public int n;
            public string size;
            public string quality;
        }

        [Serializable]
        private class ImageResponse
        {
            public List<ImageData> data;
        }

        [Serializable]
        private class ImageData
        {
            public string url;
        }

        /// <summary>
        /// DALL-E로 이미지 생성
        /// </summary>
        public IEnumerator GenerateImage(string prompt, Action<string> callback, string size = "1024x1024", string quality = "standard")
        {
            string url = "https://api.openai.com/v1/images/generations";

            ImageRequest request = new ImageRequest
            {
                model = imageModel,
                prompt = prompt,
                n = 1,
                size = size,
                quality = quality
            };

            string jsonData = JsonUtility.ToJson(request);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

            using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
            {
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");
                www.SetRequestHeader("Authorization", $"Bearer {apiKey}");

                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    ImageResponse response = JsonUtility.FromJson<ImageResponse>(www.downloadHandler.text);
                    if (response.data != null && response.data.Count > 0)
                    {
                        callback?.Invoke(response.data[0].url);
                    }
                    else
                    {
                        callback?.Invoke(null);
                    }
                }
                else
                {
                    Debug.LogError($"DALL-E Error: {www.error}");
                    callback?.Invoke(null);
                }
            }
        }

        #endregion
    }
}
