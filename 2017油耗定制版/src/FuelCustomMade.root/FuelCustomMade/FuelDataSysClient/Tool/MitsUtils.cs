﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using DevExpress.XtraGrid.Views.Grid;
using FuelDataSysClient.Properties;
using System.Globalization;
using System.Linq;
using FuelDataSysClient.Tool;

namespace FuelDataSysClient.Tool
{
    public enum Status
    {
        已上报 = 0,
        待上报 = 1,
        修改待上报 = 2,
        撤销待上报 = 3,
        未被激活 = 9
        //（9：未被激活（数据通过excel导入但未被激活）；0：已上传；1：没上传；2：修改没上传；3：撤销未上传）
    }

    public class MitsUtils
    {
        public static string CTNY = "传统能源";
        public static string FCDSHHDL = "非插电式混合动力";
        public static string CDSHHDL = "插电式混合动力";
        public static string CDD = "纯电动";
        public static string RLDC = "燃料电池";

        public static Dictionary<string, string> dictRllx = new Dictionary<string,string>();

        private const string VIN = "VIN";
        private List<string> PARAMFLOAT1 = new List<string>() { "CT_EDGL", "CT_JGL", "CT_SJGKRLXHL", "CT_SQGKRLXHL", "CT_ZHGKRLXHL", "FCDS_HHDL_DLXDCZZNL", "FCDS_HHDL_ZHGKRLXHL", "FCDS_HHDL_EDGL", "FCDS_HHDL_JGL", "FCDS_HHDL_SJGKRLXHL", "FCDS_HHDL_SQGKRLXHL", "FCDS_HHDL_QDDJEDGL", "CDS_HHDL_DLXDCZZNL", "FCDS_HHDL_DLXDCBNL", "CDS_HHDL_ZHGKRLXHL", "CDS_HHDL_QDDJEDGL", "CDS_HHDL_EDGL", "CDS_HHDL_JGL", "CDD_DLXDCZEDNL", "CDD_QDDJEDGL", "RLDC_DDGLMD", "RLDC_ZHGKHQL", "RLDC_QDDJEDGL" };
        private List<string> PARAMFLOAT2 = new List<string>() { "CDS_HHDL_HHDLZDDGLB", "FCDS_HHDL_HHDLZDDGLB" };

        private string strCon = AccessHelper.conn;
        public DataTable checkData = new DataTable();
        public Dictionary<string, string> dictCTNY;  //存放列头转换模板(传统能源)
        public Dictionary<string, string> dictFCDSHHDL;  //存放列头转换模板（非插电式混合动力）
        public Dictionary<string, string> dictCDSHHDL;  //存放列头转换模板（插电式混合动力）
        public Dictionary<string, string> dictCDD;  //存放列头转换模板（纯电动）
        public Dictionary<string, string> dictRLDC;  //存放列头转换模板（燃料电池）
        public Dictionary<string, string> dictVin; //存放列头转换模板（VIN）

        DataTable dtCtnyStatic;
        //DataTable dtFcdsStatic;
        public Dictionary<string, DataTable> dsMainStatic = new Dictionary<string,DataTable>();
        DataTable excelDT;

        private List<string> listHoliday; // 节假日数据

        string path = Application.StartupPath + Settings.Default["ExcelHeaderTemplate"];
        private static NameValueCollection FILE_NAME = (NameValueCollection)ConfigurationManager.GetSection("fileName");

        static MitsUtils()
        {
            dictRllx.Add("CTNY", CTNY);
            dictRllx.Add("FCDS", FCDSHHDL);
            dictRllx.Add("CDS", CDSHHDL);
            dictRllx.Add("CDD", CDD);
            dictRllx.Add("RLDC", RLDC);
        }

        public MitsUtils()
        {
            checkData = GetCheckData();    //获取参数数据  RLLX_PARAM  

            excelDT = this.ReadExcel(path, "").Tables[0]; //读取表头转置模板

            ReadTemplate(path);   //读取表头转置模板
        }

        // VIN excel文件名称的开头
        private string vinFileName = FILE_NAME["VIN"].ToString();

        public string VinFileName
        {
            get { return vinFileName; }
        }

        // 主表Excel文件名称的开头
        private string mainFileName =  FILE_NAME["MAIN"].ToString();

        public string MainFileName
        {
            get { return mainFileName; }
        }

        /// <summary>
        /// 获取路径folderPath下所有以fileMark开头的文件
        /// </summary>
        /// <param name="folderPath">文件夹路径</param>
        /// <param name="fileMark">文件名字的开头字符</param>
        /// <returns></returns>
        public List<string> GetFileName(string folderPath, string fileMark)
        {
            DirectoryInfo folder = new DirectoryInfo(folderPath);
            List<string> fileNameList = new List<string>();
            foreach (FileInfo file in folder.GetFiles(fileMark))
            {
                fileNameList.Add(file.FullName);
            }
            return fileNameList;
        }

        /// <summary>
        /// 读取VIN信息
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public DataSet ReadVinCsv(bool HeadYes, char span, string fileName)
        {
            DataSet ds = new DataSet();
            DataTable dt = new DataTable();

            StreamReader fileReader = new StreamReader(fileName, Encoding.UTF8);
            try
            {
                //是否为第一行（如果HeadYes为TRUE，则第一行为标题行）
                int lsi = 0;

                //列之间的分隔符
                char cv = span;
                while (fileReader.EndOfStream == false)
                {
                    string line = fileReader.ReadLine();
                    string[] y = line.Split(cv);
                    if (y.Length == 4) continue;
                    //第一行为标题行
                    if (HeadYes == true)
                    {
                        //第一行
                        if (lsi == 0)
                        {
                            for (int i = 0; i < y.Length; i++)
                            {
                                dt.Columns.Add(s2s(y[i].Trim().ToString()));
                            }
                            lsi++;
                        }
                        //从第二列开始为数据列
                        else
                        {
                            DataRow dr = dt.NewRow();
                            for (int i = 0; i < y.Length; i++)
                            {
                                dr[i] = y[i].Trim();
                            }
                            dt.Rows.Add(dr);
                        }
                    }
                    //第一行不为标题行
                    else
                    {
                        if (lsi == 0)
                        {
                            for (int i = 0; i < y.Length; i++)
                            {
                                dt.Columns.Add("Col" + i.ToString());
                            }
                            lsi++;
                        }
                        DataRow dr = dt.NewRow();
                        for (int i = 0; i < y.Length; i++)
                        {
                            dr[i] = y[i].Trim();
                        }
                        dt.Rows.Add(dr);
                    }
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                fileReader.Close();
                fileReader.Dispose();
            }
            ds.Tables.Add(dt);

            return ds;
        }

        /// <summary>
        /// 转移已用完的文件
        /// </summary>
        /// <param name="srcFileName">源文件路径</param>
        /// <param name="folderPath">目的文件夹路径</param>
        /// <param name="fileType">文件类型</param>
        public string MoveFinishedFile(string srcFileName, string folderPath, string fileType)
        {
            string msg = string.Empty;
            string folderName = Path.Combine(folderPath, FILE_NAME[fileType].ToString());

            try
            {
                if (!Directory.Exists(folderName))
                {
                    Directory.CreateDirectory(folderName);
                }
                string shortFileName = Path.GetFileNameWithoutExtension(srcFileName) + DateTime.Now.ToString("yyyyMMddHHmmss") + Path.GetExtension(srcFileName);
                string desFileName = Path.Combine(folderName, shortFileName);

                File.Move(srcFileName, desFileName);
            }
            catch (Exception ex)
            {
                msg = ex.Message + "\r\n";
            }
            return msg;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="mainId"></param>
        /// <param name="paramId"></param>
        /// <param name="paramName"></param>
        /// <param name="IsExist"></param>
        /// <returns></returns>
        public DataRow GetDivideMain(DataTable dt, string vin, string clxh, string paramName, ref string message)
        {
            foreach (DataRow dr in dt.Rows)
            {
                if (clxh==Convert.ToString(dr["CLXH"]).Trim())
                {
                    message += "";
                    return dr;
                }
            }
            switch (paramName)
            {
                case "传统能源":
                    message += string.Format("\r\n{0}: 对应车型参数“{1}”不存在", vin, clxh);
                    break;
                default: break;
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="mainId"></param>
        /// <param name="paramId"></param>
        /// <param name="paramName"></param>
        /// <param name="IsExist"></param>
        /// <returns></returns>
        public DataRow GetDivideMain(DataTable dt, string vin, string clxh, string uniqueCode, string paramName, ref string message)
        {
            foreach (DataRow dr in dt.Rows)
            {
                if (uniqueCode == Convert.ToString(dr["UNIQUE_CODE"]).Trim())
                {
                    message += "";
                    return dr;
                }
            }
            //switch (paramName)
            //{
            //    case "传统能源":
                    message += string.Format("\r\n{0}: 对应车型参数“{1}”不存在", vin, uniqueCode);
            //        break;
            //    default: break;
            //}
            return null;
        }

        /// <summary>
        /// 保存VIN信息
        /// </summary>
        /// <param name="ds"></param>
        public string SaveVinInfo(DataTable dt)
        {
            int succFuelCount = 0; //生成油耗数据的数量
            int succImCount = 0;   //成功导入的数量
            int failCount = 0;  //导入失败的数量
            int totalCount = dt.Rows.Count;
            string msg = string.Empty;

            ProcessForm pf = new ProcessForm();
            try
            {
                DataTable dtCtnyPam = this.GetRllxData(CTNY);
                DataTable dtFcdsPam = this.GetRllxData(FCDSHHDL);
                DataTable dtCdsPam = this.GetRllxData(CDSHHDL);
                DataTable dtCddPam = this.GetRllxData(CDD);
                DataTable dtRldcPam = this.GetRllxData(RLDC);

                Dictionary<string, DataTable> dicDtPam = new Dictionary<string, DataTable>();
                dicDtPam.Add(CTNY, dtCtnyPam);
                dicDtPam.Add(FCDSHHDL, dtFcdsPam);
                dicDtPam.Add(CDSHHDL, dtCdsPam);
                dicDtPam.Add(CDD, dtCddPam);
                dicDtPam.Add(RLDC, dtRldcPam);

                // 获取节假日数据
                listHoliday = this.GetHoliday();

                // 显示进度条
                pf.Show();
                int pageSize = 1;
                int totalVin = totalCount;
                int count = 0;

                pf.TotalMax = (int)Math.Ceiling((decimal)totalVin / (decimal)pageSize);
                pf.ShowProcessBar();

                foreach (DataRow drVin in dt.Rows)
                {
                    count++;
                    string vin = drVin["VIN"] == null ? "" : drVin["VIN"].ToString().Trim();
                    string clxh = drVin["CLXH"] == null ? "" : drVin["CLXH"].ToString().Trim();
                    string rllx = drVin["RLLX"] == null ? "" : drVin["RLLX"].ToString().Trim();
                    string uniqueCode = drVin["UNIQUE_CODE"] == null ? "" : drVin["UNIQUE_CODE"].ToString().Trim();
                    if (!string.IsNullOrEmpty(uniqueCode))
                    {
                        string vinMsg = this.VerifyVinData(drVin);

                        if (string.IsNullOrEmpty(vinMsg))
                        {
                            string ctnyMsg = string.Empty;

                            //DataRow drCtny = this.GetDivideMain(dtCtnyStatic, vin, clxh, CTNY, ref ctnyMsg);
                            DataRow drCtny = this.GetDivideMain(dsMainStatic[rllx], vin, clxh, uniqueCode, rllx, ref ctnyMsg);

                            if (!string.IsNullOrEmpty(ctnyMsg))
                            {
                                vinMsg += ctnyMsg;
                            }
                            
                            if (string.IsNullOrEmpty(vinMsg))
                            {
                                if (string.IsNullOrEmpty(ctnyMsg))
                                {
                                    vinMsg += this.SaveReadyData(drVin, drCtny, dicDtPam[rllx]);
                                    if (string.IsNullOrEmpty(vinMsg))
                                    {
                                        succFuelCount++; //统计导入并生成油耗数据的VIN数量
                                    }
                                }
                            }
                            else
                            {
                                string saveMsg = string.Empty;
                                vinMsg += saveMsg = this.SaveVinBak(drVin);
                                if (string.IsNullOrEmpty(saveMsg))
                                {
                                    succImCount++;
                                }
                            }
                        }
                        msg += vinMsg;

                        //if (count % 20 == 0 || count == totalCount)
                        //{
                            pf.progressBarControl1.PerformStep();
                            Application.DoEvents();
                        //}
                    }
                }
            }
            catch (Exception ex)
            {
                msg = ex.Message + "\r\n";
            }
            finally
            {
                if (pf != null)
                {
                    pf.Close();
                }
            }

            failCount = totalCount - succFuelCount - succImCount;

            if (failCount > 0)
            {
                msg += "FAILED-IMPORT";
            }

            string msgSummary = string.Format("共{0}条数据：\r\n \t{1}条导入成功（其中{2}条生成油耗数据成功；{3}条生成耗数据失败） \r\n \t{4}条导入失败\r\n",
                                totalCount, succFuelCount + succImCount, succFuelCount, succImCount, failCount);
            msg = msgSummary + msg;

            return msg;
        }

        /// <summary>
        /// 保存没有生成燃料数据的VIN
        /// </summary>
        /// <param name="drVin"></param>
        /// <returns></returns>
        public string SaveVinBak(DataRow drVin)
        {
            string genMsg = string.Empty;
            string vin = drVin["VIN"].ToString().Trim();
            string strCon = AccessHelper.conn;
            OleDbTransaction tra = null; //创建事务，开始执行事务

            try
            {
                using (OleDbConnection con = new OleDbConnection(strCon))
                {
                    con.Open();

                    tra = con.BeginTransaction();

                    OleDbParameter creTime = new OleDbParameter("@CREATETIME", DateTime.Today);
                    creTime.OleDbType = OleDbType.DBDate;
                    DateTime clzzrqDate;

                    try
                    {
                        clzzrqDate = DateTime.ParseExact(drVin["CLZZRQ"].ToString().Trim(), "yyyyMMdd", System.Globalization.CultureInfo.CurrentCulture);
                    }
                    catch (Exception)
                    {
                        clzzrqDate = Convert.ToDateTime(drVin["CLZZRQ"]);
                    }
                    
                    OleDbParameter clzzrq = new OleDbParameter("@CLZZRQ", clzzrqDate);
                    clzzrq.OleDbType = OleDbType.DBDate;

                    #region 保存VIN信息备用

                    string sqlDel = "DELETE FROM VIN_INFO WHERE VIN = '" + vin + "'";
                    AccessHelper.ExecuteNonQuery(tra, sqlDel, null);

                    string sqlStr = @"INSERT INTO VIN_INFO(VIN,CLXH,CLZZRQ,STATUS,CREATETIME,RLLX,UNIQUE_CODE) Values (@VIN, @CLXH,@CLZZRQ,@STATUS,@CREATETIME,@RLLX,@UNIQUE_CODE)";
                    OleDbParameter[] vinParamList = { 
                                         new OleDbParameter("@VIN",vin),
                                         new OleDbParameter("@CLXH",drVin["CLXH"].ToString().Trim()),
                                         clzzrq,
                                         new OleDbParameter("@STATUS","1"),
                                         creTime,
                                         new OleDbParameter("@RLLX",drVin["RLLX"].ToString().Trim()),
                                         new OleDbParameter("@UNIQUE_CODE",drVin["UNIQUE_CODE"].ToString().Trim())
                                      };
                    AccessHelper.ExecuteNonQuery(tra, sqlStr, vinParamList);

                    tra.Commit();
                    #endregion
                }
            }
            catch (Exception ex)
            {
                genMsg += ex.Message + "\r\n";
                tra.Rollback();
            }
            finally
            {
            }

            return genMsg;
        }

        /// <summary>
        /// 导入VIN信息
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="folderName"></param>
        /// <returns></returns>
        public string ImportVinData(string fileName, string folderName)
        {
            string rtnMsg = string.Empty;
            DataSet ds = this.ReadExcel(fileName, "");
   
            if (ds != null)
            {
                DataTable daVin = D2D(dictVin, ds.Tables[0], VIN);
                rtnMsg += this.SaveVinInfo(daVin);

                if (rtnMsg.ToUpper().IndexOf("FAILED-IMPORT") < 0)
                {
                    rtnMsg += this.MoveFinishedFile(fileName, folderName, "F_VIN");
                }

                if (!string.IsNullOrEmpty(rtnMsg))
                {
                    rtnMsg = Path.GetFileName(fileName) + ":\r\n" + rtnMsg + "\r\n";
                }
                else
                {
                    rtnMsg = Path.GetFileName(fileName) + ":\r\n导入成功\r\n";
                }
            }
            else
            {
                rtnMsg = fileName + "中没有数据或数据格式错误\r\n";
            }

            return rtnMsg;
        }


        #region ImportVinData  方法 
        //string rtnMsg = string.Empty;
        //    DataSet ds = this.ReadVinCsv(false, ',', fileName);  //读取CSV
        //    if (ds != null && ds.Tables[0].Rows.Count > 0)
        //    {
        //        DataTable dt = DataFormat(ds.Tables[0]);         //格式化数据
        //        rtnMsg += this.SaveData(ConvertTableHeader(dt));


        //        rtnMsg += this.MoveFinishedFile(fileName, folderName, "F_VIN");

        //        if (!string.IsNullOrEmpty(rtnMsg))
        //        {
        //            rtnMsg = Path.GetFileName(fileName) + ":\r\n" + rtnMsg + "\r\n";
        //        }
        //        else
        //        {
        //            rtnMsg = Path.GetFileName(fileName) + ":\r\n导入成功\r\n";
        //        }
        //    }
        //    else
        //    {
        //        rtnMsg = fileName + "中没有数据或数据格式错误\r\n";
        //    }

        //    return rtnMsg;
        #endregion

        #region 转换表头

        /// <summary>
        /// 转换表头
        /// </summary>
        /// <param name="dt">datatable</param>
        /// <returns></returns>
        private DataTable ConvertTableHeader(DataTable dt)
        {
            foreach (DataColumn dc in dt.Columns)
            {
                foreach (DataRow r in excelDT.Rows)
                {
                    if (dc.ColumnName == Convert.ToString(r[0]))
                    {
                        dc.ColumnName = Convert.ToString(r[1]);
                        break;
                    }
                }
            }


            return dt;
        }

        #endregion

        #region 格式化数据

        /// <summary>
        /// 把TABLE参数数据和明细数据格式化成一条 
        /// </summary>
        /// <param name="dataSource">数据源</param>
        /// <returns></returns>
        private DataTable DataFormat(DataTable dataSource)
        {
            DataTable newDt = new DataTable();
            int rowNumber = 0;
            try
            {
                newDt = dataSource.Clone();

                DataRow newdr = null;
                for (int i = 0; i < dataSource.Rows.Count; i++)
                {
                    if ("P" == Convert.ToString(dataSource.Rows[i][0]))   //保留参数 数据
                    {
                        newdr = dataSource.Rows[i];
                        rowNumber++;
                    }
                    else if ("C" == Convert.ToString(dataSource.Rows[i][0]))
                    {
                        newDt.Rows.Add(dataSource.Rows[i].ItemArray);  //插入保留参数
                        for (int j = 0; j < dataSource.Columns.Count - 1; j++)  //循环插入明细
                        {
                            if (j == dataSource.Columns.Count - 10) break;
                            newDt.Rows[i - rowNumber][j + 10] = Convert.ToString(newdr[j]);
                        }
                    }
                }
            }
            catch { newDt = null; }
            return newDt;
        }

        #endregion

        #region 保存数据
        private string SaveData(DataTable dt)
        {
            DataTable dtCtnyPam = this.GetRllxData("传统能源");
            string msg = string.Empty;
            string strCon = AccessHelper.conn;
            OleDbConnection con = new OleDbConnection(strCon);
            con.Open();
            OleDbTransaction tra = con.BeginTransaction(); //创建事务，开始执行事务

            try
            {
                DataRow[] drCtny = checkData.Select("FUEL_TYPE='" + CTNY + "' and STATUS=1");
                if (dt != null && dt.Rows.Count > 0)
                {
                    string error = string.Empty;
                    foreach (DataRow dr in dt.Rows)
                    {

                        string vin = dr["VIN"].ToString().Trim().ToUpper();

                        // 如果当前vin数据已经存在，则跳过
                        if (this.IsFuelDataExist(vin))
                        {
                            msg += vin + "已经存在。\r\n";
                            continue;
                        }

                        error = VerifyData(dr, drCtny, "IMPORT");      //单行验证
                        if (!string.IsNullOrEmpty(error))
                        {
                            msg += error;
                        }
                        else
                        {
                            #region 插入主表
                            string sqlInsertBasic = @"INSERT INTO FC_CLJBXX
                                (   VIN,USER_ID,QCSCQY,JKQCZJXS,CLZZRQ,UPLOADDEADLINE,CLXH,CLZL,
                                    RLLX,ZCZBZL,ZGCS,LTGG,ZJ,
                                    TYMC,YYC,ZWPS,ZDSJZZL,EDZK,LJ,
                                    QDXS,JYJGMC,JYBGBH,HGSPBM,QTXX,STATUS,CREATETIME,UPDATETIME
                                ) VALUES
                                (   @VIN,@USER_ID,@QCSCQY,@JKQCZJXS,@CLZZRQ,@UPLOADDEADLINE,@CLXH,@CLZL,
                                    @RLLX,@ZCZBZL,@ZGCS,@LTGG,@ZJ,
                                    @TYMC,@YYC,@ZWPS,@ZDSJZZL,@EDZK,@LJ,
                                    @QDXS,@JYJGMC,@JYBGBH,@HGSPBM,@QTXX,@STATUS,@CREATETIME,@UPDATETIME)";

                            DateTime clzzrqDate = Convert.ToDateTime(dr["CLZZRQ"].ToString().Trim());
                            OleDbParameter clzzrq = new OleDbParameter("@CLZZRQ", clzzrqDate);
                            clzzrq.OleDbType = OleDbType.DBDate;

                            DateTime uploadDeadlineDate = this.QueryUploadDeadLine(clzzrqDate);
                            OleDbParameter uploadDeadline = new OleDbParameter("@UPLOADDEADLINE", uploadDeadlineDate);
                            uploadDeadline.OleDbType = OleDbType.DBDate;

                            OleDbParameter creTime = new OleDbParameter("@CREATETIME", DateTime.Now);
                            creTime.OleDbType = OleDbType.DBDate;
                            OleDbParameter upTime = new OleDbParameter("@UPDATETIME", DateTime.Now);
                            upTime.OleDbType = OleDbType.DBDate;

                            OleDbParameter[] param = { 
                                     new OleDbParameter("@VIN",vin),
                                     new OleDbParameter("@USER_ID",Utils.userId),
                                     new OleDbParameter("@QCSCQY",dr["QCSCQY"].ToString().Trim()),
                                     new OleDbParameter("@JKQCZJXS",dr["JKQCZJXS"].ToString().Trim()),
                                     clzzrq,
                                     uploadDeadline,
                                     new OleDbParameter("@CLXH",dr["CLXH"].ToString().Trim()),
                                     new OleDbParameter("@CLZL",dr["CLZL"].ToString().Trim()),
                                     new OleDbParameter("@RLLX",dr["RLLX"].ToString().Trim()),
                                     new OleDbParameter("@ZCZBZL",dr["ZCZBZL"].ToString().Trim()),
                                     new OleDbParameter("@ZGCS",dr["ZGCS"].ToString().Trim()),
                                     new OleDbParameter("@LTGG",dr["LTGG"].ToString().Trim()),
                                     new OleDbParameter("@ZJ",dr["ZJ"].ToString().Trim()),
                                     new OleDbParameter("@TYMC",dr["TYMC"].ToString().Trim()),
                                     new OleDbParameter("@YYC",dr["YYC"].ToString().Trim()=="1"?"是":"否"),
                                     new OleDbParameter("@ZWPS",dr["ZWPS"].ToString().Trim()),
                                     new OleDbParameter("@ZDSJZZL",dr["ZDSJZZL"].ToString().Trim()),
                                     new OleDbParameter("@EDZK",dr["EDZK"].ToString().Trim()),
                                     new OleDbParameter("@LJ",dr["LJ"].ToString().Trim()),
                                     new OleDbParameter("@QDXS",dr["QDXS"].ToString().Trim()),
                                     new OleDbParameter("@JYJGMC",dr["JYJGMC"].ToString().Trim()),
                                     new OleDbParameter("@JYBGBH",dr["JYBGBH"].ToString().Trim()),
                                     new OleDbParameter("@HGSPBM",dr["HGSPBM"].ToString().Trim()),
                                     new OleDbParameter("@QTXX",dr["CT_QTXX"].ToString().Trim()),
                                     // 状态为9表示数据以导入，但未被激活，此时用来供用户修改
                                     new OleDbParameter("@STATUS","1"),
                                     creTime,
                                     upTime
                                     };
                            AccessHelper.ExecuteNonQuery(tra, sqlInsertBasic, param);

                            #endregion

                            #region 插入参数信息

                            string sqlDelParam = "DELETE FROM RLLX_PARAM_ENTITY WHERE VIN ='" + vin + "'";
                            AccessHelper.ExecuteNonQuery(tra, sqlDelParam, null);

                            // 待生成的燃料参数信息存入燃料参数表
                            foreach (DataRow drParam in dtCtnyPam.Rows)
                            {
                                string paramCode = drParam["PARAM_CODE"].ToString().Trim();
                                string sqlInsertParam = @"INSERT INTO RLLX_PARAM_ENTITY 
                                            (PARAM_CODE,VIN,PARAM_VALUE,V_ID) 
                                      VALUES
                                            (@PARAM_CODE,@VIN,@PARAM_VALUE,@V_ID)";
                                OleDbParameter[] paramList = { 
                                     new OleDbParameter("@PARAM_CODE",paramCode),
                                     new OleDbParameter("@VIN",vin),
                                     new OleDbParameter("@PARAM_VALUE",dr[paramCode]),
                                     new OleDbParameter("@V_ID","")
                                   };
                                AccessHelper.ExecuteNonQuery(tra, sqlInsertParam, paramList);
                            }
                            #endregion
                        }

                    }
                    tra.Commit();
                }
            }
            catch { tra.Rollback(); }
            finally { con.Close(); }


            return msg;
        }
        #endregion

        //得到选中数据
        private string GetUploadData(DataTable dt)
        {
            string vinStr = string.Empty;
            int count = 0;
            try
            {
                if (dt != null)
                {
                    DataRow[] drVinArr = dt.Select("check=True");

                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        if ((bool)dt.Rows[i]["check"])
                        {
                            count++;
                            vinStr += string.Format(",'{0}'", dt.Rows[i]["VIN"].ToString());
                            if (count % 50 == 0)
                            {
                                vinStr += ";";
                            }
                        }
                    }
                    //if (!string.IsNullOrEmpty(vinStr))
                    //{
                    //    vinStr = vinStr.Substring(1);
                    //}
                }
            }
            catch { }
            return vinStr;
        }

        //批量更新V_ID
        private bool UpdateV_ID(DataTable dt)
        {
            string sql = "update FC_CLJBXX set V_ID='{0}',STATUS='0' where VIN='{1}'";
            OleDbConnection con = new OleDbConnection(strCon);
            con.Open();
            OleDbTransaction tra = con.BeginTransaction(); //创建事务，开始执行事务
            try
            {

                foreach (DataRow dr in dt.Rows)
                {
                    AccessHelper.ExecuteNonQuery(tra, string.Format(sql, dr[1], dr[0]));//dr[0]:VIN;dr[1]:V_ID
                }
                tra.Commit();
                return true;
            }
            catch { tra.Rollback(); }
            finally { con.Close(); }
            return false;
        }

        FuelDataService.FuelDataSysWebService service = Utils.service;

        public bool ActionUpdate(GridView gv, DataTable dt)
        {
            bool flag = true;
            ProcessForm pf = new ProcessForm();
            pf.Text = "正在同步，请稍候";

            try
            {
                gv.PostEditor();

                string strVin = this.GetUploadData(dt);
                string[] arrVin = strVin.Split(';');

                pf.Show();

                pf.TotalMax = arrVin.Length;
                pf.ShowProcessBar();

                foreach (string vins in arrVin)
                {
                    string vin = string.Empty;
                    if (!string.IsNullOrEmpty(vins))
                    {
                        vin = vins.Substring(1);

                        DataSet tempDt = service.QueryVidByVins(Settings.Default.UserId, Settings.Default.UserPWD, vin);
                        if (tempDt != null)
                        {
                            if (tempDt.Tables[0].Rows.Count > 0)
                                flag = flag && UpdateV_ID(tempDt.Tables[0]);
                        }
                        pf.progressBarControl1.PerformStep();
                        System.Windows.Forms.Application.DoEvents();
                    }
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                if (pf != null)
                {
                    pf.Close();
                }
            }
            return flag;
        }


        /// <summary>
        /// 导入主表信息信息
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="folderName"></param>
        /// <returns></returns>
        public string ImportMainData(string fileName, string folderName, string importType, List<string> mainUpdateList)
        {
            string rtnMsg = string.Empty;
            DataSet ds = this.ReadMainExcel(fileName);
            if (ds != null)
            {
                if (importType == "IMPORT")
                {
                    // 新导入
                    rtnMsg += this.SaveMainData(ds);
                }
                else if (importType == "UPDATE")
                {
                    // 修改
                    rtnMsg += this.UpdateMainData2(ds, mainUpdateList);
                }

                if (rtnMsg.ToUpper().IndexOf("FAILED-IMPORT") < 0)
                {
                    rtnMsg += this.MoveFinishedFile(fileName, folderName, "F_MAIN");
                }
                else
                {
                    rtnMsg = Path.GetFileName(fileName) + rtnMsg + "\r\n";
                }
            }
            else
            {
                rtnMsg = fileName + "中没有数据或数据格式错误\r\n";
            }

            return rtnMsg;
        }

        /// <summary>
        /// 读主表信息
        /// </summary>
        /// <param name="fileName"></param>
        public DataSet ReadMainExcel(string fileName)
        {
            string strConn = String.Format("Provider=Microsoft.ACE.OLEDB.12.0;Data Source={0}; Extended Properties='Excel 12.0;HDR=YES;IMEX=1'", fileName); //; HDR=No
            DataSet ds = new DataSet();

            try
            {
                OleDbDataAdapter oada = new OleDbDataAdapter("select * from [传统能源$]", strConn);
                oada.Fill(ds, CTNY);

                oada = new OleDbDataAdapter("select * from [非插电式混合动力$]", strConn);
                oada.Fill(ds, FCDSHHDL);

                oada = new OleDbDataAdapter("select * from [插电式混合动力$]", strConn);
                oada.Fill(ds, CDSHHDL);

                oada = new OleDbDataAdapter("select * from [纯电动$]", strConn);
                oada.Fill(ds, CDD);

                oada = new OleDbDataAdapter("select * from [燃料电池$]", strConn);
                oada.Fill(ds, RLDC);

                //oada = new OleDbDataAdapter("select * from [非插电式混合动力$]", strConn);
                //oada.Fill(ds, FCDSHHDL);
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return ds;
        }

        /// <summary>
        /// 读表头对应关系模板
        /// </summary>
        /// <param name="fileName"></param>
        public DataSet ReadTemplateExcel(string fileName)
        {
            string strConn = String.Format("Provider=Microsoft.ACE.OLEDB.12.0;Data Source={0}; Extended Properties='Excel 12.0;HDR=YES;IMEX=1'", fileName); //; HDR=No
            DataSet ds = new DataSet();
            try
            {
                OleDbDataAdapter oada = new OleDbDataAdapter("select * from [传统能源$]", strConn);
                oada.Fill(ds, CTNY);

                oada = new OleDbDataAdapter("select * from [非插电式混合动力$]", strConn);
                oada.Fill(ds, FCDSHHDL);

                oada = new OleDbDataAdapter("select * from [插电式混合动力$]", strConn);
                oada.Fill(ds, CDSHHDL);

                oada = new OleDbDataAdapter("select * from [纯电动$]", strConn);
                oada.Fill(ds, CDD);

                oada = new OleDbDataAdapter("select * from [燃料电池$]", strConn);
                oada.Fill(ds, RLDC);

                oada = new OleDbDataAdapter("select * from [VIN$]", strConn);
                oada.Fill(ds, VIN);
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return ds;
        }

        /// <summary>
        /// 导入主表信息
        /// </summary>
        /// <param name="basicInfo"></param>
        /// <param name="ctnyInfo"></param>
        public string SaveMainData(DataSet ds)
        {
            int succCount = 0;
            int totalCount = 0;
            string msg = string.Empty;
            //string strCon = AccessHelper.conn;
            //OleDbConnection con = new OleDbConnection(strCon);
            //con.Open();
            //OleDbTransaction tra = con.BeginTransaction(); //创建事务，开始执行事务

            try
            {
                // 转换表头（用户模板中的表头转为数据库列名）
                DataTable dtCtny = D2D(dictCTNY, ds.Tables[CTNY], CTNY);
                totalCount += dtCtny.Rows.Count;
                succCount += ImpMainData(dtCtny, CTNY, ref msg);

                DataTable dtFcds = D2D(dictFCDSHHDL, ds.Tables[FCDSHHDL], FCDSHHDL);
                totalCount += dtFcds.Rows.Count;
                succCount += ImpMainData(dtFcds, FCDSHHDL, ref msg);

                DataTable dtCds = D2D(dictCDSHHDL, ds.Tables[CDSHHDL], CDSHHDL);
                totalCount += dtCds.Rows.Count;
                succCount += ImpMainData(dtCds, CDSHHDL, ref msg);

                DataTable dtCdd = D2D(dictCDD, ds.Tables[CDD], CDD);
                totalCount += dtCdd.Rows.Count;
                succCount += ImpMainData(dtCdd, CDD, ref msg);

                DataTable dtRldc = D2D(dictRLDC, ds.Tables[RLDC], RLDC);
                totalCount += dtRldc.Rows.Count;
                succCount += ImpMainData(dtRldc, RLDC, ref msg);

            }
            catch (Exception ex)
            {
                msg += ex.Message + "\r\n";
            }

            if (totalCount - succCount > 0)
            {
                msg += "FAILED-IMPORT";
            }

            string msgSummary = string.Format("共{0}条数据：\r\n \t{1}条导入成功 \r\n \t{2}条导入失败\r\n",
                            totalCount, succCount, totalCount - succCount);
            msg = msgSummary + msg;

            return msg;
        }

        public int ImpMainData(DataTable dt, string rlzl, ref string msg)
        {
            int succCount = 0;
            if(string.IsNullOrEmpty(msg))
            {
                msg = string.Empty;
            }

            try
            {
                // 转换表头（用户模板中的表头转为数据库列名）
                DataRow[] tdr = checkData.Select("FUEL_TYPE='" + rlzl + "' and STATUS=1");

                if (dt != null && dt.Rows.Count > 0)
                {
                    string error = string.Empty;
                    foreach (DataRow dr in dt.Rows)
                    {
                        error = VerifyData(dr, tdr, "IMPORT");      //单行验证
                        if (!string.IsNullOrEmpty(error))
                        {
                            msg += error;
                        }
                        else
                        {
                            if (rlzl.Equals(CTNY))
                            {
                                #region 传统能源
                                try
                                {
                                    #region insert

                                    #region old

                            //        StringBuilder strSql = new StringBuilder();
                            //        strSql.Append("insert into CTNY_MAIN(");
                            //        strSql.Append("QCSCQY,JKQCZJXS,CLXH,HGSPBM,CLZL,YYC,QDXS,ZWPS,ZCZBZL,ZDSJZZL,ZGCS,EDZK,LTGG,LJ,ZJ,RLLX,CT_BSQXS,CT_EDGL,CT_FDJXH,CT_JGL,CT_PL,CT_QCJNJS,CT_QGS,CT_QTXX,CT_SJGKRLXHL,CT_SQGKRLXHL,CT_ZHGKCO2PFL,CT_ZHGKRLXHL,CT_BSQDWS,CREATETIME,UPDATETIME,JYBGBH,JYJGMC,TYMC,UNIQUE_CODE)");
                            //        strSql.Append(" values (");
                            //        strSql.Append("@QCSCQY,@JKQCZJXS,@CLXH,@HGSPBM,@CLZL,@YYC,@QDXS,@ZWPS,@ZCZBZL,@ZDSJZZL,@ZGCS,@EDZK,@LTGG,@LJ,@ZJ,@RLLX,@CT_BSQXS,@CT_EDGL,@CT_FDJXH,@CT_JGL,@CT_PL,@CT_QCJNJS,@CT_QGS,@CT_QTXX,@CT_SJGKRLXHL,@CT_SQGKRLXHL,@CT_ZHGKCO2PFL,@CT_ZHGKRLXHL,@CT_BSQDWS,@CREATETIME,@UPDATETIME,@JYBGBH,@JYJGMC,@TYMC,@UNIQUE_CODE)");
                            //        OleDbParameter[] parameters = {											
                            //    new OleDbParameter("@QCSCQY", OleDbType.VarChar,255),					
                            //    new OleDbParameter("@JKQCZJXS", OleDbType.VarChar,255),					
                            //    new OleDbParameter("@CLXH", OleDbType.VarChar,255),					
                            //    new OleDbParameter("@HGSPBM", OleDbType.VarChar,255),					
                            //    new OleDbParameter("@CLZL", OleDbType.VarChar,255),					
                            //    new OleDbParameter("@YYC", OleDbType.VarChar,255),					
                            //    new OleDbParameter("@QDXS", OleDbType.VarChar,255),					
                            //    new OleDbParameter("@ZWPS", OleDbType.VarChar,255),					
                            //    new OleDbParameter("@ZCZBZL", OleDbType.VarChar,255),					
                            //    new OleDbParameter("@ZDSJZZL", OleDbType.VarChar,255),					
                            //    new OleDbParameter("@ZGCS", OleDbType.VarChar,255),					
                            //    new OleDbParameter("@EDZK", OleDbType.VarChar,255),					
                            //    new OleDbParameter("@LTGG", OleDbType.VarChar,200),					
                            //    new OleDbParameter("@LJ", OleDbType.VarChar,255),					
                            //    new OleDbParameter("@ZJ", OleDbType.VarChar,255),					
                            //    new OleDbParameter("@RLLX", OleDbType.VarChar,200),					
                            //    new OleDbParameter("@CT_BSQXS", OleDbType.VarChar,255),					
                            //    new OleDbParameter("@CT_EDGL", OleDbType.VarChar,255),					
                            //    new OleDbParameter("@CT_FDJXH", OleDbType.VarChar,255),					
                            //    new OleDbParameter("@CT_JGL", OleDbType.VarChar,255),					
                            //    new OleDbParameter("@CT_PL", OleDbType.VarChar,255),					
                            //    new OleDbParameter("@CT_QCJNJS", OleDbType.VarChar,255),					
                            //    new OleDbParameter("@CT_QGS", OleDbType.VarChar,255),					
                            //    new OleDbParameter("@CT_QTXX", OleDbType.VarChar,255),					
                            //    new OleDbParameter("@CT_SJGKRLXHL", OleDbType.VarChar,255),					
                            //    new OleDbParameter("@CT_SQGKRLXHL", OleDbType.VarChar,255),					
                            //    new OleDbParameter("@CT_ZHGKCO2PFL", OleDbType.VarChar,255),					
                            //    new OleDbParameter("@CT_ZHGKRLXHL", OleDbType.VarChar,255),					
                            //    new OleDbParameter("@CT_BSQDWS", OleDbType.VarChar,255),
                            //    new OleDbParameter("@CREATETIME", OleDbType.Date),
                            //    new OleDbParameter("@UPDATETIME", OleDbType.Date),
                            //    new OleDbParameter("@JYBGBH,", OleDbType.VarChar,255),
                            //    new OleDbParameter("@JYJGMC", OleDbType.VarChar,255),
                            //    new OleDbParameter("@TYMC", OleDbType.VarChar,255),          
                            //    new OleDbParameter("@UNIQUE_CODE", OleDbType.VarChar,255)
                            //};

                            //        parameters[0].Value = dr["QCSCQY"];
                            //        parameters[1].Value = dr["JKQCZJXS"];
                            //        parameters[2].Value = dr["CLXH"];
                            //        parameters[3].Value = dr["HGSPBM"];
                            //        parameters[4].Value = dr["CLZL"];
                            //        parameters[5].Value = dr["YYC"];
                            //        parameters[6].Value = dr["QDXS"];
                            //        parameters[7].Value = dr["ZWPS"];
                            //        parameters[8].Value = dr["ZCZBZL"];
                            //        parameters[9].Value = dr["ZDSJZZL"];
                            //        parameters[10].Value = dr["ZGCS"];
                            //        parameters[11].Value = dr["EDZK"];
                            //        parameters[12].Value = dr["LTGG"];
                            //        parameters[13].Value = dr["LJ"];
                            //        parameters[14].Value = dr["ZJ"];
                            //        parameters[15].Value = dr["RLLX"];
                            //        parameters[16].Value = dr["CT_BSQXS"];
                            //        parameters[17].Value = dr["CT_EDGL"];
                            //        parameters[18].Value = dr["CT_FDJXH"];
                            //        parameters[19].Value = dr["CT_JGL"];
                            //        parameters[20].Value = dr["CT_PL"];
                            //        parameters[21].Value = dr["CT_QCJNJS"];
                            //        parameters[22].Value = dr["CT_QGS"];
                            //        parameters[23].Value = dr["CT_QTXX"];
                            //        parameters[24].Value = dr["CT_SJGKRLXHL"];
                            //        parameters[25].Value = dr["CT_SQGKRLXHL"];
                            //        parameters[26].Value = dr["CT_ZHGKCO2PFL"];
                            //        parameters[27].Value = dr["CT_ZHGKRLXHL"];
                            //        parameters[28].Value = dr["CT_BSQDWS"];
                            //        parameters[29].Value = Convert.ToDateTime(DateTime.Now, CultureInfo.InvariantCulture);
                            //        parameters[30].Value = Convert.ToDateTime(DateTime.Now, CultureInfo.InvariantCulture);
                            //        parameters[31].Value = dr["JYBGBH"];
                            //        parameters[32].Value = dr["JYJGMC"];
                            //        parameters[33].Value = dr["TYMC"];
                            //        parameters[34].Value = dr["UNIQUE_CODE"];
                                    
                                    #endregion


                                    StringBuilder strSql = new StringBuilder();
                                    strSql.Append("insert into CTNY_MAIN(");
                                    strSql.Append("UNIQUE_CODE,QCSCQY,JYBGBH,JKQCZJXS,CLXH,HGSPBM,CLZL,YYC,QDXS,ZWPS,ZCZBZL,ZDSJZZL,ZGCS,EDZK,LTGG,LJ,JYJGMC,TYMC,ZJ,RLLX,CT_BSQXS,CT_EDGL,CT_FDJXH,CT_JGL,CT_PL,CT_QGS,CT_QTXX,CT_SJGKRLXHL,CT_SQGKRLXHL,CT_ZHGKCO2PFL,CT_ZHGKRLXHL,CT_BSQDWS,CREATETIME,UPDATETIME)");
                                    strSql.Append(" values (");
                                    strSql.Append("@UNIQUE_CODE,@QCSCQY,@JYBGBH,@JKQCZJXS,@CLXH,@HGSPBM,@CLZL,@YYC,@QDXS,@ZWPS,@ZCZBZL,@ZDSJZZL,@ZGCS,@EDZK,@LTGG,@LJ,@JYJGMC,@TYMC,@ZJ,@RLLX,@CT_BSQXS,@CT_EDGL,@CT_FDJXH,@CT_JGL,@CT_PL,@CT_QGS,@CT_QTXX,@CT_SJGKRLXHL,@CT_SQGKRLXHL,@CT_ZHGKCO2PFL,@CT_ZHGKRLXHL,@CT_BSQDWS,@CREATETIME,@UPDATETIME)");
                                    OleDbParameter[] parameters = {
					                    new OleDbParameter("@UNIQUE_CODE", OleDbType.VarChar,255),
					                    new OleDbParameter("@QCSCQY", OleDbType.VarChar,255),
					                    new OleDbParameter("@JYBGBH", OleDbType.VarChar,255),
					                    new OleDbParameter("@JKQCZJXS", OleDbType.VarChar,255),
					                    new OleDbParameter("@CLXH", OleDbType.VarChar,255),
					                    new OleDbParameter("@HGSPBM", OleDbType.VarChar,255),
					                    new OleDbParameter("@CLZL", OleDbType.VarChar,255),
					                    new OleDbParameter("@YYC", OleDbType.VarChar,255),
					                    new OleDbParameter("@QDXS", OleDbType.VarChar,255),
					                    new OleDbParameter("@ZWPS", OleDbType.VarChar,255),
					                    new OleDbParameter("@ZCZBZL", OleDbType.VarChar,255),
					                    new OleDbParameter("@ZDSJZZL", OleDbType.VarChar,255),
					                    new OleDbParameter("@ZGCS", OleDbType.VarChar,255),
					                    new OleDbParameter("@EDZK", OleDbType.VarChar,255),
					                    new OleDbParameter("@LTGG", OleDbType.VarChar,200),
					                    new OleDbParameter("@LJ", OleDbType.VarChar,255),
					                    new OleDbParameter("@JYJGMC", OleDbType.VarChar,255),
					                    new OleDbParameter("@TYMC", OleDbType.VarChar,255),
					                    new OleDbParameter("@ZJ", OleDbType.VarChar,255),
					                    new OleDbParameter("@RLLX", OleDbType.VarChar,200),
					                    new OleDbParameter("@CT_BSQXS", OleDbType.VarChar,255),
					                    new OleDbParameter("@CT_EDGL", OleDbType.VarChar,255),
					                    new OleDbParameter("@CT_FDJXH", OleDbType.VarChar,255),
					                    new OleDbParameter("@CT_JGL", OleDbType.VarChar,255),
					                    new OleDbParameter("@CT_PL", OleDbType.VarChar,255),
					                    new OleDbParameter("@CT_QGS", OleDbType.VarChar,255),
					                    new OleDbParameter("@CT_QTXX", OleDbType.VarChar,255),
					                    new OleDbParameter("@CT_SJGKRLXHL", OleDbType.VarChar,255),
					                    new OleDbParameter("@CT_SQGKRLXHL", OleDbType.VarChar,255),
					                    new OleDbParameter("@CT_ZHGKCO2PFL", OleDbType.VarChar,255),
					                    new OleDbParameter("@CT_ZHGKRLXHL", OleDbType.VarChar,255),
					                    new OleDbParameter("@CT_BSQDWS", OleDbType.VarChar,255),
					                    new OleDbParameter("@CREATETIME", OleDbType.Date),
					                    new OleDbParameter("@UPDATETIME", OleDbType.Date)
                                    };
                                    parameters[0].Value = dr["UNIQUE_CODE"];
                                    parameters[1].Value = dr["QCSCQY"];
                                    parameters[2].Value = dr["JYBGBH"];
                                    parameters[3].Value = dr["JKQCZJXS"];
                                    parameters[4].Value = dr["CLXH"];
                                    parameters[5].Value = dr["HGSPBM"];
                                    parameters[6].Value = dr["CLZL"];
                                    parameters[7].Value = dr["YYC"];
                                    parameters[8].Value = dr["QDXS"];
                                    parameters[9].Value = dr["ZWPS"];
                                    parameters[10].Value = dr["ZCZBZL"];
                                    parameters[11].Value = dr["ZDSJZZL"];
                                    parameters[12].Value = dr["ZGCS"];
                                    parameters[13].Value = dr["EDZK"];
                                    parameters[14].Value = dr["LTGG"];
                                    parameters[15].Value = dr["LJ"];
                                    parameters[16].Value = dr["JYJGMC"];
                                    parameters[17].Value = dr["TYMC"];
                                    parameters[18].Value = dr["ZJ"];
                                    parameters[19].Value = dr["RLLX"];
                                    parameters[20].Value = dr["CT_BSQXS"];
                                    parameters[21].Value = dr["CT_EDGL"];
                                    parameters[22].Value = dr["CT_FDJXH"];
                                    parameters[23].Value = dr["CT_JGL"];
                                    parameters[24].Value = dr["CT_PL"];
                                    parameters[25].Value = dr["CT_QGS"];
                                    parameters[26].Value = dr["CT_QTXX"];
                                    parameters[27].Value = dr["CT_SJGKRLXHL"];
                                    parameters[28].Value = dr["CT_SQGKRLXHL"];
                                    parameters[29].Value = dr["CT_ZHGKCO2PFL"];
                                    parameters[30].Value = dr["CT_ZHGKRLXHL"];
                                    parameters[31].Value = dr["CT_BSQDWS"];
                                    parameters[32].Value = Convert.ToDateTime(DateTime.Now, CultureInfo.InvariantCulture);
                                    parameters[33].Value = Convert.ToDateTime(DateTime.Now, CultureInfo.InvariantCulture);

                                    AccessHelper.ExecuteNonQuery(AccessHelper.conn, strSql.ToString(), parameters);
                                    succCount++;

                                    #endregion
                                }
                                catch (Exception ex)
                                {
                                    msg += ex.Message + "\r\n";
                                }
                                #endregion
                            }
                            else if (rlzl.Equals(FCDSHHDL))
                            {
                                #region 非插电式混合动力
                                try
                                {
                                    #region insert

                                    StringBuilder strSql = new StringBuilder();
                                    strSql.Append("insert into FCDS_MAIN(");
                                    strSql.Append("UNIQUE_CODE,QCSCQY,JYBGBH,JKQCZJXS,CLXH,HGSPBM,CLZL,YYC,QDXS,ZWPS,ZCZBZL,ZDSJZZL,ZGCS,EDZK,LTGG,LJ,JYJGMC,TYMC,ZJ,RLLX,FCDS_HHDL_BSQDWS,FCDS_HHDL_BSQXS,FCDS_HHDL_CDDMSXZGCS,FCDS_HHDL_CDDMSXZHGKXSLC,FCDS_HHDL_DLXDCBNL,FCDS_HHDL_DLXDCZBCDY,FCDS_HHDL_DLXDCZZL,FCDS_HHDL_DLXDCZZNL,FCDS_HHDL_EDGL,FCDS_HHDL_FDJXH,FCDS_HHDL_HHDLJGXS,FCDS_HHDL_HHDLZDDGLB,FCDS_HHDL_JGL,FCDS_HHDL_PL,FCDS_HHDL_QDDJEDGL,FCDS_HHDL_QDDJFZNJ,FCDS_HHDL_QDDJLX,FCDS_HHDL_QGS,FCDS_HHDL_SJGKRLXHL,FCDS_HHDL_SQGKRLXHL,FCDS_HHDL_XSMSSDXZGN,FCDS_HHDL_ZHGKRLXHL,FCDS_HHDL_ZHKGCO2PL,CREATETIME,UPDATETIME)");
                                    strSql.Append(" values (");
                                    strSql.Append("@UNIQUE_CODE,@QCSCQY,@JYBGBH,@JKQCZJXS,@CLXH,@HGSPBM,@CLZL,@YYC,@QDXS,@ZWPS,@ZCZBZL,@ZDSJZZL,@ZGCS,@EDZK,@LTGG,@LJ,@JYJGMC,@TYMC,@ZJ,@RLLX,@FCDS_HHDL_BSQDWS,@FCDS_HHDL_BSQXS,@FCDS_HHDL_CDDMSXZGCS,@FCDS_HHDL_CDDMSXZHGKXSLC,@FCDS_HHDL_DLXDCBNL,@FCDS_HHDL_DLXDCZBCDY,@FCDS_HHDL_DLXDCZZL,@FCDS_HHDL_DLXDCZZNL,@FCDS_HHDL_EDGL,@FCDS_HHDL_FDJXH,@FCDS_HHDL_HHDLJGXS,@FCDS_HHDL_HHDLZDDGLB,@FCDS_HHDL_JGL,@FCDS_HHDL_PL,@FCDS_HHDL_QDDJEDGL,@FCDS_HHDL_QDDJFZNJ,@FCDS_HHDL_QDDJLX,@FCDS_HHDL_QGS,@FCDS_HHDL_SJGKRLXHL,@FCDS_HHDL_SQGKRLXHL,@FCDS_HHDL_XSMSSDXZGN,@FCDS_HHDL_ZHGKRLXHL,@FCDS_HHDL_ZHKGCO2PL,@CREATETIME,@UPDATETIME)");
                                    OleDbParameter[] parameters = {
					                    new OleDbParameter("@UNIQUE_CODE", OleDbType.VarChar,255),
					                    new OleDbParameter("@QCSCQY", OleDbType.VarChar,255),
					                    new OleDbParameter("@JYBGBH", OleDbType.VarChar,255),
					                    new OleDbParameter("@JKQCZJXS", OleDbType.VarChar,255),
					                    new OleDbParameter("@CLXH", OleDbType.VarChar,255),
					                    new OleDbParameter("@HGSPBM", OleDbType.VarChar,255),
					                    new OleDbParameter("@CLZL", OleDbType.VarChar,255),
					                    new OleDbParameter("@YYC", OleDbType.VarChar,255),
					                    new OleDbParameter("@QDXS", OleDbType.VarChar,255),
					                    new OleDbParameter("@ZWPS", OleDbType.VarChar,255),
					                    new OleDbParameter("@ZCZBZL", OleDbType.VarChar,255),
					                    new OleDbParameter("@ZDSJZZL", OleDbType.VarChar,255),
					                    new OleDbParameter("@ZGCS", OleDbType.VarChar,255),
					                    new OleDbParameter("@EDZK", OleDbType.VarChar,255),
					                    new OleDbParameter("@LTGG", OleDbType.VarChar,200),
					                    new OleDbParameter("@LJ", OleDbType.VarChar,255),
					                    new OleDbParameter("@JYJGMC", OleDbType.VarChar,255),
					                    new OleDbParameter("@TYMC", OleDbType.VarChar,255),
					                    new OleDbParameter("@ZJ", OleDbType.VarChar,255),
					                    new OleDbParameter("@RLLX", OleDbType.VarChar,200),
					                    new OleDbParameter("@FCDS_HHDL_BSQDWS", OleDbType.VarChar,255),
					                    new OleDbParameter("@FCDS_HHDL_BSQXS", OleDbType.VarChar,255),
					                    new OleDbParameter("@FCDS_HHDL_CDDMSXZGCS", OleDbType.VarChar,255),
					                    new OleDbParameter("@FCDS_HHDL_CDDMSXZHGKXSLC", OleDbType.VarChar,255),
					                    new OleDbParameter("@FCDS_HHDL_DLXDCBNL", OleDbType.VarChar,255),
					                    new OleDbParameter("@FCDS_HHDL_DLXDCZBCDY", OleDbType.VarChar,255),
					                    new OleDbParameter("@FCDS_HHDL_DLXDCZZL", OleDbType.VarChar,255),
					                    new OleDbParameter("@FCDS_HHDL_DLXDCZZNL", OleDbType.VarChar,255),
					                    new OleDbParameter("@FCDS_HHDL_EDGL", OleDbType.VarChar,255),
					                    new OleDbParameter("@FCDS_HHDL_FDJXH", OleDbType.VarChar,255),
					                    new OleDbParameter("@FCDS_HHDL_HHDLJGXS", OleDbType.VarChar,255),
					                    new OleDbParameter("@FCDS_HHDL_HHDLZDDGLB", OleDbType.VarChar,255),
					                    new OleDbParameter("@FCDS_HHDL_JGL", OleDbType.VarChar,255),
					                    new OleDbParameter("@FCDS_HHDL_PL", OleDbType.VarChar,255),
					                    new OleDbParameter("@FCDS_HHDL_QDDJEDGL", OleDbType.VarChar,255),
					                    new OleDbParameter("@FCDS_HHDL_QDDJFZNJ", OleDbType.VarChar,255),
					                    new OleDbParameter("@FCDS_HHDL_QDDJLX", OleDbType.VarChar,255),
					                    new OleDbParameter("@FCDS_HHDL_QGS", OleDbType.VarChar,255),
					                    new OleDbParameter("@FCDS_HHDL_SJGKRLXHL", OleDbType.VarChar,255),
					                    new OleDbParameter("@FCDS_HHDL_SQGKRLXHL", OleDbType.VarChar,255),
					                    new OleDbParameter("@FCDS_HHDL_XSMSSDXZGN", OleDbType.VarChar,255),
					                    new OleDbParameter("@FCDS_HHDL_ZHGKRLXHL", OleDbType.VarChar,255),
					                    new OleDbParameter("@FCDS_HHDL_ZHKGCO2PL", OleDbType.VarChar,255),
					                    new OleDbParameter("@CREATETIME", OleDbType.Date),
					                    new OleDbParameter("@UPDATETIME", OleDbType.Date)};
                                    parameters[0].Value = dr["UNIQUE_CODE"];
                                    parameters[1].Value = dr["QCSCQY"];
                                    parameters[2].Value = dr["JYBGBH"];
                                    parameters[3].Value = dr["JKQCZJXS"];
                                    parameters[4].Value = dr["CLXH"];
                                    parameters[5].Value = dr["HGSPBM"];
                                    parameters[6].Value = dr["CLZL"];
                                    parameters[7].Value = dr["YYC"];
                                    parameters[8].Value = dr["QDXS"];
                                    parameters[9].Value = dr["ZWPS"];
                                    parameters[10].Value = dr["ZCZBZL"];
                                    parameters[11].Value = dr["ZDSJZZL"];
                                    parameters[12].Value = dr["ZGCS"];
                                    parameters[13].Value = dr["EDZK"];
                                    parameters[14].Value = dr["LTGG"];
                                    parameters[15].Value = dr["LJ"];
                                    parameters[16].Value = dr["JYJGMC"];
                                    parameters[17].Value = dr["TYMC"];
                                    parameters[18].Value = dr["ZJ"];
                                    parameters[19].Value = dr["RLLX"];
                                    parameters[20].Value = dr["FCDS_HHDL_BSQDWS"];
                                    parameters[21].Value = dr["FCDS_HHDL_BSQXS"];
                                    parameters[22].Value = dr["FCDS_HHDL_CDDMSXZGCS"];
                                    parameters[23].Value = dr["FCDS_HHDL_CDDMSXZHGKXSLC"];
                                    parameters[24].Value = dr["FCDS_HHDL_DLXDCBNL"];
                                    parameters[25].Value = dr["FCDS_HHDL_DLXDCZBCDY"];
                                    parameters[26].Value = dr["FCDS_HHDL_DLXDCZZL"];
                                    parameters[27].Value = dr["FCDS_HHDL_DLXDCZZNL"];
                                    parameters[28].Value = dr["FCDS_HHDL_EDGL"];
                                    parameters[29].Value = dr["FCDS_HHDL_FDJXH"];
                                    parameters[30].Value = dr["FCDS_HHDL_HHDLJGXS"];
                                    parameters[31].Value = dr["FCDS_HHDL_HHDLZDDGLB"];
                                    parameters[32].Value = dr["FCDS_HHDL_JGL"];
                                    parameters[33].Value = dr["FCDS_HHDL_PL"];
                                    parameters[34].Value = dr["FCDS_HHDL_QDDJEDGL"];
                                    parameters[35].Value = dr["FCDS_HHDL_QDDJFZNJ"];
                                    parameters[36].Value = dr["FCDS_HHDL_QDDJLX"];
                                    parameters[37].Value = dr["FCDS_HHDL_QGS"];
                                    parameters[38].Value = dr["FCDS_HHDL_SJGKRLXHL"];
                                    parameters[39].Value = dr["FCDS_HHDL_SQGKRLXHL"];
                                    parameters[40].Value = dr["FCDS_HHDL_XSMSSDXZGN"];
                                    parameters[41].Value = dr["FCDS_HHDL_ZHGKRLXHL"];
                                    parameters[42].Value = dr["FCDS_HHDL_ZHKGCO2PL"];
                                    parameters[43].Value = Convert.ToDateTime(DateTime.Now, CultureInfo.InvariantCulture);
                                    parameters[44].Value = Convert.ToDateTime(DateTime.Now, CultureInfo.InvariantCulture);

                                    AccessHelper.ExecuteNonQuery(AccessHelper.conn, strSql.ToString(), parameters);
                                    succCount++;

                                    #endregion
                                }
                                catch (Exception ex)
                                {
                                    msg += ex.Message + "\r\n";
                                }
                                #endregion
                            }
                            else if (rlzl.Equals(CDSHHDL))
                            {
                                #region 插电式混合动力
                                try
                                {
                                    #region insert

                                    StringBuilder strSql = new StringBuilder();
                                    strSql.Append("insert into CDS_MAIN(");
                                    strSql.Append("UNIQUE_CODE,QCSCQY,JYBGBH,JKQCZJXS,CLXH,HGSPBM,CLZL,YYC,QDXS,ZWPS,ZCZBZL,ZDSJZZL,ZGCS,EDZK,LTGG,LJ,JYJGMC,TYMC,ZJ,RLLX,CDS_HHDL_BSQDWS,CDS_HHDL_BSQXS,CDS_HHDL_CDDMSXZGCS,CDS_HHDL_CDDMSXZHGKXSLC,CDS_HHDL_DLXDCBNL,CDS_HHDL_DLXDCZBCDY,CDS_HHDL_DLXDCZZL,CDS_HHDL_DLXDCZZNL,CDS_HHDL_EDGL,CDS_HHDL_FDJXH,CDS_HHDL_HHDLJGXS,CDS_HHDL_HHDLZDDGLB,CDS_HHDL_JGL,CDS_HHDL_PL,CDS_HHDL_QDDJEDGL,CDS_HHDL_QDDJFZNJ,CDS_HHDL_QDDJLX,CDS_HHDL_QGS,CDS_HHDL_XSMSSDXZGN,CDS_HHDL_ZHGKDNXHL,CDS_HHDL_ZHGKRLXHL,CDS_HHDL_ZHKGCO2PL,CREATETIME,UPDATETIME)");
                                    strSql.Append(" values (");
                                    strSql.Append("@UNIQUE_CODE,@QCSCQY,@JYBGBH,@JKQCZJXS,@CLXH,@HGSPBM,@CLZL,@YYC,@QDXS,@ZWPS,@ZCZBZL,@ZDSJZZL,@ZGCS,@EDZK,@LTGG,@LJ,@JYJGMC,@TYMC,@ZJ,@RLLX,@CDS_HHDL_BSQDWS,@CDS_HHDL_BSQXS,@CDS_HHDL_CDDMSXZGCS,@CDS_HHDL_CDDMSXZHGKXSLC,@CDS_HHDL_DLXDCBNL,@CDS_HHDL_DLXDCZBCDY,@CDS_HHDL_DLXDCZZL,@CDS_HHDL_DLXDCZZNL,@CDS_HHDL_EDGL,@CDS_HHDL_FDJXH,@CDS_HHDL_HHDLJGXS,@CDS_HHDL_HHDLZDDGLB,@CDS_HHDL_JGL,@CDS_HHDL_PL,@CDS_HHDL_QDDJEDGL,@CDS_HHDL_QDDJFZNJ,@CDS_HHDL_QDDJLX,@CDS_HHDL_QGS,@CDS_HHDL_XSMSSDXZGN,@CDS_HHDL_ZHGKDNXHL,@CDS_HHDL_ZHGKRLXHL,@CDS_HHDL_ZHKGCO2PL,@CREATETIME,@UPDATETIME)");
                                    OleDbParameter[] parameters = {
					                    new OleDbParameter("@UNIQUE_CODE", OleDbType.VarChar,255),
					                    new OleDbParameter("@QCSCQY", OleDbType.VarChar,255),
					                    new OleDbParameter("@JYBGBH", OleDbType.VarChar,255),
					                    new OleDbParameter("@JKQCZJXS", OleDbType.VarChar,255),
					                    new OleDbParameter("@CLXH", OleDbType.VarChar,255),
					                    new OleDbParameter("@HGSPBM", OleDbType.VarChar,255),
					                    new OleDbParameter("@CLZL", OleDbType.VarChar,255),
					                    new OleDbParameter("@YYC", OleDbType.VarChar,255),
					                    new OleDbParameter("@QDXS", OleDbType.VarChar,255),
					                    new OleDbParameter("@ZWPS", OleDbType.VarChar,255),
					                    new OleDbParameter("@ZCZBZL", OleDbType.VarChar,255),
					                    new OleDbParameter("@ZDSJZZL", OleDbType.VarChar,255),
					                    new OleDbParameter("@ZGCS", OleDbType.VarChar,255),
					                    new OleDbParameter("@EDZK", OleDbType.VarChar,255),
					                    new OleDbParameter("@LTGG", OleDbType.VarChar,200),
					                    new OleDbParameter("@LJ", OleDbType.VarChar,255),
					                    new OleDbParameter("@JYJGMC", OleDbType.VarChar,255),
					                    new OleDbParameter("@TYMC", OleDbType.VarChar,255),
					                    new OleDbParameter("@ZJ", OleDbType.VarChar,255),
					                    new OleDbParameter("@RLLX", OleDbType.VarChar,200),
					                    new OleDbParameter("@CDS_HHDL_BSQDWS", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDS_HHDL_BSQXS", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDS_HHDL_CDDMSXZGCS", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDS_HHDL_CDDMSXZHGKXSLC", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDS_HHDL_DLXDCBNL", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDS_HHDL_DLXDCZBCDY", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDS_HHDL_DLXDCZZL", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDS_HHDL_DLXDCZZNL", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDS_HHDL_EDGL", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDS_HHDL_FDJXH", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDS_HHDL_HHDLJGXS", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDS_HHDL_HHDLZDDGLB", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDS_HHDL_JGL", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDS_HHDL_PL", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDS_HHDL_QDDJEDGL", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDS_HHDL_QDDJFZNJ", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDS_HHDL_QDDJLX", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDS_HHDL_QGS", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDS_HHDL_XSMSSDXZGN", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDS_HHDL_ZHGKDNXHL", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDS_HHDL_ZHGKRLXHL", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDS_HHDL_ZHKGCO2PL", OleDbType.VarChar,255),
					                    new OleDbParameter("@CREATETIME", OleDbType.Date),
					                    new OleDbParameter("@UPDATETIME", OleDbType.Date)
                                    };
                                    parameters[0].Value = dr["UNIQUE_CODE"];
                                    parameters[1].Value = dr["QCSCQY"];
                                    parameters[2].Value = dr["JYBGBH"];
                                    parameters[3].Value = dr["JKQCZJXS"];
                                    parameters[4].Value = dr["CLXH"];
                                    parameters[5].Value = dr["HGSPBM"];
                                    parameters[6].Value = dr["CLZL"];
                                    parameters[7].Value = dr["YYC"];
                                    parameters[8].Value = dr["QDXS"];
                                    parameters[9].Value = dr["ZWPS"];
                                    parameters[10].Value = dr["ZCZBZL"];
                                    parameters[11].Value = dr["ZDSJZZL"];
                                    parameters[12].Value = dr["ZGCS"];
                                    parameters[13].Value = dr["EDZK"];
                                    parameters[14].Value = dr["LTGG"];
                                    parameters[15].Value = dr["LJ"];
                                    parameters[16].Value = dr["JYJGMC"];
                                    parameters[17].Value = dr["TYMC"];
                                    parameters[18].Value = dr["ZJ"];
                                    parameters[19].Value = dr["RLLX"];
                                    parameters[20].Value = dr["CDS_HHDL_BSQDWS"];
                                    parameters[21].Value = dr["CDS_HHDL_BSQXS"];
                                    parameters[22].Value = dr["CDS_HHDL_CDDMSXZGCS"];
                                    parameters[23].Value = dr["CDS_HHDL_CDDMSXZHGKXSLC"];
                                    parameters[24].Value = dr["CDS_HHDL_DLXDCBNL"];
                                    parameters[25].Value = dr["CDS_HHDL_DLXDCZBCDY"];
                                    parameters[26].Value = dr["CDS_HHDL_DLXDCZZL"];
                                    parameters[27].Value = dr["CDS_HHDL_DLXDCZZNL"];
                                    parameters[28].Value = dr["CDS_HHDL_EDGL"];
                                    parameters[29].Value = dr["CDS_HHDL_FDJXH"];
                                    parameters[30].Value = dr["CDS_HHDL_HHDLJGXS"];
                                    parameters[31].Value = dr["CDS_HHDL_HHDLZDDGLB"];
                                    parameters[32].Value = dr["CDS_HHDL_JGL"];
                                    parameters[33].Value = dr["CDS_HHDL_PL"];
                                    parameters[34].Value = dr["CDS_HHDL_QDDJEDGL"];
                                    parameters[35].Value = dr["CDS_HHDL_QDDJFZNJ"];
                                    parameters[36].Value = dr["CDS_HHDL_QDDJLX"];
                                    parameters[37].Value = dr["CDS_HHDL_QGS"];
                                    parameters[38].Value = dr["CDS_HHDL_XSMSSDXZGN"];
                                    parameters[39].Value = dr["CDS_HHDL_ZHGKDNXHL"];
                                    parameters[40].Value = dr["CDS_HHDL_ZHGKRLXHL"];
                                    parameters[41].Value = dr["CDS_HHDL_ZHKGCO2PL"];
                                    parameters[42].Value = Convert.ToDateTime(DateTime.Now, CultureInfo.InvariantCulture);
                                    parameters[43].Value = Convert.ToDateTime(DateTime.Now, CultureInfo.InvariantCulture);

                                    AccessHelper.ExecuteNonQuery(AccessHelper.conn, strSql.ToString(), parameters);
                                    succCount++;

                                    #endregion
                                }
                                catch (Exception ex)
                                {
                                    msg += ex.Message + "\r\n";
                                }
                                #endregion
                            }
                            else if (rlzl.Equals(CDD))
                            {
                                #region 纯电动
                                try
                                {
                                    #region insert

                                    StringBuilder strSql = new StringBuilder();
                                    strSql.Append("insert into CDD_MAIN(");
                                    strSql.Append("UNIQUE_CODE,QCSCQY,JYBGBH,JKQCZJXS,CLXH,HGSPBM,CLZL,YYC,QDXS,ZWPS,ZCZBZL,ZDSJZZL,ZGCS,EDZK,LTGG,LJ,JYJGMC,TYMC,ZJ,RLLX,CDD_DDQC30FZZGCS,CDD_DDXDCZZLYZCZBZLDBZ,CDD_DLXDCBNL,CDD_DLXDCZBCDY,CDD_DLXDCZEDNL,CDD_DLXDCZZL,CDD_QDDJEDGL,CDD_QDDJFZNJ,CDD_QDDJLX,CDD_ZHGKDNXHL,CDD_ZHGKXSLC,CREATETIME,UPDATETIME)");
                                    strSql.Append(" values (");
                                    strSql.Append("@UNIQUE_CODE,@QCSCQY,@JYBGBH,@JKQCZJXS,@CLXH,@HGSPBM,@CLZL,@YYC,@QDXS,@ZWPS,@ZCZBZL,@ZDSJZZL,@ZGCS,@EDZK,@LTGG,@LJ,@JYJGMC,@TYMC,@ZJ,@RLLX,@CDD_DDQC30FZZGCS,@CDD_DDXDCZZLYZCZBZLDBZ,@CDD_DLXDCBNL,@CDD_DLXDCZBCDY,@CDD_DLXDCZEDNL,@CDD_DLXDCZZL,@CDD_QDDJEDGL,@CDD_QDDJFZNJ,@CDD_QDDJLX,@CDD_ZHGKDNXHL,@CDD_ZHGKXSLC,@CREATETIME,@UPDATETIME)");
                                    OleDbParameter[] parameters = {
					                new OleDbParameter("@UNIQUE_CODE", OleDbType.VarChar,255),
					                new OleDbParameter("@QCSCQY", OleDbType.VarChar,255),
					                new OleDbParameter("@JYBGBH", OleDbType.VarChar,255),
					                new OleDbParameter("@JKQCZJXS", OleDbType.VarChar,255),
					                new OleDbParameter("@CLXH", OleDbType.VarChar,255),
					                new OleDbParameter("@HGSPBM", OleDbType.VarChar,255),
					                new OleDbParameter("@CLZL", OleDbType.VarChar,255),
					                new OleDbParameter("@YYC", OleDbType.VarChar,255),
					                new OleDbParameter("@QDXS", OleDbType.VarChar,255),
					                new OleDbParameter("@ZWPS", OleDbType.VarChar,255),
					                new OleDbParameter("@ZCZBZL", OleDbType.VarChar,255),
					                new OleDbParameter("@ZDSJZZL", OleDbType.VarChar,255),
					                new OleDbParameter("@ZGCS", OleDbType.VarChar,255),
					                new OleDbParameter("@EDZK", OleDbType.VarChar,255),
					                new OleDbParameter("@LTGG", OleDbType.VarChar,200),
					                new OleDbParameter("@LJ", OleDbType.VarChar,255),
					                new OleDbParameter("@JYJGMC", OleDbType.VarChar,255),
					                new OleDbParameter("@TYMC", OleDbType.VarChar,255),
					                new OleDbParameter("@ZJ", OleDbType.VarChar,255),
					                new OleDbParameter("@RLLX", OleDbType.VarChar,200),
					                new OleDbParameter("@CDD_DDQC30FZZGCS", OleDbType.VarChar,255),
					                new OleDbParameter("@CDD_DDXDCZZLYZCZBZLDBZ", OleDbType.VarChar,255),
					                new OleDbParameter("@CDD_DLXDCBNL", OleDbType.VarChar,255),
					                new OleDbParameter("@CDD_DLXDCZBCDY", OleDbType.VarChar,255),
					                new OleDbParameter("@CDD_DLXDCZEDNL", OleDbType.VarChar,255),
					                new OleDbParameter("@CDD_DLXDCZZL", OleDbType.VarChar,255),
					                new OleDbParameter("@CDD_QDDJEDGL", OleDbType.VarChar,255),
					                new OleDbParameter("@CDD_QDDJFZNJ", OleDbType.VarChar,255),
					                new OleDbParameter("@CDD_QDDJLX", OleDbType.VarChar,255),
					                new OleDbParameter("@CDD_ZHGKDNXHL", OleDbType.VarChar,255),
					                new OleDbParameter("@CDD_ZHGKXSLC", OleDbType.VarChar,255),
					                new OleDbParameter("@CREATETIME", OleDbType.Date),
					                new OleDbParameter("@UPDATETIME", OleDbType.Date)
                                };
                                    parameters[0].Value = dr["UNIQUE_CODE"];
                                    parameters[1].Value = dr["QCSCQY"];
                                    parameters[2].Value = dr["JYBGBH"];
                                    parameters[3].Value = dr["JKQCZJXS"];
                                    parameters[4].Value = dr["CLXH"];
                                    parameters[5].Value = dr["HGSPBM"];
                                    parameters[6].Value = dr["CLZL"];
                                    parameters[7].Value = dr["YYC"];
                                    parameters[8].Value = dr["QDXS"];
                                    parameters[9].Value = dr["ZWPS"];
                                    parameters[10].Value = dr["ZCZBZL"];
                                    parameters[11].Value = dr["ZDSJZZL"];
                                    parameters[12].Value = dr["ZGCS"];
                                    parameters[13].Value = dr["EDZK"];
                                    parameters[14].Value = dr["LTGG"];
                                    parameters[15].Value = dr["LJ"];
                                    parameters[16].Value = dr["JYJGMC"];
                                    parameters[17].Value = dr["TYMC"];
                                    parameters[18].Value = dr["ZJ"];
                                    parameters[19].Value = dr["RLLX"];
                                    parameters[20].Value = dr["CDD_DDQC30FZZGCS"];
                                    parameters[21].Value = dr["CDD_DDXDCZZLYZCZBZLDBZ"];
                                    parameters[22].Value = dr["CDD_DLXDCBNL"];
                                    parameters[23].Value = dr["CDD_DLXDCZBCDY"];
                                    parameters[24].Value = dr["CDD_DLXDCZEDNL"];
                                    parameters[25].Value = dr["CDD_DLXDCZZL"];
                                    parameters[26].Value = dr["CDD_QDDJEDGL"];
                                    parameters[27].Value = dr["CDD_QDDJFZNJ"];
                                    parameters[28].Value = dr["CDD_QDDJLX"];
                                    parameters[29].Value = dr["CDD_ZHGKDNXHL"];
                                    parameters[30].Value = dr["CDD_ZHGKXSLC"];
                                    parameters[31].Value = Convert.ToDateTime(DateTime.Now, CultureInfo.InvariantCulture);
                                    parameters[32].Value = Convert.ToDateTime(DateTime.Now, CultureInfo.InvariantCulture);

                                    AccessHelper.ExecuteNonQuery(AccessHelper.conn, strSql.ToString(), parameters);
                                    succCount++;

                                    #endregion
                                }
                                catch (Exception ex)
                                {
                                    msg += ex.Message + "\r\n";
                                }
                                #endregion
                            }
                            else if (rlzl.Equals(RLDC))
                            {
                                #region 燃料电池
                                try
                                {
                                    #region insert

                                    StringBuilder strSql = new StringBuilder();
                                    strSql.Append("insert into RLDC_MAIN(");
                                    strSql.Append("UNIQUE_CODE,QCSCQY,JYBGBH,JKQCZJXS,CLXH,HGSPBM,CLZL,YYC,QDXS,ZWPS,ZCZBZL,ZDSJZZL,ZGCS,EDZK,LTGG,LJ,JYJGMC,TYMC,ZJ,RLLX,RLDC_CDDMSXZGXSCS,RLDC_CQPBCGZYL,RLDC_CQPRJ,RLDC_DDGLMD,RLDC_CQPLX,RLDC_DDHHJSTJXXDCZBNL,RLDC_DLXDCZZL,RLDC_QDDJEDGL,RLDC_QDDJFZNJ,RLDC_QDDJLX,RLDC_RLLX,RLDC_ZHGKHQL,RLDC_ZHGKXSLC,CREATETIME,UPDATETIME)");
                                    strSql.Append(" values (");
                                    strSql.Append("@UNIQUE_CODE,@QCSCQY,@JYBGBH,@JKQCZJXS,@CLXH,@HGSPBM,@CLZL,@YYC,@QDXS,@ZWPS,@ZCZBZL,@ZDSJZZL,@ZGCS,@EDZK,@LTGG,@LJ,@JYJGMC,@TYMC,@ZJ,@RLLX,@RLDC_CDDMSXZGXSCS,@RLDC_CQPBCGZYL,@RLDC_CQPRJ,@RLDC_DDGLMD,@RLDC_CQPLX,@RLDC_DDHHJSTJXXDCZBNL,@RLDC_DLXDCZZL,@RLDC_QDDJEDGL,@RLDC_QDDJFZNJ,@RLDC_QDDJLX,@RLDC_RLLX,@RLDC_ZHGKHQL,@RLDC_ZHGKXSLC,@CREATETIME,@UPDATETIME)");
                                    OleDbParameter[] parameters = {
					                new OleDbParameter("@UNIQUE_CODE", OleDbType.VarChar,255),
					                new OleDbParameter("@QCSCQY", OleDbType.VarChar,255),
					                new OleDbParameter("@JYBGBH", OleDbType.VarChar,255),
					                new OleDbParameter("@JKQCZJXS", OleDbType.VarChar,255),
					                new OleDbParameter("@CLXH", OleDbType.VarChar,255),
					                new OleDbParameter("@HGSPBM", OleDbType.VarChar,255),
					                new OleDbParameter("@CLZL", OleDbType.VarChar,255),
					                new OleDbParameter("@YYC", OleDbType.VarChar,255),
					                new OleDbParameter("@QDXS", OleDbType.VarChar,255),
					                new OleDbParameter("@ZWPS", OleDbType.VarChar,255),
					                new OleDbParameter("@ZCZBZL", OleDbType.VarChar,255),
					                new OleDbParameter("@ZDSJZZL", OleDbType.VarChar,255),
					                new OleDbParameter("@ZGCS", OleDbType.VarChar,255),
					                new OleDbParameter("@EDZK", OleDbType.VarChar,255),
					                new OleDbParameter("@LTGG", OleDbType.VarChar,200),
					                new OleDbParameter("@LJ", OleDbType.VarChar,255),
					                new OleDbParameter("@JYJGMC", OleDbType.VarChar,255),
					                new OleDbParameter("@TYMC", OleDbType.VarChar,255),
					                new OleDbParameter("@ZJ", OleDbType.VarChar,255),
					                new OleDbParameter("@RLLX", OleDbType.VarChar,200),
					                new OleDbParameter("@RLDC_CDDMSXZGXSCS", OleDbType.VarChar,255),
					                new OleDbParameter("@RLDC_CQPBCGZYL", OleDbType.VarChar,255),
					                new OleDbParameter("@RLDC_CQPRJ", OleDbType.VarChar,255),
					                new OleDbParameter("@RLDC_DDGLMD", OleDbType.VarChar,255),
					                new OleDbParameter("@RLDC_CQPLX", OleDbType.VarChar,255),
					                new OleDbParameter("@RLDC_DDHHJSTJXXDCZBNL", OleDbType.VarChar,255),
					                new OleDbParameter("@RLDC_DLXDCZZL", OleDbType.VarChar,255),
					                new OleDbParameter("@RLDC_QDDJEDGL", OleDbType.VarChar,255),
					                new OleDbParameter("@RLDC_QDDJFZNJ", OleDbType.VarChar,255),
					                new OleDbParameter("@RLDC_QDDJLX", OleDbType.VarChar,255),
					                new OleDbParameter("@RLDC_RLLX", OleDbType.VarChar,255),
					                new OleDbParameter("@RLDC_ZHGKHQL", OleDbType.VarChar,255),
					                new OleDbParameter("@RLDC_ZHGKXSLC", OleDbType.VarChar,255),
					                new OleDbParameter("@CREATETIME", OleDbType.Date),
					                new OleDbParameter("@UPDATETIME", OleDbType.Date)
                                };

                                    parameters[0].Value = dr["UNIQUE_CODE"];
                                    parameters[1].Value = dr["QCSCQY"];
                                    parameters[2].Value = dr["JYBGBH"];
                                    parameters[3].Value = dr["JKQCZJXS"];
                                    parameters[4].Value = dr["CLXH"];
                                    parameters[5].Value = dr["HGSPBM"];
                                    parameters[6].Value = dr["CLZL"];
                                    parameters[7].Value = dr["YYC"];
                                    parameters[8].Value = dr["QDXS"];
                                    parameters[9].Value = dr["ZWPS"];
                                    parameters[10].Value = dr["ZCZBZL"];
                                    parameters[11].Value = dr["ZDSJZZL"];
                                    parameters[12].Value = dr["ZGCS"];
                                    parameters[13].Value = dr["EDZK"];
                                    parameters[14].Value = dr["LTGG"];
                                    parameters[15].Value = dr["LJ"];
                                    parameters[16].Value = dr["JYJGMC"];
                                    parameters[17].Value = dr["TYMC"];
                                    parameters[18].Value = dr["ZJ"];
                                    parameters[19].Value = dr["RLLX"];
                                    parameters[20].Value = dr["RLDC_CDDMSXZGXSCS"];
                                    parameters[21].Value = dr["RLDC_CQPBCGZYL"];
                                    parameters[22].Value = dr["RLDC_CQPRJ"];
                                    parameters[23].Value = dr["RLDC_DDGLMD"];
                                    parameters[24].Value = dr["RLDC_CQPLX"];
                                    parameters[25].Value = dr["RLDC_DDHHJSTJXXDCZBNL"];
                                    parameters[26].Value = dr["RLDC_DLXDCZZL"];
                                    parameters[27].Value = dr["RLDC_QDDJEDGL"];
                                    parameters[28].Value = dr["RLDC_QDDJFZNJ"];
                                    parameters[29].Value = dr["RLDC_QDDJLX"];
                                    parameters[30].Value = dr["RLDC_RLLX"];
                                    parameters[31].Value = dr["RLDC_ZHGKHQL"];
                                    parameters[32].Value = dr["RLDC_ZHGKXSLC"];
                                    parameters[33].Value = Convert.ToDateTime(DateTime.Now, CultureInfo.InvariantCulture);
                                    parameters[34].Value = Convert.ToDateTime(DateTime.Now, CultureInfo.InvariantCulture);

                                    AccessHelper.ExecuteNonQuery(AccessHelper.conn, strSql.ToString(), parameters);
                                    succCount++;

                                    #endregion
                                }
                                catch (Exception ex)
                                {
                                    msg += ex.Message + "\r\n";
                                }
                                #endregion
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                msg += ex.Message + "\r\n";
            }

            return succCount;
        }

        /// <summary>
        /// 修改已经导入的主表信息
        /// </summary>
        /// <param name="ds"></param>
        /// <returns></returns>
        public string UpdateMainData(DataSet ds, List<string> mainUpdateList)
        {
            int totalCount = 0;
            int succCount = 0;
            string msg = string.Empty;
            string clxh = string.Empty;

            try
            {
                // 传统能源
                DataTable dtCtny = D2D(dictCTNY, ds.Tables[CTNY], CTNY);
                DataRow[] drCtny = checkData.Select(String.Format("FUEL_TYPE='{0}' and STATUS=1", CTNY));

                // 传统能源
                if (dtCtny != null && dtCtny.Rows.Count > 0)
                {
                    totalCount = dtCtny.Rows.Count;
                    string error = string.Empty;
                    foreach (DataRow dr in dtCtny.Rows)
                    {
                        error = VerifyData(dr, drCtny, "UPDATE");      //单行验证
                        if (!string.IsNullOrEmpty(error))
                        {
                            msg += error;
                        }
                        else
                        {
                            #region UPDATE
                            clxh = dr["CLXH"].ToString();
                            StringBuilder strSql = new StringBuilder();
                            strSql.Append("update CTNY_MAIN set ");
                            strSql.Append("QCSCQY=@QCSCQY,");
                            strSql.Append("JYBGBH=@JYBGBH,");
                            strSql.Append("JKQCZJXS=@JKQCZJXS,");
                            strSql.Append("HGSPBM=@HGSPBM,");
                            strSql.Append("CLZL=@CLZL,");
                            strSql.Append("YYC=@YYC,");
                            strSql.Append("QDXS=@QDXS,");
                            strSql.Append("ZWPS=@ZWPS,");
                            strSql.Append("ZCZBZL=@ZCZBZL,");
                            strSql.Append("ZDSJZZL=@ZDSJZZL,");
                            strSql.Append("ZGCS=@ZGCS,");
                            strSql.Append("EDZK=@EDZK,");
                            strSql.Append("LTGG=@LTGG,");
                            strSql.Append("LJ=@LJ,");
                            strSql.Append("JYJGMC=@JYJGMC,");
                            strSql.Append("TYMC=@TYMC,");
                            strSql.Append("ZJ=@ZJ,");
                            strSql.Append("RLLX=@RLLX,");
                            strSql.Append("CT_BSQXS=@CT_BSQXS,");
                            strSql.Append("CT_EDGL=@CT_EDGL,");
                            strSql.Append("CT_FDJXH=@CT_FDJXH,");
                            strSql.Append("CT_JGL=@CT_JGL,");
                            strSql.Append("CT_PL=@CT_PL,");
                            strSql.Append("CT_QCJNJS=@CT_QCJNJS,");
                            strSql.Append("CT_QGS=@CT_QGS,");
                            strSql.Append("CT_QTXX=@CT_QTXX,");
                            strSql.Append("CT_SJGKRLXHL=@CT_SJGKRLXHL,");
                            strSql.Append("CT_SQGKRLXHL=@CT_SQGKRLXHL,");
                            strSql.Append("CT_ZHGKCO2PFL=@CT_ZHGKCO2PFL,");
                            strSql.Append("CT_ZHGKRLXHL=@CT_ZHGKRLXHL,");
                            strSql.Append("CT_BSQDWS=@CT_BSQDWS");
                            strSql.Append(",CLXH=@CLXH");
                            strSql.Append(" where UNIQUE_CODE=@UNIQUE_CODE");

                            OleDbParameter[] parameters = {
					        new OleDbParameter("@QCSCQY", OleDbType.VarChar,255),
					        new OleDbParameter("@JYBGBH", OleDbType.VarChar,255),
					        new OleDbParameter("@JKQCZJXS", OleDbType.VarChar,255),
					        new OleDbParameter("@HGSPBM", OleDbType.VarChar,255),
					        new OleDbParameter("@CLZL", OleDbType.VarChar,255),
					        new OleDbParameter("@YYC", OleDbType.VarChar,255),
					        new OleDbParameter("@QDXS", OleDbType.VarChar,255),
					        new OleDbParameter("@ZWPS", OleDbType.VarChar,255),
					        new OleDbParameter("@ZCZBZL", OleDbType.VarChar,255),
					        new OleDbParameter("@ZDSJZZL", OleDbType.VarChar,255),
					        new OleDbParameter("@ZGCS", OleDbType.VarChar,255),
					        new OleDbParameter("@EDZK", OleDbType.VarChar,255),
					        new OleDbParameter("@LTGG", OleDbType.VarChar,200),
					        new OleDbParameter("@LJ", OleDbType.VarChar,255),
					        new OleDbParameter("@JYJGMC", OleDbType.VarChar,255),
					        new OleDbParameter("@TYMC", OleDbType.VarChar,255),
					        new OleDbParameter("@ZJ", OleDbType.VarChar,255),
					        new OleDbParameter("@RLLX", OleDbType.VarChar,200),
					        new OleDbParameter("@CT_BSQXS", OleDbType.VarChar,255),
					        new OleDbParameter("@CT_EDGL", OleDbType.VarChar,255),
					        new OleDbParameter("@CT_FDJXH", OleDbType.VarChar,255),
					        new OleDbParameter("@CT_JGL", OleDbType.VarChar,255),
					        new OleDbParameter("@CT_PL", OleDbType.VarChar,255),
					        new OleDbParameter("@CT_QCJNJS", OleDbType.VarChar,255),
					        new OleDbParameter("@CT_QGS", OleDbType.VarChar,255),
					        new OleDbParameter("@CT_QTXX", OleDbType.VarChar,255),
					        new OleDbParameter("@CT_SJGKRLXHL", OleDbType.VarChar,255),
					        new OleDbParameter("@CT_SQGKRLXHL", OleDbType.VarChar,255),
					        new OleDbParameter("@CT_ZHGKCO2PFL", OleDbType.VarChar,255),
					        new OleDbParameter("@CT_ZHGKRLXHL", OleDbType.VarChar,255),
					        new OleDbParameter("@CT_BSQDWS", OleDbType.VarChar,255),
					        new OleDbParameter("@CLXH", OleDbType.VarChar,255),
					        new OleDbParameter("@UNIQUE_CODE", OleDbType.VarChar,255)
                            };

                            parameters[0].Value = dr["QCSCQY"];
                            parameters[1].Value = dr["JYBGBH"];
                            parameters[2].Value = dr["JKQCZJXS"];
                            parameters[3].Value = dr["HGSPBM"];
                            parameters[4].Value = dr["CLZL"];
                            parameters[5].Value = dr["YYC"];
                            parameters[6].Value = dr["QDXS"];
                            parameters[7].Value = dr["ZWPS"];
                            parameters[8].Value = dr["ZCZBZL"];
                            parameters[9].Value = dr["ZDSJZZL"];
                            parameters[10].Value = dr["ZGCS"];
                            parameters[11].Value = dr["EDZK"];
                            parameters[12].Value = dr["LTGG"];
                            parameters[13].Value = dr["LJ"];
                            parameters[14].Value = dr["JYJGMC"];
                            parameters[15].Value = dr["TYMC"];
                            parameters[16].Value = dr["ZJ"];
                            parameters[17].Value = dr["RLLX"];
                            parameters[18].Value = dr["CT_BSQXS"];
                            parameters[19].Value = dr["CT_EDGL"];
                            parameters[20].Value = dr["CT_FDJXH"];
                            parameters[21].Value = dr["CT_JGL"];
                            parameters[22].Value = dr["CT_PL"];
                            parameters[23].Value = dr["CT_QCJNJS"];
                            parameters[24].Value = dr["CT_QGS"];
                            parameters[25].Value = dr["CT_QTXX"];
                            parameters[26].Value = dr["CT_SJGKRLXHL"];
                            parameters[27].Value = dr["CT_SQGKRLXHL"];
                            parameters[28].Value = dr["CT_ZHGKCO2PFL"];
                            parameters[29].Value = dr["CT_ZHGKRLXHL"];
                            parameters[30].Value = dr["CT_BSQDWS"];
                            parameters[31].Value = dr["CLXH"];
                            parameters[32].Value = dr["UNIQUE_CODE"];

                            AccessHelper.ExecuteNonQuery(AccessHelper.conn, strSql.ToString(), parameters);
                            succCount++;

                            mainUpdateList.Add(clxh);
                            #endregion
                        }
                    }
                }

                #region 非插电式
                //            if (dtFcdsHhdl != null && dtFcdsHhdl.Rows.Count > 0)
                //            {
                //                string error = string.Empty;
                //                foreach (DataRow dr in dtFcdsHhdl.Rows)
                //                {
                //                    error = VerifyData(dr, drFcdsHhdl, "UPDATE");      //单行验证
                //                    if (!string.IsNullOrEmpty(error))
                //                    {
                //                        msg += error;
                //                    }
                //                    else
                //                    {
                //                        #region UPDATE
                //                        mainId = dr["MAIN_ID"].ToString();
                //                        string sqlFcds = @"UPDATE MAIN_FCDSHHDL
                //                                        SET JKQCZJXS=@JKQCZJXS,QCSCQY=@QCSCQY,CLXH=@CLXH,CLZL=@CLZL,RLLX=@RLLX,
                //                                            ZCZBZL=@ZCZBZL,ZGCS=@ZGCS,LTGG=@LTGG,ZJ=@ZJ,TYMC=@TYMC,
                //                                            YYC=@YYC,ZWPS=@ZWPS,ZDSJZZL=@ZDSJZZL,EDZK=@EDZK,LJ=@LJ,
                //                                            QDXS=@QDXS,STATUS=@STATUS,JYJGMC=@JYJGMC,JYBGBH=@JYBGBH,
                //                                            FCDS_HHDL_BSQDWS=@FCDS_HHDL_BSQDWS,FCDS_HHDL_BSQXS=@FCDS_HHDL_BSQXS,
                //                                            FCDS_HHDL_CDDMSXZGCS=@FCDS_HHDL_CDDMSXZGCS,FCDS_HHDL_CDDMSXZHGKXSLC=@FCDS_HHDL_CDDMSXZHGKXSLC,
                //                                            FCDS_HHDL_DLXDCBNL=@FCDS_HHDL_DLXDCBNL,FCDS_HHDL_DLXDCZBCDY=@FCDS_HHDL_DLXDCZBCDY,
                //                                            FCDS_HHDL_DLXDCZZL=@FCDS_HHDL_DLXDCZZL,FCDS_HHDL_DLXDCZZNL=@FCDS_HHDL_DLXDCZZNL,
                //                                            FCDS_HHDL_EDGL=@FCDS_HHDL_EDGL,FCDS_HHDL_FDJXH=@FCDS_HHDL_FDJXH,
                //                                            FCDS_HHDL_HHDLJGXS=@FCDS_HHDL_HHDLJGXS,FCDS_HHDL_HHDLZDDGLB=@FCDS_HHDL_HHDLZDDGLB,
                //                                            FCDS_HHDL_JGL=@FCDS_HHDL_JGL,FCDS_HHDL_PL=@FCDS_HHDL_PL,FCDS_HHDL_QDDJEDGL=@FCDS_HHDL_QDDJEDGL,
                //                                            FCDS_HHDL_QDDJFZNJ=@FCDS_HHDL_QDDJFZNJ,FCDS_HHDL_QDDJLX=@FCDS_HHDL_QDDJLX,FCDS_HHDL_QGS=@FCDS_HHDL_QGS,
                //                                            FCDS_HHDL_SJGKRLXHL=@FCDS_HHDL_SJGKRLXHL,FCDS_HHDL_SQGKRLXHL=@FCDS_HHDL_SQGKRLXHL,
                //                                            FCDS_HHDL_XSMSSDXZGN=@FCDS_HHDL_XSMSSDXZGN,FCDS_HHDL_ZHGKRLXHL=@FCDS_HHDL_ZHGKRLXHL,
                //                                            FCDS_HHDL_ZHKGCO2PL=@FCDS_HHDL_ZHKGCO2PL,UPDATE_BY=@UPDATE_BY,UPDATETIME=@UPDATETIME,
                //                                            HGSPBM=@HGSPBM,CT_QTXX=@CT_QTXX
                //                                         WHERE MAIN_ID=@MAIN_ID";

                //                        OleDbParameter[] parameters = {
                //                        new OleDbParameter("@JKQCZJXS", OleDbType.VarChar,200),
                //                        new OleDbParameter("@QCSCQY", OleDbType.VarChar,200),
                //                        new OleDbParameter("@CLXH", OleDbType.VarChar,100),
                //                        new OleDbParameter("@CLZL", OleDbType.VarChar,200),
                //                        new OleDbParameter("@RLLX", OleDbType.VarChar,200),

                //                        new OleDbParameter("@ZCZBZL", OleDbType.VarChar,255),
                //                        new OleDbParameter("@ZGCS", OleDbType.VarChar,255),
                //                        new OleDbParameter("@LTGG", OleDbType.VarChar,200),
                //                        new OleDbParameter("@ZJ", OleDbType.VarChar,255),
                //                        new OleDbParameter("@TYMC", OleDbType.VarChar,200),

                //                        new OleDbParameter("@YYC", OleDbType.VarChar,200),
                //                        new OleDbParameter("@ZWPS", OleDbType.VarChar,255),
                //                        new OleDbParameter("@ZDSJZZL", OleDbType.VarChar,255),
                //                        new OleDbParameter("@EDZK", OleDbType.VarChar,255),
                //                        new OleDbParameter("@LJ", OleDbType.VarChar,255),

                //                        new OleDbParameter("@QDXS", OleDbType.VarChar,200),
                //                        new OleDbParameter("@STATUS", OleDbType.VarChar,1),
                //                        new OleDbParameter("@JYJGMC", OleDbType.VarChar,255),
                //                        new OleDbParameter("@JYBGBH", OleDbType.VarChar,255),
                //                        new OleDbParameter("@FCDS_HHDL_BSQDWS", OleDbType.VarChar,200),

                //                        new OleDbParameter("@FCDS_HHDL_BSQXS", OleDbType.VarChar,200),
                //                        new OleDbParameter("@FCDS_HHDL_CDDMSXZGCS", OleDbType.VarChar,200),
                //                        new OleDbParameter("@FCDS_HHDL_CDDMSXZHGKXSLC", OleDbType.VarChar,200),
                //                        new OleDbParameter("@FCDS_HHDL_DLXDCBNL", OleDbType.VarChar,200),
                //                        new OleDbParameter("@FCDS_HHDL_DLXDCZBCDY", OleDbType.VarChar,200),

                //                        new OleDbParameter("@FCDS_HHDL_DLXDCZZL", OleDbType.VarChar,200),
                //                        new OleDbParameter("@FCDS_HHDL_DLXDCZZNL", OleDbType.VarChar,200),
                //                        new OleDbParameter("@FCDS_HHDL_EDGL", OleDbType.VarChar,200),
                //                        new OleDbParameter("@FCDS_HHDL_FDJXH", OleDbType.VarChar,200),
                //                        new OleDbParameter("@FCDS_HHDL_HHDLJGXS", OleDbType.VarChar,200),

                //                        new OleDbParameter("@FCDS_HHDL_HHDLZDDGLB", OleDbType.VarChar,200),
                //                        new OleDbParameter("@FCDS_HHDL_JGL", OleDbType.VarChar,200),
                //                        new OleDbParameter("@FCDS_HHDL_PL", OleDbType.VarChar,200),
                //                        new OleDbParameter("@FCDS_HHDL_QDDJEDGL", OleDbType.VarChar,200),
                //                        new OleDbParameter("@FCDS_HHDL_QDDJFZNJ", OleDbType.VarChar,200),

                //                        new OleDbParameter("@FCDS_HHDL_QDDJLX", OleDbType.VarChar,200),
                //                        new OleDbParameter("@FCDS_HHDL_QGS", OleDbType.VarChar,200),
                //                        new OleDbParameter("@FCDS_HHDL_SJGKRLXHL", OleDbType.VarChar,200),
                //                        new OleDbParameter("@FCDS_HHDL_SQGKRLXHL", OleDbType.VarChar,200),
                //                        new OleDbParameter("@FCDS_HHDL_XSMSSDXZGN", OleDbType.VarChar,200),

                //                        new OleDbParameter("@FCDS_HHDL_ZHGKRLXHL", OleDbType.VarChar,200),
                //                        new OleDbParameter("@FCDS_HHDL_ZHKGCO2PL", OleDbType.VarChar,200),
                //                        new OleDbParameter("@UPDATE_BY", OleDbType.VarChar,200),
                //                        new OleDbParameter("@UPDATETIME", OleDbType.Date),
                //                        new OleDbParameter("@HGSPBM", OleDbType.VarChar,50),
                //                        new OleDbParameter("@CT_QTXX", OleDbType.VarChar,255),

                //                        new OleDbParameter("@MAIN_ID", OleDbType.VarChar,50)
                //                                                      };

                //                        parameters[0].Value = dr["JKQCZJXS"];
                //                        parameters[1].Value = dr["QCSCQY"];
                //                        parameters[2].Value = dr["CLXH"];
                //                        parameters[3].Value = dr["CLZL"];
                //                        parameters[4].Value = dr["RLLX"];

                //                        parameters[5].Value = dr["ZCZBZL"];
                //                        parameters[6].Value = dr["ZGCS"];
                //                        parameters[7].Value = dr["LTGG"];
                //                        parameters[8].Value = dr["ZJ"];
                //                        parameters[9].Value = dr["TYMC"];

                //                        parameters[10].Value = dr["YYC"];
                //                        parameters[11].Value = dr["ZWPS"];
                //                        parameters[12].Value = dr["ZDSJZZL"];
                //                        parameters[13].Value = dr["EDZK"];
                //                        parameters[14].Value = dr["LJ"];

                //                        parameters[15].Value = dr["QDXS"];
                //                        parameters[16].Value = (int)Status.待上报;
                //                        parameters[17].Value = dr["JYJGMC"];
                //                        parameters[18].Value = dr["JYBGBH"];
                //                        parameters[19].Value = dr["FCDS_HHDL_BSQDWS"];

                //                        parameters[20].Value = dr["FCDS_HHDL_BSQXS"];
                //                        parameters[21].Value = dr["FCDS_HHDL_CDDMSXZGCS"];
                //                        parameters[22].Value = dr["FCDS_HHDL_CDDMSXZHGKXSLC"];
                //                        parameters[23].Value = dr["FCDS_HHDL_DLXDCBNL"];
                //                        parameters[24].Value = dr["FCDS_HHDL_DLXDCZBCDY"];

                //                        parameters[25].Value = dr["FCDS_HHDL_DLXDCZZL"];
                //                        parameters[26].Value = dr["FCDS_HHDL_DLXDCZZNL"];
                //                        parameters[27].Value = dr["FCDS_HHDL_EDGL"];
                //                        parameters[28].Value = dr["FCDS_HHDL_FDJXH"];
                //                        parameters[29].Value = dr["FCDS_HHDL_HHDLJGXS"];

                //                        parameters[30].Value = dr["FCDS_HHDL_HHDLZDDGLB"];
                //                        parameters[31].Value = dr["FCDS_HHDL_JGL"];
                //                        parameters[32].Value = dr["FCDS_HHDL_PL"];
                //                        parameters[33].Value = dr["FCDS_HHDL_QDDJEDGL"];
                //                        parameters[34].Value = dr["FCDS_HHDL_QDDJFZNJ"];

                //                        parameters[35].Value = dr["FCDS_HHDL_QDDJLX"];
                //                        parameters[36].Value = dr["FCDS_HHDL_QGS"];
                //                        parameters[37].Value = dr["FCDS_HHDL_SJGKRLXHL"];
                //                        parameters[38].Value = dr["FCDS_HHDL_SQGKRLXHL"];
                //                        parameters[39].Value = dr["FCDS_HHDL_XSMSSDXZGN"];

                //                        parameters[40].Value = dr["FCDS_HHDL_ZHGKRLXHL"];
                //                        parameters[41].Value = dr["FCDS_HHDL_ZHKGCO2PL"];
                //                        parameters[42].Value = Utils.localUserId;
                //                        parameters[43].Value = DateTime.Today;
                //                        parameters[44].Value = dr["HGSPBM"];
                //                        parameters[45].Value = dr["CT_QTXX"];
                //                        parameters[46].Value = dr["MAIN_ID"];

                //                        AccessHelper.ExecuteNonQuery(AccessHelper.conn, sqlFcds.ToString(), parameters);
                //                        mainUpdateList.Add(mainId);
                //                        #endregion
                //                    }
                //                }
                //            }
                #endregion

            }
            catch (Exception ex)
            {
                msg += ex.Message + "\r\n";
            }

            if (totalCount - succCount > 0)
            {
                msg += "FAILED-IMPORT";
            }
            string msgSummary = string.Format("共{0}条数据：\r\n \t{1}条修改成功 \r\n \t{2}条修改失败\r\n",
                            totalCount, succCount, totalCount - succCount);
            msg = msgSummary + msg;

            return msg;
        }


        /// <summary>
        /// 修改已经导入的主表信息
        /// </summary>
        /// <param name="ds"></param>
        /// <returns></returns>
        public string UpdateMainData2(DataSet ds, List<string> mainUpdateList)
        {
            int totalCount = 0;
            int succCount = 0;
            string msg = string.Empty;
            string clxh = string.Empty;

            try
            {
                // 传统能源
                DataTable dtCtny = D2D(dictCTNY, ds.Tables[CTNY], CTNY);
                totalCount += dtCtny.Rows.Count;
                succCount += UpdMainData(dtCtny, CTNY, ref msg);

                DataTable dtFcds = D2D(dictFCDSHHDL, ds.Tables[FCDSHHDL], FCDSHHDL);
                totalCount += dtFcds.Rows.Count;
                succCount += UpdMainData(dtFcds, FCDSHHDL, ref msg);

                DataTable dtCds = D2D(dictCDSHHDL, ds.Tables[CDSHHDL], CDSHHDL);
                totalCount += dtCds.Rows.Count;
                succCount += UpdMainData(dtCds, CDSHHDL, ref msg);

                DataTable dtCdd = D2D(dictCDD, ds.Tables[CDD], CDD);
                totalCount += dtCdd.Rows.Count;
                succCount += UpdMainData(dtCdd, CDD, ref msg);

                DataTable dtRldc = D2D(dictRLDC, ds.Tables[RLDC], RLDC);
                totalCount += dtRldc.Rows.Count;
                succCount += UpdMainData(dtRldc, RLDC, ref msg);

            }
            catch (Exception ex)
            {
                msg += ex.Message + "\r\n";
            }

            if (totalCount - succCount > 0)
            {
                msg += "FAILED-IMPORT";
            }
            string msgSummary = string.Format("共{0}条数据：\r\n \t{1}条修改成功 \r\n \t{2}条修改失败\r\n",
                            totalCount, succCount, totalCount - succCount);
            msg = msgSummary + msg;

            return msg;
        }

        public int UpdMainData(DataTable dt, string rlzl, ref string msg)
        {
            int succCount = 0;
            if (string.IsNullOrEmpty(msg))
            {
                msg = string.Empty;
            }

            try
            {
                // 转换表头（用户模板中的表头转为数据库列名）
                DataRow[] tdr = checkData.Select("FUEL_TYPE='" + rlzl + "' and STATUS=1");

                if (dt != null && dt.Rows.Count > 0)
                {
                    string error = string.Empty;
                    foreach (DataRow dr in dt.Rows)
                    {
                        error = VerifyData(dr, tdr, "UPDATE");      //单行验证
                        if (!string.IsNullOrEmpty(error))
                        {
                            msg += error;
                        }
                        else
                        {
                            if (rlzl.Equals(CTNY))
                            {
                                #region 传统能源
                                try
                                {
                                    #region update

                                    StringBuilder strSql = new StringBuilder();
                                    strSql.Append("update CTNY_MAIN set ");
                                    strSql.Append("QCSCQY=@QCSCQY,");
                                    strSql.Append("JYBGBH=@JYBGBH,");
                                    strSql.Append("JKQCZJXS=@JKQCZJXS,");
                                    strSql.Append("CLXH=@CLXH,");
                                    strSql.Append("HGSPBM=@HGSPBM,");
                                    strSql.Append("CLZL=@CLZL,");
                                    strSql.Append("YYC=@YYC,");
                                    strSql.Append("QDXS=@QDXS,");
                                    strSql.Append("ZWPS=@ZWPS,");
                                    strSql.Append("ZCZBZL=@ZCZBZL,");
                                    strSql.Append("ZDSJZZL=@ZDSJZZL,");
                                    strSql.Append("ZGCS=@ZGCS,");
                                    strSql.Append("EDZK=@EDZK,");
                                    strSql.Append("LTGG=@LTGG,");
                                    strSql.Append("LJ=@LJ,");
                                    strSql.Append("JYJGMC=@JYJGMC,");
                                    strSql.Append("TYMC=@TYMC,");
                                    strSql.Append("ZJ=@ZJ,");
                                    strSql.Append("RLLX=@RLLX,");
                                    strSql.Append("CT_BSQXS=@CT_BSQXS,");
                                    strSql.Append("CT_EDGL=@CT_EDGL,");
                                    strSql.Append("CT_FDJXH=@CT_FDJXH,");
                                    strSql.Append("CT_JGL=@CT_JGL,");
                                    strSql.Append("CT_PL=@CT_PL,");
                                    strSql.Append("CT_QGS=@CT_QGS,");
                                    strSql.Append("CT_QTXX=@CT_QTXX,");
                                    strSql.Append("CT_SJGKRLXHL=@CT_SJGKRLXHL,");
                                    strSql.Append("CT_SQGKRLXHL=@CT_SQGKRLXHL,");
                                    strSql.Append("CT_ZHGKCO2PFL=@CT_ZHGKCO2PFL,");
                                    strSql.Append("CT_ZHGKRLXHL=@CT_ZHGKRLXHL,");
                                    strSql.Append("CT_BSQDWS=@CT_BSQDWS,");
                                    strSql.Append("UPDATETIME=@UPDATETIME");
                                    strSql.Append(" where UNIQUE_CODE=@UNIQUE_CODE ");
                                    OleDbParameter[] parameters = {
					                    new OleDbParameter("@QCSCQY", OleDbType.VarChar,255),
					                    new OleDbParameter("@JYBGBH", OleDbType.VarChar,255),
					                    new OleDbParameter("@JKQCZJXS", OleDbType.VarChar,255),
					                    new OleDbParameter("@CLXH", OleDbType.VarChar,255),
					                    new OleDbParameter("@HGSPBM", OleDbType.VarChar,255),
					                    new OleDbParameter("@CLZL", OleDbType.VarChar,255),
					                    new OleDbParameter("@YYC", OleDbType.VarChar,255),
					                    new OleDbParameter("@QDXS", OleDbType.VarChar,255),
					                    new OleDbParameter("@ZWPS", OleDbType.VarChar,255),
					                    new OleDbParameter("@ZCZBZL", OleDbType.VarChar,255),
					                    new OleDbParameter("@ZDSJZZL", OleDbType.VarChar,255),
					                    new OleDbParameter("@ZGCS", OleDbType.VarChar,255),
					                    new OleDbParameter("@EDZK", OleDbType.VarChar,255),
					                    new OleDbParameter("@LTGG", OleDbType.VarChar,200),
					                    new OleDbParameter("@LJ", OleDbType.VarChar,255),
					                    new OleDbParameter("@JYJGMC", OleDbType.VarChar,255),
					                    new OleDbParameter("@TYMC", OleDbType.VarChar,255),
					                    new OleDbParameter("@ZJ", OleDbType.VarChar,255),
					                    new OleDbParameter("@RLLX", OleDbType.VarChar,200),
					                    new OleDbParameter("@CT_BSQXS", OleDbType.VarChar,255),
					                    new OleDbParameter("@CT_EDGL", OleDbType.VarChar,255),
					                    new OleDbParameter("@CT_FDJXH", OleDbType.VarChar,255),
					                    new OleDbParameter("@CT_JGL", OleDbType.VarChar,255),
					                    new OleDbParameter("@CT_PL", OleDbType.VarChar,255),
					                    new OleDbParameter("@CT_QGS", OleDbType.VarChar,255),
					                    new OleDbParameter("@CT_QTXX", OleDbType.VarChar,255),
					                    new OleDbParameter("@CT_SJGKRLXHL", OleDbType.VarChar,255),
					                    new OleDbParameter("@CT_SQGKRLXHL", OleDbType.VarChar,255),
					                    new OleDbParameter("@CT_ZHGKCO2PFL", OleDbType.VarChar,255),
					                    new OleDbParameter("@CT_ZHGKRLXHL", OleDbType.VarChar,255),
					                    new OleDbParameter("@CT_BSQDWS", OleDbType.VarChar,255),
					                    new OleDbParameter("@UPDATETIME", OleDbType.Date),
					                    new OleDbParameter("@UNIQUE_CODE", OleDbType.VarChar,255)
                                    };
                                    parameters[0].Value = dr["QCSCQY"];
                                    parameters[1].Value = dr["JYBGBH"];
                                    parameters[2].Value = dr["JKQCZJXS"];
                                    parameters[3].Value = dr["CLXH"];
                                    parameters[4].Value = dr["HGSPBM"];
                                    parameters[5].Value = dr["CLZL"];
                                    parameters[6].Value = dr["YYC"];
                                    parameters[7].Value = dr["QDXS"];
                                    parameters[8].Value = dr["ZWPS"];
                                    parameters[9].Value = dr["ZCZBZL"];
                                    parameters[10].Value = dr["ZDSJZZL"];
                                    parameters[11].Value = dr["ZGCS"];
                                    parameters[12].Value = dr["EDZK"];
                                    parameters[13].Value = dr["LTGG"];
                                    parameters[14].Value = dr["LJ"];
                                    parameters[15].Value = dr["JYJGMC"];
                                    parameters[16].Value = dr["TYMC"];
                                    parameters[17].Value = dr["ZJ"];
                                    parameters[18].Value = dr["RLLX"];
                                    parameters[19].Value = dr["CT_BSQXS"];
                                    parameters[20].Value = dr["CT_EDGL"];
                                    parameters[21].Value = dr["CT_FDJXH"];
                                    parameters[22].Value = dr["CT_JGL"];
                                    parameters[23].Value = dr["CT_PL"];
                                    parameters[24].Value = dr["CT_QGS"];
                                    parameters[25].Value = dr["CT_QTXX"];
                                    parameters[26].Value = dr["CT_SJGKRLXHL"];
                                    parameters[27].Value = dr["CT_SQGKRLXHL"];
                                    parameters[28].Value = dr["CT_ZHGKCO2PFL"];
                                    parameters[29].Value = dr["CT_ZHGKRLXHL"];
                                    parameters[30].Value = dr["CT_BSQDWS"];
                                    parameters[31].Value = Convert.ToDateTime(DateTime.Now, CultureInfo.InvariantCulture);
                                    parameters[32].Value = dr["UNIQUE_CODE"];

                                    AccessHelper.ExecuteNonQuery(AccessHelper.conn, strSql.ToString(), parameters);
                                    succCount++;

                                    #endregion
                                }
                                catch (Exception ex)
                                {
                                    msg += ex.Message + "\r\n";
                                }
                                #endregion
                            }
                            else if (rlzl.Equals(FCDSHHDL))
                            {
                                #region 非插电式混合动力
                                try
                                {
                                    #region update

                                    StringBuilder strSql = new StringBuilder();
                                    strSql.Append("update FCDS_MAIN set ");
                                    strSql.Append("QCSCQY=@QCSCQY,");
                                    strSql.Append("JYBGBH=@JYBGBH,");
                                    strSql.Append("JKQCZJXS=@JKQCZJXS,");
                                    strSql.Append("CLXH=@CLXH,");
                                    strSql.Append("HGSPBM=@HGSPBM,");
                                    strSql.Append("CLZL=@CLZL,");
                                    strSql.Append("YYC=@YYC,");
                                    strSql.Append("QDXS=@QDXS,");
                                    strSql.Append("ZWPS=@ZWPS,");
                                    strSql.Append("ZCZBZL=@ZCZBZL,");
                                    strSql.Append("ZDSJZZL=@ZDSJZZL,");
                                    strSql.Append("ZGCS=@ZGCS,");
                                    strSql.Append("EDZK=@EDZK,");
                                    strSql.Append("LTGG=@LTGG,");
                                    strSql.Append("LJ=@LJ,");
                                    strSql.Append("JYJGMC=@JYJGMC,");
                                    strSql.Append("TYMC=@TYMC,");
                                    strSql.Append("ZJ=@ZJ,");
                                    strSql.Append("RLLX=@RLLX,");
                                    strSql.Append("FCDS_HHDL_BSQDWS=@FCDS_HHDL_BSQDWS,");
                                    strSql.Append("FCDS_HHDL_BSQXS=@FCDS_HHDL_BSQXS,");
                                    strSql.Append("FCDS_HHDL_CDDMSXZGCS=@FCDS_HHDL_CDDMSXZGCS,");
                                    strSql.Append("FCDS_HHDL_CDDMSXZHGKXSLC=@FCDS_HHDL_CDDMSXZHGKXSLC,");
                                    strSql.Append("FCDS_HHDL_DLXDCBNL=@FCDS_HHDL_DLXDCBNL,");
                                    strSql.Append("FCDS_HHDL_DLXDCZBCDY=@FCDS_HHDL_DLXDCZBCDY,");
                                    strSql.Append("FCDS_HHDL_DLXDCZZL=@FCDS_HHDL_DLXDCZZL,");
                                    strSql.Append("FCDS_HHDL_DLXDCZZNL=@FCDS_HHDL_DLXDCZZNL,");
                                    strSql.Append("FCDS_HHDL_EDGL=@FCDS_HHDL_EDGL,");
                                    strSql.Append("FCDS_HHDL_FDJXH=@FCDS_HHDL_FDJXH,");
                                    strSql.Append("FCDS_HHDL_HHDLJGXS=@FCDS_HHDL_HHDLJGXS,");
                                    strSql.Append("FCDS_HHDL_HHDLZDDGLB=@FCDS_HHDL_HHDLZDDGLB,");
                                    strSql.Append("FCDS_HHDL_JGL=@FCDS_HHDL_JGL,");
                                    strSql.Append("FCDS_HHDL_PL=@FCDS_HHDL_PL,");
                                    strSql.Append("FCDS_HHDL_QDDJEDGL=@FCDS_HHDL_QDDJEDGL,");
                                    strSql.Append("FCDS_HHDL_QDDJFZNJ=@FCDS_HHDL_QDDJFZNJ,");
                                    strSql.Append("FCDS_HHDL_QDDJLX=@FCDS_HHDL_QDDJLX,");
                                    strSql.Append("FCDS_HHDL_QGS=@FCDS_HHDL_QGS,");
                                    strSql.Append("FCDS_HHDL_SJGKRLXHL=@FCDS_HHDL_SJGKRLXHL,");
                                    strSql.Append("FCDS_HHDL_SQGKRLXHL=@FCDS_HHDL_SQGKRLXHL,");
                                    strSql.Append("FCDS_HHDL_XSMSSDXZGN=@FCDS_HHDL_XSMSSDXZGN,");
                                    strSql.Append("FCDS_HHDL_ZHGKRLXHL=@FCDS_HHDL_ZHGKRLXHL,");
                                    strSql.Append("FCDS_HHDL_ZHKGCO2PL=@FCDS_HHDL_ZHKGCO2PL,");
                                    strSql.Append("UPDATETIME=@UPDATETIME");
                                    strSql.Append(" where UNIQUE_CODE=@UNIQUE_CODE ");
                                    OleDbParameter[] parameters = {
					                    new OleDbParameter("@QCSCQY", OleDbType.VarChar,255),
					                    new OleDbParameter("@JYBGBH", OleDbType.VarChar,255),
					                    new OleDbParameter("@JKQCZJXS", OleDbType.VarChar,255),
					                    new OleDbParameter("@CLXH", OleDbType.VarChar,255),
					                    new OleDbParameter("@HGSPBM", OleDbType.VarChar,255),
					                    new OleDbParameter("@CLZL", OleDbType.VarChar,255),
					                    new OleDbParameter("@YYC", OleDbType.VarChar,255),
					                    new OleDbParameter("@QDXS", OleDbType.VarChar,255),
					                    new OleDbParameter("@ZWPS", OleDbType.VarChar,255),
					                    new OleDbParameter("@ZCZBZL", OleDbType.VarChar,255),
					                    new OleDbParameter("@ZDSJZZL", OleDbType.VarChar,255),
					                    new OleDbParameter("@ZGCS", OleDbType.VarChar,255),
					                    new OleDbParameter("@EDZK", OleDbType.VarChar,255),
					                    new OleDbParameter("@LTGG", OleDbType.VarChar,200),
					                    new OleDbParameter("@LJ", OleDbType.VarChar,255),
					                    new OleDbParameter("@JYJGMC", OleDbType.VarChar,255),
					                    new OleDbParameter("@TYMC", OleDbType.VarChar,255),
					                    new OleDbParameter("@ZJ", OleDbType.VarChar,255),
					                    new OleDbParameter("@RLLX", OleDbType.VarChar,200),
					                    new OleDbParameter("@FCDS_HHDL_BSQDWS", OleDbType.VarChar,255),
					                    new OleDbParameter("@FCDS_HHDL_BSQXS", OleDbType.VarChar,255),
					                    new OleDbParameter("@FCDS_HHDL_CDDMSXZGCS", OleDbType.VarChar,255),
					                    new OleDbParameter("@FCDS_HHDL_CDDMSXZHGKXSLC", OleDbType.VarChar,255),
					                    new OleDbParameter("@FCDS_HHDL_DLXDCBNL", OleDbType.VarChar,255),
					                    new OleDbParameter("@FCDS_HHDL_DLXDCZBCDY", OleDbType.VarChar,255),
					                    new OleDbParameter("@FCDS_HHDL_DLXDCZZL", OleDbType.VarChar,255),
					                    new OleDbParameter("@FCDS_HHDL_DLXDCZZNL", OleDbType.VarChar,255),
					                    new OleDbParameter("@FCDS_HHDL_EDGL", OleDbType.VarChar,255),
					                    new OleDbParameter("@FCDS_HHDL_FDJXH", OleDbType.VarChar,255),
					                    new OleDbParameter("@FCDS_HHDL_HHDLJGXS", OleDbType.VarChar,255),
					                    new OleDbParameter("@FCDS_HHDL_HHDLZDDGLB", OleDbType.VarChar,255),
					                    new OleDbParameter("@FCDS_HHDL_JGL", OleDbType.VarChar,255),
					                    new OleDbParameter("@FCDS_HHDL_PL", OleDbType.VarChar,255),
					                    new OleDbParameter("@FCDS_HHDL_QDDJEDGL", OleDbType.VarChar,255),
					                    new OleDbParameter("@FCDS_HHDL_QDDJFZNJ", OleDbType.VarChar,255),
					                    new OleDbParameter("@FCDS_HHDL_QDDJLX", OleDbType.VarChar,255),
					                    new OleDbParameter("@FCDS_HHDL_QGS", OleDbType.VarChar,255),
					                    new OleDbParameter("@FCDS_HHDL_SJGKRLXHL", OleDbType.VarChar,255),
					                    new OleDbParameter("@FCDS_HHDL_SQGKRLXHL", OleDbType.VarChar,255),
					                    new OleDbParameter("@FCDS_HHDL_XSMSSDXZGN", OleDbType.VarChar,255),
					                    new OleDbParameter("@FCDS_HHDL_ZHGKRLXHL", OleDbType.VarChar,255),
					                    new OleDbParameter("@FCDS_HHDL_ZHKGCO2PL", OleDbType.VarChar,255),
					                    new OleDbParameter("@UPDATETIME", OleDbType.Date),
					                    new OleDbParameter("@UNIQUE_CODE", OleDbType.VarChar,255)
                                    };
                                    parameters[0].Value = dr["QCSCQY"];
                                    parameters[1].Value = dr["JYBGBH"];
                                    parameters[2].Value = dr["JKQCZJXS"];
                                    parameters[3].Value = dr["CLXH"];
                                    parameters[4].Value = dr["HGSPBM"];
                                    parameters[5].Value = dr["CLZL"];
                                    parameters[6].Value = dr["YYC"];
                                    parameters[7].Value = dr["QDXS"];
                                    parameters[8].Value = dr["ZWPS"];
                                    parameters[9].Value = dr["ZCZBZL"];
                                    parameters[10].Value = dr["ZDSJZZL"];
                                    parameters[11].Value = dr["ZGCS"];
                                    parameters[12].Value = dr["EDZK"];
                                    parameters[13].Value = dr["LTGG"];
                                    parameters[14].Value = dr["LJ"];
                                    parameters[15].Value = dr["JYJGMC"];
                                    parameters[16].Value = dr["TYMC"];
                                    parameters[17].Value = dr["ZJ"];
                                    parameters[18].Value = dr["RLLX"];
                                    parameters[19].Value = dr["FCDS_HHDL_BSQDWS"];
                                    parameters[20].Value = dr["FCDS_HHDL_BSQXS"];
                                    parameters[21].Value = dr["FCDS_HHDL_CDDMSXZGCS"];
                                    parameters[22].Value = dr["FCDS_HHDL_CDDMSXZHGKXSLC"];
                                    parameters[23].Value = dr["FCDS_HHDL_DLXDCBNL"];
                                    parameters[24].Value = dr["FCDS_HHDL_DLXDCZBCDY"];
                                    parameters[25].Value = dr["FCDS_HHDL_DLXDCZZL"];
                                    parameters[26].Value = dr["FCDS_HHDL_DLXDCZZNL"];
                                    parameters[27].Value = dr["FCDS_HHDL_EDGL"];
                                    parameters[28].Value = dr["FCDS_HHDL_FDJXH"];
                                    parameters[29].Value = dr["FCDS_HHDL_HHDLJGXS"];
                                    parameters[30].Value = dr["FCDS_HHDL_HHDLZDDGLB"];
                                    parameters[31].Value = dr["FCDS_HHDL_JGL"];
                                    parameters[32].Value = dr["FCDS_HHDL_PL"];
                                    parameters[33].Value = dr["FCDS_HHDL_QDDJEDGL"];
                                    parameters[34].Value = dr["FCDS_HHDL_QDDJFZNJ"];
                                    parameters[35].Value = dr["FCDS_HHDL_QDDJLX"];
                                    parameters[36].Value = dr["FCDS_HHDL_QGS"];
                                    parameters[37].Value = dr["FCDS_HHDL_SJGKRLXHL"];
                                    parameters[38].Value = dr["FCDS_HHDL_SQGKRLXHL"];
                                    parameters[39].Value = dr["FCDS_HHDL_XSMSSDXZGN"];
                                    parameters[40].Value = dr["FCDS_HHDL_ZHGKRLXHL"];
                                    parameters[41].Value = dr["FCDS_HHDL_ZHKGCO2PL"];
                                    parameters[42].Value = Convert.ToDateTime(DateTime.Now, CultureInfo.InvariantCulture);
                                    parameters[43].Value = dr["UNIQUE_CODE"];

                                    AccessHelper.ExecuteNonQuery(AccessHelper.conn, strSql.ToString(), parameters);
                                    succCount++;

                                    #endregion
                                }
                                catch (Exception ex)
                                {
                                    msg += ex.Message + "\r\n";
                                }
                                #endregion
                            }
                            else if (rlzl.Equals(CDSHHDL))
                            {
                                #region 插电式混合动力
                                try
                                {
                                    #region update

                                    StringBuilder strSql = new StringBuilder();
                                    strSql.Append("update CDS_MAIN set ");
                                    strSql.Append("QCSCQY=@QCSCQY,");
                                    strSql.Append("JYBGBH=@JYBGBH,");
                                    strSql.Append("JKQCZJXS=@JKQCZJXS,");
                                    strSql.Append("CLXH=@CLXH,");
                                    strSql.Append("HGSPBM=@HGSPBM,");
                                    strSql.Append("CLZL=@CLZL,");
                                    strSql.Append("YYC=@YYC,");
                                    strSql.Append("QDXS=@QDXS,");
                                    strSql.Append("ZWPS=@ZWPS,");
                                    strSql.Append("ZCZBZL=@ZCZBZL,");
                                    strSql.Append("ZDSJZZL=@ZDSJZZL,");
                                    strSql.Append("ZGCS=@ZGCS,");
                                    strSql.Append("EDZK=@EDZK,");
                                    strSql.Append("LTGG=@LTGG,");
                                    strSql.Append("LJ=@LJ,");
                                    strSql.Append("JYJGMC=@JYJGMC,");
                                    strSql.Append("TYMC=@TYMC,");
                                    strSql.Append("ZJ=@ZJ,");
                                    strSql.Append("RLLX=@RLLX,");
                                    strSql.Append("CDS_HHDL_BSQDWS=@CDS_HHDL_BSQDWS,");
                                    strSql.Append("CDS_HHDL_BSQXS=@CDS_HHDL_BSQXS,");
                                    strSql.Append("CDS_HHDL_CDDMSXZGCS=@CDS_HHDL_CDDMSXZGCS,");
                                    strSql.Append("CDS_HHDL_CDDMSXZHGKXSLC=@CDS_HHDL_CDDMSXZHGKXSLC,");
                                    strSql.Append("CDS_HHDL_DLXDCBNL=@CDS_HHDL_DLXDCBNL,");
                                    strSql.Append("CDS_HHDL_DLXDCZBCDY=@CDS_HHDL_DLXDCZBCDY,");
                                    strSql.Append("CDS_HHDL_DLXDCZZL=@CDS_HHDL_DLXDCZZL,");
                                    strSql.Append("CDS_HHDL_DLXDCZZNL=@CDS_HHDL_DLXDCZZNL,");
                                    strSql.Append("CDS_HHDL_EDGL=@CDS_HHDL_EDGL,");
                                    strSql.Append("CDS_HHDL_FDJXH=@CDS_HHDL_FDJXH,");
                                    strSql.Append("CDS_HHDL_HHDLJGXS=@CDS_HHDL_HHDLJGXS,");
                                    strSql.Append("CDS_HHDL_HHDLZDDGLB=@CDS_HHDL_HHDLZDDGLB,");
                                    strSql.Append("CDS_HHDL_JGL=@CDS_HHDL_JGL,");
                                    strSql.Append("CDS_HHDL_PL=@CDS_HHDL_PL,");
                                    strSql.Append("CDS_HHDL_QDDJEDGL=@CDS_HHDL_QDDJEDGL,");
                                    strSql.Append("CDS_HHDL_QDDJFZNJ=@CDS_HHDL_QDDJFZNJ,");
                                    strSql.Append("CDS_HHDL_QDDJLX=@CDS_HHDL_QDDJLX,");
                                    strSql.Append("CDS_HHDL_QGS=@CDS_HHDL_QGS,");
                                    strSql.Append("CDS_HHDL_XSMSSDXZGN=@CDS_HHDL_XSMSSDXZGN,");
                                    strSql.Append("CDS_HHDL_ZHGKDNXHL=@CDS_HHDL_ZHGKDNXHL,");
                                    strSql.Append("CDS_HHDL_ZHGKRLXHL=@CDS_HHDL_ZHGKRLXHL,");
                                    strSql.Append("CDS_HHDL_ZHKGCO2PL=@CDS_HHDL_ZHKGCO2PL,");
                                    strSql.Append("UPDATETIME=@UPDATETIME");
                                    strSql.Append(" where UNIQUE_CODE=@UNIQUE_CODE ");
                                    OleDbParameter[] parameters = {
					                    new OleDbParameter("@QCSCQY", OleDbType.VarChar,255),
					                    new OleDbParameter("@JYBGBH", OleDbType.VarChar,255),
					                    new OleDbParameter("@JKQCZJXS", OleDbType.VarChar,255),
					                    new OleDbParameter("@CLXH", OleDbType.VarChar,255),
					                    new OleDbParameter("@HGSPBM", OleDbType.VarChar,255),
					                    new OleDbParameter("@CLZL", OleDbType.VarChar,255),
					                    new OleDbParameter("@YYC", OleDbType.VarChar,255),
					                    new OleDbParameter("@QDXS", OleDbType.VarChar,255),
					                    new OleDbParameter("@ZWPS", OleDbType.VarChar,255),
					                    new OleDbParameter("@ZCZBZL", OleDbType.VarChar,255),
					                    new OleDbParameter("@ZDSJZZL", OleDbType.VarChar,255),
					                    new OleDbParameter("@ZGCS", OleDbType.VarChar,255),
					                    new OleDbParameter("@EDZK", OleDbType.VarChar,255),
					                    new OleDbParameter("@LTGG", OleDbType.VarChar,200),
					                    new OleDbParameter("@LJ", OleDbType.VarChar,255),
					                    new OleDbParameter("@JYJGMC", OleDbType.VarChar,255),
					                    new OleDbParameter("@TYMC", OleDbType.VarChar,255),
					                    new OleDbParameter("@ZJ", OleDbType.VarChar,255),
					                    new OleDbParameter("@RLLX", OleDbType.VarChar,200),
					                    new OleDbParameter("@CDS_HHDL_BSQDWS", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDS_HHDL_BSQXS", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDS_HHDL_CDDMSXZGCS", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDS_HHDL_CDDMSXZHGKXSLC", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDS_HHDL_DLXDCBNL", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDS_HHDL_DLXDCZBCDY", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDS_HHDL_DLXDCZZL", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDS_HHDL_DLXDCZZNL", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDS_HHDL_EDGL", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDS_HHDL_FDJXH", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDS_HHDL_HHDLJGXS", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDS_HHDL_HHDLZDDGLB", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDS_HHDL_JGL", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDS_HHDL_PL", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDS_HHDL_QDDJEDGL", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDS_HHDL_QDDJFZNJ", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDS_HHDL_QDDJLX", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDS_HHDL_QGS", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDS_HHDL_XSMSSDXZGN", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDS_HHDL_ZHGKDNXHL", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDS_HHDL_ZHGKRLXHL", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDS_HHDL_ZHKGCO2PL", OleDbType.VarChar,255),
					                    new OleDbParameter("@UPDATETIME", OleDbType.Date),
					                    new OleDbParameter("@UNIQUE_CODE", OleDbType.VarChar,255)
                                    };
                                    parameters[0].Value = dr["QCSCQY"];
                                    parameters[1].Value = dr["JYBGBH"];
                                    parameters[2].Value = dr["JKQCZJXS"];
                                    parameters[3].Value = dr["CLXH"];
                                    parameters[4].Value = dr["HGSPBM"];
                                    parameters[5].Value = dr["CLZL"];
                                    parameters[6].Value = dr["YYC"];
                                    parameters[7].Value = dr["QDXS"];
                                    parameters[8].Value = dr["ZWPS"];
                                    parameters[9].Value = dr["ZCZBZL"];
                                    parameters[10].Value = dr["ZDSJZZL"];
                                    parameters[11].Value = dr["ZGCS"];
                                    parameters[12].Value = dr["EDZK"];
                                    parameters[13].Value = dr["LTGG"];
                                    parameters[14].Value = dr["LJ"];
                                    parameters[15].Value = dr["JYJGMC"];
                                    parameters[16].Value = dr["TYMC"];
                                    parameters[17].Value = dr["ZJ"];
                                    parameters[18].Value = dr["RLLX"];
                                    parameters[19].Value = dr["CDS_HHDL_BSQDWS"];
                                    parameters[20].Value = dr["CDS_HHDL_BSQXS"];
                                    parameters[21].Value = dr["CDS_HHDL_CDDMSXZGCS"];
                                    parameters[22].Value = dr["CDS_HHDL_CDDMSXZHGKXSLC"];
                                    parameters[23].Value = dr["CDS_HHDL_DLXDCBNL"];
                                    parameters[24].Value = dr["CDS_HHDL_DLXDCZBCDY"];
                                    parameters[25].Value = dr["CDS_HHDL_DLXDCZZL"];
                                    parameters[26].Value = dr["CDS_HHDL_DLXDCZZNL"];
                                    parameters[27].Value = dr["CDS_HHDL_EDGL"];
                                    parameters[28].Value = dr["CDS_HHDL_FDJXH"];
                                    parameters[29].Value = dr["CDS_HHDL_HHDLJGXS"];
                                    parameters[30].Value = dr["CDS_HHDL_HHDLZDDGLB"];
                                    parameters[31].Value = dr["CDS_HHDL_JGL"];
                                    parameters[32].Value = dr["CDS_HHDL_PL"];
                                    parameters[33].Value = dr["CDS_HHDL_QDDJEDGL"];
                                    parameters[34].Value = dr["CDS_HHDL_QDDJFZNJ"];
                                    parameters[35].Value = dr["CDS_HHDL_QDDJLX"];
                                    parameters[36].Value = dr["CDS_HHDL_QGS"];
                                    parameters[37].Value = dr["CDS_HHDL_XSMSSDXZGN"];
                                    parameters[38].Value = dr["CDS_HHDL_ZHGKDNXHL"];
                                    parameters[39].Value = dr["CDS_HHDL_ZHGKRLXHL"];
                                    parameters[40].Value = dr["CDS_HHDL_ZHKGCO2PL"];
                                    parameters[41].Value = Convert.ToDateTime(DateTime.Now, CultureInfo.InvariantCulture);
                                    parameters[42].Value = dr["UNIQUE_CODE"];

                                    AccessHelper.ExecuteNonQuery(AccessHelper.conn, strSql.ToString(), parameters);
                                    succCount++;

                                    #endregion
                                }
                                catch (Exception ex)
                                {
                                    msg += ex.Message + "\r\n";
                                }
                                #endregion
                            }
                            else if (rlzl.Equals(CDD))
                            {
                                #region 纯电动
                                try
                                {
                                    #region update

                                    StringBuilder strSql = new StringBuilder();
                                    strSql.Append("update CDD_MAIN set ");
                                    strSql.Append("QCSCQY=@QCSCQY,");
                                    strSql.Append("JYBGBH=@JYBGBH,");
                                    strSql.Append("JKQCZJXS=@JKQCZJXS,");
                                    strSql.Append("CLXH=@CLXH,");
                                    strSql.Append("HGSPBM=@HGSPBM,");
                                    strSql.Append("CLZL=@CLZL,");
                                    strSql.Append("YYC=@YYC,");
                                    strSql.Append("QDXS=@QDXS,");
                                    strSql.Append("ZWPS=@ZWPS,");
                                    strSql.Append("ZCZBZL=@ZCZBZL,");
                                    strSql.Append("ZDSJZZL=@ZDSJZZL,");
                                    strSql.Append("ZGCS=@ZGCS,");
                                    strSql.Append("EDZK=@EDZK,");
                                    strSql.Append("LTGG=@LTGG,");
                                    strSql.Append("LJ=@LJ,");
                                    strSql.Append("JYJGMC=@JYJGMC,");
                                    strSql.Append("TYMC=@TYMC,");
                                    strSql.Append("ZJ=@ZJ,");
                                    strSql.Append("RLLX=@RLLX,");
                                    strSql.Append("CDD_DDQC30FZZGCS=@CDD_DDQC30FZZGCS,");
                                    strSql.Append("CDD_DDXDCZZLYZCZBZLDBZ=@CDD_DDXDCZZLYZCZBZLDBZ,");
                                    strSql.Append("CDD_DLXDCBNL=@CDD_DLXDCBNL,");
                                    strSql.Append("CDD_DLXDCZBCDY=@CDD_DLXDCZBCDY,");
                                    strSql.Append("CDD_DLXDCZEDNL=@CDD_DLXDCZEDNL,");
                                    strSql.Append("CDD_DLXDCZZL=@CDD_DLXDCZZL,");
                                    strSql.Append("CDD_QDDJEDGL=@CDD_QDDJEDGL,");
                                    strSql.Append("CDD_QDDJFZNJ=@CDD_QDDJFZNJ,");
                                    strSql.Append("CDD_QDDJLX=@CDD_QDDJLX,");
                                    strSql.Append("CDD_ZHGKDNXHL=@CDD_ZHGKDNXHL,");
                                    strSql.Append("CDD_ZHGKXSLC=@CDD_ZHGKXSLC,");
                                    strSql.Append("UPDATETIME=@UPDATETIME");
                                    strSql.Append(" where UNIQUE_CODE=@UNIQUE_CODE ");
                                    OleDbParameter[] parameters = {
					                    new OleDbParameter("@QCSCQY", OleDbType.VarChar,255),
					                    new OleDbParameter("@JYBGBH", OleDbType.VarChar,255),
					                    new OleDbParameter("@JKQCZJXS", OleDbType.VarChar,255),
					                    new OleDbParameter("@CLXH", OleDbType.VarChar,255),
					                    new OleDbParameter("@HGSPBM", OleDbType.VarChar,255),
					                    new OleDbParameter("@CLZL", OleDbType.VarChar,255),
					                    new OleDbParameter("@YYC", OleDbType.VarChar,255),
					                    new OleDbParameter("@QDXS", OleDbType.VarChar,255),
					                    new OleDbParameter("@ZWPS", OleDbType.VarChar,255),
					                    new OleDbParameter("@ZCZBZL", OleDbType.VarChar,255),
					                    new OleDbParameter("@ZDSJZZL", OleDbType.VarChar,255),
					                    new OleDbParameter("@ZGCS", OleDbType.VarChar,255),
					                    new OleDbParameter("@EDZK", OleDbType.VarChar,255),
					                    new OleDbParameter("@LTGG", OleDbType.VarChar,200),
					                    new OleDbParameter("@LJ", OleDbType.VarChar,255),
					                    new OleDbParameter("@JYJGMC", OleDbType.VarChar,255),
					                    new OleDbParameter("@TYMC", OleDbType.VarChar,255),
					                    new OleDbParameter("@ZJ", OleDbType.VarChar,255),
					                    new OleDbParameter("@RLLX", OleDbType.VarChar,200),
					                    new OleDbParameter("@CDD_DDQC30FZZGCS", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDD_DDXDCZZLYZCZBZLDBZ", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDD_DLXDCBNL", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDD_DLXDCZBCDY", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDD_DLXDCZEDNL", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDD_DLXDCZZL", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDD_QDDJEDGL", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDD_QDDJFZNJ", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDD_QDDJLX", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDD_ZHGKDNXHL", OleDbType.VarChar,255),
					                    new OleDbParameter("@CDD_ZHGKXSLC", OleDbType.VarChar,255),
					                    new OleDbParameter("@UPDATETIME", OleDbType.Date),
					                    new OleDbParameter("@UNIQUE_CODE", OleDbType.VarChar,255)
                                    };
                                    parameters[0].Value = dr["QCSCQY"];
                                    parameters[1].Value = dr["JYBGBH"];
                                    parameters[2].Value = dr["JKQCZJXS"];
                                    parameters[3].Value = dr["CLXH"];
                                    parameters[4].Value = dr["HGSPBM"];
                                    parameters[5].Value = dr["CLZL"];
                                    parameters[6].Value = dr["YYC"];
                                    parameters[7].Value = dr["QDXS"];
                                    parameters[8].Value = dr["ZWPS"];
                                    parameters[9].Value = dr["ZCZBZL"];
                                    parameters[10].Value = dr["ZDSJZZL"];
                                    parameters[11].Value = dr["ZGCS"];
                                    parameters[12].Value = dr["EDZK"];
                                    parameters[13].Value = dr["LTGG"];
                                    parameters[14].Value = dr["LJ"];
                                    parameters[15].Value = dr["JYJGMC"];
                                    parameters[16].Value = dr["TYMC"];
                                    parameters[17].Value = dr["ZJ"];
                                    parameters[18].Value = dr["RLLX"];
                                    parameters[19].Value = dr["CDD_DDQC30FZZGCS"];
                                    parameters[20].Value = dr["CDD_DDXDCZZLYZCZBZLDBZ"];
                                    parameters[21].Value = dr["CDD_DLXDCBNL"];
                                    parameters[22].Value = dr["CDD_DLXDCZBCDY"];
                                    parameters[23].Value = dr["CDD_DLXDCZEDNL"];
                                    parameters[24].Value = dr["CDD_DLXDCZZL"];
                                    parameters[25].Value = dr["CDD_QDDJEDGL"];
                                    parameters[26].Value = dr["CDD_QDDJFZNJ"];
                                    parameters[27].Value = dr["CDD_QDDJLX"];
                                    parameters[28].Value = dr["CDD_ZHGKDNXHL"];
                                    parameters[29].Value = dr["CDD_ZHGKXSLC"];
                                    parameters[30].Value = Convert.ToDateTime(DateTime.Now, CultureInfo.InvariantCulture);
                                    parameters[31].Value = dr["UNIQUE_CODE"];

                                    AccessHelper.ExecuteNonQuery(AccessHelper.conn, strSql.ToString(), parameters);
                                    succCount++;

                                    #endregion
                                }
                                catch (Exception ex)
                                {
                                    msg += ex.Message + "\r\n";
                                }
                                #endregion
                            }
                            else if (rlzl.Equals(RLDC))
                            {
                                #region 燃料电池
                                try
                                {
                                    #region update

                                    StringBuilder strSql = new StringBuilder();
                                    strSql.Append("update RLDC_MAIN set ");
                                    strSql.Append("QCSCQY=@QCSCQY,");
                                    strSql.Append("JYBGBH=@JYBGBH,");
                                    strSql.Append("JKQCZJXS=@JKQCZJXS,");
                                    strSql.Append("CLXH=@CLXH,");
                                    strSql.Append("HGSPBM=@HGSPBM,");
                                    strSql.Append("CLZL=@CLZL,");
                                    strSql.Append("YYC=@YYC,");
                                    strSql.Append("QDXS=@QDXS,");
                                    strSql.Append("ZWPS=@ZWPS,");
                                    strSql.Append("ZCZBZL=@ZCZBZL,");
                                    strSql.Append("ZDSJZZL=@ZDSJZZL,");
                                    strSql.Append("ZGCS=@ZGCS,");
                                    strSql.Append("EDZK=@EDZK,");
                                    strSql.Append("LTGG=@LTGG,");
                                    strSql.Append("LJ=@LJ,");
                                    strSql.Append("JYJGMC=@JYJGMC,");
                                    strSql.Append("TYMC=@TYMC,");
                                    strSql.Append("ZJ=@ZJ,");
                                    strSql.Append("RLLX=@RLLX,");
                                    strSql.Append("RLDC_CDDMSXZGXSCS=@RLDC_CDDMSXZGXSCS,");
                                    strSql.Append("RLDC_CQPBCGZYL=@RLDC_CQPBCGZYL,");
                                    strSql.Append("RLDC_CQPRJ=@RLDC_CQPRJ,");
                                    strSql.Append("RLDC_DDGLMD=@RLDC_DDGLMD,");
                                    strSql.Append("RLDC_CQPLX=@RLDC_CQPLX,");
                                    strSql.Append("RLDC_DDHHJSTJXXDCZBNL=@RLDC_DDHHJSTJXXDCZBNL,");
                                    strSql.Append("RLDC_DLXDCZZL=@RLDC_DLXDCZZL,");
                                    strSql.Append("RLDC_QDDJEDGL=@RLDC_QDDJEDGL,");
                                    strSql.Append("RLDC_QDDJFZNJ=@RLDC_QDDJFZNJ,");
                                    strSql.Append("RLDC_QDDJLX=@RLDC_QDDJLX,");
                                    strSql.Append("RLDC_RLLX=@RLDC_RLLX,");
                                    strSql.Append("RLDC_ZHGKHQL=@RLDC_ZHGKHQL,");
                                    strSql.Append("RLDC_ZHGKXSLC=@RLDC_ZHGKXSLC,");
                                    strSql.Append("UPDATETIME=@UPDATETIME");
                                    strSql.Append(" where UNIQUE_CODE=@UNIQUE_CODE ");
                                    OleDbParameter[] parameters = {
					                    new OleDbParameter("@QCSCQY", OleDbType.VarChar,255),
					                    new OleDbParameter("@JYBGBH", OleDbType.VarChar,255),
					                    new OleDbParameter("@JKQCZJXS", OleDbType.VarChar,255),
					                    new OleDbParameter("@CLXH", OleDbType.VarChar,255),
					                    new OleDbParameter("@HGSPBM", OleDbType.VarChar,255),
					                    new OleDbParameter("@CLZL", OleDbType.VarChar,255),
					                    new OleDbParameter("@YYC", OleDbType.VarChar,255),
					                    new OleDbParameter("@QDXS", OleDbType.VarChar,255),
					                    new OleDbParameter("@ZWPS", OleDbType.VarChar,255),
					                    new OleDbParameter("@ZCZBZL", OleDbType.VarChar,255),
					                    new OleDbParameter("@ZDSJZZL", OleDbType.VarChar,255),
					                    new OleDbParameter("@ZGCS", OleDbType.VarChar,255),
					                    new OleDbParameter("@EDZK", OleDbType.VarChar,255),
					                    new OleDbParameter("@LTGG", OleDbType.VarChar,200),
					                    new OleDbParameter("@LJ", OleDbType.VarChar,255),
					                    new OleDbParameter("@JYJGMC", OleDbType.VarChar,255),
					                    new OleDbParameter("@TYMC", OleDbType.VarChar,255),
					                    new OleDbParameter("@ZJ", OleDbType.VarChar,255),
					                    new OleDbParameter("@RLLX", OleDbType.VarChar,200),
					                    new OleDbParameter("@RLDC_CDDMSXZGXSCS", OleDbType.VarChar,255),
					                    new OleDbParameter("@RLDC_CQPBCGZYL", OleDbType.VarChar,255),
					                    new OleDbParameter("@RLDC_CQPRJ", OleDbType.VarChar,255),
					                    new OleDbParameter("@RLDC_DDGLMD", OleDbType.VarChar,255),
					                    new OleDbParameter("@RLDC_CQPLX", OleDbType.VarChar,255),
					                    new OleDbParameter("@RLDC_DDHHJSTJXXDCZBNL", OleDbType.VarChar,255),
					                    new OleDbParameter("@RLDC_DLXDCZZL", OleDbType.VarChar,255),
					                    new OleDbParameter("@RLDC_QDDJEDGL", OleDbType.VarChar,255),
					                    new OleDbParameter("@RLDC_QDDJFZNJ", OleDbType.VarChar,255),
					                    new OleDbParameter("@RLDC_QDDJLX", OleDbType.VarChar,255),
					                    new OleDbParameter("@RLDC_RLLX", OleDbType.VarChar,255),
					                    new OleDbParameter("@RLDC_ZHGKHQL", OleDbType.VarChar,255),
					                    new OleDbParameter("@RLDC_ZHGKXSLC", OleDbType.VarChar,255),
					                    new OleDbParameter("@UPDATETIME", OleDbType.Date),
					                    new OleDbParameter("@UNIQUE_CODE", OleDbType.VarChar,255)
                                    };
                                    parameters[0].Value = dr["QCSCQY"];
                                    parameters[1].Value = dr["JYBGBH"];
                                    parameters[2].Value = dr["JKQCZJXS"];
                                    parameters[3].Value = dr["CLXH"];
                                    parameters[4].Value = dr["HGSPBM"];
                                    parameters[5].Value = dr["CLZL"];
                                    parameters[6].Value = dr["YYC"];
                                    parameters[7].Value = dr["QDXS"];
                                    parameters[8].Value = dr["ZWPS"];
                                    parameters[9].Value = dr["ZCZBZL"];
                                    parameters[10].Value = dr["ZDSJZZL"];
                                    parameters[11].Value = dr["ZGCS"];
                                    parameters[12].Value = dr["EDZK"];
                                    parameters[13].Value = dr["LTGG"];
                                    parameters[14].Value = dr["LJ"];
                                    parameters[15].Value = dr["JYJGMC"];
                                    parameters[16].Value = dr["TYMC"];
                                    parameters[17].Value = dr["ZJ"];
                                    parameters[18].Value = dr["RLLX"];
                                    parameters[19].Value = dr["RLDC_CDDMSXZGXSCS"];
                                    parameters[20].Value = dr["RLDC_CQPBCGZYL"];
                                    parameters[21].Value = dr["RLDC_CQPRJ"];
                                    parameters[22].Value = dr["RLDC_DDGLMD"];
                                    parameters[23].Value = dr["RLDC_CQPLX"];
                                    parameters[24].Value = dr["RLDC_DDHHJSTJXXDCZBNL"];
                                    parameters[25].Value = dr["RLDC_DLXDCZZL"];
                                    parameters[26].Value = dr["RLDC_QDDJEDGL"];
                                    parameters[27].Value = dr["RLDC_QDDJFZNJ"];
                                    parameters[28].Value = dr["RLDC_QDDJLX"];
                                    parameters[29].Value = dr["RLDC_RLLX"];
                                    parameters[30].Value = dr["RLDC_ZHGKHQL"];
                                    parameters[31].Value = dr["RLDC_ZHGKXSLC"];
                                    parameters[32].Value = Convert.ToDateTime(DateTime.Now, CultureInfo.InvariantCulture);
                                    parameters[33].Value = dr["UNIQUE_CODE"];

                                    AccessHelper.ExecuteNonQuery(AccessHelper.conn, strSql.ToString(), parameters);
                                    succCount++;

                                    #endregion
                                }
                                catch (Exception ex)
                                {
                                    msg += ex.Message + "\r\n";
                                }
                                #endregion
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                msg += ex.Message + "\r\n";
            }

            return succCount;
        }

        /// <summary>
        /// 转换表头
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public DataTable FilterD2D(Dictionary<string, string> dict, DataTable dt, string tableName)
        {
            DataTable d = new DataTable();
            for (int i = 0; i < dt.Columns.Count; )
            {
                DataColumn c = dt.Columns[i];
                d.Columns.Add(dict[c.ColumnName]);
                i++;
            }

            foreach (DataRow r in dt.Rows)
            {
                // 判断第一列是否为空，为空则认为此行数据无效
                if (r[0] != null && !string.IsNullOrEmpty(r[0].ToString()))
                {
                    DataRow ddr = d.NewRow();
                    ddr = r;
                    d.Rows.Add(ddr.ItemArray);
                }
            }

            return d;
        }

        /// <summary>
        /// 英文转中文
        /// </summary>
        /// <param name="dict"></param>
        /// <param name="dt"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public DataTable E2C(Dictionary<string, string> dict, DataTable dt, string tableName)
        {
            foreach (DataColumn dc in dt.Columns)
            {
                foreach (var kv in dict)
                {
                    if (kv.Value == dc.ColumnName)
                    {
                        dc.ColumnName = kv.Key;
                        break;
                    }
                }
            }
            return dt;
        }


        /// <summary>
        /// 转换表头
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public DataTable D2D(Dictionary<string,string>dict, DataTable dt,string tableName)
        {
            DataTable d = new DataTable();
            for (int i = 0; i < dt.Columns.Count; )
            {
                DataColumn c = dt.Columns[i];
                if (!dict.ContainsKey(c.ColumnName))
                {
                    dt.Columns.Remove(c);
                    continue;
                }
                d.Columns.Add(dict[c.ColumnName]);
                i++;
            }

            foreach (DataRow r in dt.Rows)
            {
                // 判断第一列是否为空，为空则认为此行数据无效
                if (r[0] != null && !string.IsNullOrEmpty(r[0].ToString()))
                {
                    DataRow ddr = d.NewRow();
                    ddr = r;
                    d.Rows.Add(ddr.ItemArray);
                }
            }

            return d;
        }

        /// <summary>
        /// 模板列头转置表列头
        /// </summary>
        /// <param name="str">模板列头</param>
        ///  <param name="type">燃料类型</param>
        /// <returns></returns>
        private string s2s(string str)
        {
            try
            {
                return dictVin[str];
            }
            catch
            {
                return str;
            }
        }

        private void ReadTemplate(string filePath)
        {
            DataSet ds = this.ReadTemplateExcel(filePath);
            dictCTNY = new Dictionary<string, string>();
            dictFCDSHHDL = new Dictionary<string, string>();
            dictCDSHHDL = new Dictionary<string, string>();
            dictCDD = new Dictionary<string, string>();
            dictRLDC = new Dictionary<string, string>();
            dictVin = new Dictionary<string, string>();

            foreach (DataRow r in ds.Tables[CTNY].Rows)
            {
                dictCTNY.Add(Convert.ToString(r[0]).Trim(), Convert.ToString(r[1]).Trim());
            }

            foreach (DataRow r in ds.Tables[FCDSHHDL].Rows)
            {
                dictFCDSHHDL.Add(Convert.ToString(r[0]).Trim(), Convert.ToString(r[1]).Trim());
            }

            foreach (DataRow r in ds.Tables[CDSHHDL].Rows)
            {
                dictCDSHHDL.Add(Convert.ToString(r[0]).Trim(), Convert.ToString(r[1]).Trim());
            }

            foreach (DataRow r in ds.Tables[CDD].Rows)
            {
                dictCDD.Add(Convert.ToString(r[0]).Trim(), Convert.ToString(r[1]).Trim());
            }

            foreach (DataRow r in ds.Tables[RLDC].Rows)
            {
                dictRLDC.Add(Convert.ToString(r[0]).Trim(), Convert.ToString(r[1]).Trim());
            }

            foreach (DataRow r in ds.Tables[VIN].Rows)
            {
                dictVin.Add(Convert.ToString(r[0]).Trim(), Convert.ToString(r[1]).Trim());
            }
        }

        /// <summary>
        /// 保存已经就绪的数据
        /// </summary>
        /// <param name="drVin"></param>
        /// <param name="drMain"></param>
        /// <returns></returns>
        public string SaveReadyData(DataRow drVin, DataRow drMain, DataTable dtPam)
        {
            string genMsg = string.Empty;
            string strCon = AccessHelper.conn;

            try
            {
                string strCreater = Utils.userId;
                string vin = drVin["VIN"].ToString().Trim().ToUpper();

                // 如果当前vin数据已经存在，则跳过
                if (this.IsFuelDataExist(vin))
                {
                    genMsg += vin + "已经存在。\r\n";
                    return genMsg;
                }  

                using (OleDbConnection con = new OleDbConnection(strCon))
                {
                    con.Open();
                    OleDbTransaction tra = null; //创建事务，开始执行事务
                    try
                    {
                        #region 待生成的燃料基本信息数据存入燃料基本信息表

                        tra = con.BeginTransaction();
                        string sqlInsertBasic = @"INSERT INTO FC_CLJBXX
                                (   VIN,USER_ID,QCSCQY,JKQCZJXS,CLZZRQ,UPLOADDEADLINE,CLXH,CLZL,
                                    RLLX,ZCZBZL,ZGCS,LTGG,ZJ,
                                    TYMC,YYC,ZWPS,ZDSJZZL,EDZK,LJ,
                                    QDXS,JYJGMC,JYBGBH,HGSPBM,QTXX,STATUS,CREATETIME,UPDATETIME,UNIQUE_CODE
                                ) VALUES
                                (   @VIN,@USER_ID,@QCSCQY,@JKQCZJXS,@CLZZRQ,@UPLOADDEADLINE,@CLXH,@CLZL,
                                    @RLLX,@ZCZBZL,@ZGCS,@LTGG,@ZJ,
                                    @TYMC,@YYC,@ZWPS,@ZDSJZZL,@EDZK,@LJ,
                                    @QDXS,@JYJGMC,@JYBGBH,@HGSPBM,@QTXX,@STATUS,@CREATETIME,@UPDATETIME,@UNIQUE_CODE)";

                        DateTime clzzrqDate;
                        try
                        {
                            clzzrqDate = DateTime.ParseExact(drVin["CLZZRQ"].ToString().Trim(), "yyyyMMdd", System.Globalization.CultureInfo.CurrentCulture);
                        }
                        catch (Exception)
                        {
                            clzzrqDate = Convert.ToDateTime(drVin["CLZZRQ"]);
                        }
                        
                        //DateTime clzzrqDate = Convert.ToDateTime(drVin["CLZZRQ"].ToString().Trim(), CultureInfo.InvariantCulture);
                        
                        OleDbParameter clzzrq = new OleDbParameter("@CLZZRQ", clzzrqDate);
                        clzzrq.OleDbType = OleDbType.DBDate;

                        DateTime uploadDeadlineDate = this.QueryUploadDeadLine(clzzrqDate);
                        OleDbParameter uploadDeadline = new OleDbParameter("@UPLOADDEADLINE", uploadDeadlineDate);
                        uploadDeadline.OleDbType = OleDbType.DBDate;

                        OleDbParameter creTime = new OleDbParameter("@CREATETIME", DateTime.Now);
                        creTime.OleDbType = OleDbType.DBDate;
                        OleDbParameter upTime = new OleDbParameter("@UPDATETIME", DateTime.Now);
                        upTime.OleDbType = OleDbType.DBDate;

                        OleDbParameter[] param = { 
                                     new OleDbParameter("@VIN",vin),
                                     new OleDbParameter("@USER_ID",Utils.userId),
                                     new OleDbParameter("@QCSCQY",drMain["QCSCQY"].ToString().Trim()),
                                     new OleDbParameter("@JKQCZJXS",drMain["JKQCZJXS"].ToString().Trim()),
                                     clzzrq,
                                     uploadDeadline,
                                     new OleDbParameter("@CLXH",drMain["CLXH"].ToString().Trim()),
                                     new OleDbParameter("@CLZL",drMain["CLZL"].ToString().Trim()),
                                     new OleDbParameter("@RLLX",drMain["RLLX"].ToString().Trim()),
                                     new OleDbParameter("@ZCZBZL",drMain["ZCZBZL"].ToString().Trim()),
                                     new OleDbParameter("@ZGCS",drMain["ZGCS"].ToString().Trim()),
                                     new OleDbParameter("@LTGG",drMain["LTGG"].ToString().Trim()),
                                     new OleDbParameter("@ZJ",drMain["ZJ"].ToString().Trim()),
                                     new OleDbParameter("@TYMC",drMain["TYMC"].ToString().Trim()),
                                     new OleDbParameter("@YYC",drMain["YYC"].ToString().Trim()),
                                     new OleDbParameter("@ZWPS",drMain["ZWPS"].ToString().Trim()),
                                     new OleDbParameter("@ZDSJZZL",drMain["ZDSJZZL"].ToString().Trim()),
                                     new OleDbParameter("@EDZK",drMain["EDZK"].ToString().Trim()),
                                     new OleDbParameter("@LJ",drMain["LJ"].ToString().Trim()),
                                     new OleDbParameter("@QDXS",drMain["QDXS"].ToString().Trim()),
                                     new OleDbParameter("@JYJGMC",drMain["JYJGMC"].ToString().Trim()),
                                     new OleDbParameter("@JYBGBH",drMain["JYBGBH"].ToString().Trim()),
                                     new OleDbParameter("@HGSPBM",drMain["HGSPBM"].ToString().Trim()),
                                     //new OleDbParameter("@QTXX",drMain["CT_QTXX"].ToString().Trim()),
                                     new OleDbParameter("@QTXX",drMain.Table.Columns.Contains("CT_QTXX") ? drMain["CT_QTXX"].ToString().Trim() : ""),
                                     // 状态为9表示数据以导入，但未被激活，此时用来供用户修改
                                     new OleDbParameter("@STATUS","1"),
                                     creTime,
                                     upTime,
                                     new OleDbParameter("@UNIQUE_CODE",drVin["UNIQUE_CODE"].ToString().Trim())
                                     };
                        AccessHelper.ExecuteNonQuery(tra, sqlInsertBasic, param);

                        #endregion

                        #region 插入参数信息

                        string sqlDelParam = "DELETE FROM RLLX_PARAM_ENTITY WHERE VIN ='" + vin + "'";
                        AccessHelper.ExecuteNonQuery(tra, sqlDelParam, null);

                        // 待生成的燃料参数信息存入燃料参数表
                        foreach (DataRow drParam in dtPam.Rows)
                        {
                            string paramCode = drParam["PARAM_CODE"].ToString().Trim();
                            string sqlInsertParam = @"INSERT INTO RLLX_PARAM_ENTITY 
                                            (PARAM_CODE,VIN,PARAM_VALUE,V_ID) 
                                      VALUES
                                            (@PARAM_CODE,@VIN,@PARAM_VALUE,@V_ID)";
                            OleDbParameter[] paramList = { 
                                     new OleDbParameter("@PARAM_CODE",paramCode),
                                     new OleDbParameter("@VIN",vin),
                                     new OleDbParameter("@PARAM_VALUE",drMain[paramCode]),
                                     new OleDbParameter("@V_ID","")
                                   };
                            AccessHelper.ExecuteNonQuery(tra, sqlInsertParam, paramList);
                        }
                        #endregion

                        #region 保存VIN信息备用

                        string sqlDel = "DELETE FROM VIN_INFO WHERE VIN = '" + vin + "'";
                        AccessHelper.ExecuteNonQuery(tra, sqlDel, null);

                        string sqlStr = @"INSERT INTO VIN_INFO(VIN,CLXH,CLZZRQ,STATUS,CREATETIME,RLLX,UNIQUE_CODE) Values (@VIN, @CLXH,@CLZZRQ,@STATUS,@CREATETIME,@RLLX,@UNIQUE_CODE)";
                        OleDbParameter[] vinParamList = { 
                                         new OleDbParameter("@VIN",vin),
                                         new OleDbParameter("@CLXH",drVin["CLXH"].ToString().Trim()),
                                         new OleDbParameter("@CLZZRQ",clzzrqDate),
                                         new OleDbParameter("@STATUS","0"),
                                         creTime,
                                         new OleDbParameter("@RLLX",drVin["RLLX"].ToString().Trim()),
                                         new OleDbParameter("@UNIQUE_CODE",drVin["UNIQUE_CODE"].ToString().Trim())
                                      };
                        AccessHelper.ExecuteNonQuery(tra, sqlStr, vinParamList);

                        tra.Commit();
                        #endregion
                    }
                    catch (Exception ex)
                    {
                        tra.Rollback();
                        throw ex;
                    }
                    finally
                    {
                        tra.Dispose();
                        con.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                genMsg += ex.Message;
            }

            return genMsg;
        }

        /// <summary>
        /// 检查当前VIN数据是否已经存在于燃料数据表中
        /// </summary>
        /// <param name="vin"></param>
        /// <returns></returns>
        protected bool IsFuelDataExist(string vin)
        {
            bool isExist = false;

            string sqlQuery = String.Format(@"SELECT VIN FROM FC_CLJBXX WHERE VIN='{0}''", vin);
            DataSet ds = AccessHelper.ExecuteDataSet(AccessHelper.conn, sqlQuery, null);

            if (ds != null)
            {
                if (ds.Tables[0].Rows.Count > 0)
                {
                    isExist = true;
                }
            }

            return isExist;
        }

        // 获取以导入但未生成油耗数据的VIN
        public DataTable GetImportedVinData(string vin)
        {
            DataSet dsQuery = new DataSet();
            string sqlQuery = @"SELECT VI.* FROM VIN_INFO VI WHERE VI.STATUS='1' ";

            string sw = string.Empty;
            if (!string.IsNullOrEmpty(vin))
            {
                sw += string.Format(" AND VIN LIKE '%{0}%'", vin);
            }

            try
            {
                dsQuery = AccessHelper.ExecuteDataSet(AccessHelper.conn, sqlQuery + sw, null);
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return dsQuery.Tables[0];
        }

        /// <summary>
        /// 获取全部参数数据
        /// </summary>
        /// <returns></returns>
        public DataTable GetCheckData()
        {
            string sql = "select * from RLLX_PARAM";
            DataSet ds = AccessHelper.ExecuteDataSet(AccessHelper.conn, sql, null);
            return ds.Tables[0];
        }

        /// <summary>
        /// 获取全部主表数据，用作合并VIN数据
        /// </summary>
        /// <returns></returns>
        public bool GetMainData()
        {
            bool flag = true;
            string sqlCtny = string.Format(@"SELECT * FROM CTNY_MAIN");
            DataSet ds = AccessHelper.ExecuteDataSet(AccessHelper.conn, sqlCtny.ToString(), null);
            dsMainStatic.Add(CTNY,ds.Tables[0]);

            sqlCtny = string.Format(@"SELECT * FROM FCDS_MAIN");
            ds = AccessHelper.ExecuteDataSet(AccessHelper.conn, sqlCtny.ToString(), null);
            dsMainStatic.Add(FCDSHHDL,ds.Tables[0]);

            sqlCtny = string.Format(@"SELECT * FROM CDS_MAIN");
            ds = AccessHelper.ExecuteDataSet(AccessHelper.conn, sqlCtny.ToString(), null);
            dsMainStatic.Add(CDSHHDL,ds.Tables[0]);

            sqlCtny = string.Format(@"SELECT * FROM CDD_MAIN");
            ds = AccessHelper.ExecuteDataSet(AccessHelper.conn, sqlCtny.ToString(), null);
            dsMainStatic.Add(CDD,ds.Tables[0]);

            sqlCtny = string.Format(@"SELECT * FROM RLDC_MAIN");
            ds = AccessHelper.ExecuteDataSet(AccessHelper.conn, sqlCtny.ToString(), null);
            dsMainStatic.Add(RLDC,ds.Tables[0]);

            StringBuilder sql = new StringBuilder();
            sql.Append("SELECT unique_code from ctny_main union all ");
            sql.Append("SELECT unique_code from fcds_main union all ");
            sql.Append("SELECT unique_code from cds_main  union all ");
            sql.Append("SELECT unique_code from cdd_main  union all ");
            sql.Append("SELECT unique_code from rldc_main");

            ds = AccessHelper.ExecuteDataSet(AccessHelper.conn, sql.ToString(), null);
            dtCtnyStatic = ds.Tables[0];

            if (dtCtnyStatic.Rows.Count < 1)  
            {
                flag = false;
            }
            return flag;
        }

        /// <summary>
        /// 获取已经导入的参数编码（MAIN_ID）,用于导入判断
        /// </summary>
        public int GetMainId(string mainId)
        {
            int dataCount;
            string sqlCtny = string.Format(@"SELECT MAIN_ID FROM MAIN_CTNY WHERE MAIN_ID='{0}'", mainId);
            string sqlFcds = string.Format(@"SELECT MAIN_ID FROM MAIN_FCDSHHDL WHERE MAIN_ID='{0}'", mainId);
            try
            {
                DataSet dsCtnyMainId = AccessHelper.ExecuteDataSet(AccessHelper.conn, sqlCtny, null);
                DataSet dsFcdsMainId = AccessHelper.ExecuteDataSet(AccessHelper.conn, sqlFcds, null);

                dataCount = dsCtnyMainId.Tables[0].Rows.Count + dsFcdsMainId.Tables[0].Rows.Count;
            }
            catch (Exception)
            {
                dataCount = 0;
            }
            return dataCount;
        }

        /// <summary>
        /// 根据VIN从vin信息表获取参数编码
        /// </summary>
        /// <param name="vin"></param>
        /// <returns></returns>
        public string GetMainIdFromVinData(string vin)
        {
            string CocId = string.Empty;
            string sqlMain = string.Format(@"SELECT MAIN_ID FROM VIN_INFO WHERE VIN='{0}'", vin);
            try
            {
                DataSet dsMainId = AccessHelper.ExecuteDataSet(AccessHelper.conn, sqlMain, null);
                if (dsMainId != null && dsMainId.Tables[0].Rows.Count > 0)
                {
                    CocId = dsMainId.Tables[0].Rows[0]["MAIN_ID"].ToString();
                }
            }
            catch (Exception)
            {
            }
            return CocId;
        }

        public string GetUploadUser(string vin)
        {
            string userId = string.Empty;
            string sqlUser = string.Format(@"SELECT USER_ID FROM FC_CLJBXX WHERE VIN='{0}'", vin);
            try
            {
                DataSet dsUserId = AccessHelper.ExecuteDataSet(AccessHelper.conn, sqlUser, null);
                if (dsUserId != null && dsUserId.Tables[0].Rows.Count > 0)
                {
                    userId = dsUserId.Tables[0].Rows[0]["USER_ID"] == null ? "" : dsUserId.Tables[0].Rows[0]["USER_ID"].ToString();
                }
            }
            catch (Exception)
            {
            }
            return userId;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gv"></param>
        /// <param name="dt"></param>
        /// <returns></returns>
        public List<string> GetMainIdFromControl(GridView gv, DataTable dt)
        {
            List<string> mainIdList = new List<string>();

            gv.PostEditor();

            if (dt != null && dt.Rows.Count>0)
            {
                DataRow[] drVinArr = dt.Select("check=True");

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    if ((bool)dt.Rows[i]["check"])
                    {
                        mainIdList.Add(dt.Rows[i]["CLXH"].ToString());
                    }
                }
            }
            return mainIdList;
        }

        /// <summary>
        /// 获取燃料参数规格数据
        /// </summary>
        /// <param name="fuelType"></param>
        /// <returns></returns>
        public DataTable GetRllxData(string fuelType)
        {
            string sqlQueryParam = string.Format(@"SELECT PARAM_CODE "
                                + " FROM RLLX_PARAM WHERE FUEL_TYPE='{0}' AND STATUS='1'", fuelType);
            System.Data.DataTable dtPam = AccessHelper.ExecuteDataSet(AccessHelper.conn, sqlQueryParam, null).Tables[0];

            return dtPam;
        }

        /// <summary>
        /// 验证单行数据
        /// </summary>
        /// <param name="r">验证数据</param>
        /// <param name="dr">匹配数据</param>
        /// <returns></returns>
        private string VerifyData(DataRow r, DataRow[] dr, string importType)
        {
            string message = string.Empty;

            string Jkqczjxs = Convert.ToString(r["JKQCZJXS"]);
            string Qcscqy = Convert.ToString(r["QCSCQY"]);

            // 汽车生产企业
            if (string.IsNullOrEmpty(Qcscqy))
            {
                message += "汽车生产企业不能为空!\r\n";
            }

            // 车辆型号
            string clxh = Convert.ToString(r["CLXH"]);
            string uniqueCode = Convert.ToString(r["UNIQUE_CODE"]);
            message += this.VerifyRequired("车辆型号", clxh);
            message += this.VerifyStrLen("车辆型号", clxh, 100);

            // 车辆种类
            string Clzl = Convert.ToString(r["CLZL"]);
            message += this.VerifyRequired("车辆种类", Clzl);
            Clzl = Clzl.Replace("(", "（").Replace(")", "）");
            if (Clzl == "乘用车（M1类）")
            {
                Clzl = "乘用车（M1）";
            }
            message += this.VerifyClzl(Clzl);
            message += this.VerifyStrLen("车辆种类", Clzl, 200);

            // 燃料类型
            string Rllx = Convert.ToString(r["RLLX"]);
            message += this.VerifyRequired("燃料类型", Rllx);
            message += this.VerifyStrLen("燃料类型", Rllx, 200);
            message += this.VerifyRllx(Rllx);

            // 整车整备质量
            string Zczbzl = Convert.ToString(r["ZCZBZL"]);
            message += this.VerifyRequired("整车整备质量", Zczbzl);
            if (!this.VerifyParamFormat(Zczbzl, ','))
            {
                message += "整车整备质量应填写整数，多个数值应以半角“,”隔开，中间不留空格\r\n";
            }

            // 最高车速
            string Zgcs = Convert.ToString(r["ZGCS"]);
            message += this.VerifyRequired("最高车速", Zgcs);
            if (!this.VerifyParamFormat(Zgcs, ','))
            {
                message += "最高车速应填写整数，多个数值应以半角“,”隔开，中间不留空格\r\n";
            }

            // 轮胎规格
            string Ltgg = Convert.ToString(r["LTGG"]);
            message += this.VerifyRequired("轮胎规格", Ltgg);
            message += this.VerifyStrLen("轮胎规格", Ltgg, 200);
            message += this.VerifyLtgg(Ltgg);
            // 前后轮距相同只填写一个型号数据即可，不同以(前轮轮胎型号)/(后轮轮胎型号)(引号内为半角括号，且中间不留不必要的空格)

            // 轴距
            string Zj = Convert.ToString(r["ZJ"]);
            message += this.VerifyRequired("轴距", Zj);
            message += this.VerifyInt("轴距", Zj);

            // 通用名称
            string Tymc = Convert.ToString(r["TYMC"]);
            message += this.VerifyRequired("通用名称", Tymc);
            message += this.VerifyStrLen("通用名称", Tymc, 200);

            // 越野车（G类）
            string Yyc = Convert.ToString(r["YYC"]);
            message += this.VerifyRequired("越野车（G类）", Yyc);
            message += this.VerifyYyc(Yyc);
            message += this.VerifyStrLen("越野车（G类）", Yyc, 200);

            // 座位排数
            string Zwps = Convert.ToString(r["ZWPS"]);
            message += this.VerifyRequired("座位排数", Zwps);
            message += this.VerifyInt("座位排数", Zwps);

            // 最大设计总质量
            string Zdsjzzl = Convert.ToString(r["ZDSJZZL"]);
            string Edzk = Convert.ToString(r["EDZK"]);
            message += this.VerifyRequired("最大设计总质量", Zdsjzzl);
            message += this.VerifyZdsjzzl(Zdsjzzl, Zczbzl, Edzk);
            message += this.VerifyInt("最大设计总质量", Zdsjzzl);

            // 额定载客
            message += this.VerifyRequired("额定载客", Edzk);
            message += this.VerifyInt("额定载客", Edzk);

            // 轮距（前/后）
            string Lj = Convert.ToString(r["LJ"]);
            message += this.VerifyRequired("轮距（前/后）", Lj);
            if (!this.VerifyParamFormat(Lj, '/') && Lj.IndexOf('/') < 0)
            {
                message += "轮距（前/后）应填写整数，前后轮距，中间用”/”隔开\r\n";
            }

            // 驱动型式 
            string Qdxs = Convert.ToString(r["QDXS"]);
            message += this.VerifyRequired("驱动型式", Qdxs);
            message += this.VerifyQdxs(Qdxs);
            message += this.VerifyStrLen("驱动型式", Qdxs, 200);

            // 检测机构名称
            string Jyjgmc = Convert.ToString(r["JYJGMC"]);
            message += this.VerifyRequired("检测机构名称", Jyjgmc);
            message += this.VerifyStrLen("检测机构名称", Jyjgmc, 500);

            // 报告编号
            string Jybgbh = Convert.ToString(r["JYBGBH"]);
            message += this.VerifyRequired("报告编号", Jybgbh);
            message += this.VerifyStrLen("报告编号", Jybgbh, 500);

            switch (Rllx)
            {
                case "纯电动":
                    message += this.VerifyCDD(r, dr);
                    break;
                case "非插电式混合动力":
                    message += this.VerifyHHDL(r, dr);
                    break;
                case "插电式混合动力":
                    message += this.VerifyHHDL(r, dr);
                    break;
                case "燃料电池":
                    message += this.VerifyRLDC(r, dr);
                    break;
                default:
                    message += this.VerifyCTNY(r, dr);
                    break;
            }
            if (!string.IsNullOrEmpty(message))
            {
                message = uniqueCode + "【" + clxh + "】" + "：\r\n" + message;
            }

            return message;
        }

        /// <summary>
        /// 根据导入EXCEL 查询 本地库数据
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public DataSet ReadSearchExcel(string path, string sheet, string status, string Date)
        {
            DataSet ds = ReadExcel(path, sheet);
            StringBuilder strAdd = new StringBuilder();
            strAdd.Append("select * from FC_CLJBXX where VIN in(");
            if (ds != null && ds.Tables[0].Rows.Count > 0)
            {
                foreach (DataRow r in ds.Tables[0].Rows)
                {
                    strAdd.Append("'");
                    strAdd.Append(Convert.ToString(r["VIN"]));
                    strAdd.Append("',");
                }
                string sql = strAdd.ToString().TrimEnd(',') + ")";
                return AccessHelper.ExecuteDataSet(strCon, String.Format("{0} and STATUS='{1}'{2}", sql, status, Date), null);
            }
            return new DataSet();
        }

        /// <summary>
        /// 批量修改进口日期
        /// </summary>
        /// <param name="path"></param>
        /// <param name="sheet"></param>
        /// <returns></returns>
        public int ReadUpdateDate(string path, string sheet)
        {
            int result = 0;

            // 获取节假日信息，用于生成上报截止日期
            listHoliday = this.GetHoliday();

            ProcessForm pf = new ProcessForm();

            DataSet ds = ReadExcel(path, sheet);
            if (ds != null && ds.Tables[0].Rows.Count > 0)
            {
                using (OleDbConnection con = new OleDbConnection(strCon))
                {
                    con.Open();
                    using (OleDbTransaction tra = con.BeginTransaction())
                    {
                        try
                        {
                            // 显示进度条
                            pf.Show();
                            int pageSize = 20;
                            int totalVin = ds.Tables[0].Rows.Count;
                            pf.TotalMax = (int)Math.Ceiling((decimal)totalVin / (decimal)pageSize);
                            pf.ShowProcessBar();

                            foreach (DataRow r in ds.Tables[0].Rows)
                            {
                                string statuswhere = string.Empty;
                                int status = SearchStatus(Convert.ToString(r["VIN"]));
                                bool rel = false;
                                switch (status)
                                {
                                    case (int)Status.待上报:
                                        rel = true;
                                        break;
                                    case (int)Status.修改待上报:
                                        rel = true;
                                        break;
                                    case (int)Status.已上报:
                                        statuswhere = ", STATUS=" + (int)Status.修改待上报;
                                        rel = true;
                                        break;
                                    case (int)Status.撤销待上报:
                                        break;
                                }

                                if (rel)
                                {
                                    DateTime clzzrqDate = Convert.ToDateTime(r[1].ToString());
                                    DateTime uploadDeadlineDate = this.QueryUploadDeadLine(clzzrqDate);
                                    string sql = String.Format("UPDATE FC_CLJBXX SET CLZZRQ='{0}', UPLOADDEADLINE='{1}'{2}  where VIN='{3}'", clzzrqDate, uploadDeadlineDate, statuswhere, r["VIN"]);
                                    AccessHelper.ExecuteNonQuery(tra, sql, null);
                                    pf.progressBarControl1.PerformStep();
                                    Application.DoEvents();
                                }
                            }
                            tra.Commit();
                            result = 1;
                        }
                        catch (Exception)
                        {
                            tra.Rollback();
                        }
                        finally
                        {
                            if (pf != null)
                            {
                                pf.Close();
                            }
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 导入Excel
        /// </summary>
        /// <param name="fileName">文件地址</param>
        /// <param name="sheet">名称</param>
        /// <returns></returns>
        public DataSet ReadExcel(string fileName, string sheet)
        {
            string strConn = String.Format("Provider=Microsoft.ACE.OLEDB.12.0;Data Source={0}; Extended Properties='Excel 12.0;HDR=YES;IMEX=1'", fileName); //; HDR=No
            DataSet ds = new DataSet();
            OleDbConnection conn = new OleDbConnection(strConn);
            try
            {
                conn.Open();
                DataTable sheetNames = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                if (sheetNames == null)
                {
                    return null;
                }
                else
                {
                    if (string.IsNullOrEmpty(sheet))
                    {
                        sheet = sheetNames.Rows[0]["TABLE_NAME"].ToString();
                    }
                    else
                    {
                        sheet = sheet + "$";
                    }
                    OleDbDataAdapter oada = new OleDbDataAdapter(String.Format("select * from [{0}]", sheet), strConn);
                    oada.Fill(ds,sheet.IndexOf('$')>0?sheet.Substring(0,sheet.Length-1):sheet);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                conn.Close();
            }

            return ds;
        }

        /// <summary>
        /// 查询EXCEL中VIN状态
        /// </summary>
        /// <param name="vin">VIN码</param>
        /// <returns></returns>
        private int SearchStatus(string vin)
        {
            string sql = String.Format("select status from FC_CLJBXX where VIN='{0}'", vin);
            return Convert.ToInt32(AccessHelper.ExecuteScalar(strCon, sql, null));
        }

        /// <summary>
        /// 获取节假日数据
        /// </summary>
        /// <returns></returns>
        protected List<string> GetHoliday()
        {
            List<string> holidayList = new List<string>();
            try
            {
                string sqlHol = string.Format(@"SELECT HOL_DAYS FROM FC_HOLIDAY");
                DataSet ds = AccessHelper.ExecuteDataSet(AccessHelper.conn, sqlHol, null);
                //dbUtil.QuerySingleDT(sqlHol);
                if (ds != null && ds.Tables[0].Rows.Count > 0)
                {
                    foreach (DataRow dr in ds.Tables[0].Rows)
                    {
                        holidayList.Add(dr["HOL_DAYS"].ToString());
                    }
                }
            }
            catch (Exception)
            {
            }
            return holidayList;
        }

        // 查询数据上报的截止日期
        public DateTime QueryUploadDeadLine(DateTime manufactureDate)
        {
            DateTime deadLine = new DateTime();

            try
            {
                int timeCons = Utils.timeCons;
                string strManufactureDate = manufactureDate.ToString("yyyy-MM-dd");
                if (!string.IsNullOrEmpty(strManufactureDate) && timeCons > 0)
                {
                    // 限制时长的开始计时日期为（制造日+1天）的零点
                    DateTime startDate = Convert.ToDateTime(strManufactureDate).AddDays(1);

                    // 临时截止日期为 开始计时时间+限制时长
                    deadLine = startDate.Add(new TimeSpan(timeCons, 0, 0));

                    // 查看 开始计时时间和临时截止日期 之间有无节假日
                    for (DateTime dt = startDate; dt < deadLine; dt = dt.AddDays(1))
                    {
                        if (this.VerifyHolidays(dt.ToString("yyyy-MM-dd")))
                        {
                            deadLine = deadLine.AddDays(1);
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
            return deadLine;
        }

        // 验证节假日
        protected bool VerifyHolidays(string date)
        {
            if (string.IsNullOrEmpty(date))
            {
                return false;
            }

            try
            {
                if (listHoliday != null && listHoliday.Contains(date))
                {
                    return true;
                }
            }
            catch (Exception)
            {
            }
            return false;
        }

        #region Excel导出

        private Microsoft.Office.Interop.Excel.Application excelApp = null;

        public void ExportExcel(string saveName, DataTable dt)
        {
            excelApp = new Microsoft.Office.Interop.Excel.Application();
            Microsoft.Office.Interop.Excel.Workbook excelBook = excelApp.Workbooks.Add(Type.Missing);
            Microsoft.Office.Interop.Excel.Worksheet excelSheet = (Microsoft.Office.Interop.Excel.Worksheet)excelBook.ActiveSheet;
            excelApp.Visible = false;

            try
            {
                int rowCount = dt.Rows.Count;
                int colCount = dt.Columns.Count;

                // 表头字段
                Dictionary<string, string> dictHeader = this.FillHeader(dt);

                long pageRows = 50000;//定义每页显示的行数,行数必须小于65536   
                if (rowCount > pageRows)
                {
                    int scount = (int)(rowCount / pageRows);//导出数据生成的表单数   
                    if (scount * pageRows < rowCount)//当总行数不被pageRows整除时，经过四舍五入可能页数不准   
                    {
                        scount = scount + 1;
                    }
                    for (int sc = 1; sc <= scount; sc++)
                    {
                        if (sc > 3)
                        {
                            object missing = System.Reflection.Missing.Value;
                            excelSheet = (Microsoft.Office.Interop.Excel.Worksheet)excelBook.Worksheets.Add(
                                        missing, missing, missing, missing);//添加一个sheet   
                        }
                        else
                        {
                            excelSheet = (Microsoft.Office.Interop.Excel.Worksheet)excelBook.Worksheets[sc];//取得sheet1   
                        }
                        object[,] datas = new object[pageRows + 1, colCount];

                        for (int i = 0; i < colCount; i++) //写入字段   
                        {
                            datas[0, i] = dictHeader[dt.Columns[i].ColumnName];//表头信息   
                        }

                        int init = int.Parse(((sc - 1) * pageRows).ToString());
                        int r = 0;
                        int index = 0;
                        int result;
                        if (pageRows * sc >= rowCount)
                        {
                            result = (int)rowCount;
                        }
                        else
                        {
                            result = int.Parse((pageRows * sc).ToString());
                        }

                        for (r = init; r < result; r++)
                        {
                            index = index + 1;
                            for (int i = 0; i < colCount; i++)
                            {
                                datas[index, i] = dt.Rows[r][dt.Columns[i].ToString()];
                            }

                        }

                        Microsoft.Office.Interop.Excel.Range fchR = excelSheet.Range[excelSheet.Cells[1, 1], excelSheet.Cells[index + 1, colCount]];
                        fchR.Value = datas;
                    }
                }
                else
                {
                    object[,] dataArray = new object[rowCount + 1, colCount];
                    for (int i = 0; i < dt.Columns.Count; i++)
                    {
                        dataArray[0, i] = dictHeader.Keys.Contains(dt.Columns[i].ColumnName) == true ? dictHeader[dt.Columns[i].ColumnName] : dt.Columns[i].ColumnName;
                    }

                    for (int i = 0; i < rowCount; i++)
                    {
                        for (int j = 0; j < colCount; j++)
                        {
                            dataArray[i + 1, j] = dt.Rows[i][j];
                        }
                    }
                    Microsoft.Office.Interop.Excel.Range range = excelSheet.Range[excelSheet.Cells[1, 1], excelSheet.Cells[rowCount + 1, colCount]];
                    range.Value = dataArray;
                }

                excelBook.SaveAs(saveName, Microsoft.Office.Interop.Excel.XlFileFormat.xlExcel8, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Microsoft.Office.Interop.Excel.XlSaveAsAccessMode.xlNoChange,
                    Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                this.EndReport();
            }
        }

        private Dictionary<string, string> FillHeader(DataTable dt)
        {
            Dictionary<string, string> dictHeader = new Dictionary<string, string>();

            for (int i = 0; i < dt.Columns.Count; i++)
            {
                switch (dt.Columns[i].ColumnName)
                {
                    case "V_ID":
                        dictHeader.Add(dt.Columns[i].ColumnName, "反馈码(V_ID)");
                        break;
                    case "VIN":
                        dictHeader.Add(dt.Columns[i].ColumnName, "备案号(VIN)");
                        break;
                    case "HGSPBM":
                        dictHeader.Add(dt.Columns[i].ColumnName, "海关商品编码");
                        break;
                    case "QCSCQY":
                        dictHeader.Add(dt.Columns[i].ColumnName, "汽车生产企业");
                        break;
                    case "JKQCZJXS":
                        dictHeader.Add(dt.Columns[i].ColumnName, "进口汽车经销商");
                        break;
                    case "CLXH":
                        dictHeader.Add(dt.Columns[i].ColumnName, "车辆型号");
                        break;
                    case "CLZL":
                        dictHeader.Add(dt.Columns[i].ColumnName, "车辆种类");
                        break;
                    case "RLLX":
                        dictHeader.Add(dt.Columns[i].ColumnName, "燃料类型");
                        break;
                    case "ZCZBZL":
                        dictHeader.Add(dt.Columns[i].ColumnName, "整车整备质量");
                        break;
                    case "ZGCS":
                        dictHeader.Add(dt.Columns[i].ColumnName, "最高车速");
                        break;
                    case "LTGG":
                        dictHeader.Add(dt.Columns[i].ColumnName, "轮胎规格");
                        break;
                    case "ZJ":
                        dictHeader.Add(dt.Columns[i].ColumnName, "轴距");
                        break;
                    case "TYMC":
                        dictHeader.Add(dt.Columns[i].ColumnName, "通用名称");
                        break;
                    case "YYC":
                        dictHeader.Add(dt.Columns[i].ColumnName, "越野车（G类）");
                        break;
                    case "ZWPS":
                        dictHeader.Add(dt.Columns[i].ColumnName, "座位排数");
                        break;
                    case "ZDSJZZL":
                        dictHeader.Add(dt.Columns[i].ColumnName, "最大设计总质量");
                        break;
                    case "EDZK":
                        dictHeader.Add(dt.Columns[i].ColumnName, "额定载客");
                        break;
                    case "LJ":
                        dictHeader.Add(dt.Columns[i].ColumnName, "轮距（前/后）");
                        break;
                    case "QDXS":
                        dictHeader.Add(dt.Columns[i].ColumnName, "驱动型式");
                        break;
                    case "JYJGMC":
                        dictHeader.Add(dt.Columns[i].ColumnName, "检测机构名称");
                        break;
                    case "JYBGBH":
                        dictHeader.Add(dt.Columns[i].ColumnName, "检验报告编号");
                        break;
                    case "QTXX":
                        dictHeader.Add(dt.Columns[i].ColumnName, "其他信息");
                        break;
                    case "STATUS":
                        dictHeader.Add(dt.Columns[i].ColumnName, "本地状态（9：未被激活（数据通过excel导入但未被激活）；0：已上传；1：没上传；2：修改没上传；3：撤销未上传）");
                        break;
                    case "CLZZRQ":
                        dictHeader.Add(dt.Columns[i].ColumnName, "制造日期/进口日期");
                        break;
                    case "UPLOADDEADLINE":
                        dictHeader.Add(dt.Columns[i].ColumnName, "上报截止日期");
                        break;
                    case "CREATETIME":
                        dictHeader.Add(dt.Columns[i].ColumnName, "创建日期");
                        break;
                    case "USER_ID":
                        dictHeader.Add(dt.Columns[i].ColumnName, "上报人");
                        break;
                    case "UPDATETIME":
                        dictHeader.Add(dt.Columns[i].ColumnName, "上报日期");
                        break;
                    case "UNIQUE_CODE":
                        dictHeader.Add(dt.Columns[i].ColumnName, "车型标示号");
                        break;
                    case "MAIN_ID":
                        dictHeader.Add(dt.Columns[i].ColumnName, "车型标示号");
                        break;
                    case "COCNO":
                        dictHeader.Add(dt.Columns[i].ColumnName, "COC编号");
                        break;
                    case "COCHOLDER":
                        dictHeader.Add(dt.Columns[i].ColumnName, "COC持有人");
                        break;
                    case "HGNO":
                        dictHeader.Add(dt.Columns[i].ColumnName, "海关编号");
                        break;
                    default: break;
                }
            }
            return dictHeader;
        }

        /// <summary>   
        /// 退出报表时关闭Excel和清理垃圾Excel进程   
        /// </summary>   
        private void EndReport()
        {
            object missing = System.Reflection.Missing.Value;
            try
            {
                excelApp.Workbooks.Close();
                excelApp.Workbooks.Application.Quit();
                excelApp.Application.Quit();
                excelApp.Quit();
            }
            catch { }
            finally
            {
                try
                {
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(excelApp.Workbooks);
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(excelApp.Application);
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(excelApp);
                    excelApp = null;
                }
                catch { }
                try
                {
                    //清理垃圾进程   
                    this.killProcessThread();
                }
                catch { }
                GC.Collect();
            }
        }
        /// <summary>   
        /// 杀掉不死进程   
        /// </summary>   
        private void killProcessThread()
        {
            ArrayList myProcess = new ArrayList();
            for (int i = 0; i < myProcess.Count; i++)
            {
                try
                {
                    System.Diagnostics.Process.GetProcessById(int.Parse((string)myProcess[i])).Kill();
                }
                catch { }
            }
        }

        #endregion

        #region 参数验证

        // 验证VIN
        private string VerifyVinData(DataRow drVIN)
        {
            string message = string.Empty;
            string clzzrqDate = string.Empty;

            try
            {
                clzzrqDate = Convert.ToString(DateTime.ParseExact(drVIN["CLZZRQ"].ToString().Trim(), "yyyyMMdd", System.Globalization.CultureInfo.CurrentCulture));
            }
            catch (Exception)
            {
                clzzrqDate = Convert.ToString(drVIN["CLZZRQ"]);
            }

            message += this.VerifyDateTime("进口日期", clzzrqDate);
            return message;
        }

        // 验证主表参数编码是否已经存在
        protected string VerifyMainId(string mainId, string importType)
        {
            int dataCount = this.GetMainId(mainId);

            if (importType == "IMPORT")
            {
                if (dataCount > 0)
                {
                    return "该参数编号数据已经导入，请勿重复导入\r\n";
                }
            }
            else if (importType == "UPDATE")
            {
                if (dataCount < 1)
                {
                    return "该参数编号数据不存在\r\n";
                }
            }
            return string.Empty;
        }

        // 验证燃料类型
        protected string VerifyRllx(string rllx)
        {
            if (!string.IsNullOrEmpty(rllx))
            {
                if (rllx == "汽油" || rllx == "柴油" || rllx == "两用燃料" || rllx == "双燃料" || rllx == "非插电式混合动力" || rllx == "插电式混合动力" || rllx == "纯电动" || rllx == "燃料电池")
                {
                    return string.Empty;
                }
                else
                {
                    return "燃料类型参数填写汽油、柴油、两用燃料、双燃料、纯电动、非插电式混合动力、插电式混合动力、燃料电池\r\n";
                }
            }
            return string.Empty;
        }

        protected string VerifyLtgg(string ltgg)
        {
            string message = string.Empty;
            try
            {
                if (!string.IsNullOrEmpty(ltgg))
                {
                    int indexLtgg = ltgg.IndexOf(")/(");
                    if (indexLtgg > -1)
                    {
                        string ltggHead = ltgg.Substring(0, indexLtgg + 1);
                        string ltggEnd = ltgg.Substring(indexLtgg + 3);

                        if (!ltggHead.StartsWith("(") || !ltggEnd.EndsWith(")"))
                        {
                            message = "前后轮距不相同以(前轮轮胎型号)/(后轮轮胎型号)(引号内为半角括号，且中间不留不必要的空格)";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return message;
        }

        // 验证最大设计总质量
        protected string VerifyZdsjzzl(string zdsjzzl, string zczbzl, string edzk)
        {
            if (!string.IsNullOrEmpty(zdsjzzl) && !string.IsNullOrEmpty(zczbzl) && !string.IsNullOrEmpty(edzk))
            {
                if (Convert.ToInt32(zdsjzzl) < (Convert.ToInt32(zczbzl) + Convert.ToInt32(edzk) * 65))
                {
                    return "最大设计总质量应≥整车整备质量＋乘员质量（额定载客×乘客质量，乘用车按65㎏/人核算)!\r\n";
                }
                else
                {
                    return string.Empty;
                }
            }
            return string.Empty;
        }

        // 车辆种类
        protected string VerifyClzl(string clzl)
        {
            if (!string.IsNullOrEmpty(clzl))
            {
                if (clzl == "乘用车（M1）" || clzl == "轻型客车（M2）" || clzl == "轻型货车（N1）")
                {
                    return string.Empty;
                }
                else
                {
                    return "车辆种类参数应填写“乘用车（M1）/轻型客车（M2）/轻型货车（N1）”\r\n";
                }
            }
            return string.Empty;
        }

        // 越野车
        protected string VerifyYyc(string yyc)
        {
            if (!string.IsNullOrEmpty(yyc))
            {
                if (yyc == "是" || yyc == "否" || yyc == "1" || yyc == "0")
                {
                    return string.Empty;
                }
                else
                {
                    return "越野车(G类)参数应填写“是/否”\r\n";
                }
            }
            return string.Empty;
        }

        // 驱动型式
        protected string VerifyQdxs(string qdxs)
        {
            if (!string.IsNullOrEmpty(qdxs))
            {
                if (qdxs == "前轮驱动" || qdxs == "后轮驱动" || qdxs == "分时全轮驱动" || qdxs == "全时全轮驱动" || qdxs == "智能(适时)全轮驱动")
                {
                    return string.Empty;
                }
                else
                {
                    return "驱动型式参数应填写“前轮驱动/后轮驱动/分时全轮驱动/全时全轮驱动/智能(适时)全轮驱动”\r\n";
                }
            }
            return string.Empty;
        }

        // 变速器型式
        protected string VerifyBsqxs(string bsqxs)
        {
            if (!string.IsNullOrEmpty(bsqxs))
            {
                if (bsqxs == "MT" || bsqxs == "AT" || bsqxs == "AMT" || bsqxs == "CVT" || bsqxs == "DCT" || bsqxs == "其它")
                {
                    return string.Empty;
                }
                else
                {
                    return "变速器型式参数应填写“MT/AT/AMT/CVT/DCT/其它”\r\n";
                }
            }
            return string.Empty;
        }

        // 变速器档位数
        protected string VerifyBsqdws(string bsqdws)
        {
            if (!string.IsNullOrEmpty(bsqdws))
            {
                if (bsqdws == "1" || bsqdws == "2" || bsqdws == "3" || bsqdws == "4" || bsqdws == "5" || bsqdws == "6" || bsqdws == "7" || bsqdws == "8" || bsqdws == "9" || bsqdws == "10" || bsqdws == "N.A")
                {
                    return string.Empty;
                }
                else
                {
                    return "变速器档位数参数应填写“1/2/3/4/5/6/7/8/9/10/N.A”\r\n";
                }
            }
            return string.Empty;
        }

        // 混合动力结构型式
        protected string VerifyHhdljgxs(string hhdljgxs)
        {
            if (!string.IsNullOrEmpty(hhdljgxs))
            {
                if (hhdljgxs == "串联" || hhdljgxs == "并联" || hhdljgxs == "混联" || hhdljgxs == "其它")
                {
                    return string.Empty;
                }
                else
                {
                    return "混合动力结构型式参数应填写“串联/并联/混联/其它”\r\n";
                }
            }
            return string.Empty;
        }

        // 是否具有行驶模式手动选择功能
        protected string VerifySdxzgn(string sdxzgn)
        {
            if (!string.IsNullOrEmpty(sdxzgn))
            {
                if (sdxzgn == "是" || sdxzgn == "否")
                {
                    return string.Empty;
                }
                else
                {
                    return "是否具有行驶模式手动选择功能参数应填写“是/否”\r\n";
                }
            }
            return string.Empty;
        }

        // 动力蓄电池组种类
        protected string VerifyDlxdczzl(string dlxdczzl)
        {
            if (!string.IsNullOrEmpty(dlxdczzl))
            {
                if (dlxdczzl == "金属氢化物镍电池" || dlxdczzl == "三元锂电池" || dlxdczzl == "磷酸铁锂电池" || dlxdczzl == "锰酸锂电池" || dlxdczzl == "其它")
                {
                    return string.Empty;
                }
                else
                {
                    return "动力蓄电池组种类参数应填写“金属氢化物镍电池/三元锂电池/磷酸铁锂电池/锰酸锂电池/其它”\r\n";
                }
            }
            return string.Empty;
        }

        #endregion

        #region 燃料类型验证

        /// <summary>
        /// 验证传统能源参数
        /// </summary>
        /// <param name="r">验证数据</param>
        /// <param name="dr">匹配数据</param>
        /// <returns></returns>
        protected string VerifyCTNY(DataRow r, DataRow[] dr)
        {
            string message = string.Empty;

            try
            {
                foreach (DataRow edr in dr)
                {
                    string code = Convert.ToString(edr["PARAM_CODE"]);
                    string name = Convert.ToString(edr["PARAM_NAME"]);

                    if (PARAMFLOAT1.Contains(code))
                    {
                        r[code] = this.FormatParam(Convert.ToString(r[code]), "1");
                    }
                    if (PARAMFLOAT2.Contains(code))
                    {
                        r[code] = this.FormatParam(Convert.ToString(r[code]), "2");
                    }

                    switch (code)
                    {
                        case "CT_FDJXH":
                            message += VerifySpace("发动机型号", Convert.ToString(r[code]));
                            break;
                        case "CT_PL":
                            message += VerifyInt("排量", Convert.ToString(r[code]));
                            break;
                        case "CT_EDGL":
                            message += VerifyFloat("额定功率", Convert.ToString(r[code]));
                            break;
                        case "CT_JGL":
                            if (!string.IsNullOrEmpty(Convert.ToString(r[code])))
                                message += VerifyFloat("最大净功率", Convert.ToString(r[code]));
                            break;
                        case "CT_SJGKRLXHL":
                            message += VerifyFloat("市郊工况燃料消耗量", Convert.ToString(r[code]));
                            break;
                        case "CT_SQGKRLXHL":
                            message += VerifyFloat("市区工况燃料消耗量", Convert.ToString(r[code]));
                            break;
                        case "CT_ZHGKCO2PFL":
                            message += VerifyInt("综合工况CO2排放量", Convert.ToString(r[code]));
                            break;
                        case "CT_QGS":
                            message += VerifyInt("气缸数", Convert.ToString(r[code]));
                            break;
                        case "CT_ZHGKRLXHL":
                            message += VerifyFloat("综合工况燃料消耗量", Convert.ToString(r[code]));
                            break;
                        case "CT_BSQXS":
                            message += VerifyBsqxs(Convert.ToString(r[code]));
                            break;
                        case "CT_BSQDWS":
                            message += VerifyBsqdws(Convert.ToString(r[code]));
                            break;
                        default: break;
                    }
                    if (code != "CT_JGL" && code != "CT_QTXX")
                    {
                        message += this.VerifyRequired(name, Convert.ToString(r[code]));
                    }
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            return message;
        }

        // 验证混合动力参数
        protected string VerifyHHDL(DataRow r, DataRow[] dr)
        {
            string message = string.Empty;
            try
            {
                foreach (DataRow edr in dr)
                {
                    string code = Convert.ToString(edr["PARAM_CODE"]);
                    string name = Convert.ToString(edr["PARAM_NAME"]);

                    if (PARAMFLOAT1.Contains(code))
                    {
                        r[code] = this.FormatParam(Convert.ToString(r[code]), "1");
                    }
                    if (PARAMFLOAT2.Contains(code))
                    {
                        r[code] = this.FormatParam(Convert.ToString(r[code]), "2");
                    }

                    switch (code)
                    {
                        case "FCDS_HHDL_FDJXH":
                            message += VerifySpace("发动机型号", Convert.ToString(r[code]));
                            break;
                        case "FCDS_HHDL_DLXDCBNL":
                            message += VerifyInt("动力蓄电池组比能量", Convert.ToString(r[code]));
                            break;
                        case "FCDS_HHDL_DLXDCZZNL":
                            message += VerifyFloat("动力蓄电池组总能量", Convert.ToString(r[code]));
                            break;
                        case "FCDS_HHDL_ZHGKRLXHL":
                            message += VerifyFloat("综合工况燃料消耗量", Convert.ToString(r[code]));
                            break;
                        case "FCDS_HHDL_EDGL":
                            message += VerifyFloat("额定功率", Convert.ToString(r[code]));
                            break;
                        case "FCDS_HHDL_JGL":
                            if (!string.IsNullOrEmpty(Convert.ToString(r[code])))
                                message += VerifyFloat("最大净功率", Convert.ToString(r[code]));
                            break;
                        case "FCDS_HHDL_PL":
                            message += VerifyInt("排量", Convert.ToString(r[code]));
                            break;
                        case "FCDS_HHDL_ZHKGCO2PL":
                            message += VerifyInt("综合工况CO2排放", Convert.ToString(r[code]));
                            break;
                        case "FCDS_HHDL_DLXDCZBCDY":
                            message += VerifyInt("动力蓄电池组标称电压", Convert.ToString(r[code]));
                            break;
                        case "FCDS_HHDL_SJGKRLXHL":
                            message += VerifyFloat("市郊工况燃料消耗量", Convert.ToString(r[code]));
                            break;
                        case "FCDS_HHDL_SQGKRLXHL":
                            message += VerifyFloat("市区工况燃料消耗量", Convert.ToString(r[code]));
                            break;
                        case "FCDS_HHDL_CDDMSXZGCS":
                            message += VerifyInt("纯电动模式下1km最高车速", Convert.ToString(r[code]));
                            break;
                        case "FCDS_HHDL_CDDMSXZHGKXSLC":
                            message += VerifyInt("纯电动模式下综合工况续驶里程", Convert.ToString(r[code]));
                            break;
                        case "FCDS_HHDL_QDDJFZNJ":
                            message += VerifyInt("驱动电机峰值扭矩", Convert.ToString(r[code]));
                            break;
                        case "FCDS_HHDL_QDDJEDGL":
                            message += VerifyFloat("驱动电机额定功率", Convert.ToString(r[code]));
                            break;
                        case "FCDS_HHDL_HHDLZDDGLB":
                            message += VerifyFloat2("混合动力最大电功率比", Convert.ToString(r[code]));
                            break;
                        case "FCDS_HHDL_QGS":
                            message += VerifyFloat2("气缸数", Convert.ToString(r[code]));
                            break;
                        case "FCDS_HHDL_BSQXS":
                            message += VerifyBsqxs(Convert.ToString(r[code]));
                            break;
                        case "FCDS_HHDL_BSQDWS":
                            message += VerifyBsqdws(Convert.ToString(r[code]));
                            break;
                        case "FCDS_HHDL_HHDLJGXS":
                            message += VerifyHhdljgxs(Convert.ToString(r[code]));
                            break;
                        case "FCDS_HHDL_XSMSSDXZGN":
                            message += VerifySdxzgn(Convert.ToString(r[code]));
                            break;
                        case "FCDS_HHDL_DLXDCZZL":
                            message += VerifyDlxdczzl(Convert.ToString(r[code]));
                            break;

                        case "CDS_HHDL_FDJXH":
                            message += VerifySpace("发动机型号", Convert.ToString(r[code]));
                            break;
                        case "CDS_HHDL_DLXDCBNL":
                            message += VerifyInt("动力蓄电池组比能量", Convert.ToString(r[code]));
                            break;
                        case "CDS_HHDL_DLXDCZZNL":
                            message += VerifyFloat("动力蓄电池组总能量", Convert.ToString(r[code]));
                            break;
                        case "CDS_HHDL_ZHGKRLXHL":
                            message += VerifyFloat("综合工况燃料消耗量", Convert.ToString(r[code]));
                            break;
                        case "CDS_HHDL_ZHGKDNXHL":
                            message += VerifyFloat2("综合工况电能消耗量", Convert.ToString(r[code]));
                            break;
                        case "CDS_HHDL_CDDMSXZHGKXSLC":
                            message += VerifyInt("纯电动模式下综合工况续驶里程", Convert.ToString(r[code]));
                            break;
                        case "CDS_HHDL_CDDMSXZGCS":
                            message += VerifyInt("纯电动模式下1km最高车速", Convert.ToString(r[code]));
                            break;
                        case "CDS_HHDL_QDDJFZNJ":
                            message += VerifyInt("驱动电机峰值扭矩", Convert.ToString(r[code]));
                            break;
                        case "CDS_HHDL_QDDJEDGL":
                            message += VerifyFloat("驱动电机额定功率", Convert.ToString(r[code]));
                            break;
                        case "CDS_HHDL_EDGL":
                            message += VerifyFloat("额定功率", Convert.ToString(r[code]));
                            break;
                        case "CDS_HHDL_JGL":
                            if (!string.IsNullOrEmpty(Convert.ToString(r[code])))
                                message += VerifyFloat("最大净功率", Convert.ToString(r[code]));
                            break;
                        case "CDS_HHDL_PL":
                            message += VerifyInt("排量", Convert.ToString(r[code]));
                            break;
                        case "CDS_HHDL_ZHKGCO2PL":
                            message += VerifyInt("综合工况CO2排放", Convert.ToString(r[code]));
                            break;
                        case "CDS_HHDL_DLXDCZBCDY":
                            message += VerifyInt("动力蓄电池组标称电压", Convert.ToString(r[code]));
                            break;
                        case "CDS_HHDL_HHDLZDDGLB":
                            message += VerifyFloat2("混合动力最大电功率比", Convert.ToString(r[code]));
                            break;
                        case "CDS_HHDL_QGS":
                            message += VerifyInt("气缸数", Convert.ToString(r[code]));
                            break;
                        case "CDS_HHDL_TJASYZDNXHL":
                            message += VerifyFloat2("条件A试验中电能消耗量", Convert.ToString(r[code]));
                            break;
                        case "CDS_HHDL_TJBSYZDNXHL":
                            message += VerifyFloat("条件B试验中燃料消耗量", Convert.ToString(r[code]));
                            break;
                        case "CDS_HHDL_BSQXS":
                            message += VerifyBsqxs(Convert.ToString(r[code]));
                            break;
                        case "CDS_HHDL_BSQDWS":
                            message += VerifyBsqdws(Convert.ToString(r[code]));
                            break;
                        case "CDS_HHDL_HHDLJGXS":
                            message += VerifyHhdljgxs(Convert.ToString(r[code]));
                            break;
                        case "CDS_HHDL_XSMSSDXZGN":
                            message += VerifySdxzgn(Convert.ToString(r[code]));
                            break;
                        case "CDS_HHDL_DLXDCZZL":
                            message += VerifyDlxdczzl(Convert.ToString(r[code]));
                            break;
                        default: break;
                    }
                    if (code != "FCDS_HHDL_CDDMSXZGCS" && code != "FCDS_HHDL_CDDMSXZHGKXSLC" && code != "FCDS_HHDL_JGL" && code != "CDS_HHDL_JGL")
                    {
                        message += this.VerifyRequired(name, Convert.ToString(r[code]));
                    }


                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return message;
        }

        /// <summary>
        /// 验证纯电动参数
        /// </summary>
        /// <param name="r">验证数据</param>
        /// <param name="dr">匹配数据</param>
        /// <returns></returns>
        protected string VerifyCDD(DataRow r, DataRow[] dr)
        {
            string message = string.Empty;
            try
            {
                foreach (DataRow edr in dr)
                {
                    string code = Convert.ToString(edr["PARAM_CODE"]);
                    string name = Convert.ToString(edr["PARAM_NAME"]);

                    if (PARAMFLOAT1.Contains(code))
                    {
                        r[code] = this.FormatParam(Convert.ToString(r[code]), "1");
                    }
                    if (PARAMFLOAT2.Contains(code))
                    {
                        r[code] = this.FormatParam(Convert.ToString(r[code]), "2");
                    }

                    switch (code)
                    {
                        case "CDD_DLXDCBNL":
                            message += VerifyInt("动力蓄电池组比能量", Convert.ToString(r[code]));
                            break;
                        case "CDD_DLXDCZEDNL":
                            message += VerifyFloat("动力蓄电池组总能量", Convert.ToString(r[code]));
                            break;
                        case "CDD_DDXDCZZLYZCZBZLDBZ":
                            message += VerifyInt("动力蓄电池总质量与整车整备质量的比值", Convert.ToString(r[code]));
                            break;
                        case "CDD_DLXDCZBCDY":
                            message += VerifyInt("动力蓄电池组标称电压", Convert.ToString(r[code]));
                            break;
                        case "CDD_DDQC30FZZGCS":
                            message += VerifyInt("电动汽车30分钟最高车速", Convert.ToString(r[code]));
                            break;
                        case "CDD_ZHGKXSLC":
                            message += VerifyInt("综合工况续驶里程", Convert.ToString(r[code]));
                            break;
                        case "CDD_QDDJFZNJ":
                            message += VerifyInt("驱动电机峰值扭矩", Convert.ToString(r[code]));
                            break;
                        case "CDD_QDDJEDGL":
                            message += VerifyFloat("驱动电机额定功率", Convert.ToString(r[code]));
                            break;
                        case "CDD_ZHGKDNXHL":
                            message += VerifyFloat2("综合工况电能消耗量", Convert.ToString(r[code]));
                            break;
                        case "CDD_DLXDCZZL":
                            message += VerifyDlxdczzl(Convert.ToString(r[code]));
                            break;
                        default: break;
                    }
                    message += this.VerifyRequired(name, Convert.ToString(r[code]));
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return message;
        }

        // 验证燃料电池参数
        protected string VerifyRLDC(DataRow r, DataRow[] dr)
        {
            string message = string.Empty;
            try
            {
                foreach (DataRow edr in dr)
                {
                    string code = Convert.ToString(edr["PARAM_CODE"]);
                    string name = Convert.ToString(edr["PARAM_NAME"]);

                    if (PARAMFLOAT1.Contains(code))
                    {
                        r[code] = this.FormatParam(Convert.ToString(r[code]), "1");
                    }
                    if (PARAMFLOAT2.Contains(code))
                    {
                        r[code] = this.FormatParam(Convert.ToString(r[code]), "2");
                    }

                    switch (code)
                    {
                        case "RLDC_DDGLMD":
                            message += VerifyFloat("燃料电池堆功率密度", Convert.ToString(r[code]));
                            break;
                        case "RLDC_DDHHJSTJXXDCZBNL":
                            message += VerifyInt("电电混合技术条件下动力蓄电池组比能量", Convert.ToString(r[code]));
                            break;
                        case "RLDC_ZHGKHQL":
                            if (!string.IsNullOrEmpty(Convert.ToString(r[code])))
                                message += VerifyFloat("综合工况燃料消耗量", Convert.ToString(r[code]));
                            break;
                        case "RLDC_ZHGKXSLC":
                            if (!string.IsNullOrEmpty(Convert.ToString(r[code])))
                                message += VerifyInt("综合工况续驶里程", Convert.ToString(r[code]));
                            break;
                        case "RLDC_CDDMSXZGXSCS":
                            if (!string.IsNullOrEmpty(Convert.ToString(r[code])))
                                message += VerifyInt("电动汽车30分钟最高车速", Convert.ToString(r[code]));
                            break;
                        case "RLDC_QDDJEDGL":
                            message += VerifyFloat("驱动电机额定功率", Convert.ToString(r[code]));
                            break;
                        case "RLDC_QDDJFZNJ":
                            message += VerifyInt("驱动电机峰值扭矩", Convert.ToString(r[code]));
                            break;
                        case "RLDC_CQPBCGZYL":
                            message += VerifyInt("储氢瓶标称工作压力", Convert.ToString(r[code]));
                            break;
                        case "RLDC_CQPRJ":
                            message += VerifyInt("储氢瓶容积", Convert.ToString(r[code]));
                            break;
                        case "RLDC_RLDCXTEDGL":
                            message += VerifyFloat("燃料电池系统额定功率", Convert.ToString(r[code]));
                            break;
                        case "RLDC_DLXDCZZL":
                            message += VerifyDlxdczzl(Convert.ToString(r[code]));
                            break;
                        default: break;
                    }
                    if (code != "RLDC_ZHGKHQL" && code != "RLDC_ZHGKXSLC" && code != "RLDC_CDDMSXZGXSCS")
                    {
                        message += this.VerifyRequired(name, Convert.ToString(r[code]));
                    }

                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return message;
        }

        #endregion

        #region 输入验证

        protected string FormatParam(string obj, string strFormat)
        {
            string msg = string.Empty;
            try
            {
                if (obj != null && !string.IsNullOrEmpty(obj))
                {
                    if (Regex.IsMatch(obj, "\\d+(.\\d+)?$") && strFormat == "1")
                    {
                        obj = (double.Parse(obj)).ToString("0.0");
                    }
                    if (Regex.IsMatch(obj, "\\d+(.\\d+)?$") && strFormat == "2")
                    {
                        obj = (double.Parse(obj)).ToString("0.00");
                    }
                }
            }
            catch (Exception)
            {
            }
            return obj;
        }

        // 验证不为空
        protected string VerifyRequired(string strName, string value)
        {
            string msg = string.Empty;
            if (string.IsNullOrEmpty(value))
            {
                msg = strName + "不能为空!\r\n";
            }
            return msg;
        }

        // 验证字符长度
        protected string VerifyStrLen(string strName, string value, int expectedLen)
        {
            string msg = string.Empty;
            if (!string.IsNullOrEmpty(value))
            {
                if (value.Length > expectedLen)
                {
                    msg = String.Format("{0}长度过长，最长为{1}位!\r\n", strName, expectedLen);
                }
            }
            return msg;
        }

        // 验证整型
        protected string VerifyInt(string strName, string value)
        {
            string msg = string.Empty;
            if (!string.IsNullOrEmpty(value) && !Regex.IsMatch(value, "^[0-9]*$"))
            {
                msg = strName + "应为整数!\r\n";
            }
            return msg;
        }

        // 验证浮点型1位小数
        protected string VerifyFloat(string strName, string value)
        {
            string msg = string.Empty;
            // 保留一位小数
            if (!string.IsNullOrEmpty(value) && !Regex.IsMatch(value, @"(\d){1,}\.\d{1}$"))
            {
                msg = strName + "应保留1位小数!\r\n";
            }
            return msg;
        }

        // 验证浮点型两位小数
        protected string VerifyFloat2(string strName, string value)
        {
            string msg = string.Empty;
            // 保留一位小数
            if (!string.IsNullOrEmpty(value) && !Regex.IsMatch(value, @"(\d){1,}\.\d{2}$"))
            {
                msg = strName + "应保留2位小数!\r\n";
            }
            return msg;
        }

        // 验证时间类型
        protected string VerifyDateTime(string strName, DateTime value)
        {
            string msg = string.Empty;
            try
            {
                if (value != null)
                {
                    DateTime time = Convert.ToDateTime(value.ToString());
                }
            }
            catch (Exception)
            {
                msg = strName + "应为时间类型!\r\n";
            }
            return msg;
        }

        // 验证时间类型
        protected string VerifyDateTime(string strName, string value)
        {
            string msg = string.Empty;
            try
            {
                if (value != null)
                {
                    DateTime time = Convert.ToDateTime(value);
                }
            }
            catch (Exception)
            {
                msg = strName + "应为时间类型!\r\n";
            }
            return msg;
        }

        /// <summary>
        /// 参数格式验证，多个数值以参数c隔开，中间不能有空格
        /// </summary>
        /// <param name="value">参数值</param>
        /// <param name="c"></param>
        /// <returns></returns>
        private bool VerifyParamFormat(string value, char c)
        {
            if (!string.IsNullOrEmpty(c.ToString()))
            {
                string[] valueArr = value.Split(c);
                if (valueArr[0] == "" || valueArr[valueArr.Length - 1] == "")
                {
                    return false;
                }
                foreach (string val in valueArr)
                {
                    if (!Regex.IsMatch(val, @"^[+]?\d*$"))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 参数格式验证，中间不能有空格
        /// </summary>
        /// <param name="value">参数值</param>
        /// <returns></returns>
        private string VerifySpace(string strName, string value)
        {
            string msg = string.Empty;
            // 中间空格
            if (value.Trim().IndexOf(" ")>=0)
            {
                msg = strName + "参数值中不能带有空格!\r\n";
            }
            return msg;
        }

        #endregion

        // Kill进程
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern int GetWindowThreadProcessId(IntPtr hwnd, out int ID);
        public static void Kill(Microsoft.Office.Interop.Excel.Application excel)
        {
            IntPtr t = new IntPtr(excel.Hwnd);   //得到这个句柄，具体作用是得到这块内存入口 

            int k = 0;
            GetWindowThreadProcessId(t, out k);   //得到本进程唯一标志k
            System.Diagnostics.Process p = System.Diagnostics.Process.GetProcessById(k);   //得到对进程k的引用
            p.Kill();     //关闭进程k
        }

    }
}
