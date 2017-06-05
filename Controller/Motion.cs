using System;
using System.Text;
using System.Threading;

namespace Controller
{
    public class Motion
    {
        //====================================================变量=======================================================
        # region

        public const string GtsConfig = @"\Hard Disk\GlassRobot\GTS800.cfg";
        public string TBXConfig = @"\Hard Disk\GlassRobot\ExtMdlCfg.txt";

        public MathFun.COORDtyp coord = MathFun.COORDtyp.Joint;  //当前坐标系(手动和显示用)
        public double[] moveLimitN = new double[8];  //软限位数据(国际标准单位)
        public double[] moveLimitP = new double[8];  //软限位数据(国际标准单位)
        public MathFun.EPOStyp dirPos = new MathFun.EPOStyp();  //手动直线的方向
        public int robotNum = 1;  //手动时机器人对象编号

        //位置（左机）
        public MathFun.JPOStyp curJPos1 = new MathFun.JPOStyp();  //当前位置
        public MathFun.JPOStyp teachJPos1 = new MathFun.JPOStyp();  //当前示教点
        public MathFun.EPOStyp shiftPos1 = new MathFun.EPOStyp();  //层叠平移的方向
        public MathFun.EPOStyp tool1 = new MathFun.EPOStyp();  //手爪1参数

        //位置（右机）
        public MathFun.JPOStyp curJPos2 = new MathFun.JPOStyp();  //当前位置
        public MathFun.JPOStyp teachJPos2 = new MathFun.JPOStyp();  //当前示教点
        public MathFun.EPOStyp shiftPos2 = new MathFun.EPOStyp();  //层叠平移的方向
        public MathFun.EPOStyp tool2 = new MathFun.EPOStyp();  //手爪2参数

        public MathFun.HCOORDtyp tool = new MathFun.HCOORDtyp();  //当前工具坐标

        //速度（手动）
        public double speedJ = 0.1;  //手动关节速度%
        public double speedL = 0.05;  //手动直线速度m/s
        public double[] JogSpeed = { 0, 0, 0, 0 };  //Jog运动速度：脉冲/ms };

        //各关节最大转速（减速后，出厂最上限）
        public double[] MaxSpeed = { 0.849078, 1.114040, 2.073659, 2.596357, 0.849078, 1.114040, 2.073659, 2.596357 };  //最大转速：弧度/s };
        public double MaxSpeedL = 1.5;  //最大线速度
        public double MaxSpeedA = 1.5;  //最大角速度

        //各关节最大加速度（减速后，出厂最上限）
        public double[] MaxACC = { 1.132104, 1.67106, 3.1104885, 3.4618093, 1.132104, 1.67106, 3.1104885, 3.4618093 };
        public double MaxACCL = 1;  //最大线加速度
        public double MaxACCA = 1;  //最大角加速度

        //电机轴状态
        public static int[] axisStatus = new int[8];

        //运动错误
        public int errorMotion = 0;
        public int errorIO = 0;

        //PID与误差带 //速度前馈？加速度，积分微分饱和极限
        public Gts.TPid[] pid = new Gts.TPid[8];  //8轴PID参数
        public int[] band = { 1000, 1000, 1000, 1000, 1000, 1000, 1000, 1000 };  //8轴误差带

        //IO
        public bool teachLock = false; //示教锁定（不允许切换到自动，保证安全）
        public bool moving1 = false; //指示开始运动（左）
        public bool start1 = false;  //程序单步启动标志
        public bool moving2 = false; //指示开始运动（右）
        public bool start2 = false;  //程序单步启动标志
        public bool movingD = false; //指示开始运动（双）
        public bool startD = false;  //程序单步启动标志

        public enum MovType { None, PVT, JOG, ManuLine };  //运动方式，None运动执行中或空闲(不接受新指令），PVT规划运动，JOG，ManuLine手动直线（目标不定）
        public MovType movType1 = MovType.None;  //指示运动类别
        public MovType movType2 = MovType.None;  //指示运动类别
        public MovType movTypeD = MovType.None;  //指示运动类别

        //PT运动标志
        public bool PTmark = false;  //PT运动标志，mark=0空闲，mark=1正在执行PT运动
        public double[] ptStart = new double[4];  //PT起始点
        public int time = 0;  //PT时间
        public int dT = 100;  //PT周期（毫秒）

        //线程
        private Thread threadMotion; //用于运动插补的线程
        public static Mutex GTS = new Mutex();  //固高资源占用标志（防止不同线程同时调用固高硬件资源）

        #endregion

        //================================================应用变量=======================================================
        #region

        //手爪
        public double handTimeOpen1 = 0;  //机器人气动手爪开启时间
        public double handTimeClose1 = 0;  //机器人气动手爪闭合时间
        public double handTimeOpen2 = 0;  //机器人气动手爪开启时间
        public double handTimeClose2 = 0;  //机器人气动手爪闭合时间
        public int handI1 = 0;  //手爪1输入端口
        public int handO1 = 0;  //手爪1输出端口
        public int handI2 = 0;  //手爪2输入端口
        public int handO2 = 0;  //手爪2输出端口

        //伺服参数
        public double[] acc = { 0, 0, 0, 0, 0, 0, 0, 0 };  //加速度(用户输入的)
        public double[] vmax = { 0, 0, 0, 0, 0, 0, 0, 0 };  //最大速度(用户输入的)

        //运行标志
        public bool run1 = false;  //动作线1正在运行
        public bool run2 = false;  //动作线2正在运行
        public Thread threadLine1;  //动作线1的线程
        public Thread threadLine2;  //动作线2的线程
        public Thread threadLineD;  //动作线D的线程

        #endregion

        //================================================方法（外部接口）===============================================

        //初始化控制器硬件
        public int RC_IniRobot()
        {
            short rtn = 0;  //函数返回值

            //打开控制器
            rtn = Gts.GT_Open(0, 1);
            if (rtn != 0)
                return 1;

            //复位
            rtn = Gts.GT_Reset();
            if (rtn != 0)
                return 2;

            //配置运动控制器
            byte[] tp = Encoding.ASCII.GetBytes(GtsConfig);
            rtn = Gts.GT_LoadConfig(tp);
            if (rtn != 0)
                return 3;

            //清除8个轴的报警
            rtn = Gts.GT_ClrSts(1, 8);
            if (rtn != 0)
                return 4;

            //设定8个轴的限位
            rtn = RC_SetLimit(false);
            if (rtn != 0)
                return 5;

            //加载PID参数
            rtn = RC_SetPID(pid);
            if (rtn != 0)
                return 6;

            //设定误差带
            rtn = RC_SetBand(band);
            if (rtn == -1)
                return 7;

            //设定示教器IO
            rtn = Gts.GT_SetModuleType(1);
            rtn += Gts.GT_OpenExtMdl(null);
            tp = Encoding.ASCII.GetBytes(TBXConfig);
            rtn += Gts.GT_LoadExtConfig(tp);
            if (rtn != 0)
                return 8;

            //建立PLC定时器
            SYSPLC.RC_Build();
            SYSPLC.RC_Open();

            //打开用于插补的线程
            if (threadMotion == null)  //如果线程不存在，则建立并运行
            {
                threadMotion = new Thread(new ThreadStart(intertask));
                threadMotion.Priority = ThreadPriority.Highest;
                threadMotion.IsBackground = true;
            }
            threadMotion.Start();

            return 0;
        }

        //关闭运动控制器(错误代码15)
        public int RC_Close()
        {
            //停止插补线程
            if (threadMotion != null) threadMotion.Abort();
            SYSPLC.RC_Shut();
            Gts.GT_CloseExtMdl();  //关示教盒IO

            if (Gts.GT_Close() != 0)
            {
                errorMotion = 15;
                return 1;
            }
            else
                return 0;
        }

        // 设置软限位(错误代码5)
        public short RC_SetLimit(bool release)
        {
            short i = 0;
            short rtn = 0;

            if (!GTS.WaitOne(20, false)) return -1;  //获取硬件资源，超时异常退出

            if (release)  //解除软限位
            {
                for (i = 1; i <= 8; i++)
                    rtn += Gts.GT_SetSoftLimit(i, 0x7fffffff, -0x7fffffff);
                Gts.GT_ClrSts(1, 4);  //清除限位状态标志
            }
            else
                for (i = 1; i <= 8; i++)
                    rtn += Gts.GT_SetSoftLimit(i, (int)((moveLimitP[i - 1] - MathFun.HJ[i - 1]) * MathFun.KJ[i - 1]), (int)((moveLimitN[i - 1] - MathFun.HJ[i - 1]) * MathFun.KJ[i - 1]));

            GTS.ReleaseMutex();
            if (rtn != 0)
                errorMotion = 5;
            return rtn;
        }

        //设置绝对位置(错误代码10)  //GT_SetPrfPos??
        public short RC_SetPos(double[] data)
        {
            short rtn = 0;

            if (!GTS.WaitOne(20, false)) return -1;

            for (short i = 1; i <= 8; i++)
            {
                rtn += Gts.GT_SetEncPos(i, (int)(data[i - 1]));  //设置位置
                rtn += Gts.GT_SetPrfPos(i, (int)(data[i - 1]));  //设置规划
            }

            if (rtn == 0) rtn += Gts.GT_SynchAxisPos(0xff);  //与轴同步关联

            GTS.ReleaseMutex();
            if (rtn != 0)
                errorMotion = 10;
            return rtn;
        }

        // 运动停止(错误代码11)
        public int RC_MovStop(bool em)
        {
            int option;
            if (em) option = 0xff;  //快速停止
            else option = 0;  //平滑停止

            int tryTime = 500;
            while (tryTime > 0)  //无视资源占用标志，直接尝试关闭，如失败2ms后再试，最多500次（约1秒）
            {
                if (Gts.GT_Stop(0xff, option) == 0)
                    break;
                Thread.Sleep(2);
                tryTime--;
            }

            if (tryTime < 1)  //耗尽时间结束，失败。强制掉伺服，刹车
            {
                SYSPLC.BrakeOff = false;
                SYSPLC.ServoW = false;
            }

            moving1 = false;  //运动标志
            movType1 = MovType.None;  //运动类型
            moving2 = false;  //运动标志
            movType2 = MovType.None;  //运动类型
            movingD = false;  //运动标志
            movTypeD = MovType.None;  //运动类型

            for (int i = 0; i < 4; i++)
                JogSpeed[i] = 0;
            
            return 0;
        }

        // 运动复位：使规划位置和实际相等（放弃未完成规划）(错误代码12)
        public int RC_Clear()
        {
            short rtn = 0;
            double Pos;
            uint pClock;

            if (!GTS.WaitOne(20, false)) return -1;

            for (short i = 1; i <= 8; i++)
            {
                rtn += Gts.GT_GetEncPos(i, out Pos, 1, out  pClock);
                rtn += Gts.GT_SetPos(i, (int)(Pos));
            }
            if (rtn == 0) rtn += Gts.GT_SynchAxisPos(0xff);  //与轴同步关联

            GTS.ReleaseMutex();
            if (rtn != 0)
                errorMotion = 12;
            return rtn;
        }

        //清除报警 起始axis，轴数count（错误代码4）
        public void RC_ClrSts(short axis, short count)
        {
            short rtn = Gts.GT_ClrSts(axis, count);
            if (rtn != 0)
                errorMotion = 4;

        }

        // 系统上伺服(错误代码13)
        public int RC_ServoOn(short mask)
        {
            short rtn = 0;

            if (!GTS.WaitOne(20, false)) return -1;

            RC_Clear();

            for (short i = 1; i <= 8; i++)
            {
                if ((mask & (0x01 << (i - 1))) != 0)
                    rtn += Gts.GT_AxisOn(i);
            }

            GTS.ReleaseMutex();

            Thread.Sleep(200);
            if (rtn == 0)
            {
                SYSPLC.BrakeOff = true; //有额外刹车控制的情况，用于防止上电点头现象
            }
            else
                errorMotion = 13;
            return rtn;
        }

        // 系统伺服断电（正常应该在停止状态使用）(错误代码14)
        public static int RC_ServoOff(short mask)
        {
            short rtn = 0;

            SYSPLC.BrakeOff = false;  //先行刹车，防止掉电点头现象
            Thread.Sleep(300);

            if (!GTS.WaitOne(20, false)) return -1;

            for (short i = 1; i <= 4; i++)
                if ((mask & (0x01 << (i - 1))) != 0) rtn += Gts.GT_AxisOff(i);

            GTS.ReleaseMutex();

            return rtn;
        }

        //手动按键操作
        public int RC_ManuMov()
        {
            if (dirPos.C == MathFun.COORDtyp.Joint)  //关节坐标，因为2机器人同构，几何参数相同
            {
                //设置目标速度
                if (dirPos.X == -1)
                    JogSpeed[0] = -MaxSpeed[0] * MathFun.KJ[0] * speedJ / 1000;
                else if (dirPos.X == 1)
                    JogSpeed[0] = MaxSpeed[0] * MathFun.KJ[0] * speedJ / 1000;
                if (dirPos.Y == -1)
                    JogSpeed[1] = -MaxSpeed[1] * MathFun.KJ[1] * speedJ / 1000;
                else if (dirPos.Y == 1)
                    JogSpeed[1] = MaxSpeed[1] * MathFun.KJ[1] * speedJ / 1000;
                if (dirPos.Z == -1)
                    JogSpeed[2] = -MaxSpeed[2] * MathFun.KJ[2] * speedJ / 1000;
                else if (dirPos.Z == 1)
                    JogSpeed[2] = MaxSpeed[2] * MathFun.KJ[2] * speedJ / 1000;
                if (dirPos.THETA == -1)
                    JogSpeed[3] = -MaxSpeed[3] * MathFun.KJ[3] * speedJ / 1000;
                else if (dirPos.THETA == 1)
                    JogSpeed[3] = MaxSpeed[3] * MathFun.KJ[3] * speedJ / 1000;

                //if (dirPos.THETA == -1)
                //    JogSpeed[2] = -MaxSpeed[2] * MathFun.KJ[2] * speedJ / 1000;
                //else if (dirPos.THETA == 1)
                //    JogSpeed[2] = MaxSpeed[2] * MathFun.KJ[2] * speedJ / 1000;
                //if (dirPos.PSI == -1)
                //    JogSpeed[3] = -MaxSpeed[3] * MathFun.KJ[3] * speedJ / 1000;
                //else if (dirPos.PSI == 1)
                //    JogSpeed[3] = MaxSpeed[3] * MathFun.KJ[3] * speedJ / 1000;

                for (short i = 1; i <= 8; i++)  //规划清零
                {
                    Gts.GT_SetPrfPos(i, 0);
                }
                if (robotNum == 1)
                {
                    movType1 = MovType.JOG;  //运动
                    moving1 = true;
                }
                else if (robotNum == 2)
                {
                    movType2 = MovType.JOG;  //运动
                    moving2 = true;
                }
            }
            else  //直角坐标系
            {

                for (short i = 1; i <= 8; i++)//规划清零
                {
                    Gts.GT_SetPrfPos(i, 0);
                }
                if (robotNum == 1)
                {
                    movType1 = MovType.ManuLine;  //运动
                    moving1 = true;
                }
                else if (robotNum == 2)
                {
                    movType2 = MovType.ManuLine;  //运动
                    moving2 = true;
                }
            }

            return 0;
        }

        //================================================公共方法（外部接口）结束=====================================================


        //====================================================私有字段定义========================================================

        //写PID参数(错误代码6)
        private short RC_SetPID(Gts.TPid[] pid)
        {
            short rtn = 0;
            for (int i = 0; i < 8; i++)
            {
                pid[i].derivativeLimit = 30000;
                pid[i].integralLimit = 30000;
                pid[i].limit = 30000;
                pid[i].ki = 0;
                pid[i].kd = 0;
                pid[i].kvff = 0;
                pid[i].kaff = 0;
            }
            pid[0].kp = 1.0;  //此处设置增益比例
            pid[1].kp = 1.0;
            pid[2].kp = 1.0;
            pid[3].kp = 1.0;
            pid[4].kp = 1.0;  //此处设置增益比例
            pid[5].kp = 1.0;
            pid[6].kp = 1.0;
            pid[7].kp = 1.0;

            if (!GTS.WaitOne(20, false)) return -1;
            for (short i = 1; i <= 8; i++) rtn += Gts.GT_SetPid(i, 1, ref pid[i - 1]);
            GTS.ReleaseMutex();
            if (rtn != 0)
                errorMotion = 6;
            return rtn;
        }

        //设定误差带(错误代码7)
        private short RC_SetBand(int[] band)
        {
            short rtn = 0;
            if (!GTS.WaitOne(20, false)) return -1;
            for (short i = 1; i <= 8; i++) rtn += Gts.GT_SetAxisBand(i, band[i - 1], 4);
            GTS.ReleaseMutex();
            if (rtn != 0)
                errorMotion = 7;
            return rtn;
        }

        //读取位姿（8个关节角度）(错误代码16)
        private int RC_GetPoint()
        {
            short rtn = 0;  //函数返回值
            double[] Enc = new double[8];
            uint Clock;

            if (!GTS.WaitOne(20, false)) return -1;
            rtn = Gts.GT_GetEncPos(1, out Enc[0], 8, out Clock);
            GTS.ReleaseMutex();

            if (rtn != 0) return 1;
            else
            {
                curJPos1.J1 = Enc[0] / MathFun.KJ[0] + MathFun.HJ[0];
                curJPos1.J2 = Enc[1] / MathFun.KJ[1] + MathFun.HJ[1];
                curJPos1.J3 = Enc[2] / MathFun.KJ[2] + MathFun.HJ[2];
                curJPos1.J4 = Enc[3] / MathFun.KJ[3] + MathFun.HJ[3];
                curJPos2.J1 = Enc[4] / MathFun.KJ[4] + MathFun.HJ[4];
                curJPos2.J2 = Enc[5] / MathFun.KJ[5] + MathFun.HJ[5];
                curJPos2.J3 = Enc[6] / MathFun.KJ[6] + MathFun.HJ[6];
                curJPos2.J4 = Enc[7] / MathFun.KJ[7] + MathFun.HJ[7];
            }

            return 0;
        }

        //读取状态(错误代码17) //状态字？？？
        private int RC_ReadSts(ref int[] axisStatus)
        {
            short rtn;
            uint Clock;

            if (!GTS.WaitOne(20, false)) return -1;
            rtn = Gts.GT_GetSts(1, out axisStatus[0], 8, out Clock);
            GTS.ReleaseMutex();
            if (rtn != 0)
                errorMotion = 17;
            return rtn;
        }

        //用于运动控制的独立线程
        private void intertask()
        {
            int rtn = 0;
            while (true)
            {
                if (RC_GetPoint() != 0)  //刷新当前位置
                    errorMotion = 16;
                if (RC_ReadSts(ref axisStatus) != 0)
                    errorMotion = 17;
                //if (SYSPLC.TB_emergency || SYSPLC.EmgStop1)  //左机或者双机急停
                //{
                //    moving1 = false; movingD = false;
                //    movType1 = MovType.None; movTypeD = MovType.None;
                //    if (threadLine1 != null)  //如果程序线程存在，则杀掉
                //    {
                //        Gts.GT_Stop(0x0f, 0x0f);
                //        threadLine1.Abort();
                //        threadLine1 = null;
                //    }
                //    else if (threadLineD != null)  //如果程序线程存在，则杀掉
                //    {
                //        Gts.GT_Stop(0xff, 0xff);
                //        threadLineD.Abort();
                //        threadLineD = null;
                //    }
                //}
                //if (SYSPLC.TB_emergency || SYSPLC.EmgStop2)  //右机或者双机急停
                //{
                //    moving2 = false; movingD = false;
                //    movType2 = MovType.None; movTypeD = MovType.None;
                //    if (threadLine2 != null)  //如果程序线程存在，则杀掉
                //    {
                //        Gts.GT_Stop(0xf0, 0xf0);
                //        threadLine2.Abort();
                //        threadLine2 = null;
                //    }
                //    else if (threadLineD != null)  //如果程序线程存在，则杀掉
                //    {
                //        Gts.GT_Stop(0xff, 0xff);
                //        threadLineD.Abort();
                //        threadLineD = null;
                //    }
                //}

                if (movingD)  //双机运动
                {
                    //if (movTypeD == MovType.PT)
                    //{
                    //    rtn = RC_MovL(3);
                    //    if (rtn != 0)
                    //        errorMotion = 20;
                    //}

                    //else if (RC_ReadSts(ref axisStatus) == 0)  //刷新当前状态
                    //{   //判断运动是否到位
                    //    if (((axisStatus[0] & 0x0400) == 0) && ((axisStatus[1] & 0x0400) == 0) && ((axisStatus[2] & 0x0400) == 0) && ((axisStatus[3] & 0x0400) == 0))
                    //        movingD = false;
                    //    //判断各轴状态是否正常
                    //    if (((axisStatus[0] & 0x012) != 0) || ((axisStatus[1] & 0x012) != 0) || ((axisStatus[2] & 0x012) != 0) || ((axisStatus[3] & 0x012) != 0))
                    //    {
                    //        movingD = false;
                    //        //停止插补线程
                    //        if (threadLine1 != null)  //如果程序线程存在，则杀掉
                    //        {
                    //            Gts.GT_Stop(0x0f, 0x0f);
                    //            threadLine1.Abort();
                    //            threadLine1 = null;
                    //        }

                    //        if (((axisStatus[0] & 0x002) != 0) || ((axisStatus[1] & 0x002) != 0) || ((axisStatus[2] & 0x002) != 0) || ((axisStatus[3] & 0x002) != 0))
                    //            errorMotion = 21;//伺服报警
                    //        if (((axisStatus[0] & 0x010) != 0) || ((axisStatus[1] & 0x010) != 0) || ((axisStatus[2] & 0x010) != 0) || ((axisStatus[3] & 0x010) != 0))
                    //            errorMotion = 22;//跟随误差越限
                    //    }
                    //}
                }
                else
                {
                    if (moving1)  //左机运动
                    {
                        if (movType1 == MovType.PVT)
                        {
                            rtn = RC_Mov(1);
                            if (rtn != 0)
                                errorMotion = 20;
                        }
                        else if (movType1 == MovType.JOG)
                        {
                            rtn = RC_JOG(1);
                            if (rtn != 0)
                                errorMotion = 19;
                        }
                        else if (movType1 == MovType.ManuLine)
                        {
                            rtn = RC_IMovL(1);
                            if (rtn != 0)
                                errorMotion = 19;
                        }
                    }

                    //if (moving2)  //右机运动
                    //{
                    //    if (movType2 == MovType.PT)
                    //    {
                    //        rtn = RC_MovL(2);
                    //        if (rtn != 0)
                    //            errorMotion = 20;
                    //    }
                    //    else if (movType2 == MovType.JOG)
                    //    {
                    //        rtn = RC_JOG(2);
                    //        if (rtn != 0)
                    //            errorMotion = 19;
                    //    }
                    //    else if (movType2 == MovType.ManuLine)
                    //    {
                    //        rtn = RC_IMovL(2);
                    //        if (rtn != 0)
                    //            errorMotion = 19;
                    //    }
                    //}
                }

                if (moving1 || moving2 || movingD)  //如果存在运动，刷新状态
                {
                    if (RC_ReadSts(ref axisStatus) == 0)  //如果成功刷新当前状态
                    {   //判断运动是否到位
                        if (((axisStatus[0] & 0x0400) == 0) && ((axisStatus[1] & 0x0400) == 0) && ((axisStatus[2] & 0x0400) == 0) && ((axisStatus[3] & 0x0400) == 0))
                        { moving1 = false; PTmark = false; movType1 = MovType.None; }
                        //if (((axisStatus[4] & 0x0400) == 0) && ((axisStatus[5] & 0x0400) == 0) && ((axisStatus[6] & 0x0400) == 0) && ((axisStatus[7] & 0x0400) == 0))
                        //{ moving2 = false; PTmark = false; }
                        //if (((axisStatus[0] & 0x0400) == 0) && ((axisStatus[1] & 0x0400) == 0) && ((axisStatus[2] & 0x0400) == 0) && ((axisStatus[3] & 0x0400) == 0) && ((axisStatus[4] & 0x0400) == 0) && ((axisStatus[5] & 0x0400) == 0) && ((axisStatus[6] & 0x0400) == 0) && ((axisStatus[7] & 0x0400) == 0))
                        //{ movingD = false; PTmark = false; }
                        
                        ////判断各轴状态是否正常
                        //if (((axisStatus[0] & 0x012) != 0) || ((axisStatus[1] & 0x012) != 0) || ((axisStatus[2] & 0x012) != 0) || ((axisStatus[3] & 0x012) != 0))
                        //{
                        //    moving1 = false; movingD = false;
                        //}
                        //if (((axisStatus[4] & 0x012) != 0) || ((axisStatus[5] & 0x012) != 0) || ((axisStatus[6] & 0x012) != 0) || ((axisStatus[7] & 0x012) != 0))
                        //{
                        //    moving2 = false; movingD = false;
                        //}
                        //if (((axisStatus[0] & 0x002) != 0) || ((axisStatus[1] & 0x002) != 0) || ((axisStatus[2] & 0x002) != 0) || ((axisStatus[3] & 0x002) != 0) || ((axisStatus[4] & 0x002) != 0) || ((axisStatus[5] & 0x002) != 0) || ((axisStatus[6] & 0x002) != 0) || ((axisStatus[7] & 0x002) != 0))
                        //    errorMotion = 21;  //伺服报警
                        //if (((axisStatus[0] & 0x010) != 0) || ((axisStatus[1] & 0x010) != 0) || ((axisStatus[2] & 0x010) != 0) || ((axisStatus[3] & 0x010) != 0) || ((axisStatus[4] & 0x010) != 0) || ((axisStatus[5] & 0x010) != 0) || ((axisStatus[6] & 0x010) != 0) || ((axisStatus[7] & 0x010) != 0))
                        //    errorMotion = 22;  //跟随误差越限
                    }
                }


                Thread.Sleep(20);  //线程休眠20毫秒
            }
        }

        //执行规划好的PVT运动(错误代码20)
        private int RC_Mov(int mark)
        {
            short rtn = 0;
            short i = 0;
            int Step = 0;

            double[] Timetemp = new double[1024];
            double[] Axistemp = new double[1024];
            double[] Percent = new double[1024];

            if (!GTS.WaitOne(50, false)) return -1;

            //设置运动方式PVT
            for (i = 1; i <= 4; i++) rtn += Gts.GT_PrfPvt(i);
            if (rtn != 0)
            {
                GTS.ReleaseMutex();
                return 1;
            }

            GTS.ReleaseMutex();

            //设置PVT数据
            for (i = 1; i <= 4; i++)
            {
                Step = MathFun.MF_SORTPVT(i-1);
                for (int j = 0; j < Step; j++)
                {
                    Timetemp[j] = MathFun.Ttab[i-1, j] * 1000;  //毫秒单位
                    Axistemp[j] = (MathFun.Ptab[i-1, j] - MathFun.HJ[i-1]) * MathFun.KJ[i-1];  //转为绝对编码器脉冲
                    Percent[j] = 100;
                }
                if (!GTS.WaitOne(20, false))
                    return -2;
                rtn += Gts.GT_PvtTablePercent(i, Step, ref Timetemp[0], ref Axistemp[0], ref Percent[0], 0);
                GTS.ReleaseMutex();
            }
            MathFun.PVTIndex = 0;
            
            
            if (rtn != 0)
            {
                return 2;
            }

            if (!GTS.WaitOne(20, false)) return -3;

            //设置轴号和循环
            for (i = 1; i <= 4; i++)
            {
                rtn += Gts.GT_PvtTableSelect(i, i);
                rtn += Gts.GT_SetPvtLoop(i, 1);
            }
            if (rtn != 0)
            {
                GTS.ReleaseMutex();
                return 3;
            }

            //启动运动
            rtn += Gts.GT_PvtStart(0x0f);

            GTS.ReleaseMutex();

            if (rtn == 0)
            {
                moving1 = true;
                movType1 = MovType.None;
                Thread.Sleep(20);  //阻塞20ms以免到位信号尚未解除
            }
            else
                return 4;

            return 0;
        }

        //执行JOG运动(错误代码19)
        private int RC_JOG(int mark)
        {
            short rtn = 0;
            Gts.TJogPrm jog = new Gts.TJogPrm();  //Jog参数结构体变量
            double VM = 0;
            short start, end;
            int axis;

            if (mark == 1)
            { start = 1; end = 4; axis = 0x0f; }
            else
            { start = 5; end = 8; axis = 0xf0; }


            for (short i = 0; i < 4; i++)  //速度中最大的
                if (VM < Math.Abs(JogSpeed[i])) VM = Math.Abs(JogSpeed[i]);

            if (!GTS.WaitOne(20, false)) return -1;

            //设置运动参数
            for (short i = start; i <= end; i++)
            {
                rtn += Gts.GT_PrfJog(i);
                // 读取Jog运动参数
                rtn += Gts.GT_GetJogPrm(i, out jog);
                // 设置Jog运动参数
                if (Math.Abs(JogSpeed[i - 1]) < 0.000001) //如果速度为0则加速度为默认（注：加减速度不允许设为0）
                    jog.acc = jog.dec = 0.0625;
                else
                    jog.acc = jog.dec = 0.0625 * Math.Abs(JogSpeed[i - 1]) / VM;

                Gts.GT_SetJogPrm(i, ref jog);
                // 设置Jog运动速度
                rtn += Gts.GT_SetVel(i, JogSpeed[i - 1]);
            }
            if (rtn != 0)
            {
                GTS.ReleaseMutex();
                return 2;
            }
            
            //启动运动
            rtn += Gts.GT_Update(axis);
            GTS.ReleaseMutex();

            if (rtn == 0)
            {
                if (mark == 1)
                    movType1 = MovType.None;
                else
                    movType2 = MovType.None;
            }
            return 0;
        }

        //手动直线运动操作 //RC_JOG返回值？
        private int RC_IMovL(int mark)
        {
            //基于JOG的方法
            //计算各轴速度======================================
            MathFun.JPOStyp jPos = new MathFun.JPOStyp();
            double[] rate = new double[4];

            if (RC_JOG(mark) == 0)
                MathFun.curJPOS = curJPos1;
            else
                MathFun.curJPOS = curJPos2;

            if (MathFun.MF_IMOVL(curJPos1, dirPos, speedL, ref jPos) != 0)  //注意jPos是增量，对应1mm的移动
            {
                moving1 = false;
                movType1 = MovType.None;
                RC_MovStop(false);
                return -1;
            }
            rate[0] = jPos.J1; rate[1] = jPos.J2; rate[2] = jPos.J3; rate[3] = jPos.J4;
            for (int i = 0; i < 4; i++)
                JogSpeed[i] = rate[i] * MathFun.KJ[i] * speedL;

            //启动Jog
            if (RC_JOG(mark) == 0)
            {
                if (mark == 1)
                    movType1 = MovType.ManuLine;  //保留下次进入manuLine，以便及时刷新方向
                else
                    movType2 = MovType.ManuLine;  //保留下次进入manuLine，以便及时刷新方向
            }

            return 0;
        }

    }
}
