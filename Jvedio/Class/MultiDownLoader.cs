
using Jvedio.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Jvedio.StaticVariable;

namespace Jvedio
{
    public enum DownLoadState
    {
        DownLoading,
        Completed,
        Pause,
        Fail
    }

    public class MultiDownLoader
    {
        public DownLoadState State;
        public event EventHandler InfoUpdate;

        private bool Pause { get; set; }
        private Semaphore Semaphore;
        private Semaphore SemaphoreFC2;
        private bool Cancel { get; set; }

        public List<DownLoadInfo> Movies { get; set; }

        public List<DownLoadInfo> MoviesFC2 { get; set; }

        public MultiDownLoader(List<DownLoadInfo> _Movies, List<DownLoadInfo> _MoviesFC2)
        {
            Cancel = false; Pause = false; 
            Semaphore = new Semaphore(3, 3);
            SemaphoreFC2 = new Semaphore(2, 2);
            Movies = _Movies;
            MoviesFC2 = _MoviesFC2;

        }

        //取消下载

        public void CancelDownload()
        {
            Cancel = true;
            this.State = DownLoadState.Fail;
        }

        public void PauseDownload()
        {
            Pause = true;
            this.State = DownLoadState.Pause;
        }



        public void ContinueDownload()
        {
            Pause = false;
            this.State = DownLoadState.DownLoading;
        }

        //下载
        public void StartThread()
        {
            if (Movies.Count == 0 & MoviesFC2.Count == 0) { this.State = DownLoadState.Completed; return; }
            for (int i = 0; i < Movies.Count; i++)
            {
                Thread threadObject = new Thread(DownLoad);
                threadObject.Start(Movies[i]);
            }

            for (int i = 0; i < MoviesFC2.Count; i++)
            {
                Thread threadObject = new Thread(DownLoad);
                threadObject.Start(MoviesFC2[i]);
            }
        }


        private async void DownLoad(object o)
        {
            DownLoadInfo downLoadInfo = o as DownLoadInfo;
            try
            {
                
                if (downLoadInfo.id.ToUpper().IndexOf("FC2") >= 0) SemaphoreFC2.WaitOne(); else Semaphore.WaitOne();
                if (Cancel) return;
                while (Pause & !Cancel) Task.Delay(300).Wait();


                this.State = DownLoadState.DownLoading;

                DataBase cdb = new DataBase("");
                Movie movie = await cdb.SelectMovieByID(downLoadInfo.id);
                cdb.CloseDB();
                
                string[] url = new string[] { Properties.Settings.Default.Bus, Properties.Settings.Default.BusEurope, Properties.Settings.Default.DB, Properties.Settings.Default.Library };
                bool[] enableurl = new bool[] { Properties.Settings.Default.EnableBus, Properties.Settings.Default.EnableBusEu, Properties.Settings.Default.EnableDB, Properties.Settings.Default.EnableLibrary, Properties.Settings.Default.EnableFC2 };
                string[] cookies = new string[] { Properties.Settings.Default.DBCookie };

                if (movie.title == "" | movie.smallimageurl == "" | movie.bigimageurl == "" | movie.sourceurl == "")
                    await Task.Run(() => { return Net.DownLoadFromNet(movie); });




                cdb = new DataBase("");
                movie = await cdb.SelectMovieByID(downLoadInfo.id);
                cdb.CloseDB();


                //更新总进度
                List<string> extrapicurlList = new List<string>();
                var list = movie.extraimageurl.Split(';');
                foreach (var item in list) if (!string.IsNullOrEmpty(item)) { extrapicurlList.Add(item); }
                
                downLoadInfo.maximum = extrapicurlList.Count;
                downLoadInfo.maximum += 2;

                    //小图
                    await DownLoadSmallPic(movie);
                    downLoadInfo.progress += 1; InfoUpdate?.Invoke(this, new DownloadUpdateEventArgs() { DownLoadInfo = downLoadInfo });//更新进度
                //大图
                    await DownLoadBigPic(movie);
                    downLoadInfo.progress += 1; InfoUpdate?.Invoke(this, new DownloadUpdateEventArgs() { DownLoadInfo = downLoadInfo });//更新进度





                //预览图
                bool dlimageSuccess ; string cookie = "";
                    for (int i = 0; i < extrapicurlList.Count(); i++)
                    {
                    if (!File.Exists(StaticVariable.BasePicPath + $"Extrapic\\{movie.id.ToUpper()}\\{movie.id.ToUpper()}.jpg"))
                    {
                        if (Cancel) return;
                        while (Pause & !Cancel) Task.Delay(300).Wait();
                        if (extrapicurlList[i].Length > 0)
                        {
                            (dlimageSuccess, cookie) = await DownLoadExtraPic(movie.id, extrapicurlList[i], cookie);
                            if (dlimageSuccess) Task.Delay(1000).Wait();
                            
                        }
                    }
                    downLoadInfo.progress += 1; InfoUpdate?.Invoke(this, new DownloadUpdateEventArgs() { DownLoadInfo = downLoadInfo });//更新进度
                }
            }
            finally
            {
                if (downLoadInfo.id.ToUpper().IndexOf("FC2") >= 0)
                    SemaphoreFC2.Release();
                else
                    Semaphore.Release();

            }
        }

        public string GetNfoPath(DetailMovie dm)
        {
            string result = AppDomain.CurrentDomain.BaseDirectory + "DownLoad\\NFO";
            if (!Directory.Exists(result)) Directory.CreateDirectory(result);
            if (Properties.Settings.Default.NFOSaveComBoxIndex == 0)
            {
                if (File.Exists(dm.filepath))
                {
                    FileInfo fileInfo = new FileInfo(dm.filepath);
                    result = fileInfo.DirectoryName;
                }
            }
            return System.IO.Path.Combine(result, dm.id + ".nfo");
        }







        private Task<(bool, string)> DownLoadExtraPic(string id, string url, string cookies)
        {
            string filepath = StaticVariable.BasePicPath + "ExtraPic\\" + id + "\\" + System.IO.Path.GetFileName(new Uri(url).LocalPath);
            if (!File.Exists(filepath))
            {
                return Task.Run(() => {  return Net.DownLoadImage(url,ImageType.ExtraImage, id , Cookie: cookies); });

            }
            else
            {
                return Task.Run(() => { return (false, ""); });
            }
        }




        private Task<(bool, string)> DownLoadSmallPic(Movie dm)
        {
            //不存在才下载
            if (!File.Exists(StaticVariable.BasePicPath + $"SmallPic\\{dm.id}.jpg"))
            {
                
                return Task.Run(() => {
                    return Net.DownLoadImage(dm.smallimageurl,ImageType.SmallImage, dm.id );
                });
            }
            else
            {
                return Task.Run(() => { return (false, ""); });
            }

        }

        private Task<(bool, string)> DownLoadBigPic(Movie dm)
        {
            if (!File.Exists(StaticVariable.BasePicPath + $"BigPic\\{dm.id}.jpg"))
            {
                return Task.Run(() =>
                {

                    return Net.DownLoadImage(dm.bigimageurl, ImageType.BigImage, dm.id);
                });
            }
            else
            {
                return Task.Run(() => { return (false, ""); });
            }
        }
    }

    public class DownloadUpdateEventArgs : EventArgs
    {
        public DownLoadInfo DownLoadInfo;
    }
}
