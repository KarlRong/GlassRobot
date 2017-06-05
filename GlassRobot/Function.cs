using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Microsoft.Win32;
using System.Diagnostics;
using System.ComponentModel;
using System.IO;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using System.Collections;
using Controller;

namespace GlassRobot
{
    public partial class FormMain : Form
    {
        /// <summary>
        /// 启动数字软键盘  s/ShowDialog
        /// </summary>
        /// <param name="info">提示</param>
        /// <param name="old">原有数据</param>
        /// <param name="pass">是否密码</param>
        /// <returns></returns>
        public static string KeyNum(string info, string old, bool pass)
        {
            FormKeyNum keyNum = new FormKeyNum();
            if (pass) keyNum.textBox1.PasswordChar = '*';
            keyNum.label1.Text = info;
            keyNum.textBox1.Text = old;
            if (keyNum.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
                return old;
            else
                return keyNum.textBox1.Text;
        }

        /// <summary>
        /// 显示一个信息窗口
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="content">内容</param>
        public static void MsgBox(string title, string content)
        {
            FormMsgBox msgBox = new FormMsgBox();
            msgBox.textBox1.Text = content;
            msgBox.label1.Text = title;
            msgBox.ShowDialog();
        }

        /// <summary>
        /// 读入所有参数
        /// </summary>
        private int readAllParameter()
        {
            //读入系统参数并初始化界面
            XmlElement xePRM = filePRM.DocumentElement;

            try
            {
                //时间和统计信息
                runtime = int.Parse(xePRM.SelectSingleNode("/RobotPRM/MovTime").InnerText);
                ontime = int.Parse(xePRM.SelectSingleNode("/RobotPRM/OnTime").InnerText);
                CThistory = int.Parse(xePRM.SelectSingleNode("/RobotPRM/CTall").InnerText);
                Password = xePRM.SelectSingleNode("/RobotPRM/Password").InnerText;
                int rtn = 0;
                rtn = Ld_Robot();//载入机器人参数
                if (rtn != 0) errorForm = 1;
                rtn = Ld_Notation();//读取各端口注释
                if (rtn != 0) errorForm = 2;

                //读入错误信息并初始化界面
                Ld_ErrorMsg();
            }
            catch
            {
                MessageBox.Show("当前机器人参数加载失败", "严重错误！");
                return -1;
            }

            return 0;
        }

        /// <summary>
        /// 加载故障信息
        /// </summary>
        public void  Ld_ErrorMsg()
        {
            try
            {
                //读入错误信息并初始化界面
                XmlElement xeRCD = fileERR.DocumentElement;
                XmlNode xn = fileERR.SelectSingleNode("Error");
                XmlNodeList xnl = xn.ChildNodes;
                string temp = "";
                foreach (XmlNode xnf in xnl)
                {
                    XmlElement xe = (XmlElement)xnf;
                    temp += xe.GetAttribute("time") + "  " + xe.GetAttribute("code") + "      " + xe.GetAttribute("text") + "\r\n";
                }
                textBoxErrList.Text = temp;
            }
            catch
            {
                MessageBox.Show("当前机器人参数加载失败", "严重错误！");
            }
        }

        /// <summary>
        /// 初始化数学类
        /// </summary>
        /// <returns></returns>
        private int InitMathfun()
        {
            //单位坐标
            motion.tool = MathFun.MF_HCOORDINIT();
            motion.tool.X = motion.tool1.X;
            motion.tool.Z = motion.tool1.Z;

            //初始化数学类库
            MathFun.MF_INITMATHFUN(motion.tool, motion.vmax, motion.acc, MathFun.HJ);

            return 0;
        }

        /// <summary>
        /// 保存系统定时和计数信息
        /// 错误代码5
        /// </summary>
        private int Sv_SysInfo()
        {
            try
            {
                XmlElement root = filePRM.DocumentElement;

                root.SelectSingleNode("/RobotPRM/MovTime").InnerText = runtime.ToString();
                root.SelectSingleNode("/RobotPRM/OnTime").InnerText = ontime.ToString();
                root.SelectSingleNode("/RobotPRM/CTall").InnerText = CThistory.ToString();
                root.SelectSingleNode("/RobotPRM/CTday").InnerText = CTtoday.ToString();
                root.SelectSingleNode("/RobotPRM/CT").InnerText = CTtemp.ToString();

                filePRM.Save(Var.FilePath + "Robot.PRM");
                return 0;
            }
            catch
            { return -1; }
        }

        /// <summary>
        /// 增加一条错误信息
        /// </summary>
        /// <param name="Time">错误发生时间</param>
        /// <param name="Code">错误代码</param>
        /// <param name="Alarm">错误文本</param>
        /// <returns></returns>
        private void Sv_Alarm(string Time, string Code, string Alarm)
        {
            XmlElement root = fileERR.DocumentElement;

            XmlElement xe1 = fileERR.CreateElement("record"); //创建一个<record>节点
            xe1.SetAttribute("time", Time); //设置该节点Time属性
            xe1.SetAttribute("code", Code);
            xe1.SetAttribute("text", Alarm);

            root.AppendChild(xe1); //添加到<Alarm>根节点中
            fileERR.Save(Var.FilePath + "Error.RCD");
            
            Ld_ErrorMsg();  //重新加载刷新一次
        }

        /// <summary>
        /// 读取机器人参数信息
        /// 错误代码1
        /// </summary>
        private int Ld_Robot()
        {
            try
            {
                XmlElement xePRM = filePRM.DocumentElement;

                //原点
                MathFun.HJ[0] = double.Parse(xePRM.SelectSingleNode("/RobotPRM/Home/J1").InnerText);
                MathFun.HJ[1] = double.Parse(xePRM.SelectSingleNode("/RobotPRM/Home/J2").InnerText);
                MathFun.HJ[2] = double.Parse(xePRM.SelectSingleNode("/RobotPRM/Home/J3").InnerText);
                MathFun.HJ[3] = double.Parse(xePRM.SelectSingleNode("/RobotPRM/Home/J4").InnerText);
                MathFun.HJ[4] = double.Parse(xePRM.SelectSingleNode("/RobotPRM/Home/J5").InnerText);
                MathFun.HJ[5] = double.Parse(xePRM.SelectSingleNode("/RobotPRM/Home/J6").InnerText);
                MathFun.HJ[6] = double.Parse(xePRM.SelectSingleNode("/RobotPRM/Home/J7").InnerText);
                MathFun.HJ[7] = double.Parse(xePRM.SelectSingleNode("/RobotPRM/Home/J8").InnerText);

                //手爪参数
                motion.tool1.X = double.Parse(xePRM.SelectSingleNode("/RobotPRM/Hand1/Size/CenterX").InnerText);
                motion.tool1.Z = double.Parse(xePRM.SelectSingleNode("/RobotPRM/Hand1/Size/CenterZ").InnerText);
                motion.handTimeOpen1 = double.Parse(xePRM.SelectSingleNode("/RobotPRM/Hand1/Time/Open").InnerText);
                motion.handTimeClose1 = double.Parse(xePRM.SelectSingleNode("/RobotPRM/Hand1/Time/Close").InnerText);
                motion.handI1 = int.Parse(xePRM.SelectSingleNode("/RobotPRM/Hand1/IO/IN").InnerText);
                motion.handO1 = int.Parse(xePRM.SelectSingleNode("/RobotPRM/Hand1/IO/OUT").InnerText);

                motion.tool2.X = double.Parse(xePRM.SelectSingleNode("/RobotPRM/Hand2/Size/CenterX").InnerText);
                motion.tool2.Z = double.Parse(xePRM.SelectSingleNode("/RobotPRM/Hand2/Size/CenterZ").InnerText);
                motion.handTimeOpen2 = double.Parse(xePRM.SelectSingleNode("/RobotPRM/Hand2/Time/Open").InnerText);
                motion.handTimeClose2 = double.Parse(xePRM.SelectSingleNode("/RobotPRM/Hand2/Time/Close").InnerText);
                motion.handI2 = int.Parse(xePRM.SelectSingleNode("/RobotPRM/Hand2/IO/IN").InnerText);
                motion.handO2 = int.Parse(xePRM.SelectSingleNode("/RobotPRM/Hand2/IO/OUT").InnerText);

                //软限位
                motion.moveLimitN[0] = double.Parse(xePRM.SelectSingleNode("/RobotPRM/Servo/Lim1N").InnerText);
                motion.moveLimitP[0] = double.Parse(xePRM.SelectSingleNode("/RobotPRM/Servo/Lim1P").InnerText);
                motion.moveLimitN[1] = -double.Parse(xePRM.SelectSingleNode("/RobotPRM/Servo/Lim2N").InnerText);
                motion.moveLimitP[1] = -double.Parse(xePRM.SelectSingleNode("/RobotPRM/Servo/Lim2P").InnerText);
                motion.moveLimitN[2] = double.Parse(xePRM.SelectSingleNode("/RobotPRM/Servo/Lim3N").InnerText);
                motion.moveLimitP[2] = double.Parse(xePRM.SelectSingleNode("/RobotPRM/Servo/Lim3P").InnerText);
                motion.moveLimitN[3] = double.Parse(xePRM.SelectSingleNode("/RobotPRM/Servo/Lim4N").InnerText);
                motion.moveLimitP[3] = double.Parse(xePRM.SelectSingleNode("/RobotPRM/Servo/Lim4P").InnerText);
                motion.moveLimitN[4] = double.Parse(xePRM.SelectSingleNode("/RobotPRM/Servo/Lim5N").InnerText);
                motion.moveLimitP[4] = double.Parse(xePRM.SelectSingleNode("/RobotPRM/Servo/Lim5P").InnerText);
                motion.moveLimitN[5] = -double.Parse(xePRM.SelectSingleNode("/RobotPRM/Servo/Lim6N").InnerText);
                motion.moveLimitP[5] = -double.Parse(xePRM.SelectSingleNode("/RobotPRM/Servo/Lim6P").InnerText);
                motion.moveLimitN[6] = double.Parse(xePRM.SelectSingleNode("/RobotPRM/Servo/Lim7N").InnerText);
                motion.moveLimitP[6] = double.Parse(xePRM.SelectSingleNode("/RobotPRM/Servo/Lim7P").InnerText);
                motion.moveLimitN[7] = double.Parse(xePRM.SelectSingleNode("/RobotPRM/Servo/Lim8N").InnerText);
                motion.moveLimitP[7] = double.Parse(xePRM.SelectSingleNode("/RobotPRM/Servo/Lim8P").InnerText);

                //最大速度、加速度
                motion.acc[0] = double.Parse(xePRM.SelectSingleNode("/RobotPRM/Servo/ACC1").InnerText);
                motion.acc[1] = double.Parse(xePRM.SelectSingleNode("/RobotPRM/Servo/ACC2").InnerText);
                motion.acc[2] = double.Parse(xePRM.SelectSingleNode("/RobotPRM/Servo/ACC3").InnerText);
                motion.acc[3] = double.Parse(xePRM.SelectSingleNode("/RobotPRM/Servo/ACC4").InnerText);
                motion.acc[4] = double.Parse(xePRM.SelectSingleNode("/RobotPRM/Servo/ACC5").InnerText);
                motion.acc[5] = double.Parse(xePRM.SelectSingleNode("/RobotPRM/Servo/ACC6").InnerText);
                motion.acc[6] = double.Parse(xePRM.SelectSingleNode("/RobotPRM/Servo/ACC7").InnerText);
                motion.acc[7] = double.Parse(xePRM.SelectSingleNode("/RobotPRM/Servo/ACC8").InnerText);
                motion.vmax[0] = double.Parse(xePRM.SelectSingleNode("/RobotPRM/Servo/Vmax1").InnerText);
                motion.vmax[1] = double.Parse(xePRM.SelectSingleNode("/RobotPRM/Servo/Vmax2").InnerText);
                motion.vmax[2] = double.Parse(xePRM.SelectSingleNode("/RobotPRM/Servo/Vmax3").InnerText);
                motion.vmax[3] = double.Parse(xePRM.SelectSingleNode("/RobotPRM/Servo/Vmax4").InnerText);
                motion.vmax[4] = double.Parse(xePRM.SelectSingleNode("/RobotPRM/Servo/Vmax5").InnerText);
                motion.vmax[5] = double.Parse(xePRM.SelectSingleNode("/RobotPRM/Servo/Vmax6").InnerText);
                motion.vmax[6] = double.Parse(xePRM.SelectSingleNode("/RobotPRM/Servo/Vmax7").InnerText);
                motion.vmax[7] = double.Parse(xePRM.SelectSingleNode("/RobotPRM/Servo/Vmax8").InnerText);

                //网络参数
                NetIP = xePRM.SelectSingleNode("/RobotPRM/NetWork/IP").InnerText;
                NetPort = xePRM.SelectSingleNode("/RobotPRM/NetWork/Port").InnerText;
                
                //管理密码
                if (Ld_Password() != 0)
                    return -1;

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
                    textBoxLimitBP.Text = (-motion.moveLimitP[1] * KAngle).ToString();
                    textBoxLimitBN.Text = (-motion.moveLimitN[1] * KAngle).ToString();
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
                    textBoxLimitBP.Text = (-motion.moveLimitP[5] * KAngle).ToString();
                    textBoxLimitBN.Text = (-motion.moveLimitN[5] * KAngle).ToString();
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
                tbJ1.Text = MathFun.HJ[0].ToString();
                tbJ2.Text = MathFun.HJ[1].ToString();
                tbJ3.Text = MathFun.HJ[2].ToString();
                tbJ4.Text = MathFun.HJ[3].ToString();
                tbJ5.Text = MathFun.HJ[4].ToString();
                tbJ6.Text = MathFun.HJ[5].ToString();
                tbJ7.Text = MathFun.HJ[6].ToString();
                tbJ8.Text = MathFun.HJ[7].ToString();

                return 0;
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// 保存机器人参数信息
        /// 错误代码11
        /// </summary>
        private int Sv_Robot()
        {
            try
            {
                XmlElement xePRM = filePRM.DocumentElement;

                //原点
                xePRM.SelectSingleNode("/RobotPRM/Home/J1").InnerText = MathFun.HJ[0].ToString();
                xePRM.SelectSingleNode("/RobotPRM/Home/J2").InnerText = MathFun.HJ[1].ToString();
                xePRM.SelectSingleNode("/RobotPRM/Home/J3").InnerText = MathFun.HJ[2].ToString();
                xePRM.SelectSingleNode("/RobotPRM/Home/J4").InnerText = MathFun.HJ[3].ToString();
                xePRM.SelectSingleNode("/RobotPRM/Home/J5").InnerText = MathFun.HJ[4].ToString();
                xePRM.SelectSingleNode("/RobotPRM/Home/J6").InnerText = MathFun.HJ[5].ToString();
                xePRM.SelectSingleNode("/RobotPRM/Home/J7").InnerText = MathFun.HJ[6].ToString();
                xePRM.SelectSingleNode("/RobotPRM/Home/J8").InnerText = MathFun.HJ[7].ToString();

                //手爪参数
                xePRM.SelectSingleNode("/RobotPRM/Hand1/Size/CenterX").InnerText = motion.tool1.X.ToString();
                xePRM.SelectSingleNode("/RobotPRM/Hand1/Size/CenterZ").InnerText = motion.tool1.Z.ToString();
                xePRM.SelectSingleNode("/RobotPRM/Hand1/Time/Open").InnerText = motion.handTimeOpen1.ToString();
                xePRM.SelectSingleNode("/RobotPRM/Hand1/Time/Close").InnerText = motion.handTimeClose1.ToString();
                xePRM.SelectSingleNode("/RobotPRM/Hand1/IO/IN").InnerText = motion.handI1.ToString();
                xePRM.SelectSingleNode("/RobotPRM/Hand1/IO/OUT").InnerText = motion.handO1.ToString();

                xePRM.SelectSingleNode("/RobotPRM/Hand2/Size/CenterX").InnerText = motion.tool2.X.ToString();
                xePRM.SelectSingleNode("/RobotPRM/Hand2/Size/CenterZ").InnerText = motion.tool2.Z.ToString();
                xePRM.SelectSingleNode("/RobotPRM/Hand2/Time/Open").InnerText = motion.handTimeOpen2.ToString();
                xePRM.SelectSingleNode("/RobotPRM/Hand2/Time/Close").InnerText = motion.handTimeClose2.ToString();
                xePRM.SelectSingleNode("/RobotPRM/Hand2/IO/IN").InnerText = motion.handI2.ToString();
                xePRM.SelectSingleNode("/RobotPRM/Hand2/IO/OUT").InnerText = motion.handO2.ToString();

                //软限位
                xePRM.SelectSingleNode("/RobotPRM/Servo/Lim1N").InnerText = motion.moveLimitN[0].ToString();
                xePRM.SelectSingleNode("/RobotPRM/Servo/Lim1P").InnerText = motion.moveLimitP[0].ToString();
                xePRM.SelectSingleNode("/RobotPRM/Servo/Lim2N").InnerText = (-motion.moveLimitN[1]).ToString();
                xePRM.SelectSingleNode("/RobotPRM/Servo/Lim2P").InnerText = (-motion.moveLimitP[1]).ToString();
                xePRM.SelectSingleNode("/RobotPRM/Servo/Lim3N").InnerText = motion.moveLimitN[2].ToString();
                xePRM.SelectSingleNode("/RobotPRM/Servo/Lim3P").InnerText = motion.moveLimitP[2].ToString();
                xePRM.SelectSingleNode("/RobotPRM/Servo/Lim4N").InnerText = motion.moveLimitN[3].ToString();
                xePRM.SelectSingleNode("/RobotPRM/Servo/Lim4P").InnerText = motion.moveLimitP[3].ToString();
                xePRM.SelectSingleNode("/RobotPRM/Servo/Lim5N").InnerText = motion.moveLimitN[4].ToString();
                xePRM.SelectSingleNode("/RobotPRM/Servo/Lim5P").InnerText = motion.moveLimitP[4].ToString();
                xePRM.SelectSingleNode("/RobotPRM/Servo/Lim6N").InnerText = (-motion.moveLimitN[5]).ToString();
                xePRM.SelectSingleNode("/RobotPRM/Servo/Lim6P").InnerText = (-motion.moveLimitP[5]).ToString();
                xePRM.SelectSingleNode("/RobotPRM/Servo/Lim7N").InnerText = motion.moveLimitN[6].ToString();
                xePRM.SelectSingleNode("/RobotPRM/Servo/Lim7P").InnerText = motion.moveLimitP[6].ToString();
                xePRM.SelectSingleNode("/RobotPRM/Servo/Lim8N").InnerText = motion.moveLimitN[7].ToString();
                xePRM.SelectSingleNode("/RobotPRM/Servo/Lim8P").InnerText = motion.moveLimitP[7].ToString();

                //速度和加速度
                xePRM.SelectSingleNode("/RobotPRM/Servo/ACC1").InnerText = motion.acc[0].ToString();
                xePRM.SelectSingleNode("/RobotPRM/Servo/ACC2").InnerText = motion.acc[1].ToString();
                xePRM.SelectSingleNode("/RobotPRM/Servo/ACC3").InnerText = motion.acc[2].ToString();
                xePRM.SelectSingleNode("/RobotPRM/Servo/ACC4").InnerText = motion.acc[3].ToString();
                xePRM.SelectSingleNode("/RobotPRM/Servo/ACC5").InnerText = motion.acc[4].ToString();
                xePRM.SelectSingleNode("/RobotPRM/Servo/ACC6").InnerText = motion.acc[5].ToString();
                xePRM.SelectSingleNode("/RobotPRM/Servo/ACC7").InnerText = motion.acc[6].ToString();
                xePRM.SelectSingleNode("/RobotPRM/Servo/ACC8").InnerText = motion.acc[7].ToString();
                xePRM.SelectSingleNode("/RobotPRM/Servo/Vmax1").InnerText = motion.vmax[0].ToString();
                xePRM.SelectSingleNode("/RobotPRM/Servo/Vmax2").InnerText = motion.vmax[1].ToString();
                xePRM.SelectSingleNode("/RobotPRM/Servo/Vmax3").InnerText = motion.vmax[2].ToString();
                xePRM.SelectSingleNode("/RobotPRM/Servo/Vmax4").InnerText = motion.vmax[3].ToString();
                xePRM.SelectSingleNode("/RobotPRM/Servo/Vmax5").InnerText = motion.vmax[4].ToString();
                xePRM.SelectSingleNode("/RobotPRM/Servo/Vmax6").InnerText = motion.vmax[5].ToString();
                xePRM.SelectSingleNode("/RobotPRM/Servo/Vmax7").InnerText = motion.vmax[6].ToString();
                xePRM.SelectSingleNode("/RobotPRM/Servo/Vmax8").InnerText = motion.vmax[7].ToString();

                //网络
                xePRM.SelectSingleNode("/RobotPRM/NetWork/IP").InnerText = NetIP;
                xePRM.SelectSingleNode("/RobotPRM/NetWork/Port").InnerText = NetPort;

                filePRM.Save(Var.FilePath + "Robot.PRM");

                return 0;
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// 保存机器人参数信息并立即应用
        /// 错误代码10
        /// </summary>
        private int App_Robot()
        {
            //立即应用
            try
            {
                if (motion.robotNum == 1)
                {
                    motion.handTimeOpen1 = double.Parse(textBoxReleaseTime.Text) / 1000;
                    motion.handTimeClose1 = double.Parse(textBoxCatchTime.Text) / 1000;
                    motion.handI1 = int.Parse(textBoxHandIN1.Text);
                    motion.handO1 = int.Parse(textBoxHandOUT1.Text);
                    motion.tool1.X = double.Parse(textBoxHandX.Text) / 1000;
                    motion.tool1.Z = double.Parse(textBoxHandZ.Text) / 1000;

                    motion.moveLimitN[0] = double.Parse(textBoxLimitAN.Text) / KAngle;
                    motion.moveLimitP[0] = double.Parse(textBoxLimitAP.Text) / KAngle;
                    motion.moveLimitN[1] = -double.Parse(textBoxLimitBN.Text) / KAngle;
                    motion.moveLimitP[1] = -double.Parse(textBoxLimitBP.Text) / KAngle;
                    motion.moveLimitN[2] = double.Parse(textBoxLimitCN.Text) / KAngle;
                    motion.moveLimitP[2] = double.Parse(textBoxLimitCP.Text) / KAngle;
                    motion.moveLimitN[3] = double.Parse(textBoxLimitDN.Text) / KAngle;
                    motion.moveLimitP[3] = double.Parse(textBoxLimitDP.Text) / KAngle;

                    motion.acc[0] = double.Parse(textBoxAmaxA.Text) / KAngle;
                    motion.acc[1] = double.Parse(textBoxAmaxB.Text) / KAngle;
                    motion.acc[2] = double.Parse(textBoxAmaxC.Text) / KAngle;
                    motion.acc[3] = double.Parse(textBoxAmaxD.Text) / KAngle;
                    motion.vmax[0] = double.Parse(textBoxVmaxA.Text) / KAngle;
                    motion.vmax[1] = double.Parse(textBoxVmaxB.Text) / KAngle;
                    motion.vmax[2] = double.Parse(textBoxVmaxC.Text) / KAngle;
                    motion.vmax[3] = double.Parse(textBoxVmaxD.Text) / KAngle;
                }
                else
                {
                    motion.handTimeOpen2 = double.Parse(textBoxReleaseTime.Text) / 1000;
                    motion.handTimeClose2 = double.Parse(textBoxCatchTime.Text) / 1000;
                    motion.handI2 = int.Parse(textBoxHandIN1.Text);
                    motion.handO2 = int.Parse(textBoxHandOUT1.Text);
                    motion.tool2.X = double.Parse(textBoxHandX.Text) / 1000;
                    motion.tool2.Z = double.Parse(textBoxHandZ.Text) / 1000;

                    motion.moveLimitN[4] = double.Parse(textBoxLimitAN.Text) / KAngle;
                    motion.moveLimitP[4] = double.Parse(textBoxLimitAP.Text) / KAngle;
                    motion.moveLimitN[5] = -double.Parse(textBoxLimitBN.Text) / KAngle;
                    motion.moveLimitP[5] = -double.Parse(textBoxLimitBP.Text) / KAngle;
                    motion.moveLimitN[6] = double.Parse(textBoxLimitCN.Text) / KAngle;
                    motion.moveLimitP[6] = double.Parse(textBoxLimitCP.Text) / KAngle;
                    motion.moveLimitN[7] = double.Parse(textBoxLimitDN.Text) / KAngle;
                    motion.moveLimitP[7] = double.Parse(textBoxLimitDP.Text) / KAngle;

                    motion.acc[4] = double.Parse(textBoxAmaxA.Text) / KAngle;
                    motion.acc[5] = double.Parse(textBoxAmaxB.Text) / KAngle;
                    motion.acc[6] = double.Parse(textBoxAmaxC.Text) / KAngle;
                    motion.acc[7] = double.Parse(textBoxAmaxD.Text) / KAngle;
                    motion.vmax[4] = double.Parse(textBoxVmaxA.Text) / KAngle;
                    motion.vmax[5] = double.Parse(textBoxVmaxB.Text) / KAngle;
                    motion.vmax[6] = double.Parse(textBoxVmaxC.Text) / KAngle;
                    motion.vmax[7] = double.Parse(textBoxVmaxD.Text) / KAngle;
                }
                for (int i = 0; i < 8; i++)  //限制用户输入不可大于出厂最大值
                {
                    if (motion.acc[i] > motion.MaxACC[i])
                        motion.acc[i] = motion.MaxACC[i];
                    if (motion.vmax[i] > motion.MaxSpeed[i])
                        motion.vmax[i] = motion.MaxSpeed[i];
                }

                NetIP = textBoxIP.Text;
                NetPort = textBoxPort.Text;
            }
            catch
            {
                MessageBox.Show("输入了非法参数！", "错误！");
                return -1;
            }
            if (Sv_Robot() == 0)  //保存机器人参数信息
            {
                MessageBox.Show("成功保存参数文件！", "报告");
                return 0;
            }
            else
            {
                MessageBox.Show("保存参数文件失败！", "错误");
                return -1;
            }
        }

        /// <summary>
        /// 处理命令行输入
        /// </summary>
        private void lbCmdPutin()
        {
            string temp;

            switch (lbCmd.SelectedItem.ToString())
            {
                case "NOP":
                case "END":
                case "HANDON":
                case "HANDOFF":
                case "SHIFTOFF":
                    textCMD.Text = lbCmd.SelectedItem.ToString();
                    break;

                case "MOVJ":
                case "MOVJS":
                    {
                        try
                        {
                            textCMD.Text = lbCmd.SelectedItem.ToString();
                            temp = KeyNum("请输入目标点索引(0~9999)：", indexPos.ToString("D4"), false);
                            if (int.Parse(temp) < 0 || int.Parse(temp) >= curJob.NumPos)
                            {
                                labelSysInfo.Text = "提示：当前输入值无效，请重新输入";
                                textCMD.Text += "";
                                return;
                            }
                            indexPos = int.Parse(temp) + 1;
                            textCMD.Text += " C" + temp;
                            
                            temp = KeyNum("请输入速度(0.01~1)：", defaultVJ.ToString(), false);
                            if (double.Parse(temp) < 0.01)
                            {
                                temp = "0.01";
                                labelSysInfo.Text = "提示：当前输入值超限，请确认输入";
                            }
                            else if (double.Parse(temp) > 1)
                            {
                                temp = "1";
                                labelSysInfo.Text = "提示：当前输入值超限，请确认输入";
                            }
                            defaultVJ = double.Parse(temp);
                            textCMD.Text += " V=" + temp;
                            
                            temp = KeyNum("请输入运动连续性(0/1)：", "0", false);
                            if (!(int.Parse(temp) == 0 || int.Parse(temp) == 1))
                            {
                                labelSysInfo.Text = "提示：当前输入值无效，请重新输入";
                                temp = "0";
                            }
                            textCMD.Text += " C=" + temp;
                        }
                        catch
                        {
                            labelSysInfo.Text = "提示：当前输入值无效，请重新输入";
                        }
                    }
                    break;

                case "SHIFTON":
                    {
                        try
                        {
                            textCMD.Text = "SHIFTON";
                            temp = KeyNum("请输入参考点索引(0~99)：", indexPos.ToString("D2"), false);
                            if (int.Parse(temp) < 0 || int.Parse(temp) > 99)
                            {
                                labelSysInfo.Text = "提示：当前输入值无效，请重新输入";
                                textCMD.Text += "";
                                return;
                            }
                            textCMD.Text += " P" + temp;
                        }
                        catch
                        {
                            labelSysInfo.Text = "提示：当前输入值无效，请重新输入";
                        }
                    }
                    break;

                case "DOUT":
                    {
                        try
                        {
                            textCMD.Text = "DOUT OT#";
                            temp = KeyNum("请输入端口索引(1~16)：", "1", false);
                            if (int.Parse(temp) < 1 || int.Parse(temp) > 16)
                            {
                                labelSysInfo.Text = "提示：当前输入值无效，请重新输入";
                                textCMD.Text = "";
                                return;
                            }
                            textCMD.Text += temp;

                            temp = KeyNum(@"请输入逻辑值(0/1/-，0代表OFF，1代表ON，-代表变量)：", "0", false);
                            if (temp == "-" || temp == "--")  //用变量
                            {
                                temp = KeyNum("请输入变量索引(0~99)：", "0", false);
                                if (int.Parse(temp) < 0 || int.Parse(temp) > 99)
                                {
                                    labelSysInfo.Text = "提示：当前输入值无效，请重新输入";
                                    textCMD.Text = "";
                                    return;
                                }
                                textCMD.Text += " B" + temp;
                            }
                            else if (temp == "0") textCMD.Text += " OFF";
                            else if (temp == "1") textCMD.Text += " ON";
                            else labelSysInfo.Text = "提示：当前输入值无效，请重新输入";
                        }
                        catch
                        {
                            labelSysInfo.Text = "提示：当前输入值无效，请重新输入";
                        }
                    }
                    break;

                case "DIN":
                    {
                        try
                        {
                            textCMD.Text = "DIN B";
                            temp = KeyNum("请输入变量索引(0~99)：", "0", false);
                            if (int.Parse(temp) < 0 || int.Parse(temp) > 99)
                            {
                                labelSysInfo.Text = "提示：当前输入值无效，请重新输入";
                                textCMD.Text = "";
                                return;
                            }
                            textCMD.Text += temp;

                            temp = KeyNum("请输入端口索引(1~16)：", "1", false);
                            if (int.Parse(temp) < 1 || int.Parse(temp) > 16)
                            {
                                labelSysInfo.Text = "提示：当前输入值无效，请重新输入";
                                textCMD.Text = "";
                                return;
                            }
                            textCMD.Text += " IN#" + temp;
                        }
                        catch
                        {
                            labelSysInfo.Text = "提示：当前输入值无效，请重新输入";
                        }
                    }
                    break;

                case "WAIT":
                    {
                        try
                        {
                            textCMD.Text = "WAIT";
                            temp = KeyNum("请输入端口索引(1~16，-代表纯延时)：", "1", false);
                            if ((temp != "-") && (temp != "--"))  //排除不看端口，纯等待时间情况
                            {
                                if (int.Parse(temp) < 1 || int.Parse(temp) > 16)
                                {
                                    labelSysInfo.Text = "提示：当前输入值无效，请重新输入";
                                    return;
                                }
                                textCMD.Text += " IN#" + temp;

                                temp = KeyNum(@"请输入逻辑值(0/1/-，0代表OFF，1代表ON，-代表变量)：", "0", false);
                                if (temp == "-" || temp == "--")  //用变量
                                {
                                    temp = KeyNum("请输入变量索引(0~99)：", "0", false);
                                    if (int.Parse(temp) < 0 || int.Parse(temp) > 99)
                                    {
                                        labelSysInfo.Text = "提示：当前输入值无效，请重新输入";
                                        textCMD.Text = "";
                                        return;
                                    }
                                    textCMD.Text += " B" + temp;
                                }
                                else if (temp == "0") textCMD.Text += " OFF";
                                else if (temp == "1") textCMD.Text += " ON";
                                else labelSysInfo.Text = "提示：当前输入值无效，请重新输入";
                            }
                            else
                            {
                                temp = KeyNum("请输入等待时间(秒，0代表无限)", "0", false);
                                textCMD.Text += " T#" + temp;
                            }
                        }
                        catch
                        {
                            labelSysInfo.Text = "提示：当前输入值无效，请重新输入";
                        }
                    }
                    break;

                case "SET":
                case "ADD":
                case "SUB":
                case "MUL":
                case "DIV":
                    {
                        try
                        {
                            textCMD.Text = lbCmd.SelectedItem.ToString();
                            temp = KeyNum("请输入变量1索引(0~99)：", "0", false);
                            if (int.Parse(temp) < 0 || int.Parse(temp) > 99)
                            {
                                labelSysInfo.Text = "提示：当前输入值无效，请重新输入";
                                if (int.Parse(temp) < 0)
                                    temp = "0";
                                else temp = "99";
                                return;
                            }
                            textCMD.Text += " B" + temp;
                            temp = KeyNum("请输入变量2索引(0~99，-代表立即数)：", "0", false);
                            if (temp == "-" || temp == "--")  //立即数
                            {
                                temp = KeyNum("请输入立即数：", "0", false);
                                textCMD.Text += " " + temp;
                            }
                            else
                            {
                                if (int.Parse(temp) < 0 || int.Parse(temp) > 99)
                                {
                                    labelSysInfo.Text = "提示：当前输入值无效，请重新输入";
                                    if (int.Parse(temp) < 0)
                                        temp = "0";
                                    else temp = "99";
                                    return;
                                }
                                textCMD.Text += " B" + temp;
                            }
                        }
                        catch
                        {
                            labelSysInfo.Text = "提示：当前输入值无效，请重新输入";
                        }
                    }
                    break;

                case "INC":
                case "DEC":
                case "NOT":
                    {
                        try
                        {
                            textCMD.Text = lbCmd.SelectedItem.ToString();
                            temp = KeyNum("请输入变量1索引(0~99)：", "0", false);
                            if (int.Parse(temp) < 0 || int.Parse(temp) > 99)
                            {
                                labelSysInfo.Text = "提示：当前输入值无效，请重新输入";
                                if (int.Parse(temp) < 0)
                                    temp = "0";
                                else temp = "99";
                                return;
                            }
                            textCMD.Text += " B" + temp;
                        }
                        catch
                        {
                            labelSysInfo.Text = "提示：当前输入值无效，请重新输入";
                        }
                    }
                    break;

                case "AND":
                case "OR":
                case "XOR":
                    {
                        try
                        {
                            textCMD.Text = lbCmd.SelectedItem.ToString();
                            temp = KeyNum("请输入变量1索引(0~99)：", "0", false);
                            if (int.Parse(temp) < 0 || int.Parse(temp) > 99)
                            {
                                labelSysInfo.Text = "提示：当前输入值无效，请重新输入";
                                if (int.Parse(temp) < 0)
                                    temp = "0";
                                else temp = "99";
                                return;
                            }
                            textCMD.Text += " B" + temp;
                            temp = KeyNum("请输入变量2索引(0~99)：", "0", false);
                            if (int.Parse(temp) < 0 || int.Parse(temp) > 99)
                            {
                                labelSysInfo.Text = "提示：当前输入值无效，请重新输入";
                                if (int.Parse(temp) < 0)
                                    temp = "0";
                                else temp = "99";
                                return;
                            }
                            textCMD.Text += " B" + temp;
                        }
                        catch
                        {
                            labelSysInfo.Text = "提示：当前输入值无效，请重新输入";
                        }
                    }
                    break;

                case "JNZ":
                    {
                        try
                        {
                            textCMD.Text = lbCmd.SelectedItem.ToString() + " B";
                            temp = KeyNum("请输入变量索引(0~99)：", "0", false);
                            if (int.Parse(temp) < 0 || int.Parse(temp) > 99)
                            {
                                labelSysInfo.Text = "提示：当前输入值无效，请重新输入";
                                if (int.Parse(temp) < 0)
                                    temp = "0";
                                else temp = "99";
                                return;
                            }
                            textCMD.Text += temp;
                            temp = KeyNum("请输入跳转名称代号：", "0", false);
                            textCMD.Text += " " + temp;
                        }
                        catch
                        {
                            labelSysInfo.Text = "提示：当前输入值无效，请重新输入";
                        }
                    }
                    break;

                case "JMP":
                case "*":
                    {
                        textCMD.Text = lbCmd.SelectedItem.ToString();
                        temp = KeyNum("请输入跳转名称代号：", "0", false);
                        textCMD.Text += " " + temp;
                    }
                    break;

                case "CALL":
                    {
                        try
                        {
                            if (openFileDialog.ShowDialog() == DialogResult.OK) //打开文件
                            {
                                textCMD.Text = "CALL " + openFileDialog.FileName;
                            }
                            labelSysInfo.Text = "系统提示信息";
                        }
                        catch
                        {
                            labelSysInfo.Text = "提示：打开文件失败";
                        }
                    }
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// 加载密码
        /// </summary>
        /// <returns></returns>
        private int Ld_Password()
        {

            try
            {
                XmlElement xePRM = filePRM.DocumentElement;

                //读取管理密码
                Password = xePRM.SelectSingleNode("/RobotPRM/Password").InnerText;
                return 0;
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// 保存密码
        /// </summary>
        /// <returns></returns>
        private int Sv_Password()
        {
            try
            {
                XmlElement xePRM = filePRM.DocumentElement;
                xePRM.SelectSingleNode("/RobotPRM/Password").InnerText = Password;
                filePRM.Save(Var.FilePath + "Robot.PRM");

                return 0;
            }
            catch
            {
                return -1;
            }
        }
        
        /// <summary>
        ///载入各端口注释到用户IO面板
        ///错误代码2
        /// </summary>
        private int Ld_Notation()
        {
            try
            {
                XmlElement xePRM = filePRM.DocumentElement;

                //读取各传感器注释
                tbI1.Text = xePRM.SelectSingleNode("/RobotPRM/IO/IN1").InnerText;
                tbI2.Text = xePRM.SelectSingleNode("/RobotPRM/IO/IN2").InnerText;
                tbI3.Text = xePRM.SelectSingleNode("/RobotPRM/IO/IN3").InnerText;
                tbI4.Text = xePRM.SelectSingleNode("/RobotPRM/IO/IN4").InnerText;
                tbI5.Text = xePRM.SelectSingleNode("/RobotPRM/IO/IN5").InnerText;
                tbI6.Text = xePRM.SelectSingleNode("/RobotPRM/IO/IN6").InnerText;
                tbI7.Text = xePRM.SelectSingleNode("/RobotPRM/IO/IN7").InnerText;
                tbI8.Text = xePRM.SelectSingleNode("/RobotPRM/IO/IN8").InnerText;
                tbI9.Text = xePRM.SelectSingleNode("/RobotPRM/IO/IN9").InnerText;
                tbI10.Text = xePRM.SelectSingleNode("/RobotPRM/IO/IN10").InnerText;
                tbI11.Text = xePRM.SelectSingleNode("/RobotPRM/IO/IN11").InnerText;
                tbI12.Text = xePRM.SelectSingleNode("/RobotPRM/IO/IN12").InnerText;
                tbI13.Text = xePRM.SelectSingleNode("/RobotPRM/IO/IN13").InnerText;
                tbI14.Text = xePRM.SelectSingleNode("/RobotPRM/IO/IN14").InnerText;
                tbI15.Text = xePRM.SelectSingleNode("/RobotPRM/IO/IN15").InnerText;
                tbI16.Text = xePRM.SelectSingleNode("/RobotPRM/IO/IN16").InnerText;

                tbO1.Text = xePRM.SelectSingleNode("/RobotPRM/IO/OUT1").InnerText;
                tbO2.Text = xePRM.SelectSingleNode("/RobotPRM/IO/OUT2").InnerText;
                tbO3.Text = xePRM.SelectSingleNode("/RobotPRM/IO/OUT3").InnerText;
                tbO4.Text = xePRM.SelectSingleNode("/RobotPRM/IO/OUT4").InnerText;
                tbO5.Text = xePRM.SelectSingleNode("/RobotPRM/IO/OUT5").InnerText;
                tbO6.Text = xePRM.SelectSingleNode("/RobotPRM/IO/OUT6").InnerText;
                tbO7.Text = xePRM.SelectSingleNode("/RobotPRM/IO/OUT7").InnerText;
                tbO8.Text = xePRM.SelectSingleNode("/RobotPRM/IO/OUT8").InnerText;
                tbO9.Text = xePRM.SelectSingleNode("/RobotPRM/IO/OUT9").InnerText;
                tbO10.Text = xePRM.SelectSingleNode("/RobotPRM/IO/OUT10").InnerText;
                tbO11.Text = xePRM.SelectSingleNode("/RobotPRM/IO/OUT11").InnerText;
                tbO12.Text = xePRM.SelectSingleNode("/RobotPRM/IO/OUT12").InnerText;
                tbO13.Text = xePRM.SelectSingleNode("/RobotPRM/IO/OUT13").InnerText;
                tbO14.Text = xePRM.SelectSingleNode("/RobotPRM/IO/OUT14").InnerText;
                tbO15.Text = xePRM.SelectSingleNode("/RobotPRM/IO/OUT15").InnerText;
                tbO16.Text = xePRM.SelectSingleNode("/RobotPRM/IO/OUT16").InnerText;

                return 0;
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        ///保存用户IO面板的端口注释至相应文件
        /// 错误代码12
        /// </summary>
        private int Sv_Notation()
        {
            try
            {
                XmlElement xePRM = filePRM.DocumentElement;

                xePRM.SelectSingleNode("/RobotPRM/IO/IN1").InnerText = tbI1.Text;
                xePRM.SelectSingleNode("/RobotPRM/IO/IN2").InnerText = tbI2.Text;
                xePRM.SelectSingleNode("/RobotPRM/IO/IN3").InnerText = tbI3.Text;
                xePRM.SelectSingleNode("/RobotPRM/IO/IN4").InnerText = tbI4.Text;
                xePRM.SelectSingleNode("/RobotPRM/IO/IN5").InnerText = tbI5.Text;
                xePRM.SelectSingleNode("/RobotPRM/IO/IN6").InnerText = tbI6.Text;
                xePRM.SelectSingleNode("/RobotPRM/IO/IN7").InnerText = tbI7.Text;
                xePRM.SelectSingleNode("/RobotPRM/IO/IN8").InnerText = tbI8.Text;
                xePRM.SelectSingleNode("/RobotPRM/IO/IN9").InnerText = tbI9.Text;
                xePRM.SelectSingleNode("/RobotPRM/IO/IN10").InnerText = tbI10.Text;
                xePRM.SelectSingleNode("/RobotPRM/IO/IN11").InnerText = tbI11.Text;
                xePRM.SelectSingleNode("/RobotPRM/IO/IN12").InnerText = tbI12.Text;
                xePRM.SelectSingleNode("/RobotPRM/IO/IN13").InnerText = tbI13.Text;
                xePRM.SelectSingleNode("/RobotPRM/IO/IN14").InnerText = tbI14.Text;
                xePRM.SelectSingleNode("/RobotPRM/IO/IN15").InnerText = tbI15.Text;
                xePRM.SelectSingleNode("/RobotPRM/IO/IN16").InnerText = tbI16.Text;

                xePRM.SelectSingleNode("/RobotPRM/IO/OUT1").InnerText = tbO1.Text;
                xePRM.SelectSingleNode("/RobotPRM/IO/OUT2").InnerText = tbO2.Text;
                xePRM.SelectSingleNode("/RobotPRM/IO/OUT3").InnerText = tbO3.Text;
                xePRM.SelectSingleNode("/RobotPRM/IO/OUT4").InnerText = tbO4.Text;
                xePRM.SelectSingleNode("/RobotPRM/IO/OUT5").InnerText = tbO5.Text;
                xePRM.SelectSingleNode("/RobotPRM/IO/OUT6").InnerText = tbO6.Text;
                xePRM.SelectSingleNode("/RobotPRM/IO/OUT7").InnerText = tbO7.Text;
                xePRM.SelectSingleNode("/RobotPRM/IO/OUT8").InnerText = tbO8.Text;
                xePRM.SelectSingleNode("/RobotPRM/IO/OUT9").InnerText = tbO9.Text;
                xePRM.SelectSingleNode("/RobotPRM/IO/OUT10").InnerText = tbO10.Text;
                xePRM.SelectSingleNode("/RobotPRM/IO/OUT11").InnerText = tbO11.Text;
                xePRM.SelectSingleNode("/RobotPRM/IO/OUT12").InnerText = tbO12.Text;
                xePRM.SelectSingleNode("/RobotPRM/IO/OUT13").InnerText = tbO13.Text;
                xePRM.SelectSingleNode("/RobotPRM/IO/OUT14").InnerText = tbO14.Text;
                xePRM.SelectSingleNode("/RobotPRM/IO/OUT15").InnerText = tbO15.Text;
                xePRM.SelectSingleNode("/RobotPRM/IO/OUT16").InnerText = tbO16.Text;

                filePRM.Save(Var.FilePath + "Robot.PRM");

                return 0;
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// 文件面板操作时，当内容有修改，提醒是否保存
        /// </summary>
        private bool remind()
        {
            if (modified)
            {
                DialogResult result = MessageBox.Show("要保存当前修改吗？", "保存更改吗？", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
                if (result == DialogResult.Yes)
                {
                    curJob.saveJBI(curJob.JobName);
                    modified = false;
                    return true;
                }
                else if (result == DialogResult.No)
                    return true;
                else
                    return false;
            }
            else
                return true;
        }

        /// <summary>
        /// 左机运行，编译Job文件并运行
        /// </summary>
        private void Line1()
        {
            int PC = 0;  //程序指针
            int indexP;  //记录坐标点的行索引
            int[] sPC = new int[16];  //子程序返回父程序地址（最多16层）
            string[] father = new string[16];  //父文件名
            int sPCIndex = 0;  //子程序断点索引

            MathFun.EPOStyp sftPos = new MathFun.EPOStyp();  //平移偏量
            sftPos.X = 0; sftPos.Z = 0;
            int RunMode = 1;//运行方式，若为0，则必须经过指定点；若为1-5，则光滑过渡，数值越大过渡段越大
            double TailLen = 0;  //过渡段长度

            double speedRun = 0;  //指令速度
            MathFun.JPOStyp Vo = new MathFun.JPOStyp();  //初速度
            Vo.J1 = 0; Vo.J2 = 0; Vo.J3 = 0; Vo.J4 = 0;
            MathFun.JPOStyp JPOSo = new MathFun.JPOStyp();  //初位置
            JPOSo = motion.curJPos1;
            MathFun.JPOStyp JPOS2 = new MathFun.JPOStyp();  //目标位置
            MathFun.HCOORDtyp HCoord = new MathFun.HCOORDtyp();
            double[] HV = new double[3];

            int IOport = 0;  //IO端口
            int Bnum1 = 0, Bnum2 = 0;  //变量编号
            double delay = 0;  //等待时间
            bool sfton = false;
            int LayNum = 1, LayAll = 1;
            this.Invoke((EventHandler)delegate { LayNum = int.Parse(textLStep.Text); LayAll = int.Parse(textLAll.Text); });

            while (PC < curJob.lengthProg)
            {
                this.Invoke((EventHandler)delegate { lbfile.SelectedIndex = PC; });  //高亮选择当前命令行
                if (!motion.run1) break;
                if (!motion.start1)  //如无start指令，待机
                {
                    Thread.Sleep(50);
                    continue;
                }
                string[] subCMD = lbfile.Items[PC].ToString().Split(' ');  //读入一行指令并拆为子串
                try
                {
                    switch (subCMD[0])  //处理首字串
                    {
                        case "NOP":
                            break;

                        case "MOVJ":
                            {
                                indexP = int.Parse(subCMD[1].Substring(1));  //位姿点
                                speedRun = double.Parse(subCMD[2].Substring(2));  //速度

                                RunMode = int.Parse(subCMD[3].Substring(2));  //是否连续
                                string[] subCMD2 = lbfile.Items[PC + 1].ToString().Split(' ');  //下一行
                                if ((subCMD2[0] == "MOVJ") && (RunMode != 0) && (playMode != Var.PlayMode.Step))  //连续的MOVJ运动(后续还有)
                                {
                                    if (RunMode == 1)
                                        TailLen = 0.1;
                                    else if (RunMode == 2)
                                        TailLen = 0.2;
                                    else if (RunMode == 3)
                                        TailLen = 0.3;
                                    else if (RunMode == 4)
                                        TailLen = 0.5;
                                    else
                                        TailLen = 0.2;
                                }
                                else  //不连续
                                {
                                    TailLen = 0; Vo.J1 = 0; Vo.J2 = 0; Vo.J3 = 0; Vo.J4 = 0;
                                }

                                JPOS2 = (MathFun.JPOStyp)(curJob.poses[indexP]);
                                if (sfton)  //算上偏移量
                                {
                                    HCoord = MathFun.MF_JOINT2ROBOT(JPOS2);
                                    HCoord.X += sftPos.X;
                                    HCoord.Z += sftPos.Z;
                                    MathFun.MF_ROBOT2JOINT(HCoord, ref JPOS2, 0);
                                }

                                if (MathFun.MF_MOVJ(JPOSo, JPOS2, Vo, speedRun, TailLen) != 0)
                                {
                                    this.Invoke((EventHandler)delegate { labelSysInfo.Text = "提示：运动参数错误"; });
                                }
                                else
                                {
                                    if (TailLen > 0)
                                    {
                                        JPOSo = MathFun.JPF;
                                        Vo = MathFun.JVF;
                                    }
                                    else  //如果是不连续的运动指令，等待直到运动到位
                                    {
                                        motion.movType1 = Motion.MovType.PVT;
                                        motion.moving1 = true;
                                        JPOSo = JPOS2;
                                        Vo = 0 * Vo;
                                        while (motion.moving1)
                                            Thread.Sleep(10);
                                    }
                                }
                            }
                            break;

                        case "SPEEDJ":
                            speedRun = double.Parse(subCMD[1]);
                            break;

                        case "HANDON":
                            SYSPLC.DOW = SYSPLC.DOW | (0x01 << (motion.handO1 - 1));  //手爪吸合
                            Thread.Sleep((int)(motion.handTimeOpen1 * 1000));  //固定时间
                            break;

                        case "HANDOFF":
                            SYSPLC.DOW = SYSPLC.DOW & (~(0x01 << (motion.handO1 - 1)));   //手爪打开
                            Thread.Sleep((int)(motion.handTimeClose1 * 1000));  //固定时间
                            break;

                        case "SFTON":
                            sfton = true;
                            sftPos.X = motion.shiftPos1.X * (LayNum - 1);
                            sftPos.Z = motion.shiftPos1.Z * (LayNum - 1);
                            break;

                        case "SFTOFF":
                            sfton = false;
                            break;

                        case "DOUT":
                            {
                                IOport = int.Parse(subCMD[1].Substring(3));  //端口
                                if (subCMD[2].StartsWith("B"))  //变量输出
                                {
                                    Bnum1 = int.Parse(subCMD[2].Substring(1));
                                    if (B[Bnum1] == 0)
                                        SYSPLC.DOW = SYSPLC.DOW & (~(0x01 << (IOport - 1)));
                                    else
                                        SYSPLC.DOW = SYSPLC.DOW | (0x01 << (IOport - 1));
                                }
                                else  //指定输出
                                {
                                    if (subCMD[2] == "OFF")
                                        SYSPLC.DOW = SYSPLC.DOW & (~(0x01 << (IOport - 1)));
                                    else if (subCMD[2] == "ON")
                                        SYSPLC.DOW = SYSPLC.DOW | (0x01 << (IOport - 1));
                                }
                            }
                            break;

                        case "DIN":
                            {
                                Bnum1 = int.Parse(subCMD[1].Substring(1));
                                IOport = int.Parse(subCMD[2].Substring(3));  //端口

                                if ((SYSPLC.DI & (0x01 << (IOport - 1))) != 0)
                                    B[Bnum1] = 1;
                                else
                                    B[Bnum1] = 0;
                            }
                            break;

                        case "WAIT":
                            {
                                if (subCMD[1].StartsWith("I"))  //等待端口
                                {
                                    IOport = int.Parse(subCMD[1].Substring(3));  //端口
                                    //delay = 1000 * double.Parse(subCMD[3].Substring(2));  //时间ms
                                    bool Condition = false;
                                    int timer = 0;
                                    int InPort;

                                    while (!Condition)//&& (timer < delay)
                                    {
                                        InPort = SYSPLC.DI & ((0x01 << (IOport - 1)));
                                        if (((subCMD[2] == "OFF") && (InPort == 0)) || ((subCMD[2] == "ON") && (InPort != 0)))
                                            Condition = true;
                                        else
                                            Condition = false;
                                        if (!Condition)
                                        {
                                            Thread.Sleep(20);
                                            timer += 20;
                                        }
                                    }
                                }
                                else if (subCMD[1].StartsWith("T"))  //纯等待时间
                                {
                                    double tim = double.Parse(subCMD[1].Substring(2));
                                    delay = 1000 * double.Parse(subCMD[1].Substring(2));  //输入为s转化为ms

                                    Thread.Sleep((int)delay);
                                    if (delay == 0)
                                    {
                                        while (true)
                                        {
                                            this.Invoke((EventHandler)delegate { labelSysInfo.Text = "提示：当前无限等待时间，停止运动可退出"; });
                                            Thread.Sleep(100);
                                            if (!motion.run1)
                                            {
                                                this.Invoke((EventHandler)delegate { labelSysInfo.Text = "系统提示信息"; });
                                                break;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        this.Invoke((EventHandler)delegate { labelSysInfo.Text = "提示：延时等待" + tim + "s"; });
                                        Thread.Sleep((int)delay);
                                        this.Invoke((EventHandler)delegate { labelSysInfo.Text = "系统提示信息"; });
                                    }
                                }
                            }
                            break;

                        case "SET":
                            {
                                Bnum1 = int.Parse(subCMD[1].Substring(1));

                                if (subCMD[1].StartsWith("B"))
                                {
                                    if (!subCMD[2].StartsWith("B"))
                                        B[Bnum1] = int.Parse(subCMD[2]);
                                    else
                                    {
                                        Bnum2 = int.Parse(subCMD[2].Substring(1));
                                        B[Bnum1] = B[Bnum2];
                                    }
                                }

                                if (Bnum1 == 0)  //B[0]表示层数
                                {
                                    this.Invoke((EventHandler)delegate { LayAll = B[Bnum1]; textLAll.Text = LayAll.ToString(); });
                                }
                            }
                            break;

                        case "ADD":  //暂不支持
                            break;

                        case "SUB":  //暂不支持
                            break;

                        case "INC":  //暂不支持
                            break;

                        case "DEC":  //仅用做层计数
                            Bnum1 = int.Parse(subCMD[1].Substring(1));
                            B[Bnum1]--;
                            if (Bnum1 == 0)
                            {
                                LayNum++;
                                if (LayNum > LayAll)
                                    LayNum = 1;
                                this.Invoke((EventHandler)delegate { textLStep.Text = LayNum.ToString(); });
                            }
                            break;

                        case "AND":  //暂不支持
                            break;

                        case "OR":  //暂不支持
                            break;

                        case "NOT":  //暂不支持
                            break;

                        case "XOR":  //暂不支持
                            break;

                        case "JNZ":
                            {
                                while (motion.moving1)
                                    Thread.Sleep(10);  //等待运动完成动作

                                Bnum1 = int.Parse(subCMD[1].Substring(1));
                                if (B[Bnum1] != 0)
                                {
                                    PC = lbfile.Items.IndexOf(subCMD[2]);  //指向匹配项
                                }
                            }
                            break;

                        case "JMP":
                            {
                                while (motion.moving1)
                                    Thread.Sleep(10);//等待运动完成动作

                                PC = lbfile.Items.IndexOf("* " + subCMD[1]);  //指向匹配项
                                break;
                            }

                        case "*":
                            break;

                        case "CALL":
                            {
                                sPC[sPCIndex] = PC;  //保留断点返回地址
                                father[sPCIndex] = curJob.JobName;
                                sPCIndex++;
                                curJob.loadJBI(lbfile.Items[PC].ToString().Substring(5));
                                this.Invoke((EventHandler)delegate { lbfile.Items.Clear(); });
                                for (int i = 0; i < curJob.lengthProg; i++)
                                {
                                    this.Invoke((EventHandler)delegate { lbfile.Items.Add(curJob.textLines[i].ToString()); });
                                }
                                PC = 0;
                                CTtemp = 0;
                                lbfile.SelectedIndex = 0;
                            }
                            break;

                        case "END":
                            {
                                if (sPCIndex != 0)  //子程序返回
                                {
                                    sPCIndex--;
                                    curJob.loadJBI(father[sPCIndex]);
                                    this.Invoke((EventHandler)delegate { lbfile.Items.Clear(); });
                                    for (int i = 0; i < curJob.lengthProg; i++)
                                    {
                                        this.Invoke((EventHandler)delegate { lbfile.Items.Add(curJob.textLines[i].ToString()); });
                                    }
                                    PC = sPC[sPCIndex];

                                    this.Invoke((EventHandler)delegate { lbfile.SelectedIndex = PC; });  //高亮选择当前命令行
                                }
                                else  //主程序返回
                                {
                                    if (playMode != Var.PlayMode.Auto)
                                    {
                                        PC = curJob.lengthProg;  //遇到END程序结束
                                        motion.start1 = false;
                                        motion.run1 = false;
                                        SYSPLC.TB_green = false;
                                    }
                                    else
                                        PC = -1;  //如自动循环模式则从头再来
                                }
                            }
                            break;

                        default:
                            break;
                    }
                }
                catch
                {
                    motion.run1 = false;
                    motion.start1 = false;
                    this.Invoke((EventHandler)delegate { labelSysInfo.Text = "提示：存在非法指令,检查后重新运行"; });
                    motion.threadLine1.Abort();
                    return;
                }
                PC++;
                Thread.Sleep(10);
                if (playMode == Var.PlayMode.Step) motion.start1 = false;  //若是单步，循环一次即停
            }
        }

        /// <summary>
        /// 读取绝对编码器 SerialPort.Read
        /// </summary>
        /// <param name="encData"></param>
        /// <returns></returns>
        public int ReadEncode(ref double[] encData)
        {

            if (!SerialPort.IsOpen)
            {
                try { SerialPort.Open(); }
                catch { }
            }

            if (!SerialPort.IsOpen)
            {
                MessageBox.Show("无法打开串口", "错误！");
                return -1;
            }

            byte[] CMD1 = { 0x01, 0x40, 0x00, 0x06, 0x20, 0x00, 0x20, 0x02, 0xDB, 0xC2 };  //设定模式
            byte[] CMD2 = { 0x01, 0x40, 0x00, 0x06, 0x20, 0x01, 0x00, 0x01, 0xD3, 0xC3 };  //锁定数值
            byte[] CMD3 = { 0x01, 0x40, 0x00, 0x03, 0xE6, 0x00, 0x00, 0x03, 0xF3, 0x4A };  //读取输出字串
            byte[] CRC1 = { 0xDB, 0xC2, 0x9B, 0xD7, 0x5A, 0x1B, 0x1B, 0xFD, 0xDA, 0x31, 0x9A, 0x24, 0x5B, 0xE8, 0x1B, 0xA8 };  //不同轴的校验码（1-8轴）
            byte[] CRC2 = { 0xD3, 0xC3, 0x93, 0xD6, 0x52, 0x1A, 0x13, 0xFC, 0xD2, 0x30, 0x92, 0x25, 0x53, 0xE9, 0x13, 0xA9 };  //不同轴的校验码（1-8轴）
            byte[] CRC3 = { 0xF3, 0x4A, 0xB3, 0x5F, 0x72, 0x93, 0x33, 0x75, 0xF2, 0xB9, 0xB2, 0xAC, 0x73, 0x60, 0x33, 0x20 };  //不同轴的校验码（1-8轴）
            byte[] data = new byte[14];
            long[] encPulse = new long[8];
            long encPT;
            int count;


            for (int i = 1; i <= 4; i++)
            {
                CMD1[0] = (byte)i; CMD2[0] = (byte)i; CMD3[0] = (byte)i;
                CMD1[8] = CRC1[i * 2 - 2]; CMD1[9] = CRC1[i * 2 - 1];
                CMD2[8] = CRC2[i * 2 - 2]; CMD2[9] = CRC2[i * 2 - 1];
                CMD3[8] = CRC3[i * 2 - 2]; CMD3[9] = CRC3[i * 2 - 1];

                for (int j = 0; j < 2; j++)  //读两次对比
                {
                    try
                    {
                        //发出指令，读取反馈
                        SerialPort.Write(CMD1, 0, CMD1.Length);  //设置模式
                        System.Threading.Thread.Sleep(200);
                        SerialPort.Read(data, 0, 14);
                        SerialPort.Write(CMD2, 0, CMD2.Length);  //执行锁定
                        System.Threading.Thread.Sleep(200);
                        SerialPort.Read(data, 0, 14);
                        SerialPort.Write(CMD3, 0, CMD3.Length);  //读取数据
                        System.Threading.Thread.Sleep(200);
                        SerialPort.Read(data, 0, 14);
                    }
                    catch  //出错了
                    {
                        SerialPort.Close();
                        return -1;
                    }
                    count = data[6]; count = count<<8; count += data[7];
                    encPT = count * 16777216 + (data[8]<<8) + data[9] + (data[10]<<24) + (data[11]<<16);
                    if (j > 0)  //与上次读数比较，差别较大则退出
                        if (Math.Abs(encPT - encPulse[i - 1]) > 10000)
                        {
                            SerialPort.Close();
                            return -1;
                        }
                    encPulse[i - 1] = encPT;
                }
                encData[i - 1] = encPulse[i - 1] / 1677.7216;  //绝对编码器1圈1048576码，相对编码器10000码
            }

            SerialPort.Close();
            return 0;
        }

        /// <summary>
        /// 响应示教盒手动速度UP
        /// </summary>
        private void SpeedUp()
        {
            if (manuSpeed == Var.ManuSpeed.Inc)
            {
                manuSpeed = Var.ManuSpeed.Low;
                motion.speedJ = Var.speedL;
                motion.speedL = Var.speedL * motion.MaxSpeedL;
                picSpeed.Image = Properties.Resources.SPEEDLOW;

                labelSysInfo.Text = "提示：低速";
            }
            else if (manuSpeed == Var.ManuSpeed.Low)
            {
                manuSpeed = Var.ManuSpeed.Middle;
                motion.speedJ = Var.speedM;
                motion.speedL = Var.speedM * motion.MaxSpeedL;
                picSpeed.Image = Properties.Resources.SPEEDHIGH;

                labelSysInfo.Text = "提示：中速";
            }
            else if (manuSpeed == Var.ManuSpeed.Middle)
            {
                manuSpeed = Var.ManuSpeed.High;
                motion.speedJ = Var.speedH;
                motion.speedL = Var.speedH * motion.MaxSpeedL;
                picSpeed.Image = Properties.Resources.SPEEDMAX;

                labelSysInfo.Text = "提示：高速";
            }
        }

        /// <summary>
        /// 响应示教盒手动速度Down
        /// </summary>
        private void SpeedDown()
        {
            if (manuSpeed == Var.ManuSpeed.High)
            {
                manuSpeed = Var.ManuSpeed.Middle;
                motion.speedJ = Var.speedM;
                motion.speedL = Var.speedM * motion.MaxSpeedL;
                picSpeed.Image = Properties.Resources.SPEEDHIGH;

                labelSysInfo.Text = "提示：中速";
            }
            else if (manuSpeed == Var.ManuSpeed.Middle)
            {
                manuSpeed = Var.ManuSpeed.Low;
                motion.speedJ = Var.speedL;
                motion.speedL = Var.speedL * motion.MaxSpeedL;
                picSpeed.Image = Properties.Resources.SPEEDLOW;

                labelSysInfo.Text = "提示：低速";
            }
            else if (manuSpeed == Var.ManuSpeed.Low)
            {
                manuSpeed = Var.ManuSpeed.Inc;
                motion.speedJ = Var.speedI;
                motion.speedL = Var.speedI * motion.MaxSpeedL;
                picSpeed.Image = Properties.Resources.SPEEDINC;

                labelSysInfo.Text = "提示：微动";
            }
        }

    }
}