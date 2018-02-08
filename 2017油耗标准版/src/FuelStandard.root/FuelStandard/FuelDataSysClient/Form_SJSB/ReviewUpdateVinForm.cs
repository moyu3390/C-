﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.OleDb;
using DevExpress.XtraGrid.Views.Base;
using DevExpress.XtraGrid.Views.Grid.Handler;
using DevExpress.XtraEditors;
using DevExpress.XtraTab;
using FuelDataModel;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid;
using FuelDataSysClient.Tool;
using System.Threading;
using FuelDataSysClient.SubForm;
using DevExpress.XtraSplashScreen;

namespace FuelDataSysClient
{
    public partial class ReviewUpdateVinForm : DevExpress.XtraEditors.XtraForm
    {
        FuelDataService.FuelDataSysWebService service = Utils.service;
        private string mainIds;
        private string rllxType;
        private Dictionary<string, string> dictStatus;
        private Dictionary<string, string> dictVinMainId;
        private List<string> vinList;
        InitDataTime initTime = new InitDataTime();

        public ReviewUpdateVinForm()
        {
            InitializeComponent();
            // 设置燃料类型下拉框的值
            this.SetFuelType();
            this.dtStartTime.Text = initTime.getStartTime();
            this.dtEndTime.Text = initTime.getEndTime();
        }

        public ReviewUpdateVinForm(string mainIds)
        {
            InitializeComponent();
            // 设置燃料类型下拉框的值
            this.SetFuelType();
            this.mainIds = mainIds;

            this.SearchLocal(this.mainIds);
        }

        public ReviewUpdateVinForm(string mainIds, string rllxType)
        {
            InitializeComponent();
            // 设置燃料类型下拉框的值
            this.SetFuelType();
            this.mainIds = mainIds;
            this.rllxType = rllxType;

            this.SearchLocal(this.mainIds);
        }
        /// <summary>
        /// 设置燃料类型下拉框的值
        /// </summary>
        protected void SetFuelType()
        {
            List<string> fuelTypeList = Utils.GetFuelType("SEARCH");
            this.cbRllx.Properties.Items.AddRange(fuelTypeList.ToArray());
        }
        // 查看详细
        private void dgvCljbxx_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ColumnView cv = (ColumnView)dgvCljbxx.FocusedView;
            DataRowView dr = (DataRowView)cv.GetFocusedRow();
            
            if (dr == null)
            {
                return;
            }
            string vin = (string)dr.Row.ItemArray[0];
            
            // 获取此VIN的详细信息，带入窗口
            string sql = @"select * from FC_CLJBXX where vin = @vin";
            OleDbParameter[] param = {
                                     new OleDbParameter("@vin",vin)
                                     };
            DataSet ds = AccessHelper.ExecuteDataSet(AccessHelper.conn, sql, param);
            DataTable dt = ds.Tables[0];

            // 弹出详细信息窗口，可修改
            JbxxViewForm jvf = new JbxxViewForm();
            jvf.status = "1";
            if (dt.Rows.Count > 0)
            {
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    DataColumn dc = dt.Columns[i];
                    Control[] c = jvf.Controls.Find("tb" + dc.ColumnName, true);
                    if (c.Length > 0)
                    {
                        if (c[0] is TextEdit)
                        {
                            c[0].Text = dt.Rows[0].ItemArray[i].ToString();
                            continue;
                        }
                        if (c[0] is DevExpress.XtraEditors.ComboBoxEdit )
                        {
                            DevExpress.XtraEditors.ComboBoxEdit  cb = c[0] as DevExpress.XtraEditors.ComboBoxEdit ;
                            cb.Text = dt.Rows[0].ItemArray[i].ToString();
                            if (cb.Text == "汽油" || cb.Text == "柴油" || cb.Text == "两用燃料"
                                || cb.Text == "双燃料" || cb.Text == "气体燃料" || cb.Text == "纯电动" || cb.Text == "非插电式混合动力" || cb.Text == "插电式混合动力" || cb.Text == "燃料电池")
                            {
                                string rlval = cb.Text;
                                if (cb.Text == "汽油" || cb.Text == "柴油" || cb.Text == "两用燃料"
                                || cb.Text == "双燃料" || cb.Text == "气体燃料" )
                                {
                                    rlval = "传统能源";
                                }

                                // 构建燃料参数控件
                                jvf.getParamList(rlval, true);
                            }
                        }
                    }
                }
            }

            // 获取燃料信息
            string rlsql = @"select e.* from RLLX_PARAM_ENTITY e where e.vin = @vin";
            ds = AccessHelper.ExecuteDataSet(AccessHelper.conn, rlsql, param);
            dt = ds.Tables[0];

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow drrlxx = dt.Rows[i];
                string cName = drrlxx.ItemArray[1].ToString();
                Control[] c = jvf.Controls.Find(cName, true);
                 if (c.Length > 0)
                 {
                     if (c[0] is TextEdit)
                     {
                         c[0].Text = drrlxx.ItemArray[3].ToString();
                         continue;
                     }
                     if (c[0] is DevExpress.XtraEditors.ComboBoxEdit)
                     {
                         DevExpress.XtraEditors.ComboBoxEdit cb = c[0] as DevExpress.XtraEditors.ComboBoxEdit;
                         cb.Text = drrlxx.ItemArray[3].ToString();
                     }
                 }
            }

            (jvf.Controls.Find("tc", true)[0] as XtraTabControl).SelectedTabPageIndex = 0;
            jvf.MaximizeBox = false;
            jvf.MinimizeBox = false;
            jvf.setVisible("btnbaocun", false);
            jvf.setVisible("btnbaocunshangbao", false);
            Utils.SetFormMid(jvf);
            jvf.formClosingEventHandel += new FormClosingEventHandler(refrashBySubForm);
            jvf.ShowDialog();
        }

        void refrashBySubForm(object sender, FormClosingEventArgs args)
        {
            this.SearchLocal(this.mainIds);
        }

        // 查询
        private void btnSearch_Click(object sender, EventArgs e)
        {
            this.SearchLocal(this.mainIds);
        }

        // 查询
        private void SearchLocal(string mainIds)
        {
            // 获取本地车辆基本信息
            string sql = string.Format(@"SELECT * FROM FC_CLJBXX WHERE CLXH IN ({0})", mainIds);
            string sw = "";
            string sqlOrder = " ORDER BY STATUS";
            try
            {
                if (!"".Equals(tbVin.Text))
                {
                    sw += " and (vin like '%" + tbVin.Text + "%')";
                }
                if (!"".Equals(tbClxh.Text))
                {
                    sw += " and (CLXH like '%" + tbClxh.Text + "%')";
                }
                if (!"".Equals(tbClzl.Text))
                {
                    sw += " and (CLZL like '%" + tbClzl.Text + "%')";
                }
                if (!"".Equals(cbRllx.Text))
                {
                    sw += " and (rllx like '%" + cbRllx.Text + "%')";
                }
                if (!string.IsNullOrEmpty(this.dtStartTime.Text))
                {
                    sw += " AND (CLZZRQ>=#" + Convert.ToDateTime(this.dtStartTime.Text) + "#)";
                }
                if (!string.IsNullOrEmpty(this.dtEndTime.Text))
                {
                    sw += " AND (CLZZRQ<#" + Convert.ToDateTime(this.dtEndTime.Text).Add(new TimeSpan(24, 0, 0)) + "#)";
                }

                DataSet ds = AccessHelper.ExecuteDataSet(AccessHelper.conn, sql + sw + sqlOrder, null);
                DataTable dt = ds.Tables[0];

                dgvCljbxx.DataSource = dt;

                // 获取新状态
                this.GetNewData(dt);

                lblSum.Text = string.Format("共{0}条", dt.Rows.Count);
            }
            catch (Exception ex)
            {
                MessageBox.Show("查询失败：" + ex.Message, "异常", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            
        }

        /// <summary>
        /// 更新主表关联燃料数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnUpdate_Click(object sender, EventArgs e)
        {
            string message = string.Empty;
            string strCon = AccessHelper.conn;
            SplashScreenManager.ShowForm(typeof(DevWaitForm));
            //DataTable dtCtny = this.GetMainData("CTNY");
            DataTable dtCtny = this.GetMainData(rllxType);

            MitsUtils mitsUtil = new MitsUtils();
            DataTable dtCtnyPam = mitsUtil.GetRllxData(MitsUtils.dictRllx[rllxType]);
            //DataTable dtFcdsPam = mitsUtil.GetRllxData("非插电式混合动力");

            using (OleDbConnection con = new OleDbConnection(strCon))
            {
                con.Open();
                try
                {
                    foreach (string vin in vinList)
                    {
                        //DataRow[] drCtny = dtCtny.Select("CLXH='" + this.dictVinMainId[vin] + "'");
                        DataRow[] drCtny = dtCtny.Select(String.Format("UNIQUE_CODE='{0}'", this.dictVinMainId[vin]));
                        //DataRow[] drFcds = dtFcds.Select("MAIN_ID='" + this.dictVinMainId[vin] + "'");

                        if (drCtny.Length > 0)
                        {
                            message += this.UpdateFuelData(vin, dictStatus[vin], drCtny[0], dtCtnyPam, con);
                        }
                        //if (drFcds.Length > 0)
                        //{
                        //    message += this.UpdateFuelData(vin, dictStatus[vin], drFcds[0], dtFcdsPam, con);
                        //}
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(String.Format("更新失败：{0}\r\n", ex.Message), "异常", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally 
                {
                    SplashScreenManager.CloseForm();
                }
                if (string.IsNullOrEmpty(message))
                {
                    MessageBox.Show("全部更新成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageForm mf = new MessageForm(message);
                    Utils.SetFormMid(mf);
                    mf.Text = "更新结果";
                    mf.ShowDialog();
                }
                this.SearchLocal(this.mainIds);
            }
        }

        // 确定vin修改后的状态,vin对应的参数编号
        protected void GetNewData(DataTable dtUpateVin)
        {
            dictStatus = new Dictionary<string, string>();
            dictVinMainId = new Dictionary<string, string>();
            vinList = new List<string>();

            foreach (DataRow dr in dtUpateVin.Rows)
            {
                string newStatus=string.Empty;
                switch(dr["STATUS"].ToString())
                {
                    case "1": newStatus="1";break;
                    case "0": newStatus="2";break;
                    case "2": newStatus="2";break;
                    default:break;
                }
                string vin = dr["VIN"].ToString();

                // 需要更新的vin
                vinList.Add(vin);

                // vin的新状态
                dictStatus.Add(vin, newStatus);

                // vin对应的参数编号
                //dictVinMainId.Add(vin, dr["CLXH"] == null ? "" : dr["CLXH"].ToString());
                dictVinMainId.Add(vin, dr["UNIQUE_CODE"] == null ? "" : dr["UNIQUE_CODE"].ToString());

            }
        }

        // 获取主表数据
        protected DataTable GetMainData(string fuelType)
        {
            string tableName = string.Empty;
            if (fuelType == "CTNY")
            {
                tableName = "CTNY_MAIN";
            }
            else if (fuelType == "FCDS")
            {
                tableName = "FCDS_MAIN";
            }
            else if (fuelType == "CDS")
            {
                tableName = "CDS_MAIN";
            }
            else if (fuelType == "CDD")
            {
                tableName = "CDD_MAIN";
            }
            else if (fuelType == "RLDC")
            {
                tableName = "RLDC_MAIN";
            }
            //else if (fuelType == "FCDS")
            //{
            //    tableName = "MAIN_FCDSHHDL";
            //}
            string sqlMain = string.Format(@"SELECT * FROM {0}",tableName);

            DataTable dt = AccessHelper.ExecuteDataSet(AccessHelper.conn, sqlMain, null).Tables[0]; ;

            return dt;
        }

        protected string UpdateFuelData(string vin, string status, DataRow drMain, DataTable dtPam, OleDbConnection con)
        {
            string message = string.Empty;
            OleDbTransaction tra = null; //创建事务，开始执行事务
            try
            {
                tra = con.BeginTransaction();
                #region 更新基本信息表

                string sqlUpdateBasic = @"UPDATE FC_CLJBXX SET 
                                    USER_ID=@USER_ID,QCSCQY=@QCSCQY,JKQCZJXS=@JKQCZJXS,CLXH=@CLXH,CLZL=@CLZL,
                                    RLLX=@RLLX,ZCZBZL=@ZCZBZL,ZGCS=@ZGCS,LTGG=@LTGG,ZJ=@ZJ,
                                    TYMC=@TYMC,YYC=@YYC,ZWPS=@ZWPS,ZDSJZZL=@ZDSJZZL,EDZK=@EDZK,LJ=@LJ,
                                    QDXS=@QDXS,JYJGMC=@JYJGMC,JYBGBH=@JYBGBH,STATUS=@STATUS,UPDATETIME=@UPDATETIME,
                                    HGSPBM=@HGSPBM,QTXX=@QTXX
                                   WHERE VIN=@VIN";

                OleDbParameter upTime = new OleDbParameter("@UPDATETIME", DateTime.Now);
                upTime.OleDbType = OleDbType.DBDate;

                OleDbParameter[] parameters = {
					        new OleDbParameter("@USER_ID", Utils.userId),
					        new OleDbParameter("@QCSCQY", drMain["QCSCQY"].ToString()),
					        new OleDbParameter("@JKQCZJXS", drMain["JKQCZJXS"].ToString()),
					        new OleDbParameter("@CLXH", drMain["CLXH"].ToString()),
					        new OleDbParameter("@CLZL", drMain["CLZL"].ToString()),

					        new OleDbParameter("@RLLX", drMain["RLLX"].ToString()),
					        new OleDbParameter("@ZCZBZL", drMain["ZCZBZL"].ToString()),
					        new OleDbParameter("@ZGCS", drMain["ZGCS"].ToString()),
					        new OleDbParameter("@LTGG", drMain["LTGG"].ToString()),
					        new OleDbParameter("@ZJ", drMain["ZJ"].ToString()),

					        new OleDbParameter("@TYMC", drMain["TYMC"].ToString()),
					        new OleDbParameter("@YYC", drMain["YYC"].ToString()),
					        new OleDbParameter("@ZWPS", drMain["ZWPS"].ToString()),
					        new OleDbParameter("@ZDSJZZL", drMain["ZDSJZZL"].ToString()),
					        new OleDbParameter("@EDZK", drMain["EDZK"].ToString()),
					        new OleDbParameter("@LJ", drMain["LJ"].ToString()),

					        new OleDbParameter("@QDXS", drMain["QDXS"].ToString()),
					        new OleDbParameter("@JYJGMC", drMain["JYJGMC"].ToString()),
					        new OleDbParameter("@JYBGBH", drMain["JYBGBH"].ToString()),
					        new OleDbParameter("@STATUS", status),
                            upTime,
					        new OleDbParameter("@HGSPBM", drMain["HGSPBM"].ToString()),
					        //new OleDbParameter("@QTXX", drMain["CT_QTXX"].ToString()),
					        new OleDbParameter("@QTXX", drMain.Table.Columns.Contains("CT_QTXX") ? drMain["CT_QTXX"].ToString().Trim() : ""),
                            
					        new OleDbParameter("@VIN", vin)
                        };
                AccessHelper.ExecuteNonQuery(tra, sqlUpdateBasic, parameters);

                #endregion

                #region 插入参数信息

                // 更新燃料参数表
                foreach (DataRow drParam in dtPam.Rows)
                {
                    string paramCode = drParam["PARAM_CODE"].ToString().Trim();
                    string sqlUpdateParam = @"UPDATE RLLX_PARAM_ENTITY 
                                            SET PARAM_VALUE='" + drMain[paramCode] + "' WHERE VIN='" + vin + "' AND PARAM_CODE='" + paramCode + "'";
                    //OleDbParameter[] paramList = { 
                    //                 new OleDbParameter("@PARAM_CODE",paramCode),
                    //                 new OleDbParameter("@VIN",vin),
                    //                 new OleDbParameter("@PARAM_VALUE",drMain[paramCode]),
                    //               };
                    AccessHelper.ExecuteNonQuery(tra, sqlUpdateParam, null);
                }
                tra.Commit();
                #endregion
            }
            catch (Exception ex)
            {
                tra.Rollback();
                message = ex.Message+"\r\n";
            }

            return message;
        }


        private void repositoryItemComboBox1_CustomDisplayText(object sender, DevExpress.XtraEditors.Controls.CustomDisplayTextEventArgs e)
        {
            if (e.Value != null)
            {
                switch (e.Value.ToString())
                {
                    case "0": e.DisplayText = "已上报"; break;
                    case "1": e.DisplayText = "待上报/补传待上报"; break;
                    case "2": e.DisplayText = "已修改未上报"; break;
                    default: break;
                }
            }
        }

        private void repositoryItemComboBox1_ParseEditValue(object sender, DevExpress.XtraEditors.Controls.ConvertEditValueEventArgs e)
        {
            if (e.Value != null)
            {
                switch (e.Value.ToString())
                {
                    case "已上报": e.Value = "0"; break;
                    case "待上报/补传待上报": e.Value = "1"; break;
                    case "已修改未上报": e.Value = "2"; break;
                    default: break;
                }
            }
        }
    }
}
