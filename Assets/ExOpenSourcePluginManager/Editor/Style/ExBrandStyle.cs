using UnityEngine;

namespace ExOpenSource.Editor
{
    /// <summary>
    /// EX开源插件管理器品牌色和视觉常量
    /// </summary>
    public static class ExBrandStyle
    {
        // ── 主色调 ──────────────────────────────────────────────
        /// <summary>品牌主色 - 蓝色</summary>
        public static readonly Color Primary = HexToColor("#1A73E8");
        /// <summary>强调色 - 橙色</summary>
        public static readonly Color Accent = HexToColor("#FF6D00");
        
        // ── 状态色 ──────────────────────────────────────────────
        /// <summary>成功/已安装 - 绿色</summary>
        public static readonly Color Success = HexToColor("#2E7D32");
        /// <summary>警告/更新可用 - 黄色</summary>
        public static readonly Color Warning = HexToColor("#F57F17");
        /// <summary>错误/失败 - 红色</summary>
        public static readonly Color Error = HexToColor("#C62828");

        // ── 背景色 ──────────────────────────────────────────────
        /// <summary>深色背景</summary>
        public static readonly Color BgDark = HexToColor("#1E1E1E");
        /// <summary>卡片背景</summary>
        public static readonly Color BgCard = HexToColor("#2D2D2D");

        // ── 文字色 ──────────────────────────────────────────────
        /// <summary>主要文字 - 白色</summary>
        public static readonly Color TextPrimary = HexToColor("#FFFFFF");
        /// <summary>次要文字 - 浅灰</summary>
        public static readonly Color TextSecondary = HexToColor("#B0B0B0");

        // ── 菜单树样式常量 ──────────────────────────────────────
        /// <summary>菜单项高度（px）</summary>
        public const int MenuItemHeight = 30;
        /// <summary>菜单缩进宽度（px）</summary>
        public const int MenuIndentAmount = 15;

        // ── Hex 字符串常量（用于富文本 <color=...>）──────────────
        public const string HexPrimary       = "#1A73E8";
        public const string HexAccent        = "#FF6D00";
        public const string HexSuccess       = "#2E7D32";
        public const string HexWarning       = "#F57F17";
        public const string HexError         = "#C62828";
        public const string HexTextPrimary   = "#FFFFFF";
        public const string HexTextSecondary = "#B0B0B0";

        // ── 安装状态富文本徽章 ──────────────────────────────────
        /// <summary>已安装状态标签</summary>
        public const string InstalledBadge       = "<color=#2E7D32>● 已安装</color>";
        /// <summary>未安装状态标签</summary>
        public const string NotInstalledBadge    = "<color=#B0B0B0>○ 未安装</color>";
        /// <summary>更新可用状态标签</summary>
        public const string UpdateAvailableBadge = "<color=#F57F17>↑ 有更新</color>";

        // ── 工具方法 ────────────────────────────────────────────
        /// <summary>将 Color 转为 #RRGGBB Hex 字符串</summary>
        public static string ToHex(Color color)
        {
            return $"#{ColorUtility.ToHtmlStringRGB(color)}";
        }

        /// <summary>将富文本颜色标签包裹文本</summary>
        public static string Colored(string text, string hex)
        {
            return $"<color={hex}>{text}</color>";
        }

        /// <summary>将富文本颜色标签包裹文本</summary>
        public static string Colored(string text, Color color)
        {
            return Colored(text, ToHex(color));
        }

        private static Color HexToColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var color);
            return color;
        }
    }
}
