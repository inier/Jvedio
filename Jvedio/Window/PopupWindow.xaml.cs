using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Jvedio
{
    /// <summary>
    /// Msgbox.xaml 的交互逻辑
    /// </summary>
    public partial class PopupWindow : Window
    {
        string Text;
        public Window Window;
        public DispatcherTimer DispatcherTimer;
        public DispatcherTimer CloseTimer;

        public PopupWindow(Window window, string text,bool WaitToClose=false)
        {
            InitializeComponent();
            Text = text;
            Window = window;
            TextBlock.Text = text;

            if (window.WindowState == WindowState.Minimized) window.WindowState = WindowState.Normal;
            window.Activate();
            window.Focus();
            SetLocaiton();
            this.Topmost = true;
            window.LocationChanged += (s, e) => { SetLocaiton(); };
            window.SizeChanged += (s, e) => { SetLocaiton(); };
            window.Closed += (s, e) => { this.Close(); };
            window.Deactivated += (s, e) => { this.Topmost = false; };
            window.Activated += (s, e) => { this.Topmost=true; };
            window.IsVisibleChanged += (s, e) => { if (!(bool)e.NewValue) this.Close(); };
            window.StateChanged += (s, e) => {
                if (window.WindowState == WindowState.Minimized)
                {
                    this.WindowState = WindowState.Minimized;
                }
                else
                {
                    this.WindowState = WindowState.Normal;
                    SetLocaiton();
                }
            };

            //结束动画
            DispatcherTimer = new DispatcherTimer();
            DispatcherTimer.Tick += new EventHandler(DispatcherTimer_Tick);
            DispatcherTimer.Interval = TimeSpan.FromMilliseconds(2000);
            if (!WaitToClose) DispatcherTimer.Start();

            CloseTimer = new DispatcherTimer();
            CloseTimer.Tick += new EventHandler(CloseTimer_Tick);
            CloseTimer.Interval = TimeSpan.FromMilliseconds(3000);
             if(!WaitToClose) CloseTimer.Start();


        }

        public void CloseTimer_Tick(object sender, EventArgs e)
        {
            this.Close();
            CloseTimer.Stop();
        }



        public void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            DoubleAnimation animation = new DoubleAnimation()
            {
                From = 1.0,
                To = 0.0,
                Duration = new Duration(TimeSpan.FromSeconds(0.5)),
            };
            this.BeginAnimation(Window.OpacityProperty, animation);
            DispatcherTimer.Stop();
        }

        public void SetLocaiton()
        {
            this.Left = Window.Left + Window.Width - this.Width - 10;
            this.Top = Window.Top + Window.Height - this.Height - 10;
        }


        public  void CloseWindow(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            DispatcherTimer.Start();
            CloseTimer.Start();
        }
    }
}
