using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace SmoothScroll;

public sealed class SmoothScrollApp : IDisposable
{
    private const string StartupRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "SmoothScroll";

    private readonly NotifyIcon _trayIcon;
    private readonly ContextMenuStrip _contextMenu;
    private readonly ToolStripMenuItem _toggleMenuItem;
    private readonly ToolStripMenuItem _startupMenuItem;
    private readonly MouseHook _mouseHook;
    private readonly ScrollInterpolator _interpolator;
    private bool _disposed;
    private bool _isEnabled = true;

    public SmoothScrollApp()
    {
        _interpolator = new ScrollInterpolator();
        _mouseHook = new MouseHook();
        _mouseHook.ScrollEvent += OnScrollEvent;

        _toggleMenuItem = new ToolStripMenuItem("Disable", null, OnToggleClick);
        _startupMenuItem = new ToolStripMenuItem("Start with Windows", null, OnStartupClick)
        {
            Checked = IsStartupEnabled()
        };

        _contextMenu = new ContextMenuStrip();
        _contextMenu.Items.Add(_toggleMenuItem);
        _contextMenu.Items.Add("Settings...", null, OnSettingsClick);
        _contextMenu.Items.Add(new ToolStripSeparator());
        _contextMenu.Items.Add(_startupMenuItem);
        _contextMenu.Items.Add(new ToolStripSeparator());
        _contextMenu.Items.Add("Exit", null, OnExitClick);

        _trayIcon = new NotifyIcon
        {
            Icon = CreateIcon(true),
            Text = "SmoothScroll - Enabled",
            ContextMenuStrip = _contextMenu,
            Visible = true
        };

        _trayIcon.DoubleClick += OnToggleClick;

        try
        {
            _mouseHook.Install();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to install mouse hook: {ex.Message}",
                "SmoothScroll Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            Application.Exit();
        }
    }

    private void OnScrollEvent(int delta, int x, int y)
    {
        if (_isEnabled)
        {
            _interpolator.AddScroll(delta, x, y);
        }
    }

    private void OnToggleClick(object? sender, EventArgs e)
    {
        _isEnabled = !_isEnabled;
        _mouseHook.IsEnabled = _isEnabled;

        if (!_isEnabled)
        {
            _interpolator.Stop();
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        _toggleMenuItem.Text = _isEnabled ? "Disable" : "Enable";
        _trayIcon.Text = _isEnabled ? "SmoothScroll - Enabled" : "SmoothScroll - Disabled";
        _trayIcon.Icon?.Dispose();
        _trayIcon.Icon = CreateIcon(_isEnabled);
    }

    private static Icon CreateIcon(bool enabled)
    {
        const int size = 16;
        using var bitmap = new Bitmap(size, size);
        using var g = Graphics.FromImage(bitmap);

        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        // Draw a scroll wheel icon
        var color = enabled ? Color.FromArgb(0, 150, 0) : Color.FromArgb(150, 150, 150);
        using var brush = new SolidBrush(color);
        using var pen = new Pen(color, 1.5f);

        // Outer circle (mouse body)
        g.DrawEllipse(pen, 2, 1, 11, 14);

        // Scroll wheel
        if (enabled)
        {
            g.FillRectangle(brush, 6, 3, 3, 5);
        }
        else
        {
            using var grayBrush = new SolidBrush(Color.Gray);
            g.FillRectangle(grayBrush, 6, 3, 3, 5);
        }

        // Scroll indicators (arrows)
        if (enabled)
        {
            // Up arrow
            g.DrawLine(pen, 7.5f, 2, 7.5f, 4);
            g.DrawLine(pen, 6, 3, 7.5f, 1.5f);
            g.DrawLine(pen, 9, 3, 7.5f, 1.5f);

            // Down arrow
            g.DrawLine(pen, 7.5f, 6, 7.5f, 8);
            g.DrawLine(pen, 6, 7, 7.5f, 8.5f);
            g.DrawLine(pen, 9, 7, 7.5f, 8.5f);
        }

        return Icon.FromHandle(bitmap.GetHicon());
    }

    private void OnSettingsClick(object? sender, EventArgs e)
    {
        using var form = new SettingsForm(_interpolator);
        form.ShowDialog();
    }

    private void OnStartupClick(object? sender, EventArgs e)
    {
        bool currentlyEnabled = IsStartupEnabled();

        if (currentlyEnabled)
        {
            DisableStartup();
        }
        else
        {
            EnableStartup();
        }

        _startupMenuItem.Checked = IsStartupEnabled();
    }

    private static bool IsStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, false);
            return key?.GetValue(AppName) != null;
        }
        catch
        {
            return false;
        }
    }

    private static void EnableStartup()
    {
        try
        {
            string exePath = Application.ExecutablePath;
            using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, true);
            key?.SetValue(AppName, $"\"{exePath}\"");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to enable startup: {ex.Message}",
                "SmoothScroll",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private static void DisableStartup()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, true);
            key?.DeleteValue(AppName, false);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to disable startup: {ex.Message}",
                "SmoothScroll",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void OnExitClick(object? sender, EventArgs e)
    {
        Dispose();
        Application.Exit();
    }

    public void Dispose()
    {
        if (_disposed) return;

        _trayIcon.Visible = false;
        _trayIcon.Icon?.Dispose();
        _trayIcon.Dispose();
        _contextMenu.Dispose();
        _mouseHook.Dispose();
        _interpolator.Dispose();

        _disposed = true;
    }
}
