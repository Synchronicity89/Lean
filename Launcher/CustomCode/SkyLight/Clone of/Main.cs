using System;
using System.Collections;
using System.Collections.Generic; 
using QuantConnect.Securities;  
//using QuantConnect.Models;
using QuantConnect.Data.Market;
using QuantConnect.Algorithm;
using QuantConnect;
using System.Globalization;

namespace QuantConnect {
    public static class ConvExtensions
    {
        private static readonly CultureInfo CultureInfo = CultureInfo.InvariantCulture;
        private static readonly IFormatProvider FormatProvider = CultureInfo;
        private static readonly StringComparison StringComparison = StringComparison.InvariantCulture;

        public static string ToString(this decimal value)
        {
            return value.ToString(CultureInfo);
        }

        public static string ToString(this decimal value, string message)
        {
            return string.Format(CultureInfo, message, value);
        }
    }
        /*
        *   QuantConnect University
        *   Martingale Position Sizing Experiment
        *   
        *   Martingale is a [risky/gambling] technique which can be applied to trading:
        *   When a trade is going against you, double down and flip the position (long -> short). 
        *   If the position is still going against you continue flipping direction until you win.
        *
        *   Typically martingale curves are perfectly straight, until they drop off sharply a cliff! 
        *   This is because they hide the intra-trade risk and only show the closing profits. QC shows the full 
        *   equity curve exposing true martingale risks.
        */
        public partial class QCUMartingalePositionSizing : QCAlgorithm 
    {
        //Algorithm Settings:
        decimal peakTroughDeltaBeforeFlip = 0.03m;  // Percentage below high watermark before we flip our position
        decimal targetProfit = 0.02m;               //Target profit for strategy. When achieve this exit.
        
        //Algorithm Working Variables:
        string symbol = "SPY";
        decimal magnitudeDirection = 0.2m, tradeStringProfit = 0m, leverage = 4m;
        DateTime exitDate = new DateTime();
        CloneRelativeStrengthIndex rsi = new CloneRelativeStrengthIndex(14);
        
        
        //Set up the initial algorithm backtesting settings:
        public override void Initialize()
        {
            SetStartDate(2000, 1, 1);
            SetEndDate(2014, 1, 1);; //(DateTime.Now.Date.AddDays(-1)); 
            SetCash(25000);
            AddSecurity(SecurityType.Equity, symbol, Resolution.Minute, true, leverage, false);
        }
        
        
        ///<summary>
        /// Loss per share to exit/flip direction on the asset
        ///</summary>
        private decimal MaxLossPerShare {
            get {
                //Simpler calc for max loss per share: max loss % * stock price.
                return Portfolio[symbol].AveragePrice * peakTroughDeltaBeforeFlip;
            }
        }
        
        
        ///<summary>
        ///Sum of the martingale flip-losses + current unrealised profit
        ///</summary>
        private decimal UnrealizedTradeStringProfit {
            get { 
                return tradeStringProfit + Portfolio.TotalUnrealizedProfit; 
            }
        }
        
        
        ///<summary>
        ///Short hand bool for detecting if the algorithm has reached minimum profit target
        // -> Profit on holdings rather than cash -- makes gains achievable.
        ///</summary>
        private bool MinimumProfitTargetReached {
            get { 
                return (Portfolio.TotalUnrealizedProfit / Math.Abs(Portfolio.TotalUnleveredAbsoluteHoldingsCost)) > targetProfit; 
                //return (UnrealizedTradeStringProfit / Math.Abs(Portfolio.TotalUnleveredAbsoluteHoldingsCost)) > targetProfit; 
            }
        }


        ///<summary>
        /// Enter the market, monitor the loss/peak gain and code up a fake stop.
        ///</summary>
        public void OnData(TradeBars data) 
        {   
            TradeBar SPY = data[symbol];
            decimal price = data[symbol].Close;
            
            //Update calculation for SPY-RSI:
            rsi.AddSample(SPY);
            
            //Don't trade on same day we just exited.
            if (exitDate.Date == Time.Date) return;
            
            //We dont have stock yet: this is a completely dumb entry strategy so lets just go long to kick it off.
            if (!Portfolio.HoldStock) {
                ScanForEntry(SPY);
                return;
            }
            
            //We have stock, but scan for a profit taking opportunity:
            if (MinimumProfitTargetReached) {
                ScanForExit(SPY);
                return;
            }

            //Finally we're not in green, but not flipped yet: monitor the loss to change direction:
            if (Math.Abs(price - Portfolio[symbol].AveragePrice) > MaxLossPerShare && Portfolio.TotalUnrealizedProfit < 0) {
                SetMagnitudeDirection(-2);
                Flip(); 
            }
                
        }
        
        
        ///<summary>
        /// Scan for an entry signal to invest.
        ///</summary>
        public void ScanForEntry(TradeBar SPY) {
            //Once we have enough data, start the Entry detection.
            if (rsi.Ready) {
                if (rsi.RSI > 70) {
                    magnitudeDirection = -0.2m;
                    SetHoldings(symbol, -magnitudeDirection);   //Over bought
                    Log("Entry-Short: " + magnitudeDirection + " Holdings: " + Portfolio[symbol].Quantity);
                } else if (rsi.RSI < 30) {
                    magnitudeDirection = 0.2m;
                    SetHoldings(symbol, magnitudeDirection);    //Over sold
                    Log("Entry-Long: " + magnitudeDirection + " Holdings: " + Portfolio[symbol].Quantity);
                }
            }
        }
        
        
        ///<summary>
        /// For now dumb exit;
        ///</summary>
        public void ScanForExit(TradeBar SPY) {
#pragma warning disable CA1305 // Specify IFormatProvider
            Log("Exit: " + magnitudeDirection + " Realized Profit/Loss: " + UnrealizedTradeStringProfit.ToString("C"));
#pragma warning restore CA1305 // Specify IFormatProvider
            Liquidate();
            tradeStringProfit = 0;
            magnitudeDirection = 0.2m; 
            exitDate = Time.Date;
            return;
        }
        
        
        ///<summary>
        /// Set and Normalise MagnitudeDirection Multiplier
        ///</summary> 
        public void SetMagnitudeDirection(decimal multiplier) {
            
            //Apply multiplier:
            magnitudeDirection = magnitudeDirection * multiplier;
            decimal direction = magnitudeDirection / Math.Abs(magnitudeDirection);
            
            //Normalize Max Investment to Max Leverage 
            if (Math.Abs(magnitudeDirection) > leverage) { 
                magnitudeDirection = direction * leverage;
            }
            
            //Normalize Minimum Investment to 20%;
            if (Math.Abs(magnitudeDirection) < 0.2m) {
                magnitudeDirection = direction * 0.2m;
            }
        }
        
        
        ///<summary>
        /// Flip & Invert our Holdings When We're Wrong:
        ///</summary>
        public void Flip() {
            //Record the loss
            tradeStringProfit += Portfolio.TotalUnrealisedProfit; 
            exitDate = Time.Date;
#pragma warning disable CS0618 // Type or member is obsolete
            SetHoldings(symbol, magnitudeDirection);
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning disable CA1305 // Specify IFormatProvider
            Log("Flip: " + magnitudeDirection + " Holdings: " + Portfolio[symbol].Quantity + " String Loss: " + tradeStringProfit.ToString("C"));
#pragma warning restore CA1305 // Specify IFormatProvider
        }
    }
}