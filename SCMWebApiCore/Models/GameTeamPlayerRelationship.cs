using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace SCMWebApiCore.Models
{
    public partial class GameTeamPlayerRelationship
    {
        public GameTeamPlayerRelationship()
        {
            Results = new HashSet<Results>();
        }

        public int Id { get; set; }
        public int GameId { get; set; }
        public int TeamId { get; set; }
        public int PlayerId { get; set; }

        [JsonIgnore]
        public Game Game { get; set; }
        [JsonIgnore]
        public Player Player { get; set; }
        [JsonIgnore]
        public Team Team { get; set; }
        public ICollection<Results> Results { get; set; }
    }
}
