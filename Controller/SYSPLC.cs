using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Controller
{
    public class SYSPLC
    {
        //公有成员==============================================================================================================
        public static uint T_PLC = 100;  //PLC采样周期
        //5种输入信号寄存器
        public static int DI = 0;
        public static int LLimit = 0;
        public static int RLimit = 0;
        public static int Home = 0;
        public static int Alarm = 0;

        //3种输出信号寄存器
        public static int DOR = 0, DOW = 0;  //DO状态和DO写寄存器
        public static int ServR = 0, ServW = 0;  //DO状态和DO写寄存器
        public static int ClsR = 0, ClsW = 0;  //DO状态和DO写寄存器

        //示教器的变量
        public static ushort Tbox;
        public static int TboxIOR;  //从低到高分别为：手动使能、开始、暂停、急停状态、模式（01示教，10远程，11回放）
        public static int TboxIOW;  //从低到高分别为：开始灯，暂停灯，蜂鸣（无），伺服准备，删除，插入，修改，命令一览，外部轴，机器人组，取消限制

        //重要标志位
        public static int Mod = 1; //运行模式，1示教，2远程，3再现
        public static bool Start = false;  //Start按钮状态
        public static bool Hold = false;  //Hold按钮状态
        public static bool ServPermit = false;  //允许示教伺服on（三段开关）

        public static bool ServoR = false, oServoR = false, ServoW = false; //急停的状态（读）和命令(输出)，oServoR用于指示前时刻状态，识别上升沿（发生急停）
        public static bool Manu = true; //手动/自动标志
        public static bool StartFace = false;  //StartFace用于识别触摸屏Start
        public static bool EmgStop1 = false, EmgStop2 = false;  //左右独立的急停信号
        public static bool BrakeOff = false;  //独立的刹车信号（防止上电掉电“点头”）
        public static Mutex SCAN = new Mutex();  //正在扫描端口的标志

        //端口映射变量
        public static bool[] IN = new bool[16];
        public static bool[] OTR = new bool[16];
        public static bool[] OTW = new bool[16];
        public static bool Cls0, Cls1, Cls2, Cls3;

        //示教盒输入输出映射变量
        //手动使能、开始、暂停、急停状态、模式（01示教，10远程，11回放）
        public static bool TB_deadman = true, TB_start, TB_hold, TB_emergency, TB_teach = false, TB_play = true, TB_remote;
        //开始灯，暂停灯，蜂鸣（无），伺服准备，删除，插入，修改，命令一览，外部轴，机器人组，取消限制
        public static bool TB_green, TB_white, TB_buzzer, TB_servoonready, TB_delete, TB_insert, TB_modify, TB_informlist, TB_extraaxis = false, TB_robotgroup = false, TB_releasestrict;

        //PLC主定时器定义=======================================================================================================

        //委托声明
        private delegate void TIMECALLBACK(uint wTimerID, uint msg, uint dwUser, uint dw1, uint dw2);
        //定义委托事件
        private static event TIMECALLBACK timeCallBack;
        //定义句柄
        private static uint mEvent;

        //导入多媒体定时器函数  
        [DllImport("\\windows\\mmtimer.dll")]
        private static extern uint timeBeginPeriod(uint uPeriod);
        [DllImport("\\windows\\mmtimer.dll")]
        private static extern uint timeEndPeriod(uint uPeriod);
        [DllImport("\\windows\\mmtimer.dll")]
        private static extern uint timeSetEvent(uint uDelay,
            uint uResolution,
            TIMECALLBACK lpFunction,
            uint dwUser,
            uint uFlags);
        [DllImport("\\windows\\mmtimer.dll")]
        private static extern uint timeKillEvent(uint uTimerID);

        //建立事件关联
        public static void RC_Build()
        {
            timeCallBack += new TIMECALLBACK(SysPLC);
        }

        //开定时器  
        public static void RC_Open()
        {
            timeBeginPeriod(1);
            mEvent = timeSetEvent(T_PLC, 1, timeCallBack, 0, 1);  //设置多媒体定时器
        }

        //关定时器
        public static void RC_Shut()
        {
            if (mEvent != 0)
            {
                timeEndPeriod(1);
                timeKillEvent(mEvent);
            }
        }

        //私有成员==============================================================================================================
        //多媒体定时器回调函数，PLC处理主循环

        //端口映射到各变量
        private static void MapI()
        {
            UserIOR();
            TboxIN();
        }

        //各变量映射到端口
        private static void MapO()
        {
            UserIOW();
            TboxOT();
        }

        //PLC程序主体  
        private static void Process()
        {
            if (IN[0]) EmgStop1 = false;
            else EmgStop1 = true;  //急停为常闭，所以有输入时未按急停

            ServoW = true;
            BrakeOff = false;

            if (ServoW) OTW[3] = true;  //上伺服
            else OTW[3] = false;

            if (BrakeOff) OTW[1] = true;  //刹车
            else OTW[1] = false;


            //if (TB_emergency) //按急停或者欠压后先刹车后掉伺服
            //{
            //    //BrakeOff = false;

            //    //if (BrakeOff) DOW = DOW | 0x0002;
            //    //else DOW = DOW & 0xfffd;

            //    //if (Motion.GTS.WaitOne(5, false))
            //    //{
            //    //    Gts.GT_SetDo(Gts.MC_GPO, ~DOW);  //普通DO
            //    //}
            //    //else
            //    //{
            //    //    if (Motion.GTS.WaitOne(5, false))
            //    //        Gts.GT_SetDo(Gts.MC_GPO, ~DOW);  //普通DO
            //    //}
            //    //Motion.GTS.ReleaseMutex();  //释放固高资源

            //    ServoW = true;
            //    short rtn = 1;
            //    //立即停止运动。
            //    while (true)  //无视资源占用标志，直接尝试关闭，如失败2ms后再试
            //    {
            //        if (rtn != 0)
            //        {
            //            rtn = Gts.GT_Stop(0xff, 0xff);//先停运动
            //        }

            //        if (rtn == 0)
            //        {
            //            Motion.RC_ServoOff(0x3f);
            //            break;
            //        }
            //    }
            //}

            if (TB_teach && !TB_deadman)
            {
                //if (axiSts1+axiSts2+axiSts3+axiSts4!=0)
                if (TB_servoonready)
                    Motion.RC_ServoOff(0x3f);
            }
        }

        //回调函数，与TIMECALLBACK委托参数一致
        private static void SysPLC(uint wTimerID, uint msg, uint dwUser, uint dw1, uint dw2)
        {
            int rtn = 0;
            //锁定固高资源，5ms不能获得则返回
            if (!Motion.GTS.WaitOne(5, false)) return;
            //锁定寄存器，5ms不能获得则返回
            if (!SCAN.WaitOne(5, false))
            {
                Motion.GTS.ReleaseMutex();
                return;
            }

            //读取所有IO端口，注意！固高是负逻辑（0为on，1为off），此处取反变为正逻辑
            rtn = Gts.GT_GetDi(Gts.MC_LIMIT_NEGATIVE, out LLimit);//读取左限位
            LLimit = ~LLimit & 0xffff;
            if (rtn != 0)
                LLimit = ~LLimit & 0xffff;

            rtn = Gts.GT_GetDi(Gts.MC_LIMIT_POSITIVE, out RLimit);//读取右限位

            RLimit = ~RLimit & 0xffff;
            if (rtn != 0)
                RLimit = ~RLimit & 0xffff;

            rtn = Gts.GT_GetDi(Gts.MC_ALARM, out Alarm);//读取驱动器报警
            Alarm = Alarm & 0xffff;
            if (rtn != 0)
            { }

            rtn = Gts.GT_GetDi(Gts.MC_HOME, out Home);//读取原点
            Home = ~Home & 0xffff;
            if (rtn != 0)
                Home = ~Home & 0xffff;

            rtn = Gts.GT_GetDi(Gts.MC_GPI, out DI);//读取通用输入
            DI = ~DI & 0xffff;
            if (rtn != 0)
                DI = ~DI & 0xffff;

            rtn = Gts.GT_GetDo(Gts.MC_ENABLE, out ServR);//读取伺服使能输出
            ServR = ServR & 0xffff;
            if (rtn != 0)
            { }

            rtn = Gts.GT_GetDo(Gts.MC_CLEAR, out ClsR);//读取报警复位输出
            ClsR = ~ClsR & 0xffff;
            if (rtn != 0)
                ClsR = ~ClsR & 0xffff;

            rtn = Gts.GT_GetDo(Gts.MC_GPO, out DOR);//读取通用输出
            DOR = ~DOR & 0xffff;
            if (rtn != 0)
                DOR = ~DOR & 0xffff;

            rtn = Gts.GT_GetExtIoValue(0, out Tbox, (byte)7);
            //rtn = Gts.GT_GetVal_eHMI((short)0, out Tbox);
            //rtn = Gts.GT_GetSts_eHMI(out Tbox);

            TboxIOR = Tbox;
            SCAN.ReleaseMutex();  //释放扫描寄存器
            Motion.GTS.ReleaseMutex();  //释放固高资源

            //映射到各变量
            //读取示教盒IO输入
            MapI();

            //处理逻辑关系
            Process();

            //映射到端口
            //读取示教盒IO输出
            MapO();

            //锁定固高资源，5ms不能获得则返回
            if (!Motion.GTS.WaitOne(5, false)) return;
            //锁定寄存器，5ms不能获得则返回
            if (!SCAN.WaitOne(5, false))
            {
                Motion.GTS.ReleaseMutex();
                return;
            }

            Gts.GT_SetDo(Gts.MC_GPO, ~DOW);  //普通DO

            if (Cls0) Gts.GT_ClrSts(1, 1);  //报警状态清除
            if (Cls1) Gts.GT_ClrSts(2, 1);
            if (Cls2) Gts.GT_ClrSts(3, 1);
            if (Cls3) Gts.GT_ClrSts(4, 1);

            Gts.GT_SetExtIoValue(0, (ushort)TboxIOW, (byte)7);  //示教盒指示灯

            SCAN.ReleaseMutex();  //释放扫描寄存器
            Motion.GTS.ReleaseMutex();  //释放固高资源
        }


        //映射示教盒IO输入
        private static void TboxIN()
        {
            if ((TboxIOR & 0x01) != 0)  //三段使能开关
                TB_deadman = true;
            else
                TB_deadman = false;

            if ((TboxIOR & 0x02) != 0)  //Start按钮
                TB_start = true;
            else
                TB_start = false;

            if ((TboxIOR & 0x04) != 0)  //Hold按钮
                TB_hold = true;
            else
                TB_hold = false;

            if ((TboxIOR & 0x08) != 0)  //示教盒急停按钮被按下 急停为常闭
                TB_emergency = false;
            else
                TB_emergency = true;

            if ((TboxIOR & 0x0010) == 0x10 && (TboxIOR & 0x0020) != 0x20)  //三段旋钮1 示教
            {
                TB_remote = false; TB_play = false; TB_teach = true;
            }
            else if ((TboxIOR & 0x0010) == 0x10 && (TboxIOR & 0x0020) == 0x20)  //三段旋钮2 远程
            {
                TB_teach = false; TB_remote = false; TB_play = true;
            }
            else if ((TboxIOR & 0x0010) != 0x10 && (TboxIOR & 0x0020) == 0x20)  //三段旋钮3 play
            {
                TB_teach = false; TB_play = false; TB_remote = true;
            }
            TB_deadman = true;

            return;
        }

        //映射示教盒IO输出 TB_green, TB_white, TB_buzzer, TB_servoonready, TB_delete, TB_insert, TB_modify, TB_informlist, TB_extraaxis, TB_robotgroup, TB_releasestrict
        private static void TboxOT()
        {
            if (TB_green)  //运行灯
                TboxIOW = TboxIOW | 0x01;  //点亮运行灯
            else
                TboxIOW = TboxIOW & ~0x01;  //关闭运行灯;

            if (TB_white)  //暂停灯
                TboxIOW = TboxIOW | 0x02;  //点亮暂停灯
            else
                TboxIOW = TboxIOW & ~0x02;  //关闭暂停灯

            if (TB_servoonready)  //伺服准备灯
                TboxIOW = TboxIOW | 0x08;  //点亮伺服准备灯
            else
                TboxIOW = TboxIOW & ~0x08;  //关闭暂停灯

            if (TB_delete)  //删除灯
                TboxIOW = TboxIOW | 0x10;  //点亮删除灯
            else
                TboxIOW = TboxIOW & ~0x10;  //关闭删除灯

            if (TB_insert)  //插入灯
                TboxIOW = TboxIOW | 0x20;  //点亮插入灯
            else
                TboxIOW = TboxIOW & ~0x20;  //关闭插入灯

            if (TB_modify)  //修改灯
                TboxIOW = TboxIOW | 0x40;  //点亮修改灯
            else
                TboxIOW = TboxIOW & ~0x40;  //关闭修改灯

            if (TB_informlist)  //命令一览灯 
                TboxIOW = TboxIOW | 0x80;  //点亮命令一览灯
            else
                TboxIOW = TboxIOW & ~0x80;  //关闭命令一览灯

            if (TB_extraaxis)  //外部轴灯
                TboxIOW = TboxIOW | 0x100;  //点亮外部轴灯
            else
                TboxIOW = TboxIOW & ~0x100;  //关闭外部轴灯

            if (TB_robotgroup)  //机器人组灯
                TboxIOW = TboxIOW | 0x200;  //点亮机器人组灯
            else
                TboxIOW = TboxIOW & ~0x200;  //关闭机器人组灯

            if (TB_releasestrict)  //取消限制灯
                TboxIOW = TboxIOW | 0x400;  //点亮取消限制灯
            else
                TboxIOW = TboxIOW & ~0x400;  //关闭取消限制灯

            return;
        }

        //端口映射到各变量
        private static void UserIOR()
        {
            for (int i = 0; i < 16; i++)
            {
                if ((DI & (0x0001 << i)) == 0) IN[i] = false;
                else IN[i] = true;
            }

            for (int i = 0; i < 16; i++)
            {
                if ((DOR & (0x0001 << i)) == 0) OTR[i] = false;
                else OTR[i] = true;
            }
        }

        //各变量映射到端口
        private static void UserIOW()
        {
            for (int i = 0; i < 16; i++)
            {
                if (OTW[i]) DOW = DOW | (0x0001 << i);
                else DOW = DOW & ~(0x0001 << i);
            }
        }
    }
}
