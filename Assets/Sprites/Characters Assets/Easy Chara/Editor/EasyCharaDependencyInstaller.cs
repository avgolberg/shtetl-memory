#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace HisaGames.EasyChara.Editor
{
    /// <summary>
    /// Prompts the user to install required/optional packages for Easy Chara after import.
    /// Works for Unity 2021 LTS -> Unity 6.
    /// Asset Store safe: asks permission, does not silently modify the project.
    /// </summary>
    [InitializeOnLoad]
    public static class EasyCharaDependencyInstaller
    {
        const string SessionKeyPrompted = "HisaGames.EasyChara.DependencyInstaller.PromptedOnce";
        const string PrefKeyAutoPrompt = "HisaGames.EasyChara.DependencyInstaller.AutoPrompt";

        // Packages we want
        static readonly string[] RequiredPackages =
        {
            "com.unity.2d.sprite",
            "com.unity.2d.animation"
        };

        static ListRequest _listRequest;
        static AddRequest _addRequest;
        static string _installingPackageId;
        static string[] _missing;

        static EasyCharaDependencyInstaller()
        {
            // Only run once per editor session by default
            if (SessionState.GetBool(SessionKeyPrompted, false))
                return;

            // Allow the user to disable auto prompt
            bool autoPrompt = EditorPrefs.GetBool(PrefKeyAutoPrompt, true);
            if (!autoPrompt)
                return;

            SessionState.SetBool(SessionKeyPrompted, true);

            // Delay so it runs after editor finishes compiling/importing
            EditorApplication.delayCall += CheckAndPrompt;
        }

        [MenuItem("Tools/Hisa Games/Easy Chara/Dependencies/Check and Install")]
        public static void CheckAndPrompt()
        {
            _listRequest = Client.List(true); // include indirect
            EditorApplication.update += OnListProgress;
        }

        static void OnListProgress()
        {
            if (_listRequest == null) return;
            if (!_listRequest.IsCompleted) return;

            EditorApplication.update -= OnListProgress;

            if (_listRequest.Status != StatusCode.Success)
            {
                Debug.LogWarning($"[EasyChara] Package list failed: {_listRequest.Error?.message}");
                return;
            }

            var installed = _listRequest.Result.Select(p => p.name).ToHashSet();
            _missing = RequiredPackages.Where(p => !installed.Contains(p)).ToArray();

            if (_missing.Length == 0)
            {
                if (!SessionState.GetBool("HisaGames.EasyChara.DependencyInstaller.LoggedOk", false))
                {
                    SessionState.SetBool("HisaGames.EasyChara.DependencyInstaller.LoggedOk", true);
                    Debug.Log("[EasyChara] Dependencies OK: 2D Sprite + 2D Animation are already installed.");
                }
                return;
            }

            string missingList = string.Join("\n- ", _missing);

            int choice = EditorUtility.DisplayDialogComplex(
                "Easy Chara � Missing Unity Packages",
                "This project is missing packages recommended for Easy Chara:\n\n- " + missingList +
                "\n\nInstall them now via Package Manager?",
                "Install",
                "Not now",
                "Don't ask again"
            );

            if (choice == 2)
            {
                EditorPrefs.SetBool(PrefKeyAutoPrompt, false);
                Debug.Log("[EasyChara] Dependency auto-prompt disabled. You can run it manually via menu.");
                return;
            }

            if (choice == 0)
            {
                InstallNextMissing();
            }
            else
            {
                Debug.Log("[EasyChara] Dependencies not installed (user chose Not now).");
            }
        }

        static void InstallNextMissing()
        {
            if (_missing == null || _missing.Length == 0)
                return;

            _installingPackageId = _missing[0];
            _missing = _missing.Skip(1).ToArray();

            Debug.Log($"[EasyChara] Installing package: {_installingPackageId}");
            _addRequest = Client.Add(_installingPackageId);
            EditorApplication.update += OnAddProgress;
        }

        static void OnAddProgress()
        {
            if (_addRequest == null) return;
            if (!_addRequest.IsCompleted) return;

            EditorApplication.update -= OnAddProgress;

            if (_addRequest.Status == StatusCode.Success)
            {
                Debug.Log($"[EasyChara] Installed: {_installingPackageId}");

                // Install the next missing package (if any)
                InstallNextMissing();
            }
            else
            {
                Debug.LogWarning($"[EasyChara] Failed to install {_installingPackageId}: {_addRequest.Error?.message}");
            }
        }

        [MenuItem("Tools/Hisa Games/Easy Chara/Dependencies/Enable Auto Prompt")]
        public static void EnableAutoPrompt() => EditorPrefs.SetBool(PrefKeyAutoPrompt, true);

        [MenuItem("Tools/Hisa Games/Easy Chara/Dependencies/Disable Auto Prompt")]
        public static void DisableAutoPrompt() => EditorPrefs.SetBool(PrefKeyAutoPrompt, false);
    }
}
#endif
