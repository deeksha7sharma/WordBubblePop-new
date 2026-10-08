using System;
using System.Collections.Generic;
using UnityEngine;

namespace WordBubblePop
{
    [Serializable]
    public class WordEntry
    {
        [Tooltip("The word in CAPITAL letters, for example DOG")]
        public string word;

        [Tooltip("Picture shown for this word")]
        public Sprite picture;
    }

    /// <summary>
    /// All game settings in one place. Select Assets/Settings/GameSettings
    /// and change anything in the Inspector: no code needed.
    /// </summary>
    [CreateAssetMenu(fileName = "GameSettings", menuName = "Word Bubble Pop/Game Settings")]
    public class GameSettings : ScriptableObject
    {
        [Header("Timer and levels")]
        [Tooltip("Seconds allowed for each word")]
        [Range(5, 120)] public float wordTime = 20f;

        [Tooltip("How many words in one level")]
        [Range(1, 20)] public int wordsPerLevel = 5;

        [Header("Bubbles")]
        [Tooltip("Bubble speed on level 1 (higher = faster)")]
        public float baseSpeed = 34f;

        [Tooltip("Extra speed added for every level")]
        public float speedPerLevel = 6f;

        [Tooltip("Seconds between new bubbles on level 1")]
        public float spawnInterval = 1.2f;

        [Tooltip("New bubbles come this much faster every level")]
        public float spawnFasterPerLevel = 0.07f;

        [Tooltip("Shortest possible time between bubbles")]
        public float spawnIntervalMin = 0.65f;

        [Tooltip("Bubbles on screen at level 1 (one more every level)")]
        public int maxBubblesStart = 5;

        [Tooltip("Never more bubbles than this on screen")]
        public int maxBubblesLimit = 9;

        [Tooltip("Smallest and largest bubble size")]
        public float bubbleSizeMin = 120f;
        public float bubbleSizeMax = 200f;

        [Tooltip("Bubble colours (one is picked at random for each bubble)")]
        public Color[] bubbleColors =
        {
            new Color32(0xFF, 0x8F, 0xA3, 0xFF), new Color32(0x7B, 0xDF, 0xF2, 0xFF),
            new Color32(0x9B, 0xE5, 0x8A, 0xFF), new Color32(0xFF, 0xD3, 0x6E, 0xFF),
            new Color32(0xC8, 0xA8, 0xFF, 0xFF), new Color32(0xFF, 0xB3, 0x8A, 0xFF)
        };

        [Tooltip("After this many wrong taps on one letter, the right bubble glows")]
        [Range(1, 10)] public int hintAfterMistakes = 2;

        [Header("Letter boxes")]
        public Color slotEmptyColor = Color.white;
        public Color slotFilledColor = new Color32(0xFF, 0xC9, 0x3C, 0xFF);
        public Color slotEmptyBorder = new Color32(0x9E, 0xC7, 0xB0, 0xFF);
        public Color slotCurrentBorder = new Color32(0xFF, 0x6F, 0x59, 0xFF);
        public Color slotFilledBorder = new Color32(0xE0, 0xA4, 0x00, 0xFF);

        [Header("Timer bar")]
        public Color timerColor = new Color32(0x2F, 0x8F, 0x5B, 0xFF);
        public Color timerLowColor = new Color32(0xFF, 0x6F, 0x59, 0xFF);
        [Tooltip("Timer turns red and ticks in the last seconds")]
        public float timerWarningSeconds = 5f;

        [Header("Music and sounds")]
        public AudioClip music;
        [Range(0, 1)] public float musicVolume = 0.35f;
        public AudioClip popSound;
        public AudioClip wrongSound;
        public AudioClip tickSound;
        public AudioClip winSound;
        public AudioClip gameOverSound;
        [Range(0, 1)] public float soundVolume = 0.8f;

        [Header("Voice")]
        [Tooltip("Speak letters and words out loud")]
        public bool speakWords = true;
        [Range(0.3f, 1.5f)] public float voiceSpeed = 0.8f;

        [Header("Words")]
        [Tooltip("Level 1 uses 3-letter words, level 2 four letters, level 3 five letters, then a mix")]
        public List<WordEntry> words = new List<WordEntry>();
    }
}
