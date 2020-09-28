
using Jvedio.ViewModel;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Jvedio
{
    /// <summary>
    /// WindowImageViewer.xaml 的交互逻辑
    /// </summary>
    public partial class WindowImageViewer : Window
    {

        VieModel_ImageViewer ImageViewerVieModel;

        private int imageindex = 0;
        public WindowImageViewer(string id,int index)
        {
            InitializeComponent();

            ImageViewerVieModel = new VieModel_ImageViewer(id);
            this.DataContext = ImageViewerVieModel;
            imageindex = index;
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Maximized;
        }

        private void CloseWindow(object sender, MouseEventArgs e)
        {
                this.Hide();
        }


        private void Grid_KeyDown(object sender, KeyEventArgs e)
        {

        }

        private void scrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - e.Delta);
            e.Handled = true;
        }

        private void Image_MouseUp(object sender, MouseButtonEventArgs e)
        {
            
            Image image = sender as Image;
            SetImage(image.Source);


        }

        public void SetImage(ImageSource imageSource)
        {
            var height = this.Height;
            mainImage.Source = imageSource;
            //if (imageSource.Height < height - 250) { mainImage.Height = imageSource.Height; } else { mainImage.Height = height - 250; }
        }




        private void Grid_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (imageItemsControl.Items.Count == 0) return;
            if (e.Delta > 0)
            {
                imageindex -= 1;
            }
            else
            {
                imageindex += 1;
            }

            if (imageindex < 0) { imageindex = 0; }else if(imageindex>= imageItemsControl.Items.Count) { imageindex = imageItemsControl.Items.Count - 1; }
            var image = imageItemsControl.Items[imageindex];
            //Console.WriteLine(imageItemsControl.Items[imageindex].GetType().ToString());
            SetImage((ImageSource)image);
        }

        private void baseGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    this.Close();
                    break;
                case Key.Left:
                    Grid_MouseWheel(this, new MouseWheelEventArgs(InputManager.Current.PrimaryMouseDevice, 0,1));
                    break;
                case Key.Right:
                    Grid_MouseWheel(this, new MouseWheelEventArgs(InputManager.Current.PrimaryMouseDevice, 0, -1));
                    break;
                case Key.Up:

                    break;
                case Key.Down:

                    break;
                case Key.Enter:

                    break;
                default:
                    break;

            }
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            mainImage.Height = this.Height - 250;
            mainImage.Width = this.Width;
            SetImage(ImageViewerVieModel.DetailMovie.extraimagelist[imageindex]);
        }
    }
}
