using DynamicData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jvedio
{
    public static class MediaParse
    {

        public static string GetVedioDuration(string path)
        {
            MediaInfo mediaInfo = new MediaInfo();
            mediaInfo.Open(path);
            string result = "00:00:00";
            try
            {
                result = mediaInfo.Get(0, 0, "Duration/String3").Substring(0, mediaInfo.Get(0, 0, "Duration/String3").LastIndexOf("."));
            }
            catch { }

            return result;
        }

        public static string[] GetCutOffArray(string path)
        {
            if (Properties.Settings.Default.ScreenShotNum <= 0 || Properties.Settings.Default.ScreenShotNum > 20) Properties.Settings.Default.ScreenShotNum = 10;
            string[] result = new string[Properties.Settings.Default.ScreenShotNum+2];
            string Duration = GetVedioDuration(path);
            uint Second = DurationToSecond(Duration);
            

            if(Second <20) { return null; }
            else
            {
                if (Second > 350)
                    Second = Second - 300; //去掉开头结尾
                    
                // n 等分
                uint splitLength =(uint)( Second / Properties.Settings.Default.ScreenShotNum);
                if (splitLength == 0) splitLength = 1;
                for (int i = 0; i < result.Count(); i++)
                    result[i] = SecondToDuration(300 + splitLength * i);

                if(Second-30> DurationToSecond(result[Properties.Settings.Default.ScreenShotNum - 1]))
                {
                    result[Properties.Settings.Default.ScreenShotNum] = SecondToDuration(Second - 60);
                    result[Properties.Settings.Default.ScreenShotNum + 1] = SecondToDuration(Second - 30);
                }

                return result;
            }

            
        }

        public static uint DurationToSecond(string Duration)
        {
            // 00:00:00
            if (Duration.Split(':').Count() < 3) return 0;
            uint Hour = uint.Parse( Duration.Split(':')[0]);
            uint Minutes = uint.Parse(Duration.Split(':')[1]);
            uint Seconds = uint.Parse(Duration.Split(':')[2]);
            return Hour * 3600 + Minutes * 60 + Seconds;
        }

        public static string SecondToDuration(double Second)
        {
            // 36000 10h
            if (Second ==0 ) return "00:00:00";
            TimeSpan timeSpan = TimeSpan.FromSeconds(Second);
            return $"{timeSpan.Hours.ToString().PadLeft(2,'0')}:{timeSpan.Minutes.ToString().PadLeft(2, '0')}:{timeSpan.Seconds.ToString().PadLeft(2, '0')}";


        }

        public static string MediaInfo(string VideoName)
        {
            string info = "无视频信息";
            if (File.Exists(VideoName))
            {
                MediaInfo MI = new MediaInfo();
                MI.Open(VideoName);
                //全局
                string container = MI.Get(StreamKind.General, 0, "Format");
                string bitrate = MI.Get(StreamKind.General, 0, "BitRate/String");
                string duration = MI.Get(StreamKind.General, 0, "Duration/String1");
                string fileSize = MI.Get(StreamKind.General, 0, "FileSize/String");
                //视频
                string vid = MI.Get(StreamKind.Video, 0, "ID");
                string video = MI.Get(StreamKind.Video, 0, "Format");
                string vBitRate = MI.Get(StreamKind.Video, 0, "BitRate/String");
                string vSize = MI.Get(StreamKind.Video, 0, "StreamSize/String");
                string width = MI.Get(StreamKind.Video, 0, "Width");
                string height = MI.Get(StreamKind.Video, 0, "Height");
                string risplayAspectRatio = MI.Get(StreamKind.Video, 0, "DisplayAspectRatio/String");
                string risplayAspectRatio2 = MI.Get(StreamKind.Video, 0, "DisplayAspectRatio");
                string frameRate = MI.Get(StreamKind.Video, 0, "FrameRate/String");
                string bitDepth = MI.Get(StreamKind.Video, 0, "BitDepth/String");
                string pixelAspectRatio = MI.Get(StreamKind.Video, 0, "PixelAspectRatio");
                string encodedLibrary = MI.Get(StreamKind.Video, 0, "Encoded_Library");
                string encodeTime = MI.Get(StreamKind.Video, 0, "Encoded_Date");
                string codecProfile = MI.Get(StreamKind.Video, 0, "Codec_Profile");
                string frameCount = MI.Get(StreamKind.Video, 0, "FrameCount");

                //音频
                string aid = MI.Get(StreamKind.Audio, 0, "ID");
                string audio = MI.Get(StreamKind.Audio, 0, "Format");
                string aBitRate = MI.Get(StreamKind.Audio, 0, "BitRate/String");
                string samplingRate = MI.Get(StreamKind.Audio, 0, "SamplingRate/String");
                string channel = MI.Get(StreamKind.Audio, 0, "Channel(s)");
                string aSize = MI.Get(StreamKind.Audio, 0, "StreamSize/String");

                string audioInfo = MI.Get(StreamKind.Audio, 0, "Inform") + MI.Get(StreamKind.Audio, 1, "Inform") + MI.Get(StreamKind.Audio, 2, "Inform") + MI.Get(StreamKind.Audio, 3, "Inform");
                string videoInfo = MI.Get(StreamKind.Video, 0, "Inform");

                info = System.IO.Path.GetFileName(VideoName) + "\r\n" +
                    "容器：" + container + "\r\n" +
                    "总码率：" + bitrate + "\r\n" +
                    "大小：" + fileSize + "\r\n" +
                    "时长：" + duration + "\r\n" +
                    "\r\n" +
                    "视频(" + vid + ")：" + video + "\r\n" +
                    "码率：" + vBitRate + "\r\n" +
                    "大小：" + vSize + "\r\n" +
                    "分辨率：" + width + "x" + height + "\r\n" +
                    "宽高比：" + risplayAspectRatio + "(" + risplayAspectRatio2 + ")" + "\r\n" +
                    "帧率：" + frameRate + "\r\n" +
                    "位深度：" + bitDepth + "\r\n" +
                    "像素宽高比：" + pixelAspectRatio + "\r\n" +
                    "编码库：" + encodedLibrary + "\r\n" +
                    "Profile：" + codecProfile + "\r\n" +
                    "编码时间：" + encodeTime + "\r\n" +
                    "总帧数：" + frameCount + "\r\n" +

                    "\r\n" +
                    "音频(" + aid + ")：" + audio + "\r\n" +
                    "大小：" + aSize + "\r\n" +
                    "码率：" + aBitRate + "\r\n" +
                    "采样率：" + samplingRate + "\r\n" +
                    "声道数：" + channel + "\r\n" +
                    "\r\n====详细信息====\r\n" +
                    videoInfo + "\r\n" +
                    audioInfo + "\r\n"
                    ;
                MI.Close();
            }
            return info;
        }



        //public static string[] ExtractInfo(string path)
        //{
        //    MediaInfo MI = new MediaInfo();
        //    MI.Open(path);
        //    string[] returnInfo = new string[3];

        //    //File name 0
        //    returnInfo[0] = MI.Get(0, 0, "FileName");

        //    //Date created 2
        //    returnInfo[1] = MI.Get(0, 0, "File_Created_Date").Substring(
        //        MI.Get(0, 0, "File_Created_Date").IndexOf(" ") + 1, MI.Get(0, 0, "File_Created_Date").LastIndexOf(".") - 4);

        //    //Length 4
        //    returnInfo[2] = MI.Get(0, 0, "Duration/String3").Substring(0, MI.Get(0, 0, "Duration/String3").LastIndexOf("."));

        //    return returnInfo;
        //}

    }
}
