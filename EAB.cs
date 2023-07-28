#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
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
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
	public class EAB2 : Strategy
	{
		private BigTrade BigTrade1;
		private ATR ATR1;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"EAB";
				Name										= "EAB2";
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 1;
				EntryHandling								= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy				= true;
				ExitOnSessionCloseSeconds					= 30;
				IsFillLimitOnTouch							= false;
				MaximumBarsLookBack							= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution							= OrderFillResolution.Standard;
				Slippage									= 0;
				StartBehavior								= StartBehavior.WaitUntilFlat;
				TimeInForce									= TimeInForce.Gtc;
				TraceOrders									= false;
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade							= 20;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= true;
				Atr_period					= 1;
				Atr_size					= 1;
			}
			else if (State == State.Configure)
			{
				AddDataSeries(Data.BarsPeriodType.Minute, 1);
				AddDataSeries(Data.BarsPeriodType.Tick, 1);
			}
			else if (State == State.DataLoaded)
			{				
				BigTrade1				= BigTrade(Close, @"jorge.espinar@usi.ch", @"JORGE-ZXZRO", @"00:30", 5, 12, true, false, 0, false, true, 0, false, false, true, 1, 0, Brushes.Purple, Brushes.Blue, Brushes.Red, Brushes.Green, Brushes.Orange);
				ATR1				= ATR(Close, Convert.ToInt32(Atr_period));
				BigTrade1.Plots[0].Brush = Brushes.Yellow;
				BigTrade1.Plots[1].Brush = Brushes.Red;
				BigTrade1.Plots[2].Brush = Brushes.Red;
				BigTrade1.Plots[3].Brush = Brushes.Green;
				ATR1.Plots[0].Brush = Brushes.DarkCyan;
				AddChartIndicator(BigTrade1);
				AddChartIndicator(ATR1);
				SetStopLoss("", CalculationMode.Ticks, Low[1], false);
                
			}
		}

		protected override void OnBarUpdate()
		{
			if (BarsInProgress != 0) 
				return;

			if (CurrentBars[0] < 1)
				return;

            // Set Trailing stop to ATR de 5 periodos * 3
            SetTrailStop("", CalculationMode.Ticks, ATR1[0] * Atr_size, false);

			 // Set 1
			if ((Close[0] > Open[0])
				 && (BigTrade1.NivelBT[0] > (High[0] - (High[0] - Low[0]) * 0.8))
				 && ((High[0] - Low[0]) > (ATR1[0] * Atr_size) ))
			{
				EnterShort(Convert.ToInt32(DefaultQuantity), "");
			}
			
			 // Set 2
			if ((Close[0] < Open[0])
				 && (BigTrade1.NivelBT[0] < (Low[0] + (High[0] - Low[0]) * 0.2))
				 && ((High[0] - Low[0]) > (ATR1[0] * Atr_size) ))
			{
				EnterLong(Convert.ToInt32(DefaultQuantity), "");
			}
			
		}

		#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Atr_period", Order=1, GroupName="Parameters")]
		public int Atr_period
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Atr_size", Order=2, GroupName="Parameters")]
		public int Atr_size
		{ get; set; }
		#endregion

	}
}
