using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using WallMonitor.Resources;

namespace WallMonitor.Desktop.Services
{
    public partial class AudioService : IDisposable
    {
        private readonly Dictionary<string, AudioContainer> _audioFiles = new();
        private readonly LibVLC _vlc;
        private readonly MediaPlayer _mediaPlayer;

        private static AudioService? _instance;

        public delegate void MonitoringServiceMessageReceivedHandler(object sender, MuteChangedEventArgs e);
        public event MonitoringServiceMessageReceivedHandler OnMuteChanged;

        public static AudioService Instance => _instance ??= new AudioService();

        /// <summary>
        /// Get/set the muted state
        /// </summary>
        public bool IsMuted
        {
            get => _mediaPlayer.Mute;
            set
            {
                OnMuteChanged?.Invoke(this, new MuteChangedEventArgs(_mediaPlayer.Mute, value));
                _mediaPlayer.Mute = value;
            }
        }

        public AudioService()
        {
            Core.Initialize();
            _vlc = new LibVLC();

            _mediaPlayer = new MediaPlayer(_vlc);
            _audioFiles.Add("click", new AudioContainer(_vlc, "click.wav"));
            _audioFiles.Add("cancel", new AudioContainer(_vlc, "cancel.wav"));
            _audioFiles.Add("hover", new AudioContainer(_vlc, "hover.wav"));
            _audioFiles.Add("message", new AudioContainer(_vlc, "message.wav"));
            _audioFiles.Add("serviceDown1", new AudioContainer(_vlc, "server_down1.wav"));
            _audioFiles.Add("serviceDown2", new AudioContainer(_vlc, "server_down2.wav"));
            _audioFiles.Add("serviceDown3", new AudioContainer(_vlc, "server_down3.wav"));
            _audioFiles.Add("serviceUp1", new AudioContainer(_vlc, "server_up1.wav"));
        }

        public void EnsureCreated()
        {
        }

        public void PlayClick()
        {
            _mediaPlayer.Media = _audioFiles["click"].Media;
            _mediaPlayer.Time = 0;
            _mediaPlayer.Play();
        }

        public void PlayHover()
        {
            _mediaPlayer.Media = _audioFiles["hover"].Media;
            _mediaPlayer.Time = 0;
            _mediaPlayer.Play();
        }

        public void PlayCancel()
        {
            _mediaPlayer.Media = _audioFiles["cancel"].Media;
            _mediaPlayer.Time = 0;
            _mediaPlayer.Play();
        }

        public void PlayMessage()
        {
            _mediaPlayer.Media = _audioFiles["message"].Media;
            _mediaPlayer.Time = 0;
            _mediaPlayer.Play();
        }

        public void PlayServiceDown(int level = 1)
        {
            _mediaPlayer.Media = _audioFiles[$"serviceDown{level}"].Media;
            _mediaPlayer.Time = 0;
            _mediaPlayer.Play();
        }

        public void PlayServiceUp()
        {
            _mediaPlayer.Media = _audioFiles["serviceUp1"].Media;
            _mediaPlayer.Time = 0;
            _mediaPlayer.Play();
        }

        public void Dispose()
        {
            foreach (var kvp in _audioFiles)
                kvp.Value.Dispose();
            _audioFiles.Clear();
            _mediaPlayer.Dispose();
            _vlc.Dispose();
        }

        private class AudioContainer : IDisposable
        {
            private Stream Stream { get; }
            internal Media Media { get; }

            public AudioContainer(LibVLC vlc, string file)
            {
                Stream = ResourceLoader.LoadSound(file);
                Media = new Media(vlc, new StreamMediaInput(Stream), ":no-video");
            }

            public void Dispose()
            {
                Stream.Dispose();
                Media.Dispose();
            }
        }
    }
}
