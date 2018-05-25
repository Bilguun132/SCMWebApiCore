using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCMWebApiCore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Mail;
using System.Net;

namespace SCMWebApiCore.DataProviders
{
    public class DataProvider : IDataProvider
    {
        private SCM_GAMEContext _GAMEContext;
        public DataProvider(SCM_GAMEContext _GAMEContext)
        {
            this._GAMEContext = _GAMEContext;
        }

        public async Task<object> GetPlayer(int id)
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
                return (Inventory);
            }
            catch (Exception ex)
            {
                return (ex);
            }
        }

        public async Task<List<Results>> GetDecisions(int id)
        {
            Player player = _GAMEContext.Player.Where(m => m.Id == id).FirstOrDefault();
            if (player != null)
            {
                GameTeamPlayerRelationship gameTeamPlayerRelationship = _GAMEContext.GameTeamPlayerRelationship.Where(m => m.PlayerId == player.Id).FirstOrDefault();
                if (gameTeamPlayerRelationship != null)
                {
                    List<Results> results = await _GAMEContext.Results.Where(m => m.GameTeamPlayerRelationshipId == gameTeamPlayerRelationship.Id).ToListAsync();
                    return (results);
                }
            }
            return null;
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

        public async Task SendEmail(Player player)
        {
            await Task.Run(() =>
            {
                try
                {
                    var fromAddress = new MailAddress("isemlearning@gmail.com", "ISE Learning");
                    var toAddress = new MailAddress(player.Email, player.FirstName);
                    const string fromPassword = "ISE_Admin@12345";
                    string subject = "Welcome to the SCM Game, " + player.FirstName;
                    string body = String.Format("Thank you for signing up to play the game. {0} Please use these credentials to login {1} Username: {2} Password: {3} Please access the game at http://172.19.76.55:5000", Environment.NewLine, Environment.NewLine, player.Username + Environment.NewLine, player.Password + Environment.NewLine);

                    var smtp = new SmtpClient
                    {
                        Host = "smtp.gmail.com",
                        Port = 587,
                        EnableSsl = true,
                        DeliveryMethod = SmtpDeliveryMethod.Network,
                        Credentials = new NetworkCredential(fromAddress.Address.Trim(), fromPassword.Trim()),
                        Timeout = 20000
                    };
                    using (var message = new MailMessage(fromAddress, toAddress)
                    {
                        Subject = subject,
                        Body = body
                    })
                    {
                        smtp.Send(message);
                    }
                }
                catch (Exception ex)
                {
                    Console.Write(ex);
                }
            });
        }

        public async Task<double> GetWeeklyCost(int teamId, int period)
        {
            double cost = 0;
            List<GameTeamPlayerRelationship> gameTeamPlayerRelationships = await _GAMEContext.GameTeamPlayerRelationship.Where(m => m.TeamId == teamId).ToListAsync();
            foreach (GameTeamPlayerRelationship gameteamRs in gameTeamPlayerRelationships)
            {
                Results results = await _GAMEContext.Results.Where(m => m.GameTeamPlayerRelationshipId == gameteamRs.Id && m.Period == period).FirstOrDefaultAsync();
                if (results == null) continue;
                cost += (double)results.TotalCost;
            }
            return cost;
        }
    }
}
