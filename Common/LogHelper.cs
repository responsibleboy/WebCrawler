using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections;
using System.Threading;
using log4net;

namespace Common
{
    public class LogHelper
    {
        /// <summary>
        /// 异常信息的队列
        /// </summary>
        public static Queue<string> qMsg = null;

        /// <summary>
        /// 静态构造函数
        /// </summary>
        static LogHelper()
        {
            qMsg = new Queue<string>();
            ThreadPool.QueueUserWorkItem(u =>
            {
                while (true)
                {
                    //if (qMsg == null)
                    //{
                    //    continue;
                    //}

                    string tmsg = string.Empty;
                    lock (qMsg)
                    {
                        if (qMsg.Count > 0)
                            tmsg = qMsg.Dequeue();
                    }

                    //往日志文件中写错误信息                     
                    if (!String.IsNullOrEmpty(tmsg))
                    {
                        ILog log = log4net.LogManager.GetLogger("ErrorLog");
                        log.Error(tmsg);
                    }


                    if (qMsg.Count <= 0)
                    {
                        Thread.Sleep(30);
                    }
                }
            });
        }

        /// <summary>
        /// 写入错误日志
        /// </summary>
        /// <param name="msg">错误信息</param>
        public static void WriteLog(string msg)
        {
            
            //将错误信息添加到队列中
            lock (qMsg)
            {
                qMsg.Enqueue(msg);
            }           
            
        }
    }
}
