using UnityEngine;
using UnityEditor;
using System.IO;
using System.Diagnostics;
using System;
using System.Collections.Generic;
using Debug = UnityEngine.Debug;

namespace ExOpenSource.Editor
{
    /// <summary>
    /// Git插件管理工具类
    /// </summary>
    public static class GitPluginUtility
    {
        #region 公共接口

        // 当前正在运行的 git 进程（用于取消）
        private static System.Diagnostics.Process _currentProcess;
        private static bool _cancelRequested;

        /// <summary>请求取消当前下载</summary>
        public static void RequestCancel()
        {
            _cancelRequested = true;
            try { _currentProcess?.Kill(); } catch { /* 进程可能已退出 */ }
        }

        /// <summary>
        /// 从Git仓库下载插件
        /// </summary>
        /// <param name="onProgress">进度回调，在主要步骤时调用</param>
        /// <param name="onComplete">完成回调 (success, errorMsg)</param>
        public static void DownloadPlugin(
            string userName,
            string repoName,
            string branch,
            string remotePath,
            string installPath,
            string token = "",
            Action<DownloadProgressInfo> onProgress = null,
            Action<bool, string> onComplete = null)
        {
            _cancelRequested = false;
            GitPluginConfig config = new GitPluginConfig()
            {
                repoUrl  = $"https://github.com/{userName}/{repoName}.git",
                targetBranch = branch,
                relativePath = remotePath,
                installPath  = installPath,
                token = token
            };
            ExecuteOperation(() =>
            {
                Directory.CreateDirectory(config.installPath);

                DownloadGitRepository(
                    config.repoUrl,
                    config.targetBranch,
                    config.relativePath,
                    config.installPath,
                    config.token,
                    onProgress
                );

                if (!_cancelRequested)
                    AddInstallationRecord(config);
            }, "Downloading Plugin", onProgress, onComplete);
        }

        /// <summary>
        /// 更新已安装的插件
        /// </summary>
        /// <param name="config">插件配置</param>
        /// <param name="forceReinstall">是否强制重新安装（即使版本相同）</param>
        public static void UpdatePlugin(
            string userName,
            string repoName,
            string branch,
            string remotePath,
            string installPath,
            string token = "",
            bool forceReinstall = false)
        {
            GitPluginConfig config = new GitPluginConfig()
            {
                repoUrl =  $"https://github.com/{userName}/{repoName}.git",
                targetBranch = branch,
                relativePath = remotePath,
                installPath = installPath,
                token = token
            };
            
            if (!Directory.Exists(config.installPath))
            {
                EditorUtility.DisplayDialog("Update Failed",
                    $"Plugin not found at: {config.installPath}", "OK");
                return;
            }

            // 检查是否需要更新
            if (!forceReinstall && !IsUpdateAvailable(config))
            {
                EditorUtility.DisplayDialog("Plugin Up to Date",
                    "The plugin is already at the latest version.", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog("Confirm Update",
                    $"Update plugin from {config.repoUrl}?\nThis will delete existing files at:\n{config.installPath}",
                    "Update", "Cancel"))
                return;

            ExecuteOperation(() =>
            {
                // 先卸载再重新安装
                UninstallPlugin(config.installPath);
                DownloadPlugin(userName,
                    repoName,
                    branch,
                    remotePath,
                    installPath,
                    token);
            }, "Updating Plugin");
        }

        /// <summary>
        /// 卸载插件
        /// </summary>
        /// <param name="installPath">插件安装路径</param>
        public static void UninstallPlugin(string installPath)
        {
            if (!Directory.Exists(installPath))
            {
                EditorUtility.DisplayDialog("Uninstall Failed",
                    $"Directory not found: {installPath}", "OK");
                return;
            }

            ExecuteOperation(() =>
            {
                // 删除安装目录
                Directory.Delete(installPath, true);

                // 删除对应的meta文件
                string metaFile = $"{installPath}.meta";
                if (File.Exists(metaFile))
                    File.Delete(metaFile);

                // 从安装记录中移除
                RemoveInstallationRecord(installPath);

                AssetDatabase.Refresh();
            }, "Uninstalling Plugin");
        }

        #endregion

        #region 核心实现

        private static void DownloadGitRepository(
            string repoUrl,
            string targetBranch,
            string relativePath,
            string installPath,
            string token = "",
            Action<DownloadProgressInfo> onProgress = null)
        {
            if (string.IsNullOrEmpty(repoUrl))
                throw new ArgumentException("Repository URL cannot be empty");

            if (string.IsNullOrEmpty(targetBranch))
                throw new ArgumentException("Target branch cannot be empty");

            string authenticatedUrl = ApplyTokenToUrl(repoUrl, token);
            string tempPath = Path.Combine(Application.temporaryCachePath, "GitTemp_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempPath);

            void Report(string step, float progress)
            {
                onProgress?.Invoke(new DownloadProgressInfo { Step = step, Progress = progress });
                EditorUtility.DisplayProgressBar("Git Plugin Manager", step, progress);
            }

            try
            {
                if (_cancelRequested) return;
                Report("正在初始化仓库连接...", 0.1f);

                bool isRootPath = string.IsNullOrEmpty(relativePath) || relativePath == "/" || relativePath == "\\";

                if (isRootPath)
                {
                    RunGitCommand($"clone --depth 1 --filter=blob:none \"{authenticatedUrl}\" \"{tempPath}\"");
                    if (!string.IsNullOrEmpty(targetBranch) && !_cancelRequested)
                        RunGitCommand($"checkout {targetBranch}", tempPath);
                }
                else
                {
                    if (_cancelRequested) return;
                    Report("正在克隆仓库（浅克隆）...", 0.25f);
                    RunGitCommand(
                        $"clone --depth 1 --filter=blob:none --no-checkout --branch {targetBranch} \"{authenticatedUrl}\" \"{tempPath}\"");

                    if (_cancelRequested) return;
                    Report("配置稀疏检出...", 0.45f);
                    RunGitCommand("sparse-checkout init --cone", tempPath);
                    RunGitCommand($"sparse-checkout set \"{relativePath}\"", tempPath);

                    if (_cancelRequested) return;
                    Report("正在检出文件...", 0.60f);
                    RunGitCommand("checkout", tempPath);
                }

                if (_cancelRequested) return;
                Report("正在复制文件到项目...", 0.80f);
                string sourceDir = Path.Combine(tempPath, SanitizePath(relativePath));
                if (Directory.Exists(sourceDir))
                {
                    Directory.CreateDirectory(installPath);
                    FileUtil.ReplaceDirectory(sourceDir, installPath);

                    // 删除安装目录中的 .git 目录，避免 git 子项目冲突
                    string installedGitDir = Path.Combine(installPath, ".git");
                    if (Directory.Exists(installedGitDir))
                    {
                        try { Directory.Delete(installedGitDir, true); }
                        catch (Exception e) { Debug.LogWarning($"删除.git目录失败: {e.Message}"); }
                    }

                    AssetDatabase.Refresh();
                }
                else
                {
                    throw new DirectoryNotFoundException($"仓库中未找到目录: {relativePath}");
                }
            }
            catch (Exception e)
            {
                if (!_cancelRequested)
                    Debug.LogError("DownloadGitRepository Error: " + e.Message);
                throw; // 让 ExecuteOperation 处理并通知 onComplete
            }
            finally
            {
                Report("清理临时文件...", 0.95f);
                try { if (Directory.Exists(tempPath)) Directory.Delete(tempPath, true); } catch { /* 忽略 */ }
                EditorUtility.ClearProgressBar();
            }
        }

        private static string ApplyTokenToUrl(string originalUrl, string token)
        {
            if (string.IsNullOrEmpty(token))
                return originalUrl;

            // 处理不同格式的URL
            if (originalUrl.StartsWith("https://"))
            {
                // 插入token: https://token@github.com/user/repo.git
                int startIndex = "https://".Length;
                return originalUrl.Insert(startIndex, $"{token}@");
            }

            if (originalUrl.StartsWith("http://"))
            {
                // 插入token: http://token@github.com/user/repo.git
                int startIndex = "http://".Length;
                return originalUrl.Insert(startIndex, $"{token}@");
            }

            throw new ArgumentException("Token authentication only supported for HTTP/HTTPS URLs");
        }

        private static void RunGitCommand(string command, string workingDir = null)
        {
            using (Process process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = command,
                    WorkingDirectory = workingDir ?? Application.dataPath,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                Debug.Log($"Executing: `git {command}`, working directory: `{process.StartInfo.WorkingDirectory}`");

                _currentProcess = process;
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                _currentProcess = null;

                if (process.ExitCode != 0)
                {
                    string fullError = $"Command 'git {command}' failed with error ({process.ExitCode}):\n{error}";
                    throw new InvalidOperationException(fullError);
                }
            }
        }

        #endregion

        #region 辅助功能

        private static string SanitizePath(string path)
        {
            return path.Trim().TrimEnd('/', '\\');
        }

        private static void ExecuteOperation(
            Action operation,
            string title,
            Action<DownloadProgressInfo> onProgress = null,
            Action<bool, string> onComplete = null)
        {
            try
            {
                operation.Invoke();
                if (_cancelRequested)
                {
                    Debug.Log($"{title} cancelled.");
                    onProgress?.Invoke(new DownloadProgressInfo { IsCancelled = true, IsComplete = true, Step = "已取消" });
                    onComplete?.Invoke(false, "用户取消了下载");
                }
                else
                {
                    Debug.Log($"{title} completed successfully!");
                    onProgress?.Invoke(new DownloadProgressInfo { IsComplete = true, Progress = 1f, Step = "完成" });
                    onComplete?.Invoke(true, null);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"{title} Error: {ex}");
                var errorMsg = _cancelRequested ? "用户取消了下载" : ex.Message;
                onProgress?.Invoke(new DownloadProgressInfo { IsComplete = true, Error = errorMsg, Step = "失败" });
                onComplete?.Invoke(false, errorMsg);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        #endregion

        #region 安装记录管理

        private const string INSTALL_RECORD_PATH = "Assets/GitPluginInstallRecords.asset";

        [Serializable]
        private class InstallationRecord
        {
            public List<GitPluginConfig> installedPlugins = new List<GitPluginConfig>();
        }

        private static void AddInstallationRecord(GitPluginConfig config)
        {
            var records = LoadInstallationRecords();

            // 移除旧记录（如果有）
            records.installedPlugins.RemoveAll(p => p.installPath == config.installPath);

            // 添加新记录
            records.installedPlugins.Add(config);
            SaveInstallationRecords(records);
        }

        private static void RemoveInstallationRecord(string installPath)
        {
            var records = LoadInstallationRecords();
            int removed = records.installedPlugins.RemoveAll(p => p.installPath == installPath);

            if (removed > 0)
                SaveInstallationRecords(records);
        }

        private static InstallationRecord LoadInstallationRecords()
        {
            if (File.Exists(INSTALL_RECORD_PATH))
            {
                string json = File.ReadAllText(INSTALL_RECORD_PATH);
                return JsonUtility.FromJson<InstallationRecord>(json);
            }

            return new InstallationRecord();
        }

        private static void SaveInstallationRecords(InstallationRecord records)
        {
            string json = JsonUtility.ToJson(records, true);
            File.WriteAllText(INSTALL_RECORD_PATH, json);
            AssetDatabase.ImportAsset(INSTALL_RECORD_PATH);
        }

        private static bool IsUpdateAvailable(GitPluginConfig config)
        {
            // 在实际应用中，这里应该实现版本检查逻辑
            // 例如：比较本地记录的commit hash和远程最新版本
            // 此处简化实现总是返回true，表示需要更新
            return true;
        }

        #endregion
    }

    /// <summary>
    /// Git插件配置
    /// </summary>
    [Serializable]
    public class GitPluginConfig
    {
        public string repoUrl;
        public string targetBranch;
        public string relativePath;
        public string installPath;
        public string token;

        /// <summary>
        /// 验证配置是否有效
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(repoUrl) &&
                   !string.IsNullOrEmpty(relativePath) &&
                   !string.IsNullOrEmpty(installPath);
        }
    }
}