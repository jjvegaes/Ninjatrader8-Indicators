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
using NinjaTrader.NinjaScript.BarsTypes;
using NinjaTrader.TaapVarios2;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
	public class ESGTAAPA : Strategy
	{
		private VolumePackVPOC VolumePackVPOC1;
		//private Robot rb;
		//private int rango_n_velas_kalimero = 10;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Pin sobre kalimero en vela unirenko de continuacion";
				Name										= "ESGTAAPA";
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 6;
				EntryHandling								= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy				= true;
				ExitOnSessionCloseSeconds					= 30;
				IsFillLimitOnTouch							= false;
				MaximumBarsLookBack							= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution							= OrderFillResolution.Standard;
				Slippage									= 2;
				StartBehavior								= StartBehavior.WaitUntilFlat;
				TimeInForce									= TimeInForce.Gtc;
				TraceOrders									= false;
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade							= 20;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= true;
				MechaMinima						= 3;
				SLDistance						= 10; // Trailing stop para la primera operacion
				TPDistance						= 12; // TP primera op
				SL2								= 10; // Stop normal para la segunda operacion
				TP2								= 12; // TP segunda op
				entrada_1_TS = true;
				entrada_2_SL = true;
				rango_n_velas_kalimero = 1;
				
				HoraInicio = 00;
				//rb = new Robot(this);
				
                MinutoInicio = 30;
                HoraFin = 22;
                MinutoFin = 00;
			}
			else if (State == State.Configure)
			{
				AddDataSeries(Data.BarsPeriodType.Tick, 1);
			}
			else if (State == State.DataLoaded)
			{				
				VolumePackVPOC1				= VolumePackVPOC(Close, "", "", @"", Brushes.Yellow, true);
				SetTrailStop(@"ESG1", CalculationMode.Ticks, SLDistance, false);
				SetProfitTarget(@"ESG1", CalculationMode.Ticks, TPDistance);
				SetStopLoss(@"ESG2", CalculationMode.Ticks, SL2, false);
				SetProfitTarget(@"ESG2", CalculationMode.Ticks, TP2);
				DateTime horaApertura = new DateTime(1, 1, 1, HoraInicio, MinutoInicio, 0);
            	DateTime horaCierre = new DateTime(1, 1, 1, HoraFin, MinutoFin, 0);
                
                
			}
		}


		protected override void OnBarUpdate()
		{
			string horaInicioStr = HoraInicio.ToString("00") + ":" + MinutoInicio.ToString("00");
			string horaFinStr = HoraFin.ToString("00") + ":" + MinutoFin.ToString("00");
			DateTime horaApertura = DateTime.Parse(horaInicioStr, System.Globalization.CultureInfo.InvariantCulture);
			DateTime horaCierre = DateTime.Parse(horaFinStr, System.Globalization.CultureInfo.InvariantCulture);
			


		    if (BarsInProgress != 0)
		        return;

		    if (CurrentBars[0] < 1) 
		        return;

		    // Set 1
		    if (
		        // Cortos
		        ((Close[0] < Open[0]) 
		         && (High[0] >= (Open[0] + (MechaMinima * TickSize)))
		         && (High[0] >= VolumePackVPOC1.Vpoc1[1]) 
		         && (Open[0] < VolumePackVPOC1.Vpoc1[1]) 
		         && (Times[0][0].TimeOfDay >= horaApertura.TimeOfDay)
                 && (Times[0][0].TimeOfDay <= horaCierre.TimeOfDay) 
				 && (Close[1] < Open[1]))) 
				
		    {
				if (solo_si_mueven_kalimero) {
                    if (VolumePackVPOC1.Vpoc1[0] != VolumePackVPOC1.Vpoc1[1]) {
                        if (entrada_1_TS) EnterShort(Convert.ToInt32(DefaultQuantity), @"ESG1");
                        if (entrada_2_SL) EnterShort(Convert.ToInt32(DefaultQuantity), @"ESG2");
                    } else if (VolumePackVPOC1.Vpoc1[0] != VolumePackVPOC1.Vpoc1[rango_n_velas_kalimero]) 
					{
						if (entrada_1_TS) EnterShort(Convert.ToInt32(DefaultQuantity), @"ESG1");
                        if (entrada_2_SL) EnterShort(Convert.ToInt32(DefaultQuantity), @"ESG2");
					}
                } else {
                    if (entrada_1_TS) {
						EnterShort(Convert.ToInt32(DefaultQuantity), @"ESG1"); 
						//rb.SendOrder(false, 1, SLDistance, TPDistance, 434201285, Name);
					}
                    if (entrada_2_SL) EnterShort(Convert.ToInt32(DefaultQuantity), @"ESG2");
		        }
            }

		    // Set 2
		    if (
		        // Largos
		        ((Close[0] > Open[0]) 
		         && ((Low[0] + (MechaMinima * TickSize)) <= Open[0])
		         && (Low[0] <= VolumePackVPOC1.Vpoc1[1]) 
		         && (Open[0] > VolumePackVPOC1.Vpoc1[1]) 
				 && (Times[0][0].TimeOfDay >= horaApertura.TimeOfDay)
                 && (Times[0][0].TimeOfDay <= horaCierre.TimeOfDay)
		         && (Close[1] > Open[1])))
		    {
                if (solo_si_mueven_kalimero) {
                    if (VolumePackVPOC1.Vpoc1[0] != VolumePackVPOC1.Vpoc1[1]) {
                        if (entrada_1_TS) EnterLong(Convert.ToInt32(DefaultQuantity), @"ESG1");
                        if (entrada_2_SL) EnterLong(Convert.ToInt32(DefaultQuantity), @"ESG2");
                    } else if (VolumePackVPOC1.Vpoc1[0] != VolumePackVPOC1.Vpoc1[rango_n_velas_kalimero]) 
					{
						if (entrada_1_TS) EnterLong(Convert.ToInt32(DefaultQuantity), @"ESG1");
                        if (entrada_2_SL) EnterLong(Convert.ToInt32(DefaultQuantity), @"ESG2");	
					}
                    
                } else {
                    if (entrada_1_TS) EnterLong(Convert.ToInt32(DefaultQuantity), @"ESG1");
                    if (entrada_2_SL) EnterLong(Convert.ToInt32(DefaultQuantity), @"ESG2");
                }
			}
		}


		#region Properties
		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name="rango_n_velas_kalimero", Description="Rango de velas en el que mueven kalimero", Order=12, GroupName="Parameters")]
		public int rango_n_velas_kalimero
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, 21)]
		[Display(Name="HoraInicio", Description="Hora de inicio", Order=16, GroupName="Parameters")]
		public int HoraInicio
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, 59)]
		[Display(Name="MinutoInicio", Description="Minuto de inicio", Order=13, GroupName="Parameters")]
		public int MinutoInicio
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, 22)]
		[Display(Name="HoraFin", Description="Hora de fin", Order=14, GroupName="Parameters")]
		public int HoraFin
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, 59)]
		[Display(Name="MinutoFin", Description="Minuto de fin", Order=15, GroupName="Parameters")]
		public int MinutoFin
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="MechaMinima", Description="Mecha minima para considerar", Order=1, GroupName="Parameters")]
		public int MechaMinima
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="entrada_1_TS", Order=9, GroupName="Parameters")]
		public bool entrada_1_TS
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="solo_si_mueven_kalimero", Order=11, GroupName="Parameters")]
		public bool solo_si_mueven_kalimero
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="entrada_2_SL", Order=10, GroupName="Parameters")]
		public bool entrada_2_SL
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="SLDistance", Description="Distancia del SL", Order=2, GroupName="Parameters")]
		public int SLDistance
		{ get; set; }

		[NinjaScriptProperty]
		[Range(6, int.MaxValue)]
		[Display(Name="TPDistance", Description="Distancia al TP", Order=3, GroupName="Parameters")]
		public int TPDistance
		{ get; set; }

		[NinjaScriptProperty]
		[Range(3, int.MaxValue)]
		[Display(Name="SL2", Description="SL2", Order=4, GroupName="Parameters")]
		public int SL2
		{ get; set; }

		[NinjaScriptProperty]
		[Range(4, int.MaxValue)]
		[Display(Name="TP2", Description="TP2", Order=5, GroupName="Parameters")]
		public int TP2
		{ get; set; }
		#endregion

	}
}
