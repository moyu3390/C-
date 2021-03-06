﻿namespace Catarc.Adc.NewEnergyAccountSys
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            DevExpress.XtraSplashScreen.SplashScreenManager splashScreenManager = new DevExpress.XtraSplashScreen.SplashScreenManager(this, typeof(global::Catarc.Adc.NewEnergyAccountSys.DevForm.DevSplashScreen), false, false);
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.ribbonStatusBar = new DevExpress.XtraBars.Ribbon.RibbonStatusBar();
            this.barStaticQYMC = new DevExpress.XtraBars.BarStaticItem();
            this.barStaticCopyRight = new DevExpress.XtraBars.BarStaticItem();
            this.ribbon = new DevExpress.XtraBars.Ribbon.RibbonControl();
            this.barBtnSysInfo = new DevExpress.XtraBars.BarButtonItem();
            this.rgbiSkins = new DevExpress.XtraBars.RibbonGalleryBarItem();
            this.ribbonPage1 = new DevExpress.XtraBars.Ribbon.RibbonPage();
            this.ribbonPageGroup1 = new DevExpress.XtraBars.Ribbon.RibbonPageGroup();
            this.ribbonPageGroup2 = new DevExpress.XtraBars.Ribbon.RibbonPageGroup();
            this.galleryControl1 = new DevExpress.XtraBars.Ribbon.GalleryControl();
            this.galleryControlClient1 = new DevExpress.XtraBars.Ribbon.GalleryControlClient();
            this.galleryControl2 = new DevExpress.XtraBars.Ribbon.GalleryControl();
            this.galleryControlClient2 = new DevExpress.XtraBars.Ribbon.GalleryControlClient();
            this.ToolStripMenuItem_Show = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStripMenuItem_Exit = new System.Windows.Forms.ToolStripMenuItem();
            this.navBarControl1 = new DevExpress.XtraNavBar.NavBarControl();
            this.navBarGroup1 = new DevExpress.XtraNavBar.NavBarGroup();
            this.navBarSingleInfo = new DevExpress.XtraNavBar.NavBarItem();
            this.navBarImportOldInfo = new DevExpress.XtraNavBar.NavBarItem();
            this.navBarImportNewInfo = new DevExpress.XtraNavBar.NavBarItem();
            this.navBarNoticeParam = new DevExpress.XtraNavBar.NavBarItem();
            this.navBarContacts = new DevExpress.XtraNavBar.NavBarItem();
            this.navBarGroup2 = new DevExpress.XtraNavBar.NavBarGroup();
            this.navBarSetForm = new DevExpress.XtraNavBar.NavBarItem();
            this.navBarExit = new DevExpress.XtraNavBar.NavBarItem();
            this.splitterControl1 = new DevExpress.XtraEditors.SplitterControl();
            this.navBarItem8 = new DevExpress.XtraNavBar.NavBarItem();
            this.applicationMenu1 = new DevExpress.XtraBars.Ribbon.ApplicationMenu(this.components);
            this.xtraTabbedMdiManager1 = new DevExpress.XtraTabbedMdi.XtraTabbedMdiManager(this.components);
            this.barButtonItem1 = new DevExpress.XtraBars.BarButtonItem();
            this.navBarItem0101 = new DevExpress.XtraNavBar.NavBarItem();
            this.navBarItem0102 = new DevExpress.XtraNavBar.NavBarItem();
            this.navBarItem0201 = new DevExpress.XtraNavBar.NavBarItem();
            ((System.ComponentModel.ISupportInitialize)(this.ribbon)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.galleryControl1)).BeginInit();
            this.galleryControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.galleryControl2)).BeginInit();
            this.galleryControl2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.navBarControl1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.applicationMenu1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xtraTabbedMdiManager1)).BeginInit();
            this.SuspendLayout();
            // 
            // ribbonStatusBar
            // 
            this.ribbonStatusBar.ItemLinks.Add(this.barStaticQYMC);
            this.ribbonStatusBar.ItemLinks.Add(this.barStaticCopyRight);
            this.ribbonStatusBar.Location = new System.Drawing.Point(0, 505);
            this.ribbonStatusBar.Name = "ribbonStatusBar";
            this.ribbonStatusBar.Ribbon = this.ribbon;
            this.ribbonStatusBar.Size = new System.Drawing.Size(793, 31);
            this.ribbonStatusBar.Tag = "";
            // 
            // barStaticQYMC
            // 
            this.barStaticQYMC.Caption = "企业名称";
            this.barStaticQYMC.Id = 31;
            this.barStaticQYMC.Name = "barStaticQYMC";
            this.barStaticQYMC.TextAlignment = System.Drawing.StringAlignment.Near;
            // 
            // barStaticCopyRight
            // 
            this.barStaticCopyRight.Alignment = DevExpress.XtraBars.BarItemLinkAlignment.Right;
            this.barStaticCopyRight.Caption = "版权信息";
            this.barStaticCopyRight.Id = 32;
            this.barStaticCopyRight.Name = "barStaticCopyRight";
            this.barStaticCopyRight.TextAlignment = System.Drawing.StringAlignment.Near;
            // 
            // ribbon
            // 
            this.ribbon.ApplicationIcon = global::Catarc.Adc.NewEnergyAccountSys.Properties.Resources.CarGreen;
            this.ribbon.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(236)))), ((int)(((byte)(239)))));
            this.ribbon.ExpandCollapseItem.Id = 0;
            this.ribbon.ExpandCollapseItem.Name = "";
            this.ribbon.Items.AddRange(new DevExpress.XtraBars.BarItem[] {
            this.ribbon.ExpandCollapseItem,
            this.barBtnSysInfo,
            this.barStaticQYMC,
            this.barStaticCopyRight,
            this.rgbiSkins});
            this.ribbon.Location = new System.Drawing.Point(0, 0);
            this.ribbon.MaxItemId = 34;
            this.ribbon.MdiMergeStyle = DevExpress.XtraBars.Ribbon.RibbonMdiMergeStyle.Always;
            this.ribbon.Name = "ribbon";
            this.ribbon.Pages.AddRange(new DevExpress.XtraBars.Ribbon.RibbonPage[] {
            this.ribbonPage1});
            this.ribbon.Size = new System.Drawing.Size(793, 147);
            this.ribbon.StatusBar = this.ribbonStatusBar;
            // 
            // barBtnSysInfo
            // 
            this.barBtnSysInfo.Caption = "系统说明";
            this.barBtnSysInfo.Id = 30;
            this.barBtnSysInfo.LargeGlyph = global::Catarc.Adc.NewEnergyAccountSys.Properties.Resources.barBtnHome;
            this.barBtnSysInfo.Name = "barBtnSysInfo";
            this.barBtnSysInfo.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.barBtnSysInfo_ItemClick);
            // 
            // rgbiSkins
            // 
            this.rgbiSkins.Caption = "主题颜色";
            this.rgbiSkins.Id = 33;
            this.rgbiSkins.Name = "rgbiSkins";
            this.rgbiSkins.GalleryItemClick += new DevExpress.XtraBars.Ribbon.GalleryItemClickEventHandler(this.rgbiSkins_GalleryItemClick);
            // 
            // ribbonPage1
            // 
            this.ribbonPage1.Groups.AddRange(new DevExpress.XtraBars.Ribbon.RibbonPageGroup[] {
            this.ribbonPageGroup1,
            this.ribbonPageGroup2});
            this.ribbonPage1.MergeOrder = 2;
            this.ribbonPage1.Name = "ribbonPage1";
            this.ribbonPage1.Text = "主页";
            // 
            // ribbonPageGroup1
            // 
            this.ribbonPageGroup1.ItemLinks.Add(this.barBtnSysInfo);
            this.ribbonPageGroup1.Name = "ribbonPageGroup1";
            this.ribbonPageGroup1.Text = "主页";
            // 
            // ribbonPageGroup2
            // 
            this.ribbonPageGroup2.ItemLinks.Add(this.rgbiSkins);
            this.ribbonPageGroup2.Name = "ribbonPageGroup2";
            this.ribbonPageGroup2.Text = "主题";
            // 
            // galleryControl1
            // 
            this.galleryControl1.Controls.Add(this.galleryControlClient1);
            this.galleryControl1.DesignGalleryGroupIndex = 0;
            this.galleryControl1.DesignGalleryItemIndex = 0;
            this.galleryControl1.Location = new System.Drawing.Point(0, 0);
            this.galleryControl1.Name = "galleryControl1";
            this.galleryControl1.Size = new System.Drawing.Size(120, 95);
            this.galleryControl1.TabIndex = 3;
            // 
            // galleryControlClient1
            // 
            this.galleryControlClient1.GalleryControl = this.galleryControl1;
            this.galleryControlClient1.Location = new System.Drawing.Point(2, 2);
            this.galleryControlClient1.Size = new System.Drawing.Size(99, 91);
            // 
            // galleryControl2
            // 
            this.galleryControl2.Controls.Add(this.galleryControlClient2);
            this.galleryControl2.DesignGalleryGroupIndex = 0;
            this.galleryControl2.DesignGalleryItemIndex = 0;
            this.galleryControl2.Location = new System.Drawing.Point(0, 0);
            this.galleryControl2.Name = "galleryControl2";
            this.galleryControl2.Size = new System.Drawing.Size(120, 95);
            this.galleryControl2.TabIndex = 4;
            // 
            // galleryControlClient2
            // 
            this.galleryControlClient2.GalleryControl = this.galleryControl2;
            this.galleryControlClient2.Location = new System.Drawing.Point(2, 2);
            this.galleryControlClient2.Size = new System.Drawing.Size(99, 91);
            // 
            // ToolStripMenuItem_Show
            // 
            this.ToolStripMenuItem_Show.Name = "ToolStripMenuItem_Show";
            this.ToolStripMenuItem_Show.Size = new System.Drawing.Size(116, 22);
            this.ToolStripMenuItem_Show.Text = "还原(&B)";
            // 
            // ToolStripMenuItem_Exit
            // 
            this.ToolStripMenuItem_Exit.Name = "ToolStripMenuItem_Exit";
            this.ToolStripMenuItem_Exit.Size = new System.Drawing.Size(116, 22);
            this.ToolStripMenuItem_Exit.Text = "退出(&E)";
            // 
            // navBarControl1
            // 
            this.navBarControl1.ActiveGroup = this.navBarGroup1;
            this.navBarControl1.Dock = System.Windows.Forms.DockStyle.Left;
            this.navBarControl1.Groups.AddRange(new DevExpress.XtraNavBar.NavBarGroup[] {
            this.navBarGroup1,
            this.navBarGroup2});
            this.navBarControl1.Items.AddRange(new DevExpress.XtraNavBar.NavBarItem[] {
            this.navBarSingleInfo,
            this.navBarContacts,
            this.navBarImportOldInfo,
            this.navBarImportNewInfo,
            this.navBarNoticeParam,
            this.navBarSetForm,
            this.navBarExit});
            this.navBarControl1.Location = new System.Drawing.Point(0, 147);
            this.navBarControl1.Name = "navBarControl1";
            this.navBarControl1.OptionsNavPane.ExpandedWidth = 160;
            this.navBarControl1.Size = new System.Drawing.Size(160, 358);
            this.navBarControl1.TabIndex = 5;
            this.navBarControl1.Text = "navBarControl1";
            // 
            // navBarGroup1
            // 
            this.navBarGroup1.Caption = "本地数据管理";
            this.navBarGroup1.Expanded = true;
            this.navBarGroup1.ItemLinks.AddRange(new DevExpress.XtraNavBar.NavBarItemLink[] {
            new DevExpress.XtraNavBar.NavBarItemLink(this.navBarSingleInfo),
            new DevExpress.XtraNavBar.NavBarItemLink(this.navBarImportOldInfo),
            new DevExpress.XtraNavBar.NavBarItemLink(this.navBarImportNewInfo),
            new DevExpress.XtraNavBar.NavBarItemLink(this.navBarNoticeParam),
            new DevExpress.XtraNavBar.NavBarItemLink(this.navBarContacts)});
            this.navBarGroup1.Name = "navBarGroup1";
            // 
            // navBarSingleInfo
            // 
            this.navBarSingleInfo.Caption = "单条信息录入";
            this.navBarSingleInfo.Hint = "单条信息录入";
            this.navBarSingleInfo.Name = "navBarSingleInfo";
            this.navBarSingleInfo.SmallImage = global::Catarc.Adc.NewEnergyAccountSys.Properties.Resources.nav_page_first;
            this.navBarSingleInfo.LinkClicked += new DevExpress.XtraNavBar.NavBarLinkEventHandler(this.navBarSingleInfo_LinkClicked);
            // 
            // navBarImportOldInfo
            // 
            this.navBarImportOldInfo.Caption = "申报数据管理";
            this.navBarImportOldInfo.Hint = "申报数据管理";
            this.navBarImportOldInfo.Name = "navBarImportOldInfo";
            this.navBarImportOldInfo.SmallImage = global::Catarc.Adc.NewEnergyAccountSys.Properties.Resources.nav_page_local;
            this.navBarImportOldInfo.LinkClicked += new DevExpress.XtraNavBar.NavBarLinkEventHandler(this.navBarImportOldInfo_LinkClicked);
            // 
            // navBarImportNewInfo
            // 
            this.navBarImportNewInfo.Caption = "填报数据管理";
            this.navBarImportNewInfo.Hint = "填报数据管理";
            this.navBarImportNewInfo.Name = "navBarImportNewInfo";
            this.navBarImportNewInfo.SmallImage = global::Catarc.Adc.NewEnergyAccountSys.Properties.Resources.nav_upload;
            this.navBarImportNewInfo.LinkClicked += new DevExpress.XtraNavBar.NavBarLinkEventHandler(this.navBarImportNewInfo_LinkClicked);
            // 
            // navBarNoticeParam
            // 
            this.navBarNoticeParam.Caption = "云端数据管理";
            this.navBarNoticeParam.Name = "navBarNoticeParam";
            this.navBarNoticeParam.SmallImage = global::Catarc.Adc.NewEnergyAccountSys.Properties.Resources.nav_page_second;
            this.navBarNoticeParam.LinkClicked += new DevExpress.XtraNavBar.NavBarLinkEventHandler(this.navBarNoticeParam_LinkClicked);
            // 
            // navBarContacts
            // 
            this.navBarContacts.Caption = "企业联络人";
            this.navBarContacts.Hint = "企业联络人";
            this.navBarContacts.Name = "navBarContacts";
            this.navBarContacts.SmallImage = global::Catarc.Adc.NewEnergyAccountSys.Properties.Resources.nav_tools;
            this.navBarContacts.LinkClicked += new DevExpress.XtraNavBar.NavBarLinkEventHandler(this.navBarContacts_LinkClicked);
            // 
            // navBarGroup2
            // 
            this.navBarGroup2.Caption = "工具";
            this.navBarGroup2.Expanded = true;
            this.navBarGroup2.ItemLinks.AddRange(new DevExpress.XtraNavBar.NavBarItemLink[] {
            new DevExpress.XtraNavBar.NavBarItemLink(this.navBarSetForm),
            new DevExpress.XtraNavBar.NavBarItemLink(this.navBarExit)});
            this.navBarGroup2.Name = "navBarGroup2";
            // 
            // navBarSetForm
            // 
            this.navBarSetForm.Caption = "设置";
            this.navBarSetForm.Name = "navBarSetForm";
            this.navBarSetForm.SmallImage = global::Catarc.Adc.NewEnergyAccountSys.Properties.Resources.picBox_Set;
            this.navBarSetForm.LinkClicked += new DevExpress.XtraNavBar.NavBarLinkEventHandler(this.navBarSetForm_LinkClicked);
            // 
            // navBarExit
            // 
            this.navBarExit.Caption = "退出";
            this.navBarExit.Name = "navBarExit";
            this.navBarExit.SmallImage = global::Catarc.Adc.NewEnergyAccountSys.Properties.Resources.navBtn_exit;
            this.navBarExit.LinkClicked += new DevExpress.XtraNavBar.NavBarLinkEventHandler(this.navBarExit_LinkClicked);
            // 
            // splitterControl1
            // 
            this.splitterControl1.Appearance.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.splitterControl1.Appearance.Options.UseBackColor = true;
            this.splitterControl1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.splitterControl1.Location = new System.Drawing.Point(160, 147);
            this.splitterControl1.Name = "splitterControl1";
            this.splitterControl1.Size = new System.Drawing.Size(5, 358);
            this.splitterControl1.TabIndex = 6;
            this.splitterControl1.TabStop = false;
            // 
            // navBarItem8
            // 
            this.navBarItem8.Caption = "燃料规格参数同步";
            this.navBarItem8.Name = "navBarItem8";
            // 
            // applicationMenu1
            // 
            this.applicationMenu1.Name = "applicationMenu1";
            this.applicationMenu1.Ribbon = this.ribbon;
            // 
            // xtraTabbedMdiManager1
            // 
            this.xtraTabbedMdiManager1.ClosePageButtonShowMode = DevExpress.XtraTab.ClosePageButtonShowMode.InActiveTabPageHeader;
            this.xtraTabbedMdiManager1.MdiParent = this;
            this.xtraTabbedMdiManager1.SelectedPageChanged += new System.EventHandler(this.xtraTabbedMdiManager1_SelectedPageChanged);
            // 
            // barButtonItem1
            // 
            this.barButtonItem1.Caption = "系统说明";
            this.barButtonItem1.Id = 30;
            this.barButtonItem1.LargeGlyph = global::Catarc.Adc.NewEnergyAccountSys.Properties.Resources.barBtnHome;
            this.barButtonItem1.Name = "barButtonItem1";
            // 
            // navBarItem0101
            // 
            this.navBarItem0101.Caption = "单条信息录入";
            this.navBarItem0101.Hint = "单条信息录入";
            this.navBarItem0101.Name = "navBarItem0101";
            this.navBarItem0101.SmallImage = global::Catarc.Adc.NewEnergyAccountSys.Properties.Resources.nav_page_first;
            // 
            // navBarItem0102
            // 
            this.navBarItem0102.Caption = "批量信息录入";
            this.navBarItem0102.Hint = "批量信息录入";
            this.navBarItem0102.Name = "navBarItem0102";
            this.navBarItem0102.SmallImage = global::Catarc.Adc.NewEnergyAccountSys.Properties.Resources.nav_upload;
            // 
            // navBarItem0201
            // 
            this.navBarItem0201.Caption = "企业联络人设置";
            this.navBarItem0201.Hint = "企业联络人设置";
            this.navBarItem0201.Name = "navBarItem0201";
            this.navBarItem0201.SmallImage = global::Catarc.Adc.NewEnergyAccountSys.Properties.Resources.nav_tools;
            // 
            // MainForm
            // 
            this.Appearance.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(236)))), ((int)(((byte)(239)))));
            this.Appearance.Options.UseBackColor = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(793, 536);
            this.Controls.Add(this.splitterControl1);
            this.Controls.Add(this.navBarControl1);
            this.Controls.Add(this.ribbonStatusBar);
            this.Controls.Add(this.ribbon);
            this.Controls.Add(this.galleryControl1);
            this.Controls.Add(this.galleryControl2);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.IsMdiContainer = true;
            this.Name = "MainForm";
            this.Ribbon = this.ribbon;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.StatusBar = this.ribbonStatusBar;
            this.Text = "新能源汽车国家补贴辅助申报比对平台";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.MainForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.ribbon)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.galleryControl1)).EndInit();
            this.galleryControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.galleryControl2)).EndInit();
            this.galleryControl2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.navBarControl1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.applicationMenu1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xtraTabbedMdiManager1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraBars.Ribbon.RibbonStatusBar ribbonStatusBar;
        private DevExpress.XtraBars.Ribbon.RibbonControl ribbon;
        private DevExpress.XtraBars.Ribbon.GalleryControl galleryControl1;
        private DevExpress.XtraBars.Ribbon.GalleryControl galleryControl2;
        private DevExpress.XtraBars.Ribbon.GalleryControlClient galleryControlClient1;
        private DevExpress.XtraBars.Ribbon.GalleryControlClient galleryControlClient2;
        private DevExpress.XtraEditors.SplitterControl splitterControl1;
        private DevExpress.XtraNavBar.NavBarControl navBarControl1;
        private DevExpress.XtraNavBar.NavBarGroup navBarGroup1;
        private DevExpress.XtraNavBar.NavBarGroup navBarGroup2;
        private DevExpress.XtraNavBar.NavBarItem navBarSingleInfo;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItem_Show;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItem_Exit;
        private DevExpress.XtraNavBar.NavBarItem navBarContacts;
        private DevExpress.XtraNavBar.NavBarItem navBarItem8;
        private DevExpress.XtraNavBar.NavBarItem navBarImportOldInfo;
        private DevExpress.XtraBars.BarButtonItem barBtnSysInfo;
        private DevExpress.XtraBars.Ribbon.RibbonPage ribbonPage1;
        private DevExpress.XtraBars.Ribbon.RibbonPageGroup ribbonPageGroup1;
        private DevExpress.XtraBars.Ribbon.ApplicationMenu applicationMenu1;
        private DevExpress.XtraTabbedMdi.XtraTabbedMdiManager xtraTabbedMdiManager1;
        private DevExpress.XtraBars.BarButtonItem barButtonItem1;
        private DevExpress.XtraNavBar.NavBarItem navBarItem0101;
        private DevExpress.XtraNavBar.NavBarItem navBarItem0102;
        private DevExpress.XtraNavBar.NavBarItem navBarItem0201;
        private DevExpress.XtraNavBar.NavBarItem navBarImportNewInfo;
        private DevExpress.XtraNavBar.NavBarItem navBarNoticeParam;
        private DevExpress.XtraNavBar.NavBarItem navBarSetForm;
        private DevExpress.XtraBars.BarStaticItem barStaticQYMC;
        private DevExpress.XtraBars.BarStaticItem barStaticCopyRight;
        private DevExpress.XtraBars.RibbonGalleryBarItem rgbiSkins;
        private DevExpress.XtraBars.Ribbon.RibbonPageGroup ribbonPageGroup2;
        private DevExpress.XtraNavBar.NavBarItem navBarExit;
    }
}