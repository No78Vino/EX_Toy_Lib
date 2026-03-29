using UnityEditor;
using UnityEngine;

namespace ExOpenSource.Editor
{
    /// <summary>
    /// EX开源插件管理器 GUIStyle 工厂，提供预配置的样式实例
    /// 注意：GUIStyle 必须在 OnGUI 上下文中创建，不能在静态字段初始化
    /// </summary>
    public static class ExGUIStyleHelper
    {
        private static GUIStyle _titleStyle;
        private static GUIStyle _bodyStyle;
        private static GUIStyle _linkStyle;
        private static GUIStyle _separatorStyle;

        /// <summary>一级标题样式 - 品牌主色、粗体、18号字</summary>
        public static GUIStyle TitleStyle
        {
            get
            {
                if (_titleStyle == null)
                {
                    _titleStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 18,
                        alignment = TextAnchor.MiddleLeft,
                        richText = true
                    };
                    _titleStyle.normal.textColor = ExBrandStyle.Primary;
                }
                return _titleStyle;
            }
        }

        /// <summary>正文样式 - 主文字色、12号字、支持富文本</summary>
        public static GUIStyle BodyStyle
        {
            get
            {
                if (_bodyStyle == null)
                {
                    _bodyStyle = new GUIStyle(EditorStyles.label)
                    {
                        fontSize = 12,
                        wordWrap = true,
                        richText = true,
                        alignment = TextAnchor.UpperLeft
                    };
                    _bodyStyle.normal.textColor = ExBrandStyle.TextPrimary;
                }
                return _bodyStyle;
            }
        }

        /// <summary>链接文字样式 - 次要文字色、可点击感</summary>
        public static GUIStyle LinkStyle
        {
            get
            {
                if (_linkStyle == null)
                {
                    _linkStyle = new GUIStyle(EditorStyles.label)
                    {
                        fontSize = 11,
                        richText = true
                    };
                    _linkStyle.normal.textColor = ExBrandStyle.TextSecondary;
                    _linkStyle.hover.textColor = ExBrandStyle.Primary;
                }
                return _linkStyle;
            }
        }

        /// <summary>状态徽章样式 - 指定背景色</summary>
        public static GUIStyle GetStatusBadgeStyle(Color bgColor)
        {
            var style = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter,
                richText = true,
                padding = new RectOffset(6, 6, 2, 2)
            };
            style.normal.textColor = ExBrandStyle.TextPrimary;
            var tex = MakeTex(1, 1, bgColor);
            style.normal.background = tex;
            return style;
        }

        /// <summary>重置缓存的样式（编辑器重新加载时调用）</summary>
        public static void ClearCache()
        {
            _titleStyle = null;
            _bodyStyle = null;
            _linkStyle = null;
            _separatorStyle = null;
        }

        private static Texture2D MakeTex(int width, int height, Color col)
        {
            var tex = new Texture2D(width, height);
            tex.SetPixel(0, 0, col);
            tex.Apply();
            return tex;
        }
    }
}
