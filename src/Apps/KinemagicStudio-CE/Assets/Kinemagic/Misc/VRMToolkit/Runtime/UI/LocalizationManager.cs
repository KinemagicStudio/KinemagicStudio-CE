using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace VRMToolkit.UI
{
    public static class LocalizationManager
    {
        public const string EnglishDictionaryResourcePath = "VRMToolkit/Localization/English";
        public const string JapaneseDictionaryResourcePath = "VRMToolkit/Localization/Japanese";

        public enum Language
        {
            English,
            Japanese
        }

        private static readonly Dictionary<Language, Dictionary<string, string>> _dictionaries = new();

        private static Dictionary<string, string> _currentLanguageDictionary;
        private static Language _currentLanguage = Language.English;
        private static bool _initialized = false;

        public static Language CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (_currentLanguage != value)
                {
                    _currentLanguage = value;
                    _currentLanguageDictionary = _dictionaries.ContainsKey(value) ? _dictionaries[value] : null;
                }
            }
        }

        public static void Initialize()
        {
            if (_initialized) return;

            var englishDictionaryAsset = Resources.Load<TextAsset>(EnglishDictionaryResourcePath);
            if (englishDictionaryAsset != null)
            {
                var englishDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(englishDictionaryAsset.text);
                _dictionaries[Language.English] = englishDictionary;
            }

            var japaneseDictionaryAsset = Resources.Load<TextAsset>(JapaneseDictionaryResourcePath);
            if (japaneseDictionaryAsset != null)
            {
                var japaneseDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(japaneseDictionaryAsset.text);
                _dictionaries[Language.Japanese] = japaneseDictionary;
            }

            _currentLanguageDictionary = _dictionaries[Language.English];
            _currentLanguage = Language.English;

            _initialized = true;
        }

        public static string GetText(string key)
        {
            if (!_initialized) Initialize();

            if (_currentLanguageDictionary != null && _currentLanguageDictionary.TryGetValue(key, out string value))
            {
                return value;
            }

            return key;
        }
    }
}
