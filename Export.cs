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
	// Version 1.0
	// 12-6-2021 Release Date.
	//
	// Notes:  The purrpose of the indicator is to write all bar and indicator data to a Comma Seperated Variable file (CSV) that can be easily imported to Excel for your analysis purposes.  If an indicator
	// has plots that are rendered, such as Order Flow Volume profile, they will not be included.  If an indicator has hidden plots (transparent) they will be included.  The "Columns" will be in the order
	// that the indicators are listed in the indicator UI.  In a multiple plot indicator, the plots will be listed in the order as they are shown in the indicators properties.   This indicator will read the
	// data from the beginning of the data series to the last historical bar shown and will draw a vertical reference line on the price chart to illustrate where the last data was written.
	// 
	// Operational use:  Apply to chart,** YOU MUST WAIT ** until all indicators have finished calculating/reloading, When all hve finished calculating, Click button, data writes, when Done button shows "Done", The chart will have text advising what data has been written and
	// where it has been written and advises removing from chart.  Removing will close the file OR clicking the "Done" button will close the file (and removes the button) but the indicator will remain on chart.  If you
	// leave the indicator on the chart and ylou refresh indicators or add furtrher indicators, ChartToCSV will present the write data button again	
	// click Done to close CSV file and/OR remove from chart
	// to also close the CSV file.
	// 
	// The indicator will create a new file each time it is run.  The file name will include the primary instrument and the day of the week and the time of the day, as a reference point.
	//
	//
	
	public class ChartToCSV : Indicator
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
				Description									= @"Will cycle through indicators on panel and add their plot names on right side of chart";
				Name										= "ChartToCSV";
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
						Name = "MyCustomGrid", HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top
					};
					
					System.Windows.Controls.ColumnDefinition column1 = new System.Windows.Controls.ColumnDefinition();
					
					myGrid.ColumnDefinitions.Add(column1);
					
					longButton = new System.Windows.Controls.Button
					{
						Name = "WriteData", Content = "WriteData", Foreground = Brushes.White, Background = Brushes.Green
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
				
				path 	= NinjaTrader.Core.Globals.UserDataDir+Instrument.MasterInstrument.Name+" "+DateTime.Now.DayOfWeek+" "+DateTime.Now.Hour+DateTime.Now.Minute+ ".csv"; // Define the Path to our test file
				
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
							Print ("state.terminated, removed button?");
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
				Draw.VerticalLine(this, "writedata", 1, Brushes.SkyBlue);		// Draw vertical reference line on chart, previous bar is last historial
				ReadWrite();													// go get the data
				alsodoitonce = false;											// only once
				sw.Close();														// close the CSV file
				// provide feedback on chart to remove the indicator
				Draw.TextFixed(this, "datawritten1", "Historical chart and indicator data written from: "+start_Date+ " through "+end_Date
					+"\n In file: "+path+"\nRemove indicator "+this.Name+" from chart to remove this message", TextPosition.BottomLeft);
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
						Print (this.Name+"State error on Bar: "+n+"  indicator: "+indicator.Name+"  state: "+indicator.State);
						continue;
					}
				
					if (indicator.Name != "ChartToCSV")  // don't need to read this indicator...
					{	 	
						for (int seriesCount = 0; seriesCount <  indicator.Values.Length ; seriesCount++)  // process each plot of the indicator
						{
							Plot	plot				= indicator.Plots[seriesCount];						// get a plot from the indictor
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
											
					for (int h = 0; h < Labels.Count; h++)
					{
						sw.Write(Labels[h]);
					}
					sw.WriteLine();  // kick it to the next line
					doitonce = false;
					Dispatcher.InvokeAsync((() =>
					{
						longButton.Background = Brushes.Gold;
						longButton.Foreground = Brushes.Black;
						longButton.Content = "Done";
					}));
					
				}
					
				// write bar date and time first
				sw.Write(Time.GetValueAt(n).Date.ToShortDateString()+";"+Time.GetValueAt(n).TimeOfDay+";");
				
				if (n == 0)
					start_Date = Time.GetValueAt(n);		// save the start date for feedback
				if (n == Bars.Count-2)
					end_Date = Time.GetValueAt(n);			// save the end date for feedback
				// write data after on same line
				for (int j = 0; j < Data.Count; j++)
				{
					sw.Write(Data[j]+";");  				// write the data with a comma for comma delimitation
				}
				sw.WriteLine();								// kick it to the next line to write										
				}
			}
		
		private void OnButtonClick(object sender, RoutedEventArgs rea)
		{
			System.Windows.Controls.Button button = sender as System.Windows.Controls.Button;
			if (button == longButton && button.Name == "WriteData" && button.Content == "WriteData")
			{
				button.Content = "Writing data";
				button.Name = "DataWritten";
				longButtonClicked = true;
				return;
			}	
			
			if (button == longButton && button.Name == "DataWritten" && button.Content == "Done")
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
		private ChartToCSV[] cacheChartToCSV;
		public ChartToCSV ChartToCSV()
		{
			return ChartToCSV(Input);
		}

		public ChartToCSV ChartToCSV(ISeries<double> input)
		{
			if (cacheChartToCSV != null)
				for (int idx = 0; idx < cacheChartToCSV.Length; idx++)
					if (cacheChartToCSV[idx] != null &&  cacheChartToCSV[idx].EqualsInput(input))
						return cacheChartToCSV[idx];
			return CacheIndicator<ChartToCSV>(new ChartToCSV(), input, ref cacheChartToCSV);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ChartToCSV ChartToCSV()
		{
			return indicator.ChartToCSV(Input);
		}

		public Indicators.ChartToCSV ChartToCSV(ISeries<double> input )
		{
			return indicator.ChartToCSV(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ChartToCSV ChartToCSV()
		{
			return indicator.ChartToCSV(Input);
		}

		public Indicators.ChartToCSV ChartToCSV(ISeries<double> input )
		{
			return indicator.ChartToCSV(input);
		}
	}
}

#endregion
