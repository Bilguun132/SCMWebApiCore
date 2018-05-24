using Microsoft.AspNetCore.Mvc;
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

        public async Task<ActionResult> GetPlayer(int id)
        {
            try
            {
                await _GAMEContext.GameTeamPlayerRelationship.ToListAsync();
                await _GAMEContext.PlayerRole.ToListAsync();
                await _GAMEContext.Game.ToListAsync();
                await _GAMEContext.InventoryInformation.ToListAsync();
                Player player = await _GAMEContext.Player.SingleOrDefaultAsync(m => m.Id == id);
                Game game = player.GameTeamPlayerRelationship.FirstOrDefault().Game;
                var Inventory = new Object();
                if (player != null && game != null)
                {
                    GameTeamPlayerRelationship gameTeamPlayerRelationship = _GAMEContext.GameTeamPlayerRelationship.Where(m => m.PlayerId == player.Id).SingleOrDefault();
                    List<Results> results = await _GAMEContext.Results.Where(m => m.GameTeamPlayerRelationshipId == gameTeamPlayerRelationship.Id).ToListAsync();
                    List<double?> costs = new List<double?>();
                    List<int?> inv = new List<int?>();
                    foreach (Results r in results)
                    {
                        costs.Add(r.TotalCost);
                        inv.Add(r.Inventory);
                    }
                    Results latestResult = results.Where(p => p.Period == game.Period - 1).FirstOrDefault();
                    if (latestResult != null)
                    {
                        Inventory = new
                        {
                            latestResult.PreviousOrder,
                            game.Period,
                            CurrentInventory = latestResult.Inventory,
                            latestResult.IncomingInventory,
                            latestResult.TotalCost,
                            Costs = costs,
                            Inventories = inv
                        };
                        await _GAMEContext.SaveChangesAsync();
                    }
                    else
                    {
                        Inventory = new
                        {
                            PreviousOrder = 0,
                            game.Period,
                            CurrentInventory = player.Inventory.CurrentInventory,
                            IncomingInventory = 0,
                            TotalCost = 0,
                            Costs = costs,
                            Inventories = inv
                        };
                        await _GAMEContext.SaveChangesAsync();
                    }
                }
                return new JsonResult(Inventory);
            }
            catch (Exception ex)
            {
                return new JsonResult(ex);
            }
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
