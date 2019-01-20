using System;
using System.Collections.Generic;
using System.Linq;

namespace SCMWebApiCore.Models
{
    public class AddPlayerClass
    {
        public List<PlayerClass> Players { get; set; }
        public object GameId { get; set; }
        public int FacilId { get; set; }
        public string GameName { get; set; }
    }
}
