using Microsoft.Win32;
using System.Collections.Generic;
using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Threading;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {


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
            public Canvas Canvas { get; set; }
            public double start { get; set; }
            public double Y { get; set; }
            public double end { get; set; }
            public double height { get; set; }
            public double width { get; set; }
            // Instead of separate Image & MediaElement, use UIElement
            public FrameworkElement Content { get; set; }

            // Store aspect ratio H/W
            public double AspectRatio { get; set; } = 1;

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
        }



        private void import_media(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Media Files|*.jpg;*.jpeg;*.png;*.mp4;*.mov;*.avi;*.wmv"
            };

            if (dialog.ShowDialog() != true)
                return;

            foreach (var file in dialog.FileNames)
            {
                // Hämta filtypen
                string fileType = System.IO.Path.GetExtension(file).ToLower(); // t.ex. ".jpg"

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
            double speed = 25; // pixels per second

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
            const double targetWidth = 200; // ← du bestämmer bredden
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



            // Wrap it into your custom class
            PreviewCanvasInfo info = new PreviewCanvasInfo
            {
                Canvas = Preview_canvas,
                start = translate.X,
                Y = translate.Y,
                end = translate.X + canvas.Width,
                height = Preview_canvas.Height,
                width = Preview_canvas.Width,
                Content=Content,

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

                Preview_canvas.Background = Brushes.Black;

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



                };

                info.Content = media;
                Content = media;

                //hur stoppar man en video
            }

            Preview_canvas.Children.Add(Content);

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

            if (canvases.Count % 2 == 0)
            {
                canvas.Background = Brushes.Green;
            }





            // LEFT/RIGHT Dragable inside Timeline Canvas
            Rectangle rectLeft = new Rectangle
            {
                Width = 5,
                Height = 40,
                Fill = Brushes.Green,
                Cursor = Cursors.SizeWE
            };

            Rectangle rectRight = new Rectangle
            {
                Width = 5,
                Height = 40,
                Fill = Brushes.Blue,
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
                    if (dx > 0 && rightShrinkAmount < 0) // trying to grow right side, but limit so that it doesnt grow past original
                    {
                        canvas.Width += dx;
                        rightShrinkAmount += dx;

                    }

                    else if (dx < 0 && canvas.Width > 25) // trying shrink right side + limit how small box can be
                    {
                        canvas.Width += dx;
                        rightShrinkAmount += dx;
                    }


                }
                else if (Left_isDragging)
                {
                    double newWidth = canvas.Width - dx;
                    if (dx < 0 && leftShrinkAmount < 0) // trying to grow left side, but limit so that it doesnt grow past original
                    {
                        translate.X += dx;
                        canvas.Width = newWidth;

                        leftShrinkAmount -= dx;

                    }

                    else if (dx > 0 && canvas.Width > 25) // trying shrink left side + limit how small box can be
                    {
                        translate.X += dx;
                        canvas.Width = newWidth;
                        leftShrinkAmount -= dx;
                    }

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
                previewCanvas.Canvas.Opacity = isVisible ? 1 : 0;
                previewCanvas.Canvas.IsHitTestVisible = isVisible;

                // Handle media playback if it's a MediaElement
                if (previewCanvas.Content is MediaElement media)
                {
                    if (isVisible)
                        media.Play();
                    else
                        media.Pause();
                }

                // Optionally, you can still debug width/height/aspect
                debugText.AppendLine($"Start={previewCanvas.start}, Y={previewCanvas.Y}, End={previewCanvas.end}, aspect={previewCanvas.AspectRatio}");
            }

            DebugLiv.Text = debugText.ToString();
        }








    }
}
