using System;
using System.Collections.Generic;

namespace SCMWebApiCore.Models
{
    public partial class Game
    {
        public Game()
        {
            GameTeamPlayerRelationship = new HashSet<GameTeamPlayerRelationship>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public int Period { get; set; }
        public int MaxPeriod { get; set; }
        public int DeliveryDelay { get; set; }
        public int? CurrentOrder { get; set; }

        public ICollection<GameTeamPlayerRelationship> GameTeamPlayerRelationship { get; set; }
    }
}
