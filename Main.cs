using Microsoft.Win32;
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

        private List<Canvas> Preview_canvases = new List<Canvas>();
        public MainWindow()
        {
            InitializeComponent();
            rowShower.Fill = Brushes.Transparent;
        }

        private void import_media(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();

            dialog.Multiselect = true;

            dialog.Filter =
                "Media Files|*.jpg;*.jpeg;*.png;*.mp4;*.mov;*.avi;*.wmv";

            if (dialog.ShowDialog() != true)
                return;

            foreach (var file in dialog.FileNames)
            {
                AddMediaToCanvas(file);
            }
        }

        private void AddMediaToCanvas(string path)
        {
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












            // Create a new Canvas
            Canvas Preview_canvas = new Canvas
            {
                Width = 30,
                Height = 40,
                Background = Brushes.Black
            };

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

            Preview_canvases.Add(Preview_canvas);







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
                if (!Preview_isresizing || Mouse.LeftButton != MouseButtonState.Pressed) return;

                Point currentPos = me.GetPosition(previewCanvas);
                double dx = currentPos.X - RecstartPos.X;
                double dy = currentPos.Y - RecstartPos.Y;

                double delta = (Math.Abs(dx) > Math.Abs(dy)) ? dx : dy;

                Preview_canvas.Width = Math.Max(1, Preview_canvas.Width - delta);
                Preview_canvas.Height = Math.Max(1, Preview_canvas.Height - delta);

                RecstartPos = currentPos;
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
                translate.Y = Canvas.GetTop(rowShower);

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

                if (dx <= SNAP_DISTANCE && dy <= SNAP_DISTANCE/4)
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

    }



}
