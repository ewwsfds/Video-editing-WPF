using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;


using IOPath = System.IO.Path;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        //
        private double rectangleWidth = 750;

        // use double (WPF uses double everywhere)
        private double zoomLevel = 1.0;

        private double MinZoom = 0.125;
        private double MaxZoom = 8;




        // Timeline canvas tile images
        const double TileWidth = 80;

        int pixel_per_second = 26;

        int imageWidth;
        //Timeline Canvas
        double rowHeight = 40;
        bool fadestarted = false;
        private const double SNAP_DISTANCE = 50;
        private List<Canvas> canvases = new List<Canvas>();


        //Preview Canvas

        private int offsetX = 10;
        private DateTime lastSnapCheck = DateTime.MinValue; // class-level


        private bool isPlaying = false;

        // Create the TranslateTransform once
        private TranslateTransform Vertical_Timeline_Transform = new TranslateTransform();

        public class PreviewCanvasInfo
        {
            public Canvas previewcanvas { get; set; }
            public Canvas Canvas { get; set; }
            public Rectangle rectRight { get; set; }

            public double start { get; set; }
            public double Y { get; set; }
            public double end { get; set; }
            public double height { get; set; }
            public double width { get; set; }
            // Instead of separate Image & MediaElement, use UIElement
            public FrameworkElement Content { get; set; }

            // Store aspect ratio H/W
            public double AspectRatio { get; set; } = 1;


            public double original_canvasWidth { get; set; }


            public double original_translateX { get; set; }


            public double leftShrinkAmount { get; set; }
            public double rightShrinkAmount { get; set; }



        }

        private List<PreviewCanvasInfo> Preview_canvases = new List<PreviewCanvasInfo>();


        public MainWindow()
        {
            InitializeComponent();
            rowShower.Fill = Brushes.Transparent;

            Canvas.SetZIndex(Vertical_TimeLine, 1000); // ensures it's always on top

            Vertical_line.Fill = Brushes.Transparent;
            Horizontal_line.Fill = Brushes.Transparent;

            // Apply it to the rectangle
            Vertical_TimeLine.RenderTransform = Vertical_Timeline_Transform;

            imageWidth = pixel_per_second * 5;



            // Safe: DrawTimeline only after Window is ready
            Loaded += (_, __) =>
            {
                DrawTimeline();
                ZoomSlider.ValueChanged += ZoomSlider_ValueChanged; // attach AFTER Loaded
            };


        }



        private void import_media(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Media Files|*.jpg;*.jpeg;*.png;*.mp4;*.mov;*.avi;*.wmv;*.mp3"
            };

            if (dialog.ShowDialog() != true)
                return;

            foreach (var file in dialog.FileNames)
            {
                // Hämta filtypen

                string fileType = IOPath.GetExtension(file).ToLower();
                AddMediaToCanvas(file, fileType);



            }






        }







        // Click event handler
        private void Timeline_Click(object sender, MouseButtonEventArgs e)
        {
            // Mouse position relative to the timeline Canvas (or parent)
            double mouseX = e.GetPosition(timeline).X;

            // Move the rectangle, centering it on the click
            Vertical_Timeline_Transform.X = mouseX - Vertical_TimeLine.Width / 2;

            // Update label
            CurrentTime.Content = ((int)mouseX).ToString();

            PreviewChecker();
        }

        DispatcherTimer playTimer;

        private DateTime lastTick;

        private void StartTimelinePlayback()
        {
            double speed = pixel_per_second; // pixels per second

            playTimer = new DispatcherTimer();
            playTimer.Interval = TimeSpan.FromMilliseconds(16); // ~60 FPS
            lastTick = DateTime.Now;

            playTimer.Tick += (s, e) =>
            {
                if (!isPlaying) return;

                // Measure actual elapsed time
                DateTime now = DateTime.Now;
                double deltaSeconds = (now - lastTick).TotalSeconds;
                lastTick = now;

                Vertical_Timeline_Transform.X += speed * deltaSeconds;
                CurrentTime.Content = ((int)Vertical_Timeline_Transform.X).ToString();

                PreviewChecker();
            };

            playTimer.Start();
        }



        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                isPlaying = !isPlaying;
                StartTimelinePlayback();
            }
        }

        private void PlayBTN(object sender, RoutedEventArgs e)
        {
            isPlaying = !isPlaying;
            StartTimelinePlayback();

        }




        // make a function if isplying make  Vertical_TimeLine move 5px/second using transform

        private void AddMediaToCanvas(string path, string fileType)
        {
            FrameworkElement Content = new FrameworkElement();
            Canvas Preview_canvas = new Canvas();
            BitmapImage bitmap = new BitmapImage();
            double targetWidth = 200; // ← du bestämmer bredden
            double targetHeight = 20;


            double aspectRatio = 0;

            if (fileType == ".jpg" || fileType == ".jpeg" || fileType == ".png")
            {


            }



            Preview_canvas.Width = 200;
            Preview_canvas.Height = 190;
            Preview_canvas.Background = Brushes.Black;
            // Skapa Canvas som följer bildens ratio



            // Set initial position of canvas
            Canvas.SetLeft(Preview_canvas, offsetX);
            Canvas.SetTop(Preview_canvas, 10);
            offsetX += 70;

            // Create a rectangle inside the canvas
            Rectangle rect = new Rectangle
            {
                Width = 10,
                Height = 10,
                Fill = Brushes.Red
            };

            // Set rectangle slightly outside top-left of its canvas
            Canvas.SetLeft(rect, -9);
            Canvas.SetTop(rect, -9);

            Preview_canvas.Children.Add(rect);
            previewCanvas.Children.Add(Preview_canvas);

            // Prepare dragging variables
            bool Preview_isDragging = false;
            bool Preview_isresizing = false;

            Point Preview_startPos = new Point();

            Point RecstartPos = new Point();



            TranslateTransform canvasTransform = new TranslateTransform();
            Preview_canvas.RenderTransform = canvasTransform;



            //Media Canvas
            Canvas canvasMedia = new Canvas
            {
                Width = 200,
                Height = 150,
                Background = Brushes.Red
            };
            imported_media.Children.Add(canvasMedia);




            //Timeline Canvas
            Canvas canvas = new Canvas
            {
                Width = 200,
                Height = rowHeight,
                Background = Brushes.Red
            };

            timeline.Children.Add(canvas);
            canvases.Add(canvas);





            //Make Timeline Canvas Move
            bool isDragging = false;
            Point startPos = new Point();

            TranslateTransform translate = new TranslateTransform();
            canvas.RenderTransform = translate;

            Rectangle rectRight = new Rectangle
            {
                Width = 5,
                Height = 40,
                Fill = Brushes.Blue,
                Cursor = Cursors.SizeWE
            };

            // Wrap it into your custom class
            PreviewCanvasInfo info = new PreviewCanvasInfo
            {
                previewcanvas = Preview_canvas,
                Canvas = canvas,
                start = translate.X,
                Y = translate.Y,
                end = translate.X + canvas.Width,
                height = Preview_canvas.Height,
                width = Preview_canvas.Width,
                Content = Content,
                original_canvasWidth = canvas.Width,
                original_translateX = 0,
                leftShrinkAmount=0,
                rightShrinkAmount=0,
                rectRight = rectRight,

            };







            if (fileType == ".jpg" || fileType == ".jpeg" || fileType == ".png")
            {


                // Ladda bitmap korrekt
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();


                // Räkna aspect ratio
                aspectRatio = (double)bitmap.PixelHeight / bitmap.PixelWidth;
                targetHeight = targetWidth * aspectRatio; // H = W * ratio
                Preview_canvas.Width = targetWidth;
                Preview_canvas.Height = targetHeight;

                info.AspectRatio = aspectRatio; // save it

                // Original Image
                Image image = new Image
                {
                    Source = bitmap,
                    Width = targetWidth,
                    Height = targetHeight,
                    Stretch = Stretch.Uniform
                };

                // Second Image for other canvas
                Image image2 = new Image
                {
                    Source = bitmap,
                    Width = targetWidth,
                    Height = targetHeight,
                    Stretch = Stretch.Uniform
                };
                canvasMedia.Children.Add(image2);
                info.Content = image;
                Content = image;



                // imageBrush
                Rectangle imageRect = new Rectangle();
                ImageBrush brush = new ImageBrush(bitmap);
                imageRect.Fill = brush;

                imageRect.SetBinding(WidthProperty,
                    new Binding("Width") { Source = Preview_canvas });

                imageRect.SetBinding(HeightProperty,
                    new Binding("Height") { Source = Preview_canvas });

                Preview_canvas.Children.Add(imageRect);


                // ⭐ IMPORTANT PART (tiling)
                ImageBrush brush_canvas = new ImageBrush(bitmap);

                brush_canvas.TileMode = TileMode.Tile;
                brush_canvas.ViewportUnits = BrushMappingMode.Absolute;
                brush_canvas.Viewport = new Rect(0, 0, TileWidth, (int)canvas.Height);
                brush_canvas.Stretch = Stretch.Fill;






                Rectangle tileRect = new Rectangle();


                tileRect.SetBinding(WidthProperty,
                    new Binding("Width") { Source = canvas });

                tileRect.SetBinding(HeightProperty,
                    new Binding("Height") { Source = canvas });


                canvas.Children.Add(tileRect);
                tileRect.Fill = brush_canvas;
                Canvas.SetZIndex(tileRect, -99);


                // Green outline rectangle
                Rectangle outline = new Rectangle
                {
                    Stroke = Brushes.LimeGreen,      // the outline color
                    StrokeThickness = 3,             // thickness of the border
                    Fill = Brushes.Transparent       // no fill
                };

                // Bind it to canvas size
                outline.SetBinding(WidthProperty, new Binding("Width") { Source = canvas });
                outline.SetBinding(HeightProperty, new Binding("Height") { Source = canvas });

                // Make sure it appears above thumbnails
                Canvas.SetZIndex(outline, -1); // above tileRect which is -99

                canvas.Children.Add(outline);

                canvas.Width = imageWidth;
            }


            else if (fileType == ".mp4" || fileType == ".avi" || fileType == ".wmv")
            {
                MediaElement media = new MediaElement
                {
                    Source = new Uri(path),
                    LoadedBehavior = MediaState.Manual,
                    UnloadedBehavior = MediaState.Manual,
                    Stretch = Stretch.Fill   // NOT Uniform!

                };

                media.MediaOpened += (s, e) =>
                {




                    // Räkna aspect ratio
                    double videoAspect = (double)media.NaturalVideoHeight / media.NaturalVideoWidth; // H/W
                    media.Width = targetWidth;
                    media.Height = targetWidth * videoAspect;

                    Preview_canvas.Width = targetWidth;
                    Preview_canvas.Height = targetWidth * videoAspect;

                    info.AspectRatio = videoAspect; // save it




                    // ✅ Get total duration in seconds
                    if (media.NaturalDuration.HasTimeSpan)
                    {
                        TimeSpan duration = media.NaturalDuration.TimeSpan;
                        double totalSeconds = duration.TotalSeconds;

                        canvas.Width = totalSeconds * pixel_per_second;
                    }
                };

                info.Content = media;
                Content = media;
                Preview_canvas.Children.Add(Content);

                //apply image tiles

                // create temp folder for THIS video
                string tempFolder = IOPath.Combine(IOPath.GetTempPath(), "VideoFrames_" + Guid.NewGuid());
                Directory.CreateDirectory(tempFolder);

                // run extraction
                ExtractFramesAsync(path, tempFolder, canvas);





                // Red outline rectangle
                Rectangle outline = new Rectangle
                {
                    Stroke = Brushes.Red,      // the outline color
                    StrokeThickness = 3,             // thickness of the border
                    Fill = Brushes.Transparent       // no fill
                };

                // Bind it to canvas size
                outline.SetBinding(WidthProperty, new Binding("Width") { Source = canvas });
                outline.SetBinding(HeightProperty, new Binding("Height") { Source = canvas });

                // Make sure it appears above thumbnails
                Canvas.SetZIndex(outline, -1); // above tileRect which is -99

                canvas.Children.Add(outline);



            }
            ;




            // Add it to the list
            Preview_canvases.Add(info);


            canvas.MouseLeftButtonDown += (s, me) =>
            {
                isDragging = true;
                startPos = me.GetPosition(timeline);
                canvas.CaptureMouse();
            };

            canvas.MouseMove += (s, me) =>
            {
                if (!isDragging) return;

                Point currentPos = me.GetPosition(timeline);

                double offsetX = currentPos.X - startPos.X;
                double offsetY = currentPos.Y - startPos.Y;

                translate.X += offsetX;
                translate.Y += offsetY;

                startPos = currentPos;


                highlightRowShower(canvas, currentPos);
                DebugLiv.Text = $"Zoom= {zoomLevel}, tranlateX={translate.X}, leftShrinkAmount= {info.leftShrinkAmount} , Wodth={canvas.Width},\n translate={translate.X}";

            };

            canvas.MouseLeftButtonUp += (s, me) =>
            {
                isDragging = false;
                canvas.ReleaseMouseCapture();
                RowSnap(translate, canvas);

                // Save it to preview canvas
                // Update stored info
                info.start = translate.X;
                info.Y = translate.Y;
                info.end = translate.X + canvas.Width;

            };







            // LEFT/RIGHT Dragable inside Timeline Canvas
            Rectangle rectLeft = new Rectangle
            {
                Width = 5,
                Height = 40,
                Fill = Brushes.Green,
                Cursor = Cursors.SizeWE
            };



            canvas.Children.Add(rectLeft);
            canvas.Children.Add(rectRight);

            Canvas.SetLeft(rectRight, canvas.Width - rectRight.Width);





            bool Right_isDragging = false;
            bool Left_isDragging = false;

            Point rectStartPos = new Point();

            double leftShrinkAmount = 0;
            double rightShrinkAmount = 0;


            rectLeft.MouseLeftButtonDown += (s, me2) =>
            {
                Left_isDragging = true;
                Right_isDragging = false;

                rectStartPos = me2.GetPosition(timeline);
                rectLeft.CaptureMouse();
                isDragging = false;
                me2.Handled = false;

            };

            rectRight.MouseLeftButtonDown += (s, me2) =>
            {
                Right_isDragging = true;
                Left_isDragging = false;

                rectStartPos = me2.GetPosition(timeline);
                rectRight.CaptureMouse();

                isDragging = false;
                me2.Handled = false;
            };

            this.MouseMove += (s, me2) =>
            {
                if (!Left_isDragging && !Right_isDragging) return;
                isDragging = false;

                Point currentPos = me2.GetPosition(timeline);
                double dx = currentPos.X - rectStartPos.X;

                if (Right_isDragging)
                {
                    rightShrinkAmount = info.rightShrinkAmount;
                    if (dx > 0 && rightShrinkAmount < 0) // trying to grow right side, but limit so that it doesnt grow past original
                    {
                        canvas.Width += dx;
                        rightShrinkAmount += dx;
                        info.rightShrinkAmount = rightShrinkAmount;

                    }

                    else if (dx < 0 && canvas.Width > 25) // trying shrink right side + limit how small box can be
                    {
                        canvas.Width += dx;
                        rightShrinkAmount += dx;
                        info.rightShrinkAmount = rightShrinkAmount;
                    }


                }
                else if (Left_isDragging)
                {
                     leftShrinkAmount= info.leftShrinkAmount;
                    double newWidth = canvas.Width - dx;
                    if (dx < 0 && leftShrinkAmount < 0) // trying to grow left side, but limit so that it doesnt grow past original
                    {
                        translate.X += dx;
                        canvas.Width = newWidth;

                        leftShrinkAmount -= dx;
                        info.leftShrinkAmount = leftShrinkAmount;

                    }

                    else if (dx > 0 && canvas.Width > 25) // trying shrink left side + limit how small box can be
                    {
                        translate.X += dx;
                        canvas.Width = newWidth;
                        leftShrinkAmount -= dx;
                        info.leftShrinkAmount = leftShrinkAmount;
                    }


                    DebugLiv.Text = $"Zoom= {zoomLevel}, tranlateX={translate.X}, leftShrinkAmount= {info.leftShrinkAmount} , \n Wodth={canvas.Width}, translate={translate.X}";

                }

                // important: reset start position so dragging is stable
                rectStartPos = currentPos;
                // Update stored info
                info.start = translate.X;
                info.Y = translate.Y;
                info.end = translate.X + canvas.Width;

                // keep right handle on edge
                Canvas.SetLeft(rectRight, canvas.Width - rectRight.Width);


            };

            this.MouseLeftButtonUp += (s, me2) =>
            {
                Left_isDragging = false;
                Right_isDragging = false;

                rectLeft.ReleaseMouseCapture();
                rectRight.ReleaseMouseCapture();
            };











            if (fileType == ".mp3")
            {
                // create temp folder for THIS video
                string tempFolder = IOPath.Combine(IOPath.GetTempPath(), "SoundFrames_" + Guid.NewGuid());
                Directory.CreateDirectory(tempFolder);





                // Use a MediaElement to get duration
                MediaElement audio = new MediaElement
                {
                    Source = new Uri(path),
                    LoadedBehavior = MediaState.Manual,
                    UnloadedBehavior = MediaState.Manual,
                };

                audio.MediaOpened += (s, e) =>
                {
                    if (audio.NaturalDuration.HasTimeSpan)
                    {
                        TimeSpan duration = audio.NaturalDuration.TimeSpan;
                        double totalSeconds = duration.TotalSeconds;
                        canvas.Width = totalSeconds * pixel_per_second;

                        // Now you can use totalSeconds for frame/tile calculations
                    }
                };

                // Must load the media
                audio.Loaded += (s, e) => audio.Play(); // triggers MediaOpened

                info.Content = audio;
                Content = audio;
                Preview_canvas.Children.Add(Content);




                int Frames = 90;
                ExtractSound(path, tempFolder, canvas, Frames);

                Preview_canvas.Children.Clear();

                canvas.Background = Brushes.Purple;

                previewCanvas.Children.Remove(Preview_canvas);
                return;

            }
            ;





            // PReview Drag


            double startWidth = Preview_canvas.Width;
            double startHeight = Preview_canvas.Height;
            double aspect = startWidth / startHeight;





            rect.MouseLeftButtonDown += (s, me) =>
            {
                Preview_isresizing = true;
                RecstartPos = me.GetPosition(previewCanvas);
                me.Handled = true; // prevent canvas from also reacting
            };


            // start Timer
            // Mouse down: start dragging
            Preview_canvas.MouseLeftButtonDown += (s, me) =>
            {
                Preview_isDragging = true;
                Preview_startPos = me.GetPosition(previewCanvas);
                Preview_canvas.CaptureMouse();




            };

            Preview_canvas.MouseMove += (s, me) =>
            {
                if (!Preview_isDragging) return;

                Point currentPos = me.GetPosition(previewCanvas);

                double dx = currentPos.X - Preview_startPos.X;
                double dy = currentPos.Y - Preview_startPos.Y;

                // Current position
                double left = Canvas.GetLeft(Preview_canvas);
                double top = Canvas.GetTop(Preview_canvas);

                // Move normally
                left += dx;
                top += dy;

                // --- center of preview canvas ---
                double canvasCenterX = left + Preview_canvas.Width / 2;
                double canvasCenterY = top + Preview_canvas.Height / 2;

                // --- center lines ---
                double verticalLineX = previewCanvas.ActualWidth / 2;
                double horizontalLineY = previewCanvas.ActualHeight / 2;

                const double snapDistance = 25;

                double MsnapDistance = Preview_canvas.ActualHeight * 0.35;
                // --- Vertical snap ---
                if (Math.Abs(canvasCenterX - verticalLineX) <= snapDistance && Math.Abs(currentPos.X - verticalLineX) <= MsnapDistance)
                {
                    left = verticalLineX - Preview_canvas.Width / 2;
                    Vertical_line.Fill = Brushes.Red;
                }
                else
                {
                    Vertical_line.Fill = Brushes.Transparent;
                }

                // --- Horizontal snap ---
                if (Math.Abs(canvasCenterY - horizontalLineY) <= snapDistance && Math.Abs(currentPos.Y - horizontalLineY) <= MsnapDistance)
                {
                    top = horizontalLineY - Preview_canvas.Height / 2;
                    Horizontal_line.Fill = Brushes.Red;
                }
                else
                {
                    Horizontal_line.Fill = Brushes.Transparent;
                }

                // Apply position
                Canvas.SetLeft(Preview_canvas, left);
                Canvas.SetTop(Preview_canvas, top);

                Preview_startPos = currentPos;
            };




            Preview_canvas.MouseLeftButtonUp += (s, me) =>
            {
                Preview_isDragging = false;
                Preview_canvas.ReleaseMouseCapture();

                Vertical_line.Fill = Brushes.Transparent;
                Horizontal_line.Fill = Brushes.Transparent;


            };

            // Handle mouse move on the parent (previewWindow or canvas)
            this.MouseMove += (s, me) =>
            {
                if (!Preview_isresizing || Mouse.LeftButton != MouseButtonState.Pressed)
                    return;

                Point currentPos = me.GetPosition(previewCanvas);

                double dx = currentPos.X - RecstartPos.X;
                double dy = currentPos.Y - RecstartPos.Y;

                double delta = (Math.Abs(dx) > Math.Abs(dy)) ? dx : dy;

                double minSize = 30;
                double maxSize = 250;

                // Resize ONLY width
                double newWidth = Preview_canvas.Width - delta;

                // Clamp width only
                newWidth = Math.Max(minSize, Math.Min(maxSize, newWidth));

                // Maintain aspect ratio
                // aspectRatio = H / W
                double newHeight = newWidth * info.AspectRatio;
                Preview_canvas.Width = newWidth;
                Preview_canvas.Height = newHeight;







                RecstartPos = currentPos;

                // save values
                info.width = newWidth;
                info.height = newHeight;



                Content.Width = Preview_canvas.Width;
                Content.Height = Preview_canvas.Height;


            };




            // Handle mouse up on the parent
            this.MouseLeftButtonUp += (s, me) =>
            {
                Preview_isresizing = false;


            };







            // apply image tile for videos
        }


        private async Task ExtractSound(string videoPath, string tempFolder, Canvas canvas, int Frames)
        {
            const int tileWidth = 2; // thin bars
            int height = (int)canvas.Height;

            string rawPath = IOPath.Combine(tempFolder, "audio.raw");

            // ---------- 1. EXTRACT RAW AUDIO ----------
            await Task.Run(() =>
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments =
                        $"-i \"{videoPath}\" -ac 1 -ar 8000 -f s16le \"{rawPath}\" -hide_banner -loglevel error",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process p = Process.Start(psi))
                    p.WaitForExit();
            });

            if (!File.Exists(rawPath))
                return;

            // ---------- 2. READ SAMPLES ----------
            byte[] bytes = File.ReadAllBytes(rawPath);
            short[] samples = new short[bytes.Length / 2];
            Buffer.BlockCopy(bytes, 0, samples, 0, bytes.Length);

            int samplesPerFrame = samples.Length / Frames;
            if (samplesPerFrame <= 0) return;

            double[] peaks = new double[Frames];

            // ---------- 3. CALCULATE PEAK PER FRAME ----------
            for (int f = 0; f < Frames; f++)
            {
                int start = f * samplesPerFrame;
                int end = Math.Min(start + samplesPerFrame, samples.Length);

                short max = 0;

                for (int i = start; i < end; i++)
                {
                    short val = (short)Math.Abs(samples[i]);
                    if (val > max) max = val;
                }

                peaks[f] = max / 32768.0; // normalize 0..1
            }

            // ---------- 4. DRAW WAVEFORM BITMAP ----------
            DrawingVisual dv = new DrawingVisual();

            using (DrawingContext dc = dv.RenderOpen())
            {
                Pen pen = new Pen(Brushes.White, 1);

                double centerY = height / 2.0;

                for (int i = 0; i < Frames; i++)
                {
                    double amp = peaks[i] * centerY;

                    double x = i * tileWidth;

                    dc.DrawLine(
                        pen,
                        new Point(x, centerY - amp),
                        new Point(x, centerY + amp));
                }
            }

            RenderTargetBitmap bmp = new RenderTargetBitmap(
                Frames * tileWidth,
                height,
                96, 96,
                PixelFormats.Pbgra32);

            bmp.Render(dv);

            // ---------- 5. BRUSH TILE (same system as your thumbnails) ----------
            ImageBrush brush = new ImageBrush(bmp)
            {
                Stretch = Stretch.Fill
            };

            Rectangle rect = new Rectangle();

            rect.SetBinding(WidthProperty,
                new Binding("Width") { Source = canvas });

            rect.SetBinding(HeightProperty,
                new Binding("Height") { Source = canvas });

            rect.Fill = brush;

            Canvas.SetZIndex(rect, -98); // behind thumbnails

            canvas.Children.Add(rect);
        }


        private async Task ExtractFramesAsync(string videoPath, string tempFolder, Canvas canvas)
        {
            const int TileWidth = 90;

            await Task.Run(() =>
            {
                string outputPattern = IOPath.Combine(tempFolder, "frame_%04d.png");

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-i \"{videoPath}\" -vf fps=1/5 \"{outputPattern}\" -hide_banner -loglevel error",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process proc = Process.Start(psi))
                    proc.WaitForExit();
            });

            // ---------- UI THREAD ----------

            string[] files = Directory.GetFiles(tempFolder, "*.png");
            if (files.Length == 0) return;

            Array.Sort(files); // important: frame order

            int tileHeight = (int)canvas.Height;

            // ===== BUILD FILMSTRIP BITMAP =====
            DrawingVisual dv = new DrawingVisual();

            using (DrawingContext dc = dv.RenderOpen())
            {
                for (int i = 0; i < files.Length; i++)
                {
                    BitmapImage bmp = new BitmapImage(new Uri(files[i]));

                    Rect r = new Rect(i * TileWidth, 0, TileWidth, tileHeight);

                    dc.DrawImage(bmp, r);
                }
            }

            RenderTargetBitmap filmStrip = new RenderTargetBitmap(
                TileWidth * files.Length,
                tileHeight,
                96, 96,
                PixelFormats.Pbgra32);

            filmStrip.Render(dv);

            // ===== CREATE TILED BRUSH (same as your image version) =====
            ImageBrush brush_canvas = new ImageBrush(filmStrip);

            brush_canvas.TileMode = TileMode.Tile;
            brush_canvas.ViewportUnits = BrushMappingMode.Absolute;
            brush_canvas.Viewport = new Rect(0, 0, TileWidth, tileHeight);
            brush_canvas.Stretch = Stretch.Fill;

            // ===== SINGLE RECTANGLE ONLY =====
            Rectangle tileRect = new Rectangle();

            tileRect.SetBinding(WidthProperty,
                new Binding("Width") { Source = canvas });

            tileRect.SetBinding(HeightProperty,
                new Binding("Height") { Source = canvas });

            tileRect.Fill = brush_canvas;
            canvas.Children.Add(tileRect);
            Canvas.SetZIndex(tileRect, -99);

        }




        private void RowSnap(TranslateTransform translate, Canvas canvas)
        {
            // Snap Y to rowShower

            // Start X at rowShower's current left
            double currentX = translate.X; // använd transformens X som startpunkt
            double targetX = currentX;     // börja från nuvarande transform
            double target_decoy = new double();

            double canvasWidth = canvas.Width;
            double parentWidth = timeline.ActualWidth;

            bool foundSpot = false;

            // Try to find a free spot
            while (targetX <= parentWidth)
            {
                bool collision = false;

                foreach (Canvas other in canvases)
                {
                    if (other == canvas) continue;
                    // Use transform for other canvases
                    TranslateTransform otherT = other.RenderTransform as TranslateTransform;
                    double otherX = (otherT != null) ? otherT.X : 0;
                    double otherY = (otherT != null) ? otherT.Y : 0;
                    double otherWidth = other.Width;

                    target_decoy = otherX;
                    // Check collision: only if on the same Y row & more to right than Canvas or its right edge is more to the right than the left edge of Canvas
                    if (Math.Abs(otherY - Canvas.GetTop(rowShower)) < 10 && (otherX >= targetX || otherX + otherWidth > targetX))
                    {

                        if (otherX + otherWidth <= targetX + canvasWidth || otherX < targetX + canvasWidth)
                        {
                            collision = true;
                            targetX = otherX + otherWidth; // update and check next position and so on
                            break; // this X is blocked
                        }
                    }
                }

                if (!collision)
                {
                    foundSpot = true;

                    break; // found free X
                }


            }


            if (foundSpot)
            {
                translate.X = targetX;
                double rowTop = Canvas.GetTop(rowShower);
                if (double.IsNaN(rowTop)) rowTop = 0; // fallback to 0 if unset
                translate.Y = rowTop;


            }
            else
            {
                // No place found, do nothing for now
                // do something if no place
            }

            rowShower.Fill = Brushes.Transparent;
        }




        private void highlightRowShower(Canvas canvas, Point mousePos)
        {
            TranslateTransform transform = canvas.RenderTransform as TranslateTransform;
            if (transform == null)
            {
                transform = new TranslateTransform();
                canvas.RenderTransform = transform;
            }

            double currentY = transform.Y;

            // Snap to nearest multiple of 40
            double closest = Math.Round(currentY / 40.0) * 40;

            // Limit
            double limit = 80;
            closest = Math.Min(closest, limit);

            rowShower.Width = canvas.Width;

            double thisX = transform.X;
            double thisY = transform.Y;

            Canvas nearest = null;
            double nearestDistance = double.MaxValue;

            foreach (Canvas other in canvases)
            {
                if (other == canvas) continue;

                TranslateTransform otherT = other.RenderTransform as TranslateTransform;
                if (otherT == null) continue;

                double otherX = otherT.X;
                double otherY = otherT.Y;
                double otherRight = otherX + other.Width;

                // Check distance
                double dx = Math.Abs(thisX - otherRight);
                double dy = Math.Abs(thisY - otherY);

                if (dx <= SNAP_DISTANCE && dy <= SNAP_DISTANCE / 4)
                {
                    double dist = Math.Sqrt(dx * dx + dy * dy);
                    if (dist < nearestDistance)
                    {
                        nearestDistance = dist;
                        nearest = other;
                    }
                }
            }

            if (nearest != null)
            {
                // Snap to nearest canvas
                TranslateTransform nearestT = nearest.RenderTransform as TranslateTransform;
                double nearestRight = nearestT.X + nearest.Width;
                Canvas.SetLeft(rowShower, nearestRight);
                Canvas.SetTop(rowShower, nearestT.Y);
            }
            else
            {
                // Snap to row multiples
                Canvas.SetTop(rowShower, closest);
                Canvas.SetLeft(rowShower, transform.X);
            }

            rowShower.Fill = Brushes.Gray;
        }




        private void PreviewChecker()
        {
            double timelineX = Vertical_Timeline_Transform.X;
            StringBuilder debugText = new StringBuilder();

            foreach (var previewCanvas in Preview_canvases)
            {
                // Check if timeline is within the canvas start/end
                bool isVisible = previewCanvas.start < timelineX && timelineX <= previewCanvas.end;

                // Set canvas visibility
                previewCanvas.previewcanvas.Opacity = isVisible ? 1 : 0;
                previewCanvas.previewcanvas.IsHitTestVisible = isVisible;

                // Handle media playback if it's a MediaElement
                if (previewCanvas.Content is MediaElement media)
                {
                    if (isVisible)
                        media.Play();
                    else
                        media.Pause();
                }

                // Optionally, you can still debug width/height/aspect
                debugText.AppendLine($"Start={previewCanvas.start}, Y={previewCanvas.Y}, End={previewCanvas.end}, aspect={previewCanvas.AspectRatio}, Zoom= {zoomLevel}");
            }

            DebugLiv.Text = debugText.ToString();
        }


        // Allowed zoom levels
        double[] allowedValues = new double[] { 0.125, 0.25, 0.5, 1, 2, 4, 8 };

        double lastZoomValue=1;
        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int step = (int)ZoomSlider.Value; // slider index
            zoomLevel = allowedValues[step];



            DrawTimeline();
        }

        private void DrawTimeline()
        {
            if (timeShower == null) return; // prevents crash if called too early

            timeShower.Children.Clear();
            const double pxPerSecond = 50;     // ALWAYS fixed
            const double bigTickSeconds = 5;   // number every 5 seconds

            for (double x = 0; x <= rectangleWidth; x += pxPerSecond)
            {
                bool big = ((x / pxPerSecond) % bigTickSeconds) == 0;

                Line line = new Line
                {
                    X1 = x,
                    Y1 = 0,
                    X2 = x,
                    Y2 = big ? 20 : 10,
                    Stroke = Brushes.White,
                    StrokeThickness = 1
                };

                timeShower.Children.Add(line);

                if (big)
                {
                    // pixel seconds (without zoom)
                    double baseSeconds = (x / pxPerSecond);

                    // apply zoom ONLY to time meaning
                    double shownSeconds = baseSeconds / zoomLevel;

                    TextBlock txt = new TextBlock
                    {
                        Text = $"{shownSeconds:0.##}s",
                        Foreground = Brushes.White,
                        FontSize = 12
                    };

                    Canvas.SetLeft(txt, x + 2);
                    Canvas.SetTop(txt, 20);

                    timeShower.Children.Add(txt);
                }
            }
            DebugLiv.Text = $"Zoom= {zoomLevel}";


            if (canvases == null || canvases.Count == 0)
                            return;




                double scaleFactor = zoomLevel / lastZoomValue;


                foreach (var info in Preview_canvases)
                {
                    // Use transform for other canvases
                    TranslateTransform translate = info.Canvas.RenderTransform as TranslateTransform;


                    info.Canvas.Width *= scaleFactor;


                      // Scale current X relative to last zoom
                      translate.X *= scaleFactor;
                      info.leftShrinkAmount *= scaleFactor;
                      info.rightShrinkAmount *= scaleFactor;

                    DebugLiv.Text = $"Zoom= {zoomLevel}, Width={info.Canvas.Width}, leftShrinkAmount= {info.leftShrinkAmount}, \n translate={translate.X} ";

                    // keep right handle on edge
                    Canvas.SetLeft(info.rectRight, info.Canvas.Width - info.rectRight.Width);

            }

                 lastZoomValue = zoomLevel;




        }
    }
}
