
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using static Jvedio.StaticVariable;

namespace Jvedio
{
    public  class Jvedio_BaseWindow : Window
    {
        public Point WindowPoint = new Point(100, 100);
        public Size WindowSize = new Size(800, 500);
        public JvedioWindowState WinState = JvedioWindowState.Normal;

        public Jvedio_BaseWindow()
        {
            InitStyle();
            AdjustWindow();
            this.Loaded += delegate { InitEvent(); };

            
        }

        private void SaveWindow()
        {
            if (this.WindowState != WindowState.Minimized)
            {
                if (this.WindowState == WindowState.Normal) WinState =JvedioWindowState.Normal;
                else if (this.WindowState == WindowState.Maximized) WinState = JvedioWindowState.FullScreen;
                else if (this.Width == SystemParameters.WorkArea.Width & this.Height == SystemParameters.WorkArea.Height) WinState = JvedioWindowState.Maximized;

                WindowConfig cj = new WindowConfig(this.GetType().Name);
                Rect rect = new Rect(this.Left, this.Top, this.Width, this.Height);
                cj.Save(rect, WinState);
            }
        }

        private void AdjustWindow()
        {
            //读取窗体设置
            WindowConfig cj = new WindowConfig(this.GetType().Name);
            Rect rect;
            (rect, WinState) = cj.GetValue();

            if ( rect.X!=-1 && rect.Y!=-1)
            {
                //读到属性值
                if (WinState == JvedioWindowState.Maximized)
                {
                    MaxWindow(this, new MouseButtonEventArgs(InputManager.Current.PrimaryMouseDevice, 0, MouseButton.Left));
                }
                else
                {
                    this.WindowState = WindowState.Normal;
                    this.Left = rect.X > 0 ? rect.X : 0;
                    this.Top = rect.Y > 0 ? rect.Y : 0;
                    this.Height = rect.Height > 100 ? rect.Height : 100;
                    this.Width = rect.Width > 100 ? rect.Width : 100;
                    if (this.Width == SystemParameters.WorkArea.Width | this.Height == SystemParameters.WorkArea.Height) { WinState = JvedioWindowState.Maximized; }
                }
            }
            else
            {
                WinState = JvedioWindowState.Normal;
                this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            HideMargin();



        }

        private void InitStyle()
        {
            this.Style = (Style)App.Current.Resources["Jvedio_BaseWindowStyle"];
        }

        private void InitEvent()
        {
            ControlTemplate baseWindowTemplate = (ControlTemplate)App.Current.Resources["BaseWindowControlTemplate"];
            Border minBtn = (Border)baseWindowTemplate.FindName("BorderMin", this);
            minBtn.MouseLeftButtonUp += delegate (object sender, MouseButtonEventArgs e)
            {
                MinWindow();
            };

            Border maxBtn = (Border)baseWindowTemplate.FindName("BorderMax", this);
            maxBtn.MouseLeftButtonUp += MaxWindow;

            Border closeBtn = (Border)baseWindowTemplate.FindName("BorderClose", this);
            closeBtn.MouseLeftButtonUp += delegate (object sender, MouseButtonEventArgs e)
            {
                FadeOut();
                
            };

            Border borderTitle = (Border)baseWindowTemplate.FindName("BorderTitle", this);
            borderTitle.MouseMove += MoveWindow;
            borderTitle.MouseLeftButtonDown += delegate (object sender, MouseButtonEventArgs e)
            {
                if (e.ClickCount >= 2)
                {
                    MaxWindow(this, new MouseButtonEventArgs(InputManager.Current.PrimaryMouseDevice, 0, MouseButton.Left));
                }
            };

            this.Closing += delegate {
                SaveWindow();
            };



            FadeIn();

            //if (System.Windows.Interop.ComponentDispatcher.IsThreadModal)
            //    this.Owner = App.Current.MainWindow;

        }



        public async void FadeIn()
        {
            if (Properties.Settings.Default.EnableWindowFade)
            {
                this.Opacity = 0;
                double opacity = this.Opacity;
                await Task.Run(() => {
                    while (opacity < 0.5)
                    {
                        this.Dispatcher.Invoke((Action)delegate { this.Opacity += 0.05; opacity = this.Opacity; });
                        Task.Delay(1).Wait();
                    }
                });
            }
            this.Opacity = 1;
        }

        public async void FadeOut()
        {
            if (Properties.Settings.Default.EnableWindowFade)
            {
                double opacity = this.Opacity;
                await Task.Run(() => {
                    while (opacity > 0.1)
                    {
                        this.Dispatcher.Invoke((Action)delegate { this.Opacity -= 0.05; opacity = this.Opacity; });
                        Task.Delay(1).Wait();
                    }
                });
                this.Opacity = 0;
            }
           
            this.Close();
        }


        public async void MinWindow()
        {
            if (Properties.Settings.Default.EnableWindowFade)
            {
                double opacity = this.Opacity;
                await Task.Run(() =>
                {
                    while (opacity > 0.2)
                    {
                        this.Dispatcher.Invoke((Action)delegate { this.Opacity -= 0.1; opacity = this.Opacity; });
                        Task.Delay(20).Wait();
                    }
                });
            }
            this.WindowState = WindowState.Minimized;
            this.Opacity = 1;

        }



        public void MaxWindow(object sender, MouseButtonEventArgs e)
        {
            if (WinState == JvedioWindowState.Normal)
            {
                //最大化
                WinState = JvedioWindowState.Maximized;
                WindowPoint = new Point(this.Left, this.Top);
                WindowSize = new Size(this.Width, this.Height);
                this.Height = SystemParameters.WorkArea.Height;
                this.Width = SystemParameters.WorkArea.Width;
                this.Left = SystemParameters.WorkArea.Left;
                this.Top = SystemParameters.WorkArea.Top;

            }
            else 
            {
                WinState = JvedioWindowState.Normal;
                this.Left = WindowPoint.X;
                this.Top = WindowPoint.Y;
                this.Width = WindowSize.Width;
                this.Height = WindowSize.Height;
            }
            this.WindowState = WindowState.Normal;
            HideMargin();
        }

        private void MoveWindow(object sender, MouseEventArgs e)
        {
            //移动窗口
            if (e.LeftButton == MouseButtonState.Pressed && WinState == JvedioWindowState.Normal)
            {
                this.DragMove();
            }
        }


        private void HideMargin()
        {
            ControlTemplate baseWindowTemplate = (ControlTemplate)App.Current.Resources["BaseWindowControlTemplate"];
            Border borderTitle = (Border)baseWindowTemplate.FindName("BorderTitle", this);
            Border borderMain = (Border)baseWindowTemplate.FindName("BorderMain", this);
            ResizeGrip windowResizeGrip=(ResizeGrip)baseWindowTemplate.FindName("WindowResizeGrip", this);
            if (WinState == JvedioWindowState.Normal)
            {
                if(borderTitle!=null) borderTitle.Margin = new Thickness(2, 2, 2, 0);
                if (borderMain != null) borderMain.Margin = new Thickness(2, 0, 2, 2);
                if (windowResizeGrip != null) windowResizeGrip.Visibility = Visibility.Visible;
            }
            else if (WinState == JvedioWindowState.Maximized || this.WindowState==WindowState.Maximized)
            {
                if (borderTitle != null) borderTitle.Margin = new Thickness(0);
                if (borderMain != null) borderMain.Margin = new Thickness(0);
                if (windowResizeGrip != null) windowResizeGrip.Visibility = Visibility.Collapsed;
            }
        }


    }




}
