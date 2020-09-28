using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Jvedio
{
	//http://www.codescratcher.com/wpf/create-image-slideshow-wpf/#DownloadPopup
	public class ImageSlide
	{
		private Image[] ImageControls;
		private DispatcherTimer timerImageChange;
		private List<ImageSource> Images = new List<ImageSource>();
		private static string[] ValidImageExtensions = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif" };
		private string strImagePath = "";
		private int CurrentSourceIndex, CurrentCtrlIndex, IntervalTimer = 2;
		private int MaxViewNum = 10;
		public ImageSlide(string imagepath, Image image1, Image image2)
		{
			strImagePath = imagepath;
			ImageControls = new[] { image1, image2 };

			LoadImageFolder(strImagePath);

			timerImageChange = new DispatcherTimer();
			timerImageChange.Interval = new TimeSpan(0, 0, IntervalTimer);
			timerImageChange.Tick += new EventHandler(timerImageChange_Tick);
		}

		public void Start()
		{
			timerImageChange.Start();
		}

		public void Stop()
		{
			timerImageChange.Stop();
		}


		private void LoadImageFolder(string folder)
		{
			if (!Directory.Exists(folder)) return;
			var sw = System.Diagnostics.Stopwatch.StartNew();
			if (!System.IO.Path.IsPathRooted(folder))
				folder = System.IO.Path.Combine(Environment.CurrentDirectory, folder);
			Random r = new Random();
			var sources = from file in new System.IO.DirectoryInfo(folder).GetFiles().AsParallel().Take(MaxViewNum)
						  where ValidImageExtensions.Contains(file.Extension, StringComparer.InvariantCultureIgnoreCase)
						  orderby r.Next()
						  select CreateImageSource(file.FullName, true);
			Images.Clear();
			Images.AddRange(sources);
			sw.Stop();
		}

		private ImageSource CreateImageSource(string file, bool forcePreLoad)
		{
			if (forcePreLoad)
			{
				var src = new BitmapImage();
				src.BeginInit();
				src.UriSource = new Uri(file, UriKind.Absolute);
				src.CacheOption = BitmapCacheOption.OnLoad;
				src.EndInit();
				src.Freeze();
				return src;
			}
			else
			{
				var src = new BitmapImage(new Uri(file, UriKind.Absolute));
				src.Freeze();
				return src;
			}
		}

		private void timerImageChange_Tick(object sender, EventArgs e)
		{
			PlaySlideShow();
		}

		public void PlaySlideShow()
		{
			try
			{
				if (Images.Count == 0)
					return;
				var oldCtrlIndex = CurrentCtrlIndex;
				CurrentCtrlIndex = (CurrentCtrlIndex + 1) % 2;
				CurrentSourceIndex = (CurrentSourceIndex + 1) % Images.Count;

				Image imgFadeOut = ImageControls[oldCtrlIndex];
				Image imgFadeIn = ImageControls[CurrentCtrlIndex];
				ImageSource newSource = Images[CurrentSourceIndex];
				imgFadeIn.Source = newSource;

				Storyboard StboardFadeOut = new Storyboard();
				DoubleAnimation FadeOutAnimation = new DoubleAnimation()
				{
					To = 0.0,
					Duration = new Duration(TimeSpan.FromSeconds(0.5)),
				};
				Storyboard.SetTargetProperty(FadeOutAnimation, new PropertyPath("Opacity"));
				StboardFadeOut.Children.Add(FadeOutAnimation);
				StboardFadeOut.Begin(imgFadeOut);

				Storyboard StboardFadeIn = new Storyboard();
				DoubleAnimation FadeInAnimation = new DoubleAnimation()
				{
					From = 0.0,
					To = 1.0,
					Duration = new Duration(TimeSpan.FromSeconds(0.25)),
				};
				Storyboard.SetTargetProperty(FadeInAnimation, new PropertyPath("Opacity"));
				StboardFadeIn.Children.Add(FadeInAnimation);
				StboardFadeIn.Begin(imgFadeIn);
			}
			catch  { }
		}


	}
}
