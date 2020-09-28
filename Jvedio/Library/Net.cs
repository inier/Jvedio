using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using static Jvedio.StaticVariable;


namespace Jvedio
{


    public static class Net
    {
        public static int TCPTIMEOUT = 2;   // TCP 超时
        public static int HTTPTIMEOUT = 2; // HTTP 超时
        public static int ATTEMPTNUM = 2; // 最大尝试次数
        public static string UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/69.0.3497.100 Safari/537.36";

        public static int REQUESTTIMEOUT = 3000;//网站 HTML 获取超时
        public static int FILE_REQUESTTIMEOUT = 5000;//图片下载超时
        public static int READWRITETIMEOUT = 3000;



        public enum HttpMode
        {
            Normal=0,
            RedirectGet=1
        }



        public static async Task<(string, int)> Http(string Url, string Method="GET", HttpMode Mode=HttpMode.Normal,  WebProxy Proxy = null, string Cookie = "")
        {
            string HtmlText = "";
            int StatusCode = 404;
            int num = 0;

            while (num < ATTEMPTNUM & string.IsNullOrEmpty(HtmlText))
            {
                try
                {
                    HtmlText = await Task.Run(() =>
                    {
                        string result = "";
                        HttpWebRequest Request;
                        HttpWebResponse Response=default;
                        try
                        {
                            Request = (HttpWebRequest)HttpWebRequest.Create(Url);
                            if (Cookie != "") Request.Headers.Add("Cookie", Cookie);
                            Request.Accept = "*/*";
                            Request.Timeout = REQUESTTIMEOUT;
                            Request.Method = Method;
                            Request.KeepAlive = false;
                            if (Mode == HttpMode.RedirectGet) Request.AllowAutoRedirect = false;
                            Request.Referer = Url;
                            Request.UserAgent = UserAgent;
                            Request.ReadWriteTimeout = READWRITETIMEOUT;
                            if (Proxy != null) Request.Proxy = Proxy;
                            Response = (HttpWebResponse)Request.GetResponse();



                            if (Response.StatusCode == HttpStatusCode.OK)
                            {
                                var SR = new StreamReader(Response.GetResponseStream());
                                result = SR.ReadToEnd();
                                SR.Close();
                                StatusCode = 200;
                            }else if(Response.StatusCode == HttpStatusCode.Redirect && Mode==HttpMode.RedirectGet)
                            {
                                result = Response.Headers["Location"];// 获得 library 影片 Code
                            }
                            else { num = 2; }
                            Response.Close();
                        }
                        catch (WebException e)
                        {
                            Logger.LogN($"URL={Url},Message-{e.Message}");
                            if (e.Status == WebExceptionStatus.Timeout)
                                num += 1;   
                            else
                                num = 2;   
                        }
                        catch (Exception e)
                        {
                            Logger.LogN($"URL={Url},Message-{e.Message}");
                            num = 2;
                        }
                        finally
                        {
                            if (Response != null) Response.Close();
                        }

                        return result;
                    }).TimeoutAfter(TimeSpan.FromSeconds(HTTPTIMEOUT));

                }
                catch (TimeoutException ex) { Logger.LogN($"URL={Url},Message-{ex.Message}"); num = 2;  }
            }

            return (HtmlText, StatusCode);
        }


        public static async Task<bool> TestUrl(string Url, bool EnableCookie, string Cookie, string Label)
        {
            return await  Task.Run(async ()=> {
                bool result = false;
                string content = ""; int statusCode = 404;
                if (EnableCookie)
                {
                    if (Label == "DB")
                    {
                        (content, statusCode) = await Http(Url + "v/P2Rz9", Proxy: null, Cookie: Cookie);
                        if (content != "")
                        {
                            if (content.IndexOf("FC2-659341") >= 0) result = true;
                            else result = false;
                        }
                    }
                }
                else
                {
                    (content, statusCode) = await Http(Url, Proxy: null);
                    if (content != "") result = true;
                }
                return result;
            });
        }




        private static bool IsDomainAlive(string aDomain, int aTimeoutSeconds)
        {
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    var result = client.BeginConnect(aDomain, 80, null, null);

                    var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(aTimeoutSeconds));

                    if (!success)
                    {
                        return false;
                    }

                    // we have connected
                    client.EndConnect(result);
                    return true;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                Console.WriteLine(e.Message);
                Logger.LogN($"URL={aDomain},Message-{e.Message}");
            }
            return false;
        }
        public static (byte[], string cookies) DownLoadFile(string Url,WebProxy Proxy = null, string Host = "", string SetCookie = "")
        {
            if (Url.IndexOf("fc2club.com") < 0)
                if (!IsDomainAlive(new Uri(Url).Host, TCPTIMEOUT)) { Logger.LogN($"URL={Url},Message-Tcp连接超时"); return (null, ""); }
            Util.SetCertificatePolicy();
            byte[] ImageByte=null;
            string Cookies = SetCookie;
            int num = 0;
            while (num < ATTEMPTNUM && ImageByte == null)
            {
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                HttpWebRequest Request;
                var Response = default(HttpWebResponse);
                try
                {
                    Request = (HttpWebRequest)HttpWebRequest.Create(Url);
                    if (Host != "") Request.Host = Host;
                    if (Proxy != null) Request.Proxy = Proxy;
                    if (SetCookie != "") Request.Headers.Add("Cookie", SetCookie);
                    Request.Accept = "*/*";
                    Request.Timeout = FILE_REQUESTTIMEOUT;
                    Request.Method = "GET";
                    Request.KeepAlive = false;
                    Request.Referer = Url;
                    Request.UserAgent = UserAgent;
                    Request.ReadWriteTimeout = READWRITETIMEOUT;
                    Response = (HttpWebResponse)Request.GetResponse();
                    if (Response.StatusCode== HttpStatusCode.OK)
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            Response.GetResponseStream().CopyTo(ms);
                            ImageByte = ms.ToArray();
                        };
                            //获得 app_uid
                            WebHeaderCollection Headers = Response.Headers;
                            if (Headers != null & SetCookie == "")
                        {
                            if (Headers["Set-Cookie"] != null) Cookies = Headers["Set-Cookie"].Split(';')[0];
                            Console.WriteLine(Cookies);
                        }
                                

                    }
                    else
                    {
                        num = 2;
                    }
                    Response.Close();
                }
                catch (WebException e)
                {
                    Logger.LogN($"URL={Url},Message-{e.Message}");
                    if (e.Status == WebExceptionStatus.Timeout) { num += 1; } else { num = 2; }
                }
                catch (Exception e)
                {
                    Logger.LogE(e);
                    num = 2;
                }
                finally
                {
                    if (Response !=null) Response.Close();
                }
            }
            return (ImageByte, Cookies);
        }

        public static async Task<(bool, string)> DownLoadImage(string Url, ImageType imageType, string ID,  string Cookie = "")
        {
            if (Url.IndexOf('/') < 0) { return (false, ""); }
            bool result = false; string cookies = Cookie;
            byte[] ImageBytes = null;
            (ImageBytes, cookies) = await Task.Run(() =>
            {
                string Host = "";
                if (Url.IndexOf("pics.dmm") >= 0) { Host = "pics.dmm.co.jp"; }
                else if (Url.IndexOf("pics.javcdn.pw") >= 0)
                { Host = "pics.javcdn.pw"; }
                else if (Url.IndexOf("images.javcdn.pw") >= 0) { Host = "images.javcdn.pw"; }

                //if (Url.IndexOf("jdbimgs") >= 0) cookies = AllCookies.DB;

                    //if (imageType == ImageType.ExtraImage)
                    //{
                    (ImageBytes, cookies) = DownLoadFile(Url, Host: Host, SetCookie: cookies);
                //}
                //else { (ImageBytes, cookies) = DownLoadFile(Url, Host: Host, SetCookie: Cookie); }
                return (ImageBytes, cookies);
            });


            if (ImageBytes == null) { result = false; }
            else
            {
                StaticClass.SaveImage(ID, ImageBytes, imageType, Url);
            }
            return (result, cookies);
        }



        public static async Task<bool> DownActress(string ID, string Name)
        {
            bool result = false;
            string Url = RootUrl.Bus + $"star/{ID}";
            string Content; int StatusCode; string ResultMessage;
            (Content, StatusCode) = await Http(Url);
            if (StatusCode == 200 && Content != "")
            {
                //id搜索
                BusParse busParse = new BusParse(ID, Content,VedioType.骑兵);
                Actress actress = busParse.ParseActress();
                if (actress.birthday=="" && actress.age==0 && actress.birthplace=="" ) 
                { Console.WriteLine($"该网址无演员信息：{Url}"); ResultMessage = "该网址无演员信息=>Bus"; Logger.LogN($"URL={Url},Message-{ResultMessage}"); }
                else
                {
                    actress.sourceurl = Url;
                    actress.source = "javbus";
                    actress.id = ID;
                    actress.name = Name;
                    //保存信息
                    DataBase cdb = new DataBase();
                    cdb.InsertActress(actress);
                    cdb.CloseDB();
                    result = true;
                }
            }
            else { Console.WriteLine($"无法访问 404：{Url}"); ResultMessage = "无法访问=>Bus"; Logger.LogN($"URL={Url},Message-{ResultMessage}"); }
            return result;
        }


        public static async Task<(bool,string)> DownLoadFromNet(Movie movie)
        {
            DataBase dataBase;
            Movie newMovie;
            if (movie.vediotype ==(int)VedioType.欧美)
            {
                if(EnableUrl.BusEu) await new BusCrawler(movie.id, (VedioType)movie.vediotype).Crawl();
            }
            else
            {
                if(movie.id.ToUpper().IndexOf("FC2") >= 0)
                {
                    if (EnableUrl.FC2Club) await new Fc2ClubCrawler(movie.id).Crawl();

                    dataBase = new DataBase();
                    newMovie = await dataBase.SelectMovieByID(movie.id);
                    dataBase.CloseDB();

                    if (EnableUrl.DB) if (newMovie.title=="" || newMovie.sourceurl == "") await new DBCrawler(movie.id).Crawl();
                }
                else
                {
                    if (EnableUrl.Bus) await new BusCrawler(movie.id, (VedioType)movie.vediotype).Crawl();

                    dataBase = new DataBase();
                    newMovie = await dataBase.SelectMovieByID(movie.id);
                    dataBase.CloseDB();

                    if (EnableUrl.Library) if (newMovie.title == "" || newMovie.sourceurl == "") await new LibraryCrawler(movie.id).Crawl();

                    dataBase = new DataBase();
                    newMovie = await dataBase.SelectMovieByID(movie.id);
                    dataBase.CloseDB();

                    if (EnableUrl.DB) if (newMovie.title == "" || newMovie.sourceurl == "") await new DBCrawler(movie.id).Crawl();

                }

            }

            dataBase = new DataBase();
            newMovie = await dataBase.SelectMovieByID(movie.id);
            dataBase.CloseDB();

            if (newMovie.title != "")
            {
                return (true, "");
            }
            else
            {
                return (false, "");
            }

        }









    }









    public static class Util
    {
        /// <summary>
        /// Sets the cert policy.
        /// </summary>
        public static void SetCertificatePolicy()
        {
            ServicePointManager.ServerCertificateValidationCallback
                       += RemoteCertificateValidate;
        }

        /// <summary>
        /// Remotes the certificate validate.
        /// </summary>
        private static bool RemoteCertificateValidate(
           object sender, X509Certificate cert,
            X509Chain chain, SslPolicyErrors error)
        {
            // trust any certificate!!!
            //System.Console.WriteLine("Warning, trust any certificate");
            return true;
        }
    }



}
