using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NPCSimulation.Core
{
    [RequireComponent(typeof(Collider2D))]
    public class WorldObject : MonoBehaviour
    {
        [Header("Object Info")]
        public string objectName = "Unknown Object";
        public ObjectType objectType = ObjectType.Generic;
        
        [Header("AI-Managed Description")]
        [TextArea(2, 5)]
        [Tooltip("AI가 관찰하고 자유롭게 업데이트하는 설명")]
        public string currentDescription = "";
        
        [Header("Initial Properties")]
        [Tooltip("Inspector에서 설정할 초기 속성들")]
        public List<PropertyPair> initialProperties = new List<PropertyPair>();
        
        [Header("Interaction")]
        public bool isInteractable = true;
        public float interactionRange = 1.5f;
        
        [Header("Perception")]
        public bool isVisible = true;
        public bool isObstacle = false;

        private Dictionary<string, string> properties = new Dictionary<string, string>();

        private void Start()
        {
            InitializeProperties();
        }

        private void InitializeProperties()
        {
            foreach (var prop in initialProperties)
            {
                if (!string.IsNullOrEmpty(prop.key))
                {
                    properties[prop.key] = prop.value;
                }
            }
            
            if (string.IsNullOrEmpty(currentDescription) && properties.Count > 0)
            {
                UpdateDescription();
            }
        }

        public string GetProperty(string key)
        {
            return properties.ContainsKey(key) ? properties[key] : null;
        }

        public bool SetProperty(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning($"[WorldObject] Cannot set property with empty key");
                return false;
            }

            string oldValue = GetProperty(key);
            properties[key] = value;

            Debug.Log($"[WorldObject] {objectName}: '{key}' = '{value}' (이전: '{oldValue}')");

            UpdateDescription();
            OnPropertyChanged?.Invoke(this, key, oldValue, value);
            ApplyVisualEffect(key, value);

            return true;
        }

        public void SetProperties(Dictionary<string, string> newProperties)
        {
            foreach (var kvp in newProperties)
            {
                SetProperty(kvp.Key, kvp.Value);
            }
        }

        public string GetAllPropertiesAsString()
        {
            if (properties.Count == 0)
                return "속성 없음";

            return string.Join(", ", properties.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
        }

        public IReadOnlyDictionary<string, string> Properties => properties;

        public bool HasProperty(string key)
        {
            return properties.ContainsKey(key);
        }

        public bool RemoveProperty(string key)
        {
            bool removed = properties.Remove(key);
            if (removed)
            {
                UpdateDescription();
                Debug.Log($"[WorldObject] {objectName}: Property '{key}' removed");
            }
            return removed;
        }

        public void UpdateFromNaturalLanguage(string naturalDescription)
        {
            currentDescription = naturalDescription;
            Debug.Log($"[WorldObject] {objectName} description updated: {naturalDescription}");
        }

        public string GetAIDescription()
        {
            string desc = $"{objectName}";
            
            if (!string.IsNullOrEmpty(currentDescription))
            {
                desc += $": {currentDescription}";
            }
            
            if (properties.Count > 0)
            {
                desc += $" [{GetAllPropertiesAsString()}]";
            }
            
            return desc;
        }

        public string GetState(string stateName)
        {
            return GetProperty(stateName) ?? "unknown";
        }

        public bool SetState(string stateName, string newValue)
        {
            return SetProperty(stateName, newValue);
        }

        public string GetAllStatesAsString()
        {
            return GetAllPropertiesAsString();
        }

        public string GetDescription()
        {
            return GetAIDescription();
        }

        private void UpdateDescription()
        {
            if (properties.Count > 0)
            {
                currentDescription = GetAllPropertiesAsString();
            }
        }

        private void ApplyVisualEffect(string propertyKey, string value)
        {
            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            if (renderer == null) return;

            string lowerKey = propertyKey.ToLower();
            string lowerValue = value.ToLower();

            if (lowerKey.Contains("power") || lowerKey.Contains("전원"))
            {
                if (lowerValue.Contains("on") || lowerValue.Contains("켜짐") || lowerValue.Contains("켜져"))
                {
                    renderer.color = Color.white;
                }
                else if (lowerValue.Contains("off") || lowerValue.Contains("꺼짐") || lowerValue.Contains("꺼져"))
                {
                    renderer.color = new Color(0.3f, 0.3f, 0.3f);
                }
            }
            else if (lowerKey.Contains("brightness") || lowerKey.Contains("밝기"))
            {
                if (lowerValue.Contains("밝") || lowerValue.Contains("bright"))
                {
                    renderer.color = Color.white;
                }
                else if (lowerValue.Contains("어둡") || lowerValue.Contains("dark") || lowerValue.Contains("dim"))
                {
                    renderer.color = new Color(0.5f, 0.5f, 0.5f);
                }
            }
            else if (lowerKey.Contains("cleanliness") || lowerKey.Contains("청결") || lowerKey.Contains("상태"))
            {
                if (lowerValue.Contains("더럽") || lowerValue.Contains("dirty") || lowerValue.Contains("messy"))
                {
                    renderer.color = new Color(0.7f, 0.7f, 0.6f);
                }
                else if (lowerValue.Contains("깨끗") || lowerValue.Contains("clean"))
                {
                    renderer.color = Color.white;
                }
            }
            else if (lowerKey.Contains("color") || lowerKey.Contains("색"))
            {
                if (lowerValue.Contains("빨강") || lowerValue.Contains("red"))
                {
                    renderer.color = Color.red;
                }
                else if (lowerValue.Contains("파랑") || lowerValue.Contains("blue"))
                {
                    renderer.color = Color.blue;
                }
                else if (lowerValue.Contains("노랑") || lowerValue.Contains("yellow"))
                {
                    renderer.color = Color.yellow;
                }
                else if (lowerValue.Contains("초록") || lowerValue.Contains("green"))
                {
                    renderer.color = Color.green;
                }
            }
        }

        public event Action<WorldObject, string, string, string> OnPropertyChanged;

        private void OnDrawGizmos()
        {
            if (!isInteractable) return;

            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, interactionRange);
        }
    }

    public enum ObjectType
    {
        Generic,
        Furniture,
        Appliance,
        Door,
        Light,
        Container,
        Decoration,
        Food,
        Tool
    }

    [Serializable]
    public class PropertyPair
    {
        [Tooltip("속성 이름 (예: 밝기, 청결도, 온도 등)")]
        public string key = "";
        
        [Tooltip("속성 값 (자유 형식: 예: '매우 밝음', '깨끗함', '따뜻함')")]
        public string value = "";
    }
}
