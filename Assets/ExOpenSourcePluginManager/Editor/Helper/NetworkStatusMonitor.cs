using System;
using System.Collections;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace ExOpenSource.Editor
{
    /// <summary>
    /// 异步检测 GitHub 网络连通性，结果缓存供各页面使用
    /// </summary>
    [InitializeOnLoad]
    public static class NetworkStatusMonitor
    {
        private const string CHECK_URL = "https://raw.githubusercontent.com";
        private const float CHECK_TIMEOUT = 8f;
        private const float RECHECK_INTERVAL = 60f; // 1分钟重新检测一次

        public static bool IsOnline { get; private set; } = true;
        public static bool HasChecked { get; private set; } = false;

        public static event Action<bool> OnStatusChanged;

        private static double _lastCheckTime = -999;

        static NetworkStatusMonitor()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnEditorUpdate()
        {
            if (HasChecked && EditorApplication.timeSinceStartup - _lastCheckTime < RECHECK_INTERVAL)
                return;

            _lastCheckTime = EditorApplication.timeSinceStartup;
            EditorCoroutineUtility.StartCoroutineOwnerless(CheckConnectionCoroutine());
        }

        public static void ForceRecheck()
        {
            _lastCheckTime = -999;
        }

        private static IEnumerator CheckConnectionCoroutine()
        {
            using (var request = UnityWebRequest.Head(CHECK_URL))
            {
                request.timeout = (int)CHECK_TIMEOUT;
                request.SetRequestHeader("User-Agent", "UnityEditor/" + Application.unityVersion);

                yield return request.SendWebRequest();

                bool online = request.result == UnityWebRequest.Result.Success
                              || request.responseCode > 0; // 有响应码即表示网络通

                if (online != IsOnline || !HasChecked)
                {
                    IsOnline = online;
                    HasChecked = true;
                    OnStatusChanged?.Invoke(IsOnline);
                }

                HasChecked = true;
            }
        }
    }
}
