using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;

namespace eartq;

public partial class MainWindow : Window
{
    private Process _ffmpegProcess;
    private bool _isRecording = false;
    private Storyboard _pulseStoryboard;
    private AppSettings _settings;
    private System.Windows.Forms.NotifyIcon _notifyIcon;

    public MainWindow()
    {
        InitializeComponent();
        this.Loaded += MainWindow_Loaded;
        SetupPulseAnimation();
        SetupTrayIcon();
    }

    private void SetupTrayIcon()
    {
        _notifyIcon = new System.Windows.Forms.NotifyIcon();
        
        try
        {
            string exePath = Process.GetCurrentProcess().MainModule.FileName;
            _notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
        }
        catch 
        {
            // Fallback if extraction fails
        }

        _notifyIcon.Text = "Acil Durum Widget";
        _notifyIcon.Visible = true;
        
        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("Göster/Gizle", null, (s, e) => {
            if (this.Visibility == Visibility.Visible) this.Visibility = Visibility.Hidden;
            else this.Visibility = Visibility.Visible;
        });
        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        menu.Items.Add("Ayarlar", null, (s, e) => MenuSettings_Click(null, null));
        menu.Items.Add("Çıkış", null, (s, e) => MenuExit_Click(null, null));
        
        _notifyIcon.ContextMenuStrip = menu;
        _notifyIcon.DoubleClick += (s, e) => {
            if (this.Visibility == Visibility.Visible) this.Visibility = Visibility.Hidden;
            else this.Visibility = Visibility.Visible;
        };
    }

    private void SetupPulseAnimation()
    {
        _pulseStoryboard = new Storyboard();
        var animation = new DoubleAnimation
        {
            From = 1.0,
            To = 0.4,
            Duration = new Duration(TimeSpan.FromSeconds(0.5)),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever
        };
        Storyboard.SetTarget(animation, RecordIndicator);
        Storyboard.SetTargetProperty(animation, new PropertyPath(UIElement.OpacityProperty));
        _pulseStoryboard.Children.Add(animation);
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _settings = SettingsManager.Load();
        
        string ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe");
        if (!File.Exists(ffmpegPath))
        {
            try
            {
                RecordIndicator.Fill = System.Windows.Media.Brushes.Orange;
                _pulseStoryboard.Begin();
                
                var progress = new Progress<ProgressInfo>(p =>
                {
                    // Optionally update tooltip with progress
                    RecordIndicator.ToolTip = $"FFmpeg indiriliyor: %{p.DownloadedBytes * 100 / p.TotalBytes}";
                });
                
                await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, AppDomain.CurrentDomain.BaseDirectory, progress);
                
                RecordIndicator.ToolTip = "Kayıt için hazır.";
                _pulseStoryboard.Stop();
                RecordIndicator.Fill = System.Windows.Media.Brushes.Red;
                ModernMessageBox.Show("Gerekli altyapı (FFmpeg) başarıyla indirildi. Artık ayarlardan cihaz seçip kayıt yapabilirsiniz.", "Hazır");
            }
            catch (Exception ex)
            { 
                _pulseStoryboard.Stop();
                RecordIndicator.Fill = System.Windows.Media.Brushes.Red;
                ModernMessageBox.Show($"İndirme hatası: {ex.Message}", "Hata");
            }
        }
    }

    private void WidgetBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            try { this.DragMove(); } catch { }
        }
    }

    private void BtnRecord_Click(object sender, RoutedEventArgs e)
    {
        if (_isRecording)
        {
            StopRecording();
        }
        else
        {
            StartRecording();
        }
    }

    private void StartRecording()
    {
        string ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe");
        if (!File.Exists(ffmpegPath))
        {
            ModernMessageBox.Show("ffmpeg altyapısı şu anda arka planda indiriliyor veya eksik. Lütfen indirme işleminin tamamlanmasını bekleyin. Turuncu yanıp sönme bitince tekrar deneyin.", "Bekleyin");
            return;
        }

        _settings = SettingsManager.Load(); // Reload in case settings changed

        string videoDevice = _settings.SelectedCamera;
        string audioDevice = _settings.SelectedMicrophone;
        
        // If nothing is selected, attempt to fallback to first available by reading them here (simplistic fallback)
        if (string.IsNullOrEmpty(videoDevice))
        {
            ModernMessageBox.Show("Lütfen ayarlardan bir Kamera ve Mikrofon seçin.", "Cihaz Seçilmedi");
            new SettingsWindow().ShowDialog();
            return;
        }

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string videosFolder = string.IsNullOrEmpty(_settings.OutputDirectory) ? Environment.GetFolderPath(Environment.SpecialFolder.MyVideos) : _settings.OutputDirectory;
        string outputPath = Path.Combine(videosFolder, $"AcilDurumKayit_{timestamp}.mp4");

        string audioArgs = !string.IsNullOrEmpty(audioDevice) ? $":audio=\"{audioDevice}\"" : "";
        string arguments = $"-f dshow -i video=\"{videoDevice}\"{audioArgs} -c:v libx264 -preset ultrafast -pix_fmt yuv420p -c:a aac -y \"{outputPath}\"";

        try
        {
            _ffmpegProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = arguments,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            _ffmpegProcess.Start();
            _isRecording = true;
            RecordIndicator.Fill = System.Windows.Media.Brushes.Transparent;
            WidgetBorder.BorderBrush = System.Windows.Media.Brushes.Green;
            _pulseStoryboard.Begin();
        }
        catch (Exception ex)
        {
            ModernMessageBox.Show($"Kayıt başlatılamadı: {ex.Message}", "Hata");
        }
    }

    private void StopRecording()
    {
        if (_ffmpegProcess != null && !_ffmpegProcess.HasExited)
        {
            try
            {
                _ffmpegProcess.StandardInput.WriteLine("q");
                _ffmpegProcess.WaitForExit(3000); 
                if (!_ffmpegProcess.HasExited)
                {
                    _ffmpegProcess.Kill();
                }
            }
            catch { }
        }

        _isRecording = false;
        _pulseStoryboard.Stop();
        RecordIndicator.Fill = System.Windows.Media.Brushes.Red;
        RecordIndicator.Opacity = 1.0;
        WidgetBorder.BorderBrush = System.Windows.Media.Brushes.Red;
    }

    private void MenuSettings_Click(object sender, RoutedEventArgs e)
    {
        var settingsWin = new SettingsWindow();
        settingsWin.ShowDialog();
    }

    private void MenuExit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        StopRecording();
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }
        base.OnClosed(e);
    }
}