using System;
using System.Collections.Generic;

namespace SCMWebApiCore.Models
{
    public partial class Player
    {
        public Player()
        {
            GameTeamPlayerRelationship = new HashSet<GameTeamPlayerRelationship>();
        }

        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public int? PlayerRoleId { get; set; }
        public bool? IsAvailable { get; set; }
        public string ConnectionId { get; set; }
        public int? InventoryId { get; set; }
        public bool? HasMadeDecision { get; set; }

        public InventoryInformation Inventory { get; set; }
        public PlayerRole PlayerRole { get; set; }
        public ICollection<GameTeamPlayerRelationship> GameTeamPlayerRelationship { get; set; }
    }
}
