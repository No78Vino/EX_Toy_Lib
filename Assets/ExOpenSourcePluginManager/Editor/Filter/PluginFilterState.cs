using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ExOpenSource.Editor
{
    public enum InstallFilter
    {
        All,
        Installed,
        NotInstalled
    }

    /// <summary>
    /// 插件过滤状态，管理菜单树的标签过滤和安装状态过滤
    /// </summary>
    public class PluginFilterState
    {
        /// <summary>当前激活的标签，null 表示不过滤标签</summary>
        public string ActiveTag { get; private set; }

        /// <summary>安装状态过滤</summary>
        public InstallFilter InstallFilter { get; private set; } = InstallFilter.All;

        public event Action OnChanged;

        public void SetTag(string tag)
        {
            // 再次点击同一标签则取消过滤
            ActiveTag = (ActiveTag == tag) ? null : tag;
            OnChanged?.Invoke();
        }

        public void SetInstallFilter(InstallFilter filter)
        {
            InstallFilter = filter;
            OnChanged?.Invoke();
        }

        public void Reset()
        {
            ActiveTag = null;
            InstallFilter = InstallFilter.All;
            OnChanged?.Invoke();
        }

        /// <summary>判断插件是否通过当前过滤条件</summary>
        public bool Matches(ExPluginItem plugin)
        {
            // 标签过滤
            if (!string.IsNullOrEmpty(ActiveTag))
            {
                if (plugin.Tags == null || plugin.Tags.Length == 0) return false;
                bool hasTag = false;
                foreach (var t in plugin.Tags)
                    if (t == ActiveTag) { hasTag = true; break; }
                if (!hasTag) return false;
            }

            // 安装状态过滤（用 Directory.Exists 避免 GetFiles 性能消耗）
            if (InstallFilter != InstallFilter.All)
            {
                bool installed = IsInstalled(plugin.LocalPath);
                if (InstallFilter == InstallFilter.Installed && !installed) return false;
                if (InstallFilter == InstallFilter.NotInstalled && installed) return false;
            }

            return true;
        }

        /// <summary>从所有已加载的配置中收集全部唯一标签</summary>
        public static List<string> CollectAllTags(List<ExMenuConfig> configs)
        {
            var tags = new HashSet<string>();
            if (configs == null) return new List<string>();
            foreach (var cfg in configs)
            {
                if (cfg.Plugins == null) continue;
                foreach (var plugin in cfg.Plugins)
                {
                    if (plugin.Tags == null) continue;
                    foreach (var t in plugin.Tags)
                        if (!string.IsNullOrEmpty(t)) tags.Add(t);
                }
            }
            var list = new List<string>(tags);
            list.Sort();
            return list;
        }

        private static bool IsInstalled(string localPath)
        {
            if (string.IsNullOrEmpty(localPath)) return false;
            return Directory.Exists(localPath) && !ExOpenSourceNetworkHelper.IsFolderEmpty(localPath);
        }
    }
}
