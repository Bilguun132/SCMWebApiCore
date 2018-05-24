using Microsoft.AspNetCore.Mvc;
using SCMWebApiCore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SCMWebApiCore.DataProviders
{
    public interface IDataProvider
    {
        Task<Player> Join(string role, string connectionId);
        Task<object> GetPlayer(int id);
        void SendEmail(Player player);
        Task<List<Results>> GetDecisions(int id);
    }
}
