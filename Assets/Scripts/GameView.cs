using UnityEngine;
using UnityEngine.UI;

namespace WordBubblePop
{
    /// <summary>
    /// Links the game code to the objects in the scene.
    /// Every object listed here is a normal Unity object in the Hierarchy:
    /// you can move it, resize it, recolour it or change its text in the Inspector.
    /// Only change these links if you replace an object with a new one.
    /// </summary>
    public class GameView : MonoBehaviour
    {
        [Header("Layout")]
        public CanvasScaler scaler;
        public RectTransform safeArea;
        public RectTransform column;
        [Tooltip("Widest the game column may get (design width)")]
        public float columnMaxWidth = 1080f;

        [Header("Top bar")]
        public Text levelText;
        public Text starText;
        public Button soundButton;
        public Text soundText;

        [Header("Word card")]
        public Button pictureButton;
        public Image picture;
        public RectTransform slotsRow;
        [Tooltip("Look of one letter box. It is copied for every letter of the word.")]
        public Image slotTemplate;
        [Tooltip("Gap between letter boxes")]
        public float slotGap = 18f;
        public Image timerFill;
        public Text timerText;
        public Image[] progressDots;
        public Color dotDone = new Color32(0x2F, 0x8F, 0x5B, 0xFF);
        public Color dotCurrent = new Color32(0xFF, 0xC9, 0x3C, 0xFF);
        public Color dotTodo = new Color32(0x1D, 0x5E, 0x3C, 0x2E);
        public CanvasGroup earned;
        public Text earnedText;

        [Header("River")]
        [Tooltip("Bubbles float inside this area")]
        public RectTransform bubbleField;
        public Sprite splashSprite;
        public Color splashColor = new Color32(0xFF, 0xC9, 0x3C, 0xFF);

        [Header("Start screen")]
        public GameObject startScreen;
        public Button playButton;
        public Text playText;
        public Button resetButton;

        [Header("Level complete screen")]
        public GameObject levelScreen;
        public Text levelTitle;
        public Text levelStarsText;
        public RectTransform learnedGrid;
        [Tooltip("Look of one learned word. It is copied for every word.")]
        public GameObject learnedTemplate;
        public Button nextButton;
        public Color[] confettiColors =
        {
            new Color32(0xFF, 0x8F, 0xA3, 0xFF), new Color32(0x7B, 0xDF, 0xF2, 0xFF),
            new Color32(0x9B, 0xE5, 0x8A, 0xFF), new Color32(0xFF, 0xC9, 0x3C, 0xFF),
            new Color32(0xFF, 0x6F, 0x59, 0xFF)
        };

        [Header("Time's up screen")]
        public GameObject overScreen;
        public Image answerPicture;
        public Text answerWord;
        public Text overInfo;
        public Button retryButton;
    }
}
