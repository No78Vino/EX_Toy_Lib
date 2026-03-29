using System;
using System.IO;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace ExOpenSource.Editor
{
    [Serializable]
    public struct RepoInfo
    {
        [LabelText("目录Git仓库用户名")] [LabelWidth(150)] [PropertyOrder(0)]
        public string userName;

        [LabelText("目录Git仓库名")] [LabelWidth(150)] [PropertyOrder(0)]
        public string repoName;

        [LabelText("目录Git分支名")] [LabelWidth(150)] [PropertyOrder(0)]
        public string branch;

        [LabelText("远端菜单路径")] [LabelWidth(150)] [PropertyOrder(0)]
        public string remoteMenuPath;

        [Space]
        [Delayed]
        [LabelText("本地菜单路径")]
        [LabelWidth(150)]
        [PropertyOrder(2)]
        [InlineButton(nameof(LoadMenu), SdfIconType.Download, "下载目录", ShowIf = "@!ExistMenuJson()")]
        [InlineButton(nameof(UpdateMenu), SdfIconType.ArrowUpCircle, "更新目录", ShowIf = nameof(ExistMenuJson))]
        [InlineButton(nameof(OpenMenuInExplore), SdfIconType.Folder, "打开目录所在文件夹", ShowIf = nameof(ExistMenuJson))]
        public string localMenuPath;

        [HideLabel]
        [ShowInInspector]
        [DisplayAsString(EnableRichText = true)]
        [PropertyOrder(1)]
        public string rawContentGitRepoUrl
        {
            get
            {
                var gitRepoUrl = ExOpenSourceNetworkHelper.FormatGitHubUrl(userName, repoName, branch, remoteMenuPath);
                return $"<color={ExBrandStyle.HexTextPrimary}>下载内容地址(Raw URL):{gitRepoUrl}</color>";
            }
        }

        [ShowIf(nameof(ExistMenuJson))]
        [ShowInInspector]
        [DisplayAsString(EnableRichText = true)]
        [HideLabel]
        [PropertyOrder(4)]
        private string MenuVersion
        {
            get
            {
                var version = "--";
                var fullPath = Path.Combine(Application.dataPath, "../", localMenuPath);
                if (!File.Exists(fullPath)) Debug.LogWarning($"找不到目录配置文件: {fullPath}");
                try
                {
                    var json = File.ReadAllText(fullPath);
                    var config = JsonUtility.FromJson<ExMenuConfig>(json);
                    version = config.Version;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"解析配置文件失败: {e.Message}");
                }

                return $"<color={ExBrandStyle.HexTextPrimary}>菜单目录版本: {version}</color>";
            }
        }

        public void LoadMenu()
        {
            ExOpenSourceNetworkHelper.DownloadFile(GetGitFileDownloadConfig());
        }

        public void UpdateMenu()
        {
            LoadMenu();
        }

        /// <summary>
        ///     打开菜单目录在资源管理器中
        /// </summary>
        public void OpenMenuInExplore()
        {
            var fullPath = Path.Combine(Application.dataPath, "../", localMenuPath);
            if (File.Exists(fullPath))
                EditorUtility.RevealInFinder(fullPath);
            else
                Debug.LogWarning($"菜单文件不存在: {fullPath}");
        }

        private bool ExistMenuJson()
        {
            var isExist = File.Exists(Path.Combine(Application.dataPath, "../", localMenuPath));
            return isExist;
        }

        private GitFileDownloadConfig GetGitFileDownloadConfig()
        {
            return new GitFileDownloadConfig
            {
                Username = userName,
                Repository = repoName,
                Branch = branch,
                RemoteFilePath = remoteMenuPath,
                LocalFilePath = localMenuPath,
                GitToken = ExOpenSourcePluginManagerSetting.Instance.ReadToken()
            };
        }
        
        public static bool Equal(RepoInfo a,RepoInfo b)
        {
            return a.repoName == b.repoName
                   && a.userName == b.userName
                   && a.branch == b.branch
                   && a.remoteMenuPath == b.remoteMenuPath;
        }
    }
}
