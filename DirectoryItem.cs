using System.Collections.Generic;
using System.IO;
using System;
using System.Text.RegularExpressions;
using System.Linq;

namespace WpfDirectoryViewer
{
    public class DirectoryItem
    {
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public bool IsDirectory { get; set; }
        public List<DirectoryItem> Children { get; set; }
        public long FileSize { get; set; } // 文件大小，单位为字节
        public DateTime LastModified { get; set; } // 最后修改时间
        
        public DirectoryItem()
        {
            Children = new List<DirectoryItem>();
        }
        
        // 自然排序比较器
        private static readonly NaturalSortComparer NaturalSort = new NaturalSortComparer();
        
        public static DirectoryItem CreateDirectoryItem(string path)
        {
            var item = new DirectoryItem
            {
                Name = Path.GetFileName(path) ?? "",
                FullPath = path,
                IsDirectory = Directory.Exists(path)
            };
            
            // 获取文件大小和修改时间
            try
            {
                if (!item.IsDirectory)
                {
                    FileInfo fileInfo = new FileInfo(path);
                    item.FileSize = fileInfo.Length;
                    item.LastModified = fileInfo.LastWriteTime;
                }
                else
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(path);
                    item.LastModified = dirInfo.LastWriteTime;
                }
            }
            catch (Exception)
            {
                // 处理无法获取文件信息的情况
                item.FileSize = 0;
                item.LastModified = DateTime.MinValue;
            }
            
            if (item.IsDirectory)
            {
                try
                {
                    // 添加子目录
                    foreach (var dir in Directory.GetDirectories(path))
                    {
                        item.Children.Add(CreateDirectoryItem(dir));
                    }
                    
                    // 添加文件
                    foreach (var file in Directory.GetFiles(path))
                    {
                        item.Children.Add(CreateDirectoryItem(file));
                    }
                    
                    // 对目录项进行自然排序：目录在前，文件在后，然后按名称自然排序
                    item.Children = item.Children.OrderBy(c => !c.IsDirectory).ThenBy(c => c.Name, NaturalSort).ToList();
                }
                catch (UnauthorizedAccessException)
                {
                    // 处理无访问权限的情况
                }
                catch (DirectoryNotFoundException)
                {
                    // 处理目录不存在的情况
                }
            }
            
            return item;
        }
        
        // 自然排序比较器类
        private class NaturalSortComparer : IComparer<string>
        {
            private readonly Regex _regex = new Regex(@"(\d+)|\((\d+)\)$", RegexOptions.Compiled);
            
            public int Compare(string x, string y)
            {
                // 特殊情况处理：对于相同前缀的文件名，不带括号的排在带括号的前面
                string xBaseName = GetBaseNameWithoutBracket(x);
                string yBaseName = GetBaseNameWithoutBracket(y);
                
                if (xBaseName.Equals(yBaseName, StringComparison.OrdinalIgnoreCase))
                {
                    // 检查是否有括号
                    bool xHasBracket = HasBracketNumber(x);
                    bool yHasBracket = HasBracketNumber(y);
                    
                    if (!xHasBracket && yHasBracket)
                    {
                        return -1; // x没有括号，排在前面
                    }
                    if (xHasBracket && !yHasBracket)
                    {
                        return 1; // y没有括号，排在前面
                    }
                }
                
                // 提取末尾的数字（包含普通数字和带括号的数字）
                var xMatch = _regex.Match(x);
                var yMatch = _regex.Match(y);
                
                // 如果都匹配到了数字
                if (xMatch.Success && yMatch.Success)
                {
                    // 提取数字部分
                    string xNumber = xMatch.Groups[1].Value != "" ? xMatch.Groups[1].Value : xMatch.Groups[2].Value;
                    string yNumber = yMatch.Groups[1].Value != "" ? yMatch.Groups[1].Value : yMatch.Groups[2].Value;
                    
                    // 比较数字前面的部分
                    string xPrefix = x.Substring(0, xMatch.Index);
                    string yPrefix = y.Substring(0, yMatch.Index);
                    
                    if (xPrefix.Equals(yPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        // 如果前缀相同，则比较数字值
                        if (int.TryParse(xNumber, out int xInt) && int.TryParse(yNumber, out int yInt))
                        {
                            return xInt.CompareTo(yInt);
                        }
                    }
                }
                
                // 否则使用默认字符串比较
                return string.Compare(x, y, StringComparison.OrdinalIgnoreCase);
            }
            
            // 获取不包含括号数字的基本文件名
            private string GetBaseNameWithoutBracket(string fileName)
            {
                var match = Regex.Match(fileName, @"^(.*?)\s*\(\d+\)(\.[^.]+)$", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return match.Groups[1].Value + match.Groups[2].Value;
                }
                return fileName;
            }
            
            // 检查文件名是否包含括号数字
            private bool HasBracketNumber(string fileName)
            {
                return Regex.IsMatch(fileName, @"\s*\(\d+\)\.[^.]+$", RegexOptions.IgnoreCase);
            }
        }
    }
}