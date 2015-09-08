using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using WealthLab;
using WealthLab.Indicators;

namespace WealthLab.Strategies
{
	public class Doroob : WealthScript
	{
	
		//Create parameters


		bool signleToSell = false;
		double priceAtCross = 0;
		bool firstUp = false;
		bool firstDown = false;
		double priceAtCrossDown = 0;
		double rocAtCrossDown = 0;

		double  topPrice = 0;
		double  lastTop = 0;
		double  breakPrice = 0;
		double  rocPrice = 0;
		
		
		bool trend       = true;  // true bullish up trend
		int trendRating  = 0;  //  0 nutral, 1 bulish, 2 strong bulish.    if trend bearish then 1 bearish, 2 strong brerish
		
		//  should we try to init postion before the market goes into overslod?
		bool initPosition = true;
		
		
		//DateTime  syncDate = new DateTime(2013, 12, 20, 12, 30 , 0);
		

		StrategyParameter rocPeriod;
		StrategyParameter rsiPeriod;
		StrategyParameter oversold;
		StrategyParameter overbought;
		StrategyParameter stopLose;
		StrategyParameter priceRise;
		
		public Doroob()
		{
			rocPeriod = CreateParameter("ROC_Period", 20, 20, 20, -1);
			rsiPeriod = CreateParameter("RSI Period", 29, 29, 29, -1);
			oversold = CreateParameter("Oversold", 35, 30, 44, 1);
			overbought = CreateParameter("Overbought",66, 70, 90, 2);
			stopLose = CreateParameter("StopLose", 2, 1, 10, 1);
			priceRise = CreateParameter("PriceRise", 3, 1, 6, 1);
		}

		
		protected override void Execute()
		{
			int level;
			
			signleToSell = false;
			priceAtCross = 0;
			firstUp = false;
			firstDown = false;
			priceAtCrossDown = 0;
			rocAtCrossDown = 0;

			topPrice = 0;
			lastTop = 0;
			breakPrice = 0;
			rocPrice = 0;
			
			//Create and plot  period RSI
			RSI rsi20 = RSI.Series(Close, rsiPeriod.ValueInt);
			ChartPane rsiPane = CreatePane(50, true, true);

			PlotSeries(rsiPane, rsi20, Color.Navy, LineStyle.Solid, 3);
			DrawHorzLine(rsiPane, oversold.Value, Color.Red, LineStyle.Dotted, 2);
			DrawHorzLine(rsiPane, overbought.Value, Color.Green, LineStyle.Dotted, 2);


			ChartPane paneROC1 = CreatePane(40, true, true);
			PlotSeries(paneROC1, ROC.Series(Close, rocPeriod.ValueInt), Color.FromArgb(255, 112, 128, 144), LineStyle.Dotted, 2);
			DrawHorzLine(paneROC1, 0, Color.Red, LineStyle.Solid, 2);

			int period = Math.Max(rsiPeriod.ValueInt, rocPeriod.ValueInt);
			

			if (initPosition)
			{
				// init starting state to force buy into the market
				priceAtCrossDown = Close[period + 1];
				rocAtCrossDown = ROC.Value(period + 1, Close, rocPeriod.ValueInt);
				firstDown = true;
			}

			double High60 = 0;
			double Low60 = 0;
			int n = Bars.BarInterval;
			int num = ( 60 / n ) - 1; // the first hour bar
			int firstIntradayBar = -1;
			
			
			//Trading system main loop
			for (int bar = period + 1; bar < Bars.Count; bar++)
			{
				if( Bars.IntradayBarNumber( bar ) == 0 )
					firstIntradayBar = bar;
						
				if( Bars.IntradayBarNumber( bar ) <= num ) // highlight the first hour
					SetBarColor( bar, Color.Silver );
					
				if( Bars.IntradayBarNumber( bar ) == num ) // get the highest high and the lowest low after first hour
				{
					High60 = Highest.Series( Bars.High, num+1 )[bar];
					Low60 = Lowest.Series( Bars.Low, num+1 )[bar];
						
					if( firstIntradayBar > -1 )
					{
						DrawLine( PricePane, bar, High60, firstIntradayBar, High60, Color.Blue, LineStyle.Dashed, 1 ); 
						DrawLine( PricePane, bar, Low60, firstIntradayBar, Low60, Color.Red, LineStyle.Dashed, 1 ); 
					}
				}				
				
				if ( Close[bar] > topPrice)
				{
					topPrice = Close[bar];
				}
				
				level = oversold.ValueInt;

				if (CrossUnder(bar, rsi20, level))
				{

					if (!firstDown)
					{
						// first time to go below
						// set cross happen and record the targit price
						firstDown = true;
						priceAtCrossDown = Close[bar];
						rocAtCrossDown = ROC.Value(bar, Close, rocPeriod.ValueInt);
					}
				}

				// play trayling 
				int addToPosition = 0;
				bool rsiOk = false;
				bool priceOK = false;
				if (firstDown)
				{
					// ROC must improve by delta
					double delta = Math.Abs(rocAtCrossDown * 0.2);

					double newrocSMA = ROC.Value(bar, Close, rocPeriod.ValueInt);

					
					if (bar == Bars.Count - 1) {
						rocPrice = rocAtCrossDown + delta;
					}
					
					if (newrocSMA <= (rocAtCrossDown + delta))
					{

						if (newrocSMA < rocAtCrossDown)
						{
							rocAtCrossDown = newrocSMA;
						}
					}
					else
					{
						rsiOk = true;
					}

					double riseV = priceRise.ValueInt;
					delta = priceAtCrossDown * (riseV /1000.0 + 0.00017);

					if (bar == Bars.Count - 1) {
						breakPrice = priceAtCrossDown + delta;
					}
					
					if (Close[bar] < (priceAtCrossDown + delta))
					{
						// DrawLabel(PricePane, "ready to buy, price is not rising: " + Close[bar].ToString() + " less than " + (priceAtCrossDown + delta).ToString());

						if (Close[bar] < priceAtCrossDown)
						{
							priceAtCrossDown = Close[bar];
						}
					}
					else
					{
						priceOK = true;
					}

					if (priceOK && rsiOk)
					{
						addToPosition++;
						firstDown = false;
					}
				}

				// you can  have only one active position
				foreach (Position pos in Positions)
				{
					if (pos.Active && pos.PositionType == PositionType.Long)
					{
						addToPosition = 0;
						break;
					}
				}

				if (addToPosition > 0)
				{
					// OverSold
					// Close all shorts
					foreach (Position pos in Positions)
					{
						if (pos.Active && pos.PositionType == PositionType.Short)
						{
							CoverAtMarket(bar + 1, pos);
						}
					}

					//	    DrawLabel(PricePane, "buy at bar = " + bar.ToString());
					Position p = BuyAtMarket(bar + 1);
					firstUp = false;
				}


				level = overbought.ValueInt;
				int ClosedTrades = 0;
				signleToSell = false;
				if (CrossOver(bar, rsi20, overbought.ValueInt))
				{
					if (!firstUp)
					{
						// first time to go above
						// set cross happen and record the targit price
						firstUp = true;
						priceAtCross = Close[bar];
					}
				}

				if (firstUp)
				{
					double riseV = priceRise.ValueInt;
			
					double delta = priceAtCross * (riseV /1000 + 0.00017);

					priceOK = true;
					if (Close[bar] >= (priceAtCross - delta))
					{

						if (Close[bar] > priceAtCross)
						{
							priceAtCross = Close[bar];
						}
					}
					else
					{
						priceOK = false;
					}		
					
					// keep as long ROC over zero
					if (ROC.Value(bar, Close, rocPeriod.ValueInt) <= 0 && !priceOK)
					{
						signleToSell = true;
						firstUp = false;
					}
				}

				// sync real account with strategy 
				//			if (Bars.Date[bar] == syncDate)
				//			{
				//				signleToSell = true;  // close postions
				//			}
				
				// Over Bought
				// wait until price either move up or stopped out
				if (signleToSell)
				{
					firstUp = false;
					//DrawLabel(PricePane, ActivePositions.Count.ToString());
					foreach (Position pos in Positions)
					{
						if (pos.Active && pos.PositionType == PositionType.Long)
						{
							SellAtMarket(bar + 1, pos);
							ClosedTrades++;
						}
					}
					signleToSell = false;

					if (ClosedTrades > 0)
					{
						// Short only after sell long position
						if ( !trend)
						{
							ShortAtMarket(bar + 1);
						}
					}

				}
				// sell on % lose
				foreach (Position pos in Positions)
				{
					if (pos.Active &&
						pos.PositionType == PositionType.Long &&
						pos.EntryPrice > (Close[bar] + pos.EntryPrice * (0.01 + stopLose.ValueInt * 0.001))
						&& bar >= pos.EntryBar
						)
					{
						SellAtMarket(bar + 1, pos, "stop lose");
						signleToSell = false;
						firstUp = false;
						firstDown = false;
						continue;
					}
					if (pos.Active &&
						pos.PositionType == PositionType.Short &&
						Close[bar] > (pos.EntryPrice + pos.EntryPrice * (0.01 + stopLose.ValueInt * 0.001))
						&& bar >= pos.EntryBar
						)
					{
						CoverAtMarket(bar + 1, pos, "stop lose Short");
					}
				}
			}
			double currentPrice = Close[Bars.Count-1];
			DrawLabel(paneROC1, "Top: " + topPrice.ToString(), Color.Red);
			DrawLabel(paneROC1, "Current Price: " + currentPrice.ToString());		
			
			DrawLabel(paneROC1, "Goal : " + " At +5%  " +(currentPrice*1.05).ToString(), Color.BlueViolet);
			DrawLabel(paneROC1, "Goal : " + " At +8%  " +(currentPrice*1.08).ToString(), Color.MediumSpringGreen);
			DrawLabel(paneROC1, "Goal : " + " At +10% " +(currentPrice*1.10).ToString(), Color.DarkOliveGreen);

			
			DrawLabel(PricePane, "Drop          : " + " At -5%  " +(topPrice*0.95).ToString(), Color.DarkGreen);
			DrawLabel(PricePane, "Correction   : " + " At -8%  " +(topPrice*0.92).ToString(), Color.DarkBlue);
			DrawLabel(PricePane, "Correction   : " + " At -10% " +(topPrice*0.90).ToString(), Color.Red);
			DrawLabel(PricePane, "Bear market: " + " At -20% " +(topPrice*0.80).ToString(), Color.DarkRed);

			if (breakPrice > 0)
			{
				DrawLabel(PricePane, "Break above Price: " + breakPrice.ToString(), Color.DarkGoldenrod);
			}
			if (rocPrice > 0)
			{
				DrawLabel(PricePane, "Break above Momuntem: " +rocPrice.ToString(), Color.DarkGoldenrod);
			}

			
			DrawHorzLine(PricePane, currentPrice*1.05, Color.BlueViolet, LineStyle.Solid, 6);
			DrawHorzLine(PricePane, currentPrice*1.08, Color.MediumSpringGreen, LineStyle.Solid, 3);
			DrawHorzLine(PricePane, currentPrice*1.10, Color.DarkOliveGreen, LineStyle.Solid, 3);
			
			
			DrawHorzLine(PricePane, topPrice*0.95, Color.DarkGreen, LineStyle.Solid, 3);
			DrawHorzLine(PricePane, topPrice*0.92, Color.DarkBlue, LineStyle.Solid, 3);
			DrawHorzLine(PricePane, topPrice*0.90, Color.Red, LineStyle.Solid, 3);
			DrawHorzLine(PricePane, topPrice*0.80, Color.DarkRed, LineStyle.Solid, 3);
			
			
			if (breakPrice > 0)
			{
				DrawHorzLine(PricePane, breakPrice, Color.DarkGoldenrod, LineStyle.Solid, 4);				
			}
		}
	}
}