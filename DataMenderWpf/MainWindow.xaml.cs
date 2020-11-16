using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DataFrameEtc;

namespace DataMenderWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {

        }
        List<DirectoryInfo> delets = new List<DirectoryInfo>();

        private void RecurseFolder(DirectoryInfo dir, List<FileInfo> files)
        {
            var s = dir.GetFileSystemInfos();
            txtOutput.Text += dir.FullName + ": " + s.Length + Environment.NewLine;
            DateTime prev = DateTime.MinValue;
            if (dir.Name.Length == 3 && dir.Parent.Name == "minute" && dir.Parent.Parent.Name == "usa" && s.Length == 0)
            {
                delets.Add(dir);
            }

            foreach (var sysInfo in s.OrderBy(n => n.Name))
            {
                //txtOutput.Text += sysInfo.FullName + Environment.NewLine;
                if (sysInfo is FileInfo)
                {
                    if (dir.Name.Length == 3 && dir.Parent.Name == "minute" && dir.Parent.Parent.Name == "usa")
                    {
                        var n = sysInfo.Name.Substring(0, 8);
                        DateTime dt = new DateTime(int.Parse(n.Substring(0, 4)), int.Parse(n.Substring(4, 2)), int.Parse(n.Substring(6, 2)));
                        //files.Add(sysInfo as FileInfo);
                        if (dt - prev > TimeSpan.FromDays(4))
                        {
                            txtOutput.Text += dt.ToString() + " - " + prev.ToString() + " = " + (dt - prev).ToString() + Environment.NewLine;
                        }
                        prev = dt;
                    }
                }
                else
                {
                    RecurseFolder(sysInfo as DirectoryInfo, files);
                }
            }
        }

        private void btnOrdersVsPositions_Click(object sender, RoutedEventArgs e)
        {
            Int32 accountHint = int.Parse(cboAccountHint.SelectedValue.ToString().Substring(6));
            string direction = cboAccountHint.SelectedValue.ToString().Substring(0, 5);
            var positionsDir = new DirectoryInfo("..\\..\\..\\..\\..\\");
            var csvs = positionsDir.GetFiles("all*.csv");
            Comparer comparer = new Comparer();
            var t = comparer.Results(csvs, accountHint, direction);
            //var r = ordersdf.Filter(new PrimitiveDataFrameColumn<bool>("Account").Contains(52));
            t.ForEach(te => txtOutput.Text += te + Environment.NewLine);
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            DirectoryInfo dataDir = new DirectoryInfo("..\\..\\..\\Data");
            List<FileInfo> files = new List<FileInfo>();
            RecurseFolder(dataDir, files);
            foreach (var dd in delets)
            {
                txtOutput.Text += "rmdir " + dd.FullName + Environment.NewLine;
            }
        }

    }
}
//['account', 'action', 'activeStartTime', 'activeStopTime', 'adjustableTrailingUnit', 'adjustedOrderType', 'adjustedStopLimitPrice', 'adjustedStopPrice', 'adjustedTrailingAmount', 'algoId', 
//'algoParams', 'algoStrategy', 'allOrNone', 'auctionStrategy', 'autoCancelDate', 'autoCancelParent', 'auxPrice', 'basisPoints', 'basisPointsType', 'blockOrder', 'cashQty', 
//'clearingAccount', 'clearingIntent', 'clientId', 'conditions', 'conditionsCancelOrder', 'conditionsIgnoreRth', 'continuousUpdate', 'delta', 'deltaNeutralAuxPrice', 
//'deltaNeutralClearingAccount', 'deltaNeutralClearingIntent', 'deltaNeutralConId', 'deltaNeutralDesignatedLocation', 'deltaNeutralOpenClose', 'deltaNeutralOrderType', 
//'deltaNeutralSettlingFirm', 'deltaNeutralShortSale', 'deltaNeutralShortSaleSlot', 'designatedLocation', 'discretionaryAmt', 'discretionaryUpToLimitPrice', 'displaySize', 
//'dontUseAutoPriceForHedge', 'eTradeOnly', 'exemptCode', 'extOperator', 'faGroup', 'faMethod', 'faPercentage', 'faProfile', 'filledQuantity', 'firmQuoteOnly', 'goodAfterTime', 
//'goodTillDate', 'hedgeParam', 'hedgeType', 'hidden', 'imbalanceOnly', 'isOmsContainer', 'isPeggedChangeAmountDecrease', 'lmtPrice', 'lmtPriceOffset', 'mifid2DecisionAlgo', 
//'mifid2DecisionMaker', 'mifid2ExecutionAlgo', 'mifid2ExecutionTrader', 'minQty', 'modelCode', 'nbboPriceCap', 'notHeld', 'ocaGroup', 'ocaType', 'openClose', 
//'optOutSmartRouting', 'orderComboLegs', 'orderId', 'orderMiscOptions', 'orderRef', 'orderType', 'origin', 'outsideRth', 'overridePercentageConstraints', 'parentId', 
//'parentPermId', 'peggedChangeAmount', 'percentOffset', 'permId', 'randomizePrice', 'randomizeSize', 'refFuturesConId', 'referenceChangeAmount', 'referenceContractId', 
//'referenceExchangeId', 'referencePriceType', 'routeMarketableToBbo', 'rule80A', 'scaleAutoReset', 'scaleInitFillQty', 'scaleInitLevelSize', 'scaleInitPosition', 
//'scalePriceAdjustInterval', 'scalePriceAdjustValue', 'scalePriceIncrement', 'scaleProfitOffset', 'scaleRandomPercent', 'scaleSubsLevelSize', 'scaleTable', 'settlingFirm', 
//'shareholder', 'shortSaleSlot', 'smartComboRoutingParams', 'softDollarTier', 'solicited', 'startingPrice', 'stockRangeLower', 'stockRangeUpper', 'stockRefPrice', 'sweepToFill', 
//'tif', 'totalQuantity', 'trailStopPrice', 'trailingPercent', 'transmit', 'triggerMethod', 'triggerPrice', 'usePriceMgmtAlgo', 'volatility', 'volatilityType', 'whatIf']
//orderId, status, filled, remaining, avgFillPrice, permId, parentId, lastFillPrice, clientId, whyHeld, mktCapPrice
//0, PreSubmitted, 0.0, 2.0, 0.0, 472836781, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 2.0, 0.0, 525186885, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 1.0, 0.0, 1126634671, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 3.0, 0.0, 1358068347, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 3.0, 0.0, 578626039, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 1.0, 0.0, 1126634667, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 2.0, 0.0, 1648111537, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 2.0, 0.0, 1013371892, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 3.0, 0.0, 472836796, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 2.0, 0.0, 429776910, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 2.0, 0.0, 1651667301, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 3.0, 0.0, 593121474, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 1.0, 0.0, 1013371773, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 2.0, 0.0, 1013371899, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 2.0, 0.0, 1651667311, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 3.0, 0.0, 1793482360, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 2.0, 0.0, 1489424985, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 2.0, 0.0, 1489424999, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 3.0, 0.0, 1423420790, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 4.0, 0.0, 930410965, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 2.0, 0.0, 349402865, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 1.0, 0.0, 1648111512, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 3.0, 0.0, 930410960, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 2.0, 0.0, 1283176798, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 3.0, 0.0, 930410972, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 3.0, 0.0, 1013371850, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 3.0, 0.0, 525186786, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 3.0, 0.0, 578355785, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 1.0, 0.0, 1871569721, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 2.0, 0.0, 1013371858, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 4.0, 0.0, 441263099, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 2.0, 0.0, 441263093, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 3.0, 0.0, 1013371868, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 2.0, 0.0, 349402861, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 2.0, 0.0, 1225771471, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 1.0, 0.0, 472836716, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 1.0, 0.0, 1126634725, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 1.0, 0.0, 1126634853, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 2.0, 0.0, 1238644631, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 2.0, 0.0, 1107069626, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 2.0, 0.0, 1013371808, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 3.0, 0.0, 1225771465, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 3.0, 0.0, 1013371936, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 1.0, 0.0, 1013371822, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 1.0, 0.0, 1914062172, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 2.0, 0.0, 225206818, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 1.0, 0.0, 1871569730, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 3.0, 0.0, 441263106, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 2.0, 0.0, 1019128559, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 2.0, 0.0, 525186844, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 3.0, 0.0, 1579140557, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 2.0, 0.0, 1019128554, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 1.0, 0.0, 1013371827, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 1.0, 0.0, 1914062272, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 2.0, 0.0, 119712244, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 3.0, 0.0, 1223270847, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 2.0, 0.0, 119712250, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 2.0, 0.0, 225206837, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 1.0, 0.0, 472836725, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 1.0, 0.0, 1871569622, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 1.0, 0.0, 128175992, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 2.0, 0.0, 1891157478, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 3.0, 0.0, 1251023439, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 2.0, 0.0, 105407767, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 3.0, 0.0, 225206798, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 3.0, 0.0, 1013372046, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 2.0, 0.0, 930410908, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 1.0, 0.0, 1107069584, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 3.0, 0.0, 1013371786, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 2.0, 0.0, 1489425067, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 3.0, 0.0, 1651667356, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 3.0, 0.0, 525186850, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 2.0, 0.0, 1013371797, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 3.0, 0.0, 1013371922, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 4.0, 0.0, 525186875, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 1.0, 0.0, 1507819429, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 3.0, 0.0, 1251023466, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 3.0, 0.0, 1489424952, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 3.0, 0.0, 525186867, 0, 0.0, 0, , 0.0
//0, PreSubmitted, 0.0, 4.0, 0.0, 225206803, 0, 0.0, 0, , 0.0


//action, autoCancelDate, orderType, totalQuantity, lmtPrice, tif

//+contract    2637556357776: 28812380,UNP,BAG,,0.0,?,,SMART,,USD,28812380,COMB,False,,combo: 444757041 | 1,444757059 | -1; 444757041,1,BUY,SMART,0,0,,-1; 444757059,1,SELL,SMART,0,0,,-1 Contract
//		index	'0UNP'	str
//-		order	2637556357608: 0,0,472836781: LMT SELL 2@4.500000 GTC	Order
//		account	'U3333333'	str   (for example)
//		action	'SELL'	str
//		activeStartTime	''	str
//		activeStopTime	''	str
//		adjustableTrailingUnit	0	int
//		adjustedOrderType	'None'	str
//		adjustedStopLimitPrice	1.7976931348623157e+308	float
//		adjustedStopPrice	1.7976931348623157e+308	float
//		adjustedTrailingAmount	1.7976931348623157e+308	float
//		algoId	''	str
//		algoParams	None	NoneType
//		algoStrategy	''	str
//		allOrNone	False	bool
//		auctionStrategy	0	int
//		autoCancelDate	''	str
//		autoCancelParent	False	bool
//		auxPrice	0.0	float
//		basisPoints	1.7976931348623157e+308	float
//		basisPointsType	2147483647	int
//		blockOrder	False	bool
//		cashQty	0.0	float
//		clearingAccount	''	str
//		clearingIntent	'IB'	str
//		clientId	0	int
//+		conditions	[]  list
//		conditionsCancelOrder	False	bool
//		conditionsIgnoreRth	False	bool
//		continuousUpdate	False	bool
//		delta	1.7976931348623157e+308	float
//		deltaNeutralAuxPrice	1.7976931348623157e+308	float
//		deltaNeutralClearingAccount	''	str
//		deltaNeutralClearingIntent	''	str
//		deltaNeutralConId	0	int
//		deltaNeutralDesignatedLocation	''	str
//		deltaNeutralOpenClose	'?'	str
//		deltaNeutralOrderType	'None'	str
//		deltaNeutralSettlingFirm	''	str
//		deltaNeutralShortSale	False	bool
//		deltaNeutralShortSaleSlot	0	int
//		designatedLocation	''	str
//		discretionaryAmt	0.0	float
//		discretionaryUpToLimitPrice	False	bool
//		displaySize	0	int
//		dontUseAutoPriceForHedge	True	bool
//		eTradeOnly	False	bool
//		exemptCode	-1	int
//		extOperator	''	str
//		faGroup	''	str
//		faMethod	''	str
//		faPercentage	''	str
//		faProfile	''	str
//		filledQuantity	1.7976931348623157e+308	float
//		firmQuoteOnly	False	bool
//		goodAfterTime	''	str
//		goodTillDate	''	str
//		hedgeParam	''	str
//		hedgeType	''	str
//		hidden	False	bool
//		imbalanceOnly	False	bool
//		isOmsContainer	False	bool
//		isPeggedChangeAmountDecrease	False	bool
//		lmtPrice	4.5	float
//		lmtPriceOffset	1.7976931348623157e+308	float
//		mifid2DecisionAlgo	''	str
//		mifid2DecisionMaker	''	str
//		mifid2ExecutionAlgo	''	str
//		mifid2ExecutionTrader	''	str
//		minQty	2147483647	int
//		modelCode	''	str
//		nbboPriceCap	1.7976931348623157e+308	float
//		notHeld	False	bool
//		ocaGroup	''	str
//		ocaType	3	int
//		openClose	''	str
//		optOutSmartRouting	False	bool
//		orderComboLegs	None	NoneType
//		orderId	0	int
//		orderMiscOptions	None	NoneType
//		orderRef	''	str
//		orderType	'LMT'	str
//		origin	0	int
//		outsideRth	False	bool
//		overridePercentageConstraints	False	bool
//		parentId	0	int
//		parentPermId	0	int
//		peggedChangeAmount	0.0	float
//		percentOffset	1.7976931348623157e+308	float
//		permId	472836781	int
//		randomizePrice	False	bool
//		randomizeSize	False	bool
//		refFuturesConId	0	int
//		referenceChangeAmount	0.0	float
//		referenceContractId	0	int
//		referenceExchangeId	''	str
//		referencePriceType	0	int
//		routeMarketableToBbo	False	bool
//		rule80A	'0'	str
//		scaleAutoReset	False	bool
//		scaleInitFillQty	2147483647	int
//		scaleInitLevelSize	2147483647	int
//		scaleInitPosition	2147483647	int
//		scalePriceAdjustInterval	2147483647	int
//		scalePriceAdjustValue	1.7976931348623157e+308	float
//		scalePriceIncrement	1.7976931348623157e+308	float
//		scaleProfitOffset	1.7976931348623157e+308	float
//		scaleRandomPercent	False	bool
//		scaleSubsLevelSize	2147483647	int
//		scaleTable	''	str
//		settlingFirm	''	str
//		shareholder	''	str
//		shortSaleSlot	0	int
//		smartComboRoutingParams	None	NoneType
//+		softDollarTier	2637556392120: Name: , Value: , DisplayName: SoftDollarTier
//	 solicited   False bool
//   startingPrice	1.7976931348623157e+308	float
//		stockRangeLower	1.7976931348623157e+308	float
//		stockRangeUpper	1.7976931348623157e+308	float
//		stockRefPrice	1.7976931348623157e+308	float
//		sweepToFill	False	bool
//		tif	'GTC'	str
//		totalQuantity	2.0	float
//		trailStopPrice	1.7976931348623157e+308	float
//		trailingPercent	1.7976931348623157e+308	float
//		transmit	True	bool
//		triggerMethod	0	int
//		triggerPrice	1.7976931348623157e+308	float
//		usePriceMgmtAlgo	False	bool
//		volatility	1.7976931348623157e+308	float
//		volatilityType	0	int
//		whatIf	False	bool

//-contract    2305033823624: 428560601,AAPL,OPT,20201218,130.0,C,100,,,USD,AAPL 201218C00130000,AAPL,False,,combo: Contract
//comboLegs   None NoneType
//		comboLegsDescrip	''	str
//		conId	428560601	int
//		currency	'USD'	str
//		deltaNeutralContract	None	NoneType
//		exchange	''	str
//		includeExpired	False	bool
//		lastTradeDateOrContractMonth	'20201218'	str
//		localSymbol	'AAPL  201218C00130000'	str
//		multiplier	'100'	str
//		primaryExchange	''	str
//		right	'C'	str
//		secId	''	str
//		secIdType	''	str
//		secType	'OPT'	str
//		strike	130.0	float
//		symbol	'AAPL'	str
//		tradingClass	'AAPL'	str
