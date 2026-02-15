using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace InvertColorShader
{
    public partial class MainWindow : Window
    {
        private MediaPlayer mediaPlayer;
        private RippleEffect rippleEffect; // Changed from InvertColorEffect
        private VideoDrawing videoDrawing;
        private DrawingBrush videoBrush;
        private DispatcherTimer timer;
        private DispatcherTimer timeTimer; // New timer for updating Time property
        private string currentVideoPath;
        private double currentTime = 0.0;

        public MainWindow()
        {
            InitializeComponent();

            // Initialize MediaPlayer
            mediaPlayer = new MediaPlayer();
            mediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
            mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;

            // Create the shader effect instance
            rippleEffect = new RippleEffect();

            // Set up timer to refresh the video frame
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(33); // ~30 fps
            timer.Tick += Timer_Tick;

            // Set up timer to update the Time property
            timeTimer = new DispatcherTimer();
            timeTimer.Interval = TimeSpan.FromMilliseconds(50); // 20 fps update for time
            timeTimer.Tick += TimeTimer_Tick;

            // Create VideoDrawing that points to our MediaPlayer
            videoDrawing = new VideoDrawing();
            videoDrawing.Rect = new Rect(0, 0, 100, 100);
            videoDrawing.Player = mediaPlayer;

            // Create DrawingBrush from the VideoDrawing
            videoBrush = new DrawingBrush(videoDrawing);
            videoBrush.Stretch = Stretch.Uniform;
            videoBrush.AlignmentX = AlignmentX.Center;
            videoBrush.AlignmentY = AlignmentY.Center;

            // Assign the brush to our rectangle
            VideoRectangle.Fill = videoBrush;
        }

        private void MediaPlayer_MediaOpened(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (mediaPlayer.NaturalVideoWidth > 0 && mediaPlayer.NaturalVideoHeight > 0)
                {
                    videoDrawing.Rect = new Rect(0, 0,
                        mediaPlayer.NaturalVideoWidth,
                        mediaPlayer.NaturalVideoHeight);

                    StatusText.Text = $"Video loaded: {Path.GetFileName(currentVideoPath)} " +
                                     $"({mediaPlayer.NaturalVideoWidth}x{mediaPlayer.NaturalVideoHeight})";
                }

                timer.Start();
            });
        }

        private void MediaPlayer_MediaEnded(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                mediaPlayer.Position = TimeSpan.Zero;
                mediaPlayer.Play();
            });
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // Force UI to update with new video frame
            VideoRectangle.InvalidateVisual();
        }

        private void TimeTimer_Tick(object sender, EventArgs e)
        {
            // Update the Time property to create animation
            if (rippleEffect != null && VideoRectangle.Effect != null)
            {
                currentTime += 0.05; // Increment time
                rippleEffect.Time = currentTime;
            }
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Video Files|*.mp4;*.avi;*.wmv;*.mov;*.mkv;*.mpg;*.mpeg|All Files|*.*",
                Title = "Select a video file"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                mediaPlayer.Stop();
                timer.Stop();
                timeTimer.Stop();
                currentTime = 0.0;

                currentVideoPath = openFileDialog.FileName;
                mediaPlayer.Open(new Uri(currentVideoPath));

                NoVideoText.Visibility = Visibility.Collapsed;
                VideoRectangle.Visibility = Visibility.Visible;

                ApplyShaderButton.IsEnabled = true;
                RemoveShaderButton.IsEnabled = false;
                PlayButton.IsEnabled = true;
                PauseButton.IsEnabled = true;
                StopButton.IsEnabled = true;

                StatusText.Text = $"Loading: {Path.GetFileName(currentVideoPath)}";
            }
        }

        private void ApplyShaderButton_Click(object sender, RoutedEventArgs e)
        {
            // Apply the shader effect to the Rectangle
            VideoRectangle.Effect = rippleEffect;

            // Start the time animation timer
            timeTimer.Start();

            // Update button states
            ApplyShaderButton.IsEnabled = false;
            RemoveShaderButton.IsEnabled = true;

            StatusText.Text = "Ripple effect applied";
        }

        private void RemoveShaderButton_Click(object sender, RoutedEventArgs e)
        {
            // Remove the shader effect
            VideoRectangle.Effect = null;

            // Stop the time animation timer
            timeTimer.Stop();

            // Update button states
            ApplyShaderButton.IsEnabled = true;
            RemoveShaderButton.IsEnabled = false;

            StatusText.Text = "Ripple effect removed";
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (mediaPlayer.Source != null)
            {
                mediaPlayer.Play();
                timer.Start();
                if (VideoRectangle.Effect != null)
                    timeTimer.Start();
                StatusText.Text = "Playing";
            }
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (mediaPlayer.Source != null)
            {
                mediaPlayer.Pause();
                timer.Stop();
                timeTimer.Stop();
                StatusText.Text = "Paused";
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (mediaPlayer.Source != null)
            {
                mediaPlayer.Stop();
                timer.Stop();
                timeTimer.Stop();
                currentTime = 0.0;
                if (rippleEffect != null)
                    rippleEffect.Time = 0.0;
                StatusText.Text = "Stopped";
                VideoRectangle.InvalidateVisual();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            mediaPlayer?.Close();
            timer?.Stop();
            timeTimer?.Stop();
            base.OnClosed(e);
        }






        public class RippleEffect : ShaderEffect
        {
            public static readonly DependencyProperty InputProperty =
                ShaderEffect.RegisterPixelShaderSamplerProperty("Input", typeof(RippleEffect), 0);

            public static readonly DependencyProperty TimeProperty =
                DependencyProperty.Register("Time", typeof(double), typeof(RippleEffect),
                        new UIPropertyMetadata(0.0, PixelShaderConstantCallback(0))); // Register C0

            public RippleEffect()
            {
                PixelShader = new PixelShader();
                PixelShader.UriSource = new Uri("pack://application:,,,/InvertColor.ps");

                UpdateShaderValue(InputProperty);
                UpdateShaderValue(TimeProperty);
            }

            public Brush Input
            {
                get { return (Brush)GetValue(InputProperty); }
                set { SetValue(InputProperty, value); }
            }

            public double Time
            {
                get { return (double)GetValue(TimeProperty); }
                set { SetValue(TimeProperty, value); }
            }
        }
    }
}
