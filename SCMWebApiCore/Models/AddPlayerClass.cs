﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SCMWebApiCore.Models
{
    public class AddPlayerClass
    {
        public List<PlayerClass> Players { get; set; }
        public int GameId { get; set; }
    }
}