﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace SCMWebApiCore.Models
{
    public partial class Results
    {
        public int Id { get; set; }
        public int? GameTeamPlayerRelationshipId { get; set; }
        public int? Inventory { get; set; }
        public int? IncomingInventory { get; set; }
        public int? PreviousOrder { get; set; }
        public double? TotalCost { get; set; }
        public int? Period { get; set; }
        
        [JsonIgnore]
        public GameTeamPlayerRelationship GameTeamPlayerRelationship { get; set; }
    }
}
