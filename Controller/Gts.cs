using System.Runtime.InteropServices;

namespace Controller
{
    /// <summary>
    /// 用于封装固高驱动函数的类
    /// </summary>
    public class Gts
    {
        public const string GTSPATH = @"\Hard Disk\GlassRobot\GTS.dll";  //驱动库所在路径
        public const string TBXPATH = @"\Hard Disk\GlassRobot\EXTMDL.dll";  //驱动库所在路径
        
        public const short MC_NONE = -1;

        public const short MC_LIMIT_POSITIVE = 0;
        public const short MC_LIMIT_NEGATIVE = 1;
        public const short MC_ALARM = 2;
        public const short MC_HOME = 3;
        public const short MC_GPI = 4;
        public const short MC_ARRIVE = 5;

        public const short MC_ENABLE = 10;
        public const short MC_CLEAR = 11;
        public const short MC_GPO = 12;

        public const short MC_DAC = 20;
        public const short MC_STEP = 21;
        public const short MC_PULSE = 22;
        public const short MC_ENCODER = 23;
        public const short MC_ADC = 24;

        public const short MC_AXIS = 30;
        public const short MC_PROFILE = 31;
        public const short MC_CONTROL = 32;

        public const short CAPTURE_HOME = 1;
        public const short CAPTURE_INDEX = 2;
        public const short CAPTURE_PROBE = 3;

        public const short PT_MODE_STATIC = 0;
        public const short PT_MODE_DYNAMIC = 1;

        public const short PT_SEGMENT_NORMAL = 0;
        public const short PT_SEGMENT_EVEN = 1;
        public const short PT_SEGMENT_STOP = 2;

        public const short GEAR_MASTER_ENCODER = 1;
        public const short GEAR_MASTER_PROFILE = 2;
        public const short GEAR_MASTER_AXIS = 3;

        public const short FOLLOW_MASTER_ENCODER = 1;
        public const short FOLLOW_MASTER_PROFILE = 2;
        public const short FOLLOW_MASTER_AXIS = 3;

        public const short FOLLOW_EVENT_START = 1;
        public const short FOLLOW_EVENT_PASS = 2;

        public const short FOLLOW_SEGMENT_NORMAL = 0;
        public const short FOLLOW_SEGMENT_EVEN = 1;
        public const short FOLLOW_SEGMENT_STOP = 2;
        public const short FOLLOW_SEGMENT_CONTINUE = 3;

        public const short INTERPOLATION_AXIS_MAX = 4;
        public const short CRD_FIFO_MAX = 4096;
        public const short CRD_MAX = 2;
        public const short CRD_OPERATION_DATA_EXT_MAX = 2;

        public const short CRD_OPERATION_TYPE_NONE = 0;
        public const short CRD_OPERATION_TYPE_BUF_IO_DELAY = 1;
        public const short CRD_OPERATION_TYPE_LASER_ON = 2;
        public const short CRD_OPERATION_TYPE_LASER_OFF = 3;
        public const short CRD_OPERATION_TYPE_BUF_DA = 4;
        public const short CRD_OPERATION_TYPE_LASER_CMD = 5;
        public const short CRD_OPERATION_TYPE_LASER_FOLLOW = 6;
        public const short CRD_OPERATION_TYPE_LMTS_ON = 7;
        public const short CRD_OPERATION_TYPE_LMTS_OFF = 8;
        public const short CRD_OPERATION_TYPE_SET_STOP_IO = 9;
        public const short CRD_OPERATION_TYPE_BUF_MOVE = 10;
        public const short CRD_OPERATION_TYPE_BUF_GEAR = 11;
        public const short CRD_OPERATION_TYPE_SET_SEG_NUM = 12;
        public const short CRD_OPERATION_TYPE_STOP_MOTION = 13;
        public const short CRD_OPERATION_TYPE_SET_VAR_VALUE = 14;
        public const short CRD_OPERATION_TYPE_JUMP_NEXT_SEG = 15;
        public const short CRD_OPERATION_TYPE_SYNCH_PRF_POS = 16;

        public const short INTERPOLATION_MOTION_TYPE_LINE = 0;
        public const short INTERPOLATION_MOTION_TYPE_CIRCLE = 1;

        public const short INTERPOLATION_CIRCLE_PLAT_XY = 0;
        public const short INTERPOLATION_CIRCLE_PLAT_YZ = 1;
        public const short INTERPOLATION_CIRCLE_PLAT_ZX = 2;

        public const short INTERPOLATION_CIRCLE_DIR_CW = 0;
        public const short INTERPOLATION_CIRCLE_DIR_CCW = 1;


        public struct TTrapPrm
        {
            public double acc;
            public double dec;
            public double velStart;
            public short smoothTime;
        }

        public struct TJogPrm
        {
            public double acc;
            public double dec;
            public double smooth;
        }

        public struct TPid
        {
            public double kp;
            public double ki;
            public double kd;
            public double kvff;
            public double kaff;

            public int integralLimit;
            public int derivativeLimit;
            public short limit;
        }

        public struct TThreadSts
        {
            public short run;
            public short error;
            public double result;
            public short line;
        }

        public struct TVarInfo
        {
            public short id;
            public short dataType;
            public double dumb0;
            public double dumb1;
            public double dumb2;
            public double dumb3;
        }

        public struct TCrdPrm
        {
            public short dimension;
            public short profile1;
            public short profile2;
            public short profile3;
            public short profile4;
            public short profile5;
            public short profile6;
            public short profile7;
            public short profile8;

            public double synVelMax;
            public double synAccMax;
            public short evenTime;
            public short setOriginFlag;
            public long originPos1;
            public long originPos2;
            public long originPos3;
            public long originPos4;
            public long originPos5;
            public long originPos6;
            public long originPos7;
            public long originPos8;
        }

        public struct TCrdBufOperation
        {
            public short flag;
            public ushort delay;
            public short doType;
            public ushort doMask;
            public ushort doValue;
            public ushort dataExt1;
            public ushort dataExt2;
        }

        public struct TCrdData
        {
            public short motionType;
            public short circlePlat;
            public long posX;
            public long posY;
            public long posZ;
            public long posA;
            public double radius;
            public short circleDir;
            public double lCenterX;
            public double lCenterY;
            public double vel;
            public double acc;
            public short velEndZero;
            public TCrdBufOperation operation;

            public double cosX;
            public double cosY;
            public double cosZ;
            public double cosA;
            public double velEnd;
            public double velEndAdjust;
            public double r;
        }

        [DllImport(GTSPATH)]
        public static extern short GT_SetCardNo(short index);
        [DllImport(GTSPATH)]
        public static extern short GT_GetCardNo(out short index);

        [DllImport(GTSPATH)]
        public static extern short GT_Open(short channel, short param);
        [DllImport(GTSPATH)]
        public static extern short GT_Close();

        [DllImport(GTSPATH)]
        public static extern short GT_LoadConfig(byte[] path);//读取配置器文件

        [DllImport(GTSPATH)]
        public static extern short GT_GetVersion(out string pVersion);

        [DllImport(GTSPATH)]
        public static extern short GT_SetDo(short doType, int value);
        [DllImport(GTSPATH)]
        public static extern short GT_SetDoBit(short doType, short doIndex, short value);
        [DllImport(GTSPATH)]
        public static extern short GT_GetDo(short doType, out int pValue);
        [DllImport(GTSPATH)]
        public static extern short GT_SetDoBitReverse(short doType, short doIndex, short value, short reverseTime);

        [DllImport(GTSPATH)]
        public static extern short GT_GetDi(short diType, out int pValue);
        [DllImport(GTSPATH)]
        public static extern short GT_GetDiReverseCount(short diType, short diIndex, out uint reverseCount, short count);
        [DllImport(GTSPATH)]
        public static extern short GT_SetDiReverseCount(short diType, short diIndex, ref uint reverseCount, short count);
        [DllImport(GTSPATH)]
        public static extern short GT_GetDiRaw(short diType, out int pValue);

        [DllImport(GTSPATH)]
        public static extern short GT_SetDac(short dac, ref short value, short count);
        [DllImport(GTSPATH)]
        public static extern short GT_GetDac(short dac, out short value, short count, out uint pClock);

        [DllImport(GTSPATH)]
        public static extern short GT_GetAdc(short adc, out double pValue, short count, out uint pClock);
        [DllImport(GTSPATH)]
        public static extern short GT_GetAdcValue(short adc, out short pValue, short count, out uint pClock);

        [DllImport(GTSPATH)]
        public static extern short GT_SetEncPos(short encoder, int encPos);
        [DllImport(GTSPATH)]
        public static extern short GT_GetEncPos(short encoder, out double pValue, short count, out uint pClock);
        [DllImport(GTSPATH)]
        public static extern short GT_GetEncVel(short encoder, out double pValue, short count, out uint pClock);

        [DllImport(GTSPATH)]
        public static extern short GT_SetCaptureMode(short encoder, short mode);
        [DllImport(GTSPATH)]
        public static extern short GT_GetCaptureMode(short encoder, out short pMode, short count);
        [DllImport(GTSPATH)]
        public static extern short GT_GetCaptureStatus(short encoder, out short pStatus, out int pValue, short count, out uint pClock);
        [DllImport(GTSPATH)]
        public static extern short GT_SetCaptureSense(short encoder, short mode, short sense);
        [DllImport(GTSPATH)]
        public static extern short GT_ClearCaptureStatus(short encoder);

        [DllImport(GTSPATH)]
        public static extern short GT_Reset();
        [DllImport(GTSPATH)]
        public static extern short GT_GetClock(out uint pClock, out uint pLoop);
        [DllImport(GTSPATH)]
        public static extern short GT_GetClockHighPrecision(out uint pClock);

        [DllImport(GTSPATH)]
        public static extern short GT_GetSts(short axis, out int pSts, short count, out uint pClock);
        [DllImport(GTSPATH)]
        public static extern short GT_ClrSts(short axis, short count);
        [DllImport(GTSPATH)]
        public static extern short GT_AxisOn(short axis);
        [DllImport(GTSPATH)]
        public static extern short GT_AxisOff(short axis);
        [DllImport(GTSPATH)]
        public static extern short GT_Stop(int mask, int option);
        [DllImport(GTSPATH)]
        public static extern short GT_SetPrfPos(short profile, int prfPos);
        [DllImport(GTSPATH)]
        public static extern short GT_SynchAxisPos(int mask);
        [DllImport(GTSPATH)]
        public static extern short GT_ZeroPos(short axis, short count);

        [DllImport(GTSPATH)]
        public static extern short GT_SetSoftLimit(short axis, int positive, int negative);
        [DllImport(GTSPATH)]
        public static extern short GT_GetSoftLimit(short axis, out int pPositive, out int pNegative);
        [DllImport(GTSPATH)]
        public static extern short GT_SetAxisBand(short axis, int band, int time);
        [DllImport(GTSPATH)]
        public static extern short GT_GetAxisBand(short axis, out int pBand, out int pTime);
        [DllImport(GTSPATH)]
        public static extern short GT_SetBacklash(short axis, int compValue, double compChangeValue, int compDir);
        [DllImport(GTSPATH)]
        public static extern short GT_GetBacklash(short axis, out int pCompValue, out double pCompChangeValue, out int pCompDir);

        [DllImport(GTSPATH)]
        public static extern short GT_GetPrfPos(short profile, out double pValue, short count, out uint pClock);
        [DllImport(GTSPATH)]
        public static extern short GT_GetPrfVel(short profile, out double pValue, short count, out uint pClock);
        [DllImport(GTSPATH)]
        public static extern short GT_GetPrfAcc(short profile, out double pValue, short count, out uint pClock);
        [DllImport(GTSPATH)]
        public static extern short GT_GetPrfMode(short profile, out int pValue, short count, out uint pClock);

        [DllImport(GTSPATH)]
        public static extern short GT_GetAxisPrfPos(short axis, out double pValue, short count, out uint pClock);
        [DllImport(GTSPATH)]
        public static extern short GT_GetAxisPrfVel(short axis, out double pValue, short count, out uint pClock);
        [DllImport(GTSPATH)]
        public static extern short GT_GetAxisPrfAcc(short axis, out double pValue, short count, out uint pClock);
        [DllImport(GTSPATH)]
        public static extern short GT_GetAxisEncPos(short axis, out double pValue, short count, out uint pClock);
        [DllImport(GTSPATH)]
        public static extern short GT_GetAxisEncVel(short axis, out double pValue, short count, out uint pClock);
        [DllImport(GTSPATH)]
        public static extern short GT_GetAxisEncAcc(short axis, out double pValue, short count, out uint pClock);
        [DllImport(GTSPATH)]
        public static extern short GT_GetAxisError(short axis, out double pValue, short count, out uint pClock);

        [DllImport(GTSPATH)]
        public static extern short GT_SetControlFilter(short control, short index);
        [DllImport(GTSPATH)]
        public static extern short GT_GetControlFilter(short control, out short pIndex);

        [DllImport(GTSPATH)]
        public static extern short GT_SetPid(short control, short index, ref TPid pPid);
        [DllImport(GTSPATH)]
        public static extern short GT_GetPid(short control, short index, out TPid pPid);

        [DllImport(GTSPATH)]
        public static extern short GT_Update(int mask);
        [DllImport(GTSPATH)]
        public static extern short GT_SetPos(short profile, int pos);
        [DllImport(GTSPATH)]
        public static extern short GT_GetPos(short profile, out int pPos);
        [DllImport(GTSPATH)]
        public static extern short GT_SetVel(short profile, double vel);
        [DllImport(GTSPATH)]
        public static extern short GT_GetVel(short profile, out double pVel);

        [DllImport(GTSPATH)]
        public static extern short GT_PrfTrap(short profile);
        [DllImport(GTSPATH)]
        public static extern short GT_SetTrapPrm(short profile, ref TTrapPrm pPrm);
        [DllImport(GTSPATH)]
        public static extern short GT_GetTrapPrm(short profile, out TTrapPrm pPrm);

        [DllImport(GTSPATH)]
        public static extern short GT_PrfJog(short profile);
        [DllImport(GTSPATH)]
        public static extern short GT_SetJogPrm(short profile, ref TJogPrm pPrm);
        [DllImport(GTSPATH)]
        public static extern short GT_GetJogPrm(short profile, out TJogPrm pPrm);

        [DllImport(GTSPATH)]
        public static extern short GT_PrfPt(short profile, short mode);
        [DllImport(GTSPATH)]
        public static extern short GT_SetPtLoop(short profile, int loop);
        [DllImport(GTSPATH)]
        public static extern short GT_GetPtLoop(short profile, out int pLoop);
        [DllImport(GTSPATH)]
        public static extern short GT_PtSpace(short profile, out short pSpace, short fifo);
        [DllImport(GTSPATH)]
        public static extern short GT_PtData(short profile, double pos, int time, short type, short fifo);
        [DllImport(GTSPATH)]
        public static extern short GT_PtClear(short profile, short fifo);
        [DllImport(GTSPATH)]
        public static extern short GT_PtStart(int mask, int option);
        [DllImport(GTSPATH)]
        public static extern short GT_SetPtMemory(short profile, short memory);
        [DllImport(GTSPATH)]
        public static extern short GT_GetPtMemory(short profile, out short pMemory);

        [DllImport(GTSPATH)]
        public static extern short GT_PrfGear(short profile, short dir);
        [DllImport(GTSPATH)]
        public static extern short GT_SetGearMaster(short profile, short masterIndex, short masterType, short masterItem);
        [DllImport(GTSPATH)]
        public static extern short GT_GetGearMaster(short profile, out short pMasterIndex, out short pMasterType, out short pMasterItem);
        [DllImport(GTSPATH)]
        public static extern short GT_SetGearRatio(short profile, int masterEven, int slaveEven, int masterSlope);
        [DllImport(GTSPATH)]
        public static extern short GT_GetGearRatio(short profile, out int pMasterEven, out int pSlaveEven, out int pMasterSlope);
        [DllImport(GTSPATH)]
        public static extern short GT_GearStart(int mask);

        [DllImport(GTSPATH)]
        public static extern short GT_PrfFollow(short profile, short dir);
        [DllImport(GTSPATH)]
        public static extern short GT_SetFollowMaster(short profile, short masterIndex, short masterType, short masterItem);
        [DllImport(GTSPATH)]
        public static extern short GT_GetFollowMaster(short profile, out short pMasterIndex, out short pMasterType, out short pMasterItem);
        [DllImport(GTSPATH)]
        public static extern short GT_SetFollowLoop(short profile, int loop);
        [DllImport(GTSPATH)]
        public static extern short GT_GetFollowLoop(short profile, out int pLoop);
        [DllImport(GTSPATH)]
        public static extern short GT_SetFollowEvent(short profile, short followEvent, short masterDir, int pos);
        [DllImport(GTSPATH)]
        public static extern short GT_GetFollowEvent(short profile, out short pFollowEvent, out short pMasterDir, out int pPos);
        [DllImport(GTSPATH)]
        public static extern short GT_FollowSpace(short profile, out short pSpace, short fifo);
        [DllImport(GTSPATH)]
        public static extern short GT_FollowData(short profile, int masterSegment, double slaveSegment, short type, short fifo);
        [DllImport(GTSPATH)]
        public static extern short GT_FollowClear(short profile, short fifo);
        [DllImport(GTSPATH)]
        public static extern short GT_FollowStart(int mask, int option);
        [DllImport(GTSPATH)]
        public static extern short GT_FollowSwitch(int mask);
        [DllImport(GTSPATH)]
        public static extern short GT_SetFollowMemory(short profile, short memory);
        [DllImport(GTSPATH)]
        public static extern short GT_GetFollowMemory(short profile, out short memory);

        [DllImport(GTSPATH)]
        public static extern short GT_Download(string pFileName);

        [DllImport(GTSPATH)]
        public static extern short GT_GetFunId(string pFunName, out short pFunId);
        [DllImport(GTSPATH)]
        public static extern short GT_Bind(short thread, short funId, short page);

        [DllImport(GTSPATH)]
        public static extern short GT_RunThread(short thread);
        [DllImport(GTSPATH)]
        public static extern short GT_StopThread(short thread);
        [DllImport(GTSPATH)]
        public static extern short GT_PauseThread(short thread);

        [DllImport(GTSPATH)]
        public static extern short GT_GetThreadSts(short thread, out TThreadSts pThreadSts);

        [DllImport(GTSPATH)]
        public static extern short GT_GetVarId(string pFunName, string pVarName, out TVarInfo pVarInfo);
        [DllImport(GTSPATH)]
        public static extern short GT_SetVarValue(short page, ref TVarInfo pVarInfo, ref double pValue, short count);
        [DllImport(GTSPATH)]
        public static extern short GT_GetVarValue(short page, ref TVarInfo pVarInfo, out double pValue, short count);

        [DllImport(GTSPATH)]
        public static extern short GT_SetCrdPrm(short crd, ref TCrdPrm pCrdPrm);
        [DllImport(GTSPATH)]
        public static extern short GT_GetCrdPrm(short crd, out TCrdPrm pCrdPrm);
        [DllImport(GTSPATH)]
        public static extern short GT_CrdSpace(short crd, out int pSpace, short fifo);
        [DllImport(GTSPATH)]
        public static extern short GT_CrdData(short crd, ref TCrdData pCrdData, short fifo);
        [DllImport(GTSPATH)]
        public static extern short GT_CrdStart(short mask, short option);
        [DllImport(GTSPATH)]
        public static extern short GT_SetOverride(short crd, double synVelRatio);
        [DllImport(GTSPATH)]
        public static extern short GT_InitLookAhead(short crd, short fifo, double T, double accMax, short n, ref TCrdData pLookAheadBuf);
        [DllImport(GTSPATH)]
        public static extern short GT_CrdClear(short crd, short fifo);
        [DllImport(GTSPATH)]
        public static extern short GT_CrdStatus(short crd, out short pRun, out int pSegment, short fifo);
        [DllImport(GTSPATH)]
        public static extern short GT_SetUserSegNum(short crd, int segNum, short fifo);
        [DllImport(GTSPATH)]
        public static extern short GT_GetUserSegNum(short crd, out int pSegment, short fifo);
        [DllImport(GTSPATH)]
        public static extern short GT_GetRemainderSegNum(short crd, out int pSegment, short fifo);
        [DllImport(GTSPATH)]
        public static extern short GT_SetCrdStopDec(short crd, double decSmoothStop, double decAbruptStop);
        [DllImport(GTSPATH)]
        public static extern short GT_GetCrdStopDec(short crd, out double pDecSmoothStop, out double pDecAbruptStop);
        [DllImport(GTSPATH)]
        public static extern short GT_GetCrdPos(short crd, out double pPos);
        [DllImport(GTSPATH)]
        public static extern short GT_GetCrdVel(short crd, out double pSynVel);

        [DllImport(GTSPATH)]
        public static extern short GT_PrfPvt(short profile);
        [DllImport(GTSPATH)]
        public static extern short GT_SetPvtLoop(short profile, int loop);
        [DllImport(GTSPATH)]
        public static extern short GT_GetPvtLoop(short profile, out int pLoopCount, out int pLoop);
        [DllImport(GTSPATH)]
        public static extern short GT_PvtStatus(short profile, out short pTableId, out double pTime, short count);
        [DllImport(GTSPATH)]
        public static extern short GT_PvtStart(int mask);
        [DllImport(GTSPATH)]
        public static extern short GT_PvtTableSelect(short profile, short tableId);

        [DllImport(GTSPATH)]
        public static extern short GT_PvtTable(short tableId, int count, ref double pTime, ref double pPos, ref double pVel);
        [DllImport(GTSPATH)]
        public static extern short GT_PvtTableEx(short tableId, int count, ref double pTime, ref double pPos, ref double pVelBegin, ref double pVelEnd);
        [DllImport(GTSPATH)]
        public static extern short GT_PvtTableComplete(short tableId, int count, ref double pTime, ref double pPos, ref double pA, ref double pB, ref double pC, double velBegin, double velEnd);
        [DllImport(GTSPATH)]
        public static extern short GT_PvtTablePercent(short tableId, int count, ref double pTime, ref double pPos, ref double pPercent, double velBegin);
        [DllImport(GTSPATH)]
        public static extern short GT_PvtPercentCalculate(int n, ref double pTime, ref double pPos, ref double pPercent, double velBegin, ref double pVel);
        [DllImport(GTSPATH)]
        public static extern short GT_PvtTableContinuous(short tableId, int count, ref double pPos, ref double pVel, ref double pPercent, ref double pVelMax, ref double pAcc, ref double pDec, double timeBegin);
        [DllImport(GTSPATH)]
        public static extern short GT_PvtContinuousCalculate(int n, ref double pPos, ref double pVel, ref double pPercent, ref double pVelMax, ref double pAcc, ref double pDec, ref double pTime);

        [DllImport(GTSPATH)]
        public static extern short GT_AlarmOff(short axis);
        [DllImport(GTSPATH)]
        public static extern short GT_AlarmOn(short axis);
        [DllImport(GTSPATH)]
        public static extern short GT_LmtsOn(short axis, short limitType);
        [DllImport(GTSPATH)]
        public static extern short GT_LmtsOff(short axis, short limitType);
        [DllImport(GTSPATH)]
        public static extern short GT_ProfileScale(short axis, short alpha, short beta);
        [DllImport(GTSPATH)]
        public static extern short GT_EncScale(short axis, short alpha, short beta);
        [DllImport(GTSPATH)]
        public static extern short GT_StepDir(short step);
        [DllImport(GTSPATH)]
        public static extern short GT_StepPulse(short step);
        [DllImport(GTSPATH)]
        public static extern short GT_SetMtrBias(short dac, short bias);
        [DllImport(GTSPATH)]
        public static extern short GT_GetMtrBias(short dac, out short pBias);
        [DllImport(GTSPATH)]
        public static extern short GT_SetMtrLmt(short dac, short limit);
        [DllImport(GTSPATH)]
        public static extern short GT_GetMtrLmt(short dac, out short pLimit);
        [DllImport(GTSPATH)]
        public static extern short GT_EncSns(ushort sense);
        [DllImport(GTSPATH)]
        public static extern short GT_EncOn(short encoder);
        [DllImport(GTSPATH)]
        public static extern short GT_EncOff(short encoder);
        [DllImport(GTSPATH)]
        public static extern short GT_SetPosErr(short control, int error);
        [DllImport(GTSPATH)]
        public static extern short GT_GetPosErr(short control, out int pError);
        [DllImport(GTSPATH)]
        public static extern short GT_SetStopDec(short profile, double decSmoothStop, double decAbruptStop);
        [DllImport(GTSPATH)]
        public static extern short GT_GetStopDec(short profile, out double pDecSmoothStop, out double pDecAbruptStop);
        [DllImport(GTSPATH)]
        public static extern short GT_LmtSns(ushort sense);
        [DllImport(GTSPATH)]
        public static extern short GT_CtrlMode(short axis, short mode);
        [DllImport(GTSPATH)]
        public static extern short GT_SetStopIo(short axis, short stopType, short inputType, short inputIndex);
        [DllImport(GTSPATH)]
        public static extern short GT_GpiSns(ushort sense);



        [DllImport(GTSPATH)]
        public static extern short GT_CrdDataCircle(short crd, ref TCrdData pCrdData, short fifo);
        [DllImport(GTSPATH)]
        public static extern short GT_LnXY(short crd, int x, int y, double synVel, double synAcc, double velEnd, short fifo);
        [DllImport(GTSPATH)]
        public static extern short GT_LnXYZ(short crd, int x, int y, int z, double synVel, double synAcc, double velEnd, short fifo);
        [DllImport(GTSPATH)]
        public static extern short GT_LnXYZA(short crd, int x, int y, int z, int a, double synVel, double synAcc, double velEnd, short fifo);
        [DllImport(GTSPATH)]
        public static extern short GT_LnXYG0(short crd, int x, int y, double synVel, double synAcc, short fifo);
        [DllImport(GTSPATH)]
        public static extern short GT_LnXYZG0(short crd, int x, int y, int z, double synVel, double synAcc, short fifo);
        [DllImport(GTSPATH)]
        public static extern short GT_LnXYZAG0(short crd, int x, int y, int z, int a, double synVel, double synAcc, short fifo);
        [DllImport(GTSPATH)]
        public static extern short GT_ArcXYR(short crd, int x, int y, double radius, short circleDir, double synVel, double synAcc, double velEnd, short fifo);
        [DllImport(GTSPATH)]
        public static extern short GT_ArcXYC(short crd, int x, int y, double xCenter, double yCenter, short circleDir, double synVel, double synAcc, double velEnd, short fifo);
        [DllImport(GTSPATH)]
        public static extern short GT_ArcYZR(short crd, int y, int z, double radius, short circleDir, double synVel, double synAcc, double velEnd, short fifo);
        [DllImport(GTSPATH)]
        public static extern short GT_ArcYZC(short crd, int y, int z, double yCenter, double zCenter, short circleDir, double synVel, double synAcc, double velEnd, short fifo);
        [DllImport(GTSPATH)]
        public static extern short GT_ArcZXR(short crd, int z, int x, double radius, short circleDir, double synVel, double synAcc, double velEnd, short fifo);
        [DllImport(GTSPATH)]
        public static extern short GT_ArcZXC(short crd, int z, int x, double zCenter, double xCenter, short circleDir, double synVel, double synAcc, double velEnd, short fifo);
        [DllImport(GTSPATH)]
        public static extern short GT_BufIO(short crd, ushort doType, ushort doMask, ushort doValue, short fifo);
        [DllImport(GTSPATH)]
        public static extern short GT_BufDelay(short crd, ushort delayTime, short fifo);
        [DllImport(GTSPATH)]
        public static extern short GT_BufDA(short crd, short chn, short daValue, short fifo);
        [DllImport(GTSPATH)]
        public static extern short GT_BufLmtsOn(short crd, short axis, short limitType, short fifo);
        [DllImport(GTSPATH)]
        public static extern short GT_BufLmtsOff(short crd, short axis, short limitType, short fifo);
        [DllImport(GTSPATH)]
        public static extern short GT_BufSetStopIo(short crd, short axis, short stopType, short inputType, short inputIndex, short fifo);
        [DllImport(GTSPATH)]
        public static extern short GT_BufMove(short crd, short moveAxis, int pos, double vel, double acc, short modal, short fifo);
        [DllImport(GTSPATH)]
        public static extern short GT_BufGear(short crd, short gearAxis, int pos, short fifo);

        [DllImport(GTSPATH)]
        public static extern short GT_HomeInit();
        [DllImport(GTSPATH)]
        public static extern short GT_Home(short axis, int pos, double vel, double acc, int offset);
        [DllImport(GTSPATH)]
        public static extern short GT_Index(short axis, int pos, int offset);
        [DllImport(GTSPATH)]
        public static extern short GT_HomeStop(short axis, int pos, double vel, double acc);
        [DllImport(GTSPATH)]
        public static extern short GT_HomeSts(short axis, out ushort pStatus);

        //示教盒IO驱动
        [DllImport(TBXPATH)]
        public static extern short GT_SetModuleType(short iType);
        [DllImport(TBXPATH)]
        public static extern short GT_OpenExtMdl(byte[] fileName);
        [DllImport(TBXPATH)]
        public static extern short GT_CloseExtMdl();
        [DllImport(TBXPATH)]
        public static extern short GT_LoadExtConfig(byte[] fileName);
        [DllImport(TBXPATH)]
        public static extern short GT_SetExtIoValue(short mdl, ushort value, byte station);
        [DllImport(TBXPATH)]
        public static extern short GT_GetExtIoValue(short mdl, out ushort Value, byte station);
        [DllImport(TBXPATH)]
        public static extern short GT_SetExtDaVoltage(short mdl, short chn, double value, byte station);
        [DllImport(TBXPATH)]
        public static extern short GT_GetExtAdVoltage(short chn, out double Value, byte station);
        [DllImport(TBXPATH)]
        public static extern short GT_GetStsExtMdl(out ushort Status, byte station);
        [DllImport(TBXPATH)]
        public static extern short GT_SetPLCMdlMode(short mode);
        [DllImport(TBXPATH)]
        public static extern short GT_GetPLCMdlMode(out short mode);
    }
}
