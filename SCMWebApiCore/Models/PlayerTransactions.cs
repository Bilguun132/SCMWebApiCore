using System;
using System.Collections.Generic;

namespace SCMWebApiCore.Models
{
    public partial class PlayerTransactions
    {
        public int Id { get; set; }
        public int OrderMadeFrom { get; set; }
        public int OrderMadeTo { get; set; }
        public int GameId { get; set; }
        public int TeamId { get; set; }
        public int OrderMadePeriod { get; set; }
        public int OrderReceivePeriod { get; set; }
        public int OrderQty { get; set; }
        public int? SentQty { get; set; }
        public int? Inventory { get; set; }
        public decimal? Cost { get; set; }
    }
}
