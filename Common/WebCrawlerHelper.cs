using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Common
{
    /// <summary>
    /// 简单网络爬虫类
    /// </summary>
    public class WebCrawlerHelper
    {
        #region 定义
        
        /// <summary>
        /// 异步请求最多个数
        /// </summary>
        private int reqCount = 5;
        /// <summary>
        /// 网页编码格式
        /// </summary>
        private Encoding htmlEncoding = null;
        /// <summary>
        /// 已经下载的URL，(url,depth)
        /// </summary>
        private Dictionary<string, int> dicLoadedUrls = null;
        /// <summary>
        /// 还未下载的URL, (url,delpth)
        /// </summary>
        private Dictionary<string, int> dicUnloadUrls = null;
        /// <summary>
        /// 每个元素代表一个工作实例是否正在工作
        /// </summary>
        private bool[] reqsBusy = null;
        /// <summary>
        /// 超时时间
        /// </summary>
        private int maxTime = 2 * 60 * 1000;
        /// <summary>
        /// 最大深度
        /// </summary>
        private int maxDepth = 10;
        /// <summary>
        /// 第一个下载连接的主要部分
        /// </summary>
        private string baseUrl = "";
        /// <summary>
        /// 根下载链接
        /// </summary>
        private string rootUrl = "";        
        /// <summary>
        /// 用户代理，"Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; Trident/4.0)"
        /// </summary>
        private string _userAgent = "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; Trident/4.0)";
        /// <summary>
        /// 获取的内容，"text/html"
        /// </summary>
        private string _accept = "text/html";
        /// <summary>
        /// 请求的方法，"GET"
        /// </summary>
        private string _method = "GET";
        /// <summary>
        /// 互斥锁
        /// </summary>
        private readonly object _locker = new object();
        /// <summary>
        /// 是否停止搜索
        /// </summary>
        private bool isStopDown = false;
        /// <summary>
        /// 保存文件编号
        /// </summary>
        private int fileIndex = 1;
        /// <summary>
        /// 筛选触发器
        /// </summary>
        private Timer checkTimer = null;
        /// <summary>
        /// 是否保存HTML原文件
        /// </summary>
        private bool isSaveHtml = false;

        #endregion

        #region 工作
        /// <summary>
        /// 参数初始化
        /// </summary>
        /// <param name="_url">请求地址</param>
        /// <param name="_reqCount">异步请求个数</param>
        /// <param name="_htmlEncoding">网页编码格式</param>
        /// <param name="_maxTime">超时时间，毫秒</param>
        /// <param name="_maxDepth">搜索最大深度</param>
        /// <param name="_isSaveHtml">是否保存HTML原文件</param>
        private void Init(string _url, int _reqCount, Encoding _htmlEncoding, int _maxTime, int _maxDepth, bool _isSaveHtml)
        {
            if (!_url.Contains("http://"))
            {
                rootUrl = "http://" + _url;
            }
            else
            {
                rootUrl = _url;
            }
            baseUrl = rootUrl.Replace("www.", "");
            baseUrl = baseUrl.Replace("http://", "");
            baseUrl = baseUrl.TrimEnd('/');
            
            this.reqCount = _reqCount;
            this.htmlEncoding = _htmlEncoding;
            this.maxTime = _maxTime;
            this.maxDepth = _maxDepth;
            this.isSaveHtml = _isSaveHtml;

            if (dicLoadedUrls == null)
            {
                dicLoadedUrls = new Dictionary<string, int>();
            }
            dicLoadedUrls.Clear();
            if (dicUnloadUrls == null)
            {
                dicUnloadUrls = new Dictionary<string, int>();
            }
            dicUnloadUrls.Clear();
            AddUrls(new string[1] { rootUrl }, 0);
            reqsBusy = new bool[this.reqCount];
            for (int i = 0; i < reqCount; i++)
            {
                reqsBusy[i] = false;
            }

            isStopDown = false;
            fileIndex = 1;

            if (checkTimer == null)
            {
                checkTimer = new Timer(new TimerCallback(CheckFinish), null, 0, 300);
            }
        }

        /// <summary>
        /// 分配工作
        /// </summary>
        private void DispatchWork()
        {
            if (isStopDown)
            {
                return;
            }
            for (int i = 0; i < reqCount; i++)
            {
                if (!reqsBusy[i]) //判断此编号的工作实例是否空闲
                {
                    RequestResource(i); //让此工作实例请求资源
                }
            }
        }

        /// <summary>
        /// 异步请求
        /// </summary>
        /// <param name="index"></param>
        private void RequestResource(int index)
        {
            int depth;
            string url = "";
            try
            {
                lock (_locker)
                {
                    if (dicUnloadUrls.Count <= 0) //判断是否还有未下载的URL
                    {
                        //_workingSignals.FinishWorking(index); //设置工作实例的状态为Finished
                        reqsBusy[index] = false;
                        return;
                    }
                    reqsBusy[index] = true;
                    //_workingSignals.StartWorking(index); //设置工作状态为Working
                    depth = dicUnloadUrls.First().Value; //取出第一个未下载的URL
                    url = dicUnloadUrls.First().Key;
                    dicLoadedUrls.Add(url, depth); //把该URL加入到已下载里
                    dicUnloadUrls.Remove(url); //把该URL从未下载中移除
                }

                HttpWebRequest req = WebRequest.Create(url) as HttpWebRequest;
                req.Method = _method; //请求方法
                req.Accept = _accept; //接受的内容
                req.UserAgent = _userAgent; //用户代理
                RequestState rs = new RequestState(req, url, depth, index); //回调方法的参数
                var result = req.BeginGetResponse(new AsyncCallback(ReceivedResource), rs); //异步请求
                ThreadPool.RegisterWaitForSingleObject(result.AsyncWaitHandle, //注册超时处理方法
                        TimeoutCallback, rs, maxTime, true);
            }
            catch (WebException we)
            {
                //MessageBox.Show("RequestResource " + we.Message + url + we.Status);
                LogHelper.WriteLog("RequestResource " + we.Message + url + we.Status);
            }
        }

        /// <summary>
        /// 处理请求的响应
        /// </summary>
        /// <param name="ar"></param>
        private void ReceivedResource(IAsyncResult ar)
        {
            RequestState rs = (RequestState)ar.AsyncState; //得到请求时传入的参数
            HttpWebRequest req = rs.Req;
            string url = rs.Url;
            try
            {
                HttpWebResponse res = (HttpWebResponse)req.EndGetResponse(ar); //获取响应
                if (isStopDown) //判断是否中止下载
                {
                    res.Close();
                    req.Abort();
                    return;
                }
                if (res != null && res.StatusCode == HttpStatusCode.OK) //判断是否成功获取响应
                {
                    Stream resStream = res.GetResponseStream(); //得到资源流
                    rs.ResStream = resStream;
                    var result = resStream.BeginRead(rs.Data, 0, rs.BufferSize, //异步请求读取数据
                        new AsyncCallback(ReceivedData), rs);
                }
                else //响应失败
                {
                    res.Close();
                    rs.Req.Abort();
                    reqsBusy[rs.Index] = false; //重置工作状态
                    DispatchWork(); //分配新任务
                }
            }
            catch (WebException we)
            {
                LogHelper.WriteLog("ReceivedResource " + we.Message + url + we.Status);
            }
        }

        /// <summary>
        /// 接收数据并处理
        /// </summary>
        /// <param name="ar"></param>
        private void ReceivedData(IAsyncResult ar)
        {
            RequestState rs = (RequestState)ar.AsyncState; //获取参数
            HttpWebRequest req = rs.Req;
            Stream resStream = rs.ResStream;
            string url = rs.Url;
            int depth = rs.Depth;
            string html = "";
            int index = rs.Index;
            int read = 0;

            try
            {
                read = resStream.EndRead(ar); //获得数据读取结果
                if (isStopDown)//判断是否中止下载
                {
                    rs.ResStream.Close();
                    req.Abort();
                    return;
                }
                if (read > 0)
                {
                    MemoryStream ms = new MemoryStream(rs.Data, 0, read); //利用获得的数据创建内存流
                    StreamReader reader = new StreamReader(ms, htmlEncoding);
                    string str = reader.ReadToEnd(); //读取所有字符
                    rs.sbHtml.Append(str); // 添加到之前的末尾
                    var result = resStream.BeginRead(rs.Data, 0, rs.BufferSize, //再次异步请求读取数据
                        new AsyncCallback(ReceivedData), rs);
                    return;
                }
                html = rs.sbHtml.ToString();
                SaveContents(html); //保存到本地
                string[] urls = GetUrls(html); //获取页面中的链接
                AddUrls(urls, depth + 1); //过滤链接并添加到未下载集合中
                reqsBusy[index] = false; //重置工作状态
                DispatchWork(); //分配新任务
            }
            catch (WebException we)
            {
                LogHelper.WriteLog("ReceivedData Web " + we.Message + url + we.Status);
            }
        }

        /// <summary>
        /// 请求超时处理
        /// </summary>
        /// <param name="state"></param>
        /// <param name="timedOut"></param>
        private void TimeoutCallback(object state, bool timedOut)
        {
            if (timedOut) //判断是否是超时
            {
                RequestState rs = state as RequestState;
                if (rs != null)
                {
                    rs.Req.Abort(); //撤销请求
                }
                reqsBusy[rs.Index] = false; //重置工作状态
                DispatchWork(); //分配新任务
            }
        }

        /// <summary>
        /// 保存html
        /// </summary>
        /// <param name="html"></param>
        private void SaveContents(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                return;
            }
            
            if(isSaveHtml)
            {
                string path = "";
                //lock (_locker)
                //{
                //    path = string.Format("{0}{1}\\{2:D5}.txt", System.AppDomain.CurrentDomain.BaseDirectory, "html", fileIndex++);
                //}
                path = string.Format("{0}{1}\\{2}.txt", System.AppDomain.CurrentDomain.BaseDirectory, "html", DateTime.Now.ToString("yyyyMMddHHmmssfff"));

                try
                {
                    string dir = Path.GetDirectoryName(path);
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    using (StreamWriter fs = new StreamWriter(path))
                    {
                        fs.Write(html);
                    }
                }
                catch (IOException ioe)
                {
                    //MessageBox.Show("SaveContents IO" + ioe.Message + " path=" + path);
                    LogHelper.WriteLog("SaveContents IO" + ioe.Message + " path=" + path);
                }
            }

            if (ContentsSavedEvent != null)
            {
                ContentsSavedEvent(html);
            }
        }

        /// <summary>
        /// 检查是否已经全部下载完毕
        /// </summary>
        /// <param name="param"></param>
        private void CheckFinish(object param)
        {
            bool notEnd = false;
            foreach (var b in reqsBusy)
            {
                notEnd |= b;
            }

            if (!notEnd && (dicUnloadUrls == null || dicUnloadUrls.Count <= 0))
            {
                if (checkTimer != null)
                {
                    checkTimer.Dispose();
                    checkTimer = null;
                }
                if (dicLoadedUrls != null)
                {
                    dicLoadedUrls.Clear();
                    dicLoadedUrls = null;
                }
                if (dicUnloadUrls != null)
                {
                    dicUnloadUrls.Clear();
                    dicUnloadUrls = null;
                }
                if (DownloadFinishedEvent != null)
                {
                    DownloadFinishedEvent();
                }
            }
        }
        #endregion

        #region URL处理
        /// <summary>
        /// 从页面中提取网络链接
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        private string[] GetUrls(string html)
        {
            string pattern = @"http://([\w-]+\.)+[\w-]+(/[\w- ./?%&=]*)?";
            Regex r = new Regex(pattern, RegexOptions.IgnoreCase); //新建正则模式
            MatchCollection m = r.Matches(html); //获得匹配结果
            string[] urls = new string[m.Count];
            for (int i = 0; i < m.Count; i++)
            {
                urls[i] = m[i].ToString(); //提取出结果
            }
            return urls;
        }

        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="urls"></param>
        /// <param name="depth"></param>
        private void AddUrls(string[] urls, int depth)
        {
            if (depth >= maxDepth)
            {
                return;
            }
            foreach (string url in urls)
            {
                string cleanUrl = url.Trim();
                int end = cleanUrl.IndexOf(' ');
                if (end > 0)
                {
                    cleanUrl = cleanUrl.Substring(0, end);
                }
                cleanUrl = cleanUrl.TrimEnd('/');
                if (IsAvailableUrl(cleanUrl))
                {
                    if (cleanUrl.Contains(baseUrl))
                    {
                        dicUnloadUrls.Add(cleanUrl, depth);
                    }
                    else
                    {
                        // 外部链接
                    }
                }
            }
        }

        /// <summary>
        /// 是否是有效链接
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private bool IsAvailableUrl(string url)
        {
            if (dicLoadedUrls.ContainsKey(url) || dicUnloadUrls.ContainsKey(url))
            {
                return false;
            }
            if (url.Contains(".jpg") || url.Contains(".gif")
                || url.Contains(".png") || url.Contains(".css")
                || url.Contains(".js"))
            {
                return false;
            }
            return true;
        }
        #endregion

        #region events
        /// <summary>
        /// 
        /// </summary>
        /// <param name="html"></param>
        public delegate void ContentsSavedHandler(string html);
        /// <summary>
        /// 正文内容被保存到本地后触发
        /// </summary>
        public event ContentsSavedHandler ContentsSavedEvent = null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="count"></param>
        public delegate void DownloadFinishHandler();
        /// <summary>
        /// 全部链接下载分析完毕后触发
        /// </summary>
        public event DownloadFinishHandler DownloadFinishedEvent = null;
        #endregion

        #region 下载
        /// <summary>
        /// 开始下载
        /// </summary>
        /// <param name="_url">请求地址</param>
        /// <param name="_reqCount">异步请求个数</param>
        /// <param name="_htmlEncoding">网页编码格式</param>
        /// <param name="_maxTime">超时时间，毫秒</param>
        /// <param name="_maxDepth">搜索最大深度</param>
        /// <param name="_isSaveHtml">是否保存HTML原文件</param>

        public void Download(string _url, int _reqCount, Encoding _htmlEncoding, int _maxTime, int _maxDepth, bool _isSaveHtml)
        {
            if (string.IsNullOrEmpty(_url))
            {
                return;
            }
            Init(_url, _reqCount, _htmlEncoding, _maxTime, _maxDepth, _isSaveHtml);
            DispatchWork();
        }

        /// <summary>
        /// 终止下载
        /// </summary>
        public void Abort()
        {
            isStopDown = true;
            for (int i = 0; i < reqCount; i++)
            {
                reqsBusy[i] = false;
            }

            if (dicLoadedUrls != null)
            {
                dicLoadedUrls.Clear();
                dicLoadedUrls = null;
            }
            if (dicUnloadUrls != null)
            {
                dicUnloadUrls.Clear();
                dicUnloadUrls = null;
            }
        } 
        #endregion
    }


    public class RequestState
    {
        #region 自定义私有变量
        /// <summary>
        /// 接收数据包的空间大小,131072
        /// </summary>
        private const int BUFFER_SIZE = 131072;
        /// <summary>
        /// 接收数据包的buffer
        /// </summary>
        private byte[] _data = new byte[BUFFER_SIZE];
        /// <summary>
        /// 存放所有接收到的字符
        /// </summary>
        private StringBuilder _sb = new StringBuilder();
        /// <summary>
        /// http请求
        /// </summary>
        private HttpWebRequest _req = null;
        /// <summary>
        /// 请求的URL
        /// </summary>
        private string _url = "";
        /// <summary>
        /// 此次请求的相对深度
        /// </summary>
        private int _depth = 0;
        /// <summary>
        /// 工作实例的编号
        /// </summary>
        private int _index = 0;
        /// <summary>
        /// 接收数据流
        /// </summary>
        private Stream _resStream = null;
        #endregion

        #region 属性
        /// <summary>
        /// http请求
        /// </summary>
        public HttpWebRequest Req
        {
            get
            {
                //if(_req==null)
                //{
                //    _req = WebRequest.Create(Url) as HttpWebRequest;
                //}
                return _req;
            }
            private set
            {
                _req = value;
            }
        }
        /// <summary>
        /// 请求的URL
        /// </summary>
        public string Url
        {
            get
            {
                return _url;
            }
            private set
            {
                _url = value;
            }
        }
        /// <summary>
        /// 此次请求的相对深度
        /// </summary>
        public int Depth
        {
            get
            {
                return _depth;
            }
            private set
            {
                _depth = value;
            }
        }
        /// <summary>
        /// 工作实例的编号
        /// </summary>
        public int Index
        {
            get
            {
                return _index;
            }
            private set
            {
                _index = value;
            }
        }
        /// <summary>
        /// 接收数据流
        /// </summary>
        public Stream ResStream
        {
            get
            {
                return _resStream;
            }
            set
            {
                _resStream = value;
            }
        }
        /// <summary>
        /// 存放所有接收到的字符
        /// </summary>
        public StringBuilder sbHtml
        {
            get
            {
                return _sb;
            }
        }
        /// <summary>
        /// 接收数据包的buffer
        /// </summary>
        public byte[] Data
        {
            get
            {
                return _data;
            }
        }
        /// <summary>
        /// 接收数据包的空间大小,131072
        /// </summary>
        public int BufferSize
        {
            get
            {
                return BUFFER_SIZE;
            }
        }
        #endregion

        #region 构造函数
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="req">http请求</param>
        /// <param name="url">请求地址</param>
        /// <param name="depth">此次请求的相对深度</param>
        /// <param name="index"> 工作实例的编号</param>
        public RequestState(HttpWebRequest req, string url, int depth, int index)
        {
            this._req = req;
            this._url = url;
            this._depth = depth;
            this._index = index;
        }
        #endregion
    }
}
