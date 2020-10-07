using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using QueryEngine;
using System.Collections.Specialized;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading;
using System.Security.Permissions;
using static Jvedio.StaticVariable;
using static Jvedio.StaticClass;

namespace Jvedio
{
    public static class Scan
    {
        public static double MinFileSize = Properties.Settings.Default.ScanMinFileSize * 1024 * 1024;

        public static List<string> SearchPattern = new List<string>();


        public static void InitSearchPattern()
        {
            //视频后缀来自 Everything (位置：搜索-管理筛选器-视频-编辑)
            string SearchPatternPath = AppDomain.CurrentDomain.BaseDirectory + @"\Data\SearchPattern.txt";
            if (File.Exists(SearchPatternPath))
            {
                StreamReader sr = new StreamReader(SearchPatternPath);
                string ScanVetioType = sr.ReadToEnd().Replace("，", ",");
                sr.Close();
                foreach (var item in ScanVetioType.Split(','))
                {
                    if (!SearchPattern.Contains("." + item)) { SearchPattern.Add("." + item); }
                }
                    
            }

            MinFileSize = Properties.Settings.Default.ScanMinFileSize * 1024 * 1024;
            //如果文件视频类型为空，则使用默认的视频类型
            if (SearchPattern.Count==0) {
                string ScanVetioType = Resource_String.ScanVetioType;
                foreach (var item in ScanVetioType.Split(','))
                    SearchPattern.Add("." + item);
            }
        }




        public static bool IsProperMovie(string FilePath)
        {
            bool result = false;
            if (!File.Exists(FilePath)) { return false; }
            if (SearchPattern.Contains(System.IO.Path.GetExtension(FilePath).ToLower()))
            {
                if (new System.IO.FileInfo(FilePath).Length >= MinFileSize) { result = true; }
            }
            return result;
        }




        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public static List<string> ScanAllDrives()
        {
            List<string> result = new List<string>();
            try
            {
                var entries = Engine.GetAllFilesAndDirectories();
                entries.ForEach(arg => {
                    if (arg is FileAndDirectoryEntry & !arg.IsFolder)
                    {
                        result.Add(arg.FullFileName);
                    }
                });
            }
            catch (Exception e)
            {
                Logger.LogE(e);
            }

            //扫描根目录
            StringCollection stringCollection = new StringCollection();
            foreach (var item in Environment.GetLogicalDrives())
            {
                try
                {
                    if (Directory.Exists(item)) { stringCollection.Add(item); }
                }
                catch (Exception e)
                {
                    Logger.LogE(e);
                    continue;
                }

            }
            result.AddRange(ScanTopPaths(stringCollection));

           
            return FirstFilter(result);
        }


        //根据 视频后缀文件大小筛选
        public static List<string> FirstFilter(List<string> FilePathList, string ID = "")
        {
            if (ID == "")
            {
                return FilePathList
                    .Where(s => SearchPattern.Contains(Path.GetExtension(s).ToLower()))
                    .Where(s => !File.Exists(s) || new FileInfo(s).Length >= MinFileSize).OrderBy(s => s).ToList();
            }
            else
            {
                return FilePathList
                    .Where(s => SearchPattern.Contains(Path.GetExtension(s).ToLower()))
                    .Where(s => !File.Exists(s) || new FileInfo(s).Length >= MinFileSize)
                    .Where(s => Identify.GetFanhao(new FileInfo(s).Name).ToUpper() == ID.ToUpper())
                    .OrderBy(s => s).ToList();
            }
        }

        public static List<string> ScanTopPaths(StringCollection stringCollection)
        {
            List<string> result = new List<string>();
            foreach (var item in stringCollection)
            {
                try
                {
                    foreach (var path in Directory.GetFiles(item, "*.*", SearchOption.TopDirectoryOnly)) result.Add(path);
                }
                catch { continue; }
            }
            return result;
        }

        public static List<string> GetSubSectionFeature()
        {
            List<string> result = new List<string>();
            string SubSectionFeature = Resource_String.SubSectionFeature;
            string SplitFeature = SubSectionFeature.Replace("，", ",");
            if (SplitFeature.Split(',').Count() > 0) { foreach (var item in SplitFeature.Split(',')) { if (!result.Contains(item)) { result.Add(item); } } }
            return result;
        }


        /// <summary>
        /// 给出一组视频路径，判断是否是分段视频
        /// </summary>
        /// <param name="FilePathList"></param>
        /// <returns></returns>

        public static bool IsSubSection(List<string> FilePathList)
        {
            bool result = true;
            string FatherPath = new FileInfo(FilePathList[0]).Directory.FullName;

            //只要目录不同即不为分段视频
            for (int i = 0; i < FilePathList.Count; i++)
            {
                if (new FileInfo(FilePathList[i]).Directory.FullName != FatherPath) { return false; }
            }

            //目录都相同，判断是否分段视频的特征


            // -1  cd1  _1   fhd1  
            string regexFeature = "";
            foreach (var item in GetSubSectionFeature()) { regexFeature += item + "|"; }
            regexFeature = "(" + regexFeature.Substring(0, regexFeature.Length - 1) + ")[1-9]{1}";


            string MatchesName = "";
            foreach (var item in FilePathList)
            {
                foreach (var re in Regex.Matches(item, regexFeature)) { MatchesName += re.ToString(); }
            }

            for (int i = 1; i <= FilePathList.Count; i++) { result &= MatchesName.IndexOf(i.ToString()) >= 0; }


            if (!result)
            {
                result = true;
                //数字后面存在 A,B,C……
                //XXX-000-A  
                //XXX-000-B
                regexFeature = "";
                foreach (var item in GetSubSectionFeature()) { regexFeature += item + "|"; }
                regexFeature = "((" + regexFeature.Substring(0, regexFeature.Length - 1) + ")|[0-9]{1,})[a-n]{1}";

                MatchesName = "";
                foreach (var item in FilePathList)
                {
                    foreach (var re in Regex.Matches(item, regexFeature, RegexOptions.IgnoreCase)) { MatchesName += re.ToString(); }
                }
                MatchesName = MatchesName.ToLower();
                string characters = "abcdefghijklmn";
                for (int i = 0; i < Math.Min( FilePathList.Count,characters.Length); i++) {  result &= MatchesName.IndexOf(characters[i]) >= 0;  }
            }



            return result;

        }






        public static List<string> ScanPaths(StringCollection stringCollection, CancellationToken cancellationToken)
        {
            List<string> result = new List<string>();
            foreach (var item in stringCollection) { result.AddRange(GetAllFilesFromFolder(item, cancellationToken)); }
            var result2 = result
                .Where(s => SearchPattern.Contains(System.IO.Path.GetExtension(s).ToLower()))
                .Where(s => File.Exists(s) ? new System.IO.FileInfo(s).Length >= MinFileSize : 1 > 0).OrderBy(s => s).ToList();
            return result2;
        }

        public static List<string> ScanNFO(StringCollection stringCollection, CancellationToken cancellationToken, Action<string> callBack)
        {
            List<string> result = new List<string>();
            foreach (var item in stringCollection) { result.AddRange(GetAllFilesFromFolder(item, cancellationToken, "*.*", callBack)); }
            return result.Where(s => Path.GetExtension(s).ToLower().IndexOf("nfo") > 0).ToList();
        }




        public static  List<string> GetAllFilesFromFolder(string root, CancellationToken cancellationToken , string pattern = "", Action<string> callBack = null)
        {
            Queue<string> folders = new Queue<string>();
            List<string> files = new List<string>();
            folders.Enqueue(root);
            while (folders.Count != 0)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
                catch (OperationCanceledException e)
                {
                    Logger.LogE(e);
                    break;
                }

                string currentFolder = folders.Dequeue();
                //Console.WriteLine($"扫描中{currentFolder}");
                try
                {
                    string[] filesInCurrent = System.IO.Directory.GetFiles(currentFolder, pattern == "" ? "*.*" : pattern, System.IO.SearchOption.TopDirectoryOnly);
                    files.AddRange(filesInCurrent);
                    foreach(var file in filesInCurrent) {if(callBack!=null) callBack(file); }
                }
                catch
                {
                }
                try
                {
                    string[] foldersInCurrent = System.IO.Directory.GetDirectories(currentFolder, pattern == "" ? "*.*" : pattern, System.IO.SearchOption.TopDirectoryOnly);
                    foreach (string _current in foldersInCurrent)
                    {
                        folders.Enqueue(_current);
                        if (callBack != null) callBack(_current); 
                    }
                }
                catch
                {
                }
            }
            return files;
        }



        /// <summary>
        /// 分类视频并导入
        /// </summary>
        /// <param name="MoviePaths"></param>
        /// <param name="ct"></param>
        /// <param name="IsEurope"></param>
        /// <returns></returns>
        public static double DistinctMovieAndInsert(List<string> MoviePaths , CancellationToken ct, bool IsEurope = false)
        {
            Logger.LogScanInfo("\n-----【" + DateTime.Now.ToString() + "】-----");
            Logger.LogScanInfo($"\n扫描出 => {MoviePaths.Count}  个 \n");

            DataBase cdb = new DataBase();

            //检查未识别出番号的视频
            List<string> r1 = new List<string>();
            string c1 = "";
            string id = "";
            VedioType  vt = 0;
            double totalinsertnum = 0;
            double unidentifynum = 0;
            foreach (var item in MoviePaths)
            {
                if (File.Exists(item))
                {
                    
                    id = IsEurope ? Identify.GetEuFanhao(new FileInfo(item).Name) : Identify.GetFanhao(new FileInfo(item).Name);
                    //Console.WriteLine($"{id}=>{item}");
                    if (IsEurope) { if (string.IsNullOrEmpty(id)) vt = 0; else vt = VedioType.欧美; }
                    else
                    {
                        vt = Identify.GetVedioType(id);
                    }

                    if (vt != 0)
                    {
                        r1.Add(item);
                    }
                    else
                    {
                        //写日志
                        c1 += "   " + item + "\n";
                        unidentifynum++;
                    }
                }
            }
            Logger.LogScanInfo($"\n【未识别出的视频：{unidentifynum}个】\n" + c1);

            //检查 重复|分段 视频
            Dictionary<string, List<string>> repeatlist = new Dictionary<string, List<string>>();
            string c2 = "";
            foreach (var item in r1)
            {
                if (File.Exists(item))
                {
                    id = IsEurope ? Identify.GetEuFanhao(new FileInfo(item).Name) : Identify.GetFanhao(new FileInfo(item).Name);
                    if (!repeatlist.ContainsKey(id))
                    {
                        List<string> pathlist = new List<string>();
                        pathlist.Add(item);
                        repeatlist.Add(id, pathlist);
                    }
                    else
                    {
                        repeatlist[id].Add(item);
                    }
                }
            }

            Console.WriteLine("repeatlist:" + repeatlist.Count);

            List<string> removelist = new List<string>();
            List<List<string>> subsectionlist = new List<List<string>>();
            foreach (KeyValuePair<string, List<string>> kvp in repeatlist)
            {
                if (kvp.Value.Count > 1)
                {
                    bool issubsection = IsSubSection(kvp.Value);
                    if (issubsection)
                    {
                        subsectionlist.Add(kvp.Value);
                    }
                    else
                    {
                        c2 += $"   识别码为：{kvp.Key}\n";
                        (string maxfilepath, List<string> Excludelsist) = ExcludeMaximumSize(kvp.Value);
                        removelist.AddRange(Excludelsist);
                        c2 += $"      导入的：{maxfilepath}，文件大小：{new FileInfo(maxfilepath).Length}\n";
                        Excludelsist.ForEach(arg =>
                        {
                            c2 += $"      未导入：{arg}，文件大小：{new FileInfo(arg).Length}\n";
                        });
                    }

                }
                else
                {

                }
            }
            Logger.LogScanInfo($"\n【重复的视频：{removelist.Count + subsectionlist.Count}个】\n" + c2);
            List<string> insertList = r1.Except(removelist).ToList();

            Console.WriteLine("removelist:" + removelist.Count);
            Console.WriteLine("subsectionlist:" + subsectionlist.Count);


            //导入分段视频
            foreach (var item in subsectionlist)
            {
                insertList = insertList.Except(item).ToList();

                try
                {
                    ct.ThrowIfCancellationRequested();
                }
                catch (OperationCanceledException ex)
                {
                    Logger.LogE(ex);
                    break;
                }
                string subsection = "";
                FileInfo fileinfo = new FileInfo(item[0]);
                id = IsEurope ? Identify.GetEuFanhao(fileinfo.Name) : Identify.GetFanhao(fileinfo.Name);
                if (IsEurope) { if (string.IsNullOrEmpty(id)) continue; else vt = VedioType.欧美; } else { vt = Identify.GetVedioType(id); }
                if (string.IsNullOrEmpty(id) | vt == 0) { continue; }

                //文件大小视为所有文件之和
                double filesize = 0;
                for (int i = 0; i < item.Count; i++)
                {
                    if (!File.Exists(item[i])) { continue; }
                    FileInfo fi = new FileInfo(item[i]);
                    subsection += item[i] + ";";
                    filesize += fi.Length;
                }

                //获取创建日期
                string createDate = "";
                try { createDate = fileinfo.CreationTime.ToString("yyyy-MM-dd HH:mm:ss"); }
                catch { }
                if (createDate == "") createDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                Movie movie = new Movie()
                {
                    filepath = item[0],
                    id = id,
                    filesize = filesize,
                    vediotype = (int)vt,
                    subsection = subsection.Substring(0, subsection.Length - 1),
                    scandate = createDate
                };
                cdb.InsertScanMovie(movie); totalinsertnum += 1;
            }

            Console.WriteLine("insertList:" + insertList.Count);


            //导入所有视频


            foreach (var item in insertList)
            {
                try
                {
                    ct.ThrowIfCancellationRequested();
                }
                catch (OperationCanceledException ex)
                {
                    Logger.LogE(ex);
                    break;
                }
                if (!File.Exists(item)) { continue; }
                FileInfo fileinfo = new FileInfo(item);

                string createDate = "";
                try { createDate = fileinfo.CreationTime.ToString("yyyy-MM-dd HH:mm:ss"); }
                catch { }
                if (createDate == "") createDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                id = IsEurope ? Identify.GetEuFanhao(fileinfo.Name) : Identify.GetFanhao(fileinfo.Name);
                if (IsEurope) { if (string.IsNullOrEmpty(id)) continue; else vt = VedioType.欧美; } else { vt = Identify.GetVedioType(id); }
                Movie movie = new Movie()
                {
                    filepath = item,
                    id = id,
                    filesize = fileinfo.Length,
                    vediotype = (int)vt,
                    scandate = createDate
                };
                cdb.InsertScanMovie(movie); totalinsertnum += 1;
            }
            cdb.CloseDB();
            Logger.LogScanInfo($"\n总计导入 => {totalinsertnum}个\n");


            //从 主数据库中 复制信息
            if (Properties.Settings.Default.DataBasePath.Split('\\').Last().Split('.').First().ToLower() != "info")
            {
                try
                {
                    //待修复 的 bug
                    CopyDatabaseInfo(Properties.Settings.Default.DataBasePath.Split('\\').Last().Split('.').First().ToLower());
                }
                catch { }
            }
                



            return totalinsertnum;
        }

        public static (string, List<string>) ExcludeMaximumSize(List<string> pathlist)
        {
            double maxsize = 0;
            int maxsizeindex = 0;
            double filesize = 0;
            int i = 0;
            foreach (var item in pathlist)
            {
                if (File.Exists(item))
                {
                    filesize = new FileInfo(item).Length;
                    if (maxsize < filesize) { maxsize = filesize; maxsizeindex = i; }
                }
                i++;
            }
            string maxsizepth = pathlist[maxsizeindex];
            pathlist.RemoveAt(maxsizeindex);
            return (maxsizepth, pathlist);
        }

    }
}
