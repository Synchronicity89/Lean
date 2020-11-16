from clr import AddReference
import pandas
AddReference("System")
AddReference("QuantConnect.Research")
AddReference("QuantConnect.Common")
AddReference("QuantConnect.Logging")
#AddReference("QuantConnect.Data")

from System import *
from QuantConnect import *
from QuantConnect.Logging import *
#from Data import *
#from QuantConnect.Data import *
from QuantConnect.Research import *
from datetime import datetime, timedelta
from custom_data import QuandlFuture, Nifty
import pandas as pd


#from System import *
#from QuantConnect import *
#from QuantConnect.Data import SubscriptionDataSource
from QuantConnect.Python import PythonData, PythonQuandl
from datetime import datetime
import decimal

class QuandlFuture(PythonQuandl):
    '''Custom quandl data type for setting customized value column name. Value column is used for the primary trading calculations and charting.'''
    def __init__(self):
        # Define ValueColumnName: cannot be None, Empty or non-existant column name
        # If ValueColumnName is "Close", do not use PythonQuandl, use Quandl:
        # self.AddData[QuandlFuture](self.crude, Resolution.Daily)
        self.ValueColumnName = "Settle"


class Nifty(PythonData):
    '''NIFTY Custom Data Class'''
    def GetSource(self, config, date, isLiveMode):
        return SubscriptionDataSource("https://www.dropbox.com/s/rsmg44jr6wexn2h/CNXNIFTY.csv?dl=1", SubscriptionTransportMedium.RemoteFile);


    def Reader(self, config, line, date, isLiveMode):
        if not (line.strip() and line[0].isdigit()): return None

        # New Nifty object
        index = Nifty();
        index.Symbol = config.Symbol

        try:
            # Example File Format:
            # Date,       Open       High        Low       Close     Volume      Turnover
            # 2011-09-13  7792.9    7799.9     7722.65    7748.7    116534670    6107.78
            data = line.split(',')
            index.Time = datetime.strptime(data[0], "%Y-%m-%d")
            index.Value = decimal.Decimal(data[4])
            index["Open"] = float(data[1])
            index["High"] = float(data[2])
            index["Low"] = float(data[3])
            index["Close"] = float(data[4])


        except ValueError:
                # Do nothing
                return None

        return index

class SecurityHistoryTest():
    def __init__(self, start_date, security_type, symbol):
        self.qb = QuantBook()
        self.qb.SetStartDate(start_date)
        self.symbol = self.qb.AddSecurity(security_type, symbol).Symbol
        self.column = 'close'

    def __str__(self):
        return "{} on {}".format(self.symbol.ID, self.qb.StartDate)

    def test_period_overload(self, period):
        history = self.qb.History([self.symbol], period)
        return history[self.column].unstack(level=0)

    def test_daterange_overload(self, end):
        start = end - timedelta(1)
        history = self.qb.History([self.symbol], start, end)
        return history[self.column].unstack(level=0)

class OptionHistoryTest(SecurityHistoryTest):
    def test_daterange_overload(self, end, start = None):
        if start is None:
            start = end - timedelta(1)
        history = self.qb.GetOptionHistory(self.symbol, start, end)
        return history.GetAllData()

class FutureHistoryTest(SecurityHistoryTest):
    def test_daterange_overload(self, end, start = None, maxFilter = 182):
        if start is None:
            start = end - timedelta(1)
        self.qb.Securities[self.symbol].SetFilter(0, maxFilter) # default is 35 days
        history = self.qb.GetFutureHistory(self.symbol, start, end)
        return history.GetAllData()

class FutureContractHistoryTest():
    def __init__(self, start_date, security_type, symbol):
        self.qb = QuantBook()
        self.qb.SetStartDate(start_date)
        self.symbol = symbol
        self.column = 'close'

    def test_daterange_overload(self, end):
        start = end - timedelta(1)
        history = self.qb.GetFutureHistory(self.symbol, start, end)
        return history.GetAllData()

class OptionContractHistoryTest(FutureContractHistoryTest):
    def test_daterange_overload(self, end):
        start = end - timedelta(1)
        history = self.qb.GetOptionHistory(self.symbol, start, end)
        return history.GetAllData()

class CustomDataHistoryTest(SecurityHistoryTest):
    def __init__(self, start_date, security_type, symbol):
        self.qb = QuantBook()
        self.qb.SetStartDate(start_date)

        if security_type == 'Nifty':
            type = Nifty
            self.column = 'close'
        elif security_type == 'QuandlFuture':
            type = QuandlFuture
            self.column = 'settle'
        else:
            raise

        self.symbol = self.qb.AddData(type, symbol, Resolution.Daily).Symbol

class MultipleSecuritiesHistoryTest(SecurityHistoryTest):
    def __init__(self, start_date, security_type, symbol):
        self.qb = QuantBook()
        self.qb.SetStartDate(start_date)
        self.qb.AddEquity('SPY', Resolution.Daily)
        self.qb.AddForex('EURUSD', Resolution.Daily)
        self.qb.AddCrypto('BTCUSD', Resolution.Daily)

    def test_period_overload(self, period):
        history = self.qb.History(self.qb.Securities.Keys, period)
        return history['close'].unstack(level=0)

class FundamentalHistoryTest():
    def __init__(self):
        self.qb = QuantBook()

    def getFundamentals(self, ticker, selector, start, end):
        return self.qb.GetFundamental(ticker, selector, start, end)


startDate = datetime(2014, 5, 9)
a = CompositeLogHandler()
securityTestHistory = MultipleSecuritiesHistoryTest(startDate, None, None)

#// Get the last 5 candles
periodHistory = securityTestHistory.test_period_overload(5)

#// Note there is no data for BTCUSD at 2014

#//symbol                 EURUSD         SPY
#//time
#//2014-05-03 00:00:00        NaN        173.580655
#//2014-05-04 20:00:00   1.387185               NaN
#//2014-05-05 20:00:00   1.387480               NaN
#//2014-05-06 00:00:00        NaN        173.903690
#//2014-05-06 20:00:00   1.392925               NaN
#//2014-05-07 00:00:00        NaN        172.426958
#//2014-05-07 20:00:00   1.391070               NaN
#//2014-05-08 00:00:00        NaN        173.423752
#//2014-05-08 20:00:00   1.384265               NaN
#//2014-05-09 00:00:00        NaN        173.229931
Console.WriteLine(periodHistory)

count = periodHistory.shape[0]
Assert.AreEqual(10, count)

#// Get the one day of data
timedeltaHistory = securityTestHistory.test_period_overload(TimeSpan.FromDays(8));
firstIndex = timedeltaHistory.index.values[0]

#// EURUSD exchange time zone is NY but data is UTC so we have a 4 hour difference with algo TZ which is NY
Assert.AreEqual(datetime(startDate.years, startDate.days - 8, startDate.hours + 20), firstIndex);