using System.IO;
using System.Text.Json;

namespace WpfDirectoryViewer;

public class Configuration
{
    public string CurrentDirectoryPath { get; set; } = string.Empty;
    public string OutputDirectoryPath { get; set; } = string.Empty;
    public bool ExportTxt { get; set; } = true;  // 默认输出txt
    public bool ExportMd { get; set; } = true;   // 默认输出md
    public bool ExportHtml { get; set; } = true; // 默认输出html
    public bool ShowFileSize { get; set; } = true;    // 默认显示文件大小
    public bool ShowLastModified { get; set; } = true; // 默认显示修改时间
    public bool ExportTxtAsPathList { get; set; } = false; // false=树形结构，true=完整路径
    
    // 获取配置文件路径（与软件同目录，同名的json文件）
    public static string GetConfigurationFilePath()
    {
        string appName;
        string appDirectory;
        
        try
        {
            // 获取当前正在运行的可执行文件的完整路径
            string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            appName = Path.GetFileNameWithoutExtension(exePath);
            appDirectory = Path.GetDirectoryName(exePath);
        }
        catch
        {
            // 如果上面的方法失败，回退到使用应用程序目录和入口点名称
            appName = System.AppDomain.CurrentDomain.FriendlyName;
            appName = Path.GetFileNameWithoutExtension(appName);
            appDirectory = System.AppContext.BaseDirectory;
        }
        
        // 如果appName仍然为空，则使用默认名称
        if (string.IsNullOrEmpty(appName))
        {
            appName = "WpfDirectoryViewer";
        }
        
        // 确保appDirectory不为空
        if (string.IsNullOrEmpty(appDirectory))
        {
            appDirectory = System.AppContext.BaseDirectory;
        }
        
        return Path.Combine(appDirectory, $"{appName}.json");
    }
    
    // 保存配置到文件
    public void Save()
    {
        string filePath = GetConfigurationFilePath();
        string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);
    }
    
    // 从文件加载配置，如果文件不存在则返回默认配置
    public static Configuration Load()
    {
        string filePath = GetConfigurationFilePath();
        
        if (File.Exists(filePath))
        {
            try
            {
                string json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<Configuration>(json) ?? GetDefaultConfiguration();
            }
            catch
            {
                // 如果解析失败，返回默认配置
                return GetDefaultConfiguration();
            }
        }
        
        // 文件不存在，返回默认配置
        return GetDefaultConfiguration();
    }
    
    // 获取默认配置
    private static Configuration GetDefaultConfiguration()
    {
        string currentDirectory = Directory.GetCurrentDirectory();
        return new Configuration
        {
            CurrentDirectoryPath = currentDirectory,
            OutputDirectoryPath = currentDirectory,
            ExportTxt = true,
            ExportMd = true,
            ExportHtml = true,
            ShowFileSize = true,
            ShowLastModified = true,
            ExportTxtAsPathList = false
        };
    }
}