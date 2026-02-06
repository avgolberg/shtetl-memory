using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HisaGames.EasyChara
{
    /// <summary>
    /// Ensures EventSystem uses the correct UI input module depending on what the project supports.
    /// - If new Input System package exists -> use InputSystemUIInputModule
    /// - Else -> fallback to StandaloneInputModule (legacy)
    ///
    /// Compatible with Unity 2021 LTS up to Unity 6+.
    /// No hard dependency on com.unity.inputsystem (uses reflection).
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EventSystem))]
    public class EasyCharaEventSystemAutoFix : MonoBehaviour
    {
        [Tooltip("Logs chosen module once when the scene loads.")]
        public bool verboseLog = false;

        // Cache reflection result so we don't repeatedly call Type.GetType
        static Type _inputSystemUIModuleType;

        void Reset()
        {
            // Helpful: ensure there is only one EventSystem in the demo scene.
            // (We don't enforce delete; just warn in logs.)
        }

        void Awake()
        {
            EnsureCorrectInputModule();
        }

#if UNITY_EDITOR
        // Also run in editor when scene changes / domain reload,
        // so the demo scene self-heals without pressing Play.
        void OnValidate()
        {
            // Avoid running during import spam; keep it lightweight.
            if (!isActiveAndEnabled) return;
            EnsureCorrectInputModule();
        }
#endif

        void EnsureCorrectInputModule()
        {
            // Detect if InputSystemUIInputModule exists (Input System package installed)
            _inputSystemUIModuleType ??=
                Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");

            bool hasNewInputSystem = (_inputSystemUIModuleType != null);

            if (hasNewInputSystem)
            {
                // Remove legacy module if present
                var legacy = GetComponent<StandaloneInputModule>();
                if (legacy != null)
                    DestroyComponent(legacy);

                // Add new module if missing
                if (GetComponent(_inputSystemUIModuleType) == null)
                    gameObject.AddComponent(_inputSystemUIModuleType);

                if (verboseLog) Debug.Log("[EasyChara] EventSystem: Using InputSystemUIInputModule (new).", this);
            }
            else
            {
                // Remove new module if present (in case someone imported scene into a legacy-only project)
                RemoveIfExists("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");

                // Ensure legacy module exists
                if (GetComponent<StandaloneInputModule>() == null)
                    gameObject.AddComponent<StandaloneInputModule>();

                if (verboseLog) Debug.Log("[EasyChara] EventSystem: Using StandaloneInputModule (legacy).", this);
            }
        }

        void RemoveIfExists(string assemblyQualifiedTypeName)
        {
            var t = Type.GetType(assemblyQualifiedTypeName);
            if (t == null) return;

            var c = GetComponent(t);
            if (c != null)
                DestroyComponent(c);
        }

        static void DestroyComponent(Component c)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) UnityEngine.Object.DestroyImmediate(c);
            else UnityEngine.Object.Destroy(c);
#else
            UnityEngine.Object.Destroy(c);
#endif
        }
    }
}