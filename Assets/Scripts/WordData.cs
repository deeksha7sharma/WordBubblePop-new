using System.Collections.Generic;
using UnityEngine;

namespace WordBubblePop
{
    /// <summary>
    /// Picks the words for a level from the list in GameSettings.
    /// The default word list below is only used once, when the scene is first built.
    /// </summary>
    public static class WordData
    {
        public static readonly string[] DefaultWords =
        {
            "CAT", "DOG", "SUN", "COW", "BUS", "EGG", "HAT", "CUP", "BEE", "FOX", "OWL", "PIG", "BED", "KEY", "ANT",
            "FISH", "BALL", "TREE", "MOON", "CAKE", "DUCK", "FROG", "LION", "BOOK", "STAR", "BEAR", "KITE", "MILK", "BOAT", "HAND",
            "APPLE", "MANGO", "HORSE", "TIGER", "HOUSE", "TRAIN", "CHAIR", "GRAPE", "SNAKE", "ZEBRA", "CLOCK", "PLANT", "TEETH", "SHEEP"
        };

        /// <summary>Level 1 = 3 letters, 2 = 4 letters, 3 = 5 letters, 4+ = any length.</summary>
        public static List<WordEntry> PickWords(List<WordEntry> all, int level, int count)
        {
            var valid = all.FindAll(w => w != null && !string.IsNullOrWhiteSpace(w.word));
            var pool = new List<WordEntry>();
            if (level <= 3)
            {
                int length = level + 2;
                pool = valid.FindAll(w => w.word.Trim().Length == length);
            }
            if (pool.Count == 0) pool = valid;

            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            var result = pool.GetRange(0, Mathf.Min(count, pool.Count));
            // Short list? Repeat words so the level still has enough.
            int k = 0;
            while (result.Count < count && pool.Count > 0) result.Add(pool[k++ % pool.Count]);
            return result;
        }

        public static string LevelInfo(int level)
        {
            return level <= 3 ? (level + 2) + " letters" : "mixed words";
        }
    }
}
