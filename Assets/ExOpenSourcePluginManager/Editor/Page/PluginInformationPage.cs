using System.IO;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace ExOpenSource.Editor
{
    public class PluginInformationPage
    {
        private ExMenuConfig _menuConfig;
        private ExPluginItem _pluginItem;

        // 下载进度状态
        private bool _isDownloading;
        private DownloadProgressInfo _progress;
        private string _lastError;

        [TabGroup("Tab", "基础信息", order: 2)]
        [PropertyOrder(10)]
        [ShowInInspector, HideLabel]
        public ExPluginItem PluginItem => _pluginItem;

        public PluginInformationPage(ExPluginItem pluginItem, ExMenuConfig menuConfig)
        {
            _pluginItem = pluginItem;
            _menuConfig = menuConfig;
        }

        // ── 离线提示横幅 ────────────────────────────────────────
        [TabGroup("Tab", "本地信息")]
        [PropertyOrder(-10)]
        [ShowInInspector, HideLabel]
        [DisplayAsString(EnableRichText = true)]
        [ShowIf(nameof(ShowOfflineBanner))]
        private string OfflineBanner =>
            $"<color={ExBrandStyle.HexWarning}>⚠  当前处于离线状态，下载功能不可用。请检查网络连接。</color>";

        private bool ShowOfflineBanner => !NetworkStatusMonitor.IsOnline && NetworkStatusMonitor.HasChecked;

        // ── 本地路径和状态 ─────────────────────────────────────
        [TabGroup("Tab", "本地信息")]
        [PropertyOrder(0)]
        [DisplayAsString(Overflow = false, EnableRichText = true)]
        [ShowInInspector, HideLabel]
        [HideIf(nameof(_isDownloading))]
        private string LocalPath => $"<color={ExBrandStyle.HexTextPrimary}>本地路径:{_pluginItem.LocalPath}</color>";

        [TabGroup("Tab", "本地信息")]
        [PropertyOrder(1)]
        [DisplayAsString(EnableRichText = true)]
        [ShowInInspector, HideLabel]
        [HideIf(nameof(_isDownloading))]
        private string State
        {
            get
            {
                if (ExistPlugin())
                    return $"<color={ExBrandStyle.HexSuccess}><b>● 已安装</b></color>";
                return $"<color={ExBrandStyle.HexTextSecondary}>○ 未安装</color>";
            }
        }

        // ── 下载进度面板 ────────────────────────────────────────
        [TabGroup("Tab", "本地信息")]
        [PropertyOrder(5)]
        [ShowInInspector, HideLabel]
        [DisplayAsString(EnableRichText = true)]
        [ShowIf(nameof(_isDownloading))]
        private string ProgressStep =>
            $"<color={ExBrandStyle.HexPrimary}>{_progress?.Step ?? "准备中..."}</color>";

        [TabGroup("Tab", "本地信息")]
        [PropertyOrder(6)]
        [ShowInInspector, HideLabel]
        [ProgressBar(0, 1, ColorMember = nameof(ProgressColor), DrawValueLabel = true)]
        [ShowIf(nameof(_isDownloading))]
        private float ProgressValue => _progress?.Progress ?? 0f;

        private Color ProgressColor => ExBrandStyle.Primary;

        [TabGroup("Tab", "本地信息")]
        [PropertyOrder(7)]
        [Button("取消下载", ButtonSizes.Medium, Icon = SdfIconType.X)]
        [ShowIf(nameof(_isDownloading))]
        public void CancelDownload()
        {
            GitPluginUtility.RequestCancel();
            _isDownloading = false;
            _lastError = null;
        }

        // ── 错误面板 ────────────────────────────────────────────
        [TabGroup("Tab", "本地信息")]
        [PropertyOrder(8)]
        [ShowInInspector, HideLabel]
        [DisplayAsString(EnableRichText = true)]
        [ShowIf(nameof(HasError))]
        private string ErrorMessage =>
            $"<color={ExBrandStyle.HexError}>✕ 下载失败：{_lastError}</color>";

        private bool HasError => !_isDownloading && !string.IsNullOrEmpty(_lastError);

        [HorizontalGroup("Tab/本地信息/ErrorButtons")]
        [Button("重试", ButtonSizes.Medium, Icon = SdfIconType.ArrowClockwise)]
        [ShowIf(nameof(HasError))]
        public void RetryDownload()
        {
            _lastError = null;
            StartInstall();
        }

        [HorizontalGroup("Tab/本地信息/ErrorButtons")]
        [Button("关闭", ButtonSizes.Medium, Icon = SdfIconType.X)]
        [ShowIf(nameof(HasError))]
        public void DismissError()
        {
            _lastError = null;
        }

        // ── 操作按钮 ────────────────────────────────────────────
        [HorizontalGroup("Tab/本地信息/Buttons")]
        [Button("安装", ButtonSizes.Medium, Icon = SdfIconType.Download)]
        [HideIf(nameof(HideInstallButton))]
        public void Install()
        {
            if (ExistPlugin())
            {
                Debug.LogWarning("插件已安装，无需重复安装");
                return;
            }
            if (!NetworkStatusMonitor.IsOnline && NetworkStatusMonitor.HasChecked)
            {
                EditorUtility.DisplayDialog("离线状态", "当前网络不可用，无法下载插件。", "确定");
                return;
            }

            var result = EditorUtility.DisplayDialog("安装插件",
                $"是否确认安装插件 {_pluginItem.Name}？\n安装将从远端Git仓库下载插件文件。",
                "确认", "取消");
            if (!result) return;

            StartInstall();
        }

        private bool HideInstallButton => ExistPlugin() || _isDownloading || HasError;

        [HorizontalGroup("Tab/本地信息/Buttons")]
        [Button("卸载", ButtonSizes.Medium, Icon = SdfIconType.Trash)]
        [ShowIf(nameof(ExistPlugin))]
        [HideIf(nameof(_isDownloading))]
        public void Uninstall()
        {
            if (!ExistPlugin()) { Debug.LogWarning("插件未安装，无法卸载"); return; }

            var result = EditorUtility.DisplayDialog("卸载插件",
                $"是否确认卸载插件 {_pluginItem.Name}？\n卸载将删除本地插件文件夹。",
                "确认", "取消");
            if (!result) return;

            ExOpenSourceNetworkHelper.DeleteFolder(_pluginItem.LocalPath);
            Debug.Log($"插件 {_pluginItem.Name} 已卸载");
            _lastError = null;
            ExOpenSourcePluginManagerWindow.RefreshMenuTree();
        }

        [HorizontalGroup("Tab/本地信息/Buttons")]
        [Button("重装", ButtonSizes.Medium, Icon = SdfIconType.ArrowClockwise)]
        [ShowIf(nameof(ExistPlugin))]
        [HideIf(nameof(_isDownloading))]
        public void ReInstall()
        {
            var result = EditorUtility.DisplayDialog("重装插件",
                $"是否确认重装插件 {_pluginItem.Name}？\n重装将删除当前插件并重新下载最新版本。",
                "确认", "取消");
            if (!result) return;

            if (ExistPlugin())
            {
                ExOpenSourceNetworkHelper.DeleteFolder(_pluginItem.LocalPath);
                Debug.Log($"插件 {_pluginItem.Name} 已卸载");
            }
            StartInstall();
        }

        [HorizontalGroup("Tab/本地信息/Buttons")]
        [Button("打开所在文件夹", ButtonSizes.Medium, Icon = SdfIconType.Folder2Open)]
        [ShowIf(nameof(ExistPlugin))]
        [HideIf(nameof(_isDownloading))]
        public void OpenFolderInExplore()
        {
            EditorUtility.RevealInFinder(_pluginItem.LocalPath);
        }

        // ── 说明书 ───────────────────────────────────────────────
        [TitleGroup("Tab/本地信息/说明书", order: 99, HideWhenChildrenAreInvisible = true)]
        [ShowInInspector]
        [ShowIf("@!string.IsNullOrEmpty(_pluginItem.IntroductionURL)")]
        [PropertyOrder(10)]
        [Button("打开说明书", ButtonSizes.Medium, Icon = SdfIconType.Book)]
        public void OpenGuideMdInExplore()
        {
            Application.OpenURL(_pluginItem.IntroductionURL);
        }

        [TitleGroup("Tab/本地信息/说明书")]
        [ShowInInspector]
        [HideIf("@!string.IsNullOrEmpty(_pluginItem.IntroductionURL)")]
        [PropertyOrder(10)]
        [HideLabel, DisplayAsString(Overflow = false, EnableRichText = true)]
        public string tip =>
            $"<color={ExBrandStyle.HexAccent}>提示: 说明书链接未配置，请联系插件作者添加。\n" +
            "请查看插件的Git仓库或联系作者获取更多信息。</color>";

        // ── 核心下载逻辑 ─────────────────────────────────────────
        private void StartInstall()
        {
            _isDownloading = true;
            _lastError = null;
            _progress = new DownloadProgressInfo { Step = "准备中...", Progress = 0f };

            string user = string.IsNullOrEmpty(_pluginItem.GitURL_Username)
                ? _menuConfig.DefaultGit_UserName : _pluginItem.GitURL_Username;
            string repoName = string.IsNullOrEmpty(_pluginItem.GitURL_RepoName)
                ? _menuConfig.DefaultGit_RepoName : _pluginItem.GitURL_RepoName;
            string branch = string.IsNullOrEmpty(_pluginItem.GitURL_Branch)
                ? _menuConfig.DefaultGit_Branch : _pluginItem.GitURL_Branch;

            GitPluginUtility.DownloadPlugin(
                user, repoName, branch,
                _pluginItem.GitURL_Path,
                _pluginItem.LocalPath,
                ExOpenSourceNetworkHelper.Token(),
                info =>
                {
                    _progress = info;
                    // Unity Editor 主线程刷新
                    EditorApplication.delayCall += () =>
                        EditorWindow.focusedWindow?.Repaint();
                },
                (success, error) =>
                {
                    _isDownloading = false;
                    if (success)
                    {
                        _lastError = null;
                        UpmInstaller.AddUpmPackageList(_pluginItem.Dependencies);
                        ExOpenSourcePluginManagerWindow.RefreshMenuTree();
                    }
                    else
                    {
                        _lastError = error;
                    }
                    EditorApplication.delayCall += () =>
                        EditorWindow.focusedWindow?.Repaint();
                });
        }

        private bool ExistPlugin()
        {
            return !ExOpenSourceNetworkHelper.IsFolderEmpty(_pluginItem.LocalPath);
        }
    }
}
