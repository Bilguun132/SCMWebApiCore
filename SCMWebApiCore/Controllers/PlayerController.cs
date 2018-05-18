﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SCMWebApiCore.Models;
using System.Net.Mail;
using System.Net;


namespace SCMWebApiCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlayerController : ControllerBase
    {
        public enum Role { Retailer = 1, Wholesaler, Distributor, Factory }
        private SCM_GAMEContext _GAMEContext;
        private readonly IHubContext<ChatHub> hubContext;
        public PlayerController(SCM_GAMEContext _GAMEContext, IHubContext<ChatHub> hub)
        {
            this._GAMEContext = _GAMEContext;
            hubContext = hub;
        }

        // GET api/values
        [HttpGet]
        public async Task<ActionResult> Get()
        {
            await _GAMEContext.PlayerRole.ToListAsync();
            var JsonString = await _GAMEContext.Player.ToListAsync();
            return new JsonResult(JsonString);
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public async Task<ActionResult> Get(int id)
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
                    Results latestResult = results.Where(p=> p.Period == game.Period).FirstOrDefault();
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
                }
                return new JsonResult(Inventory);
            }
            catch (Exception ex)
            {
                return new JsonResult(ex);
            }
        }

        [HttpPost]
        [Route("Login")]
        public async Task<ActionResult> Login([FromBody] LoginClass login)
        {
            await _GAMEContext.Player.ToListAsync();
            await _GAMEContext.PlayerRole.ToListAsync();
            Player player = _GAMEContext.Player.FirstOrDefault(m => m.Username == login.UserName.ToLower() && m.Password == login.Password);
            if (player == null) return NotFound(new JsonResult("No such user") { StatusCode = 404 });

            return new JsonResult(player);

        }
        // POST api/values
        [HttpPost]
        [Route("AddPlayers")]
        public async Task AddPlayers([FromBody] List<PlayerClass> players)
        {
            try
            {
                var GroupedPlayer = players.GroupBy(u => u.Team).Select(grp => grp.ToList()).ToList();
                Game game = new Game
                {
                    Name = "New Game",
                    Period = 1,
                    MaxPeriod = 40,
                    DeliveryDelay = 2,
                    CurrentOrder = 5
                };
                _GAMEContext.Game.Add(game);
                await _GAMEContext.SaveChangesAsync();
                foreach (var group in GroupedPlayer)
                {
                    Team team = new Team
                    {
                        Name = "New Team"
                    };
                    _GAMEContext.Team.Add(team);
                    await _GAMEContext.SaveChangesAsync();

                    foreach (PlayerClass newPlayer in group)
                    {
                        Player player = new Player
                        {
                            FirstName = newPlayer.FirstName,
                            LastName = newPlayer.LastName,
                            Email = newPlayer.Email,
                            HasMadeDecision = false
                        };

                        InventoryInformation inventoryInformation = new InventoryInformation
                        {
                            CurrentInventory = 10,
                            Backlogs = 0,
                            IncomingInventory = 0,
                            TotalCost = 0,
                            NewOrder = 0
                        };

                        switch (newPlayer.Role)
                        {
                            case "Retailer":
                                player.PlayerRoleId = (int)Role.Retailer;
                                break;
                            case "Distributor":
                                player.PlayerRoleId = (int)Role.Distributor;
                                break;
                            case "Wholesaler":
                                player.PlayerRoleId = (int)Role.Wholesaler;
                                break;
                            case "Factory":
                                player.PlayerRoleId = (int)Role.Factory;
                                break;
                            default:
                                break;
                        }

                        var userName = "";
                        userName = (newPlayer.FirstName.Length > 3 ? userName += newPlayer.FirstName.Substring(0, 3) : userName += newPlayer.FirstName[0]);
                        userName = (newPlayer.LastName.Length > 3 ? userName += newPlayer.LastName.Substring(0, 3) : userName += newPlayer.LastName[0]);
                        player.Username = userName.ToLower();
                        player.Password = userName.ToLower();
                        _GAMEContext.InventoryInformation.Add(inventoryInformation);
                        _GAMEContext.Player.Add(player);
                        await _GAMEContext.SaveChangesAsync();
                        player.InventoryId = inventoryInformation.Id;
                        GameTeamPlayerRelationship gameTeamPlayerRelationship = new GameTeamPlayerRelationship
                        {
                            GameId = game.Id,
                            TeamId = team.Id,
                            PlayerId = player.Id
                        };
                        _GAMEContext.GameTeamPlayerRelationship.Add(gameTeamPlayerRelationship);
                        await _GAMEContext.SaveChangesAsync();
                        Results results = new Results
                        {
                            GameTeamPlayerRelationshipId = gameTeamPlayerRelationship.Id,
                            PreviousOrder = 0,
                            Inventory = player.Inventory.CurrentInventory,
                            IncomingInventory = 0,
                            TotalCost = 0,
                            Period = 1,
                            OrderQty = 0,
                            SentQty = 0
                        };
                        _GAMEContext.Results.Add(results);
                        //  SendEmail(player);
                    }
                }
                await _GAMEContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {

            }

        }

        public void SendEmail(Player player)
        {
            try
            {
                var fromAddress = new MailAddress("isemlearning@gmail.com", "ISE Learning");
                var toAddress = new MailAddress(player.Email, player.FirstName);
                const string fromPassword = "ISE_Admin@12345";
                string subject = "Welcome to the SCM Game, " + player.FirstName;
                string body = String.Format("Thank you for signing up to play the game. {0} Please use these credentials to login {1} Username: {2} Password: {3} Please access the game at http://172.16.124.17:3001", Environment.NewLine, Environment.NewLine, player.Username + Environment.NewLine, player.Password + Environment.NewLine);

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

        }

        [HttpPost]
        [Route("MakeOrder")]
        public async Task MakeOrder([FromBody] OrderClass orderClass)
        {
            List<Player> players = await _GAMEContext.Player.ToListAsync();
            Player player = players.SingleOrDefault(m => m.Id == orderClass.PlayerId);
            await _GAMEContext.InventoryInformation.ToListAsync();
            await _GAMEContext.Game.ToListAsync();
            await _GAMEContext.GameTeamPlayerRelationship.ToListAsync();
            if (player != null)
            {
                InventoryInformation inventoryInformation = player.Inventory;
            }

            int OrderMadeTo = 0;
            switch (player.PlayerRoleId)
            {
                case (int)Role.Retailer:
                    OrderMadeTo = (int)_GAMEContext.GameTeamPlayerRelationship.Where(m => m.TeamId == player.GameTeamPlayerRelationship.FirstOrDefault().TeamId && m.Player.PlayerRoleId == (int)Role.Wholesaler).FirstOrDefault().PlayerId;
                    break;
                case (int)Role.Wholesaler:
                    OrderMadeTo = (int)_GAMEContext.GameTeamPlayerRelationship.Where(m => m.TeamId == player.GameTeamPlayerRelationship.FirstOrDefault().TeamId && m.Player.PlayerRoleId == (int)Role.Distributor).FirstOrDefault().PlayerId;
                    break;
                case (int)Role.Distributor:
                    OrderMadeTo = (int)_GAMEContext.GameTeamPlayerRelationship.Where(m => m.TeamId == player.GameTeamPlayerRelationship.FirstOrDefault().TeamId && m.Player.PlayerRoleId == (int)Role.Factory).FirstOrDefault().PlayerId;
                    break;
                case (int)Role.Factory:
                    OrderMadeTo = 0;
                    break;
                default:
                    break;
            }

            GameTeamPlayerRelationship gameTeamPlayerRelationship = await _GAMEContext.GameTeamPlayerRelationship.SingleOrDefaultAsync(m => m.PlayerId == player.Id);
            PlayerTransactions playerTransactions = new PlayerTransactions
            {
                OrderMadeFrom = player.Id,
                OrderMadeTo = OrderMadeTo,
                OrderQty = orderClass.OrderQty,
                GameId = gameTeamPlayerRelationship.GameId,
                TeamId = gameTeamPlayerRelationship.TeamId,
                OrderMadePeriod = gameTeamPlayerRelationship.Game.Period,
                OrderReceivePeriod = gameTeamPlayerRelationship.Game.Period + 2,
            };
            player.HasMadeDecision = true;
            _GAMEContext.PlayerTransactions.Add(playerTransactions);

            bool success = true;
            List<GameTeamPlayerRelationship> gameTeamPlayerRelationships = _GAMEContext.GameTeamPlayerRelationship.Where(m => m.TeamId == gameTeamPlayerRelationship.TeamId).ToList();
            foreach (GameTeamPlayerRelationship p in gameTeamPlayerRelationships)
            {
                if (p.Player.HasMadeDecision == false) success = false;
            }

            if (success)
            {
                foreach (GameTeamPlayerRelationship p in gameTeamPlayerRelationships)
                {
                    p.Player.HasMadeDecision = false;
                }
                try
                {
                    await _GAMEContext.SaveChangesAsync();
                    await UpdateResults(gameTeamPlayerRelationship.TeamId, gameTeamPlayerRelationship.Game.Period + 1);
                }
                catch (Exception ex)
                {

                }

                //    await hubContext.Clients.All.SendAsync("ProgressGamePlay");
            }
            await _GAMEContext.SaveChangesAsync();
        }

        public async Task UpdateResults(int? teamId, int? period)
        {
            List<GameTeamPlayerRelationship> gameTeamPlayerRelationships = _GAMEContext.GameTeamPlayerRelationship.Where(m => m.TeamId == teamId).ToList();
            List<PlayerTransactions> playerTransactions = _GAMEContext.PlayerTransactions.Where(m => m.TeamId == teamId).ToList();
            foreach (GameTeamPlayerRelationship rs in gameTeamPlayerRelationships)
            {
                Player player = rs.Player;
                int newOrder;
                int sentOrder = 0;
                int madeOrder = 0;
                PlayerTransactions outgoingPlayerTransaction = null;
                PlayerTransactions incomingPlayerTransaction = null;
                switch (player.PlayerRoleId)
                {
                    case (int)Role.Retailer:
                        newOrder = (int)rs.Game.CurrentOrder;
                        var currentInventory = rs.Player.Inventory.CurrentInventory;
                        incomingPlayerTransaction = playerTransactions.Where(m => m.OrderMadeFrom == player.Id && m.OrderReceivePeriod == period).SingleOrDefault();
                        rs.Player.Inventory.IncomingInventory = incomingPlayerTransaction != null ? incomingPlayerTransaction.SentQty : 0;
                        madeOrder = incomingPlayerTransaction != null ? incomingPlayerTransaction.OrderQty : 0;
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
                        rs.Player.Inventory.CurrentInventory -= newOrder;
                        break;
                    case (int)Role.Wholesaler:
                        outgoingPlayerTransaction = playerTransactions.Where(m => m.OrderMadeTo == player.Id && m.OrderMadePeriod == period-1).SingleOrDefault();
                        newOrder = outgoingPlayerTransaction.OrderQty;
                        if (rs.Player.Inventory.CurrentInventory < 0)
                        {
                            sentOrder = 0;
                        }
                        else
                        {
                            if(rs.Player.Inventory.CurrentInventory > newOrder)
                            {
                                sentOrder = newOrder;
                            }
                            else
                            {
                                sentOrder = rs.Player.Inventory.CurrentInventory;
                            }
                        }
                        rs.Player.Inventory.CurrentInventory -= newOrder;
                        outgoingPlayerTransaction.SentQty = sentOrder;
                        incomingPlayerTransaction = playerTransactions.Where(m => m.OrderMadeFrom == player.Id && m.OrderReceivePeriod == period).SingleOrDefault();
                        madeOrder = incomingPlayerTransaction != null ? incomingPlayerTransaction.OrderQty : 0;
                        rs.Player.Inventory.IncomingInventory = incomingPlayerTransaction != null ? incomingPlayerTransaction.SentQty : 0;
                        rs.Player.Inventory.NewOrder = newOrder;
                        break;
                    case (int)Role.Distributor:
                        outgoingPlayerTransaction = playerTransactions.Where(m => m.OrderMadeTo == player.Id && m.OrderMadePeriod == period-1).SingleOrDefault();
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
                        rs.Player.Inventory.CurrentInventory -= newOrder;
                        outgoingPlayerTransaction.SentQty = sentOrder;
                        incomingPlayerTransaction = playerTransactions.Where(m => m.OrderMadeFrom == player.Id && m.OrderReceivePeriod == period).SingleOrDefault();
                        madeOrder = incomingPlayerTransaction != null ? incomingPlayerTransaction.OrderQty : 0;
                        rs.Player.Inventory.IncomingInventory = incomingPlayerTransaction != null ? incomingPlayerTransaction.SentQty : 0;
                        rs.Player.Inventory.NewOrder = newOrder;
                        break;
                    case (int)Role.Factory:
                        outgoingPlayerTransaction = playerTransactions.Where(m => m.OrderMadeTo == player.Id && m.OrderMadePeriod == period-1).SingleOrDefault();
                        var transaction = playerTransactions.Where(m => m.OrderMadeFrom == player.Id && m.OrderMadePeriod == period - 1).SingleOrDefault();
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
                        rs.Player.Inventory.CurrentInventory -= newOrder;
                        outgoingPlayerTransaction.SentQty = sentOrder;
                        transaction.SentQty = newOrder;
                        incomingPlayerTransaction = playerTransactions.Where(m => m.OrderMadeFrom == player.Id && m.OrderReceivePeriod == period).SingleOrDefault();
                        madeOrder = incomingPlayerTransaction != null ? incomingPlayerTransaction.OrderQty : 0;
                        rs.Player.Inventory.IncomingInventory = incomingPlayerTransaction != null ? incomingPlayerTransaction.OrderQty : 0;
                        rs.Player.Inventory.NewOrder = newOrder;
                        break;
                    default:
                        break;
                }
                double cost = player.Inventory.CurrentInventory > 0 ? player.Inventory.CurrentInventory : Math.Abs(player.Inventory.CurrentInventory * 2);
                player.Inventory.TotalCost += cost;
                PlayerTransactions orderedTransaction = playerTransactions.Where(m => m.OrderMadeFrom == player.Id && m.OrderMadePeriod == period-1).FirstOrDefault();
                Results results = new Results
                {
                    GameTeamPlayerRelationshipId = rs.Id,
                    Inventory = rs.Player.Inventory.CurrentInventory,
                    IncomingInventory = rs.Player.Inventory.IncomingInventory,
                    TotalCost = player.Inventory.TotalCost,
                    Period = period,
                    PreviousOrder = player.Inventory.NewOrder,
                    OrderQty = orderedTransaction.OrderQty,
                    SentQty = sentOrder

                };
                _GAMEContext.Results.Add(results);
                await _GAMEContext.SaveChangesAsync();
            }
            gameTeamPlayerRelationships.FirstOrDefault().Game.Period += 1;
            await _GAMEContext.SaveChangesAsync();
        }


        // PUT api/values/5
        [HttpGet]
        [Route("GetDecisions/{id}")]
        public async Task<ActionResult> GetDecisions(int id)
        {
            Player player = _GAMEContext.Player.Where(m => m.Id == id).FirstOrDefault();
            if (player != null)
            {
                GameTeamPlayerRelationship gameTeamPlayerRelationship = _GAMEContext.GameTeamPlayerRelationship.Where(m => m.PlayerId == player.Id).FirstOrDefault();
                if (gameTeamPlayerRelationship != null)
                {
                    List<Results> results = await _GAMEContext.Results.Where(m => m.GameTeamPlayerRelationshipId == gameTeamPlayerRelationship.Id).ToListAsync();
                    return new JsonResult(results);
                }
            }
            return this.NotFound("No player found");
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }

    public class LoginClass
    {
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}
