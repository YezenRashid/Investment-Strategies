using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using WealthLab;
using WealthLab.Indicators;

namespace WealthLab.Strategies {
	public class Doroob: WealthScript {
		StrategyParameter line;
		StrategyParameter minDays;
		
		public Doroob()
		{
			line = CreateParameter("Line", 27, 20, 50, 1);
			minDays = CreateParameter("Min Days", 2, 2, 40, 1);
		}

		protected override void Execute()
		{
			DateTime startDate = Date[0];
			int consequetiveDays = 0;
			bool started = false;
			DrawLabel(PricePane,"Here is a list of dates where VIX closed above: " + line.Value + " for more than: " + minDays.Value + " days"); 
			for (int bar = 0; bar < Bars.Count; bar++)
			{	
				if(Close[bar] > line.Value)
				{
					consequetiveDays++;
					if(!started)
					{
						startDate = Date[bar];
						started = true;
					}
				}
				if(started && (Close[bar] < line.Value || bar == Bars.Count - 1))
				{
					if(consequetiveDays > minDays.Value)
					{
						DrawLabel(PricePane,"Number of days: " + consequetiveDays + "\t Start: " + startDate.ToShortDateString() + 	"\t End: " + Date[bar].ToShortDateString() + "\n");
					}					
					started = false;
					consequetiveDays = 0;
				}				
			}
		}
	}
}