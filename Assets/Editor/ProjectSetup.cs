using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace WordBubblePop.EditorTools
{
    /// <summary>
    /// First time the project opens: sets the app name, app ID, portrait mode and icon,
    /// builds the game scene and opens it.
    /// You can run these again from the menu: Word Bubble Pop.
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        const string AppId = "com.deeksha.wordbubblepop";
        const string DoneKey = "WordBubblePop.SetupChecked";

        static ProjectSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (SessionState.GetBool(DoneKey, false) || EditorApplication.isPlayingOrWillChangePlaymode) return;
                SessionState.SetBool(DoneKey, true);

                if (PlayerSettings.productName != "Word Bubble Pop") ApplyPlayerSettings();

                if (!File.Exists(SceneBuilder.ScenePath))
                {
                    SceneBuilder.Build();
                }
                else if (EditorSceneManager.GetActiveScene().path != SceneBuilder.ScenePath &&
                         string.IsNullOrEmpty(EditorSceneManager.GetActiveScene().path))
                {
                    EditorSceneManager.OpenScene(SceneBuilder.ScenePath);
                }
            };
        }

        [MenuItem("Word Bubble Pop/Apply Project Settings", false, 40)]
        public static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = "Deeksha";
            PlayerSettings.productName = "Word Bubble Pop";
            PlayerSettings.bundleVersion = "1.0.0";

            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AppId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, AppId);

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            PlayerSettings.SplashScreen.backgroundColor = new Color32(0xBF, 0xE9, 0xF2, 0xFF);

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/AppIcon.png");
            if (icon != null)
                PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);

            AssetDatabase.SaveAssets();
            Debug.Log("Word Bubble Pop: project settings applied (name, app ID " + AppId + ", portrait, icon).");
        }
    }
}
