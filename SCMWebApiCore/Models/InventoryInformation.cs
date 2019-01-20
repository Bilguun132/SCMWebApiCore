using System;
using System.Collections.Generic;

namespace SCMWebApiCore.Models
{
    public partial class InventoryInformation
    {
        public InventoryInformation()
        {
            Player = new HashSet<Player>();
        }

        public int Id { get; set; }
        public int CurrentInventory { get; set; }
        public int? Backlogs { get; set; }
        public int? IncomingInventory { get; set; }
        public int? NewOrder { get; set; }
        public double? TotalCost { get; set; }

        virtual public ICollection<Player> Player { get; set; }
    }
}
