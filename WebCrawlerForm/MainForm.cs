using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WebCrawlerForm
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        #region 自定义
        private Common.WebCrawlerHelper webCrawlerHelper = null;
        /// <summary>
        /// 筛选正则表达式
        /// </summary>
        private string regex = ""; 
        #endregion

        #region 委托
        public void ContentsSaved(string html)
        {
            Regex r = new Regex(regex, RegexOptions.IgnoreCase); //新建正则模式
            MatchCollection m = r.Matches(html); //获得匹配结果            
            for (int i = 0; i < m.Count; i++)
            {
                ShowInfo(m[i].ToString());
            }
        }

        public void DownloadFinished()
        {
            StopDownload();
        } 
        #endregion

        #region 其他
        delegate void dgStopDownload();
        public void StopDownload()
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new dgStopDownload(StopDownload));
            }
            else
            {
                webCrawlerHelper.Abort();

                this.btnStop.Enabled = false;
                this.btnStart.Enabled = true;
            }
        }
        #endregion

        #region 窗体显示

        delegate void dgShowInfo(string showInfo, bool isError);

        /// <summary>
        /// 在窗体上显示信息
        /// </summary>
        /// <param name="showInfo">需要添加的信息</param>
        /// <param name="isError">是否是错误信息</param>
        /// <param name="logType">日志类型</param>
        public void ShowInfo(string showInfo, bool isError = false)
        {
            if (String.IsNullOrEmpty(showInfo))
            {
                return;
            }
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new dgShowInfo(ShowInfo), showInfo, isError);
            }
            else if (rtxtResult != null)
            {
                if (isError)
                {
                    showInfo = string.Format("{0}：ERROR {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), showInfo);
                    int index = rtxtResult.TextLength; //未添加时的长度
                    rtxtResult.AppendText(showInfo);
                    rtxtResult.AppendText("\r\n");
                    int length = rtxtResult.TextLength; //添加后的长度
                    rtxtResult.Select(index, length);
                    rtxtResult.SelectionColor = Color.Red; //错误行设置为红色
                }
                else
                {
                    showInfo = string.Format("{0}：{1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), showInfo);
                    rtxtResult.AppendText(showInfo);
                    rtxtResult.AppendText("\r\n");
                }
                rtxtResult.ScrollToCaret();

                Common.FileHelper.WriteTextAsync(showInfo, System.AppDomain.CurrentDomain.BaseDirectory + "result");
            }
        }
        #endregion


        #region MainForm
        private void MainForm_Load(object sender, EventArgs e)
        {
            webCrawlerHelper = new Common.WebCrawlerHelper();
            webCrawlerHelper.ContentsSavedEvent += this.ContentsSaved;
            webCrawlerHelper.DownloadFinishedEvent += this.DownloadFinished;

            this.btnStop.Enabled = false;
            this.btnStart.Enabled = true;
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(txtURL.Text))
            {
                MessageBox.Show("请填写URL地址！", "错误提示");
                return;
            }
            if (String.IsNullOrEmpty(cmbRegex.Text))
            {
                MessageBox.Show("请填写筛选正则表达式！", "错误提示");
                return;
            }
            regex = cmbRegex.Text;
            rtxtResult.Clear();
            webCrawlerHelper.Download(txtURL.Text,
                Configs.WebCrawlerFormApp.ReqCount,
                Configs.WebCrawlerFormApp.HtmlEncoding,
                Configs.WebCrawlerFormApp.MaxTime,
                Configs.WebCrawlerFormApp.MaxDepth,
                Configs.WebCrawlerFormApp.IsSaveHtml);

            this.btnStop.Enabled = true;
            this.btnStart.Enabled = false;
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            StopDownload();
        } 
        #endregion
        
    }
}
