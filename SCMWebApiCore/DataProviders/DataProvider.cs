using Microsoft.EntityFrameworkCore;
using SCMWebApiCore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SCMWebApiCore.DataProviders
{
    public class DataProvider : IDataProvider
    {
        private SCM_GAMEContext _GAMEContext;
        public DataProvider(SCM_GAMEContext _GAMEContext)
        {
            this._GAMEContext = _GAMEContext;
        }


        public async Task<Player> Join(string role, string connectionId)
        {
            await _GAMEContext.PlayerRole.ToListAsync();
            Player player = await _GAMEContext.Player.SingleOrDefaultAsync(m => m.PlayerRole.Role == role);
            if (player!= null)
            {
                player.IsAvailable = false;
                player.ConnectionId = connectionId;
            }
            await _GAMEContext.SaveChangesAsync();

            return player;
        }
    }
}
