
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Jvedio.StaticVariable;

namespace Jvedio
{

    public class InfoParse
    {
        protected string HtmlText { get; set; }
        protected string ID { get; set; }
        protected VedioType VedioType { get; set; }

        public  InfoParse(string htmlText, string id="",  VedioType vedioType=VedioType.步兵)
        {
            ID = id;
            HtmlText = htmlText;
            VedioType = vedioType;

        }


        public virtual Dictionary<string,string> Parse()
        {
            return new Dictionary<string, string>();
        }

    }


    public class BusParse : InfoParse
    {
        public BusParse(string id, string htmlText , VedioType vedioType) : base(htmlText, id, vedioType) { }


        public override Dictionary<string, string> Parse()
        {
                Dictionary<string, string> result = new Dictionary<string, string>();
                if (HtmlText == "") { return result; }
                string content; string title; string id = "";

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(HtmlText);

                //基本信息
                HtmlNodeCollection headerNodes = doc.DocumentNode.SelectNodes("//span[@class='header']");
                if (headerNodes != null)
                {
                    foreach (HtmlNode headerNode in headerNodes)
                    {
                        try
                        {
                            title = headerNode.InnerText;
                            switch (title)
                            {
                                case "識別碼:":
                                    id = headerNode.ParentNode.SelectNodes("span")[1].InnerText;
                                    result.Add("id", id.ToUpper());
                                    break;
                                case "發行日期:":
                                    content = headerNode.ParentNode.InnerText;
                                    result.Add("releasedate", Regex.Match(content, "[0-9]{4}-[0-9]{2}-[0-9]{2}").Value);
                                    result.Add("year", Regex.Match(content, "[0-9]{4}").Value);
                                    break;
                                case "長度:":
                                    content = headerNode.ParentNode.InnerText;
                                    result.Add("runtime", Regex.Match(content, "[0-9]+").Value);
                                    break;
                                case "製作商:":
                                    content = headerNode.ParentNode.SelectSingleNode("a").InnerText;
                                    result.Add("studio", content);
                                    break;
                                case "系列:":
                                    content = headerNode.ParentNode.SelectSingleNode("a").InnerText;
                                    result.Add("tag", content);
                                    break;
                                case "導演:":
                                    content = headerNode.ParentNode.SelectSingleNode("a").InnerText;
                                    result.Add("director", content);
                                    break;
                                default:
                                    break;
                            }
                        }
                        catch { continue; }
                    }
                }

                //标题
                HtmlNodeCollection titleNodes = doc.DocumentNode.SelectNodes("//h3");
                if (titleNodes != null)
                {
                    result.Add("title", titleNodes[0].InnerText.Replace(id, "").Substring(1));
                }
                string genre = ""; string actress = ""; string actressid = "";
                HtmlNodeCollection genreNodes = doc.DocumentNode.SelectNodes("//span[@class='genre']/a");
                if (genreNodes != null)
                {
                    foreach (HtmlNode genreNode in genreNodes)
                    {
                        try
                        {
                            if (genreNode.ParentNode.Attributes["onmouseover"] != null)
                            {
                                //演员
                                actress = actress + genreNode.InnerText + " ";
                                string link = genreNode.Attributes["href"]?.Value;

                                actressid = actressid + link.Split('/')[link.Split('/').Count() - 1] + " ";
                            }
                            else
                            {
                                //类别
                                genre = genre + genreNode.InnerText + " ";
                            }
                        }
                        catch { continue; }

                    }
                    if (genre.Length > 0) { result.Add("genre", genre.Substring(0, genre.Length - 1)); }
                    if (actress.Length > 0)
                    {
                        result.Add("actor", actress.Substring(0, actress.Length - 1));
                        result.Add("actorid", actressid.Substring(0, actressid.Length - 1));
                        string url_a = "";
                        foreach (var item in actressid.Split(' '))
                        {
                            try
                            {
                                if (item.IndexOf(" ") < 0 & item.Length > 0)
                                {
                                    if (VedioType == VedioType.骑兵)
                                    {
                                        url_a = url_a + $"https://pics.javcdn.pw/actress/" + item + "_a.jpg;";
                                    }
                                    else if (VedioType == VedioType.步兵)
                                    {
                                        //https://images.javbus.one/actress/41r_a.jpg
                                        url_a = url_a + RootUrl.BusEu.Replace("www", "images") + "actress/" + item + "_a.jpg;";
                                    }
                                    else
                                    {
                                        url_a = url_a + $"https://images.javcdn.pw/actress/" + item + ".jpg;";
                                    }
                                }
                            }
                            catch { continue; }
                        }
                        result.Add("actressimageurl", url_a);
                    }
                }

                //大图
                string movieid = ""; string bigimageurl = "";
                HtmlNodeCollection bigimgeNodes = doc.DocumentNode.SelectNodes("//a[@class='bigImage']");
                if (bigimgeNodes != null)
                {
                    movieid = Regex.Match(bigimgeNodes[0].Attributes["href"].Value, "([a-z]|[0-9])+_b").Value.Replace("_b", "");
                    result.Add("bigimageurl", bigimgeNodes[0].Attributes["href"].Value);
                    bigimageurl = bigimgeNodes[0].Attributes["href"].Value;
                }

                //小图
                if (bigimageurl != "")
                {
                    if (bigimageurl.IndexOf("pics.dmm.co.jp") >= 0)
                    {
                        result.Add("smallimageurl", bigimageurl.Replace("pl.jpg", "ps.jpg"));
                    }
                    else
                    {
                    //Console.WriteLine(VedioType.ToString());
                        if (VedioType == VedioType.骑兵)
                        {
                            result.Add("smallimageurl", "https://pics.javcdn.pw/thumb/" + movieid + ".jpg");//骑兵
                        }
                        else if (VedioType == VedioType.步兵)
                        {
                            result.Add("smallimageurl", "https://images.javcdn.pw/thumbs/" + movieid + ".jpg");//步兵
                        }
                        else if (VedioType == VedioType.欧美)
                        {
                            //https://images.javbus.one/thumb/10jc.jpg
                            result.Add("smallimageurl", RootUrl.BusEu.Replace("www", "images") + "thumbs/" + movieid + ".jpg");//欧美

                        }
                    }
                }






                //预览图
                string url_e = "";
                HtmlNodeCollection extrapicNodes = doc.DocumentNode.SelectNodes("//a[@class='sample-box']");
                if (extrapicNodes != null)
                {
                    foreach (HtmlNode extrapicNode in extrapicNodes)
                    {
                        try
                        {
                            url_e = url_e + extrapicNode.Attributes["href"].Value + ";";
                        }
                        catch { continue; }
                    }
                    result.Add("extraimageurl", url_e);
                }
                return result;
            }


        public Actress ParseActress()
        {
            Actress result = new Actress();
            if (HtmlText == "") { return result; }
            string info;
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(HtmlText);

            //基本信息
            HtmlNodeCollection infoNodes = doc.DocumentNode.SelectNodes("//div[@class='photo-info']/p");
            if (infoNodes != null)
            {
                foreach (HtmlNode infoNode in infoNodes)
                {
                    try
                    {
                        info = infoNode.InnerText;
                        if (info.IndexOf("生日") >= 0)
                        {
                            result.birthday = info.Replace("生日: ", "");
                        }
                        else if (info.IndexOf("年齡") >= 0)
                        {
                            int age = 0;
                            int.TryParse( info.Replace("年齡: ", ""),out age);
                            result.age = age;
                        }
                        else if (info.IndexOf("身高") >= 0)
                        {
                            result.height = Regex.Match(info, @"[0-9]+").Value;
                        }
                        else if (info.IndexOf("罩杯") >= 0)
                        {
                            result.cup = info.Replace("罩杯: ", "");
                        }
                        else if (info.IndexOf("胸圍") >= 0)
                        {
                            result.chest =int.Parse( Regex.Match(info, @"[0-9]+").Value);
                        }
                        else if (info.IndexOf("腰圍") >= 0)
                        {
                            result.waist = int.Parse(Regex.Match(info, @"[0-9]+").Value);
                        }
                        else if (info.IndexOf("臀圍") >= 0)
                        {
                            result.hipline = int.Parse(Regex.Match(info, @"[0-9]+").Value);
                        }
                        else if (info.IndexOf("愛好") >= 0)
                        {
                            result.hobby = info.Replace("愛好: ", "");
                        }
                    }
                    catch { continue; }
                }
            }
            return result;
        }


    }

    public class LibraryParse : InfoParse
    {
        public LibraryParse(string id, string htmlText, VedioType vedioType= 0) : base(htmlText, id, vedioType) { }


        public override Dictionary<string, string> Parse()
        {
                Dictionary<string, string> result = new Dictionary<string, string>();
                if (HtmlText == "") { return result; }
                string content; string id = "";

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(HtmlText);

                HtmlNode titleNode = doc.DocumentNode.SelectSingleNode("//h3[@class='post-title text']/a");
                if (titleNode != null)
                {
                    id = titleNode.InnerText.Split(' ')[0].ToUpper();
                    result.Add("id", id);
                    result.Add("title", titleNode.InnerText.ToUpper().Replace(id, "").Substring(1));
                }

                HtmlNodeCollection infoNodes = doc.DocumentNode.SelectNodes("//div[@id='video_info']/div/table/tr");
                if (infoNodes != null)
                {
                    foreach (HtmlNode infoNode in infoNodes)
                    {
                        try
                        {

                            if (infoNode.InnerText.IndexOf("发行日期") >= 0)
                            {
                                content = infoNode.SelectNodes("td")[1]?.InnerText;
                                result.Add("releasedate", content);
                            }
                            else if (infoNode.InnerText.IndexOf("长度") >= 0)
                            {
                                content = infoNode.SelectSingleNode("td/span")?.InnerText;
                                result.Add("runtime", content);
                            }
                            else if (infoNode.InnerText.IndexOf("导演") >= 0)
                            {
                                content = infoNode.SelectSingleNode("td/span/a")?.InnerText;
                                if (content != null) result.Add("director", content);

                            }
                            else if (infoNode.InnerText.IndexOf("发行商") >= 0)
                            {
                                content = infoNode.SelectSingleNode("td/span/a")?.InnerText;
                                if (content != null) result.Add("studio", content);
                            }

                            else if (infoNode.InnerText.IndexOf("使用者评价:") >= 0)
                            {
                                content = infoNode.SelectSingleNode("td/span")?.InnerText;
                                string rating = Regex.Match(content, @"([0-9]|\.)+").Value;
                                result.Add("rating", Math.Ceiling(double.Parse(rating) * 10).ToString());
                            }
                            else if (infoNode.InnerText.IndexOf("类别") >= 0)
                            {
                                HtmlNodeCollection genreNodes = infoNode.SelectNodes("td/span/a");
                                if (genreNodes != null)
                                {
                                    string genre = "";
                                    foreach (HtmlNode genreNode in genreNodes)
                                    {
                                        genre = genre + genreNode.InnerText + " ";
                                    }
                                    result.Add("genre", genre);
                                }

                            }
                            else if (infoNode.InnerText.IndexOf("演员") >= 0)
                            {
                                HtmlNodeCollection actressNodes = infoNode.SelectNodes("td/span/span/a");
                                if (actressNodes != null)
                                {
                                    string actress = "";
                                    foreach (HtmlNode actressNode in actressNodes)
                                    {
                                        actress = actress + actressNode.InnerText + " ";
                                    }
                                    result.Add("actor", actress);
                                }

                            }
                        }
                        catch (NullReferenceException ex) { continue; }
                    }
                }

                //大图：library没有小图，大小图一致
                HtmlNode bigimageNode = doc.DocumentNode.SelectSingleNode("//img[@id='video_jacket_img']");
                if (bigimageNode != null) { result.Add("bigimageurl", "http:" + bigimageNode.Attributes["src"].Value); result.Add("smallimageurl", result["bigimageurl"].Replace("pl.jpg", "ps.jpg")); }



                //预览图
                HtmlNodeCollection extrapicNodes = doc.DocumentNode.SelectNodes("//div[@class='previewthumbs']/img");
                if (extrapicNodes != null)
                {
                    string extraimage = "";
                    foreach (HtmlNode extrapicNode in extrapicNodes)
                    {
                        try { extraimage = extraimage + "http:" + extrapicNode.Attributes["src"].Value + ";"; }
                        catch { continue; }

                    }
                    result.Add("extraimageurl", extraimage);
                }

                return result;
        }

    }

    public class JavDBParse : InfoParse
    {
        protected string MovieCode { get; set; }

        public JavDBParse(string id, string htmlText , string movieCode):base(htmlText)  {
            ID = id;
            HtmlText = htmlText;
            MovieCode = movieCode;
        }





        public override Dictionary<string, string> Parse()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            if (HtmlText == "") { return result; }
            string content; string id = "";

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(HtmlText);

            HtmlNode titleNode = doc.DocumentNode.SelectSingleNode("//h2[@class='title is-4']/strong");
            if (titleNode != null)
            {
                id = titleNode.InnerText.Split(' ')[0];
                result.Add("id", id);
                result.Add("title", titleNode.InnerText.Replace(id, " ").Substring(1));
            }

            HtmlNodeCollection infoNodes = doc.DocumentNode.SelectNodes("//nav[@class='panel video-panel-info']/div");
            if (infoNodes != null)
            {
                foreach (HtmlNode infoNode in infoNodes)
                {
                    try
                    {
                        if (infoNode.InnerText.IndexOf("時間") >= 0)
                        {
                            content = infoNode.SelectSingleNode("span").InnerText;
                            if (content != "N/A") { result.Add("releasedate", content); }
                        }
                        else if (infoNode.InnerText.IndexOf("時長") >= 0)
                        {
                            content = infoNode.SelectSingleNode("span").InnerText;
                            if (content != "N/A") { result.Add("runtime", Regex.Match(content, "[0-9]+").Value); }
                        }
                        else if (infoNode.InnerText.IndexOf("賣家") >= 0)
                        {
                            content = infoNode.SelectSingleNode("span/a").InnerText;
                            if (content != "N/A") { result.Add("director", content); }
                        }
                        else if (infoNode.InnerText.IndexOf("評分") >= 0)
                        {
                            content = infoNode.SelectSingleNode("span").InnerText;
                            if (content != "N/A")
                            {
                                string rating = Regex.Match(content, @"([0-9]|\.)+分").Value.Replace("分", "");
                                result.Add("rating", Math.Ceiling(double.Parse(rating) * 20).ToString());
                            }
                        }
                        else if (infoNode.InnerText.IndexOf("類別") >= 0)
                        {
                            HtmlNodeCollection genreNodes = infoNode.SelectNodes("span/a");
                            if (genreNodes != null)
                            {
                                string genre = "";
                                foreach (HtmlNode genreNode in genreNodes)
                                {
                                    genre = genre + genreNode.InnerText + " ";
                                }
                                result.Add("genre", genre);
                            }

                        }
                        else if (infoNode.InnerText.IndexOf("片商") >= 0)
                        {
                            content = infoNode.SelectSingleNode("span/a").InnerText;
                            if (content != "N/A") { result.Add("studio", content); }
                        }
                        else if (infoNode.InnerText.IndexOf("系列") >= 0)
                        {
                            content = infoNode.SelectSingleNode("span/a").InnerText;
                            if (content != "N/A") { result.Add("tag", content); }
                        }
                        else if (infoNode.InnerText.IndexOf("演員") >= 0)
                        {
                            HtmlNodeCollection actressNodes = infoNode.SelectNodes("span/a");
                            if (actressNodes != null)
                            {
                                string actress = "";
                                foreach (HtmlNode actressNode in actressNodes)
                                {
                                    actress = actress + actressNode.InnerText + " ";
                                }
                                result.Add("actor", actress);
                            }

                        }
                    }
                    catch { continue; }
                }

            }
            //大小图
            HtmlNode bigimageNode = doc.DocumentNode.SelectSingleNode("//img[@class='video-cover']");
            if (bigimageNode != null) { result.Add("bigimageurl", bigimageNode.Attributes["src"].Value); }

            string smallimageurl = "https://jdbimgs.com/thumbs/" + MovieCode.ToLower().Substring(0, 2) + "/" + MovieCode + ".jpg";
            result.Add("smallimageurl", smallimageurl);

            //预览图
            HtmlNodeCollection extrapicNodes = doc.DocumentNode.SelectNodes("//a[@class='tile-item']");
            if (extrapicNodes != null)
            {
                string extraimage = "";
                foreach (HtmlNode extrapicNode in extrapicNodes)
                {
                    try
                    {
                        if (extrapicNode.Attributes["href"].Value.IndexOf("/v/") < 0) { extraimage = extraimage + extrapicNode.Attributes["href"].Value + ";"; }
                    }
                    catch { continue; }
                }
                result.Add("extraimageurl", extraimage);
            }

            return result;
        }





    }

    public class Fc2ClubParse : InfoParse
    {
        public Fc2ClubParse(string id, string htmlText, VedioType vedioType= 0) : base(htmlText, id, vedioType) { }


        public override Dictionary<string, string> Parse()
        {

            Dictionary<string, string> result = new Dictionary<string, string>();
            if (HtmlText == "") { return result; }
            string content; string title; string id = "";

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(HtmlText);

            //基本信息
            HtmlNodeCollection headerNodes = doc.DocumentNode.SelectNodes("//h5");
            if (headerNodes != null)
            {
                foreach (HtmlNode headerNode in headerNodes)
                {
                    try
                    {
                        title = headerNode.InnerText;
                        //Console.WriteLine(title);
                        if (title.IndexOf("影片评分") >= 0)
                        {
                            content = title;
                            result.Add("rating", Regex.Match(content, "[0-9]+").Value);
                        }
                        else if (title.IndexOf("资源参数") >= 0)
                        {
                            content = title;
                            if (content.IndexOf("无码") > 0) { result.Add("vediotype", "1"); }
                            else { result.Add("vediotype", "2"); }
                            result.Add("otherinfo", content.Replace("资源参数：", "FC2资源参数：").Replace(" ", "").Replace("\n", "").Replace("\r", "") + " ");
                        }
                        else if (title.IndexOf("卖家信息") >= 0)
                        {
                            content = headerNode.SelectSingleNode("a").InnerText;
                            result.Add("director", content.Replace("\n", "").Replace("\r", ""));
                            result.Add("studio", content.Replace("\n", "").Replace("\r", ""));
                        }
                        else if (title.IndexOf("影片标签") >= 0)
                        {
                            HtmlNodeCollection genreNodes = headerNode.SelectNodes("a");
                            if (genreNodes != null)
                            {
                                string genre = "";
                                foreach (HtmlNode genreNode in genreNodes)
                                {
                                    genre = genre + genreNode.InnerText + " ";
                                }
                                if (genre.Length > 0) { result.Add("genre", genre.Substring(0, genre.Length - 1)); }
                            }

                        }
                        else if (title.IndexOf("女优名字") >= 0)
                        {
                            content = title;
                            result.Add("actor", content.Replace("女优名字：", "").Replace("\n", "").Replace("\r", "").Replace("/"," "));
                        }
                    }
                    catch { continue; }
                }
            }

            //标题
            HtmlNodeCollection titleNodes = doc.DocumentNode.SelectNodes("//h3");
            if (titleNodes != null)
            {
                foreach (HtmlNode titleNode in titleNodes)
                {
                    try
                    {
                        if (titleNode.InnerText.IndexOf("FC2优质资源推荐") < 0)
                        {
                            id = titleNode.InnerText.Split(' ')[0];
                            result.Add("id", id);
                            result.Add("title", titleNode.InnerText.Replace(id, "").Substring(1).Replace("\n", "").Replace("\r", ""));
                            break;
                        }
                    }
                    catch { continue; }
                }

            }

            //预览图
            string url_e = "";
            HtmlNodeCollection extrapicNodes = doc.DocumentNode.SelectNodes("//ul[@class='slides']/li/img");
            if (extrapicNodes != null)
            {
                foreach (HtmlNode extrapicNode in extrapicNodes)
                {
                    try
                    {
                        url_e = url_e + "https://fc2club.com" + extrapicNode.Attributes["src"].Value + ";";
                    }
                    catch { continue; }
                }
                result.Add("extraimageurl", url_e);
            }

            //大图小图
            if (url_e.IndexOf(';') > 0)
            {
                result.Add("bigimageurl", url_e.Split(';')[0]);
                result.Add("smallimageurl", url_e.Split(';')[0]);
            }

            //发行日期和发行年份
            if (url_e.IndexOf(";") > 0)
            {
                // / uploadfile / 2018 / 1213 / 20181213104511782.jpg
                string url = url_e.Split(';')[0];
                string datestring = Regex.Match(url, "[0-9]{4}/[0-9]{4}").Value;

                result.Add("releasedate", datestring.Substring(0, 4) + "-" + datestring.Substring(5, 2) + "-" + datestring.Substring(7, 2));
                result.Add("year", datestring.Substring(0, 4));
            }

            return result;
        }

    }
}
