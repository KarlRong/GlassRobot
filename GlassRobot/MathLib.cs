using System.Runtime.InteropServices;

namespace GlassRobot
{
    /// <summary>
    /// 用于封装MathFun函数的类
    /// </summary>
    public class MathFun
    {
        public const string MathPath = @".\MathLibT.dll";  //数学函数库所在路径

        public enum COORDtyp { Joint, Cylinder, Robot, World, Tool1 = 100, Tool2, Tool3, Tool4, User1 = 200, User2, User3, User4 };  //枚举坐标类型（标明位置数据所属坐标系，Joint关节坐标，Robot是机器人0坐标，是所有运动学基础坐标。World世界坐标。）

        public struct JPOStyp  //关节坐标
        {
            public double J1;
            public double J2;
            public double J3;
            public double J4;
            public double J5;
            public double J6;
        }

        public struct CPOStyp  //圆柱坐标
        {
            public double R;
            public double A;
            public double H;
            public double PHI;
            public double THETA;
            public double PSI;
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
        }

        public struct QPOStyp //直角坐标（伪四元数），便于直观判断工具姿态
        {
            public COORDtyp C;//坐标系
            public double X;
            public double Y;
            public double Z;//3个位置
            public double kx;
            public double ky;
            public double kz;//姿态（pos = k*(x,y,z)，(x,y,z)为工具Z轴指向的单位向量，k表示工具绕自身Z轴的转角圈数：k=exp(Angle)
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
        }

        /* 重要参数 */
        //脉冲当量的倒数，即某关节每圈脉冲数（或者直线关节每米脉冲数），电机编码器每圈脉冲数 x 减速器减速比
        public static double[] KJ = new double[6];

        //原点偏移量，即机器人停在真正机械原点时的编码器读数/（-KJ），//读取：关节角 = 码盘/KJ + HJ;  输出：码盘 = (关节角 - HJ) * KJ;
        public static double[] HJ = new double[6];

        /* 数学函数 */
        [DllImport(MathPath)]
        public static extern int MF_COORDTRANS(HCOORDtyp SOURCE, HCOORDtyp TRANS, ref HCOORDtyp RESULT);  //坐标变换

        [DllImport(MathPath)]
        public static extern int MF_CYLINDER2JOINT(CPOStyp CPOS, ref JPOStyp JPOS, int FLAG);  //圆柱坐标转换为关节坐标

        [DllImport(MathPath)]
        public static extern int MF_EUL2TR(EPOStyp EPOS, ref HCOORDtyp HPOS);  //欧拉坐标->齐次坐标

        [DllImport(MathPath)]
        public static extern int MF_HCOORDINIT(ref HCOORDtyp HCOORD);  //初始化一个单位阵

        [DllImport(MathPath)]
        public static extern int MF_IMOVL(JPOStyp JPOS, EPOStyp DIR, double V, ref JPOStyp JV);  //增量直线（手动用）

        [DllImport(MathPath)]
        public static extern int MF_ISINTERFERENCE(JPOStyp JPOS, double[] PP, double[] PN);  //检查是否处于干涉区内

        [DllImport(MathPath)]
        public static extern int MF_JOINT2CYLINDER(JPOStyp JPOS, ref CPOStyp CPOS);  //关节坐标转换为圆柱坐标

        [DllImport(MathPath)]
        public static extern int MF_JOINT2PULSE(JPOStyp JPOS, double[] ENC);  //关节坐标转换为脉冲数

        [DllImport(MathPath)]
        public static extern int MF_JOINT2ROBOT(JPOStyp JPOS, ref HCOORDtyp HPOS);  //关节坐标转换为机器人坐标

        [DllImport(MathPath)]
        public static extern int MF_MOVC(HCOORDtyp HPOS1, HCOORDtyp HPOS2, HCOORDtyp HPOS3, double A, double V, double AF, double W, int MOD, double HEAD, double TAIL);  //空间圆弧插补

        [DllImport(MathPath)]
        public static extern int MF_MOVCW(HCOORDtyp HPOS1, HCOORDtyp HPOS2, HCOORDtyp HPOS3, double A, double V, double AF, double W, int MOD, double HEAD, double TAIL);  //空间圆弧插补

        [DllImport(MathPath)]
        public static extern int MF_MOVJ(JPOStyp JPOS1, JPOStyp JPOS2, JPOStyp JVO, double V, double TAIL, int PT);  //点位运动插补

        [DllImport(MathPath)]
        public static extern int MF_MOVJS(JPOStyp[] JPOS, JPOStyp JV, double[] V, int NUM, int PT, int FIN);  //连续PTP运动（关节空间上的样条）

        [DllImport(MathPath)]
        public static extern int MF_MOVL(HCOORDtyp HPOS1, HCOORDtyp HPOS2, double A, double V, double AF, double W, int MOD, double HEADLEN, double TAILLEN);  //空间直线插补

        [DllImport(MathPath)]
        public static extern int MF_MOVS(HCOORDtyp[] HPOS, double[] V, double A, int NUM, int FIN);  //样条曲线插补

        [DllImport(MathPath)]
        public static extern int MF_PULSE2JOINT(double[] ENC, ref JPOStyp JPOS);  //脉冲数转关节坐标

        [DllImport(MathPath)]
        public static extern int MF_QP2TR(QPOStyp QPOS, ref HCOORDtyp HPOS);  //伪四元坐标转齐次坐标

        [DllImport(MathPath)]
        public static extern int MF_ROBOT2JOINT(HCOORDtyp HPOS, ref JPOStyp JPOS, int FLAG);  //机器人坐标转换为关节坐标

        [DllImport(MathPath)]
        public static extern int MF_SETTOOLCOORD(JPOStyp[] JPOS, ref HCOORDtyp TOOLCOORD);  //3点建立工具坐标系

        [DllImport(MathPath)]
        public static extern int MF_SETUSERCOORD(JPOStyp[] JPOS, ref HCOORDtyp USERCOORD);  //3点确定用户坐标系

        [DllImport(MathPath)]
        public static extern int MF_TOOL2WRIST(HCOORDtyp TOOL, HCOORDtyp TRANS, ref HCOORDtyp WRIST);  //已知工具点坐标求腕点坐标

        [DllImport(MathPath)]
        public static extern int MF_TR2EUL(HCOORDtyp HCOORD, ref EPOStyp EPOS, int FLAG);  //齐次坐标->欧拉坐标

        [DllImport(MathPath)]
        public static extern int MF_TR2QP(HCOORDtyp HPOS, ref QPOStyp QPOS);  //齐次坐标转伪四元坐标

        [DllImport(MathPath)]
        public static extern int MF_WRIST2TOOL(HCOORDtyp WRIST, HCOORDtyp TRANS, ref HCOORDtyp TOOL);  //已知腕点坐标求工具点坐标

        /* 设定和读取关键参数 */
        [DllImport(MathPath)]
        public static extern int MF_INITMATHFUN(double[] LIMITP, double[] LIMITN, HCOORDtyp WORLD, HCOORDtyp USER, HCOORDtyp TOOL, double[] JV, double[] JA, double V, double W, double A, double AF, double[] HOME, double DT);  //初始化
        [DllImport(MathPath)]
        public static extern int MF_INITPOS(JPOStyp CURPOS);  //将当前位置作为初始位置（消除上段规划残余）

        [DllImport(MathPath)]
        public static extern int MF_SETLIMIT(double[] LIMITP, double[] LIMITN);  //设定软限位
        [DllImport(MathPath)]
        public static extern int MF_GETLIMIT(double[] LIMITP, double[] LIMITN);  //读取软限位

        [DllImport(MathPath)]
        public static extern int MF_SETWORLD(HCOORDtyp WORLD);  //设定世界坐标
        [DllImport(MathPath)]
        public static extern int MF_GETWORLD(ref HCOORDtyp WORLD);  //读取世界坐标

        [DllImport(MathPath)]
        public static extern int MF_SETUSER(HCOORDtyp USER);  //设定用户坐标
        [DllImport(MathPath)]
        public static extern int MF_GETUSER(ref HCOORDtyp USER);  //读取用户坐标

        [DllImport(MathPath)]
        public static extern int MF_SETTOOL(HCOORDtyp TOOL);  //设定工具坐标
        [DllImport(MathPath)]
        public static extern int MF_GETTOOL(ref HCOORDtyp TOOL);  //读取工具坐标

        [DllImport(MathPath)]
        public static extern int MF_SETVMAX(double[] JV);  //设定最高速度（关节）
        [DllImport(MathPath)]
        public static extern int MF_GETVMAX(double[] JV);  //读取最高速度（关节）

        [DllImport(MathPath)]
        public static extern int MF_SETVMAXL(double V, double W);  //设定最高速度（CP）
        [DllImport(MathPath)]
        public static extern int MF_GETVMAXL(ref double V, ref double W);  //读取最高速度（CP）

        [DllImport(MathPath)]
        public static extern int MF_SETAMAX(double[] JA);  //设定最高加速度（关节）
        [DllImport(MathPath)]
        public static extern int MF_GETAMAX(double[] JA);  //读取最高加速度（关节）

        [DllImport(MathPath)]
        public static extern int MF_SETAMAXL(double A, double AF);  //设定最高加速度（CP）
        [DllImport(MathPath)]
        public static extern int MF_GETAMAXL(ref double A, ref double AF);  //读取最高加速度（CP）

        [DllImport(MathPath)]
        public static extern int MF_SETHOME(double[] ENC);  //设定机械原点
        [DllImport(MathPath)]
        public static extern int MF_GETHOME(double[] ENC);  //读取机械原点

        [DllImport(MathPath)]
        public static extern int MF_SETTIMER(double DT);  //设定插补周期
        [DllImport(MathPath)]
        public static extern int MF_GETTIMER(ref double DT);  //读取插补周期

        [DllImport(MathPath)]
        public static extern int MF_SETCURJPOS(JPOStyp CURJPOS);  //设定当前位置（用于反解中选多解等）
        [DllImport(MathPath)]
        public static extern int MF_GETREMAIN(ref JPOStyp JPOS, ref JPOStyp JV, ref HCOORDtyp HPOS, double[] HV);  //读取残余的位置和速度（用于连续过渡段）

        /* 读取插补数据 */
        [DllImport(MathPath)]
        public static extern int MF_GETPVT(ref double PTAB, ref double VTAB, ref double TTAB, ref int NUM, int FIN, int I);  //读取PVT表

        [DllImport(MathPath)]
        public static extern int MF_GETPT(ref double JPT1, ref double JPT2, ref double JPT3, ref double JPT4, ref double JPT5, ref double JPT6, ref int STEP);  //读取PT表

    }
}
