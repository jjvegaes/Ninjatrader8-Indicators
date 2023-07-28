#region Using declarations
using System;
using System.Collections.Generic;
using System.Globalization;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using System.Windows.Controls;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Gui;
using NinjaTrader.Gui.NinjaScript.AtmStrategy;
using System.Linq;
using System.Windows.Data;
using System.Xml.Linq;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.Code;
using System.Threading;
using System.Collections.Concurrent;
using System.IO;
using NinjaTrader.NinjaScript.Indicators.TH;
#endregion
namespace NinjaTrader.NinjaScript.Indicators
{
    public enum JoiaGestorBreakEvenAutoTypes
    {
        Disabled = 0,
        HODL = 1,
        Enabled = 2,
        PlusTrail1Bar = 5,
        PlusTrail2Bar = 6,
        PlusTrail3Bar = 7,
        PlusTrail5Bar = 8,
        PlusTrailMovingAverage1 = 9,
        PlusTrailMovingAverage2 = 10,
        PlusTrailMovingAverage3 = 11
    };
    public enum JoiaGestorCloseAutoTypes
    {
        Disabled = 0,
        EquityCloseAllTarget = 1,
        MovingAverage1Slope = 6,
        MovingAverage1SlopeMinProfit = 7,
        MovingAverage2Slope = 8,
        MovingAverage2SlopeMinProfit = 9,
        MovingAverage3Slope = 10,
        MovingAverage3SlopeMinProfit = 11
    };
    public enum JoiaGestorStopLossSnapTypes
    {
        Disabled = 0,
        Snap1Bar = 1,
        Snap2Bar = 2,
        Snap3Bar = 3,
        Snap5Bar = 5,
        Snap8Bar = 8,
        SnapPBLevel = 9
    }
    public enum JoiaGestorEntryVolumeAutoTypes
    {
        Option1 = 0,
        Option2 = 1,
        Option3 = 2,
        Option4 = 3,
        Option5 = 4
    }
    public class JoiaGestor : Indicator
    {
        private const string SystemVersion = "v1.273";
        private const string SystemName = "JoiaGestor";
        private const string FullSystemName = SystemName + " - " + SystemVersion;
        private const string SystemDescription = "Gestro de riesgo";
        private const string SignalName = "JoiaGestor";
        private const string ObjectPrefix = "JoiaGestor_";
        private const string InfoLink = "https://github.com/jjvegaes/Ninjatrader8-Indicators";
        private const string JoiaGestorSessionStateFileName = "JoiaGestorSessionState.csv";
        private const int ZombieSetupBuyCode = 1;
        private const int ZombieSetupSellCode = -1;
        private const int ZombieSetupNoCode = 0;
        private const int AveragePriceLineZOrder = 50000;
        private Account account = null;
        private string atmStrategyName = string.Empty;
        private NinjaTrader.Gui.Tools.AccountSelector accountSelector = null;
        private System.Windows.Threading.DispatcherTimer timer;
        private double lastAccountBalance = 0;
        private DateTime lastOrderOutputTime = DateTime.MinValue;
        private bool hasRanOnceFirstCycle = false;
        private bool hasDrawnButtons = false;
        private bool accountHadPositions = false;
        private RealLogger RealLogger = new RealLogger(SystemName);
        private RealInstrumentService RealInstrumentService = new RealInstrumentService();
        private RealTradeService RealTradeService = new RealTradeService();
        private RealPositionService RealPositionService = new RealPositionService();
        private RealOrderService RealOrderService = new RealOrderService();
        private Dictionary<long, Order> ninjaTraderOrders = new Dictionary<long, Order>();
        private List<RealSessionState> accountSessionState = new List<RealSessionState>();
        private bool IsNinjaTraderOrdersAlreadyLoaded = false;
        private RealRunOncePerBar AutoPilotRunOncePerBar = new RealRunOncePerBar();
        private RealRunOncePerBar AutoAddOnRunOncePerBar = new RealRunOncePerBar();
        private RealRunOncePerBar AutoCloseRunOncePerBar = new RealRunOncePerBar();
        private RealRunOncePerBar AutoBreakEvenRunOncePerBar = new RealRunOncePerBar();
        private readonly object ClosePositionLock = new object();
        private readonly object NewPositionLock = new object();
        private readonly object MarketOrderLock = new object();
        private readonly object PositionTPSLOrderDelayLock = new object();
        private readonly object RefreshTPSLLock = new object();
        private readonly object RefreshPositionInfoLock = new object();
        private readonly object PopAutoJumpToSnapLock = new object();
        private readonly object DropAutoJumpToSnapLock = new object();
        private readonly object ClosePositionsInProfitLock = new object();
        private readonly object ClosePositionsInLossLock = new object();
        private readonly object RealTimePipelineLock = new object();
        private readonly object DelayedPipelineLock = new object();
        private DateTime lastPositionQuantityChange = DateTime.MinValue;
        private string lastATMStopLossStrategyName = string.Empty;
        private string lastATMTakeProfitStrategyName = string.Empty;
        private System.Windows.Controls.Grid thLayoutGrid = null;
        private System.Windows.Controls.Grid buttonGrid = null;
        private System.Windows.Controls.Grid labelGrid = null;
        private System.Windows.Controls.Button toggleAutoBEButton = null;
        private System.Windows.Controls.Button closeAllButton = null;
        private System.Windows.Controls.Button toggleAutoCloseButton = null;
        private System.Windows.Controls.Button BEButton = null;
        private System.Windows.Controls.Button SLButton = null;
        private System.Windows.Controls.Button TPButton = null;
        private System.Windows.Controls.Button revButton = null;
        private System.Windows.Controls.Button BuyDropButton = null;
        private System.Windows.Controls.Button SellDropButton = null;
        private System.Windows.Controls.Button BuyPopButton = null;
        private System.Windows.Controls.Button SellPopButton = null;
        private System.Windows.Controls.Button BuyMarketButton = null;
        private System.Windows.Controls.Button SellMarketButton = null;
        private System.Windows.Controls.Button toggleBogeyTargetButton = null;
        private System.Windows.Controls.Button toggleEntryVolumeAutoButton = null;
        private System.Windows.Controls.Button toggleAutoAddOnButton = null;
        private System.Windows.Controls.Button toggleTradeSignalButton = null;
        private System.Windows.Controls.Button toggleAutoPilotButton = null;
        private System.Windows.Controls.Label riskInfoLabel = null;
        private System.Windows.Controls.Label profitInfoLabel = null;
        private System.Windows.Controls.Label bogeyTargetInfoLabel = null;
        private System.Windows.Controls.Label dayOverMaxLossInfoLabel = null;
        private const string ToggleReverseButtonText = "Girar";
        private const string ToggleReverseButtonToolTip = "Girar posición";
        private const string ToggleReverseBButtonText = "RevB";
        private const string ToggleReverseBButtonToolTip = "Reverse Blended Positions";
        private const string ToggleCloseButtonText = "Cerrar";
        private const string ToggleCloseButtonToolTip = "Cerrar todas las posiciones en el activo actual en la cuenta actual";
        private const string ToggleFlatButtonText = "Flat";
        private const string ToggleFlatButtonToolTip = "Flatten Everything";
        private const string ToggleCloseBButtonText = "Cerrar B";
        private const string ToggleCloseBButtonToolTip = "Close Blended Positions";
        private const string ToggleTPButtonText = "TP+";
        private const string ToggleTPButtonToolTip = "Alejar Take-Profit del precio actual";
        private const string ToggleBEButtonText = "BE+";
        private const string ToggleBEButtonToolTip = "Acercar Break-Even al precio actual (asegurar mayor beneficio)";
        private const string ToggleSLButtonText = "SL+";
        private const string ToggleSLButtonToolTip = "Acercar Stop-Loss al precio actual (asegurar menor pérdida)";
        private const string ToggleBuyMarketButtonText = "Comprar";
        private const string ToggleBuyMarketButtonToolTip = "Ejecutar una posición de compra a mercado";
        private const string ToggleSellMarketButtonText = "Vender";
        private const string ToggleSellMarketButtonToolTip = "Ejecutar una posición de venta a mercado";
        private const string TogglePopBuyButtonText = "Pop+";
        private const string TogglePopBuyButtonToolTip = "Pop+ Buy Stop";
        private const string TogglePopSellButtonText = "Pop-";
        private const string TogglePopSellButtonToolTip = "Pop- Sell Stop";
        private const string ToggleDropBuyButtonText = "Drop+";
        private const string ToggleDropBuyButtonToolTip = "Drop+ Buy Limit";
        private const string ToggleDropSellButtonText = "Drop-";
        private const string ToggleDropSellButtonToolTip = "Drop- Sell Limit";
        private const string ToggleAutoBEButtonDisabledText = "AB.OFF";
        private const string ToggleAutoBEButtonDisabledToolTip = "Auto Break-Even Off";
        private const string ToggleAutoBEButtonEnabledText = "AB.ON";
        private const string ToggleAutoBEButtonEnabledToolTip = "Auto Break-Even On";
        private const string ToggleAutoBEHDLButtonEnabledText = "HODL";
        private const string ToggleAutoBEHDLButtonEnabledToolTip = "HODL";
        private const string ToggleCFTButtonEnabledText = "CF+T";
        private const string ToggleCFTButtonEnabledToolTip = "Creeper Flip + Trail";
        private const string ToggleAutoBETZRButtonEnabledText = "ZF+RT";
        private const string ToggleAutoBETZRButtonEnabledToolTip = "Zombie Flip + Resume Trail";
        private const string ToggleAutoBET5BButtonEnabledText = "AB+T5B";
        private const string ToggleAutoBET5BButtonEnabledToolTip = "Auto Break-Even + Trail 5 Bars";
        private const string ToggleAutoBET3BButtonEnabledText = "AB+T3B";
        private const string ToggleAutoBET3BButtonEnabledToolTip = "Auto Break-Even + Trail 3 Bars";
        private const string ToggleAutoBET2BButtonEnabledText = "AB+T2B";
        private const string ToggleAutoBET2BButtonEnabledToolTip = "Auto Break-Even + Trail 2 Bars";
        private const string ToggleAutoBET1BButtonEnabledText = "AB+T1B";
        private const string ToggleAutoBET1BButtonEnabledToolTip = "Auto Break-Even + Trail 1 Bar";
        private const string ToggleAutoBETM3ButtonEnabledText = "AB+TM3";
        private const string ToggleAutoBETM3ButtonEnabledToolTip = "Auto Break-Even + Trail MA 3";
        private const string ToggleAutoBETM2ButtonEnabledText = "AB+TM2";
        private const string ToggleAutoBETM2ButtonEnabledToolTip = "Auto Break-Even + Trail MA 2";
        private const string ToggleAutoBETM1ButtonEnabledText = "AB+TM1";
        private const string ToggleAutoBETM1ButtonEnabledToolTip = "Auto Break-Even + Trail MA 1";
        private const string ToggleAutoCloseECAButtonEnabledText = "ECAT";
        private const string ToggleAutoCloseECAButtonEnabledToolTip = "Equity Close All Target";
        private const string ToggleAutoCloseM1SButtonEnabledText = "AC.M1S";
        private const string ToggleAutoCloseM1SButtonEnabledToolTip = "Auto Close MA 1 Slope";
        private const string ToggleAutoCloseM1SMPButtonEnabledText = "AC.M1$";
        private const string ToggleAutoCloseM1SMPButtonEnabledToolTip = "Auto Close MA 1 Slope + Min Profit";
        private const string ToggleAutoCloseM2SButtonEnabledText = "AC.M2S";
        private const string ToggleAutoCloseM2SButtonEnabledToolTip = "Auto Close MA 2 Slope";
        private const string ToggleAutoCloseM2SMPButtonEnabledText = "AC.M2$";
        private const string ToggleAutoCloseM2SMPButtonEnabledToolTip = "Auto Close MA 2 Slope + Min Profit";
        private const string ToggleAutoCloseM3SButtonEnabledText = "AC.M3S";
        private const string ToggleAutoCloseM3SButtonEnabledToolTip = "Auto Close MA 3 Slope";
        private const string ToggleAutoCloseM3SMPButtonEnabledText = "AC.M3$";
        private const string ToggleAutoCloseM3SMPButtonEnabledToolTip = "Auto Close MA 3 Slope + Min Profit";
        private const string ToggleAutoCloseZFButtonEnabledText = "AC.ZF";
        private const string ToggleAutoCloseZFButtonEnabledToolTip = "Auto Close Zombie Flip";
        private const string ToggleAutoCloseZFMPButtonEnabledText = "AC.ZF$";
        private const string ToggleAutoCloseZFMPButtonEnabledToolTip = "Auto Close Zombie Flip + Min Profit";
        private const string ToggleAutoCloseCFButtonEnabledText = "AC.CF";
        private const string ToggleAutoCloseCFButtonEnabledToolTip = "Auto Close Creeper Flip";
        private const string ToggleAutoCloseCFMPButtonEnabledText = "AC.CF$";
        private const string ToggleAutoCloseCFMPButtonEnabledToolTip = "Auto Close Creeper Flip + Min Profit";
        private const string ToggleAutoCloseButtonDisabledText = "AC.OFF";
        private const string ToggleAutoCloseButtonDisabledToolTip = "Auto Close Off";
        private const string ToggleTradeSignalButtonDisabledText = "S.OFF";
        private const string ToggleTradeSignalButtonDisabledToolTip = "Trade Signal Off";
        private const string ToggleTradeSignalBSAButtonEnabledText = "S.BS.A";
        private const string ToggleTradeSignalBSAButtonEnabledTextToolTip = "Trade Signal All";
        private const string ToggleTradeSignalBSFButtonEnabledText = "S.BS.F";
        private const string ToggleTradeSignalBSFButtonEnabledTextToolTip = "Trade Signal Filtered";
        private const string ToggleTradeSignalBOButtonEnabledText = "S.BO";
        private const string ToggleTradeSignalBOButtonEnabledTextToolTip = "Trade Signal Buy Only";
        private const string ToggleTradeSignalSOButtonEnabledText = "S.SO";
        private const string ToggleTradeSignalSOButtonEnabledTextToolTip = "Trade Signal Sell Only";
        private string ToggleAutoEntryVolOption1ButtonEnabledText = "V(1)";
        private string ToggleAutoEntryVolOption1ButtonEnabledToolTip = "Volume (1)";
        private string ToggleAutoEntryVolOption2ButtonEnabledText = "V(2)";
        private string ToggleAutoEntryVolOption2ButtonEnabledToolTip = "Volume (2)";
        private string ToggleAutoEntryVolOption3ButtonEnabledText = "V(3)";
        private string ToggleAutoEntryVolOption3ButtonEnabledToolTip = "Volume (3)";
        private string ToggleAutoEntryVolOption4ButtonEnabledText = "V(4)";
        private string ToggleAutoEntryVolOption4ButtonEnabledToolTip = "Volume (4)";
        private string ToggleAutoEntryVolOption5ButtonEnabledText = "V(5)";
        private string ToggleAutoEntryVolOption5ButtonEnabledToolTip = "Volume (5)";
        private const string ToggleAutoPilotLiteButtonDisabledText = "APL.OFF";
        private const string ToggleAutoPilotLiteButtonDisabledToolTip = "Auto Pilot Lite Off";
        private const string ToggleAutoPilotLiteNext1ButtonEnabledText = "APL.N(1)";
        private const string ToggleAutoPilotLiteNext1ButtonEnabledToolTip = "Auto Pilot Lite Next Trade 1";
        private const string ToggleAutoPilotLiteBuy1ButtonEnabledText = "APL.B(1)";
        private const string ToggleAutoPilotLiteBuy1ButtonEnabledToolTip = "Auto Pilot Lite Buy Trade 1";
        private const string ToggleAutoPilotLiteSell1ButtonEnabledText = "APL.S(1)";
        private const string ToggleAutoPilotLiteSell1ButtonEnabledToolTip = "Auto Pilot Lite Sell Trade 1";
        private const string ToggleAutoPilotButtonDisabledText = "AP.OFF";
        private const string ToggleAutoPilotButtonDisabledToolTip = "Auto Pilot Off";
        private const string ToggleAutoPilotCount1ButtonEnabledText = "AP.T(1)";
        private const string ToggleAutoPilotCount1ButtonEnabledToolTip = "Auto Pilot Trade 1";
        private const string ToggleAutoPilotCount2ButtonEnabledText = "AP.T(2)";
        private const string ToggleAutoPilotCount2ButtonEnabledToolTip = "Auto Pilot Trade 2";
        private const string ToggleAutoPilotCount3ButtonEnabledText = "AP.T(3)";
        private const string ToggleAutoPilotCount3ButtonEnabledToolTip = "Auto Pilot Trade 3";
        private const string ToggleAutoPilotCount4ButtonEnabledText = "AP.T(4)";
        private const string ToggleAutoPilotCount4ButtonEnabledToolTip = "Auto Pilot Trade 4";
        private const string ToggleAutoPilotCount5ButtonEnabledText = "AP.T(5)";
        private const string ToggleAutoPilotCount5ButtonEnabledToolTip = "Auto Pilot Trade 5";
        private const string ToggleAutoPilotCount6ButtonEnabledText = "AP.T(6)";
        private const string ToggleAutoPilotCount6ButtonEnabledToolTip = "Auto Pilot Trade 6";
        private const string ToggleAutoPilotCount7ButtonEnabledText = "AP.T(7)";
        private const string ToggleAutoPilotCount7ButtonEnabledToolTip = "Auto Pilot Trade 7";
        private const string ToggleAutoPilotCount8ButtonEnabledText = "AP.T(8)";
        private const string ToggleAutoPilotCount8ButtonEnabledToolTip = "Auto Pilot Trade 8";
        private const string ToggleAutoPilotCount9ButtonEnabledText = "AP.T(9)";
        private const string ToggleAutoPilotCount9ButtonEnabledToolTip = "Auto Pilot Trade 9";
        private const string ToggleAutoPilotCount10ButtonEnabledText = "AP.T(10)";
        private const string ToggleAutoPilotCount10ButtonEnabledToolTip = "Auto Pilot Trade 10";
        private const string ToggleAutoAddOnButtonDisabledText = "AA.OFF";
        private const string ToggleAutoAddOnButtonDisabledToolTip = "Auto AddOn Off";
        private const string ToggleAutoAddOnFLAButtonEnabledText = "AA.FLA";
        private string ToggleAutoAddOnFLAButtonEnabledToolTip = "Auto AddOn Forward or Limit to Profit All";
        private const string ToggleAutoAddOnLTPAButtonEnabledText = "AA.LTPA";
        private string ToggleAutoAddOnLTPAButtonEnabledToolTip = "Auto AddOn Limit to Profit All";
        private const string ToggleAutoAddOnLTPFButtonEnabledText = "AA.LTPF";
        private string ToggleAutoAddOnLTPFButtonEnabledToolTip = "Auto AddOn Limit to Profit Forward";
        private const string ToggleAutoAddOnLTPBButtonEnabledText = "AA.LTPB";
        private string ToggleAutoAddOnLTPBButtonEnabledToolTip = "Auto AddOn Limit to Profit Back";
        private const string ToggleAutoAddOnAllButtonEnabledText = "AA.ALL";
        private string ToggleAutoAddOnAllButtonEnabledToolTip = "Auto AddOn All";
        private const string ToggleAutoAddOnForwardButtonEnabledText = "AA.FWD";
        private string ToggleAutoAddOnForwardButtonEnabledToolTip = "Auto AddOn Forward";
        private const string ToggleAutoAddOnBackButtonEnabledText = "AA.BCK";
        private string ToggleAutoAddOnBackButtonEnabledToolTip = "Auto AddOn Back";
        private const string ToggleBogeyTargetButtonDisabledText = "BT.OFF";
        private const string ToggleBogeyTargetButtonDisabledToolTip = "Bogey Target Off";
        private const string ToggleBogeyTargetX1ButtonEnabledText = "BT.X1";
        private string ToggleBogeyTargetX1ButtonEnabledToolTip = "Bogey Target X1";
        private const string ToggleBogeyTargetX2ButtonEnabledText = "BT.X2";
        private string ToggleBogeyTargetX2ButtonEnabledToolTip = "Bogey Target X2";
        private const string ToggleBogeyTargetX3ButtonEnabledText = "BT.X3";
        private string ToggleBogeyTargetX3ButtonEnabledToolTip = "Bogey Target X3";
        private const string ToggleBogeyTargetX4ButtonEnabledText = "BT.X4";
        private string ToggleBogeyTargetX4ButtonEnabledToolTip = "Bogey Target X4";
        private const string ToggleBogeyTargetX5ButtonEnabledText = "BT.X5";
        private string ToggleBogeyTargetX5ButtonEnabledToolTip = "Bogey Target X5";
        private const string ToggleBogeyTargetX6ButtonEnabledText = "BT.X6";
        private string ToggleBogeyTargetX6ButtonEnabledToolTip = "Bogey Target X6";
        private const string ToggleBogeyTargetX7ButtonEnabledText = "BT.X7";
        private string ToggleBogeyTargetX7ButtonEnabledToolTip = "Bogey Target X7";
        private const string ToggleBogeyTargetX8ButtonEnabledText = "BT.X8";
        private string ToggleBogeyTargetX8ButtonEnabledToolTip = "Bogey Target X8";
        private const string ToggleBogeyTargetX9ButtonEnabledText = "BT.X9";
        private string ToggleBogeyTargetX9ButtonEnabledToolTip = "Bogey Target X9";
        private const string ToggleBogeyTargetX10ButtonEnabledText = "BT.X10";
        private string ToggleBogeyTargetX10ButtonEnabledToolTip = "Bogey Target X10";
        private const string ToggleBogeyTargetX11ButtonEnabledText = "BT.X11";
        private string ToggleBogeyTargetX11ButtonEnabledToolTip = "Bogey Target X11";
        private const string ToggleBogeyTargetX12ButtonEnabledText = "BT.X12";
        private string ToggleBogeyTargetX12ButtonEnabledToolTip = "Bogey Target X12";
        private const string ToggleBogeyTargetX13ButtonEnabledText = "BT.X13";
        private string ToggleBogeyTargetX13ButtonEnabledToolTip = "Bogey Target X13";
        private const string ToggleBogeyTargetX14ButtonEnabledText = "BT.X14";
        private string ToggleBogeyTargetX14ButtonEnabledToolTip = "Bogey Target X14";
        private const string ToggleBogeyTargetX15ButtonEnabledText = "BT.X15";
        private string ToggleBogeyTargetX15ButtonEnabledToolTip = "Bogey Target X15";
        private const string ToggleBogeyTargetX16ButtonEnabledText = "BT.X16";
        private string ToggleBogeyTargetX16ButtonEnabledToolTip = "Bogey Target X16";
        private const string ToggleBogeyTargetX17ButtonEnabledText = "BT.X17";
        private string ToggleBogeyTargetX17ButtonEnabledToolTip = "Bogey Target X17";
        private const string ToggleBogeyTargetX18ButtonEnabledText = "BT.X18";
        private string ToggleBogeyTargetX18ButtonEnabledToolTip = "Bogey Target X18";
        private const string ToggleBogeyTargetX19ButtonEnabledText = "BT.X19";
        private string ToggleBogeyTargetX19ButtonEnabledToolTip = "Bogey Target X19";
        private const string ToggleBogeyTargetX20ButtonEnabledText = "BT.X20";
        private string ToggleBogeyTargetX20ButtonEnabledToolTip = "Bogey Target X20";
        private const string HHToggleAutoBEButtonName = "HHToggleAutoBEButton";
        private const string HHCloseAllButtonName = "HHCloseAllButton";
        private const string HHToggleAutoCloseButtonName = "HHToggleAutoCloseButton";
        private const string HHToggleEntryVolumeAutoButtonName = "HHToggleEntryVolumeAutoButton";
        private const string HHToggleTradeSignalButtonName = "HHToggleTradeSignalButton";
        private const string HHToggleAutoPilotButtonName = "HHToggleAutoPilotButton";
        private const string HHToggleAutoAddOnButtonName = "HHToggleAutoAddOnButton";
        private const string HHToggleBogeyTargetButtonName = "HHToggleBogeyTargetButton";
        private const string HHBEButtonName = "HHBEButton";
        private const string HHSLButtonName = "HHSLButton";
        private const string HHTPButtonName = "HHTPButton";
        private const string HHRevButtonName = "HHRevButton";
        private const string HHBuyDropButtonName = "HHBDropButton";
        private const string HHSellDropButtonName = "HHSDropButton";
        private const string HHBuyPopButtonName = "HHBPopButton";
        private const string HHSellPopButtonName = "HHSPopButton";
        private const string HHBuyMarketButtonName = "HHBMarketButton";
        private const string HHSellMarketButtonName = "HHSMarketButton";
        private const string HHRiskInfoLabelName = "HHRILabel";
        private const string HHProfitInfoLabelName = "HHPILabel";
        private const string HHBogeyTargetInfoLabelName = "HHBTILabel";
        private const string HHDayOverMaxLossInfoLabelName = "HHDOMLILabel";
        private const double MIN_EXCESS_MARGIN = 25;
        private const int MICRO_TO_EMINI_MULTIPLIER = 10;
        private const int DEFAULT_VOLUME_SIZE = 1;
        private const int BogeyTargetLevelLineChangePlotIndex = 0;
        private Brush bogeyTargetLevelLineBrush = Brushes.Transparent;
        private DashStyleHelper bogeyTargetLevelLineDashStyle;
        private int bogeyTargetLevelLineWidth = 1;
        private double bogeyTargetMultiplier = 1;
        private int bogeyTargetBaseVolumeSize = DEFAULT_VOLUME_SIZE;
        private const int DayOverMaxLossLevelLineChangePlotIndex = 1;
        private Brush dayOverMaxLossInfoTextColor = Brushes.Silver;
        private Brush dayOverMaxLossLevelLineBrush = Brushes.Transparent;
        private DashStyleHelper dayOverMaxLossLevelLineDashStyle;
        private int dayOverMaxLossLevelLineWidth = 1;
        private const int DayOverAccountBalanceFloorLevelLineChangePlotIndex = 2;
        private Brush dayOverAccountBalanceFloorLevelLineBrush = Brushes.Transparent;
        private DashStyleHelper dayOverAccountBalanceFloorLevelLineDashStyle;
        private int dayOverAccountBalanceFloorLevelLineWidth = 1;
        private const int ECATakeProfitLevelLineChangePlotIndex = 3;
        private Brush ecaTakeProfitLevelLineBrush = Brushes.Transparent;
        private DashStyleHelper ecaTakeProfitLevelLineDashStyle;
        private int ecaTakeProfitLevelLineWidth = 1;
        private const int AveragePriceLevelLineChangePlotIndex = 4;
        private Brush averagePriceLevelLineBrush = Brushes.Transparent;
        private DashStyleHelper averagePriceLevelLineDashStyle;
        private int averagePriceLevelLineWidth = 1;
        private double averagePriceLevelHorizontalOffset = 50;
        private JoiaGestorEntryVolumeAutoTypes currentEntryVolumeAutoStatus = JoiaGestorEntryVolumeAutoTypes.Option1;
        private JoiaGestorEntryVolumeAutoTypes lastToggleEntryVolumeAutoStatus = JoiaGestorEntryVolumeAutoTypes.Option1;
        private JoiaGestorEntryVolumeAutoTypes nextEntryVolumeAutoStatus = JoiaGestorEntryVolumeAutoTypes.Option1;
        private DateTime lastEntryVolumeAutoChangeTime = DateTime.MinValue;
        private const int EntryVolumeAutoColorDelaySeconds = 1;
        private const int AutoAddOnColorDelaySeconds = 5;
        private JoiaGestorCloseAutoTypes currentCloseAutoStatus = JoiaGestorCloseAutoTypes.Disabled;
        private JoiaGestorCloseAutoTypes lastToggleEntryCloseAutoStatus = JoiaGestorCloseAutoTypes.Disabled;
        private JoiaGestorCloseAutoTypes nextCloseAutoStatus = JoiaGestorCloseAutoTypes.Disabled;
        private DateTime lastCloseAutoChangeTime = DateTime.MinValue;
        private const int CloseAutoColorDelaySeconds = 5;
        private JoiaGestorBreakEvenAutoTypes currentBreakEvenAutoStatus = JoiaGestorBreakEvenAutoTypes.Disabled;
        private JoiaGestorBreakEvenAutoTypes lastToggleEntryBreakEvenAutoStatus = JoiaGestorBreakEvenAutoTypes.Disabled;
        private JoiaGestorBreakEvenAutoTypes nextBreakEvenAutoStatus = JoiaGestorBreakEvenAutoTypes.Disabled;
        private DateTime lastBreakEvenAutoChangeTime = DateTime.MinValue;
        private const int BeakEvenAutoColorDelaySeconds = 5;
        private string HedgehogEntrySymbol1FullName = "";
        private string HedgehogEntrySymbol2FullName = "";
        private Instrument attachedInstrument = null;
        private bool attachedInstrumentIsEmini = false;
        private bool attachedInstrumentIsFuture = false;
        private bool attachedInstrumentServerSupported = false;
        private double attachedInstrumentTickSize = 0;
        private double attachedInstrumentTickValue = 0;
        private int attachedInstrumentTicksPerPoint = 0;
        private int attachedInstrumentPositionMaxVolume = 0;
        private EMA autoCloseAndTrailMA1Buffer;
        private EMA autoCloseAndTrailMA2Buffer;
        private EMA autoCloseAndTrailMA3Buffer;
        private double autoCloseAndZombieFlipValue;
        private double autoCloseAndZombieFlipValue2;
        private double autoPilotSetupCreeperValue;
        private double autoPilotSetupCreeperValue2;
        private double autoPilotSetupCreeperValue3;
        private double autoPilotSetupCreeperValue4;
        private double autoPilotSetupCreeperValue5;
        private double autoPilotSetupCreeperValue6;
        private double autoPilotSetupCreeperValue7;
        private double autoPilotSetupCreeperValue8;
        private double autoPilotSetupCreeperValue9;
        private double autoPilotSetupCreeperValue10;
        private double autoCloseAndTrailMA1Value;
        private double autoCloseAndTrailMA1Value2;
        private double autoCloseAndTrailMA1Value3;
        private double autoCloseAndTrailMA2Value;
        private double autoCloseAndTrailMA2Value2;
        private double autoCloseAndTrailMA2Value3;
        private double autoCloseAndTrailMA3Value;
        private double autoCloseAndTrailMA3Value2;
        private double autoCloseAndTrailMA3Value3;
        private double snapPowerBoxUpperValue;
        private double snapPowerBoxLowerValue;
        private EMA autoPilotSetupWalkerBuffer;
        private double autoPilotSetupWalkerValue;
        private double autoPilotSetupWalkerValue2;
        private EMA autoPilotSpeedLineFilterBuffer;
        private double autoPilotSpeedLineFilterValue;
        private double autoPilotSpeedLineFilterValue2;
        private double autoPilotSpeedLineFilterValue3;
        private double autoPilotSpeedLineFilterValue4;
        private double autoPilotSpeedLineFilterValue5;
        private double autoPilotSpeedLineFilterValue6;
        private double autoPilotSpeedLineFilterValue7;
        private double autoPilotSpeedLineFilterValue8;
        private double autoPilotSpeedLineFilterValue9;
        private double autoPilotSpeedLineFilterValue10;
        private EMA autoPilotFilterBuffer;
        bool autoPilotBullishTrend = true;
        private double autoPilotSetupZombieValue;
        private double autoPilotSetupZombieValue2;
        private double autoPilotSetupZombieValue3;
        private double autoPilotSetupZombieValue4;
        private double autoPilotSetupZombieValue5;
        private double autoPilotSetupZombieValue6;
        private double autoPilotSetupZombieValue7;
        private double autoPilotSetupZombieValue8;
        private double autoPilotSetupZombieValue9;
        private double autoPilotSetupZombieValue10;
        private Instrument mymInstrument = null;
        private Instrument mesInstrument = null;
        private Instrument m2kInstrument = null;
        private Instrument mnqInstrument = null;
        private Instrument ymInstrument = null;
        private Instrument esInstrument = null;
        private Instrument rtyInstrument = null;
        private Instrument nqInstrument = null;
        private bool instrumentsSubscribed = false;
        private bool subscribedToOnOrderUpdate = false;
        private bool subscribedToPreviewMouseLeftButtonDown = false;
        private double mymLastAsk = 0;
        private double mymLastBid = 0;
        private double mesLastAsk = 0;
        private double mesLastBid = 0;
        private double m2kLastAsk = 0;
        private double m2kLastBid = 0;
        private double mnqLastAsk = 0;
        private double mnqLastBid = 0;
        private int marketDataBidAskPulseStatus = 0;
        private int marketDataBidAskChangeStatus = 0;
        private const string MYMPrefix = "MYM";
        private const string MESPrefix = "MES";
        private const string M2KPrefix = "M2K";
        private const string MNQPrefix = "MNQ";
        private double ymLastAsk = 0;
        private double ymLastBid = 0;
        private double esLastAsk = 0;
        private double esLastBid = 0;
        private double rtyLastAsk = 0;
        private double rtyLastBid = 0;
        private double nqLastAsk = 0;
        private double nqLastBid = 0;
        private const string YMPrefix = "YM";
        private const string ESPrefix = "ES";
        private const string RTYPrefix = "RTY";
        private const string NQPrefix = "NQ";
        private double maxDDInDollars = 0;
        private double lastAccountBalanceFloorDollars = 0;
        private bool validateAttachedPositionStopLossQuantity = false;
        private bool validateAttachedPositionTakeProfitQuantity = false;
        private bool validateBlendedPositionStopLossQuantity = false;
        private bool validateBlendedPositionTakeProfitQuantity = false;
        private bool riskInfoHasChanged = false;
        private MarketPosition riskInfoMarketPosition = MarketPosition.Flat;
        private int riskInfoQuantity = 0;
        private double riskInfoPositionPrice = 0;
        private bool profitInfoHasChanged = false;
        private MarketPosition profitInfoMarketPosition = MarketPosition.Flat;
        private int profitInfoQuantity = 0;
        private double profitInfoPositionPrice = 0;
        private double dayOverMaxLossDollars = 0;
        private bool dayOverMaxLossHasChanged = false;
        private MarketPosition dayOverMaxLossMarketPosition = MarketPosition.Flat;
        private int dayOverMaxLossPositionQuantity = 0;
        private double dayOverMaxLossPositionPrice = 0;
        
        private double lastDayOverMaxLossInfoDollars = 0;
        private string lastDayOverMaxLossLabelText = "";
        private double lastDayOverMaxLossDollars = 0;
        private MarketPosition lastDayOverMaxLossPositionType = MarketPosition.Flat;
        private double lastDayOverMaxLossClosedOrderProfit = 0;
        private double lastDayOverMaxLossPositionPrice = 0;
        private int lastDayOverMaxLossPositionQuantity = 0;
        private double lastDayOverMaxLossLevelLinePrice = 0;
        private bool activeDayOverMaxLossAutoClose = false;
        private bool dayOverMaxLossLineVisible = false;
        private bool bogeyTargetHasChanged = false;
        private MarketPosition bogeyTargetMarketPosition = MarketPosition.Flat;
        private int bogeyTargetPositionQuantity = 0;
        private double bogeyTargetPositionPrice = 0;
        private double lastBogeyTargetInfoDollars = 0;
        private double lastBogeyTargetBaseDollars = 0;
        private MarketPosition lastBogeyTargetPositionType = MarketPosition.Flat;
        private double lastBogeyTargetClosedOrderProfit = 0;
        private double lastBogeyTargetPositionPrice = 0;
        private int lastBogeyTargetPositionQuantity = 0;
        private double lastBogeyTargetLevelLinePrice = 0;
        private bool bogeyTargetLineVisible = false;
        private bool dayOverAccountBalanceFloorHasChanged = false;
        private MarketPosition dayOverAccountBalanceFloorMarketPosition = MarketPosition.Flat;
        private int dayOverAccountBalanceFloorPositionQuantity = 0;
        private double dayOverAccountBalanceFloorPositionPrice = 0;
        private double lastDayOverAccountBalanceFloorInfoDollars = 0;
        private double lastDayOverAccountBalanceFloorDollars = 0;
        private MarketPosition lastDayOverAccountBalanceFloorPositionType = MarketPosition.Flat;
        private double lastDayOverAccountBalanceFloorPositionPrice = 0;
        private int lastDayOverAccountBalanceFloorPositionQuantity = 0;
        private double lastDayOverAccountBalanceFloorLevelLinePrice = 0;
        private double lastDayOverAccountBalance = 0;
        private double lastDayOverAccountFloorInfoDollars = 0;
        private string lastDayOverAccountFloorLabelText = "";
        private DateTime lastDayOverAccountBalanceRefreshTime = DateTime.MinValue;
        private const int DayOverAccountBalanceRefreshDelaySeconds = 10;
        private bool dayOverAccountBalanceFloorLineVisible = false;
        private double cacheECATakeProfitDollars = 0;
        private bool ecaTakeProfitHasChanged = false;
        private MarketPosition ecaTakeProfitMarketPosition = MarketPosition.Flat;
        private int ecaTakeProfitPositionQuantity = 0;
        private double ecaTakeProfitPositionPrice = 0;
        private double lastECATakeProfitInfoDollars = 0;
        private double lastECATakeProfitDollars = 0;
        private MarketPosition lastECATakeProfitPositionType = MarketPosition.Flat;
        private double lastECATakeProfitClosedOrderProfit = 0;
        private double lastECATakeProfitPositionPrice = 0;
        private int lastECATakeProfitPositionQuantity = 0;
        private double lastECATakeProfitLevelLinePrice = 0;
        private bool ecaTakeProfitLineVisible = false;
        private bool averagePriceHasChanged = false;
        private MarketPosition averagePriceMarketPosition = MarketPosition.Flat;
        private int averagePricePositionQuantity = 0;
        private double averagePricePositionPrice = 0;
        private double lastAveragePriceInfoDollars = 0;
        private double lastAveragePriceDollars = 0;
        private MarketPosition lastAveragePricePositionType = MarketPosition.Flat;
        private double lastAveragePriceClosedOrderProfit = 0;
        private double lastAveragePricePositionPrice = 0;
        private int lastAveragePricePositionQuantity = 0;
        private double lastAveragePriceLevelLinePrice = 0;
        private bool averagePriceLineVisible = false;
        private bool attachedInstrumentHasChanged = false;
        private bool attachedInstrumentHasPosition = false;
        private MarketPosition attachedInstrumentMarketPosition = MarketPosition.Flat;
        private int attachedInstrumentPositionQuantity = 0;
        private double attachedInstrumentPositionPrice = 0;
        private double attachedInstrumentPositionStopLossPrice = 0;
        private double attachedInstrumentPositionTakeProfitPrice = 0;
        
        private MarketPosition lastAttachedInstrumentPositionType = MarketPosition.Flat;
        private double lastAttachedInstrumentPositionPrice = 0;
        private int lastAttachedInstrumentPositionQuantity = 0;
        private bool blendedInstrumentHasChanged = false;
        private bool blendedInstrumentHasPosition = false;
        private Instrument blendedInstrument = null;
        private MarketPosition blendedInstrumentMarketPosition = MarketPosition.Flat;
        private int blendedInstrumentPositionQuantity = 0;
        private double blendedInstrumentPositionPrice = 0;
        private double blendedInstrumentPositionStopLossPrice = 0;
        private double blendedInstrumentPositionTakeProfitPrice = 0;
        private double lastBlendedInstrumentInfoDollars = 0;
        private double lastBlendedInstrumentDollars = 0;
        private MarketPosition lastBlendedInstrumentPositionType = MarketPosition.Flat;
        private double lastBlendedInstrumentPositionPrice = 0;
        private int lastBlendedInstrumentPositionQuantity = 0;
        private double lastAccountIntradayExcessMargin = 0;
        private bool isInReplayMode = false;
        private ATR atrBuffer;
        private double atrValue = 0;
        private bool allButtonsDisabled = false;
        private double previous1ClosePrice = 0;
        private double previous2ClosePrice = 0;
        private double previous3ClosePrice = 0;
        private double previous4ClosePrice = 0;
        private double previous5ClosePrice = 0;
        private double previous6ClosePrice = 0;
        private double previous7ClosePrice = 0;
        private double previous8ClosePrice = 0;
        private double previous9ClosePrice = 0;
        private double previous10ClosePrice = 0;
        private double previous1LowPrice = 0;
        private double previous2LowPrice = 0;
        private double previous3LowPrice = 0;
        private double previous4LowPrice = 0;
        private double previous5LowPrice = 0;
        private double previous6LowPrice = 0;
        private double previous7LowPrice = 0;
        private double previous8LowPrice = 0;
        private double previous1HighPrice = 0;
        private double previous2HighPrice = 0;
        private double previous3HighPrice = 0;
        private double previous4HighPrice = 0;
        private double previous5HighPrice = 0;
        private double previous6HighPrice = 0;
        private double previous7HighPrice = 0;
        private double previous8HighPrice = 0;
        private bool previous1CandleBullish = false;
        private bool previous2CandleBullish = false;
        private bool previous3CandleBullish = false;
        private bool previous4CandleBullish = false;
        private bool previous5CandleBullish = false;
        private bool previous6CandleBullish = false;
        private bool previous7CandleBullish = false;
        private bool previous8CandleBullish = false;
        public override string DisplayName
        {
            get { return FullSystemName; }
        }
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = SystemName;
                Description = FullSystemName;
                Calculate = Calculate.OnPriceChange;
                MaximumBarsLookBack = MaximumBarsLookBack.TwoHundredFiftySix;
                IsOverlay = true;
                IsChartOnly = true;
                IsSuspendedWhileInactive = false;
                DisplayInDataBox = false;
                DrawOnPricePanel = true;
                PaintPriceMarkers = false;
                PrintTo = PrintTo.OutputTab1;
                UseAutoPositionStopLoss = false;
                UseAutoPositionTakeProfit = false;
                LimitAddOnVolumeToInProfit = false;
                AutoPositionCloseType = JoiaGestorCloseAutoTypes.Disabled;
                AutoPositionBreakEvenType = JoiaGestorBreakEvenAutoTypes.Disabled;
                StopLossInitialTicks = 21;
                StopLossInitialATRMultiplier = 0;
                StopLossInitialSnapType = JoiaGestorStopLossSnapTypes.Disabled;
                StopLossInitialMaxTicks = 0;
                StopLossInitialDollars = 0;
                StopLossInitialDollarsCombined = false;
                StopLossJumpTicks = 2;
                StopLossCTRLJumpTicks = true;
                StopLossRefreshOnVolumeChange = true;
                StopLossRefreshManagementEnabled = true;
                BreakEvenInitialTicks = 2;
                BreakEvenJumpTicks = 2;
                BreakEvenTurboJumpTicks = 4;
                BreakEvenAutoTriggerTicks = 15;
                BreakEvenAutoTriggerATRMultiplier = 0;
                TakeProfitInitialTicks = 45;
                TakeProfitInitialATRMultiplier = 0;
                TakeProfitSyncECATargetPrice = true;
                TakeProfitJumpTicks = 20;
                TakeProfitCtrlSLMultiplier = 2;
                TakeProfitRefreshManagementEnabled = true;
                ShowAveragePriceLine = true;
                ShowAveragePriceLineQuantity = true;
                ShowAveragePriceLineQuantityInMicros = false;
                ATRPeriod = 21;
                SnapPowerBoxPeriod = 8;
                SnapPowerBoxAutoAdjustPeriodsOnM1 = true;
                UseBlendedInstruments = false;
                UseIntradayMarginCheck = false;
                RefreshTPSLPaddingTicks = 0;
                RefreshTPSLOrderDelaySeconds = 0;
                SingleOrderChunkMaxQuantity = 10;
                SingleOrderChunkMinQuantity = 5;
                SingleOrderChunkDelayMilliseconds = 10;
                AutoCloseMinProfitDollarsPerVolume = 5;
                AutoCloseAndTrailMA1Period = 20;
                AutoCloseAndTrailMA2Period = 100;
                AutoCloseAndTrailMA3Period = 200;
                DayOverMaxLossDollars = 0;
                DayOverMaxLossBTBaseRatio = 2.5;
                DayOverAccountBalanceFloorDollars = 0;
                ECATargetDollars = 0;
                ECATargetDollarsPerOtherVolume = 5;
                ECATargetDollarsPerMNQVolume = 10;
                ECATargetDollarsPerNQVolume = 100;
                ECATargetDollarsPerM2KVolume = 5;
                ECATargetDollarsPerRTYVolume = 50;
                ECATargetDollarsPerMESVolume = 5;
                ECATargetDollarsPerESVolume = 50;
                ECATargetDollarsPerMYMVolume = 5;
                ECATargetDollarsPerYMVolume = 50;
                ECATargetATRMultiplierPerVolume = 0.5;
                ECAMaxDDInDollars = 1500;
                ExcessIntradayMarginMinDollars = 0;
                AutoEntryVolumeType = JoiaGestorEntryVolumeAutoTypes.Option1;
                AutoEntryVolumeOption1 = 1;
                AutoEntryVolumeOption2 = 2;
                AutoEntryVolumeOption3 = 3;
                AutoEntryVolumeOption4 = 4;
                AutoEntryVolumeOption5 = 5;
                UseHedgehogEntry = false;
                HedgehogEntryBuySymbol1SellSymbol2 = true;
                HedgehogEntrySymbol1 = "MES";
                HedgehogEntrySymbol2 = "M2K";
                UseAccountInfoLogging = false;
                AccountInfoLoggingPath = @"C:\MetaTrader\AccountInfo_NT.csv";
                UsePositionProfitLogging = false;
                DebugLogLevel = 0;
                OrderWaitOutputThrottleSeconds = 1;
                IgnoreInstrumentServerSupport = false;
                ShowButtonAutoBreakEven = true;
                ShowButtonReverse = true;
                ShowButtonClose = true;
                ShowButtonAutoClose = true;
                ShowButtonTPPlus = true;
                ShowButtonBEPlus = true;
                ShowButtonSLPlus = true;
                ShowButtonBuyMarket = true;
                ShowButtonSellMarket = true;
                ShowButtonVolume = true;
                AddPlot(new Stroke(Brushes.LimeGreen, DashStyleHelper.Solid, 3), PlotStyle.Line, "BogeyTargetLine");
                AddPlot(new Stroke(Brushes.Red, DashStyleHelper.Solid, 3), PlotStyle.Line, "DayOverMaxLossLine");
                AddPlot(new Stroke(Brushes.Red, DashStyleHelper.Dash, 3), PlotStyle.Line, "DayOverAccountBalanceFloorLine");
                AddPlot(new Stroke(Brushes.LimeGreen, DashStyleHelper.Dash, 3), PlotStyle.Line, "ECATakeProfitLine");
                AddPlot(new Stroke(Brushes.SkyBlue, DashStyleHelper.Solid, 3), PlotStyle.Line, "AveragePriceLine");
            }
            else if (State == State.Configure)
            {
                attachedInstrument = this.Instrument;
                attachedInstrumentIsEmini = IsEminiInstrument(attachedInstrument);
                attachedInstrumentIsFuture = RealInstrumentService.IsFutureInstrumentType(this.attachedInstrument);
                attachedInstrumentServerSupported = (IgnoreInstrumentServerSupport || this.Instrument.MasterInstrument.IsServerSupported);
                attachedInstrumentTickSize = RealInstrumentService.GetTickSize(attachedInstrument);
                attachedInstrumentTicksPerPoint = RealInstrumentService.GetTicksPerPoint(attachedInstrumentTickSize);
                attachedInstrumentTickValue = RealInstrumentService.GetTickValue(attachedInstrument);
                GenerateEntryVolumeAutoButtonText();
                currentEntryVolumeAutoStatus = AutoEntryVolumeType;
                lastToggleEntryVolumeAutoStatus = currentEntryVolumeAutoStatus;
                currentCloseAutoStatus = AutoPositionCloseType;
                currentBreakEvenAutoStatus = AutoPositionBreakEvenType;
                if (DayOverMaxLossBTBaseRatio > 0  && DayOverMaxLossDollars == 0)
                {
                    dayOverMaxLossDollars =  DayOverMaxLossBTBaseRatio;
                }
                else
                {
                    dayOverMaxLossDollars = DayOverMaxLossDollars;
                }
                if (this.ECAMaxDDInDollars == 0)
                    maxDDInDollars = this.ECAMaxDDInDollars;
                else
                    maxDDInDollars = this.ECAMaxDDInDollars * -1;
                if (attachedInstrumentServerSupported)
                {
                    if (attachedInstrumentIsFuture)
                    {
                        HedgehogEntrySymbol1FullName = HedgehogEntrySymbol1 + GetCurrentFuturesMonthYearPrefix();
                        HedgehogEntrySymbol2FullName = HedgehogEntrySymbol2 + GetCurrentFuturesMonthYearPrefix();
                        string mymFullName = MYMPrefix + GetCurrentFuturesMonthYearPrefix();
                        string mesFullName = MESPrefix + GetCurrentFuturesMonthYearPrefix();
                        string m2kFullName = M2KPrefix + GetCurrentFuturesMonthYearPrefix();
                        string mnqFullName = MNQPrefix + GetCurrentFuturesMonthYearPrefix();
                        string ymFullName = YMPrefix + GetCurrentFuturesMonthYearPrefix();
                        string esFullName = ESPrefix + GetCurrentFuturesMonthYearPrefix();
                        string rtyFullName = RTYPrefix + GetCurrentFuturesMonthYearPrefix();
                        string nqFullName = NQPrefix + GetCurrentFuturesMonthYearPrefix();
                        if (this.Instrument.FullName != mymFullName)
                        {
                            ValidateInstrument(mymFullName);
                        }
                        if (this.Instrument.FullName != mesFullName)
                        {
                            ValidateInstrument(mesFullName);
                        }
                        if (this.Instrument.FullName != m2kFullName)
                        {
                            ValidateInstrument(m2kFullName);
                        }
                        if (this.Instrument.FullName != mnqFullName)
                        {
                            ValidateInstrument(mnqFullName);
                        }
                        if (this.Instrument.FullName != ymFullName)
                        {
                            ValidateInstrument(ymFullName);
                        }
                        if (this.Instrument.FullName != esFullName)
                        {
                            ValidateInstrument(esFullName);
                        }
                        if (this.Instrument.FullName != rtyFullName)
                        {
                            ValidateInstrument(rtyFullName);
                        }
                        if (this.Instrument.FullName != nqFullName)
                        {
                            ValidateInstrument(nqFullName);
                        }
                        mymInstrument = Instrument.GetInstrument(mymFullName);
                        mesInstrument = Instrument.GetInstrument(mesFullName);
                        m2kInstrument = Instrument.GetInstrument(m2kFullName);
                        mnqInstrument = Instrument.GetInstrument(mnqFullName);
                        ymInstrument = Instrument.GetInstrument(ymFullName);
                        esInstrument = Instrument.GetInstrument(esFullName);
                        rtyInstrument = Instrument.GetInstrument(rtyFullName);
                        nqInstrument = Instrument.GetInstrument(nqFullName);
                        blendedInstrument = GetBlendedInstrument(attachedInstrument);
                        if (!instrumentsSubscribed)
                        {
                            if (this.DebugLogLevel > 10) RealLogger.PrintOutput("*** OnStateChange Subscribing to MarketDataUpdate (State.Configure):");
                            if (mymInstrument != null) WeakEventManager<Instrument, MarketDataEventArgs>.AddHandler(mymInstrument, "MarketDataUpdate", MarketData_Update);
                            if (mesInstrument != null) WeakEventManager<Instrument, MarketDataEventArgs>.AddHandler(mesInstrument, "MarketDataUpdate", MarketData_Update);
                            if (m2kInstrument != null) WeakEventManager<Instrument, MarketDataEventArgs>.AddHandler(m2kInstrument, "MarketDataUpdate", MarketData_Update);
                            if (mnqInstrument != null) WeakEventManager<Instrument, MarketDataEventArgs>.AddHandler(mnqInstrument, "MarketDataUpdate", MarketData_Update);
                            if (ymInstrument != null) WeakEventManager<Instrument, MarketDataEventArgs>.AddHandler(ymInstrument, "MarketDataUpdate", MarketData_Update);
                            if (esInstrument != null) WeakEventManager<Instrument, MarketDataEventArgs>.AddHandler(esInstrument, "MarketDataUpdate", MarketData_Update);
                            if (rtyInstrument != null) WeakEventManager<Instrument, MarketDataEventArgs>.AddHandler(rtyInstrument, "MarketDataUpdate", MarketData_Update);
                            if (nqInstrument != null) WeakEventManager<Instrument, MarketDataEventArgs>.AddHandler(nqInstrument, "MarketDataUpdate", MarketData_Update);
                            instrumentsSubscribed = true;
                        }
                    }
                }
                atrValue = 0;
                ForceRefresh();
            }
            else if (State == State.DataLoaded)
            {
                RealLogger.PrintOutput("Loading " + SystemVersion + " on " + this.attachedInstrument.FullName + " (" + BarsPeriod + ")", PrintTo.OutputTab1);
                RealLogger.PrintOutput("Loading " + SystemVersion + " on " + this.attachedInstrument.FullName + " (" + BarsPeriod + ")", PrintTo.OutputTab2);
                RealLogger.PrintOutput("IsFutureType=" + attachedInstrumentIsFuture.ToString() + " IsInstrumentServerSupported=" + attachedInstrumentServerSupported.ToString(), PrintTo.OutputTab2);
                if (attachedInstrumentServerSupported)
                {
                    hasRanOnceFirstCycle = false;
                    activeDayOverMaxLossAutoClose = false;
                    atrBuffer = ATR(ATRPeriod);
                    bogeyTargetLevelLineBrush = Plots[BogeyTargetLevelLineChangePlotIndex].Brush;
                    bogeyTargetLevelLineDashStyle = Plots[BogeyTargetLevelLineChangePlotIndex].DashStyleHelper;
                    bogeyTargetLevelLineWidth = (int)Plots[BogeyTargetLevelLineChangePlotIndex].Width;
                    bogeyTargetLevelLineBrush.Freeze();
                    dayOverMaxLossLevelLineBrush = Plots[DayOverMaxLossLevelLineChangePlotIndex].Brush;
                    dayOverMaxLossLevelLineDashStyle = Plots[DayOverMaxLossLevelLineChangePlotIndex].DashStyleHelper;
                    dayOverMaxLossLevelLineWidth = (int)Plots[DayOverMaxLossLevelLineChangePlotIndex].Width;
                    dayOverMaxLossLevelLineBrush.Freeze();
                    dayOverMaxLossInfoTextColor.Freeze();
                    dayOverAccountBalanceFloorLevelLineBrush = Plots[DayOverAccountBalanceFloorLevelLineChangePlotIndex].Brush;
                    dayOverAccountBalanceFloorLevelLineDashStyle = Plots[DayOverAccountBalanceFloorLevelLineChangePlotIndex].DashStyleHelper;
                    dayOverAccountBalanceFloorLevelLineWidth = (int)Plots[DayOverAccountBalanceFloorLevelLineChangePlotIndex].Width;
                    dayOverAccountBalanceFloorLevelLineBrush.Freeze();
                    ecaTakeProfitLevelLineBrush = Plots[ECATakeProfitLevelLineChangePlotIndex].Brush;
                    ecaTakeProfitLevelLineDashStyle = Plots[ECATakeProfitLevelLineChangePlotIndex].DashStyleHelper;
                    ecaTakeProfitLevelLineWidth = (int)Plots[ECATakeProfitLevelLineChangePlotIndex].Width;
                    ecaTakeProfitLevelLineBrush.Freeze();
                    averagePriceLevelLineBrush = Plots[AveragePriceLevelLineChangePlotIndex].Brush;
                    averagePriceLevelLineDashStyle = Plots[AveragePriceLevelLineChangePlotIndex].DashStyleHelper;
                    averagePriceLevelLineWidth = (int)Plots[AveragePriceLevelLineChangePlotIndex].Width;
                    averagePriceLevelLineBrush.Freeze();
                    autoCloseAndTrailMA1Buffer = EMA(Close, AutoCloseAndTrailMA1Period);
                    autoCloseAndTrailMA2Buffer = EMA(Close, AutoCloseAndTrailMA2Period);
                    autoCloseAndTrailMA3Buffer = EMA(Close, AutoCloseAndTrailMA3Period);
                    isInReplayMode = this.Bars.IsInReplayMode;
                    this.RealPositionService.IsInReplayMode = isInReplayMode;
                    if (BarsInProgress == 0 && ChartControl != null && timer == null)
                    {
                        ChartControl.Dispatcher.InvokeAsync(() =>
                        {
                            timer = new System.Windows.Threading.DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 250), IsEnabled = true };
                            WeakEventManager<System.Windows.Threading.DispatcherTimer, EventArgs>.AddHandler(timer, "Tick", OnTimerTick);
                        });
                    }
                    if (ChartControl != null)
                    {
                        ChartControl.Dispatcher.InvokeAsync(() =>
                        {
                            SubscribePreviewMouseLeftButtonDown();
                        });
                    }
                    /*
                    if (IsStrategyAttachedToChart())
                    {
                        if (ChartControl != null)
                        {
                            if (ChartControl.Dispatcher.CheckAccess())
                            {
                                DrawButtonPanel();
                            }
                            else
                            {
                                ChartControl.Dispatcher.InvokeAsync((() =>
                                {
                                   DrawButtonPanel();
                                }));
                            }
                        }
                    }
                    */
                }
            }
            else if (State == State.Terminated)
            {
                if (this.DebugLogLevel > 10) RealLogger.PrintOutput("*** OnStateChange State.Terminated:");
                hasRanOnceFirstCycle = false;
                hasDrawnButtons = false;
                activeDayOverMaxLossAutoClose = false;
                if (attachedInstrumentServerSupported)
                {
                    UnloadAccountEvents();
                    if (ChartControl != null)
                    {
                        ChartControl.Dispatcher.InvokeAsync(() =>
                        {
                            UnSubscribePreviewMouseLeftButtonDown();
                        });
                    }
                    if (attachedInstrumentIsFuture)
                    {
                        if (instrumentsSubscribed)
                        {
                            if (this.DebugLogLevel > 10) RealLogger.PrintOutput("*** OnStateChange Unsubscribing to MarketDataUpdate (State.Terminated):");
                            if (mymInstrument != null) WeakEventManager<Instrument, MarketDataEventArgs>.RemoveHandler(mymInstrument, "MarketDataUpdate", MarketData_Update);
                            if (mesInstrument != null) WeakEventManager<Instrument, MarketDataEventArgs>.RemoveHandler(mesInstrument, "MarketDataUpdate", MarketData_Update);
                            if (m2kInstrument != null) WeakEventManager<Instrument, MarketDataEventArgs>.RemoveHandler(m2kInstrument, "MarketDataUpdate", MarketData_Update);
                            if (mnqInstrument != null) WeakEventManager<Instrument, MarketDataEventArgs>.RemoveHandler(mnqInstrument, "MarketDataUpdate", MarketData_Update);
                            if (ymInstrument != null) WeakEventManager<Instrument, MarketDataEventArgs>.RemoveHandler(ymInstrument, "MarketDataUpdate", MarketData_Update);
                            if (esInstrument != null) WeakEventManager<Instrument, MarketDataEventArgs>.RemoveHandler(esInstrument, "MarketDataUpdate", MarketData_Update);
                            if (rtyInstrument != null) WeakEventManager<Instrument, MarketDataEventArgs>.RemoveHandler(rtyInstrument, "MarketDataUpdate", MarketData_Update);
                            if (nqInstrument != null) WeakEventManager<Instrument, MarketDataEventArgs>.RemoveHandler(nqInstrument, "MarketDataUpdate", MarketData_Update);
                            instrumentsSubscribed = false;
                        }
                    }
                    if (ChartControl != null && timer != null)
                    {
                        ChartControl.Dispatcher.InvokeAsync(() =>
                        {
                            WeakEventManager<System.Windows.Threading.DispatcherTimer, EventArgs>.RemoveHandler(timer, "Tick", OnTimerTick);
                            timer = null;
                        });
                    }
                    if (ChartControl != null)
                    {
                        if (ChartControl.Dispatcher.CheckAccess())
                        {
                            RemoveButtonPanel();
                        }
                        else
                        {
                            ChartControl.Dispatcher.InvokeAsync((() =>
                            {
                                RemoveButtonPanel();
                            }));
                        }
                    }
                }
            }
        }
        
        
        protected override void OnBarUpdate()
        {
            {
            }
            if (attachedInstrumentServerSupported)
            {
                RefreshAccount();
                RefreshATMStrategyName();
                lastAccountIntradayExcessMargin = GetAccountIntradayExcessMargin();
                if (CurrentBar > 11)
                {
                    previous1ClosePrice = Close[1];
                    previous2ClosePrice = Close[2];
                    previous3ClosePrice = Close[3];
                    previous4ClosePrice = Close[4];
                    previous5ClosePrice = Close[5];
                    previous6ClosePrice = Close[6];
                    previous7ClosePrice = Close[7];
                    previous8ClosePrice = Close[8];
                    previous9ClosePrice = Close[9];
                    previous10ClosePrice = Close[10];
                    previous1HighPrice = High[1];
                    previous2HighPrice = High[2];
                    previous3HighPrice = High[3];
                    previous4HighPrice = High[4];
                    previous5HighPrice = High[5];
                    previous6HighPrice = High[6];
                    previous7HighPrice = High[7];
                    previous8HighPrice = High[8];
                    previous1LowPrice = Low[1];
                    previous2LowPrice = Low[2];
                    previous3LowPrice = Low[3];
                    previous4LowPrice = Low[4];
                    previous5LowPrice = Low[5];
                    previous6LowPrice = Low[6];
                    previous7LowPrice = Low[7];
                    previous8LowPrice = Low[8];
                    previous1CandleBullish = previous1ClosePrice >= previous2ClosePrice;
                    previous2CandleBullish = previous2ClosePrice >= previous3ClosePrice;
                    previous3CandleBullish = previous3ClosePrice >= previous4ClosePrice;
                    previous4CandleBullish = previous4ClosePrice >= previous5ClosePrice;
                    previous5CandleBullish = previous5ClosePrice >= previous6ClosePrice;
                    previous6CandleBullish = previous6ClosePrice >= previous7ClosePrice;
                    previous7CandleBullish = previous7ClosePrice >= previous8ClosePrice;
                    previous8CandleBullish = previous8ClosePrice >= previous9ClosePrice;
                }
                if (StopLossInitialATRMultiplier > 0 || TakeProfitInitialATRMultiplier > 0 || ECATargetATRMultiplierPerVolume > 0)
                {
                    if (CurrentBar > ATRPeriod)
                    {
                        atrValue = atrBuffer[1];
                    }
                }
                RefreshObjects();
                if (CurrentBar > 5) 
                {
                    AutoAddOnRunOncePerBar.UpdateBarTime(Time[0]);
                    AutoCloseRunOncePerBar.UpdateBarTime(Time[0]);
                    AutoBreakEvenRunOncePerBar.UpdateBarTime(Time[0]);
                }
            }
        }
        private void RefreshObjects()
        {
            HandleEntryVolumeAutoStatusChange();
            HandleAutoCloseStatusChange();
            HandleAutoBreakEvenStatusChange();
        }
        private void HandleAutoBreakEvenStatusChange()
        {
            if (lastBreakEvenAutoChangeTime != DateTime.MinValue && ChartControl != null)
            {
                bool fullyEnabled = lastBreakEvenAutoChangeTime <= GetDateTimeNow();
                if (fullyEnabled)
                {
                    if (toggleAutoBEButton != null)
                    {
                        currentBreakEvenAutoStatus = nextBreakEvenAutoStatus;
                        lastBreakEvenAutoChangeTime = DateTime.MinValue;
                        nextBreakEvenAutoStatus = JoiaGestorBreakEvenAutoTypes.Disabled;
                        OnAutoBreakStatusChange();
                        ChartControl.Dispatcher.InvokeAsync((Action)(() =>
                        {
                            Brush buttonBGColor = (currentBreakEvenAutoStatus == JoiaGestorBreakEvenAutoTypes.Disabled) ? Brushes.DimGray : Brushes.HotPink;
                            toggleAutoBEButton.Background = buttonBGColor;
                        }));
                        RealLogger.PrintOutput("Activated break-even auto type " + currentBreakEvenAutoStatus);
                    }
                }
            }
        }
        private void OnAutoBreakStatusChange()
        {
            if (currentBreakEvenAutoStatus == JoiaGestorBreakEvenAutoTypes.HODL)
            {
                CancelPositionTPOrders("OnAutoBreakStatusChange", attachedInstrument);
                if (IsBlendedInstrumentEnabled())
                {
                    CancelPositionTPOrders("OnAutoBreakStatusChange", blendedInstrument);
                }
            }
        }
        private void HandleAutoCloseStatusChange()
        {
            if (lastCloseAutoChangeTime != DateTime.MinValue && ChartControl != null)
            {
                bool fullyEnabled = lastCloseAutoChangeTime <= GetDateTimeNow();
                if (fullyEnabled)
                {
                    if (toggleAutoCloseButton != null)
                    {
                        currentCloseAutoStatus = nextCloseAutoStatus;
                        lastCloseAutoChangeTime = DateTime.MinValue;
                        nextCloseAutoStatus = JoiaGestorCloseAutoTypes.Disabled;
                        ChartControl.Dispatcher.InvokeAsync((Action)(() =>
                        {
                            Brush buttonBGColor = (currentCloseAutoStatus == JoiaGestorCloseAutoTypes.Disabled) ? Brushes.DimGray : Brushes.HotPink;
                            toggleAutoCloseButton.Background = buttonBGColor;
                            closeAllButton.Content = (currentCloseAutoStatus == JoiaGestorCloseAutoTypes.EquityCloseAllTarget) ? ToggleFlatButtonText : (IsBlendedInstrumentEnabled()) ? ToggleCloseBButtonText : ToggleCloseButtonText;
                            closeAllButton.ToolTip = (currentCloseAutoStatus == JoiaGestorCloseAutoTypes.EquityCloseAllTarget) ? ToggleFlatButtonToolTip : (IsBlendedInstrumentEnabled()) ? ToggleCloseBButtonToolTip : ToggleCloseButtonToolTip;
                        }));
                        RealLogger.PrintOutput("Activated close auto type " + currentCloseAutoStatus);
                    }
                }
            }
        }
        private void HandleEntryVolumeAutoStatusChange()
        {
            if (lastEntryVolumeAutoChangeTime != DateTime.MinValue && ChartControl != null)
            {
                bool fullyEnabled = lastEntryVolumeAutoChangeTime <= GetDateTimeNow();
                if (fullyEnabled)
                {
                    if (toggleEntryVolumeAutoButton != null)
                    {
                        currentEntryVolumeAutoStatus = nextEntryVolumeAutoStatus;
                        lastEntryVolumeAutoChangeTime = DateTime.MinValue;
                        nextEntryVolumeAutoStatus = JoiaGestorEntryVolumeAutoTypes.Option1;
                        RealLogger.PrintOutput("Activated auto entry volume type " + currentEntryVolumeAutoStatus + " (" + CalculateAutoEntryVolume(currentEntryVolumeAutoStatus) + ")");
                    }
                }
            }
        }
        private bool IsBlendedInstrumentEnabled()
        {
            bool returnFLag = false;
            if (UseBlendedInstruments && blendedInstrument != null)
            {
                returnFLag = true;
            }
            return returnFLag;
        }
        private Instrument GetBlendedInstrument(Instrument instrument)
        {
            Instrument foundBlendedInstrument = null;
            if (instrument != null)
            {
                if (instrument == mymInstrument)
                {
                    foundBlendedInstrument = ymInstrument;
                }
                else if (instrument == mesInstrument)
                {
                    foundBlendedInstrument = esInstrument;
                }
                else if (instrument == m2kInstrument)
                {
                    foundBlendedInstrument = rtyInstrument;
                }
                else if (instrument == mnqInstrument)
                {
                    foundBlendedInstrument = nqInstrument;
                }
                else if (instrument == ymInstrument)
                {
                    foundBlendedInstrument = mymInstrument;
                }
                else if (instrument == esInstrument)
                {
                    foundBlendedInstrument = mesInstrument;
                }
                else if (instrument == rtyInstrument)
                {
                    foundBlendedInstrument = m2kInstrument;
                }
                else if (instrument == nqInstrument)
                {
                    foundBlendedInstrument = mnqInstrument;
                }
            }
            return foundBlendedInstrument;
        }
        private void RefreshAccount()
        {
            if (hasRanOnceFirstCycle)
            {
                Account tempAccount = GetAccount();
                if (account != null & tempAccount != account)
                {
                    hasRanOnceFirstCycle = false;
                }
            }
        }
        private void RefreshATMStrategyName()
        {
            if (hasRanOnceFirstCycle)
            {
                string tempATMStrategyName = GetATMStrategy();
                if (tempATMStrategyName != atmStrategyName)
                {
                    hasRanOnceFirstCycle = false;
                }
            }
        }
        private void MarketData_Update(object sender, MarketDataEventArgs e)
        {
            bool newBidAsk = false;
            if (e.Instrument != null)
            {
                Interlocked.Exchange(ref marketDataBidAskPulseStatus, 1);
                double lastPrice = 0;
                double newPrice = 0;
                if (e.MarketDataType == MarketDataType.Ask)
                {
                    lastPrice = RealInstrumentService.GetAskPrice(e.Instrument);
                    newPrice = e.Ask;
                    if (lastPrice != newPrice)
                    {
                        newBidAsk = true;
                        RealInstrumentService.SetAskPrice(e.Instrument, newPrice);
                    }
                }
                else if (e.MarketDataType == MarketDataType.Bid)
                {
                    lastPrice = RealInstrumentService.GetBidPrice(e.Instrument);
                    newPrice = e.Bid;
                    if (lastPrice != newPrice)
                    {
                        newBidAsk = true;
                        RealInstrumentService.SetBidPrice(e.Instrument, newPrice);
                    }
                }
                else if (e.MarketDataType == MarketDataType.Last)
                {
                    lastPrice = RealInstrumentService.GetLastPrice(e.Instrument);
                    newPrice = e.Last;
                    if (lastPrice != newPrice)
                    {
                        RealInstrumentService.SetLastPrice(e.Instrument, newPrice);
                    }
                }
                if (newBidAsk)
                {
                    Interlocked.Exchange(ref marketDataBidAskChangeStatus, 1);
                }
            }
            if (newBidAsk)
            {
                var lockTimeout = TimeSpan.FromSeconds(10);
                bool lockTaken = false;
                try
                {
                    Monitor.TryEnter(RealTimePipelineLock, lockTimeout, ref lockTaken);
                    if (lockTaken)
                    {
                        RealTimePipeline();
                    }
                }
                catch (Exception ex)
                {
                    RealLogger.PrintOutput("Exception calling RealTimePipeline:" + ex.Message + " " + ex.StackTrace);
                    throw;
                }
                finally
                {
                    if (lockTaken)
                        Monitor.Exit(RealTimePipelineLock);
                }
            }
        }
        private bool hasMultiCycleOrderElementsOutput = false;
        private bool hasActiveOrdersOutput = false;
        private bool hasOrderUpdateCycleOutput = false;
        private void RealTimePipeline()
        {
            const string signalName = "RealTimePipeline";
            if (HasRanOnceFirstCycle() && account != null)
            {
                IsNinjaTraderOrdersAlreadyLoaded = false;
                if (RealOrderService.AreAllOrderUpdateCyclesComplete())
                {
                    if (hasMultiCycleOrderElementsOutput)
                    {
                        RealLogger.PrintOutput("Multi-cycle order update(s) cleared...", PrintTo.OutputTab1, true);
                        hasMultiCycleOrderElementsOutput = false;
                    }
                    if (hasActiveOrdersOutput)
                    {
                        RealLogger.PrintOutput("Active order(s) cleared...", PrintTo.OutputTab1, true);
                        hasActiveOrdersOutput = false;
                    }
                    if (hasOrderUpdateCycleOutput)
                    {
                        RealLogger.PrintOutput("Order update cycle cleared...", PrintTo.OutputTab1, true);
                        hasOrderUpdateCycleOutput = false;
                    }
                    if (!IsAllButtonsDisabled()) AttemptToClosePositionsInProfit();
                    if (!IsAllButtonsDisabled()) AttemptToClosePositionsInLoss();
                    if (accountHadPositions && IsAccountFlat()
                        && RealOrderService.AreAllOrderUpdateCyclesComplete()
                        && RealOrderService.OrderCount == 0)
                    {
                        RealLogger.PrintOutput("Account is flat...", PrintTo.OutputTab1, true);
                        accountHadPositions = false;
                    }
                    if (!IsAccountFlat())
                    {
                        if (!accountHadPositions) RealLogger.PrintOutput("Account has active orders...", PrintTo.OutputTab1, true);
                        accountHadPositions = true;
                    }
                }
                else
                {
                    bool readyOutputWithThrottle = (lastOrderOutputTime == DateTime.MinValue || lastOrderOutputTime >= (GetDateTimeNow()).AddSeconds(OrderWaitOutputThrottleSeconds));
                    if (readyOutputWithThrottle)
                    {
                        if (RealOrderService.OrderUpdateMultiCycleCache.HasElements())
                        {
                            RealLogger.PrintOutput("Waiting on " + RealOrderService.OrderUpdateMultiCycleCache.Count.ToString() + " multi-cycle order update(s) to clear...", PrintTo.OutputTab1, true);
                            hasMultiCycleOrderElementsOutput = true;
                        }
                        else if (RealOrderService.HasActiveMarketOrders())
                        {
                            RealLogger.PrintOutput("Waiting on active orders to clear...", PrintTo.OutputTab1, true);
                            hasActiveOrdersOutput = true;
                        }
                        else
                        {
                            RealLogger.PrintOutput("Waiting on order update cycle to clear...", PrintTo.OutputTab1, true);
                            hasOrderUpdateCycleOutput = true;
                        }
                        lastOrderOutputTime = GetDateTimeNow();
                    }
                }
                if (!IsAllButtonsDisabled() && RealOrderService.AreAllOrderUpdateCyclesComplete())
                {
                    HandlePositionInfoRefresh(signalName);
                    RefreshDayOverLines();
                    HandleTPSLRefresh(signalName);
                    RefreshRiskInfoLabel();
                    RefreshProfitInfoLabel();
                    if ((dayOverMaxLossInfoLabel != null && dayOverMaxLossInfoLabel.Content != "")
                        || (bogeyTargetInfoLabel != null && bogeyTargetInfoLabel.Content != "")
                        || (riskInfoLabel != null && riskInfoLabel.Content != "")
                        || (profitInfoLabel != null && profitInfoLabel.Content != ""))
                    {
                        if (labelGrid != null)
                        {
                            labelGrid.Background = Brushes.Black;
                        }
                    }
                    else
                    {
                        if (labelGrid != null)
                        {
                            labelGrid.Background = Brushes.Transparent;
                        }
                    }
                    AttemptAccountInfoLogging();
                } 
            }
        }
        private void OnTimerTick(object sender, EventArgs e)
        {
            var lockTimeout = TimeSpan.FromSeconds(10);
            bool lockTaken = false;
            try
            {
                Monitor.TryEnter(DelayedPipelineLock, lockTimeout, ref lockTaken);
                if (lockTaken)
                {
                    DelayedPipeline();
                }
            }
            catch (Exception ex)
            {
                RealLogger.PrintOutput("Exception calling DelayedPipeline:" + ex.Message + " " + ex.StackTrace);
                throw;
            }
            finally
            {
                if (lockTaken)
                    Monitor.Exit(DelayedPipelineLock);
            }
        }
        private void DelayedPipeline()
        {
            const string signalName = "DelayedPipeline";
            if (!hasDrawnButtons)
            {
                if (IsStrategyAttachedToChart() && HasRanOnceFirstCycle())
                {
                    if (hasRanOnceFirstCycle && !hasDrawnButtons)
                    {
                        ForceRefresh();
                    }
                }
            }
        }
        private void ResetPositionTPSLOrderDelayOrderDelay()
        {
            lock (PositionTPSLOrderDelayLock)
            {
                lastPositionQuantityChange = DateTime.MinValue;
            }
        }
        private void SetPositionTPSLOrderDelayOrderDelay()
        {
            lock (PositionTPSLOrderDelayLock)
            {
                if (RefreshTPSLOrderDelaySeconds > 0)
                    lastPositionQuantityChange = (GetDateTimeNow()).AddSeconds(RefreshTPSLOrderDelaySeconds);
                else
                    lastPositionQuantityChange = GetDateTimeNow();
            }
        }
        private DateTime GetDateTimeNow()
        {
            DateTime now;
            if (isInReplayMode)
            {
                now = NinjaTrader.Cbi.Connection.PlaybackConnection.Now;
            }
            else
            {
                now = DateTime.Now;
            }
            return now;
        }
        private bool HasPositionTPSLOrderDelay()
        {
            bool returnFlag = false;
            if (RefreshTPSLOrderDelaySeconds > 0)
            {
                lock (PositionTPSLOrderDelayLock)
                {
                    bool delayTPSLOrders = (lastPositionQuantityChange >= GetDateTimeNow());
                    if (delayTPSLOrders)
                        returnFlag = true;
                }
            }
            return returnFlag;
        }
        private void OnOrderUpdate(object sender, OrderEventArgs e)
        {
            try
            {
                if (e != null && e.Order != null)
                {
                    RealOrderService.InOrderUpdateCycleIncrement();
                    bool hasPositionQuantityChanged = false;
                    int remainingPositionQuantity = 0;
                    RealOrder updatedOrder = RealOrderService.BuildRealOrder(e.Order.Account, e.Order.Instrument, e.Order.Id, e.Order.OrderId, e.Order.Name, e.Order.OrderType, e.Order.OrderAction,
                        e.Order.Quantity, e.Order.QuantityChanged,
                        e.Order.LimitPrice, e.Order.LimitPriceChanged, e.Order.StopPrice, e.Order.StopPriceChanged, e.OrderState, e.Order.Filled);
                    RealOrderService.AddOrUpdateOrder(updatedOrder);
                    string instrumentName = attachedInstrument.FullName;
                    bool isAttachedInstrument = e.Order.Instrument == attachedInstrument;
                    bool isBlendedInstrument = IsBlendedInstrumentEnabled() && e.Order.Instrument == blendedInstrument;
                    string orderUniqueId = RealOrderService.BuildOrderUniqueId(e.Order);
                    if (DebugLogLevel > 15 && isAttachedInstrument && !RealOrderService.OrderUpdateMultiCycleCache.HasElements()) RealLogger.PrintOutput("***** OnOrderUpdate-" + instrumentName + ": START *****");
                    bool isOrderInitialized = (e.Order.OrderState == OrderState.Initialized);
                    bool isOrderCancelPending = (e.Order.OrderState == OrderState.CancelPending || e.Order.OrderState == OrderState.CancelSubmitted);
                    bool isChangePending = (e.Order.OrderState == OrderState.ChangePending || e.Order.OrderState == OrderState.ChangeSubmitted);
                    bool isStopOrderWorking = (e.Order.IsStopMarket && e.Order.OrderState == OrderState.Working);
                    if (isOrderInitialized || isOrderCancelPending || isChangePending)
                    {
                        bool addedToMultiCycleCache = RealOrderService.OrderUpdateMultiCycleCache.RegisterUniqueId(orderUniqueId);
                        if (DebugLogLevel > 5 && isAttachedInstrument && addedToMultiCycleCache) RealLogger.PrintOutput("OnOrderUpdate-" + instrumentName + ": Adding to cache OrderState=" + e.Order.OrderState.ToString() + " name=" + e.Order.Name + " OrderId=" + e.Order.OrderId + " InternalId=" + orderUniqueId + " orderaction=" + e.Order.OrderAction.ToString() + " orderType=" + e.Order.OrderType.ToString() + " orderQuan=" + e.Order.Quantity.ToString());
                    }
                    bool isCancelPendingOrdersActivated = ((e.Order.IsStopMarket || e.Order.IsLimit) && e.Order.OrderState == OrderState.CancelSubmitted);
                    bool isFilledPendingOrdersActivated = (!e.Order.IsMarket && (e.Order.OrderState == OrderState.Filled || e.Order.OrderState == OrderState.PartFilled));
                    bool isRejected = e.Order.OrderState == OrderState.Rejected;
                    if (isRejected)
                    {
                        if (isAttachedInstrument || isBlendedInstrument)
                        {
                            RealLogger.PrintOutput("OnOrderUpdate-" + instrumentName + ": rejected OrderState=" + e.Order.OrderState.ToString() + " name=" + e.Order.Name + " OrderId=" + e.Order.OrderId + " InternalId=" + orderUniqueId + " price=" + Math.Max(e.Order.StopPrice, e.Order.LimitPrice).ToString() + " orderaction=" + e.Order.OrderAction.ToString() + " lastPrice=" + RealInstrumentService.GetLastPrice(e.Order.Instrument).ToString() + " bidPrice=" + RealInstrumentService.GetBidPrice(e.Order.Instrument).ToString() + " askPrice=" + RealInstrumentService.GetAskPrice(e.Order.Instrument).ToString() + " instrument=" + e.Order.Instrument.FullName);
                            bool isStopLossOrder = (e.Order.Name == RealOrderService.BuildStopOrderName());
                            bool isTakeProfitOrder = (e.Order.Name == RealOrderService.BuildTargetOrderName());
                            if (isStopLossOrder || isTakeProfitOrder)
                            {
                                RealPosition foundPosition = null;
                                if (RealPositionService.TryGetByInstrumentFullName(e.Order.Instrument.FullName, out foundPosition))
                                {
                                    if (foundPosition.IsValid)
                                    {
                                        foundPosition.IsValid = false;
                                        string errorMessage = SystemName + " can no longer protect your position with a stoploss or take profit order due to a rejection from the exchange.Refresh indicators using F5 or close position to reset protection.";
                                        Dispatcher.InvokeAsync(() =>
                                        {
                                            NinjaTrader.Gui.Tools.NTMessageBoxSimple.Show(Window.GetWindow(ChartControl.OwnerChart as DependencyObject), errorMessage, FullSystemName, MessageBoxButton.OK, MessageBoxImage.Stop);
                                        });
                                    }
                                    RealLogger.PrintOutput("***CRITICAL ERROR: OnOrderUpdate-" + instrumentName + ": rejected OrderState=" + e.Order.OrderState.ToString() + " - JoiaGestor no longer protecting with SL/TP due to exchange rejection.  Refresh indicators using F5 or close position to reset protection.");
                                }
                            }
                        }
                    }
                    if (isCancelPendingOrdersActivated || isFilledPendingOrdersActivated || isStopOrderWorking)
                    {
                    }
                    bool hasFilledOrder = e.Order.Filled > 0 && (e.Order.OrderState == OrderState.PartFilled || e.Order.OrderState == OrderState.Filled);
                    if (hasFilledOrder)
                    {
                        int filledQuantity = RealOrderService.GetFilledOrderQuantity(e.Order);
                        if (DebugLogLevel > 15) RealLogger.PrintOutput("***** OnOrderUpdate-" + instrumentName + ": order filled OrderState=" + e.Order.OrderState.ToString() + " name=" + e.Order.Name + " OrderId=" + e.Order.OrderId + " InternalId=" + orderUniqueId + " quantity=" + e.Order.Quantity + " filled=" + e.Order.Filled + " orderaction=" + e.Order.OrderAction.ToString() + " positionCount=" + Convert.ToString(RealPositionService.PositionCount));
                        MarketPosition marketPosition = ConvertOrderActionToMarketPosition(e.Order.OrderAction);
                        RealPosition newPosition = RealPositionService.BuildRealPosition(e.Order.Account, e.Order.Instrument, marketPosition, filledQuantity, e.Order.AverageFillPrice, GetDateTimeNow());
                        if (DebugLogLevel > 15) RealLogger.PrintOutput("OnOrderUpdate before AddUpdate positioncount=" + RealPositionService.PositionCount + " filledQuantity=" + filledQuantity + " orderFilled=" + e.Order.Filled + " OrderState = " + e.Order.OrderState.ToString());
                        remainingPositionQuantity = RealPositionService.AddOrUpdatePosition(newPosition);
                        if (DebugLogLevel > 15) RealLogger.PrintOutput("OnOrderUpdate after AddUpdate remainingPositionQuantity=" + remainingPositionQuantity);
                        if (DebugLogLevel > 2)
                        {
                            if (DebugLogLevel > 2 && isAttachedInstrument) RealLogger.PrintOutput("OnOrderUpdate-" + instrumentName + ": order filled OrderState=" + e.Order.OrderState.ToString() + " name=" + e.Order.Name + " OrderId=" + e.Order.OrderId + " InternalId=" + orderUniqueId + " quantity=" + e.Order.Quantity + " filled=" + e.Order.Filled + " orderaction=" + e.Order.OrderAction.ToString() + " positionCount=" + Convert.ToString(RealPositionService.PositionCount) + " filledquan=" + filledQuantity.ToString() + " poQuan=" + remainingPositionQuantity.ToString() + " instrument=" + e.Order.Instrument.FullName);
                        }
                        if (remainingPositionQuantity > 0) hasPositionQuantityChanged = true;
                    }
                    bool isCompletedMarketOrder = (e.Order.IsMarket && Order.IsTerminalState(e.Order.OrderState));
                    bool isCompletedStopOrder = (e.Order.IsStopMarket && (e.Order.OrderState == OrderState.Accepted || e.Order.OrderState == OrderState.Working));
                    bool isCompletedLimitOrder = (e.Order.IsLimit && (e.Order.OrderState == OrderState.Accepted || e.Order.OrderState == OrderState.Working));
                    bool isCompletedCancelledOrder = (e.Order.OrderState == OrderState.Cancelled);
                    bool isFilledStopOrder = (e.Order.IsStopMarket && e.Order.OrderState == OrderState.Filled || e.Order.OrderState == OrderState.PartFilled);
                    bool isFilledLimit = (e.Order.IsLimit && e.Order.OrderState == OrderState.Filled || e.Order.OrderState == OrderState.PartFilled);
                    bool foundOrderUniqueId = false;
                    foundOrderUniqueId = RealOrderService.OrderUpdateMultiCycleCache.ContainsKey(orderUniqueId);
                    if (foundOrderUniqueId && 
                        (isCompletedMarketOrder || isCompletedStopOrder || isCompletedLimitOrder || isRejected || isCompletedCancelledOrder || isFilledStopOrder || isFilledLimit))
                    {
                        if (DebugLogLevel > 5 && isAttachedInstrument) RealLogger.PrintOutput("OnOrderUpdate-" + instrumentName + ": Removing from cache OrderState=" + e.Order.OrderState.ToString() + " name=" + e.Order.Name + " OrderId=" + e.Order.OrderId + " InternalId=" + orderUniqueId + " orderaction=" + e.Order.OrderAction.ToString() + " orderType=" + e.Order.OrderType.ToString());
                        RealOrderService.OrderUpdateMultiCycleCache.DeregisterUniqueId(orderUniqueId);
                    }
                    else
                    {
                        bool foundInCache = false;
                        if (foundOrderUniqueId)
                            foundInCache = RealOrderService.OrderUpdateMultiCycleCache.TouchUniqueId(orderUniqueId);
                        if (DebugLogLevel > 8 && isAttachedInstrument) RealLogger.PrintOutput("OnOrderUpdate-" + instrumentName + ": Not removed from cache OrderState=" + e.Order.OrderState.ToString() + " name=" + e.Order.Name + " OrderId=" + e.Order.OrderId + " InternalId=" + orderUniqueId + " orderaction=" + e.Order.OrderAction.ToString() + " orderType=" + e.Order.OrderType.ToString() + " foundInCache=" + foundInCache);
                    }
                    if (Order.IsTerminalState(e.Order.OrderState))  
                    {
                        RealOrderService.RemoveOrder(e.Order.Id);
                        int orderCount = RealOrderService.OrderCount;
                        for (int index = 0; index < orderCount; index++)
                        {
                            RealOrder order = null;
                            if (RealOrderService.TryGetByIndex(index, out order))
                            {
                            }
                        }
                    }
                    else
                    {
                    }
                    if (hasPositionQuantityChanged)
                    {
                        if (remainingPositionQuantity > 0)
                        {
                            validateAttachedPositionStopLossQuantity = true;
                            validateAttachedPositionTakeProfitQuantity = true;
                            validateBlendedPositionStopLossQuantity = true;
                            validateBlendedPositionTakeProfitQuantity = true;
                        }
                        else
                        {
                            validateAttachedPositionStopLossQuantity = false;
                            validateAttachedPositionTakeProfitQuantity = false;
                            validateBlendedPositionStopLossQuantity = false;
                            validateBlendedPositionTakeProfitQuantity = false;
                        }
                    }
                    if (DebugLogLevel > 15 && isAttachedInstrument && !RealOrderService.OrderUpdateMultiCycleCache.HasElements()) RealLogger.PrintOutput("***** OnOrderUpdate-" + instrumentName + ": END *****");
                }
            }
            catch (Exception ex)
            {
                RealLogger.PrintOutput("Exception calling OnOrderUpdate:" + ex.Message + " " + ex.StackTrace);
            }
            finally
            {
                RealOrderService.InOrderUpdateCycleDecrement();
            }
        }
        private void LoadPositions()
        {
            RealPositionService.LoadPositions(account);
            RealOrderService.LoadOrders(account, RealPositionService.PositionCount);
            if (RealPositionService.PositionCount > 0)
            {
                validateAttachedPositionStopLossQuantity = true;
                validateAttachedPositionTakeProfitQuantity = true;
                validateBlendedPositionStopLossQuantity = true;
                validateBlendedPositionTakeProfitQuantity = true;
            }
        }
        private string EscapeKeyName(string keyName)
        {
            string newKeyName = keyName.Replace(' ', '_');
            return newKeyName;
        }
        string BuildBogeyTargetHLineKey()
        {
            string key = BuildObjectFullName("dayoverbt_");
            return key;
        }
        string BuildDayOverMaxLossHLineKey()
        {
            string key = BuildObjectFullName("dayoverdml_");
            return key;
        }
        string BuildDayOverAccountBalanceFloorHLineKey()
        {
            string key = BuildObjectFullName("dayoverabfl_");
            return key;
        }
        string BuildECATakeProfitHLineKey()
        {
            string key = BuildObjectFullName("ecatpl_");
            return key;
        }
        string BuildAveragePriceHLineKey()
        {
            string key = BuildObjectFullName("avgpl_");
            return key;
        }
        private string BuildObjectFullName(string name)
        {
            string fullName = ObjectPrefix + name;
            return fullName;
        }
        private bool RemoveDayOverMaxLossLine()
        {
            bool returnFlag = false;
            if (dayOverMaxLossLineVisible)
            {
                string key = BuildDayOverMaxLossHLineKey();
                TriggerCustomEvent(o =>
                {
                    RemoveDrawObject(key);
                }, null);
                dayOverMaxLossLineVisible = false;
                returnFlag = true;
            }
            return returnFlag;
        }
        private bool RemoveBogeyTargetLine()
        {
            bool returnFlag = false;
            if (bogeyTargetLineVisible)
            {
                string key = BuildBogeyTargetHLineKey();
                TriggerCustomEvent(o =>
                {
                    RemoveDrawObject(key);
                }, null);
                bogeyTargetLineVisible = false;
                returnFlag = true;
            }
            return returnFlag;
        }
        private bool RemoveDayOverAccountBalanceFloorLine()
        {
            bool returnFlag = false;
            if (dayOverAccountBalanceFloorLineVisible)
            {
                string key = BuildDayOverAccountBalanceFloorHLineKey();
                TriggerCustomEvent(o =>
                {
                    RemoveDrawObject(key);
                }, null);
                dayOverAccountBalanceFloorLineVisible = false;
                returnFlag = true;
            }
            return returnFlag;
        }
        private bool RemoveECATakeProfitLine()
        {
            bool returnFlag = false;
            if (ecaTakeProfitLineVisible)
            {
                string key = BuildECATakeProfitHLineKey();
                TriggerCustomEvent(o =>
                {
                    RemoveDrawObject(key);
                }, null);
                ecaTakeProfitLineVisible = false;
                returnFlag = true;
            }
            return returnFlag;
        }
        private bool RemoveAveragePriceLine()
        {
            bool returnFlag = false;
            if (averagePriceLineVisible)
            {
                string key = BuildAveragePriceHLineKey();
                TriggerCustomEvent(o =>
                {
                    RemoveDrawObject(key);
                }, null);
                averagePriceLineVisible = false;
                returnFlag = true;
            }
            return returnFlag;
        }
        private void DrawHLine(string key, double price, Brush lineColor, DashStyleHelper lineDashStyle, int lineWidth, int zOrder = 10000)
        {
            TriggerCustomEvent(o =>
            {
                HorizontalLine tempObject = Draw.HorizontalLine(this, key, false, price, lineColor, lineDashStyle, lineWidth);
                tempObject.IsLocked = true;
                tempObject.IgnoresUserInput = true;
                tempObject.ZOrder = zOrder;
                tempObject.Dispose();
            }, null);
        }
        private void DrawLabeledHLine(string key, double price, Brush lineColor, DashStyleHelper lineDashStyle, int lineWidth, string text, double horizontalOffset, int zOrder = 10000)
        {
            TriggerCustomEvent(o =>
            {
                int fontSize = 13;
                int opacity = 100;
                ZLabeledHorizontalLine tempObject = DrawZLabledLine.ZLabeledHorizontalLine(this, key, false, price, lineColor, lineDashStyle, lineWidth);
                tempObject.AppendPriceTime = false;
                tempObject.DisplayText = text;
                tempObject.Font = new SimpleFont(this.ChartControl.Properties.LabelFont.Family.ToString(), fontSize);
                tempObject.HorizontalOffset = horizontalOffset;
                tempObject.OutlineStroke = new Stroke(lineColor, lineWidth);
                tempObject.AreaOpacity = opacity;
                tempObject.TextBrush = Brushes.Black;
                tempObject.BackgroundBrush = lineColor;
                tempObject.ZOrder = zOrder;
                tempObject.IsLocked = true;
                tempObject.IgnoresUserInput = true;
                tempObject.Dispose();
            }, null);
        }
        private void RefreshDayOverLines()
        {
            bool removedLine = false;
            if (IsDayOverAccountBalanceFloorEnabled() && dayOverAccountBalanceFloorHasChanged)
            {
                if (IsDayOverAccountBalanceFloorEnabled())
                {
                    bool readyForRefresh = lastDayOverAccountBalanceRefreshTime <= GetDateTimeNow();
                    if (readyForRefresh || lastDayOverAccountBalance <= 0)
                    {
                        lastDayOverAccountBalance = Math.Round(account.Get(AccountItem.CashValue, Currency.UsDollar), 2);
                        lastDayOverAccountBalanceRefreshTime = (GetDateTimeNow()).AddSeconds(DayOverAccountBalanceRefreshDelaySeconds);
                    }
                }
                bool lastDayOverAccountBalanceFloorDollarsChanged = lastDayOverAccountBalance != lastDayOverAccountBalanceFloorDollars;
                if (lastDayOverAccountBalanceFloorDollarsChanged) lastDayOverAccountBalanceFloorDollars = lastDayOverAccountBalance;
                bool lastDayOverAccountBalanceFloorPositionTypeChanged = dayOverAccountBalanceFloorMarketPosition != lastDayOverAccountBalanceFloorPositionType;
                bool lastDayOverAccountBalanceFloorPositionPriceChanged = dayOverAccountBalanceFloorPositionPrice != lastDayOverAccountBalanceFloorPositionPrice;
                bool lastDayOverAccountBalanceFloorPositionQuantityChanged = dayOverAccountBalanceFloorPositionQuantity != lastDayOverAccountBalanceFloorPositionQuantity;
                if (attachedInstrumentHasPosition && lastDayOverAccountBalance > 0 &&
                    (lastDayOverAccountBalanceFloorPositionTypeChanged ||
                    lastDayOverAccountBalanceFloorDollarsChanged ||
                    lastDayOverAccountBalanceFloorPositionPriceChanged ||
                    lastDayOverAccountBalanceFloorPositionQuantityChanged))
                {
                    lastDayOverAccountBalanceFloorPositionType = dayOverAccountBalanceFloorMarketPosition;
                    lastDayOverAccountBalanceFloorPositionPrice = dayOverAccountBalanceFloorPositionPrice;
                    lastDayOverAccountBalanceFloorPositionQuantity = dayOverAccountBalanceFloorPositionQuantity;
                    double newLinePrice = 0;
                    double equityToFloorDiff = lastDayOverAccountBalance - DayOverAccountBalanceFloorDollars;
                    if (lastDayOverAccountBalanceFloorPositionType == MarketPosition.Long)
                    {
                        int balanceToFloorRemainingTicks = (int)Math.Ceiling((equityToFloorDiff / (lastDayOverAccountBalanceFloorPositionQuantity * attachedInstrumentTickValue)));
                        newLinePrice = lastDayOverAccountBalanceFloorPositionPrice - (balanceToFloorRemainingTicks * attachedInstrumentTickSize);
                    }
                    else if (lastDayOverAccountBalanceFloorPositionType == MarketPosition.Short)
                    {
                        int balanceToFloorRemainingTicks = (int)Math.Floor(equityToFloorDiff / (lastDayOverAccountBalanceFloorPositionQuantity * attachedInstrumentTickValue));
                        newLinePrice = lastDayOverAccountBalanceFloorPositionPrice + (balanceToFloorRemainingTicks * attachedInstrumentTickSize);
                    }
                    if (newLinePrice != 0)
                    {
                        bool dayOverAccountBalanceFloorLinePriceChanged = (newLinePrice != lastDayOverAccountBalanceFloorLevelLinePrice);
                        if (dayOverAccountBalanceFloorLinePriceChanged)
                        {
                            lastDayOverAccountBalanceFloorLevelLinePrice = newLinePrice;
                            string key = BuildDayOverAccountBalanceFloorHLineKey();
                            DrawHLine(key, lastDayOverAccountBalanceFloorLevelLinePrice, dayOverAccountBalanceFloorLevelLineBrush, dayOverAccountBalanceFloorLevelLineDashStyle, dayOverAccountBalanceFloorLevelLineWidth);
                            dayOverAccountBalanceFloorLineVisible = true;
                        }
                    }
                }
            }
            if (dayOverAccountBalanceFloorLineVisible && (!attachedInstrumentHasPosition || !IsDayOverAccountBalanceFloorEnabled()))
            {
                lastDayOverAccountBalanceRefreshTime = GetDateTimeNow();
                lastDayOverAccountBalanceFloorPositionType = MarketPosition.Flat;
                lastDayOverAccountBalanceFloorLevelLinePrice = 0;
                lastDayOverAccountBalanceFloorPositionPrice = 0;
                lastDayOverAccountBalanceFloorPositionQuantity = 0;
                removedLine = (RemoveDayOverAccountBalanceFloorLine()) ? true : removedLine;
            }
            if (IsDayOverMaxLossEnabled() && dayOverMaxLossHasChanged)
            {
                double newLastDayOverMaxLossClosedOrderProfit = Math.Round(account.Get(AccountItem.RealizedProfitLoss, Currency.UsDollar), 2);
                bool lastDayOverMaxLossClosedOrderProfitChanged = newLastDayOverMaxLossClosedOrderProfit != lastDayOverMaxLossClosedOrderProfit;
                if (lastDayOverMaxLossClosedOrderProfitChanged) lastDayOverMaxLossClosedOrderProfit = newLastDayOverMaxLossClosedOrderProfit;
                double newDayOverMaxLossDollars = 0;
                newDayOverMaxLossDollars = this.dayOverMaxLossDollars + lastDayOverMaxLossClosedOrderProfit;
                bool lastDayOverMaxLossDollarsChanged = newDayOverMaxLossDollars != lastDayOverMaxLossDollars;
                if (lastDayOverMaxLossDollarsChanged) lastDayOverMaxLossDollars = newDayOverMaxLossDollars;
                bool lastDayOverMaxLossPositionTypeChanged = dayOverMaxLossMarketPosition != lastDayOverMaxLossPositionType;
                bool lastDayOverMaxLossPositionPriceChanged = dayOverMaxLossPositionPrice != lastDayOverMaxLossPositionPrice;
                bool lastDayOverMaxLossPositionQuantityChanged = dayOverMaxLossPositionQuantity != lastDayOverMaxLossPositionQuantity;
                if (lastDayOverMaxLossDollars <= 0)
                {
                    activeDayOverMaxLossAutoClose = true;
                }
                else
                {
                    activeDayOverMaxLossAutoClose = false;
                }
                if (attachedInstrumentHasPosition || blendedInstrumentHasPosition)
                {
                    if (!activeDayOverMaxLossAutoClose &&
                        (lastDayOverMaxLossPositionTypeChanged || lastDayOverMaxLossDollarsChanged || lastDayOverMaxLossPositionPriceChanged || lastDayOverMaxLossPositionQuantityChanged))
                    {
                        lastDayOverMaxLossPositionType = dayOverMaxLossMarketPosition;
                        lastDayOverMaxLossPositionPrice = dayOverMaxLossPositionPrice;
                        lastDayOverMaxLossPositionQuantity = dayOverMaxLossPositionQuantity;
                        double newLinePrice = 0;
                        if (lastDayOverMaxLossPositionType == MarketPosition.Long)
                        {
                            newLinePrice = GetDayOverMaxLossFromDollars(MarketPosition.Long, lastDayOverMaxLossPositionPrice, lastDayOverMaxLossPositionQuantity, lastDayOverMaxLossDollars);
                        }
                        else if (lastDayOverMaxLossPositionType == MarketPosition.Short)
                        {
                            newLinePrice = GetDayOverMaxLossFromDollars(MarketPosition.Short, lastDayOverMaxLossPositionPrice, lastDayOverMaxLossPositionQuantity, lastDayOverMaxLossDollars);
                        }
                        if (newLinePrice != 0)
                        {
                            bool dayOverMaxLossLinePriceChanged = (newLinePrice != lastDayOverMaxLossLevelLinePrice);
                            if (dayOverMaxLossLinePriceChanged)
                            {
                                lastDayOverMaxLossLevelLinePrice = newLinePrice;
                                string key = BuildDayOverMaxLossHLineKey();
                                DrawHLine(key, lastDayOverMaxLossLevelLinePrice, dayOverMaxLossLevelLineBrush, dayOverMaxLossLevelLineDashStyle, dayOverMaxLossLevelLineWidth);
                                dayOverMaxLossLineVisible = true;
                                dayOverMaxLossLinePriceChanged = false;
                            }
                        }
                    }
                }
            }
            if (dayOverMaxLossLineVisible && ((!attachedInstrumentHasPosition && !blendedInstrumentHasPosition) || !IsDayOverMaxLossEnabled()))
            {
                lastDayOverMaxLossPositionType = MarketPosition.Flat;
                lastDayOverMaxLossLevelLinePrice = 0;
                lastDayOverMaxLossPositionPrice = 0;
                lastDayOverMaxLossPositionQuantity = 0;
                removedLine = (RemoveDayOverMaxLossLine()) ? true : removedLine;
            }
            if (dayOverMaxLossInfoLabel != null && (dayOverMaxLossHasChanged || dayOverAccountBalanceFloorHasChanged))
            {
                bool lastDayOverAccountFloorInfoDollarsChanged = false;
                bool lastDayOverMaxLossInfoDollarsChanged = false;
                if (IsDayOverAccountBalanceFloorEnabled() && dayOverAccountBalanceFloorHasChanged)
                {
                    lastDayOverAccountFloorInfoDollarsChanged = lastDayOverAccountFloorInfoDollars != DayOverAccountBalanceFloorDollars;
                    if (lastDayOverAccountFloorInfoDollarsChanged) lastDayOverAccountFloorInfoDollars = DayOverAccountBalanceFloorDollars;
                    if (lastDayOverAccountFloorInfoDollarsChanged)
                    {
                        string formattedAccountFloorDollars = "";
                        formattedAccountFloorDollars = "$" + lastDayOverAccountFloorInfoDollars.ToString("N0");
                        lastDayOverAccountFloorLabelText = " Floor: " + formattedAccountFloorDollars + " ";
                    }
                }
                if (IsDayOverMaxLossEnabled() && dayOverMaxLossHasChanged)
                {
                    lastDayOverMaxLossInfoDollarsChanged = lastDayOverMaxLossInfoDollars != lastDayOverMaxLossDollars;
                    if (lastDayOverMaxLossInfoDollarsChanged) lastDayOverMaxLossInfoDollars = lastDayOverMaxLossDollars;
                    if (lastDayOverMaxLossInfoDollarsChanged)
                    {
                        double tempDayOverMaxLossRemaining = lastDayOverMaxLossDollars;
                        string formattedDailyMaxDollars = "";
                        if (lastDayOverMaxLossDollars > 0)
                        {
                            formattedDailyMaxDollars = "$" + tempDayOverMaxLossRemaining.ToString("N0");
                        }
                        else
                        {
                            formattedDailyMaxDollars = "($" + tempDayOverMaxLossRemaining.ToString("N0") + ")";
                        }
                        lastDayOverMaxLossLabelText = " MaxLoss: $" + dayOverMaxLossDollars.ToString("N0") + " / " + formattedDailyMaxDollars + " ";
                    }
                }
                if (lastDayOverAccountFloorInfoDollarsChanged || lastDayOverMaxLossInfoDollarsChanged)
                {
                    if (lastDayOverMaxLossLabelText != "" && lastDayOverAccountFloorLabelText != "")
                    {
                        dayOverMaxLossInfoLabel.Content = lastDayOverMaxLossLabelText + "   " + lastDayOverAccountFloorLabelText;
                    }
                    else if (lastDayOverMaxLossLabelText != "" && lastDayOverAccountFloorLabelText == "")
                    {
                        dayOverMaxLossInfoLabel.Content = lastDayOverMaxLossLabelText;
                    }
                    else if (lastDayOverMaxLossLabelText == "" && lastDayOverAccountFloorLabelText != "")
                    {
                        dayOverMaxLossInfoLabel.Content = lastDayOverAccountFloorLabelText;
                    }
                    else
                    {
                        dayOverMaxLossInfoLabel.Content = "";
                    }
                }
            }
            dayOverAccountBalanceFloorHasChanged = false;
            dayOverMaxLossHasChanged = false;
            bogeyTargetHasChanged = false;
            bool isECATPEnabled = (currentCloseAutoStatus == JoiaGestorCloseAutoTypes.EquityCloseAllTarget);
            if (isECATPEnabled && ecaTakeProfitHasChanged)
            {
                if (attachedInstrumentHasPosition || blendedInstrumentHasPosition)
                {
                    bool lastECATakeProfitDollarsChanged = cacheECATakeProfitDollars != lastECATakeProfitDollars;
                    if (lastECATakeProfitDollarsChanged) lastECATakeProfitDollars = cacheECATakeProfitDollars;
                    bool lastECATakeProfitPositionTypeChanged = ecaTakeProfitMarketPosition != lastECATakeProfitPositionType;
                    bool lastECATakeProfitPositionPriceChanged = ecaTakeProfitPositionPrice != lastECATakeProfitPositionPrice;
                    bool lastECATakeProfitPositionQuantityChanged = ecaTakeProfitPositionQuantity != lastECATakeProfitPositionQuantity;
                    if (attachedInstrumentHasPosition || blendedInstrumentHasPosition)
                    {
                        if (lastECATakeProfitPositionTypeChanged || lastECATakeProfitDollarsChanged || lastECATakeProfitPositionPriceChanged || lastECATakeProfitPositionQuantityChanged)
                        {
                            lastECATakeProfitPositionType = ecaTakeProfitMarketPosition;
                            lastECATakeProfitPositionPrice = ecaTakeProfitPositionPrice;
                            lastECATakeProfitPositionQuantity = ecaTakeProfitPositionQuantity;
                            double newLinePrice = 0;
                            if (lastECATakeProfitPositionType == MarketPosition.Long)
                            {
                                newLinePrice = GetECATakeProfitPriceFromDollars(MarketPosition.Long, lastECATakeProfitPositionPrice, lastECATakeProfitPositionQuantity, lastECATakeProfitDollars);
                            }
                            else if (lastECATakeProfitPositionType == MarketPosition.Short)
                            {
                                newLinePrice = GetECATakeProfitPriceFromDollars(MarketPosition.Short, lastECATakeProfitPositionPrice, lastECATakeProfitPositionQuantity, lastECATakeProfitDollars);
                            }
                            if (newLinePrice != 0)
                            {
                                bool ecaTakeProfitLinePriceChanged = (newLinePrice != lastECATakeProfitLevelLinePrice);
                                if (ecaTakeProfitLinePriceChanged)
                                {
                                    lastECATakeProfitLevelLinePrice = newLinePrice;
                                    string key = BuildECATakeProfitHLineKey();
                                    DrawHLine(key, lastECATakeProfitLevelLinePrice, ecaTakeProfitLevelLineBrush, ecaTakeProfitLevelLineDashStyle, ecaTakeProfitLevelLineWidth);
                                    ecaTakeProfitLineVisible = true;
                                    ecaTakeProfitLinePriceChanged = false;
                                }
                            }
                        }
                    }
                }
            }
            isECATPEnabled = (currentCloseAutoStatus == JoiaGestorCloseAutoTypes.EquityCloseAllTarget);
            if (ecaTakeProfitLineVisible && ((!attachedInstrumentHasPosition && !blendedInstrumentHasPosition) || !isECATPEnabled))
            {
                lastECATakeProfitPositionType = MarketPosition.Flat;
                lastECATakeProfitLevelLinePrice = 0;
                lastECATakeProfitPositionPrice = 0;
                lastECATakeProfitPositionQuantity = 0;
                removedLine = (RemoveECATakeProfitLine()) ? true : removedLine;
            }
            ecaTakeProfitHasChanged = false;
            if (averagePriceHasChanged)
            {
                if (attachedInstrumentHasPosition || blendedInstrumentHasPosition)
                {
                    bool lastAveragePricePositionTypeChanged = averagePriceMarketPosition != lastAveragePricePositionType;
                    bool lastAveragePricePositionPriceChanged = averagePricePositionPrice != lastAveragePricePositionPrice;
                    bool lastAveragePricePositionQuantityChanged = averagePricePositionQuantity != lastAveragePricePositionQuantity;
                    if (lastAveragePricePositionTypeChanged || lastAveragePricePositionPriceChanged || lastAveragePricePositionQuantityChanged)
                    {
                        lastAveragePricePositionType = averagePriceMarketPosition;
                        lastAveragePricePositionPrice = averagePricePositionPrice;
                        lastAveragePricePositionQuantity = averagePricePositionQuantity;
                        double newLinePrice = 0;
                        newLinePrice = lastAveragePricePositionPrice;
                        if (newLinePrice != 0)
                        {
                            bool averagePriceLinePriceChanged = (newLinePrice != lastAveragePriceLevelLinePrice
                                || lastAveragePricePositionPriceChanged || lastAveragePricePositionQuantityChanged);
                            if (averagePriceLinePriceChanged)
                            {
                                lastAveragePriceLevelLinePrice = newLinePrice;
                                string key = BuildAveragePriceHLineKey();
                                if (ShowAveragePriceLine)
                                {
                                    string formatter = "N" + RealInstrumentService.GetTickSizeDecimalPlaces(attachedInstrumentTickSize);
                                    string lineText = lastAveragePriceLevelLinePrice.ToString(formatter);
                                    if (ShowAveragePriceLineQuantity)
                                    {
                                        lineText += " (" + lastAveragePricePositionQuantity.ToString("N0") + ")";
                                    }
                                    DrawLabeledHLine(key, lastAveragePriceLevelLinePrice, averagePriceLevelLineBrush, averagePriceLevelLineDashStyle,
                                        averagePriceLevelLineWidth, lineText, averagePriceLevelHorizontalOffset, AveragePriceLineZOrder);
                                    averagePriceLineVisible = true;
                                }
                                averagePriceLinePriceChanged = false;
                            }
                        }
                    }
                }
            }
            if (averagePriceLineVisible && !attachedInstrumentHasPosition && !blendedInstrumentHasPosition)
            {
                lastAveragePricePositionType = MarketPosition.Flat;
                lastAveragePricePositionPrice = 0;
                lastAveragePricePositionQuantity = 0;
                lastAveragePriceLevelLinePrice = 0;
                removedLine = (RemoveAveragePriceLine()) ? true : removedLine;
            }
            averagePriceHasChanged = false;
        }
        private void RefreshProfitInfoLabel()
        {
            if (profitInfoLabel != null && profitInfoHasChanged)
            {
                double profitInfoTotalDollars = 0;
                int profitInfoAttachedTicks = 0;
                double profitInfoAttachedDollars = 0;
                bool attachedInstrumentHasTakeProfit = attachedInstrumentPositionTakeProfitPrice > 0;
                bool blendedInstrumentHasTakeProfit = blendedInstrumentPositionTakeProfitPrice > 0;
                if (attachedInstrumentHasPosition)
                {
                    profitInfoAttachedDollars = Math.Round(GetPositionProfitWithStoLoss(attachedInstrument, attachedInstrumentMarketPosition, attachedInstrumentPositionQuantity, attachedInstrumentPositionPrice, attachedInstrumentPositionTakeProfitPrice), 2);
                    if (attachedInstrumentHasTakeProfit) profitInfoAttachedTicks = (int)Math.Round(((profitInfoAttachedDollars / attachedInstrumentPositionQuantity) / RealInstrumentService.GetTickValue(attachedInstrument)), MidpointRounding.ToEven);
                    profitInfoTotalDollars += profitInfoAttachedDollars;
                }
                double profitInfoBlendedDollars = 0;
                double profitInfoBlendedTicks = 0;
                if (blendedInstrumentHasPosition)
                {
                    profitInfoBlendedDollars = Math.Round(GetPositionProfitWithStoLoss(blendedInstrument, blendedInstrumentMarketPosition, blendedInstrumentPositionQuantity, blendedInstrumentPositionPrice, blendedInstrumentPositionTakeProfitPrice), 2);
                    if (blendedInstrumentHasTakeProfit) profitInfoBlendedTicks = (int)Math.Round(((profitInfoBlendedDollars / blendedInstrumentPositionQuantity) / RealInstrumentService.GetTickValue(blendedInstrument)), MidpointRounding.ToEven);
                    profitInfoTotalDollars += profitInfoBlendedDollars;
                }
                if ((attachedInstrumentHasPosition && profitInfoHasChanged) || (blendedInstrumentHasPosition && profitInfoHasChanged))
                {
                    string profitDollarText = "";
                    string profitFullText = "";
                    if (profitInfoTotalDollars >= 0)
                    {
                        profitInfoLabel.Foreground = Brushes.LightGreen;
                        profitDollarText = "$" + profitInfoTotalDollars.ToString("N2");
                    }
                    else
                    {
                        profitInfoLabel.Foreground = Brushes.Tomato;
                        profitDollarText = "-$" + profitInfoTotalDollars.ToString("N2").Replace("-", "");
                    }
                    if (attachedInstrumentHasPosition
                        && attachedInstrumentHasTakeProfit
                        && profitInfoAttachedTicks != 0
                        && blendedInstrumentHasPosition
                        && blendedInstrumentHasTakeProfit
                        && profitInfoBlendedTicks != 0)
                    {
                        int profitInfoMaxTicks = (int)Math.Max(profitInfoAttachedTicks, profitInfoBlendedTicks);
                        profitFullText = " TakeProfit: " + profitDollarText + " (" + profitInfoMaxTicks.ToString("N0") + ") ";
                    }
                    else if (attachedInstrumentHasPosition
                        && attachedInstrumentHasTakeProfit
                        && profitInfoAttachedTicks != 0
                        && !blendedInstrumentHasPosition
                        && !blendedInstrumentHasTakeProfit
                        && profitInfoBlendedTicks == 0)
                    {
                        profitFullText = " TakeProfit: " + profitDollarText + " (" + profitInfoAttachedTicks.ToString("N0") + ") ";
                    }
                    else if (!attachedInstrumentHasPosition
                        && !attachedInstrumentHasTakeProfit
                        && profitInfoAttachedTicks == 0
                        && blendedInstrumentHasPosition
                        && blendedInstrumentHasTakeProfit
                        && profitInfoBlendedTicks != 0)
                    {
                        profitFullText = " TakeProfit: " + profitDollarText + " (" + profitInfoBlendedTicks.ToString("N0") + ") ";
                    }
                    else
                    {
                        profitInfoLabel.Foreground = Brushes.White;
                        profitFullText = " TakeProfit: " + "none ";
                    }
                    profitInfoLabel.Content = profitFullText;
                }
                else
                {
                    profitInfoLabel.Foreground = Brushes.White;
                    profitInfoLabel.Content = "";
                }
                profitInfoHasChanged = false;
            }
        }
        private void RefreshRiskInfoLabel()
        {
            if (riskInfoLabel != null && riskInfoHasChanged)
            {
                double riskInfoTotalDollars = 0;
                int riskInfoAttachedTicks = 0;
                double riskInfoAttachedDollars = 0;
                bool attachedInstrumentHasStopLoss = attachedInstrumentPositionStopLossPrice > 0;
                bool blendedInstrumentHasStopLoss = blendedInstrumentPositionStopLossPrice > 0;
                if (attachedInstrumentHasPosition)
                {
                    riskInfoAttachedDollars = Math.Round(GetPositionProfitWithStoLoss(attachedInstrument, attachedInstrumentMarketPosition, attachedInstrumentPositionQuantity, attachedInstrumentPositionPrice, attachedInstrumentPositionStopLossPrice), 2);
                    if (attachedInstrumentHasStopLoss) riskInfoAttachedTicks = (int)Math.Round(((riskInfoAttachedDollars / attachedInstrumentPositionQuantity) / RealInstrumentService.GetTickValue(attachedInstrument)), MidpointRounding.ToEven);
                    riskInfoTotalDollars += riskInfoAttachedDollars;
                }
                double riskInfoBlendedDollars = 0;
                double riskInfoBlendedTicks = 0;
                if (blendedInstrumentHasPosition)
                {
                    riskInfoBlendedDollars = Math.Round(GetPositionProfitWithStoLoss(blendedInstrument, blendedInstrumentMarketPosition, blendedInstrumentPositionQuantity, blendedInstrumentPositionPrice, blendedInstrumentPositionStopLossPrice), 2);
                    if (blendedInstrumentHasStopLoss) riskInfoBlendedTicks = (int)Math.Round(((riskInfoBlendedDollars / blendedInstrumentPositionQuantity) / RealInstrumentService.GetTickValue(blendedInstrument)), MidpointRounding.ToEven);
                    riskInfoTotalDollars += riskInfoBlendedDollars;
                }
                if ((attachedInstrumentHasPosition && riskInfoHasChanged) || (blendedInstrumentHasPosition && riskInfoHasChanged))
                {
                    string riskDollarText = "";
                    string riskFullText = "";
                    if (riskInfoTotalDollars >= 0)
                    {
                        riskInfoLabel.Foreground = Brushes.LightGreen;
                        riskDollarText = "$" + riskInfoTotalDollars.ToString("N2");
                    }
                    else
                    {
                        riskInfoLabel.Foreground = Brushes.Tomato;
                        riskDollarText = "-$" + riskInfoTotalDollars.ToString("N2").Replace("-", "");
                    }
                    if (attachedInstrumentHasPosition
                        && attachedInstrumentHasStopLoss
                        && riskInfoAttachedTicks != 0
                        && blendedInstrumentHasPosition
                        && blendedInstrumentHasStopLoss
                        && riskInfoBlendedTicks != 0)
                    {
                        int riskInfoMaxTicks = (int)Math.Max(riskInfoAttachedTicks, riskInfoBlendedTicks);
                        riskFullText = " StopLoss: " + riskDollarText + " (" + riskInfoMaxTicks.ToString("N0") + ") ";
                    }
                    else if (attachedInstrumentHasPosition
                        && attachedInstrumentHasStopLoss 
                        && riskInfoAttachedTicks != 0
                        && !blendedInstrumentHasPosition
                        && !blendedInstrumentHasStopLoss
                        && riskInfoBlendedTicks == 0)
                    {
                        riskFullText = " StopLoss: " + riskDollarText + " (" + riskInfoAttachedTicks.ToString("N0") + ") ";
                    }
                    else if (!attachedInstrumentHasPosition
                        && !attachedInstrumentHasStopLoss
                        && riskInfoAttachedTicks == 0
                        && blendedInstrumentHasPosition
                        && blendedInstrumentHasStopLoss
                        && riskInfoBlendedTicks != 0)
                    {
                        riskFullText = " StopLoss: " + riskDollarText + " (" + riskInfoBlendedTicks.ToString("N0") + ") ";
                    }
                    else
                    {
                        riskInfoLabel.Foreground = Brushes.White;
                        riskFullText = " StopLoss: " + "Sin SL ";
                    }
                    riskInfoLabel.Content = riskFullText;
                }
                else
                {
                    riskInfoLabel.Foreground = Brushes.White;
                    riskInfoLabel.Content = "";
                }
                riskInfoHasChanged = false;
            }
        }
        private void OnButtonClick(object sender, RoutedEventArgs re)
        {
            System.Windows.Controls.Button button = sender as System.Windows.Controls.Button;
            if (button == revButton && button.Name == HHRevButtonName)
            {
                if (DebugLogLevel > 0) RealLogger.PrintOutput("Botón de girar posición pulsado");
                string signalName = "Girar Botón";
                bool positionFound = HandleReverse(signalName);
                if (!positionFound)
                {
                    if (IsBlendedInstrumentEnabled())
                    {
                        RealLogger.PrintOutput("Error girando posición: No se ha encontrado ninguna posición en " + attachedInstrument.FullName.ToString() + " or " + blendedInstrument.FullName.ToString());
                    }
                    else
                    {
                        RealLogger.PrintOutput("Error girando posición: No se ha encontrado ninguna posición en " + attachedInstrument.FullName.ToString());
                    }
                }
                return;
            }
            else if (button == closeAllButton && button.Name == HHCloseAllButtonName)
            {
                if (DebugLogLevel > 0) RealLogger.PrintOutput("Botón de cerrado pulsado");
                string signalName = "CerrarBoton";
                Instrument limitToSingleInstrument = (currentCloseAutoStatus == JoiaGestorCloseAutoTypes.EquityCloseAllTarget) ? null : attachedInstrument;
                Instrument secondaryInstrument = null;
                if (IsBlendedInstrumentEnabled())
                {
                    if (limitToSingleInstrument != null)
                    {
                        secondaryInstrument = blendedInstrument;
                    }
                    else
                    {
                        limitToSingleInstrument = blendedInstrument;
                    }
                }
                bool positionFound = FlattenEverything(signalName, true, limitToSingleInstrument, secondaryInstrument);
                if (!positionFound)
                {
                    RealLogger.PrintOutput("Error cerrando posiciones: no se ha encontrado ninguna posición en " + attachedInstrument.FullName.ToString());
                }
                return;
            }
            else if (button == toggleAutoCloseButton && button.Name == HHToggleAutoCloseButtonName)
            {
                if (IsCtrlKeyDown())
                {
                    DisableAutoCloseButton(0);
                }
                else
                {
                    string buttonContent = button.Content.ToString();
                    if (currentCloseAutoStatus != JoiaGestorCloseAutoTypes.Disabled && nextCloseAutoStatus == JoiaGestorCloseAutoTypes.Disabled)
                    {
                        currentCloseAutoStatus = JoiaGestorCloseAutoTypes.Disabled;
                        DisableAutoCloseButton(0);
                    }
                    else if (buttonContent == ToggleAutoCloseButtonDisabledText)
                    {
                        SetAutoCloseButton(button, ToggleAutoCloseECAButtonEnabledText, ToggleAutoCloseECAButtonEnabledToolTip,
                            JoiaGestorCloseAutoTypes.EquityCloseAllTarget, CloseAutoColorDelaySeconds, Brushes.DimGray);
                    }
                    else if (buttonContent == ToggleAutoCloseCFMPButtonEnabledText)
                    {
                        SetAutoCloseButton(button, ToggleAutoCloseM1SButtonEnabledText, ToggleAutoCloseM1SButtonEnabledToolTip,
                            JoiaGestorCloseAutoTypes.MovingAverage1Slope, CloseAutoColorDelaySeconds, Brushes.DimGray);
                    }
                    else if (buttonContent == ToggleAutoCloseM1SButtonEnabledText)
                    {
                        SetAutoCloseButton(button, ToggleAutoCloseM1SMPButtonEnabledText, ToggleAutoCloseM1SMPButtonEnabledToolTip,
                            JoiaGestorCloseAutoTypes.MovingAverage1SlopeMinProfit, CloseAutoColorDelaySeconds, Brushes.DimGray);
                    }
                    else if (buttonContent == ToggleAutoCloseM1SMPButtonEnabledText)
                    {
                        SetAutoCloseButton(button, ToggleAutoCloseM2SButtonEnabledText, ToggleAutoCloseM2SButtonEnabledToolTip,
                            JoiaGestorCloseAutoTypes.MovingAverage2Slope, CloseAutoColorDelaySeconds, Brushes.DimGray);
                    }
                    else if (button.Content.ToString() == ToggleAutoCloseM2SButtonEnabledText)
                    {
                        SetAutoCloseButton(button, ToggleAutoCloseM2SMPButtonEnabledText, ToggleAutoCloseM2SMPButtonEnabledToolTip,
                            JoiaGestorCloseAutoTypes.MovingAverage2SlopeMinProfit, CloseAutoColorDelaySeconds, Brushes.DimGray);
                    }
                    else if (buttonContent == ToggleAutoCloseM2SMPButtonEnabledText)
                    {
                        SetAutoCloseButton(button, ToggleAutoCloseM3SButtonEnabledText, ToggleAutoCloseM3SButtonEnabledToolTip,
                            JoiaGestorCloseAutoTypes.MovingAverage3Slope, CloseAutoColorDelaySeconds, Brushes.DimGray);
                    }
                    else if (buttonContent == ToggleAutoCloseM3SButtonEnabledText)
                    {
                        SetAutoCloseButton(button, ToggleAutoCloseM3SMPButtonEnabledText, ToggleAutoCloseM3SMPButtonEnabledToolTip,
                            JoiaGestorCloseAutoTypes.MovingAverage3SlopeMinProfit, CloseAutoColorDelaySeconds, Brushes.DimGray);
                    }
                    else
                    {
                        DisableAutoCloseButton(CloseAutoColorDelaySeconds);
                    }
                }
                return;
            }
            else if (button == toggleEntryVolumeAutoButton && button.Name == HHToggleEntryVolumeAutoButtonName)
            {
                if (IsCtrlKeyDown() || IsAllButtonsDisabled())
                {
                    if (IsCtrlKeyDown())
                    {
                        if (IsAllButtonsDisabled())
                        {
                            allButtonsDisabled = false;
                            SetButtonPanelVisiblity();
                            RealLogger.PrintOutput("Activar todos " + SystemName + " botones");
                        }
                        else
                        {
                            allButtonsDisabled = true;
                            activeDayOverMaxLossAutoClose = false;
                            SetButtonPanelHidden();
                            if (toggleEntryVolumeAutoButton != null && ShowButtonVolume)
                            {
                                toggleEntryVolumeAutoButton.Visibility = Visibility.Visible;
                            }
                            RealLogger.PrintOutput("Desactivar todos " + SystemName + " botones");
                        }
                    }
                }
                else
                {
                    if (lastToggleEntryVolumeAutoStatus == JoiaGestorEntryVolumeAutoTypes.Option1)
                    {
                        lastToggleEntryVolumeAutoStatus = JoiaGestorEntryVolumeAutoTypes.Option2;
                        button.Content = ToggleAutoEntryVolOption2ButtonEnabledText;
                        button.ToolTip = ToggleAutoEntryVolOption2ButtonEnabledToolTip;
                        currentEntryVolumeAutoStatus = JoiaGestorEntryVolumeAutoTypes.Option2;
                        nextEntryVolumeAutoStatus = JoiaGestorEntryVolumeAutoTypes.Option2;
                        lastEntryVolumeAutoChangeTime = GetDateTimeNow();
                    }
                    else if (lastToggleEntryVolumeAutoStatus == JoiaGestorEntryVolumeAutoTypes.Option2)
                    {
                        lastToggleEntryVolumeAutoStatus = JoiaGestorEntryVolumeAutoTypes.Option3;
                        button.Content = ToggleAutoEntryVolOption3ButtonEnabledText;
                        button.ToolTip = ToggleAutoEntryVolOption3ButtonEnabledToolTip;
                        currentEntryVolumeAutoStatus = JoiaGestorEntryVolumeAutoTypes.Option3;
                        nextEntryVolumeAutoStatus = JoiaGestorEntryVolumeAutoTypes.Option3;
                        lastEntryVolumeAutoChangeTime = GetDateTimeNow();
                    }
                    else if (lastToggleEntryVolumeAutoStatus == JoiaGestorEntryVolumeAutoTypes.Option3)
                    {
                        lastToggleEntryVolumeAutoStatus = JoiaGestorEntryVolumeAutoTypes.Option4;
                        button.Content = ToggleAutoEntryVolOption4ButtonEnabledText;
                        button.ToolTip = ToggleAutoEntryVolOption4ButtonEnabledToolTip;
                        currentEntryVolumeAutoStatus = JoiaGestorEntryVolumeAutoTypes.Option4;
                        nextEntryVolumeAutoStatus = JoiaGestorEntryVolumeAutoTypes.Option4;
                        lastEntryVolumeAutoChangeTime = GetDateTimeNow();
                    }
                    else if (lastToggleEntryVolumeAutoStatus == JoiaGestorEntryVolumeAutoTypes.Option4)
                    {
                        lastToggleEntryVolumeAutoStatus = JoiaGestorEntryVolumeAutoTypes.Option5;
                        button.Content = ToggleAutoEntryVolOption5ButtonEnabledText;
                        button.ToolTip = ToggleAutoEntryVolOption5ButtonEnabledToolTip;
                        currentEntryVolumeAutoStatus = JoiaGestorEntryVolumeAutoTypes.Option5;
                        nextEntryVolumeAutoStatus = JoiaGestorEntryVolumeAutoTypes.Option5;
                        lastEntryVolumeAutoChangeTime = GetDateTimeNow();
                    }
                    else
                    {
                        lastToggleEntryVolumeAutoStatus = JoiaGestorEntryVolumeAutoTypes.Option1;
                        button.Content = ToggleAutoEntryVolOption1ButtonEnabledText;
                        button.ToolTip = ToggleAutoEntryVolOption1ButtonEnabledToolTip;
                        currentEntryVolumeAutoStatus = JoiaGestorEntryVolumeAutoTypes.Option1;
                        nextEntryVolumeAutoStatus = JoiaGestorEntryVolumeAutoTypes.Option1;
                        lastEntryVolumeAutoChangeTime = GetDateTimeNow();
                    }
                }
            }
            else if (button == toggleAutoBEButton && button.Name == HHToggleAutoBEButtonName)
            {
                if (IsCtrlKeyDown())
                {
                    DisableAutoBEButton(0);
                }
                else
                {
                    string buttonContent = button.Content.ToString();
                    if (currentBreakEvenAutoStatus != JoiaGestorBreakEvenAutoTypes.Disabled && nextBreakEvenAutoStatus == JoiaGestorBreakEvenAutoTypes.Disabled)
                    {
                        currentBreakEvenAutoStatus = JoiaGestorBreakEvenAutoTypes.Disabled;
                        DisableAutoBEButton(0);
                    }
                    else if (buttonContent == ToggleAutoBEButtonDisabledText)
                    {
                        SetAutoBEButton(button, ToggleAutoBEHDLButtonEnabledText, ToggleAutoBEHDLButtonEnabledToolTip,
                            JoiaGestorBreakEvenAutoTypes.HODL, BeakEvenAutoColorDelaySeconds, Brushes.DimGray);
                    }
                    else if (buttonContent == ToggleCFTButtonEnabledText)
                    {
                        SetAutoBEButton(button, ToggleAutoBEButtonEnabledText, ToggleAutoBEButtonEnabledToolTip,
                            JoiaGestorBreakEvenAutoTypes.Enabled, BeakEvenAutoColorDelaySeconds, Brushes.DimGray);
                    }
                    else if (buttonContent == ToggleAutoBEButtonEnabledText)
                    {
                        SetAutoBEButton(button, ToggleAutoBET5BButtonEnabledText, ToggleAutoBET5BButtonEnabledToolTip,
                            JoiaGestorBreakEvenAutoTypes.PlusTrail5Bar, BeakEvenAutoColorDelaySeconds, Brushes.DimGray);
                    }
                    else if (buttonContent == ToggleAutoBET5BButtonEnabledText)
                    {
                        SetAutoBEButton(button, ToggleAutoBET3BButtonEnabledText, ToggleAutoBET3BButtonEnabledToolTip,
                            JoiaGestorBreakEvenAutoTypes.PlusTrail3Bar, BeakEvenAutoColorDelaySeconds, Brushes.DimGray);
                    }
                    else if (buttonContent == ToggleAutoBET3BButtonEnabledText)
                    {
                        SetAutoBEButton(button, ToggleAutoBET2BButtonEnabledText, ToggleAutoBET2BButtonEnabledToolTip,
                            JoiaGestorBreakEvenAutoTypes.PlusTrail2Bar, BeakEvenAutoColorDelaySeconds, Brushes.DimGray);
                    }
                    else if (buttonContent == ToggleAutoBET2BButtonEnabledText)
                    {
                        SetAutoBEButton(button, ToggleAutoBET1BButtonEnabledText, ToggleAutoBET1BButtonEnabledToolTip,
                            JoiaGestorBreakEvenAutoTypes.PlusTrail1Bar, BeakEvenAutoColorDelaySeconds, Brushes.DimGray);
                    }
                    else if (buttonContent == ToggleAutoBET1BButtonEnabledText)
                    {
                        SetAutoBEButton(button, ToggleAutoBETM3ButtonEnabledText, ToggleAutoBETM3ButtonEnabledToolTip,
                            JoiaGestorBreakEvenAutoTypes.PlusTrailMovingAverage3, BeakEvenAutoColorDelaySeconds, Brushes.DimGray);
                    }
                    else if (buttonContent == ToggleAutoBETM3ButtonEnabledText)
                    {
                        SetAutoBEButton(button, ToggleAutoBETM2ButtonEnabledText, ToggleAutoBETM2ButtonEnabledToolTip,
                            JoiaGestorBreakEvenAutoTypes.PlusTrailMovingAverage2, BeakEvenAutoColorDelaySeconds, Brushes.DimGray);
                    }
                    else if (buttonContent == ToggleAutoBETM2ButtonEnabledText)
                    {
                        SetAutoBEButton(button, ToggleAutoBETM1ButtonEnabledText, ToggleAutoBETM1ButtonEnabledToolTip,
                            JoiaGestorBreakEvenAutoTypes.PlusTrailMovingAverage1, BeakEvenAutoColorDelaySeconds, Brushes.DimGray);
                    }
                    else
                    {
                        DisableAutoBEButton(BeakEvenAutoColorDelaySeconds);
                    }
                }
                return;
            }
            else if (button == TPButton && button.Name == HHTPButtonName)
            {
                if (DebugLogLevel > 0) RealLogger.PrintOutput("TP+ pulsado");
                string signalName = "TP+ Botón";
                bool positionFound = HandleTakeProfitPlus(signalName);
                if (!positionFound)
                {
                    RealLogger.PrintOutput("TP+ Error: No hay posiciones en " + attachedInstrument.FullName.ToString());
                }
                return;
            }
            else if (button == BEButton && button.Name == HHBEButtonName)
            {
                if (DebugLogLevel > 0) RealLogger.PrintOutput("BE+ pulsado");
                string signalName = "BE+ Botón";
                bool positionFound = HandleBreakEvenPlus(signalName);
                if (!positionFound)
                {
                    RealLogger.PrintOutput("BE+ Error: No hay posiciones en " + attachedInstrument.FullName.ToString());
                }
                return;
            }
            else if (button == SLButton && button.Name == HHSLButtonName)
            {
                if (DebugLogLevel > 0) RealLogger.PrintOutput("SL+ pulsado");
                string signalName = "SL+ Botón";
                bool positionFound = HandleStopLossPlus(signalName);
                if (!positionFound)
                {
                    RealLogger.PrintOutput("SL+ Error: No hay posiciones en " + attachedInstrument.FullName.ToString());
                }
                return;
            }
            else if (button == BuyMarketButton && button.Name == HHBuyMarketButtonName)
            {
                if (DebugLogLevel > 0) RealLogger.PrintOutput("Buy Market pulsado");
                string signalName = "Buy Market Botón";
                HandleBuyMarket(signalName);
                return;
            }
            else if (button == SellMarketButton && button.Name == HHSellMarketButtonName)
            {
                if (DebugLogLevel > 0) RealLogger.PrintOutput("Sell Market pulsado");
                string signalName = "Sell Market Botón";
                HandleSellMarket(signalName);
                return;
            }
        }
        private void DisableAutoCloseButton(int delaySeconds)
        {
            SetAutoCloseButton(toggleAutoCloseButton, ToggleAutoCloseButtonDisabledText, ToggleAutoCloseButtonDisabledToolTip,
                JoiaGestorCloseAutoTypes.Disabled, delaySeconds, Brushes.DimGray);
        }
        private void DisableAutoBEButton(int delaySeconds)
        {
            SetAutoBEButton(toggleAutoBEButton, ToggleAutoBEButtonDisabledText, ToggleAutoBEButtonDisabledToolTip,
                JoiaGestorBreakEvenAutoTypes.Disabled, delaySeconds, Brushes.DimGray);
        }
        private void SetAutoCloseButton(System.Windows.Controls.Button button, string buttonText, string buttonToolTip, JoiaGestorCloseAutoTypes nextType,
            int delaySeconds, Brush backgroundColor)
        {
            button.Content = buttonText;
            button.ToolTip = buttonToolTip;
            button.Background = backgroundColor;
            nextCloseAutoStatus = nextType;
            lastCloseAutoChangeTime = (GetDateTimeNow()).AddSeconds(delaySeconds);
        }
        private void SetAutoBEButton(System.Windows.Controls.Button button, string buttonText, string buttonToolTip, JoiaGestorBreakEvenAutoTypes nextType,
            int delaySeconds, Brush backgroundColor)
        {
            button.Content = buttonText;
            button.ToolTip = buttonToolTip;
            button.Background = backgroundColor;
            nextBreakEvenAutoStatus = nextType;
            lastBreakEvenAutoChangeTime = (GetDateTimeNow()).AddSeconds(delaySeconds);
        }
        private bool AttemptBogeyTargetSmartJump()
        {
            bool returnFlag = false;
            return returnFlag;
        }
        private bool IsAllButtonsDisabled()
        {
            return allButtonsDisabled;
        }
        private void UnloadAccountEvents()
        {
            if (account != null)
            {
                if (subscribedToOnOrderUpdate)
                {
                    if (this.DebugLogLevel > 10) RealLogger.PrintOutput("*** UnloadAccountEvents: Unsubscribing to OrderUpdate:");
                    WeakEventManager<Account, OrderEventArgs>.RemoveHandler(account, "OrderUpdate", OnOrderUpdate);
                    subscribedToOnOrderUpdate = false;
                }
                account = null;
            }
        }
        private void SubscribePreviewMouseLeftButtonDown()
        {
            if (ChartControl != null)
            {
                if (!subscribedToPreviewMouseLeftButtonDown)
                {
                    WeakEventManager<ChartControl, MouseButtonEventArgs>.AddHandler(ChartControl, "PreviewMouseLeftButtonDown", ChartControl_PreviewMouseLeftButtonDown);
                    subscribedToPreviewMouseLeftButtonDown = true;
                }
            }
        }
        private void UnSubscribePreviewMouseLeftButtonDown()
        {
            if (ChartControl != null)
            {
                if (subscribedToPreviewMouseLeftButtonDown)
                {
                    WeakEventManager<ChartControl, MouseButtonEventArgs>.RemoveHandler(ChartControl, "PreviewMouseLeftButtonDown", ChartControl_PreviewMouseLeftButtonDown);
                    subscribedToPreviewMouseLeftButtonDown = false;
                }
            }
        }
        private bool HandleReverse(string signalName)
        {
            bool positionFound = false;
            Instrument tempInstrument = null;
            int tempQuantity = 0;
            MarketPosition tempMarketPosition;
            OrderAction revOrderAction;
            RealPosition foundAttachedPosition = null;
            RealPosition foundBlendedPosition = null;
            int foundAttachedQuantity = 0;
            int foundBlendedQuantity = 0;
            OrderAction foundAttachedOrderAction = OrderAction.Buy;
            OrderAction foundBlendedOrderAction = OrderAction.Buy;
            int positionCount = RealPositionService.PositionCount;
            for (int index = 0; index < positionCount; index++)
            {
                RealPosition position = null;
                if (RealPositionService.TryGetByIndex(index, out position))
                {
                    bool isAttachedPosition = RealPositionService.IsValidPosition(position, attachedInstrument);
                    bool isBlendedPosition = IsBlendedInstrumentEnabled() && RealPositionService.IsValidPosition(position, blendedInstrument);
                    if (isAttachedPosition || isBlendedPosition)
                    {
                        position.StoreState();
                        positionFound = true;
                        if (isAttachedPosition)
                        {
                            foundAttachedPosition = position;
                            foundAttachedQuantity = position.Quantity;
                            foundAttachedOrderAction = ConvertMarketPositionToRevOrderAction(position.MarketPosition);
                        }
                        else if (isBlendedPosition)
                        {
                            foundBlendedPosition = position;
                            foundBlendedQuantity = position.Quantity;
                            foundBlendedOrderAction = ConvertMarketPositionToRevOrderAction(position.MarketPosition);
                        }
                    }
                }
            }
            Instrument firstInstrumentReadyToClose = null;
            Instrument secondInstrumentReadyToClose = null;
            bool hasAttachedPositionReadyToClose = foundAttachedPosition != null && !foundAttachedPosition.HasStateChanged() && !foundAttachedPosition.IsFlat();
            bool hasBlendPositionReadyToClose = foundBlendedPosition != null && !foundBlendedPosition.HasStateChanged() && !foundBlendedPosition.IsFlat();
            if (hasAttachedPositionReadyToClose && hasBlendPositionReadyToClose)
            {
                firstInstrumentReadyToClose = foundAttachedPosition.Instrument;
                secondInstrumentReadyToClose = foundBlendedPosition.Instrument;
            }
            else if (hasAttachedPositionReadyToClose && !hasBlendPositionReadyToClose)
            {
                firstInstrumentReadyToClose = foundAttachedPosition.Instrument;
            }
            else if (!hasAttachedPositionReadyToClose && hasBlendPositionReadyToClose)
            {
                firstInstrumentReadyToClose = foundBlendedPosition.Instrument;
            }
            if (firstInstrumentReadyToClose != null)
            {
                FlattenEverything(signalName, true, firstInstrumentReadyToClose, secondInstrumentReadyToClose);
                if (hasAttachedPositionReadyToClose)
                {
                    if (DebugLogLevel > 2) RealLogger.PrintOutput(signalName + " opening " + foundAttachedOrderAction.ToString().ToLower() + " " + foundAttachedPosition.Instrument.FullName + " Quantity=" + foundAttachedQuantity, PrintTo.OutputTab1);
                    SubmitMarketOrder(foundAttachedPosition.Instrument, foundAttachedOrderAction, OrderEntry.Manual, foundAttachedQuantity);
                }
                if (hasBlendPositionReadyToClose)
                {
                    if (DebugLogLevel > 2) RealLogger.PrintOutput(signalName + " opening " + foundAttachedOrderAction.ToString().ToLower() + " " + foundBlendedPosition.Instrument.FullName + " Quantity=" + foundBlendedQuantity, PrintTo.OutputTab1);
                    SubmitMarketOrder(foundBlendedPosition.Instrument, foundBlendedOrderAction, OrderEntry.Manual, foundBlendedQuantity);
                }
            }
            return positionFound;
        }
        private bool HandleBreakEvenPlus(string signalName)
        {
            double oldStopLossPrice = 0;
            int oldOrderQuantity = 0;
            double newStopLossPrice = 0;
            int tempQuantity = 0;
            bool positionFound = false;
            bool hasStopLoss = false;
            bool hasValidNewStopLossPrice = false;
            bool hasStopLossPriceMismatch = false;
            int stopLossOrderCount = 0;
            OrderType orderType = OrderType.Unknown;
            double multiPositionAveragePrice = 0;
            bool attachedHasBeenProcessed = false;
            bool blendedHasBeenProcessed = false;
            double tempAttachedInstrumentPositionStopLossPrice = 0;
            double tempBlendedInstrumentPositionStopLossPrice = 0;
            int positionCount = RealPositionService.PositionCount;
            for (int index = 0; index < positionCount; index++)
            {
                RealPosition position = null;
                if (RealPositionService.TryGetByIndex(index, out position))
                {
                    if (RealPositionService.IsValidPosition(position, attachedInstrument) && position.IsValid)
                    {
                        position.StoreState();
                        positionFound = true;
                        tempQuantity = position.Quantity;
                        newStopLossPrice = 0;
                        stopLossOrderCount = 0;
                        multiPositionAveragePrice = (lastAveragePriceLevelLinePrice != 0) ? lastAveragePriceLevelLinePrice : position.AveragePrice;
                        oldStopLossPrice = RealOrderService.GetStopLossInfo(position.Account, position.Instrument, ConvertMarketPositionToSLOrderAction(position.MarketPosition), out orderType, out oldOrderQuantity, out stopLossOrderCount);
                        hasStopLoss = (oldStopLossPrice > 0);
                        if (DebugLogLevel > 2) RealLogger.PrintOutput("Current SL price=" + oldStopLossPrice.ToString() + " ticksize=" + attachedInstrumentTickSize.ToString() + " ticks/point=" + attachedInstrumentTicksPerPoint.ToString());
                        if (!hasStopLoss)
                        {
                            newStopLossPrice = GetInitialStopLossPrice(position.MarketPosition, multiPositionAveragePrice, position.Quantity);
                            if (blendedInstrumentHasPosition && tempBlendedInstrumentPositionStopLossPrice > 0)
                                newStopLossPrice = tempBlendedInstrumentPositionStopLossPrice;
                            newStopLossPrice = FilterStopLossByMarketPrice(position.Instrument, position.MarketPosition, newStopLossPrice);
                            hasValidNewStopLossPrice = newStopLossPrice > 0;
                            if (hasValidNewStopLossPrice && !position.HasStateChanged() && !position.IsFlat())
                            {
                                OrderAction orderAction = ConvertMarketPositionToSLOrderAction(position.MarketPosition);
                                double lastPrice = RealInstrumentService.GetLastPrice(position.Instrument);
                                bool isPriceValid = RealOrderService.IsValidStopLossPrice(position.Instrument, orderAction, newStopLossPrice, lastPrice);
                                if (isPriceValid)
                                {
                                    if (DebugLogLevel > 2) RealLogger.PrintOutput("New SL price=" + newStopLossPrice.ToString());
                                    CreatePositionStopLoss(signalName, position.Instrument, orderAction, OrderEntry.Manual, tempQuantity, newStopLossPrice);
                                }
                            }
                        }
                        else
                        {
                            if (position.MarketPosition == MarketPosition.Long)
                            {
                                newStopLossPrice = GetInitialBreakEvenStopLossPrice(position.MarketPosition, multiPositionAveragePrice);
                                if (newStopLossPrice > multiPositionAveragePrice && oldStopLossPrice >= newStopLossPrice)
                                {
                                    newStopLossPrice = GetStopLossPriceFromJumpTicks(position.MarketPosition, oldStopLossPrice, GetBreakEvenJumpTicks());
                                }
                                if (blendedInstrumentHasPosition && tempBlendedInstrumentPositionStopLossPrice > 0 && blendedHasBeenProcessed)
                                    newStopLossPrice = tempBlendedInstrumentPositionStopLossPrice;
                                double bidPrice = RealInstrumentService.GetBidPrice(position.Instrument);
                                if (newStopLossPrice > bidPrice)
                                {
                                    newStopLossPrice = 0;
                                }
                            }
                            else if (position.MarketPosition == MarketPosition.Short)
                            {
                                newStopLossPrice = GetInitialBreakEvenStopLossPrice(position.MarketPosition, multiPositionAveragePrice);
                                if (newStopLossPrice < multiPositionAveragePrice && oldStopLossPrice <= newStopLossPrice)
                                {
                                    newStopLossPrice = GetStopLossPriceFromJumpTicks(position.MarketPosition, oldStopLossPrice, GetBreakEvenJumpTicks());
                                }
                                if (blendedInstrumentHasPosition && tempBlendedInstrumentPositionStopLossPrice > 0 && blendedHasBeenProcessed)
                                    newStopLossPrice = tempBlendedInstrumentPositionStopLossPrice;
                                double askPrice = RealInstrumentService.GetAskPrice(position.Instrument);
                                if (newStopLossPrice < askPrice)
                                {
                                    newStopLossPrice = 0;
                                }
                            }
                            hasStopLossPriceMismatch = oldStopLossPrice > 0 && newStopLossPrice > 0 && oldStopLossPrice != newStopLossPrice;
                            if (hasStopLossPriceMismatch && !position.HasStateChanged() && !position.IsFlat())
                            {
                                OrderAction orderAction = ConvertMarketPositionToSLOrderAction(position.MarketPosition);
                                double lastPrice = RealInstrumentService.GetLastPrice(position.Instrument);
                                bool isPriceValid = RealOrderService.IsValidStopLossPrice(position.Instrument, orderAction, newStopLossPrice, lastPrice);
                                if (isPriceValid)
                                {
                                    if (DebugLogLevel > 2) RealLogger.PrintOutput("Updated SL price=" + newStopLossPrice.ToString() + " old=" + oldStopLossPrice.ToString());
                                    UpdatePositionStopLoss(signalName, position.Instrument, orderAction, OrderEntry.Manual, 0, newStopLossPrice);
                                }
                            }
                        }
                        attachedHasBeenProcessed = true;
                        tempAttachedInstrumentPositionStopLossPrice = (newStopLossPrice == 0) ? oldStopLossPrice : newStopLossPrice;
                    }
                    else if (IsBlendedInstrumentEnabled() && RealPositionService.IsValidPosition(position, blendedInstrument) && position.IsValid)
                    {
                        position.StoreState();
                        positionFound = true;
                        tempQuantity = position.Quantity;
                        newStopLossPrice = 0;
                        stopLossOrderCount = 0;
                        multiPositionAveragePrice = (lastAveragePriceLevelLinePrice != 0) ? lastAveragePriceLevelLinePrice : position.AveragePrice;
                        oldStopLossPrice = RealOrderService.GetStopLossInfo(position.Account, position.Instrument, ConvertMarketPositionToSLOrderAction(position.MarketPosition), out orderType, out oldOrderQuantity, out stopLossOrderCount);
                        hasStopLoss = (oldStopLossPrice > 0);
                        if (DebugLogLevel > 2) RealLogger.PrintOutput("Current SL price=" + oldStopLossPrice.ToString() + " ticksize=" + attachedInstrumentTickSize.ToString() + " ticks/point=" + attachedInstrumentTicksPerPoint.ToString());
                        if (!hasStopLoss)
                        {
                            newStopLossPrice = GetInitialStopLossPrice(position.MarketPosition, multiPositionAveragePrice, position.Quantity);
                            if (attachedInstrumentHasPosition && tempAttachedInstrumentPositionStopLossPrice > 0)
                                newStopLossPrice = tempAttachedInstrumentPositionStopLossPrice;
                            newStopLossPrice = FilterStopLossByMarketPrice(position.Instrument, position.MarketPosition, newStopLossPrice);
                            hasValidNewStopLossPrice = newStopLossPrice > 0;
                            if (hasValidNewStopLossPrice && !position.HasStateChanged() && !position.IsFlat())
                            {
                                OrderAction orderAction = ConvertMarketPositionToSLOrderAction(position.MarketPosition);
                                double lastPrice = RealInstrumentService.GetLastPrice(position.Instrument);
                                bool isPriceValid = RealOrderService.IsValidStopLossPrice(position.Instrument, orderAction, newStopLossPrice, lastPrice);
                                if (isPriceValid)
                                {
                                    if (DebugLogLevel > 2) RealLogger.PrintOutput("New SL price=" + newStopLossPrice.ToString());
                                    CreatePositionStopLoss(signalName, position.Instrument, orderAction, OrderEntry.Manual, tempQuantity, newStopLossPrice);
                                }
                            }
                        }
                        else
                        {
                            newStopLossPrice = GetInitialBreakEvenStopLossPrice(position.MarketPosition, multiPositionAveragePrice);
                            if (position.MarketPosition == MarketPosition.Long)
                            {
                                if (newStopLossPrice > multiPositionAveragePrice && oldStopLossPrice >= newStopLossPrice)
                                {
                                    newStopLossPrice = GetStopLossPriceFromJumpTicks(position.MarketPosition, oldStopLossPrice, GetBreakEvenJumpTicks());
                                }
                                if (attachedInstrumentHasPosition && tempAttachedInstrumentPositionStopLossPrice > 0 && attachedHasBeenProcessed)
                                    newStopLossPrice = tempAttachedInstrumentPositionStopLossPrice;
                                double bidPrice = RealInstrumentService.GetBidPrice(position.Instrument);
                                if (newStopLossPrice > bidPrice)
                                {
                                    newStopLossPrice = 0;
                                }
                            }
                            else if (position.MarketPosition == MarketPosition.Short)
                            {
                                if (newStopLossPrice < multiPositionAveragePrice && oldStopLossPrice <= newStopLossPrice)
                                {
                                    newStopLossPrice = GetStopLossPriceFromJumpTicks(position.MarketPosition, oldStopLossPrice, GetBreakEvenJumpTicks());
                                }
                                double askPrice = RealInstrumentService.GetAskPrice(position.Instrument);
                                if (newStopLossPrice < askPrice)
                                {
                                    newStopLossPrice = 0;
                                }
                            }
                            RealLogger.PrintOutput("Updated SL price=" + newStopLossPrice.ToString() + " old=" + oldStopLossPrice.ToString());
                            hasStopLossPriceMismatch = oldStopLossPrice > 0 && newStopLossPrice > 0 && oldStopLossPrice != newStopLossPrice;
                            if (hasStopLossPriceMismatch && !position.HasStateChanged() && !position.IsFlat())
                            {
                                OrderAction orderAction = ConvertMarketPositionToSLOrderAction(position.MarketPosition);
                                double lastPrice = RealInstrumentService.GetLastPrice(position.Instrument);
                                bool isPriceValid = RealOrderService.IsValidStopLossPrice(position.Instrument, orderAction, newStopLossPrice, lastPrice);
                                if (isPriceValid)
                                {
                                    if (DebugLogLevel > 2) RealLogger.PrintOutput("Updated SL price=" + newStopLossPrice.ToString() + " old=" + oldStopLossPrice.ToString());
                                    UpdatePositionStopLoss(signalName, position.Instrument, orderAction, OrderEntry.Manual, 0, newStopLossPrice);
                                }
                            }
                        }
                        blendedHasBeenProcessed = true;
                        tempBlendedInstrumentPositionStopLossPrice = (newStopLossPrice == 0) ? oldStopLossPrice : newStopLossPrice;
                    }
                }
            }
            return positionFound;
        }
        private int GetBreakEvenJumpTicks()
        {
            int jumpTicks = this.BreakEvenJumpTicks;
            if (IsCtrlKeyDown())
            {
                jumpTicks = this.BreakEvenTurboJumpTicks;
            }
            return jumpTicks;
        }
        private bool HandleStopLossPlus(string signalName, double overrideStopLossPrice = 0)
        {
            double oldStopLossPrice = 0;
            int oldOrderQuantity = 0;
            double newStopLossPrice = 0;
            bool positionFound = false;
            bool hasStopLoss = false;
            bool hasValidNewStopLossPrice = false;
            bool hasStopLossPriceMismatch = false;
            int stopLossOrderCount = 0;
            OrderType orderType = OrderType.Unknown;
            int tempQuantity = 0;
            bool isCtrlKeyDown = IsCtrlKeyDown();
            MarketPosition positionFoundMarketPosition = MarketPosition.Flat;
            double multiPositionAveragePrice = 0;
            int positionCount = RealPositionService.PositionCount;
            bool attachedHasBeenProcessed = false;
            bool blendedHasBeenProcessed = false;
            double tempAttachedInstrumentPositionStopLossPrice = 0;
            double tempBlendedInstrumentPositionStopLossPrice = 0;
            for (int index = 0; index < positionCount; index++)
            {
                RealPosition position = null;
                if (RealPositionService.TryGetByIndex(index, out position))
                {
                    if (RealPositionService.IsValidPosition(position, attachedInstrument) && position.IsValid)
                    {
                        position.StoreState();
                        positionFound = true;
                        tempQuantity = position.Quantity;
                        newStopLossPrice = 0;
                        stopLossOrderCount = 0;
                        multiPositionAveragePrice = (lastAveragePriceLevelLinePrice != 0) ? lastAveragePriceLevelLinePrice : position.AveragePrice;
                        positionFoundMarketPosition = position.MarketPosition;
                        oldStopLossPrice = RealOrderService.GetStopLossInfo(position.Account, position.Instrument, ConvertMarketPositionToSLOrderAction(position.MarketPosition), out orderType, out oldOrderQuantity, out stopLossOrderCount);
                        hasStopLoss = (oldStopLossPrice > 0);
                        if (DebugLogLevel > 2) RealLogger.PrintOutput("Current SL price=" + oldStopLossPrice.ToString() + " ticksize=" + attachedInstrumentTickSize.ToString() + " ticks/point=" + attachedInstrumentTicksPerPoint.ToString());
                        if (!hasStopLoss)
                        {
                            if (overrideStopLossPrice != 0)
                                newStopLossPrice = overrideStopLossPrice;
                            else
                                newStopLossPrice = GetInitialStopLossPrice(position.MarketPosition, multiPositionAveragePrice, position.Quantity);
                            if (blendedInstrumentHasPosition && tempBlendedInstrumentPositionStopLossPrice > 0)
                                newStopLossPrice = tempBlendedInstrumentPositionStopLossPrice;
                            newStopLossPrice = FilterStopLossByMarketPrice(position.Instrument, position.MarketPosition, newStopLossPrice);
                            RealLogger.PrintOutput("new SL price=" + newStopLossPrice.ToString());
                            hasValidNewStopLossPrice = newStopLossPrice > 0;
                            if (hasValidNewStopLossPrice && !position.HasStateChanged() && !position.IsFlat())
                            {
                                OrderAction orderAction = ConvertMarketPositionToSLOrderAction(position.MarketPosition);
                                double lastPrice = RealInstrumentService.GetLastPrice(position.Instrument);
                                bool isPriceValid = RealOrderService.IsValidStopLossPrice(position.Instrument, orderAction, newStopLossPrice, lastPrice);
                                if (isPriceValid)
                                {
                                    if (DebugLogLevel > 2) RealLogger.PrintOutput("New SL price=" + newStopLossPrice.ToString());
                                    CreatePositionStopLoss(signalName, position.Instrument, orderAction, OrderEntry.Manual, tempQuantity, newStopLossPrice);
                                }
                            }
                        }
                        else
                        {
                            if (StopLossCTRLJumpTicks)
                            {
                                if (isCtrlKeyDown)
                                {
                                    newStopLossPrice = GetStopLossPriceFromJumpTicks(position.MarketPosition, oldStopLossPrice, this.StopLossJumpTicks);
                                }
                                else
                                {
                                    newStopLossPrice = GetNextSnapPrice(position.MarketPosition, oldStopLossPrice);
                                }
                            }
                            else
                            {
                                if (isCtrlKeyDown)
                                {
                                    newStopLossPrice = GetNextSnapPrice(position.MarketPosition, oldStopLossPrice);
                                }
                                else
                                {
                                    newStopLossPrice = GetStopLossPriceFromJumpTicks(position.MarketPosition, oldStopLossPrice, this.StopLossJumpTicks);
                                }
                            }
                            if (overrideStopLossPrice != 0) newStopLossPrice = overrideStopLossPrice;
                            if (blendedInstrumentHasPosition && tempBlendedInstrumentPositionStopLossPrice > 0 && blendedHasBeenProcessed)
                                newStopLossPrice = tempBlendedInstrumentPositionStopLossPrice;
                            newStopLossPrice = FilterStopLossByMarketPrice(position.Instrument, position.MarketPosition, newStopLossPrice);
                            hasStopLossPriceMismatch = oldStopLossPrice > 0 && newStopLossPrice > 0 && oldStopLossPrice != newStopLossPrice;
                            if (hasStopLossPriceMismatch && !position.HasStateChanged() && !position.IsFlat())
                            {
                                OrderAction orderAction = ConvertMarketPositionToSLOrderAction(position.MarketPosition);
                                double lastPrice = RealInstrumentService.GetLastPrice(position.Instrument);
                                bool isPriceValid = RealOrderService.IsValidStopLossPrice(position.Instrument, orderAction, newStopLossPrice, lastPrice);
                                if (isPriceValid)
                                {
                                    if (DebugLogLevel > 2) RealLogger.PrintOutput("Updated SL price=" + newStopLossPrice.ToString() + " old=" + oldStopLossPrice.ToString());
                                    UpdatePositionStopLoss(signalName, position.Instrument, orderAction, OrderEntry.Manual, 0, newStopLossPrice);
                                }
                            }
                        }
                        attachedHasBeenProcessed = true;
                        tempAttachedInstrumentPositionStopLossPrice = (newStopLossPrice == 0) ? oldStopLossPrice : newStopLossPrice;
                    }
                    else if (IsBlendedInstrumentEnabled() && RealPositionService.IsValidPosition(position, blendedInstrument) && position.IsValid)
                    {
                        position.StoreState();
                        positionFound = true;
                        tempQuantity = position.Quantity;
                        newStopLossPrice = 0;
                        stopLossOrderCount = 0;
                        multiPositionAveragePrice = (lastAveragePriceLevelLinePrice != 0) ? lastAveragePriceLevelLinePrice : position.AveragePrice;
                        positionFoundMarketPosition = position.MarketPosition;
                        oldStopLossPrice = RealOrderService.GetStopLossInfo(position.Account, position.Instrument, ConvertMarketPositionToSLOrderAction(position.MarketPosition), out orderType, out oldOrderQuantity, out stopLossOrderCount);
                        hasStopLoss = (oldStopLossPrice > 0);
                        if (DebugLogLevel > 2) RealLogger.PrintOutput("Current SL price=" + oldStopLossPrice.ToString() + " ticksize=" + attachedInstrumentTickSize.ToString() + " ticks/point=" + attachedInstrumentTicksPerPoint.ToString());
                        if (!hasStopLoss)
                        {
                            if (overrideStopLossPrice != 0)
                                newStopLossPrice = overrideStopLossPrice;
                            else
                                newStopLossPrice = GetInitialStopLossPrice(position.MarketPosition, multiPositionAveragePrice, position.Quantity);
                            if (attachedInstrumentHasPosition && tempAttachedInstrumentPositionStopLossPrice > 0)
                                newStopLossPrice = tempAttachedInstrumentPositionStopLossPrice;
                            newStopLossPrice = FilterStopLossByMarketPrice(position.Instrument, position.MarketPosition, newStopLossPrice);
                            RealLogger.PrintOutput("new SL price=" + newStopLossPrice.ToString());
                            hasValidNewStopLossPrice = newStopLossPrice > 0;
                            if (hasValidNewStopLossPrice && !position.HasStateChanged() && !position.IsFlat())
                            {
                                OrderAction orderAction = ConvertMarketPositionToSLOrderAction(position.MarketPosition);
                                double lastPrice = RealInstrumentService.GetLastPrice(position.Instrument);
                                bool isPriceValid = RealOrderService.IsValidStopLossPrice(position.Instrument, orderAction, newStopLossPrice, lastPrice);
                                if (isPriceValid)
                                {
                                    if (DebugLogLevel > 2) RealLogger.PrintOutput("New SL price=" + newStopLossPrice.ToString());
                                    CreatePositionStopLoss(signalName, position.Instrument, orderAction, OrderEntry.Manual, tempQuantity, newStopLossPrice);
                                }
                            }
                        }
                        else
                        {
                            if (StopLossCTRLJumpTicks)
                            {
                                if (isCtrlKeyDown)
                                {
                                    newStopLossPrice = GetStopLossPriceFromJumpTicks(position.MarketPosition, oldStopLossPrice, this.StopLossJumpTicks);
                                }
                                else
                                {
                                    newStopLossPrice = GetNextSnapPrice(position.MarketPosition, oldStopLossPrice);
                                }
                            }
                            else
                            {
                                if (isCtrlKeyDown)
                                {
                                    newStopLossPrice = GetNextSnapPrice(position.MarketPosition, oldStopLossPrice);
                                }
                                else
                                {
                                    newStopLossPrice = GetStopLossPriceFromJumpTicks(position.MarketPosition, oldStopLossPrice, this.StopLossJumpTicks);
                                }
                            }
                            if (overrideStopLossPrice != 0) newStopLossPrice = overrideStopLossPrice;
                            if (attachedInstrumentHasPosition && tempAttachedInstrumentPositionStopLossPrice > 0 && attachedHasBeenProcessed)
                                newStopLossPrice = tempAttachedInstrumentPositionStopLossPrice;
                            newStopLossPrice = FilterStopLossByMarketPrice(position.Instrument, position.MarketPosition, newStopLossPrice);
                            hasStopLossPriceMismatch = oldStopLossPrice > 0 && newStopLossPrice > 0 && oldStopLossPrice != newStopLossPrice;
                            if (hasStopLossPriceMismatch && !position.HasStateChanged() && !position.IsFlat())
                            {
                                OrderAction orderAction = ConvertMarketPositionToSLOrderAction(position.MarketPosition);
                                double lastPrice = RealInstrumentService.GetLastPrice(position.Instrument);
                                bool isPriceValid = RealOrderService.IsValidStopLossPrice(position.Instrument, orderAction, newStopLossPrice, lastPrice);
                                if (isPriceValid)
                                {
                                    if (DebugLogLevel > 2) RealLogger.PrintOutput("Updated SL price=" + newStopLossPrice.ToString() + " old=" + oldStopLossPrice.ToString());
                                    UpdatePositionStopLoss(signalName, position.Instrument, orderAction, OrderEntry.Manual, 0, newStopLossPrice);
                                }
                            }
                        }
                        blendedHasBeenProcessed = true;
                        tempBlendedInstrumentPositionStopLossPrice = (newStopLossPrice == 0) ? oldStopLossPrice : newStopLossPrice;
                    }
                }
            }
            return positionFound;
        }
        private double GetNextSnapPrice(MarketPosition marketPosition, double oldStopLossPrice)
        {
            double snapStopLossPrice = 0;
            if (marketPosition == MarketPosition.Long)
            {
                snapStopLossPrice = CalculateSnapBarLowPrice(JoiaGestorStopLossSnapTypes.SnapPBLevel);
                if (snapStopLossPrice <= oldStopLossPrice)
                {
                    snapStopLossPrice = CalculateSnapBarLowPrice(JoiaGestorStopLossSnapTypes.Snap8Bar);
                    if (snapStopLossPrice <= oldStopLossPrice)
                    {
                        snapStopLossPrice = CalculateSnapBarLowPrice(JoiaGestorStopLossSnapTypes.Snap5Bar);
                        if (snapStopLossPrice <= oldStopLossPrice)
                        {
                            snapStopLossPrice = CalculateSnapBarLowPrice(JoiaGestorStopLossSnapTypes.Snap3Bar);
                            if (snapStopLossPrice <= oldStopLossPrice)
                            {
                                snapStopLossPrice = CalculateSnapBarLowPrice(JoiaGestorStopLossSnapTypes.Snap2Bar);
                                if (snapStopLossPrice <= oldStopLossPrice)
                                {
                                    snapStopLossPrice = CalculateSnapBarLowPrice(JoiaGestorStopLossSnapTypes.Snap1Bar);
                                }
                            }
                        }
                    }
                }
            }
            else if (marketPosition == MarketPosition.Short)
            {
                snapStopLossPrice = CalculateSnapBarHighPrice(JoiaGestorStopLossSnapTypes.SnapPBLevel);
                if (snapStopLossPrice >= oldStopLossPrice)
                {
                    snapStopLossPrice = CalculateSnapBarHighPrice(JoiaGestorStopLossSnapTypes.Snap8Bar);
                    if (snapStopLossPrice >= oldStopLossPrice)
                    {
                        snapStopLossPrice = CalculateSnapBarHighPrice(JoiaGestorStopLossSnapTypes.Snap5Bar);
                        if (snapStopLossPrice >= oldStopLossPrice)
                        {
                            snapStopLossPrice = CalculateSnapBarHighPrice(JoiaGestorStopLossSnapTypes.Snap3Bar);
                            if (snapStopLossPrice >= oldStopLossPrice)
                            {
                                snapStopLossPrice = CalculateSnapBarHighPrice(JoiaGestorStopLossSnapTypes.Snap2Bar);
                                if (snapStopLossPrice >= oldStopLossPrice)
                                {
                                    snapStopLossPrice = CalculateSnapBarHighPrice(JoiaGestorStopLossSnapTypes.Snap1Bar);
                                }
                            }
                        }
                    }
                }
            }
            return snapStopLossPrice;
        }
        private bool HandleTPSLRefresh(string signalName)
        {
            double oldStopLossPrice = 0;
            double oldTakeProfitPrice = 0;
            int oldOrderQuantity = 0;
            double newStopLossPrice = 0;
            double newTakeProfitPrice = 0;
            double triggerStopLossPrice = 0;
            bool hasPosition = false;
            bool hasStopLoss = false;
            bool hasTakeProfit = false;
            bool hasProfitLocked = false;
            bool hasHitPriceTrigger = false;
            bool hasTakeProfitPriceMismatch = false;
            bool hasStopLossPriceMismatch = false;
            bool hasValidNewTakeProfitPrice = false;
            bool hasValidNewStopLossPrice = false;
            int stopLossOrderCount = 0;
            int takeProfitOrderCount = 0;
            int tempQuantity = 0;
            OrderType orderType = OrderType.Unknown;
            bool hasStopLossQuantityMismatch = false;
            bool hasTakeProfitQuantityMismatch = false; 
            var lockTimeout = TimeSpan.FromSeconds(10);
            bool lockTaken = false;
            try
            {
                Monitor.TryEnter(RefreshTPSLLock, lockTimeout, ref lockTaken);
                if (lockTaken)
                {
                    if ((!IsAccountFlat(attachedInstrument) || (IsBlendedInstrumentEnabled() && !IsAccountFlat(blendedInstrument)))
                        && RealOrderService.AreAllOrderUpdateCyclesComplete()
                        && !HasPositionTPSLOrderDelay())
                    {
                        double multiPositionAveragePrice = 0;
                        int positionCount = RealPositionService.PositionCount;
                        for (int index = 0; index < positionCount; index++)
                        {
                            RealPosition position = null;
                            if (RealPositionService.TryGetByIndex(index, out position))
                            {
                                if (RealPositionService.IsValidPosition(position, attachedInstrument) && position.IsValid)
                                {
                                    position.StoreState();
                                    tempQuantity = position.Quantity;
                                    hasPosition = true;
                                    multiPositionAveragePrice = (lastAveragePriceLevelLinePrice != 0) ? lastAveragePriceLevelLinePrice : position.AveragePrice;
                                    MarketPosition reversedMarketPosition = MarketPosition.Flat;
                                    if (position.MarketPosition == MarketPosition.Long)
                                        reversedMarketPosition = MarketPosition.Short;
                                    else
                                        reversedMarketPosition = MarketPosition.Long;
                                    if (CancelPositionTPSLOrders("TPSLRefresh-Rev", attachedInstrument, ConvertMarketPositionToSLOrderAction(reversedMarketPosition))) return hasPosition; 
                                    hasProfitLocked = false;
                                    hasHitPriceTrigger = false;
                                    triggerStopLossPrice = 0;
                                    newStopLossPrice = 0;
                                    stopLossOrderCount = 0;
                                    oldOrderQuantity = 0;
                                    oldStopLossPrice = RealOrderService.GetStopLossInfo(position.Account, position.Instrument, ConvertMarketPositionToSLOrderAction(position.MarketPosition), out orderType, out oldOrderQuantity, out stopLossOrderCount);
                                    hasStopLoss = (oldStopLossPrice > 0);
                                    hasStopLossQuantityMismatch = oldOrderQuantity != tempQuantity;
                                    if (this.StopLossRefreshManagementEnabled)
                                    {
                                        if (hasStopLoss && !hasStopLossQuantityMismatch) validateAttachedPositionStopLossQuantity = false;
                                        if (!hasStopLoss && IsAutoPositionStopLossEnabled())
                                        {
                                            if (DebugLogLevel > 2) RealLogger.PrintOutput("refresh SL price=" + oldStopLossPrice.ToString() + " auto=" + (IsAutoPositionStopLossEnabled()).ToString() + " oldquan=" + oldOrderQuantity.ToString() + " orderType=" + orderType);
                                            newStopLossPrice = GetInitialStopLossPrice(position.MarketPosition, multiPositionAveragePrice, position.Quantity);
                                            if (blendedInstrumentHasPosition && blendedInstrumentPositionStopLossPrice > 0)
                                                newStopLossPrice = blendedInstrumentPositionStopLossPrice;
                                            if (IsDayOverMaxLossEnabled())
                                            {
                                                if (lastDayOverMaxLossLevelLinePrice > 0)
                                                {
                                                    newStopLossPrice = FilterStopLossByPriceMax(position.MarketPosition, lastDayOverMaxLossLevelLinePrice, oldStopLossPrice, newStopLossPrice);
                                                }
                                                else
                                                {
                                                    newStopLossPrice = 0;
                                                }
                                            }
                                            newStopLossPrice = FilterStopLossByMarketPrice(position.Instrument, position.MarketPosition, newStopLossPrice);
                                            hasValidNewStopLossPrice = newStopLossPrice > 0;
                                            if (hasValidNewStopLossPrice && !position.HasStateChanged() && !position.IsFlat() && RealOrderService.AreAllOrderUpdateCyclesComplete())
                                            {
                                                OrderAction orderAction = ConvertMarketPositionToSLOrderAction(position.MarketPosition);
                                                double lastPrice = RealInstrumentService.GetLastPrice(position.Instrument);
                                                bool isPriceValid = RealOrderService.IsValidStopLossPrice(position.Instrument, orderAction, newStopLossPrice, lastPrice);
                                                if (isPriceValid)
                                                {
                                                    if (DebugLogLevel > 2) RealLogger.PrintOutput("New SL price=" + newStopLossPrice.ToString());
                                                    CreatePositionStopLoss(signalName, position.Instrument, orderAction, OrderEntry.Manual, tempQuantity, newStopLossPrice);
                                                }
                                                if (hasStopLossQuantityMismatch)
                                                {
                                                    validateAttachedPositionStopLossQuantity = false;
                                                }
                                            }
                                        }
                                        else if (hasStopLoss && hasStopLossQuantityMismatch && validateAttachedPositionStopLossQuantity)
                                        {
                                            if (DebugLogLevel > 0) RealLogger.PrintOutput("SLQuanMismatch Current SL price=" + oldStopLossPrice.ToString() + " oldSLQuantity=" + oldOrderQuantity.ToString() + " PosQuantity=" + tempQuantity.ToString() + " SLOrderCount=" + stopLossOrderCount.ToString() + " attachedIn=" + attachedInstrument.FullName + " barPeriod=" + BarsPeriod.ToString());
                                            if (this.StopLossRefreshOnVolumeChange)
                                            {
                                                newStopLossPrice = GetInitialStopLossPrice(position.MarketPosition, multiPositionAveragePrice, position.Quantity, oldStopLossPrice);
                                            }
                                            else
                                            {
                                                newStopLossPrice = oldStopLossPrice;
                                            }
                                            if (blendedInstrumentHasPosition && blendedInstrumentPositionStopLossPrice > 0)
                                                newStopLossPrice = blendedInstrumentPositionStopLossPrice;
                                            if (IsDayOverMaxLossEnabled() && lastDayOverMaxLossLevelLinePrice > 0)
                                            {
                                                newStopLossPrice = FilterStopLossByPriceMax(position.MarketPosition, lastDayOverMaxLossLevelLinePrice, oldStopLossPrice, newStopLossPrice);
                                            }
                                            newStopLossPrice = FilterStopLossByMarketPrice(position.Instrument, position.MarketPosition, newStopLossPrice);
                                            hasValidNewStopLossPrice = newStopLossPrice > 0;
                                            if (hasValidNewStopLossPrice
                                                && hasStopLossQuantityMismatch
                                                && !position.HasStateChanged()
                                                && !position.IsFlat()
                                                && RealOrderService.AreAllOrderUpdateCyclesComplete())
                                            {
                                                OrderAction orderAction = ConvertMarketPositionToSLOrderAction(position.MarketPosition);
                                                double lastPrice = RealInstrumentService.GetLastPrice(position.Instrument);
                                                bool isPriceValid = RealOrderService.IsValidStopLossPrice(position.Instrument, orderAction, newStopLossPrice, lastPrice);
                                                if (isPriceValid)
                                                {
                                                    bool hasMultipleStopLossOrders = (stopLossOrderCount > 1);
                                                    if (hasMultipleStopLossOrders)
                                                    {
                                                        ConsolidatePositionTPSLOrders("HandleTPSLRefresh", position.Instrument);
                                                    }
                                                    UpdatePositionStopLoss(signalName, position.Instrument, orderAction, OrderEntry.Manual, tempQuantity, newStopLossPrice);
                                                    if (hasStopLossQuantityMismatch)
                                                    {
                                                        validateAttachedPositionStopLossQuantity = false;
                                                    }
                                                }
                                            }
                                        }
                                        else if (hasStopLoss && IsDayOverMaxLossEnabled() && lastDayOverMaxLossLevelLinePrice > 0 && !validateAttachedPositionStopLossQuantity)
                                        {
                                            newStopLossPrice = FilterStopLossByPriceMax(position.MarketPosition, lastDayOverMaxLossLevelLinePrice, oldStopLossPrice, newStopLossPrice);
                                            newStopLossPrice = FilterStopLossByMarketPrice(position.Instrument, position.MarketPosition, newStopLossPrice);
                                            hasStopLossPriceMismatch = oldStopLossPrice > 0 && newStopLossPrice > 0 && oldStopLossPrice != newStopLossPrice;
                                            if (hasStopLossPriceMismatch
                                                && !position.HasStateChanged()
                                                && !position.IsFlat()
                                                && RealOrderService.AreAllOrderUpdateCyclesComplete())
                                            {
                                                OrderAction orderAction = ConvertMarketPositionToSLOrderAction(position.MarketPosition);
                                                double lastPrice = RealInstrumentService.GetLastPrice(position.Instrument);
                                                bool isPriceValid = RealOrderService.IsValidStopLossPrice(position.Instrument, orderAction, newStopLossPrice, lastPrice);
                                                if (isPriceValid)
                                                {
                                                    UpdatePositionStopLoss(signalName, position.Instrument, orderAction, OrderEntry.Manual, tempQuantity, newStopLossPrice);
                                                }
                                            }
                                        }
                                    }
                                    if ((BreakEvenAutoTriggerTicks > 0 || BreakEvenAutoTriggerATRMultiplier > 0
                                        && currentBreakEvenAutoStatus != JoiaGestorBreakEvenAutoTypes.Disabled))
                                    {
                                        if (hasStopLoss && (BreakEvenAutoTriggerTicks > 0 || BreakEvenAutoTriggerATRMultiplier > 0))
                                        {
                                            triggerStopLossPrice = GetTriggerBreakEvenStopLossPrice(position.MarketPosition, multiPositionAveragePrice);
                                            if (position.MarketPosition == MarketPosition.Long)
                                            {
                                                if (oldStopLossPrice > multiPositionAveragePrice)
                                                {
                                                    hasProfitLocked = true;
                                                }
                                                double bidPrice = RealInstrumentService.GetBidPrice(position.Instrument);
                                                if (triggerStopLossPrice <= bidPrice)
                                                {
                                                    hasHitPriceTrigger = true;
                                                }
                                            }
                                            else if (position.MarketPosition == MarketPosition.Short)
                                            {
                                                if (oldStopLossPrice < multiPositionAveragePrice)
                                                {
                                                    hasProfitLocked = true;
                                                }
                                                double askPrice = RealInstrumentService.GetAskPrice(position.Instrument);
                                                if (triggerStopLossPrice >= askPrice)
                                                {
                                                    hasHitPriceTrigger = true;
                                                }
                                            }
                                        }
                                        if (hasPosition
                                            && hasStopLoss
                                            && !position.HasStateChanged()
                                            && !position.IsFlat()
                                            && RealOrderService.AreAllOrderUpdateCyclesComplete())
                                        {
                                            if (!hasProfitLocked && hasHitPriceTrigger
                                                && currentBreakEvenAutoStatus != JoiaGestorBreakEvenAutoTypes.HODL
                                      )
                                            {
                                                if (DebugLogLevel > 0) RealLogger.PrintOutput("Auto BE hit trigger price of " + triggerStopLossPrice.ToString("N2"), PrintTo.OutputTab1, false);
                                                HandleBreakEvenPlus("AutoBreakEven");
                                            }
                                            else if (currentBreakEvenAutoStatus != JoiaGestorBreakEvenAutoTypes.Disabled &&
                                                (currentBreakEvenAutoStatus != JoiaGestorBreakEvenAutoTypes.Enabled || currentBreakEvenAutoStatus != JoiaGestorBreakEvenAutoTypes.HODL) && hasProfitLocked)
                                            {
                                                if (position.MarketPosition == MarketPosition.Long)
                                                {
                                                    double entryPrice = CalculateTrailLowPrice(position.MarketPosition, false);
                                                    if (entryPrice != 0 && entryPrice > oldStopLossPrice)
                                                    {
                                                        TrailBuyPositionStopLoss("AutoBreakEven");
                                                    }
                                                }
                                                else if (position.MarketPosition == MarketPosition.Short)
                                                {
                                                    double entryPrice = CalculateTrailHighPrice(position.MarketPosition);
                                                    if (entryPrice != 0 && entryPrice < oldStopLossPrice)
                                                    {
                                                        TrailSellPositionStopLoss("AutoBreakEven");
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    newTakeProfitPrice = 0;
                                    takeProfitOrderCount = 0;
                                    oldOrderQuantity = 0;
                                    oldTakeProfitPrice = RealOrderService.GetTakeProfitInfo(position.Account, position.Instrument, ConvertMarketPositionToTPOrderAction(position.MarketPosition), out orderType, out oldOrderQuantity, out takeProfitOrderCount);
                                    hasTakeProfit = (oldTakeProfitPrice > 0);
                                    hasTakeProfitQuantityMismatch = oldOrderQuantity != tempQuantity;
                                    if (TakeProfitRefreshManagementEnabled)
                                    {
                                        if (hasTakeProfit && !hasTakeProfitQuantityMismatch) validateAttachedPositionTakeProfitQuantity = false;
                                        if (!hasTakeProfit && IsAutoPositionTakeProfitEnabled())
                                        {
                                            if (DebugLogLevel > 2) RealLogger.PrintOutput("refresh tp price=" + oldTakeProfitPrice.ToString() + " auto=" + (IsAutoPositionTakeProfitEnabled()).ToString() + " oldquan=" + oldOrderQuantity.ToString());
                                            newTakeProfitPrice = GetInitialTakeProfitPrice(position.MarketPosition, multiPositionAveragePrice);
                                            if (blendedInstrumentHasPosition && blendedInstrumentPositionTakeProfitPrice > 0)
                                                newTakeProfitPrice = blendedInstrumentPositionTakeProfitPrice;
                                            if (currentBreakEvenAutoStatus != JoiaGestorBreakEvenAutoTypes.HODL)
                                            {
                                                if (IsECATPEnabled())
                                                {
                                                    if (lastECATakeProfitLevelLinePrice > 0)
                                                    {
                                                        newTakeProfitPrice = FilterTakeProfitByPriceMax(position.MarketPosition, lastECATakeProfitLevelLinePrice, oldTakeProfitPrice, newTakeProfitPrice);
                                                        newTakeProfitPrice = FilterTakeProfitByForceSync(TakeProfitSyncECATargetPrice, lastECATakeProfitLevelLinePrice, newTakeProfitPrice);
                                                    }
                                                    else
                                                    {
                                                        newTakeProfitPrice = 0;
                                                    }
                                                }
                                                else
                                                {
                                                }
                                            }
                                            newTakeProfitPrice = FilterTakeProfitByMarketPrice(position.Instrument, position.MarketPosition, newTakeProfitPrice);
                                            hasValidNewTakeProfitPrice = newTakeProfitPrice > 0;
                                            if (hasValidNewTakeProfitPrice
                                                && !position.HasStateChanged()
                                                && !position.IsFlat()
                                                && RealOrderService.AreAllOrderUpdateCyclesComplete())
                                            {
                                                OrderAction orderAction = ConvertMarketPositionToTPOrderAction(position.MarketPosition);
                                                double lastPrice = RealInstrumentService.GetLastPrice(position.Instrument);
                                                bool isPriceValid = RealOrderService.IsValidTakeProfitPrice(position.Instrument, orderAction, newTakeProfitPrice, lastPrice);
                                                if (isPriceValid)
                                                {
                                                    if (DebugLogLevel > 2) RealLogger.PrintOutput("New TP price=" + newTakeProfitPrice.ToString());
                                                    CreatePositionTakeProfit(signalName, position.Instrument, orderAction, OrderEntry.Manual, tempQuantity, newTakeProfitPrice);
                                                }
                                                if (hasTakeProfitQuantityMismatch)
                                                {
                                                    validateAttachedPositionTakeProfitQuantity = false;
                                                }
                                            }
                                        }
                                        else if (hasTakeProfit && hasTakeProfitQuantityMismatch && validateAttachedPositionTakeProfitQuantity)
                                        {
                                            newTakeProfitPrice = oldTakeProfitPrice;
                                            if (blendedInstrumentHasPosition && blendedInstrumentPositionTakeProfitPrice > 0)
                                                newTakeProfitPrice = blendedInstrumentPositionTakeProfitPrice;
                                            if (currentBreakEvenAutoStatus != JoiaGestorBreakEvenAutoTypes.HODL)
                                            {
                                                if (IsECATPEnabled())
                                                {
                                                    if (lastECATakeProfitLevelLinePrice > 0)
                                                    {
                                                        newTakeProfitPrice = FilterTakeProfitByPriceMax(position.MarketPosition, lastECATakeProfitLevelLinePrice, oldTakeProfitPrice, newTakeProfitPrice);
                                                        newTakeProfitPrice = FilterTakeProfitByForceSync(TakeProfitSyncECATargetPrice, lastECATakeProfitLevelLinePrice, newTakeProfitPrice);
                                                    }
                                                }
                                                else
                                                {
                                                }
                                            }
                                            newTakeProfitPrice = FilterTakeProfitByMarketPrice(position.Instrument, position.MarketPosition, newTakeProfitPrice);
                                            hasValidNewTakeProfitPrice = newTakeProfitPrice > 0;
                                            if (hasValidNewTakeProfitPrice
                                                && hasTakeProfitQuantityMismatch
                                                && !position.HasStateChanged()
                                                && !position.IsFlat()
                                                && RealOrderService.AreAllOrderUpdateCyclesComplete())
                                            {
                                                OrderAction orderAction = ConvertMarketPositionToTPOrderAction(position.MarketPosition);
                                                double lastPrice = RealInstrumentService.GetLastPrice(position.Instrument);
                                                bool isPriceValid = RealOrderService.IsValidTakeProfitPrice(position.Instrument, orderAction, newTakeProfitPrice, lastPrice);
                                                if (isPriceValid)
                                                {
                                                    bool hasMultipleTakeProfitOrders = (takeProfitOrderCount > 1);
                                                    if (hasMultipleTakeProfitOrders)
                                                    {
                                                        ConsolidatePositionTPSLOrders("HandleTPSLRefresh", position.Instrument);
                                                    }
                                                    UpdatePositionTakeProfit(signalName, position.Instrument, orderAction, OrderEntry.Manual, tempQuantity, newTakeProfitPrice);
                                                    if (hasTakeProfitQuantityMismatch)
                                                    {
                                                        validateAttachedPositionTakeProfitQuantity = false;
                                                    }
                                                }
                                            }
                                        }
                                        else if (hasTakeProfit
                                            && IsECATPEnabled()
                                            && lastECATakeProfitLevelLinePrice > 0
                                            && !validateAttachedPositionTakeProfitQuantity
                                            && currentBreakEvenAutoStatus != JoiaGestorBreakEvenAutoTypes.HODL)
                                        {
                                            newTakeProfitPrice = FilterTakeProfitByPriceMax(position.MarketPosition, lastECATakeProfitLevelLinePrice, oldTakeProfitPrice, newTakeProfitPrice);
                                            newTakeProfitPrice = FilterTakeProfitByForceSync(TakeProfitSyncECATargetPrice, lastECATakeProfitLevelLinePrice, newTakeProfitPrice);
                                            newTakeProfitPrice = FilterTakeProfitByMarketPrice(position.Instrument, position.MarketPosition, newTakeProfitPrice);
                                            hasTakeProfitPriceMismatch = oldTakeProfitPrice > 0 && newTakeProfitPrice > 0 && oldTakeProfitPrice != newTakeProfitPrice;
                                            if (hasTakeProfitPriceMismatch && !position.HasStateChanged() && !position.IsFlat() && RealOrderService.AreAllOrderUpdateCyclesComplete())
                                            {
                                                OrderAction orderAction = ConvertMarketPositionToTPOrderAction(position.MarketPosition);
                                                double lastPrice = RealInstrumentService.GetLastPrice(position.Instrument);
                                                bool isPriceValid = RealOrderService.IsValidTakeProfitPrice(position.Instrument, orderAction, newTakeProfitPrice, lastPrice);
                                                if (isPriceValid)
                                                {
                                                    UpdatePositionTakeProfit(signalName, position.Instrument, orderAction, OrderEntry.Manual, tempQuantity, newTakeProfitPrice);
                                                }
                                            }
                                        }
                                    }
                                    attachedInstrumentPositionStopLossPrice = (newStopLossPrice == 0) ? oldStopLossPrice : newStopLossPrice;
                                    attachedInstrumentPositionTakeProfitPrice = (newTakeProfitPrice == 0) ? oldTakeProfitPrice : newTakeProfitPrice;
                                    attachedInstrumentHasChanged = true;
                                    riskInfoHasChanged = true;
                                    dayOverMaxLossHasChanged = true;
                                    bogeyTargetHasChanged = true;
                                    dayOverAccountBalanceFloorHasChanged = true;
                                    ecaTakeProfitHasChanged = true;
                                    averagePriceHasChanged = true;
                                }
                                else if (IsBlendedInstrumentEnabled() && RealPositionService.IsValidPosition(position, blendedInstrument) && position.IsValid)
                                {
                                    position.StoreState();
                                    tempQuantity = position.Quantity;
                                    hasPosition = true;
                                    multiPositionAveragePrice = (lastAveragePriceLevelLinePrice != 0) ? lastAveragePriceLevelLinePrice : position.AveragePrice;
                                    MarketPosition reversedMarketPosition = MarketPosition.Flat;
                                    if (position.MarketPosition == MarketPosition.Long)
                                        reversedMarketPosition = MarketPosition.Short;
                                    else
                                        reversedMarketPosition = MarketPosition.Long;
                                    if (CancelPositionTPSLOrders("TPSLRefresh-Rev", blendedInstrument, ConvertMarketPositionToSLOrderAction(reversedMarketPosition))) return hasPosition; 
                                    hasProfitLocked = false;
                                    hasHitPriceTrigger = false;
                                    triggerStopLossPrice = 0;
                                    newStopLossPrice = 0;
                                    stopLossOrderCount = 0;
                                    oldOrderQuantity = 0;
                                    oldStopLossPrice = RealOrderService.GetStopLossInfo(position.Account, position.Instrument, ConvertMarketPositionToSLOrderAction(position.MarketPosition), out orderType, out oldOrderQuantity, out stopLossOrderCount);
                                    hasStopLoss = (oldStopLossPrice > 0);
                                    hasStopLossQuantityMismatch = oldOrderQuantity != tempQuantity;
                                    if (this.StopLossRefreshManagementEnabled)
                                    {
                                        if (hasStopLoss && !hasStopLossQuantityMismatch) validateBlendedPositionStopLossQuantity = false;
                                        if (!hasStopLoss && IsAutoPositionStopLossEnabled())
                                        {
                                            if (DebugLogLevel > 2) RealLogger.PrintOutput("refresh SL price=" + oldStopLossPrice.ToString() + " auto=" + (IsAutoPositionStopLossEnabled()).ToString() + " oldquan=" + oldOrderQuantity.ToString() + " orderType=" + orderType);
                                            newStopLossPrice = GetInitialStopLossPrice(position.MarketPosition, position.AveragePrice, position.Quantity);
                                            if (attachedInstrumentHasPosition && attachedInstrumentPositionStopLossPrice > 0)
                                                newStopLossPrice = attachedInstrumentPositionStopLossPrice;
                                            if (IsDayOverMaxLossEnabled())
                                            {
                                                if (lastDayOverMaxLossLevelLinePrice > 0)
                                                {
                                                    newStopLossPrice = FilterStopLossByPriceMax(position.MarketPosition, lastDayOverMaxLossLevelLinePrice, oldStopLossPrice, newStopLossPrice);
                                                }
                                                else
                                                {
                                                    newStopLossPrice = 0;
                                                }
                                            }
                                            newStopLossPrice = FilterStopLossByMarketPrice(position.Instrument, position.MarketPosition, newStopLossPrice);
                                            hasValidNewStopLossPrice = newStopLossPrice > 0;
                                            if (hasValidNewStopLossPrice && !position.HasStateChanged() && !position.IsFlat() && RealOrderService.AreAllOrderUpdateCyclesComplete())
                                            {
                                                OrderAction orderAction = ConvertMarketPositionToSLOrderAction(position.MarketPosition);
                                                double lastPrice = RealInstrumentService.GetLastPrice(position.Instrument);
                                                bool isPriceValid = RealOrderService.IsValidStopLossPrice(position.Instrument, orderAction, newStopLossPrice, lastPrice);
                                                if (isPriceValid)
                                                {
                                                    if (DebugLogLevel > 2) RealLogger.PrintOutput("New SL price=" + newStopLossPrice.ToString());
                                                    CreatePositionStopLoss(signalName, position.Instrument, orderAction, OrderEntry.Manual, tempQuantity, newStopLossPrice);
                                                }
                                                if (hasStopLossQuantityMismatch)
                                                {
                                                    validateBlendedPositionStopLossQuantity = false;
                                                }
                                            }
                                        }
                                        else if (hasStopLoss && hasStopLossQuantityMismatch && validateBlendedPositionStopLossQuantity)
                                        {
                                            if (DebugLogLevel > 0) RealLogger.PrintOutput("SLQuanMismatch Current SL price=" + oldStopLossPrice.ToString() + " oldSLQuantity=" + oldOrderQuantity.ToString() + " PosQuantity=" + tempQuantity.ToString() + " SLOrderCount=" + stopLossOrderCount.ToString() + " attachedIn=" + attachedInstrument.FullName + " barPeriod=" + BarsPeriod.ToString());
                                            if (this.StopLossRefreshOnVolumeChange)
                                            {
                                                newStopLossPrice = GetInitialStopLossPrice(position.MarketPosition, position.AveragePrice, position.Quantity, oldStopLossPrice);
                                            }
                                            else
                                            {
                                                newStopLossPrice = oldStopLossPrice;
                                            }
                                            if (IsDayOverMaxLossEnabled() && lastDayOverMaxLossLevelLinePrice > 0)
                                            {
                                                newStopLossPrice = FilterStopLossByPriceMax(position.MarketPosition, lastDayOverMaxLossLevelLinePrice, oldStopLossPrice, newStopLossPrice);
                                            }
                                            newStopLossPrice = FilterStopLossByMarketPrice(position.Instrument, position.MarketPosition, newStopLossPrice);
                                            hasValidNewStopLossPrice = newStopLossPrice > 0;
                                            if (hasValidNewStopLossPrice
                                                && hasStopLossQuantityMismatch
                                                && !position.HasStateChanged()
                                                && !position.IsFlat()
                                                && RealOrderService.AreAllOrderUpdateCyclesComplete())
                                            {
                                                OrderAction orderAction = ConvertMarketPositionToSLOrderAction(position.MarketPosition);
                                                double lastPrice = RealInstrumentService.GetLastPrice(position.Instrument);
                                                bool isPriceValid = RealOrderService.IsValidStopLossPrice(position.Instrument, orderAction, newStopLossPrice, lastPrice);
                                                if (isPriceValid)
                                                {
                                                    bool hasMultipleStopLossOrders = (stopLossOrderCount > 1);
                                                    if (hasMultipleStopLossOrders)
                                                    {
                                                        ConsolidatePositionTPSLOrders("HandleTPSLRefresh", position.Instrument);
                                                    }
                                                    UpdatePositionStopLoss(signalName, position.Instrument, orderAction, OrderEntry.Manual, tempQuantity, newStopLossPrice);
                                                    if (hasStopLossQuantityMismatch)
                                                    {
                                                        validateBlendedPositionStopLossQuantity = false;
                                                    }
                                                }
                                            }
                                        }
                                        else if (hasStopLoss && IsDayOverMaxLossEnabled() && lastDayOverMaxLossLevelLinePrice > 0 && !validateBlendedPositionStopLossQuantity)
                                        {
                                            newStopLossPrice = FilterStopLossByPriceMax(position.MarketPosition, lastDayOverMaxLossLevelLinePrice, oldStopLossPrice, newStopLossPrice);
                                            newStopLossPrice = FilterStopLossByMarketPrice(position.Instrument, position.MarketPosition, newStopLossPrice);
                                            hasStopLossPriceMismatch = oldStopLossPrice > 0 && newStopLossPrice > 0 && oldStopLossPrice != newStopLossPrice;
                                            if (hasStopLossPriceMismatch && !position.HasStateChanged() && !position.IsFlat() && RealOrderService.AreAllOrderUpdateCyclesComplete())
                                            {
                                                OrderAction orderAction = ConvertMarketPositionToSLOrderAction(position.MarketPosition);
                                                double lastPrice = RealInstrumentService.GetLastPrice(position.Instrument);
                                                bool isPriceValid = RealOrderService.IsValidStopLossPrice(position.Instrument, orderAction, newStopLossPrice, lastPrice);
                                                if (isPriceValid)
                                                {
                                                    UpdatePositionStopLoss(signalName, position.Instrument, orderAction, OrderEntry.Manual, tempQuantity, newStopLossPrice);
                                                }
                                            }
                                        }
                                    }
                                    if ((BreakEvenAutoTriggerTicks > 0 || BreakEvenAutoTriggerATRMultiplier > 0)
                                        && currentBreakEvenAutoStatus != JoiaGestorBreakEvenAutoTypes.Disabled)
                                    {
                                        if (hasStopLoss && (BreakEvenAutoTriggerTicks > 0 || BreakEvenAutoTriggerATRMultiplier > 0))
                                        {
                                            triggerStopLossPrice = GetTriggerBreakEvenStopLossPrice(position.MarketPosition, position.AveragePrice);
                                            if (position.MarketPosition == MarketPosition.Long)
                                            {
                                                if (oldStopLossPrice > position.AveragePrice)
                                                {
                                                    hasProfitLocked = true;
                                                }
                                                double bidPrice = RealInstrumentService.GetBidPrice(position.Instrument);
                                                if (triggerStopLossPrice <= bidPrice)
                                                {
                                                    hasHitPriceTrigger = true;
                                                }
                                            }
                                            else if (position.MarketPosition == MarketPosition.Short)
                                            {
                                                if (oldStopLossPrice < position.AveragePrice)
                                                {
                                                    hasProfitLocked = true;
                                                }
                                                double askPrice = RealInstrumentService.GetAskPrice(position.Instrument);
                                                if (triggerStopLossPrice >= askPrice)
                                                {
                                                    hasHitPriceTrigger = true;
                                                }
                                            }
                                        }
                                        if (hasPosition && hasStopLoss && !position.HasStateChanged() && !position.IsFlat() && RealOrderService.AreAllOrderUpdateCyclesComplete())
                                        {
                                            if (!hasProfitLocked && hasHitPriceTrigger
                                                && currentBreakEvenAutoStatus != JoiaGestorBreakEvenAutoTypes.HODL)
                                            {
                                                if (DebugLogLevel > 0) RealLogger.PrintOutput("Auto BE hit trigger price of " + triggerStopLossPrice.ToString("N2"), PrintTo.OutputTab1, false);
                                                HandleBreakEvenPlus("AutoBreakEven");
                                            }
                                            else if (currentBreakEvenAutoStatus != JoiaGestorBreakEvenAutoTypes.Disabled &&
                                                (currentBreakEvenAutoStatus != JoiaGestorBreakEvenAutoTypes.Enabled || currentBreakEvenAutoStatus != JoiaGestorBreakEvenAutoTypes.HODL) && hasProfitLocked)
                                            {
                                                if (position.MarketPosition == MarketPosition.Long)
                                                {
                                                    double entryPrice = CalculateTrailLowPrice(position.MarketPosition, false);
                                                    if (entryPrice != 0 && entryPrice > oldStopLossPrice)
                                                    {
                                                        TrailBuyPositionStopLoss("AutoBreakEven");
                                                    }
                                                }
                                                else if (position.MarketPosition == MarketPosition.Short)
                                                {
                                                    double entryPrice = CalculateTrailHighPrice(position.MarketPosition);
                                                    if (entryPrice != 0 && entryPrice < oldStopLossPrice)
                                                    {
                                                        TrailSellPositionStopLoss("AutoBreakEven");
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    newTakeProfitPrice = 0;
                                    takeProfitOrderCount = 0;
                                    oldOrderQuantity = 0;
                                    oldTakeProfitPrice = RealOrderService.GetTakeProfitInfo(position.Account, position.Instrument, ConvertMarketPositionToTPOrderAction(position.MarketPosition), out orderType, out oldOrderQuantity, out takeProfitOrderCount);
                                    hasTakeProfit = (oldTakeProfitPrice > 0);
                                    hasTakeProfitQuantityMismatch = oldOrderQuantity != tempQuantity;
                                    if (TakeProfitRefreshManagementEnabled)
                                    {
                                        if (hasTakeProfit && !hasTakeProfitQuantityMismatch) validateBlendedPositionTakeProfitQuantity = false;
                                        if (!hasTakeProfit && IsAutoPositionTakeProfitEnabled())
                                        {
                                            if (DebugLogLevel > 2) RealLogger.PrintOutput("refresh tp price=" + oldTakeProfitPrice.ToString() + " auto=" + (IsAutoPositionTakeProfitEnabled()).ToString() + " oldquan=" + oldOrderQuantity.ToString());
                                            newTakeProfitPrice = GetInitialTakeProfitPrice(position.MarketPosition, position.AveragePrice);
                                            if (attachedInstrumentHasPosition && attachedInstrumentPositionTakeProfitPrice > 0)
                                                newTakeProfitPrice = attachedInstrumentPositionTakeProfitPrice;
                                            if (currentBreakEvenAutoStatus != JoiaGestorBreakEvenAutoTypes.HODL)
                                            {
                                                if (IsECATPEnabled())
                                                {
                                                    if (lastECATakeProfitLevelLinePrice > 0)
                                                    {
                                                        newTakeProfitPrice = FilterTakeProfitByPriceMax(position.MarketPosition, lastECATakeProfitLevelLinePrice, oldTakeProfitPrice, newTakeProfitPrice);
                                                        newTakeProfitPrice = FilterTakeProfitByForceSync(TakeProfitSyncECATargetPrice, lastECATakeProfitLevelLinePrice, newTakeProfitPrice);
                                                    }
                                                    else
                                                    {
                                                        newTakeProfitPrice = 0;
                                                    }
                                                }
                                                else
                                                {
                                                }
                                            }
                                            newTakeProfitPrice = FilterTakeProfitByMarketPrice(position.Instrument, position.MarketPosition, newTakeProfitPrice);
                                            hasValidNewTakeProfitPrice = newTakeProfitPrice > 0;
                                            if (hasValidNewTakeProfitPrice && !position.HasStateChanged() && !position.IsFlat() && RealOrderService.AreAllOrderUpdateCyclesComplete())
                                            {
                                                OrderAction orderAction = ConvertMarketPositionToTPOrderAction(position.MarketPosition);
                                                double lastPrice = RealInstrumentService.GetLastPrice(position.Instrument);
                                                bool isPriceValid = RealOrderService.IsValidTakeProfitPrice(position.Instrument, orderAction, newTakeProfitPrice, lastPrice);
                                                if (isPriceValid)
                                                {
                                                    if (DebugLogLevel > 2) RealLogger.PrintOutput("New TP price=" + newTakeProfitPrice.ToString());
                                                    CreatePositionTakeProfit(signalName, position.Instrument, orderAction, OrderEntry.Manual, tempQuantity, newTakeProfitPrice);
                                                }
                                                if (hasTakeProfitQuantityMismatch)
                                                {
                                                    validateBlendedPositionTakeProfitQuantity = false;
                                                }
                                            }
                                        }
                                        else if (hasTakeProfit && hasTakeProfitQuantityMismatch && validateBlendedPositionTakeProfitQuantity)
                                        {
                                            newTakeProfitPrice = oldTakeProfitPrice;
                                            if (currentBreakEvenAutoStatus != JoiaGestorBreakEvenAutoTypes.HODL)
                                            {
                                                if (IsECATPEnabled())
                                                {
                                                    if (lastECATakeProfitLevelLinePrice > 0)
                                                    {
                                                        newTakeProfitPrice = FilterTakeProfitByPriceMax(position.MarketPosition, lastECATakeProfitLevelLinePrice, oldTakeProfitPrice, newTakeProfitPrice);
                                                        newTakeProfitPrice = FilterTakeProfitByForceSync(TakeProfitSyncECATargetPrice, lastECATakeProfitLevelLinePrice, newTakeProfitPrice);
                                                    }
                                                }
                                            }
                                            newTakeProfitPrice = FilterTakeProfitByMarketPrice(position.Instrument, position.MarketPosition, newTakeProfitPrice);
                                            hasValidNewTakeProfitPrice = newTakeProfitPrice > 0;
                                            if (hasValidNewTakeProfitPrice
                                                && hasTakeProfitQuantityMismatch
                                                && !position.HasStateChanged()
                                                && !position.IsFlat()
                                                && RealOrderService.AreAllOrderUpdateCyclesComplete())
                                            {
                                                OrderAction orderAction = ConvertMarketPositionToTPOrderAction(position.MarketPosition);
                                                double lastPrice = RealInstrumentService.GetLastPrice(position.Instrument);
                                                bool isPriceValid = RealOrderService.IsValidTakeProfitPrice(position.Instrument, orderAction, newTakeProfitPrice, lastPrice);
                                                if (isPriceValid)
                                                {
                                                    bool hasMultipleTakeProfitOrders = (takeProfitOrderCount > 1);
                                                    if (hasMultipleTakeProfitOrders)
                                                    {
                                                        ConsolidatePositionTPSLOrders("HandleTPSLRefresh", position.Instrument);
                                                    }
                                                    UpdatePositionTakeProfit(signalName, position.Instrument, orderAction, OrderEntry.Manual, tempQuantity, newTakeProfitPrice);
                                                    if (hasTakeProfitQuantityMismatch)
                                                    {
                                                        validateBlendedPositionTakeProfitQuantity = false;
                                                    }
                                                }
                                            }
                                        }
                                        else if (hasTakeProfit
                                            && IsECATPEnabled()
                                            && lastECATakeProfitLevelLinePrice > 0 
                                            && !validateBlendedPositionTakeProfitQuantity
                                            && currentBreakEvenAutoStatus != JoiaGestorBreakEvenAutoTypes.HODL)
                                        {
                                            newTakeProfitPrice = FilterTakeProfitByPriceMax(position.MarketPosition, lastECATakeProfitLevelLinePrice, oldTakeProfitPrice, newTakeProfitPrice);
                                            newTakeProfitPrice = FilterTakeProfitByForceSync(TakeProfitSyncECATargetPrice, lastECATakeProfitLevelLinePrice, newTakeProfitPrice);
                                            newTakeProfitPrice = FilterTakeProfitByMarketPrice(position.Instrument, position.MarketPosition, newTakeProfitPrice);
                                            hasTakeProfitPriceMismatch = oldTakeProfitPrice > 0 && newTakeProfitPrice > 0 && oldTakeProfitPrice != newTakeProfitPrice;
                                            if (hasTakeProfitPriceMismatch && !position.HasStateChanged() && !position.IsFlat() && RealOrderService.AreAllOrderUpdateCyclesComplete())
                                            {
                                                OrderAction orderAction = ConvertMarketPositionToTPOrderAction(position.MarketPosition);
                                                double lastPrice = RealInstrumentService.GetLastPrice(position.Instrument);
                                                bool isPriceValid = RealOrderService.IsValidTakeProfitPrice(position.Instrument, orderAction, newTakeProfitPrice, lastPrice);
                                                if (isPriceValid)
                                                {
                                                    UpdatePositionTakeProfit(signalName, position.Instrument, orderAction, OrderEntry.Manual, tempQuantity, newTakeProfitPrice);
                                                }
                                            }
                                        }
                                    }
                                    blendedInstrumentPositionStopLossPrice = (newStopLossPrice == 0) ? oldStopLossPrice : newStopLossPrice;
                                    blendedInstrumentPositionTakeProfitPrice = (newTakeProfitPrice == 0) ? oldTakeProfitPrice : newTakeProfitPrice;
                                    blendedInstrumentHasChanged = true;
                                }
                            }
                        }
                    }
                    if (IsAccountFlat(attachedInstrument) && RealOrderService.AreAllOrderUpdateCyclesComplete())
                    {
                        CancelPositionTPSLOrders("TPSLRefresh-All", attachedInstrument);
                        attachedInstrumentHasPosition = false;
                        attachedInstrumentMarketPosition = MarketPosition.Flat;
                        attachedInstrumentPositionPrice = 0;
                        attachedInstrumentPositionQuantity = 0;
                        attachedInstrumentPositionStopLossPrice = 0;
                        attachedInstrumentPositionTakeProfitPrice = 0;
                        attachedInstrumentHasChanged = true;
                        riskInfoHasChanged = true;
                        profitInfoHasChanged = true;
                        dayOverMaxLossHasChanged = true;
                        bogeyTargetHasChanged = true;
                        dayOverAccountBalanceFloorHasChanged = true;
                        ecaTakeProfitHasChanged = true;
                        averagePriceHasChanged = true;
                    }
                    if (IsAccountFlat(blendedInstrument) && RealOrderService.AreAllOrderUpdateCyclesComplete())
                    {
                        CancelPositionTPSLOrders("TPSLRefresh-All", blendedInstrument);
                        blendedInstrumentHasPosition = false;
                        blendedInstrumentMarketPosition = MarketPosition.Flat;
                        blendedInstrumentPositionPrice = 0;
                        blendedInstrumentPositionQuantity = 0;
                        blendedInstrumentPositionStopLossPrice = 0;
                        blendedInstrumentPositionTakeProfitPrice = 0;
                        blendedInstrumentHasChanged = true;
                        bogeyTargetHasChanged = true;
                        ecaTakeProfitHasChanged = true;
                        averagePriceHasChanged = true;
                    }
                }
            }
            catch (Exception ex)
            {
                RealLogger.PrintOutput("Exception calling HandleTPSLRefresh:" + ex.Message + " " + ex.StackTrace);
                throw;
            }
            finally
            {
                if (lockTaken)
                    Monitor.Exit(RefreshTPSLLock);
            }
            return hasPosition;
        }
        private bool HandlePositionInfoRefresh(string signalName)
        {
            bool hasPosition = false;
            var lockTimeout = TimeSpan.FromSeconds(10);
            bool lockTaken = false;
            try
            {
                Monitor.TryEnter(RefreshPositionInfoLock, lockTimeout, ref lockTaken);
                if (lockTaken)
                {
                    if ((!IsAccountFlat(attachedInstrument) || (IsBlendedInstrumentEnabled() && !IsAccountFlat(blendedInstrument)))
                        && RealOrderService.AreAllOrderUpdateCyclesComplete())
                    {
                        double multiPositionAveragePrice = 0;
                        int positionCount = RealPositionService.PositionCount;
                        for (int index = 0; index < positionCount; index++)
                        {
                            RealPosition position = null;
                            if (RealPositionService.TryGetByIndex(index, out position))
                            {
                                if (RealPositionService.IsValidPosition(position, attachedInstrument) && position.IsValid)
                                {
                                    hasPosition = true;
                                    multiPositionAveragePrice = (lastAveragePriceLevelLinePrice != 0) ? lastAveragePriceLevelLinePrice : position.AveragePrice;
                                    attachedInstrumentHasPosition = true;
                                    attachedInstrumentMarketPosition = position.MarketPosition;
                                    attachedInstrumentPositionQuantity = position.Quantity;
                                    attachedInstrumentPositionPrice = position.AveragePrice;
                                    attachedInstrumentHasChanged = true;
                                    riskInfoMarketPosition = position.MarketPosition;
                                    riskInfoQuantity = position.Quantity;
                                    riskInfoPositionPrice = position.AveragePrice;
                                    riskInfoHasChanged = true;
                                    profitInfoMarketPosition = position.MarketPosition;
                                    profitInfoQuantity = position.Quantity;
                                    profitInfoPositionPrice = position.AveragePrice;
                                    profitInfoHasChanged = true;
                                    dayOverMaxLossMarketPosition = position.MarketPosition;
                                    dayOverMaxLossPositionQuantity = position.Quantity;
                                    dayOverMaxLossPositionPrice = position.AveragePrice;
                                    dayOverMaxLossHasChanged = true;
                                    bogeyTargetMarketPosition = position.MarketPosition;
                                    bogeyTargetPositionQuantity = position.Quantity;
                                    bogeyTargetPositionPrice = position.AveragePrice;
                                    bogeyTargetHasChanged = true;
                                    dayOverAccountBalanceFloorMarketPosition = position.MarketPosition;
                                    dayOverAccountBalanceFloorPositionQuantity = position.Quantity;
                                    dayOverAccountBalanceFloorPositionPrice = position.AveragePrice;
                                    dayOverAccountBalanceFloorHasChanged = true;
                                    ecaTakeProfitMarketPosition = position.MarketPosition;
                                    ecaTakeProfitPositionQuantity = position.Quantity;
                                    ecaTakeProfitPositionPrice = position.AveragePrice;
                                    ecaTakeProfitHasChanged = true;
                                    averagePriceMarketPosition = position.MarketPosition;
                                    averagePricePositionQuantity = position.Quantity;
                                    averagePricePositionPrice = position.AveragePrice;
                                    averagePriceHasChanged = true;
                                }
                                else if (IsBlendedInstrumentEnabled() && RealPositionService.IsValidPosition(position, blendedInstrument) && position.IsValid)
                                {
                                    hasPosition = true;
                                    multiPositionAveragePrice = (lastAveragePriceLevelLinePrice != 0) ? lastAveragePriceLevelLinePrice : position.AveragePrice;
                                    blendedInstrumentHasPosition = true;
                                    blendedInstrumentMarketPosition = position.MarketPosition;
                                    blendedInstrumentPositionQuantity = position.Quantity;
                                    blendedInstrumentPositionPrice = position.AveragePrice;
                                    blendedInstrumentHasChanged = true;
                                }
                            }
                        }
                    }
                    if (IsBlendedInstrumentEnabled())
                    {
                        if (attachedInstrumentHasChanged || blendedInstrumentHasChanged)
                        {
                            MarketPosition mixedInstrumentMarketPosition = (attachedInstrumentHasPosition) ? attachedInstrumentMarketPosition : blendedInstrumentMarketPosition;
                            double newWeightedAveragePrice = (attachedInstrumentHasPosition) ? attachedInstrumentPositionPrice : blendedInstrumentPositionPrice;
                            int mixedInstrumentQuantitySum = (attachedInstrumentHasPosition) ? attachedInstrumentPositionQuantity : blendedInstrumentPositionQuantity;
                            if (attachedInstrumentHasPosition && blendedInstrumentHasPosition)
                            {
                                int eminiQuantity = 0;
                                double eminiAveragePrice = 0;
                                int microQuantity = 0;
                                double microAveragePrice = 0;
                                if (attachedInstrumentIsEmini)
                                {
                                    microQuantity = blendedInstrumentPositionQuantity;
                                    microAveragePrice = blendedInstrumentPositionPrice;
                                    eminiQuantity = attachedInstrumentPositionQuantity * MICRO_TO_EMINI_MULTIPLIER;
                                    eminiAveragePrice = attachedInstrumentPositionPrice;
                                }
                                else
                                {
                                    microQuantity = attachedInstrumentPositionQuantity;
                                    microAveragePrice = attachedInstrumentPositionPrice;
                                    eminiQuantity = blendedInstrumentPositionQuantity * MICRO_TO_EMINI_MULTIPLIER;
                                    eminiAveragePrice = blendedInstrumentPositionPrice;
                                }
                                mixedInstrumentQuantitySum = microQuantity + eminiQuantity;
                                newWeightedAveragePrice = ((microAveragePrice * microQuantity) + (eminiAveragePrice * eminiQuantity)) / mixedInstrumentQuantitySum;
                            }
                            else if (attachedInstrumentHasPosition && !blendedInstrumentHasPosition)
                            {
                                if (attachedInstrumentIsEmini)
                                {
                                    mixedInstrumentQuantitySum = attachedInstrumentPositionQuantity * MICRO_TO_EMINI_MULTIPLIER;
                                }
                            }
                            else if (!attachedInstrumentHasPosition && blendedInstrumentHasPosition)
                            {
                                if (!attachedInstrumentIsEmini)
                                {
                                    mixedInstrumentQuantitySum = blendedInstrumentPositionQuantity * MICRO_TO_EMINI_MULTIPLIER;
                                }
                            }
                            riskInfoMarketPosition = mixedInstrumentMarketPosition;
                            riskInfoQuantity = mixedInstrumentQuantitySum;
                            riskInfoPositionPrice = newWeightedAveragePrice;
                            riskInfoHasChanged = true;
                            profitInfoMarketPosition = mixedInstrumentMarketPosition;
                            profitInfoQuantity = mixedInstrumentQuantitySum;
                            profitInfoPositionPrice = newWeightedAveragePrice;
                            profitInfoHasChanged = true;
                            dayOverMaxLossMarketPosition = mixedInstrumentMarketPosition;
                            dayOverMaxLossPositionQuantity = mixedInstrumentQuantitySum;
                            dayOverMaxLossPositionPrice = newWeightedAveragePrice;
                            dayOverMaxLossHasChanged = true;
                            dayOverAccountBalanceFloorMarketPosition = mixedInstrumentMarketPosition;
                            dayOverAccountBalanceFloorPositionQuantity = mixedInstrumentQuantitySum;
                            dayOverAccountBalanceFloorPositionPrice = newWeightedAveragePrice;
                            dayOverAccountBalanceFloorHasChanged = true;
                            bogeyTargetMarketPosition = mixedInstrumentMarketPosition;
                            bogeyTargetPositionQuantity = mixedInstrumentQuantitySum;
                            bogeyTargetPositionPrice = newWeightedAveragePrice;
                            bogeyTargetHasChanged = true;
                            averagePriceMarketPosition = mixedInstrumentMarketPosition;
                            averagePricePositionQuantity = mixedInstrumentQuantitySum;
                            averagePricePositionPrice = newWeightedAveragePrice;
                            averagePriceHasChanged = true;
                            ecaTakeProfitMarketPosition = mixedInstrumentMarketPosition;
                            ecaTakeProfitPositionQuantity = mixedInstrumentQuantitySum;
                            ecaTakeProfitPositionPrice = newWeightedAveragePrice;
                            ecaTakeProfitHasChanged = true;
                        }
                    }
                    if (IsAccountFlat(attachedInstrument) && RealOrderService.AreAllOrderUpdateCyclesComplete())
                    {
                        attachedInstrumentHasPosition = false;
                        attachedInstrumentMarketPosition = MarketPosition.Flat;
                        attachedInstrumentPositionPrice = 0;
                        attachedInstrumentPositionQuantity = 0;
                        attachedInstrumentPositionStopLossPrice = 0;
                        attachedInstrumentPositionTakeProfitPrice = 0;
                        attachedInstrumentHasChanged = true;
                        riskInfoHasChanged = true;
                        profitInfoHasChanged = true;
                        dayOverMaxLossHasChanged = true;
                        bogeyTargetHasChanged = true;
                        dayOverAccountBalanceFloorHasChanged = true;
                        ecaTakeProfitHasChanged = true;
                        averagePriceHasChanged = true;
                    }
                    if (IsAccountFlat(blendedInstrument) && RealOrderService.AreAllOrderUpdateCyclesComplete())
                    {
                        blendedInstrumentHasPosition = false;
                        blendedInstrumentMarketPosition = MarketPosition.Flat;
                        blendedInstrumentPositionPrice = 0;
                        blendedInstrumentPositionQuantity = 0;
                        blendedInstrumentPositionStopLossPrice = 0;
                        blendedInstrumentPositionTakeProfitPrice = 0;
                        blendedInstrumentHasChanged = true;
                        bogeyTargetHasChanged = true;
                        ecaTakeProfitHasChanged = true;
                        averagePriceHasChanged = true;
                    }
                }
            }
            catch (Exception ex)
            {
                RealLogger.PrintOutput("Exception calling HandlePositionInfoRefresh:" + ex.Message + " " + ex.StackTrace);
                throw;
            }
            finally
            {
                if (lockTaken)
                    Monitor.Exit(RefreshPositionInfoLock);
            }
            return hasPosition;
        }
        private double FilterStopLossByPriceMax(MarketPosition marketPosition, double filterPrice, double oldStopLossPrice, double newStopLossPrice)
        {
            if (filterPrice != 0)
            {
                if (marketPosition == MarketPosition.Long)
                {
                    if ((oldStopLossPrice != 0 && oldStopLossPrice < filterPrice)
                        || (newStopLossPrice != 0 && newStopLossPrice < filterPrice))
                    {
                        newStopLossPrice = filterPrice;
                    }
                }
                else if (marketPosition == MarketPosition.Short)
                {
                    if ((oldStopLossPrice != 0 && oldStopLossPrice > filterPrice)
                        || (newStopLossPrice != 0 && newStopLossPrice > filterPrice))
                    {
                        newStopLossPrice = filterPrice;
                    }
                }
            }
            return newStopLossPrice;
        }
        private double FilterStopLossByMarketPrice(Instrument instrument, MarketPosition marketPosition, double newStopLossPrice)
        {
            if (newStopLossPrice != 0)
            {
                if (marketPosition == MarketPosition.Long)
                {
                    double bidPrice = RealInstrumentService.GetBidPrice(instrument);
                    double lastPrice = RealInstrumentService.GetLastPrice(instrument);
                    if (newStopLossPrice >= (Math.Min(bidPrice, lastPrice) - (attachedInstrumentTickSize * RefreshTPSLPaddingTicks)))
                    {
                        newStopLossPrice = 0;
                    }
                }
                else if (marketPosition == MarketPosition.Short)
                {
                    double askPrice = RealInstrumentService.GetAskPrice(instrument);
                    double lastPrice = RealInstrumentService.GetLastPrice(instrument);
                    if (newStopLossPrice <= (Math.Max(askPrice, lastPrice) + (attachedInstrumentTickSize * RefreshTPSLPaddingTicks)))
                    {
                        newStopLossPrice = 0;
                    }
                }
            }
            return newStopLossPrice;
        }
        private double FilterTakeProfitByPriceMax(MarketPosition marketPosition, double filterPrice, double oldTakeProfitPrice, double newTakeProfitPrice)
        {
            if (filterPrice != 0)
            {
                if (marketPosition == MarketPosition.Long)
                {
                    if ((oldTakeProfitPrice != 0 && oldTakeProfitPrice > filterPrice)
                        || (newTakeProfitPrice != 0 && newTakeProfitPrice > filterPrice))
                    {
                        newTakeProfitPrice = filterPrice;
                    }
                }
                else if (marketPosition == MarketPosition.Short)
                {
                    if ((oldTakeProfitPrice != 0 && oldTakeProfitPrice < filterPrice)
                        || (newTakeProfitPrice != 0 && newTakeProfitPrice < filterPrice))
                    {
                        newTakeProfitPrice = filterPrice;
                    }
                }
            }
            return newTakeProfitPrice;
        }
        private double FilterTakeProfitByMarketPrice(Instrument instrument, MarketPosition marketPosition, double newTakeProfitPrice)
        {
            if (newTakeProfitPrice != 0)
            {
                if (marketPosition == MarketPosition.Long)
                {
                    double askPrice = RealInstrumentService.GetAskPrice(instrument);
                    double lastPrice = RealInstrumentService.GetLastPrice(instrument);
                    if (newTakeProfitPrice <= (Math.Max(askPrice, lastPrice) + (attachedInstrumentTickSize * RefreshTPSLPaddingTicks)))
                    {
                        newTakeProfitPrice = 0;
                    }
                }
                else if (marketPosition == MarketPosition.Short)
                {
                    double bidPrice = RealInstrumentService.GetBidPrice(instrument);
                    double lastPrice = RealInstrumentService.GetLastPrice(instrument);
                    if (newTakeProfitPrice >= (Math.Min(bidPrice, lastPrice) - (attachedInstrumentTickSize * RefreshTPSLPaddingTicks)))
                    {
                        newTakeProfitPrice = 0;
                    }
                }
            }
            return newTakeProfitPrice;
        }
        private double FilterTakeProfitByForceSync(bool applyForceSync, double forcePrice, double newTakeProfitPrice)
        {
            if (applyForceSync)
            {
                newTakeProfitPrice = forcePrice;
            }
            return newTakeProfitPrice;
        }
    private bool HandleTakeProfitPlus(string signalName, double overrideTakeProfitPrice = 0)
        {
            double oldTakeProfitPrice = 0;
            int oldOrderQuantity = 0;
            double newTakeProfitPrice = 0;
            bool positionFound = false;
            bool hasTakeProfit = false;
            bool hasValidNewTakeProfitPrice = false;
            bool hasTakeProfitPriceMismatch = false;
            int takeProfitOrderCount = 0;
            OrderType orderType = OrderType.Unknown;
            int tempQuantity = 0;
            bool isCtrlKeyDown = false;
            MarketPosition positionFoundMarketPosition = MarketPosition.Flat;
            double multiPositionAveragePrice = 0;
            bool attachedHasBeenProcessed = false;
            bool blendedHasBeenProcessed = false;
            double tempAttachedInstrumentPositionTakeProfitPrice = 0;
            double tempBlendedInstrumentPositionTakeProfitPrice = 0;
            int positionCount = RealPositionService.PositionCount;
            for (int index = 0; index < positionCount; index++)
            {
                RealPosition position = null;
                if (RealPositionService.TryGetByIndex(index, out position))
                {
                    if (RealPositionService.IsValidPosition(position, attachedInstrument) && position.IsValid)
                    {
                        position.StoreState();
                        positionFound = true;
                        tempQuantity = position.Quantity;
                        newTakeProfitPrice = 0;
                        takeProfitOrderCount = 0;
                        positionFoundMarketPosition = position.MarketPosition;
                        multiPositionAveragePrice = (lastAveragePriceLevelLinePrice != 0) ? lastAveragePriceLevelLinePrice : position.AveragePrice;
                        isCtrlKeyDown = IsCtrlKeyDown();
                        if (isCtrlKeyDown && overrideTakeProfitPrice == 0) break;
                        oldTakeProfitPrice = RealOrderService.GetTakeProfitInfo(position.Account, position.Instrument, ConvertMarketPositionToTPOrderAction(position.MarketPosition), out orderType, out oldOrderQuantity, out takeProfitOrderCount);
                        hasTakeProfit = oldTakeProfitPrice == 0;
                        if (DebugLogLevel > 2) RealLogger.PrintOutput("Current TP price=" + oldTakeProfitPrice.ToString() + " ticksize=" + attachedInstrumentTickSize.ToString() + " ticks/point=" + attachedInstrumentTicksPerPoint.ToString() + " position quantity=" + tempQuantity.ToString());
                        if (hasTakeProfit)
                        {
                            newTakeProfitPrice = (overrideTakeProfitPrice != 0) ? overrideTakeProfitPrice : GetInitialTakeProfitPrice(position.MarketPosition, multiPositionAveragePrice);
                            if (blendedInstrumentHasPosition && tempBlendedInstrumentPositionTakeProfitPrice > 0)
                                newTakeProfitPrice = tempBlendedInstrumentPositionTakeProfitPrice;
                            newTakeProfitPrice = FilterTakeProfitByMarketPrice(position.Instrument, position.MarketPosition, newTakeProfitPrice);
                            hasValidNewTakeProfitPrice = newTakeProfitPrice > 0;
                            if (hasValidNewTakeProfitPrice && !position.HasStateChanged() && !position.IsFlat())
                            {
                                OrderAction orderAction = ConvertMarketPositionToTPOrderAction(position.MarketPosition);
                                double lastPrice = RealInstrumentService.GetLastPrice(position.Instrument);
                                bool isPriceValid = RealOrderService.IsValidTakeProfitPrice(position.Instrument, orderAction, newTakeProfitPrice, lastPrice);
                                if (isPriceValid)
                                {
                                    if (DebugLogLevel > 2) RealLogger.PrintOutput("New TP price=" + newTakeProfitPrice.ToString());
                                    CreatePositionTakeProfit(signalName, position.Instrument, orderAction, OrderEntry.Manual, tempQuantity, newTakeProfitPrice);
                                }
                            }
                        }
                        else
                        {
                            newTakeProfitPrice = (overrideTakeProfitPrice != 0) ? overrideTakeProfitPrice : GetTakeProfitPriceFromJumpTicks(position.MarketPosition, oldTakeProfitPrice, this.TakeProfitJumpTicks);
                            if (blendedInstrumentHasPosition && tempBlendedInstrumentPositionTakeProfitPrice > 0 && blendedHasBeenProcessed)
                                newTakeProfitPrice = tempBlendedInstrumentPositionTakeProfitPrice;
                            newTakeProfitPrice = FilterTakeProfitByMarketPrice(position.Instrument, position.MarketPosition, newTakeProfitPrice);
                            hasTakeProfitPriceMismatch = oldTakeProfitPrice > 0 && newTakeProfitPrice > 0 && oldTakeProfitPrice != newTakeProfitPrice;
                            if (hasTakeProfitPriceMismatch && !position.HasStateChanged() && !position.IsFlat())
                            {
                                OrderAction orderAction = ConvertMarketPositionToTPOrderAction(position.MarketPosition);
                                double lastPrice = RealInstrumentService.GetLastPrice(position.Instrument);
                                bool isPriceValid = RealOrderService.IsValidTakeProfitPrice(position.Instrument, orderAction, newTakeProfitPrice, lastPrice);
                                if (isPriceValid)
                                {
                                    if (DebugLogLevel > 2) RealLogger.PrintOutput("Updated TP price=" + newTakeProfitPrice.ToString() + " old=" + oldTakeProfitPrice.ToString());
                                    UpdatePositionTakeProfit(signalName, position.Instrument, orderAction, OrderEntry.Manual, 0, newTakeProfitPrice);
                                }
                            }
                        }
                        attachedHasBeenProcessed = true;
                        tempAttachedInstrumentPositionTakeProfitPrice = (newTakeProfitPrice == 0) ? oldTakeProfitPrice : newTakeProfitPrice;
                    }
                    else if (IsBlendedInstrumentEnabled() && RealPositionService.IsValidPosition(position, blendedInstrument) && position.IsValid)
                    {
                        position.StoreState();
                        positionFound = true;
                        tempQuantity = position.Quantity;
                        newTakeProfitPrice = 0;
                        takeProfitOrderCount = 0;
                        positionFoundMarketPosition = position.MarketPosition;
                        multiPositionAveragePrice = (lastAveragePriceLevelLinePrice != 0) ? lastAveragePriceLevelLinePrice : position.AveragePrice;
                        isCtrlKeyDown = IsCtrlKeyDown();
                        if (isCtrlKeyDown && overrideTakeProfitPrice == 0) break;
                        oldTakeProfitPrice = RealOrderService.GetTakeProfitInfo(position.Account, position.Instrument, ConvertMarketPositionToTPOrderAction(position.MarketPosition), out orderType, out oldOrderQuantity, out takeProfitOrderCount);
                        hasTakeProfit = oldTakeProfitPrice == 0;
                        if (DebugLogLevel > 2) RealLogger.PrintOutput("Current TP price=" + oldTakeProfitPrice.ToString() + " ticksize=" + attachedInstrumentTickSize.ToString() + " ticks/point=" + attachedInstrumentTicksPerPoint.ToString() + " position quantity=" + tempQuantity.ToString());
                        if (hasTakeProfit)
                        {
                            newTakeProfitPrice = (overrideTakeProfitPrice != 0) ? overrideTakeProfitPrice : GetInitialTakeProfitPrice(position.MarketPosition, multiPositionAveragePrice);
                            if (attachedInstrumentHasPosition && tempAttachedInstrumentPositionTakeProfitPrice > 0)
                                newTakeProfitPrice = tempAttachedInstrumentPositionTakeProfitPrice;
                            newTakeProfitPrice = FilterTakeProfitByMarketPrice(position.Instrument, position.MarketPosition, newTakeProfitPrice);
                            hasValidNewTakeProfitPrice = newTakeProfitPrice > 0;
                            if (hasValidNewTakeProfitPrice && !position.HasStateChanged() && !position.IsFlat())
                            {
                                OrderAction orderAction = ConvertMarketPositionToTPOrderAction(position.MarketPosition);
                                double lastPrice = RealInstrumentService.GetLastPrice(position.Instrument);
                                bool isPriceValid = RealOrderService.IsValidTakeProfitPrice(position.Instrument, orderAction, newTakeProfitPrice, lastPrice);
                                if (isPriceValid)
                                {
                                    if (DebugLogLevel > 2) RealLogger.PrintOutput("New TP price=" + newTakeProfitPrice.ToString());
                                    CreatePositionTakeProfit(signalName, position.Instrument, orderAction, OrderEntry.Manual, tempQuantity, newTakeProfitPrice);
                                }
                            }
                        }
                        else
                        {
                            newTakeProfitPrice = (overrideTakeProfitPrice != 0) ? overrideTakeProfitPrice : GetTakeProfitPriceFromJumpTicks(position.MarketPosition, oldTakeProfitPrice, this.TakeProfitJumpTicks);
                            if (attachedInstrumentHasPosition && tempAttachedInstrumentPositionTakeProfitPrice > 0 && attachedHasBeenProcessed)
                                newTakeProfitPrice = tempAttachedInstrumentPositionTakeProfitPrice;
                            newTakeProfitPrice = FilterTakeProfitByMarketPrice(position.Instrument, position.MarketPosition, newTakeProfitPrice);
                            hasTakeProfitPriceMismatch = oldTakeProfitPrice > 0 && newTakeProfitPrice > 0 && oldTakeProfitPrice != newTakeProfitPrice;
                            if (hasTakeProfitPriceMismatch && !position.HasStateChanged() && !position.IsFlat())
                            {
                                OrderAction orderAction = ConvertMarketPositionToTPOrderAction(position.MarketPosition);
                                double lastPrice = RealInstrumentService.GetLastPrice(position.Instrument);
                                bool isPriceValid = RealOrderService.IsValidTakeProfitPrice(position.Instrument, orderAction, newTakeProfitPrice, lastPrice);
                                if (isPriceValid)
                                {
                                    if (DebugLogLevel > 2) RealLogger.PrintOutput("Updated TP price=" + newTakeProfitPrice.ToString() + " old=" + oldTakeProfitPrice.ToString());
                                    UpdatePositionTakeProfit(signalName, position.Instrument, orderAction, OrderEntry.Manual, 0, newTakeProfitPrice);
                                }
                            }
                        }
                        blendedHasBeenProcessed = true;
                        tempBlendedInstrumentPositionTakeProfitPrice = (newTakeProfitPrice == 0) ? oldTakeProfitPrice : newTakeProfitPrice;
                    }
                }
            }
            if (positionFound && isCtrlKeyDown && overrideTakeProfitPrice == 0)
            {
                if (positionFoundMarketPosition == MarketPosition.Long)
                {
                    HandleSellSnap("HandleTakeProfitPlus");
                }
                else if (positionFoundMarketPosition == MarketPosition.Short)
                {
                    HandleBuySnap("HandleTakeProfitPlus");
                }
            }
            return positionFound;
        }
        private bool HandleSellSnap(string signalName)
        {
            double newSellSnapPrice = 0;
            bool positionFound = false;
            bool isShortPosition = false;
            bool orderFound = false;
            double oldStopLossPrice = 0;
            double takeProfitPrice = 0;
            int stopLossOrderCount = 0;
            OrderType orderType = OrderType.Unknown;
            int positionCount = RealPositionService.PositionCount;
            for (int index = 0; index < positionCount; index++)
            {
                RealPosition position = null;
                if (RealPositionService.TryGetByIndex(index, out position))
                {
                    if (RealPositionService.IsValidPosition(position, attachedInstrument) && position.IsValid)
                    {
                        isShortPosition = (position.MarketPosition == MarketPosition.Short);
                        positionFound = true;
                        int oldOrderQuantity = 0;
                        stopLossOrderCount = 0;
                        oldStopLossPrice = RealOrderService.GetStopLossInfo(position.Account, position.Instrument, ConvertMarketPositionToSLOrderAction(position.MarketPosition), out orderType, out oldOrderQuantity, out stopLossOrderCount);
                        int stopLossTicks = CalculateStopLossTicks(position.MarketPosition, position.AveragePrice, oldStopLossPrice, attachedInstrumentTickSize);
                        int stopLossMultipliedTicks = (int)(stopLossTicks * TakeProfitCtrlSLMultiplier);
                        takeProfitPrice = CalculateTakeProfitPrice(position.MarketPosition, position.AveragePrice, stopLossMultipliedTicks, attachedInstrumentTickSize, attachedInstrumentTicksPerPoint);
                        break;
                    }
                }
            }
            if (!isShortPosition && positionFound && CheckSnapPositionTPSL())
            {
                if (takeProfitPrice > 0)
                {
                    HandleTakeProfitPlus("SellSnap", takeProfitPrice);
                }
                positionFound = false;
            }
            else if (isShortPosition && positionFound && CheckSnapPositionTPSL())
            {
                TrailSellPositionStopLoss("SellSnap", true);
            }
            else if (!positionFound || !CheckSnapPositionTPSL())
            {
                orderFound = CancelPopDropOrders("SellSnap");
                if (!orderFound)
                {
                    newSellSnapPrice = CalculateTrailLowPrice(MarketPosition.Short, true);
                    double bidPrice = RealInstrumentService.GetBidPrice(attachedInstrument);
                    if (newSellSnapPrice >= bidPrice)
                    {
                        newSellSnapPrice = 0;
                    }
                    if (newSellSnapPrice != 0)
                    {
                        if (DebugLogLevel > 2) RealLogger.PrintOutput("New Snap- price=" + newSellSnapPrice.ToString());
                        int autoEntryVolume = CalculateAutoEntryVolume(currentEntryVolumeAutoStatus);
                        CreateSellStop(signalName, attachedInstrument, OrderAction.SellShort, OrderEntry.Manual, autoEntryVolume, newSellSnapPrice);
                    }
                }
            }
            return positionFound;
        }
        private void TrailSellPositionStopLoss(string signalName, bool force1Bar = false)
        {
            double newEntryPrice = CalculateTrailHighPrice(MarketPosition.Short, force1Bar);
            double askPrice = RealInstrumentService.GetAskPrice(attachedInstrument);
            double lastPrice = RealInstrumentService.GetLastPrice(attachedInstrument);
            if (newEntryPrice <= (Math.Max(askPrice, lastPrice) + (attachedInstrumentTickSize * RefreshTPSLPaddingTicks)))
            {
                newEntryPrice = 0;
            }
            if (newEntryPrice != 0)
            {
                HandleStopLossPlus(signalName, newEntryPrice);
            }
        }
        private void TrailBuyPositionStopLoss(string signalName, bool force1Bar = false)
        {
            double newEntryPrice = CalculateTrailLowPrice(MarketPosition.Long, force1Bar);
            double bidPrice = RealInstrumentService.GetBidPrice(attachedInstrument);
            double lastPrice = RealInstrumentService.GetLastPrice(attachedInstrument);
            if (newEntryPrice >= (Math.Min(bidPrice, lastPrice) - (attachedInstrumentTickSize * RefreshTPSLPaddingTicks)))
            {
                newEntryPrice = 0;
            }
            if (newEntryPrice != 0)
            {
                HandleStopLossPlus(signalName, newEntryPrice);
            }
        }
        private double CalculateTrailLowPrice(MarketPosition positionType, bool force1Bar = false)
        {
            double entryPrice = 0;
            if (force1Bar || currentBreakEvenAutoStatus == JoiaGestorBreakEvenAutoTypes.PlusTrail1Bar)
                entryPrice = previous1LowPrice - (attachedInstrumentTickSize * 2);
            else if (currentBreakEvenAutoStatus == JoiaGestorBreakEvenAutoTypes.PlusTrail2Bar)
                entryPrice = Math.Min(previous1LowPrice, previous2LowPrice) - (attachedInstrumentTickSize * 2);
            else if (currentBreakEvenAutoStatus == JoiaGestorBreakEvenAutoTypes.PlusTrail3Bar)
                entryPrice = Math.Min(Math.Min(previous1LowPrice, previous2LowPrice), previous3LowPrice) - (attachedInstrumentTickSize * 2);
            else if (currentBreakEvenAutoStatus == JoiaGestorBreakEvenAutoTypes.PlusTrail5Bar)
                entryPrice = Math.Min(Math.Min(Math.Min(Math.Min(previous1LowPrice, previous2LowPrice), previous3LowPrice), previous4LowPrice), previous5LowPrice) - (attachedInstrumentTickSize * 2);
            else if (currentBreakEvenAutoStatus == JoiaGestorBreakEvenAutoTypes.PlusTrailMovingAverage1)
                entryPrice = GetBreakEvenAutoMovingAverage1Price(positionType);
            else if (currentBreakEvenAutoStatus == JoiaGestorBreakEvenAutoTypes.PlusTrailMovingAverage2)
                entryPrice = GetBreakEvenAutoMovingAverage2Price(positionType);
            else if (currentBreakEvenAutoStatus == JoiaGestorBreakEvenAutoTypes.PlusTrailMovingAverage3)
                entryPrice = GetBreakEvenAutoMovingAverage3Price(positionType);
    	    return entryPrice;
        }
        private double CalculateTrailHighPrice(MarketPosition positionType, bool force1Bar = false)
        {
            double entryPrice = 0;
            if (force1Bar || currentBreakEvenAutoStatus == JoiaGestorBreakEvenAutoTypes.PlusTrail1Bar)
                entryPrice = previous1HighPrice + (attachedInstrumentTickSize * 2);
            else if (currentBreakEvenAutoStatus == JoiaGestorBreakEvenAutoTypes.PlusTrail2Bar)
                entryPrice = Math.Max(previous1HighPrice, previous2HighPrice) + (attachedInstrumentTickSize * 2);
            else if (currentBreakEvenAutoStatus == JoiaGestorBreakEvenAutoTypes.PlusTrail3Bar)
                entryPrice = Math.Max(Math.Max(previous1HighPrice, previous2HighPrice), previous3HighPrice) + (attachedInstrumentTickSize * 2);
            else if (currentBreakEvenAutoStatus == JoiaGestorBreakEvenAutoTypes.PlusTrail5Bar)
                entryPrice = Math.Max(Math.Max(Math.Max(Math.Max(previous1HighPrice, previous2HighPrice), previous3HighPrice), previous4HighPrice), previous5HighPrice) + (attachedInstrumentTickSize * 2);
            else if (currentBreakEvenAutoStatus == JoiaGestorBreakEvenAutoTypes.PlusTrailMovingAverage1)
                entryPrice = GetBreakEvenAutoMovingAverage1Price(positionType);
            else if (currentBreakEvenAutoStatus == JoiaGestorBreakEvenAutoTypes.PlusTrailMovingAverage2)
                entryPrice = GetBreakEvenAutoMovingAverage2Price(positionType);
            else if (currentBreakEvenAutoStatus == JoiaGestorBreakEvenAutoTypes.PlusTrailMovingAverage3)
                entryPrice = GetBreakEvenAutoMovingAverage3Price(positionType);
            return entryPrice;
        }
        private double CalculateSnapBarLowPrice(JoiaGestorStopLossSnapTypes stopLossSnapType)
        {
            double entryPrice = 0;
            if (stopLossSnapType == JoiaGestorStopLossSnapTypes.Snap1Bar)
                entryPrice = previous1LowPrice - (attachedInstrumentTickSize * 2);
            else if (stopLossSnapType == JoiaGestorStopLossSnapTypes.Snap2Bar)
                entryPrice = Math.Min(previous1LowPrice, previous2LowPrice) - (attachedInstrumentTickSize * 2);
            else if (stopLossSnapType == JoiaGestorStopLossSnapTypes.Snap3Bar)
                entryPrice = Math.Min(Math.Min(previous1LowPrice, previous2LowPrice), previous3LowPrice) - (attachedInstrumentTickSize * 2);
            else if (stopLossSnapType == JoiaGestorStopLossSnapTypes.Snap5Bar)
                entryPrice = Math.Min(Math.Min(Math.Min(Math.Min(previous1LowPrice, previous2LowPrice), previous3LowPrice), previous4LowPrice), previous5LowPrice) - (attachedInstrumentTickSize * 2);
            else if (stopLossSnapType == JoiaGestorStopLossSnapTypes.Snap8Bar)
                entryPrice = Math.Min(Math.Min(Math.Min(Math.Min(Math.Min(Math.Min(Math.Min(
                    previous1LowPrice,
                    previous2LowPrice),
                    previous3LowPrice),
                    previous4LowPrice),
                    previous5LowPrice),
                    previous6LowPrice),
                    previous7LowPrice),
                    previous8LowPrice) - (attachedInstrumentTickSize * 2);
            else if (stopLossSnapType == JoiaGestorStopLossSnapTypes.SnapPBLevel)
                entryPrice = snapPowerBoxLowerValue - (attachedInstrumentTickSize * 2);
                return entryPrice;
        }
        private double CalculateSnapBarHighPrice(JoiaGestorStopLossSnapTypes stopLossSnapType)
        {
            double entryPrice = 0;
            if (stopLossSnapType == JoiaGestorStopLossSnapTypes.Snap1Bar)
                entryPrice = previous1HighPrice + (attachedInstrumentTickSize * 2);
            else if (stopLossSnapType == JoiaGestorStopLossSnapTypes.Snap2Bar)
                entryPrice = Math.Max(previous1HighPrice, previous2HighPrice) + (attachedInstrumentTickSize * 2);
            else if (stopLossSnapType == JoiaGestorStopLossSnapTypes.Snap3Bar)
                entryPrice = Math.Max(Math.Max(previous1HighPrice, previous2HighPrice), previous3HighPrice) + (attachedInstrumentTickSize * 2);
            else if (stopLossSnapType == JoiaGestorStopLossSnapTypes.Snap5Bar)
                entryPrice = Math.Max(Math.Max(Math.Max(Math.Max(previous1HighPrice, previous2HighPrice), previous3HighPrice), previous4HighPrice), previous5HighPrice) + (attachedInstrumentTickSize * 2);
            else if (stopLossSnapType == JoiaGestorStopLossSnapTypes.Snap8Bar)
                entryPrice = Math.Max(Math.Max(Math.Max(Math.Max(Math.Max(Math.Max(Math.Max(
                    previous1HighPrice,
                    previous2HighPrice),
                    previous3HighPrice),
                    previous4HighPrice),
                    previous5HighPrice),
                    previous6HighPrice),
                    previous7HighPrice),
                    previous8HighPrice) + (attachedInstrumentTickSize * 2);
            else if (stopLossSnapType == JoiaGestorStopLossSnapTypes.SnapPBLevel)
                entryPrice = snapPowerBoxUpperValue + (attachedInstrumentTickSize * 2);
            return entryPrice;
        }
        private double GetBreakEvenAutoMovingAverage1Price(MarketPosition positionType)
        {
            double maValue = 0;
            if (positionType == MarketPosition.Long)
                maValue = Math.Floor(autoCloseAndTrailMA1Value * attachedInstrumentTicksPerPoint) / attachedInstrumentTicksPerPoint;
            else
                maValue = Math.Ceiling(autoCloseAndTrailMA1Value * attachedInstrumentTicksPerPoint) / attachedInstrumentTicksPerPoint;
            return maValue;
        }
        private double GetBreakEvenAutoMovingAverage2Price(MarketPosition positionType)
        {
            double maValue = 0;
            if (positionType == MarketPosition.Long)
                maValue = Math.Floor(autoCloseAndTrailMA2Value * attachedInstrumentTicksPerPoint) / attachedInstrumentTicksPerPoint;
            else
                maValue = Math.Ceiling(autoCloseAndTrailMA2Value * attachedInstrumentTicksPerPoint) / attachedInstrumentTicksPerPoint;
            return maValue;
        }
        private double GetBreakEvenAutoMovingAverage3Price(MarketPosition positionType)
        {
            double maValue = 0;
            if (positionType == MarketPosition.Long)
                maValue = Math.Floor(autoCloseAndTrailMA3Value * attachedInstrumentTicksPerPoint) / attachedInstrumentTicksPerPoint;
            else
                maValue = Math.Ceiling(autoCloseAndTrailMA3Value * attachedInstrumentTicksPerPoint) / attachedInstrumentTicksPerPoint;
            return maValue;
        }
        private bool CheckSnapPositionTPSL()
        {
            bool returnFlag = true; 
            return returnFlag;
        }
        private int CalculateStopLossTicks(MarketPosition marketPosition, double averagePrice, double stopLossPrice, double tickSize)
        {
            int stopLossTicks = 0;
            if (averagePrice > 0 && stopLossPrice > 0)
            {
                bool isBuyPosition = (marketPosition == MarketPosition.Long);
                if (isBuyPosition)
                {
                    stopLossTicks = (int)Math.Floor((stopLossPrice - averagePrice) / tickSize);
                }
                else
                {
                    stopLossTicks = (int)Math.Ceiling((averagePrice - stopLossPrice) / tickSize);
                }
                if (stopLossTicks < 0)
                {
                    stopLossTicks *= -1;
                }
            }
            return stopLossTicks;
        }
        private bool GetPopDropOrderCount(out int buyCount, out int sellCount)
        {
            bool orderFound = false;
            buyCount = 0;
            sellCount = 0;
            int orderCount = RealOrderService.OrderCount;
            for (int index = 0; index < orderCount; index++)
            {
                RealOrder order = null;
                if (RealOrderService.TryGetByIndex(index, out order))
                {
                    if (order.Instrument == attachedInstrument && (RealOrderService.IsValidBuyStopOrder(order, attachedInstrument, OrderAction.BuyToCover) || RealOrderService.IsValidSellStopOrder(order, attachedInstrument, OrderAction.SellShort)
                        || RealOrderService.IsValidBuyLimitOrder(order, attachedInstrument, OrderAction.Buy) || RealOrderService.IsValidSellLimitOrder(order, attachedInstrument, OrderAction.Sell)))
                    {
                        orderFound = true;
                        if (!Order.IsTerminalState(order.OrderState))
                        {
                            if (RealOrderService.IsValidBuyStopOrder(order, attachedInstrument, OrderAction.BuyToCover) || RealOrderService.IsValidBuyLimitOrder(order, attachedInstrument, OrderAction.Buy))
                            {
                                orderFound = true;
                                buyCount++;
                            }
                            else if (RealOrderService.IsValidSellStopOrder(order, attachedInstrument, OrderAction.SellShort) || RealOrderService.IsValidSellLimitOrder(order, attachedInstrument, OrderAction.Sell))
                            {
                                orderFound = true;
                                sellCount++;
                            }
                        }
                    }
                }
            }
            return orderFound;
        }
        private void LoadNinjaTraderOrders()
        {
            if (!IsNinjaTraderOrdersAlreadyLoaded)
            {
                lock (account.Orders)
                {
                    if (!IsNinjaTraderOrdersAlreadyLoaded)
                    {
                        ninjaTraderOrders.Clear();
                        foreach (Order orderItem in account.Orders)
                        {
                            if (!Order.IsTerminalState(orderItem.OrderState))
                            {
                                ninjaTraderOrders.Add(orderItem.Id, orderItem);
                            }
                        }
                        IsNinjaTraderOrdersAlreadyLoaded = true;
                    }
                }
            }
        }
        private Order GetNinjaTraderOrder(RealOrder order)
        {
            LoadNinjaTraderOrders();
            Order foundOrder = null;
            ninjaTraderOrders.TryGetValue(order.OrderId, out foundOrder);
            return foundOrder;
        }
        private bool CancelPopDropOrders(string signalName)
        {
            bool orderFound = false;
            int orderCount = RealOrderService.OrderCount;
            for (int index = 0; index < orderCount; index++)
            {
                RealOrder order = null;
                if (RealOrderService.TryGetByIndex(index, out order))
                {
                    if (order.Instrument == attachedInstrument && (RealOrderService.IsValidBuyStopOrder(order, attachedInstrument, OrderAction.BuyToCover) || RealOrderService.IsValidSellStopOrder(order, attachedInstrument, OrderAction.SellShort)
                        || RealOrderService.IsValidBuyLimitOrder(order, attachedInstrument, OrderAction.Buy) || RealOrderService.IsValidSellLimitOrder(order, attachedInstrument, OrderAction.Sell)))
                    {
                        orderFound = true;
                        if (!Order.IsTerminalState(order.OrderState))
                        {
                            if (DebugLogLevel > 2) RealLogger.PrintOutput(signalName + " is cancelling pending order " + order.Instrument.FullName + " Type=" + order.OrderType.ToString());
                            Order foundNTOrder = GetNinjaTraderOrder(order);
                            if (foundNTOrder != null)
                            {
                                try
                                {
                                    account.Cancel(new[] { foundNTOrder });
                                }
                                catch (Exception ex)
                                {
                                    RealLogger.PrintOutput("Exception in " + signalName + ":" + ex.Message + " " + ex.StackTrace);  
                                }
                            }
                        }
                    }
                }
            }
            return orderFound;
        }
        private bool HandleBuySnap(string signalName)
        {
            double newBuySnapPrice = 0;
            bool positionFound = false;
            bool isLongPosition = false;
            bool orderFound = false;
            double oldStopLossPrice = 0;
            int stopLossOrderCount = 0;
            double takeProfitPrice = 0;
            OrderType orderType = OrderType.Unknown;
            int positionCount = RealPositionService.PositionCount;
            for (int index = 0; index < positionCount; index++)
            {
                RealPosition position = null;
                if (RealPositionService.TryGetByIndex(index, out position))
                {
                    if (RealPositionService.IsValidPosition(position, attachedInstrument) && position.IsValid)
                    {
                        isLongPosition = (position.MarketPosition == MarketPosition.Long);
                        positionFound = true;
                        int oldOrderQuantity = 0;
                        stopLossOrderCount = 0;
                        oldStopLossPrice = RealOrderService.GetStopLossInfo(position.Account, position.Instrument, ConvertMarketPositionToSLOrderAction(position.MarketPosition), out orderType, out oldOrderQuantity, out stopLossOrderCount);
                        int stopLossTicks = CalculateStopLossTicks(position.MarketPosition, position.AveragePrice, oldStopLossPrice, attachedInstrumentTickSize);
                        int stopLossMultipliedTicks = (int)(stopLossTicks * TakeProfitCtrlSLMultiplier);
                        takeProfitPrice = CalculateTakeProfitPrice(position.MarketPosition, position.AveragePrice, stopLossMultipliedTicks, attachedInstrumentTickSize, attachedInstrumentTicksPerPoint);
                        break;
                    }
                }
            }
            if (!isLongPosition && positionFound && CheckSnapPositionTPSL())
            {
                if (takeProfitPrice > 0)
                {
                    HandleTakeProfitPlus("BuySnap", takeProfitPrice);
                }
                positionFound = false;
            }
            else if (isLongPosition && positionFound && CheckSnapPositionTPSL())
            {
                TrailBuyPositionStopLoss("BuySnap", true);
            }
            else if (!positionFound || !CheckSnapPositionTPSL())
            {
                orderFound = CancelPopDropOrders("BuySnap");
                if (!orderFound)
                {
                    newBuySnapPrice = CalculateTrailHighPrice(MarketPosition.Long, true);
                    double askPrice = RealInstrumentService.GetAskPrice(attachedInstrument);
                    if (newBuySnapPrice <= askPrice)
                    {
                        newBuySnapPrice = 0;
                    }
                    if (newBuySnapPrice != 0)
                    {
                        if (DebugLogLevel > 2) RealLogger.PrintOutput("New Snap+ price=" + newBuySnapPrice.ToString());
                        int autoEntryVolume = CalculateAutoEntryVolume(currentEntryVolumeAutoStatus);
                        CreateBuyStop(signalName, attachedInstrument, OrderAction.BuyToCover, OrderEntry.Manual, autoEntryVolume, newBuySnapPrice);
                    }
                }
            }
            return positionFound;
        }
        private bool HandleBuyMarket(string signalName)
        {
            bool buyMarketSucceeded = false;
            if (HasRanOnceFirstCycle() && RealOrderService.AreAllOrderUpdateCyclesComplete())
            {
                const OrderAction orderAction = OrderAction.Buy;
                double askPrice = RealInstrumentService.GetAskPrice(attachedInstrument);
                int autoEntryincrementVolumeSize = CalculateAutoEntryVolume(currentEntryVolumeAutoStatus);
                int limitedIncrementVolumeSize = GetLimitedIncrementVolumeSize(autoEntryincrementVolumeSize, bogeyTargetBaseVolumeSize, LimitAddOnVolumeToInProfit);
                int buyPositionVolumeSize = 0;
                double buyPositionPrice = 0;
                int sellPositionVolumeSize = 0;
                double sellPositionPrice = 0;
                bool hasPosition = GetPositionVolume(attachedInstrument, out buyPositionVolumeSize, out buyPositionPrice, out sellPositionVolumeSize, out sellPositionPrice);
                bool hasBuyPosition = buyPositionVolumeSize > 0;
                double positionPrice = buyPositionPrice;
                bool isValidPositionMaxVolume = attachedInstrumentPositionMaxVolume > 0;
                bool isOverVolumePositionMax = limitedIncrementVolumeSize > attachedInstrumentPositionMaxVolume
                    || (limitedIncrementVolumeSize + buyPositionVolumeSize) > attachedInstrumentPositionMaxVolume;
                if ((!isOverVolumePositionMax && isValidPositionMaxVolume) || (isOverVolumePositionMax && !isValidPositionMaxVolume))
                {
                    bool passedMarginCheck = OrderMarginCheck(orderAction, attachedInstrument, limitedIncrementVolumeSize, buyPositionVolumeSize, sellPositionVolumeSize);
                    if (passedMarginCheck)
                    {
                        if (!LimitAddOnVolumeToInProfit || (hasPosition && !hasBuyPosition))
                        {
                            RealLogger.PrintOutput("Send buy order at price (" + askPrice.ToString() + ")");
                            ReducePositionTakeProfitVolume(signalName, sellPositionVolumeSize, autoEntryincrementVolumeSize);
                            SubmitMarketOrder(attachedInstrument, orderAction, OrderEntry.Manual, autoEntryincrementVolumeSize);
                            buyMarketSucceeded = true;
                        }
                        else
                        {
                            double bidPrice = RealInstrumentService.GetBidPrice(attachedInstrument);
                            bool positionInProfit = bidPrice >= positionPrice;
                            int newVolumeSize = DEFAULT_VOLUME_SIZE;
                            if (TryGetLimitAddOnVolumeSize(buyPositionVolumeSize, positionInProfit, autoEntryincrementVolumeSize, bogeyTargetBaseVolumeSize, out newVolumeSize))
                            {
                                RealLogger.PrintOutput("Send buy order at price (" + askPrice.ToString() + ")");
                                ReducePositionTakeProfitVolume(signalName, sellPositionVolumeSize, newVolumeSize);
                                SubmitMarketOrder(attachedInstrument, orderAction, OrderEntry.Manual, newVolumeSize);
                                buyMarketSucceeded = true;
                            }
                            else
                            {
                                RealLogger.PrintOutput("BLOCKED buy order due to LimitAddOnVolumeInProfit where position price at (" + positionPrice
                                    + ") must be less than market price at (" + askPrice + ")");
                            }
                        }
                    }
                    else
                    {
                        double intradayMargin = GetInstrumentIntradayMargin(orderAction, attachedInstrument);
                        RealLogger.PrintOutput("BLOCKED: Buy volume attempt greater than margin allowed.  limitedIncrementVolumeSize=" + limitedIncrementVolumeSize + " intradayMargin=" + intradayMargin);
                    }
                }
                else
                {
                    RealLogger.PrintOutput("BLOCKED: Buy volume attempt greater than instrument position max volume.  attachedInstrumentPositionMaxVolume="
                        + attachedInstrumentPositionMaxVolume + " limitedIncrementVolumeSize= " + limitedIncrementVolumeSize);
                }
            }
            return buyMarketSucceeded;
        }
        private bool HandleSellMarket(string signalName)
        {
            bool sellMarketSucceeded = false;
            if (HasRanOnceFirstCycle() && RealOrderService.AreAllOrderUpdateCyclesComplete())
            {
                const OrderAction orderAction = OrderAction.Sell;
                double bidPrice = RealInstrumentService.GetBidPrice(attachedInstrument);
                int autoEntryincrementVolumeSize = CalculateAutoEntryVolume(currentEntryVolumeAutoStatus);
                int limitedIncrementVolumeSize = GetLimitedIncrementVolumeSize(autoEntryincrementVolumeSize, bogeyTargetBaseVolumeSize, LimitAddOnVolumeToInProfit);
                int buyPositionVolumeSize = 0;
                double buyPositionPrice = 0;
                int sellPositionVolumeSize = 0;
                double sellPositionPrice = 0;
                bool hasPosition = GetPositionVolume(attachedInstrument, out buyPositionVolumeSize, out buyPositionPrice, out sellPositionVolumeSize, out sellPositionPrice);
                bool hasSellPosition = sellPositionVolumeSize > 0;
                double positionPrice = sellPositionPrice;
                bool isValidPositionMaxVolume = attachedInstrumentPositionMaxVolume > 0;
                bool isOverVolumePositionMax = limitedIncrementVolumeSize > attachedInstrumentPositionMaxVolume
                    || (limitedIncrementVolumeSize + sellPositionVolumeSize) > attachedInstrumentPositionMaxVolume;
                if ((!isOverVolumePositionMax && isValidPositionMaxVolume) || (isOverVolumePositionMax && !isValidPositionMaxVolume))
                {
                    bool passedMarginCheck = OrderMarginCheck(orderAction, attachedInstrument, limitedIncrementVolumeSize, buyPositionVolumeSize, sellPositionVolumeSize);
                    if (passedMarginCheck)
                    {
                        if (!LimitAddOnVolumeToInProfit || (hasPosition && !hasSellPosition))
                        {
                            RealLogger.PrintOutput("Send sell order at price (" + bidPrice.ToString() + ")");
                            ReducePositionTakeProfitVolume(signalName, buyPositionVolumeSize, autoEntryincrementVolumeSize);
                            SubmitMarketOrder(attachedInstrument, orderAction, OrderEntry.Manual, autoEntryincrementVolumeSize);
                            sellMarketSucceeded = true;
                        }
                        else
                        {
                            double askPrice = RealInstrumentService.GetAskPrice(attachedInstrument);
                            bool positionInProfit = askPrice <= positionPrice;
                            int newVolumeSize = DEFAULT_VOLUME_SIZE;
                            if (TryGetLimitAddOnVolumeSize(sellPositionVolumeSize, positionInProfit, autoEntryincrementVolumeSize, bogeyTargetBaseVolumeSize, out newVolumeSize))
                            {
                                RealLogger.PrintOutput("Send sell order at price (" + bidPrice.ToString() + ")");
                                ReducePositionTakeProfitVolume(signalName, buyPositionVolumeSize, newVolumeSize);
                                SubmitMarketOrder(attachedInstrument, orderAction, OrderEntry.Manual, newVolumeSize);
                                sellMarketSucceeded = true;
                            }
                            else
                            {
                                RealLogger.PrintOutput("BLOCKED sell order due to LimitAddOnVolumeInProfit where position price at (" + positionPrice
                                    + ") must be greater than market price at (" + bidPrice + ")");
                            }
                        }
                    }
                    else
                    {
                        double intradayMargin = GetInstrumentIntradayMargin(orderAction, attachedInstrument);
                        RealLogger.PrintOutput("BLOCKED: Sell volume attempt greater than margin allowed.  limitedIncrementVolumeSize=" + limitedIncrementVolumeSize + " intradayMargin=" + intradayMargin);
                    }
                }
                else
                {
                    RealLogger.PrintOutput("BLOCKED: Sell volume attempt greater than instrument position max volume.  attachedInstrumentPositionMaxVolume="
                        + attachedInstrumentPositionMaxVolume + " limitedIncrementVolumeSize= " + limitedIncrementVolumeSize);
                }
            }
            return sellMarketSucceeded;
        }
        private void ReducePositionTakeProfitVolume(string signalName, int positionVolumeSize, int reduceVolumeSize)
        {
            if (TakeProfitRefreshManagementEnabled)
            {
                bool hasTakeProfit = attachedInstrumentPositionTakeProfitPrice > 0;
                bool isValidPositionMaxVolume = attachedInstrumentPositionMaxVolume > 0;
                if (hasTakeProfit && isValidPositionMaxVolume)
                {
                    int potentialNewVolumeSize = (reduceVolumeSize + positionVolumeSize);
                    bool isOverTotalVolumePositionMax = potentialNewVolumeSize > attachedInstrumentPositionMaxVolume;
                    int volumeOverMax = potentialNewVolumeSize - attachedInstrumentPositionMaxVolume;
                    if (isOverTotalVolumePositionMax)
                    {
                        int newVolumeSize = attachedInstrumentPositionMaxVolume - reduceVolumeSize;
                        bool isValidNewVolumeSize = newVolumeSize > 0;
                        OrderAction takeProfitOrderAction = ConvertMarketPositionToTPOrderAction(attachedInstrumentMarketPosition);
                        double lastPrice = RealInstrumentService.GetLastPrice(attachedInstrument);
                        bool isPriceValid = RealOrderService.IsValidTakeProfitPrice(attachedInstrument, takeProfitOrderAction, attachedInstrumentPositionTakeProfitPrice, lastPrice);
                        if (isPriceValid && isValidNewVolumeSize)
                        {
                            UpdatePositionTakeProfit(signalName, attachedInstrument, takeProfitOrderAction, OrderEntry.Manual, newVolumeSize, attachedInstrumentPositionTakeProfitPrice);
                        }
                    }
                }
            }
        }
        private int CalculateATRTicks(double atrValue, double atrMultiplier, int ticksPerPoint)
        {
            int atrTicks = 0;
            if (atrMultiplier > 0)
            {
                atrTicks = (int)((atrValue * ticksPerPoint) * atrMultiplier);
            }
            return atrTicks;
        }
        private double GetInitialStopLossPrice(MarketPosition marketPosition, double averagePrice, int quantity, double currentStopLossPrice = 0)
        {
            double newStopLossPrice = currentStopLossPrice;
            bool useStopLossTicks = (this.StopLossInitialTicks > 0);
            bool useStopLossDollars = (this.StopLossInitialDollars > 0);
            int stopLossTicks = this.StopLossInitialTicks;
            bool allowATROverride = (this.StopLossInitialATRMultiplier > 0);
            if (useStopLossDollars)
            {
                double commissionPerSide = GetCommissionPerSide(attachedInstrument);
                bool includeCommissions = (commissionPerSide > 0);
                double netStopLossDollars = this.StopLossInitialDollars;
                double tickValue = RealInstrumentService.GetTickValue(attachedInstrument);
                double tickSize = attachedInstrumentTickSize;
                int ticksPerPoint = attachedInstrumentTicksPerPoint;
                if (StopLossInitialDollarsCombined && currentStopLossPrice == 0)
                {
                    if (allowATROverride)
                    {
                        int newATRStopLossTicks = CalculateATRTicks(atrValue, this.StopLossInitialATRMultiplier, attachedInstrumentTicksPerPoint);
                        stopLossTicks = Math.Max(newATRStopLossTicks, this.StopLossInitialTicks);
                    }
                    newStopLossPrice = CalculateStopLossPrice(marketPosition, averagePrice, stopLossTicks, attachedInstrumentTickSize, attachedInstrumentTicksPerPoint);
                    if (StopLossInitialSnapType != JoiaGestorStopLossSnapTypes.Disabled)
                    {
                        if (marketPosition == MarketPosition.Long)
                        {
                            double newSnapStopLossPrice = CalculateSnapBarLowPrice(StopLossInitialSnapType); 
                            newStopLossPrice = Math.Min(newStopLossPrice, newSnapStopLossPrice);
                        }
                        else if (marketPosition == MarketPosition.Short)
                        {
                            double newSnapStopLossPrice = CalculateSnapBarHighPrice(StopLossInitialSnapType); 
                            newStopLossPrice = Math.Max(newStopLossPrice, newSnapStopLossPrice);
                        }
                    }
                }
                if (includeCommissions)
                {
                    netStopLossDollars = netStopLossDollars - (quantity * commissionPerSide * 2);
                }
                if (marketPosition == MarketPosition.Long)
                {
                    double newStopLossDollarPrice = (Math.Floor(averagePrice * ticksPerPoint) / ticksPerPoint) - (Math.Ceiling((netStopLossDollars / ((tickValue * quantity) / tickSize)) * ticksPerPoint) / ticksPerPoint);
                    newStopLossPrice = (StopLossInitialDollarsCombined) ? Math.Max(newStopLossPrice, newStopLossDollarPrice) : newStopLossDollarPrice;
                }
                else if (marketPosition == MarketPosition.Short)
                {
                    double newStopLossDollarPrice = (Math.Ceiling(averagePrice * ticksPerPoint) / ticksPerPoint) + (Math.Ceiling((netStopLossDollars / ((tickValue * quantity) / tickSize)) * ticksPerPoint) / ticksPerPoint);
                    newStopLossPrice = (StopLossInitialDollarsCombined) ? Math.Min(newStopLossPrice, newStopLossDollarPrice) : newStopLossDollarPrice;
                }
            }
            else if (useStopLossTicks)
            {
                if (allowATROverride)
                {
                    int newATRStopLossTicks = CalculateATRTicks(atrValue, this.StopLossInitialATRMultiplier, attachedInstrumentTicksPerPoint);
                    stopLossTicks = Math.Max(newATRStopLossTicks, this.StopLossInitialTicks);
                }
                newStopLossPrice = CalculateStopLossPrice(marketPosition, averagePrice, stopLossTicks, attachedInstrumentTickSize, attachedInstrumentTicksPerPoint);
                if (StopLossInitialSnapType != JoiaGestorStopLossSnapTypes.Disabled)
                {
                    if (marketPosition == MarketPosition.Long)
                    {
                        double newSnapStopLossPrice = CalculateSnapBarLowPrice(StopLossInitialSnapType); 
                        newStopLossPrice = Math.Min(newStopLossPrice, newSnapStopLossPrice);
                    }
                    else if (marketPosition == MarketPosition.Short)
                    {
                        double newSnapStopLossPrice = CalculateSnapBarHighPrice(StopLossInitialSnapType); 
                        newStopLossPrice = Math.Max(newStopLossPrice, newSnapStopLossPrice);
                    }
                }
                if (StopLossInitialMaxTicks > 0)
                {
                    if (marketPosition == MarketPosition.Long)
                    {
                        double tempMaxStopLossPrice = CalculateStopLossPrice(marketPosition, averagePrice, StopLossInitialMaxTicks, attachedInstrumentTickSize, attachedInstrumentTicksPerPoint);
                        newStopLossPrice = Math.Max(newStopLossPrice, tempMaxStopLossPrice);
                    }
                    else if (marketPosition == MarketPosition.Short)
                    {
                        double tempMaxStopLossPrice = CalculateStopLossPrice(marketPosition, averagePrice, StopLossInitialMaxTicks, attachedInstrumentTickSize, attachedInstrumentTicksPerPoint);
                        newStopLossPrice = Math.Min(newStopLossPrice, tempMaxStopLossPrice);
                    }
                }
            }
            bool isStopLossPriceInvalid = newStopLossPrice <= 0;
            if (isStopLossPriceInvalid)
            {
                newStopLossPrice = 0 + attachedInstrumentTickSize;
            }
            double normalizedPrice = RealInstrumentService.NormalizePrice(attachedInstrument, newStopLossPrice);
            return normalizedPrice;
        }
        private double GetBogeyTargetFromDollars(MarketPosition marketPosition, double averagePrice, int quantity, double bogeyTargetDollars)
        {
            double newBogeyTargetPrice = 0;
            double commissionPerSide = GetCommissionPerSide(attachedInstrument);
            bool includeCommissions = (commissionPerSide > 0);
            double netBogeyTargetDollars = bogeyTargetDollars;
            double tickValue = RealInstrumentService.GetTickValue(attachedInstrument);
            double tickSize = attachedInstrumentTickSize;
            int ticksPerPoint = attachedInstrumentTicksPerPoint;
            if (includeCommissions)
            {
                netBogeyTargetDollars = netBogeyTargetDollars + (quantity * commissionPerSide);
            }
            if (marketPosition == MarketPosition.Long)
            {
                double newBogeyTargetDollarPrice = (Math.Ceiling(averagePrice * ticksPerPoint) / ticksPerPoint) + (Math.Ceiling((netBogeyTargetDollars / ((tickValue * quantity) / tickSize)) * ticksPerPoint) / ticksPerPoint);
                newBogeyTargetPrice = newBogeyTargetDollarPrice;
            }
            else if (marketPosition == MarketPosition.Short)
            {
                double newBogeyTargetDollarPrice = (Math.Floor(averagePrice * ticksPerPoint) / ticksPerPoint) - (Math.Ceiling((netBogeyTargetDollars / ((tickValue * quantity) / tickSize)) * ticksPerPoint) / ticksPerPoint);
                newBogeyTargetPrice = newBogeyTargetDollarPrice;
            }
            double normalizedPrice = RealInstrumentService.NormalizePrice(attachedInstrument, newBogeyTargetPrice);
            return normalizedPrice;
        }
        private double GetDayOverMaxLossFromDollars(MarketPosition marketPosition, double averagePrice, int quantity, double dayOverDollars)
        {
            double newStopLossPrice = 0;
            double commissionPerSide = GetCommissionPerSide(attachedInstrument);
            bool includeCommissions = (commissionPerSide > 0);
            double netStopLossDollars = dayOverDollars;
            double tickValue = RealInstrumentService.GetTickValue(attachedInstrument);
            double tickSize = attachedInstrumentTickSize;
            int ticksPerPoint = attachedInstrumentTicksPerPoint;
            if (includeCommissions)
            {
                netStopLossDollars = netStopLossDollars - (quantity * commissionPerSide);
            }
            if (marketPosition == MarketPosition.Long)
            {
                double newStopLossDollarPrice = (Math.Floor(averagePrice * ticksPerPoint) / ticksPerPoint) - (Math.Ceiling((netStopLossDollars / ((tickValue * quantity) / tickSize)) * ticksPerPoint) / ticksPerPoint);
                newStopLossPrice = newStopLossDollarPrice;
            }
            else if (marketPosition == MarketPosition.Short)
            {
                double newStopLossDollarPrice = (Math.Ceiling(averagePrice * ticksPerPoint) / ticksPerPoint) + (Math.Ceiling((netStopLossDollars / ((tickValue * quantity) / tickSize)) * ticksPerPoint) / ticksPerPoint);
                newStopLossPrice = newStopLossDollarPrice;
            }
            double normalizedPrice = RealInstrumentService.NormalizePrice(attachedInstrument, newStopLossPrice);
            return normalizedPrice;
        }
        private double GetECATakeProfitDollars(int totalVolume, int totalOtherVolume,
            int totalMYMVolume, int totalMESVolume, int totalM2KVolume, int totalMNQVolume,
            int totalYMVolume, int totalESVolume, int totalRTYVolume, int totalNQVolume)
        {
            double newECATakeProfitDollars = 0;
            bool hasTakeProfitDollars = ECATargetDollars > 0;
            if (hasTakeProfitDollars)
            {
                newECATakeProfitDollars = ECATargetDollars;
            }
            else
            {
                bool hasProfitATRMultiplierPerVolume = ECATargetATRMultiplierPerVolume > 0;
                double perVolumeProfitDollars = 0;
                if (totalMYMVolume > 0)
                    perVolumeProfitDollars += (ECATargetDollarsPerMYMVolume * totalMYMVolume);
                if (totalMESVolume > 0)
                    perVolumeProfitDollars += (ECATargetDollarsPerMESVolume * totalMESVolume);
                if (totalM2KVolume > 0)
                    perVolumeProfitDollars += (ECATargetDollarsPerM2KVolume * totalM2KVolume);
                if (totalMNQVolume > 0)
                    perVolumeProfitDollars += (ECATargetDollarsPerMNQVolume * totalMNQVolume);
                if (totalYMVolume > 0)
                    perVolumeProfitDollars += (ECATargetDollarsPerYMVolume * totalYMVolume);
                if (totalESVolume > 0)
                    perVolumeProfitDollars += (ECATargetDollarsPerESVolume * totalESVolume);
                if (totalRTYVolume > 0)
                    perVolumeProfitDollars += (ECATargetDollarsPerRTYVolume * totalRTYVolume);
                if (totalNQVolume > 0)
                    perVolumeProfitDollars += (ECATargetDollarsPerNQVolume * totalNQVolume);
                if (totalOtherVolume > 0)
                    perVolumeProfitDollars += (ECATargetDollarsPerOtherVolume * totalOtherVolume);
                if (hasProfitATRMultiplierPerVolume)
                {
                    int atrTicks = CalculateATRTicks(atrValue, ECATargetATRMultiplierPerVolume, attachedInstrumentTicksPerPoint);
                    double atrExpectedProfit = 0;
                    atrExpectedProfit += RealInstrumentService.ConvertTicksToDollars(attachedInstrument, atrTicks, totalVolume);
                    if (atrExpectedProfit > perVolumeProfitDollars)
                    {
                        perVolumeProfitDollars = atrExpectedProfit;
                    }
                }
                newECATakeProfitDollars = perVolumeProfitDollars;
            }
            return newECATakeProfitDollars;
        }
        private double GetECATakeProfitPriceFromDollars(MarketPosition marketPosition, double averagePrice, int quantity, double takeProfitDollars)
        {
            double newECATakeProfitPrice = 0;
            double commissionPerSide = GetCommissionPerSide(attachedInstrument);
            bool includeCommissions = (commissionPerSide > 0);
            double netECATakeProfitDollars = takeProfitDollars;
            double tickValue = RealInstrumentService.GetTickValue(attachedInstrument);
            double tickSize = attachedInstrumentTickSize;
            int ticksPerPoint = attachedInstrumentTicksPerPoint;
            if (includeCommissions)
            {
                if (marketPosition == MarketPosition.Long)
                    netECATakeProfitDollars = netECATakeProfitDollars + (quantity * commissionPerSide * 2);
                else if (marketPosition == MarketPosition.Short)
                    netECATakeProfitDollars = netECATakeProfitDollars + (quantity * commissionPerSide * 2);
            }
            if (marketPosition == MarketPosition.Long)
            {
                double newECATakeProfitDollarPrice = (Math.Ceiling(averagePrice * ticksPerPoint) / ticksPerPoint) + (Math.Ceiling((netECATakeProfitDollars / ((tickValue * quantity) / tickSize)) * ticksPerPoint) / ticksPerPoint);
                newECATakeProfitPrice = newECATakeProfitDollarPrice;
            }
            else if (marketPosition == MarketPosition.Short)
            {
                double newECATakeProfitDollarPrice = (Math.Floor(averagePrice * ticksPerPoint) / ticksPerPoint) - (Math.Ceiling((netECATakeProfitDollars / ((tickValue * quantity) / tickSize)) * ticksPerPoint) / ticksPerPoint);
                newECATakeProfitPrice = newECATakeProfitDollarPrice;
            }
            double normalizedPrice = RealInstrumentService.NormalizePrice(attachedInstrument, newECATakeProfitPrice);
            return normalizedPrice;
        }
        private double GetStopLossPriceFromJumpTicks(MarketPosition marketPosition, double oldStopLossPrice, int jumpTicks)
        {
            double newStopLossPrice = 0;
            newStopLossPrice = CalculateStopLossPlusPrice(marketPosition, oldStopLossPrice, jumpTicks, attachedInstrumentTickSize, attachedInstrumentTicksPerPoint);
            double normalizedPrice = RealInstrumentService.NormalizePrice(attachedInstrument, newStopLossPrice);
            return normalizedPrice;
        }
        private double CalculateStopLossPrice(MarketPosition marketPosition, double price, int ticks, double tickSize, int ticksPerPoint)
        {
            double newStopLossPrice = 0;
            if (marketPosition == MarketPosition.Long)
            {
                newStopLossPrice = (Math.Floor(price * ticksPerPoint) / ticksPerPoint) - ((double)ticks * tickSize);
            }
            else if (marketPosition == MarketPosition.Short)
            {
                newStopLossPrice = (Math.Ceiling(price * ticksPerPoint) / ticksPerPoint) + ((double)ticks * tickSize);
            }
            double normalizedPrice = RealInstrumentService.NormalizePrice(attachedInstrument, newStopLossPrice);
            return normalizedPrice;
        }
        private double CalculateStopLossPlusPrice(MarketPosition marketPosition, double price, int ticks, double tickSize, int ticksPerPoint)
        {
            double newStopLossPrice = 0;
            if (marketPosition == MarketPosition.Long)
            {
                newStopLossPrice = (Math.Ceiling(price * ticksPerPoint) / ticksPerPoint) + ((double)ticks * tickSize);
            }
            else if (marketPosition == MarketPosition.Short)
            {
                newStopLossPrice = (Math.Floor(price * ticksPerPoint) / ticksPerPoint) - ((double)ticks * tickSize);
            }
            double normalizedPrice = RealInstrumentService.NormalizePrice(attachedInstrument, newStopLossPrice);
            return normalizedPrice;
        }
        private double GetInitialBreakEvenStopLossPrice(MarketPosition marketPosition, double averagePrice)
        {
            double newStopLossPrice = 0;
            if (marketPosition == MarketPosition.Long)
            {
                newStopLossPrice = (Math.Ceiling(averagePrice * attachedInstrumentTicksPerPoint) / attachedInstrumentTicksPerPoint) + ((double)this.BreakEvenInitialTicks * attachedInstrumentTickSize);
            }
            else if (marketPosition == MarketPosition.Short)
            {
                newStopLossPrice = (Math.Floor(averagePrice * attachedInstrumentTicksPerPoint) / attachedInstrumentTicksPerPoint) - ((double)this.BreakEvenInitialTicks * attachedInstrumentTickSize);
            }
            double normalizedPrice = RealInstrumentService.NormalizePrice(attachedInstrument, newStopLossPrice);
            return normalizedPrice;
        }
        private double GetTriggerBreakEvenStopLossPrice(MarketPosition marketPosition, double averagePrice)
        {
            double newStopLossPrice = 0;
            int breakEventriggerTicks = this.BreakEvenAutoTriggerTicks;
            if (this.BreakEvenAutoTriggerATRMultiplier > 0)
            {
                int newATRTriggerTicks = CalculateATRTicks(atrValue, this.BreakEvenAutoTriggerATRMultiplier, attachedInstrumentTicksPerPoint);
                breakEventriggerTicks = Math.Max(newATRTriggerTicks, this.BreakEvenAutoTriggerTicks);
            }
            if (marketPosition == MarketPosition.Long)
            {
                newStopLossPrice = (Math.Ceiling(averagePrice * attachedInstrumentTicksPerPoint) / attachedInstrumentTicksPerPoint) + ((double)breakEventriggerTicks * attachedInstrumentTickSize);
            }
            else if (marketPosition == MarketPosition.Short)
            {
                newStopLossPrice = (Math.Floor(averagePrice * attachedInstrumentTicksPerPoint) / attachedInstrumentTicksPerPoint) - ((double)breakEventriggerTicks * attachedInstrumentTickSize);
            }
            double normalizedPrice = RealInstrumentService.NormalizePrice(attachedInstrument, newStopLossPrice);
            return normalizedPrice;
        }
        private double GetInitialTakeProfitPrice(MarketPosition marketPosition, double averagePrice)
        {
            double newTakeProfitPrice = 0;
            int takeProfitTicks = this.TakeProfitInitialTicks;
            if (this.TakeProfitInitialATRMultiplier > 0)
            {
                int newATRTakeProfitTicks = CalculateATRTicks(atrValue, this.TakeProfitInitialATRMultiplier, attachedInstrumentTicksPerPoint);
                takeProfitTicks = Math.Max(newATRTakeProfitTicks, this.TakeProfitInitialTicks);
            }
            newTakeProfitPrice = CalculateTakeProfitPrice(marketPosition, averagePrice, takeProfitTicks, attachedInstrumentTickSize, attachedInstrumentTicksPerPoint);
            bool isTakeProfitPriceInvalid = newTakeProfitPrice <= 0;
            if (isTakeProfitPriceInvalid)
            {
                newTakeProfitPrice = 0 + attachedInstrumentTickSize;
            }
            double normalizedPrice = RealInstrumentService.NormalizePrice(attachedInstrument, newTakeProfitPrice);
            return normalizedPrice;
        }
        private double GetTakeProfitPriceFromJumpTicks(MarketPosition marketPosition, double oldTakeProfitPrice, int jumpTicks)
        {
            double newTakeProfitPrice = 0;
            newTakeProfitPrice = CalculateTakeProfitPrice(marketPosition, oldTakeProfitPrice, jumpTicks, attachedInstrumentTickSize, attachedInstrumentTicksPerPoint);
            return newTakeProfitPrice;
        }
        double CalculateTakeProfitPrice(MarketPosition marketPosition, double price, int ticks, double tickSize, int ticksPerPoint)
        {
            double newTakeProfitPrice = 0;
            if (price > 0 && ticks > 0)
            {
                bool isBuyPosition = (marketPosition == MarketPosition.Long);
                if (marketPosition == MarketPosition.Long)
                {
                    newTakeProfitPrice = (Math.Ceiling(price * ticksPerPoint) / ticksPerPoint) + ((double)ticks * tickSize);
                }
                else if (marketPosition == MarketPosition.Short)
                {
                    newTakeProfitPrice = (Math.Floor(price * ticksPerPoint) / ticksPerPoint) - ((double)ticks * tickSize);
                }
            }
            return newTakeProfitPrice;
        }
        private MarketPosition ConvertOrderActionToMarketPosition(OrderAction orderAction)
        {
            MarketPosition marketPosition = MarketPosition.Flat;
            if (orderAction == OrderAction.Buy || orderAction == OrderAction.BuyToCover)
            {
                marketPosition = MarketPosition.Long;
            }
            else if (orderAction == OrderAction.Sell || orderAction == OrderAction.SellShort)
            {
                marketPosition = MarketPosition.Short;
            }
            else
            {
                RealLogger.PrintOutput("Order action type  " + orderAction.ToString() + " not supported.");
            }
            return marketPosition;
        }
        private OrderAction ConvertMarketPositionToRevOrderAction(MarketPosition marketPosition)
        {
            OrderAction orderAction = OrderAction.BuyToCover;
            if (marketPosition == MarketPosition.Long)
            {
                orderAction = OrderAction.Sell;
            }
            else if (marketPosition == MarketPosition.Short)
            {
                orderAction = OrderAction.BuyToCover;
            }
            else
            {
                RealLogger.PrintOutput("Market position type  " + marketPosition.ToString() + " not supported.");
            }
            return orderAction;
        }
        private OrderAction ConvertMarketPositionToSLOrderAction(MarketPosition marketPosition)
        {
            OrderAction orderAction = OrderAction.BuyToCover;
            if (marketPosition == MarketPosition.Long)
            {
                orderAction = OrderAction.Sell;
            }
            else if (marketPosition == MarketPosition.Short)
            {
                orderAction = OrderAction.BuyToCover;
            }
            else
            {
                RealLogger.PrintOutput("Market position type  " + marketPosition.ToString() + " not supported.");
            }
            return orderAction;
        }
        private OrderAction ConvertMarketPositionToTPOrderAction(MarketPosition marketPosition)
        {
            OrderAction orderAction = OrderAction.BuyToCover;
            if (marketPosition == MarketPosition.Long)
            {
                orderAction = OrderAction.Sell;
            }
            else if (marketPosition == MarketPosition.Short)
            {
                orderAction = OrderAction.BuyToCover;
            }
            else
            {
                RealLogger.PrintOutput("Market position type  " + marketPosition.ToString() + " not supported.");
            }
            return orderAction;
        }
        private int GetMaxPositionSize(Instrument instrument)
        {
            int maxPositionSize = 0;
            if (account != null && account.Risk != null && account.Risk.ByMasterInstrument.ContainsKey(instrument.MasterInstrument))
            {
                maxPositionSize = account.Risk.ByMasterInstrument[instrument.MasterInstrument].MaxPositionSize;
            }
            else
            {
                RealLogger.PrintOutput("ERROR: Missing max position size for instrument '" + instrument.FullName + "'");
            }
            return maxPositionSize;
        }
        private double GetAccountIntradayExcessMargin()
        {
            double intradayExcessMargin = 0;
            if (account != null)
            {
                intradayExcessMargin = Math.Round(account.Get(AccountItem.ExcessIntradayMargin, Currency.UsDollar), 2);
            }
            return intradayExcessMargin;
        }
        private double GetInstrumentIntradayMargin(OrderAction orderAction, Instrument instrument)
        {
            double intradayMargin = 0;
            if (account != null && account.Risk != null && account.Risk.ByMasterInstrument.ContainsKey(instrument.MasterInstrument))
            {
                if (orderAction == OrderAction.Buy)
                {
                    intradayMargin = account.Risk.ByMasterInstrument[instrument.MasterInstrument].BuyIntradayMargin;
                }
                else if (orderAction == OrderAction.Sell)
                {
                    intradayMargin = account.Risk.ByMasterInstrument[instrument.MasterInstrument].SellIntradayMargin;
                }
                else
                {
                    RealLogger.PrintOutput("GetInstrumentIntradayMargin: Order action type  " + orderAction.ToString() + " not supported.");
                }
            }
            else
            {
                RealLogger.PrintOutput("ERROR: Missing intraday margin for instrument '" + instrument.FullName + "'");
            }
            return intradayMargin;
        }
        private bool OrderMarginCheck(OrderAction orderAction, Instrument instrument, int volume, int buyPositionVolume, int sellPositionVolume)
        {
            bool marginCheckSucceeded = false;
            if (HasRanOnceFirstCycle())
            {
                if (UseIntradayMarginCheck)
                {
                    marginCheckSucceeded = true;
                    return marginCheckSucceeded;
                }
                double intradayMargin = 0;
                bool validOrderType = false;
                if (orderAction == OrderAction.Buy)
                {
                    intradayMargin = GetInstrumentIntradayMargin(orderAction, instrument);
                    validOrderType = true;
                }
                else if (orderAction == OrderAction.Sell)
                {
                    intradayMargin = GetInstrumentIntradayMargin(orderAction, instrument);
                    validOrderType = true;
                }
                else
                {
                    RealLogger.PrintOutput("OrderMarginCheck: Order action type  " + orderAction.ToString() + " not supported.");
                }
                if (validOrderType)
                {
                    bool hasValidRiskTemplate = intradayMargin > 0;
                    if (hasValidRiskTemplate)
                    {
                        double excessInitialMargin = account.Get(AccountItem.ExcessIntradayMargin, Currency.UsDollar);
                        double totalRequiredIntradayMargin = volume * intradayMargin;
                        double remainingExcessIntradayMargin = 0;
                        bool addingToExistingPosition = (orderAction == OrderAction.Buy && buyPositionVolume > 0)
                            || (orderAction == OrderAction.Sell && sellPositionVolume > 0);
                        bool adddingToNoPosition = (buyPositionVolume == 0 && sellPositionVolume == 0);
                        bool reducingAnExistingPosition = (orderAction == OrderAction.Buy && sellPositionVolume > 0) ||
                            (orderAction == OrderAction.Sell && buyPositionVolume > 0);
                        if (addingToExistingPosition || adddingToNoPosition)
                        {
                            remainingExcessIntradayMargin = excessInitialMargin - totalRequiredIntradayMargin;
                        }
                        else if (reducingAnExistingPosition)
                        {
                            remainingExcessIntradayMargin = excessInitialMargin + totalRequiredIntradayMargin;
                        }
                        marginCheckSucceeded = remainingExcessIntradayMargin > MIN_EXCESS_MARGIN;
                    }
                    else
                    {
                        RealLogger.PrintOutput("Missing valid risk template attached to account with IntradayMargin.");
                        marginCheckSucceeded = true;
                    }
                }
            }
            return marginCheckSucceeded;
        }
        private bool GetPositionVolume(Instrument instrument, out int buyVolume, out int sellVolume)
        {
            bool positionFound = false;
            buyVolume = 0;
            sellVolume = 0;
            int positionCount = RealPositionService.PositionCount;
            for (int index = 0; index < positionCount; index++)
            {
                RealPosition position = null;
                if (RealPositionService.TryGetByIndex(index, out position))
                {
                    if (position.Instrument == instrument)
                    {
                        positionFound = true;
                        if (position.MarketPosition == MarketPosition.Long)
                        {
                            buyVolume += position.Quantity;
                        }
                        else if (position.MarketPosition == MarketPosition.Short)
                        {
                            sellVolume += position.Quantity;
                        }
                    }
                }
            }
            return positionFound;
        }
        private bool GetPositionVolume(Instrument instrument, out int buyVolume, out double buyPositionPrice, out int sellVolume, out double sellPositionPrice)
        {
            bool positionFound = false;
            buyVolume = 0;
            sellVolume = 0;
            buyPositionPrice = 0;
            sellPositionPrice = 0;
            int positionCount = RealPositionService.PositionCount;
            for (int index = 0; index < positionCount; index++)
            {
                RealPosition position = null;
                if (RealPositionService.TryGetByIndex(index, out position))
                {
                    if (position.Instrument == instrument)
                    {
                        positionFound = true;
                        if (position.MarketPosition == MarketPosition.Long)
                        {
                            buyPositionPrice = position.AveragePrice;
                            buyVolume += position.Quantity;
                        }
                        else if (position.MarketPosition == MarketPosition.Short)
                        {
                            sellPositionPrice = position.AveragePrice;
                            sellVolume += position.Quantity;
                        }
                    }
                }
            }
            return positionFound;
        }
        private void AttemptToEngageAutobot()
        {
            if (1 == 2)
            {
                if (UseHedgehogEntry)
                {
                    lock (NewPositionLock)
                    {
                        int positionCount = RealPositionService.PositionCount;
                        int autoEntryVolume = CalculateAutoEntryVolume(currentEntryVolumeAutoStatus);
                        for (int index = 0; index < positionCount; index++)
                        {
                            RealPosition position = null;
                            if (RealPositionService.TryGetByIndex(index, out position))
                            {
                                if (IsAccountFlat()
                                    && RealOrderService.AreAllOrderUpdateCyclesComplete())
                                {
                                    if (HedgehogEntryBuySymbol1SellSymbol2)
                                    {
                                        if (DebugLogLevel > 2) RealLogger.PrintOutput("Hedgehog Entry buy (" + HedgehogEntrySymbol1FullName + ") sell (" + HedgehogEntrySymbol2FullName + ")", PrintTo.OutputTab1);
                                        Instrument buyInstrument = Instrument.GetInstrument(HedgehogEntrySymbol1FullName);
                                        SubmitMarketOrder(buyInstrument, OrderAction.Buy, OrderEntry.Automated, autoEntryVolume);
                                        Instrument sellInstrument = Instrument.GetInstrument(HedgehogEntrySymbol2FullName);
                                        SubmitMarketOrder(sellInstrument, OrderAction.Sell, OrderEntry.Automated, autoEntryVolume);
                                    }
                                    else
                                    {
                                        if (DebugLogLevel > 2) RealLogger.PrintOutput("Hedgehog Entry buy (" + HedgehogEntrySymbol2FullName + ") sell (" + HedgehogEntrySymbol1FullName + ")", PrintTo.OutputTab1);
                                        Instrument buyInstrument = Instrument.GetInstrument(HedgehogEntrySymbol2FullName);
                                        SubmitMarketOrder(buyInstrument, OrderAction.Buy, OrderEntry.Automated, autoEntryVolume);
                                        Instrument sellInstrument = Instrument.GetInstrument(HedgehogEntrySymbol1FullName);
                                        SubmitMarketOrder(sellInstrument, OrderAction.Sell, OrderEntry.Automated, autoEntryVolume);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        private void AttemptAccountInfoLogging()
        {
            if (UseAccountInfoLogging)
            {
                double accountBalance = Math.Round(account.Get(AccountItem.CashValue, Currency.UsDollar), 2);
                double grossRealizedPnL = Math.Round(account.Get(AccountItem.GrossRealizedProfitLoss, Currency.UsDollar), 2);
                double realizedPnL = Math.Round(account.Get(AccountItem.RealizedProfitLoss, Currency.UsDollar), 2);
                double accountBalanceWithNewPnL = accountBalance;
                if (grossRealizedPnL != 0) accountBalanceWithNewPnL = accountBalance - grossRealizedPnL + realizedPnL;
                if (accountBalanceWithNewPnL != lastAccountBalance)
                {
                    RealLogger.PrintOutput("Logging account information - $" + accountBalanceWithNewPnL.ToString("N2"), PrintTo.OutputTab2);
                    string content = "ACCOUNT_BALANCE,ACCOUNT_EQUITY\r\n" + Convert.ToString(accountBalanceWithNewPnL) + "," + Convert.ToString(accountBalanceWithNewPnL);
                    File.WriteAllText(AccountInfoLoggingPath, content);
                    lastAccountBalance = accountBalanceWithNewPnL;
                }
            }
        }
        /*
        private void LoadDayOverMaxLossHighestPnLInSessionData()
        {
            return;
            string fileNameAndPath = System.IO.Path.GetTempPath() + JoiaGestorSessionStateFileName;
            string formattedDateTime = GetDateTimeNow().ToString("d");
            DateTime currentDate = Convert.ToDateTime(formattedDateTime);
            if (File.Exists(fileNameAndPath))
            {
                using (StreamReader reader = new StreamReader(fileNameAndPath))
                {
                    string currentLine;
                    while ((currentLine = reader.ReadLine()) != null)
                    {
                        if (!(currentLine.IndexOf("DATE,ACCOUNT_ID,ACCOUNT_PNL", StringComparison.CurrentCultureIgnoreCase) >= 0))
                        {
                            RealLogger.PrintOutput("currentLine=" + currentLine);
                            string[] fields = currentLine.Split(',');
                            int fieldAccountId = Convert.ToInt32(fields[1]);
                            if (account.Id == fieldAccountId)
                            {
                                DateTime fieldDate = Convert.ToDateTime(fields[0]);
                                double fieldPnL = Convert.ToDouble(fields[2]);
                                RealLogger.PrintOutput("fieldAccountId=" + fieldAccountId + " date=" + fieldDate + " fieldPnL=" + fieldPnL);
                                bool needsToBeReset = fieldDate < currentDate;
                                if (needsToBeReset)
                                {
                                    lastDayOverMaxLossHighestPnLInSessionChangeDate = currentDate;
                                    lastDayOverMaxLossHighestPnLInSessionPnL = 0;
                                }
                                else
                                {
                                    lastDayOverMaxLossHighestPnLInSessionChangeDate = fieldDate;
                                    lastDayOverMaxLossHighestPnLInSessionPnL = fieldPnL;
                                }
                                RealLogger.PrintOutput("lastDayOverMaxLossHighestPnLInSessionChangeDate=" + lastDayOverMaxLossHighestPnLInSessionChangeDate + " lastDayOverMaxLossHighestPnLInSessionPnL=" + lastDayOverMaxLossHighestPnLInSessionPnL);
                            }
                        }
                    }
                }
            }
            else
            {
                lastDayOverMaxLossHighestPnLInSessionChangeDate = currentDate;
                lastDayOverMaxLossHighestPnLInSessionPnL = 0;
            }
        }
        */
        /*
         * private void StoreStateDayOverMaxLossHighestPnLInSession()
        {
            return; 
            try
            {
                string fileNameAndPath = System.IO.Path.GetTempPath() + JoiaGestorSessionStateFileName;
                string formattedDateTime = GetDateTimeNow().ToString("d");
                double realizedPnL;
                if (DebugLogLevel > 15) RealLogger.PrintOutput("Storing State in (" + fileNameAndPath + ")");
                StringBuilder content = new StringBuilder();
                content.Append("DATE,ACCOUNT_ID,ACCOUNT_PNL\r\n");
                foreach (Account accountItem in Account.All)
                {
                    realizedPnL = Math.Round(accountItem.Get(AccountItem.RealizedProfitLoss, Currency.UsDollar), 2);
                    content.AppendFormat("{0},{1},{2}\r\n", formattedDateTime, accountItem.Id, realizedPnL);
                }
                File.WriteAllText(fileNameAndPath, content.ToString());
            }
            catch (Exception ex)
            {
                RealLogger.PrintOutput("Exception calling StoreStateDayOverMaxLossHighestPnLInSession:" + ex.Message + " " + ex.StackTrace);
            }
        }
        */
        private double GetPositionProfitWithStoLoss(Instrument instrument, MarketPosition marketPosition, int quantity, double averagePrice, double stopLossPrice)
        {
            double positionProfit = 0;
            double tickValue = RealInstrumentService.GetTickValue(instrument);
            double tickSize = instrument.MasterInstrument.TickSize;
            if (marketPosition == MarketPosition.Long)
            {
                positionProfit = (stopLossPrice - averagePrice) * ((tickValue * quantity) / tickSize);
            }
            else if (marketPosition == MarketPosition.Short)
            {
                positionProfit = (averagePrice - stopLossPrice) * ((tickValue * quantity) / tickSize);
            }
            double commissionPerSide = GetCommissionPerSide(instrument);
            bool includeCommissions = (commissionPerSide > 0);
            if (includeCommissions)
            {
                positionProfit = positionProfit - (quantity * commissionPerSide * 2);
            }
            return (Math.Round(positionProfit, 2, MidpointRounding.ToEven));
        }
        private double GetPositionProfit(RealPosition position)
        {
            double positionProfit = 0;
            double totalVolume = position.Quantity;
            double averagePrice = position.AveragePrice;
            double tickValue = RealInstrumentService.GetTickValue(position.Instrument);
            double tickSize = position.Instrument.MasterInstrument.TickSize;
            if (position.MarketPosition == MarketPosition.Long)
            {
                double bid = RealInstrumentService.GetBidPrice(position.Instrument);
                positionProfit = (bid - averagePrice) * ((tickValue * totalVolume) / tickSize);
            }
            else if (position.MarketPosition == MarketPosition.Short)
            {
                double ask = RealInstrumentService.GetAskPrice(position.Instrument);
                positionProfit = (averagePrice - ask) * ((tickValue * totalVolume) / tickSize);
            }
            double commissionPerSide = GetCommissionPerSide(position.Instrument);
            bool includeCommissions = (commissionPerSide > 0);
            if (includeCommissions)
            {
                positionProfit = positionProfit - (totalVolume * commissionPerSide * 2);
            }
            return (Math.Round(positionProfit, 2, MidpointRounding.ToEven));
        }
        private bool IsMicroInstrument(Instrument instrument)
        {
            bool returnFlag = false;
            if (instrument.FullName.StartsWith(MYMPrefix) || instrument.FullName.StartsWith(MESPrefix) || instrument.FullName.StartsWith(M2KPrefix) || instrument.FullName.StartsWith(MNQPrefix))
            {
                returnFlag = true;
            }
            return returnFlag;
        }
        private bool IsEminiInstrument(Instrument instrument)
        {
            bool returnFlag = false;
            if (instrument.FullName.StartsWith(YMPrefix) || instrument.FullName.StartsWith(ESPrefix) || instrument.FullName.StartsWith(RTYPrefix) || instrument.FullName.StartsWith(NQPrefix))
            {
                returnFlag = true;
            }
            return returnFlag;
        }
        private double GetCommissionPerSide(Instrument instrument)
        {
            double commissionPerSide = 0;
            if (account != null && account.Commission != null && account.Commission.ByMasterInstrument.ContainsKey(instrument.MasterInstrument))
            {
                commissionPerSide = account.Commission.ByMasterInstrument[instrument.MasterInstrument].PerUnit;
            }
            else
            {
            }
            /*
            if (IsMicroInstrument(instrument))
            {
                commissionPerSide = MicroCommissionPerSide;
            }
            else if (IsEminiInstrument(instrument))
            {
                commissionPerSide = EminiCommissionPerSide;
            }
            else
            {
                RealLogger.PrintOutput("ERROR: Missing commission per side for instrument '" + instrument.FullName + "'");
            }
            */
            return commissionPerSide;
        }
        private void ChartControl_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                if (IsCtrlKeyDown())
                {
                    DumpDebugInternalsToOutput();
                }
            }
        }
        private void DumpDebugInternalsToOutput()
        {
            try
            {
                if (hasRanOnceFirstCycle)
                {
                    RealLogger.PrintOutput("***Activated DumpDebugInternalsToOutput...");
                    int positionCount = RealPositionService.PositionCount;
                    RealLogger.PrintOutput("Dump Positions: positionCount=" + positionCount);
                    for (int index = 0; index < positionCount; index++)
                    {
                        RealPosition position = null;
                        if (RealPositionService.TryGetByIndex(index, out position))
                        {
                            RealLogger.PrintOutput("- Instrument=" + position.Instrument.FullName + " Type=" + position.MarketPosition.ToString() + " Quan=" + position.Quantity);
                        }
                    }
                    int orderCount = RealOrderService.OrderCount;
                    RealLogger.PrintOutput("Dump Orders: orderCount=" + orderCount);
                    for (int index = 0; index < orderCount; index++)
                    {
                        RealOrder order = null;
                        if (RealOrderService.TryGetByIndex(index, out order))
                        {
                            RealLogger.PrintOutput("- Instrument=" + order.Instrument.FullName + " Type=" + order.OrderType.ToString() + " Quan=" + order.Quantity + " State=" + order.OrderState.ToString() + " OrderId=" + order.OrderId);
                        }
                    }
                    int orderUpdateMultiCycleCacheCount = RealOrderService.OrderUpdateMultiCycleCache.Count;
                    RealLogger.PrintOutput("Dump Order Update Multi-Cycle Orders: orderUpdateMultiCycleCacheCount=" + orderUpdateMultiCycleCacheCount);
                    int orderPartialFillCacheCount = RealOrderService.OrderPartialFillCache.Count;
                    RealLogger.PrintOutput("Dump OrderPartialFillCache: orderPartialFillCacheCount=" + orderPartialFillCacheCount);
                    Dictionary<string, int> keyValues = RealOrderService.OrderPartialFillCache;
                    foreach (KeyValuePair<string, int> entry in keyValues)
                    {
                        RealLogger.PrintOutput("- Key=" + entry.Key + " Value=" + entry.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                RealLogger.PrintOutput("Exception calling DumpInternalsToOutput:" + ex.Message + " " + ex.StackTrace);
                throw;
            }
        }
        private void AttemptToClosePositionsInProfit()
        {
            if (currentCloseAutoStatus != JoiaGestorCloseAutoTypes.Disabled || UsePositionProfitLogging)
            {
                var lockTimeout = TimeSpan.FromSeconds(10);
                bool lockTaken = false;
                try
                {
                    Monitor.TryEnter(ClosePositionsInProfitLock, lockTimeout, ref lockTaken);
                    if (lockTaken)
                    {
                        if (RealOrderService.AreAllOrderUpdateCyclesComplete())
                        {
                            bool isCloseAutoSlopeAll = (currentCloseAutoStatus == JoiaGestorCloseAutoTypes.MovingAverage1Slope
                                || currentCloseAutoStatus == JoiaGestorCloseAutoTypes.MovingAverage1SlopeMinProfit
                                || currentCloseAutoStatus == JoiaGestorCloseAutoTypes.MovingAverage2Slope
                                || currentCloseAutoStatus == JoiaGestorCloseAutoTypes.MovingAverage2SlopeMinProfit
                                || currentCloseAutoStatus == JoiaGestorCloseAutoTypes.MovingAverage3Slope
                                || currentCloseAutoStatus == JoiaGestorCloseAutoTypes.MovingAverage3SlopeMinProfit
                                );
                            if ((!IsAccountFlat(attachedInstrument) || !IsAccountFlat(blendedInstrument)) && currentBreakEvenAutoStatus != JoiaGestorBreakEvenAutoTypes.HODL)
                            {
                                int totalVolume = 0;
                                int totalOtherVolume = 0;
                                int totalMNQVolume = 0;
                                int totalNQVolume = 0;
                                int totalM2KVolume = 0;
                                int totalRTYVolume = 0;
                                int totalMESVolume = 0;
                                int totalESVolume = 0;
                                int totalMYMVolume = 0;
                                int totalYMVolume = 0;
                                double totalUnrealizedProfitLoss = 0;
                                double unrealizedProfitLoss = 0;
                                bool hasPosition = false;
                                bool hasAttachedPosition = false;
                                bool hasBlendedPosition = false;
                                double attachedPositionAveragePrice = 0;
                                double totalUnrealizedProfitLossAttached = 0;
                                int totalQuantityAttached = 0;
                                MarketPosition positionMarketPosition = MarketPosition.Flat;
                                int positionQuantity = 0;
                                int positionCount = RealPositionService.PositionCount;
                                for (int index = 0; index < positionCount; index++)
                                {
                                    RealPosition position = null;
                                    if (RealPositionService.TryGetByIndex(index, out position))
                                    {
                                        hasPosition = true;
                                        positionQuantity = position.Quantity;
                                        totalVolume += positionQuantity;
                                        if (position.Instrument == mymInstrument)
                                            totalMYMVolume += position.Quantity;
                                        else if (position.Instrument == mesInstrument)
                                            totalMESVolume += position.Quantity;
                                        else if (position.Instrument == m2kInstrument)
                                            totalM2KVolume += position.Quantity;
                                        else if (position.Instrument == mnqInstrument)
                                            totalMNQVolume += position.Quantity;
                                        else if (position.Instrument == ymInstrument)
                                            totalYMVolume += position.Quantity;
                                        else if (position.Instrument == esInstrument)
                                            totalESVolume += position.Quantity;
                                        else if (position.Instrument == rtyInstrument)
                                            totalRTYVolume += position.Quantity;
                                        else if (position.Instrument == nqInstrument)
                                            totalNQVolume += position.Quantity;
                                        else
                                            totalOtherVolume += position.Quantity;
                                        unrealizedProfitLoss = GetPositionProfit(position);
                                        totalUnrealizedProfitLoss += Math.Round(unrealizedProfitLoss, 2);
                                        if (position.Instrument == attachedInstrument)
                                        {
                                            hasAttachedPosition = true;
                                            positionMarketPosition = position.MarketPosition;
                                            totalUnrealizedProfitLossAttached += Math.Round(unrealizedProfitLoss, 2);
                                            totalQuantityAttached += positionQuantity;
                                            attachedPositionAveragePrice = position.AveragePrice;
                                        }
                                        else if (position.Instrument == blendedInstrument)
                                        {
                                            hasBlendedPosition = true;
                                        }
                                    }
                                }
                                if (hasPosition)
                                {
                                    double minAutoCloseProfit = (AutoCloseMinProfitDollarsPerVolume * totalQuantityAttached);
                                    double expectedECAProfitDollars = GetECATakeProfitDollars(totalVolume, totalOtherVolume,
                                        totalMYMVolume, totalMESVolume, totalM2KVolume, totalMNQVolume,
                                        totalYMVolume, totalESVolume, totalRTYVolume, totalNQVolume);
                                    cacheECATakeProfitDollars = expectedECAProfitDollars;
                                    if (UsePositionProfitLogging && hasAttachedPosition)
                                    {
                                        if (currentCloseAutoStatus == JoiaGestorCloseAutoTypes.EquityCloseAllTarget)
                                        {
                                            RealLogger.PrintOutput("Total vs Target PnL: $" + totalUnrealizedProfitLoss.ToString("N2") + " vs $" + expectedECAProfitDollars.ToString("N2") + " with " + Convert.ToString(totalVolume) + " volume / avg price " + attachedPositionAveragePrice.ToString("N2"), PrintTo.OutputTab1, true, true);
                                        }
                                        else
                                        {
                                            RealLogger.PrintOutput("Total PnL: $" + totalUnrealizedProfitLoss.ToString("N2") + " with " + Convert.ToString(totalVolume) + " volume / avg price " + attachedPositionAveragePrice.ToString("N2"), PrintTo.OutputTab1, true, true);
                                        }
                                    }
                                    if (hasAttachedPosition || hasBlendedPosition)
                                    {
                                        if (IsECATPEnabled()
                                            && currentBreakEvenAutoStatus != JoiaGestorBreakEvenAutoTypes.HODL
                                            && totalUnrealizedProfitLoss > 0
                                            && expectedECAProfitDollars > 0
                                            && totalUnrealizedProfitLoss >= expectedECAProfitDollars)
                                        {
                                            bool hasSyncedECATakeProfit = (IsAutoPositionTakeProfitEnabled() && IsECATPEnabled()
                                                && this.TakeProfitSyncECATargetPrice && lastECATakeProfitLevelLinePrice > 0
                                                && this.TakeProfitRefreshManagementEnabled
                                                && (attachedInstrumentPositionTakeProfitPrice > 0 || blendedInstrumentPositionTakeProfitPrice > 0));
                                            if (!hasSyncedECATakeProfit)
                                            {
                                                RealLogger.PrintOutput("ECATP target reached: Total vs Target PnL: $" + totalUnrealizedProfitLoss.ToString("N2") + " vs $" + expectedECAProfitDollars.ToString("N2") + " with " + Convert.ToString(totalVolume) + " volume", PrintTo.OutputTab1, true, false);
                                                FlattenEverything("EquityCloseAllTakeProfit", true, null);
                                            }
                                        }
                                        else if (isCloseAutoSlopeAll)
                                        {
                                            if (AutoCloseRunOncePerBar.IsFirstRunThisBar)
                                            {
                                                AutoCloseRunOncePerBar.SetRunCompletedThisBar();
                                                bool closePositionFlag = false;
                                                bool hasMinAutoCloseProfit = totalUnrealizedProfitLossAttached >= minAutoCloseProfit;
                                                if (currentCloseAutoStatus == JoiaGestorCloseAutoTypes.MovingAverage1Slope
                                                    || (currentCloseAutoStatus == JoiaGestorCloseAutoTypes.MovingAverage1SlopeMinProfit && hasMinAutoCloseProfit))
                                                {
                                                    if (hasAttachedPosition && positionMarketPosition == MarketPosition.Long)
                                                    {
                                                        if (autoCloseAndTrailMA1Value < autoCloseAndTrailMA1Value2 && autoCloseAndTrailMA1Value2 >= autoCloseAndTrailMA1Value3)
                                                        {
                                                            closePositionFlag = true;
                                                        }
                                                    }
                                                    else if (hasAttachedPosition && positionMarketPosition == MarketPosition.Short)
                                                    {
                                                        if (autoCloseAndTrailMA1Value >= autoCloseAndTrailMA1Value2 && autoCloseAndTrailMA1Value2 < autoCloseAndTrailMA1Value3)
                                                        {
                                                            closePositionFlag = true;
                                                        }
                                                    }
                                                }
                                                else if (currentCloseAutoStatus == JoiaGestorCloseAutoTypes.MovingAverage2Slope
                                                    || (currentCloseAutoStatus == JoiaGestorCloseAutoTypes.MovingAverage2SlopeMinProfit && hasMinAutoCloseProfit))
                                                {
                                                    if (hasAttachedPosition && positionMarketPosition == MarketPosition.Long)
                                                    {
                                                        if (autoCloseAndTrailMA2Value < autoCloseAndTrailMA2Value2 && autoCloseAndTrailMA2Value2 >= autoCloseAndTrailMA2Value3)
                                                        {
                                                            closePositionFlag = true;
                                                        }
                                                    }
                                                    else if (hasAttachedPosition && positionMarketPosition == MarketPosition.Short)
                                                    {
                                                        if (autoCloseAndTrailMA2Value >= autoCloseAndTrailMA2Value2 && autoCloseAndTrailMA2Value2 < autoCloseAndTrailMA2Value3)
                                                        {
                                                            closePositionFlag = true;
                                                        }
                                                    }
                                                }
                                                else if (currentCloseAutoStatus == JoiaGestorCloseAutoTypes.MovingAverage3Slope
                                                    || (currentCloseAutoStatus == JoiaGestorCloseAutoTypes.MovingAverage3SlopeMinProfit && hasMinAutoCloseProfit))
                                                {
                                                    if (hasAttachedPosition && positionMarketPosition == MarketPosition.Long)
                                                    {
                                                        if (autoCloseAndTrailMA3Value < autoCloseAndTrailMA3Value2 && autoCloseAndTrailMA3Value2 >= autoCloseAndTrailMA3Value3)
                                                        {
                                                            closePositionFlag = true;
                                                        }
                                                    }
                                                    else if (hasAttachedPosition && positionMarketPosition == MarketPosition.Short)
                                                    {
                                                        if (autoCloseAndTrailMA3Value >= autoCloseAndTrailMA3Value2 && autoCloseAndTrailMA3Value2 < autoCloseAndTrailMA3Value3)
                                                        {
                                                            closePositionFlag = true;
                                                        }
                                                    }
                                                }
                                                if (closePositionFlag)
                                                {
                                                    FlattenEverything(currentCloseAutoStatus.ToString(), true, attachedInstrument);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    RealLogger.PrintOutput("Exception calling AttemptToClosePositionsInProfit:" + ex.Message + " " + ex.StackTrace);
                    throw;
                }
                finally
                {
                    if (lockTaken)
                    {
                        Monitor.Exit(ClosePositionsInProfitLock);
                    }
                }
            }
        }
        private void AttemptToClosePositionsInLoss()
        {
            if (IsMaxDDStopLossEnabled() || IsDayOverAccountBalanceFloorEnabled() || IsDayOverMaxLossEnabled())
            {
                var lockTimeout = TimeSpan.FromSeconds(10);
                bool lockTaken = false;
                try
                {
                    Monitor.TryEnter(ClosePositionsInLossLock, lockTimeout, ref lockTaken);
                    if (lockTaken)
                    {
                        if (!IsAccountFlat(attachedInstrument)
                            && RealOrderService.AreAllOrderUpdateCyclesComplete())
                        {
                            int totalVolume = 0;
                            int totalMicroVolume = 0;
                            int totalEminiVolume = 0;
                            double totalUnrealizedProfitLoss = 0;
                            double unrealizedProfitLoss = 0;
                            int positionQuantity = 0;
                            double oldStopLossPrice = 0;
                            OrderType stopLossOrderType = OrderType.Unknown;
                            int oldStopLossOrderQuantity = 0;
                            int stopLossOrderCount = 0;
                            bool hasStopLoss = false;
                            int positionCount = RealPositionService.PositionCount;
                            for (int index = 0; index < positionCount; index++)
                            {
                                RealPosition position = null;
                                if (RealPositionService.TryGetByIndex(index, out position))
                                {
                                    oldStopLossPrice = 0;
                                    stopLossOrderType = OrderType.Unknown;
                                    oldStopLossOrderQuantity = 0;
                                    stopLossOrderCount = 0;
                                    hasStopLoss = false;
                                    positionQuantity = position.Quantity;
                                    totalVolume += positionQuantity;
                                    if (IsMicroInstrument(position.Instrument)) totalMicroVolume += positionQuantity;
                                    else if (IsEminiInstrument(position.Instrument)) totalEminiVolume += positionQuantity;
                                    unrealizedProfitLoss = GetPositionProfit(position);
                                    totalUnrealizedProfitLoss += Math.Round(unrealizedProfitLoss, 2);
                                    oldStopLossPrice = RealOrderService.GetStopLossInfo(position.Account, position.Instrument, ConvertMarketPositionToSLOrderAction(position.MarketPosition), out stopLossOrderType, out oldStopLossOrderQuantity, out stopLossOrderCount);
                                    hasStopLoss = (oldStopLossPrice > 0);
                                }
                            }
                            if (totalVolume > 0 && totalUnrealizedProfitLoss < 0)
                            {
                                double netLiquidationBalance = Math.Round(account.Get(AccountItem.NetLiquidation, Currency.UsDollar), 2);
                                if (IsMaxDDStopLossEnabled() && totalUnrealizedProfitLoss <= maxDDInDollars)
                                {
                                    RealLogger.PrintOutput("Max DD reached: $" + totalUnrealizedProfitLoss.ToString("N2") + " ($" + maxDDInDollars.ToString("N2") + ") with " + Convert.ToString(totalVolume) + " volume", PrintTo.OutputTab1, false);
                                    FlattenEverything("MaxDDStopLoss", true, null);
                                }
                                else if (IsDayOverAccountBalanceFloorEnabled() && netLiquidationBalance <= DayOverAccountBalanceFloorDollars)
                                {
                                    RealLogger.PrintOutput("Day over account balance floor reached: $" + netLiquidationBalance.ToString("N2") + "/ $" + DayOverAccountBalanceFloorDollars.ToString("N2"), PrintTo.OutputTab1, false);
                                    FlattenEverything("DayOverAccountBalanceFloorDollars", true, null);
                                }
                                else if (IsDayOverMaxLossEnabled() && (activeDayOverMaxLossAutoClose || (!hasStopLoss && (lastDayOverMaxLossDollars > 0 && unrealizedProfitLoss < 0 && (unrealizedProfitLoss * -1) >= lastDayOverMaxLossDollars)))) 
                                {
                                    string formattedDailyMaxLossDollars = "";
                                    if (lastDayOverMaxLossDollars > 0)
                                    {
                                        formattedDailyMaxLossDollars = "$" + lastDayOverMaxLossDollars.ToString("N0");
                                    }
                                    else
                                    {
                                        formattedDailyMaxLossDollars = "($" + lastDayOverMaxLossDollars.ToString("N0") + ")";
                                    }
                                    RealLogger.PrintOutput("Day over daily max loss reached: " + formattedDailyMaxLossDollars + " / $" + dayOverMaxLossDollars.ToString("N0"), PrintTo.OutputTab1, false);
                                    FlattenEverything("DayOverMaxLoss", true, null);
                                    activeDayOverMaxLossAutoClose = false;
                                }
                                else if (IsExcessIntradayMarginMinDollarsEnabled() && lastAccountIntradayExcessMargin != 0 && lastAccountIntradayExcessMargin < ExcessIntradayMarginMinDollars)
                                {
                                    RealLogger.PrintOutput("Excess intraday margin min dollars reached: $" + lastAccountIntradayExcessMargin.ToString("N0")
                                        + " ($" + ExcessIntradayMarginMinDollars.ToString("N0") + ")");
                                    FlattenEverything("ExcessIntradayMarginMinDollars", true, null);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    RealLogger.PrintOutput("Exception calling AttemptToClosePositionsInLoss:" + ex.Message + " " + ex.StackTrace);
                    throw;
                }
                finally
                {
                    if (lockTaken)
                    {
                        Monitor.Exit(ClosePositionsInLossLock);
                    }
                }
            }
        }
        private void CreateSellStop(string signalName, Instrument instrument, OrderAction orderAction, OrderEntry orderEntry, int quantity, double price)
        {
            if (orderAction != OrderAction.BuyToCover && orderAction != OrderAction.SellShort)
            {
                RealLogger.PrintOutput("ERROR: Order action of " + orderAction.ToString() + " not supported.");
                return;
            }
            OrderType orderType = OrderType.StopMarket;
            lock (account.Orders)
            {
                string orderName = RealOrderService.BuildEntryOrderName();
                try
                {
                    Order entryOrder = account.CreateOrder(instrument, orderAction, orderType, orderEntry, TimeInForce.Day, quantity, 0, price, "", orderName, Core.Globals.MaxDate, null);
                    if (HasATMStrategy()) NinjaTrader.NinjaScript.AtmStrategy.StartAtmStrategy(atmStrategyName, entryOrder);
                    account.Submit(new[] { entryOrder });
                }
                catch (Exception ex)
                {
                    RealLogger.PrintOutput("Exception in CreateSellStop:" + ex.Message + " " + ex.StackTrace);  
                }
            }
        }
        private void CreateBuyStop(string signalName, Instrument instrument, OrderAction orderAction, OrderEntry orderEntry, int quantity, double price)
        {
            if (orderAction != OrderAction.BuyToCover && orderAction != OrderAction.SellShort)
            {
                RealLogger.PrintOutput("ERROR: Order action of " + orderAction.ToString() + " not supported.");
                return;
            }
            OrderType orderType = OrderType.StopMarket;
            lock (account.Orders)
            {
                string orderName = RealOrderService.BuildEntryOrderName();
                try
                {
                    Order entryOrder = account.CreateOrder(instrument, orderAction, orderType, orderEntry, TimeInForce.Day, quantity, 0, price, "", orderName, Core.Globals.MaxDate, null);
                    if (HasATMStrategy()) NinjaTrader.NinjaScript.AtmStrategy.StartAtmStrategy(atmStrategyName, entryOrder);
                    account.Submit(new[] { entryOrder });
                }
                catch (Exception ex)
                {
                    RealLogger.PrintOutput("Exception in CreateBuyStop:" + ex.Message + " " + ex.StackTrace);  
                }
            }
        }
        private void CreateSellLimit(string signalName, Instrument instrument, OrderAction orderAction, OrderEntry orderEntry, int quantity, double price)
        {
            if (orderAction != OrderAction.Buy && orderAction != OrderAction.Sell)
            {
                RealLogger.PrintOutput("ERROR: Order action of " + orderAction.ToString() + " not supported.");
                return;
            }
            OrderType orderType = OrderType.Limit;
            lock (account.Orders)
            {
                string orderName = RealOrderService.BuildEntryOrderName();
                try
                {
                    Order entryOrder = account.CreateOrder(instrument, orderAction, orderType, orderEntry, TimeInForce.Day, quantity, price, 0, "", orderName, Core.Globals.MaxDate, null);
                    if (HasATMStrategy()) NinjaTrader.NinjaScript.AtmStrategy.StartAtmStrategy(atmStrategyName, entryOrder);
                    account.Submit(new[] { entryOrder });
                }
                catch (Exception ex)
                {
                    RealLogger.PrintOutput("Exception in CreateSellLimit:" + ex.Message + " " + ex.StackTrace);  
                }
            }
        }
        private void CreateBuyLimit(string signalName, Instrument instrument, OrderAction orderAction, OrderEntry orderEntry, int quantity, double price)
        {
            if (orderAction != OrderAction.Buy && orderAction != OrderAction.Sell)
            {
                RealLogger.PrintOutput("ERROR: Order action of " + orderAction.ToString() + " not supported.");
                return;
            }
            OrderType orderType = OrderType.Limit;
            lock (account.Orders)
            {
                string orderName = RealOrderService.BuildEntryOrderName();
                try
                {
                    Order entryOrder = account.CreateOrder(instrument, orderAction, orderType, orderEntry, TimeInForce.Day, quantity, price, 0, "", orderName, Core.Globals.MaxDate, null);
                    if (HasATMStrategy()) NinjaTrader.NinjaScript.AtmStrategy.StartAtmStrategy(atmStrategyName, entryOrder);
                    account.Submit(new[] { entryOrder });
                }
                catch (Exception ex)
                {
                    RealLogger.PrintOutput("Exception in CreateBuyLimit:" + ex.Message + " " + ex.StackTrace);  
                }
            }
        }
        private void UpdateStopOrder(string signalName, Instrument instrument, OrderAction orderAction, OrderEntry orderEntry, double price)
        {
            if (orderAction != OrderAction.BuyToCover && orderAction != OrderAction.SellShort)
            {
                RealLogger.PrintOutput("ERROR: Order action of " + orderAction.ToString() + " not supported in UpdateStopOrder.");
                return;
            }
            double lastPrice = RealInstrumentService.GetLastPrice(instrument);
            if (orderAction == OrderAction.BuyToCover && price <= lastPrice)
            {
                RealLogger.PrintOutput("ERROR: Stop order price must be greater than last price.");
                return;
            }
            else if (orderAction == OrderAction.SellShort && price >= lastPrice)
            {
                RealLogger.PrintOutput("ERROR: Stop order price must be less than last price.");
                return;
            }
            bool orderChanged = false;
            int orderCount = RealOrderService.OrderCount;
            for (int index = 0; index < orderCount; index++)
            {
                RealOrder order = null;
                if (RealOrderService.TryGetByIndex(index, out order))
                {
                    if (RealOrderService.IsValidBuyStopOrder(order, instrument, OrderAction.BuyToCover) || RealOrderService.IsValidSellStopOrder(order, instrument, OrderAction.SellShort))
                    {
                        orderChanged = false;
                        Order foundNTOrder = GetNinjaTraderOrder(order);
                        if (foundNTOrder != null)
                        {
                            if (foundNTOrder.StopPrice != price)
                            {
                                foundNTOrder.StopPriceChanged = price;
                                orderChanged = true;
                            }
                            if (orderChanged)
                            {
                                try
                                {
                                    account.Change(new[] { foundNTOrder });
                                }
                                catch (Exception ex)
                                {
                                    RealLogger.PrintOutput("Exception in UpdateStopOrder:" + ex.Message + " " + ex.StackTrace);  
                                }
                            }
                        }
                    }
                }
            }
        }
        private void UpdateLimitOrder(string signalName, Instrument instrument, OrderAction orderAction, OrderEntry orderEntry, double price)
        {
            if (orderAction != OrderAction.Buy && orderAction != OrderAction.Sell)
            {
                RealLogger.PrintOutput("ERROR: Order action of " + orderAction.ToString() + " not supported in UpdateLimitOrder.");
                return;
            }
            double lastPrice = RealInstrumentService.GetLastPrice(instrument);
            if (orderAction == OrderAction.Buy && price >= lastPrice)
            {
                RealLogger.PrintOutput("ERROR: Limit order price must be less than last price.");
                return;
            }
            else if (orderAction == OrderAction.Sell && price <= lastPrice)
            {
                RealLogger.PrintOutput("ERROR: Limit order price must be greater than last price.");
                return;
            }
            bool orderChanged = false;
            int orderCount = RealOrderService.OrderCount;
            for (int index = 0; index < orderCount; index++)
            {
                RealOrder order = null;
                if (RealOrderService.TryGetByIndex(index, out order))
                {
                    if (RealOrderService.IsValidBuyLimitOrder(order, instrument, OrderAction.Buy) || RealOrderService.IsValidSellLimitOrder(order, instrument, OrderAction.Sell))
                    {
                        orderChanged = false;
                        Order foundNTOrder = GetNinjaTraderOrder(order);
                        if (foundNTOrder != null)
                        {
                            if (foundNTOrder.LimitPrice != price)
                            {
                                foundNTOrder.LimitPriceChanged = price;
                                orderChanged = true;
                            }
                            if (orderChanged)
                            {
                                try
                                {
                                    account.Change(new[] { foundNTOrder });
                                }
                                catch (Exception ex)
                                {
                                    RealLogger.PrintOutput("Exception in UpdateLimitOrder:" + ex.Message + " " + ex.StackTrace);  
                                }
                            }
                        }
                    }
                }
            }
        }
        private void CreatePositionStopLoss(string signalName, Instrument instrument, OrderAction orderAction, OrderEntry orderEntry, int quantity, double price)
        {
            if (orderAction != OrderAction.BuyToCover && orderAction != OrderAction.Sell)
            {
                RealLogger.PrintOutput("ERROR: Order action of " + orderAction.ToString() + " not supported.");
                return;
            }
            double lastPrice = RealInstrumentService.GetLastPrice(instrument);
            bool isValid = RealOrderService.IsValidStopLossPrice(instrument, orderAction, price, lastPrice);
            if (orderAction == OrderAction.BuyToCover && !isValid)
            {
                RealLogger.PrintOutput("ERROR: Stop Loss order price must be greater than last price.");
                return;
            }
            else if (orderAction == OrderAction.Sell && !isValid)
            {
                RealLogger.PrintOutput("ERROR: Stop Loss order price must be less than last price.");
                return;
            }
            OrderType orderType = OrderType.StopMarket;
            lock (account.Orders)
            {
                string orderName = RealOrderService.BuildStopOrderName();
                try
                {
                    Order stopOrder = account.CreateOrder(instrument, orderAction, orderType, orderEntry, TimeInForce.Gtc, quantity, 0, price, "", orderName, Core.Globals.MaxDate, null);
                    account.Submit(new[] { stopOrder });
                }
                catch (Exception ex)
                {
                    RealLogger.PrintOutput("Exception in CreatePositionStopLoss:" + ex.Message + " " + ex.StackTrace);  
                }
            }
        }
        private void UpdatePositionStopLoss(string signalName, Instrument instrument, OrderAction orderAction, OrderEntry orderEntry, int quantity, double price)
        {
            if (orderAction != OrderAction.BuyToCover && orderAction != OrderAction.Sell)
            {
                RealLogger.PrintOutput("ERROR: Order action of " + orderAction.ToString() + " not supported.");
                return;
            }
            double lastPrice = RealInstrumentService.GetLastPrice(instrument);
            bool isValid = RealOrderService.IsValidStopLossPrice(instrument, orderAction, price, lastPrice);
            if (orderAction == OrderAction.BuyToCover && !isValid)
            {
                RealLogger.PrintOutput("ERROR: Stop Loss order price must be greater than last price.");
                return;
            }
            else if (orderAction == OrderAction.Sell && !isValid)
            {
                RealLogger.PrintOutput("ERROR: Stop Loss order price must be less than last price.");
                return;
            }
            int orderCount = RealOrderService.OrderCount;
            for (int index = 0; index < orderCount; index++)
            {
                RealOrder order = null;
                if (RealOrderService.TryGetByIndex(index, out order))
                {
                    if (RealOrderService.IsValidStopLossOrder(order, instrument, orderAction))
                    {
                        bool orderChanged = false;
                        Order foundNTOrder = GetNinjaTraderOrder(order);
                        if (foundNTOrder != null)
                        {
                            if (foundNTOrder.Quantity != quantity && quantity != 0)
                            {
                                foundNTOrder.QuantityChanged = quantity;
                                orderChanged = true;
                            }
                            if (foundNTOrder.StopPrice != price)
                            {
                                foundNTOrder.StopPriceChanged = price;
                                orderChanged = true;
                            }
                            if (orderChanged)
                            {
                                try
                                {
                                    account.Change(new[] { foundNTOrder });
                                }
                                catch (Exception ex)
                                {
                                    RealLogger.PrintOutput("Exception in UpdatePositionStopLoss:" + ex.Message + " " + ex.StackTrace);  
                                }
                            }
                            if (quantity != 0) break; 
                        }
                    }
                }
            }
        }
        private void CreatePositionTakeProfit(string signalName, Instrument instrument, OrderAction orderAction, OrderEntry orderEntry, int quantity, double price)
        {
            if (orderAction != OrderAction.BuyToCover && orderAction != OrderAction.Sell)
            {
                RealLogger.PrintOutput("ERROR: Order action of " + orderAction.ToString() + " not supported.");
                return;
            }
            double lastPrice = RealInstrumentService.GetLastPrice(instrument);
            bool isValid = RealOrderService.IsValidTakeProfitPrice(instrument, orderAction, price, lastPrice);
            if (orderAction == OrderAction.BuyToCover && !isValid)
            {
                RealLogger.PrintOutput("ERROR: Take Profit order price must be less than last price.");
                return;
            }
            else if (orderAction == OrderAction.Sell && !isValid)
            {
                RealLogger.PrintOutput("ERROR: Take Profit order price must be greater than last price.");
                return;
            }
            OrderType orderType = OrderType.Limit;
            lock (account.Orders)
            {
                string orderName = RealOrderService.BuildTargetOrderName();
                try
                {
                    Order targetOrder = account.CreateOrder(instrument, orderAction, orderType, orderEntry, TimeInForce.Gtc, quantity, price, 0, "", orderName, Core.Globals.MaxDate, null);
                    account.Submit(new[] { targetOrder });
                }
                catch (Exception ex)
                {
                    RealLogger.PrintOutput("Exception in CreatePositionTakeProfit:" + ex.Message + " " + ex.StackTrace);  
                }
            }
        }
        private bool ConsolidatePositionTPSLOrders(string signalName, Instrument instrument)
        {
            bool returnFlag = false;
            bool closeAll = false;
            int buyOrderCount = 0;
            int sellOrderCount = 0;
            bool hasSkippedFirst = false;
            OrderType stopLossOrderType = OrderType.StopMarket;
            OrderType takeProfitOrderType = OrderType.Limit;
            List<Order> cancelOrderList = new List<Order>();
            int orderCount = RealOrderService.OrderCount;
            for (int index = 0; index < orderCount; index++)
            {
                RealOrder order = null;
                bool isFirstStopLossOrder = false;
                bool isFirstTakeProfitOrder = false;
                Order firstStopLossOrder = null;
                Order firstTakeProfitOrder = null;
                if (RealOrderService.TryGetByIndex(index, out order))
                {
                    if (RealOrderService.IsValidStopLossOrder(order, instrument, OrderAction.BuyToCover) || RealOrderService.IsValidStopLossOrder(order, instrument, OrderAction.Sell)
                       || RealOrderService.IsValidTakeProfitOrder(order, instrument, OrderAction.BuyToCover) || RealOrderService.IsValidTakeProfitOrder(order, instrument, OrderAction.Sell))
                    {
                        if (order.OrderType == stopLossOrderType && (order.OrderState == OrderState.Accepted || order.OrderState == OrderState.Working))
                        {
                            if (!isFirstStopLossOrder)
                            {
                                isFirstStopLossOrder = true;
                                Order foundNTOrder = GetNinjaTraderOrder(order);
                                firstStopLossOrder = foundNTOrder;
                            }
                            else
                            {
                                Order foundNTOrder = GetNinjaTraderOrder(order);
                                cancelOrderList.Add(foundNTOrder);
                            }
                        }
                        else if (order.OrderType == takeProfitOrderType && order.OrderState == OrderState.Working)
                        {
                            if (!isFirstTakeProfitOrder)
                            {
                                isFirstTakeProfitOrder = true;
                                Order foundNTOrder = GetNinjaTraderOrder(order);
                                firstTakeProfitOrder = foundNTOrder;
                            }
                            else
                            {
                                Order foundNTOrder = GetNinjaTraderOrder(order);
                                cancelOrderList.Add(foundNTOrder);
                            }
                        }
                    }
                }
                int stopLossQuantity = 0;
                int takeProfitQuantity = 0;
                if (firstStopLossOrder != null)
                {
                    stopLossQuantity = firstStopLossOrder.Quantity;
                }
                if (firstTakeProfitOrder != null)
                {
                    takeProfitQuantity = firstTakeProfitOrder.Quantity;
                }
                foreach (Order cancelOrder in cancelOrderList)
                {
                    bool increamentStopLossQuantity = false;
                    bool increamentTakeProfitQuantity = false;
                    int initialStopLossQuantity = 0;
                    int initialTakeProfitQuantity = 0;
                    int cancelOrderQuantity = 0;
                    try
                    {
                        increamentStopLossQuantity = (cancelOrder.OrderType == stopLossOrderType && firstStopLossOrder != null);
                        increamentTakeProfitQuantity = (cancelOrder.OrderType == takeProfitOrderType && firstTakeProfitOrder != null);
                        cancelOrderQuantity = cancelOrder.Quantity;
                        account.Cancel(new[] { cancelOrder });
                        if (increamentStopLossQuantity)
                        {
                            stopLossQuantity += cancelOrderQuantity;
                            firstStopLossOrder.QuantityChanged = stopLossQuantity;
                            account.Change(new[] { firstStopLossOrder });
                            firstStopLossOrder.Quantity = stopLossQuantity;
                        }
                        else if (increamentTakeProfitQuantity)
                        {
                            takeProfitQuantity += cancelOrderQuantity;
                            firstTakeProfitOrder.QuantityChanged = takeProfitQuantity;
                            account.Change(new[] { firstTakeProfitOrder });
                            firstTakeProfitOrder.Quantity = takeProfitQuantity;
                        }
                        returnFlag = true;
                    }
                    catch (Exception ex)
                    {
                        RealLogger.PrintOutput("Exception in ConsolidatePositionTPSLOrders:" + ex.Message + " " + ex.StackTrace);  
                    }
                }
            }
            return returnFlag;
        }
        private bool CancelPositionTPSLOrders(string signalName, Instrument instrument, OrderAction? orderAction = null)
        {
            bool returnFlag = false;
            bool closeAll = false;
            OrderAction tempOrderAction = OrderAction.BuyToCover;
            if (orderAction == null)
                closeAll = true;
            else if (orderAction == OrderAction.BuyToCover)
                tempOrderAction = OrderAction.BuyToCover;
            else if (orderAction == OrderAction.Sell)
                tempOrderAction = OrderAction.Sell;
            else
            {
                RealLogger.PrintOutput("Order action type not supported: " + Convert.ToString(orderAction));
            }
            int orderCount = RealOrderService.OrderCount;
            for (int index = 0; index < orderCount; index++)
            {
                RealOrder order = null;
                if (RealOrderService.TryGetByIndex(index, out order))
                {
                    if (closeAll && (RealOrderService.IsValidStopLossOrder(order, instrument, OrderAction.BuyToCover) || RealOrderService.IsValidStopLossOrder(order, instrument, OrderAction.Sell)
                        || RealOrderService.IsValidTakeProfitOrder(order, instrument, OrderAction.BuyToCover) || RealOrderService.IsValidTakeProfitOrder(order, instrument, OrderAction.Sell))
                        || (!closeAll && (RealOrderService.IsValidStopLossOrder(order, instrument, tempOrderAction) || RealOrderService.IsValidTakeProfitOrder(order, instrument, tempOrderAction))))
                    {
                        Order foundNTOrder = GetNinjaTraderOrder(order);
                        if (foundNTOrder != null)
                        {
                            try
                            {
                                account.Cancel(new[] { foundNTOrder });
                                returnFlag = true;
                            }
                            catch (Exception ex)
                            {
                                RealLogger.PrintOutput("Exception in CancelPositionTPSLOrders:" + ex.Message + " " + ex.StackTrace);  
                            }
                        }
                    }
                }
            }
            return returnFlag;
        }
        private bool CancelPositionTPOrders(string signalName, Instrument instrument, OrderAction? orderAction = null)
        {
            bool returnFlag = false;
            bool closeAll = false;
            OrderAction tempOrderAction = OrderAction.BuyToCover;
            if (orderAction == null)
                closeAll = true;
            else if (orderAction == OrderAction.BuyToCover)
                tempOrderAction = OrderAction.BuyToCover;
            else if (orderAction == OrderAction.Sell)
                tempOrderAction = OrderAction.Sell;
            else
            {
                RealLogger.PrintOutput("Order action type not supported: " + Convert.ToString(orderAction));
            }
            int orderCount = RealOrderService.OrderCount;
            for (int index = 0; index < orderCount; index++)
            {
                RealOrder order = null;
                if (RealOrderService.TryGetByIndex(index, out order))
                {
                    if (closeAll
                        && (RealOrderService.IsValidTakeProfitOrder(order, instrument, OrderAction.BuyToCover)
                        || RealOrderService.IsValidTakeProfitOrder(order, instrument, OrderAction.Sell))
                        || (!closeAll && RealOrderService.IsValidTakeProfitOrder(order, instrument, tempOrderAction)))
                    {
                        Order foundNTOrder = GetNinjaTraderOrder(order);
                        if (foundNTOrder != null)
                        {
                            try
                            {
                                account.Cancel(new[] { foundNTOrder });
                                returnFlag = true;
                            }
                            catch (Exception ex)
                            {
                                RealLogger.PrintOutput("Exception in CancelPositionTPSLOrders:" + ex.Message + " " + ex.StackTrace);  
                            }
                        }
                    }
                }
            }
            return returnFlag;
        }
        private void UpdatePositionTakeProfit(string signalName, Instrument instrument, OrderAction orderAction, OrderEntry orderEntry, int quantity, double price)
        {
            if (orderAction != OrderAction.BuyToCover && orderAction != OrderAction.Sell)
            {
                RealLogger.PrintOutput("ERROR: Order action of " + orderAction.ToString() + " not supported.");
                return;
            }
            double lastPrice = RealInstrumentService.GetLastPrice(instrument);
            bool isValid = RealOrderService.IsValidTakeProfitPrice(instrument, orderAction, price, lastPrice);
            if (orderAction == OrderAction.BuyToCover && !isValid)
            {
                RealLogger.PrintOutput("ERROR: Take Profit order price must be less than last price.");
                return;
            }
            else if (orderAction == OrderAction.Sell && !isValid)
            {
                RealLogger.PrintOutput("ERROR: Take Profit order price must be greater than last price.");
                return;
            }
            int orderCount = RealOrderService.OrderCount;
            for (int index = 0; index < orderCount; index++)
            {
                RealOrder order = null;
                if (RealOrderService.TryGetByIndex(index, out order))
                {
                    if (RealOrderService.IsValidTakeProfitOrder(order, instrument, orderAction))
                    {
                        bool orderChanged = false;
                        Order foundNTOrder = GetNinjaTraderOrder(order);
                        if (foundNTOrder != null)
                        {
                            if (foundNTOrder.Quantity != quantity && quantity != 0)
                            {
                                foundNTOrder.QuantityChanged = quantity;
                                orderChanged = true;
                            }
                            if (foundNTOrder.LimitPrice != price)
                            {
                                foundNTOrder.LimitPriceChanged = price;
                                orderChanged = true;
                            }
                            if (orderChanged)
                            {
                                try
                                {
                                    account.Change(new[] { foundNTOrder });
                                }
                                catch (Exception ex)
                                {
                                    RealLogger.PrintOutput("Exception in UpdatePositionTakeProfit:" + ex.Message + " " + ex.StackTrace);  
                                }
                            }
                            if (quantity != 0) break; 
                        }
                    }
                }
            }
        }
        private bool FlattenEverything(string signalName, bool continueTillZeroRemainingQuantity, Instrument limitToSingleInstrument, Instrument secondaryInstrument = null)
        {
            bool positionFound = false;
            if (RealOrderService.AreAllOrderUpdateCyclesComplete())
            {
                CloseAllAccountPendingOrders(signalName, limitToSingleInstrument);
                if (secondaryInstrument != null) CloseAllAccountPendingOrders(signalName, secondaryInstrument);
                if (!IsAccountFlat())
                {
                    double unrealizedProfitLoss = 0;
                    OrderAction orderAction = OrderAction.Buy;
                    int positionCount = RealPositionService.PositionCount;
                    for (int index = 0; index < positionCount; index++)
                    {
                        RealPosition position = null;
                        if (RealPositionService.TryGetByIndex(index, out position))
                        {
                            if (limitToSingleInstrument == null || position.Instrument == limitToSingleInstrument || position.Instrument == secondaryInstrument)
                            {
                                position.StoreState(); 
                                positionFound = true;
                                unrealizedProfitLoss = GetPositionProfit(position);
                                if (position.MarketPosition == MarketPosition.Long)
                                    orderAction = OrderAction.Sell;
                                else if (position.MarketPosition == MarketPosition.Short)
                                    orderAction = OrderAction.Buy;
                                if (position.Quantity > 0 && !position.HasStateChanged() && !position.IsFlat())
                                {
                                    if (DebugLogLevel > 0) RealLogger.PrintOutput(signalName + " closing " + position.MarketPosition.ToString() + " " + position.Instrument.FullName + " Quantity=" + position.Quantity + " Profit=" + Convert.ToString(unrealizedProfitLoss), PrintTo.OutputTab1);
                                    SubmitMarketOrderChunked(position.Instrument, orderAction, OrderEntry.Manual, position.Quantity, continueTillZeroRemainingQuantity);
                                }
                                if (!continueTillZeroRemainingQuantity) break;
                            }
                        }
                    }
                }
            }
            return positionFound;
        }
        private bool IsMaxDDStopLossEnabled()
        {
            bool returnFlag = false;
            if (maxDDInDollars < 0)
            {
                returnFlag = true;
            }
            return (returnFlag);
        }
        private bool IsDayOverAccountBalanceFloorEnabled()
        {
            bool returnFlag = false;
            if (DayOverAccountBalanceFloorDollars > 0)
            {
                returnFlag = true;
            }
            return (returnFlag);
        }
        private bool IsDayOverMaxLossEnabled()
        {
            bool returnFlag = false;
            if (dayOverMaxLossDollars > 0)
            {
                returnFlag = true;
            }
            return (returnFlag);
        }
        private bool IsAutoPositionTakeProfitEnabled()
        {
            bool returnFlag = false;
            if (UseAutoPositionTakeProfit && currentBreakEvenAutoStatus != JoiaGestorBreakEvenAutoTypes.HODL)
            {
                returnFlag = true;
            }
            return (returnFlag);
        }
        private bool IsAutoPositionStopLossEnabled()
        {
            bool returnFlag = false;
            if (UseAutoPositionStopLoss)
            {
                returnFlag = true;
            }
            return (returnFlag);
        }
        private bool IsECATPEnabled()
        {
            bool returnFlag = false;
            if ((ECATargetDollars > 0 || ECATargetDollarsPerOtherVolume > 0) && currentCloseAutoStatus == JoiaGestorCloseAutoTypes.EquityCloseAllTarget)
            {
                returnFlag = true;
            }
            return (returnFlag);
        }
        private bool IsExcessIntradayMarginMinDollarsEnabled()
        {
            bool returnFlag = false;
            if (ExcessIntradayMarginMinDollars > 0)
                returnFlag = true;
            return (returnFlag);
        }
        private int GetValidVolumeSize(int volumeSize)
        {
            int newVolumeSize = DEFAULT_VOLUME_SIZE;
            bool isPositiveVolumeSize = volumeSize > 0;
            if (isPositiveVolumeSize)
            {
                newVolumeSize = volumeSize;
            }
            else
            {
                RealLogger.PrintOutput("GetValidVolumeSize: Invalid volume size: " + volumeSize.ToString("N0"));
            }
            return newVolumeSize;
        }
        private int GetLimitedIncrementVolumeSize(int incrementVolumeSize, int bogeyTargetBaseVolumeSize, bool limitAddOnVolumeToInProfit)
        {
            int newVolumeSize = DEFAULT_VOLUME_SIZE;
            int validatedIncrementVolumeSize = GetValidVolumeSize(incrementVolumeSize);
            int validatedBogeyTargetBaseVolumeSize = GetValidVolumeSize(bogeyTargetBaseVolumeSize);
            bool isIncrementVolumeSizeValidForBogey = limitAddOnVolumeToInProfit && validatedIncrementVolumeSize <= validatedBogeyTargetBaseVolumeSize;
            if (!limitAddOnVolumeToInProfit
               || isIncrementVolumeSizeValidForBogey)
            {
                newVolumeSize = incrementVolumeSize;
            }
            else if (validatedIncrementVolumeSize <= validatedBogeyTargetBaseVolumeSize)
            {
                newVolumeSize = validatedIncrementVolumeSize;
            }
            else
            {
                newVolumeSize = validatedBogeyTargetBaseVolumeSize;
            }
            return newVolumeSize;
        }
        private bool TryGetLimitAddOnVolumeSize(int positionVolumeSize, bool positionInProfit, int incrementVolumeSize, int bogeyTargetBaseVolumeSize, out int newVolumeSize)
        {
            bool addOnVolumeAllowed = false;
            newVolumeSize = DEFAULT_VOLUME_SIZE;
            if (LimitAddOnVolumeToInProfit)
            {
                int limitedIncrementVolumeSize = GetLimitedIncrementVolumeSize(incrementVolumeSize, bogeyTargetBaseVolumeSize, LimitAddOnVolumeToInProfit);
                bool isPositionVolumeAlreadyAtBogeyVolumeSize = positionVolumeSize >= bogeyTargetBaseVolumeSize;
                if (positionInProfit && isPositionVolumeAlreadyAtBogeyVolumeSize)
                {
                    newVolumeSize = incrementVolumeSize;
                    addOnVolumeAllowed = true;
                }
                else
                {
                    int remainingVolumeSize = bogeyTargetBaseVolumeSize - positionVolumeSize;
                    bool isRemainingVolumeSizeValid = remainingVolumeSize > 0;
                    bool isFullIncrementTooLarge = limitedIncrementVolumeSize > remainingVolumeSize;
                    if (isRemainingVolumeSizeValid && isFullIncrementTooLarge)
                    {
                        newVolumeSize = remainingVolumeSize;
                        addOnVolumeAllowed = true;
                    }
                    else if (isRemainingVolumeSizeValid && !isFullIncrementTooLarge)
                    {
                        newVolumeSize = limitedIncrementVolumeSize;
                        addOnVolumeAllowed = true;
                    }
                }
            }
            return addOnVolumeAllowed;
        }
        private int GetRandomNumber(int maxValue)
        {
            int randomNumber = 0;
            if (maxValue == 1)
            {
                randomNumber = maxValue;
            }
            else if (maxValue > 0)
            {
                int minValue = (int)maxValue / 2; 
                Random random = new Random();
                randomNumber = random.Next(minValue, maxValue + 1);
            }
            return randomNumber;
        }
        private void SubmitMarketOrder(Instrument instrument, OrderAction orderAction, OrderEntry orderEntry, int quantity)
        {
            lock (MarketOrderLock)
            {
                string orderName = RealOrderService.BuildEntryOrderName();
                try
                {
                    Order entryOrder = account.CreateOrder(instrument, orderAction, OrderType.Market, orderEntry, TimeInForce.Day, quantity, 0, 0, "", orderName, Core.Globals.MaxDate, null);
                    if (HasATMStrategy())
                    {
                        NinjaTrader.NinjaScript.AtmStrategy.StartAtmStrategy(atmStrategyName, entryOrder);
                    }
                    account.Submit(new[] { entryOrder });
                }
                catch (Exception ex)
                {
                    RealLogger.PrintOutput("Exception in SubmitMarketOrder:" + ex.Message + " " + ex.StackTrace);  
                }
            }
        }
        private void SubmitMarketOrderChunked(Instrument instrument, OrderAction orderAction, OrderEntry orderEntry, int quantity, bool continueTillZeroRemainingQuantity = true)
        {
            int quantityRemaining = quantity;
            int chunkedQuantity = 0;
            int cycleCount = 1;
            lock (MarketOrderLock)
            {
                if (quantityRemaining > 0)
                {
                    lock (MarketOrderLock)
                    {
                        while (quantityRemaining > 0)
                        {
                            if (quantityRemaining > this.SingleOrderChunkMaxQuantity)
                            {
                                int randomQuantity = GetRandomNumber(this.SingleOrderChunkMaxQuantity);
                                chunkedQuantity = randomQuantity;
                            }
                            else if (quantityRemaining > this.SingleOrderChunkMinQuantity)
                            {
                                int randomQuantity = GetRandomNumber(this.SingleOrderChunkMinQuantity);
                                chunkedQuantity = randomQuantity;
                            }
                            else
                            {
                                chunkedQuantity = quantityRemaining;
                            }
                            quantityRemaining -= chunkedQuantity;
                            if (cycleCount > 1 && SingleOrderChunkDelayMilliseconds > 0) Thread.Sleep(SingleOrderChunkDelayMilliseconds);
                            string orderName = RealOrderService.BuildExitOrderName();
                            try
                            {
                                Order exitOrder = account.CreateOrder(instrument, orderAction, OrderType.Market, orderEntry, TimeInForce.Day, chunkedQuantity, 0, 0, "", orderName, Core.Globals.MaxDate, null);
                                account.Submit(new[] { exitOrder });
                            }
                            catch (Exception ex)
                            {
                                RealLogger.PrintOutput("Exception in SubmitMarketOrder:" + ex.Message + " " + ex.StackTrace);  
                            }
                            cycleCount++;
                            if (!continueTillZeroRemainingQuantity) break;
                        }
                    }
                }
            }
        }
        private int CalculateAutoEntryVolume(JoiaGestorEntryVolumeAutoTypes entryVolumeAutoType)
        {
            int entryVolume = AutoEntryVolumeOption1;
            if (entryVolumeAutoType == JoiaGestorEntryVolumeAutoTypes.Option2)
            {
                entryVolume = AutoEntryVolumeOption2;
            }
            else if (entryVolumeAutoType == JoiaGestorEntryVolumeAutoTypes.Option3)
            {
                entryVolume = AutoEntryVolumeOption3;
            }
            else if (entryVolumeAutoType == JoiaGestorEntryVolumeAutoTypes.Option4)
            {
                entryVolume = AutoEntryVolumeOption4;
            }
            else if (entryVolumeAutoType == JoiaGestorEntryVolumeAutoTypes.Option5)
            {
                entryVolume = AutoEntryVolumeOption5;
            }
            return entryVolume;
        }
        private void GenerateEntryVolumeAutoButtonText()
        {
            const string AutoEntryButtonPrefix = "V(";
            const string AutoEntryButtonSuffix = ")";
            const string AutoEntryButtonToolTipPrefix = "Volume (";
            ToggleAutoEntryVolOption1ButtonEnabledText = AutoEntryButtonPrefix + AutoEntryVolumeOption1 + AutoEntryButtonSuffix;
            ToggleAutoEntryVolOption1ButtonEnabledToolTip = AutoEntryButtonToolTipPrefix + AutoEntryVolumeOption1 + AutoEntryButtonSuffix;
            ToggleAutoEntryVolOption2ButtonEnabledText = AutoEntryButtonPrefix + AutoEntryVolumeOption2 + AutoEntryButtonSuffix;
            ToggleAutoEntryVolOption2ButtonEnabledToolTip = AutoEntryButtonToolTipPrefix + AutoEntryVolumeOption2 + AutoEntryButtonSuffix;
            ToggleAutoEntryVolOption3ButtonEnabledText = AutoEntryButtonPrefix + AutoEntryVolumeOption3 + AutoEntryButtonSuffix;
            ToggleAutoEntryVolOption3ButtonEnabledToolTip = AutoEntryButtonToolTipPrefix + AutoEntryVolumeOption3 + AutoEntryButtonSuffix;
            ToggleAutoEntryVolOption4ButtonEnabledText = AutoEntryButtonPrefix + AutoEntryVolumeOption4 + AutoEntryButtonSuffix;
            ToggleAutoEntryVolOption4ButtonEnabledToolTip = AutoEntryButtonToolTipPrefix + AutoEntryVolumeOption4 + AutoEntryButtonSuffix;
            ToggleAutoEntryVolOption5ButtonEnabledText = AutoEntryButtonPrefix + AutoEntryVolumeOption5 + AutoEntryButtonSuffix;
            ToggleAutoEntryVolOption5ButtonEnabledToolTip = AutoEntryButtonToolTipPrefix + AutoEntryVolumeOption5 + AutoEntryButtonSuffix;
        }
        
        private bool IsAccountFlat()
        {
            bool returnFlag = true;
            returnFlag = (RealPositionService.PositionCount == 0);
            return returnFlag;
        }
        private bool IsAccountFlat(Instrument instrument)
        {
            bool returnFlag = true;
            RealPosition position = new RealPosition();
            returnFlag = (!RealPositionService.TryGetByInstrumentFullName(instrument.FullName, out position));
            return returnFlag;
        }
        private void CloseAllAccountPendingOrders(string signalName, Instrument limitToSingleInstrument)
        {
            int orderCount = RealOrderService.OrderCount;
            for (int index = 0; index < orderCount; index++)
            {
                RealOrder order = null;
                if (RealOrderService.TryGetByIndex(index, out order))
                {
                    if (limitToSingleInstrument == null || order.Instrument == limitToSingleInstrument)
                    {
                        if (!Order.IsTerminalState(order.OrderState))
                        {
                            if (DebugLogLevel > 0) RealLogger.PrintOutput(signalName + " is cancelling pending order " + order.Instrument.FullName + " Type=" + order.OrderType.ToString());
                            Order foundNTOrder = GetNinjaTraderOrder(order);
                            if (foundNTOrder != null)
                            {
                                try
                                {
                                    account.Cancel(new[] { foundNTOrder });
                                }
                                catch (Exception ex)
                                {
                                    RealLogger.PrintOutput("Exception in CloseAllAccountPendingOrders:" + ex.Message + " " + ex.StackTrace);  
                                }
                            }
                        }
                    }
                }
            }
        }
        protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
        {
            try
            {
                base.OnRender(chartControl, chartScale);
                if (!hasDrawnButtons)
                {
                    if (IsStrategyAttachedToChart() && HasRanOnceFirstCycle())
                    {
                        DrawButtonPanel();
                        SetButtonPanelVisiblity();
                        hasDrawnButtons = true;
                    }
                }
            }
            catch (Exception ex)
            {
                RealLogger.PrintOutput("Exception in OnRender:" + ex.Message + " " + ex.StackTrace);  
                throw;
            }
        }
        private bool HasRanOnceFirstCycle()
        {
            if (!hasRanOnceFirstCycle && attachedInstrumentServerSupported && BarsInProgress == 0 && CurrentBar > 0) 
            {
                this.RealOrderService = new RealOrderService();
                this.RealPositionService = new RealPositionService();
                lastOrderOutputTime = DateTime.MinValue;
                LoadAccount();
                if (account != null)
                {
                    LoadATMStrategy();
                    LoadPositions();
                    if (IsDayOverAccountBalanceFloorEnabled())
                    {
                        RealLogger.PrintOutput("Day Over Account Balance Floor: $" + DayOverAccountBalanceFloorDollars.ToString("N2"), PrintTo.OutputTab1);
                        RealLogger.PrintOutput("Day Over Account Balance Floor: $" + DayOverAccountBalanceFloorDollars.ToString("N2"), PrintTo.OutputTab2);
                    }
                    if (IsDayOverMaxLossEnabled())
                    {
                        RealLogger.PrintOutput("Day Over Daily Max Loss: $" + dayOverMaxLossDollars.ToString("N2"), PrintTo.OutputTab1);
                        RealLogger.PrintOutput("Day Over Daily Max Loss: $" + dayOverMaxLossDollars.ToString("N2"), PrintTo.OutputTab2);
                    }
                    if (IsMaxDDStopLossEnabled())
                    {
                        RealLogger.PrintOutput("Max DD: $" + maxDDInDollars.ToString("N2"), PrintTo.OutputTab1);
                        RealLogger.PrintOutput("Max DD: $" + maxDDInDollars.ToString("N2"), PrintTo.OutputTab2);
                    }
                    RealLogger.PrintOutput("Detected commission per side: " + GetCommissionPerSide(attachedInstrument).ToString("N2") + " for " + attachedInstrument.FullName, PrintTo.OutputTab1);
                    RealLogger.PrintOutput("Detected commission per side: " + GetCommissionPerSide(attachedInstrument).ToString("N2") + " for " + attachedInstrument.FullName, PrintTo.OutputTab2);
                    attachedInstrumentPositionMaxVolume = GetMaxPositionSize(attachedInstrument);
                    RealLogger.PrintOutput("Detected position max volume: " + attachedInstrumentPositionMaxVolume.ToString("N0") + " for " + attachedInstrument.FullName, PrintTo.OutputTab1);
                    if (UseHedgehogEntry && attachedInstrumentIsFuture)
                    {
                        RealLogger.PrintOutput("Validating HedgehogEntrySymbol1...", PrintTo.OutputTab2);
                        ValidateInstrument(HedgehogEntrySymbol1FullName);
                        RealLogger.PrintOutput("Validating HedgehogEntrySymbol2...", PrintTo.OutputTab2);
                        ValidateInstrument(HedgehogEntrySymbol2FullName);
                    }
                    hasRanOnceFirstCycle = true;
                }
            }
            return hasRanOnceFirstCycle;
        }
        private string GetATMStrategy()
        {
            string tempATMStrategyName = null;
            try
            {
                AtmStrategy atmStrategy = this.ChartControl.OwnerChart.ChartTrader.AtmStrategy;
                if (atmStrategy != null)
                {
                    tempATMStrategyName = atmStrategy.Template + "";
                }
            }
            catch (Exception ex)
            {
            }
            return tempATMStrategyName;
        }
        private void LoadATMStrategy(string newATMStrategyName = "")
        {
            {
                string tempATMStrategyName = newATMStrategyName;
                if (tempATMStrategyName == string.Empty)
                {
                    tempATMStrategyName = GetATMStrategy();
                }
                else
                {
                    tempATMStrategyName = newATMStrategyName;
                }
                if (tempATMStrategyName != atmStrategyName)
                {
                    atmStrategyName = tempATMStrategyName;
                    string displayATMStrategyName = (string.IsNullOrEmpty(atmStrategyName)) ? "None" : atmStrategyName;
                    RealLogger.PrintOutput("Found ATM strategy name (" + displayATMStrategyName + ")", PrintTo.OutputTab1);
                    RealLogger.PrintOutput("Found ATM strategy name (" + displayATMStrategyName + ")", PrintTo.OutputTab2);
                }
            }
        }
        private Account GetAccount()
        {
            Account tempAccount = null;
            try
            {
                tempAccount = this.ChartControl.OwnerChart.ChartTrader.Account;
            }
            catch
            {
            }
            return tempAccount;
        }
        private void LoadAccount(Account newAccount = null)
        {
            {
                Account tempAccount = newAccount;
                if (newAccount == null)
                {
                    tempAccount = GetAccount();
                }
                else
                {
                    tempAccount = newAccount;
                }
                if (tempAccount != null && tempAccount != account)
                {
                    if (account != null) UnloadAccountEvents();
                    account = tempAccount;
                    RealLogger.PrintOutput("Found account name (" + Convert.ToString(account.DisplayName) + ")", PrintTo.OutputTab1);
                    RealLogger.PrintOutput("Found account name (" + Convert.ToString(account.DisplayName) + ")", PrintTo.OutputTab2);
                    if (!subscribedToOnOrderUpdate)
                    {
                        if (this.DebugLogLevel > 10) RealLogger.PrintOutput("*** LoadAccount: Subscribing to OrderUpdate:");
                        WeakEventManager<Account, OrderEventArgs>.AddHandler(account, "OrderUpdate", OnOrderUpdate);
                        subscribedToOnOrderUpdate = true;
                    }
                }
                else if (tempAccount == null)
                {
                    RealLogger.PrintOutput("Account name not found.", PrintTo.OutputTab1);
                    RealLogger.PrintOutput("Account name not found.", PrintTo.OutputTab2);
                }
            }
        }
        private void SetButtonPanelVisiblity()
        {
            if (buttonGrid != null)
            {
                buttonGrid.Background = Brushes.Black;
                if (thLayoutGrid != null)
                {
                    thLayoutGrid.Visibility = Visibility.Visible;
                }
                if (labelGrid != null)
                {
                    labelGrid.Visibility = Visibility.Visible;
                }
                if (riskInfoLabel != null)
                {
                    riskInfoLabel.Visibility = Visibility.Visible;
                }
                if (profitInfoLabel != null)
                {
                    profitInfoLabel.Visibility = Visibility.Visible;
                }
                if (bogeyTargetInfoLabel != null)
                {
                    bogeyTargetInfoLabel.Visibility = Visibility.Visible;
                }
                if (dayOverMaxLossInfoLabel != null)
                {
                    dayOverMaxLossInfoLabel.Visibility = Visibility.Visible;
                }
                if (revButton != null && ShowButtonReverse)
                {
                    revButton.Visibility = Visibility.Visible;
                }
                if (closeAllButton != null && ShowButtonClose)
                {
                    closeAllButton.Visibility = Visibility.Visible;
                }
                if (toggleAutoCloseButton != null && ShowButtonAutoClose)
                {
                    toggleAutoCloseButton.Visibility = Visibility.Visible;
                }
                if (toggleAutoBEButton != null && ShowButtonAutoBreakEven)
                {
                    toggleAutoBEButton.Visibility = Visibility.Visible;
                }
                if (TPButton != null && ShowButtonTPPlus)
                {
                    TPButton.Visibility = Visibility.Visible;
                }
                if (BEButton != null && ShowButtonBEPlus)
                {
                    BEButton.Visibility = Visibility.Visible;
                }
                if (SLButton != null && ShowButtonSLPlus)
                {
                    SLButton.Visibility = Visibility.Visible;
                }
                if (BuyMarketButton != null && ShowButtonBuyMarket)
                {
                    BuyMarketButton.Visibility = Visibility.Visible;
                }
                if (SellMarketButton != null && ShowButtonSellMarket)
                {
                    SellMarketButton.Visibility = Visibility.Visible;
                }
                if (toggleEntryVolumeAutoButton != null && ShowButtonVolume)
                {
                    toggleEntryVolumeAutoButton.Visibility = Visibility.Visible;
                }
            }
        }
        private void SetButtonPanelHidden()
        {
            if (buttonGrid != null)
            {
                buttonGrid.Background = Brushes.Transparent;
                if (thLayoutGrid != null)
                {
                    thLayoutGrid.Visibility = Visibility.Visible;
                }
                if (labelGrid != null)
                {
                    labelGrid.Visibility = Visibility.Hidden;
                }
                if (riskInfoLabel != null)
                {
                    riskInfoLabel.Visibility = Visibility.Hidden;
                }
                if (profitInfoLabel != null)
                {
                    profitInfoLabel.Visibility = Visibility.Hidden;
                }
                if (bogeyTargetInfoLabel != null)
                {
                    bogeyTargetInfoLabel.Visibility = Visibility.Hidden;
                }
                if (dayOverMaxLossInfoLabel != null)
                {
                    dayOverMaxLossInfoLabel.Visibility = Visibility.Hidden;
                }
                if (revButton != null)
                {
                    revButton.Visibility = Visibility.Hidden;
                }
                if (closeAllButton != null)
                {
                    closeAllButton.Visibility = Visibility.Hidden;
                }
                if (toggleAutoCloseButton != null)
                {
                    toggleAutoCloseButton.Visibility = Visibility.Hidden;
                }
                if (toggleAutoBEButton != null)
                {
                    toggleAutoBEButton.Visibility = Visibility.Hidden;
                }
                if (toggleBogeyTargetButton != null)
                {
                    toggleBogeyTargetButton.Visibility = Visibility.Hidden;
                }
                if (TPButton != null)
                {
                    TPButton.Visibility = Visibility.Hidden;
                }
                if (BEButton != null)
                {
                    BEButton.Visibility = Visibility.Hidden;
                }
                if (SLButton != null)
                {
                    SLButton.Visibility = Visibility.Hidden;
                }
                if (BuyPopButton != null)
                {
                    BuyPopButton.Visibility = Visibility.Hidden;
                }
                if (SellPopButton != null)
                {
                    SellPopButton.Visibility = Visibility.Hidden;
                }
                if (BuyDropButton != null)
                {
                    BuyDropButton.Visibility = Visibility.Hidden;
                }
                if (SellDropButton != null)
                {
                    SellDropButton.Visibility = Visibility.Hidden;
                }
                if (BuyMarketButton != null)
                {
                    BuyMarketButton.Visibility = Visibility.Hidden;
                }
                if (SellMarketButton != null)
                {
                    SellMarketButton.Visibility = Visibility.Hidden;
                }
                if (toggleAutoAddOnButton != null)
                {
                    toggleAutoAddOnButton.Visibility = Visibility.Hidden;
                }
                if (toggleAutoPilotButton != null)
                {
                    toggleAutoPilotButton.Visibility = Visibility.Hidden;
                }
                if (toggleTradeSignalButton != null)
                {
                    toggleTradeSignalButton.Visibility = Visibility.Hidden;
                }
                if (toggleEntryVolumeAutoButton != null)
                {
                    toggleEntryVolumeAutoButton.Visibility = Visibility.Hidden;
                }
            }
        }
        private bool IsStrategyAttachedToChart()
        {
            return (this.ChartBars != null);
        }
        private void RemoveButtonPanel()
        {
            if (buttonGrid != null)
            {
                if (UserControlCollection.Contains(buttonGrid))
                {
                    if (revButton != null)
                    {
                        WeakEventManager<System.Windows.Controls.Button, RoutedEventArgs>.RemoveHandler(revButton, "Click", OnButtonClick);
                        buttonGrid.Children.Remove(revButton);
                        revButton = null;
                    }
                    if (closeAllButton != null)
                    {
                        WeakEventManager<System.Windows.Controls.Button, RoutedEventArgs>.RemoveHandler(closeAllButton, "Click", OnButtonClick);
                        buttonGrid.Children.Remove(closeAllButton);
                        closeAllButton = null;
                    }
                    if (toggleAutoCloseButton != null)
                    {
                        WeakEventManager<System.Windows.Controls.Button, RoutedEventArgs>.RemoveHandler(toggleAutoCloseButton, "Click", OnButtonClick);
                        buttonGrid.Children.Remove(toggleAutoCloseButton);
                        toggleAutoCloseButton = null;
                    }
                    if (toggleAutoBEButton != null)
                    {
                        WeakEventManager<System.Windows.Controls.Button, RoutedEventArgs>.RemoveHandler(toggleAutoBEButton, "Click", OnButtonClick);
                        buttonGrid.Children.Remove(toggleAutoBEButton);
                        toggleAutoBEButton = null;
                    }
                    if (toggleBogeyTargetButton != null)
                    {
                        WeakEventManager<System.Windows.Controls.Button, RoutedEventArgs>.RemoveHandler(toggleBogeyTargetButton, "Click", OnButtonClick);
                        buttonGrid.Children.Remove(toggleBogeyTargetButton);
                        toggleBogeyTargetButton = null;
                    }
                    if (toggleEntryVolumeAutoButton != null)
                    {
                        WeakEventManager<System.Windows.Controls.Button, RoutedEventArgs>.RemoveHandler(toggleEntryVolumeAutoButton, "Click", OnButtonClick);
                        buttonGrid.Children.Remove(toggleEntryVolumeAutoButton);
                        toggleEntryVolumeAutoButton = null;
                    }
                    if (toggleAutoAddOnButton != null)
                    {
                        WeakEventManager<System.Windows.Controls.Button, RoutedEventArgs>.RemoveHandler(toggleAutoAddOnButton, "Click", OnButtonClick);
                        buttonGrid.Children.Remove(toggleAutoAddOnButton);
                        toggleAutoAddOnButton = null;
                    }
                    if (toggleTradeSignalButton != null)
                    {
                        WeakEventManager<System.Windows.Controls.Button, RoutedEventArgs>.RemoveHandler(toggleTradeSignalButton, "Click", OnButtonClick);
                        buttonGrid.Children.Remove(toggleTradeSignalButton);
                        toggleTradeSignalButton = null;
                    }
                    if (toggleAutoPilotButton != null)
                    {
                        WeakEventManager<System.Windows.Controls.Button, RoutedEventArgs>.RemoveHandler(toggleAutoPilotButton, "Click", OnButtonClick);
                        buttonGrid.Children.Remove(toggleAutoPilotButton);
                        toggleAutoPilotButton = null;
                    }
                    if (TPButton != null)
                    {
                        WeakEventManager<System.Windows.Controls.Button, RoutedEventArgs>.RemoveHandler(TPButton, "Click", OnButtonClick);
                        buttonGrid.Children.Remove(TPButton);
                        TPButton = null;
                    }
                    if (BEButton != null)
                    {
                        WeakEventManager<System.Windows.Controls.Button, RoutedEventArgs>.RemoveHandler(BEButton, "Click", OnButtonClick);
                        buttonGrid.Children.Remove(BEButton);
                        BEButton = null;
                    }
                    if (SLButton != null)
                    {
                        WeakEventManager<System.Windows.Controls.Button, RoutedEventArgs>.RemoveHandler(SLButton, "Click", OnButtonClick);
                        buttonGrid.Children.Remove(SLButton);
                        SLButton = null;
                    }
                    if (BuyPopButton != null)
                    {
                        WeakEventManager<System.Windows.Controls.Button, RoutedEventArgs>.RemoveHandler(BuyPopButton, "Click", OnButtonClick);
                        buttonGrid.Children.Remove(BuyPopButton);
                        BuyPopButton = null;
                    }
                    if (SellPopButton != null)
                    {
                        WeakEventManager<System.Windows.Controls.Button, RoutedEventArgs>.RemoveHandler(SellPopButton, "Click", OnButtonClick);
                        buttonGrid.Children.Remove(SellPopButton);
                        SellPopButton = null;
                    }
                    if (BuyDropButton != null)
                    {
                        WeakEventManager<System.Windows.Controls.Button, RoutedEventArgs>.RemoveHandler(BuyDropButton, "Click", OnButtonClick);
                        buttonGrid.Children.Remove(BuyDropButton);
                        BuyDropButton = null;
                    }
                    if (SellDropButton != null)
                    {
                        WeakEventManager<System.Windows.Controls.Button, RoutedEventArgs>.RemoveHandler(SellDropButton, "Click", OnButtonClick);
                        buttonGrid.Children.Remove(SellDropButton);
                        SellDropButton = null;
                    }
                    if (BuyMarketButton != null)
                    {
                        WeakEventManager<System.Windows.Controls.Button, RoutedEventArgs>.RemoveHandler(BuyMarketButton, "Click", OnButtonClick);
                        buttonGrid.Children.Remove(BuyMarketButton);
                        BuyMarketButton = null;
                    }
                    if (SellMarketButton != null)
                    {
                        WeakEventManager<System.Windows.Controls.Button, RoutedEventArgs>.RemoveHandler(SellMarketButton, "Click", OnButtonClick);
                        buttonGrid.Children.Remove(SellMarketButton);
                        SellMarketButton = null;
                    }
                    thLayoutGrid = null;
                }
            }
        }
        private void DrawButtonPanel()
        {
            if (thLayoutGrid == null)
            {
                if (!UserControlCollection.Contains(thLayoutGrid))
                {
                    thLayoutGrid = new System.Windows.Controls.Grid
                    {
                        Name = "HHTHLayoutGrid",
                        Margin = new Thickness(0, 0, 40, 0),
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Top
                    };
                    System.Windows.Controls.RowDefinition thLayoutRow1 = new System.Windows.Controls.RowDefinition();
                    System.Windows.Controls.RowDefinition thLayoutRow2 = new System.Windows.Controls.RowDefinition();
                    thLayoutGrid.RowDefinitions.Add(thLayoutRow1);
                    thLayoutGrid.RowDefinitions.Add(thLayoutRow2);
                    System.Windows.Controls.ColumnDefinition thLayoutColumn1 = new System.Windows.Controls.ColumnDefinition();
                    thLayoutGrid.ColumnDefinitions.Add(thLayoutColumn1);
                    buttonGrid = new System.Windows.Controls.Grid
                    {
                        Name = "HHButtonGrid",
                        Margin = new Thickness(0, 0, 0, 0),
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Top,
                        Background = Brushes.Black
                    };
                    System.Windows.Controls.RowDefinition row1 = new System.Windows.Controls.RowDefinition();
                    buttonGrid.RowDefinitions.Add(row1);
                    System.Windows.Controls.ColumnDefinition column1 = new System.Windows.Controls.ColumnDefinition();
                    System.Windows.Controls.ColumnDefinition column2 = new System.Windows.Controls.ColumnDefinition();
                    System.Windows.Controls.ColumnDefinition column3 = new System.Windows.Controls.ColumnDefinition();
                    System.Windows.Controls.ColumnDefinition column4 = new System.Windows.Controls.ColumnDefinition();
                    System.Windows.Controls.ColumnDefinition column5 = new System.Windows.Controls.ColumnDefinition();
                    System.Windows.Controls.ColumnDefinition column6 = new System.Windows.Controls.ColumnDefinition();
                    System.Windows.Controls.ColumnDefinition column7 = new System.Windows.Controls.ColumnDefinition();
                    System.Windows.Controls.ColumnDefinition column8 = new System.Windows.Controls.ColumnDefinition();
                    System.Windows.Controls.ColumnDefinition column9 = new System.Windows.Controls.ColumnDefinition();
                    System.Windows.Controls.ColumnDefinition column10 = new System.Windows.Controls.ColumnDefinition();
                    System.Windows.Controls.ColumnDefinition column11 = new System.Windows.Controls.ColumnDefinition();
                    System.Windows.Controls.ColumnDefinition column12 = new System.Windows.Controls.ColumnDefinition();
                    System.Windows.Controls.ColumnDefinition column13 = new System.Windows.Controls.ColumnDefinition();
                    System.Windows.Controls.ColumnDefinition column14 = new System.Windows.Controls.ColumnDefinition();
                    System.Windows.Controls.ColumnDefinition column15 = new System.Windows.Controls.ColumnDefinition();
                    System.Windows.Controls.ColumnDefinition column16 = new System.Windows.Controls.ColumnDefinition();
                    System.Windows.Controls.ColumnDefinition column17 = new System.Windows.Controls.ColumnDefinition();
                    System.Windows.Controls.ColumnDefinition column18 = new System.Windows.Controls.ColumnDefinition();
                    buttonGrid.ColumnDefinitions.Add(column1);
                    buttonGrid.ColumnDefinitions.Add(column2);
                    buttonGrid.ColumnDefinitions.Add(column3);
                    buttonGrid.ColumnDefinitions.Add(column4);
                    buttonGrid.ColumnDefinitions.Add(column5);
                    buttonGrid.ColumnDefinitions.Add(column6);
                    buttonGrid.ColumnDefinitions.Add(column7);
                    buttonGrid.ColumnDefinitions.Add(column8);
                    buttonGrid.ColumnDefinitions.Add(column9);
                    buttonGrid.ColumnDefinitions.Add(column10);
                    buttonGrid.ColumnDefinitions.Add(column11);
                    buttonGrid.ColumnDefinitions.Add(column12);
                    buttonGrid.ColumnDefinitions.Add(column13);
                    buttonGrid.ColumnDefinitions.Add(column14);
                    buttonGrid.ColumnDefinitions.Add(column15);
                    buttonGrid.ColumnDefinitions.Add(column16);
                    buttonGrid.ColumnDefinitions.Add(column17);
                    buttonGrid.ColumnDefinitions.Add(column18);
                    revButton = new System.Windows.Controls.Button
                    {
                        Name = HHRevButtonName,
                        Content = (IsBlendedInstrumentEnabled()) ? ToggleReverseBButtonText : ToggleReverseButtonText,
                        ToolTip = (IsBlendedInstrumentEnabled()) ? ToggleReverseBButtonToolTip : ToggleReverseButtonToolTip,
                        Foreground = Brushes.White,
                        Background = Brushes.DarkOrange,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 2, 0),
                        Visibility = (hasRanOnceFirstCycle) ? Visibility.Visible : Visibility.Hidden,
                    };
                    revButton.Height = revButton.Height / 2;
                    revButton.Width = revButton.Width / 2;
                    //revButton.Content = new System.Windows.Controls.TextBlock { Text = revButton.Content.ToString(), FontSize = revButton.Content.FontSize / 2 }; 
                    string closeButtonText = (currentCloseAutoStatus == JoiaGestorCloseAutoTypes.EquityCloseAllTarget) ? ToggleFlatButtonText : (IsBlendedInstrumentEnabled()) ? ToggleCloseBButtonText : ToggleCloseButtonText;
                    string closeButtonToolTip = (currentCloseAutoStatus == JoiaGestorCloseAutoTypes.EquityCloseAllTarget) ? ToggleFlatButtonToolTip : (IsBlendedInstrumentEnabled()) ? ToggleCloseBButtonToolTip : ToggleCloseButtonToolTip;
                    closeAllButton = new System.Windows.Controls.Button
                    {
                        Name = HHCloseAllButtonName,
                        Content = closeButtonText,
                        ToolTip = closeButtonToolTip,
                        Foreground = Brushes.White,
                        Background = Brushes.Red,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 2, 0),
                        Visibility = (hasRanOnceFirstCycle) ? Visibility.Visible : Visibility.Hidden,
                    };
                    string tempContent;
                    string tempToolTip;
                    if (currentCloseAutoStatus == JoiaGestorCloseAutoTypes.EquityCloseAllTarget)
                    {
                        tempContent = ToggleAutoCloseECAButtonEnabledText;
                        tempToolTip = ToggleAutoCloseECAButtonEnabledToolTip;
                    }
                    else if (currentCloseAutoStatus == JoiaGestorCloseAutoTypes.MovingAverage1Slope)
                    {
                        tempContent = ToggleAutoCloseM1SButtonEnabledText;
                        tempToolTip = ToggleAutoCloseM1SButtonEnabledToolTip;
                    }
                    else if (currentCloseAutoStatus == JoiaGestorCloseAutoTypes.MovingAverage1SlopeMinProfit)
                    {
                        tempContent = ToggleAutoCloseM1SMPButtonEnabledText;
                        tempToolTip = ToggleAutoCloseM1SMPButtonEnabledToolTip;
                    }
                    else if (currentCloseAutoStatus == JoiaGestorCloseAutoTypes.MovingAverage2Slope)
                    {
                        tempContent = ToggleAutoCloseM2SButtonEnabledText;
                        tempToolTip = ToggleAutoCloseM2SButtonEnabledToolTip;
                    }
                    else if (currentCloseAutoStatus == JoiaGestorCloseAutoTypes.MovingAverage2SlopeMinProfit)
                    {
                        tempContent = ToggleAutoCloseM2SMPButtonEnabledText;
                        tempToolTip = ToggleAutoCloseM2SMPButtonEnabledToolTip;
                    }
                    else if (currentCloseAutoStatus == JoiaGestorCloseAutoTypes.MovingAverage3Slope)
                    {
                        tempContent = ToggleAutoCloseM3SButtonEnabledText;
                        tempToolTip = ToggleAutoCloseM3SButtonEnabledToolTip;
                    }
                    else if (currentCloseAutoStatus == JoiaGestorCloseAutoTypes.MovingAverage3SlopeMinProfit)
                    {
                        tempContent = ToggleAutoCloseM3SMPButtonEnabledText;
                        tempToolTip = ToggleAutoCloseM3SMPButtonEnabledToolTip;
                    }
                    else
                    {
                        tempContent = ToggleAutoCloseButtonDisabledText;
                        tempToolTip = ToggleAutoCloseButtonDisabledToolTip;
                    }
                    Brush tempBrush = (currentCloseAutoStatus != JoiaGestorCloseAutoTypes.Disabled) ? Brushes.HotPink : Brushes.DimGray;
                    toggleAutoCloseButton = new System.Windows.Controls.Button
                    {
                        Name = HHToggleAutoCloseButtonName,
                        Content = tempContent,
                        ToolTip = tempToolTip,
                        Foreground = Brushes.White,
                        Background = tempBrush,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 2, 0),
                        Visibility = (hasRanOnceFirstCycle) ? Visibility.Visible : Visibility.Hidden
                    };
                    if (currentBreakEvenAutoStatus == JoiaGestorBreakEvenAutoTypes.Enabled)
                    {
                        tempContent = ToggleAutoBEButtonEnabledText;
                        tempToolTip = ToggleAutoBEButtonEnabledToolTip;
                    }
                    else if (currentBreakEvenAutoStatus == JoiaGestorBreakEvenAutoTypes.HODL)
                    {
                        tempContent = ToggleAutoBEHDLButtonEnabledText;
                        tempToolTip = ToggleAutoBEHDLButtonEnabledToolTip;
                    }
                    else if (currentBreakEvenAutoStatus == JoiaGestorBreakEvenAutoTypes.PlusTrail1Bar)
                    {
                        tempContent = ToggleAutoBET1BButtonEnabledText;
                        tempToolTip = ToggleAutoBET1BButtonEnabledToolTip;
                    }
                    else if (currentBreakEvenAutoStatus == JoiaGestorBreakEvenAutoTypes.PlusTrail2Bar)
                    {
                        tempContent = ToggleAutoBET2BButtonEnabledText;
                        tempToolTip = ToggleAutoBET2BButtonEnabledToolTip;
                    }
                    else if (currentBreakEvenAutoStatus == JoiaGestorBreakEvenAutoTypes.PlusTrail3Bar)
                    {
                        tempContent = ToggleAutoBET3BButtonEnabledText;
                        tempToolTip = ToggleAutoBET3BButtonEnabledToolTip;
                    }
                    else if (currentBreakEvenAutoStatus == JoiaGestorBreakEvenAutoTypes.PlusTrail5Bar)
                    {
                        tempContent = ToggleAutoBET5BButtonEnabledText;
                        tempToolTip = ToggleAutoBET5BButtonEnabledToolTip;
                    }
                    else if (currentBreakEvenAutoStatus == JoiaGestorBreakEvenAutoTypes.PlusTrailMovingAverage1)
                    {
                        tempContent = ToggleAutoBETM1ButtonEnabledText;
                        tempToolTip = ToggleAutoBETM1ButtonEnabledToolTip;
                    }
                    else if (currentBreakEvenAutoStatus == JoiaGestorBreakEvenAutoTypes.PlusTrailMovingAverage2)
                    {
                        tempContent = ToggleAutoBETM2ButtonEnabledText;
                        tempToolTip = ToggleAutoBETM2ButtonEnabledToolTip;
                    }
                    else if (currentBreakEvenAutoStatus == JoiaGestorBreakEvenAutoTypes.PlusTrailMovingAverage3)
                    {
                        tempContent = ToggleAutoBETM3ButtonEnabledText;
                        tempToolTip = ToggleAutoBETM3ButtonEnabledToolTip;
                    }
                    else
                    {
                        tempContent = ToggleAutoBEButtonDisabledText;
                        tempToolTip = ToggleAutoBEButtonDisabledToolTip;
                    }
                    tempBrush = (currentBreakEvenAutoStatus != JoiaGestorBreakEvenAutoTypes.Disabled) ? Brushes.HotPink : Brushes.DimGray;
                    toggleAutoBEButton = new System.Windows.Controls.Button
                    {
                        Name = HHToggleAutoBEButtonName,
                        Content = tempContent,
                        ToolTip = tempToolTip,
                        Foreground = Brushes.White,
                        Background = tempBrush,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 2, 0),
                        Visibility = (hasRanOnceFirstCycle) ? Visibility.Visible : Visibility.Hidden
                    };
                    toggleBogeyTargetButton = new System.Windows.Controls.Button
                    {
                        Name = HHToggleBogeyTargetButtonName,
                        Content = tempContent,
                        ToolTip = tempToolTip,
                        Foreground = Brushes.White,
                        Background = tempBrush,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 2, 0),
                        Visibility = (hasRanOnceFirstCycle) ? Visibility.Visible : Visibility.Hidden
                    };
                    TPButton = new System.Windows.Controls.Button
                    {
                        Name = HHTPButtonName,
                        Content = ToggleTPButtonText,
                        ToolTip = ToggleTPButtonToolTip,
                        Foreground = Brushes.White,
                        Background = Brushes.Silver,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 2, 0),
                        Visibility = (hasRanOnceFirstCycle) ? Visibility.Visible : Visibility.Hidden
                    };
                    BEButton = new System.Windows.Controls.Button
                    {
                        Name = HHBEButtonName,
                        Content = ToggleBEButtonText,
                        ToolTip = ToggleBEButtonToolTip,
                        Foreground = Brushes.White,
                        Background = Brushes.DarkCyan,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 2, 0),
                        Visibility = (hasRanOnceFirstCycle) ? Visibility.Visible : Visibility.Hidden
                    };
                    SLButton = new System.Windows.Controls.Button
                    {
                        Name = HHSLButtonName,
                        Content = ToggleSLButtonText,
                        ToolTip = ToggleSLButtonToolTip,
                        Foreground = Brushes.White,
                        Background = Brushes.Silver,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 2, 0),
                        Visibility = (hasRanOnceFirstCycle) ? Visibility.Visible : Visibility.Hidden
                    };
                    BuyDropButton = new System.Windows.Controls.Button
                    {
                        Name = HHBuyDropButtonName,
                        Content = ToggleDropBuyButtonText,
                        ToolTip = ToggleDropBuyButtonToolTip,
                        Foreground = Brushes.White,
                        Background = Brushes.Blue,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 2, 0),
                        Visibility = (hasRanOnceFirstCycle) ? Visibility.Visible : Visibility.Hidden
                    };
                    SellDropButton = new System.Windows.Controls.Button
                    {
                        Name = HHSellDropButtonName,
                        Content = ToggleDropSellButtonText,
                        ToolTip = ToggleDropSellButtonToolTip,
                        Foreground = Brushes.White,
                        Background = Brushes.OrangeRed,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 2, 0),
                        Visibility = (hasRanOnceFirstCycle) ? Visibility.Visible : Visibility.Hidden
                    };
                    BuyPopButton = new System.Windows.Controls.Button
                    {
                        Name = HHBuyPopButtonName,
                        Content = TogglePopBuyButtonText,
                        ToolTip = TogglePopBuyButtonToolTip,
                        Foreground = Brushes.White,
                        Background = Brushes.Blue,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 2, 0),
                        Visibility = (hasRanOnceFirstCycle) ? Visibility.Visible : Visibility.Hidden
                    };
                    SellPopButton = new System.Windows.Controls.Button
                    {
                        Name = HHSellPopButtonName,
                        Content = TogglePopSellButtonText,
                        ToolTip = TogglePopSellButtonToolTip,
                        Foreground = Brushes.White,
                        Background = Brushes.OrangeRed,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 2, 0),
                        Visibility = (hasRanOnceFirstCycle) ? Visibility.Visible : Visibility.Hidden
                    };
                    BuyMarketButton = new System.Windows.Controls.Button
                    {
                        Name = HHBuyMarketButtonName,
                        Content = ToggleBuyMarketButtonText,
                        ToolTip = ToggleBuyMarketButtonToolTip,
                        Foreground = Brushes.White,
                        Background = Brushes.Green,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 2, 0),
                        Visibility = (hasRanOnceFirstCycle) ? Visibility.Visible : Visibility.Hidden
                    };
                    SellMarketButton = new System.Windows.Controls.Button
                    {
                        Name = HHSellMarketButtonName,
                        Content = ToggleSellMarketButtonText,
                        ToolTip = ToggleSellMarketButtonToolTip,
                        Foreground = Brushes.White,
                        Background = Brushes.Red,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 2, 0),
                        Visibility = (hasRanOnceFirstCycle) ? Visibility.Visible : Visibility.Hidden
                    };
                    if (currentEntryVolumeAutoStatus == JoiaGestorEntryVolumeAutoTypes.Option2)
                    {
                        tempContent = ToggleAutoEntryVolOption2ButtonEnabledText;
                        tempToolTip = ToggleAutoEntryVolOption2ButtonEnabledToolTip;
                    }
                    else if (currentEntryVolumeAutoStatus == JoiaGestorEntryVolumeAutoTypes.Option3)
                    {
                        tempContent = ToggleAutoEntryVolOption3ButtonEnabledText;
                        tempToolTip = ToggleAutoEntryVolOption3ButtonEnabledToolTip;
                    }
                    else if (currentEntryVolumeAutoStatus == JoiaGestorEntryVolumeAutoTypes.Option4)
                    {
                        tempContent = ToggleAutoEntryVolOption4ButtonEnabledText;
                        tempToolTip = ToggleAutoEntryVolOption4ButtonEnabledToolTip;
                    }
                    else if (currentEntryVolumeAutoStatus == JoiaGestorEntryVolumeAutoTypes.Option5)
                    {
                        tempContent = ToggleAutoEntryVolOption5ButtonEnabledText;
                        tempToolTip = ToggleAutoEntryVolOption5ButtonEnabledToolTip;
                    }
                    else
                    {
                        tempContent = ToggleAutoEntryVolOption1ButtonEnabledText;
                        tempToolTip = ToggleAutoEntryVolOption1ButtonEnabledToolTip;
                    }
                    tempBrush = Brushes.HotPink;
                    toggleEntryVolumeAutoButton = new System.Windows.Controls.Button
                    {
                        Name = HHToggleEntryVolumeAutoButtonName,
                        Content = tempContent,
                        ToolTip = tempToolTip,
                        Foreground = Brushes.White,
                        Background = tempBrush,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 2, 0),
                        Visibility = (hasRanOnceFirstCycle) ? Visibility.Visible : Visibility.Hidden
                    };
                    toggleAutoAddOnButton = new System.Windows.Controls.Button
                    {
                        Name = HHToggleAutoAddOnButtonName,
                        Content = tempContent,
                        ToolTip = tempToolTip,
                        Foreground = Brushes.White,
                        Background = tempBrush,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 2, 0),
                        Visibility = (hasRanOnceFirstCycle) ? Visibility.Visible : Visibility.Hidden
                    };
                    toggleTradeSignalButton = new System.Windows.Controls.Button
                    {
                        Name = HHToggleTradeSignalButtonName,
                        Content = tempContent,
                        ToolTip = tempToolTip,
                        Foreground = Brushes.White,
                        Background = tempBrush,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 2, 0),
                        Visibility = (hasRanOnceFirstCycle) ? Visibility.Visible : Visibility.Hidden
                    };
                    WeakEventManager<System.Windows.Controls.Button, RoutedEventArgs>.AddHandler(closeAllButton, "Click", OnButtonClick);
                    WeakEventManager<System.Windows.Controls.Button, RoutedEventArgs>.AddHandler(revButton, "Click", OnButtonClick);
                    WeakEventManager<System.Windows.Controls.Button, RoutedEventArgs>.AddHandler(toggleAutoCloseButton, "Click", OnButtonClick);
                    WeakEventManager<System.Windows.Controls.Button, RoutedEventArgs>.AddHandler(toggleAutoBEButton, "Click", OnButtonClick);
                    WeakEventManager<System.Windows.Controls.Button, RoutedEventArgs>.AddHandler(toggleBogeyTargetButton, "Click", OnButtonClick);
                    WeakEventManager<System.Windows.Controls.Button, RoutedEventArgs>.AddHandler(TPButton, "Click", OnButtonClick);
                    WeakEventManager<System.Windows.Controls.Button, RoutedEventArgs>.AddHandler(BEButton, "Click", OnButtonClick);
                    WeakEventManager<System.Windows.Controls.Button, RoutedEventArgs>.AddHandler(SLButton, "Click", OnButtonClick);
                    WeakEventManager<System.Windows.Controls.Button, RoutedEventArgs>.AddHandler(BuyDropButton, "Click", OnButtonClick);
                    WeakEventManager<System.Windows.Controls.Button, RoutedEventArgs>.AddHandler(SellDropButton, "Click", OnButtonClick);
                    WeakEventManager<System.Windows.Controls.Button, RoutedEventArgs>.AddHandler(BuyPopButton, "Click", OnButtonClick);
                    WeakEventManager<System.Windows.Controls.Button, RoutedEventArgs>.AddHandler(SellPopButton, "Click", OnButtonClick);
                    WeakEventManager<System.Windows.Controls.Button, RoutedEventArgs>.AddHandler(BuyMarketButton, "Click", OnButtonClick);
                    WeakEventManager<System.Windows.Controls.Button, RoutedEventArgs>.AddHandler(SellMarketButton, "Click", OnButtonClick);
                    WeakEventManager<System.Windows.Controls.Button, RoutedEventArgs>.AddHandler(toggleEntryVolumeAutoButton, "Click", OnButtonClick);
                    WeakEventManager<System.Windows.Controls.Button, RoutedEventArgs>.AddHandler(toggleAutoAddOnButton, "Click", OnButtonClick);
                    WeakEventManager<System.Windows.Controls.Button, RoutedEventArgs>.AddHandler(toggleTradeSignalButton, "Click", OnButtonClick);
                    WeakEventManager<System.Windows.Controls.Button, RoutedEventArgs>.AddHandler(toggleAutoPilotButton, "Click", OnButtonClick);
                    int gridColumnIndex = buttonGrid.ColumnDefinitions.Count -1;
                    if (ShowButtonVolume)
                    {
                        System.Windows.Controls.Grid.SetColumn(toggleEntryVolumeAutoButton, gridColumnIndex);
                        System.Windows.Controls.Grid.SetRow(toggleEntryVolumeAutoButton, 0);
                        buttonGrid.ColumnDefinitions[gridColumnIndex].MinWidth = 70;
                        buttonGrid.Children.Add(toggleEntryVolumeAutoButton);
                    }
                    else
                    {
                        System.Windows.Controls.Grid.SetColumn(toggleEntryVolumeAutoButton, gridColumnIndex);
                        System.Windows.Controls.Grid.SetRow(toggleEntryVolumeAutoButton, 0);
                        toggleEntryVolumeAutoButton.Visibility = Visibility.Hidden;
                    }
                    if (ShowButtonVolume) gridColumnIndex--;
                    if (ShowButtonSellMarket)
                    {
                        System.Windows.Controls.Grid.SetColumn(SellMarketButton, gridColumnIndex);
                        System.Windows.Controls.Grid.SetRow(SellMarketButton, 0);
                        buttonGrid.ColumnDefinitions[gridColumnIndex].MinWidth = 70;
                        buttonGrid.Children.Add(SellMarketButton);
                    }
                    else
                    {
                        System.Windows.Controls.Grid.SetColumn(SellMarketButton, gridColumnIndex);
                        System.Windows.Controls.Grid.SetRow(SellMarketButton, 0);
                        SellMarketButton.Visibility = Visibility.Hidden;
                    }
                    if (ShowButtonSellMarket) gridColumnIndex--;
                    if (ShowButtonBuyMarket)
                    {
                        System.Windows.Controls.Grid.SetColumn(BuyMarketButton, gridColumnIndex);
                        System.Windows.Controls.Grid.SetRow(BuyMarketButton, 0);
                        buttonGrid.ColumnDefinitions[gridColumnIndex].MinWidth = 70;
                        buttonGrid.Children.Add(BuyMarketButton);
                    }
                    else
                    {
                        System.Windows.Controls.Grid.SetColumn(BuyMarketButton, gridColumnIndex);
                        System.Windows.Controls.Grid.SetRow(BuyMarketButton, 0);
                        BuyMarketButton.Visibility = Visibility.Hidden;
                    }
                    if (ShowButtonBuyMarket) gridColumnIndex--;
                    if (ShowButtonSLPlus)
                    {
                        System.Windows.Controls.Grid.SetColumn(SLButton, gridColumnIndex);
                        System.Windows.Controls.Grid.SetRow(SLButton, 0);
                        buttonGrid.ColumnDefinitions[gridColumnIndex].MinWidth = 70;
                        buttonGrid.Children.Add(SLButton);
                    }
                    else
                    {
                        System.Windows.Controls.Grid.SetColumn(SLButton, gridColumnIndex);
                        System.Windows.Controls.Grid.SetRow(SLButton, 0);
                        SLButton.Visibility = Visibility.Hidden;
                    }
                    if (ShowButtonSLPlus) gridColumnIndex--;
                    if (ShowButtonBEPlus)
                    {
                        System.Windows.Controls.Grid.SetColumn(BEButton, gridColumnIndex);
                        System.Windows.Controls.Grid.SetRow(BEButton, 0);
                        buttonGrid.ColumnDefinitions[gridColumnIndex].MinWidth = 70;
                        buttonGrid.Children.Add(BEButton);
                    }
                    else
                    {
                        System.Windows.Controls.Grid.SetColumn(BEButton, gridColumnIndex);
                        System.Windows.Controls.Grid.SetRow(BEButton, 0);
                        BEButton.Visibility = Visibility.Hidden;
                    }
                    if (ShowButtonBEPlus) gridColumnIndex--;
                    if (ShowButtonTPPlus)
                    {
                        System.Windows.Controls.Grid.SetColumn(TPButton, gridColumnIndex);
                        System.Windows.Controls.Grid.SetRow(TPButton, 0);
                        buttonGrid.ColumnDefinitions[gridColumnIndex].MinWidth = 70;
                        buttonGrid.Children.Add(TPButton);
                    }
                    else
                    {
                        System.Windows.Controls.Grid.SetColumn(TPButton, gridColumnIndex);
                        System.Windows.Controls.Grid.SetRow(TPButton, 0);
                        TPButton.Visibility = Visibility.Hidden;
                    }
                    if (ShowButtonTPPlus) gridColumnIndex--;
                    if (ShowButtonAutoClose)
                    {
                        System.Windows.Controls.Grid.SetColumn(toggleAutoCloseButton, gridColumnIndex);
                        System.Windows.Controls.Grid.SetRow(toggleAutoCloseButton, 0);
                        buttonGrid.ColumnDefinitions[gridColumnIndex].MinWidth = 70;
                        buttonGrid.Children.Add(toggleAutoCloseButton);
                    }
                    else
                    {
                        System.Windows.Controls.Grid.SetColumn(toggleAutoCloseButton, gridColumnIndex);
                        System.Windows.Controls.Grid.SetRow(toggleAutoCloseButton, 0);
                        toggleAutoCloseButton.Visibility = Visibility.Hidden;
                    }
                    if (ShowButtonAutoClose) gridColumnIndex--;
                    if (ShowButtonClose)
                    {
                        System.Windows.Controls.Grid.SetColumn(closeAllButton, gridColumnIndex);
                        System.Windows.Controls.Grid.SetRow(closeAllButton, 0);
                        buttonGrid.ColumnDefinitions[gridColumnIndex].MinWidth = 70;
                        buttonGrid.Children.Add(closeAllButton);
                    }
                    else
                    {
                        System.Windows.Controls.Grid.SetColumn(closeAllButton, gridColumnIndex);
                        System.Windows.Controls.Grid.SetRow(closeAllButton, 0);
                        closeAllButton.Visibility = Visibility.Hidden;
                    }
                    if (ShowButtonClose) gridColumnIndex--;
                    if (ShowButtonReverse)
                    {
                        System.Windows.Controls.Grid.SetColumn(revButton, gridColumnIndex);
                        System.Windows.Controls.Grid.SetRow(revButton, 0);
                        buttonGrid.ColumnDefinitions[gridColumnIndex].MinWidth = 70;
                        buttonGrid.Children.Add(revButton);
                    }
                    else
                    {
                        System.Windows.Controls.Grid.SetColumn(revButton, gridColumnIndex);
                        System.Windows.Controls.Grid.SetRow(revButton, 0);
                        revButton.Visibility = Visibility.Hidden;
                    }
                    if (ShowButtonReverse) gridColumnIndex--;
                    if (ShowButtonAutoBreakEven)
                    {
                        System.Windows.Controls.Grid.SetColumn(toggleAutoBEButton, gridColumnIndex);
                        System.Windows.Controls.Grid.SetRow(toggleAutoBEButton, 0);
                        buttonGrid.ColumnDefinitions[gridColumnIndex].MinWidth = 70;
                        buttonGrid.Children.Add(toggleAutoBEButton);
                    }
                    else
                    {
                        System.Windows.Controls.Grid.SetColumn(toggleAutoBEButton, gridColumnIndex);
                        System.Windows.Controls.Grid.SetRow(toggleAutoBEButton, 0);
                        toggleAutoBEButton.Visibility = Visibility.Hidden;
                    }
                    if (ShowButtonAutoBreakEven) gridColumnIndex--;
                    #region LabelGrid
                    labelGrid = new System.Windows.Controls.Grid
                    {
                        Name = "HHLabelGrid",
                        Margin = new Thickness(0, 0, 0, 0),
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Top,
                        Background = Brushes.Black
                    };
                    System.Windows.Controls.RowDefinition labelRow1 = new System.Windows.Controls.RowDefinition();
                    labelGrid.RowDefinitions.Add(labelRow1);
                    System.Windows.Controls.ColumnDefinition labelColumn1 = new System.Windows.Controls.ColumnDefinition();
                    System.Windows.Controls.ColumnDefinition labelColumn2 = new System.Windows.Controls.ColumnDefinition();
                    System.Windows.Controls.ColumnDefinition labelColumn3 = new System.Windows.Controls.ColumnDefinition();
                    System.Windows.Controls.ColumnDefinition labelColumn4 = new System.Windows.Controls.ColumnDefinition();
                    labelGrid.ColumnDefinitions.Add(labelColumn1);
                    labelGrid.ColumnDefinitions.Add(labelColumn2);
                    labelGrid.ColumnDefinitions.Add(labelColumn3);
                    labelGrid.ColumnDefinitions.Add(labelColumn4);
                    #endregion
                    riskInfoLabel = new System.Windows.Controls.Label
                    {
                        Name = HHRiskInfoLabelName,
                        Content = "",
                        Foreground = Brushes.White,
                        Background = Brushes.Transparent,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 0, 0),
                        Visibility = (hasRanOnceFirstCycle) ? Visibility.Visible : Visibility.Hidden,
                    };
                    profitInfoLabel = new System.Windows.Controls.Label
                    {
                        Name = HHProfitInfoLabelName,
                        Content = "",
                        Foreground = Brushes.White,
                        Background = Brushes.Transparent,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 0, 0),
                        Visibility = (hasRanOnceFirstCycle) ? Visibility.Visible : Visibility.Hidden,
                    };
                    bogeyTargetInfoLabel = new System.Windows.Controls.Label
                    {
                        Name = HHBogeyTargetInfoLabelName,
                        Content = "",
                        Foreground = Brushes.Silver,
                        Background = Brushes.Transparent,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 0, 0),
                        Visibility = (hasRanOnceFirstCycle) ? Visibility.Visible : Visibility.Hidden,
                    };
                    dayOverMaxLossInfoLabel = new System.Windows.Controls.Label
                    {
                        Name = HHDayOverMaxLossInfoLabelName,
                        Content = "",
                        Foreground = dayOverMaxLossInfoTextColor,
                        Background = Brushes.Transparent,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 0, 0),
                        Visibility = (hasRanOnceFirstCycle) ? Visibility.Visible : Visibility.Hidden,
                    };
                    System.Windows.Controls.Grid.SetColumn(dayOverMaxLossInfoLabel, 3);
                    System.Windows.Controls.Grid.SetRow(dayOverMaxLossInfoLabel, 3);
                    System.Windows.Controls.Grid.SetColumn(bogeyTargetInfoLabel, 2);
                    System.Windows.Controls.Grid.SetRow(bogeyTargetInfoLabel, 2);
                    System.Windows.Controls.Grid.SetColumn(riskInfoLabel, 1);
                    System.Windows.Controls.Grid.SetRow(riskInfoLabel, 1);
                    System.Windows.Controls.Grid.SetColumn(profitInfoLabel, 0);
                    System.Windows.Controls.Grid.SetRow(profitInfoLabel, 0);
                    labelGrid.Children.Add(riskInfoLabel);
                    labelGrid.Children.Add(profitInfoLabel);
                    labelGrid.Children.Add(bogeyTargetInfoLabel);
                    labelGrid.Children.Add(dayOverMaxLossInfoLabel);
                    System.Windows.Controls.Grid.SetColumn(labelGrid, 0);
                    System.Windows.Controls.Grid.SetRow(labelGrid, 1);
                    thLayoutGrid.Children.Add(labelGrid);
                    thLayoutGrid.Children.Add(buttonGrid);
                    System.Windows.Controls.Grid.SetColumn(buttonGrid, 0);
                    System.Windows.Controls.Grid.SetRow(buttonGrid, 0);
                    UserControlCollection.Add(thLayoutGrid);
                }
            }
        }
        private bool HasATMStrategy()
        {
            return !string.IsNullOrEmpty(atmStrategyName);
        }
        private void ValidateInstrument(string instrumentName)
        {
            Instrument instrument = Instrument.GetInstrument(instrumentName);
            if (instrument != null)
            {
                RealLogger.PrintOutput("Instrument =" + instrument.FullName + " Tick Size=" + Convert.ToString(instrument.MasterInstrument.TickSize) + " Tick Value=" + Convert.ToString(RealInstrumentService.GetTickValue(instrument)), PrintTo.OutputTab2);
            }
        }
        private string GetCurrentFuturesMonthYearPrefix()
        {
            string tempText = null;
            if (attachedInstrumentIsFuture)
            {
                tempText = this.attachedInstrument.FullName.Substring(this.attachedInstrument.MasterInstrument.Name.Length, 6);
            }
            return tempText;
        }
        private bool IsCtrlKeyDown()
        {
            bool returnFlag = false;
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                returnFlag = true;
            }
            return returnFlag;
        }
        #region Properties
        [NinjaScriptProperty]
        [Display(Name = "IndicatorName", Order = 0, GroupName = "0) Indicator Information")]
        public string IndicatorName
        {
            get { return FullSystemName; }
            set { }
        }
        [NinjaScriptProperty]
        [Display(Name = "IndicatorTermsOfUse", Description = SystemDescription, Order = 1, GroupName = "0) Indicator Information")]
        public string IndicatorTermsOfUse
        {
            get { return SystemDescription; }
            set { }
        }
        [NinjaScriptProperty]
        [Display(Name = "IndicatorInfoLink", Order = 2, GroupName = "0) Indicator Information")]
        public string IndicatorInfoLink
        {
            get { return InfoLink; }
            set { }
        }
        [NinjaScriptProperty]
        [Display(Name = "UseAutoPositionStopLoss", Order = 1, GroupName = "1) Order Management Settings")]
        public bool UseAutoPositionStopLoss
        { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "UseAutoPositionTakeProfit", Order = 2, GroupName = "1) Order Management Settings")]
        public bool UseAutoPositionTakeProfit
        { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "LimitAddOnVolumeToInProfit", Order = 3, GroupName = "1) Order Management Settings")]
        public bool LimitAddOnVolumeToInProfit
        { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "AutoPositionCloseType", Order = 4, GroupName = "1) Order Management Settings")]
        public JoiaGestorCloseAutoTypes AutoPositionCloseType
        { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "AutoPositionBreakEvenType", Order = 5, GroupName = "1) Order Management Settings")]
        public JoiaGestorBreakEvenAutoTypes AutoPositionBreakEvenType
        { get; set; }
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "StopLossInitialTicks", Order = 6, GroupName = "1) Order Management Settings")]
        public int StopLossInitialTicks
        { get; set; }
        [NinjaScriptProperty]
        [Range(0, double.MaxValue)]
        [Display(Name = "StopLossInitialATRMultiplier", Order = 7, GroupName = "1) Order Management Settings")]
        public double StopLossInitialATRMultiplier
        { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "StopLossInitialSnapType", Order = 8, GroupName = "1) Order Management Settings")]
        public JoiaGestorStopLossSnapTypes StopLossInitialSnapType
        { get; set; }
        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name = "StopLossInitialMaxTicks", Order = 9, GroupName = "1) Order Management Settings")]
        public int StopLossInitialMaxTicks
        { get; set; }
        [NinjaScriptProperty]
        [Range(0, double.MaxValue)]
        [Display(Name = "StopLossInitialDollars", Order = 10, GroupName = "1) Order Management Settings")]
        public double StopLossInitialDollars
        { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "StopLossInitialDollarsCombined", Order = 11, GroupName = "1) Order Management Settings")]
        public bool StopLossInitialDollarsCombined
        { get; set; }
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "StopLossJumpTicks", Order = 12, GroupName = "1) Order Management Settings")]
        public int StopLossJumpTicks
        { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "StopLossCTRLJumpTicks", Order = 13, GroupName = "1) Order Management Settings")]
        public bool StopLossCTRLJumpTicks
        { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "StopLossRefreshOnVolumeChange", Order = 14, GroupName = "1) Order Management Settings")]
        public bool StopLossRefreshOnVolumeChange
        { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "StopLossRefreshManagementEnabled ", Order = 15, GroupName = "1) Order Management Settings")]
        public bool StopLossRefreshManagementEnabled
        { get; set; }
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "BreakEvenInitialTicks", Order = 16, GroupName = "1) Order Management Settings")]
        public int BreakEvenInitialTicks
        { get; set; }
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "BreakEvenJumpTicks", Order = 17, GroupName = "1) Order Management Settings")]
        public int BreakEvenJumpTicks
        { get; set; }
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "BreakEvenTurboJumpTicks", Order = 18, GroupName = "1) Order Management Settings")]
        public int BreakEvenTurboJumpTicks
        { get; set; }
        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name = "BreakEvenAutoTriggerTicks", Order = 19, GroupName = "1) Order Management Settings")]
        public int BreakEvenAutoTriggerTicks
        { get; set; }
        [NinjaScriptProperty]
        [Range(0, double.MaxValue)]
        [Display(Name = "BreakEvenAutoTriggerATRMultiplier", Order = 20, GroupName = "1) Order Management Settings")]
        public double BreakEvenAutoTriggerATRMultiplier
        { get; set; }
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "TakeProfitInitialTicks", Order = 23, GroupName = "1) Order Management Settings")]
        public int TakeProfitInitialTicks
        { get; set; }
        [NinjaScriptProperty]
        [Range(0, double.MaxValue)]
        [Display(Name = "TakeProfitInitialATRMultiplier", Order = 24, GroupName = "1) Order Management Settings")]
        public double TakeProfitInitialATRMultiplier
        { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "TakeProfitSyncECATargetPrice", Order = 26, GroupName = "1) Order Management Settings")]
        public bool TakeProfitSyncECATargetPrice
        { get; set; }
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "TakeProfitJumpTicks", Order = 27, GroupName = "1) Order Management Settings")]
        public int TakeProfitJumpTicks
        { get; set; }
        [NinjaScriptProperty]
        [Range(0.1, double.MaxValue)]
        [Display(Name = "TakeProfitCtrlSLMultiplier", Order = 28, GroupName = "1) Order Management Settings")]
        public double TakeProfitCtrlSLMultiplier
        { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "TakeProfitRefreshManagementEnabled", Order = 29, GroupName = "1) Order Management Settings")]
        public bool TakeProfitRefreshManagementEnabled
        { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "ShowAveragePriceLine", Order = 39, GroupName = "1) Order Management Settings")]
        public bool ShowAveragePriceLine
        { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "ShowAveragePriceLineQuantity", Order = 40, GroupName = "1) Order Management Settings")]
        public bool ShowAveragePriceLineQuantity
        { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "ShowAveragePriceLineQuantityInMicros", Order = 41, GroupName = "1) Order Management Settings")]
        public bool ShowAveragePriceLineQuantityInMicros
        { get; set; }
        [NinjaScriptProperty]
        [Range(2, int.MaxValue)]
        [Display(Name = "ATRPeriod", Order = 42, GroupName = "1) Order Management Settings")]
        public int ATRPeriod
        { get; set; }
        [NinjaScriptProperty]
        [Range(2, int.MaxValue)]
        [Display(Name = "SnapPowerBoxPeriod", Order = 43, GroupName = "1) Order Management Settings")]
        public int SnapPowerBoxPeriod
        { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "SnapPowerBoxAutoAdjustPeriodsOnM1", Order = 44, GroupName = "1) Order Management Settings")]
        public bool SnapPowerBoxAutoAdjustPeriodsOnM1
        { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "UseBlendedInstruments", Order = 45, GroupName = "1) Order Management Settings")]
        public bool UseBlendedInstruments
        { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "UseIntradayMarginCheck", Order = 46, GroupName = "1) Order Management Settings")]
        public bool UseIntradayMarginCheck
        { get; set; }
        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name = "RefreshTPSLPaddingTicks", Order = 47, GroupName = "1) Order Management Settings")]
        public int RefreshTPSLPaddingTicks
        { get; set; }
        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name = "RefreshTPSLOrderDelaySeconds", Order = 48, GroupName = "1) Order Management Settings")]
        public int RefreshTPSLOrderDelaySeconds
        { get; set; }
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "SingleOrderChunkMaxQuantity", Order = 49, GroupName = "1) Order Management Settings")]
        public int SingleOrderChunkMaxQuantity
        { get; set; }
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "SingleOrderChunkMinQuantity", Order = 50, GroupName = "1) Order Management Settings")]
        public int SingleOrderChunkMinQuantity
        { get; set; }
        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name = "SingleOrderChunkDelayMilliseconds", Order = 51, GroupName = "1) Order Management Settings")]
        public int SingleOrderChunkDelayMilliseconds
        { get; set; }
        [NinjaScriptProperty]
        [Range(0, double.MaxValue)]
        [Display(Name = "AutoCloseMinProfitDollarsPerVolume", Order = 1, GroupName = "2) Auto Close Settings")]
        public double AutoCloseMinProfitDollarsPerVolume
        { get; set; }
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "AutoCloseAndTrailMA1Period", Order = 2, GroupName = "2) Auto Close Settings")]
        public int AutoCloseAndTrailMA1Period
        { get; set; }
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "AutoCloseAndTrailMA2Period", Order = 3, GroupName = "2) Auto Close Settings")]
        public int AutoCloseAndTrailMA2Period
        { get; set; }
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "AutoCloseAndTrailMA3Period", Order = 4, GroupName = "2) Auto Close Settings")]
        public int AutoCloseAndTrailMA3Period
        { get; set; }
        [NinjaScriptProperty]
        [Range(0, double.MaxValue)]
        [Display(Name = "DayOverMaxLossDollars", Order = 8, GroupName = "2) Auto Close Settings")]
        public double DayOverMaxLossDollars
        { get; set; }
        [NinjaScriptProperty]
        [Range(0, double.MaxValue)]
        [Display(Name = "DayOverMaxLossBTBaseRatio", Order = 9, GroupName = "2) Auto Close Settings")]
        public double DayOverMaxLossBTBaseRatio
        { get; set; }
        [NinjaScriptProperty]
        [Range(0, double.MaxValue)]
        [Display(Name = "DayOverAccountBalanceFloorDollars", Order = 10, GroupName = "2) Auto Close Settings")]
        public double DayOverAccountBalanceFloorDollars
        { get; set; }
        [NinjaScriptProperty]
        [Range(0, double.MaxValue)]
        [Display(Name = "ECATargetDollars", Order = 11, GroupName = "2) Auto Close Settings")]
        public double ECATargetDollars
        { get; set; }
        [NinjaScriptProperty]
        [Range(0.01, double.MaxValue)]
        [Display(Name = "ECATargetDollarsPerOtherVolume", Order = 12, GroupName = "2) Auto Close Settings")]
        public double ECATargetDollarsPerOtherVolume
        { get; set; }
        [NinjaScriptProperty]
        [Range(0.01, double.MaxValue)]
        [Display(Name = "ECATargetDollarsPerMNQVolume", Order = 13, GroupName = "2) Auto Close Settings")]
        public double ECATargetDollarsPerMNQVolume
        { get; set; }
        [NinjaScriptProperty]
        [Range(0.01, double.MaxValue)]
        [Display(Name = "ECATargetDollarsPerNQVolume", Order = 14, GroupName = "2) Auto Close Settings")]
        public double ECATargetDollarsPerNQVolume
        { get; set; }
        [NinjaScriptProperty]
        [Range(0.01, double.MaxValue)]
        [Display(Name = "ECATargetDollarsPerM2KVolume", Order = 15, GroupName = "2) Auto Close Settings")]
        public double ECATargetDollarsPerM2KVolume
        { get; set; }
        [NinjaScriptProperty]
        [Range(0.01, double.MaxValue)]
        [Display(Name = "ECATargetDollarsPerRTYVolume", Order = 16, GroupName = "2) Auto Close Settings")]
        public double ECATargetDollarsPerRTYVolume
        { get; set; }
        [NinjaScriptProperty]
        [Range(0.01, double.MaxValue)]
        [Display(Name = "ECATargetDollarsPerMESVolume", Order = 17, GroupName = "2) Auto Close Settings")]
        public double ECATargetDollarsPerMESVolume
        { get; set; }
        [NinjaScriptProperty]
        [Range(0.01, double.MaxValue)]
        [Display(Name = "ECATargetDollarsPerESVolume", Order = 18, GroupName = "2) Auto Close Settings")]
        public double ECATargetDollarsPerESVolume
        { get; set; }
        [NinjaScriptProperty]
        [Range(0.01, double.MaxValue)]
        [Display(Name = "ECATargetDollarsPerMYMVolume", Order = 19, GroupName = "2) Auto Close Settings")]
        public double ECATargetDollarsPerMYMVolume
        { get; set; }
        [NinjaScriptProperty]
        [Range(0.01, double.MaxValue)]
        [Display(Name = "ECATargetDollarsPerYMVolume", Order = 20, GroupName = "2) Auto Close Settings")]
        public double ECATargetDollarsPerYMVolume
        { get; set; }
        [NinjaScriptProperty]
        [Range(0, double.MaxValue)]
        [Display(Name = "ECATargetATRMultiplierPerVolume", Order = 21, GroupName = "2) Auto Close Settings")]
        public double ECATargetATRMultiplierPerVolume
        { get; set; }
        [NinjaScriptProperty]
        [Range(0, double.MaxValue)]
        [Display(Name = "ECAMaxDDInDollars", Order = 22, GroupName = "2) Auto Close Settings")]
        public double ECAMaxDDInDollars
        { get; set; }
        [NinjaScriptProperty]
        [Range(0, double.MaxValue)]
        [Display(Name = "ExcessIntradayMarginMinDollars", Order = 23, GroupName = "2) Auto Close Settings")]
        public double ExcessIntradayMarginMinDollars
        { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "AutoEntryVolumeType", Order = 0, GroupName = "3) Auto Entry Settings")]
        public JoiaGestorEntryVolumeAutoTypes AutoEntryVolumeType
        { get; set; }
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "AutoEntryVolumeOption1", Order = 1, GroupName = "3) Auto Entry Settings")]
        public int AutoEntryVolumeOption1
        { get; set; }
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "AutoEntryVolumeOption2", Order = 2, GroupName = "3) Auto Entry Settings")]
        public int AutoEntryVolumeOption2
        { get; set; }
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "AutoEntryVolumeOption3", Order = 3, GroupName = "3) Auto Entry Settings")]
        public int AutoEntryVolumeOption3
        { get; set; }
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "AutoEntryVolumeOption4", Order = 4, GroupName = "3) Auto Entry Settings")]
        public int AutoEntryVolumeOption4
        { get; set; }
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "AutoEntryVolumeOption5", Order = 5, GroupName = "3) Auto Entry Settings")]
        public int AutoEntryVolumeOption5
        { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "UseHedgehogEntry", Order = 0, GroupName = "4) Hedgehog Settings")]
        public bool UseHedgehogEntry
        { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "HedgehogEntryBuySymbol1SellSymbol2", Order = 1, GroupName = "4) Hedgehog Settings")]
        public bool HedgehogEntryBuySymbol1SellSymbol2
        { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "HedgehogEntrySymbol1", Order = 2, GroupName = "4) Hedgehog Settings")]
        public string HedgehogEntrySymbol1
        { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "HedgehogEntrySymbol2", Order = 3, GroupName = "4) Hedgehog Settings")]
        public string HedgehogEntrySymbol2
        { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "UsePositionProfitLogging", Order = 1, GroupName = "5) Output Log Settings")]
        public bool UsePositionProfitLogging
        { get; set; }
        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name = "DebugLogLevel", Order = 2, GroupName = "5) Output Log Settings")]
        public int DebugLogLevel
        { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "OrderWaitOutputThrottleSeconds", Order = 3, GroupName = "5) Output Log Settings")]
        public int OrderWaitOutputThrottleSeconds
        { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "UseAccountInfoLogging", Order = 1, GroupName = "6) Account Logging Settings")]
        public bool UseAccountInfoLogging
        { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "AccountInfoLoggingPath", Order = 2, GroupName = "6) Account Logging Settings")]
        public string AccountInfoLoggingPath
        { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "IgnoreInstrumentServerSupport", Order = 3, GroupName = "6) Account Logging Settings")]
        public bool IgnoreInstrumentServerSupport
        { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "ShowButtonAutoBreakEven", Order = 1, GroupName = "7) Button Settings")]
        public bool ShowButtonAutoBreakEven
        { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "ShowButtonReverse", Order = 2, GroupName = "7) Button Settings")]
        public bool ShowButtonReverse
        { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "ShowButtonClose", Order = 3, GroupName = "7) Button Settings")]
        public bool ShowButtonClose
        { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "ShowButtonAutoClose", Order = 4, GroupName = "7) Button Settings")]
        public bool ShowButtonAutoClose
        { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "ShowButtonTPPlus", Order = 6, GroupName = "7) Button Settings")]
        public bool ShowButtonTPPlus
        { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "ShowButtonBEPlus", Order = 7, GroupName = "7) Button Settings")]
        public bool ShowButtonBEPlus
        { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "ShowButtonSLPlus", Order = 8, GroupName = "7) Button Settings")]
        public bool ShowButtonSLPlus
        { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "ShowButtonBuyMarket", Order = 9, GroupName = "7) Button Settings")]
        public bool ShowButtonBuyMarket
        { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "ShowButtonSellMarket", Order = 10, GroupName = "7) Button Settings")]
        public bool ShowButtonSellMarket
        { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "ShowButtonVolume", Order = 18, GroupName = "7) Button Settings")]
        public bool ShowButtonVolume
        { get; set; }
        #endregion
    }
}
namespace NinjaTrader.NinjaScript.Indicators.TH
{
    // by making a separate class for this custom extended MenuItem that adds a drop-down arrow, I can
    // make multiple new instances of this menu with different text and children options
    public class DropMenu : MenuItem
    {
        private DockPanel				contentDockPanel;
        System.Windows.Shapes.Path		comboBoxArrowPath;
        private Viewbox					pathViewBox;
        public object	Content
        { get; set; }
        public Brush	DownArrowBrush
        { get; set; }			
        public Brush	PopupBackground
        { get; set; }
        public DropMenu()
        {
            // this dock panel will let us align the text or other header control to the left and 
            // our down arrow to the right
            contentDockPanel = new DockPanel()
            {
                MinHeight			= 11,
                MinWidth			= 25
            };
            // set the default properties for this control
            Header					= contentDockPanel;
            Margin					= new System.Windows.Thickness(0);
            Padding					= new System.Windows.Thickness(1);
            Loaded += DropMenu_Loaded;
        }
        public void DropMenu_Loaded(object sender, RoutedEventArgs e)
        {
            // add the content to the dock panel first, if its available
            if (Content != null)
            {
                // if the object passed in is a UIElement, add it to the panel
                if (Content is UIElement)
                    contentDockPanel.Children.Add(Content as UIElement);
                // otherwise if its something easily converted to a string, stuff the string in a textblock
                else
                {
                    TextBlock contentTextBlock = new TextBlock()
                    {
                        FontFamily			= FontFamily,
                        FontSize			= FontSize,
                        FontStretch			= FontStretch,
                        FontStyle			= FontStyle,
                        FontWeight			= FontWeight,
                        HorizontalAlignment	= HorizontalAlignment.Left,
                        Margin				= new Thickness(0, 0, 2, 0),
                        Padding				= new Thickness(6, 1.5, 2, 1.5),
                        Text				= Content.ToString()
                    };
                    contentDockPanel.Children.Add(contentTextBlock);
                }
            }
            // set the width of the dockpanel to match our menu control
            contentDockPanel.Height		= Height;
            contentDockPanel.Width		= Width;
            SetDefaultBrushes();
            // TODO: when comboBoxArrowPath or pathViewBox is instantiated in the constructor, errors are occuring when holding the F5 key
            // inner items are being instantiated here to avoid issues when created in constructor
            // this is the path for the down arrow
            comboBoxArrowPath = new System.Windows.Shapes.Path()
            {
                                        // Down arrow geometry
                Data					= Geometry.Parse("M0 1 L6 10 L12 1 L10 0 L6 6 L2 0 Z"),
                Height					= 7,
                HorizontalAlignment		= HorizontalAlignment.Right,
                Fill					= DownArrowBrush ?? Foreground,
                Stretch					= System.Windows.Media.Stretch.Fill,
                Stroke					= DownArrowBrush ?? Foreground,
                VerticalAlignment		= VerticalAlignment.Bottom,
                Width					= 11
            };
            // this viewbox will hold the dimensions for the down arrown path geometry and is scalable
            pathViewBox = new Viewbox()
            {
                Height					= 6,
                HorizontalAlignment		= HorizontalAlignment.Right,
                Margin					= new Thickness(6, 0, 6, 4),
                VerticalAlignment		= VerticalAlignment.Bottom,
                Width					= 11
            };
            // to prevent an error attempting to access the ViewBox template before the object is ready, we do this after the loaded event
            // this is from a WeakEventManager so the handler is nulled (and set for garbage collection) immediately after running so we don't have to remove the handler ourselves later during teardown
            System.Windows.WeakEventManager<Viewbox, RoutedEventArgs>.AddHandler(pathViewBox, "Loaded", PathViewBox_Loaded);
            pathViewBox.Child	= comboBoxArrowPath;
            // add our down arrown in after adding text or uielements
            contentDockPanel.Children.Add(pathViewBox);
        }
        private void PathViewBox_Loaded(object sender, RoutedEventArgs e)
        {
            SetMenuPopup();
        }
        // TODO: should this be moved to LoadBrushesFromSkin()? 
        private void SetDefaultBrushes()
        {
            // set a new default for Foreground that can be overridden
            // full qualified namespaces used here to show where these tools are
            Foreground		= (Foreground.ToString() != "#FF212121" ? Foreground : (System.Windows.Application.Current.TryFindResource("FontLabelBrush") as SolidColorBrush ?? Brushes.Purple));
            // same for background
            Background		= (Background.ToString() != "#00FFFFFF" ? Background : (Application.Current.TryFindResource("ComboBoxBackgroundBrush") as LinearGradientBrush ?? new LinearGradientBrush(Colors.Purple, Colors.Pink, 1)));
            // same for border
            BorderBrush		= (BorderBrush.ToString() != "#00FFFFFF") ? BorderBrush : (Application.Current.TryFindResource("ButtonBorderBrush") as SolidColorBrush ?? Brushes.Purple);
            // color for the menu background
            PopupBackground	= PopupBackground ?? (Application.Current.TryFindResource("SubMenuBackground") as SolidColorBrush ?? new SolidColorBrush(Brushes.Purple.Color) ?? Brushes.Purple);
        }
        private void SetMenuPopup()
        {
            System.Windows.Controls.Primitives.Popup popupMenu	= (System.Windows.Controls.Primitives.Popup)Template.FindName("PART_Popup", this);				
            System.Windows.Controls.Border popupBorder			= (popupMenu.Child as Border);
            popupBorder.BorderBrush								= BorderBrush;
            popupBorder.Background								= PopupBackground;
            // TODO: also set the default brush for the menu divider | separator to MenuSeparator,
            // as well as all of the other colors from 'Colors for menus / drop downs throughout the application'.
            // https://www.syncfusion.com/faq/wpf/menu/how-can-i-style-a-separator-used-as-a-menu-item, how do i do this in code?
        }
    }
    public class RealSessionState
    {
        public long AccountId
        { get; set; }
        public DateTime ChangeDate
        { get; set; }
        public double HighestPnL
        { get; set; }
    }
    public class RealRunOncePerBar
    {
        private DateTime runOncePerBarLastNewBarTime = DateTime.MinValue;
        private DateTime runOncePerBarLastRunBarTime = DateTime.MinValue;
        public bool IsFirstRunThisBar
        {
            get
            {
                return (runOncePerBarLastRunBarTime != runOncePerBarLastNewBarTime);
            }
        }
        public void SetRunCompletedThisBar()
        {
            runOncePerBarLastRunBarTime = runOncePerBarLastNewBarTime;
        }
        public void UpdateBarTime(DateTime currentNewBarTime)
        {
            DateTime tempNewBarTime = currentNewBarTime;
            if (tempNewBarTime != runOncePerBarLastNewBarTime)
            {
                runOncePerBarLastNewBarTime = tempNewBarTime;
            }
        }
    }
    public class RealUtility
    {
    }
    public class RealLogger
    {
        private string systemName = String.Empty;
        private int lastPrintOutputHashCode = 0;
        private const int delaySeconds = 1;
        private DateTime lastCheckTime = DateTime.MinValue;
        private DateTime lastRunTime = DateTime.MinValue;
        public RealLogger(string systemName)
        {
            this.systemName = systemName;
        }
        public void PrintOutput(string output, PrintTo outputTab = PrintTo.OutputTab1, bool blockDuplicateMessages = false, bool throttleEverySecond = false)
        {
            bool outputNow = true;
            if (throttleEverySecond)
            {
                lastCheckTime = DateTime.Now;
                if (lastCheckTime > lastRunTime)
                { outputNow = true; }
                else
                { outputNow = false; }
            }
            if (outputNow)
            {
                if (blockDuplicateMessages)
                {
                    int tempHashCode = output.GetHashCode();
                    if (tempHashCode != lastPrintOutputHashCode)
                    {
                        lastPrintOutputHashCode = tempHashCode;
                        lastRunTime = DateTime.Now.AddSeconds(delaySeconds);
                        Output.Process(DateTime.Now + " " + systemName + ": " + output, outputTab);
                    }
                }
                else
                {
                    lastRunTime = DateTime.Now.AddSeconds(delaySeconds);
                    Output.Process(DateTime.Now + " " + systemName + ": " + output, outputTab);
                }
            }
        }
    }
    public class RealInstrumentService
    {
        private readonly ConcurrentDictionary<string, double> askPriceCache = new ConcurrentDictionary<string, double>();
        private readonly ConcurrentDictionary<string, double> bidPriceCache = new ConcurrentDictionary<string, double>();
        private readonly ConcurrentDictionary<string, double> lastPriceCache = new ConcurrentDictionary<string, double>();
        private readonly Dictionary<double, int> tickSizeDecimalPlaceCountCache = new Dictionary<double, int>();
        private string BuildKeyName(Instrument instrument)
        {
            string keyName = instrument.FullName;
            return keyName;
        }
        public double NormalizePrice(Instrument instrument, double price)
        {
            double newPrice = 0;
            int decimalPlaces = GetTickSizeDecimalPlaces(instrument.MasterInstrument.TickSize);
            string formatText = string.Concat("N", decimalPlaces);
            string stringPriceValue = price.ToString(formatText);
            newPrice = double.Parse(stringPriceValue);
            return newPrice;
        }
        public int GetTickSizeDecimalPlaces(double tickSize)
        {
            int decimalPlaceCount = 0;
            if (tickSize < 0) return decimalPlaceCount;
            if (tickSizeDecimalPlaceCountCache.ContainsKey(tickSize))
            {
                decimalPlaceCount = tickSizeDecimalPlaceCountCache[tickSize];
            }
            else
            {
                var parts = tickSize.ToString(CultureInfo.InvariantCulture).Split('.');
                if (parts.Length < 2)
                    decimalPlaceCount = 0;
                else
                    decimalPlaceCount = parts[1].TrimEnd('0').Length;
                tickSizeDecimalPlaceCountCache.Add(tickSize, decimalPlaceCount);
            }
            return decimalPlaceCount;
        }
        public static double ConvertTicksToDollars(Instrument instrument, int ticks, int contracts)
        {
            double dollarValue = 0;
            if (ticks > 0 && contracts > 0)
            {
                double tickValue = GetTickValue(instrument);
                double tickSize = GetTickSize(instrument);
                dollarValue = tickValue * ticks * contracts;
            }
            return dollarValue;
        }
        public static double GetTickValue(Instrument instrument)
        {
            double tickValue = instrument.MasterInstrument.PointValue * instrument.MasterInstrument.TickSize;
            return tickValue;
        }
        public static int GetTicksPerPoint(double tickSize)
        {
            int tickPoint = 1;
            if (tickSize < 1)
            {
                tickPoint = (int)(1.0 / tickSize);
            }
            return (tickPoint);
        }
        public static bool IsFutureInstrumentType(Instrument instrument)
        {
            bool isFuture = (instrument.MasterInstrument.InstrumentType == InstrumentType.Future);
            return isFuture;
        }
        public static double GetTickSize(Instrument instrument)
        {
            double tickSize = instrument.MasterInstrument.TickSize;
            return (tickSize);
        }
        public double GetLastPrice(Instrument instrument)
        {
            double lastPrice = 0;
            string keyName = BuildKeyName(instrument);
            if (!lastPriceCache.TryGetValue(keyName, out lastPrice))
            {
                if (instrument.MarketData != null && instrument.MarketData.Last != null)
                {
                    lastPrice = instrument.MarketData.Last.Price;
                }
                else if (instrument.MarketData != null && instrument.MarketData.Bid != null)
                {
                    lastPrice = instrument.MarketData.Bid.Price;
                }
                else
                {
                    lastPrice = 0;
                }
            }
            return lastPrice;
        }
        public double GetAskPrice(Instrument instrument)
        {
            double askPrice = 0;
            string keyName = BuildKeyName(instrument);
            if (!askPriceCache.TryGetValue(keyName, out askPrice))
            {
                if (instrument.MarketData != null && instrument.MarketData.Last != null)
                {
                    askPrice = instrument.MarketData.Last.Ask;
                }
                else if (instrument.MarketData != null && instrument.MarketData.Ask != null)
                {
                    askPrice = instrument.MarketData.Ask.Price;
                }
                else
                {
                    askPrice = 0;
                }
            }
            return askPrice;
        }
        public double GetBidPrice(Instrument instrument)
        {
            double bidPrice = 0;
            string keyName = BuildKeyName(instrument);
            if (!bidPriceCache.TryGetValue(keyName, out bidPrice))
            {
                if (instrument.MarketData != null && instrument.MarketData.Last != null)
                {
                    bidPrice = instrument.MarketData.Last.Bid;
                }
                else if (instrument.MarketData != null && instrument.MarketData.Bid != null)
                {
                    bidPrice = instrument.MarketData.Bid.Price;
                }
                else
                {
                    bidPrice = 0;
                }
            }
            return bidPrice;
        }
        public double SetAskPrice(Instrument instrument, double askPrice)
        {
            string keyName = BuildKeyName(instrument);
            double newPrice = askPriceCache.AddOrUpdate(String.Copy(keyName), askPrice, (oldkey, oldvalue) => askPrice);
            return newPrice;
        }
        public double SetBidPrice(Instrument instrument, double bidPrice)
        {
            string keyName = BuildKeyName(instrument);
            double newPrice = bidPriceCache.AddOrUpdate(String.Copy(keyName), bidPrice, (oldkey, oldvalue) => bidPrice);
            return newPrice;
        }
        public double SetLastPrice(Instrument instrument, double lastPrice)
        {
            string keyName = BuildKeyName(instrument);
            double newPrice = lastPriceCache.AddOrUpdate(String.Copy(keyName), lastPrice, (oldkey, oldvalue) => lastPrice);
            return newPrice;
        }
    }
    public class RealMultiCycleCache
    {
        private Dictionary<string, DateTime> multiCycleCache = new Dictionary<string, DateTime>();
        private readonly object MultiCycleCacheLock = new object();
        private const int AutoExpireSeconds = 10;
        private DateTime GenerateExpirationTime()
        {
            return DateTime.Now.AddSeconds(AutoExpireSeconds);
        }
        public int Count
        {
            get { return multiCycleCache.Count; }
        }
        public bool HasElements(bool logExpiredElements = false)
        {
            bool hasElementsFlag = false;
            hasElementsFlag = (multiCycleCache.Count != 0);
            if (hasElementsFlag)
            {
                ClearExpiredElements(logExpiredElements);
            }
            hasElementsFlag = (multiCycleCache.Count != 0);
            return hasElementsFlag;
        }
        public bool ContainsKey(string uniqueId)
        {
            bool returnFlag = false;
            lock (MultiCycleCacheLock)
            {
                returnFlag = multiCycleCache.ContainsKey(uniqueId);
            }
            return returnFlag;
        }
        public bool TouchUniqueId(string uniqueId)
        {
            bool returnFlag = false;
            lock (MultiCycleCacheLock)
            {
                if (multiCycleCache.ContainsKey(uniqueId))
                {
                    DateTime expireDateTime = GenerateExpirationTime();
                    if (multiCycleCache[uniqueId] != null)
                    {
                        returnFlag = true;
                        multiCycleCache[uniqueId] = expireDateTime;
                    }
                }
            }
            return returnFlag;
        }
        public bool RegisterUniqueId(string uniqueId)
        {
            bool returnFlag = false;
            lock (MultiCycleCacheLock)
            {
                if (!multiCycleCache.ContainsKey(uniqueId))
                {
                    DateTime expireDateTime = GenerateExpirationTime();
                    multiCycleCache.Add(uniqueId, expireDateTime);
                    returnFlag = true;
                }
            }
            return returnFlag;
        }
        public void DeregisterUniqueId(string uniqueId)
        {
            lock (MultiCycleCacheLock)
            {
                multiCycleCache.Remove(uniqueId);
            }
        }
        public void ClearExpiredElements(bool logExpiredElements)
        {
            lock (MultiCycleCacheLock)
            {
                RealLogger logger = null;
                List<string> expiredElements = new List<string>();
                DateTime currentTime = DateTime.Now;
                foreach (KeyValuePair<string, DateTime> element in multiCycleCache)
                {
                    if (element.Value <= currentTime)
                    {
                        expiredElements.Add(element.Key);
                    }
                }
                foreach (string keyName in expiredElements)
                {
                    if (logExpiredElements)
                    {
                        if (logger == null) logger = new RealLogger("InternalLogger");
                        logger.PrintOutput("MultiCycleCache keyName=" + keyName + " has expired and was removed");
                    }
                    multiCycleCache.Remove(keyName);
                }
            }
        }
        public void Clear()
        {
            lock (MultiCycleCacheLock)
            {
                multiCycleCache.Clear();
            }
        }
    }
    public class RealOrder
    {
        private const int StateHasChanged = 1;
        private const int StateHasNotChanged = 0;
        private int stateChangeStatus = StateHasChanged;
        private long orderId = 0;
        private string exchangeOrderId = "";
        private string name = "";
        private OrderType orderType = OrderType.Unknown;
        private double averagePrice = 0;
        private double limitPrice = 0;
        private double limitPriceChanged = 0;
        private double stopPrice = 0;
        private double stopPriceChanged = 0;
        private OrderState orderState = OrderState.Unknown;
        private OrderAction orderAction = OrderAction.Buy;
        private Instrument instrument = null;
        private int quantity = 0;
        private int quantityChanged = 0;
        private int quantityFilled = 0;
        private Account account = null;
        public RealOrder()
        {
        }
        public string ExchangeOrderId
        {
            get
            {
                return exchangeOrderId;
            }
            set
            {
                ChangeStateFlag();
                exchangeOrderId = value;
            }
        }
        public long OrderId
        {
            get
            {
                return orderId;
            }
            set
            {
                ChangeStateFlag();
                orderId = value;
            }
        }
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                ChangeStateFlag();
                name = value;
            }
        }
        public bool IsValid
        {
            get
            {
                if (orderType != OrderType.Unknown)
                    return true;
                else
                    return false;
            }
        }
        public bool IsLimit
        {
            get
            {
                return (this.OrderType == OrderType.Limit);
            }
        }
        public bool IsStopMarket
        {
            get
            {
                return (this.OrderType == OrderType.StopMarket);
            }
        }
        public bool IsMarket
        {
            get
            {
                return (this.OrderType == OrderType.Market);
            }
        }
        public Account Account
        {
            get
            {
                return account;
            }
            set
            {
                ChangeStateFlag();
                account = value;
            }
        }
        public OrderAction OrderAction
        {
            get
            {
                return orderAction;
            }
            set
            {
                ChangeStateFlag();
                orderAction = value;
            }
        }
        public OrderType OrderType
        {
            get
            {
                return orderType;
            }
            set
            {
                ChangeStateFlag();
                orderType = value;
            }
        }
        public OrderState OrderState
        {
            get
            {
                return orderState;
            }
            set
            {
                ChangeStateFlag();
                orderState = value;
            }
        }
        public double AveragePrice
        {
            get
            {
                return averagePrice;
            }
            set
            {
                ChangeStateFlag();
                averagePrice = value;
            }
        }
        public double LimitPrice
        {
            get
            {
                return limitPrice;
            }
            set
            {
                ChangeStateFlag();
                limitPrice = value;
            }
        }
        public double LimitPriceChanged
        {
            get
            {
                return limitPriceChanged;
            }
            set
            {
                ChangeStateFlag();
                limitPriceChanged = value;
            }
        }
        public double StopPrice
        {
            get
            {
                return stopPrice;
            }
            set
            {
                ChangeStateFlag();
                stopPrice = value;
            }
        }
        public double StopPriceChanged
        {
            get
            {
                return stopPriceChanged;
            }
            set
            {
                ChangeStateFlag();
                stopPriceChanged = value;
            }
        }
        public Instrument Instrument
        {
            get
            {
                return instrument;
            }
            set
            {
                ChangeStateFlag();
                instrument = value;
            }
        }
        public int Quantity
        {
            get
            {
                return quantity;
            }
            set
            {
                ChangeStateFlag();
                quantity = value;
            }
        }
        public int QuantityChanged
        {
            get
            {
                return quantityChanged;
            }
            set
            {
                ChangeStateFlag();
                quantityChanged = value;
            }
        }
        public int QuantityFilled
        {
            get
            {
                return quantityFilled;
            }
            set
            {
                ChangeStateFlag();
                quantityFilled = value;
            }
        }
        public int QuantityRemaining
        {
            get
            {
                return (quantity - quantityFilled);
            }
        }
        public bool HasStateChanged()
        {
            return (stateChangeStatus == StateHasChanged);
        }
        public void StoreState()
        {
            ResetStateFlag();
        }
        private void ChangeStateFlag()
        {
            Interlocked.Exchange(ref stateChangeStatus, StateHasChanged);
        }
        private void ResetStateFlag()
        {
            Interlocked.Exchange(ref stateChangeStatus, StateHasNotChanged);
        }
    }
    public class RealOrderService
    {
        private readonly Dictionary<string, int> orderPartialFillCache = new Dictionary<string, int>();
        public readonly object OrderPartialFillCacheLock = new object();
        private readonly RealMultiCycleCache orderUpdateMultiCycleCache = new RealMultiCycleCache();
        private readonly List<RealOrder> realOrders = new List<RealOrder>();
        private readonly object realOrderLock = new object();
        private const string TargetOrderName = "Target";
        private const string StopOrderName = "Stop";
        private const string EntryOrderName = "Entry";
        private const string ExitOrderName = "Exit";
        private const int InOrderUpdateCycleFinishedStatus = 0;
        private int inOrderUpdateCycleCounter = 0;
        public int OrderCount
        {
            get { return realOrders.Count; }
        }
        public void InOrderUpdateCycleIncrement()
        {
            Interlocked.Increment(ref inOrderUpdateCycleCounter);
        }
        public void InOrderUpdateCycleDecrement()
        {
            Interlocked.Decrement(ref inOrderUpdateCycleCounter);
        }
        public bool InOrderUpdateCycle()
        {
            return (inOrderUpdateCycleCounter > InOrderUpdateCycleFinishedStatus);
        }
        public bool IsValidOrder(RealOrder order, Instrument instrument)
        {
            bool returnFlag = false;
            if (order.Instrument.FullName == instrument.FullName && order.OrderType != OrderType.Unknown)
            {
                returnFlag = true;
            }
            return returnFlag;
        }
        public Dictionary<string, int> OrderPartialFillCache
        {
            get { return orderPartialFillCache; }
        }
        public RealMultiCycleCache OrderUpdateMultiCycleCache
        {
            get { return orderUpdateMultiCycleCache; }
        }
        public string BuildOrderUniqueId(Order order)
        {
            string keyName = order.Id.ToString();
            return keyName;
        }
        public string BuildEntryOrderName()
        {
            string keyName = EntryOrderName;
            return keyName;
        }
        public string BuildExitOrderName()
        {
            string keyName = ExitOrderName;
            return keyName;
        }
        public string BuildTargetOrderName()
        {
            string keyName = TargetOrderName;
            return keyName;
        }
        public string BuildStopOrderName()
        {
            string keyName = StopOrderName;
            return keyName;
        }
        public bool TryGetByIndex(int index, out RealOrder realOrder)
        {
            bool returnFlag = false;
            realOrder = null;
            try
            {
                realOrder = realOrders.ElementAt(index);
                returnFlag = true;
            }
            catch
            {
            }
            return returnFlag;
        }
        public bool TryGetById(long orderId, out RealOrder realOrder)
        {
            bool returnFlag = false;
            realOrder = null;
            int realOrderCount = OrderCount;
            try
            {
                RealOrder tempRealOrder = null;
                for (int index = 0; index < realOrderCount; index++)
                {
                    tempRealOrder = realOrders.ElementAt(index);
                    if (tempRealOrder != null)
                    {
                        if (tempRealOrder.OrderId == orderId)
                        {
                            realOrder = tempRealOrder;
                            returnFlag = true;
                            break;
                        }
                    }
                }
            }
            catch
            {
            }
            return returnFlag;
        }
        public RealOrder BuildRealOrder(Account account, Instrument instrument, long orderId, string exchangeOrderId, string name, OrderType orderType, OrderAction orderAction, int quantity, int quantityChanged,
            double limitPrice, double limitPriceChanged, double stopPrice, double stopPriceChanged,
            OrderState orderState, int quantityFilled)
        {
            RealOrder realOrder = new RealOrder();
            realOrder.Account = account;
            realOrder.Instrument = instrument;
            realOrder.OrderId = orderId;
            realOrder.ExchangeOrderId = exchangeOrderId;
            realOrder.Name = name;
            realOrder.OrderType = orderType;
            realOrder.OrderAction = orderAction;
            realOrder.Quantity = quantity;
            realOrder.QuantityChanged = quantityChanged;
            realOrder.QuantityFilled = quantityFilled;
            realOrder.LimitPrice = limitPrice;
            realOrder.LimitPriceChanged = limitPriceChanged;
            realOrder.StopPrice = stopPrice;
            realOrder.StopPriceChanged = stopPriceChanged;
            realOrder.OrderState = orderState;
            return realOrder;
        }
        public void RemoveOrder(long orderId)
        {
            RealOrder foundOrder = null;
            lock (realOrderLock)
            {
                if (TryGetById(orderId, out foundOrder))
                {
                    realOrders.Remove(foundOrder);
                }
            }
        }
        public void RemoveAllTerminalStateOrders()
        {
            RealOrder tempRealOrder = null;
            List<RealOrder> removeOrderList = new List<RealOrder>();
            lock (realOrderLock)
            { 
                int realOrderCount = OrderCount;
                for (int index = 0; index < realOrderCount; index++)
                {
                    tempRealOrder = realOrders.ElementAt(index);
                    if (tempRealOrder != null)
                    {
                        if (Order.IsTerminalState(tempRealOrder.OrderState))
                        {
                            removeOrderList.Add(tempRealOrder);
                        }
                    }
                }
                foreach (RealOrder removeOrder in removeOrderList)
                {
                    realOrders.Remove(removeOrder);
                }
            }
        }
        public int AddOrUpdateOrder(RealOrder order)
        {
            int orderQuantityRemaining = 0;
            lock (realOrderLock)
            {
                RealOrder foundOrder = null;
                if (TryGetById(order.OrderId, out foundOrder))
                {
                    foundOrder.ExchangeOrderId = order.ExchangeOrderId;
                    foundOrder.Quantity = order.Quantity;
                    foundOrder.QuantityChanged = order.QuantityChanged;
                    foundOrder.StopPrice = order.StopPrice;
                    foundOrder.StopPriceChanged = order.StopPriceChanged;
                    foundOrder.LimitPrice = order.LimitPrice;
                    foundOrder.LimitPriceChanged = order.LimitPriceChanged;
                    foundOrder.OrderState = order.OrderState;
                    foundOrder.OrderAction = order.OrderAction;
                    foundOrder.QuantityFilled = order.QuantityFilled;
                    orderQuantityRemaining = foundOrder.Quantity;
                }
                else
                {
                    realOrders.Add(order);
                    orderQuantityRemaining = order.Quantity;
                }
            }
            return orderQuantityRemaining;
        }
        public void LoadOrders(Account account, int positionCount)
        {
            lock (realOrderLock)
            {
                lock (account.Orders)
                {
                    realOrders.Clear();
                    foreach (Order orderItem in account.Orders)
                    {
                        if (!Order.IsTerminalState(orderItem.OrderState))
                        {
                            RealOrder order = BuildRealOrder(account,
                                orderItem.Instrument,
                                orderItem.Id,
                                orderItem.OrderId,
                                orderItem.Name,
                                orderItem.OrderType,
                                orderItem.OrderAction,
                                orderItem.Quantity,
                                orderItem.QuantityChanged,
                                orderItem.LimitPrice,
                                orderItem.LimitPriceChanged,
                                orderItem.StopPrice,
                                orderItem.StopPriceChanged,
                                orderItem.OrderState,
                                orderItem.Filled);
                            AddOrUpdateOrder(order);
                        }
                    }
                }
            }
        }
        public double GetStopLossInfo(Account account, Instrument instrument, OrderAction orderAction, out OrderType orderType, out int orderQuantity, out int orderCount)
        {
            double stopLossPrice = 0;
            orderType = OrderType.Unknown;
            orderQuantity = 0;
            orderCount = 0;
            int intOrderCount = OrderCount;
            for (int index = 0; index < intOrderCount; index++)
            {
                RealOrder order = null;
                if (TryGetByIndex(index, out order))
                {
                    if (RealOrderService.IsValidStopLossOrder(order, instrument, orderAction))
                    {
                        stopLossPrice = order.StopPrice;
                        orderType = order.OrderType;
                        orderQuantity += order.QuantityRemaining;
                        orderCount++;
                    }
                }
            }
            return stopLossPrice;
        }
        public double GetTakeProfitInfo(Account account, Instrument instrument, OrderAction orderAction, out OrderType orderType, out int orderQuantity, out int orderCount)
        {
            double takeProfitPrice = 0;
            orderType = OrderType.Unknown;
            orderQuantity = 0;
            orderCount = 0;
            int intOrderCount = OrderCount;
            for (int index = 0; index < intOrderCount; index++)
            {
                RealOrder order = null;
                if (TryGetByIndex(index, out order))
                {
                    if (RealOrderService.IsValidTakeProfitOrder(order, instrument, orderAction))
                    {
                        takeProfitPrice = order.LimitPrice;
                        orderType = order.OrderType;
                        orderQuantity += order.QuantityRemaining;
                        orderCount++;
                    }
                }
            }
            return takeProfitPrice;
        }
        public void SubmitLimitOrder(Account account, Order limitOrder)
        {
        }
        public static bool IsValidStopLossPrice(Instrument instrument, OrderAction orderAction, double price, double lastPrice)
        {
            bool returnFlag = false;
            if (orderAction == OrderAction.BuyToCover && price > lastPrice)
            {
                returnFlag = true;
            }
            else if (orderAction == OrderAction.Sell && price < lastPrice)
            {
                returnFlag = true;
            }
            return returnFlag;
        }
        public static bool IsValidTakeProfitPrice(Instrument instrument, OrderAction orderAction, double price, double lastPrice)
        {
            bool returnFlag = false;
            if (orderAction == OrderAction.BuyToCover && price < lastPrice)
            {
                returnFlag = true;
            }
            else if (orderAction == OrderAction.Sell && price > lastPrice)
            {
                returnFlag = true;
            }
            return returnFlag;
        }
        public static bool IsValidStopLossOrder(RealOrder order, Instrument instrument, OrderAction orderAction)
        {
            bool returnFlag = false;
            if (!Order.IsTerminalState(order.OrderState) && order.OrderAction == orderAction && order.Instrument == instrument && order.IsStopMarket && order.Name.StartsWith(StopOrderName))
            {
                returnFlag = true;
            }
            return returnFlag;
        }
        public static bool IsValidTakeProfitOrder(RealOrder order, Instrument instrument, OrderAction orderAction)
        {
            bool returnFlag = false;
            if (!Order.IsTerminalState(order.OrderState) && order.OrderAction == orderAction && order.Instrument == instrument && order.IsLimit && order.Name.StartsWith(TargetOrderName))
            {
                returnFlag = true;
            }
            return returnFlag;
        }
        public static bool IsValidSellStopOrder(RealOrder order, Instrument instrument, OrderAction orderAction)
        {
            bool returnFlag = false;
            if (!Order.IsTerminalState(order.OrderState) && order.OrderAction == orderAction && order.Instrument == instrument && order.IsStopMarket && order.Name.StartsWith(EntryOrderName))
            {
                returnFlag = true;
            }
            return returnFlag;
        }
        public static bool IsValidBuyStopOrder(RealOrder order, Instrument instrument, OrderAction orderAction)
        {
            bool returnFlag = false;
            if (!Order.IsTerminalState(order.OrderState) && order.OrderAction == orderAction && order.Instrument == instrument && order.IsStopMarket && order.Name.StartsWith(EntryOrderName))
            {
                returnFlag = true;
            }
            return returnFlag;
        }
        public static bool IsValidSellLimitOrder(RealOrder order, Instrument instrument, OrderAction orderAction)
        {
            bool returnFlag = false;
            if (!Order.IsTerminalState(order.OrderState) && order.OrderAction == orderAction && order.Instrument == instrument && order.IsLimit && order.Name.StartsWith(EntryOrderName))
            {
                returnFlag = true;
            }
            return returnFlag;
        }
        public static bool IsValidBuyLimitOrder(RealOrder order, Instrument instrument, OrderAction orderAction)
        {
            bool returnFlag = false;
            if (!Order.IsTerminalState(order.OrderState) && order.OrderAction == orderAction && order.Instrument == instrument && order.IsLimit && order.Name.StartsWith(EntryOrderName))
            {
                returnFlag = true;
            }
            return returnFlag;
        }
        public int GetFilledOrderQuantity(Order order)
        {
            int quantity = order.Filled;
            if (order.OrderState == OrderState.PartFilled || order.OrderState == OrderState.Filled)
            {
                lock (OrderPartialFillCacheLock)
                {
                    string orderUniqueId = BuildOrderUniqueId(order);
                    if (orderPartialFillCache.ContainsKey(orderUniqueId))
                    {
                        int currentFilledQuantity = orderPartialFillCache[orderUniqueId];
                        quantity = order.Filled - currentFilledQuantity;
                        if (order.OrderState == OrderState.Filled)
                            orderPartialFillCache.Remove(orderUniqueId);
                        else
                            orderPartialFillCache[orderUniqueId] = order.Filled;
                    }
                    else
                    {
                        if (order.OrderState == OrderState.PartFilled)
                            orderPartialFillCache[orderUniqueId] = order.Filled;
                    }
                }
            }
            return quantity;
        }
        public bool AreAllOrderUpdateCyclesComplete()
        {
            bool returnFlag = false;
            if (!HasActiveMarketOrders()
                && !this.OrderUpdateMultiCycleCache.HasElements()
                && !this.InOrderUpdateCycle())
            {
                returnFlag = true;
            }
            return returnFlag;
        }
        public bool HasActiveMarketOrders()
        {
            bool hasActiveMarketOrders = false;
            bool isActiveMarketOrder = false;
            int orderCount = this.OrderCount;
            for (int index = 0; index < orderCount; index++)
            {
                RealOrder order = null;
                if (this.TryGetByIndex(index, out order))
                {
                    isActiveMarketOrder = (order.IsMarket && !Order.IsTerminalState(order.OrderState));
                    if (isActiveMarketOrder)
                    {
                        hasActiveMarketOrders = true;
                        break;
                    }
                }
            }
            return hasActiveMarketOrders;
        }
    }
    public class RealTradeService
    {
    }
    public class RealPositionService
    {
        private readonly List<RealPosition> realPositions = new List<RealPosition>();
        private readonly object realPositionLock = new object();
        private bool isInReplayMode = false;
        public bool IsInReplayMode
        {
            get
            {
                return isInReplayMode;
            }
            set
            {
                isInReplayMode = value;
            }
        }
        private DateTime GetDateTimeNow()
        {
            DateTime now;
            if (isInReplayMode)
            {
                now = NinjaTrader.Cbi.Connection.PlaybackConnection.Now;
            }
            else
            {
                now = DateTime.Now;
            }
            return now;
        }
        public int PositionCount
        {
            get { return realPositions.Count; }
        }
        public bool IsValidPosition(RealPosition position, Instrument instrument)
        {
            bool returnFlag = false;
            if (position.Instrument.FullName == instrument.FullName && !position.IsFlat())
            {
                returnFlag = true;
            }
            return returnFlag;
        }
        public bool TryGetByIndex(int index, out RealPosition realPosition)
        {
            bool returnFlag = false;
            realPosition = null;
            try
            {
                realPosition = realPositions.ElementAt(index);
                returnFlag = true;
            }
            catch
            {
            }
            return returnFlag;
        }
        public bool TryGetByInstrumentFullName(string instrumentFullName, out RealPosition realPosition)
        {
            bool returnFlag = false;
            realPosition = null;
            int realPositionCount = PositionCount;
            try
            {
                RealPosition tempRealPosition = null;
                for (int index = 0; index < realPositionCount; index++)
                {
                    tempRealPosition = realPositions.ElementAt(index);
                    if (tempRealPosition != null)
                    {
                        if (tempRealPosition.Instrument.FullName == instrumentFullName)
                        {
                            realPosition = tempRealPosition;
                            returnFlag = true;
                            break;
                        }
                    }
                }
            }
            catch
            {
            }
            return returnFlag;
        }
        public RealPosition BuildRealPosition(Account account, Instrument instrument, MarketPosition marketPosition, int quantity, double averagePrice, DateTime createDate)
        {
            RealPosition realPosition = new RealPosition();
            realPosition.Account = account;
            realPosition.Instrument = instrument;
            realPosition.MarketPosition = marketPosition;
            realPosition.Quantity = quantity;
            realPosition.AveragePrice = averagePrice;
            realPosition.CreateDate = createDate;
            return realPosition;
        }
        private int GetNewQuantity(RealPosition existingPosition, RealPosition newPosition)
        {
            int newQuantity = 0;
            if (existingPosition.MarketPosition == MarketPosition.Long)
            {
                if (newPosition.MarketPosition == MarketPosition.Long)
                    newQuantity = existingPosition.Quantity + newPosition.Quantity;
                else
                    newQuantity = existingPosition.Quantity - newPosition.Quantity;
            }
            else if (existingPosition.MarketPosition == MarketPosition.Short)
            {
                if (newPosition.MarketPosition == MarketPosition.Long)
                    newQuantity = existingPosition.Quantity - newPosition.Quantity;
                else
                    newQuantity = existingPosition.Quantity + newPosition.Quantity;
            }
            return newQuantity;
        }
        private MarketPosition FlipMarketPosition(MarketPosition marketPosition)
        {
            MarketPosition newMarketPosition = marketPosition;
            if (marketPosition == MarketPosition.Long)
            {
                newMarketPosition = MarketPosition.Short;
            }
            else if (marketPosition == MarketPosition.Short)
            {
                newMarketPosition = MarketPosition.Long;
            }
            return newMarketPosition;
        }
        public int AddOrUpdatePosition(RealPosition position)
        {
            int positionQuantity = 0;
            lock (realPositionLock)
            {
                RealPosition foundPosition = null;
                if (TryGetByInstrumentFullName(position.Instrument.FullName, out foundPosition))
                {
                    MarketPosition newMarketPosition;
                    int newQuantity = GetNewQuantity(foundPosition, position);
                    if (newQuantity < 0)
                    {
                        newQuantity *= -1; 
                        newMarketPosition = FlipMarketPosition(foundPosition.MarketPosition);
                    }
                    else
                    {
                        newMarketPosition = foundPosition.MarketPosition;
                    }
                    if (newQuantity == 0)
                    {
                        realPositions.Remove(foundPosition);
                    }
                    else
                    {
                        bool isIncreasingPositionQuantity = (newQuantity > foundPosition.Quantity);
                        int quantitySum = foundPosition.Quantity + position.Quantity;
                        double newAveragePrice = foundPosition.AveragePrice;
                        if (isIncreasingPositionQuantity)
                        {
                            newAveragePrice = ((foundPosition.AveragePrice * foundPosition.Quantity) + (position.AveragePrice * position.Quantity)) / quantitySum;
                        }
                        double tickSize = position.Instrument.MasterInstrument.TickSize;
                        int ticksPerPoint = RealInstrumentService.GetTicksPerPoint(tickSize);
                        foundPosition.AveragePrice = newAveragePrice;
                        foundPosition.Quantity = newQuantity;
                        foundPosition.MarketPosition = newMarketPosition;
                        positionQuantity = newQuantity;
                    }
                }
                else
                {
                    realPositions.Add(position);
                    positionQuantity = position.Quantity;
                }
            }
            return positionQuantity;
        }
        public void LoadPositions(Account account)
        {
            lock (realPositionLock)
            {
                lock (account.Positions)
                {
                    realPositions.Clear();
                    foreach (Position positionItem in account.Positions)
                    {
                        RealPosition position = BuildRealPosition(account,
                            positionItem.Instrument,
                            positionItem.MarketPosition,
                            positionItem.Quantity,
                            positionItem.AveragePrice,
                            GetDateTimeNow());
                        AddOrUpdatePosition(position);
                    }
                }
            }
        }
    }
    public class RealPosition
    {
        private const int StateHasChanged = 1;
        private const int StateHasNotChanged = 0;
        private int stateChangeStatus = StateHasChanged;
        private string positionId = null;
        private bool isValid = true;
        private MarketPosition marketPosition = MarketPosition.Flat;
        private double averagePrice = 0;
        private Instrument instrument = null;
        private int quantity = 0;
        private Account account = null;
        private DateTime createDate = DateTime.MinValue;
        public RealPosition()
        {
            this.positionId = Guid.NewGuid().ToString();
        }
        public string PositionId
        {
            get
            {
                return positionId;
            }
        }
        public bool IsValid
        {
            get
            {
                return isValid;
            }
            set
            {
                ChangeStateFlag();
                isValid = value;
            }
        }
        public Account Account
        {
            get
            {
                return account;
            }
            set
            {
                ChangeStateFlag();
                account = value;
            }
        }
        public MarketPosition MarketPosition
        {
            get
            {
                return marketPosition;
            }
            set
            {
                ChangeStateFlag();
                marketPosition = value;
            }
        }
        public double AveragePrice
        {
            get
            {
                return averagePrice;
            }
            set
            {
                ChangeStateFlag();
                averagePrice = value;
            }
        }
        public Instrument Instrument
        {
            get
            {
                return instrument;
            }
            set
            {
                ChangeStateFlag();
                instrument = value;
            }
        }
        public int Quantity
        {
            get
            {
                return quantity;
            }
            set
            {
                ChangeStateFlag();
                quantity = value;
            }
        }
        public DateTime CreateDate
        {
            get
            {
                return createDate;
            }
            set
            {
                ChangeStateFlag();
                createDate = value;
            }
        }
        public bool IsFlat()
        {
            return (marketPosition == MarketPosition.Flat || quantity == 0);
        }
        public bool HasStateChanged()
        {
            return (stateChangeStatus == StateHasChanged);
        }
        public void StoreState()
        {
            ResetStateFlag();
        }
        private void ChangeStateFlag()
        {
            Interlocked.Exchange(ref stateChangeStatus, StateHasChanged);
        }
        private void ResetStateFlag()
        {
            Interlocked.Exchange(ref stateChangeStatus, StateHasNotChanged);
        }
    }
}
#region NinjaScript generated code. Neither change nor remove.
namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private JoiaGestor[] cacheJoiaGestor;
		public JoiaGestor JoiaGestor(string indicatorName, string indicatorTermsOfUse, string indicatorInfoLink, bool useAutoPositionStopLoss, bool useAutoPositionTakeProfit, bool limitAddOnVolumeToInProfit, JoiaGestorCloseAutoTypes autoPositionCloseType, JoiaGestorBreakEvenAutoTypes autoPositionBreakEvenType, int stopLossInitialTicks, double stopLossInitialATRMultiplier, JoiaGestorStopLossSnapTypes stopLossInitialSnapType, int stopLossInitialMaxTicks, double stopLossInitialDollars, bool stopLossInitialDollarsCombined, int stopLossJumpTicks, bool stopLossCTRLJumpTicks, bool stopLossRefreshOnVolumeChange, bool stopLossRefreshManagementEnabled, int breakEvenInitialTicks, int breakEvenJumpTicks, int breakEvenTurboJumpTicks, int breakEvenAutoTriggerTicks, double breakEvenAutoTriggerATRMultiplier, int takeProfitInitialTicks, double takeProfitInitialATRMultiplier, bool takeProfitSyncECATargetPrice, int takeProfitJumpTicks, double takeProfitCtrlSLMultiplier, bool takeProfitRefreshManagementEnabled, bool showAveragePriceLine, bool showAveragePriceLineQuantity, bool showAveragePriceLineQuantityInMicros, int aTRPeriod, int snapPowerBoxPeriod, bool snapPowerBoxAutoAdjustPeriodsOnM1, bool useBlendedInstruments, bool useIntradayMarginCheck, int refreshTPSLPaddingTicks, int refreshTPSLOrderDelaySeconds, int singleOrderChunkMaxQuantity, int singleOrderChunkMinQuantity, int singleOrderChunkDelayMilliseconds, double autoCloseMinProfitDollarsPerVolume, int autoCloseAndTrailMA1Period, int autoCloseAndTrailMA2Period, int autoCloseAndTrailMA3Period, double dayOverMaxLossDollars, double dayOverMaxLossBTBaseRatio, double dayOverAccountBalanceFloorDollars, double eCATargetDollars, double eCATargetDollarsPerOtherVolume, double eCATargetDollarsPerMNQVolume, double eCATargetDollarsPerNQVolume, double eCATargetDollarsPerM2KVolume, double eCATargetDollarsPerRTYVolume, double eCATargetDollarsPerMESVolume, double eCATargetDollarsPerESVolume, double eCATargetDollarsPerMYMVolume, double eCATargetDollarsPerYMVolume, double eCATargetATRMultiplierPerVolume, double eCAMaxDDInDollars, double excessIntradayMarginMinDollars, JoiaGestorEntryVolumeAutoTypes autoEntryVolumeType, int autoEntryVolumeOption1, int autoEntryVolumeOption2, int autoEntryVolumeOption3, int autoEntryVolumeOption4, int autoEntryVolumeOption5, bool useHedgehogEntry, bool hedgehogEntryBuySymbol1SellSymbol2, string hedgehogEntrySymbol1, string hedgehogEntrySymbol2, bool usePositionProfitLogging, int debugLogLevel, int orderWaitOutputThrottleSeconds, bool useAccountInfoLogging, string accountInfoLoggingPath, bool ignoreInstrumentServerSupport, bool showButtonAutoBreakEven, bool showButtonReverse, bool showButtonClose, bool showButtonAutoClose, bool showButtonTPPlus, bool showButtonBEPlus, bool showButtonSLPlus, bool showButtonBuyMarket, bool showButtonSellMarket, bool showButtonVolume)
		{
			return JoiaGestor(Input, indicatorName, indicatorTermsOfUse, indicatorInfoLink, useAutoPositionStopLoss, useAutoPositionTakeProfit, limitAddOnVolumeToInProfit, autoPositionCloseType, autoPositionBreakEvenType, stopLossInitialTicks, stopLossInitialATRMultiplier, stopLossInitialSnapType, stopLossInitialMaxTicks, stopLossInitialDollars, stopLossInitialDollarsCombined, stopLossJumpTicks, stopLossCTRLJumpTicks, stopLossRefreshOnVolumeChange, stopLossRefreshManagementEnabled, breakEvenInitialTicks, breakEvenJumpTicks, breakEvenTurboJumpTicks, breakEvenAutoTriggerTicks, breakEvenAutoTriggerATRMultiplier, takeProfitInitialTicks, takeProfitInitialATRMultiplier, takeProfitSyncECATargetPrice, takeProfitJumpTicks, takeProfitCtrlSLMultiplier, takeProfitRefreshManagementEnabled, showAveragePriceLine, showAveragePriceLineQuantity, showAveragePriceLineQuantityInMicros, aTRPeriod, snapPowerBoxPeriod, snapPowerBoxAutoAdjustPeriodsOnM1, useBlendedInstruments, useIntradayMarginCheck, refreshTPSLPaddingTicks, refreshTPSLOrderDelaySeconds, singleOrderChunkMaxQuantity, singleOrderChunkMinQuantity, singleOrderChunkDelayMilliseconds, autoCloseMinProfitDollarsPerVolume, autoCloseAndTrailMA1Period, autoCloseAndTrailMA2Period, autoCloseAndTrailMA3Period, dayOverMaxLossDollars, dayOverMaxLossBTBaseRatio, dayOverAccountBalanceFloorDollars, eCATargetDollars, eCATargetDollarsPerOtherVolume, eCATargetDollarsPerMNQVolume, eCATargetDollarsPerNQVolume, eCATargetDollarsPerM2KVolume, eCATargetDollarsPerRTYVolume, eCATargetDollarsPerMESVolume, eCATargetDollarsPerESVolume, eCATargetDollarsPerMYMVolume, eCATargetDollarsPerYMVolume, eCATargetATRMultiplierPerVolume, eCAMaxDDInDollars, excessIntradayMarginMinDollars, autoEntryVolumeType, autoEntryVolumeOption1, autoEntryVolumeOption2, autoEntryVolumeOption3, autoEntryVolumeOption4, autoEntryVolumeOption5, useHedgehogEntry, hedgehogEntryBuySymbol1SellSymbol2, hedgehogEntrySymbol1, hedgehogEntrySymbol2, usePositionProfitLogging, debugLogLevel, orderWaitOutputThrottleSeconds, useAccountInfoLogging, accountInfoLoggingPath, ignoreInstrumentServerSupport, showButtonAutoBreakEven, showButtonReverse, showButtonClose, showButtonAutoClose, showButtonTPPlus, showButtonBEPlus, showButtonSLPlus, showButtonBuyMarket, showButtonSellMarket, showButtonVolume);
		}
		public JoiaGestor JoiaGestor(ISeries<double> input, string indicatorName, string indicatorTermsOfUse, string indicatorInfoLink, bool useAutoPositionStopLoss, bool useAutoPositionTakeProfit, bool limitAddOnVolumeToInProfit, JoiaGestorCloseAutoTypes autoPositionCloseType, JoiaGestorBreakEvenAutoTypes autoPositionBreakEvenType, int stopLossInitialTicks, double stopLossInitialATRMultiplier, JoiaGestorStopLossSnapTypes stopLossInitialSnapType, int stopLossInitialMaxTicks, double stopLossInitialDollars, bool stopLossInitialDollarsCombined, int stopLossJumpTicks, bool stopLossCTRLJumpTicks, bool stopLossRefreshOnVolumeChange, bool stopLossRefreshManagementEnabled, int breakEvenInitialTicks, int breakEvenJumpTicks, int breakEvenTurboJumpTicks, int breakEvenAutoTriggerTicks, double breakEvenAutoTriggerATRMultiplier, int takeProfitInitialTicks, double takeProfitInitialATRMultiplier, bool takeProfitSyncECATargetPrice, int takeProfitJumpTicks, double takeProfitCtrlSLMultiplier, bool takeProfitRefreshManagementEnabled, bool showAveragePriceLine, bool showAveragePriceLineQuantity, bool showAveragePriceLineQuantityInMicros, int aTRPeriod, int snapPowerBoxPeriod, bool snapPowerBoxAutoAdjustPeriodsOnM1, bool useBlendedInstruments, bool useIntradayMarginCheck, int refreshTPSLPaddingTicks, int refreshTPSLOrderDelaySeconds, int singleOrderChunkMaxQuantity, int singleOrderChunkMinQuantity, int singleOrderChunkDelayMilliseconds, double autoCloseMinProfitDollarsPerVolume, int autoCloseAndTrailMA1Period, int autoCloseAndTrailMA2Period, int autoCloseAndTrailMA3Period, double dayOverMaxLossDollars, double dayOverMaxLossBTBaseRatio, double dayOverAccountBalanceFloorDollars, double eCATargetDollars, double eCATargetDollarsPerOtherVolume, double eCATargetDollarsPerMNQVolume, double eCATargetDollarsPerNQVolume, double eCATargetDollarsPerM2KVolume, double eCATargetDollarsPerRTYVolume, double eCATargetDollarsPerMESVolume, double eCATargetDollarsPerESVolume, double eCATargetDollarsPerMYMVolume, double eCATargetDollarsPerYMVolume, double eCATargetATRMultiplierPerVolume, double eCAMaxDDInDollars, double excessIntradayMarginMinDollars, JoiaGestorEntryVolumeAutoTypes autoEntryVolumeType, int autoEntryVolumeOption1, int autoEntryVolumeOption2, int autoEntryVolumeOption3, int autoEntryVolumeOption4, int autoEntryVolumeOption5, bool useHedgehogEntry, bool hedgehogEntryBuySymbol1SellSymbol2, string hedgehogEntrySymbol1, string hedgehogEntrySymbol2, bool usePositionProfitLogging, int debugLogLevel, int orderWaitOutputThrottleSeconds, bool useAccountInfoLogging, string accountInfoLoggingPath, bool ignoreInstrumentServerSupport, bool showButtonAutoBreakEven, bool showButtonReverse, bool showButtonClose, bool showButtonAutoClose, bool showButtonTPPlus, bool showButtonBEPlus, bool showButtonSLPlus, bool showButtonBuyMarket, bool showButtonSellMarket, bool showButtonVolume)
		{
			if (cacheJoiaGestor != null)
				for (int idx = 0; idx < cacheJoiaGestor.Length; idx++)
					if (cacheJoiaGestor[idx] != null && cacheJoiaGestor[idx].IndicatorName == indicatorName && cacheJoiaGestor[idx].IndicatorTermsOfUse == indicatorTermsOfUse && cacheJoiaGestor[idx].IndicatorInfoLink == indicatorInfoLink && cacheJoiaGestor[idx].UseAutoPositionStopLoss == useAutoPositionStopLoss && cacheJoiaGestor[idx].UseAutoPositionTakeProfit == useAutoPositionTakeProfit && cacheJoiaGestor[idx].LimitAddOnVolumeToInProfit == limitAddOnVolumeToInProfit && cacheJoiaGestor[idx].AutoPositionCloseType == autoPositionCloseType && cacheJoiaGestor[idx].AutoPositionBreakEvenType == autoPositionBreakEvenType && cacheJoiaGestor[idx].StopLossInitialTicks == stopLossInitialTicks && cacheJoiaGestor[idx].StopLossInitialATRMultiplier == stopLossInitialATRMultiplier && cacheJoiaGestor[idx].StopLossInitialSnapType == stopLossInitialSnapType && cacheJoiaGestor[idx].StopLossInitialMaxTicks == stopLossInitialMaxTicks && cacheJoiaGestor[idx].StopLossInitialDollars == stopLossInitialDollars && cacheJoiaGestor[idx].StopLossInitialDollarsCombined == stopLossInitialDollarsCombined && cacheJoiaGestor[idx].StopLossJumpTicks == stopLossJumpTicks && cacheJoiaGestor[idx].StopLossCTRLJumpTicks == stopLossCTRLJumpTicks && cacheJoiaGestor[idx].StopLossRefreshOnVolumeChange == stopLossRefreshOnVolumeChange && cacheJoiaGestor[idx].StopLossRefreshManagementEnabled == stopLossRefreshManagementEnabled && cacheJoiaGestor[idx].BreakEvenInitialTicks == breakEvenInitialTicks && cacheJoiaGestor[idx].BreakEvenJumpTicks == breakEvenJumpTicks && cacheJoiaGestor[idx].BreakEvenTurboJumpTicks == breakEvenTurboJumpTicks && cacheJoiaGestor[idx].BreakEvenAutoTriggerTicks == breakEvenAutoTriggerTicks && cacheJoiaGestor[idx].BreakEvenAutoTriggerATRMultiplier == breakEvenAutoTriggerATRMultiplier && cacheJoiaGestor[idx].TakeProfitInitialTicks == takeProfitInitialTicks && cacheJoiaGestor[idx].TakeProfitInitialATRMultiplier == takeProfitInitialATRMultiplier && cacheJoiaGestor[idx].TakeProfitSyncECATargetPrice == takeProfitSyncECATargetPrice && cacheJoiaGestor[idx].TakeProfitJumpTicks == takeProfitJumpTicks && cacheJoiaGestor[idx].TakeProfitCtrlSLMultiplier == takeProfitCtrlSLMultiplier && cacheJoiaGestor[idx].TakeProfitRefreshManagementEnabled == takeProfitRefreshManagementEnabled && cacheJoiaGestor[idx].ShowAveragePriceLine == showAveragePriceLine && cacheJoiaGestor[idx].ShowAveragePriceLineQuantity == showAveragePriceLineQuantity && cacheJoiaGestor[idx].ShowAveragePriceLineQuantityInMicros == showAveragePriceLineQuantityInMicros && cacheJoiaGestor[idx].ATRPeriod == aTRPeriod && cacheJoiaGestor[idx].SnapPowerBoxPeriod == snapPowerBoxPeriod && cacheJoiaGestor[idx].SnapPowerBoxAutoAdjustPeriodsOnM1 == snapPowerBoxAutoAdjustPeriodsOnM1 && cacheJoiaGestor[idx].UseBlendedInstruments == useBlendedInstruments && cacheJoiaGestor[idx].UseIntradayMarginCheck == useIntradayMarginCheck && cacheJoiaGestor[idx].RefreshTPSLPaddingTicks == refreshTPSLPaddingTicks && cacheJoiaGestor[idx].RefreshTPSLOrderDelaySeconds == refreshTPSLOrderDelaySeconds && cacheJoiaGestor[idx].SingleOrderChunkMaxQuantity == singleOrderChunkMaxQuantity && cacheJoiaGestor[idx].SingleOrderChunkMinQuantity == singleOrderChunkMinQuantity && cacheJoiaGestor[idx].SingleOrderChunkDelayMilliseconds == singleOrderChunkDelayMilliseconds && cacheJoiaGestor[idx].AutoCloseMinProfitDollarsPerVolume == autoCloseMinProfitDollarsPerVolume && cacheJoiaGestor[idx].AutoCloseAndTrailMA1Period == autoCloseAndTrailMA1Period && cacheJoiaGestor[idx].AutoCloseAndTrailMA2Period == autoCloseAndTrailMA2Period && cacheJoiaGestor[idx].AutoCloseAndTrailMA3Period == autoCloseAndTrailMA3Period && cacheJoiaGestor[idx].DayOverMaxLossDollars == dayOverMaxLossDollars && cacheJoiaGestor[idx].DayOverMaxLossBTBaseRatio == dayOverMaxLossBTBaseRatio && cacheJoiaGestor[idx].DayOverAccountBalanceFloorDollars == dayOverAccountBalanceFloorDollars && cacheJoiaGestor[idx].ECATargetDollars == eCATargetDollars && cacheJoiaGestor[idx].ECATargetDollarsPerOtherVolume == eCATargetDollarsPerOtherVolume && cacheJoiaGestor[idx].ECATargetDollarsPerMNQVolume == eCATargetDollarsPerMNQVolume && cacheJoiaGestor[idx].ECATargetDollarsPerNQVolume == eCATargetDollarsPerNQVolume && cacheJoiaGestor[idx].ECATargetDollarsPerM2KVolume == eCATargetDollarsPerM2KVolume && cacheJoiaGestor[idx].ECATargetDollarsPerRTYVolume == eCATargetDollarsPerRTYVolume && cacheJoiaGestor[idx].ECATargetDollarsPerMESVolume == eCATargetDollarsPerMESVolume && cacheJoiaGestor[idx].ECATargetDollarsPerESVolume == eCATargetDollarsPerESVolume && cacheJoiaGestor[idx].ECATargetDollarsPerMYMVolume == eCATargetDollarsPerMYMVolume && cacheJoiaGestor[idx].ECATargetDollarsPerYMVolume == eCATargetDollarsPerYMVolume && cacheJoiaGestor[idx].ECATargetATRMultiplierPerVolume == eCATargetATRMultiplierPerVolume && cacheJoiaGestor[idx].ECAMaxDDInDollars == eCAMaxDDInDollars && cacheJoiaGestor[idx].ExcessIntradayMarginMinDollars == excessIntradayMarginMinDollars && cacheJoiaGestor[idx].AutoEntryVolumeType == autoEntryVolumeType && cacheJoiaGestor[idx].AutoEntryVolumeOption1 == autoEntryVolumeOption1 && cacheJoiaGestor[idx].AutoEntryVolumeOption2 == autoEntryVolumeOption2 && cacheJoiaGestor[idx].AutoEntryVolumeOption3 == autoEntryVolumeOption3 && cacheJoiaGestor[idx].AutoEntryVolumeOption4 == autoEntryVolumeOption4 && cacheJoiaGestor[idx].AutoEntryVolumeOption5 == autoEntryVolumeOption5 && cacheJoiaGestor[idx].UseHedgehogEntry == useHedgehogEntry && cacheJoiaGestor[idx].HedgehogEntryBuySymbol1SellSymbol2 == hedgehogEntryBuySymbol1SellSymbol2 && cacheJoiaGestor[idx].HedgehogEntrySymbol1 == hedgehogEntrySymbol1 && cacheJoiaGestor[idx].HedgehogEntrySymbol2 == hedgehogEntrySymbol2 && cacheJoiaGestor[idx].UsePositionProfitLogging == usePositionProfitLogging && cacheJoiaGestor[idx].DebugLogLevel == debugLogLevel && cacheJoiaGestor[idx].OrderWaitOutputThrottleSeconds == orderWaitOutputThrottleSeconds && cacheJoiaGestor[idx].UseAccountInfoLogging == useAccountInfoLogging && cacheJoiaGestor[idx].AccountInfoLoggingPath == accountInfoLoggingPath && cacheJoiaGestor[idx].IgnoreInstrumentServerSupport == ignoreInstrumentServerSupport && cacheJoiaGestor[idx].ShowButtonAutoBreakEven == showButtonAutoBreakEven && cacheJoiaGestor[idx].ShowButtonReverse == showButtonReverse && cacheJoiaGestor[idx].ShowButtonClose == showButtonClose && cacheJoiaGestor[idx].ShowButtonAutoClose == showButtonAutoClose && cacheJoiaGestor[idx].ShowButtonTPPlus == showButtonTPPlus && cacheJoiaGestor[idx].ShowButtonBEPlus == showButtonBEPlus && cacheJoiaGestor[idx].ShowButtonSLPlus == showButtonSLPlus && cacheJoiaGestor[idx].ShowButtonBuyMarket == showButtonBuyMarket && cacheJoiaGestor[idx].ShowButtonSellMarket == showButtonSellMarket && cacheJoiaGestor[idx].ShowButtonVolume == showButtonVolume && cacheJoiaGestor[idx].EqualsInput(input))
						return cacheJoiaGestor[idx];
			return CacheIndicator<JoiaGestor>(new JoiaGestor(){ IndicatorName = indicatorName, IndicatorTermsOfUse = indicatorTermsOfUse, IndicatorInfoLink = indicatorInfoLink, UseAutoPositionStopLoss = useAutoPositionStopLoss, UseAutoPositionTakeProfit = useAutoPositionTakeProfit, LimitAddOnVolumeToInProfit = limitAddOnVolumeToInProfit, AutoPositionCloseType = autoPositionCloseType, AutoPositionBreakEvenType = autoPositionBreakEvenType, StopLossInitialTicks = stopLossInitialTicks, StopLossInitialATRMultiplier = stopLossInitialATRMultiplier, StopLossInitialSnapType = stopLossInitialSnapType, StopLossInitialMaxTicks = stopLossInitialMaxTicks, StopLossInitialDollars = stopLossInitialDollars, StopLossInitialDollarsCombined = stopLossInitialDollarsCombined, StopLossJumpTicks = stopLossJumpTicks, StopLossCTRLJumpTicks = stopLossCTRLJumpTicks, StopLossRefreshOnVolumeChange = stopLossRefreshOnVolumeChange, StopLossRefreshManagementEnabled = stopLossRefreshManagementEnabled, BreakEvenInitialTicks = breakEvenInitialTicks, BreakEvenJumpTicks = breakEvenJumpTicks, BreakEvenTurboJumpTicks = breakEvenTurboJumpTicks, BreakEvenAutoTriggerTicks = breakEvenAutoTriggerTicks, BreakEvenAutoTriggerATRMultiplier = breakEvenAutoTriggerATRMultiplier, TakeProfitInitialTicks = takeProfitInitialTicks, TakeProfitInitialATRMultiplier = takeProfitInitialATRMultiplier, TakeProfitSyncECATargetPrice = takeProfitSyncECATargetPrice, TakeProfitJumpTicks = takeProfitJumpTicks, TakeProfitCtrlSLMultiplier = takeProfitCtrlSLMultiplier, TakeProfitRefreshManagementEnabled = takeProfitRefreshManagementEnabled, ShowAveragePriceLine = showAveragePriceLine, ShowAveragePriceLineQuantity = showAveragePriceLineQuantity, ShowAveragePriceLineQuantityInMicros = showAveragePriceLineQuantityInMicros, ATRPeriod = aTRPeriod, SnapPowerBoxPeriod = snapPowerBoxPeriod, SnapPowerBoxAutoAdjustPeriodsOnM1 = snapPowerBoxAutoAdjustPeriodsOnM1, UseBlendedInstruments = useBlendedInstruments, UseIntradayMarginCheck = useIntradayMarginCheck, RefreshTPSLPaddingTicks = refreshTPSLPaddingTicks, RefreshTPSLOrderDelaySeconds = refreshTPSLOrderDelaySeconds, SingleOrderChunkMaxQuantity = singleOrderChunkMaxQuantity, SingleOrderChunkMinQuantity = singleOrderChunkMinQuantity, SingleOrderChunkDelayMilliseconds = singleOrderChunkDelayMilliseconds, AutoCloseMinProfitDollarsPerVolume = autoCloseMinProfitDollarsPerVolume, AutoCloseAndTrailMA1Period = autoCloseAndTrailMA1Period, AutoCloseAndTrailMA2Period = autoCloseAndTrailMA2Period, AutoCloseAndTrailMA3Period = autoCloseAndTrailMA3Period, DayOverMaxLossDollars = dayOverMaxLossDollars, DayOverMaxLossBTBaseRatio = dayOverMaxLossBTBaseRatio, DayOverAccountBalanceFloorDollars = dayOverAccountBalanceFloorDollars, ECATargetDollars = eCATargetDollars, ECATargetDollarsPerOtherVolume = eCATargetDollarsPerOtherVolume, ECATargetDollarsPerMNQVolume = eCATargetDollarsPerMNQVolume, ECATargetDollarsPerNQVolume = eCATargetDollarsPerNQVolume, ECATargetDollarsPerM2KVolume = eCATargetDollarsPerM2KVolume, ECATargetDollarsPerRTYVolume = eCATargetDollarsPerRTYVolume, ECATargetDollarsPerMESVolume = eCATargetDollarsPerMESVolume, ECATargetDollarsPerESVolume = eCATargetDollarsPerESVolume, ECATargetDollarsPerMYMVolume = eCATargetDollarsPerMYMVolume, ECATargetDollarsPerYMVolume = eCATargetDollarsPerYMVolume, ECATargetATRMultiplierPerVolume = eCATargetATRMultiplierPerVolume, ECAMaxDDInDollars = eCAMaxDDInDollars, ExcessIntradayMarginMinDollars = excessIntradayMarginMinDollars, AutoEntryVolumeType = autoEntryVolumeType, AutoEntryVolumeOption1 = autoEntryVolumeOption1, AutoEntryVolumeOption2 = autoEntryVolumeOption2, AutoEntryVolumeOption3 = autoEntryVolumeOption3, AutoEntryVolumeOption4 = autoEntryVolumeOption4, AutoEntryVolumeOption5 = autoEntryVolumeOption5, UseHedgehogEntry = useHedgehogEntry, HedgehogEntryBuySymbol1SellSymbol2 = hedgehogEntryBuySymbol1SellSymbol2, HedgehogEntrySymbol1 = hedgehogEntrySymbol1, HedgehogEntrySymbol2 = hedgehogEntrySymbol2, UsePositionProfitLogging = usePositionProfitLogging, DebugLogLevel = debugLogLevel, OrderWaitOutputThrottleSeconds = orderWaitOutputThrottleSeconds, UseAccountInfoLogging = useAccountInfoLogging, AccountInfoLoggingPath = accountInfoLoggingPath, IgnoreInstrumentServerSupport = ignoreInstrumentServerSupport, ShowButtonAutoBreakEven = showButtonAutoBreakEven, ShowButtonReverse = showButtonReverse, ShowButtonClose = showButtonClose, ShowButtonAutoClose = showButtonAutoClose, ShowButtonTPPlus = showButtonTPPlus, ShowButtonBEPlus = showButtonBEPlus, ShowButtonSLPlus = showButtonSLPlus, ShowButtonBuyMarket = showButtonBuyMarket, ShowButtonSellMarket = showButtonSellMarket, ShowButtonVolume = showButtonVolume }, input, ref cacheJoiaGestor);
		}
	}
}
namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.JoiaGestor JoiaGestor(string indicatorName, string indicatorTermsOfUse, string indicatorInfoLink, bool useAutoPositionStopLoss, bool useAutoPositionTakeProfit, bool limitAddOnVolumeToInProfit, JoiaGestorCloseAutoTypes autoPositionCloseType, JoiaGestorBreakEvenAutoTypes autoPositionBreakEvenType, int stopLossInitialTicks, double stopLossInitialATRMultiplier, JoiaGestorStopLossSnapTypes stopLossInitialSnapType, int stopLossInitialMaxTicks, double stopLossInitialDollars, bool stopLossInitialDollarsCombined, int stopLossJumpTicks, bool stopLossCTRLJumpTicks, bool stopLossRefreshOnVolumeChange, bool stopLossRefreshManagementEnabled, int breakEvenInitialTicks, int breakEvenJumpTicks, int breakEvenTurboJumpTicks, int breakEvenAutoTriggerTicks, double breakEvenAutoTriggerATRMultiplier, int takeProfitInitialTicks, double takeProfitInitialATRMultiplier, bool takeProfitSyncECATargetPrice, int takeProfitJumpTicks, double takeProfitCtrlSLMultiplier, bool takeProfitRefreshManagementEnabled, bool showAveragePriceLine, bool showAveragePriceLineQuantity, bool showAveragePriceLineQuantityInMicros, int aTRPeriod, int snapPowerBoxPeriod, bool snapPowerBoxAutoAdjustPeriodsOnM1, bool useBlendedInstruments, bool useIntradayMarginCheck, int refreshTPSLPaddingTicks, int refreshTPSLOrderDelaySeconds, int singleOrderChunkMaxQuantity, int singleOrderChunkMinQuantity, int singleOrderChunkDelayMilliseconds, double autoCloseMinProfitDollarsPerVolume, int autoCloseAndTrailMA1Period, int autoCloseAndTrailMA2Period, int autoCloseAndTrailMA3Period, double dayOverMaxLossDollars, double dayOverMaxLossBTBaseRatio, double dayOverAccountBalanceFloorDollars, double eCATargetDollars, double eCATargetDollarsPerOtherVolume, double eCATargetDollarsPerMNQVolume, double eCATargetDollarsPerNQVolume, double eCATargetDollarsPerM2KVolume, double eCATargetDollarsPerRTYVolume, double eCATargetDollarsPerMESVolume, double eCATargetDollarsPerESVolume, double eCATargetDollarsPerMYMVolume, double eCATargetDollarsPerYMVolume, double eCATargetATRMultiplierPerVolume, double eCAMaxDDInDollars, double excessIntradayMarginMinDollars, JoiaGestorEntryVolumeAutoTypes autoEntryVolumeType, int autoEntryVolumeOption1, int autoEntryVolumeOption2, int autoEntryVolumeOption3, int autoEntryVolumeOption4, int autoEntryVolumeOption5, bool useHedgehogEntry, bool hedgehogEntryBuySymbol1SellSymbol2, string hedgehogEntrySymbol1, string hedgehogEntrySymbol2, bool usePositionProfitLogging, int debugLogLevel, int orderWaitOutputThrottleSeconds, bool useAccountInfoLogging, string accountInfoLoggingPath, bool ignoreInstrumentServerSupport, bool showButtonAutoBreakEven, bool showButtonReverse, bool showButtonClose, bool showButtonAutoClose, bool showButtonTPPlus, bool showButtonBEPlus, bool showButtonSLPlus, bool showButtonBuyMarket, bool showButtonSellMarket, bool showButtonVolume)
		{
			return indicator.JoiaGestor(Input, indicatorName, indicatorTermsOfUse, indicatorInfoLink, useAutoPositionStopLoss, useAutoPositionTakeProfit, limitAddOnVolumeToInProfit, autoPositionCloseType, autoPositionBreakEvenType, stopLossInitialTicks, stopLossInitialATRMultiplier, stopLossInitialSnapType, stopLossInitialMaxTicks, stopLossInitialDollars, stopLossInitialDollarsCombined, stopLossJumpTicks, stopLossCTRLJumpTicks, stopLossRefreshOnVolumeChange, stopLossRefreshManagementEnabled, breakEvenInitialTicks, breakEvenJumpTicks, breakEvenTurboJumpTicks, breakEvenAutoTriggerTicks, breakEvenAutoTriggerATRMultiplier, takeProfitInitialTicks, takeProfitInitialATRMultiplier, takeProfitSyncECATargetPrice, takeProfitJumpTicks, takeProfitCtrlSLMultiplier, takeProfitRefreshManagementEnabled, showAveragePriceLine, showAveragePriceLineQuantity, showAveragePriceLineQuantityInMicros, aTRPeriod, snapPowerBoxPeriod, snapPowerBoxAutoAdjustPeriodsOnM1, useBlendedInstruments, useIntradayMarginCheck, refreshTPSLPaddingTicks, refreshTPSLOrderDelaySeconds, singleOrderChunkMaxQuantity, singleOrderChunkMinQuantity, singleOrderChunkDelayMilliseconds, autoCloseMinProfitDollarsPerVolume, autoCloseAndTrailMA1Period, autoCloseAndTrailMA2Period, autoCloseAndTrailMA3Period, dayOverMaxLossDollars, dayOverMaxLossBTBaseRatio, dayOverAccountBalanceFloorDollars, eCATargetDollars, eCATargetDollarsPerOtherVolume, eCATargetDollarsPerMNQVolume, eCATargetDollarsPerNQVolume, eCATargetDollarsPerM2KVolume, eCATargetDollarsPerRTYVolume, eCATargetDollarsPerMESVolume, eCATargetDollarsPerESVolume, eCATargetDollarsPerMYMVolume, eCATargetDollarsPerYMVolume, eCATargetATRMultiplierPerVolume, eCAMaxDDInDollars, excessIntradayMarginMinDollars, autoEntryVolumeType, autoEntryVolumeOption1, autoEntryVolumeOption2, autoEntryVolumeOption3, autoEntryVolumeOption4, autoEntryVolumeOption5, useHedgehogEntry, hedgehogEntryBuySymbol1SellSymbol2, hedgehogEntrySymbol1, hedgehogEntrySymbol2, usePositionProfitLogging, debugLogLevel, orderWaitOutputThrottleSeconds, useAccountInfoLogging, accountInfoLoggingPath, ignoreInstrumentServerSupport, showButtonAutoBreakEven, showButtonReverse, showButtonClose, showButtonAutoClose, showButtonTPPlus, showButtonBEPlus, showButtonSLPlus, showButtonBuyMarket, showButtonSellMarket, showButtonVolume);
		}
		public Indicators.JoiaGestor JoiaGestor(ISeries<double> input , string indicatorName, string indicatorTermsOfUse, string indicatorInfoLink, bool useAutoPositionStopLoss, bool useAutoPositionTakeProfit, bool limitAddOnVolumeToInProfit, JoiaGestorCloseAutoTypes autoPositionCloseType, JoiaGestorBreakEvenAutoTypes autoPositionBreakEvenType, int stopLossInitialTicks, double stopLossInitialATRMultiplier, JoiaGestorStopLossSnapTypes stopLossInitialSnapType, int stopLossInitialMaxTicks, double stopLossInitialDollars, bool stopLossInitialDollarsCombined, int stopLossJumpTicks, bool stopLossCTRLJumpTicks, bool stopLossRefreshOnVolumeChange, bool stopLossRefreshManagementEnabled, int breakEvenInitialTicks, int breakEvenJumpTicks, int breakEvenTurboJumpTicks, int breakEvenAutoTriggerTicks, double breakEvenAutoTriggerATRMultiplier, int takeProfitInitialTicks, double takeProfitInitialATRMultiplier, bool takeProfitSyncECATargetPrice, int takeProfitJumpTicks, double takeProfitCtrlSLMultiplier, bool takeProfitRefreshManagementEnabled, bool showAveragePriceLine, bool showAveragePriceLineQuantity, bool showAveragePriceLineQuantityInMicros, int aTRPeriod, int snapPowerBoxPeriod, bool snapPowerBoxAutoAdjustPeriodsOnM1, bool useBlendedInstruments, bool useIntradayMarginCheck, int refreshTPSLPaddingTicks, int refreshTPSLOrderDelaySeconds, int singleOrderChunkMaxQuantity, int singleOrderChunkMinQuantity, int singleOrderChunkDelayMilliseconds, double autoCloseMinProfitDollarsPerVolume, int autoCloseAndTrailMA1Period, int autoCloseAndTrailMA2Period, int autoCloseAndTrailMA3Period, double dayOverMaxLossDollars, double dayOverMaxLossBTBaseRatio, double dayOverAccountBalanceFloorDollars, double eCATargetDollars, double eCATargetDollarsPerOtherVolume, double eCATargetDollarsPerMNQVolume, double eCATargetDollarsPerNQVolume, double eCATargetDollarsPerM2KVolume, double eCATargetDollarsPerRTYVolume, double eCATargetDollarsPerMESVolume, double eCATargetDollarsPerESVolume, double eCATargetDollarsPerMYMVolume, double eCATargetDollarsPerYMVolume, double eCATargetATRMultiplierPerVolume, double eCAMaxDDInDollars, double excessIntradayMarginMinDollars, JoiaGestorEntryVolumeAutoTypes autoEntryVolumeType, int autoEntryVolumeOption1, int autoEntryVolumeOption2, int autoEntryVolumeOption3, int autoEntryVolumeOption4, int autoEntryVolumeOption5, bool useHedgehogEntry, bool hedgehogEntryBuySymbol1SellSymbol2, string hedgehogEntrySymbol1, string hedgehogEntrySymbol2, bool usePositionProfitLogging, int debugLogLevel, int orderWaitOutputThrottleSeconds, bool useAccountInfoLogging, string accountInfoLoggingPath, bool ignoreInstrumentServerSupport, bool showButtonAutoBreakEven, bool showButtonReverse, bool showButtonClose, bool showButtonAutoClose, bool showButtonTPPlus, bool showButtonBEPlus, bool showButtonSLPlus, bool showButtonBuyMarket, bool showButtonSellMarket, bool showButtonVolume)
		{
			return indicator.JoiaGestor(input, indicatorName, indicatorTermsOfUse, indicatorInfoLink, useAutoPositionStopLoss, useAutoPositionTakeProfit, limitAddOnVolumeToInProfit, autoPositionCloseType, autoPositionBreakEvenType, stopLossInitialTicks, stopLossInitialATRMultiplier, stopLossInitialSnapType, stopLossInitialMaxTicks, stopLossInitialDollars, stopLossInitialDollarsCombined, stopLossJumpTicks, stopLossCTRLJumpTicks, stopLossRefreshOnVolumeChange, stopLossRefreshManagementEnabled, breakEvenInitialTicks, breakEvenJumpTicks, breakEvenTurboJumpTicks, breakEvenAutoTriggerTicks, breakEvenAutoTriggerATRMultiplier, takeProfitInitialTicks, takeProfitInitialATRMultiplier, takeProfitSyncECATargetPrice, takeProfitJumpTicks, takeProfitCtrlSLMultiplier, takeProfitRefreshManagementEnabled, showAveragePriceLine, showAveragePriceLineQuantity, showAveragePriceLineQuantityInMicros, aTRPeriod, snapPowerBoxPeriod, snapPowerBoxAutoAdjustPeriodsOnM1, useBlendedInstruments, useIntradayMarginCheck, refreshTPSLPaddingTicks, refreshTPSLOrderDelaySeconds, singleOrderChunkMaxQuantity, singleOrderChunkMinQuantity, singleOrderChunkDelayMilliseconds, autoCloseMinProfitDollarsPerVolume, autoCloseAndTrailMA1Period, autoCloseAndTrailMA2Period, autoCloseAndTrailMA3Period, dayOverMaxLossDollars, dayOverMaxLossBTBaseRatio, dayOverAccountBalanceFloorDollars, eCATargetDollars, eCATargetDollarsPerOtherVolume, eCATargetDollarsPerMNQVolume, eCATargetDollarsPerNQVolume, eCATargetDollarsPerM2KVolume, eCATargetDollarsPerRTYVolume, eCATargetDollarsPerMESVolume, eCATargetDollarsPerESVolume, eCATargetDollarsPerMYMVolume, eCATargetDollarsPerYMVolume, eCATargetATRMultiplierPerVolume, eCAMaxDDInDollars, excessIntradayMarginMinDollars, autoEntryVolumeType, autoEntryVolumeOption1, autoEntryVolumeOption2, autoEntryVolumeOption3, autoEntryVolumeOption4, autoEntryVolumeOption5, useHedgehogEntry, hedgehogEntryBuySymbol1SellSymbol2, hedgehogEntrySymbol1, hedgehogEntrySymbol2, usePositionProfitLogging, debugLogLevel, orderWaitOutputThrottleSeconds, useAccountInfoLogging, accountInfoLoggingPath, ignoreInstrumentServerSupport, showButtonAutoBreakEven, showButtonReverse, showButtonClose, showButtonAutoClose, showButtonTPPlus, showButtonBEPlus, showButtonSLPlus, showButtonBuyMarket, showButtonSellMarket, showButtonVolume);
		}
	}
}
namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.JoiaGestor JoiaGestor(string indicatorName, string indicatorTermsOfUse, string indicatorInfoLink, bool useAutoPositionStopLoss, bool useAutoPositionTakeProfit, bool limitAddOnVolumeToInProfit, JoiaGestorCloseAutoTypes autoPositionCloseType, JoiaGestorBreakEvenAutoTypes autoPositionBreakEvenType, int stopLossInitialTicks, double stopLossInitialATRMultiplier, JoiaGestorStopLossSnapTypes stopLossInitialSnapType, int stopLossInitialMaxTicks, double stopLossInitialDollars, bool stopLossInitialDollarsCombined, int stopLossJumpTicks, bool stopLossCTRLJumpTicks, bool stopLossRefreshOnVolumeChange, bool stopLossRefreshManagementEnabled, int breakEvenInitialTicks, int breakEvenJumpTicks, int breakEvenTurboJumpTicks, int breakEvenAutoTriggerTicks, double breakEvenAutoTriggerATRMultiplier, int takeProfitInitialTicks, double takeProfitInitialATRMultiplier, bool takeProfitSyncECATargetPrice, int takeProfitJumpTicks, double takeProfitCtrlSLMultiplier, bool takeProfitRefreshManagementEnabled, bool showAveragePriceLine, bool showAveragePriceLineQuantity, bool showAveragePriceLineQuantityInMicros, int aTRPeriod, int snapPowerBoxPeriod, bool snapPowerBoxAutoAdjustPeriodsOnM1, bool useBlendedInstruments, bool useIntradayMarginCheck, int refreshTPSLPaddingTicks, int refreshTPSLOrderDelaySeconds, int singleOrderChunkMaxQuantity, int singleOrderChunkMinQuantity, int singleOrderChunkDelayMilliseconds, double autoCloseMinProfitDollarsPerVolume, int autoCloseAndTrailMA1Period, int autoCloseAndTrailMA2Period, int autoCloseAndTrailMA3Period, double dayOverMaxLossDollars, double dayOverMaxLossBTBaseRatio, double dayOverAccountBalanceFloorDollars, double eCATargetDollars, double eCATargetDollarsPerOtherVolume, double eCATargetDollarsPerMNQVolume, double eCATargetDollarsPerNQVolume, double eCATargetDollarsPerM2KVolume, double eCATargetDollarsPerRTYVolume, double eCATargetDollarsPerMESVolume, double eCATargetDollarsPerESVolume, double eCATargetDollarsPerMYMVolume, double eCATargetDollarsPerYMVolume, double eCATargetATRMultiplierPerVolume, double eCAMaxDDInDollars, double excessIntradayMarginMinDollars, JoiaGestorEntryVolumeAutoTypes autoEntryVolumeType, int autoEntryVolumeOption1, int autoEntryVolumeOption2, int autoEntryVolumeOption3, int autoEntryVolumeOption4, int autoEntryVolumeOption5, bool useHedgehogEntry, bool hedgehogEntryBuySymbol1SellSymbol2, string hedgehogEntrySymbol1, string hedgehogEntrySymbol2, bool usePositionProfitLogging, int debugLogLevel, int orderWaitOutputThrottleSeconds, bool useAccountInfoLogging, string accountInfoLoggingPath, bool ignoreInstrumentServerSupport, bool showButtonAutoBreakEven, bool showButtonReverse, bool showButtonClose, bool showButtonAutoClose, bool showButtonTPPlus, bool showButtonBEPlus, bool showButtonSLPlus, bool showButtonBuyMarket, bool showButtonSellMarket, bool showButtonVolume)
		{
			return indicator.JoiaGestor(Input, indicatorName, indicatorTermsOfUse, indicatorInfoLink, useAutoPositionStopLoss, useAutoPositionTakeProfit, limitAddOnVolumeToInProfit, autoPositionCloseType, autoPositionBreakEvenType, stopLossInitialTicks, stopLossInitialATRMultiplier, stopLossInitialSnapType, stopLossInitialMaxTicks, stopLossInitialDollars, stopLossInitialDollarsCombined, stopLossJumpTicks, stopLossCTRLJumpTicks, stopLossRefreshOnVolumeChange, stopLossRefreshManagementEnabled, breakEvenInitialTicks, breakEvenJumpTicks, breakEvenTurboJumpTicks, breakEvenAutoTriggerTicks, breakEvenAutoTriggerATRMultiplier, takeProfitInitialTicks, takeProfitInitialATRMultiplier, takeProfitSyncECATargetPrice, takeProfitJumpTicks, takeProfitCtrlSLMultiplier, takeProfitRefreshManagementEnabled, showAveragePriceLine, showAveragePriceLineQuantity, showAveragePriceLineQuantityInMicros, aTRPeriod, snapPowerBoxPeriod, snapPowerBoxAutoAdjustPeriodsOnM1, useBlendedInstruments, useIntradayMarginCheck, refreshTPSLPaddingTicks, refreshTPSLOrderDelaySeconds, singleOrderChunkMaxQuantity, singleOrderChunkMinQuantity, singleOrderChunkDelayMilliseconds, autoCloseMinProfitDollarsPerVolume, autoCloseAndTrailMA1Period, autoCloseAndTrailMA2Period, autoCloseAndTrailMA3Period, dayOverMaxLossDollars, dayOverMaxLossBTBaseRatio, dayOverAccountBalanceFloorDollars, eCATargetDollars, eCATargetDollarsPerOtherVolume, eCATargetDollarsPerMNQVolume, eCATargetDollarsPerNQVolume, eCATargetDollarsPerM2KVolume, eCATargetDollarsPerRTYVolume, eCATargetDollarsPerMESVolume, eCATargetDollarsPerESVolume, eCATargetDollarsPerMYMVolume, eCATargetDollarsPerYMVolume, eCATargetATRMultiplierPerVolume, eCAMaxDDInDollars, excessIntradayMarginMinDollars, autoEntryVolumeType, autoEntryVolumeOption1, autoEntryVolumeOption2, autoEntryVolumeOption3, autoEntryVolumeOption4, autoEntryVolumeOption5, useHedgehogEntry, hedgehogEntryBuySymbol1SellSymbol2, hedgehogEntrySymbol1, hedgehogEntrySymbol2, usePositionProfitLogging, debugLogLevel, orderWaitOutputThrottleSeconds, useAccountInfoLogging, accountInfoLoggingPath, ignoreInstrumentServerSupport, showButtonAutoBreakEven, showButtonReverse, showButtonClose, showButtonAutoClose, showButtonTPPlus, showButtonBEPlus, showButtonSLPlus, showButtonBuyMarket, showButtonSellMarket, showButtonVolume);
		}
		public Indicators.JoiaGestor JoiaGestor(ISeries<double> input , string indicatorName, string indicatorTermsOfUse, string indicatorInfoLink, bool useAutoPositionStopLoss, bool useAutoPositionTakeProfit, bool limitAddOnVolumeToInProfit, JoiaGestorCloseAutoTypes autoPositionCloseType, JoiaGestorBreakEvenAutoTypes autoPositionBreakEvenType, int stopLossInitialTicks, double stopLossInitialATRMultiplier, JoiaGestorStopLossSnapTypes stopLossInitialSnapType, int stopLossInitialMaxTicks, double stopLossInitialDollars, bool stopLossInitialDollarsCombined, int stopLossJumpTicks, bool stopLossCTRLJumpTicks, bool stopLossRefreshOnVolumeChange, bool stopLossRefreshManagementEnabled, int breakEvenInitialTicks, int breakEvenJumpTicks, int breakEvenTurboJumpTicks, int breakEvenAutoTriggerTicks, double breakEvenAutoTriggerATRMultiplier, int takeProfitInitialTicks, double takeProfitInitialATRMultiplier, bool takeProfitSyncECATargetPrice, int takeProfitJumpTicks, double takeProfitCtrlSLMultiplier, bool takeProfitRefreshManagementEnabled, bool showAveragePriceLine, bool showAveragePriceLineQuantity, bool showAveragePriceLineQuantityInMicros, int aTRPeriod, int snapPowerBoxPeriod, bool snapPowerBoxAutoAdjustPeriodsOnM1, bool useBlendedInstruments, bool useIntradayMarginCheck, int refreshTPSLPaddingTicks, int refreshTPSLOrderDelaySeconds, int singleOrderChunkMaxQuantity, int singleOrderChunkMinQuantity, int singleOrderChunkDelayMilliseconds, double autoCloseMinProfitDollarsPerVolume, int autoCloseAndTrailMA1Period, int autoCloseAndTrailMA2Period, int autoCloseAndTrailMA3Period, double dayOverMaxLossDollars, double dayOverMaxLossBTBaseRatio, double dayOverAccountBalanceFloorDollars, double eCATargetDollars, double eCATargetDollarsPerOtherVolume, double eCATargetDollarsPerMNQVolume, double eCATargetDollarsPerNQVolume, double eCATargetDollarsPerM2KVolume, double eCATargetDollarsPerRTYVolume, double eCATargetDollarsPerMESVolume, double eCATargetDollarsPerESVolume, double eCATargetDollarsPerMYMVolume, double eCATargetDollarsPerYMVolume, double eCATargetATRMultiplierPerVolume, double eCAMaxDDInDollars, double excessIntradayMarginMinDollars, JoiaGestorEntryVolumeAutoTypes autoEntryVolumeType, int autoEntryVolumeOption1, int autoEntryVolumeOption2, int autoEntryVolumeOption3, int autoEntryVolumeOption4, int autoEntryVolumeOption5, bool useHedgehogEntry, bool hedgehogEntryBuySymbol1SellSymbol2, string hedgehogEntrySymbol1, string hedgehogEntrySymbol2, bool usePositionProfitLogging, int debugLogLevel, int orderWaitOutputThrottleSeconds, bool useAccountInfoLogging, string accountInfoLoggingPath, bool ignoreInstrumentServerSupport, bool showButtonAutoBreakEven, bool showButtonReverse, bool showButtonClose, bool showButtonAutoClose, bool showButtonTPPlus, bool showButtonBEPlus, bool showButtonSLPlus, bool showButtonBuyMarket, bool showButtonSellMarket, bool showButtonVolume)
		{
			return indicator.JoiaGestor(input, indicatorName, indicatorTermsOfUse, indicatorInfoLink, useAutoPositionStopLoss, useAutoPositionTakeProfit, limitAddOnVolumeToInProfit, autoPositionCloseType, autoPositionBreakEvenType, stopLossInitialTicks, stopLossInitialATRMultiplier, stopLossInitialSnapType, stopLossInitialMaxTicks, stopLossInitialDollars, stopLossInitialDollarsCombined, stopLossJumpTicks, stopLossCTRLJumpTicks, stopLossRefreshOnVolumeChange, stopLossRefreshManagementEnabled, breakEvenInitialTicks, breakEvenJumpTicks, breakEvenTurboJumpTicks, breakEvenAutoTriggerTicks, breakEvenAutoTriggerATRMultiplier, takeProfitInitialTicks, takeProfitInitialATRMultiplier, takeProfitSyncECATargetPrice, takeProfitJumpTicks, takeProfitCtrlSLMultiplier, takeProfitRefreshManagementEnabled, showAveragePriceLine, showAveragePriceLineQuantity, showAveragePriceLineQuantityInMicros, aTRPeriod, snapPowerBoxPeriod, snapPowerBoxAutoAdjustPeriodsOnM1, useBlendedInstruments, useIntradayMarginCheck, refreshTPSLPaddingTicks, refreshTPSLOrderDelaySeconds, singleOrderChunkMaxQuantity, singleOrderChunkMinQuantity, singleOrderChunkDelayMilliseconds, autoCloseMinProfitDollarsPerVolume, autoCloseAndTrailMA1Period, autoCloseAndTrailMA2Period, autoCloseAndTrailMA3Period, dayOverMaxLossDollars, dayOverMaxLossBTBaseRatio, dayOverAccountBalanceFloorDollars, eCATargetDollars, eCATargetDollarsPerOtherVolume, eCATargetDollarsPerMNQVolume, eCATargetDollarsPerNQVolume, eCATargetDollarsPerM2KVolume, eCATargetDollarsPerRTYVolume, eCATargetDollarsPerMESVolume, eCATargetDollarsPerESVolume, eCATargetDollarsPerMYMVolume, eCATargetDollarsPerYMVolume, eCATargetATRMultiplierPerVolume, eCAMaxDDInDollars, excessIntradayMarginMinDollars, autoEntryVolumeType, autoEntryVolumeOption1, autoEntryVolumeOption2, autoEntryVolumeOption3, autoEntryVolumeOption4, autoEntryVolumeOption5, useHedgehogEntry, hedgehogEntryBuySymbol1SellSymbol2, hedgehogEntrySymbol1, hedgehogEntrySymbol2, usePositionProfitLogging, debugLogLevel, orderWaitOutputThrottleSeconds, useAccountInfoLogging, accountInfoLoggingPath, ignoreInstrumentServerSupport, showButtonAutoBreakEven, showButtonReverse, showButtonClose, showButtonAutoClose, showButtonTPPlus, showButtonBEPlus, showButtonSLPlus, showButtonBuyMarket, showButtonSellMarket, showButtonVolume);
		}
	}
}
#endregion
