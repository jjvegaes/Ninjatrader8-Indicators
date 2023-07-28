//
// Copyright (C) 2020, NinjaTrader LLC <www.ninjatrader.com>
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component
// Coded by NinjaTrader_ChelseaB
// Big thank you to NinjaTrader_MichaelM, and NinjaTrader_Jesse for their assistance, patience, and contributions
//
#region Using declarations
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript.DrawingTools;

using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Gui;
using NinjaTrader.Gui.NinjaScript.AtmStrategy;
using System.Linq;
using System.Windows.Data;
using System.Xml.Linq;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// The SampleWPFModifications demonstrates making UI changes to the chart.
	/// This adds a row to the chart grid for a top, left, and right area for custom WPF controls and also adds and modifies buttons in the ChartTrader area.
	/// 
	/// Note, I'll be using the fully qualified namespace path for each type only once, on the first use (for education)
	/// </summary>
	public class SampleWPFModifications : Indicator
	{
		#region Common chart object variables
		private System.Windows.Controls.Grid				chartGrid, chartTraderGrid, chartTraderButtonsGrid;
		private NinjaTrader.Gui.Chart.ChartTab				chartTab;
		private System.Windows.Style						chartTraderButtonStyle, mainMenuItemStyle, systemMenuStyle;
		private NinjaTrader.Gui.Chart.Chart					chartWindow;
		private System.Windows.Media.StreamGeometry			comboBoxArrowGeometry;
		private System.Windows.Media.SolidColorBrush		controlFontBrush;
		private System.Windows.Media.LinearGradientBrush	controlBackgroundBrush, dropMenuBackgroundBrush;
		private int											tabControlStartColumn, tabControlStartRow;
		private System.Windows.Controls.TabItem				tabItem;
		#endregion

		#region Use case #1: Custom top toolbar variables
		private SolidColorBrush								chartBackgroundBrush, controlBorderBrush, subMenuBrush;
		private System.Windows.Controls.StackPanel			toolbarTopHorizontalStackPanel;
		private System.Windows.Controls.MenuItem			toolbarTopMenuDropMenu1SubItem1, toolbarTopMenuDropMenu1SubItem2;
		private System.Windows.Controls.Menu				toolbarTopMenu1;
		private System.Windows.Controls.Button				toolbarTopMenuButton1;
		private bool										topToolbarActive;
		#endregion

		#region Use case #2: Custom left toolbar variables
		private System.Windows.Controls.MenuItem			toolbarLeftMenuDropMenu2SubItem1, toolbarLeftMenuDropMenu2SubItem2;
		private System.Windows.Controls.Button				toolbarLeftMenuButton2;
		private Grid										leftInnerGrid;
		private bool										leftToolbarActive;
		#endregion

		#region Use case #3: Custom right side panel variables

		private Button										rightSidePanelbutton1, rightSidePanelbutton2;
		private Grid										rightSidePanelGrid;
		private GridSplitter								rightSidePanelGridSplitter;
		private bool										rightSidePanelActive;
		#endregion

		#region Use case #4: Custom menu added to titlebar (NTBar) variables

		private bool										ntBarActive;
		private System.Windows.DependencyObject				searchObject;
		private Menu										ntBarMenu;
		private NinjaTrader.Gui.Tools.NTMenuItem			ntBartopMenuItem, ntBartopMenuItemSubItem1, ntBartopMenuItemSubItem2;
		#endregion

		#region Use case #5: Custom chart trader buttons variables		

		private System.Windows.Controls.RowDefinition		customCtaddedRow1, customCtaddedRow2;
		private Grid										lowerButtonsGrid, upperButtonsGrid;
		private Button[]									chartTraderCustomButtonsArray;
		private bool										customCtButtonsActive;
		#endregion

		#region Use case #6: Modify existing chart trader buttons variables

		private LinearGradientBrush							ctOriginalButtonBrush;
		private Button										modifyCtBuyMarketButton, modifyCtSellMarketButton;
		private System.Windows.Media.Brush					originalButtonColor;
		private bool										modifyCtButtonsActive;


		private bool ctOriginalButtonInfoFound = false;
		#endregion

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description							= @"Demonstrations of modifying wpf elements of a chart";
				Name								= "Sample WPF Modifications";
				Calculate							= Calculate.OnBarClose;
				IsOverlay							= true;
				DisplayInDataBox					= false;
				PaintPriceMarkers					= false;
				IsSuspendedWhileInactive			= false;
			}
			else if (State == State.Historical)
			{
				#region Use case #3: Custom right side panel initialize variables
				rightSidePanelActive		= false;
				#endregion

				if (ChartControl != null)
				{
					ChartControl.Dispatcher.InvokeAsync((System.Action)(() =>
					{
						LoadBrushesFromSkin();
						// WPF modifications wait until State.Historical to play nice with duplicating tabs
						CreateWPFControls();
					}));
				}
			}
			else if (State == State.Terminated)
			{
				if (ChartControl != null)
				{
					ChartControl.Dispatcher.InvokeAsync((() =>
					{
						DisposeWPFControls();
					}));
				}
			}
		}
		
		protected void CreateWPFControls()
		{
			// the main chart window
			chartWindow			= System.Windows.Window.GetWindow(ChartControl.Parent) as Chart;
			// if not added to a chart, do nothing
			if (chartWindow == null)
				return;
			// this is the grid in the chart window
			chartGrid			= chartWindow.MainTabControl.Parent as Grid;
			// this is the entire chart trader area grid
			chartTraderGrid		= (chartWindow.FindFirst("ChartWindowChartTraderControl") as ChartTrader).Content as Grid;
			
			#region Use case #1: Custom top toolbar wpf objects
			// TODO: should this be loaded from xaml? at least one example should be loaded from xaml to demonstrate both approaches, but probably all expect the simplest should be loaded with xaml. the textbox should state if the xaml file cannot be loaded.
			// upper tool bar objects
			// upper tool bar menu
			toolbarTopMenu1 = new Menu()
			{
				Background			= chartBackgroundBrush,
				BorderBrush			= chartBackgroundBrush,
				Padding				= new System.Windows.Thickness(0),
				Margin				= new Thickness(0),
				VerticalAlignment	= System.Windows.VerticalAlignment.Center
			};
			
			toolbarTopHorizontalStackPanel = new StackPanel()
			{
				Background			= chartBackgroundBrush,
				HorizontalAlignment	= System.Windows.HorizontalAlignment.Stretch,
				Orientation			= System.Windows.Controls.Orientation.Horizontal,
				Visibility			= Visibility.Hidden
			};

			// See 'Public class DropMenu' for the creation of this custom menu
			DropMenu customToolbarTopMenuDropMenu1 = new DropMenu()
			{
				// The menu Content property is what shows on the menu button
				// Content can be assigned a string, a UIElement, or something easily converted to a string

				// Try controlling the text size or font type by assigning a TextBlock to the content
				//Content				= new TextBlock() { Text = "My Text", FontFamily = new FontFamily("Arial"), FontSize = 20 },

				Content				= "Menu 1",
				Margin				= new Thickness(1),
				ToolTip				= "customToolbarTopMenuDropMenu1"

				// Try changing the colors
				//,Background		= Brushes.Blue
				//,Foreground		= Brushes.Yellow
				//,DownArrowBrush		= Brushes.Green
				//,PopupBackground		= Brushes.Purple
			};

			toolbarTopMenuDropMenu1SubItem1 = new MenuItem()
			{
				Header				= "Sub-MenuItem 1",
			};

			toolbarTopMenuDropMenu1SubItem1.Click += TopToolbarButtonMenu_Click;
			customToolbarTopMenuDropMenu1.Items.Add(toolbarTopMenuDropMenu1SubItem1);

			toolbarTopMenuDropMenu1SubItem2 = new MenuItem()
			{
				Header				= "Sub-MenuItem 2",
			};

			toolbarTopMenuDropMenu1SubItem2.Click += TopToolbarButtonMenu_Click;
			customToolbarTopMenuDropMenu1.Items.Add(toolbarTopMenuDropMenu1SubItem2);
			
			toolbarTopMenu1.Items.Add(customToolbarTopMenuDropMenu1);
			toolbarTopHorizontalStackPanel.Children.Add(toolbarTopMenu1);

			// upper tool bar button, has text and image
			toolbarTopMenuButton1 = new Button()
			{
				Background			= controlBackgroundBrush,
				BorderBrush			= controlBorderBrush,
				FontSize			= 12,
				Foreground			= controlFontBrush,
				HorizontalAlignment	= HorizontalAlignment.Left,
				Padding				= new Thickness(5, 2, 5, 2),
				Margin				= new Thickness(0, 1, 1, 1),
				MinWidth			= 1,
				ToolTip				= "customToolbarTopMenuButton1"
			};

			// this stackpanel allows us to place text and a picture horizontally in customToolbarTopMenuButton1
			StackPanel customToolbarTopMenuButton1StackPanel = new StackPanel()
			{
				Orientation			= Orientation.Horizontal,
				VerticalAlignment	= VerticalAlignment.Top,
				HorizontalAlignment	= HorizontalAlignment.Right
			};

			System.Windows.Controls.TextBlock newTextBlock = new TextBlock()
			{
				HorizontalAlignment	= HorizontalAlignment.Right,
				Margin				= new Thickness(0, 0, 0, 0),
				Text				= "Button 1",
				VerticalAlignment	= VerticalAlignment.Top
			};

			customToolbarTopMenuButton1StackPanel.Children.Add(newTextBlock);

			// check to see if an image exists in Documents\NinjaTrader 8\templates\SampleWPFModifications called B1.png.
			// if its there, include this with the button
			System.Windows.Media.Imaging.BitmapImage buttonImage = null;

			// try and find an image file to go in the button
			try	{ buttonImage = new System.Windows.Media.Imaging.BitmapImage(new Uri(NinjaTrader.Core.Globals.UserDataDir + @"templates\SampleWPFModifications\B1.png")); }
			// button image can't be found then we won't add the imageControl later
			catch (Exception e) { buttonImage = null; }

			System.Windows.Controls.Image imageControl = null;

			if (buttonImage != null)
			{
				imageControl = new Image()
				{
					Source		= buttonImage,
					Height		= 10,
					Margin		= new Thickness(5, 0, 0, 0),
					Width		= 10
				};
			}

			if (buttonImage != null)
				customToolbarTopMenuButton1StackPanel.Children.Add(imageControl);

			toolbarTopMenuButton1.Content	= customToolbarTopMenuButton1StackPanel;
			toolbarTopMenuButton1.Click		+= TopToolbarButtonMenu_Click;

			Grid customToolbarTopMenuGrid1 = new Grid()
			{
				Background				= chartBackgroundBrush,
				HorizontalAlignment		= HorizontalAlignment.Left
			};

			customToolbarTopMenuGrid1.Children.Add(toolbarTopMenuButton1);
			toolbarTopHorizontalStackPanel.Children.Add(customToolbarTopMenuGrid1);

			chartGrid.Children.Add(toolbarTopHorizontalStackPanel);
			toolbarTopHorizontalStackPanel.Visibility = Visibility.Hidden;
			#endregion

			#region Use case #2: Custom left toolbar wpf objects

			// left toolbar objects
			// each vertical object needs its own menu
			leftInnerGrid = new Grid()
			{
				Visibility		= Visibility.Hidden
			};

			leftInnerGrid.RowDefinitions.Add(new RowDefinition());
			leftInnerGrid.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);

			// this gives a little space at the bottom to line up with the bottom of the chart
			leftInnerGrid.RowDefinitions.Add(new RowDefinition());
			leftInnerGrid.RowDefinitions[1].Height = new GridLength(39);

			Menu customToolbarLeftMenu1 = new Menu()
			{
				Background			= chartBackgroundBrush,
				Margin				= new Thickness(0),
				Padding				= new Thickness(0)
			};

			// this allows our menus to stack vertically
			System.Windows.Controls.VirtualizingStackPanel customToolBarVerticalStackPanel = new VirtualizingStackPanel()
			{
				Background			= chartBackgroundBrush,
				HorizontalAlignment	= HorizontalAlignment.Stretch,
				Orientation			= Orientation.Vertical,
				VerticalAlignment	= VerticalAlignment.Stretch
			};

			// See 'Public class DropMenu' declaration for the creation of this custom menu
			DropMenu customToolbarLeftMenuDropMenu2 = new DropMenu()
			{
				// The menu Content is what shows on the menu
				// Content cabe be assigned a string, a UIElement, or something easily converted to a string

				// Try controlling the text size or font type by assigning a TextBlock to the content
				//Content				= new TextBlock() { Text = "My Text", FontFamily = new FontFamily("Arial"), FontSize = 20 },

				Content				= "Menu 2",
				Margin				= new Thickness(1),
				ToolTip				= "customToolbarLeftMenuDropMenu2"

				// Try changing the colors
				//,Background		= Brushes.Blue
				//,Foreground		= Brushes.Yellow
				//,DownArrowBrush		= Brushes.Green
				//,PopupBackground		= Brushes.Purple
			};

			toolbarLeftMenuDropMenu2SubItem1 = new NTMenuItem()
			{
				BorderThickness		= new Thickness(0),
				Header				= "Sub-MenuItem 1"
			};

			toolbarLeftMenuDropMenu2SubItem1.Click += LeftToolbarButtonMenu_Click;
			customToolbarLeftMenuDropMenu2.Items.Add(toolbarLeftMenuDropMenu2SubItem1);

			toolbarLeftMenuDropMenu2SubItem2 = new NTMenuItem()
			{
				Header				= "Sub-MenuItem 2",
			};

			toolbarLeftMenuDropMenu2SubItem2.Click += LeftToolbarButtonMenu_Click;
			customToolbarLeftMenuDropMenu2.Items.Add(toolbarLeftMenuDropMenu2SubItem2);

			customToolbarLeftMenu1.Items.Add(customToolbarLeftMenuDropMenu2);
			customToolBarVerticalStackPanel.Children.Add(customToolbarLeftMenu1);

			Grid customToolbarLeftMenuGrid2 = new Grid()
			{
				HorizontalAlignment		= HorizontalAlignment.Stretch,
			};

			toolbarLeftMenuButton2 = new Button()
			{
				Background			= controlBackgroundBrush,
				BorderBrush			= controlBorderBrush,
				Content				= "Button 2",
				FontSize			= 12,
				Foreground			= controlFontBrush,				
				ToolTip				= "customToolbarLeftMenuButton2",
				Padding				= new Thickness(5, 2, 5, 2),
				Margin				= new Thickness(1, 0, 1, 1),
				MinWidth			= 1
			};

			toolbarLeftMenuButton2.Click += LeftToolbarButtonMenu_Click;
			customToolbarLeftMenuGrid2.Children.Add(toolbarLeftMenuButton2);

			customToolBarVerticalStackPanel.Children.Add(customToolbarLeftMenuGrid2);
			leftInnerGrid.Children.Add(customToolBarVerticalStackPanel);

			chartGrid.Children.Add(leftInnerGrid);
			#endregion

			#region Use case #3: Custom right side panel wpf objects
			// the buttons in the right panel will be within this grid
			rightSidePanelGrid = new Grid() { };
			// this gridsplitter will allow the column our grid is in to be resized
			rightSidePanelGridSplitter	= new GridSplitter()
			{
				Background			= Brushes.Transparent,
				HorizontalAlignment	= HorizontalAlignment.Left,
				ResizeBehavior		= GridResizeBehavior.BasedOnAlignment,
				ResizeDirection		= GridResizeDirection.Columns,
				VerticalAlignment	= VerticalAlignment.Stretch,
				Width				= 6
			};

			// add 3 rows for a row of text, a row for first button, a row for second button
			rightSidePanelGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(50) });
			rightSidePanelGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(50) });
			rightSidePanelGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(50) });

			TextBlock label = new TextBlock()
			{
				FontFamily			= ChartControl.Properties.LabelFont.Family,
				FontSize			= 13,
				Foreground			= ChartControl.Properties.ChartText,
				HorizontalAlignment	= HorizontalAlignment.Center,
				Margin				= new Thickness(5, 5, 5, 5),
				Text				= string.Format("{0} {1} {2}", Instrument.FullName, BarsPeriod.Value, BarsPeriod.BarsPeriodType)
			};

			Grid.SetRow(label, 0);
			rightSidePanelGrid.Children.Add(label);

			rightSidePanelbutton1 = new Button()
			{
				Content				= "Button 1",
				HorizontalAlignment	= HorizontalAlignment.Center
			};
			rightSidePanelbutton1.Click += SidePanelButton_Click;

			Grid.SetRow(rightSidePanelbutton1, 1);
			rightSidePanelGrid.Children.Add(rightSidePanelbutton1);

			rightSidePanelbutton2 = new Button()
			{
				Content				= "Button 2",
				HorizontalAlignment	= HorizontalAlignment.Center
			};
			rightSidePanelbutton2.Click += SidePanelButton_Click;

			Grid.SetRow(rightSidePanelbutton2, 2);
			rightSidePanelGrid.Children.Add(rightSidePanelbutton2);
			#endregion

			#region Use case #4: Custom menu added to titlebar (NTBar) wpf objects
			// this is the actual object that you add to the chart windows Main Menu
			// which will act as a container for all the menu items
			ntBarMenu = new Menu
			{
				// important to set the alignment, otherwise you will never see the menu populated
				VerticalAlignment			= VerticalAlignment.Top,
				VerticalContentAlignment	= VerticalAlignment.Top,
				// make sure to style as a System Menu	
				Style						= systemMenuStyle
			};

			// thanks to Jesse for these figures to use for the icon
			System.Windows.Media.Geometry topMenuItem1Icon = Geometry.Parse("m 70.5 173.91921 c -4.306263 -1.68968 -4.466646 -2.46776 -4.466646 -21.66921 0 -23.88964 -1.364418 -22.5 22.091646 -22.5 23.43572 0 22.08568 -1.36412 22.10832 22.33888 0.0184 19.29356 -0.19638 20.3043 -4.64473 21.85501 -2.91036 1.01455 -32.493061 0.99375 -35.08859 -0.0247 z M 21 152.25 l 0 -7.5 20.25 0 20.25 0 0 7.5 0 7.5 -20.25 0 -20.25 0 0 -7.5 z m 93.75 0 0 -7.5 42.75 0 42.75 0 0 7.5 0 7.5 -42.75 0 -42.75 0 0 -7.5 z m 15.75 -38.33079 c -4.30626 -1.68968 -4.46665 -2.46775 -4.46665 -21.66921 0 -23.889638 -1.36441 -22.5 22.09165 -22.5 23.43572 0 22.08568 -1.364116 22.10832 22.338885 0.0185 19.293555 -0.19638 20.304295 -4.64473 21.855005 -2.91036 1.01455 -32.49306 0.99375 -35.08859 -0.0247 z M 21 92.25 l 0 -7.5 50.25 0 50.25 0 0 7.5 0 7.5 -50.25 0 -50.25 0 0 -7.5 z m 153.75 0 0 -7.5 12.75 0 12.75 0 0 7.5 0 7.5 -12.75 0 -12.75 0 0 -7.5 z M 55.5 53.919211 C 51.193737 52.229528 51.033354 51.451456 51.033354 32.25 51.033354 8.3603617 49.668936 9.75 73.125 9.75 96.560723 9.75 95.210685 8.3858835 95.23332 32.088887 95.25177 51.382441 95.03694 52.393181 90.588593 53.943883 87.678232 54.95844 58.095529 54.93764 55.5 53.919211 Z M 21 32.25 l 0 -7.5 12.75 0 12.75 0 0 7.5 0 7.5 -12.75 0 -12.75 0 0 -7.5 z m 78.75 0 0 -7.5 50.25 0 50.25 0 0 7.5 0 7.5 -50.25 0 -50.25 0 0 -7.5 z");

			// this is the menu item which will appear on the chart's Main Menu
			ntBartopMenuItem = new NTMenuItem()
			{
				// comment out or delete the Header assignment below to only show the icon
				Header				= "NTBar Menu",
				Icon				= topMenuItem1Icon,
				Margin				= new Thickness(0),
				Padding				= new Thickness(1),
				Style				= mainMenuItemStyle,
				VerticalAlignment	= VerticalAlignment.Center
			};

			ntBarMenu.Items.Add(ntBartopMenuItem);

			ntBartopMenuItemSubItem1 = new NTMenuItem()
			{
				BorderThickness		= new Thickness(0),
				Header				= "Sub-MenuItem 1"
			};

			ntBartopMenuItemSubItem1.Click += NTBarMenu_Click;
			ntBartopMenuItem.Items.Add(ntBartopMenuItemSubItem1);

			ntBartopMenuItemSubItem2 = new NTMenuItem()
			{
				Header				= "Sub-MenuItem 2"
			};

			ntBartopMenuItemSubItem2.Click += NTBarMenu_Click;
			ntBartopMenuItem.Items.Add(ntBartopMenuItemSubItem2);

			// add the menu which contains all menu items to the chart
			//chartWindow.MainMenu.Add(ntBarMenu);
			#endregion

			#region Use case #5: Custom chart trader buttons wpf objects
			// This adds two grid spaces for buttons to the chart trader area. One is below the pnl box (and above the instrument selector, one is below the bid and ask (at the bottom). Rows can be added to either the upper or lower grid to add more buttons in.

			// this grid contains the existing chart trader buttons
			chartTraderButtonsGrid	= chartTraderGrid.Children[0] as Grid;

			// this grid is a grid i'm adding to a new row (at the bottom) in the grid that contains bid and ask prices and order controls (chartTraderButtonsGrid)
			upperButtonsGrid = new Grid();
			Grid.SetColumnSpan(upperButtonsGrid, 3);

			upperButtonsGrid.ColumnDefinitions.Add(new ColumnDefinition());
			upperButtonsGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength((double)Application.Current.FindResource("MarginBase")) }); // separator column
			upperButtonsGrid.ColumnDefinitions.Add(new ColumnDefinition());

			// this grid is to organize stuff below
			lowerButtonsGrid = new Grid();
			Grid.SetColumnSpan(lowerButtonsGrid, 4);

			lowerButtonsGrid.ColumnDefinitions.Add(new ColumnDefinition());
			lowerButtonsGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength((double)Application.Current.FindResource("MarginBase")) });
			lowerButtonsGrid.ColumnDefinitions.Add(new ColumnDefinition());

			// these rows will be added later, but we can create them now so they only get created once
			customCtaddedRow1	= new RowDefinition() { Height = new GridLength(31) };
			customCtaddedRow2	= new RowDefinition() { Height = new GridLength(40) };

			// this style (provided by NinjaTrader_MichaelM) gives the correct default minwidth (and colors) to make buttons appear like chart trader buttons

			// all of the buttons are basically the same so to save lines of code I decided to use a loop over an array
			chartTraderCustomButtonsArray = new Button[4];

			for (int i = 0; i < 4; ++i)
			{
				chartTraderCustomButtonsArray[i]	= new Button()
				{
					Content			= string.Format("Button {0}", i + 1),
					Height			= 30,
					Margin			= new Thickness(0,0,0,0),
					Padding			= new Thickness(0,0,0,0),
					Style			= chartTraderButtonStyle
				};

				// change colors of the buttons if you'd like. i'm going to change the first and fourth.
				if (i % 3 != 0)
				{
					chartTraderCustomButtonsArray[i].Background		= Brushes.Gray;
					chartTraderCustomButtonsArray[i].BorderBrush	= Brushes.DimGray;
				}
			}

			chartTraderCustomButtonsArray[0].Click += ChartTraderButtonMenu_Click;
			chartTraderCustomButtonsArray[1].Click += ChartTraderButtonMenu_Click;
			chartTraderCustomButtonsArray[2].Click += ChartTraderButtonMenu_Click;
			chartTraderCustomButtonsArray[3].Click += ChartTraderButtonMenu_Click;

			Grid.SetColumn(chartTraderCustomButtonsArray[1], 2);
			// add button3 to the lower grid
			Grid.SetColumn(chartTraderCustomButtonsArray[2], 0);
			// add button4 to the lower grid
			Grid.SetColumn(chartTraderCustomButtonsArray[3], 2);
			for (int i = 0; i < 2; ++i)
				upperButtonsGrid.Children.Add(chartTraderCustomButtonsArray[i]);
			for (int i = 2; i < 4; ++i)
				lowerButtonsGrid.Children.Add(chartTraderCustomButtonsArray[i]);
			#endregion

			#region Use case #6: Modify existing chart trader buttons wpf objects

			modifyCtBuyMarketButton			= chartTraderGrid.FindFirst("ChartTraderControlQuickBuyMarketButton") as Button;
			modifyCtSellMarketButton		= chartTraderGrid.FindFirst("ChartTraderControlQuickSellMarketButton") as Button;
			#endregion

			if (TabSelected())
				ShowWPFControls();

			chartWindow.MainTabControl.SelectionChanged += TabChangedHandler;
		}

		private void DisposeWPFControls()
		{
			if (chartWindow != null)
				chartWindow.MainTabControl.SelectionChanged -= TabChangedHandler;

			HideWPFControls();
			
			#region Use case #1: Custom top toolbar remove handlers / dispose objects

			if (toolbarTopMenuDropMenu1SubItem1 != null)
				toolbarTopMenuDropMenu1SubItem1.Click -= TopToolbarButtonMenu_Click;

			if (toolbarTopMenuDropMenu1SubItem2 != null)
				toolbarTopMenuDropMenu1SubItem2.Click -= TopToolbarButtonMenu_Click;

			if (toolbarTopMenuButton1 != null)
				toolbarTopMenuButton1.Click -= TopToolbarButtonMenu_Click;

			chartGrid.Children.Remove(toolbarTopHorizontalStackPanel);
			#endregion

			#region Use case #2: Custom left toolbar remove handlers / dispose objects

			if (toolbarLeftMenuDropMenu2SubItem1 != null)
				toolbarLeftMenuDropMenu2SubItem1.Click -= LeftToolbarButtonMenu_Click;

			if (toolbarLeftMenuDropMenu2SubItem2 != null)
				toolbarLeftMenuDropMenu2SubItem2.Click -= LeftToolbarButtonMenu_Click;

			if (toolbarLeftMenuButton2 != null)
				toolbarLeftMenuButton2.Click -= LeftToolbarButtonMenu_Click;

			chartGrid.Children.Remove(leftInnerGrid);
			#endregion

			#region Use case #3: Custom right side panel remove remove handlers / dispose objects

			if (rightSidePanelbutton1 != null)
				rightSidePanelbutton1.Click -= SidePanelButton_Click;

			if (rightSidePanelbutton2 != null)
				rightSidePanelbutton2.Click -= SidePanelButton_Click;
			#endregion

			#region Use case #4: Custom menu added to titlebar (NTBar) remove handlers / dispose objects

			if (ntBartopMenuItemSubItem1 != null)
				ntBartopMenuItemSubItem1.Click -= NTBarMenu_Click;

			if (ntBartopMenuItemSubItem2 != null)
				ntBartopMenuItemSubItem2.Click -= NTBarMenu_Click;

			if (ntBarMenu != null)
			{
				chartWindow.MainMenu.Remove(ntBarMenu);
				ntBarActive = false;
			}
			#endregion

			#region Use case #5: Custom chart trader buttons remove handlers / dispose objects

			for (int i = 0; i < 4; i++)
				if (chartTraderCustomButtonsArray[i] != null)
					chartTraderCustomButtonsArray[i].Click -= ChartTraderButtonMenu_Click;

			if (customCtButtonsActive)
			{
				if (chartTraderButtonsGrid != null || upperButtonsGrid != null)
				{
					chartTraderButtonsGrid.Children.Remove(upperButtonsGrid);
					chartTraderButtonsGrid.RowDefinitions.Remove(customCtaddedRow1);
				}

				if (chartTraderButtonsGrid != null || lowerButtonsGrid != null)
				{
					chartTraderGrid.Children.Remove(lowerButtonsGrid);
					chartTraderGrid.RowDefinitions.Remove(customCtaddedRow2);
				}

				customCtButtonsActive	= false;
			}
			#endregion
		}

		private void HideWPFControls()
		{
			#region Use case #1: Custom top toolbar hide controls

			if (topToolbarActive)
			{
				if (toolbarTopHorizontalStackPanel != null)
				{
					chartGrid.RowDefinitions.RemoveAt(Grid.GetRow(toolbarTopHorizontalStackPanel));
					toolbarTopHorizontalStackPanel.Visibility = Visibility.Collapsed;
				}

				// if the childs column is 1 (so we can move it to 0) and the column is to the right of the column we are removing, shift it left
				for (int i = 0; i < chartGrid.Children.Count; i++)
				{
					if (Grid.GetRow(chartGrid.Children[i]) > 0 && Grid.GetRow(chartGrid.Children[i]) > Grid.GetRow(toolbarTopMenu1))
						Grid.SetRow(chartGrid.Children[i], Grid.GetRow(chartGrid.Children[i]) - 1);
				}

				topToolbarActive = false;
			}
			#endregion

			#region Use case #2: Custom left toolbar hide controls

			if (leftToolbarActive)
			{
				if (leftInnerGrid != null)
				{
					chartGrid.ColumnDefinitions.RemoveAt(Grid.GetColumn(leftInnerGrid));
					leftInnerGrid.Visibility = Visibility.Collapsed;
				}

				for (int i = 0; i < chartGrid.Children.Count; i++)
				{
					if (Grid.GetColumn(chartGrid.Children[i]) > 0 && Grid.GetColumn(chartGrid.Children[i]) > Grid.GetColumn(leftInnerGrid))
						Grid.SetColumn(chartGrid.Children[i], Grid.GetColumn(chartGrid.Children[i]) - 1);
				}

				leftToolbarActive = false;
			}
			#endregion

			#region Use case #3: Custom right side panel remove controls

			if (rightSidePanelActive)
			{
				// remove the column of our added grid
				chartGrid.ColumnDefinitions.RemoveAt(Grid.GetColumn(rightSidePanelGrid));
				// then remove the grid and gridsplitter
				chartGrid.Children.Remove(rightSidePanelGrid);
				chartGrid.Children.Remove(rightSidePanelGridSplitter);

				// if the childs column is 1 (so we can move it to 0) and the column is to the right of the column we are removing, shift it left
				for (int i = 0; i < chartGrid.Children.Count; i++)
					if ( Grid.GetColumn(chartGrid.Children[i]) > 0 && Grid.GetColumn(chartGrid.Children[i]) > Grid.GetColumn(rightSidePanelGrid) )
						Grid.SetColumn(chartGrid.Children[i], Grid.GetColumn(chartGrid.Children[i]) - 1);

				rightSidePanelActive = false;
			}
			#endregion

			#region Use case #4: Custom menu added to titlebar (NTBar) hide controls.

			if (ntBarActive)
			{
				chartWindow.MainMenu.Remove(ntBarMenu);
				ntBarActive					= false;
			}
			#endregion

			#region Use case #5: Custom chart trader buttons remove controls

			if (customCtButtonsActive)
			{
				if (chartTraderButtonsGrid != null || upperButtonsGrid != null)
				{
					chartTraderButtonsGrid.Children.Remove(upperButtonsGrid);
					chartTraderButtonsGrid.RowDefinitions.Remove(customCtaddedRow1);
				}

				if (chartTraderButtonsGrid != null || lowerButtonsGrid != null)
				{
					chartTraderGrid.Children.Remove(lowerButtonsGrid);
					chartTraderGrid.RowDefinitions.Remove(customCtaddedRow2);
				}

				customCtButtonsActive = false;
			}
			#endregion

			#region Use case #6: Modify existing chart trader buttons hide controls
			// if this instance of the script has added event handlers and changed colors, then set everything back to original state
			if (modifyCtButtonsActive)
			{
				// when the tab is selected or the indicator is removed, reset the colors and remove the added click handler
				if (modifyCtBuyMarketButton != null)
				{
					modifyCtBuyMarketButton.Click		-= ModifyCtButton_Click;
					modifyCtBuyMarketButton.Background	= ctOriginalButtonBrush;
				}

				if (modifyCtSellMarketButton != null)
				{
					modifyCtSellMarketButton.Click		-= ModifyCtButton_Click;
					modifyCtSellMarketButton.Background	= ctOriginalButtonBrush;
				}

				Button foundButton = chartTraderButtonsGrid.FindFirst("ctOriginalButtonInfo") as Button;
				if (foundButton != null)
					chartTraderButtonsGrid.Children.Remove(foundButton);

				modifyCtButtonsActive = false;
			}
			#endregion
		}

		private void LoadBrushesFromSkin()
		{
			// while pulling brushes from a skin to use later in the chart,
			// sometimes we need to be in the thread of the chart when the brush is initialized

			#region CommonResources

			chartBackgroundBrush		= System.Windows.Application.Current.TryFindResource("ChartControl.ChartBackground") as SolidColorBrush ?? new SolidColorBrush(Brushes.Purple.Color);
			mainMenuItemStyle			= Application.Current.TryFindResource("MainMenuItem") as Style;
			systemMenuStyle				= Application.Current.TryFindResource("SystemMenuStyle") as Style;

			controlFontBrush			= Application.Current.TryFindResource("FontButtonBrush") as SolidColorBrush ?? new SolidColorBrush(Brushes.Purple.Color);
			controlBackgroundBrush		= Application.Current.TryFindResource("ButtonBackgroundBrush") as LinearGradientBrush ?? new LinearGradientBrush(Colors.Purple, Colors.Pink, 1);
			controlBorderBrush			= Application.Current.TryFindResource("ButtonBorderBrush") as SolidColorBrush ?? Brushes.Purple;
			dropMenuBackgroundBrush		= Application.Current.TryFindResource("ComboBoxBackgroundBrush") as LinearGradientBrush ?? new LinearGradientBrush(Colors.Purple, Colors.Pink, 1);
			subMenuBrush				= Application.Current.TryFindResource("SubMenuBackground") as SolidColorBrush ?? new SolidColorBrush(Brushes.Purple.Color);
			#endregion

			#region Use case #5: Custom chart trader buttons			
			// this style (provided by NinjaTrader_MichaelM) gives the correct default minwidth (and colors) to make buttons appear like chart trader buttons
			chartTraderButtonStyle		= Application.Current.TryFindResource("BasicEntryButton") as Style;
			#endregion

			#region Use case #6: Modify existing chart trader buttons initialize variables

			ctOriginalButtonBrush		= Application.Current.TryFindResource("ChartTrader.ButtonBackground") as LinearGradientBrush ?? new LinearGradientBrush(Brushes.Purple.Color, Brushes.Pink.Color, 1);
			#endregion
		}

		protected override void OnBarUpdate() { }

		private void ShowWPFControls()
		{
			#region Use case #1: Custom top toolbar insert controls

			if (!topToolbarActive)
			{
				// if no indicator has added rows to the chart grid, add first row that becomes the chart row
				// this doesn't actually change the chart, but allows us to work with other indicators that add rows, or multiple instances
				if (chartGrid.RowDefinitions.Count == 0)
					chartGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
				
				// save the current row of the chart, which will become our row
				tabControlStartRow		= Grid.GetRow(chartWindow.MainTabControl);
				// insert a new row above the chart row
				chartGrid.RowDefinitions.Insert(tabControlStartRow, new RowDefinition() { Height = new GridLength(24) });

				// move all items below the chart, including the chart, down one row
				for (int i = 0; i < chartGrid.Children.Count; i++)
					if (Grid.GetRow(chartGrid.Children[i]) >= tabControlStartRow)
						Grid.SetRow(chartGrid.Children[i], Grid.GetRow(chartGrid.Children[i]) + 1);

				// set the columns and rows for our new items to the now emptied row space where the chart previously was
				Grid.SetColumn(toolbarTopHorizontalStackPanel, Grid.GetColumn(chartWindow.MainTabControl));
				Grid.SetRow(toolbarTopHorizontalStackPanel, tabControlStartRow);				

				// show the toolbar
				toolbarTopHorizontalStackPanel.Visibility	= Visibility.Visible;
				// let the script know the panel is active
				topToolbarActive							= true;
			}
			#endregion

			#region Use case #2: Custom left toolbar insert controls

			if (!leftToolbarActive)
			{
				tabControlStartColumn = Grid.GetColumn(chartWindow.MainTabControl);
				// insert a new column before the chart column
				chartGrid.ColumnDefinitions.Insert(tabControlStartColumn, new ColumnDefinition() { Width = new GridLength(0, GridUnitType.Auto) });

				// move all items right of the chart, and including the chart, one column right
				for (int i = 0; i < chartGrid.Children.Count; i++)
					if (Grid.GetColumn(chartGrid.Children[i]) >= tabControlStartColumn)
						Grid.SetColumn(chartGrid.Children[i], Grid.GetColumn(chartGrid.Children[i]) + 1);

				// set the columns and rows for our new items
				Grid.SetColumn(leftInnerGrid, tabControlStartColumn);
				Grid.SetRow(leftInnerGrid, Grid.GetRow(chartWindow.MainTabControl));

				// show the toolbar
				leftInnerGrid.Visibility	= Visibility.Visible;
				// left the script know the panel is active
				leftToolbarActive			= true;
			}
			#endregion

			#region Use case #3: Custom right side panel insert controls

			if (!rightSidePanelActive)
			{
				tabControlStartColumn = Grid.GetColumn(chartWindow.MainTabControl);

				// a new column is added to the right of MainTabControl
				chartGrid.ColumnDefinitions.Insert((tabControlStartColumn + 1), new ColumnDefinition()
				{
					// The width will need to be GridUnitType.Star to work with the gridsplitter from chartTrader (as well as our own)
					// The width set here is a ratio to other star columns (such as when we make the chart column starred below)
					Width		= new GridLength(1, GridUnitType.Star),
					// the minimum width should at least be big enough to grab our added gridspliiter with mouse
					MinWidth	= 10
				});

				// all items to the right of the MainTabControl are shifted to the right
				for (int i = 0; i < chartGrid.Children.Count; i++)
					if (Grid.GetColumn(chartGrid.Children[i]) > tabControlStartColumn)
						Grid.SetColumn(chartGrid.Children[i], Grid.GetColumn(chartGrid.Children[i]) + 1);

				// and then we set our new grid to be within the new column of the chart grid (and on the same row as the MainTabControl)
				Grid.SetColumn(rightSidePanelGrid, Grid.GetColumn(chartWindow.MainTabControl) + 1);
				Grid.SetRow(rightSidePanelGrid, Grid.GetRow(chartWindow.MainTabControl));

				chartGrid.Children.Add(rightSidePanelGrid);

				// add a grid splitter to the same column as our side panel grid to allow us to resize the width of our panel
				Grid.SetColumn(rightSidePanelGridSplitter, Grid.GetColumn(rightSidePanelGrid));
				Grid.SetRow(rightSidePanelGridSplitter, Grid.GetRow(rightSidePanelGrid));

				chartGrid.Children.Add(rightSidePanelGridSplitter);

				// to work with the added gridsplitter, the chart column to the left needs to be width star and larger than our panel
				chartGrid.ColumnDefinitions[Grid.GetColumn(chartWindow.MainTabControl)].Width = new GridLength(5, GridUnitType.Star);

				// let the script know the panel is active
				rightSidePanelActive = true;
			}
			#endregion

			#region Use case #4: Custom menu added to titlebar (NTBar) insert controls
			if (!ntBarActive)
			{
				chartWindow.MainMenu.Add(ntBarMenu);
				ntBarActive					= true;
			}
			#endregion

			#region Use case #5: Custom chart trader buttons insert controls
					
			if (!customCtButtonsActive)
			{
				// add a new row (addedRow1) for upperButtonsGrid to the existing buttons grid
				chartTraderButtonsGrid.RowDefinitions.Add(customCtaddedRow1);
				// set our upper grid to that new panel
				Grid.SetRow(upperButtonsGrid, (chartTraderButtonsGrid.RowDefinitions.Count - 1));
				// and add it to the buttons grid
				chartTraderButtonsGrid.Children.Add(upperButtonsGrid);
			
				// add a new row (addedRow2) for our lowerButtonsGrid below the ask and bid prices and pnl display			
				chartTraderGrid.RowDefinitions.Add(customCtaddedRow2);
				Grid.SetRow(lowerButtonsGrid, (chartTraderGrid.RowDefinitions.Count - 1));
				chartTraderGrid.Children.Add(lowerButtonsGrid);

				customCtButtonsActive	= true;
			}
			#endregion

			#region Use case #6: Modify existing chart trader buttons insert controls
			// add a hidden button here we can use to persist on the chart.
			// if the button exists, then we've already added this script once and we should do nothing
			// the goal is to only allow the handlers and colors to be set by one instance of this script
			// .FindFirst is searching by AutomationId
			Button foundButton = chartTraderButtonsGrid.FindFirst("ctOriginalButtonInfo") as Button;
			// if the button doesn't exist then create and add it using an AutomationID that other script instances can see
			if (foundButton == null)
			{
				Button ctOriginalButtonInfo = new Button()
				{
					Visibility = Visibility.Collapsed,
					Background = ctOriginalButtonBrush
				};

				chartTraderButtonsGrid.Children.Add(ctOriginalButtonInfo);
				// set an AutomationId name so that we can check the chart trader area for the button so we can find it later
				System.Windows.Automation.AutomationProperties.SetAutomationId(ctOriginalButtonInfo, "ctOriginalButtonInfo");
				
				// when the tab is selected or the indicator is added change the button colors and add an additional event handler
				if (modifyCtBuyMarketButton != null)
				{
					modifyCtBuyMarketButton.Click		+= ModifyCtButton_Click;
					modifyCtBuyMarketButton.Background	= Brushes.Green;
				}

				if (modifyCtSellMarketButton != null)
				{
					modifyCtSellMarketButton.Click		+= ModifyCtButton_Click;
					modifyCtSellMarketButton.Background	= Brushes.Red;
				}

				modifyCtButtonsActive = true;
			}
			#endregion
		}

		#region Use case #1: Custom top toolbar click handler method

		protected void TopToolbarButtonMenu_Click(object sender, RoutedEventArgs eventArgs)
		{
			if (sender is MenuItem)
			{
				MenuItem menuItem = sender as MenuItem;

				if (menuItem == toolbarTopMenuDropMenu1SubItem1)
					Draw.TextFixed(this, "infobox", "Top toolbar > Menu 1 > Sub-MenuItem 1", TextPosition.BottomLeft, Brushes.Green, new SimpleFont("Arial", 25), Brushes.Transparent, Brushes.Transparent, 100);

				else if (menuItem == toolbarTopMenuDropMenu1SubItem2)
					Draw.TextFixed(this, "infobox", "Top toolbar > Menu 1 > Sub-MenuItem 2", TextPosition.BottomLeft, Brushes.ForestGreen, new SimpleFont("Arial", 25), Brushes.Transparent, Brushes.Transparent, 100);
			}

			else if (sender is Button)
			{
				Button button = sender as Button;

				if (button == toolbarTopMenuButton1)
					Draw.TextFixed(this, "infobox", "Top toolbar > Button 1", TextPosition.BottomLeft, Brushes.OrangeRed, new SimpleFont("Arial", 25), Brushes.Transparent, Brushes.Transparent, 100);
			}

			ForceRefresh();
		}
		#endregion

		#region Use case #2: Custom top toolbar click handler method

		protected void LeftToolbarButtonMenu_Click(object sender, RoutedEventArgs eventArgs)
		{
			if (sender is MenuItem)
			{
				MenuItem menuItem = sender as MenuItem;

				if (menuItem == toolbarLeftMenuDropMenu2SubItem1)
					// full qualified namespaces used here to show where these tools are
					NinjaTrader.NinjaScript.DrawingTools.Draw.TextFixed(this, "infobox", "Custom toolbar left > Menu 2 > Sub-MenuItem 1", NinjaTrader.NinjaScript.DrawingTools.TextPosition.BottomLeft, Brushes.DarkMagenta, new Gui.Tools.SimpleFont("Arial", 25), Brushes.Transparent, Brushes.Transparent, 100);

				else if (menuItem == toolbarLeftMenuDropMenu2SubItem2)
					Draw.TextFixed(this, "infobox", "Left toolbar > Menu 2 > Sub-MenuItem 2", TextPosition.BottomLeft, Brushes.DarkOrchid, new SimpleFont("Arial", 25), Brushes.Transparent, Brushes.Transparent, 100);
			}

			else if (sender is Button)
			{
				Button button = sender as Button;

				if (button == toolbarLeftMenuButton2)
					Draw.TextFixed(this, "infobox", "Left toolbar > Button 2", TextPosition.BottomLeft, Brushes.MediumTurquoise, new SimpleFont("Arial", 25), Brushes.Transparent, Brushes.Transparent, 100);
			}

			ForceRefresh();
		}
		#endregion

		#region Use case #3: Custom right side panel click handler methods

		protected void SidePanelButton_Click(object sender, RoutedEventArgs eventArgs)
		{
			Button button = sender as Button;

			if (button == rightSidePanelbutton1)
				Draw.TextFixed(this, "infobox", "Right side panel > Button 1", TextPosition.BottomLeft, Brushes.Green, new SimpleFont("Arial", 25), Brushes.Transparent, Brushes.Transparent, 100);

			else if (button == rightSidePanelbutton2)
				Draw.TextFixed(this, "infobox", "Right side panel > Button 2", TextPosition.BottomLeft, Brushes.DarkRed, new SimpleFont("Arial", 25), Brushes.Transparent, Brushes.Transparent, 100);

			// refresh the chart so that the text box will appear on the next render pass even if there is no incoming data
			ForceRefresh();
		}
		#endregion

		#region Use case #4: Custom menu added to titlebar (NTBar) click handler methods

		protected void NTBarMenu_Click(object sender, RoutedEventArgs eventArgs)
		{
			MenuItem menuItem = sender as MenuItem;

			if (menuItem == ntBartopMenuItemSubItem1)
				Draw.TextFixed(this, "infobox", "Titlebar > NTBar Menu > Sub-MenuItem 1", TextPosition.BottomLeft, Brushes.Green, new SimpleFont("Arial", 25), Brushes.Transparent, Brushes.Transparent, 100);

			else if (menuItem == ntBartopMenuItemSubItem2)
				Draw.TextFixed(this, "infobox", "Titlebar > NTBar Menu > Sub-MenuItem 2", TextPosition.BottomLeft, Brushes.ForestGreen, new SimpleFont("Arial", 25), Brushes.Transparent, Brushes.Transparent, 100);

			ForceRefresh();
		}
		#endregion

		#region Use case #5: Custom chart trader buttons click handler methods

		protected void ChartTraderButtonMenu_Click(object sender, RoutedEventArgs eventArgs)
		{
			Button button = sender as Button;

			if (button == chartTraderCustomButtonsArray[0])
				Draw.TextFixed(this, "infobox", "Chart trader custom Button 1", TextPosition.BottomLeft, Brushes.Green, new SimpleFont("Arial", 25), Brushes.Transparent, Brushes.Transparent, 100);

			else if (button == chartTraderCustomButtonsArray[1])
				Draw.TextFixed(this, "infobox", "Chart trader custom Button 2", TextPosition.BottomLeft, Brushes.DarkRed, new SimpleFont("Arial", 25), Brushes.Transparent, Brushes.Transparent, 100);

			else if (button == chartTraderCustomButtonsArray[2])
				Draw.TextFixed(this, "infobox", "Chart trader custom Button 3", TextPosition.BottomLeft, Brushes.DarkOrange, new SimpleFont("Arial", 25), Brushes.Transparent, Brushes.Transparent, 100);

			else if (button == chartTraderCustomButtonsArray[3])
				Draw.TextFixed(this, "infobox", "Chart trader custom Button 4", TextPosition.BottomLeft, Brushes.CadetBlue, new SimpleFont("Arial", 25), Brushes.Transparent, Brushes.Transparent, 100);

			ForceRefresh();
		}
		#endregion

		#region Use case #6: Modify existing chart trader buttons click handler methods

		protected void ModifyCtButton_Click(object sender, RoutedEventArgs eventArgs)
		{
			Button button = sender as Button;

			if (button == modifyCtBuyMarketButton)
				Draw.TextFixed(this, "infobox", "Buy Market Button Clicked", TextPosition.BottomLeft, Brushes.Green, new SimpleFont("Arial", 25), Brushes.Transparent, ChartControl.Properties.ChartBackground, 100);

			else if (button == modifyCtSellMarketButton)
				Draw.TextFixed(this, "infobox", "Sell Market Button Clicked", TextPosition.BottomLeft, Brushes.Red, new SimpleFont("Arial", 25), Brushes.Transparent, ChartControl.Properties.ChartBackground, 100);

			ForceRefresh();
		}
		#endregion

		private bool TabSelected()
		{
			if (ChartControl == null || chartWindow == null || chartWindow.MainTabControl == null)
				return false;

			bool tabSelected = false;

			if (ChartControl.ChartTab == ((chartWindow.MainTabControl.Items.GetItemAt(chartWindow.MainTabControl.SelectedIndex) as TabItem).Content as ChartTab))
				tabSelected = true;

			return tabSelected;
		}

		// Runs ShowWPFControls if this is the selected chart tab, other wise runs HideWPFControls()
		private void TabChangedHandler(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count <= 0)
				return;

			tabItem = e.AddedItems[0] as TabItem;
			if (tabItem == null)
				return;

			chartTab = tabItem.Content as ChartTab;
			if (chartTab == null)
				return;

			if (TabSelected())
				ShowWPFControls();
			else
				HideWPFControls();
		}

		#region Use case #1: Custom drop-down menu class
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
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private SampleWPFModifications[] cacheSampleWPFModifications;
		public SampleWPFModifications SampleWPFModifications()
		{
			return SampleWPFModifications(Input);
		}

		public SampleWPFModifications SampleWPFModifications(ISeries<double> input)
		{
			if (cacheSampleWPFModifications != null)
				for (int idx = 0; idx < cacheSampleWPFModifications.Length; idx++)
					if (cacheSampleWPFModifications[idx] != null &&  cacheSampleWPFModifications[idx].EqualsInput(input))
						return cacheSampleWPFModifications[idx];
			return CacheIndicator<SampleWPFModifications>(new SampleWPFModifications(), input, ref cacheSampleWPFModifications);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.SampleWPFModifications SampleWPFModifications()
		{
			return indicator.SampleWPFModifications(Input);
		}

		public Indicators.SampleWPFModifications SampleWPFModifications(ISeries<double> input )
		{
			return indicator.SampleWPFModifications(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.SampleWPFModifications SampleWPFModifications()
		{
			return indicator.SampleWPFModifications(Input);
		}

		public Indicators.SampleWPFModifications SampleWPFModifications(ISeries<double> input )
		{
			return indicator.SampleWPFModifications(input);
		}
	}
}

#endregion
