﻿/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace QuantConnect.Orders.Serialization
{
    /// <summary>
    /// Data transfer object used for serializing an <see cref="OrderEvent"/> that was just generated by an algorithm
    /// </summary>
    public class SerializedOrderEvent
    {
        /// <summary>
        /// The unique order event id
        /// </summary>
        [JsonProperty("id")]
        public string Id => $"{AlgorithmId}-{OrderId}-{OrderEventId}";

        /// <summary>
        /// Algorithm Id, BacktestId or DeployId
        /// </summary>
        [JsonProperty("algorithm-id")]
        public string AlgorithmId { get; set; }

        /// <summary>
        /// Id of the order this event comes from.
        /// </summary>
        [JsonProperty("order-id")]
        public int OrderId { get; set; }

        /// <summary>
        /// The unique order event id for each order
        /// </summary>
        [JsonProperty("order-event-id")]
        public int OrderEventId { get; set; }

        /// <summary>
        /// Easy access to the order symbol associated with this event.
        /// </summary>
        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        /// <summary>
        /// The time of this event in unix timestamp
        /// </summary>
        [JsonProperty("time")]
        public double Time { get; set; }

        /// <summary>
        /// Status message of the order.
        /// </summary>
        [JsonProperty("status"), JsonConverter(typeof(StringEnumConverter), true)]
        public OrderStatus Status { get; set; }

        /// <summary>
        /// The fee amount associated with the order
        /// </summary>
        [JsonProperty("order-fee-amount", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public decimal? OrderFeeAmount { get; set; }

        /// <summary>
        /// The fee currency associated with the order
        /// </summary>
        [JsonProperty("order-fee-currency", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string OrderFeeCurrency { get; set; }

        /// <summary>
        /// Fill price information about the order
        /// </summary>
        [JsonProperty("fill-price")]
        public decimal FillPrice { get; set; }

        /// <summary>
        /// Currency for the fill price
        /// </summary>
        [JsonProperty("fill-price-currency")]
        public string FillPriceCurrency { get; set; }

        /// <summary>
        /// Number of shares of the order that was filled in this event.
        /// </summary>
        [JsonProperty("fill-quantity")]
        public decimal FillQuantity { get; set; }

        /// <summary>
        /// Order direction.
        /// </summary>
        [JsonProperty("direction"), JsonConverter(typeof(StringEnumConverter), true)]
        public OrderDirection Direction { get; set; }

        /// <summary>
        /// Any message from the exchange.
        /// </summary>
        [DefaultValue(""), JsonProperty("message", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Message { get; set; }

        /// <summary>
        /// True if the order event is an assignment
        /// </summary>
        [JsonProperty("is-assignment")]
        public bool IsAssignment { get; set; }

        /// <summary>
        /// The current order quantity
        /// </summary>
        [JsonProperty("quantity")]
        public decimal Quantity { get; set; }

        /// <summary>
        /// The current stop price
        /// </summary>
        [JsonProperty("stop-price", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public decimal? StopPrice { get; set; }

        /// <summary>
        /// The current limit price
        /// </summary>
        [JsonProperty("limit-price", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public decimal? LimitPrice { get; set; }

        /// <summary>
        /// Empty constructor required for JSON converter.
        /// </summary>
        private SerializedOrderEvent()
        {

        }

        /// <summary>
        /// Creates a new instances based on the provided order event and algorithm Id
        /// </summary>
        public SerializedOrderEvent(OrderEvent orderEvent, string algorithmId)
        {
            AlgorithmId = algorithmId;
            OrderId = orderEvent.OrderId;
            OrderEventId = orderEvent.Id;
            Symbol = orderEvent.Symbol.ID.ToString();
            Time = QuantConnect.Time.DateTimeToUnixTimeStamp(orderEvent.UtcTime);
            Status = orderEvent.Status;
            if (orderEvent.OrderFee.Value.Currency != Currencies.NullCurrency)
            {
                OrderFeeAmount = orderEvent.OrderFee.Value.Amount;
                OrderFeeCurrency = orderEvent.OrderFee.Value.Currency;
            }
            FillPrice = orderEvent.FillPrice;
            FillPriceCurrency = orderEvent.FillPriceCurrency;
            FillQuantity = orderEvent.FillQuantity;
            Direction = orderEvent.Direction;
            Message = orderEvent.Message;
            IsAssignment = orderEvent.IsAssignment;
            Quantity = orderEvent.Quantity;
            StopPrice = orderEvent.StopPrice;
            LimitPrice = orderEvent.LimitPrice;
        }
    }
}
