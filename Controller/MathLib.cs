using System;
using System.Runtime.InteropServices;

namespace Controller
{
    /// <summary>
    /// 用于封装MathFun函数的类
    /// </summary>
    public class MathFun
    {
        public enum COORDtyp { Joint, Robot, World };  //枚举坐标类型（标明位置数据所属坐标系，Joint关节坐标，Robot是机器人坐标，是所有运动学基础坐标。World世界坐标。）

        public struct JPOStyp  //关节坐标
        {
            public double J1;
            public double J2;
            public double J3;
            public double J4;

            //重载“*”，（按比例缩放）
            public static JPOStyp operator *(double k, JPOStyp JPos)
            {
                JPOStyp temp = new JPOStyp();
                temp.J1 = k * JPos.J1;
                temp.J2 = k * JPos.J2;
                temp.J3 = k * JPos.J3;
                temp.J4 = k * JPos.J4;

                return temp;
            }

            //重载“-”，（按比例缩放）
            public static JPOStyp operator -(JPOStyp JPos1, JPOStyp JPos2)
            {
                JPOStyp temp = new JPOStyp();
                temp.J1 = JPos1.J1 - JPos2.J1;
                temp.J2 = JPos1.J2 - JPos2.J2;
                temp.J3 = JPos1.J3 - JPos2.J3;
                temp.J4 = JPos1.J4 - JPos2.J4;

                return temp;
            }

            public static double[] JPOS2Array(JPOStyp JPOS)
            {
                double[] J = new double[4];

                J[0] = JPOS.J1;
                J[1] = JPOS.J2;
                J[2] = JPOS.J3;
                J[3] = JPOS.J4;
                return J;
            }
        }

        public struct EPOStyp  //直角坐标（欧拉）
        {
            public COORDtyp C;  //坐标系
            public double X;
            public double Y;
            public double Z;//3个位置
            public double PHI;
            public double THETA;
            public double PSI;//3个姿态（Z-Y-Z欧拉角）
            
            //重载“*”，（按比例缩放）
            public static EPOStyp operator *(double k, EPOStyp EPos)
            {
                EPOStyp temp = new EPOStyp();
                temp.C = EPos.C;
                temp.X = k * EPos.X;
                temp.Y = k * EPos.Y;
                temp.Z = k * EPos.Z;
                temp.PHI = k * EPos.PHI;
                temp.THETA = k * EPos.THETA;
                temp.PSI = k * EPos.PSI;

                return temp;
            }
        }

        public struct HCOORDtyp  //直角坐标或变换矩阵（齐次）
        {
            public COORDtyp C;
            public double X;
            public double Y;
            public double Z;
            public double R11;
            public double R12;
            public double R13;
            public double R21;
            public double R22;
            public double R23;
            public double R31;
            public double R32;
            public double R33;

            //重载“*”，（矩阵乘法）
            public static HCOORDtyp operator *(HCOORDtyp firHCoord, HCOORDtyp secHCoord)
            {
                HCOORDtyp temp = new HCOORDtyp();
                temp.C = firHCoord.C;
                temp.R11 = firHCoord.R11 * secHCoord.R11 + firHCoord.R12 * secHCoord.R21 + firHCoord.R13 * secHCoord.R31;
                temp.R12 = firHCoord.R11 * secHCoord.R12 + firHCoord.R12 * secHCoord.R22 + firHCoord.R13 * secHCoord.R32;
                temp.R13 = firHCoord.R11 * secHCoord.R13 + firHCoord.R12 * secHCoord.R23 + firHCoord.R13 * secHCoord.R33;

                temp.R21 = firHCoord.R21 * secHCoord.R11 + firHCoord.R22 * secHCoord.R21 + firHCoord.R23 * secHCoord.R31;
                temp.R22 = firHCoord.R21 * secHCoord.R12 + firHCoord.R22 * secHCoord.R22 + firHCoord.R23 * secHCoord.R32;
                temp.R23 = firHCoord.R21 * secHCoord.R13 + firHCoord.R22 * secHCoord.R23 + firHCoord.R23 * secHCoord.R33;

                temp.R31 = firHCoord.R31 * secHCoord.R11 + firHCoord.R32 * secHCoord.R21 + firHCoord.R33 * secHCoord.R31;
                temp.R32 = firHCoord.R31 * secHCoord.R12 + firHCoord.R32 * secHCoord.R22 + firHCoord.R33 * secHCoord.R32;
                temp.R33 = firHCoord.R31 * secHCoord.R13 + firHCoord.R32 * secHCoord.R23 + firHCoord.R33 * secHCoord.R33;

                temp.X = firHCoord.R11 * secHCoord.X + firHCoord.R12 * secHCoord.Y + firHCoord.R13 * secHCoord.Z + firHCoord.X;
                temp.Y = firHCoord.R21 * secHCoord.X + firHCoord.R22 * secHCoord.Y + firHCoord.R23 * secHCoord.Z + firHCoord.Y;
                temp.Z = firHCoord.R31 * secHCoord.X + firHCoord.R32 * secHCoord.Y + firHCoord.R33 * secHCoord.Z + firHCoord.Z;

                return temp;
            }

            //齐次坐标求逆
            public static HCOORDtyp inv(HCOORDtyp HCoord)
            {
                //由于齐次坐标矩阵的特殊性，所以求逆可以采用更简单的方式
                //A(1,2,3;1,2,3)构成单位正交矩阵，而A矩阵的第4行为[0 0 0 1]所以可以用比较简单的方式求逆。
                HCOORDtyp temp = new HCOORDtyp();
                temp.C = HCoord.C;

                //旋转分量是单位正交阵，转置即求逆
                temp.R11 = HCoord.R11; temp.R12 = HCoord.R21; temp.R13 = HCoord.R31;
                temp.R21 = HCoord.R12; temp.R22 = HCoord.R22; temp.R23 = HCoord.R32;
                temp.R31 = HCoord.R13; temp.R32 = HCoord.R23; temp.R33 = HCoord.R33;

                //平移分量
                temp.X = -(HCoord.R11 * HCoord.X + HCoord.R21 * HCoord.Y + HCoord.R31 * HCoord.Z);
                temp.Y = -(HCoord.R12 * HCoord.X + HCoord.R22 * HCoord.Y + HCoord.R32 * HCoord.Z);
                temp.Z = -(HCoord.R13 * HCoord.X + HCoord.R23 * HCoord.Y + HCoord.R33 * HCoord.Z);

                return temp;
            }
        }

        /* 重要参数 */
        //脉冲当量的倒数，即某关节每弧度脉冲数（或者直线关节每米脉冲数），电机编码器每弧度脉冲数 x 减速器减速比
        public static double[] KJ = { 5000 * 159 / Math.PI, -5000 * 158 / Math.PI, 5000 * 141 / Math.PI, 5000 * 141 / Math.PI, 5000 * 159 / Math.PI, -5000 * 158 / Math.PI, 5000 * 141 / Math.PI, 5000 * 141 / Math.PI };
        
        //原点偏移量，即机器人停在真正机械原点时的编码器读数/（-KJ），//读取：关节角 = 码盘/KJ + HJ;  输出：码盘 = (关节角 - HJ) * KJ;
        public static double[] HJ = { Math.PI / 2, 0, 0, 0, 0, 0, 0, 0 };

        //工具坐标变换矩阵
        public static HCOORDtyp TOOL = new HCOORDtyp();

        //各关节最大转速（减速后）
        private static double[] Vmax = { 1, 1, 1, 1 };  //最大转速：弧度/s

        //各关节最大加速度
        private static double[] Amax = { 1, 1, 1, 1 };

        //机械结构参数
        private static double[] a = { 1.35, 0.55, 0, 0, 0, 0 };
        private static double[] alf = { 0, 0, Math.PI / 2, 0, 0, 0 };
        private static double[] d = { 0, 0, 0, 0.27, 0, 0 };
        
        //参考点和速度
        public static JPOStyp curJPOS = new JPOStyp();  //用于反解的参考位置，一般是上次反解值，或者当前值（外部通过函数设置）
        public static JPOStyp JVF = new JPOStyp();  //上次规划的末端速度
        public static JPOStyp JPF = new JPOStyp();  //上次规划的末端位姿

        //PVT运动数据表，单张表最大插补步数1024步，超出则需要交替使用（仅发生在大量MOVJS点情况）
        //本模块计算填满PVT表后会等待运动线程将其读取清空，然后才从开头处继续填写
        public static double[,] Ptab = new double[4, 1024];
        public static double[,] Ttab = new double[4, 1024];
        public static int PVTbusy = 0;  //PVT占用标志（=1不许其他线程读写）
        public static int PVTready = 0;  //PVT表就绪
        public static int PVTIndex = 0;  //PVT表索引
        
        /* 数学函数 */
        
        //齐次坐标绕y轴旋转
        private static HCOORDtyp roty(HCOORDtyp POS, double Ang)
        {
            //POS原位姿，Ang旋转角度
            HCOORDtyp trans;
            trans.C = POS.C;
            trans.R11 = Math.Cos(Ang);
            trans.R12 = 0;
            trans.R13 = -Math.Sin(Ang);
            trans.R21 = 0;
            trans.R22 = 1;
            trans.R23 = 0;
            trans.R31 = Math.Sin(Ang);
            trans.R32 = 0;
            trans.R33 = Math.Cos(Ang);
            trans.X = 0;
            trans.Y = 0;
            trans.Z = 0;

            return (POS * trans);
        }

        //已知两端点速度，距离，最大速度加速度，标准的5段梯形速度规划 //拐点？？？
        private static int PLANV(double S, double Vo, double Vf, double A, double V, ref double[] t, ref double[] s)
        {
            //S距离，Vo/Vf端点速度，V最大速度、A加速度。t[0]加速段时间、t[1]匀速段时间、t[2]减速段时间
            //结果返回给定速度、加速度条件下的运动时间和位移

            double S1, S3, Sf, Vr, Tf, t1, t2, t3;

            Tf = Math.Abs((Vf - Vo) / A);  //由速度决定的最小运动时间（直接用最大加速度，起点至终点的时间）
            Sf = (Vo + Vf) * Tf / 2;  //对应位移

            if (S < Sf)  //拐点在Vo、Vf下方
            {
                V = -V; A = -A;
            }

            t1 = (V - Vo) / A;  //加速时间
            t3 = (V - Vf) / A;  //减速时间
            S1 = (V + Vo) * t1 / 2;  //加速段位移
            S3 = (V + Vf) * t3 / 2;  //减速段位移

            if (((S < (S1 + S3)) && (V >= 0)) || ((S > (S1 + S3)) && (V < 0)))   //距离太短，存在加速不足问题，三角形速度
            {
                Vr = Math.Sqrt(A * S + Vo * Vo / 2 + Vf * Vf / 2);  //实际中间速度
                if ((V > 0) && (Vo < 0) && (Vf < 0) && (S < 0) && (Vr < -Vo) && (Vr < -Vf))  //Vo,Vf,S均小于零，而V大于零的情况，Vr不必为正
                    Vr = -Vr;
                else if ((V < 0) && (Vo > 0) && (Vf > 0) && (S > 0) && (Vr < Vo) && (Vr < Vf))  //Vo,Vf,S均大于零，而V小于零的情况，Vr不必为负
                    Vr = Vr * 1;
                else  //Vr取V同符号
                    Vr = Math.Sign(V) * Vr;

                t1 = Math.Abs((Vr - Vo) / A);
                t3 = Math.Abs((Vr - Vf) / A);
                t2 = 0;
                S1 = Vo * t1 + (Vr - Vo) * t1 / 2;
                S3 = Vf * t2 + (Vr - Vf) * t3 / 2;
            }
            else  //梯形速度
            {
                Vr = V;
                t2 = Math.Abs((S - S1 - S3) / V);

            }
            t[0] = t1; t[1] = t2; t[2] = t3;
            s[0] = S1; s[1] = S - S1 - S3; s[2] = S3;

            return 0;
        }

        //初始化一个单位阵
        public static HCOORDtyp MF_HCOORDINIT()
        {
            HCOORDtyp HCOORD = new HCOORDtyp();
            HCOORD.C = COORDtyp.Robot;
            HCOORD.X = 0;
            HCOORD.Y = 0;
            HCOORD.Z = 0;
            HCOORD.R11 = 1;
            HCOORD.R12 = 0;
            HCOORD.R13 = 0;
            HCOORD.R21 = 0;
            HCOORD.R22 = 1;
            HCOORD.R23 = 0;
            HCOORD.R31 = 0;
            HCOORD.R32 = 0;
            HCOORD.R33 = 1;
            return HCOORD;
        }

        //关节坐标转换为机器人坐标
        public static HCOORDtyp MF_JOINT2ROBOT(JPOStyp JPOS)
        {
            //运动学正解，关节坐标->工具点
            HCOORDtyp HPOS = new HCOORDtyp();
            HPOS.C = COORDtyp.Robot;
            HPOS.X = Math.Cos(JPOS.J1) * a[0] + Math.Cos(JPOS.J1 + JPOS.J2) * a[1] + Math.Sin(JPOS.J1 + JPOS.J2 + JPOS.J3) * d[3];
            HPOS.Y = 0;
            HPOS.Z = Math.Sin(JPOS.J1) * a[0] + Math.Sin(JPOS.J1 + JPOS.J2) * a[1] - Math.Cos(JPOS.J1 + JPOS.J2 + JPOS.J3) * d[3];
            HPOS.R11 = 0.5 * Math.Cos(JPOS.J1 + JPOS.J2 + JPOS.J3 - JPOS.J4) + 0.5 * Math.Cos(JPOS.J1 + JPOS.J2 + JPOS.J3 + JPOS.J4);
            HPOS.R12 = -0.5 * Math.Sin(JPOS.J1 + JPOS.J2 + JPOS.J3 + JPOS.J4) + 0.5 * Math.Sin(JPOS.J1 + JPOS.J2 + JPOS.J3 - JPOS.J4);
            HPOS.R13 = Math.Sin(JPOS.J1 + JPOS.J2 + JPOS.J3);
            HPOS.R21 = -Math.Sin(JPOS.J4);
            HPOS.R22 = -Math.Cos(JPOS.J4);
            HPOS.R23 = 0;
            HPOS.R31 = 0.5 * Math.Sin(JPOS.J1 + JPOS.J2 + JPOS.J3 + JPOS.J4) + 0.5 * Math.Sin(JPOS.J1 + JPOS.J2 + JPOS.J3 - JPOS.J4);
            HPOS.R32 = -0.5 * Math.Cos(JPOS.J1 + JPOS.J2 + JPOS.J3 - JPOS.J4) + 0.5 * Math.Cos(JPOS.J1 + JPOS.J2 + JPOS.J3 + JPOS.J4);
            HPOS.R33 = -Math.Cos(JPOS.J1 + JPOS.J2 + JPOS.J3);

            HPOS = HPOS * TOOL;  //变换工具点坐标

            return HPOS;
        }

        //机器人坐标转换为关节坐标 //逆解
        public static int MF_ROBOT2JOINT(HCOORDtyp HPOS, ref JPOStyp JPOS, int FLAG)
        {
            //运动学反解，工具点->关节坐标
            //HPOS齐次坐标，JPOS关节坐标，FLAG对应多解标志：bit0=1对应1关节采用overhead姿态，bit1=1对应3关节低肘型，bit2=1对应5关节>0（腕上扬），bit3=1确定强制设定多解姿态（否则根据当前姿态自动设定）

            double q1, q2, q3, q4, q123, x3, z3;

            HPOS = HPOS * HCOORDtyp.inv(TOOL);  //变换腕点坐标

            q4 = Math.Atan2(-HPOS.R21, -HPOS.R22);
            q123 = Math.Atan2(HPOS.R31, -HPOS.R33);
            x3 = HPOS.X - Math.Sin(q123) * d[3];
            z3 = HPOS.Z + Math.Cos(q123) * d[3];
            q2 = Math.Acos((x3 * x3 + z3 * z3 - a[0] * a[0] - a[1] * a[1]) / (2.0 * a[0] * a[1]));  //余弦定理
            if ((FLAG & 0x00000008) != 0)  //指定肘型
            {
                if ((FLAG & 0x00000002) == 0)  //高肘型,q2<0
                    q2 = -q2;
            }
            else  //不指定
            {
                if (curJPOS.J2 < 0)  //按上次计算确定肘型
                    q2 = -q2;
            }

            q1 = -Math.Atan2((-z3 * a[1] * Math.Cos(q2) + x3 * a[1] * Math.Sin(q2) - z3 * a[0]), (z3 * a[1] * Math.Sin(q2) + x3 * a[1] * Math.Cos(q2) + x3 * a[0]));
            q3 = q123 - q2 - q1;

            JPOS.J1 = q1; JPOS.J2 = q2; JPOS.J3 = q3; JPOS.J4 = q4;

            //将结果保存为当前点，作为下次多解时的选择依据，当前点可由用户随时设定
            curJPOS.J1 = JPOS.J1; curJPOS.J2 = JPOS.J2; curJPOS.J3 = JPOS.J3; curJPOS.J4 = JPOS.J4;

            return 0;
        }

        //增量直线（手动用）
        public static int MF_IMOVL(JPOStyp JPOS, EPOStyp DIR, double V, ref JPOStyp JV)
        {
            //返回当前瞬时各关节速度分量，供运动线程JOG方式使用
            //JPOS为当前点，DIR为增量（方向/旋转），V为线速度或角速度，JV指明各关节速度

            HCOORDtyp hpos;
            JPOStyp dP, P2 = new JPOStyp();

            hpos = MF_JOINT2ROBOT(JPOS);  //到腕点

            hpos.X = hpos.X + DIR.X / 1000; //指向前方1mm处（注意长度单位是m）
            hpos.Y = hpos.Y + DIR.Y / 1000;
            hpos.Z = hpos.Z + DIR.Z / 1000;
            hpos = roty(hpos, DIR.THETA / 1000);  //指向前方0.001弧度处。此处借用欧拉角传递数据，并非真正欧拉角，数据表示分别绕坐标系xyz轴的转动

            if(MF_ROBOT2JOINT(hpos, ref P2, 0) != 0)  
                return -1;  //反解不存在

            dP = P2 - JPOS;
            JV = (V * 1000) * dP;  //关节坐标增量/时间 = 各关节速度，单位rad/s

            return 0;
        }

        //点位运动插补 //。。。。
        public static int MF_MOVJ(JPOStyp JPOS1, JPOStyp JPOS2, JPOStyp JVO, double V, double TAIL)
        {
            //通用的单段PTP运动规划，5段法，加速度连续
            //初速度指定（前段剩余），连续度指定（即规划的剩余残差，0-100%，按时间算），本段始末加速度为零
            //可用于连续PTP控制，如果初速度为零，TAIL为1，即变为单独PTP运动
            //全部用最大速度和加速度计算各轴时间（1阶速度曲线），取其最大者，并按给定速度V（%）放大，再按连续度TAIL（%）截取
            //然后各轴按此时间做定时间运动规划
            //最终结果为PVT表
            //JPOS1起点，JPOS2终点，JVO初速度，V速度（%），TAIL残余度（%）
            int i, rtn;
            double Tl, Tcut, TK, Vr, Scut;
            double[] SL, jP1, jP2, jVo, jPf = new double[4], jVf = new double[4], t = new double[3], s = new double[3];
            double[,] PS = new double[4, 3];  //各轴位移段
            double[,] PT = new double[4, 3];  //各轴时间段

            if ((V > 1) || (V <= 0) || (TAIL >= 1) || (TAIL < 0))  //数据不合理
                return -1;

            //关节角和关节速度都转为数组
            SL = JPOStyp.JPOS2Array(JPOS2 - JPOS1);  //位移
            jP1 = JPOStyp.JPOS2Array(JPOS1);
            jP2 = JPOStyp.JPOS2Array(JPOS2);
            jVo = JPOStyp.JPOS2Array(JVO);

            if ((Math.Abs(SL[0]) < 0.000001) && (Math.Abs(SL[1]) < 0.000001) && (Math.Abs(SL[2]) < 0.000001) && (Math.Abs(SL[3]) < 0.000001))
                return -1;  //位移不可全为零

            if ((Math.Abs(jVo[0]) > Vmax[0]) || (Math.Abs(jVo[1]) > Vmax[1]) || (Math.Abs(jVo[2]) > Vmax[2]) || (Math.Abs(jVo[3]) > Vmax[3]))
                return -1;  //初速度不可大于最大速度

            Vr = 0;  //实际最高速度
            Tl = 0;  //时间最大值


            //考虑1阶速度曲线的情况计算运动时间（为了下一步升2阶，所有Amax减半使用）
            for (i = 0; i < 4; i++)
            {
                PLANV(SL[i], jVo[i], 0, Amax[i] / 2, Vmax[i], ref t, ref s);
                PS[i, 0] = s[0]; PS[i, 1] = s[1]; PS[i, 2] = s[2];
                PT[i, 0] = t[0]; PT[i, 1] = t[1]; PT[i, 2] = t[2];
                if (Tl < (t[0] + t[1] + t[2]))  //取其最大时间
                    Tl = t[0] + t[1] + t[2];
            }

            //按给定速度（%）放大时间
            Tl = Tl / V;
            Tcut = Tl * (1 - TAIL);  //截断时间
            for (i = 0; i < 4; i++)
            {
                TK = (PT[i, 0] + PT[i, 1] + PT[i, 2]) / Tl;
                if (TK >= 0.000001)
                {
                    PT[i, 0] /= TK;
                    PT[i, 1] /= TK;
                    PT[i, 2] /= TK;
                }
                else
                {
                    PT[i, 0] = Tl / 2;
                    PT[i, 1] = 0;
                    PT[i, 2] = Tl / 2;
                }
            }

            //按照统一时间重新规划1阶速度
            for (i = 0; i < 4; i++)
            {
                if (Tcut < PT[i, 0])  //加速段就截断，只有1段
                {
                    Scut = jVo[i] * Tcut + (PS[i, 0] - jVo[i] * PT[i, 0]) * Tcut * Tcut / PT[i, 0] / PT[i, 0];
                    jVf[i] = 2 * Scut / Tcut - jVo[i];
                    PS[i, 0] = jP1[i] + Scut; PT[i, 0] = Tcut;
                    PS[i, 1] = PS[i, 0]; PT[i, 1] = Tcut;
                    PS[i, 2] = PS[i, 0]; PT[i, 2] = Tcut;
                }
                else if (Tcut >= (PT[i, 0] + PT[i, 1]))  //减速段截断（或者不截断）,共3段
                {
                    Scut = PS[i, 0] + PS[i, 1] + PS[i, 2] * (1 - (Tl - Tcut) * (Tl - Tcut) / PT[i, 2] / PT[i, 2]);
                    if (TAIL > 0.000001)
                        jVf[i] = 2 * (PS[i, 0] + PS[i, 1] + PS[i, 2] - Scut) / (Tl - Tcut);
                    else
                        jVf[i] = 0;
                    PS[i, 0] += jP1[i];
                    PS[i, 1] += PS[i, 0]; PT[i, 1] += PT[i, 0];
                    PS[i, 2] = jP1[i] + Scut; PT[i, 2] = Tcut;
                }
                else  //匀速段截断, 共2段
                {
                    Scut = PS[i, 0] + PS[i, 1] * (Tcut - PT[i, 0]) / PT[i, 1];
                    jVf[i] = PS[i, 1] / PT[i, 1];
                    PS[i, 0] += jP1[i];
                    PS[i, 1] = PS[i, 0] + Scut; PT[i, 1] = Tcut;
                    PS[i, 2] = PS[i, 1]; PT[i, 2] = Tcut;
                }
            }

            //写入PVT表。MOVJ总是写在PVT头部，然后读取清空，只有MOVJS才会产生长的PVT
            PVTbusy = 1;  //锁定PVT表
            PVTready = 0;
            for (i = 0; i < 4; i++)
            {
                //写入起始点
                Ptab[i, PVTIndex] = jP1[i]; Ttab[i, PVTIndex] += 0;
                //写入1分段
                Ptab[i, PVTIndex + 1] = PS[i, 0]; Ttab[i, PVTIndex + 1] = Ttab[i, PVTIndex] + PT[i, 0];
                //写入2分段
                Ptab[i, PVTIndex + 2] = PS[i, 1]; Ttab[i, PVTIndex + 2] = Ttab[i, PVTIndex] + PT[i, 1];
                //写入3分段
                Ptab[i, PVTIndex + 3] = PS[i, 2]; Ttab[i, PVTIndex + 3] = Ttab[i, PVTIndex] + PT[i, 2];
            }
            PVTIndex += 3;
            PVTbusy = 0;

            JVF.J1 = jVf[0]; JVF.J2 = jVf[1]; JVF.J3 = jVf[2]; JVF.J4 = jVf[3];
            JPF.J1 = PS[0, 2]; JPF.J2 = PS[1, 2]; JPF.J3 = PS[2, 2]; JPF.J4 = PS[3, 2];

            if (TAIL == 0)
                PVTready = 1;  //计算完毕，非PT模式，PVT可读

            return 0;
        }

        //初始化
        public static int MF_INITMATHFUN(HCOORDtyp inTOOL, double[] JV, double[] JA, double[] HOME)  //初始化
        {
            TOOL = inTOOL;

            for (int i = 0; i < 4; i++)
            {
                Vmax[i] = JV[i];
                Amax[i] = JA[i];
                HJ[i] = HOME[i];
            }
            
            return 0;
        }

        //整理第i轴PVT数据
        public static int MF_SORTPVT(int i)
        {
            //本算法规划中是允许时间为零段的（对应匀速段没有的情况，为了标号对齐，没有去除重复点），但固高控制器是不允许的
            //最后写入控制器PVT的话要把重复点去除

            int j, k;

            if (PVTready == 0)  //数据未就绪
                return -7;

            PVTbusy = 1;  //锁定

            k = 0;
            for (j = 0; j <= PVTIndex; j++)
            {
                if ((j == 0) || ((Ttab[i,j] - Ttab[i,j - 1]) > 0.0001))  //如果不与上一点重复才写入（最后6轴PVT长度是不一致的）
                {
                    Ptab[i,k] = Ptab[i,j];
                    Ttab[i,k] = Ttab[i,j];
                    k++;
                }
            }

            PVTbusy = 0;

            return k;
        }
    }
}
