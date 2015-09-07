using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using WealthLab;
using WealthLab.Indicators;
using Community.Components;

namespace WealthLab.Strategies
{
	public class MyStrategy : WealthScript
	{
		//Pushed indicator StrategyParameter statements
		private StrategyParameter slider1;
		private StrategyParameter slider2;
		private StrategyParameter slider3;
		public MyStrategy()
		{
			//Pushed indicator CreateParameter statements
			slider1 = CreateParameter("EMA_Period_1",10,2,200,20);
			slider2 = CreateParameter("EMA_Period_2",30,2,200,20);
			slider3 = CreateParameter("WilliamsR_Period_3",3,2,200,20);

		}
		protected override void Execute()
		{
			SMA sma10 = SMA.Series(Close, 10);
			EMA ema30 = EMA.Series(Close, 30,EMACalculation.Modern);
			WilliamsR wlm = WilliamsR.Series(Bars, 3);
			
			BBandLower  LB = BBandLower.Series(Close,29, 2.5);
			BBandUpper  UB = BBandUpper.Series(Close,29,2.5);
			SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(10, Color.FromArgb(102,0,0,255)));
			PlotSeriesFillBand(PricePane, LB,UB,Color.FromArgb(255,176,196,222), shadowBrush,LineStyle.Solid,2);
			
			DateTime nextReportDate = new DateTime(2010, 1, 1);
			FundamentalItem fi = GetFundamentalItem (Bars.Count - 1, Bars.Symbol, "earnings per share");
			
			bool crossOver = false;
			bool pullback  = false;
			bool crossUnder = false;
			bool pullup  = false;

			
			for (int bar = 30 ; bar < Bars.Count; bar++)
			{
				SetContext( "SPY", true );

				bool  signal = sma10[bar] >  ema30[bar];
				bool  goLong = false;
				bool  goShort = false;
				if (signal && wlm[bar] < 20)
				{
					goLong = true;
				}
				if (!signal && wlm[bar] > 80)
				{
					crossOver = false;
					goShort = true;
				}
				
				RestoreContext();
			
				//goLong = true;
				if (!IsLastPositionActive)
				{
					if (goLong)
					{
					
						if ( CrossOver( bar, sma10, ema30))
						{
							crossOver = true;
							pullback = false;
							crossUnder = false;
							pullup = false;
							continue;
						}
				
						if (crossOver && Close[bar] < sma10[bar] && pullback == false)
						{
							pullback = true;
						}
					
						if (pullback  && Close[bar] > sma10[bar] )
						{
							//	DrawCircle( PricePane, 10, bar, High[bar], Color.Green, Color.DarkGreen, WealthLab.LineStyle.Solid, 2, true );
							//	crossOver = false;
							pullback = false;
							BuyAtMarket(bar+1, "SMA 10 is above EMA 30 and pull back is done ");		
						}
					}
					else
						if (goShort)
					{
						if ( CrossUnder( bar, sma10, ema30))
						{
							crossUnder = true;
							pullup = false;
							crossOver = false;
							pullback = false;
							continue;
						}
				
						if (crossUnder && Close[bar] > sma10[bar] && pullup == false)
						{
							pullup = true;
						}
					
						if (pullup  && Close[bar] < sma10[bar] )
						{
							//	DrawCircle( PricePane, 10, bar, High[bar], Color.Green, Color.DarkGreen, WealthLab.LineStyle.Solid, 2, true );
							//	crossOver = false;
							pullup = false;
							ShortAtMarket(bar+1, "SMA 10 is below EMA 30 and pull back is done ");		
						}						
						//	DrawCircle( PricePane, 10, bar, High[bar], Color.Blue, Color.Black, WealthLab.LineStyle.Solid, 2, true );
					}
				}
				else
				{
					if ( LastActivePosition.PositionType == PositionType.Long)
					{
						double b = LB[bar];
						if (
							LastActivePosition.EntryPrice > Close[bar] && Close[bar] < b
							&& bar >= LastActivePosition.EntryBar
							)
						{
							SellAtMarket(bar + 1, LastActivePosition, "stop lose");
							crossOver = false;
							pullback = false;
							continue;
						}
					
						if (Bars.IntradayBarNumber(bar) == 0 && EarningsDate.InWindow(this, bar, "earnings per share", 365, 0))
						{
							//DrawLabel(PricePane, "Next Earnings: " + Date[bar].ToString());
							PrintDebug(Date[bar].ToString());
						}
						
						if (  Date[bar].Day >  (LastActivePosition.EntryDate.Day + 4) &&  Bars.IsLastBarOfDay(bar) ) 
						{
							SellAtMarket(bar + 1, LastActivePosition, "Close on 5th day");
							crossOver = false;
							pullback = false;
							continue;
						}
						else
						
							if (CrossUnder( bar, sma10, ema30))
						{
							SellAtMarket(bar + 1, LastActivePosition, "Take profit");
							crossOver = false;
							pullback = false;
							continue;
						}
					}
					else if  ( LastActivePosition.PositionType == PositionType.Short)
					{
						double b = UB[bar];
						if (
							LastActivePosition.EntryPrice < Close[bar] && Close[bar] > b
							&& bar >= LastActivePosition.EntryBar
							)
						{
							CoverAtMarket(bar + 1, LastActivePosition, "Short stop lose");
							crossUnder = false;
							pullup = false;
							continue;
						}
					

						if (  Date[bar].Day >  (LastActivePosition.EntryDate.Day + 4) &&  Bars.IsLastBarOfDay(bar) ) 
						{
							CoverAtMarket(bar + 1, LastActivePosition, "Close on 5th day");
							crossUnder = false;
							pullup = false;
							continue;
						}
						else
						
							if (CrossOver( bar, sma10, ema30))
						{
							CoverAtMarket(bar + 1, LastActivePosition, "Take profit");
							crossUnder = false;
							pullup = false;
							continue;
						}

					}
				}
			}
			//Pushed indicator ChartPane statements
			ChartPane paneWilliamsR1 = CreatePane(40,true,true);

			//Pushed indicator PlotSeries statements
			PlotSeries(PricePane,sma10,Color.DarkGreen,LineStyle.Solid,2);
			PlotSeries(PricePane,ema30,Color.Blue,LineStyle.Solid,2);
			PlotSeries(paneWilliamsR1,wlm,Color.OliveDrab,LineStyle.Solid,2);

		}
	}
}