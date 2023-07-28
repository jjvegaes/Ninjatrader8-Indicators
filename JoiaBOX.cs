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
	public class JoiaBOX : Indicator
	{
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Fibonacci de una caja temporal predefinida. Por defecto traza un fibonacci con el rango horario desde las 8 am hasta las 9 am.";
				Name										= "JoiaBOX";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= true;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
				HoraInicio									= DateTime.Parse("08:00", System.Globalization.CultureInfo.InvariantCulture);
				HoraFin										= DateTime.Parse("09:00", System.Globalization.CultureInfo.InvariantCulture);
				NumDias										= 3;
				Color										= Brushes.Gold;
				LineWidth									= 2;
			}
			else if (State == State.Configure)
			{
			}
		}

		protected override void OnBarUpdate()
		{
		// 	// Obtener la hora actual
		// 	int currentHour = Time[0].Hour;

			// Inicializar los índices de la vela de las 8 am y 9 am a -1
			int candle8amIndex = 1;
			int candle9amIndex = 1;

			// Iterar a través de las velas hasta encontrar la vela de las 8 am y la vela de las 9 am
			for (int i = 0; i < CurrentBars[0]; i++) // Por cada vela desde 0 hasta la vela actual
			{
			    if (Time[i].Hour == HoraFin.Hour && Time[i].Minute == HoraFin.Minute && candle8amIndex == 1) // Si la hora de la vela es 8 y el índice de la vela de las 8 am es -1
			    {
			        // Si encontramos la vela de las 8 am, almacenamos su índice
			        candle8amIndex = i;
			    }
			    else if (Time[i].Hour == HoraInicio.Hour && Time[i].Minute == HoraInicio.Minute && candle9amIndex == 1)
			    {
			        // Si encontramos la vela de las 9 am, almacenamos su índice
			        candle9amIndex = i;
			        break;
			    }
			}
			Print("Candle8amIndex: " + candle8amIndex + " Candle9amIndex: " + candle9amIndex);

			if (CurrentBars[0] < 65)
				return;
			// Find the lowest low and highest high of the last 60 candles
			double lowestLow = double.MaxValue, highestHigh = double.MinValue;
			
			int lowi = 0, highi = 0;
			for (int i = candle8amIndex; i <= candle9amIndex; i++)
			{
			    if (Low[i] < lowestLow) lowestLow = Low[i]; lowi = i;
			    if (High[i] > highestHigh) highestHigh = High[i]; highi = i;
			}
            Print("LowestLow: " + lowestLow + " HighestHigh: " + highestHigh);
			
            // Draw the Fibonacci lines
            double fiboMinus27 = lowestLow + (-0.27 * (highestHigh - lowestLow));
            double fiboPlus27 = lowestLow + (1.27 * (highestHigh - lowestLow));
            
            Draw.Line(this, "FiboMinus27", true, candle8amIndex, fiboMinus27, candle9amIndex, fiboMinus27, Color, DashStyleHelper.Solid, LineWidth);
            //Draw.Text(this, "FiboMinus27Text", "MaxFO", candle8amIndex, fiboMinus27+0.0001*fiboMinus27, Color);
            Draw.Line(this, "FiboPlus27", true, candle8amIndex, fiboPlus27, candle9amIndex, fiboPlus27, Color, DashStyleHelper.Solid, LineWidth);
            //Draw.Text(this, "FiboPlus27Text", "MaxFO", candle8amIndex, fiboPlus27, Color);
            Draw.Rectangle(this, "CajaAsiaticaRect", true, candle8amIndex, lowestLow, candle9amIndex, highestHigh, Color, Brushes.Gold, LineWidth);

		}

		#region Properties
		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="HoraInicio", Description="Hora de inicio de la caja", Order=1, GroupName="Parameters")]
		public DateTime HoraInicio
		{ get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="HoraFin", Description="Hora de fin de la caja", Order=2, GroupName="Parameters")]
		public DateTime HoraFin
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="NumDias", Description="Numero de dias a mostrar la caja", Order=3, GroupName="Parameters")]
		public int NumDias
		{ get; set; }

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="Color", Description="Fibo color", Order=4, GroupName="Parameters")]
		public Brush Color
		{ get; set; }

		[Browsable(false)]
		public string ColorSerializable
		{
			get { return Serialize.BrushToString(Color); }
			set { Color = Serialize.StringToBrush(value); }
		}			

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="LineWidth", Description="Ancho de linea", Order=5, GroupName="Parameters")]
		public int LineWidth
		{ get; set; }
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private JoiaBOX[] cacheJoiaBOX;
		public JoiaBOX JoiaBOX(DateTime horaInicio, DateTime horaFin, int numDias, Brush color, int lineWidth)
		{
			return JoiaBOX(Input, horaInicio, horaFin, numDias, color, lineWidth);
		}

		public JoiaBOX JoiaBOX(ISeries<double> input, DateTime horaInicio, DateTime horaFin, int numDias, Brush color, int lineWidth)
		{
			if (cacheJoiaBOX != null)
				for (int idx = 0; idx < cacheJoiaBOX.Length; idx++)
					if (cacheJoiaBOX[idx] != null && cacheJoiaBOX[idx].HoraInicio == horaInicio && cacheJoiaBOX[idx].HoraFin == horaFin && cacheJoiaBOX[idx].NumDias == numDias && cacheJoiaBOX[idx].Color == color && cacheJoiaBOX[idx].LineWidth == lineWidth && cacheJoiaBOX[idx].EqualsInput(input))
						return cacheJoiaBOX[idx];
			return CacheIndicator<JoiaBOX>(new JoiaBOX(){ HoraInicio = horaInicio, HoraFin = horaFin, NumDias = numDias, Color = color, LineWidth = lineWidth }, input, ref cacheJoiaBOX);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.JoiaBOX JoiaBOX(DateTime horaInicio, DateTime horaFin, int numDias, Brush color, int lineWidth)
		{
			return indicator.JoiaBOX(Input, horaInicio, horaFin, numDias, color, lineWidth);
		}

		public Indicators.JoiaBOX JoiaBOX(ISeries<double> input , DateTime horaInicio, DateTime horaFin, int numDias, Brush color, int lineWidth)
		{
			return indicator.JoiaBOX(input, horaInicio, horaFin, numDias, color, lineWidth);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.JoiaBOX JoiaBOX(DateTime horaInicio, DateTime horaFin, int numDias, Brush color, int lineWidth)
		{
			return indicator.JoiaBOX(Input, horaInicio, horaFin, numDias, color, lineWidth);
		}

		public Indicators.JoiaBOX JoiaBOX(ISeries<double> input , DateTime horaInicio, DateTime horaFin, int numDias, Brush color, int lineWidth)
		{
			return indicator.JoiaBOX(input, horaInicio, horaFin, numDias, color, lineWidth);
		}
	}
}

#endregion
