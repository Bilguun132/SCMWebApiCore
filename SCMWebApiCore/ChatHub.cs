using Microsoft.AspNetCore.SignalR;
using SCMWebApiCore.DataProviders;
using SCMWebApiCore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SCMWebApiCore
{
    public class ChatHub: Hub
    {
        private IDataProvider dataProvider;
        public ChatHub(IDataProvider dataProvider)
        {
            this.dataProvider = dataProvider;
        }
        public async Task UpdateAllPlayers(string role)
        {
            Player player = await dataProvider.Join(role, Context.ConnectionId);
            await Clients.Caller.SendAsync("UpdateAllPlayers", player);
            await Clients.Others.SendAsync("UpdateAllPlayers", null);
        }

        public async Task ProgressGamePlay()
        {
            await Clients.All.SendAsync("ProgressGamePlay");
        }
    }
}
