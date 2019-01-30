using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace SCMWebApiCore.Models
{
    public partial class GameTeamPlayerRelationship
    {
        public GameTeamPlayerRelationship() => Results = new HashSet<Results>();

        public int Id { get; set; }
        public int GameId { get; set; }
        public int TeamId { get; set; }
        public int PlayerId { get; set; }

        [JsonIgnore]
        virtual public Game Game { get; set; }
        [JsonIgnore]
        virtual public Player Player { get; set; }
        [JsonIgnore]
        virtual public Team Team { get; set; }
        virtual public ICollection<Results> Results { get; set; }
    }
}
