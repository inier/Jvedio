
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Jvedio.Net;
using static Jvedio.StaticVariable;
using static Jvedio.StaticClass;
using System.IO;
namespace Jvedio
{
    public class Crawler 
    {
        public static int TASKDELAY_SHORT = 300;//短时间暂停
        public static int TASKDELAY_MEDIUM = 1000;//长时间暂停
        public static int TASKDELAY_LONG = 2000;//长时间暂停


        protected bool result = false;
        protected string resultMessage = "";
        protected string Url = "";
        protected string Content = "";
        protected int StatusCode=404;
        protected VedioType VedioType;
        protected string MovieCode;
        protected string Cookies = "";

        protected WebSite webSite;

        public string ID { get; set; }

        public Crawler(string Id)
        {
            ID = Id;
        }

        public static void SaveInfo(Dictionary<string, string> Info,WebSite webSite)
        {
            string id = "";
            try { id = Info["id"].ToUpper(); }
            catch { return; }
            if (id == "") return;
            //保存信息
            DataBase.UpdateInfoFromNet(Info, webSite);
            DetailMovie detailMovie = DataBase.SelectDetailMovieById(id);
            

            //nfo 信息保存到视频同目录
            if (Properties.Settings.Default.SaveInfoToNFO)
            {
                if (Directory.Exists(Properties.Settings.Default.NFOSavePath))
                {
                    //固定位置
                    SaveToNFO(detailMovie, Path.Combine(Properties.Settings.Default.NFOSavePath, $"{id}.nfo"));
                }
                else
                {
                    //与视频同路径
                    string path = detailMovie.filepath;
                    if (System.IO.File.Exists(path))
                    {
                        SaveToNFO(detailMovie, Path.Combine(new FileInfo(path).DirectoryName, $"{id}.nfo"));
                    }
                }
            }




        }


        protected virtual async Task<string> GetMovieCode()
        {
            return await Task.Run(() => { return ""; });
        }

        public virtual async Task<bool> Crawl()
        {
            (Content, StatusCode) = await Net.Http(Url,Cookie: Cookies);
            if (StatusCode == 200 & Content != "") { SaveInfo(GetInfo(), webSite); return true; }
            else { resultMessage = "Get html Fail"; Logger.LogN($"URL={Url},Message-{resultMessage}"); return false; }
        }

        protected virtual  Dictionary<string,string> GetInfo()
        {
            return new Dictionary<string, string>();
        }
    }


    public class BusCrawler : Crawler
    {

        public BusCrawler(string Id,VedioType vedioType) :base(Id) {
            VedioType = vedioType;
            if (vedioType == VedioType.欧美) { Url = RootUrl.BusEu + ID.Replace(".", "-"); webSite = WebSite.BusEu; }
            else { Url = RootUrl.Bus + ID.ToUpper(); webSite = WebSite.Bus; }
            
            }


        protected override Dictionary<string, string> GetInfo()
        {
            Dictionary<string, string> Info = new BusParse(ID, Content, VedioType).Parse();
            if (Info.Count <= 0) { Console.WriteLine($"解析失败：{Url}"); resultMessage = "Parse Fail=>Bus"; Logger.LogN($"URL={Url},Message-{resultMessage}"); }
            else
            {
                Info.Add("sourceurl", Url);
                Info.Add("source", "javbus");
                Task.Delay(TASKDELAY_SHORT).Wait();
            }
            return Info;
        }

    }


    public class Fc2ClubCrawler : Crawler
    {
        public Fc2ClubCrawler(string Id) : base(Id) { Url = "https://fc2club.com/html/" + ID + ".html"; webSite = WebSite.FC2Club; }


        protected override Dictionary<string, string> GetInfo()
        {
            Dictionary<string, string> Info = new Dictionary<string, string>();
            Info = new Fc2ClubParse(ID, Content).Parse();
            if (Info.Count <= 0) { Console.WriteLine($"解析失败：{Url}"); resultMessage = "Parse Fail=>FC2"; Logger.LogN($"URL={Url},Message-{resultMessage}"); }
            else
            {
                Info.Add("sourceurl", Url);
                Info.Add("source", "fc2club");
                Task.Delay(TASKDELAY_MEDIUM).Wait();
            }
            return Info;

        }

    }


    public class DBCrawler : Crawler
    {
        public DBCrawler(string Id) : base(Id) { 
            Url = RootUrl.DB + $"search?q={ID}&f=all";
            Cookies = AllCookies.DB;
            webSite = WebSite.DB;
        }

        protected override async Task<string> GetMovieCode()
        {
            string result = "";
            //先从数据库获取
            result = DataBase.SelectInfoByID("code", "javdb", ID);
            if (result == "")
            {
                //从网络获取
                string content; int statusCode;
                (content, statusCode) = await Net.Http(Url, Cookie: Cookies);

                if (statusCode == 200 & content != "")
                    result = GetMovieCodeFromSearchResult(content);
            }

            //存入数据库
            if (result != "") { DataBase.SaveMovieCodeByID(ID, "javdb", result); }
            
            return result;
        }

        protected string GetMovieCodeFromSearchResult(string content)
        {
            string result = "";
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(content);

            HtmlNodeCollection gridNodes = doc.DocumentNode.SelectNodes("//div[@class='grid columns']/div/a/div[@class='uid']");
            if (gridNodes != null)
            {
                foreach (HtmlNode gridNode in gridNodes)
                {
                    if (gridNode.InnerText.ToUpper() == ID.ToUpper())
                    {
                        result = gridNode.ParentNode.Attributes["href"].Value.Replace("/v/", "");
                        break;
                    }
                }
            }
            return result;
        }



        public override async Task<bool> Crawl()
        {
            MovieCode=await GetMovieCode();
            if (MovieCode != "")
            {
                //解析
                Url = RootUrl.DB + $"v/{MovieCode}";
                return await base.Crawl();
            }
            else
            {
                return false;
            }
        }


        protected override Dictionary<string, string> GetInfo()
        {
            Dictionary<string, string> Info = new Dictionary<string, string>();
            Info = new JavDBParse(ID,Content, MovieCode).Parse();
            if (Info.Count <= 0) { Console.WriteLine($"解析失败：{Url}"); resultMessage = "Parse Fail=>DB"; Logger.LogN($"URL={Url},Message-{resultMessage}"); }
            else
            {
                Info.Add("sourceurl", Url);
                Info.Add("source", "javdb");
                Task.Delay(TASKDELAY_MEDIUM).Wait();
            }
            return Info;

        }

    }

    public class LibraryCrawler : Crawler
    {
        public LibraryCrawler(string Id) : base(Id)
        {
            Url = RootUrl.Library + $"vl_searchbyid.php?keyword={ID}";
            webSite = WebSite.Library;
        }

        protected override async Task<string> GetMovieCode()
        {
            string result;
            //先从数据库获取
            result = DataBase.SelectInfoByID("code", "library", ID);
            if (result == "" || result.IndexOf("zh-cn")>=0)
            {
                Console.WriteLine(Url);
                //从网络获取
                string Location; int StatusCode;
                (Location, StatusCode) = await Net.Http(Url, Mode: HttpMode.RedirectGet);

                if (Location.IndexOf("=") >= 0) result = Location.Split('=')[1];
            }

            //存入数据库
            if (result != "" && result.IndexOf("zh-cn") < 0) { DataBase.SaveMovieCodeByID(ID, "library", result);  } else { result = ""; }
            
            return result;
        }


        public override async Task<bool> Crawl()
        {
            MovieCode = await GetMovieCode();
            if (MovieCode != "")
            {
                //解析
                Url = RootUrl.Library + $"?v={MovieCode}";
                return await base.Crawl();
            }
            else
            {
                return false;
            }
        }


        protected override Dictionary<string, string> GetInfo()
        {
            Dictionary<string, string> Info = new Dictionary<string, string>();
            Info = new LibraryParse(ID, Content).Parse();
            if (Info.Count <= 0) { Console.WriteLine($"解析失败：{Url}"); resultMessage = "Parse Fail=>Library"; Logger.LogN($"URL={Url},Message-{resultMessage}"); }
            else
            {
                Info.Add("sourceurl", Url);
                Info.Add("source", "javlibrary");
                Task.Delay(TASKDELAY_MEDIUM).Wait();
            }
            return Info;
        }

    }



    public class Jav321Crawler : Crawler
    {
        public Jav321Crawler(string Id) : base(Id)
        {
            Url = RootUrl.Jav321 + $"video/{ID.ToJav321()}";
            webSite = WebSite.Jav321;
        }



        public override async Task<bool> Crawl()
        {
            (Content, StatusCode) = await Net.Http(Url, Cookie: Cookies);
            if (StatusCode == 200 & Content != "") {


                Dictionary<string, string> Info = GetInfo();




            }
            



            if (MovieCode != "")
            {
                //解析
                Url = RootUrl.Library + $"?v={MovieCode}";
                return await base.Crawl();
            }
            else
            {
                return false;
            }
        }


        protected override Dictionary<string, string> GetInfo()
        {
            Dictionary<string, string> Info = new Dictionary<string, string>();
            Info = new Jav321Parse(ID, Content).Parse();
            if (Info.Count <= 0) { Console.WriteLine($"解析失败：{Url}"); resultMessage = "Parse Fail=>Library"; Logger.LogN($"URL={Url},Message-{resultMessage}"); }
            else
            {
                Info.Add("sourceurl", Url);
                Info.Add("source", "jav321");
                Task.Delay(TASKDELAY_MEDIUM).Wait();
            }
            return Info;
        }

    }
}
