using System;
using System.Drawing;
using System.Windows.Forms;

namespace SmoothScroll;

public sealed class SettingsForm : Form
{
    private readonly ScrollInterpolator _interpolator;

    private TrackBar _speedTrackBar = null!;
    private TrackBar _frictionTrackBar = null!;
    private Label _speedValueLabel = null!;
    private Label _frictionValueLabel = null!;

    public SettingsForm(ScrollInterpolator interpolator)
    {
        _interpolator = interpolator;
        InitializeComponents();
        LoadCurrentSettings();
    }

    private void InitializeComponents()
    {
        Text = "SmoothScroll Settings";
        Size = new Size(400, 250);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        var mainPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(15),
            RowCount = 4,
            ColumnCount = 2
        };
        mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
        mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        // Speed label
        var speedLabel = new Label
        {
            Text = "Speed:",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        mainPanel.Controls.Add(speedLabel, 0, 0);

        // Speed trackbar panel
        var speedPanel = new Panel { Dock = DockStyle.Fill };
        _speedTrackBar = new TrackBar
        {
            Minimum = 5,
            Maximum = 100,
            Value = 25,
            TickFrequency = 10,
            Dock = DockStyle.Top,
            Height = 30
        };
        _speedTrackBar.ValueChanged += OnSpeedChanged;
        _speedValueLabel = new Label
        {
            Text = "2.5",
            Dock = DockStyle.Bottom,
            TextAlign = ContentAlignment.TopCenter
        };
        speedPanel.Controls.Add(_speedTrackBar);
        speedPanel.Controls.Add(_speedValueLabel);
        mainPanel.Controls.Add(speedPanel, 1, 0);

        // Friction label
        var frictionLabel = new Label
        {
            Text = "Smoothness:",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        mainPanel.Controls.Add(frictionLabel, 0, 1);

        // Friction trackbar panel
        var frictionPanel = new Panel { Dock = DockStyle.Fill };
        _frictionTrackBar = new TrackBar
        {
            Minimum = 50,
            Maximum = 98,
            Value = 85,
            TickFrequency = 5,
            Dock = DockStyle.Top,
            Height = 30
        };
        _frictionTrackBar.ValueChanged += OnFrictionChanged;
        _frictionValueLabel = new Label
        {
            Text = "85%",
            Dock = DockStyle.Bottom,
            TextAlign = ContentAlignment.TopCenter
        };
        frictionPanel.Controls.Add(_frictionTrackBar);
        frictionPanel.Controls.Add(_frictionValueLabel);
        mainPanel.Controls.Add(frictionPanel, 1, 1);

        // Info label
        var infoLabel = new Label
        {
            Text = "Higher smoothness = longer glide\nHigher speed = faster scrolling",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.TopLeft,
            ForeColor = Color.Gray
        };
        mainPanel.SetColumnSpan(infoLabel, 2);
        mainPanel.Controls.Add(infoLabel, 0, 2);

        // Buttons panel
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 10, 0, 0)
        };

        var closeButton = new Button
        {
            Text = "Close",
            Width = 80
        };
        closeButton.Click += (s, e) => Close();

        var resetButton = new Button
        {
            Text = "Reset",
            Width = 80
        };
        resetButton.Click += OnResetClick;

        buttonPanel.Controls.Add(closeButton);
        buttonPanel.Controls.Add(resetButton);
        mainPanel.SetColumnSpan(buttonPanel, 2);
        mainPanel.Controls.Add(buttonPanel, 0, 3);

        Controls.Add(mainPanel);
    }

    private void LoadCurrentSettings()
    {
        _speedTrackBar.Value = Math.Clamp((int)(_interpolator.VelocityMultiplier * 10), 5, 100);
        _frictionTrackBar.Value = Math.Clamp((int)(_interpolator.Friction * 100), 50, 98);
        UpdateLabels();
    }

    private void OnSpeedChanged(object? sender, EventArgs e)
    {
        _interpolator.VelocityMultiplier = _speedTrackBar.Value / 10.0;
        UpdateLabels();
    }

    private void OnFrictionChanged(object? sender, EventArgs e)
    {
        _interpolator.Friction = _frictionTrackBar.Value / 100.0;
        UpdateLabels();
    }

    private void UpdateLabels()
    {
        _speedValueLabel.Text = $"{_speedTrackBar.Value / 10.0:F1}";
        _frictionValueLabel.Text = $"{_frictionTrackBar.Value}%";
    }

    private void OnResetClick(object? sender, EventArgs e)
    {
        _speedTrackBar.Value = 25;  // 2.5
        _frictionTrackBar.Value = 85;
    }
}
