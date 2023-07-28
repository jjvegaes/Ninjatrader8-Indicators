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
using SharpDX.DirectWrite;
using NinjaTrader.Core;
using System.IO;  // for streamwriter
#endregion
//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	// Versión 1.0
	// 01-05-2022 Fecha de lanzamiento.
	//
	// Notas: El propósito del indicador es escribir todos los datos de barras e indicadores en un archivo (CSV) que se puede importar fácilmente a Excel para fines de análisis. Si un indicador
	// tiene plots que se pintan mediante render, como el perfil de volumen, no se incluirán. Si un indicador tiene gráficos ocultos (transparentes) se incluirán. Las "Columnas" estarán en el orden
	// que los indicadores se enumeran en la interfaz de los indicadores. En un indicador de múltiples plots, los plots se enumerarán en el orden en que se muestran en las propiedades de los indicadores. Este indicador leerá el
	// datos desde el comienzo de la serie de datos hasta la última barra histórica mostrada y dibujará una línea de referencia vertical en el gráfico de precios para ilustrar dónde se escribieron los últimos datos.
	//
	// Uso operativo: añadir el indicador al gráfico, ** HAY QUE ESPERAR ** hasta que todos los indicadores hayan terminado de calcular/recargar, cuando todos hayan terminado de cargar, haz click en el botón de exportar, cuando el botón muestre "Done", en el gráfico aparecerá un texto avisando de qué datos se han escrito y
	// donde se han escrito, en este punto se aconseja eliminarlo del gráfico. Eliminar cerrará el archivo O hacer clic en el botón "Listo" cerrará el archivo (y eliminará el botón) pero el indicador permanecerá en el gráfico. Si usted
	// deje el indicador en el gráfico y actualice los indicadores o agregue más indicadores, JoiaEXPORTADOR presentará el botón de escribir datos nuevamente
	// haga clic en Listo para cerrar el archivo CSV y/O eliminarlo del gráfico
	// para cerrar también el archivo CSV.
	//
	// El indicador creará un nuevo archivo cada vez que se ejecute. El nombre del archivo incluirá el instrumento principal y el día de la semana y la hora del día, como punto de referencia.
	//
	//
	
	public class JoiaEXPORTADOR : Indicator
	{				
		private List <string> Labels;
		private List <double> Data;		
		private bool init = true, doitonce = true, alsodoitonce = true;		
		private StreamWriter sw; 
		private string path;
		private bool longButtonClicked;
		private System.Windows.Controls.Button longButton;
		private System.Windows.Controls.Grid myGrid;
		private DateTime start_Date, end_Date;
		private bool doneWriting = false;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Exporta todo el historico cargado en el grafico, asi como tambien todos los indicadores";
				Name										= "JoiaEXPORTADOR";
				Calculate									= Calculate.OnEachTick;
				IsOverlay									= true;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				IsSuspendedWhileInactive					= true;
			}
			else if (State == State.Historical)
			{
				if (UserControlCollection.Contains(myGrid))
					return;
				
				Dispatcher.InvokeAsync((() =>
				{
					myGrid = new System.Windows.Controls.Grid
					{
						Name = "CuadriculaGrafico", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Bottom
					};
					
					System.Windows.Controls.ColumnDefinition column1 = new System.Windows.Controls.ColumnDefinition();
					
					myGrid.ColumnDefinitions.Add(column1);
					
					longButton = new System.Windows.Controls.Button
					{
						Name = "Exportar", Content = "Exportar", Foreground = Brushes.White, Background = Brushes.Green
					};
		
					longButton.Click += OnButtonClick;
					
					System.Windows.Controls.Grid.SetColumn(longButton, 0);
					
					myGrid.Children.Add(longButton);
					
					UserControlCollection.Add(myGrid);
				}));				
				
				
				Data = new List<double>();  	// initialize for Data list
				Labels = new List<string>();	// initialize the labels list
				Labels.Add("Date;");			// assign the basic labels
				Labels.Add("Time;");
				Labels.Add("Open;");
				Labels.Add("High;");
				Labels.Add("Low;");
				Labels.Add("Close;");
				Labels.Add("Volume;");
				
				path 	= NinjaTrader.Core.Globals.UserDataDir+Instrument.MasterInstrument.Name+" "+DateTime.Now.DayOfWeek+" "+DateTime.Now.Hour+DateTime.Now.Minute+ ".csv"; // Ruta al archivo exportado
				
				sw = File.AppendText(path);  // Open the path for writing
				
			}
			else if (State == State.Terminated)
			{
				Dispatcher.InvokeAsync((() =>
				{
					if (myGrid != null)
					{
						if (longButton != null)
						{
							myGrid.Children.Remove(longButton);
							longButton.Click -= OnButtonClick;
							longButton = null;
							Print ("state.terminated, ocultar el botón?");
						}
					}
				}));
				
				if (sw != null)
				{
					sw.Dispose();
					sw = null;
				}
			}
		}
		protected override void OnBarUpdate()
		{
			if (doneWriting)  // when the done button is pressed, no need for OBU checks
				return;
			
			if (State == State.Realtime && longButtonClicked && alsodoitonce)  // on the button click, on the first tick run the process.
			{
				Draw.VerticalLine(this, "exportar", 1, Brushes.SkyBlue);		// Draw vertical reference line on chart, previous bar is last historial
				ReadWrite();													// go get the data
				alsodoitonce = false;											// only once
				sw.Close();														// close the CSV file
				// provide feedback on chart to remove the indicator
				Draw.TextFixed(this, "exportado1", "Histórico y datos de los indicadores exportados desde: "+start_Date+ " hasta "+end_Date
					+"\n Exportado en archivo: "+path+"\nQuitar indicador "+this.Name+" del gráfico para quitar este mensaje", TextPosition.BottomLeft);
			}
		}
		
		private void ReadWrite()
		{
			for (int n = 0; n < Bars.Count-1; n++) // process the historical data only
			{
				Data.Clear(); // clear list first
				// add basics
				Data.Add(Open.GetValueAt(n));
				Data.Add(High.GetValueAt(n));
				Data.Add(Low.GetValueAt(n));
				Data.Add(Close.GetValueAt(n));
				Data.Add(Volume.GetValueAt(n));
			
				lock (ChartControl.Indicators)
				foreach (IndicatorBase indicator in ChartControl.Indicators)	// loop through indicators on chart		
  				{
					if (indicator.State < State.Realtime)						// they all need to be in state realtime
					{
						Print (this.Name+"State error en vela: "+n+"  indicador: "+indicator.Name+"  state: "+indicator.State);
						continue;
					}
				
					if (indicator.Name != "JoiaEXPORTADOR")  // don't need to read this indicator...
					{	 	
						for (int seriesCount = 0; seriesCount <  indicator.Values.Length ; seriesCount++)  // process each plot of the indicator
						{
							Plot	plot				= indicator.Plots[seriesCount];						// get a plot from the indicator
							double val					= indicator.Values[seriesCount].GetValueAt(n);		// now get a specific bar value							
							Data.Add(val);				// add indicators current plot value to list;
										
							if (init)
							{
								Labels.Add(indicator.Name+":"+plot.Name+";");	 // add indicator : plotname to labels list
							}
						} 
					}
				}	
				init = false;  //grab labels just once
					
				// now write labels to file first (header row)
				if (!init && doitonce)
				{
					int LC = Labels.Count-1;
											
					for (int h = 0; h < Labels.Count; h++) // write labels to file
					{
						sw.Write(Labels[h]); 
					}
					sw.WriteLine();  // kick it to the next line
					doitonce = false; // only once
					Dispatcher.InvokeAsync((() => // update the button
					{
						longButton.Background = Brushes.Gold;
						longButton.Foreground = Brushes.Black;
						longButton.Content = "Done";
					}));
					
				}
					
				// write bar date and time first
				sw.Write(Time.GetValueAt(n).Date.ToShortDateString()+";"+Time.GetValueAt(n).TimeOfDay+";"); // write the date and time
				
				if (n == 0) // save the start date for feedback
					start_Date = Time.GetValueAt(n);		// save the start date for feedback
				if (n == Bars.Count-2)
					end_Date = Time.GetValueAt(n);			// save the end date for feedback
				// write data after on same line
				for (int j = 0; j < Data.Count; j++)
				{
					sw.Write(Data[j]+";");  				// write the data with a ; for delimitation
				}
				sw.WriteLine();								// kick it to the next line to write										
				}
			}
		
		private void OnButtonClick(object sender, RoutedEventArgs rea) // button click event
		{
			System.Windows.Controls.Button button = sender as System.Windows.Controls.Button; // get the button
			if (button == longButton && button.Name == "Exportar" && button.Content == "Exportar") // if the button is the long button and the name is Exportar and the content is Exportar
			{
				button.Content = "Exportando datos"; // change the button content
				button.Name = "datosescritos"; // change the button name
				longButtonClicked = true;
				return;
			}	
			
			if (button == longButton && button.Name == "datosescritos" && button.Content == "Done") // if the button is the long button and the name is datosescritos and the content is Done
			{
				Dispatcher.InvokeAsync((() =>		// update the button
				{
					if (myGrid != null) // if the grid is not null
					{
						if (longButton != null) // if the button is not null
						{
							myGrid.Children.Remove(longButton); // remove the button from the grid
							longButton.Click -= OnButtonClick; // remove the click event
							longButton = null; // set the button to null
						}
					}
				}));
				doneWriting = true;
				return;
			}
		}
		
		#region Properties
		
		#endregion		
	}
}
#region NinjaScript generated code. Neither change nor remove.
namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private JoiaEXPORTADOR[] cacheJoiaEXPORTADOR;
		public JoiaEXPORTADOR JoiaEXPORTADOR()
		{
			return JoiaEXPORTADOR(Input);
		}
		public JoiaEXPORTADOR JoiaEXPORTADOR(ISeries<double> input)
		{
			if (cacheJoiaEXPORTADOR != null)
				for (int idx = 0; idx < cacheJoiaEXPORTADOR.Length; idx++)
					if (cacheJoiaEXPORTADOR[idx] != null &&  cacheJoiaEXPORTADOR[idx].EqualsInput(input))
						return cacheJoiaEXPORTADOR[idx];
			return CacheIndicator<JoiaEXPORTADOR>(new JoiaEXPORTADOR(), input, ref cacheJoiaEXPORTADOR);
		}
	}
}
namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.JoiaEXPORTADOR JoiaEXPORTADOR()
		{
			return indicator.JoiaEXPORTADOR(Input);
		}
		public Indicators.JoiaEXPORTADOR JoiaEXPORTADOR(ISeries<double> input )
		{
			return indicator.JoiaEXPORTADOR(input);
		}
	}
}
namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.JoiaEXPORTADOR JoiaEXPORTADOR()
		{
			return indicator.JoiaEXPORTADOR(Input);
		}
		public Indicators.JoiaEXPORTADOR JoiaEXPORTADOR(ISeries<double> input )
		{
			return indicator.JoiaEXPORTADOR(input);
		}
	}
}
#endregion
