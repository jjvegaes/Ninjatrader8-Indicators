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
	public class JoiaINFO : Indicator
	{
        private string AccountName;
		NinjaTrader.Gui.Tools.AccountSelector xAlselector;

		private string			timeLeft	= string.Empty;
		private DateTime		now		 	= Core.Globals.Now;
		private bool			connected,
								hasRealtimeData;
		private SessionIterator sessionIterator;

		private System.Windows.Threading.DispatcherTimer timer;
        private double dblAsk = 0;
        private double dblBid = 0;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Muestra informacion del tiempo restante de la vela actual, del beneficio por tick, del spread y de la cuenta que estamos usando actualmente, que con el chart trader ocultado puede ser muy util.";
				Name										= "JoiaINFO";
				Calculate									= Calculate.OnEachTick;
				IsOverlay									= true;
				DisplayInDataBox							= false;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				IsSuspendedWhileInactive					= true;
				LetraSize									= 14;
				LetraColor									= Brushes.White;
				MostrarSpread								= true;
				MostrarNombreCuentaActual					= true;
				MostrarTR									= true;
			}
			else if (State == State.Realtime)
			{
				if (timer == null && IsVisible)
				{
					if (Bars.BarsType.IsTimeBased && Bars.BarsType.IsIntraday)
					{
						lock (Connection.Connections)
						{
							if (Connection.Connections.ToList().FirstOrDefault(c => c.Status == ConnectionStatus.Connected && c.InstrumentTypes.Contains(Instrument.MasterInstrument.InstrumentType)) == null)
								Draw.TextFixed(this, "NinjaScriptInfo", NinjaTrader.Custom.Resource.BarTimerDisconnectedError, TextPosition.BottomRight, ChartControl.Properties.ChartText, ChartControl.Properties.LabelFont, Brushes.Transparent, Brushes.Transparent, 0);
							else
							{
								if (!SessionIterator.IsInSession(Now, false, true))
									Draw.TextFixed(this, "NinjaScriptInfo", NinjaTrader.Custom.Resource.BarTimerSessionTimeError, TextPosition.BottomRight, ChartControl.Properties.ChartText, ChartControl.Properties.LabelFont, Brushes.Transparent, Brushes.Transparent, 0);
								else
									Draw.TextFixed(this, "NinjaScriptInfo", "", TextPosition.BottomRight, ChartControl.Properties.ChartText, ChartControl.Properties.LabelFont, Brushes.Transparent, Brushes.Transparent, 0);
							}
						}
					}
					else
					{
                        Draw.TextFixed(this, "NinjaScriptInfo", NinjaTrader.Custom.Resource.BarTimerTimeBasedError, TextPosition.BottomRight, ChartControl.Properties.ChartText, ChartControl.Properties.LabelFont, Brushes.Transparent, Brushes.Transparent, 0);
				
                    }
				}
			}
			else if (State == State.Terminated)
			{
				if (timer == null)
					return;

				timer.IsEnabled = false;
				timer = null;
			}
		}

		protected override void OnBarUpdate()
		{
			if (State == State.Realtime)
			{
				hasRealtimeData = true;
				connected = true;
			}

			ChartControl.Dispatcher.InvokeAsync((Action)(() =>
			{
				//You have to put the stuff below within this ChartControl.Dispatcher.InvokeAsync((Action)(() =>, because you are trying to access something on a different thread.
				xAlselector = Window.GetWindow(ChartControl.Parent).FindFirst("ChartTraderControlAccountSelector") as NinjaTrader.Gui.Tools.AccountSelector;
				AccountName = xAlselector.SelectedAccount.ToString();
                if (MostrarNombreCuentaActual) {
                    Draw.TextFixed(this, "account", "Cuenta: " + AccountName + Environment.NewLine+ Environment.NewLine, TextPosition.BottomRight, LetraColor, new Gui.Tools.SimpleFont("Arial", LetraSize), Brushes.Transparent, Brushes.Transparent, 0);
                }
			}));

		}

		protected override void OnConnectionStatusUpdate(ConnectionStatusEventArgs connectionStatusUpdate)
		{
			if (connectionStatusUpdate.PriceStatus == ConnectionStatus.Connected
				&& connectionStatusUpdate.Connection.InstrumentTypes.Contains(Instrument.MasterInstrument.InstrumentType)
				&& Bars.BarsType.IsTimeBased
				&& Bars.BarsType.IsIntraday)
			{
				connected = true;

				if (DisplayTime() && timer == null)
				{
					ChartControl.Dispatcher.InvokeAsync(() =>
					{
						timer			= new System.Windows.Threading.DispatcherTimer { Interval = new TimeSpan(0, 0, 1), IsEnabled = true };
						timer.Tick		+= OnTimerTick;
					});
				}
			}
			else if (connectionStatusUpdate.PriceStatus == ConnectionStatus.Disconnected)
				connected = false;
		}

		private bool DisplayTime()
		{
			return ChartControl != null
					&& Bars != null
					&& Bars.Instrument.MarketData != null
					&& IsVisible;
		}

		private void OnTimerTick(object sender, EventArgs e)
		{
			ForceRefresh();

			if (DisplayTime())
			{
				if (timer != null && !timer.IsEnabled)
					timer.IsEnabled = true;

				if (connected)
				{
					if (SessionIterator.IsInSession(Now, false, true))
					{
						if (hasRealtimeData)
						{
							TimeSpan barTimeLeft = Bars.GetTime(Bars.Count - 1).Subtract(Now);
                            double TValue = Instrument.MasterInstrument.PointValue * TickSize;
                            double SpreadCost = (dblAsk - dblBid) / TickSize * TValue; // 
							timeLeft = (barTimeLeft.Ticks < 0
								? "00:00:00"
								: barTimeLeft.Hours.ToString("00") + ":" + barTimeLeft.Minutes.ToString("00") + ":" + barTimeLeft.Seconds.ToString("00"));
							if (MostrarSpread) {Draw.TextFixed(this, "Spread", "Spread: " +  SpreadCost + "$	Tick Value: "+ TValue.ToString()+"$" + Environment.NewLine, TextPosition.BottomLeft, LetraColor, new Gui.Tools.SimpleFont("Arial", LetraSize), Brushes.Transparent, Brushes.Transparent, 0);}
                            if (MostrarTR){ 
								Draw.TextFixed(this, "TimeRemaining","	Time Remaining:" + timeLeft + Environment.NewLine, TextPosition.BottomRight, LetraColor,  new Gui.Tools.SimpleFont("Arial", LetraSize), Brushes.Transparent, Brushes.Transparent, 0);
                            	Draw.TextFixed(this, "Time", "Time: " + DateTime.Now.ToString(), TextPosition.BottomRight,  LetraColor,  new Gui.Tools.SimpleFont("Arial", LetraSize), Brushes.Transparent, Brushes.Transparent, 0);
							}
						}
						else
							Draw.TextFixed(this, "NinjaScriptInfo"," ", TextPosition.BottomRight, ChartControl.Properties.ChartText, ChartControl.Properties.LabelFont, Brushes.Transparent, Brushes.Transparent, 0);
					}
					else
						Draw.TextFixed(this, "NinjaScriptInfo", NinjaTrader.Custom.Resource.BarTimerSessionTimeError, TextPosition.BottomRight, ChartControl.Properties.ChartText, ChartControl.Properties.LabelFont, Brushes.Transparent, Brushes.Transparent, 0);
				}
				else
				{
					Draw.TextFixed(this, "NinjaScriptInfo", NinjaTrader.Custom.Resource.BarTimerDisconnectedError, TextPosition.BottomRight, ChartControl.Properties.ChartText, ChartControl.Properties.LabelFont, Brushes.Transparent, Brushes.Transparent, 0);

					if (timer != null)
						timer.IsEnabled = false;
				}
			}
		}

        protected override void OnMarketData(MarketDataEventArgs marketDataUpdate)
		{
            if (marketDataUpdate.MarketDataType == MarketDataType.Ask || marketDataUpdate.MarketDataType == MarketDataType.Bid)
            {
                if (marketDataUpdate.MarketDataType == MarketDataType.Ask)
                    dblAsk = marketDataUpdate.Price;

                if (marketDataUpdate.MarketDataType == MarketDataType.Bid)
                    dblBid = marketDataUpdate.Price;
            }

        }

		private SessionIterator SessionIterator
		{
			get
			{
				if (sessionIterator == null)
					sessionIterator = new SessionIterator(Bars);
				return sessionIterator;
			}
		}

		private DateTime Now
		{
			get
			{
				now = (Cbi.Connection.PlaybackConnection != null ? Cbi.Connection.PlaybackConnection.Now : Core.Globals.Now);

				if (now.Millisecond > 0)
					now = Core.Globals.MinDate.AddSeconds((long)Math.Floor(now.Subtract(Core.Globals.MinDate).TotalSeconds));

				return now;
			}
		}

		#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="LetraSize", Description="Tamano de letra", Order=1, GroupName="Parameters")]
		public int LetraSize
		{ get; set; }

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="LetraColor", Description="Color de letra", Order=2, GroupName="Parameters")]
		public Brush LetraColor
		{ get; set; }

		[Browsable(false)]
		public string LetraColorSerializable
		{
			get { return Serialize.BrushToString(LetraColor); }
			set { LetraColor = Serialize.StringToBrush(value); }
		}			

		[NinjaScriptProperty]
		[Display(Name="MostrarSpread", Description="Mostrar spread y beneficio por tick", Order=3, GroupName="Parameters")]
		public bool MostrarSpread
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="MostrarNombreCuentaActual", Description="Muestra el nombre de cuenta con el que estamos operando", Order=4, GroupName="Parameters")]
		public bool MostrarNombreCuentaActual
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="MostrarTR", Description="Muestra tiempo restante de la vela actual y hora", Order=5, GroupName="Parameters")]
		public bool MostrarTR
		{ get; set; }
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private JoiaINFO[] cacheJoiaINFO;
		public JoiaINFO JoiaINFO(int letraSize, Brush letraColor, bool mostrarSpread, bool mostrarNombreCuentaActual, bool mostrarTR)
		{
			return JoiaINFO(Input, letraSize, letraColor, mostrarSpread, mostrarNombreCuentaActual, mostrarTR);
		}

		public JoiaINFO JoiaINFO(ISeries<double> input, int letraSize, Brush letraColor, bool mostrarSpread, bool mostrarNombreCuentaActual, bool mostrarTR)
		{
			if (cacheJoiaINFO != null)
				for (int idx = 0; idx < cacheJoiaINFO.Length; idx++)
					if (cacheJoiaINFO[idx] != null && cacheJoiaINFO[idx].LetraSize == letraSize && cacheJoiaINFO[idx].LetraColor == letraColor && cacheJoiaINFO[idx].MostrarSpread == mostrarSpread && cacheJoiaINFO[idx].MostrarNombreCuentaActual == mostrarNombreCuentaActual && cacheJoiaINFO[idx].MostrarTR == mostrarTR && cacheJoiaINFO[idx].EqualsInput(input))
						return cacheJoiaINFO[idx];
			return CacheIndicator<JoiaINFO>(new JoiaINFO(){ LetraSize = letraSize, LetraColor = letraColor, MostrarSpread = mostrarSpread, MostrarNombreCuentaActual = mostrarNombreCuentaActual, MostrarTR = mostrarTR }, input, ref cacheJoiaINFO);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.JoiaINFO JoiaINFO(int letraSize, Brush letraColor, bool mostrarSpread, bool mostrarNombreCuentaActual, bool mostrarTR)
		{
			return indicator.JoiaINFO(Input, letraSize, letraColor, mostrarSpread, mostrarNombreCuentaActual, mostrarTR);
		}

		public Indicators.JoiaINFO JoiaINFO(ISeries<double> input , int letraSize, Brush letraColor, bool mostrarSpread, bool mostrarNombreCuentaActual, bool mostrarTR)
		{
			return indicator.JoiaINFO(input, letraSize, letraColor, mostrarSpread, mostrarNombreCuentaActual, mostrarTR);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.JoiaINFO JoiaINFO(int letraSize, Brush letraColor, bool mostrarSpread, bool mostrarNombreCuentaActual, bool mostrarTR)
		{
			return indicator.JoiaINFO(Input, letraSize, letraColor, mostrarSpread, mostrarNombreCuentaActual, mostrarTR);
		}

		public Indicators.JoiaINFO JoiaINFO(ISeries<double> input , int letraSize, Brush letraColor, bool mostrarSpread, bool mostrarNombreCuentaActual, bool mostrarTR)
		{
			return indicator.JoiaINFO(input, letraSize, letraColor, mostrarSpread, mostrarNombreCuentaActual, mostrarTR);
		}
	}
}

#endregion
