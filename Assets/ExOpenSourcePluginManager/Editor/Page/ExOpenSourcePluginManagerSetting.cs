using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using Sirenix.Utilities.Editor;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace ExOpenSource.Editor
{
    public enum ConnectionStatus
    {
        [LabelText("待检测")] Pending,
        [LabelText("检测中")] Checking,
        [LabelText("连接成功")] Success,
        [LabelText("连接失败")] Failed
    }

    [PLFilePath(ExOpenSourceConstParam.SETTING_ASSET_PATH)]
    public class ExOpenSourcePluginManagerSetting : ExScriptableSingleton<ExOpenSourcePluginManagerSetting>
    {
        // ════════════════════════════════════════════════════════
        //  Tab: 令牌配置
        // ════════════════════════════════════════════════════════
        [TabGroup("Tabs", "令牌配置", Order = 0)]
        [InfoBox(ExOpenSourceConstParam.REPO_TOKEN_INTRO)]
        [LabelText(ExOpenSourceConstParam.REPO_TOKEN_FILE_PATH)]
        [LabelWidth(150)]
        [Sirenix.OdinInspector.FilePath(RequireExistingPath = true, Extensions = "txt", AbsolutePath = true,
            IncludeFileExtension = true)]
        [InlineButton(nameof(ReadToken), SdfIconType.Upload, "加载令牌")]
        public string TokenFilePath = "";

        [TabGroup("Tabs", "令牌配置")]
        [ShowInInspector]
        [DisplayAsString(EnableRichText = true)]
        [LabelText(ExOpenSourceConstParam.REPO_TOKEN)]
        [LabelWidth(150)]
        private string showToken = "";

        // ════════════════════════════════════════════════════════
        //  Tab: 仓库管理
        // ════════════════════════════════════════════════════════
        [TabGroup("Tabs", "仓库管理", Order = 1)]
        [Title(ExOpenSourceConstParam.REPO_CONNECTION_TITLE)]
        [LabelText(" ")]
        [ListDrawerSettings(OnTitleBarGUI = nameof(DrawAddDefaultMenuButton))]
        public List<RepoInfo> repoInfos = new List<RepoInfo>(ExOpenSourceConstParam.OfficialRepoInfos);

        // ════════════════════════════════════════════════════════
        //  Tab: 网络检测
        // ════════════════════════════════════════════════════════
        [TabGroup("Tabs", "网络检测", Order = 2)]
        [ShowInInspector, HideLabel]
        [DisplayAsString(EnableRichText = true)]
        [PropertyOrder(3)]
        private string connectionInfo =>
            $"<color={ExBrandStyle.HexAccent}>状态:{connectionStatus}</color>  响应时间:{responseTime:0.00}ms";

        [TabGroup("Tabs", "网络检测")]
        [GUIColor(0.85f, 1f, 0.85f)]
        [ShowInInspector, HideLabel, ReadOnly]
        [TextArea(1, 20)]
        [PropertyOrder(2)]
        private string connectionMessage = "准备测试连接";

        [TabGroup("Tabs", "网络检测")]
        [Button("检测网络连接", ButtonSizes.Large, Icon = SdfIconType.Wifi)]
        [PropertyOrder(1)]
        public void CheckConnectionToGitRepo()
        {
            if (isTestingConnection) return;

            connectionStatus = ConnectionStatus.Checking;
            connectionMessage = "正在测试连接...";
            connectionStartTime = EditorApplication.timeSinceStartup;
            debugUrl = FormatGitHubUrl(ExOpenSourceConstParam.GIT_REPO_RAW_URL, ExOpenSourceConstParam.DEFAULT_MENU_PATH);

            isTestingConnection = true;
            EditorCoroutineUtility.StartCoroutineOwnerless(TestConnectionCoroutine());
        }

        // ── 内部状态 ─────────────────────────────────────────────
        private double connectionStartTime;
        private ConnectionStatus connectionStatus = ConnectionStatus.Pending;
        private string debugUrl;
        private bool isTestingConnection;
        private double responseTime;

        private void OnDisable()  => Save();
        private void OnDestroy()  => Save();
        private void OnValidate() => Save();

        // ── 网络检测协程 ─────────────────────────────────────────
        private IEnumerator TestConnectionCoroutine()
        {
            using (var request = UnityWebRequest.Head(debugUrl))
            {
                request.timeout = 10;
                if (!string.IsNullOrEmpty(ReadToken()))
                    request.SetRequestHeader("Authorization", $"token {ReadToken()}");
                else
                    request.SetRequestHeader("User-Agent", "UnityEditor/" + Application.unityVersion);

                var operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    connectionMessage = $"正在连接... ({Mathf.FloorToInt(request.downloadProgress * 100)}%)";
                    yield return null;
                }

                var totalTime = (EditorApplication.timeSinceStartup - connectionStartTime) * 1000;
                responseTime = totalTime;

                if (request.result == UnityWebRequest.Result.Success)
                {
                    connectionStatus = ConnectionStatus.Success;
                    connectionMessage = $"测试地址:{debugUrl}\n连接成功！\nHTTP 状态: {request.responseCode}\n响应时间: {totalTime:0} ms";
                }
                else
                {
                    connectionStatus = ConnectionStatus.Failed;
                    connectionMessage = HandleConnectionError(request, debugUrl);
                }

                isTestingConnection = false;
            }
        }

        private string HandleConnectionError(UnityWebRequest request, string url)
        {
            return request.responseCode switch
            {
                401 or 403 => $"测试地址:{url}\n访问被拒绝 (HTTP {request.responseCode})\n• 私有仓库？请添加 GitHub 令牌",
                404 => $"测试地址:{url}\n文件未找到/GitHub令牌错误 (HTTP 404)\n• 检查文件路径: {ExOpenSourceConstParam.DEFAULT_MENU_PATH}\n• 检查GitHub令牌是否输入正确，或者令牌已过期",
                429 => $"测试地址:{url}\n⚠GitHub 速率限制\n• 添加 GitHub 令牌可提高限制",
                _ when request.result == UnityWebRequest.Result.ConnectionError =>
                    $"测试地址:{url}\n网络连接失败\n请检查网络连接，DNS是否正常。",
                _ => $"测试地址:{url}\n连接失败: {request.error} (HTTP {request.responseCode})"
            };
        }

        private string FormatGitHubUrl(string repoUrl, string path)
        {
            var encodedPath = UnityWebRequest.EscapeURL(path.Replace(" ", "%20"))
                .Replace("%3A", ":").Replace("%2F", "/").Replace("%5C", "/");
            return $"{repoUrl}/{encodedPath}";
        }

        public string ReadToken()
        {
            if (string.IsNullOrEmpty(TokenFilePath))
            {
                showToken = "--";
                return string.Empty;
            }

            try
            {
                if (File.Exists(TokenFilePath))
                {
                    EditorWindow.focusedWindow?.Repaint();
                    var t = File.ReadAllText(TokenFilePath).Trim();
                    showToken = $"<color={ExBrandStyle.HexAccent}>{t}</color>";
                    return t;
                }

                Debug.LogWarning($"令牌文件不存在: {TokenFilePath}");
                EditorWindow.focusedWindow?.ShowNotification(new GUIContent($"令牌文件不存在: {TokenFilePath}"));
                EditorWindow.focusedWindow?.Repaint();
                showToken = "--";
                return string.Empty;
            }
            catch (Exception e)
            {
                Debug.LogError($"读取令牌失败: {e.Message}");
                EditorWindow.focusedWindow?.ShowNotification(new GUIContent($"读取令牌失败: {e.Message}"));
                EditorWindow.focusedWindow?.Repaint();
                showToken = "--";
                return string.Empty;
            }
        }

        private void DrawAddDefaultMenuButton()
        {
            if (SirenixEditorGUI.ToolbarButton(new GUIContent("添加默认库")))
            {
                foreach (var repo in ExOpenSourceConstParam.OfficialRepoInfos)
                {
                    if (repoInfos.Exists(r => r.userName == repo.userName && r.repoName == repo.repoName))
                    {
                        Debug.LogWarning($"已存在仓库: {repo.userName}/{repo.repoName}");
                        continue;
                    }
                    repoInfos.Add(repo);
                    Debug.Log($"添加默认仓库: {repo.userName}/{repo.repoName}");
                }
            }
        }
    }
}
