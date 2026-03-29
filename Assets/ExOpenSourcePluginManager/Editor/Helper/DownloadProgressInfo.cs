using System;

namespace ExOpenSource.Editor
{
    /// <summary>
    /// 下载进度数据传输对象，用于 GitPluginUtility 向 UI 报告进度
    /// </summary>
    [Serializable]
    public class DownloadProgressInfo
    {
        /// <summary>当前步骤描述（中文）</summary>
        public string Step;
        /// <summary>总进度 0-1</summary>
        public float Progress;
        /// <summary>错误信息，非空表示失败</summary>
        public string Error;
        /// <summary>是否已完成（成功或失败）</summary>
        public bool IsComplete;
        /// <summary>是否被用户取消</summary>
        public bool IsCancelled;

        public bool IsSuccess => IsComplete && string.IsNullOrEmpty(Error) && !IsCancelled;
        public bool IsFailed  => IsComplete && (!string.IsNullOrEmpty(Error) || IsCancelled);
    }
}
