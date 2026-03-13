using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Forms;
using System.Collections.Generic;

namespace eartq
{
    public partial class SettingsWindow : Window
    {
        private AppSettings _settings;

        public SettingsWindow()
        {
            InitializeComponent();
            _settings = SettingsManager.Load();
            LoadSettingsToUI();
            LoadDevicesAsync();
        }

        private void LoadSettingsToUI()
        {
            TxtOutputFolder.Text = _settings.OutputDirectory;
            if (string.IsNullOrEmpty(TxtOutputFolder.Text))
                TxtOutputFolder.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
        }

        private async void LoadDevicesAsync()
        {
            string ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe");
            if (!File.Exists(ffmpegPath))
            {
                ModernMessageBox.Show("ffmpeg.exe bulunamadı. Önce ana uygulamayı bir kez çalıştırıp indirilmesini bekleyin.");
                return;
            }

            var (videoDevices, audioDevices) = GetDirectShowDevices(ffmpegPath);

            CmbCameras.ItemsSource = videoDevices;
            CmbMicrophones.ItemsSource = audioDevices;

            if (videoDevices.Contains(_settings.SelectedCamera))
                CmbCameras.SelectedItem = _settings.SelectedCamera;
            else if (videoDevices.Count > 0)
                CmbCameras.SelectedIndex = 0;

            if (audioDevices.Contains(_settings.SelectedMicrophone))
                CmbMicrophones.SelectedItem = _settings.SelectedMicrophone;
            else if (audioDevices.Count > 0)
                CmbMicrophones.SelectedIndex = 0;
        }

        private (List<string> video, List<string> audio) GetDirectShowDevices(string ffmpegPath)
        {
            var videoList = new List<string>();
            var audioList = new List<string>();

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = "-list_devices true -f dshow -i dummy",
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = process.StandardError.ReadToEnd();
            process.WaitForExit();

            foreach (var line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (line.Contains("Alternative name")) continue;

                // Match [dshow @ ...] "Device Name" (video) or (audio)
                var match = Regex.Match(line, "\"([^\"]+)\"\\s*\\((video|audio)\\)");
                if (match.Success)
                {
                    string deviceName = match.Groups[1].Value;
                    string deviceType = match.Groups[2].Value;

                    if (deviceType == "video" && !videoList.Contains(deviceName))
                        videoList.Add(deviceName);
                    else if (deviceType == "audio" && !audioList.Contains(deviceName))
                        audioList.Add(deviceName);
                }
            }

            return (videoList, audioList);
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                {
                    TxtOutputFolder.Text = dialog.SelectedPath;
                }
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            _settings.SelectedCamera = CmbCameras.SelectedItem?.ToString() ?? "";
            _settings.SelectedMicrophone = CmbMicrophones.SelectedItem?.ToString() ?? "";
            _settings.OutputDirectory = TxtOutputFolder.Text;

            SettingsManager.Save(_settings);
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
