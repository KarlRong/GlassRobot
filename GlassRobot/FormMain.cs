using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Xml;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Collections;
using System.Diagnostics;
using Controller;

namespace GlassRobot
{
    public partial class FormMain : Form
    {
        //定义公共变量=======================================================================================
        #region
        public Motion motion; //运动控制器对象

        public const double KAngle = 57.29577951;//每弧度对应的度数

        public bool modified = false;//文本变化标记
        public string Password = "10086";//密码
        public string NetIP = "";//网络IP
        public string NetPort = "";//网络端口

        public Job curJob = new Job();  //当前主文件

        public int[] B = new int[100];//预定义变量数组，开放给用户使用

        public int[] Indexlabel = new int[200];//用于存放标号所在行的索引

        public int ontime = 0, runtime = 0, time1m = 0, time5s = 0;  //上电时间和运行时间，1分钟计数器，5秒钟计数器
        public int CThistory = 0, CTtoday = 0, CTtemp = 0;  //工作计件：历史总数，本次开机以来总数，当前数（可清零）

        public XmlDocument fileJob = new XmlDocument();  //机器人工作文件
        public XmlDocument filePRM = new XmlDocument();  //机器人参数文件
        public XmlDocument fileERR = new XmlDocument();  //机器人错误信息

        public MathFun.EPOStyp[] globalPos = new MathFun.EPOStyp[100];//机器人全局位置变量
        MathFun.EPOStyp[] PEpos = new MathFun.EPOStyp[100];//机器人运动程序临时位置变量

        public bool recorded = false;  //错误记录标志
        public bool saved = false;  //是否记录当前位置？记录后再移动失效，示教器返回失效
        public Var.ManuSpeed manuSpeed = Var.ManuSpeed.Low;  //当前手动速度
        public Var.CurrentMode currentMode = Var.CurrentMode.Teach;  //机器人当前模式（示教/自动）
        public Var.PlayMode playMode = Var.PlayMode.Cycle;  //自动工作模式
        public Var.CurrentStatus currentStatus = Var.CurrentStatus.Stop;  //机器人当前状态
        public Var.CompileMode CompMode = Var.CompileMode.Run;
        public int errorCode = 0;  //错误代码
        public int errorForm = 0;
        public int errorMath = 0;
        public bool run = false;  //正在运行
        public bool IOrefreshLock = false;  //IO刷新锁定

        public int indexPos = 0;  //文本编辑中缺省的示教点索引和速度
        public double defaultVJ = 0, defaultVL = 0;

        #endregion

        public FormMain()
        {
            InitializeComponent();
        }


        //主窗口初始化 RC_IniRobot InitMathfun
        private void FormMain_Load(object sender, EventArgs e)
        {
            //创建motion对象
            motion = new Motion();

            //打开参数文件，读入全部参数
            try
            {
                filePRM.Load(Var.FilePath + "Robot.PRM");
                fileERR.Load(Var.FilePath + "Error.RCD");
            }
            catch
            {
                MessageBox.Show("无法打开系统文件！", "严重错误！");
                this.Close();
                return;
            }
                
            if (readAllParameter() != 0)  //读入所有参数并初始化所有界面
            {
                MessageBox.Show("无法读入系统参数！", "严重错误！");
                this.Close();
                return;
            }

            //运动控制器初始化
            motion.errorMotion = motion.RC_IniRobot();
            if (motion.errorMotion != 0)
            {
                MessageBox.Show("运动控制器初始化失败", "严重错误！");
                this.Close();
                return;
            }

            //初始化运动函数库
            InitMathfun();

            //读取绝对编码器
            double[] encData = new double[8];
            if (ReadEncode(ref encData) == 0)
            {
                if (motion.RC_SetPos(encData) == -1)
                    MessageBox.Show(Var.errMsgofMotion[10], "严重错误");
            }
            else
                MessageBox.Show("读取绝对编码器错误！", "警告！");
            
            //加载主工作文件
            curJob.loadJBI(@"\Hard Disk\GlassRobot\Job1.JBI");
            lbfile.Items.Clear();
            for (int i = 0; i < curJob.lengthProg; i++)
            {
                lbfile.Items.Add(curJob.textLines[i].ToString());
            }
            tbJobName.Text = curJob.name;
            labelSysInfo.Text = "提示：" + curJob.name + "文件已加载";
            labelFile.Text = "关闭";
            if (curJob.NumPos > 0)
            {
                curJob.IndexPos = 0;
                btnCurCPos_Click(null, null);
            }

            
            //设定初始页面
            tabControl.SelectedIndex = 2;
            panelEncoder.Visible = false;
            if (SYSPLC.Mod == 1)
            {
                currentMode = Var.CurrentMode.Teach;
                panelManu.Visible = true;
                panelRun.Visible = false;
            }
            else if (SYSPLC.Mod == 2)
            {
                currentMode = Var.CurrentMode.Remote;
                panelManu.Visible = false;
                panelRun.Visible = true;
            }
            else if (SYSPLC.Mod == 3)
            {
                currentMode = Var.CurrentMode.Play;
                panelManu.Visible = false;
                panelRun.Visible = true;
            }
        }

        // 退出主窗口 RC_MovStop RC_Close
        private void FormMain_Closing(object sender, CancelEventArgs e)
        {
            motion.RC_MovStop(false);
            SYSPLC.TboxIOW = ~0xffff;
            Gts.GT_SetExtIoValue(0, (ushort)SYSPLC.TboxIOW, (byte)7);  //示教盒指示灯
            //Gts.GT_SetVal_eHMI((short)0, (ushort)SYSPLC.TboxIOW);

            motion.RC_Close();
            Sv_SysInfo();  //保存系统信息
        }

        //窗口关闭（需退出后台线程、关闭伺服，尚未）
        private void buttonQuit_Click(object sender, EventArgs e)
        {
            bool close = remind();
            if (!close)
                return;
            this.Close();
        }

        //各界面各菜单图片响应=================================
        #region

        //打开/关闭工作文件
        private void picFileOpen_Click(object sender, EventArgs e)
        {
            if (run)
            {
                labelSysInfo.Text = "提示：正在运行，不可操作当前文件，请先停止";
                return;
            }
            if (labelFile.Text == "打开")  //打开文件
            {
                if (openFileDialog.ShowDialog() == DialogResult.OK) //打开文件
                {
                    curJob.loadJBI(openFileDialog.FileName);
                    lbfile.Items.Clear();
                    for (int i = 0; i < curJob.lengthProg; i++)
                    {
                        lbfile.Items.Add(curJob.textLines[i].ToString());
                    }
                    tbJobName.Text = curJob.name;
                    labelSysInfo.Text = "提示：" + curJob.name + "文件已加载";
                    labelFile.Text = "关闭";
                    if (curJob.NumPos > 0)
                    {
                        curJob.IndexPos = 0;
                        btnCurCPos_Click(null, null);
                    }
                }
            }
            else  //关闭当前文件
            {
                bool close = remind();
                if (!close)
                    return;
                lbfile.Items.Clear();
                curJob.Close();
                btnCurCPos.Text = "C----";
                picSaveAs.Enabled = false;
                picInsert.Enabled = false;
                picDelete.Enabled = false;
                textCMD.Enabled = false;
                labelSysInfo.Text = "系统提示信息";
                labelFile.Text = "打开";
                tbJobName.Text = "";
            }
        }

        //开始编辑（开编辑权限）
        private void picEdit_Click(object sender, EventArgs e)//编辑文件,点击此按钮，若本来Enabled 为 false，则变为true；若本来为true，则调出编辑工具对当前行进行编辑。
        {
            if (picSaveAs.Enabled == false)
            {
                string pass = KeyNum("请输入管理密码：", "", true);
                if (pass != Password)
                {
                    MessageBox.Show("密码错误！");
                    return;
                }
                picSaveAs.Enabled = true;
                picInsert.Enabled = true;
                picDelete.Enabled = true;
                textCMD.Enabled = true;
            }
            else  //关闭权限
            {
                picSaveAs.Enabled = false;
                picInsert.Enabled = false;
                picDelete.Enabled = false;
                textCMD.Enabled = false;
            }
        }

        //保存文件 FileName
        private void picSaveAs_Click(object sender, EventArgs e)//另存文件
        {
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                curJob.name = saveFileDialog.FileName.Remove(0, saveFileDialog.FileName.LastIndexOf('\\')+1);
                curJob.saveJBI(saveFileDialog.FileName);
                modified = false;
                tbJobName.Text = saveFileDialog.FileName;
            }
        }

        //密码修改 Sleep(50)
        private void pbSet_Click(object sender, EventArgs e)
        {
            string pass = "";
            string secpass = "";
            DialogResult result = MessageBox.Show("要修改当前密码吗？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            if (result == DialogResult.Yes)
            {
                pass = KeyNum("请输入旧的管理密码：", "", true);
                if (pass != Password)  //初始密码10086
                {
                    MessageBox.Show("密码错误,请重新操作");
                    return;
                }
                else  //关闭权限
                {
                    pass = KeyNum("请输入新的管理密码：", "", true);
                    Thread.Sleep(50);
                    secpass = KeyNum("请重新输入新的管理密码：", "", true);
                    if (pass == secpass)
                    {
                        Password = pass;
                        Sv_Password();
                        MessageBox.Show("管理密码修改成功");
                    }
                    else
                    {
                        MessageBox.Show("管理密码修改失败，请重新操作");
                    }
                }
            }
        }

        //工具设定
        private void picTool_Click(object sender, EventArgs e)
        {
            panelTool.Visible = true;
            panelServo.Visible = false;
            panelNet.Visible = false;
        }

        //伺服参数设定
        private void picServoParam_Click(object sender, EventArgs e)
        {
            panelTool.Visible = false;
            panelServo.Visible = true;
            panelNet.Visible = false;
        }

        //网络参数设定
        private void picNetwork_Click(object sender, EventArgs e)
        {
            panelTool.Visible = false;
            panelServo.Visible = false;
            panelNet.Visible = true;
        }

        //保存所有参数
        private void picSaveParam_Click(object sender, EventArgs e)
        {
            App_Robot();//保存并立即应用
        }

        //读取绝对编码器界面
        private void picEncoder_Click(object sender, EventArgs e)
        {
            panelEncoder.Visible = true;
            panelManu.Visible = false;
            panelRun.Visible = false;
        }

        //自动界面
        private void picPlay_Click(object sender, EventArgs e)
        {
            //if ((currentMode == Var.CurrentMode.Teach) || (currentMode == Var.CurrentMode.Remote))
            //{
            //    if (!motion.teachLock)  //没有锁定在示教状态
            //    {
                    currentMode = Var.CurrentMode.Play;
                    picOpMod.Image = Properties.Resources.AUTO;
                    panelEncoder.Visible = false;
                    panelManu.Visible = false;
                    panelRun.Visible = true;
            //    }
            //    else errorCode = 99;  //锁定示教，不能切换，报错
            //}
        }

        //手动界面
        private void picTeach_Click(object sender, EventArgs e)
        {
            if (!run)  //没有正在运行
            {
                currentMode = Var.CurrentMode.Teach;
                picOpMod.Image = Properties.Resources.HAND;

                panelEncoder.Visible = false;
                panelManu.Visible = true;
                panelRun.Visible = false;
            }
            else errorCode = 99;  //正在自动运行，不能切换，报错
        }

        //系统IO界面
        private void picSysIO_Click(object sender, EventArgs e)
        {
            panelSysIO.Visible = true;
            panelUserIO.Visible = false;
        }

        //用户IO界面
        private void picUserIO_Click(object sender, EventArgs e)
        {
            panelSysIO.Visible = false;
            panelUserIO.Visible = true;
        }

        //统计信息界面
        private void picCountInfo_Click(object sender, EventArgs e)
        {
            panelCountInfo.Visible = true;
            panelErrList.Visible = false;
            panelBackup.Visible = false;
            panelVersionInfo.Visible = false;
        }

        //错误信息界面
        private void picErrList_Click(object sender, EventArgs e)
        {
            textBoxErrList.Text = "";
            Ld_ErrorMsg();
            if (textBoxErrList.Text.Length > 0)
                textBoxErrList.SelectionStart = textBoxErrList.Text.Length - 1;
            panelCountInfo.Visible = false;
            panelErrList.Visible = true;
            panelBackup.Visible = false;
            panelVersionInfo.Visible = false;
        }

        //备份界面
        private void picBackUp_Click(object sender, EventArgs e)
        {
            panelCountInfo.Visible = false;
            panelErrList.Visible = false;
            panelBackup.Visible = true;
            panelVersionInfo.Visible = false;
        }

        //版本信息界面
        private void picVer_Click(object sender, EventArgs e)
        {
            panelCountInfo.Visible = false;
            panelErrList.Visible = false;
            panelBackup.Visible = false;
            panelVersionInfo.Visible = true;
        }

        //帮助信息界面
        private void picHelp_Click(object sender, EventArgs e)
        {
            //Process helpprocess = new Process();
            //helpprocess.StartInfo.FileName = "PegHelp.exe";
            //helpprocess.StartInfo.Arguments = Var.HelpPath;
            //helpprocess.Start();
        }

        #endregion

        //各状态图片响应=======================================
        #region

        //状态显示（不能在此改变状态，以防误操作）

        //切换操作机（仅对示教状态有用） motoin.tool
        private void picManipulator_Click(object sender, EventArgs e)
        {
            if (motion.robotNum == 1)
            {
                motion.robotNum = 2;
                picManipulator.Image = Properties.Resources.ROBOT2;
                SYSPLC.TB_robotgroup = true;
                motion.tool.X = motion.tool2.X;
                motion.tool.Z = motion.tool2.Z;
                MathFun.TOOL = motion.tool;
            }
            else if (motion.robotNum == 2)
            {
                motion.robotNum = 1;
                picManipulator.Image = Properties.Resources.ROBOT1;
                SYSPLC.TB_robotgroup = false;
                motion.tool.X = motion.tool1.X;
                motion.tool.Z = motion.tool1.Z;
                MathFun.TOOL = motion.tool;
            }
        }

        //切换坐标系（仅对示教状态有用） 坐标
        private void picCoord_Click(object sender, EventArgs e)
        {
            if (currentMode != Var.CurrentMode.Teach)
                return;

            if (motion.coord == MathFun.COORDtyp.Robot)
            {
                motion.coord = MathFun.COORDtyp.Joint;
                motion.dirPos.C = MathFun.COORDtyp.Joint;
                picCoord.Image = Properties.Resources.COORDROBOT;

                picAN.Image = Properties.Resources.AN;
                picAP.Image = Properties.Resources.AP;
                picBN.Image = Properties.Resources.BN;
                picBP.Image = Properties.Resources.BP;
                picCN.Image = Properties.Resources.CN;
                picCP.Image = Properties.Resources.CP;
                picDN.Image = Properties.Resources.DN;
                picDP.Image = Properties.Resources.DP;
            }
            else if (motion.coord == MathFun.COORDtyp.Joint)
            {
                motion.coord = MathFun.COORDtyp.Robot;
                motion.dirPos.C = MathFun.COORDtyp.Robot;
                picCoord.Image = Properties.Resources.COORDCARTESIAN;

                picAN.Image = Properties.Resources.XN;
                picAP.Image = Properties.Resources.XP;
                picBN.Image = Properties.Resources.ZN;
                picBP.Image = Properties.Resources.ZP;
                picCN.Image = Properties.Resources.RN;
                picCP.Image = Properties.Resources.RP;
                picDN.Image = Properties.Resources.RzN;
                picDP.Image = Properties.Resources.RzP;
            }
        }

        //切换速度（仅限示教状态） speedJ speedL
        private void picSpeed_Click(object sender, EventArgs e)
        {
            if (currentMode != Var.CurrentMode.Teach)
                return;

            if (manuSpeed == Var.ManuSpeed.Inc)
            {
                manuSpeed = Var.ManuSpeed.Low;
                motion.speedJ = Var.speedL;
                motion.speedL = motion.MaxSpeedL * Var.speedL;
                picSpeed.Image = Properties.Resources.SPEEDLOW;
            }
            else if (manuSpeed == Var.ManuSpeed.Low)
            {
                manuSpeed = Var.ManuSpeed.Middle;
                motion.speedJ = Var.speedM;
                motion.speedL = motion.MaxSpeedL * Var.speedM;
                picSpeed.Image = Properties.Resources.SPEEDHIGH;
            }
            else if (manuSpeed == Var.ManuSpeed.Middle)
            {
                manuSpeed = Var.ManuSpeed.High;
                motion.speedJ = Var.speedH;
                motion.speedL = motion.MaxSpeedL * Var.speedH;
                picSpeed.Image = Properties.Resources.SPEEDMAX;
            }
            else if (manuSpeed == Var.ManuSpeed.High)
            {
                manuSpeed = Var.ManuSpeed.Inc;
                motion.speedJ = Var.speedI;
                motion.speedL = motion.MaxSpeedL * Var.speedI;
                picSpeed.Image = Properties.Resources.SPEEDINC;
            }
        }

        //手动/自动切换（仅受示教盒旋钮控制，软件界面不可更改，以防误操作）
        private void picOpMod_Click(object sender, EventArgs e)
        {
            //if (currentMode == Var.CurrentMode.Teach)
            //    picPlay_Click(null, null);
            //else if (currentMode == Var.CurrentMode.Play)
            //    picTeach_Click(null, null);
        }

        //示教锁定切换（仅限示教状态） motion.teachLock
        private void picTeachLock_Click(object sender, EventArgs e)
        {
            if (currentMode != Var.CurrentMode.Teach)
                return;

            if (motion.teachLock)
            {
                motion.teachLock = false;
                picTeachLock.Image = Properties.Resources.TUNLOCK;
            }
            else
            {
                motion.teachLock = true;
                picTeachLock.Image = Properties.Resources.TLOCK;
            }
        }

        //伺服电源状态显示（不能在此改变状态，以防误操作）

        //运行方式切换（单步/单次/循环）playMode
        private void picCycle_Click(object sender, EventArgs e)
        {
            if (playMode == Var.PlayMode.Auto)
            {
                playMode = Var.PlayMode.Cycle;
                picCycle.Image = Properties.Resources.CYCLERUN;
            }
            else if (playMode == Var.PlayMode.Cycle)
            {
                playMode = Var.PlayMode.Step;
                picCycle.Image = Properties.Resources.POINTRUN;
            }
            else if (playMode == Var.PlayMode.Step)
            {
                playMode = Var.PlayMode.Auto;
                picCycle.Image = Properties.Resources.AUTORUN;
            }
        }

        #endregion

        //工作文件各界面操作===================================
        #region

        //插入一行程序
        private void picInsert_Click(object sender, EventArgs e)
        {
            lbfile.Items.Insert(lbfile.SelectedIndex + 1, textCMD.Text);
            curJob.insLine(lbfile.SelectedIndex + 1, textCMD.Text);
            lbfile.SelectedIndex++;
            modified = true;  //发生修改
        }

        //删除一行程序
        private void picDelete_Click(object sender, EventArgs e)
        {
            if (lbfile.SelectedIndex != -1)
            {
                curJob.delLine(lbfile.SelectedIndex);
                lbfile.SelectedIndex--;
                lbfile.Items.RemoveAt(lbfile.SelectedIndex + 1);
                modified = true;
            }
        }

        //命令行输入得焦点（点击），激活命令行列表
        private void textCMD_GotFocus(object sender, EventArgs e)
        {
            lbCmd.Visible = true;  //打开命令行列表
            lbCmd.Focus();  //命令行列表得到焦点
            if (lbCmd.SelectedIndex == -1)
                lbCmd.SelectedIndex = 2;
            SYSPLC.TB_informlist = true;
        }

        //命令行列表失去焦点
        private void lbCmd_LostFocus(object sender, EventArgs e)
        {
            if (lbCmd.Visible)
                lbCmd.Focus();  //命令行列表强制得到焦点
        }

        //命令行列表输入
        private void lbCmd_KeyDown(object sender, KeyEventArgs e)
        {
            int keynum = (int)e.KeyData;
            switch (keynum)
            {
                case 27:  //Esc
                case 69:  //E，命令一览
                    lbCmd.Visible = false;
                    SYSPLC.TB_informlist = false;
                    break;
                case 115:  //F4，选择
                case 89:  //Y，确认
                    lbCmdPutin();
                    lbCmd.Visible = false;
                    SYSPLC.TB_informlist = false;
                    break;
                default:
                    break;
            }
        }

        //抢断文件显示窗口对按键的反应 //什么情况下抢断？
        private void lbfile_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
        }

        #endregion

        //系统设置各界面参数读取、设置=========================
        #region

        //panelTool中的文本框得焦事件
        private void textBoxCatchTime_GotFocus(object sender, EventArgs e)
        {
            picSaveParam.Focus();
            textBoxCatchTime.Text = KeyNum("请输入手爪吸合时间：", textBoxCatchTime.Text, false);
        }

        private void textBoxReleaseTime_GotFocus(object sender, EventArgs e)
        {
            picSaveParam.Focus();
            textBoxReleaseTime.Text = KeyNum("请输入手爪释放时间：", textBoxReleaseTime.Text, false);
        }
        
        private void textBoxHandIN1_GotFocus(object sender, EventArgs e)
        {
            picSaveParam.Focus();
            textBoxHandIN1.Text = KeyNum("请输入手爪输入端口：", textBoxHandIN1.Text, false);
        }
        
        private void textBoxHandOUT1_GotFocus(object sender, EventArgs e)
        {
            picSaveParam.Focus();
            textBoxHandOUT1.Text = KeyNum("请输入手爪输出端口：", textBoxHandOUT1.Text, false);
        }
        
        private void textBoxHandX_GotFocus(object sender, EventArgs e)
        {
            picSaveParam.Focus();
            textBoxHandX.Text = KeyNum("请输入中心坐标X：", textBoxHandX.Text, false);
        }

        private void textBoxHandY_GotFocus(object sender, EventArgs e)
        {
            picSaveParam.Focus();
            textBoxHandY.Text = KeyNum("请输入中心坐标Y：", textBoxHandY.Text, false);
        }

        private void textBoxHandZ_GotFocus(object sender, EventArgs e)
        {
            picSaveParam.Focus();
            textBoxHandZ.Text = KeyNum("请输入中心坐标Z：", textBoxHandZ.Text, false);
        }

        //panelServo中的文本框得焦事件
        private void textBoxLimitAP_GotFocus(object sender, EventArgs e)
        {
            picSaveParam.Focus();
            textBoxLimitAP.Text = KeyNum("请输入A轴正限位：", textBoxLimitAP.Text, false);
        }

        private void textBoxLimitAN_GotFocus(object sender, EventArgs e)
        {
            picSaveParam.Focus();
            textBoxLimitAN.Text = KeyNum("请输入A轴负限位：", textBoxLimitAN.Text, false);
        }

        private void textBoxLimitBP_GotFocus(object sender, EventArgs e)
        {
            picSaveParam.Focus();
            textBoxLimitBP.Text = KeyNum("请输入B轴正限位：", textBoxLimitBP.Text, false);
        }

        private void textBoxLimitBN_GotFocus(object sender, EventArgs e)
        {
            picSaveParam.Focus();
            textBoxLimitBN.Text = KeyNum("请输入B轴负限位：", textBoxLimitBN.Text, false);
        }

        private void textBoxLimitCP_GotFocus(object sender, EventArgs e)
        {
            picSaveParam.Focus();
            textBoxLimitCP.Text = KeyNum("请输入C轴正限位：", textBoxLimitCP.Text, false);
        }

        private void textBoxLimitCN_GotFocus(object sender, EventArgs e)
        {
            picSaveParam.Focus();
            textBoxLimitCN.Text = KeyNum("请输入C轴负限位：", textBoxLimitCN.Text, false);
        }

        private void textBoxLimitDP_GotFocus(object sender, EventArgs e)
        {
            picSaveParam.Focus();
            textBoxLimitDP.Text = KeyNum("请输入D轴正限位：", textBoxLimitDP.Text, false);
        }

        private void textBoxLimitDN_GotFocus(object sender, EventArgs e)
        {
            picSaveParam.Focus();
            textBoxLimitDN.Text = KeyNum("请输入D轴正负限位：", textBoxLimitDN.Text, false);
        }

        private void textBoxVmaxA_GotFocus(object sender, EventArgs e)
        {
            picSaveParam.Focus();
            textBoxVmaxA.Text = KeyNum("请输入A轴最大速度：", textBoxVmaxA.Text, false);
        }

        private void textBoxVmaxB_GotFocus(object sender, EventArgs e)
        {
            picSaveParam.Focus();
            textBoxVmaxB.Text = KeyNum("请输入B轴最大速度：", textBoxVmaxB.Text, false);
        }

        private void textBoxVmaxC_GotFocus(object sender, EventArgs e)
        {
            picSaveParam.Focus();
            textBoxVmaxC.Text = KeyNum("请输入C轴最大速度：", textBoxVmaxC.Text, false);
        }

        private void textBoxVmaxD_GotFocus(object sender, EventArgs e)
        {
            picSaveParam.Focus();
            textBoxVmaxD.Text = KeyNum("请输入D轴最大速度：", textBoxVmaxD.Text, false);
        }

        private void textBoxAmaxA_GotFocus(object sender, EventArgs e)
        {
            picSaveParam.Focus();
            textBoxAmaxA.Text = KeyNum("请输入A轴最大加速度：", textBoxAmaxA.Text, false);
        }

        private void textBoxAmaxB_GotFocus(object sender, EventArgs e)
        {
            picSaveParam.Focus();
            textBoxAmaxB.Text = KeyNum("请输入B轴最大加速度：", textBoxAmaxB.Text, false);
        }

        private void textBoxAmaxC_GotFocus(object sender, EventArgs e)
        {
            picSaveParam.Focus();
            textBoxAmaxC.Text = KeyNum("请输入C轴最大加速度：", textBoxAmaxC.Text, false);
        }

        private void textBoxAmaxD_GotFocus(object sender, EventArgs e)
        {
            picSaveParam.Focus();
            textBoxAmaxD.Text = KeyNum("请输入D轴最大加速度：", textBoxAmaxD.Text, false);
        }

        //panelNet中的文本框得焦事件
        private void textBoxIP_GotFocus(object sender, EventArgs e)
        {
            picSaveParam.Focus();
            textBoxIP.Text = KeyNum("请输入IP地址：", textBoxIP.Text, false);
        }

        private void textBoxPort_GotFocus(object sender, EventArgs e)
        {
            picSaveParam.Focus();
            textBoxPort.Text = KeyNum("请输入端口号：", textBoxPort.Text, false);
        }

        #endregion

        //示教运行各界面控件响应===============================

        //读取绝对编码器 ReadEncode
        private void btnreadall_Click(object sender, EventArgs e)
        {
            //读取绝对编码器
            double[] encData = new double[8];
        
            if (ReadEncode(ref encData) == 0)
            {
                short rtn = motion.RC_SetPos(encData);
                if (rtn == -1)
                {
                    MessageBox.Show(Var.errMsgofMotion[10], "严重错误");
                }
                else
                {
                    for (int i = 0; i < 8; i++)  //转变为弧度单位
                        encData[i] = encData[i] / (-MathFun.KJ[i]);
                    tbJ1.Text = encData[0].ToString();
                    tbJ2.Text = encData[1].ToString();
                    tbJ3.Text = encData[2].ToString();
                    tbJ4.Text = encData[3].ToString();
                    tbJ5.Text = encData[4].ToString();
                    tbJ6.Text = encData[5].ToString();
                    tbJ7.Text = encData[6].ToString();
                    tbJ8.Text = encData[7].ToString();
                }
            }
            else
            {
                MessageBox.Show("读取绝对编码器错误！", "警告！");
            }
        }
        
        //加载原来的零位信息  MathFun.HJ[]
        private void btnLoadHome_Click(object sender, EventArgs e)
        {
            tbJ1.Text = MathFun.HJ[0].ToString();
            tbJ2.Text = MathFun.HJ[1].ToString();
            tbJ3.Text = MathFun.HJ[2].ToString();
            tbJ4.Text = MathFun.HJ[3].ToString();
            tbJ5.Text = MathFun.HJ[4].ToString();
            tbJ6.Text = MathFun.HJ[5].ToString();
            tbJ7.Text = MathFun.HJ[6].ToString();
            tbJ8.Text = MathFun.HJ[7].ToString();
        }

        //把当前值设为原点并保存
        private void btnSetHome_Click(object sender, EventArgs e)
        {
            try
            {
                XmlElement xePRM = filePRM.DocumentElement;
                if (motion.robotNum == 1)
                {
                    MathFun.HJ[0] = double.Parse(tbJ1.Text) + Math.PI / 2;
                    MathFun.HJ[1] = double.Parse(tbJ2.Text);
                    MathFun.HJ[2] = double.Parse(tbJ3.Text);
                    MathFun.HJ[3] = double.Parse(tbJ4.Text);

                    xePRM.SelectSingleNode("/RobotPRM/Home/J1").InnerText = MathFun.HJ[0].ToString();
                    xePRM.SelectSingleNode("/RobotPRM/Home/J2").InnerText = MathFun.HJ[1].ToString();
                    xePRM.SelectSingleNode("/RobotPRM/Home/J3").InnerText = MathFun.HJ[2].ToString();
                    xePRM.SelectSingleNode("/RobotPRM/Home/J4").InnerText = MathFun.HJ[3].ToString();
                }
                else
                {
                    MathFun.HJ[4] = double.Parse(tbJ5.Text) + Math.PI / 2;
                    MathFun.HJ[5] = double.Parse(tbJ6.Text);
                    MathFun.HJ[6] = double.Parse(tbJ7.Text);
                    MathFun.HJ[7] = double.Parse(tbJ8.Text);

                    xePRM.SelectSingleNode("/RobotPRM/Home/J5").InnerText = MathFun.HJ[4].ToString();
                    xePRM.SelectSingleNode("/RobotPRM/Home/J6").InnerText = MathFun.HJ[5].ToString();
                    xePRM.SelectSingleNode("/RobotPRM/Home/J7").InnerText = MathFun.HJ[6].ToString();
                    xePRM.SelectSingleNode("/RobotPRM/Home/J8").InnerText = MathFun.HJ[7].ToString();
                }

                filePRM.Save(Var.FilePath + "Robot.PRM");
            }
            catch
            {
                MessageBox.Show("输入了非法参数！", "错误！");
            }
        }

        //panelManu 中的控件响应==================

        //手动按钮================================ motion.dirPos.X RC_ManuMov
        #region

        //操作键按下，开始运动
        private void picAN_MouseDown(object sender, MouseEventArgs e)
        {

            if (SYSPLC.TB_teach)
            {
                picAP.Enabled = false;
                picBN.Enabled = false;
                picBP.Enabled = false;
                picCN.Enabled = false;
                picCP.Enabled = false;
                picDN.Enabled = false;
                picDP.Enabled = false;

                if ((motion.coord == MathFun.COORDtyp.Joint)) //关节坐标
                {
                    picAN.Image = Properties.Resources.ANblue;
                }
                else//其它直角坐标
                {
                    picAN.Image = Properties.Resources.XNblue;
                }
                motion.dirPos.X = -1;
                motion.RC_ManuMov();
            }
        }

        //操作键释放，停止运动
        private void picAN_MouseUp(object sender, MouseEventArgs e)
        {
            if ((motion.coord == MathFun.COORDtyp.Joint)) //关节坐标
            {
                picAN.Image = Properties.Resources.AN;
            }
            else  //其它直角坐标
            {
                picAN.Image = Properties.Resources.XN;
            }

            picAP.Enabled = true;
            picBN.Enabled = true;
            picBP.Enabled = true;
            picCN.Enabled = true;
            picCP.Enabled = true;
            picDN.Enabled = true;
            picDP.Enabled = true;

            motion.dirPos = 0 * motion.dirPos;
            motion.RC_MovStop(false);
        }

        private void picAP_MouseDown(object sender, MouseEventArgs e)
        {
            if (SYSPLC.TB_teach)
            {
                picAN.Enabled = false;
                picBN.Enabled = false;
                picBP.Enabled = false;
                picCN.Enabled = false;
                picCP.Enabled = false;
                picDN.Enabled = false;
                picDP.Enabled = false;

                if ((motion.coord == MathFun.COORDtyp.Joint)) //关节坐标
                {
                    picAP.Image = Properties.Resources.APblue;
                }
                else  //其它直角坐标
                {
                    picAP.Image = Properties.Resources.XPblue;
                }
                motion.dirPos.X = 1;
                motion.RC_ManuMov();
            }
        }

        private void picAP_MouseUp(object sender, MouseEventArgs e)
        {
            if ((motion.coord == MathFun.COORDtyp.Joint)) //关节坐标
            {
                picAP.Image = Properties.Resources.AP;
            }
            else  //其它直角坐标
            {
                picAP.Image = Properties.Resources.XP;
            }

            picAN.Enabled = true;
            picBN.Enabled = true;
            picBP.Enabled = true;
            picCN.Enabled = true;
            picCP.Enabled = true;
            picDN.Enabled = true;
            picDP.Enabled = true;

            motion.dirPos = 0 * motion.dirPos;
            motion.RC_MovStop(false);
        }

        private void picBN_MouseDown(object sender, MouseEventArgs e)
        {

            if (SYSPLC.TB_teach)
            {
                picAN.Enabled = false;
                picAP.Enabled = false;
                picBP.Enabled = false;
                picCN.Enabled = false;
                picCP.Enabled = false;
                picDN.Enabled = false;
                picDP.Enabled = false;

                if (motion.coord == MathFun.COORDtyp.Joint)  //关节坐标
                {
                    picBN.Image = Properties.Resources.BNblue;
                }
                else  //其它直角坐标
                {
                    picBN.Image = Properties.Resources.ZNblue;
                }
                motion.dirPos.Y = -1;
                motion.RC_ManuMov();
            }
        }

        private void picBN_MouseUp(object sender, MouseEventArgs e)
        {
            if (motion.coord == MathFun.COORDtyp.Joint)  //关节坐标
            {
                picBN.Image = Properties.Resources.BN;
            }
            else  //其它直角坐标
            {
                picBN.Image = Properties.Resources.ZN;
            }

            picAN.Enabled = true;
            picAP.Enabled = true;
            picBP.Enabled = true;
            picCN.Enabled = true;
            picCP.Enabled = true;
            picDN.Enabled = true;
            picDP.Enabled = true;

            motion.dirPos = 0 * motion.dirPos;
            motion.RC_MovStop(false);
        }

        private void picBP_MouseDown(object sender, MouseEventArgs e)
        {

            if (SYSPLC.TB_teach)
            {
                picAN.Enabled = false;
                picAP.Enabled = false;
                picBN.Enabled = false;
                picCN.Enabled = false;
                picCP.Enabled = false;
                picDN.Enabled = false;
                picDP.Enabled = false;

                if (motion.coord == MathFun.COORDtyp.Joint)  //关节坐标
                {
                    picBP.Image = Properties.Resources.BPblue;
                }
                else  //其它直角坐标
                {
                    picBP.Image = Properties.Resources.ZPblue;
                }
                motion.dirPos.Y = 1;
                motion.RC_ManuMov();
            }
        }

        private void picBP_MouseUp(object sender, MouseEventArgs e)
        {
            if (motion.coord == MathFun.COORDtyp.Joint)  //关节坐标
            {
                picBP.Image = Properties.Resources.BP;
            }
            else  //其它直角坐标
            {
                picBP.Image = Properties.Resources.ZP;
            }

            picAN.Enabled = true;
            picAP.Enabled = true;
            picBN.Enabled = true;
            picCN.Enabled = true;
            picCP.Enabled = true;
            picDN.Enabled = true;
            picDP.Enabled = true;

            motion.dirPos = 0 * motion.dirPos;
            motion.RC_MovStop(false);
        }

        private void picCN_MouseDown(object sender, MouseEventArgs e)
        {

            if (SYSPLC.TB_teach)
            {
                picAN.Enabled = false;
                picAP.Enabled = false;
                picBN.Enabled = false;
                picBP.Enabled = false;
                picCP.Enabled = false;
                picDN.Enabled = false;
                picDP.Enabled = false;

                if (motion.coord == MathFun.COORDtyp.Joint)  //关节坐标
                {
                    picCN.Image = Properties.Resources.CNblue;
                }
                else  //其它直角坐标
                {
                    picCN.Image = Properties.Resources.RNblue;
                }
                motion.dirPos.Z = -1;
                motion.RC_ManuMov();
            }
        }

        private void picCN_MouseUp(object sender, MouseEventArgs e)
        {
            if (motion.coord == MathFun.COORDtyp.Joint)  //关节坐标
            {
                picCN.Image = Properties.Resources.CN;
            }
            else  //其它直角坐标
            {
                picCN.Image = Properties.Resources.RN;
            }

            picAN.Enabled = true;
            picAP.Enabled = true;
            picBN.Enabled = true;
            picBP.Enabled = true;
            picCP.Enabled = true;
            picDN.Enabled = true;
            picDP.Enabled = true;

            motion.dirPos = 0 * motion.dirPos;
            motion.RC_MovStop(false);
        }

        private void picCP_MouseDown(object sender, MouseEventArgs e)
        {

            if (SYSPLC.TB_teach)
            {
                picAN.Enabled = false;
                picAP.Enabled = false;
                picBN.Enabled = false;
                picBP.Enabled = false;
                picCN.Enabled = false;
                picDN.Enabled = false;
                picDP.Enabled = false;

                if (motion.coord == MathFun.COORDtyp.Joint)  //关节坐标
                {
                    picCP.Image = Properties.Resources.CPblue;
                }
                else  //其它直角坐标
                {
                    picCP.Image = Properties.Resources.RPblue;
                }
                motion.dirPos.Z = 1;
                motion.RC_ManuMov();
            }
        }

        private void picCP_MouseUp(object sender, MouseEventArgs e)
        {
            if (motion.coord == MathFun.COORDtyp.Joint)  //关节坐标
            {
                picCP.Image = Properties.Resources.CP;
            }
            else  //其它直角坐标
            {
                picCP.Image = Properties.Resources.RP;
            }

            picAN.Enabled = true;
            picAP.Enabled = true;
            picBN.Enabled = true;
            picBP.Enabled = true;
            picCN.Enabled = true;
            picDN.Enabled = true;
            picDP.Enabled = true;

            motion.dirPos = 0 * motion.dirPos;
            motion.RC_MovStop(false);
        }

        private void picDN_MouseDown(object sender, MouseEventArgs e)
        {

            if (SYSPLC.TB_teach)
            {
                picAN.Enabled = false;
                picAP.Enabled = false;
                picBN.Enabled = false;
                picBP.Enabled = false;
                picCN.Enabled = false;
                picCP.Enabled = false;
                picDP.Enabled = false;
                if (motion.coord == MathFun.COORDtyp.Joint)  //关节坐标
                {
                    picDN.Image = Properties.Resources.DNblue;
                }
                else  //其它直角坐标
                {
                    picDN.Image = Properties.Resources.RzNblue;
                }
                motion.dirPos.THETA = -1;
                motion.RC_ManuMov();
            }
        }

        private void picDN_MouseUp(object sender, MouseEventArgs e)
        {

            picAN.Enabled = true;
            picAP.Enabled = true;
            picBN.Enabled = true;
            picBP.Enabled = true;
            picCN.Enabled = true;
            picCP.Enabled = true;
            picDP.Enabled = true;
            if (motion.coord == MathFun.COORDtyp.Joint)  //关节坐标
            {
                picDN.Image = Properties.Resources.DN;
            }
            else  //其它直角坐标
            {
                picDN.Image = Properties.Resources.RzN;
            }

            motion.dirPos = 0 * motion.dirPos;
            motion.RC_MovStop(false);
        }

        private void picDP_MouseDown(object sender, MouseEventArgs e)
        {

            if (SYSPLC.TB_teach)
            {
                picAN.Enabled = false;
                picAP.Enabled = false;
                picBN.Enabled = false;
                picBP.Enabled = false;
                picCN.Enabled = false;
                picCP.Enabled = false;
                picDN.Enabled = false;
                if (motion.coord == MathFun.COORDtyp.Joint)  //关节坐标
                {
                    picDP.Image = Properties.Resources.DPblue;
                }
                else  //其它直角坐标
                {
                    picDP.Image = Properties.Resources.RzPblue;
                }
                motion.dirPos.THETA = 1;
                motion.RC_ManuMov();
            }
        }

        private void picDP_MouseUp(object sender, MouseEventArgs e)
        {

            picAN.Enabled = true;
            picAP.Enabled = true;
            picBN.Enabled = true;
            picBP.Enabled = true;
            picCN.Enabled = true;
            picCP.Enabled = true;
            picDN.Enabled = true;
            if (motion.coord == MathFun.COORDtyp.Joint)  //关节坐标
            {
                picDP.Image = Properties.Resources.DP;
            }
            else  //其它直角坐标
            {
                picDP.Image = Properties.Resources.RzP;
            }

            motion.dirPos = 0 * motion.dirPos;
            motion.RC_MovStop(false);
        }

        #endregion

        //panelHand中其他按钮事件=================
        #region
        //伺服状态切换 三段使能 RC_ServoOn
        private void buttonServo_Click(object sender, EventArgs e)
        {
            if ((SYSPLC.TB_teach && SYSPLC.TB_deadman) || SYSPLC.TB_play || SYSPLC.TB_remote)
            {
                if (buttonServo.Text == "伺服使能")
                {
                    //if (motion.RC_ServoOn(0xff) != 0)
                    if (motion.RC_ServoOn(0x01) != 0)
                        {
                        Motion.RC_ServoOff(0xff);  //如果部分轴使能失败，强制全部失能
                        labelSysInfo.Text = "提示：伺服使能失败";
                    }
                    else
                    {
                        SYSPLC.TB_servoonready = true;
                        buttonServo.Text = "伺服失能";
                        buttonServo.BackColor = System.Drawing.Color.Red;
                        picServo.Image = Properties.Resources.SERVOON;
                        labelSysInfo.Text = "提示：伺服使能";
                    }
                }
                else
                {
                    if (Motion.RC_ServoOff(0xff) != 0)
                    {
                        labelSysInfo.Text = "提示：伺服失能失败";
                    }
                    else
                    {
                        SYSPLC.TB_servoonready = false;
                        buttonServo.Text = "伺服使能";
                        buttonServo.BackColor = System.Drawing.Color.Green;
                        picServo.Image = Properties.Resources.SERVOOFF;
                        labelSysInfo.Text = "提示：伺服失能";
                    }
                }
                if (labelSysInfo.Text == "提示：三段使能开关未打开")
                    labelSysInfo.Text = "系统提示信息";
            }
            else
            {
                labelSysInfo.Text = "提示：三段使能开关未打开";
                return;
            }

        }

        //重置下位机（仅用于调试）
        private void buttonreset_Click(object sender, EventArgs e)
        {
            //buttonreset.BackColor = Color.Gray;
            //run = false;
            //motion.start1 = false;
            //motion.RC_MovStop(false);
            //////如果线程存在，则先关闭，新建立并运行
            ////if (motion.threadRun != null)
            ////{
            ////    motion.threadRun.Abort();
            ////    motion.threadRun = null;
            ////}

            ////重新读取绝对编码器
            //double[] encData = new double[8];
            //if (ReadEncode(ref encData) == 0)
            //{
            //    motion.RC_SetPos(encData);
            //    labelSysInfo.Text = "提示：重置成功";
            //}
            //else
            //    labelSysInfo.Text = "提示：读取绝对编码器失败，请重试";
            //motion.RC_Clear();//规划实际位置清零
            //buttonreset.BackColor = Color.Yellow;
        }

        //手爪状态切换
        private void buttonHand_Click(object sender, EventArgs e)
        {
            if (buttonHand.Text == "手爪吸合")
            {

                buttonHand.Text = "手爪释放";
                buttonHand.BackColor = System.Drawing.Color.Red;
            }
            else
            {

                buttonHand.Text = "手爪吸合";
                buttonHand.BackColor = System.Drawing.Color.Green;
            }
        }

        //更改工作文件示教点索引
        private void btnCurCPos_Click(object sender, EventArgs e)
        {
            if (curJob.JobName == null) return;  //没有打开的文件，返回
            if (curJob.NumPos <= 0) return;  //没有示教点的文件，返回

            MathFun.JPOStyp jPos = new MathFun.JPOStyp();
            jPos = (MathFun.JPOStyp)curJob.poses[curJob.IndexPos];
            textBoxA.Text = (jPos.J1 * KAngle).ToString("F3") + "deg";
            textBoxB.Text = (jPos.J2 * KAngle).ToString("F3") + "deg";
            textBoxC.Text = (jPos.J3 * KAngle).ToString("F3") + "deg";
            textBoxD.Text = (jPos.J4 * KAngle).ToString("F3") + "deg";
            btnCurCPos.Text = "C" + curJob.IndexPos.ToString("D4");
        }

        //上一示教点
        private void btnPrev_Click(object sender, EventArgs e)
        {
            if (curJob.JobName == null) return;  //没有打开的文件，返回
            if (curJob.IndexPos > 0) curJob.IndexPos--;
            btnCurCPos.Text = "C" + curJob.IndexPos.ToString("D4");
            btnCurCPos_Click(null, null);
        }

        //下一示教点
        private void btnNext_Click(object sender, EventArgs e)
        {
            if (curJob.JobName == null) return;  //没有打开的文件，返回
            if (curJob.IndexPos < (curJob.NumPos - 1)) curJob.IndexPos++;
            btnCurCPos.Text = "C" + curJob.IndexPos.ToString("D4");
            btnCurCPos_Click(null, null);
        }

        //插入一个示教点在当前点之后
        private void buttonIns_Click(object sender, EventArgs e)
        {
            motion.teachJPos1 = motion.curJPos1;
            curJob.insPos(curJob.IndexPos + 1, motion.teachJPos1);
            curJob.IndexPos++;
            btnCurCPos.Text = "C" + curJob.IndexPos.ToString("D4");
        }

        //删除一个示教点
        private void buttonDel_Click(object sender, EventArgs e)
        {
            if (curJob.IndexPos < 0)
                return;
            curJob.delPos(curJob.IndexPos);
            curJob.IndexPos--;
            if (curJob.IndexPos >= 0)
                btnCurCPos.Text = "C" + curJob.IndexPos.ToString("D4");
            else
                btnCurCPos.Text = "C----";
        }

        //清除驱动器报警 RC_ClrSts
        private void btnClr_Click(object sender, EventArgs e)
        {
            motion.RC_ClrSts(1, 8);
        }

        //恢复出厂设置（并没有保存在磁盘上）
        private void btnSetDefault_Click(object sender, EventArgs e)
        {
            if (motion.robotNum == 1)
            {
                motion.handTimeOpen1 = 0.5;
                motion.handTimeClose1 = 0.5;
                motion.handI1 = 5;
                motion.handO1 = 5;
                motion.tool1.X = 0;
                motion.tool1.Z = 0;

                motion.moveLimitN[0] = -2;
                motion.moveLimitP[0] = 2;
                motion.moveLimitN[1] = -2;
                motion.moveLimitP[1] = 2;
                motion.moveLimitN[2] = -2;
                motion.moveLimitP[2] = 2;
                motion.moveLimitN[3] = -4;
                motion.moveLimitP[3] = 4;

                motion.acc[0] = 1;
                motion.acc[1] = 1;
                motion.acc[2] = 1;
                motion.acc[3] = 1;
                motion.vmax[0] = 1;
                motion.vmax[1] = 1;
                motion.vmax[2] = 1;
                motion.vmax[3] = 1;
            }
            else
            {
                motion.handTimeOpen2 = 0.5;
                motion.handTimeClose2 = 0.5;
                motion.handI2 = 6;
                motion.handO2 = 6;
                motion.tool2.X = 0;
                motion.tool2.Z = 0;

                motion.moveLimitN[4] = -2;
                motion.moveLimitP[4] = 2;
                motion.moveLimitN[5] = -2;
                motion.moveLimitP[5] = 2;
                motion.moveLimitN[6] = -2;
                motion.moveLimitP[6] = 2;
                motion.moveLimitN[7] = -4;
                motion.moveLimitP[7] = 4;

                motion.acc[4] = 1;
                motion.acc[5] = 1;
                motion.acc[6] = 1;
                motion.acc[7] = 1;
                motion.vmax[4] = 1;
                motion.vmax[5] = 1;
                motion.vmax[6] = 1;
                motion.vmax[7] = 1;
            }

            NetIP = "192.168.0.2";
            NetPort = "6801";

            //更新显示，注意单位变换
            if (motion.robotNum == 1)
            {
                textBoxCatchTime.Text = (motion.handTimeClose1 * 1000).ToString();
                textBoxReleaseTime.Text = (motion.handTimeOpen1 * 1000).ToString();
                textBoxHandIN1.Text = motion.handI1.ToString();
                textBoxHandOUT1.Text = motion.handO1.ToString();
                textBoxHandX.Text = (motion.tool1.X * 1000).ToString();
                textBoxHandZ.Text = (motion.tool1.Z * 1000).ToString();

                textBoxLimitAP.Text = (motion.moveLimitP[0] * KAngle).ToString();
                textBoxLimitAN.Text = (motion.moveLimitN[0] * KAngle).ToString();
                textBoxLimitBP.Text = (motion.moveLimitP[1] * KAngle).ToString();
                textBoxLimitBN.Text = (motion.moveLimitN[1] * KAngle).ToString();
                textBoxLimitCP.Text = (motion.moveLimitP[2] * KAngle).ToString();
                textBoxLimitCN.Text = (motion.moveLimitN[2] * KAngle).ToString();
                textBoxLimitDP.Text = (motion.moveLimitP[3] * KAngle).ToString();
                textBoxLimitDN.Text = (motion.moveLimitN[3] * KAngle).ToString();
                textBoxVmaxA.Text = (motion.vmax[0] * KAngle).ToString();
                textBoxVmaxB.Text = (motion.vmax[1] * KAngle).ToString();
                textBoxVmaxC.Text = (motion.vmax[2] * KAngle).ToString();
                textBoxVmaxD.Text = (motion.vmax[3] * KAngle).ToString();
                textBoxAmaxA.Text = (motion.acc[0] * KAngle).ToString();
                textBoxAmaxB.Text = (motion.acc[1] * KAngle).ToString();
                textBoxAmaxC.Text = (motion.acc[2] * KAngle).ToString();
                textBoxAmaxD.Text = (motion.acc[3] * KAngle).ToString();
            }
            else
            {
                textBoxCatchTime.Text = (motion.handTimeClose2 * 1000).ToString();
                textBoxReleaseTime.Text = (motion.handTimeOpen2 * 1000).ToString();
                textBoxHandIN1.Text = motion.handI2.ToString();
                textBoxHandOUT1.Text = motion.handO2.ToString();
                textBoxHandX.Text = (motion.tool2.X * 1000).ToString();
                textBoxHandZ.Text = (motion.tool2.Z * 1000).ToString();

                textBoxLimitAP.Text = (motion.moveLimitP[4] * KAngle).ToString();
                textBoxLimitAN.Text = (motion.moveLimitN[4] * KAngle).ToString();
                textBoxLimitBP.Text = (motion.moveLimitP[5] * KAngle).ToString();
                textBoxLimitBN.Text = (motion.moveLimitN[5] * KAngle).ToString();
                textBoxLimitCP.Text = (motion.moveLimitP[6] * KAngle).ToString();
                textBoxLimitCN.Text = (motion.moveLimitN[6] * KAngle).ToString();
                textBoxLimitDP.Text = (motion.moveLimitP[7] * KAngle).ToString();
                textBoxLimitDN.Text = (motion.moveLimitN[7] * KAngle).ToString();
                textBoxVmaxA.Text = (motion.vmax[4] * KAngle).ToString();
                textBoxVmaxB.Text = (motion.vmax[5] * KAngle).ToString();
                textBoxVmaxC.Text = (motion.vmax[6] * KAngle).ToString();
                textBoxVmaxD.Text = (motion.vmax[7] * KAngle).ToString();
                textBoxAmaxA.Text = (motion.acc[4] * KAngle).ToString();
                textBoxAmaxB.Text = (motion.acc[5] * KAngle).ToString();
                textBoxAmaxC.Text = (motion.acc[6] * KAngle).ToString();
                textBoxAmaxD.Text = (motion.acc[7] * KAngle).ToString();
            }
            textBoxIP.Text = NetIP;
            textBoxPort.Text = NetPort;

            motion.RC_SetLimit(false);
        }

        //清除软限位 motion.RC_SetLimit  TB_releasestrict
        private void btnClrSoftLim_Click(object sender, EventArgs e)
        {
            if (!SYSPLC.TB_releasestrict)  //原本有限，变无限
            {
                if (motion.RC_SetLimit(true) == 0)
                    SYSPLC.TB_releasestrict = true;
            }
            else  //原本无限，变有限
            {
                if (motion.RC_SetLimit(false) == 0)
                    SYSPLC.TB_releasestrict = false;
            }
        }

        //记录示教点
        private void buttonAffirm_Click(object sender, EventArgs e)
        {
            if (curJob.IndexPos < 0)
                return;

            MathFun.JPOStyp curJPos = new MathFun.JPOStyp();
            if (motion.robotNum == 1)
                curJPos = motion.curJPos1;
            else if (motion.robotNum == 2)
                curJPos = motion.curJPos2; 
            if ((curJob.IndexPos < curJob.NumPos) && (curJob.IndexPos >= 0))
            {
                curJob.poses[curJob.IndexPos] = curJPos;  //更新数据数组
                curJob.delPos(curJob.IndexPos);  //更新文件记录（先删后加）
                curJob.insPos(curJob.IndexPos, curJPos);
            }
        }

        //前往已有的示教点 
        private void buttonForward_Click(object sender, EventArgs e)
        {
            if (buttonServo.Text == "伺服失能")
            {
                if (buttonForward.Text == "前往")
                {
                    MathFun.JPOStyp secJPos = new MathFun.JPOStyp();
                    secJPos = (MathFun.JPOStyp)curJob.poses[curJob.IndexPos];
                    MathFun.JPOStyp Vo = new MathFun.JPOStyp();
                    Vo.J1 = 0; Vo.J2 = 0; Vo.J3 = 0; Vo.J4 = 0;
                    if (motion.robotNum == 1)
                    {
                        MathFun.MF_MOVJ(motion.curJPos1, secJPos, Vo, motion.speedJ, 0);
                        motion.movType1 = Motion.MovType.PVT;
                        motion.moving1 = true;
                    }
                    else if (motion.robotNum == 2)
                    {
                        MathFun.MF_MOVJ(motion.curJPos2, secJPos, Vo, motion.speedJ, 0);
                        motion.movType2 = Motion.MovType.PVT;
                        motion.moving2 = true;
                    }
                    buttonForward.Text = "停止";
                }
                else
                {
                    buttonForward.Text = "前往";
                    motion.RC_MovStop(false);  //平滑停止
                }
                if (labelSysInfo.Text == "提示：请先上伺服再运动")
                    labelSysInfo.Text = "系统提示信息";
            }
            else
            {
                labelSysInfo.Text = "提示：请先上伺服再运动";
            }
        }

        #endregion

        //运行界面事件============================
        #region

        //启用左侧抓取线程
        private void btnUse1_Click(object sender, EventArgs e)
        {
            if (btnUseD.Text == "停用")  //如果双机协同模式，禁止启动
            {
                labelSysInfo.Text = "当前处于协调运行模式，禁止单线程启动。";
                return;
            }

            if (btnUse1.Text == "停用")  //如果原来运行，则停止
            {
                motion.run1 = false;
                if (motion.threadLine1 != null)  //如果线程存在，则杀掉
                {
                    motion.threadLine1.Abort();
                    motion.threadLine1 = null;
                }
                textLStep.Enabled = true;
                textLSpeed.Enabled = true;
                btnUse1.Text = "启用";
                btnUse1.BackColor = Color.Green;
            }
            else//如果原来停止，则启用
            {
                //if (!SYSPLC.ServoR)
                //{
                //    MsgBox("错误！", "当前伺服电源未接通。");
                //    return;
                //}

                if (motion.threadLine1 != null)  //如果线程存在，则杀掉，重新建立并运行
                {
                    motion.threadLine1.Abort();
                    motion.threadLine1 = null;
                }
                motion.threadLine1 = new Thread(new ThreadStart(Line1));
                motion.threadLine1.IsBackground = true;
                motion.threadLine1.Start();

                textLStep.Enabled = false;
                textLSpeed.Enabled = false;
                btnUse1.Text = "停用";
                btnUse1.BackColor = Color.DarkOrange;
                motion.run1 = true;
                motion.start1 = true;
            }
        }

        #endregion

        //周边系统面板各界面控件响应===========================
        #region
        
        //保存IO端口的注释
        private void btnsave_Click(object sender, EventArgs e)
        {
            Sv_Notation();//保存传感器注释
        }
        
        //切换是否刷新IO显示
        private void btnlock_Click(object sender, EventArgs e)
        {
            if (panelUserIO.Visible)
            {
                if (btnlock.Text == "锁定")
                {
                    IOrefreshLock = true;
                    btnlock.BackColor = System.Drawing.Color.Red;
                    btnlock.Text = "取消锁定";
                }
                else
                {
                    IOrefreshLock = false;
                    btnlock.BackColor = SystemColors.Control;
                    btnlock.Text = "锁定";
                }
            }
            if (panelSysIO.Visible)
            {
                if (btnlock1.Text == "锁定")
                {
                    IOrefreshLock = true;
                    btnlock1.BackColor = System.Drawing.Color.Red;
                    btnlock1.Text = "取消锁定";
                }
                else
                {
                    IOrefreshLock = false;
                    btnlock1.BackColor = SystemColors.Control;
                    btnlock1.Text = "锁定";
                }
            }
        }

        //强制输出
        private void btnenforce_Click(object sender, EventArgs e)
        {
            if (panelUserIO.Visible)
            {
                if (checkBoxO01.Checked) SYSPLC.DOW = SYSPLC.DOW | 0x0001;
                else SYSPLC.DOW = SYSPLC.DOW & 0xfffe;
                if (checkBoxO02.Checked) SYSPLC.DOW = SYSPLC.DOW | 0x0002;
                else SYSPLC.DOW = SYSPLC.DOW & 0xfffd;
                if (checkBoxO03.Checked) SYSPLC.DOW = SYSPLC.DOW | 0x0004;
                else SYSPLC.DOW = SYSPLC.DOW & 0xfffb;
                if (checkBoxO04.Checked) SYSPLC.DOW = SYSPLC.DOW | 0x0008;
                else SYSPLC.DOW = SYSPLC.DOW & 0xfff7;
                if (checkBoxO05.Checked) SYSPLC.DOW = SYSPLC.DOW | 0x0010;
                else SYSPLC.DOW = SYSPLC.DOW & 0xffef;
                if (checkBoxO06.Checked) SYSPLC.DOW = SYSPLC.DOW | 0x0020;
                else SYSPLC.DOW = SYSPLC.DOW & 0xffdf;
                if (checkBoxO07.Checked) SYSPLC.DOW = SYSPLC.DOW | 0x0040;
                else SYSPLC.DOW = SYSPLC.DOW & 0xffbf;
                if (checkBoxO08.Checked) SYSPLC.DOW = SYSPLC.DOW | 0x0080;
                else SYSPLC.DOW = SYSPLC.DOW & 0xff7f;
                if (checkBoxO09.Checked) SYSPLC.DOW = SYSPLC.DOW | 0x0100;
                else SYSPLC.DOW = SYSPLC.DOW & 0xfeff;
                if (checkBoxO10.Checked) SYSPLC.DOW = SYSPLC.DOW | 0x0200;
                else SYSPLC.DOW = SYSPLC.DOW & 0xfdff;
                if (checkBoxO11.Checked) SYSPLC.DOW = SYSPLC.DOW | 0x0400;
                else SYSPLC.DOW = SYSPLC.DOW & 0xfbff;
                if (checkBoxO12.Checked) SYSPLC.DOW = SYSPLC.DOW | 0x0800;
                else SYSPLC.DOW = SYSPLC.DOW & 0xf7ff;
                if (checkBoxO13.Checked) SYSPLC.DOW = SYSPLC.DOW | 0x1000;
                else SYSPLC.DOW = SYSPLC.DOW & 0xefff;
                if (checkBoxO14.Checked) SYSPLC.DOW = SYSPLC.DOW | 0x2000;
                else SYSPLC.DOW = SYSPLC.DOW & 0xdfff;
                if (checkBoxO15.Checked) SYSPLC.DOW = SYSPLC.DOW | 0x4000;
                else SYSPLC.DOW = SYSPLC.DOW & 0xbfff;
                if (checkBoxO16.Checked) SYSPLC.DOW = SYSPLC.DOW | 0x8000;
                else SYSPLC.DOW = SYSPLC.DOW & 0x7fff;
                if (btnlock.Text == "取消锁定")  //强制完成后自动取消锁定以便观察效果
                {
                    IOrefreshLock = false;
                    btnlock.BackColor = SystemColors.Control;
                    btnlock.Text = "锁定";
                }
            }

            if (panelSysIO.Visible)
            {
                if (checkBoxC1.Checked) SYSPLC.Cls0 = true;
                else SYSPLC.Cls0 = false;
                if (checkBoxC2.Checked) SYSPLC.Cls1 = true;
                else SYSPLC.Cls1 = false;
                if (checkBoxC3.Checked) SYSPLC.Cls2 = true;
                else SYSPLC.Cls2 = false;
                if (checkBoxC4.Checked) SYSPLC.Cls3 = true;
                else SYSPLC.Cls3 = false;

                //if (checkBoxS1.Checked) motion.RC_ServoOn(1);//Gts.GT_AxisOn(1);
                //else Motion.RC_ServoOff(1);//Gts.GT_AxisOff(1);
                //if (checkBoxS2.Checked) motion.RC_ServoOn(2);//Gts.GT_AxisOn(2);
                //else Motion.RC_ServoOff(2);//Gts.GT_AxisOff(2);
                //if (checkBoxS3.Checked) motion.RC_ServoOn(3);//Gts.GT_AxisOn(3);
                //else Motion.RC_ServoOff(3);//Gts.GT_AxisOff(3);
                //if (checkBoxS4.Checked) motion.RC_ServoOn(4);//Gts.GT_AxisOn(4);
                //else Motion.RC_ServoOff(4);//Gts.GT_AxisOff(4);

                if (btnlock1.Text == "取消锁定")
                {
                    IOrefreshLock = false;
                    btnlock1.BackColor = SystemColors.Control;
                    btnlock1.Text = "锁定";
                }
            }

        }

        #endregion

        //系统信息面板中各界面控件响应=========================
        #region
        //panelCountInfo中按钮事件
        private void btnClear_Click(object sender, EventArgs e)
        {
            CTtemp = 0;
            CT.Text = CTtemp.ToString();  //显示刷新
        }

        //panelBackup中控件事件
        private void checkBoxAllFile_CheckStateChanged(object sender, EventArgs e)
        {
            if (checkBoxAllFile.Checked)
            {
                checkBoxJob.Checked = true;
                checkBoxParam.Checked = true;
                checkBoxErrList.Checked = true;
            }
        }
        private void checkBoxJob_CheckStateChanged(object sender, EventArgs e)
        {
            if (!checkBoxJob.Checked)
                checkBoxAllFile.Checked = false;
            if (checkBoxJob.Checked && checkBoxParam.Checked && checkBoxErrList.Checked)
                checkBoxAllFile.Checked = true;
        }

        private void checkBoxParam_CheckStateChanged(object sender, EventArgs e)
        {
            if (!checkBoxParam.Checked)
                checkBoxAllFile.Checked = false;
            if (checkBoxJob.Checked && checkBoxParam.Checked && checkBoxErrList.Checked)
                checkBoxAllFile.Checked = true;
        }

        private void checkBoxErrList_CheckStateChanged(object sender, EventArgs e)
        {
            if (!checkBoxErrList.Checked)
                checkBoxAllFile.Checked = false;
            if (checkBoxJob.Checked && checkBoxParam.Checked && checkBoxErrList.Checked)
                checkBoxAllFile.Checked = true;
        }
        private void btnStartSave_Click(object sender, EventArgs e)
        {
            string UPath = @"\USBDISK\";
            if (rbBackUp.Checked)
            {
                if (checkBoxJob.Checked)//备份工作文件
                {
                    string path1, path2;

                    try
                    {
                        path1 = Var.JobPath;
                        path2 = UPath;
                        string[] files = Directory.GetFiles(@path1);
                        foreach (string file in files)
                        {
                            FileInfo fi = new FileInfo(@file);
                            if (fi.Extension == ".JBI")
                            {
                                File.Copy(@file, path2 + Path.GetFileName(@file), true);
                            }
                        }
                    }
                    catch
                    {
                        MessageBox.Show("请检测源工作文件是否存在", "错误！");
                        return;
                    }
                    MessageBox.Show("工作文件保存成功！");
                }

                if (checkBoxParam.Checked)
                {
                    string path1, path2;

                    try
                    {
                        path1 = Var.FilePath + "Robot.PRM";
                        path2 = UPath + "Robot.PRM";
                        File.Copy(path1, path2, true);

                    }
                    catch
                    {
                        MessageBox.Show("请检测源系统参数文件是否存在", "错误！");
                        return;
                    }
                    MessageBox.Show("系统参数文件保存成功！");
                }

                if (checkBoxErrList.Checked)
                {
                    string path1, path2;

                    try
                    {
                        path1 = Var.FilePath + "Error.RCD";
                        path2 = UPath + "Error.RCD";
                        File.Copy(path1, path2, true);

                    }
                    catch
                    {
                        MessageBox.Show("请检测源故障信息文件是否存在", "错误！");
                        return;
                    }
                    MessageBox.Show("故障信息文件保存成功！");
                }
            }
            else
            {
                if (rbRestore.Checked)//恢复文件
                {
                    if (checkBoxJob.Checked)
                    {
                        string path1, path2;

                        try
                        {
                            path1 = Var.JobPath;
                            path2 = UPath;
                            string[] files = Directory.GetFiles(@path2);
                            foreach (string file in files)
                            {
                                FileInfo fi = new FileInfo(@file);
                                if (fi.Extension == ".JBI")
                                {
                                    File.Copy(@file, path1 + Path.GetFileName(@file), true);
                                }
                            }
                        }
                        catch
                        {
                            MessageBox.Show("请检测源工作文件是否存在", "错误！");
                            return;
                        }

                        MessageBox.Show("工作文件读取成功！");
                    }

                    if (checkBoxParam.Checked)
                    {
                        string path1, path2;

                        try
                        {
                            path1 = Var.FilePath + "Robot.PRM";
                            path2 = UPath + "Robot.PRM";
                            File.Copy(path2, path1, true);

                        }
                        catch
                        {
                            MessageBox.Show("请检测源系统参数文件是否存在", "错误！");
                            return;
                        }

                        MessageBox.Show("系统参数文件读取成功！");
                    }

                    if (checkBoxErrList.Checked)
                    {
                        string path1, path2;

                        try
                        {
                            path1 = Var.FilePath + "Error.RCD";
                            path2 = UPath + "Error.RCD";
                            File.Copy(path2, path1, true);

                        }
                        catch
                        {
                            MessageBox.Show("请检测源故障信息文件是否存在", "错误！");
                            return;
                        }

                        MessageBox.Show("故障信息文件读取成功！");
                    }
                }
            }
        }
        //文件保存和恢复路径设置
        private void listBox_file_SelectedIndexChanged(object sender, EventArgs e)
        {
            //try
            //{
            //    tbUpath.Text = listBox_file.SelectedItem.ToString() + "\\";
            //    DirectoryInfo dirInfo = new DirectoryInfo(listBox_file.SelectedItem.ToString());
            //    DirectoryInfo[] dirInfos = dirInfo.GetDirectories();
            //    listBox_file.Items.Clear();
            //    foreach (DirectoryInfo dir in dirInfos)
            //    {
            //        if (dir != null)
            //        {
            //            listBox_file.Items.Add(dir.FullName);
            //        }
            //    }
            //}
            //catch
            //{
            //    tbUpath.Text = "\\";
            //}
        }
        //文件保存和恢复路径
        private void tbUpath_GotFocus(object sender, EventArgs e)
        {
            //if (!listBox_file.Visible)
            //    listBox_file.Visible = true;
            //else
            //    listBox_file.Visible = false;

            //DirectoryInfo dirInfo = new DirectoryInfo("\\");
            //DirectoryInfo[] dirInfos = dirInfo.GetDirectories();
            //listBox_file.Items.Clear();
            //foreach (DirectoryInfo dir in dirInfos)
            //{
            //    if (dir != null)
            //    {
            //        listBox_file.Items.Add(dir.FullName);
            //    }
            //}
        }
        #endregion

        //低精度Timer事件（1s）  DateTime.Now.ToLongDateString() + DateTime.Now.ToLongTimeString()
        private void timer1S_Tick(object sender, EventArgs e)
        {
            ontime++;  //时间加1秒
            if (motion.moving1 || motion.moving2 || motion.movingD) runtime++;  //运行时间加1秒

            //刷新统计信息
            if (panelCountInfo.Visible)
            {
                labelTime.Text = DateTime.Now.ToLongDateString() + DateTime.Now.ToLongTimeString();  //显示当前时间

                //计算上电的时、分、秒
                int hour = ontime / 3600;
                int min = (ontime % 3600) / 60;
                int sec = ontime % 60;
                OnTime.Text = hour.ToString() + "小时" + min.ToString() + "分" + sec.ToString() + "秒";

                //计算运行的时、分、秒
                hour = runtime / 3600;
                min = (runtime % 3600) / 60;
                sec = runtime % 60;
                RunTime.Text = hour.ToString() + "小时" + min.ToString() + "分" + sec.ToString() + "秒";

                CTnow.Text = CTtoday.ToString();  //显示本次开机以来包数
                CT.Text = CTtemp.ToString();  //显示计数
                CTall.Text = CThistory.ToString();  //显示历史总数
            }

            //刷新故障记录窗口
            if (motion.errorMotion != 0)  //运动控制器错误代码不为零，未显示过（首次出现）
            {
                labelSysInfo.Text = Var.errMsgofMotion[motion.errorMotion] + "R" + motion.errorMotion.ToString("D4");
                if (errorCode < 30)  //重要错误写入文件
                {
                    string Code = "R" + motion.errorMotion.ToString("D4");
                    string Time = DateTime.Now.ToLongDateString() + DateTime.Now.ToLongTimeString();  //当前时间
                    string Alarm = Var.errMsgofMotion[motion.errorMotion];
                    Sv_Alarm(Time, Code, Alarm);
                }
                motion.errorMotion = 0;
            }
            if (errorForm != 0)  //界面及软件系统错误代码不为零，未显示过（首次出现）
            {
                labelSysInfo.Text = Var.errMsgofForm[errorForm] + "F" + errorForm.ToString("D4");
                if (errorCode < 30)  //重要错误写入文件
                {
                    string Code = "F" + errorForm.ToString("D4");
                    string Time = DateTime.Now.ToLongDateString() + DateTime.Now.ToLongTimeString();  //当前时间
                    string Alarm = Var.errMsgofForm[errorForm];
                    Sv_Alarm(Time, Code, Alarm);
                }
                errorForm = 0;
            }
            if (errorMath != 0)  //计算函数库错误代码不为零，未显示过（首次出现）
            {
                labelSysInfo.Text = Var.errMsgofMath[errorMath] + "P" + errorMath.ToString("D4");
                if (errorCode < 30)  //重要错误写入文件
                {
                    string Code = "P" + errorMath.ToString("D4");
                    string Time = DateTime.Now.ToLongDateString() + DateTime.Now.ToLongTimeString();  //当前时间
                    string Alarm = Var.errMsgofMath[errorMath];
                    Sv_Alarm(Time, Code, Alarm);
                }
                errorMath = 0;
            }
            if (motion.errorIO != 0)  //周边系统（IO）错误代码不为零，未显示过（首次出现）
            {
                labelSysInfo.Text = Var.errMsgofIO[motion.errorIO] + "IO" + motion.errorIO.ToString("D4");
                if (errorCode < 30)  //重要错误写入文件
                {
                    string Code = "IO" + motion.errorIO.ToString("D4");
                    string Time = DateTime.Now.ToLongDateString() + DateTime.Now.ToLongTimeString();  //当前时间
                    string Alarm = Var.errMsgofIO[motion.errorIO];
                    Sv_Alarm(Time, Code, Alarm);
                }
                motion.errorIO = 0;
            }
            
            time1m++; time5s++;

            //系统提示信息复位（提示信息最长显示5秒）
            if (time5s > 4)  
                if (labelSysInfo.Text != "系统提示信息")
                {
                    labelSysInfo.Text = "系统提示信息";
                    time5s = 0;
                }

            //每10分钟保存一次信息（每秒过于频繁）。
            if (time1m > 599)
            {
                time1m = 0;
                Sv_SysInfo();
            }
        }

        //高精度Timer事件（200ms） 笛卡尔坐标 SYSPLC.IN
        private void timer200ms_Tick(object sender, EventArgs e)
        {
            if (SYSPLC.TB_play && !motion.teachLock && (currentMode != Var.CurrentMode.Play))
            {
                currentMode = Var.CurrentMode.Play;
                picOpMod.Image = Properties.Resources.AUTO;
                panelEncoder.Visible = false;
                panelManu.Visible = false;
                panelRun.Visible = true;
            }
            if (SYSPLC.TB_teach && !run && (currentMode != Var.CurrentMode.Teach))
            {
                currentMode = Var.CurrentMode.Teach;
                picOpMod.Image = Properties.Resources.HAND;

                panelEncoder.Visible = false;
                panelManu.Visible = true;
                panelRun.Visible = false;
            }
            if ((currentMode == Var.CurrentMode.Play) && SYSPLC.TB_start && !motion.start1)
            {
                if (motion.run1)
                    motion.start1 = true;
                else
                    btnUse1_Click(null, null);
                SYSPLC.TB_green = true;
                SYSPLC.TB_white = false;
            }
            if ((currentMode == Var.CurrentMode.Play) && SYSPLC.TB_hold && motion.start1)
            {
                motion.start1 = false;
                SYSPLC.TB_green = false;
                SYSPLC.TB_white = true;
            }

            if (panelManu.Visible && (tabControl.SelectedIndex == 2))  //手动界面上
            {
                //实时刷新
                if (motion.coord == MathFun.COORDtyp.Joint)//当前坐标为关节坐标时
                {
                    if (motion.robotNum == 1)
                    {
                        textBoxAR.Text = (motion.curJPos1.J1 * KAngle).ToString("F3") + "deg";
                        textBoxBR.Text = (motion.curJPos1.J2 * KAngle).ToString("F3") + "deg";
                        textBoxCR.Text = (motion.curJPos1.J3 * KAngle).ToString("F3") + "deg";
                        textBoxDR.Text = (motion.curJPos1.J4 * KAngle).ToString("F3") + "deg";
                    }
                    else
                    {
                        textBoxAR.Text = (motion.curJPos2.J1 * KAngle).ToString("F3") + "deg";
                        textBoxBR.Text = (motion.curJPos2.J2 * KAngle).ToString("F3") + "deg";
                        textBoxCR.Text = (motion.curJPos2.J3 * KAngle).ToString("F3") + "deg";
                        textBoxDR.Text = (motion.curJPos2.J4 * KAngle).ToString("F3") + "deg";
                    }
                }
                else  //笛卡尔坐标
                {
                    MathFun.HCOORDtyp tpPos = new MathFun.HCOORDtyp();
                    MathFun.JPOStyp jPos = new MathFun.JPOStyp();
                    double tp = 0;

                    if (motion.robotNum == 1)
                    {
                        jPos = motion.curJPos1;
                        tpPos = MathFun.MF_JOINT2ROBOT(motion.curJPos1);
                    }
                    else
                    {
                        jPos = motion.curJPos2;
                        tpPos = MathFun.MF_JOINT2ROBOT(motion.curJPos2);
                    }
                    tp = 1000 * tpPos.X;
                    textBoxAR.Text = tp.ToString("F2") + "mm";
                    tp = 1000 * tpPos.Z;
                    textBoxBR.Text = tp.ToString("F2") + "mm";
                    tp = (jPos.J1 + jPos.J2 + jPos.J3) * KAngle;
                    textBoxCR.Text = tp.ToString("F3") + "deg";
                    tp = jPos.J4 * KAngle;
                    textBoxDR.Text = tp.ToString("F3") + "deg";
                }
            }
            else if (panelRun.Visible && (tabControl.SelectedIndex == 2))  //自动界面上
            {
                if (run)
                {
                    textLStep.Text = "?";
                    textRStep.Text = "?";
                    textDStep.Text = "?";
                }
            }
            else if (panelSysIO.Visible && (tabControl.SelectedIndex == 3))  //系统IO界面上
            {
                //刷新IO端口显示
                if (!IOrefreshLock)  //系统IO  
                {
                    int IO = 0;
                    int RLimit, LLimit, Home, Alarm, ClsR, ServR;

                    if (!SYSPLC.SCAN.WaitOne(5, false)) return;  //5ms不能获得PLC信息，退出 Mutex
                    RLimit = SYSPLC.RLimit;
                    LLimit = SYSPLC.LLimit;
                    Home = SYSPLC.Home;
                    Alarm = SYSPLC.Alarm;
                    ClsR = SYSPLC.ClsR;
                    ServR = SYSPLC.ServR;
                    SYSPLC.SCAN.ReleaseMutex();

                    IO = RLimit;
                    if ((IO & 0x01) == 0) checkBoxRL1.Checked = false;
                    else checkBoxRL1.Checked = true;
                    if ((IO & 0x02) == 0) checkBoxRL2.Checked = false;
                    else checkBoxRL2.Checked = true;
                    if ((IO & 0x04) == 0) checkBoxRL3.Checked = false;
                    else checkBoxRL3.Checked = true;
                    if ((IO & 0x08) == 0) checkBoxRL4.Checked = false;
                    else checkBoxRL4.Checked = true;
                    if ((IO & 0x10) == 0) checkBoxRL5.Checked = false;
                    else checkBoxRL5.Checked = true;
                    if ((IO & 0x20) == 0) checkBoxRL6.Checked = false;
                    else checkBoxRL6.Checked = true;
                    if ((IO & 0x40) == 0) checkBoxRL7.Checked = false;
                    else checkBoxRL7.Checked = true;
                    if ((IO & 0x80) == 0) checkBoxRL8.Checked = false;
                    else checkBoxRL8.Checked = true;

                    IO = LLimit;
                    if ((IO & 0x01) == 0) checkBoxLL1.Checked = false;
                    else checkBoxLL1.Checked = true;
                    if ((IO & 0x02) == 0) checkBoxLL2.Checked = false;
                    else checkBoxLL2.Checked = true;
                    if ((IO & 0x04) == 0) checkBoxLL3.Checked = false;
                    else checkBoxLL3.Checked = true;
                    if ((IO & 0x08) == 0) checkBoxLL4.Checked = false;
                    else checkBoxLL4.Checked = true;
                    if ((IO & 0x10) == 0) checkBoxLL5.Checked = false;
                    else checkBoxLL5.Checked = true;
                    if ((IO & 0x20) == 0) checkBoxLL6.Checked = false;
                    else checkBoxLL6.Checked = true;
                    if ((IO & 0x40) == 0) checkBoxLL7.Checked = false;
                    else checkBoxLL7.Checked = true;
                    if ((IO & 0x80) == 0) checkBoxLL8.Checked = false;
                    else checkBoxLL8.Checked = true;

                    IO = Home;
                    if ((IO & 0x01) == 0) checkBoxH1.Checked = false;
                    else checkBoxH1.Checked = true;
                    if ((IO & 0x02) == 0) checkBoxH2.Checked = false;
                    else checkBoxH2.Checked = true;
                    if ((IO & 0x04) == 0) checkBoxH3.Checked = false;
                    else checkBoxH3.Checked = true;
                    if ((IO & 0x08) == 0) checkBoxH4.Checked = false;
                    else checkBoxH4.Checked = true;
                    if ((IO & 0x10) == 0) checkBoxH5.Checked = false;
                    else checkBoxH5.Checked = true;
                    if ((IO & 0x20) == 0) checkBoxH6.Checked = false;
                    else checkBoxH6.Checked = true;
                    if ((IO & 0x40) == 0) checkBoxH7.Checked = false;
                    else checkBoxH7.Checked = true;
                    if ((IO & 0x80) == 0) checkBoxH8.Checked = false;
                    else checkBoxH8.Checked = true;

                    IO = Alarm;
                    if ((IO & 0x01) == 0) checkBoxA1.Checked = false;
                    else checkBoxA1.Checked = true;
                    if ((IO & 0x02) == 0) checkBoxA2.Checked = false;
                    else checkBoxA2.Checked = true;
                    if ((IO & 0x04) == 0) checkBoxA3.Checked = false;
                    else checkBoxA3.Checked = true;
                    if ((IO & 0x08) == 0) checkBoxA4.Checked = false;
                    else checkBoxA4.Checked = true;
                    if ((IO & 0x10) == 0) checkBoxA5.Checked = false;
                    else checkBoxA5.Checked = true;
                    if ((IO & 0x20) == 0) checkBoxA6.Checked = false;
                    else checkBoxA6.Checked = true;
                    if ((IO & 0x40) == 0) checkBoxA7.Checked = false;
                    else checkBoxA7.Checked = true;
                    if ((IO & 0x80) == 0) checkBoxA8.Checked = false;
                    else checkBoxA8.Checked = true;

                    IO = ClsR;
                    if ((IO & 0x01) == 0) checkBoxC1.Checked = false;
                    else checkBoxC1.Checked = true;
                    if ((IO & 0x02) == 0) checkBoxC2.Checked = false;
                    else checkBoxC2.Checked = true;
                    if ((IO & 0x04) == 0) checkBoxC3.Checked = false;
                    else checkBoxC3.Checked = true;
                    if ((IO & 0x08) == 0) checkBoxC4.Checked = false;
                    else checkBoxC4.Checked = true;
                    if ((IO & 0x10) == 0) checkBoxC5.Checked = false;
                    else checkBoxC5.Checked = true;
                    if ((IO & 0x20) == 0) checkBoxC6.Checked = false;
                    else checkBoxC6.Checked = true;
                    if ((IO & 0x40) == 0) checkBoxC7.Checked = false;
                    else checkBoxC7.Checked = true;
                    if ((IO & 0x80) == 0) checkBoxC8.Checked = false;
                    else checkBoxC8.Checked = true;

                    IO = ServR;
                    if ((IO & 0x01) == 0) checkBoxS1.Checked = false;
                    else checkBoxS1.Checked = true;
                    if ((IO & 0x02) == 0) checkBoxS2.Checked = false;
                    else checkBoxS2.Checked = true;
                    if ((IO & 0x04) == 0) checkBoxS3.Checked = false;
                    else checkBoxS3.Checked = true;
                    if ((IO & 0x08) == 0) checkBoxS4.Checked = false;
                    else checkBoxS4.Checked = true;
                    if ((IO & 0x10) == 0) checkBoxS5.Checked = false;
                    else checkBoxS5.Checked = true;
                    if ((IO & 0x20) == 0) checkBoxS6.Checked = false;
                    else checkBoxS6.Checked = true;
                    if ((IO & 0x40) == 0) checkBoxS7.Checked = false;
                    else checkBoxS7.Checked = true;
                    if ((IO & 0x80) == 0) checkBoxS8.Checked = false;
                    else checkBoxS8.Checked = true;
                }
            }
            else if (panelUserIO.Visible && (tabControl.SelectedIndex == 3))  //用户IO界面上
            {
                //刷新IO端口显示
                if (!IOrefreshLock)  //系统IO
                {
                    if (SYSPLC.IN[0]) checkBoxI01.Checked = true;
                    else checkBoxI01.Checked = false;
                    if (SYSPLC.IN[1]) checkBoxI02.Checked = true;
                    else checkBoxI02.Checked = false;
                    if (SYSPLC.IN[2]) checkBoxI03.Checked = true;
                    else checkBoxI03.Checked = false;
                    if (SYSPLC.IN[3]) checkBoxI04.Checked = true;
                    else checkBoxI04.Checked = false;
                    if (SYSPLC.IN[4]) checkBoxI05.Checked = true;
                    else checkBoxI05.Checked = false;
                    if (SYSPLC.IN[5]) checkBoxI06.Checked = true;
                    else checkBoxI06.Checked = false;
                    if (SYSPLC.IN[6]) checkBoxI07.Checked = true;
                    else checkBoxI07.Checked = false;
                    if (SYSPLC.IN[7]) checkBoxI08.Checked = true;
                    else checkBoxI08.Checked = false;
                    if (SYSPLC.IN[8]) checkBoxI09.Checked = true;
                    else checkBoxI09.Checked = false;
                    if (SYSPLC.IN[9]) checkBoxI10.Checked = true;
                    else checkBoxI10.Checked = false;
                    if (SYSPLC.IN[10]) checkBoxI11.Checked = true;
                    else checkBoxI11.Checked = false;
                    if (SYSPLC.IN[11]) checkBoxI12.Checked = true;
                    else checkBoxI12.Checked = false;
                    if (SYSPLC.IN[12]) checkBoxI13.Checked = true;
                    else checkBoxI13.Checked = false;
                    if (SYSPLC.IN[13]) checkBoxI14.Checked = true;
                    else checkBoxI14.Checked = false;
                    if (SYSPLC.IN[14]) checkBoxI15.Checked = true;
                    else checkBoxI15.Checked = false;
                    if (SYSPLC.IN[15]) checkBoxI16.Checked = true;
                    else checkBoxI16.Checked = false;

                    if (SYSPLC.OTR[0]) checkBoxO01.Checked = true;
                    else checkBoxO01.Checked = false;
                    if (SYSPLC.OTR[1]) checkBoxO02.Checked = true;
                    else checkBoxO02.Checked = false;
                    if (SYSPLC.OTR[2]) checkBoxO03.Checked = true;
                    else checkBoxO03.Checked = false;
                    if (SYSPLC.OTR[3]) checkBoxO04.Checked = true;
                    else checkBoxO04.Checked = false;
                    if (SYSPLC.OTR[4]) checkBoxO05.Checked = true;
                    else checkBoxO05.Checked = false;
                    if (SYSPLC.OTR[5]) checkBoxO06.Checked = true;
                    else checkBoxO06.Checked = false;
                    if (SYSPLC.OTR[6]) checkBoxO07.Checked = true;
                    else checkBoxO07.Checked = false;
                    if (SYSPLC.OTR[7]) checkBoxO08.Checked = true;
                    else checkBoxO08.Checked = false;
                    if (SYSPLC.OTR[8]) checkBoxO09.Checked = true;
                    else checkBoxO09.Checked = false;
                    if (SYSPLC.OTR[9]) checkBoxO10.Checked = true;
                    else checkBoxO10.Checked = false;
                    if (SYSPLC.OTR[10]) checkBoxO11.Checked = true;
                    else checkBoxO11.Checked = false;
                    if (SYSPLC.OTR[11]) checkBoxO12.Checked = true;
                    else checkBoxO12.Checked = false;
                    if (SYSPLC.OTR[12]) checkBoxO13.Checked = true;
                    else checkBoxO13.Checked = false;
                    if (SYSPLC.OTR[13]) checkBoxO14.Checked = true;
                    else checkBoxO14.Checked = false;
                    if (SYSPLC.OTR[14]) checkBoxO15.Checked = true;
                    else checkBoxO15.Checked = false;
                    if (SYSPLC.OTR[15]) checkBoxO16.Checked = true;
                    else checkBoxO16.Checked = false;
                }
            }
        }

        //按键按下的处理 //截断按键？
        private void FormMain_KeyDown(object sender, KeyEventArgs e)
        {
            int keynum = (int)e.KeyData;
            bool cutkey = true;  //是否截断系统按键处理
            switch (keynum)
            {
                case 27:  //Esc
                    break;
                case 8:  //退格
                    break;
                case 113:  //F2，加速
                    SpeedUp();
                    break;
                case 114:  //F3，减速
                    SpeedDown();
                    break;
                case 115:  //F4，选择
                    if ((tabControl.SelectedIndex == 0) && (curJob.JobName != null))
                    {
                        lbCmdPutin();
                        lbCmd.Visible = false;
                        SYSPLC.TB_informlist = false;
                    }
                    break;
                case 116:  //F5，伺服
                    buttonServo_Click(null, null);
                    break;
                case 118:  //F7，取消限制
                    if (currentMode == Var.CurrentMode.Teach)  //只有示教状态
                    {
                        if (!SYSPLC.TB_releasestrict)  //原本有限，变无限
                        {
                            if (motion.RC_SetLimit(true) == 0)
                                SYSPLC.TB_releasestrict = true;
                        }
                        else  //原本无限，变有限
                        {
                            if (motion.RC_SetLimit(false) == 0)
                                SYSPLC.TB_releasestrict = false;
                        }
                    }
                    break;
                case 119:  //F8，机器人组
                    picManipulator_Click(null, null);
                    break;
                case 120:  //F9，外部轴
                    break;
                case 121:  //F10，多画面
                    break;
                case 86:  //V，直接打开
                    break;
                case 71:  //G，翻页
                    if (tabControl.SelectedIndex > 3)
                        tabControl.SelectedIndex = -1;
                    tabControl.SelectedIndex++;
                    break;
                case 46:  //Del，辅助
                    break;
                case 13:  //回车
                    if ((tabControl.SelectedIndex == 0) && (curJob.JobName != null))
                    {
                        lbCmdPutin();
                        lbCmd.Visible = false;
                        SYSPLC.TB_informlist = false;
                    }
                    break;
                case 70:  //F，主菜单
                    tabControl.Focus();
                    break;
                case 67:  //C，坐标系
                    picCoord_Click(null, null);
                    break;
                case 37:  //左
                case 38:  //上
                    if ((tabControl.SelectedIndex == 2) && panelManu.Visible)
                        btnPrev_Click(null, null);
                    else
                        cutkey = false;
                    break;
                case 39:  //右
                case 40:  //下
                    if ((tabControl.SelectedIndex == 2) && panelManu.Visible)
                        btnNext_Click(null, null);
                    else
                        cutkey = false;
                    break;
                case 81:  //Q，X-
                    if (SYSPLC.TB_teach && !motion.moving1 && !motion.moving2)
                        picAN_MouseDown(null, null);
                    break;
                case 87:  //W，X+
                    if (SYSPLC.TB_teach && !motion.moving1 && !motion.moving2)
                        picAP_MouseDown(null, null);
                    break;
                case 65:  //A，Y-
                    if (SYSPLC.TB_teach && !motion.moving1 && !motion.moving2)
                        picBN_MouseDown(null, null);
                    break;
                case 83:  //S，Y+
                    if (SYSPLC.TB_teach && !motion.moving1 && !motion.moving2)
                        picBP_MouseDown(null, null);
                    break;
                case 90:  //Z，Z-
                    if (SYSPLC.TB_teach && !motion.moving1 && !motion.moving2)
                        picCN_MouseDown(null, null);
                    break;
                case 88:  //X，Z+
                    if (SYSPLC.TB_teach && !motion.moving1 && !motion.moving2)
                        picCP_MouseDown(null, null);
                    break;
                case 85:  //U，A-
                    if (SYSPLC.TB_teach && !motion.moving1 && !motion.moving2)
                        picDN_MouseDown(null, null);
                    break;
                case 73:  //I，A+
                    if (SYSPLC.TB_teach && !motion.moving1 && !motion.moving2)
                        picDP_MouseDown(null, null);
                    break;
                case 74:  //J，B-
                    //motion.dirPos.THETA = -1;
                    //motion.RC_ManuMov();
                    break;
                case 75:  //K，B+
                    //motion.dirPos.THETA = 1;
                    //motion.RC_ManuMov();
                    break;
                case 78:  //N，C-
                    //motion.dirPos.PSI = -1;
                    //motion.RC_ManuMov();
                    break;
                case 77:  //M，C+
                    //motion.dirPos.PSI = 1;
                    //motion.RC_ManuMov();
                    break;
                case 65552:  //Shift，上档
                    if (playMode == Var.PlayMode.Cycle)
                    {
                        playMode = Var.PlayMode.Auto;
                        picCycle.Image = Properties.Resources.AUTORUN;
                    }
                    else if (playMode == Var.PlayMode.Auto)
                    {
                        playMode = Var.PlayMode.Step;
                        picCycle.Image = Properties.Resources.POINTRUN;
                    }
                    else if (playMode == Var.PlayMode.Step)
                    {
                        playMode = Var.PlayMode.Cycle;
                        picCycle.Image = Properties.Resources.CYCLERUN;
                    }
                    break;
                case 131089:  //Ctrl，连锁
                    if (currentMode == Var.CurrentMode.Teach)
                    {
                        if (motion.teachLock)
                        {
                            motion.teachLock = false;
                            picTeachLock.Image = Properties.Resources.TUNLOCK;
                        }
                        else
                        {
                            motion.teachLock = true;
                            picTeachLock.Image = Properties.Resources.TLOCK;
                        }
                    }
                    break;
                case 76:  //L，插补
                    break;
                case 82:  //R，区域
                    break;
                case 69:  //E，命令一览
                    if ((tabControl.SelectedIndex == 0) && (curJob.JobName != null))
                    {
                        if (!lbCmd.Visible)
                        {
                            lbCmd.Visible = true;
                            lbCmd.SelectedIndex = 0;
                            lbCmd.Focus();
                            SYSPLC.TB_informlist = true;
                        }
                        else
                        {
                            lbCmd.Visible = false;
                            lbfile.Focus();
                            SYSPLC.TB_informlist = false;
                        }
                    }
                    break;
                case 79:  //O，后退
                    break;
                case 80:  //P，前进
                    if ((tabControl.SelectedIndex == 2) && panelManu.Visible && (curJob.NumPos > 0) && !motion.moving1 && !motion.moving2)
                        buttonForward_Click(null, null);
                    break;
                case 66:  //B，插入
                    if (!SYSPLC.TB_insert)
                    {
                        SYSPLC.TB_insert = true;
                        SYSPLC.TB_modify = false;
                        SYSPLC.TB_delete = false;
                    }
                    else
                    {
                        SYSPLC.TB_insert = false;
                    }
                    break;
                case 68:  //D，修改
                    if (!SYSPLC.TB_modify)
                    {
                        SYSPLC.TB_modify = true;
                        SYSPLC.TB_insert = false;
                        SYSPLC.TB_delete = false;
                    }
                    else
                    {
                        SYSPLC.TB_modify = false;
                    }
                    break;
                case 72:  //H，清除
                    break;
                case 84:  //T，删除
                    if (!SYSPLC.TB_delete)
                    {
                        SYSPLC.TB_delete = true;
                        SYSPLC.TB_insert = false;
                        SYSPLC.TB_modify = false;
                    }
                    else
                    {
                        SYSPLC.TB_delete = false;
                    }
                    break;
                case 89:  //Y，确认
                    if ((tabControl.SelectedIndex == 0) && textCMD.Enabled)  //文件编辑页面
                    {
                        if (SYSPLC.TB_modify)
                        {
                            picDelete_Click(null, null);
                            picInsert_Click(null, null);
                        }
                        else if (SYSPLC.TB_delete)
                            picDelete_Click(null, null);
                        else if (SYSPLC.TB_insert)
                            picInsert_Click(null, null);
                    }
                    else if ((tabControl.SelectedIndex == 2) && panelManu.Visible)  //手动示教页面
                    {
                        if (SYSPLC.TB_modify)
                            buttonAffirm_Click(null, null);
                        else if (SYSPLC.TB_delete)
                            buttonDel_Click(null, null);
                        else if (SYSPLC.TB_insert)
                            buttonIns_Click(null, null);
                    }

                    break;
                case 48:  //0
                    break;
                case 49:  //1
                    break;
                case 50:  //2
                    break;
                case 51:  //3
                    break;
                case 52:  //4
                    break;
                case 53:  //5
                    break;
                case 54:  //6
                    break;
                case 55:  //7
                    break;
                case 56:  //8
                    break;
                case 57:  //9
                    break;
                case 190:  //.
                    break;
                case 109:  //-
                    break;
                default:
                    break;
            }
            if (cutkey) e.Handled = true;
        }

        //按键抬起处理
        private void FormMain_KeyUp(object sender, KeyEventArgs e)
        {
            switch ((int)e.KeyData)
            {
                case 81:  //Q，X-
                    picAN_MouseUp(null, null);
                    break;
                case 87:  //W，X+
                    picAP_MouseUp(null, null);
                    break;
                case 65:  //A，Y-
                    picBN_MouseUp(null, null);
                    break;
                case 83:  //S，Y+
                    picBP_MouseUp(null, null);
                    break;
                case 90:  //Z，Z-
                    picCN_MouseUp(null, null);
                    break;
                case 88:  //X，Z+
                    picCP_MouseUp(null, null);
                    break;
                case 85:  //U，A-
                    picDN_MouseUp(null, null);
                    break;
                case 73:  //I，A+
                    picDP_MouseUp(null, null);
                    break;
                case 74:  //J，B-
                    //motion.dirPos.THETA = 0;
                    //motion.RC_ManuMov();
                    break;
                case 75:  //K，B+
                    //motion.dirPos.THETA = 0;
                    //motion.RC_ManuMov();
                    break;
                case 78:  //N，C-
                    //motion.dirPos.PSI = 0;
                    //motion.RC_ManuMov();
                    break;
                case 77:  //M，C+
                    //motion.dirPos.PSI = 0;
                    //motion.RC_ManuMov();
                    break;
                case 80:  //P，前进
                    if ((tabControl.SelectedIndex == 2) && panelManu.Visible && (curJob.NumPos > 0))
                        motion.RC_MovStop(false);
                    break;
                default:
                    break;
            }
        }

        private void panelEncoder_GotFocus(object sender, EventArgs e)
        {

        }




    
    }
}