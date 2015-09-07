using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Expiration_Roll_Yield
{
    class Program
    {
        static void Main(string[] args)
        {
            DateTime currentDate = new DateTime(2015, 1, 22);
            DateTime nextDate = firstDayNextMonth(currentDate);

            DateTime experation = expirationDate(currentDate);
            DateTime nextExperation = expirationDate(nextDate);

            //int firstMonthDays = weekDays(experation, currentDate);
            //int secondMonthDays = weekDays(currentDate, nextExperation);

            //Console.WriteLine(firstMonthDays);
            //Console.WriteLine(secondMonthDays);
            Console.WriteLine(experation.ToLongDateString());
            Console.WriteLine(nextExperation.ToLongDateString());
            Console.Read();
        }

        static DateTime expirationDate(DateTime d) {
            DateTime nextMonth = firstDayNextMonth(d);
            int fridayCount = 0;

            while (fridayCount != 3) {
                if (nextMonth.DayOfWeek == DayOfWeek.Friday) {
                    fridayCount++;
                }
                
                if(fridayCount == 3) {
                    break;
                }

                nextMonth = nextMonth.AddDays(1);
            }

            nextMonth = nextMonth.Subtract(new TimeSpan(30, 0, 0, 0));
            return nextMonth;
        }

        static DateTime firstDayNextMonth(DateTime d) {
            DateTime nextMonth;

            if (d.Month == 12) {
                nextMonth = new DateTime(d.Year + 1, 1, 1);
            } else {
                nextMonth = new DateTime(d.Year, d.Month + 1, 1);
            }

            return nextMonth;
        }

        static int weekDays(DateTime experation, DateTime nextExperation) {
            int days = 0;
            while (experation != nextExperation) {
                if(experation.DayOfWeek != DayOfWeek.Saturday && experation.DayOfWeek != DayOfWeek.Sunday) {
                    days++;
                }
                experation = experation.AddDays(1);
            }

            return days;
        }
    }
}
