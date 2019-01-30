using System;
using System.Collections.Generic;

namespace SCMWebApiCore.Models
{
    public partial class Team
    {
        public Team()
        {
            GameTeamPlayerRelationship = new HashSet<GameTeamPlayerRelationship>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public int CurrentPeriod { get; set; }
        public int CurrentOrder { get; set; }

        virtual public ICollection<GameTeamPlayerRelationship> GameTeamPlayerRelationship { get; set; }
    }
}
