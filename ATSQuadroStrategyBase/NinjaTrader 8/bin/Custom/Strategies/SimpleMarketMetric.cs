using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.Strategies;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Serialization;

namespace NinjaTrader.NinjaScript.Strategies
{
    public class SimpleMarketMetric : StrategyBase
    {
        private MFI mfi;
        private Series<double> smoothedTrueRange;
        private Series<double> smoothedDiPlus;
        private Series<double> smoothedDiMinus;
        private Series<double> adxSeries;
        private int lastTrendSwitch = 0;

        private int signalBars = 3;
        private int trailLookBackBars = 3;
        private double longStopPrice = 0, shortStopPrice = 0;
        private int stopSize = 28;
        public EMA emaFast { get; set; }
        public EMA emaMedium { get; set; }
        public EMA emaSlow { get; set; }

        private const int Ema8Index = 0;
        private const int Ema13Index = 1;
        private const int Ema21Index = 2;
        private const int mfiIndex = 3;
        private const int trendUpIndex = 4;
        private const int trendDownIndex = 5;

        // User Inputs
        private bool enableBuySellSignals;
        private bool enableMaFilter;
        private int maFilterPeriod;
        private string maFilterType;


        protected override void OnStateChange()
        {
            base.OnStateChange();

            if (State == State.SetDefaults)
            {
                Description = @"SimpleMarketMetric using the StrategyBase Strategy Foundation";
                Name = "SimpleMarketMetric";
                Fast = 10;
                Slow = 25;
                TradeSignalCrossoverMode = 1;
                Calculate = Calculate.OnBarClose;
                ProfitTargetTicks = 20;
                MaxProfitLines = 10;
                DisplayInDataBox = true;
                DrawOnPricePanel = true;
                ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;

                EnableBuySellSignals = true;
                EnableMaFilter = true;
                MaFilterPeriod = 200;
                MaFilterType = "EMA";

                EnableSupportResistance = true;
                SupportResistanceLookback = 20;
                MaxSupportResistanceLines = 20;

                EnableRealPriceLine = true;
                RealPriceLineColor = Brushes.White;
                RealPriceLineStyle = DashStyleHelper.Dot;
                ShowCloseDots = true;
                CloseDotColor = Brushes.White;

                EnableDashboard = true;
                EnableDashboardSignals = true;
                MfiBullishColor = Brushes.Lime;
                MfiBearishColor = Brushes.Red;
                IsOverlay = false;
            }
            else if (State == State.Configure)
            {
                AddDataSeries(BarsPeriodType.Minute, 1);
                AddDataSeries(BarsPeriodType.Minute, 2);
                AddDataSeries(BarsPeriodType.Minute, 5);
            }
            else if (State == State.DataLoaded)
            {
                emaFast = EMA(Close, 8);
                emaMedium = EMA(Close, 13);
                emaSlow = EMA(Close, 21);
                mfi = MFI(10);
                smoothedTrueRange = new Series<double>(this);
                smoothedDiPlus = new Series<double>(this);
                smoothedDiMinus = new Series<double>(this);
                adxSeries = new Series<double>(this);
            }
        }

        protected override void OnAccountItemUpdate(Account account, AccountItem accountItem, double value)
        {
            base.OnAccountItemUpdate(account, accountItem, value);
        }

        protected override void OnConnectionStatusUpdate(ConnectionStatusEventArgs connectionStatusUpdate)
        {
            base.OnConnectionStatusUpdate(connectionStatusUpdate);
        }

        protected override void OnMarketData(MarketDataEventArgs marketDataUpdate)
        {
            base.OnMarketData(marketDataUpdate);
        }

        protected override void OnBarUpdate()
        {
            //Add your custom strategy logic here.
            if (CurrentBar < BarsRequiredToTrade)
                return;

            //Trade Engine Signal State to pass in
            AlgoSignalAction = AlgoSignalAction.None;

            //DoubleMACrossover mode
            if (TradeSignalCrossoverMode == 1)
            {
                if (base.Position.MarketPosition != MarketPosition.Long && CrossAbove(smaFast, smaSlow, 1))
                    AlgoSignalAction = AlgoSignalAction.GoLong;
                else if (base.Position.MarketPosition != MarketPosition.Short && CrossBelow(smaFast, smaSlow, 1))
                    AlgoSignalAction = AlgoSignalAction.GoShort;
            }

            //PullbackOCO mode
            if (AlgoSignalAction == AlgoSignalAction.None && TradeSignalPullBackOCOMode > 0)
            {
                if (smaFast[0] > smaSlow[0])
                {
                    if (Close[0] < smaFast[0])
                        AlgoSignalAction = AlgoSignalAction.GoOCOEntry;
                }
                else if (smaFast[0] < smaSlow[0])
                {
                    if (Close[0] > smaFast[0])
                        AlgoSignalAction = AlgoSignalAction.GoOCOEntry;
                }

                //if mode 2 wait for flat  -reset any
                if (TradeSignalPullBackOCOMode == 2 && Position.MarketPosition != MarketPosition.Flat) AlgoSignalAction = AlgoSignalAction.None;
            }
            base.OnBarUpdate();
        }

        protected override void OnOrderUpdate(Order order, double limitPrice, double stopPrice, int quantity, int filled, double averageFillPrice, OrderState orderState, DateTime time, ErrorCode error, string comment)
        {
            base.OnOrderUpdate(order, limitPrice, stopPrice, quantity, filled, averageFillPrice, orderState, time, error, comment);
        }

        protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
        {
            base.OnExecutionUpdate(execution, executionId, price, quantity, marketPosition, orderId, time);
        }

        public override bool OnPreTradeEntryValidate(bool isLong)
        {
            //if long or short trade coming then check account balance or time
            //or other rules and return true to allow the trade or false to block the trade
            if (isLong)
            {
                return true;
            }
            else
            {
                return true;
            }
        }

        public override void OnStrategyTradeWorkFlowUpdated(StrategyTradeWorkFlowUpdatedEventArgs e)
        {
            //here you can do stuff based on the workflow state update
            if (base.TradeWorkFlow == StrategyTradeWorkFlowState.ExitTrade)
            {
                base.BackBrush = Brushes.Gray;
            }
        }

        public override Order SubmitShort(string signal)
        {
            orderEntry = SubmitOrderUnmanaged(0, OrderAction.SellShort, OrderType.Market, 1, 0, 0, String.Empty, signal);
            return orderEntry;
        }

        public override Order SubmitLong(string signal)
        {
            orderEntry = SubmitOrderUnmanaged(0, OrderAction.Buy, OrderType.Market, 1, 0, 0, String.Empty, signal);
            return orderEntry;
        }

        public override void SubmitProfitTarget(Order orderEntry, string oCOId)
        {
            if (orderEntry.OrderAction == OrderAction.Buy)
            {
                string str = (orderEntry != null) ? orderEntry.Name.Replace("↑", string.Empty) : "Long";
                str = str.Substring(3);
                double price = orderEntry.AverageFillPrice + (10 * base.TickSize);
                price = base.Instrument.MasterInstrument.RoundToTickSize(price);
                base.orderTarget1 = base.SubmitOrderUnmanaged(0, OrderAction.Sell, OrderType.Limit, 1, price, 0.0, string.Format("{0}.OCO1.{1}", str, oCOId), "↓Trg1" + str);
            }
            else if (orderEntry.OrderAction == OrderAction.SellShort)
            {
                string str2 = (orderEntry != null) ? orderEntry.Name.Replace("↓", string.Empty) : "Short";
                str2 = str2.Substring(3);
                double price = orderEntry.AverageFillPrice - (10 * base.TickSize);
                price = base.Instrument.MasterInstrument.RoundToTickSize(price);

                base.orderTarget1 = base.SubmitOrderUnmanaged(0, OrderAction.BuyToCover, OrderType.Limit, 1, price, 0.0, string.Format("{0}.OCO1.{1}", str2, oCOId), "↑Trg1" + str2);
            }
        }

        public override void SubmitStopLoss(Order orderEntry)
        {
            if (orderEntry.OrderAction == OrderAction.Buy)
            {
                string str = (orderEntry != null) ? orderEntry.Name.Replace("↑", string.Empty) : "Long";
                str = str.Substring(3);
                double price = orderEntry.AverageFillPrice - (stopSize * base.TickSize);
                price = base.Instrument.MasterInstrument.RoundDownToTickSize(price);
                base.orderStop1 = base.SubmitOrderUnmanaged(0, OrderAction.Sell, OrderType.StopMarket, 1, price, price, string.Format("{0}.OCO1.{1}", str, base.oCOId), "↓Stp1" + str);
            }
            else if (orderEntry.OrderAction == OrderAction.SellShort)
            {
                string str2 = (orderEntry != null) ? orderEntry.Name.Replace("↓", string.Empty) : "Short";
                str2 = str2.Substring(3);
                double price = orderEntry.AverageFillPrice + (stopSize * base.TickSize);
                price = base.Instrument.MasterInstrument.RoundToTickSize(price);
                base.orderStop1 = base.SubmitOrderUnmanaged(0, OrderAction.BuyToCover, OrderType.StopMarket, 1, price, price, string.Format("{0}.OCO1.{1}", str2, base.oCOId), "↑Stp1" + str2);
            }
        }

        public override bool SubmitStopLossWillOccur()
        {
            return true;
        }

        public override bool SubmitProfitTargetWillOccur()
        {

            return true;

        }

        public override void TradeManagement(double lastPrice)
        {
            //if some rule says to exit  you can call ito the workflow and execute an exit
            // base.TradeWorkFlow = base.ProcessWorkFlow(StrategyTradeWorkFlowState.ExitTrade);
            // this.inManageCurrentPosition = false;  unlock
            //return before the next section if so... return
            try
            {
                if (base.Position.MarketPosition == MarketPosition.Long)
                {
                    //here we can test for excursion for trailing stops
                    double ticksExcursion = (int)Math.Round((double)((lastPrice - base.Position.AveragePrice) / base.TickSize), 0);
                    if (ticksExcursion > 0)
                    {
                        //get lowset low price within n bars set by trailLookBackBars
                        longStopPrice = Lows[0][LowestBar(base.Lows[0], Math.Min(trailLookBackBars, CurrentBars[0]))];
                        longStopPrice = Math.Min(GetCurrentBid(0) - TickSize, longStopPrice);
                        longStopPrice = Instrument.MasterInstrument.RoundToTickSize(longStopPrice);

                        //test the stoplosses are active and can be changed, compate stop price and then modify the order if required
                        if (base.IsOrderActiveCanChangeOrCancel(base.orderStop1))
                        {
                            if (this.longStopPrice > base.orderStop1.StopPrice)
                            {
                                base.ChangeOrder(base.orderStop1, base.orderStop1.Quantity, this.longStopPrice, this.longStopPrice);
                            }
                        }
                    }
                }
                else if (base.Position.MarketPosition == MarketPosition.Short)
                {
                    double ticksExcursion = (int)Math.Round((double)((base.Position.AveragePrice - lastPrice) / base.TickSize), 0);
                    if (ticksExcursion > 0)
                    {
                        //get highest high price within n bars set by trailLookBackBars
                        shortStopPrice = Highs[0][HighestBar(base.Highs[0], Math.Min(trailLookBackBars, CurrentBars[0]))];
                        shortStopPrice = Math.Max(GetCurrentAsk(0) + TickSize, shortStopPrice);
                        shortStopPrice = Instrument.MasterInstrument.RoundToTickSize(shortStopPrice);

                        //test the stoplosses are active and can be changed, compare stop price and then modify the order if required
                        if (base.IsOrderActiveCanChangeOrCancel(base.orderStop1))
                        {
                            if (this.shortStopPrice < base.orderStop1.StopPrice)
                            {
                                base.ChangeOrder(base.orderStop1, base.orderStop1.Quantity, this.shortStopPrice, this.shortStopPrice);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Print(string.Format("TradeManagement >> Error >> {0}" + ex.ToString()));
            }
        }

        #region Properties
        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "Fast", GroupName = "NinjaScriptStrategyParameters", Order = 0)]
        public int Fast
        { get; set; }

        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "Slow", GroupName = "NinjaScriptStrategyParameters", Order = 1)]
        public int Slow
        { get; set; }


        [Range(0, 1), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "SignalType1", Description = "0:off, 1:Double MA Crossover", GroupName = "NinjaScriptStrategyParameters", Order = 2)]
        public int TradeSignalCrossoverMode
        { get; set; }


        [Range(0, 2), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "SignalType2", Description = "0:off, 1:CloseAndOCO, 2:OCOWaitForFlat", GroupName = "NinjaScriptStrategyParameters", Order = 3)]
        public int TradeSignalPullBackOCOMode
        { get; set; }


        [NinjaScriptProperty]
        [Display(Name = "Enable Buy/Sell Signals", Order = 1, GroupName = "Signal Settings")]
        public bool EnableBuySellSignals
        {
            get { return enableBuySellSignals; }
            set { enableBuySellSignals = value; }
        }

        [NinjaScriptProperty]
        [Display(Name = "Enable MA Filter", Order = 2, GroupName = "Signal Settings")]
        public bool EnableMaFilter
        {
            get { return enableMaFilter; }
            set { enableMaFilter = value; }
        }

        //[NinjaScriptProperty]
        [Range(5, 500), NinjaScriptProperty]
        [Display(Name = "MA Filter Period", Order = 3, GroupName = "Signal Settings")]
        public int MaFilterPeriod
        {
            get { return maFilterPeriod; }
            set { maFilterPeriod = value; }
        }

        [NinjaScriptProperty]
        [Display(Name = "MA Filter Type", Order = 4, GroupName = "Signal Settings")]
        public string MaFilterType
        {
            get { return maFilterType; }
            set { maFilterType = value; }
        }
        [NinjaScriptProperty]
        [Range(1, 100)]
        [Display(Name = "Profit Target (Ticks)", Order = 5, GroupName = "Profit Settings")]
        public int ProfitTargetTicks { get; set; }

        [NinjaScriptProperty]
        [Range(1, 50)]
        [Display(Name = "Max Profit Lines", Order = 6, GroupName = "Profit Settings")]
        public int MaxProfitLines { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Enable S/R", Order = 7, GroupName = "Support/Resistance")]
        public bool EnableSupportResistance { get; set; }

        [NinjaScriptProperty]
        [Range(5, 100)]
        [Display(Name = "Pivot Sensitivity", Order = 8, GroupName = "Support/Resistance")]
        public int SupportResistanceLookback { get; set; }

        [NinjaScriptProperty]
        [Range(1, 50)]
        [Display(Name = "Max Lines", Order = 9, GroupName = "Support/Resistance")]
        public int MaxSupportResistanceLines { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Enable Real Price Line", Order = 10, GroupName = "Real Price")]
        public bool EnableRealPriceLine { get; set; }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Line Color", Order = 11, GroupName = "Real Price")]
        public Brush RealPriceLineColor { get; set; }

        [Browsable(false)]
        public string RealPriceLineColorSerializable
        {
            get { return Serialize.BrushToString(RealPriceLineColor); }
            set { RealPriceLineColor = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [Display(Name = "Line Style", Order = 12, GroupName = "Real Price")]
        public DashStyleHelper RealPriceLineStyle { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Show Close Dots", Order = 13, GroupName = "Real Price")]
        public bool ShowCloseDots { get; set; }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Dot Color", Order = 14, GroupName = "Real Price")]
        public Brush CloseDotColor { get; set; }

        [Browsable(false)]
        public string CloseDotColorSerializable
        {
            get { return Serialize.BrushToString(CloseDotColor); }
            set { CloseDotColor = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [Display(Name = "Enable Dashboard", Order = 15, GroupName = "Dashboard")]
        public bool EnableDashboard { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Enable Buy/Sell Dots", Order = 16, GroupName = "Dashboard")]
        public bool EnableDashboardSignals { get; set; }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Bullish MFI Color", Order = 17, GroupName = "Dashboard")]
        public Brush MfiBullishColor { get; set; }

        [Browsable(false)]
        public string MfiBullishColorSerializable
        {
            get { return Serialize.BrushToString(MfiBullishColor); }
            set { MfiBullishColor = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Bearish MFI Color", Order = 18, GroupName = "Dashboard")]
        public Brush MfiBearishColor { get; set; }

        [Browsable(false)]
        public string MfiBearishColorSerializable
        {
            get { return Serialize.BrushToString(MfiBearishColor); }
            set { MfiBearishColor = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [Display(Name = "Enable Chop Filter", Order = 19, GroupName = "Dashboard")]
        public bool EnableChopFilter { get; set; }

        #endregion
    }
}