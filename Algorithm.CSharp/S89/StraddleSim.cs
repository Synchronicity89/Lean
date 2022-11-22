using QLNet;
using QuantConnect.Securities;
using QuantConnect.Util;
using RDotNet;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Securities.Option;
using Option = QuantConnect.Securities.Option.Option;
using QuantConnect.Data;
using Newtonsoft.Json.Linq;

namespace QuantConnect.Algorithm.CSharp.S89
{
    public class StraddleSim : QCAlgorithm
    {
        private Symbol symbol;
        private string symbolInit = "SPX";
        private decimal abs = 0.0M;
        private string lug = "";
        private int logCount = 0;

        public override void Initialize()
        {
            this.SetStartDate(2022, 8, 8);  // Set Start Date
            this.SetEndDate(2022, 10, 18);
            this.SetCash(100000);  // Set Strategy Cash
            this.symbol = this.AddIndex(symbolInit, Resolution.Hour).Symbol;
        }

        public override void OnData(Slice slice)
        {
            if (Time.Hour < 11 && Time.Minute < 30)
            {
                var openHigh = Securities[symbol].High - Securities[symbol].Open;
                var openLow = Securities[symbol].Open - Securities[symbol].Low;
                if (openHigh >= openLow)
                {
                    abs += openHigh;
                }
                else
                {
                    abs += openLow;
                }
                lug += Time.Hour.ToString().PadLeft(2, '0') + Time.Minute.ToString().PadLeft(2, '0') + "_" + abs + ";";
                logCount++;
                if (logCount % 10 == 0)
                {
                    Log(lug);
                    lug = "";
                }
            }
        }
    }

}
