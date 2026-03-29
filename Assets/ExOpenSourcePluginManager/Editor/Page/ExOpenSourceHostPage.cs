using System.IO;
using Sirenix.OdinInspector;
using UnityEngine;

namespace ExOpenSource.Editor
{
    public class ExOpenSourceHostPage
    {
        // ── 品牌标题区 ───────────────────────────────────────────────────────
        [BoxGroup("EX 开源插件管理器")]
        [ShowInInspector, HideLabel]
        [DisplayAsString(false, 20, TextAlignment.Left, true)]
        public string BrandTitle =>
            $"<color={ExBrandStyle.HexPrimary}><size=22><b>EX 开源插件管理器</b></size></color>\n" +
            $"<color={ExBrandStyle.HexTextSecondary}><size=12>一站式管理你的 GitHub 开源插件收藏夹</size></color>";

        [BoxGroup("EX 开源插件管理器")]
        [ShowInInspector, HideLabel]
        [DisplayAsString(false, 12, TextAlignment.Left, true)]
        public string StatsBar =>
            $"<color={ExBrandStyle.HexTextSecondary}>已安装插件：</color>" +
            $"<color={ExBrandStyle.HexSuccess}><b>{GetInstalledCount()}</b></color>" +
            $"<color={ExBrandStyle.HexTextSecondary}>  /  总插件：</color>" +
            $"<color={ExBrandStyle.HexTextPrimary}><b>{GetTotalCount()}</b></color>";

        // ── 功能卡片区 ───────────────────────────────────────────────────────
        [BoxGroup("功能介绍")]
        [PropertyOrder(1)]
        [ShowInInspector, HideLabel]
        [DisplayAsString(false, 13, TextAlignment.Left, true)]
        public string Card1 =>
            $"<color={ExBrandStyle.HexTextSecondary}><b>为什么需要这个工具</b></color>\n" +
            $"<color={ExBrandStyle.HexTextPrimary}>你是否曾在 GitHub 的收藏夹里一个一个翻找插件，复制粘贴，手动拖进项目？\n" +
            "每次用到某个好工具，都要来来回回找半天，还有时候只想取其中一个子文件夹……\n\n" +
            $"</color><color={ExBrandStyle.HexTextSecondary}>这个工具就是为了解决这个痛点而生的。</color>";

        [BoxGroup("功能介绍")]
        [PropertyOrder(2)]
        [ShowInInspector, HideLabel]
        [DisplayAsString(false, 13, TextAlignment.Left, true)]
        public string Card2 =>
            $"<color={ExBrandStyle.HexTextSecondary}><b>核心功能</b></color>\n" +
            $"<color={ExBrandStyle.HexPrimary}>●</color> <color={ExBrandStyle.HexTextPrimary}><b>浏览插件目录</b></color>  " +
            $"<color={ExBrandStyle.HexTextSecondary}>— 从远端 menu.json 拉取插件列表，左侧菜单树分类展示\n</color>" +
            $"<color={ExBrandStyle.HexPrimary}>●</color> <color={ExBrandStyle.HexTextPrimary}><b>一键下载安装</b></color>  " +
            $"<color={ExBrandStyle.HexTextSecondary}>— 通过 Git sparse-checkout 精准拉取指定子目录，无需克隆整个仓库\n</color>" +
            $"<color={ExBrandStyle.HexPrimary}>●</color> <color={ExBrandStyle.HexTextPrimary}><b>自动处理依赖</b></color>  " +
            $"<color={ExBrandStyle.HexTextSecondary}>— 安装时自动将 UPM 包依赖写入 manifest.json\n</color>" +
            $"<color={ExBrandStyle.HexPrimary}>●</color> <color={ExBrandStyle.HexTextPrimary}><b>多仓库支持</b></color>     " +
            $"<color={ExBrandStyle.HexTextSecondary}>— 可配置多个 GitHub 仓库来源，统一管理</color>";

        [BoxGroup("功能介绍")]
        [PropertyOrder(3)]
        [ShowInInspector, HideLabel]
        [DisplayAsString(false, 13, TextAlignment.Left, true)]
        public string Card3 =>
            $"<color={ExBrandStyle.HexTextSecondary}><b>快速开始</b></color>\n" +
            $"<color={ExBrandStyle.HexAccent}><b>Step 1</b></color> <color={ExBrandStyle.HexTextPrimary}>前往</color> " +
            $"<color={ExBrandStyle.HexPrimary}><b>设置</b></color> <color={ExBrandStyle.HexTextPrimary}>页面，下载插件目录（menu.json）\n</color>" +
            $"<color={ExBrandStyle.HexAccent}><b>Step 2</b></color> <color={ExBrandStyle.HexTextPrimary}>在左侧菜单树中浏览插件分类\n</color>" +
            $"<color={ExBrandStyle.HexAccent}><b>Step 3</b></color> <color={ExBrandStyle.HexTextPrimary}>点击插件 →「本地信息」Tab → 点击</color> " +
            $"<color={ExBrandStyle.HexSuccess}><b>安装</b></color> <color={ExBrandStyle.HexTextPrimary}>按钮</color>";

        // ── 关于作者区 ───────────────────────────────────────────────────────
        [BoxGroup("关于")]
        [ShowInInspector, HideLabel]
        [DisplayAsString(false, 12, TextAlignment.Left, true)]
        public string AuthorInfo =>
            $"<color={ExBrandStyle.HexTextSecondary}>作者花了 3 个晚上写出这个工具，利用 AI 辅助开发，" +
            "仍有不少地方可以打磨，欢迎反馈和贡献。\n\n" +
            $"</color><color={ExBrandStyle.HexTextSecondary}>EX-HARD 游戏开发交流 QQ 群：</color>" +
            $"<color={ExBrandStyle.HexPrimary}><b>616570103</b></color>";

        // ── 统计计算 ─────────────────────────────────────────────────────────
        private int GetInstalledCount()
        {
            int count = 0;
            var setting = ExOpenSourcePluginManagerSetting.Instance;
            if (setting == null || setting.repoInfos == null) return 0;

            foreach (var repo in setting.repoInfos)
            {
                var fullPath = Path.Combine(Application.dataPath, "../", repo.localMenuPath);
                if (!File.Exists(fullPath)) continue;
                try
                {
                    var json = File.ReadAllText(fullPath);
                    var config = JsonUtility.FromJson<ExMenuConfig>(json);
                    if (config?.Plugins == null) continue;
                    foreach (var plugin in config.Plugins)
                    {
                        if (!ExOpenSourceNetworkHelper.IsFolderEmpty(plugin.LocalPath))
                            count++;
                    }
                }
                catch { /* 忽略解析错误 */ }
            }
            return count;
        }

        private int GetTotalCount()
        {
            int count = 0;
            var setting = ExOpenSourcePluginManagerSetting.Instance;
            if (setting == null || setting.repoInfos == null) return 0;

            foreach (var repo in setting.repoInfos)
            {
                var fullPath = Path.Combine(Application.dataPath, "../", repo.localMenuPath);
                if (!File.Exists(fullPath)) continue;
                try
                {
                    var json = File.ReadAllText(fullPath);
                    var config = JsonUtility.FromJson<ExMenuConfig>(json);
                    if (config?.Plugins != null)
                        count += config.Plugins.Count;
                }
                catch { /* 忽略解析错误 */ }
            }
            return count;
        }
    }
}
