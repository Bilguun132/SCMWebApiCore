using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace SCMWebApiCore.Models
{
    public partial class PlayerRole
    {
        public PlayerRole()
        {
            Player = new HashSet<Player>();
        }

        public int Id { get; set; }
        public string Role { get; set; }

        [JsonIgnore]
        public ICollection<Player> Player { get; set; }
    }
}
