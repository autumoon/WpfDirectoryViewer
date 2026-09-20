using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using WPF = System.Windows;
using WPFControls = System.Windows.Controls;
using WPFInput = System.Windows.Input;
using Microsoft.Win32;
using WPFInterop = System.Windows.Interop;
using WPFMedia = System.Windows.Media;
using Forms = System.Windows.Forms;

namespace WpfDirectoryViewer;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : WPF.Window
{
    private string _currentDirectoryPath;
    private string _outputDirectoryPath;
    private Configuration _configuration;

    public MainWindow()
    {
        InitializeComponent();
        
        // 加载配置文件
        _configuration = Configuration.Load();
        
        // 设置从配置中读取的值
        // 当前目录：仅当路径有效时才填入并加载
        var savedCurrent = _configuration.CurrentDirectoryPath;
        if (!string.IsNullOrWhiteSpace(savedCurrent) && Directory.Exists(savedCurrent))
        {
            _currentDirectoryPath = savedCurrent;
            CurrentPathTextBlock.Text = _currentDirectoryPath;
        }
        else
        {
            _currentDirectoryPath = string.Empty;
            CurrentPathTextBlock.Text = string.Empty;
        }
        
        // 输出目录：仅当路径有效时才填入
        var savedOutput = _configuration.OutputDirectoryPath;
        if (!string.IsNullOrWhiteSpace(savedOutput) && Directory.Exists(savedOutput))
        {
            _outputDirectoryPath = savedOutput;
            OutputPathTextBlock.Text = _outputDirectoryPath;
        }
        else
        {
            _outputDirectoryPath = string.Empty;
            OutputPathTextBlock.Text = string.Empty;
        }
        
        // 设置导出格式复选框状态
        ExportTxtCheckBox.IsChecked = _configuration.ExportTxt;
        ExportMdCheckBox.IsChecked = _configuration.ExportMd;
        ExportHtmlCheckBox.IsChecked = _configuration.ExportHtml;
        
        // 设置显示选项复选框状态
        ShowFileSizeCheckBox.IsChecked = _configuration.ShowFileSize;
        ShowLastModifiedCheckBox.IsChecked = _configuration.ShowLastModified;
        
        // 设置输出形式单选按钮状态
        TreeFormatRadioButton.IsChecked = !_configuration.ExportTxtAsPathList;
        PathListFormatRadioButton.IsChecked = _configuration.ExportTxtAsPathList;
        
        LoadDirectoryStructure(_currentDirectoryPath);
        
        // 设置窗口启动时居中显示，考虑多显示器和DPI缩放
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
            Rectangle workingArea = Forms.Screen.FromHandle(helper.Handle).WorkingArea;
            
            // 计算窗口在当前屏幕上的居中位置
            // 将屏幕工作区坐标从物理像素转换为WPF逻辑单位
            double screenLeft = workingArea.Left / dpiX;
            double screenTop = workingArea.Top / dpiY;
            double screenWidth = workingArea.Width / dpiX;
            double screenHeight = workingArea.Height / dpiY;
            
            // 计算窗口左上角位置，使其在屏幕上居中
            double left = screenLeft + (screenWidth - this.Width) / 2;
            double top = screenTop + (screenHeight - this.Height) / 2;
            
            // 设置窗口位置
            this.Left = left;
            this.Top = top;
        };
        
        // 注册窗口关闭事件，保存配置
        this.Closing += MainWindow_Closing;
    }
    
    // 窗口关闭时保存配置
    private void MainWindow_Closing(object sender, CancelEventArgs e)
    {
        // 更新配置
        _configuration.CurrentDirectoryPath = _currentDirectoryPath;
        _configuration.OutputDirectoryPath = _outputDirectoryPath;
        _configuration.ExportTxt = ExportTxtCheckBox.IsChecked ?? true;
        _configuration.ExportMd = ExportMdCheckBox.IsChecked ?? true;
        _configuration.ExportHtml = ExportHtmlCheckBox.IsChecked ?? true;
        _configuration.ShowFileSize = ShowFileSizeCheckBox.IsChecked ?? true;
        _configuration.ShowLastModified = ShowLastModifiedCheckBox.IsChecked ?? true;
        _configuration.ExportTxtAsPathList = PathListFormatRadioButton.IsChecked == true;
        
        // 保存配置到文件
        _configuration.Save();

        // 托盘生命周期：非退出状态下点击关闭按钮时隐藏到托盘，不退出程序
        var app = WPF.Application.Current as App;
        if (app != null && !app.IsExiting)
        {
            e.Cancel = true;
            this.Hide();
        }
    }
    
    private void OutputBrowseButton_Click(object sender, WPF.RoutedEventArgs e)
    {
        var folderBrowserDialog = new OpenFolderDialog
        {
            Title = "选择输出目录",
            InitialDirectory = _outputDirectoryPath
        };

        if (folderBrowserDialog.ShowDialog() == true)
        {
            _outputDirectoryPath = folderBrowserDialog.FolderName;
            OutputPathTextBlock.Text = _outputDirectoryPath;
        }
    }


    private void CurrentPathTextBlock_DragEnter(object sender, WPF.DragEventArgs e)
    {
        if (e.Data.GetDataPresent(WPF.DataFormats.FileDrop))
        {
            e.Effects = WPF.DragDropEffects.Copy;
        }
        else
        {
            e.Effects = WPF.DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void OutputPathTextBlock_DragEnter(object sender, WPF.DragEventArgs e)
    {
        if (e.Data.GetDataPresent(WPF.DataFormats.FileDrop))
        {
            e.Effects = WPF.DragDropEffects.Copy;
        }
        else
        {
            e.Effects = WPF.DragDropEffects.None;
        }
        e.Handled = true;
    }
    
    private void OutputPathTextBlock_Drop(object sender, WPF.DragEventArgs e)
    {
        if (e.Data.GetDataPresent(WPF.DataFormats.FileDrop))
        {
            string[] droppedFiles = (string[])e.Data.GetData(WPF.DataFormats.FileDrop);
            if (droppedFiles != null && droppedFiles.Length > 0)
            {
                string droppedPath = droppedFiles[0];
                if (Directory.Exists(droppedPath))
                {
                    _outputDirectoryPath = droppedPath;
                    OutputPathTextBlock.Text = _outputDirectoryPath;
                }
                else
                {
                    WPF.MessageBox.Show("请拖拽一个有效的目录。", "提示", WPF.MessageBoxButton.OK, WPF.MessageBoxImage.Information);
                }
            }
        }
        e.Handled = true;
    }

    private void CurrentPathTextBlock_Drop(object sender, WPF.DragEventArgs e)
    {
        if (e.Data.GetDataPresent(WPF.DataFormats.FileDrop))
        {
            string[] droppedFiles = (string[])e.Data.GetData(WPF.DataFormats.FileDrop);
            if (droppedFiles != null && droppedFiles.Length > 0)
            {
                string droppedPath = droppedFiles[0];
                if (Directory.Exists(droppedPath))
                {
                    _currentDirectoryPath = droppedPath;
                    CurrentPathTextBlock.Text = _currentDirectoryPath;
                    
                    // 自动设置输出目录为上一级目录
                    string parentDirectory = Directory.GetParent(_currentDirectoryPath)?.FullName ?? _currentDirectoryPath;
                    _outputDirectoryPath = parentDirectory;
                    OutputPathTextBlock.Text = _outputDirectoryPath;
                    
if (!string.IsNullOrEmpty(_currentDirectoryPath))
        {
            LoadDirectoryStructure(_currentDirectoryPath);
        }
                }
                else
                {
                    WPF.MessageBox.Show("请拖拽一个有效的目录。", "提示", WPF.MessageBoxButton.OK, WPF.MessageBoxImage.Information);
                }
            }
        }
        e.Handled = true;
    }

    // 导出格式复选框的事件处理程序
        private void ExportFormatCheckBox_Checked(object sender, WPF.RoutedEventArgs e)
        {
            // 无需特殊处理，配置将在窗口关闭时自动保存
        }
        
        private void ExportButton_Click(object sender, WPF.RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(_currentDirectoryPath) || !Directory.Exists(_currentDirectoryPath))
                {
                    WPF.MessageBox.Show("请先选择一个有效的目录。", "提示", WPF.MessageBoxButton.OK, WPF.MessageBoxImage.Information);
                    return;
                }
                
                if (string.IsNullOrEmpty(_outputDirectoryPath) || !Directory.Exists(_outputDirectoryPath))
                {
                    WPF.MessageBox.Show("请先设置一个有效的输出目录。", "提示", WPF.MessageBoxButton.OK, WPF.MessageBoxImage.Information);
                    return;
                }

                Cursor = WPFInput.Cursors.Wait;
                
                // 获取完整的目录结构
                var rootItem = DirectoryItem.CreateDirectoryItem(_currentDirectoryPath);
                
                // 获取当前目录名称作为文件名
                string directoryName = Path.GetFileName(_currentDirectoryPath);
                if (string.IsNullOrEmpty(directoryName))
                {
                    directoryName = "root";
                }
                
                // 获取复选框状态
                bool exportTxt = ExportTxtCheckBox.IsChecked ?? true;
                bool exportMd = ExportMdCheckBox.IsChecked ?? true;
                bool exportHtml = ExportHtmlCheckBox.IsChecked ?? true;
                bool showFileSize = ShowFileSizeCheckBox.IsChecked ?? true;
                bool showLastModified = ShowLastModifiedCheckBox.IsChecked ?? true;
                bool exportAsPathList = PathListFormatRadioButton.IsChecked == true;
                
                // 检查是否至少选择了一种导出格式
                if (!exportTxt && !exportMd && !exportHtml)
                {
                    WPF.MessageBox.Show("请至少选择一种导出格式。", "提示", WPF.MessageBoxButton.OK, WPF.MessageBoxImage.Information);
                    return;
                }
                
                // 构建导出的文件路径列表，用于显示成功消息
                List<string> exportedFiles = new List<string>();
                
                // 根据配置导出文件
                if (exportTxt)
                {
                    string txtFilePath = Path.Combine(_outputDirectoryPath, $"{directoryName}.txt");
                    if (exportAsPathList)
                    {
                        ExportToPathList(rootItem, txtFilePath, showFileSize, showLastModified);
                    }
                    else
                    {
                        ExportToTxt(rootItem, txtFilePath, showFileSize, showLastModified);
                    }
                    exportedFiles.Add(txtFilePath);
                }
                
                if (exportMd)
                {
                    string mdFilePath = Path.Combine(_outputDirectoryPath, $"{directoryName}.md");
                    if (exportAsPathList)
                    {
                        ExportToMarkdownPathList(rootItem, mdFilePath, showFileSize, showLastModified);
                    }
                    else
                    {
                        ExportToMarkdown(rootItem, mdFilePath, showFileSize, showLastModified);
                    }
                    exportedFiles.Add(mdFilePath);
                }
                
                if (exportHtml)
                {
                    string htmlFilePath = Path.Combine(_outputDirectoryPath, $"{directoryName}.html");
                    if (exportAsPathList)
                    {
                        ExportToHtmlPathList(rootItem, htmlFilePath, showFileSize, showLastModified);
                    }
                    else
                    {
                        ExportToHtml(rootItem, htmlFilePath, showFileSize, showLastModified);
                    }
                    exportedFiles.Add(htmlFilePath);
                }
                
                // 显示导出成功消息
                WPF.MessageBox.Show("导出成功！\n\n已导出文件到：\n" + string.Join("\n", exportedFiles),
                                "成功", WPF.MessageBoxButton.OK, WPF.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                WPF.MessageBox.Show($"导出目录结构时发生错误: {ex.Message}", "错误", WPF.MessageBoxButton.OK, WPF.MessageBoxImage.Error);
            }
            finally
            {
                Cursor = WPFInput.Cursors.Arrow;
            }
        }

        private void Window_MouseDoubleClick(object sender, WPF.Input.MouseButtonEventArgs e)
        {
            // 检查点击的是否是窗口背景（Grid或Window本身），而不是其他控件
            if (e.OriginalSource is WPFControls.Grid || e.OriginalSource is WPFControls.Border || e.OriginalSource is WPF.Window)
            {
                // 调用导出按钮的点击事件处理程序
                ExportButton_Click(sender, new WPF.RoutedEventArgs());
            }
        }

        
        // 添加复选框状态改变的事件处理程序，刷新TreeView显示
        private void ShowFileSizeCheckBox_Checked(object sender, WPF.RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_currentDirectoryPath))
            {
                LoadDirectoryStructure(_currentDirectoryPath);
            }
        }
        
        private void ShowLastModifiedCheckBox_Checked(object sender, WPF.RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_currentDirectoryPath))
            {
                LoadDirectoryStructure(_currentDirectoryPath);
            }
        }

    private void BrowseButton_Click(object sender, WPF.RoutedEventArgs e)
    {
        var folderBrowserDialog = new OpenFolderDialog
        {
            Title = "选择目录",
            InitialDirectory = _currentDirectoryPath
        };

        if (folderBrowserDialog.ShowDialog() == true)
        {
            _currentDirectoryPath = folderBrowserDialog.FolderName;
            CurrentPathTextBlock.Text = _currentDirectoryPath;
            
            // 自动设置输出目录为上一级目录
            string parentDirectory = Directory.GetParent(_currentDirectoryPath)?.FullName ?? _currentDirectoryPath;
            _outputDirectoryPath = parentDirectory;
            OutputPathTextBlock.Text = _outputDirectoryPath;
            
            LoadDirectoryStructure(_currentDirectoryPath);
        }
    }

    private void RefreshButton_Click(object sender, WPF.RoutedEventArgs e)
    {
        LoadDirectoryStructure(_currentDirectoryPath);
    }

    private void LoadDirectoryStructure(string path)
    {
        try
        {
            Cursor = WPFInput.Cursors.Wait;
            DirectoryTreeView.Items.Clear();
            
            if (Directory.Exists(path))
            {
                var rootItem = DirectoryItem.CreateDirectoryItem(path);
                DirectoryTreeView.Items.Add(rootItem);
                
                // 默认展开根节点
                if (DirectoryTreeView.Items.Count > 0)
                {
                    var rootNode = DirectoryTreeView.ItemContainerGenerator.ContainerFromIndex(0) as WPFControls.TreeViewItem;
                    if (rootNode != null)
                    {
                        rootNode.IsExpanded = true;
                    }
                }
            }
            // 目录不存在时忽略，不显示错误消息
        }
        catch (Exception ex)
        {
            WPF.MessageBox.Show($"加载目录结构时发生错误: {ex.Message}", "错误", WPF.MessageBoxButton.OK, WPF.MessageBoxImage.Error);
        }
        finally
        {
            Cursor = WPFInput.Cursors.Arrow;
        }
    }

    private void ExportToHtml(DirectoryItem rootItem, string filePath, bool showFileSize, bool showLastModified)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("<!DOCTYPE html>");
                sb.AppendLine("<html>");
                sb.AppendLine("<head>");
                sb.AppendLine("<meta charset='utf-8'>");
                sb.AppendLine("<title>目录结构 - " + rootItem.Name + "</title>");
                sb.AppendLine("<style>");
                sb.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
                sb.AppendLine(".folder { color: #0066cc; font-weight: bold; }");
                sb.AppendLine(".file { color: #333; }");
                sb.AppendLine(".indent { margin-left: 20px; }");
                sb.AppendLine("</style>");
                sb.AppendLine("</head>");
                sb.AppendLine("<body>");
                sb.AppendLine("<h1>目录结构: " + rootItem.FullPath + "</h1>");
                
                // 生成目录结构HTML
                GenerateHtmlStructure(sb, rootItem, 0, showFileSize, showLastModified);
                
                sb.AppendLine("</body>");
                sb.AppendLine("</html>");
                
                // 确保目录存在
                string directory = Path.GetDirectoryName(filePath) ?? string.Empty;
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    Console.WriteLine($"创建目录: {directory}");
                }
                
                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
                Console.WriteLine($"ExportToHtml: 成功写入文件 {filePath}");
            } 
            catch (Exception ex)
            {
                Console.WriteLine($"ExportToHtml: 写入文件 {filePath} 时发生错误: {ex.Message}");
                throw; // 重新抛出异常，让上层处理
            }
        }

    private string FormatFileSize(long sizeInBytes)
        {
            if (sizeInBytes == 0)
                return "0 B";
                
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = sizeInBytes;
            
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size = size / 1024;
            }
            
            return $"{size:0.##} {sizes[order]}";
        }
        
        private void GenerateHtmlStructure(StringBuilder sb, DirectoryItem item, int level, bool showFileSize, bool showLastModified)
        {
            string indent = new string(' ', level * 4);
            string cssClass = item.IsDirectory ? "folder" : "file";
            string icon = item.IsDirectory ? "📁" : "📄";
            
            string additionalInfo = string.Empty;
            if (item.LastModified > DateTime.MinValue && showLastModified)
            {
                string modifyTime = item.LastModified.ToString("yyyy-MM-dd HH:mm:ss");
                if (item.IsDirectory)
                {
                    additionalInfo = $" <span style='color: #666; font-size: 0.8em;'>(修改时间: {modifyTime})</span>";
                }
                else if (showFileSize)
                {
                    string fileSize = FormatFileSize(item.FileSize);
                    additionalInfo = $" <span style='color: #666; font-size: 0.8em;'>({fileSize}, 修改时间: {modifyTime})</span>";
                }
                else
                {
                    additionalInfo = $" <span style='color: #666; font-size: 0.8em;'>(修改时间: {modifyTime})</span>";
                }
            }
            else if (!item.IsDirectory && showFileSize && item.FileSize > 0)
            {
                string fileSize = FormatFileSize(item.FileSize);
                additionalInfo = $" <span style='color: #666; font-size: 0.8em;'>({fileSize})</span>";
            }
            
            sb.AppendLine($"<div class='{cssClass}'>{indent}{icon} {item.Name}{additionalInfo}</div>");
            
            if (item.IsDirectory && item.Children != null)
            {
                sb.AppendLine($"<div class='indent'>");
                foreach (var child in item.Children)
                {
                    GenerateHtmlStructure(sb, child, level + 1, showFileSize, showLastModified);
                }
                sb.AppendLine($"</div>");
            }
        }

    private void ExportToHtmlPathList(DirectoryItem rootItem, string filePath, bool showFileSize, bool showLastModified)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("<!DOCTYPE html>");
                sb.AppendLine("<html>");
                sb.AppendLine("<head>");
                sb.AppendLine("<meta charset='utf-8'>");
                sb.AppendLine("<title>目录结构 - " + rootItem.Name + "</title>");
                sb.AppendLine("<style>");
                sb.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
                sb.AppendLine(".folder { color: #0066cc; font-weight: bold; }");
                sb.AppendLine(".file { color: #333; }");
                sb.AppendLine("</style>");
                sb.AppendLine("</head>");
                sb.AppendLine("<body>");
                sb.AppendLine("<h1>目录结构: " + rootItem.FullPath + "</h1>");
                
                // 生成完整路径格式的目录结构HTML（根目录本身不输出）
                GenerateHtmlPathList(sb, rootItem, true, showFileSize, showLastModified);
                
                sb.AppendLine("</body>");
                sb.AppendLine("</html>");
                
                // 确保目录存在
                string directory = Path.GetDirectoryName(filePath) ?? string.Empty;
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    Console.WriteLine($"创建目录: {directory}");
                }
                
                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
                Console.WriteLine($"ExportToHtmlPathList: 成功写入文件 {filePath}");
            } 
            catch (Exception ex)
            {
                Console.WriteLine($"ExportToHtmlPathList: 写入文件 {filePath} 时发生错误: {ex.Message}");
                throw; // 重新抛出异常，让上层处理
            }
        }

    private void GenerateHtmlPathList(StringBuilder sb, DirectoryItem item, bool isRoot, bool showFileSize, bool showLastModified)
        {
            // 根目录本身不输出（第一行由 ExportToHtmlPathList 输出）
            if (!isRoot)
            {
                string cssClass = item.IsDirectory ? "folder" : "file";
                string icon = item.IsDirectory ? "📁" : "📄";
                
                // 构建附加信息（文件大小和修改时间）——与 GenerateHtmlStructure 一致
                string additionalInfo = string.Empty;
                if (item.LastModified > DateTime.MinValue && showLastModified)
                {
                    string modifyTime = item.LastModified.ToString("yyyy-MM-dd HH:mm:ss");
                    if (item.IsDirectory)
                    {
                        additionalInfo = $" <span style='color: #666; font-size: 0.8em;'>(修改时间: {modifyTime})</span>";
                    }
                    else if (showFileSize)
                    {
                        string fileSize = FormatFileSize(item.FileSize);
                        additionalInfo = $" <span style='color: #666; font-size: 0.8em;'>({fileSize}, 修改时间: {modifyTime})</span>";
                    }
                    else
                    {
                        additionalInfo = $" <span style='color: #666; font-size: 0.8em;'>(修改时间: {modifyTime})</span>";
                    }
                }
                else if (!item.IsDirectory && showFileSize && item.FileSize > 0)
                {
                    string fileSize = FormatFileSize(item.FileSize);
                    additionalInfo = $" <span style='color: #666; font-size: 0.8em;'>({fileSize})</span>";
                }
                
                // 目录以 "/" 结尾，输出项完整绝对路径，不使用缩进嵌套
                string displayName = item.IsDirectory ? item.FullPath + "/" : item.FullPath;
                sb.AppendLine($"<div class='{cssClass}'>{icon} {displayName}{additionalInfo}</div>");
            }
            
            // 递归处理子项
            if (item.IsDirectory && item.Children != null)
            {
                foreach (var child in item.Children)
                {
                    GenerateHtmlPathList(sb, child, false, showFileSize, showLastModified);
                }
            }
        }

    private void ExportToTxt(DirectoryItem rootItem, string filePath, bool showFileSize, bool showLastModified)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine(rootItem.FullPath);
                
                // 生成ASCII树状图格式的目录结构
                GenerateTxtStructure(sb, rootItem, 0, new List<bool>(), showFileSize, showLastModified);
                
                // 确保目录存在
                string directory = Path.GetDirectoryName(filePath) ?? string.Empty;
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    Console.WriteLine($"创建目录: {directory}");
                }
            
            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            Console.WriteLine($"ExportToTxt: 成功写入文件 {filePath}");
        } 
        catch (Exception ex)
        {
            Console.WriteLine($"ExportToTxt: 写入文件 {filePath} 时发生错误: {ex.Message}");
            throw; // 重新抛出异常，让上层处理
        }
    }

    private void GeneratePathList(StringBuilder sb, DirectoryItem item, bool isRoot, bool showFileSize, bool showLastModified)
    {
        // 根目录本身不输出（第一行由 ExportToPathList 输出）
        if (!isRoot)
        {
            // 构建附加信息（文件大小和修改时间）——与 GenerateTxtStructure 一致
            string additionalInfo = string.Empty;
            if (item.LastModified > DateTime.MinValue && showLastModified)
            {
                string modifyTime = item.LastModified.ToString("yyyy-MM-dd HH:mm:ss");
                if (!item.IsDirectory && showFileSize)
                {
                    string fileSize = FormatFileSize(item.FileSize);
                    additionalInfo = $"  ({fileSize}, {modifyTime})";
                }
                else
                {
                    additionalInfo = $"  ({modifyTime})";
                }
            }
            else if (!item.IsDirectory && showFileSize && item.FileSize > 0)
            {
                string fileSize = FormatFileSize(item.FileSize);
                additionalInfo = $"  ({fileSize})";
            }

            // 目录以 "/" 结尾，输出项完整绝对路径
            string displayName = item.IsDirectory ? item.FullPath + "/" : item.FullPath;
            sb.AppendLine($"{displayName}{additionalInfo}");
        }

        // 递归处理子项
        if (item.IsDirectory && item.Children != null)
        {
            foreach (var child in item.Children)
            {
                GeneratePathList(sb, child, false, showFileSize, showLastModified);
            }
        }
    }

    private void ExportToPathList(DirectoryItem rootItem, string filePath, bool showFileSize, bool showLastModified)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine(rootItem.FullPath);
                
                // 生成完整路径格式的目录结构（根目录本身不输出）
                GeneratePathList(sb, rootItem, true, showFileSize, showLastModified);
                
                // 确保目录存在
                string directory = Path.GetDirectoryName(filePath) ?? string.Empty;
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    Console.WriteLine($"创建目录: {directory}");
                }
            
            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            Console.WriteLine($"ExportToPathList: 成功写入文件 {filePath}");
        } 
        catch (Exception ex)
        {
            Console.WriteLine($"ExportToPathList: 写入文件 {filePath} 时发生错误: {ex.Message}");
            throw; // 重新抛出异常，让上层处理
        }
    }

    private void GenerateTxtStructure(StringBuilder sb, DirectoryItem item, int level, List<bool> isLastAtEachLevel, bool showFileSize, bool showLastModified)
    {
        // 根目录特殊处理
        if (level == 0)
        {
            // 根目录已经在ExportToTxt中显示，这里只需要处理子项
            if (item.IsDirectory && item.Children != null)
            {
                for (int i = 0; i < item.Children.Count; i++)
                {
                    bool childIsLast = (i == item.Children.Count - 1);
                    List<bool> newIsLastAtEachLevel = new List<bool>(isLastAtEachLevel);
                    newIsLastAtEachLevel.Add(childIsLast);
                    GenerateTxtStructure(sb, item.Children[i], level + 1, newIsLastAtEachLevel, showFileSize, showLastModified);
                }
            }
            return;
        }

        // 构建前缀和缩进
        StringBuilder indentBuilder = new StringBuilder();
        for (int i = 0; i < level - 1; i++)
        {
            if (i < isLastAtEachLevel.Count && isLastAtEachLevel[i])
                indentBuilder.Append("    "); // 上一级是最后一个元素，这里用空格
            else
                indentBuilder.Append("│   "); // 上一级不是最后一个元素，这里用垂直线
        }

        // 添加当前级别的连接符
        bool isLast = level > 0 && isLastAtEachLevel.Count >= level && isLastAtEachLevel[level - 1];
        string connector = isLast ? "└── " : "├── ";
        indentBuilder.Append(connector);

        // 构建附加信息（文件大小和修改时间）
        string additionalInfo = string.Empty;
        if (item.LastModified > DateTime.MinValue && showLastModified)
        {
            string modifyTime = item.LastModified.ToString("yyyy-MM-dd HH:mm:ss");
            if (!item.IsDirectory && showFileSize)
            {
                string fileSize = FormatFileSize(item.FileSize);
                additionalInfo = $"  ({fileSize}, {modifyTime})";
            }
            else
            {
                additionalInfo = $"  ({modifyTime})";
            }
        }
        else if (!item.IsDirectory && showFileSize && item.FileSize > 0)
        {
            string fileSize = FormatFileSize(item.FileSize);
            additionalInfo = $"  ({fileSize})";
        }

        // 添加当前项
        // 目录名以 "/" 结尾作为标记，便于反向重建识别空目录
        string displayName = item.IsDirectory ? item.Name + "/" : item.Name;
        sb.AppendLine($"{indentBuilder}{displayName}{additionalInfo}");

        // 递归处理子项
        if (item.IsDirectory && item.Children != null)
        {
            for (int i = 0; i < item.Children.Count; i++)
            {
                bool childIsLast = (i == item.Children.Count - 1);
                List<bool> newIsLastAtEachLevel = new List<bool>(isLastAtEachLevel);
                newIsLastAtEachLevel.Add(childIsLast);
                GenerateTxtStructure(sb, item.Children[i], level + 1, newIsLastAtEachLevel, showFileSize, showLastModified);
            }
        }
    }

    private void ExportToMarkdown(DirectoryItem rootItem, string filePath, bool showFileSize, bool showLastModified)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("# 目录结构: " + rootItem.FullPath);
                sb.AppendLine();
                
                // 生成Markdown格式的目录结构
                GenerateMarkdownStructure(sb, rootItem, 0, showFileSize, showLastModified);
                
                // 确保目录存在
                string directory = Path.GetDirectoryName(filePath) ?? string.Empty;
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    Console.WriteLine($"创建目录: {directory}");
                }
                
                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
                Console.WriteLine($"ExportToMarkdown: 成功写入文件 {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ExportToMarkdown: 写入文件 {filePath} 时发生错误: {ex.Message}");
                throw; // 重新抛出异常，让上层处理
            }
        }

    private void GenerateMarkdownStructure(StringBuilder sb, DirectoryItem item, int level, bool showFileSize, bool showLastModified)
        {
            // 根目录特殊处理
            if (level == 0)
            {
                // 根目录已经在ExportToMarkdown中显示为标题
                if (item.IsDirectory && item.Children != null)
                {
                    foreach (var child in item.Children)
                    {
                        GenerateMarkdownStructure(sb, child, level + 1, showFileSize, showLastModified);
                    }
                }
                return;
            }

            string indent = new string(' ', (level - 1) * 4);
            string prefix = item.IsDirectory ? "📁 " : "📄 ";
            
            string additionalInfo = string.Empty;
            if (item.LastModified > DateTime.MinValue && showLastModified)
            {
                string modifyTime = item.LastModified.ToString("yyyy-MM-dd HH:mm:ss");
                if (!item.IsDirectory && showFileSize)
                {
                    string fileSize = FormatFileSize(item.FileSize);
                    additionalInfo = $"  ({fileSize}, {modifyTime})";
                }
                else
                {
                    additionalInfo = $"  ({modifyTime})";
                }
            }
            else if (!item.IsDirectory && showFileSize && item.FileSize > 0)
            {
                string fileSize = FormatFileSize(item.FileSize);
                additionalInfo = $"  ({fileSize})";
            }
            
            sb.AppendLine($"{indent}- {prefix}{item.Name}{additionalInfo}");
            
            if (item.IsDirectory && item.Children != null)
            {
                foreach (var child in item.Children)
                {
                    GenerateMarkdownStructure(sb, child, level + 1, showFileSize, showLastModified);
                }
            }
        }

    private void ExportToMarkdownPathList(DirectoryItem rootItem, string filePath, bool showFileSize, bool showLastModified)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("# 目录结构: " + rootItem.FullPath);
                sb.AppendLine();
                
                // 生成完整路径格式的目录结构Markdown（根目录本身不输出）
                GenerateMarkdownPathList(sb, rootItem, true, showFileSize, showLastModified);
                
                // 确保目录存在
                string directory = Path.GetDirectoryName(filePath) ?? string.Empty;
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    Console.WriteLine($"创建目录: {directory}");
                }
                
                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
                Console.WriteLine($"ExportToMarkdownPathList: 成功写入文件 {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ExportToMarkdownPathList: 写入文件 {filePath} 时发生错误: {ex.Message}");
                throw; // 重新抛出异常，让上层处理
            }
        }

    private void GenerateMarkdownPathList(StringBuilder sb, DirectoryItem item, bool isRoot, bool showFileSize, bool showLastModified)
        {
            // 根目录本身不输出（标题由 ExportToMarkdownPathList 输出）
            if (!isRoot)
            {
                string prefix = item.IsDirectory ? "📁 " : "📄 ";
                
                // 构建附加信息（文件大小和修改时间）——与 GenerateMarkdownStructure 一致
                string additionalInfo = string.Empty;
                if (item.LastModified > DateTime.MinValue && showLastModified)
                {
                    string modifyTime = item.LastModified.ToString("yyyy-MM-dd HH:mm:ss");
                    if (!item.IsDirectory && showFileSize)
                    {
                        string fileSize = FormatFileSize(item.FileSize);
                        additionalInfo = $"  ({fileSize}, {modifyTime})";
                    }
                    else
                    {
                        additionalInfo = $"  ({modifyTime})";
                    }
                }
                else if (!item.IsDirectory && showFileSize && item.FileSize > 0)
                {
                    string fileSize = FormatFileSize(item.FileSize);
                    additionalInfo = $"  ({fileSize})";
                }
                
                // 目录以 "/" 结尾，输出项完整绝对路径，无缩进
                string displayName = item.IsDirectory ? item.FullPath + "/" : item.FullPath;
                sb.AppendLine($"- {prefix}{displayName}{additionalInfo}");
            }
            
            // 递归处理子项
            if (item.IsDirectory && item.Children != null)
            {
                foreach (var child in item.Children)
                {
                    GenerateMarkdownPathList(sb, child, false, showFileSize, showLastModified);
                }
            }
        }
}