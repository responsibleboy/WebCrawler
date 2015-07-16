using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Configs
{
    public class WebCrawlerFormApp
    {
        /// <summary>
        /// 异步请求最多个数
        /// </summary>
        public static int ReqCount
        {
            get
            {
                string temp = System.Configuration.ConfigurationManager.AppSettings["reqCount"];
                if(String.IsNullOrEmpty(temp))
                {
                    temp = "5";
                }
                int ret = 5;
                if(!int.TryParse(temp,out ret))
                {
                    ret = 5;
                }
                return ret;
            }
        }

        /// <summary>
        /// 网页编码格式
        /// </summary>
        public static Encoding HtmlEncoding
        {
            get
            {
                string temp = System.Configuration.ConfigurationManager.AppSettings["htmlEncoding"];
                if (String.IsNullOrEmpty(temp))
                {
                    temp = "GB18030";
                }
                return Encoding.GetEncoding(temp);
            }
        }

        /// <summary>
        /// 超时时间，毫秒
        /// </summary>
        public static int MaxTime
        {
            get
            {
                string temp = System.Configuration.ConfigurationManager.AppSettings["maxTime"];
                if (String.IsNullOrEmpty(temp))
                {
                    temp = "120000";
                }
                int ret = 120000;
                if (!int.TryParse(temp, out ret))
                {
                    ret = 120000;
                }
                return ret;
            }
        }

        /// <summary>
        /// 最大搜索深度
        /// </summary>
        public static int MaxDepth
        {
            get
            {
                string temp = System.Configuration.ConfigurationManager.AppSettings["maxDepth"];
                if (String.IsNullOrEmpty(temp))
                {
                    temp = "10";
                }
                int ret = 10;
                if (!int.TryParse(temp, out ret))
                {
                    ret = 10;
                }
                return ret;
            }
        }

        /// <summary>
        /// 是否保存HTML原文件
        /// </summary>
        public static bool IsSaveHtml
        {
            get
            {
                string temp = System.Configuration.ConfigurationManager.AppSettings["isSaveHtml"];
                if (String.IsNullOrEmpty(temp) || (temp.ToLower() != "true" && temp.ToLower() != "false"))
                {
                    temp = "false";
                }
                return Convert.ToBoolean(temp);
            }
        }
    }
}
