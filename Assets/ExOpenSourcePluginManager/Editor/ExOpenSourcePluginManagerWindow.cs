using System;
using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace ExOpenSource.Editor
{
    public class ExOpenSourcePluginManagerWindow : OdinMenuEditorWindow
    {
        [MenuItem("EXTool/EX开源插件管理器（Github）")]
        private static void OpenWindow()
        {
            var window = GetWindow<ExOpenSourcePluginManagerWindow>();
            window.titleContent = new GUIContent(ExOpenSourceConstParam.POOF_LIB_MGR);
            window.minSize = new Vector2(800, 600);
            window.Show();
        }

        private static ExOpenSourcePluginManagerWindow Instance => GetWindow<ExOpenSourcePluginManagerWindow>();

        // 过滤状态
        private readonly PluginFilterState _filterState = new PluginFilterState();
        // 缓存所有配置和标签，避免在 BuildMenuTree 里重复 IO
        private List<ExMenuConfig> _cachedConfigs;
        private List<string> _cachedTags = new List<string>();

        public static void RefreshMenuTree()
        {
            if (Instance != null) Instance.ForceMenuTreeRebuild();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _filterState.OnChanged += ForceMenuTreeRebuild;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _filterState.OnChanged -= ForceMenuTreeRebuild;
        }

        // 在菜单树上方绘制过滤工具栏
        protected override void OnBeginDrawEditors()
        {
            DrawFilterToolbar();
        }

        private void DrawFilterToolbar()
        {
            // ── 安装状态过滤 ──────────────────────────────────────
            SirenixEditorGUI.BeginToolbarBox();

            GUILayout.BeginHorizontal();
            GUILayout.Label("状态:", GUILayout.Width(34));

            DrawFilterButton("全部",      InstallFilter.All);
            DrawFilterButton("已安装",    InstallFilter.Installed);
            DrawFilterButton("未安装",    InstallFilter.NotInstalled);

            GUILayout.FlexibleSpace();

            // 重置过滤
            if (GUILayout.Button("重置过滤", EditorStyles.toolbarButton, GUILayout.Width(60)))
                _filterState.Reset();

            GUILayout.EndHorizontal();

            // ── 标签过滤（自适应换行） ───────────────────────────────
            if (_cachedTags.Count > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("标签:", GUILayout.Width(34));

                GUILayout.BeginVertical();
                float tagLineX = 0f;
                float totalWidth = EditorGUIUtility.currentViewWidth - 42f; // 减去左侧标签和边距
                int lineIndex = 0;

                if (lineIndex == 0) GUILayout.BeginHorizontal();
                foreach (var tag in _cachedTags)
                {
                    float tagWidth = EditorStyles.toolbarButton.CalcSize(new GUIContent(tag)).x + 8f;
                    if (tagLineX + tagWidth > totalWidth && tagLineX > 0f)
                    {
                        // 当前行放不下，换行
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        tagLineX = 0f;
                    }

                    bool isActive = _filterState.ActiveTag == tag;
                    var oldColor = GUI.color;
                    if (isActive) GUI.color = new Color(0.4f, 0.7f, 1f);
                    if (GUILayout.Button(tag, EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                        _filterState.SetTag(tag);
                    GUI.color = oldColor;
                    tagLineX += tagWidth;
                }
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();

                GUILayout.EndHorizontal();
            }

            SirenixEditorGUI.EndToolbarBox();
        }

        private void DrawFilterButton(string label, InstallFilter filter)
        {
            bool isActive = _filterState.InstallFilter == filter;
            var oldColor = GUI.color;
            if (isActive) GUI.color = new Color(0.4f, 0.7f, 1f);
            if (GUILayout.Button(label, EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                _filterState.SetInstallFilter(filter);
            GUI.color = oldColor;
        }

        protected override OdinMenuTree BuildMenuTree()
        {
            var tree = new OdinMenuTree(false);
            tree.Add("首页", new ExOpenSourceHostPage(), EditorIcons.House);
            tree.Add("设置", ExOpenSourcePluginManagerSetting.Instance, EditorIcons.SettingsCog);

            tree.DefaultMenuStyle.Height = ExBrandStyle.MenuItemHeight;
            tree.DefaultMenuStyle.IndentAmount = ExBrandStyle.MenuIndentAmount;
            tree.Config.DrawSearchToolbar = true;

            // 加载并缓存配置
            _cachedConfigs = LoadMenuConfigs();
            _cachedTags = PluginFilterState.CollectAllTags(_cachedConfigs);

            if (_cachedConfigs != null && _cachedConfigs.Count > 0)
            {
                foreach (var item in _cachedConfigs)
                {
                    tree.Add(item.Name, item, EditorIcons.List);

                    foreach (var plugin in item.Plugins)
                    {
                        // 过滤
                        if (!_filterState.Matches(plugin)) continue;

                        // 安装状态图标：已安装用绿色标记，未安装用默认
                        bool installed = !ExOpenSourceNetworkHelper.IsFolderEmpty(plugin.LocalPath);
                        var icon = installed ? EditorIcons.TestPassed : EditorIcons.TestNormal;

                        tree.Add(plugin.MenuPath, new PluginInformationPage(plugin, item), icon);
                    }
                }
            }

            return tree;
        }

        private List<ExMenuConfig> LoadMenuConfigs()
        {
            var repoInfos = ExOpenSourcePluginManagerSetting.Instance.repoInfos;
            if (repoInfos == null || repoInfos.Count == 0)
            {
                Debug.LogWarning("没有配置任何仓库信息");
                return null;
            }

            var configs = new List<ExMenuConfig>();

            foreach (var repo in repoInfos)
            {
                var fullPath = Path.Combine(Application.dataPath, "../", repo.localMenuPath);
                if (!File.Exists(fullPath))
                {
                    Debug.LogWarning($"找不到目录配置文件: {fullPath}");
                    continue;
                }

                try
                {
                    var json = File.ReadAllText(fullPath);
                    var config = JsonUtility.FromJson<ExMenuConfig>(json);
                    if (config != null) configs.Add(config);
                }
                catch (Exception e)
                {
                    Debug.LogError($"解析配置文件失败: {e.Message}");
                }
            }

            if (configs.Count != 0) return configs;
            Debug.LogWarning("没有有效的目录配置文件");
            return null;
        }
    }
}
