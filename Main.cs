using Microsoft.Win32;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
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
        public MainWindow()
        {
            InitializeComponent();
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
            Canvas canvas = new Canvas
            {
                Width = 200,
                Height = 150,
                Background = Brushes.Red
            };

            imported_media.Children.Add(canvas);
        }



    }
}
