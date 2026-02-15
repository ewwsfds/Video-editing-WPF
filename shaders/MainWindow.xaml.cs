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
        private InvertColorEffect invertEffect;
        private VideoDrawing videoDrawing;
        private DrawingBrush videoBrush;
        private DispatcherTimer timer;
        private string currentVideoPath;

        public MainWindow()
        {
            InitializeComponent();

            // Initialize MediaPlayer
            mediaPlayer = new MediaPlayer();
            mediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
            mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;

            // Create the shader effect instance
            invertEffect = new InvertColorEffect();

            // Set up timer to refresh the video frame
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(33); // ~30 fps
            timer.Tick += Timer_Tick;

            // Create VideoDrawing that points to our MediaPlayer
            videoDrawing = new VideoDrawing();
            videoDrawing.Rect = new Rect(0, 0, 100, 100); // Will be updated when video loads
            videoDrawing.Player = mediaPlayer; // This links the MediaPlayer to the VideoDrawing

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
                // Update the drawing rectangle to match video aspect ratio
                if (mediaPlayer.NaturalVideoWidth > 0 && mediaPlayer.NaturalVideoHeight > 0)
                {
                    videoDrawing.Rect = new Rect(0, 0,
                        mediaPlayer.NaturalVideoWidth,
                        mediaPlayer.NaturalVideoHeight);

                    StatusText.Text = $"Video loaded: {Path.GetFileName(currentVideoPath)} " +
                                     $"({mediaPlayer.NaturalVideoWidth}x{mediaPlayer.NaturalVideoHeight})";
                }

                // Start the timer to refresh frames
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
            // This is needed because MediaPlayer doesn't automatically invalidate the UI
            VideoRectangle.InvalidateVisual();
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
                // Stop current video if playing
                mediaPlayer.Stop();
                timer.Stop();

                currentVideoPath = openFileDialog.FileName;

                // Load the new video
                mediaPlayer.Open(new Uri(currentVideoPath));

                // Show video area, hide text
                NoVideoText.Visibility = Visibility.Collapsed;
                VideoRectangle.Visibility = Visibility.Visible;

                // Enable/disable buttons
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
            // Apply the shader effect to the Rectangle (UIElement)
            VideoRectangle.Effect = invertEffect;

            // Update button states
            ApplyShaderButton.IsEnabled = false;
            RemoveShaderButton.IsEnabled = true;

            StatusText.Text = "Shader effect applied";
        }

        private void RemoveShaderButton_Click(object sender, RoutedEventArgs e)
        {
            // Remove the shader effect from the Rectangle
            VideoRectangle.Effect = null;

            // Update button states
            ApplyShaderButton.IsEnabled = true;
            RemoveShaderButton.IsEnabled = false;

            StatusText.Text = "Shader effect removed";
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (mediaPlayer.Source != null)
            {
                mediaPlayer.Play();
                timer.Start();
                StatusText.Text = "Playing";
            }
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (mediaPlayer.Source != null)
            {
                mediaPlayer.Pause();
                StatusText.Text = "Paused";
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (mediaPlayer.Source != null)
            {
                mediaPlayer.Stop();
                timer.Stop();
                StatusText.Text = "Stopped";

                // Force redraw to clear the frame
                VideoRectangle.InvalidateVisual();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            // Clean up resources
            mediaPlayer?.Close();
            timer?.Stop();
            base.OnClosed(e);
        }





        public class InvertColorEffect : ShaderEffect
        {
            public static readonly DependencyProperty InputProperty =
                ShaderEffect.RegisterPixelShaderSamplerProperty("Input", typeof(InvertColorEffect), 0);

            public InvertColorEffect()
            {
                PixelShader = new PixelShader();

                // Make sure InvertColor.ps is set to "Resource" build action
                PixelShader.UriSource = new Uri("pack://application:,,,/InvertColor.ps");

                UpdateShaderValue(InputProperty);
            }

            public Brush Input
            {
                get { return (Brush)GetValue(InputProperty); }
                set { SetValue(InputProperty, value); }
            }
        }
    }
}
