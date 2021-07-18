using System;


//
//	Make sure to change "BasicTemplateAlgorithm" to your algorithm class name, and that all
//	files use "public partial class" if you want to split up your algorithm namespace into multiple files.
//

//public partial class BasicTemplateAlgorithm : QCAlgorithm, IAlgorithm
//{
//  Extension functions can go here...(ones that need access to QCAlgorithm functions e.g. Debug, Log etc.)
//}

//public class Indicator 
//{
//  ...or you can define whole new classes independent of the QuantConnect Context
//}
namespace QuantConnect.Algorithm.CSharp
{

    using timedelta = System.TimeSpan;

    using System.Collections.Generic;
    using QuantConnect.Securities;

    public class MyCalendar
        //: AbstractHolidayCalendar
        : TradingCalendar
    {
        public MyCalendar(SecurityManager securityManager, MarketHoursDatabase marketHoursDatabase) : base(securityManager, marketHoursDatabase)
        {
        }

        //public object rules = new List<object> {
        //        Holiday("NewYearsDay", month: 1, day: 1, observance: nearest_workday),
        //        USMartinLutherKingJr,
        //        USPresidentsDay,
        //        GoodFriday,
        //        USMemorialDay,
        //        Holiday("USIndependenceDay", month: 7, day: 4, observance: nearest_workday),
        //        USLaborDay,
        //        USThanksgivingDay,
        //        Holiday("Christmas", month: 12, day: 25, observance: nearest_workday)
        //    };

        // ------------------------------------------------------------------------------
        // Business days
        // ------------------------------------------------------------------------------
        //<parser-error>
        //<parser-error>
        // TODO: to be tested
        public static object last_trading_day(object expiry)
        {
            //// American options cease trading on the third Friday, at the close of business 
            //// - Weekly options expire the same day as their last trading day, which will usually be a Friday (PM-settled), [or Mondays? & Wednesdays?]
            //// 
            //// SPX cash index options (and other cash index options) expire on the Saturday following the third Friday of the expiration month. 
            //// However, the last trading day is the Thursday before that third Friday. Settlement price Friday morning opening (AM-settled).
            //// http://www.daytradingbias.com/?p=84847
            //var dd = expiry;
            //// if expiry on a Saturday (standard options), then last trading day is 1d earlier 
            //if (dd.weekday() == 5)
            //{
            //    dd -= timedelta(days: 1);
            //}
            //// check that Friday is not an holiday (e.g. Good Friday) and loop back
            //while (USTradingCalendar().holidays(dd, dd).tolist())
            //{
            //    // if list empty (dd is not an holiday) -> False
            //    dd -= timedelta(days: 1);
            //}
            //return dd;
            return null;
        }
    }
}

