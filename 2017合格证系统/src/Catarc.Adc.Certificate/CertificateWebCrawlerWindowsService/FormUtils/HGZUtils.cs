﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Text.RegularExpressions;
using System.Data.SqlClient;
using CertificateWebCrawlerWindowsService.Utils;
using CertificateWebCrawlerWindowsService.Helper;
using System.Threading;


namespace CertificateWebCrawlerWindowsService
{
    public class HGZUtils
    {
        private bool IsStopped = false;
        string HtmlName = "";
        string MaxPageNum = "1";
        string param = string.Empty;
        LogManager logMgr = new LogManager();
        int PageFrom = 1;//线程开始页
        int PageTo = 1;//线程结束页 
        private static int iThreadNum = 3;//int.Parse(Settings.Default.ThreadNum);
        //最多3个线程同时运行，当前空闲个数为3
        //private static Semaphore Idleupdate = new Semaphore(iThreadNum, iThreadNum);

        int iThreadRuned = 0;

        public HGZUtils()
        {
        }

        /// <summary>
        /// 开始监控:单个线程
        /// </summary>
        /// <param name="param">条件限制，日期限制</param>
        /// <param name="paramPageFrom">页数从</param>
        /// <param name="paramPageTo">页数至</param>
        public void StartThreadPool(string param, string paramPageFrom, string paramPageTo)
        {
            string msg = string.Empty;
            if (!string.IsNullOrEmpty(param))
            {
                this.param = param;
            }
            if (!string.IsNullOrEmpty(paramPageFrom))
            {
                this.PageFrom = int.Parse(paramPageFrom);
            }

            //获取最大页数
            string MaxPageNumm = Tool.GetHtmlSourceListMaxPageNum(Tool.strTargerUrlListHGZ + "1");
            int iMaxPageNumm = int.Parse(MaxPageNumm);
            msg = string.Format("机动车合格证申请数据共{0}页", MaxPageNumm);
            LogWrite(msg);
            if (!string.IsNullOrEmpty(paramPageTo))
            {
                this.PageTo = int.Parse(paramPageTo);
                if (PageTo > iMaxPageNumm)
                {
                    PageTo = iMaxPageNumm;
                }
            }
            else
            {
                this.PageTo = iMaxPageNumm;
            }

            IsStopped = true;
            try
            {
                string dTime = string.Empty;

                if (IsStopped)
                {
                    Thread thAutoCrawler;

                    thAutoCrawler = new Thread(new ThreadStart(AutoCrawlerData));  //获取线程
                    thAutoCrawler.IsBackground = true;
                    thAutoCrawler.Start();
                }

            }
            catch (System.Exception ex)
            {
                msg = DateTime.Now.ToString("G") + " 自动抓取机动车合格证申请数据失败：" + ex.Message;
                LogWrite(msg);
            }
        }
        //开始监控：根据页数LIST抓取页面，并且单条数据插入
        public void Start(string ids)
        {
            string msg = string.Empty;
            IsStopped = true;
            //获取最大页数
            string MaxPageNumm = Tool.GetHtmlSourceListMaxPageNum(Tool.strTargerUrlListHGZ + "1");
            int iMaxPageNumm = int.Parse(MaxPageNumm);
            msg = string.Format("机动车合格证申请数据共{0}页", MaxPageNumm);
            LogWrite(msg);

            IsStopped = true;
            try
            {
                ParameterizedThreadStart start = new ParameterizedThreadStart(AutoCrawlerDataOne);
                string dTime = string.Empty;

                string[] dTimes = ids.Split(',');
                int iMinPageNum = int.Parse(dTimes[0]);
                int iMaxPageNum = int.Parse(dTimes[dTimes.Length - 1]);
                int iPage;
                for (int i = 0; i < dTimes.Length; i++)
                {
                    iPage = int.Parse(dTimes[i]);
                    if (iPage > iMaxPageNumm)
                    {
                        continue;
                    }
                    if (IsStopped)
                    {
                        Thread thread = new Thread(start);
                        thread.Start(iPage.ToString());
                    }
                }

            }
            catch (System.Exception ex)
            {
                msg = DateTime.Now.ToString("G") + " 自动抓取机动车合格证申请数据失败：" + ex.Message;
                LogWrite(msg);
            }
        }
        //停止监控
        public void Stop()
        {
            IsStopped = false;

            LogWrite(DateTime.Now.ToString("G") + "  停止抓取机动车合格证申请数据");
        }

        /// <summary>
        /// 获取资源编录列表-机动车合格证申请界面
        /// </summary>
        /// <param name="strLoginUrl"></param>
        /// <param name="strTargerUrl2"></param>
        public DataTable GetHtmlSourceListHGZ(string strTargerUrl, String page)
        {
            DataTable data = new DataTable();
            string strContent = Tool.ReadHTML(strTargerUrl);
            if (!string.IsNullOrEmpty(strContent))
            {
                ConvertHGZ convertHGZ = new ConvertHGZ();
                var dataList = convertHGZ.getListHGZ(strContent, page);
                //按时间返回信息
                if (!string.IsNullOrEmpty(param))
                {
                    data = dataList.Clone();
                    DataRow[] drs = dataList.Select(param);
                    if (drs != null && drs.Count() > 0)
                    {
                        data = drs.CopyToDataTable();
                    }

                }
                else
                {
                    data = dataList;
                }
            }
            return data;
        }

        /// <summary>
        /// 插入数据库
        /// </summary>
        /// <param name="strLoginUrl"></param>
        /// <param name="strTargerUrl"></param>
        /// <param name="data">列表数据</param>
        public void InsertDataHGZ(string strTargerUrl, DataTable data, int page)
        {
            LogWrite(string.Format("{0} 正在抓取合格证详细信息第{1}页，共{2}条数据", DateTime.Now.ToString("G"), (page).ToString(), data.Rows.Count.ToString()));

            string strContex = string.Empty;
            string msg = string.Empty;
            int dataCount = data.Rows.Count;
            InsertHGZ insertHGZ = new InsertHGZ();
            ConvertHGZ convertHGZ = new ConvertHGZ();
            insertHGZ.InsertListHGZ(data);
            DataTable dtInsert = new DataTable();
            int i = 0;
            try
            {
                for (i = 0; i < dataCount; i++)
                {
                    if (IsStopped)
                    {
                        string id = data.Rows[i]["SQBH"].ToString().Trim();
                        string app_time = data.Rows[i]["APP_TIME"].ToString().Trim();
                        string app_type = data.Rows[i]["APP_TYPE"].ToString().Trim();

                        string strContent = Tool.ReadHTML(strTargerUrl + id);
                        if (!string.IsNullOrEmpty(strContent))
                        {
                            var dataDetails = convertHGZ.getDetailsHGZ(app_time, app_type, strContent);
                            //try
                            //{
                            //    insertHGZ.InsertDBHGZ(dataDetails);
                            //}
                            //catch (ArgumentException ex)
                            //{
                            //    msg = string.Format("{0} 正在抓取第{1}页，插入合格证详细信息数据时出错：i={2},id={3},{4}", DateTime.Now.ToString("G"), page.ToString(), i.ToString(),id, ex.InnerException.Message);
                            //    LogWrite(msg);
                            //}
                            if (i == 0)
                            {
                                dtInsert = dataDetails;
                            }
                            else
                            {
                                DataRow dr = dataDetails.Rows[0];
                                dtInsert.Rows.Add(dr.ItemArray);
                            }
                        }
                    }
                }
                if (IsStopped)
                {
                    insertHGZ.InsertDBHGZ(dtInsert);
                    LogWrite(string.Format("{0} 插入合格证详细信息第{1}页，共{2}条数据", DateTime.Now.ToString("G"), (page).ToString(), dtInsert.Rows.Count.ToString()));
                }

            }
            catch (ArgumentException ex)
            {
                msg = string.Format("{0} 正在抓取第{1}页，插入合格证详细信息数据时出错：i={2},{3}", DateTime.Now.ToString("G"), page.ToString(), i.ToString(), ex.InnerException.Message);
                LogWrite(msg);
            }
            //msg = string.Format("{0} 正在抓取第{1}页，插入详细信息{2}条数据", DateTime.Now.ToString("G"), page.ToString(), dataCount.ToString());
            //LogWrite(msg);
        }

        //写入界面和日志
        private void LogWrite(string Message)
        {
            logMgr.WriteToFile(Message);
        }

        /// <summary>
        /// 按照指定的页数的list抓取数据：
        /// </summary>
        /// <param name="dTime">页数的list</param>
        private void AutoCrawlerData()
        {
            string msg = string.Empty;
            DataTable data = new DataTable();

            int page = 1;
            for (page = PageFrom; page <= PageTo; page++)
            {
                try
                {
                    if (IsStopped)
                    {
                        // 资源编录列表-机动车合格证申请界面
                        data = GetHtmlSourceListHGZ(Tool.strTargerUrlListHGZ + page.ToString(), page.ToString());
                        msg = string.Format("{0} 机动车合格证申请 正在抓取第{1}页，该页面共{2}条数据", DateTime.Now.ToString("G"), page.ToString(), data.Rows.Count);
                        LogWrite(msg);
                        //插入数据库
                        if (data != null && data.Rows.Count > 0)
                        {
                            InsertDataHGZ(Tool.strTargerUrlDetailsHGZ, data, page);
                        }
                    }
                    else
                    {
                        msg = DateTime.Now.ToString("G") + "  停止抓取机动车合格证申请数据";
                        LogWrite(msg);
                        //Idleupdate.Release();//释放一个资源
                        return;
                    }
                }
                catch (System.Exception ex)
                {
                    msg = string.Format("{0} 自动抓取机动车合格证申请数据失败:{1}", DateTime.Now.ToString("G"), ex.Message);

                    LogWrite(msg);
                }
                //iThreadRuned++;//记录插入的页数
                //if (iThreadRuned+PageFrom>=PageTo)
                //{
                //    msg = DateTime.Now.ToString("G") + "  机动车合格证申请数据抓取结束";
                //    LogWrite(msg);
                //}
                //Idleupdate.Release();//释放一个资源
            }
        }

        /// <summary>
        /// 按照指定的页数的list抓取数据：
        /// </summary>
        /// <param name="dTime">页数的list</param>
        private void AutoCrawlerDataOne(object oPage)
        {
            //Control.CheckForIllegalCrossThreadCalls = false;
            //线程等待信号
            //Idleupdate.WaitOne();

            string msg = string.Empty;
            DataTable data = new DataTable();

            int page = int.Parse(oPage.ToString());

            try
            {
                if (IsStopped)
                {
                    // 资源编录列表-机动车合格证申请界面
                    data = GetHtmlSourceListHGZ(Tool.strTargerUrlListHGZ + page.ToString(), page.ToString());

                    msg = string.Format("{0} 机动车合格证申请 正在抓取第{1}页，该页面共{2}条数据", DateTime.Now.ToString("G"), page.ToString(), data.Rows.Count);
                    LogWrite(msg);
                    //插入数据库
                    if (data != null && data.Rows.Count > 0)
                    {
                        InsertDataHGZ(Tool.strTargerUrlDetailsHGZ, data, page);
                    }
                }
                else
                {
                    msg = DateTime.Now.ToString("G") + "  停止抓取机动车合格证申请数据";
                    LogWrite(msg);
                    //Idleupdate.Release();//释放一个资源
                    return;
                }
            }
            catch (System.Exception ex)
            {
                msg = string.Format("{0} 自动抓取机动车合格证申请数据失败:{1}", DateTime.Now.ToString("G"), ex.Message);

                LogWrite(msg);
            }
            //iThreadRuned++;//记录插入的页数
            //if (iThreadRuned+PageFrom>=PageTo)
            //{
            //    msg = DateTime.Now.ToString("G") + "  机动车合格证申请数据抓取结束";
            //    LogWrite(msg);
            //}
            //Idleupdate.Release();//释放一个资源
        }


    }
}
