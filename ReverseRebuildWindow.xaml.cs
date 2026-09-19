using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using WPF = System.Windows;
using WPFInterop = System.Windows.Interop;
using Forms = System.Windows.Forms;

namespace WpfDirectoryViewer
{
    public partial class ReverseRebuildWindow : Window
    {
        public ReverseRebuildWindow()
        {
            InitializeComponent();

            // 启动时在当前屏幕居中显示，考虑多显示器和DPI缩放
            this.SourceInitialized += (sender, e) =>
            {
                // 获取窗口句柄
                WPFInterop.WindowInteropHelper helper = new WPFInterop.WindowInteropHelper(this);

                // 获取窗口的DPI缩放因子
                WPF.PresentationSource source = WPF.PresentationSource.FromVisual(this);
                double dpiX = 1.0;
                double dpiY = 1.0;
                if (source != null && source.CompositionTarget != null)
                {
                    dpiX = source.CompositionTarget.TransformToDevice.M11;
                    dpiY = source.CompositionTarget.TransformToDevice.M22;
                }

                // 获取窗口所在屏幕的工作区（考虑多显示器）
                System.Drawing.Rectangle workingArea = Forms.Screen.FromHandle(helper.Handle).WorkingArea;

                // 将屏幕工作区坐标从物理像素转换为WPF逻辑单位
                double screenLeft = workingArea.Left / dpiX;
                double screenTop = workingArea.Top / dpiY;
                double screenWidth = workingArea.Width / dpiX;
                double screenHeight = workingArea.Height / dpiY;

                // 若窗口尚未测量，先进行一次测量以取得合适尺寸
                if (double.IsNaN(this.Width) || double.IsNaN(this.Height))
                {
                    this.Measure(new WPF.Size(double.PositiveInfinity, double.PositiveInfinity));
                    this.Arrange(new WPF.Rect(this.DesiredSize));
                }

                double left = screenLeft + (screenWidth - this.Width) / 2;
                double top = screenTop + (screenHeight - this.Height) / 2;

                // 设置窗口位置
                this.Left = left;
                this.Top = top;
            };
        }

        private void BrowseTxt_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new Microsoft.Win32.OpenFileDialog
            {
                Title = "选择目录树TXT",
                Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*"
            };
            if (ofd.ShowDialog() == true)
            {
                ApplyTxtPath(ofd.FileName);
            }
        }

        private void BrowseOutput_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "选择输出根目录",
                InitialDirectory = string.IsNullOrWhiteSpace(TxtOutputDir.Text) ? Directory.GetCurrentDirectory() : TxtOutputDir.Text
            };
            if (dialog.ShowDialog() == true)
            {
                TxtOutputDir.Text = dialog.FolderName;
            }
        }

        private void AnalyzeRootFromTxt_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(TxtInputPath.Text))
            {
                TryAnalyzeRootName(TxtInputPath.Text);
            }
            else
            {
                Log("请选择有效的TXT文件。");
            }
        }

        private void TryAnalyzeRootName(string txtPath)
        {
            try
            {
                var firstLine = ReadFirstNonEmptyLine(txtPath);
                if (!string.IsNullOrWhiteSpace(firstLine))
                {
                    var rootName = Path.GetFileName(Path.TrimEndingDirectorySeparator(firstLine));
                    TxtRootName.Text = rootName;
                    Log($"解析根目录名: {rootName}");
                }
                else
                {
                    Log("TXT 文件为空。");
                }
            }
            catch (Exception ex)
            {
                Log($"解析根目录名失败: {ex.Message}");
            }
        }

        private string ReadFirstNonEmptyLine(string path)
        {
            using var sr = new StreamReader(path, Encoding.UTF8, true);
            while (!sr.EndOfStream)
            {
                var line = sr.ReadLine();
                if (!string.IsNullOrWhiteSpace(line)) return line.Trim();
            }
            return string.Empty;
        }

        private void ApplyTxtPath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;
            TxtInputPath.Text = filePath;
            // 输出目录默认为txt所在目录
            try
            {
                var dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                {
                    TxtOutputDir.Text = dir;
                }
            }
            catch { }

            TryAnalyzeRootName(filePath);
        }

        private void TxtInputPath_DragEnter(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                if (files != null && files.Length > 0 && string.Equals(Path.GetExtension(files[0]), ".txt", StringComparison.OrdinalIgnoreCase))
                {
                    e.Effects = System.Windows.DragDropEffects.Copy;
                }
                else
                {
                    e.Effects = System.Windows.DragDropEffects.None;
                }
            }
            else
            {
                e.Effects = System.Windows.DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void TxtInputPath_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    var path = files[0];
                    if (string.Equals(Path.GetExtension(path), ".txt", StringComparison.OrdinalIgnoreCase))
                    {
                        ApplyTxtPath(path);
                    }
                    else
                    {
                        Log("只支持拖入 .txt 文件。");
                    }
                }
            }
            e.Handled = true;
        }

        private void TxtInputPath_PreviewDragEnter(object sender, System.Windows.DragEventArgs e)
        {
            // 与 TxtInputPath_DragEnter 相同逻辑，提升可靠性
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                if (files != null && files.Length > 0 && string.Equals(Path.GetExtension(files[0]), ".txt", StringComparison.OrdinalIgnoreCase))
                {
                    e.Effects = System.Windows.DragDropEffects.Copy;
                }
                else
                {
                    e.Effects = System.Windows.DragDropEffects.None;
                }
            }
            else
            {
                e.Effects = System.Windows.DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void TxtInputPath_PreviewDragOver(object sender, System.Windows.DragEventArgs e)
        {
            // 持续反馈允许状态
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                if (files != null && files.Length > 0 && string.Equals(Path.GetExtension(files[0]), ".txt", StringComparison.OrdinalIgnoreCase))
                {
                    e.Effects = System.Windows.DragDropEffects.Copy;
                }
                else
                {
                    e.Effects = System.Windows.DragDropEffects.None;
                }
            }
            else
            {
                e.Effects = System.Windows.DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void TxtInputPath_PreviewDrop(object sender, System.Windows.DragEventArgs e)
        {
            // 与 TxtInputPath_Drop 一致
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    var path = files[0];
                    if (string.Equals(Path.GetExtension(path), ".txt", StringComparison.OrdinalIgnoreCase))
                    {
                        ApplyTxtPath(path);
                    }
                    else
                    {
                        Log("只支持拖入 .txt 文件。");
                    }
                }
            }
            e.Handled = true;
        }

        private void CreateStructure_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TxtLog.Clear();

                var txtPath = TxtInputPath.Text.Trim();
                var outDir = TxtOutputDir.Text.Trim();
                var dryRun = ChkDryRun.IsChecked == true;

                if (!File.Exists(txtPath))
                {
                    Log("请输入有效的TXT路径。");
                    return;
                }
                if (string.IsNullOrWhiteSpace(outDir))
                {
                    Log("请输入输出目录。");
                    return;
                }
                
                // 尝试创建输出目录（如果不存在）
                if (!Directory.Exists(outDir))
                {
                    try
                    {
                        Directory.CreateDirectory(outDir);
                        Log($"已创建输出目录: {outDir}");
                    }
                    catch (Exception ex)
                    {
                        Log($"创建输出目录失败: {ex.Message}");
                        return;
                    }
                }

                var lines = File.ReadAllLines(txtPath, Encoding.UTF8);
                if (lines.Length == 0)
                {
                    Log("TXT 文件内容为空。");
                    return;
                }

                string rootAbs = lines[0].Trim();
                if (string.IsNullOrWhiteSpace(rootAbs))
                {
                    Log("第一行（根绝对路径）为空。");
                    return;
                }
                string rootName = Path.GetFileName(Path.TrimEndingDirectorySeparator(rootAbs));
                if (string.IsNullOrWhiteSpace(rootName)) rootName = "root";
                TxtRootName.Text = rootName;

                string targetRoot = Path.Combine(outDir, rootName);
                Log($"目标根目录: {targetRoot}");

                var entries = IsTreeFormat(lines) ? ParseTreeLines(lines) : ParsePathList(lines, rootAbs);

                if (dryRun)
                {
                    Log("Dry Run 模式：仅展示将要创建的路径，不实际创建。");
                    foreach (var e2 in entries)
                    {
                        Log($"{(e2.IsDirectory ? "[DIR]" : "[FILE]")} {Path.Combine(targetRoot, e2.RelativePath)}");
                    }
                    Log("预览完成。");
                    return;
                }

                // 真正创建
                Directory.CreateDirectory(targetRoot);
                foreach (var e2 in entries)
                {
                    string fullPath = Path.Combine(targetRoot, e2.RelativePath);
                    if (e2.IsDirectory)
                    {
                        if (!Directory.Exists(fullPath))
                        {
                            Directory.CreateDirectory(fullPath);
                            Log($"创建目录: {fullPath}");
                        }
                        else
                        {
                            Log($"目录已存在: {fullPath}");
                        }
                    }
                    else
                    {
                        string? parent = Path.GetDirectoryName(fullPath);
                        if (!string.IsNullOrEmpty(parent) && !Directory.Exists(parent))
                        {
                            Directory.CreateDirectory(parent);
                        }
                        if (!File.Exists(fullPath))
                        {
                            string referenceFilePath = TxtReferenceFile.Text.Trim();
                            if (File.Exists(referenceFilePath))
                            {
                                // 如果参考文件存在，复制参考文件内容
                                File.Copy(referenceFilePath, fullPath, true);
                                Log($"创建文件(从参考文件复制): {fullPath}");
                            }
                            else
                            {
                                // 否则创建空文件
                                using (File.Create(fullPath)) { }
                                Log($"创建文件(空文件): {fullPath}");
                            }
                        }
                        else
                        {
                            Log($"文件已存在: {fullPath}");
                        }
                    }
                }

                Log("创建完成。");
            }
            catch (Exception ex)
            {
                Log($"创建结构失败: {ex.Message}");
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BrowseReferenceFile_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new Microsoft.Win32.OpenFileDialog
            {
                Title = "选择参考文件",
                Filter = "所有文件 (*.*)|*.*"
            };
            if (ofd.ShowDialog() == true)
            {
                TxtReferenceFile.Text = ofd.FileName;
            }
        }

        private void TxtReferenceFile_DragEnter(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    e.Effects = System.Windows.DragDropEffects.Copy;
                }
                else
                {
                    e.Effects = System.Windows.DragDropEffects.None;
                }
            }
            else
            {
                e.Effects = System.Windows.DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void TxtReferenceFile_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    var path = files[0];
                    if (File.Exists(path))
                    {
                        TxtReferenceFile.Text = path;
                    }
                }
            }
            e.Handled = true;
        }

        private void TxtReferenceFile_PreviewDragEnter(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    e.Effects = System.Windows.DragDropEffects.Copy;
                }
                else
                {
                    e.Effects = System.Windows.DragDropEffects.None;
                }
            }
            else
            {
                e.Effects = System.Windows.DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void TxtReferenceFile_PreviewDragOver(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    e.Effects = System.Windows.DragDropEffects.Copy;
                }
                else
                {
                    e.Effects = System.Windows.DragDropEffects.None;
                }
            }
            else
            {
                e.Effects = System.Windows.DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void TxtReferenceFile_PreviewDrop(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    var path = files[0];
                    if (File.Exists(path))
                    {
                        TxtReferenceFile.Text = path;
                    }
                }
            }
            e.Handled = true;
        }

        #region 输出目录拖拽功能
        
        private void TxtOutputDir_DragEnter(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    e.Effects = System.Windows.DragDropEffects.Copy;
                }
                else
                {
                    e.Effects = System.Windows.DragDropEffects.None;
                }
            }
            else
            {
                e.Effects = System.Windows.DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void TxtOutputDir_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    var path = files[0];
                    // 检查是否为目录或文件，如果是文件则使用其所在目录
                    if (Directory.Exists(path))
                    {
                        TxtOutputDir.Text = path;
                    }
                    else if (File.Exists(path))
                    {
                        TxtOutputDir.Text = Path.GetDirectoryName(path);
                    }
                }
            }
            e.Handled = true;
        }

        private void TxtOutputDir_PreviewDragEnter(object sender, System.Windows.DragEventArgs e)
        {
            // 与 TxtOutputDir_DragEnter 相同逻辑，提升可靠性
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    e.Effects = System.Windows.DragDropEffects.Copy;
                }
                else
                {
                    e.Effects = System.Windows.DragDropEffects.None;
                }
            }
            else
            {
                e.Effects = System.Windows.DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void TxtOutputDir_PreviewDragOver(object sender, System.Windows.DragEventArgs e)
        {
            // 持续反馈允许状态
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    e.Effects = System.Windows.DragDropEffects.Copy;
                }
                else
                {
                    e.Effects = System.Windows.DragDropEffects.None;
                }
            }
            else
            {
                e.Effects = System.Windows.DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void TxtOutputDir_PreviewDrop(object sender, System.Windows.DragEventArgs e)
        {
            // 与 TxtOutputDir_Drop 一致
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    var path = files[0];
                    // 检查是否为目录或文件，如果是文件则使用其所在目录
                    if (Directory.Exists(path))
                    {
                        TxtOutputDir.Text = path;
                    }
                    else if (File.Exists(path))
                    {
                        TxtOutputDir.Text = Path.GetDirectoryName(path);
                    }
                }
            }
            e.Handled = true;
        }
        
        #endregion

        private void Log(string msg)
        {
            TxtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}\r\n");
            TxtLog.ScrollToEnd();
        }

        private static readonly Regex SizeTimeMetadataRegex = new Regex("\\s*\\(\\d+(?:\\.\\d+)?\\s*(?:B|KB|MB|GB|TB),\\s*\\d{4}-\\d{2}-\\d{2} \\d{2}:\\d{2}:\\d{2}\\)\\s*$", RegexOptions.Compiled);
        private static readonly Regex TimeMetadataRegex = new Regex("\\s*\\(\\d{4}-\\d{2}-\\d{2} \\d{2}:\\d{2}:\\d{2}\\)\\s*$", RegexOptions.Compiled);
        private static readonly Regex SizeMetadataRegex = new Regex("\\s*\\(\\d+(?:\\.\\d+)?\\s*(?:B|KB|MB|GB|TB)\\)\\s*$", RegexOptions.Compiled);

        // 只移除本工具导出的大小/时间元数据括号，不误删名称中合法的末尾括号
        private static string RemoveTrailingMetadata(string line)
        {
            if (SizeTimeMetadataRegex.IsMatch(line)) return SizeTimeMetadataRegex.Replace(line, string.Empty);
            if (TimeMetadataRegex.IsMatch(line)) return TimeMetadataRegex.Replace(line, string.Empty);
            if (SizeMetadataRegex.IsMatch(line)) return SizeMetadataRegex.Replace(line, string.Empty);
            return line;
        }

        // 检测 TXT 是否为树形结构（含 "├── " 或 "└── " 连接符）
        private bool IsTreeFormat(string[] lines)
        {
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].TrimEnd();
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.Contains("├── ") || line.Contains("└── "))
                {
                    return true;
                }
            }
            return false;
        }

        // 解析完整路径格式：每行为完整绝对路径，目录以 "/" 或 "\" 结尾
        private List<TreeEntry> ParsePathList(string[] allLines, string rootAbs)
        {
            var result = new List<TreeEntry>();
            if (allLines.Length <= 1) return result;

            string normalizedRoot = rootAbs.TrimEnd('/', '\\');

            for (int i = 1; i < allLines.Length; i++)
            {
                string line = allLines[i].TrimEnd();
                if (string.IsNullOrWhiteSpace(line)) continue;

                // 只移除导出元数据（大小/时间）括号
                line = RemoveTrailingMetadata(line);

                // 目录以 "/" 或 "\" 结尾
                bool isDir = line.EndsWith("/") || line.EndsWith("\\");
                if (isDir)
                {
                    line = line.TrimEnd('/', '\\');
                }

                // 去掉根前缀得到相对路径；若不以根路径开头（手工编辑为相对路径等）则保留原字符串
                string relPath = line;
                if (!string.IsNullOrEmpty(normalizedRoot) &&
                    line.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
                {
                    relPath = line.Substring(normalizedRoot.Length).TrimStart('/', '\\');
                }

                // 若该行就是根路径本身，跳过
                if (string.IsNullOrEmpty(relPath)) continue;

                result.Add(new TreeEntry { RelativePath = relPath, IsDirectory = isDir });
            }

            return result;
        }

        private List<TreeEntry> ParseTreeLines(string[] allLines)
        {
            var result = new List<TreeEntry>();
            if (allLines.Length <= 1) return result;

            // 预先计算每一行的深度与名称
            var items = new List<(int depth, string name, bool isDirFromMarker)>();
            for (int i = 1; i < allLines.Length; i++)
            {
                string line = allLines[i].TrimEnd();
                if (string.IsNullOrWhiteSpace(line)) continue;

                // 只移除导出元数据（大小/时间）括号，保留名称中合法的末尾括号
                line = RemoveTrailingMetadata(line);

                int idx = line.IndexOf("└── ", StringComparison.Ordinal);
                if (idx < 0) idx = line.IndexOf("├── ", StringComparison.Ordinal);
                if (idx < 0) continue; // 非树形行，跳过

                string prefix = line.Substring(0, idx);
                string name = line.Substring(idx + 4).Trim();

                // 识别目录标记：名称以 "/" 或 "\" 结尾视为目录（新导出格式）
                bool isDirFromMarker = name.EndsWith("/") || name.EndsWith("\\");
                if (isDirFromMarker)
                {
                    name = name.TrimEnd('/', '\\');
                }

                // 计算深度：按每4字符一组（"│   "或"    ")统计
                int depth = 0;
                for (int p = 0; p + 4 <= prefix.Length; p += 4)
                {
                    depth++;
                }

                items.Add((depth, name, isDirFromMarker));
            }

            // 使用下一个条目的深度判断当前是否是目录
            var stack = new Stack<string>(); // 存放目录路径片段

            for (int i = 0; i < items.Count; i++)
            {
                var (depth, name, isDirFromMarker) = items[i];
                int nextDepth = (i + 1 < items.Count) ? items[i + 1].depth : -1;
                // 目录标记优先；旧格式无标记时仍按下一行深度判断
                bool isDir = isDirFromMarker || nextDepth > depth;

                // 调整栈，使其大小 == depth
                while (stack.Count > depth)
                    stack.Pop();

                string relPath;
                if (stack.Count == 0)
                {
                    relPath = name; // 第一层：直接是子项
                }
                else
                {
                    relPath = Path.Combine(string.Join(Path.DirectorySeparatorChar, stack.ToArray()), name);
                    // 注意：stack.ToArray()顺序为栈从顶到底，需反转
                    var arr = stack.ToArray();
                    Array.Reverse(arr);
                    relPath = Path.Combine(string.Join(Path.DirectorySeparatorChar, arr), name);
                }

                result.Add(new TreeEntry { RelativePath = relPath, IsDirectory = isDir });

                if (isDir)
                {
                    // 入栈目录名
                    // 先同步一次正确的相对路径片段栈（仅存储目录名片段）
                    while (stack.Count > depth)
                        stack.Pop();
                    stack.Push(name);
                }
            }

            return result;
        }

        private class TreeEntry
        {
            public string RelativePath { get; set; } = string.Empty;
            public bool IsDirectory { get; set; }
        }
    }
}
