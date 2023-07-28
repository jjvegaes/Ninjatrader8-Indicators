/* A way to add references to the code. */
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
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class JoiaX : Indicator
	{
        private VolumePackVPOC VolumePackVPOC1;
		private SMA SMA1;
        private bool _isInitialized = false;
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Cuantas X tenemos";
				Name										= "JoiaX";
				Calculate									= Calculate.OnEachTick;
				IsOverlay									= false;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
                PrintTo                                     = PrintTo.OutputTab1;
			}
			else if (State == State.Configure)
			{
				AddDataSeries(Data.BarsPeriodType.Minute, 1);
				AddDataSeries("ES 03-23", Data.BarsPeriodType.Minute, 1, Data.MarketDataType.Last);
				AddDataSeries("ES 03-23", Data.BarsPeriodType.Tick, 1, Data.MarketDataType.Last);
			}
            else if (State == State.DataLoaded)
			{				
				VolumePackVPOC1				= VolumePackVPOC(Closes[2], @"jorge.espinar@usi.ch", @"JORGE-UTYAZ", @"ES 03-23", Brushes.Yellow);
				SMA1						= SMA(Close, 14);
			}
		}

		protected override void OnBarUpdate()
		{
            // if (BarsInProgress != 0) 
            //     Print(" salida por causa de BarsInProgress != 0 = " + BarsInProgress);
			// 	return;

			if (CurrentBars[0] < 2)
                Print(" salida por causa de CurrentBars[0] = " + CurrentBars[0]);
				return;

            if (BarsInProgress == 1){
                Print("inicializado con éxito BarsInProgress = " + BarsInProgress);
                _isInitialized = true;
            }

            if (BarsInProgress != 1)
                //Print(" salida por causa de BarsInProgress != 1 = " + BarsInProgress);
                return;

            if (!_isInitialized)
                //Print(" salida por causa de _isInitialized que es false = " + _isInitialized);
                return;

            if (CurrentBar > 4) {
                Print("VolumePackVPOC1.Vpoc1[0] = " + VolumePackVPOC1.Vpoc1[0]);
                Print("VolumePackVPOC1.Vpoc2[0] = " + VolumePackVPOC1.Vpoc2[0]);
                Print("VolumePackVPOC1.Vpoc1[1] = " + VolumePackVPOC1.Vpoc1[1]);



                var secondTimeFrameLastBarClosed = Closes[1][0];
                // Set 1
                if (VolumePackVPOC1.Vpoc1[0] < GetCurrentAsk(0))
                {
                    BarBrush = Brushes.Aqua;
                    Draw.ArrowUp(this, @"AUniRenkoSantoGrial Arrow up_1", true, 0, Close[1], Brushes.Lime);
                    Draw.TextFixed(this, "Solo compras", "X", TextPosition.BottomRight);
                    Draw.TextFixed(this, "kal", "Solo compras, kalimero por debajo " + VolumePackVPOC1.Vpoc1[0] + Environment.NewLine+ Environment.NewLine, TextPosition.TopLeft , Brushes.Gold, new Gui.Tools.SimpleFont("Arial", 20), Brushes.Transparent, Brushes.Transparent, 0);
                }
                
                // Set 2
                if (VolumePackVPOC1.Vpoc2[0] > GetCurrentAsk(0))
                {
                    Draw.ArrowDown(this, @"AUniRenkoSantoGrial Arrow down_1", false, 0, SMA1[0], Brushes.Red);
                    BarBrush = Brushes.Salmon;
                    Draw.TextFixed(this, "Solo ventas", "X", TextPosition.BottomRight);
                }
            }
		}
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private JoiaX[] cacheJoiaX;
		public JoiaX JoiaX()
		{
			return JoiaX(Input);
		}

		public JoiaX JoiaX(ISeries<double> input)
		{
			if (cacheJoiaX != null)
				for (int idx = 0; idx < cacheJoiaX.Length; idx++)
					if (cacheJoiaX[idx] != null &&  cacheJoiaX[idx].EqualsInput(input))
						return cacheJoiaX[idx];
			return CacheIndicator<JoiaX>(new JoiaX(), input, ref cacheJoiaX);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.JoiaX JoiaX()
		{
			return indicator.JoiaX(Input);
		}

		public Indicators.JoiaX JoiaX(ISeries<double> input )
		{
			return indicator.JoiaX(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.JoiaX JoiaX()
		{
			return indicator.JoiaX(Input);
		}

		public Indicators.JoiaX JoiaX(ISeries<double> input )
		{
			return indicator.JoiaX(input);
		}
	}
}

#endregion
