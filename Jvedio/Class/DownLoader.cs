using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using static Jvedio.StaticVariable;

namespace Jvedio
{
    public class DownLoader
    {
        public event EventHandler InfoUpdate;
        private Semaphore Semaphore;
        private Semaphore SemaphoreFC2;
        public DownLoadState State ;
        private bool Cancel { get; set; }



        public List<Movie> Movies { get; set; }


        public List<Movie> MoviesFC2 { get; set; }


        public DownLoader(List<Movie> _movies, List<Movie> _moviesFC2)
        {
            Movies = _movies;
            MoviesFC2 = _moviesFC2;
            Semaphore = new Semaphore(3, 3);
            SemaphoreFC2 = new Semaphore(2, 2);
            downLoadProgress = new DownLoadProgress() { lockobject = new object(), value = 0, maximum = Movies.Count+ MoviesFC2.Count };
        }

        public DownLoadProgress downLoadProgress;



        //取消下载

        public  void  CancelDownload()
        {
            Cancel = true;
            State = DownLoadState.Fail;
        }

        public void StartThread()
        {
            if (Movies.Count == 0 & MoviesFC2.Count==0) { this.State = DownLoadState.Completed; return; }
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

            Console.WriteLine($"启动了{Movies.Count+ MoviesFC2.Count}个线程");

        }


        private async void DownLoad(object o)
        {

            //下载信息=>下载图片
            Movie movie = o as Movie;
            try
            { 
                if(movie.id.ToUpper().IndexOf("FC2")>=0 ) SemaphoreFC2.WaitOne(); else Semaphore.WaitOne();
                if (Cancel | movie.id == "") return;
                bool success; string resultMessage;
                //下载信息
                State = DownLoadState.DownLoading;
                if (movie.title == "" | movie.smallimageurl == "" | movie.bigimageurl == ""  | movie.sourceurl=="")
                {
                    (success, resultMessage) = await Task.Run(() => { return Net.DownLoadFromNet(movie); });
                    if (success) InfoUpdate?.Invoke(this, new InfoUpdateEventArgs() { Movie = movie, progress = downLoadProgress.value });
                }


                DetailMovie dm = new DetailMovie(); DataBase cdb = new DataBase("");
                dm = cdb.SelectDetailMovieById(movie.id); cdb.CloseDB();
                //下载小图
                await DownLoadSmallPic(dm);
                dm.smallimage = StaticClass.GetBitmapImage(dm.id, "SmallPic");
                InfoUpdate?.Invoke(this, new InfoUpdateEventArgs() { Movie = dm, progress = downLoadProgress.value, state = State });

                
                if (dm.sourceurl?.IndexOf("fc2club") >= 0)
                {
                    //复制大图
                    if (File.Exists(StaticVariable.BasePicPath + $"SmallPic\\{dm.id}.jpg") & !File.Exists(StaticVariable.BasePicPath + $"BigPic\\{dm.id}.jpg"))
                    {
                        File.Copy(StaticVariable.BasePicPath + $"SmallPic\\{dm.id}.jpg", StaticVariable.BasePicPath + $"BigPic\\{dm.id}.jpg");
                    }
                }
                else
                {
                    //下载大图
                    await DownLoadBigPic(dm);
                }
                dm.bigimage = StaticClass.GetBitmapImage(dm.id, "BigPic");
                lock (downLoadProgress.lockobject) downLoadProgress.value += 1;
                InfoUpdate?.Invoke(this, new InfoUpdateEventArgs() { Movie = dm, progress = downLoadProgress.value, state = State });

                Task.Delay(1000).Wait();
            }
            catch(Exception e)
            {
                Logger.LogE(e);
            }
            finally
            {
                if (movie.id.ToUpper().IndexOf("FC2") >= 0)
                    SemaphoreFC2.Release();
                else
                    Semaphore.Release();
            
            

            }
        }


        private async Task<(bool, string)> DownLoadSmallPic(DetailMovie dm)
        {
            //不存在才下载
            if (!File.Exists(StaticVariable.BasePicPath + $"SmallPic\\{dm.id}.jpg"))
            {
                Console.WriteLine("开始下载小图");
                Console.WriteLine(dm.source);
                if (dm.source == "javdb") return (false, "");
                else return await Net.DownLoadImage(dm.smallimageurl, ImageType.SmallImage, dm.id);
            }
            else return  (false, ""); 

        }


        private async Task<(bool, string)> DownLoadBigPic(DetailMovie dm)
        {
            if (!File.Exists(StaticVariable.BasePicPath + $"BigPic\\{dm.id}.jpg"))
            {
                return await Net.DownLoadImage(dm.bigimageurl, ImageType.BigImage, dm.id);
            }
            else
            {
                return (false, "");
            }
        }
    }

    public class InfoUpdateEventArgs : EventArgs
    {
        public Movie  Movie;
        public double progress = 0;
        public DownLoadState state;
    }

}
