using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SCMWebApiCore.Models;

namespace SCMWebApiCore.DataProviders
{
    public class DataProvider : IDataProvider
    {
        private SCM_GAMEContext _GAMEContext;
        private enum Role { Retailer = 1, Wholesaler, Distributor, Factory, Facilitator }
        public DataProvider(SCM_GAMEContext _GAMEContext)
        {
            this._GAMEContext = _GAMEContext;
        }

        async Task<object> IDataProvider.GetPlayer(int id)
        {

            try
            {
                Player player = await _GAMEContext.Player.FirstOrDefaultAsync(m => m.Id == id);
                Team team = null;
                if (player.GameTeamPlayerRelationship.Count != 0) team = player.GameTeamPlayerRelationship.FirstOrDefault().Team;

                if (await ShouldUpdateResults(teamId: team.Id)) await UpdateResults(team.Id, team.CurrentPeriod);

                var Inventory = new Object();
                if (player != null && team != null)
                {
                    GameTeamPlayerRelationship gameTeamPlayerRelationship = _GAMEContext.GameTeamPlayerRelationship.Where(m => m.PlayerId == player.Id).FirstOrDefault();
                    List<Results> results = await _GAMEContext.Results.Where(m => m.GameTeamPlayerRelationshipId == gameTeamPlayerRelationship.Id).ToListAsync();
                    List<double?> costs = new List<double?>();
                    List<int?> inv = new List<int?>();
                    foreach (Results r in results)
                    {
                        costs.Add(r.TotalCost);

                        inv.Add(r.Inventory);
                    }
                    Results latestResult = results.FirstOrDefault(p => p.Period == team.CurrentPeriod - 1);
                    if (latestResult != null)
                    {
                        Inventory = new
                        {
                            latestResult.PreviousOrder,
                            CurrentPeriod = team.CurrentPeriod - 1,
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
                            CurrentPeriod = team.CurrentPeriod - 1,
                            player.Inventory.CurrentInventory,
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

        public async Task<Boolean> ShouldUpdateResults(int? teamId)
        {
            bool success = true;
            List<GameTeamPlayerRelationship> gameTeamPlayerRelationships = _GAMEContext.GameTeamPlayerRelationship.Where(m => m.TeamId == teamId).ToList();
            foreach (GameTeamPlayerRelationship p in gameTeamPlayerRelationships)
            {
                if (p.Player.HasMadeDecision == false)
                {
                    success = false;
                    break;
                }
            }

            if (success)
            {
                Parallel.ForEach(gameTeamPlayerRelationships, p =>
                {
                    p.Player.HasMadeDecision = false;
                });

                await _GAMEContext.SaveChangesAsync();

            }

            return success;
        }

        public async Task UpdateResults(int? teamId, int? period)
        {
            List<GameTeamPlayerRelationship> gameTeamPlayerRelationships = _GAMEContext.GameTeamPlayerRelationship.Where(m => m.TeamId == teamId).ToList();
            List<PlayerTransactions> playerTransactions = _GAMEContext.PlayerTransactions.Where(m => m.TeamId == teamId).ToList();
            foreach (GameTeamPlayerRelationship rs in gameTeamPlayerRelationships)
            {
                Player player = rs.Player;
                int newOrder = 0;
                int sentOrder = 0;
                PlayerTransactions outgoingPlayerTransaction = null;
                PlayerTransactions incomingPlayerTransaction = null;
                switch (player.PlayerRoleId)
                {
                    case (int)Role.Retailer:
                        newOrder = (int)rs.Team.CurrentOrder;
                        var currentInventory = rs.Player.Inventory.CurrentInventory;
                        incomingPlayerTransaction = playerTransactions.FirstOrDefault(m => m.OrderMadeFrom == player.Id && m.OrderReceivePeriod == period);
                        rs.Player.Inventory.NewOrder = newOrder;
                        if (rs.Player.Inventory.CurrentInventory < 0)
                        {
                            sentOrder = 0;
                        }
                        else
                        {
                            if (rs.Player.Inventory.CurrentInventory > newOrder)
                            {
                                sentOrder = newOrder;
                            }
                            else
                            {
                                sentOrder = rs.Player.Inventory.CurrentInventory;
                            }
                        }
                        break;
                    case (int)Role.Wholesaler:
                        outgoingPlayerTransaction = playerTransactions.FirstOrDefault(m => m.OrderMadeTo == player.Id && m.OrderMadePeriod == period);
                        newOrder = outgoingPlayerTransaction.OrderQty;
                        if (rs.Player.Inventory.CurrentInventory < 0)
                        {
                            sentOrder = 0;
                        }
                        else
                        {
                            if (rs.Player.Inventory.CurrentInventory > newOrder)
                            {
                                sentOrder = newOrder;
                            }
                            else
                            {
                                sentOrder = rs.Player.Inventory.CurrentInventory;
                            }
                        }
                        outgoingPlayerTransaction.SentQty = sentOrder;
                        incomingPlayerTransaction = playerTransactions.FirstOrDefault(m => m.OrderMadeFrom == player.Id && m.OrderReceivePeriod == period);
                        break;
                    case (int)Role.Distributor:
                        outgoingPlayerTransaction = playerTransactions.FirstOrDefault(m => m.OrderMadeTo == player.Id && m.OrderMadePeriod == period);
                        newOrder = outgoingPlayerTransaction.OrderQty;
                        if (rs.Player.Inventory.CurrentInventory < 0)
                        {
                            sentOrder = 0;
                        }
                        else
                        {
                            if (rs.Player.Inventory.CurrentInventory > newOrder)
                            {
                                sentOrder = newOrder;
                            }
                            else
                            {
                                sentOrder = rs.Player.Inventory.CurrentInventory;
                            }
                        }
                        outgoingPlayerTransaction.SentQty = sentOrder;
                        incomingPlayerTransaction = playerTransactions.FirstOrDefault(m => m.OrderMadeFrom == player.Id && m.OrderReceivePeriod == period);
                        break;
                    case (int)Role.Factory:
                        outgoingPlayerTransaction = playerTransactions.FirstOrDefault(m => m.OrderMadeTo == player.Id && m.OrderMadePeriod == period);
                        var transaction = playerTransactions.FirstOrDefault(m => m.OrderMadeFrom == player.Id && m.OrderMadePeriod == period);
                        newOrder = outgoingPlayerTransaction.OrderQty;
                        if (rs.Player.Inventory.CurrentInventory < 0)
                        {
                            sentOrder = 0;
                        }
                        else
                        {
                            if (rs.Player.Inventory.CurrentInventory > newOrder)
                            {
                                sentOrder = newOrder;
                            }
                            else
                            {
                                sentOrder = rs.Player.Inventory.CurrentInventory;
                            }
                        }
                        outgoingPlayerTransaction.SentQty = sentOrder;
                        transaction.SentQty = newOrder;
                        incomingPlayerTransaction = playerTransactions.FirstOrDefault(m => m.OrderMadeFrom == player.Id && m.OrderReceivePeriod == period);
                        break;
                    default:
                        break;
                }
                rs.Player.Inventory.CurrentInventory -= newOrder;
                rs.Player.Inventory.IncomingInventory = (period<=4) ? 4 : incomingPlayerTransaction != null ? incomingPlayerTransaction.OrderQty : 0;
                rs.Player.Inventory.CurrentInventory += (int)rs.Player.Inventory.IncomingInventory;
                rs.Player.Inventory.NewOrder = newOrder;
                double cost = player.Inventory.CurrentInventory > 0 ? player.Inventory.CurrentInventory / 2 : Math.Abs(player.Inventory.CurrentInventory);
                player.Inventory.TotalCost += cost;
                PlayerTransactions orderedTransaction = playerTransactions.FirstOrDefault(m => m.OrderMadeFrom == player.Id && m.OrderMadePeriod == period);
                PlayerTransactions requestedTransactionToPlayer = playerTransactions.FirstOrDefault(m => m.OrderMadeTo == player.Id && m.OrderMadePeriod == period);
                int totalNeeded = 0;
                if (rs.Player.Inventory.CurrentInventory < 0) totalNeeded += Math.Abs(rs.Player.Inventory.CurrentInventory);
                if (requestedTransactionToPlayer != null) totalNeeded += requestedTransactionToPlayer.OrderQty;
                Results results = new Results
                {
                    GameTeamPlayerRelationshipId = rs.Id,
                    Inventory = rs.Player.Inventory.CurrentInventory,
                    IncomingInventory = rs.Player.Inventory.IncomingInventory,
                    TotalCost = player.Inventory.TotalCost,
                    Period = period,
                    PreviousOrder = player.Inventory.NewOrder,
                    OrderQty = orderedTransaction.OrderQty,
                    SentQty = sentOrder,
                    TotalNeeded = totalNeeded
                };
                _GAMEContext.Results.Add(results);
                await _GAMEContext.SaveChangesAsync();
            }

            Game game = gameTeamPlayerRelationships.FirstOrDefault().Game;
            Team team = gameTeamPlayerRelationships.FirstOrDefault().Team;

            List<int> demandData = Newtonsoft.Json.JsonConvert.DeserializeObject<List<int>>(game.DemandInformation);
            team.CurrentOrder = demandData[team.CurrentPeriod];
            team.CurrentPeriod += 1;
            await _GAMEContext.SaveChangesAsync();
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
            Player player = await _GAMEContext.Player.FirstOrDefaultAsync(m => m.PlayerRole.Role == role);
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
            await Task.Run(async () =>
            {
                try
                {
                    await _GAMEContext.PlayerRole.ToListAsync();
                    await _GAMEContext.GameTeamPlayerRelationship.ToListAsync();
                    var fromAddress = new MailAddress("isemlearning@gmail.com", "ISE Learning");
                    var toAddress = new MailAddress(player.Email, player.FirstName);
                    Game game = await _GAMEContext.Game.Where(m => m.Id == player.GameTeamPlayerRelationship.FirstOrDefault().GameId).FirstOrDefaultAsync();
                    var gameUrl = (game != null && game.GameUrl != "" ? game.GameUrl : "http://172.19.76.55:5000");
                    const string fromPassword = "ISE_Admin@12345";
                    string subject = "Welcome to the SCM Game, " + player.FirstName;
                    string body = String.Format("Thank you for signing up to play the game. {0} Please use these credentials to login " +
                                                "{1} Username: " +
                                                "{2} Password: " +
                                                "{3} You are playing as: " +
                                                "{4} Please access the game at {5}", Environment.NewLine, Environment.NewLine, 
                                                player.Username + Environment.NewLine, player.Password + Environment.NewLine, player.PlayerRole.Role + Environment.NewLine, gameUrl);

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
                var totalCost = _GAMEContext.Results.Where(m => m.GameTeamPlayerRelationshipId == gameteamRs.Id && m.Period == period).Sum(m => m.TotalCost);
                cost += (double)(totalCost);

            }
            return cost;
        }
    }
}
