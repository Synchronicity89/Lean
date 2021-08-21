using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Statistics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Algorithm.CSharp.S89
{
    public class Util
    {
        public static bool TimeBetween(DateTime datetime, TimeSpan start, TimeSpan end)
        {
            // convert datetime to a TimeSpan
            TimeSpan now = datetime.TimeOfDay;
            // see if start comes before end
            if (start < end)
                return start <= now && now <= end;
            // start is after end, so do the inverse comparison
            return !(end < now && now < start);
        }

        public static decimal MidPrice(OptionContract optionContract)
        {
            return (optionContract.AskPrice + optionContract.BidPrice) / 2;
        }

        public static TimeSpan timedelta(int days)
        {
            return TimeSpan.FromDays(days);
        }
    }

    public abstract class QCAlgorithmS89 : QCAlgorithm
    {
        private const int sever = 1; //Debug
        /// <summary>
        /// Log messages for debugging purpose
        /// </summary>
        /// <param name="message"></param>
        /// <param name="assert"></param>
        /// <param name="severity">0 info, 1 debug, 2 error</param>
        protected void LogCust(string message, Func<bool> assert = null, int severity = 0)
        {
            bool fail = true;
            if (assert != null && !assert())
            {
                fail = false;
            }
            if (fail && severity >= sever)
            {
                if (severity == 2) Error(message); else Debug(message);
            };
        }

        protected bool TimeIs(int day, int hour, int minute)
        {
            return this.Time.Day == day && this.Time.Hour == hour && this.Time.Minute == minute;
        }

        protected bool HourMinuteIs(int hour, int minute)
        {
            return this.Time.Hour == hour && this.Time.Minute == minute;
        }
    }

    public class OrderTicketInfo2
    {
        public Guid OcaId { get; set; }
        public Combo Combo { get; set; }
        public OrderTicket OrderTicket { get; set; }
        public DateTime TimeFilled { get; set; } = DateTime.MinValue;
        public decimal HighestPrice { get; set; }
        public decimal LowestPrice { get; set; }
        public OptionContract OptionContract { get; set; }
        public bool AssignedOption { get; set; }
        public Security EquityUnderlying { get; set; }
        public bool Canceled { get; set; }
    }

    public class Combo
    {
        public TradeDirection Direction { get; set; }

    }
}
