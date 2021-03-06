﻿using System;
using System.Collections.Generic;

namespace SCMWebApiCore.Models
{
    public partial class Game
    {
        public Game() => GameTeamPlayerRelationship = new HashSet<GameTeamPlayerRelationship>();

        public int Id { get; set; }
        public string Name { get; set; }
        public int MaxPeriod { get; set; }
        public int DeliveryDelay { get; set; }
        public string DemandInformation { get; set; }
        public int? FacilitatorId { get; set; }
        public string GameUrl { get; set; }

        virtual public ICollection<GameTeamPlayerRelationship> GameTeamPlayerRelationship { get; set; }
    }
}
