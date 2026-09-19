using System.Configuration;
using System.Data;
using System.Windows;
using Forms = System.Windows.Forms;
using System.Drawing;
using System;
using System.Diagnostics;

namespace WpfDirectoryViewer;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private Forms.NotifyIcon? _trayIcon;
    private bool _mainHiddenForReverse = false;
    private ReverseRebuildWindow? _reverseWindow;

    // 标记程序是否正在退出：退出时允许主窗口真正关闭
    public bool IsExiting { get; private set; }

    public void ExitApplication()
    {
        IsExiting = true;
        Shutdown();
    }

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        // 初始化托盘图标与菜单
        _trayIcon = new Forms.NotifyIcon();
        _trayIcon.Text = "目录结构查看器";

        // 优先从WPF资源中加载图标，保持与应用图标一致
        try
        {
            // 使用 pack URI 加载嵌入资源
            var packUri = new Uri("pack://application:,,,/Images/app.ico", UriKind.Absolute);
            var sri = GetResourceStream(packUri);
            if (sri != null && sri.Stream != null)
            {
                _trayIcon.Icon = new Icon(sri.Stream);
            }
        }
        catch { /* 忽略资源加载失败 */ }

        // 回退：从物理路径加载图标（发布目录中带有 Images/app.ico）
        if (_trayIcon.Icon == null)
        {
            try
            {
                var iconPath = System.IO.Path.Combine(System.AppContext.BaseDirectory, "Images", "app.ico");
                if (System.IO.File.Exists(iconPath))
                {
                    _trayIcon.Icon = new Icon(iconPath);
                }
            }
            catch { /* 忽略 */ }
        }

        // 回退：从可执行文件提取关联图标
        if (_trayIcon.Icon == null)
        {
            try
            {
                var exe = Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exe))
                {
                    var assoc = Icon.ExtractAssociatedIcon(exe);
                    if (assoc != null) _trayIcon.Icon = assoc;
                }
            }
            catch { /* 忽略 */ }
        }

        // 最后回退：使用系统应用图标
        if (_trayIcon.Icon == null)
        {
            _trayIcon.Icon = SystemIcons.Application;
        }

        var contextMenu = new Forms.ContextMenuStrip();
        var openMainItem = new Forms.ToolStripMenuItem("打开主窗口");
        openMainItem.Click += (s, _) =>
        {
            ShowMainWindow();
        };

        var openReverseItem = new Forms.ToolStripMenuItem("反向重建结构...");
        openReverseItem.Click += (s, _) =>
        {
            ShowReverseWindow();
        };

        var exitItem = new Forms.ToolStripMenuItem("退出");
        exitItem.Click += (s, _) =>
        {
            ExitApplication();
        };

        contextMenu.Items.Add(openMainItem);
        contextMenu.Items.Add(openReverseItem);
        contextMenu.Items.Add(new Forms.ToolStripSeparator());
        contextMenu.Items.Add(exitItem);

        _trayIcon.ContextMenuStrip = contextMenu;
        // 确保在设置好Icon之后再显示
        _trayIcon.Visible = true;

        // 给出一次气泡提示，便于用户确认托盘图标已生效
        try
        {
            _trayIcon.BalloonTipTitle = "目录结构查看器";
            _trayIcon.BalloonTipText = "托盘已就绪，右键可打开‘反向重建结构…’";
            _trayIcon.ShowBalloonTip(3000);
        }
        catch { /* 某些环境可能禁用气泡提示 */ }

        // 双击托盘图标打开主窗口
        _trayIcon.DoubleClick += (s, _) => ShowMainWindow();
        // 点击气泡提示时，直接打开“反向重建结构”窗口
        _trayIcon.BalloonTipClicked += (s, _) =>
        {
            ShowReverseWindow();
        };

        // 系统关机/注销时允许程序正常退出，不再隐藏到托盘
        this.SessionEnding += App_SessionEnding;
    }

    private void App_SessionEnding(object sender, SessionEndingCancelEventArgs e)
    {
        IsExiting = true;
    }

    private void ShowMainWindow()
    {
        // MainWindow 可能已被关闭或从未创建，此时重新创建
        if (this.MainWindow == null || !this.MainWindow.IsLoaded)
        {
            this.MainWindow = new MainWindow();
            this.MainWindow.Show();
        }
        else
        {
            if (this.MainWindow.WindowState == WindowState.Minimized)
                this.MainWindow.WindowState = WindowState.Normal;
            this.MainWindow.Show();
            this.MainWindow.Activate();
        }
        // 用户主动显示主窗口，认为不是“为反向窗口而隐藏”状态
        _mainHiddenForReverse = false;
    }

    private void ShowReverseWindow()
    {
        // 若反向窗口已存在且仍可用，则恢复并激活已有窗口
        if (_reverseWindow != null)
        {
            if (!_reverseWindow.IsLoaded)
            {
                _reverseWindow = null;
            }
            else
            {
                if (_reverseWindow.WindowState == WindowState.Minimized)
                    _reverseWindow.WindowState = WindowState.Normal;
                _reverseWindow.Show();
                _reverseWindow.Activate();
                return;
            }
        }

        // 打开反向窗口时，隐藏主窗口
        if (this.MainWindow != null && this.MainWindow.IsVisible)
        {
            this.MainWindow.Hide();
            _mainHiddenForReverse = true;
        }
        var win = new ReverseRebuildWindow();
        _reverseWindow = win;
        // 当反向窗口关闭时，如果主窗口是为反向窗口而隐藏的，则整个程序退出
        win.Closed += (s, e) =>
        {
            _reverseWindow = null;
            if (_mainHiddenForReverse)
            {
                ExitApplication();
            }
        };
        win.Show();
        win.Activate();
    }

    private void Application_Exit(object sender, ExitEventArgs e)
    {
        if (_trayIcon != null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _trayIcon = null;
        }
    }

    // 该方法保留以兼容早期逻辑（当前未使用）
    private void MinimizeMainToTray()
    {
        if (this.MainWindow != null)
        {
            this.MainWindow.WindowState = WindowState.Minimized;
        }
    }
}

