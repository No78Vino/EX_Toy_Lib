namespace ExOpenSource.Editor
{
    public static class ExOpenSourceConstParam
    {
        public const string SETTING_ASSET_PATH = "ProjectSettings/ExOpenSourceSetting.asset";
        public const string DEFAULT_MENU_PATH = "Assets/_EXToyLib/menu.json";
        public const string GIT_REPO_RAW_URL = "https://raw.githubusercontent.com/No78Vino/EX_Toy_Lib/main";
        
        public static readonly RepoInfo[] OfficialRepoInfos = new[]
        {
            new RepoInfo
            {
                userName = "No78Vino",
                repoName = "EX_Toy_Lib",
                branch = "main",
                remoteMenuPath = "Assets/_EXToyLib/menu.json",
                localMenuPath = "Assets/Plugins/ExOpenSource/menu_ex.json"
            },
        };
        
        #region text

        public const string POOF_LIB_MGR = "EX开源插件管理器";
        public const string POOF_LIB_HOST_TITLE = "欢迎使用EX开源插件管理器";
        public const string POOF_LIB_HOST_MSG = "<color=" + ExBrandStyle.HexTextPrimary + "><size=16>"
                                                + "此工具用于管理项目资源目录\n"
                                                + "1. 创建Assets/_PoofLibrary/menu.json文件\n"
                                                + "2. 按照JSON格式配置资源目录\n"
                                                + "3. 左侧菜单将自动显示配置的资源目录"
                                                + "</size></color>";
        
        public const string POOF_LIB_HOST_INTRO = "<color=" + ExBrandStyle.HexTextPrimary + "><size=20>"
                                                + "使用说明：\n"
                                                + "EX开源插件管理器。可以搜罗 "
                                                + "</size></color>";
        
        public const string REPO_SETTING = "仓库设置";
        public const string REPO_TOKEN_GROUP = "GitHub令牌设置";
        public const string REPO_TOKEN = "GitHub 令牌";
        public const string REPO_TOKEN_FILE_PATH = "令牌缓存文件路径";
        public const string REPO_SETTING_GROUP_SUB_1 = "仓库设置/网络检测";
        public const string REPO_SETTING_GROUP_SUB_CONNECTION = "仓库设置/网络检测/connection";

        public const string REPO_TOKEN_INTRO =
            "访问令牌(可选):\n" +
            "• 私有仓库必须提供令牌，令牌可以避免GitHub速率限制\n" +
            "• 令牌创建地址: https://github.com/settings/tokens \n" +
            "• 本项目用到的token权限只需要 `repo` 权限即可。（如果你只访问public仓库，只需要勾选public_repo）\n" +
            "• <color=" + ExBrandStyle.HexWarning + "> 注意：请勿泄露令牌信息！ 如需要使用令牌，请将其存入一个text文件，然后选择读取该text文件路径。</color>";

        public const string REPO_CONNECTION_TITLE = "连接的仓库配置";
        #endregion
    }
}