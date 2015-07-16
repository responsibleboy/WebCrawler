using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Common
{
    public class FileHelper
    {
    /// <summary>
        /// 信息队列
        /// </summary>
        public static Queue<string> qMsg = null;
        /// <summary>
        /// 文件路径
        /// </summary>
        private static string filePath = "";

        /// <summary>
        /// 静态构造函数
        /// </summary>
        static FileHelper()
        {
            qMsg = new Queue<string>();
            ThreadPool.QueueUserWorkItem(u =>
            {
                while (true)
                {
                    string tmsg = string.Empty;
                    lock (qMsg)
                    {
                        if (qMsg.Count > 0)
                            tmsg = qMsg.Dequeue();
                    }

                    //往文件中写信息                     
                    if (!String.IsNullOrEmpty(tmsg))
                    {
                        int index = tmsg.IndexOf("&&");
                        string logTypeStr = tmsg.Substring(0, index);
                        LogType logType = LogType.Other;
                        if (logTypeStr == string.Format("{0}", LogType.Insert))
                        {
                            logType = LogType.Insert;
                        }
                        else if (logTypeStr == string.Format("{0}", LogType.Update))
                        {
                            logType = LogType.Update;
                        }
                        WriteText(tmsg.Substring(index + 2), "", logType);
                    }

                    if (qMsg.Count <= 0)
                    {
                        Thread.Sleep(300);
                    }
                }
            });
        }

        /// <summary>
        /// 写入文件
        /// </summary>
        /// <param name="strText">写入的内容</param>
        /// <param name="_filePath">文件路径</param>
        /// <param name="logType"></param>
        public static void WriteTextAsync(string strText, string _filePath = "", LogType logType = LogType.Other)
        {
            filePath = _filePath;
            //将错误信息添加到队列中
            lock (qMsg)
            {
                qMsg.Enqueue(string.Format("{0}&&{1}\r\n", logType, strText));
            }
        }
        
        /// <summary>
        /// 获取文件的全路径
        /// </summary>
        /// <param name="logType"></param>
        /// <returns></returns>
        private static string GetFilePath(LogType logType = LogType.Other)
        {
            string _filePath = filePath;
            if(String.IsNullOrEmpty(_filePath))
            {
                _filePath = System.AppDomain.CurrentDomain.BaseDirectory + "Log";
            }
            if (!Directory.Exists(_filePath))
            {
                Directory.CreateDirectory(_filePath);
            }
            switch (logType)
            {
                case LogType.Insert:
                    _filePath = _filePath + "\\" + DateTime.Now.ToString("yyyy-MM-dd") + "_Insert.txt";
                    break;
                case LogType.Update:
                    _filePath = _filePath + "\\" + DateTime.Now.ToString("yyyy-MM-dd") + "_Update.txt";
                    break;
                default:
                    _filePath = _filePath + "\\" + DateTime.Now.ToString("yyyy-MM-dd") + ".txt";
                    break;
            }
            if (!File.Exists(_filePath))
            {
                //File.Create(_filePath);
                FileStream fs = File.Create(_filePath);
                fs.Close();
                fs.Dispose();
            }

            return _filePath;
        }
                
        /// <summary>
        /// 写入文件
        /// </summary>
        /// <param name="text">内容</param>
        /// <param name="_filePath">文件路径</param>
        /// <param name="logType">类型</param>
        public static void WriteText(string text, string _filePath, LogType logType)
        {
            if (String.IsNullOrEmpty(text))
            {
                return;
            }
            text = text.Replace("\n", "\r\n");

            FileStream fs = null;
            try
            {
                if(String.IsNullOrEmpty(_filePath))
                {
                    _filePath = GetFilePath(logType);
                }                
                fs = File.OpenWrite(_filePath);
                byte[] btFile = Encoding.UTF8.GetBytes(text);
                //设定书写的開始位置为文件的末尾  
                fs.Position = fs.Length;
                //将待写入内容追加到文件末尾  
                fs.Write(btFile, 0, btFile.Length);
            }
            finally
            {
                if (fs != null)
                {
                    fs.Close();
                    fs.Dispose();
                }
            }
        }
    }

    /// <summary>
    /// 日志类型
    /// </summary>
    public enum LogType
    {
        /// <summary>
        /// 插入型
        /// </summary>
        Insert,
        /// <summary>
        /// 更新型
        /// </summary>
        Update,
        /// <summary>
        /// 其他型
        /// </summary>
        Other
    }
}
