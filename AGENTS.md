# EX_Toy_Lib

Unity 2022.3 (LTS) project containing the **EX Open Source Plugin Manager** and a collection of reusable Unity toy plugins/toolkits.

## Build & Development

This is a Unity project. Open it with Unity Editor 2022.3.16f1 or later. There is no CLI-based build or test runner configured.

- **Unity version**: 2022.3.16f1
- **Render pipeline**: Universal Render Pipeline (URP) 14.0.10
- **IDE support**: Rider, Visual Studio, VS Code (configured in manifest)

## Key Dependencies

- **Odin Inspector** (paid, v3.2+) — required for plugin manager UI; located in `Assets/Plugins/Sirenix/`
- **Newtonsoft JSON** (`com.unity.nuget.newtonsoft-json` 3.2.1)
- **Editor Coroutines** (`com.unity.editorcoroutines` 1.0.0)
- **Loxodon Framework** (MVVM, via git URL) — used by EX-Maid For UI plugin
- **Unity Entities** (`com.unity.entities` 1.2.3) — present but not heavily used yet
- **Unity Test Framework** (`com.unity.test-framework` 1.1.33)

## Architecture

### Plugin Manager (`Assets/ExOpenSourcePluginManager/`)

Editor-only Unity package (`com.exhard.exopensourcepluginmanager`) that provides a custom window for browsing, downloading, and managing open-source Unity plugins from GitHub. Accessed via Unity toolbar: **EX_Tool -> EX开源插件管理器（Github）**.

- `Editor/Menu/` — Data models: `ExMenuConfig` (menu/repo config), `ExPluginItem` (individual plugin entry)
- `Editor/Page/` — UI pages: settings page (`ExOpenSourcePluginManagerSetting`), plugin info display (`PluginInformationPage`), repo config (`RepoInfo`), host page
- `Editor/Helper/` — Network helper (`ExOpenSourceNetworkHelper` handles GitHub API/raw downloads), git plugin download utility (`GitPluginUtility`), UPM installer
- `ExScriptableSingleton.cs` — Base class for ScriptableObject singletons (settings persistence)
- `ExOpenSourceConstParam.cs` — Constants and configuration parameters

The plugin manager resolves plugin download URLs by combining: `DefaultGit_UserName`, `DefaultGit_RepoName`, `DefaultGit_Branch`, and each plugin's `GitURL_Path`. Plugins can override the default repo by specifying their own `GitURL_Username/RepoName/Branch`.

### Plugin Library (`Assets/_EXToyLib/`)

Source directory for the author's own plugins. These are the upstream files that the plugin manager downloads to `Assets/Plugins/ExOpenSource/`. Each plugin is self-contained in its own subfolder.

- `ActivityQueueController/` — Sequential activity queue framework for time-ordered task execution
- `BezierTrajectory/` — Bezier curve-based projectile/object trajectory controller
- `ExMech/` — Mech node base system (`MechNodeBase.cs`)
- `GravityForCharacterController/` — Gravity implementation for Unity CharacterController (since Rigidbody gravity conflicts with it)
- `ObjectScatter/` — Object scatter tool with custom Editor
- `ValueSecondOrderSystem/` — Second-order dynamics simulator (Jacobson's algorithm) for procedural secondary motion animations
- `_Other/` — Misc utilities: `AutoRotation`, `UnityTransformSync`

### Menu Configuration (`Assets/_EXToyLib/menu.json`)

Central JSON manifest that defines all available plugins. The plugin manager fetches this to populate its UI. See README.md for full schema documentation.

### Other Directories

- `Assets/Scripts/` — Test scripts and procedural skybox controller
- `Assets/Plugins/Sirenix/` — Odin Inspector (paid, not in repo)
- `Assets/Plugins/ExOpenSource/` — Downloaded plugin instances (local install targets)
- `Assets/Scenes/` — `SampleScene.unity`, `TestingGrounds.unity`
- `Assets/Settings/` — URP render pipeline assets and post-processing profiles
- `Assets/GameAssets/` — Game art assets (prefabs, skybox, terrain, sun shafts)

## Language

This is a Chinese-language project. Code comments, UI strings, and documentation are primarily in Chinese. README.md is in Chinese.
