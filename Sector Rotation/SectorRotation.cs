using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using WealthLab;
using WealthLab.Indicators;
using System.Collections;
using System.IO;

namespace WealthLab.Strategies
{
	public class MyStrategy : WealthScript
	{
		protected override void Execute()
		{
			int days = 250;
			int timeFrame = 66; // 3 months	
			int month = -1;
			
			SMA smaYear = SMA.Series(Close, days);
			PlotSeries(PricePane, smaYear, Color.Red, WealthLab.LineStyle.Solid, 2);
			
			DrawLine(PricePane, 7, Close[7], 35, Close[35], Color.Red, LineStyle.Solid, 3);
			DrawLine(PricePane, 36, Close[36], 66, Close[66], Color.Green, LineStyle.Solid, 3);
			DrawLine(PricePane, 67, Close[67], 97, Close[97], Color.Red, LineStyle.Solid, 3);
			DrawLine(PricePane, 98, Close[98], 128, Close[128], Color.Green, LineStyle.Solid, 3);
			DrawLine(PricePane, 129, Close[129], 159, Close[159], Color.Red, LineStyle.Solid, 3);
			DrawLine(PricePane, 160, Close[160], 190, Close[190], Color.Green, LineStyle.Solid, 3);
			DrawLine(PricePane, 191, Close[191], 221, Close[221], Color.Red, LineStyle.Solid, 3);
			DrawLine(PricePane, 222, Close[222], 249, Close[249], Color.Green, LineStyle.Solid, 3);
			
			double duringEarning = (Close[35] - Close[7]) / Close[7] + (Close[97] - Close[66]) / Close[66] + (Close[159] - Close[129]) / Close[129] 
				+ (Close[221] - Close[191]) / Close[191];
			duringEarning = 100 * duringEarning;
			
			double notEarning = (Close[66] - Close[36]) / Close[36] + (Close[128] - Close[98]) / Close[98] + (Close[190] - Close[160]) / Close[160] + (Close[249] - Close[222]) / Close[222];
			notEarning = 100 * notEarning;
				
			DrawLabel(PricePane, "During Earnings " + duringEarning.ToString());
			DrawLabel(PricePane, "Not Earnings " + notEarning.ToString());
			DrawLabel(PricePane, "All year " + (100 * (Close[249] - Close[1]) / Close[1]).ToString());
			
			
			for(int bar = timeFrame; bar < Bars.Count; bar++) {
				if(bar >days && Close[bar] < smaYear[bar]) {
					// Exit all positions
					foreach(Position p in Positions) {
						if (p.Active) {
							SetContext(p.Symbol , true);

							SellAtMarket( bar+1, p, "S&P is below sma");
							RestoreContext();

						}
					}
					continue;
				}
				
				if(Date[bar].Month != month) {
					month =  Date[bar].Month;
					List<double> percentageGained3M = new List<double>();
					int [] order = new int[DataSetSymbols.Count];
					
					for(int ds = 0; ds < DataSetSymbols.Count; ds++)
					{
						if(DataSetSymbols[ds]!= "SPY")
						{
			
							SetContext(DataSetSymbols[ds], true);
							order[ds] = ds;
							double percentageGained = (100 * (Close[bar] - Close[bar - timeFrame])) / Close[bar];
							percentageGained3M.Add(percentageGained);
							RestoreContext();
						}
					}
					
					//Sort list and retain order for dataseries
					for(int i = 1; i < percentageGained3M.Count; i++)
					{
						int j = i;
						while(j > 0)
						{
							if(percentageGained3M[j-1] < percentageGained3M[j])
							{
								double temp = percentageGained3M[j - 1];
								int temp2 = order[j - 1];
								percentageGained3M[j - 1] = percentageGained3M[j];
								order[j - 1] = order[j];
								percentageGained3M[j] = temp;
								order[j] = temp2;
								j--;
							}
							else
								break;
						}
					}
					
					// Re-balance sectors every month
					string sector1 = DataSetSymbols[order[0]];
					string sector2 = DataSetSymbols[order[1]];
					string sector3 = DataSetSymbols[order[2]];
					bool found1 = false;
					bool found2 = false;
					bool found3 = false;
					
					PrintDebug("In " + sector1 );
					PrintDebug("In " + sector2);
					PrintDebug("In " + sector3);
					List<Position> positionsToClose = new List<Position>();
					
					foreach(Position p in Positions) {
						if (p.Active)
						{
							if(p.Symbol == sector1) {
								found1 = true;
							} else if(p.Symbol == sector2) {
								found2 = true;
							} else if(p.Symbol == sector3) {
								found3 = true;
							}
						
							if(p.Symbol != sector1 && p.Symbol != sector2 && p.Symbol != sector3) {
								PrintDebug("Out " + p.Symbol);
								positionsToClose.Add(p);
							}
						}
					}
					
					foreach(Position p in positionsToClose) {
						SetContext(p.Symbol, true);
						SellAtMarket(bar + 1, p, "Sector is no longer outperforming other sectors.");
						RestoreContext();
					}
					
					if(!found1) {
						SetContext(sector1, true);
						BuyAtMarket(bar + 1, "Top Performing Sector");
						RestoreContext();
					}
					
					if(!found2) {
						SetContext(sector2, true);
						BuyAtMarket(bar + 1, "Top Performing Sector");
						RestoreContext();
					}
					
					if(!found3) {
						SetContext(sector3, true);
						BuyAtMarket(bar + 1, "Top Performing Sector");
						RestoreContext();
					}
					positionsToClose.Clear();
					percentageGained3M.Clear();
				}
			
			}
		}
	}
}

//					for(int i = 0; i < percentageGained3M.Count; i++) {
//						PrintDebug(order[i]);
//						PrintDebug(percentageGained3M[i]);
//					}