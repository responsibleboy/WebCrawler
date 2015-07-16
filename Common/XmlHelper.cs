using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Common
{
    public class XmlHelper
    {
        /// <summary>
        /// 从XML串中读取节点
        /// </summary>
        /// <param name="strXml">XML串</param>
        /// <param name="strNode">节点</param>
        /// <returns></returns>
        public static string GetXmlNodeText(string strXml, string strNode)
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(strXml);
                XmlNode xmlNode = xmlDoc.SelectSingleNode(strNode);
                if (xmlNode != null)
                {
                    return xmlNode.InnerText;
                }
                else
                {
                    return "";
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show("读取XML串失败！\n错误信息：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                string msg = "读取XML串失败！错误信息：" + ex.Message;
                LogHelper.WriteLog(msg);
                return "";
            }
        }

        /// <summary>
        /// 从XML串中转换成datatable
        /// </summary>
        /// <param name="strXml">XML串</param>
        /// <param name="strNode">节点</param>
        /// <returns></returns>
        public static DataTable XmlToDataTable(string strXml, string strNode)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(strXml);

            XmlNodeList xmlNode = xmlDoc.SelectNodes(strNode);

            DataTable dt = new DataTable();
            DataRow dr;

            if (xmlNode != null)
            {
                for (int i = 0; i < xmlNode.Count; i++)
                {
                    dr = dt.NewRow();
                    XmlElement xe = (XmlElement)xmlNode.Item(i);

                    for (int k = 0; k < xe.ChildNodes.Count; k++)
                    {
                        if (!dt.Columns.Contains(xe.ChildNodes.Item(k).Name))
                        {
                            dt.Columns.Add(xe.ChildNodes.Item(k).Name);
                            dr[xe.ChildNodes.Item(k).Name] = xe.ChildNodes.Item(k).InnerText;
                        }
                        else
                        {
                            dr[xe.ChildNodes.Item(k).Name] = xe.ChildNodes.Item(k).InnerText;
                        }
                    }

                    dt.Rows.Add(dr);
                }
            }

            return dt;


        }

        /// <summary>
        /// 读取key-value键值对
        /// </summary>
        /// <param name="strXml"></param>
        /// <param name="strNode"></param>
        /// <returns></returns>
        public static Dictionary<string,string> GetKeyValue(string strXml,string strNode)
        {
            Dictionary<string, string> dicKeyValue = new Dictionary<string, string>();
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(strXml);
                XmlNode xmlNode = xmlDoc.SelectSingleNode(strNode);
                if (xmlNode != null)
                {
                    //return xmlNode.InnerText;
                //TODO:没有获取
                }
            }
            catch(Exception ex)
            {
                string msg = "从XML文件中的指定节点下读取子节点的key-value信息失败。详细信息：" + ex.Message;
                LogHelper.WriteLog(msg);
            }

            return dicKeyValue;
        }
    }
}
