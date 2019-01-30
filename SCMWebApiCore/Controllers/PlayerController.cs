using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SCMWebApiCore.Models;


using SCMWebApiCore.DataProviders;

namespace SCMWebApiCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlayerController : ControllerBase
    {
        public enum Role { Retailer = 1, Wholesaler, Distributor, Factory, Facilitator }
        private SCM_GAMEContext _GAMEContext;
        private readonly IHubContext<ChatHub> hubContext;
        private IDataProvider dataProvider;
        public PlayerController(SCM_GAMEContext _GAMEContext, IHubContext<ChatHub> hub, IDataProvider dataProvider)

        {
            this._GAMEContext = _GAMEContext;
            this.dataProvider = dataProvider;
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
            return new JsonResult(await dataProvider.GetPlayer(id));
        }



        [HttpPost]
        [Route("Login")]
        public async Task<ActionResult> Login([FromBody] LoginClass login)
        {
            Player player = await _GAMEContext.Player.FirstOrDefaultAsync(m => m.Username == login.UserName.ToLower() && m.Password == login.Password);
            if (player == null) return NotFound(new JsonResult("No such user") { StatusCode = 404 });

            return new JsonResult(player);

        }

        [HttpPost]
        [Route("AdminLogin")]
        public async Task<ActionResult> AdminLogin([FromBody] LoginClass login)
        {
            await _GAMEContext.Player.ToListAsync();
            await _GAMEContext.PlayerRole.ToListAsync();
            Player player = _GAMEContext.Player.FirstOrDefault(m => m.Username == login.UserName.ToLower() && m.Password == login.Password && m.PlayerRoleId == (int)Role.Facilitator);
            if (player == null) return NotFound(new JsonResult("Facilitator Not Found") { StatusCode = 404 });

            return new JsonResult(player);
        }

        // POST api/values
        [HttpPost]
        [Route("AddPlayers")]
        public async Task AddPlayers([FromBody] AddPlayerClass addPlayerClass)
        {
            try
            {
                List<PlayerClass> players = addPlayerClass.Players;
                var GroupedPlayer = players.GroupBy(u => u.Team).Select(grp => grp.ToList()).ToList();
                Game game;
                if (addPlayerClass.GameId == null)
                {
                    List<int> demandData = new List<int>();
                    for (int i=1; i<=40; i++)
                    {
                        demandData.Add(5);
                    }
                    game = new Game
                    {
                        Name = addPlayerClass.GameName,
                        MaxPeriod = 40,
                        DeliveryDelay = 2,
                        FacilitatorId = addPlayerClass.FacilId,
                        DemandInformation = Newtonsoft.Json.JsonConvert.SerializeObject(demandData),
                        GameUrl = "https://winegame.edventist.com/"
                    };
                    _GAMEContext.Game.Add(game);
                    await _GAMEContext.SaveChangesAsync();
                }
                else
                {
                    game = await _GAMEContext.Game.Where(m => m.Id == (int)addPlayerClass.GameId).FirstOrDefaultAsync();
                }

                foreach (List<PlayerClass> group in GroupedPlayer)
                {
                    Player groupPlayer = await _GAMEContext.Player.Where(m => m.Id == group.FirstOrDefault().Id).FirstOrDefaultAsync();
                    await _GAMEContext.GameTeamPlayerRelationship.ToListAsync();
                    await _GAMEContext.Team.ToListAsync();
                    Team team = new Team();
                    if (groupPlayer != null)
                    {
                        team = groupPlayer.GameTeamPlayerRelationship.FirstOrDefault().Team;
                        team.Name = group.FirstOrDefault().Team;
                    }
                    else
                    {
                        team = new Team
                        {
                            Name = group.FirstOrDefault().Team,
                            CurrentOrder = 5,
                            CurrentPeriod = 1
                        };
                        _GAMEContext.Team.Add(team);
                    }
                
                    await _GAMEContext.SaveChangesAsync();

                    foreach (PlayerClass newPlayer in group)
                    {
                        Player player = await _GAMEContext.Player.Where(m => m.Id == newPlayer.Id).FirstOrDefaultAsync();
                        if (player == null)
                        {
                            player = new Player
                            {
                                FirstName = newPlayer.FirstName,
                                LastName = newPlayer.LastName,
                                Email = newPlayer.Email,
                                HasMadeDecision = false
                            };

                            InventoryInformation inventoryInformation = new InventoryInformation
                            {
                                CurrentInventory = 15,
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
                            userName = (newPlayer.FirstName.Length > 3 ? userName += newPlayer.FirstName.Substring(0, 3) : userName += newPlayer.FirstName);
                            userName = (newPlayer.LastName.Length > 3 ? userName += newPlayer.LastName.Substring(0, 3) : userName += newPlayer.LastName);
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
                                Period = 0,
                                OrderQty = 0,
                                SentQty = 0
                            };
                            _GAMEContext.Results.Add(results);
                            await dataProvider.SendEmail(player);
                        }
                        else
                        {
                            player.FirstName = newPlayer.FirstName;
                            player.LastName = newPlayer.LastName;
                            player.Email = newPlayer.Email;
                        }
                        await _GAMEContext.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Write(ex.ToString());
            }

        }

        [HttpGet]
        [Route("SendEmail/{playerId}")]
        public async Task<ActionResult> SendEmail(int playerId)
        {
            Player player = await _GAMEContext.Player.Where(m => m.Id == playerId).FirstOrDefaultAsync();
            if (player == null) return this.NotFound("No such player exists");
            await dataProvider.SendEmail(player);
            return this.Ok("Sent Email");
        }

        [HttpPost]
        [Route("MakeOrder")]
        public async Task MakeOrder([FromBody] OrderClass orderClass)
        {
            List<Player> players = await _GAMEContext.Player.ToListAsync();
            Player player = players.FirstOrDefault(m => m.Id == orderClass.PlayerId);
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

            GameTeamPlayerRelationship gameTeamPlayerRelationship = await _GAMEContext.GameTeamPlayerRelationship.FirstOrDefaultAsync(m => m.PlayerId == player.Id);
            int currentPeriod = gameTeamPlayerRelationship.Team.CurrentPeriod;
             PlayerTransactions playerTransactions = await _GAMEContext.PlayerTransactions.FirstOrDefaultAsync(m => m.OrderMadeFrom == player.Id && m.OrderMadePeriod == currentPeriod);
            if (playerTransactions == null)
            {
                playerTransactions = new PlayerTransactions();
                _GAMEContext.PlayerTransactions.Add(playerTransactions);
            }
            playerTransactions.OrderMadeFrom = player.Id;
            playerTransactions.OrderMadeTo = OrderMadeTo;
            playerTransactions.OrderQty = orderClass.OrderQty;
            playerTransactions.GameId = gameTeamPlayerRelationship.GameId;
            playerTransactions.TeamId = gameTeamPlayerRelationship.TeamId;
            playerTransactions.OrderMadePeriod = gameTeamPlayerRelationship.Team.CurrentPeriod;
            playerTransactions.OrderReceivePeriod = gameTeamPlayerRelationship.Team.CurrentPeriod + 2;

            player.HasMadeDecision = true;
            await _GAMEContext.SaveChangesAsync();
            bool success = true;
            List<GameTeamPlayerRelationship> gameTeamPlayerRelationships = _GAMEContext.GameTeamPlayerRelationship.Where(m => m.TeamId == gameTeamPlayerRelationship.TeamId).ToList();
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

                try
                {
                    await _GAMEContext.SaveChangesAsync();
                    await UpdateResults(gameTeamPlayerRelationship.TeamId, gameTeamPlayerRelationship.Team.CurrentPeriod);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }

            }
            else
            {
                await _GAMEContext.SaveChangesAsync();
            }
        }

        [NonAction]
        private async Task UpdateResults(int? teamId, int? period)
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
                        newOrder = (int)rs.Team.CurrentOrder;
                        var currentInventory = rs.Player.Inventory.CurrentInventory;
                        incomingPlayerTransaction = playerTransactions.FirstOrDefault(m => m.OrderMadeFrom == player.Id && m.OrderReceivePeriod == period);
                        rs.Player.Inventory.IncomingInventory = incomingPlayerTransaction != null ? incomingPlayerTransaction.SentQty : 0;
                        rs.Player.Inventory.CurrentInventory += (int)rs.Player.Inventory.IncomingInventory;
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
                        rs.Player.Inventory.NewOrder = newOrder;
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
                        rs.Player.Inventory.CurrentInventory -= newOrder;
                        outgoingPlayerTransaction.SentQty = sentOrder;
                        incomingPlayerTransaction = playerTransactions.FirstOrDefault(m => m.OrderMadeFrom == player.Id && m.OrderReceivePeriod == period);
                        madeOrder = incomingPlayerTransaction != null ? incomingPlayerTransaction.OrderQty : 0;
                        rs.Player.Inventory.IncomingInventory = incomingPlayerTransaction != null ? incomingPlayerTransaction.SentQty : 0;
                        rs.Player.Inventory.CurrentInventory += (int)rs.Player.Inventory.IncomingInventory;
                        rs.Player.Inventory.NewOrder = newOrder;
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
                        rs.Player.Inventory.CurrentInventory -= newOrder;
                        outgoingPlayerTransaction.SentQty = sentOrder;
                        incomingPlayerTransaction = playerTransactions.FirstOrDefault(m => m.OrderMadeFrom == player.Id && m.OrderReceivePeriod == period);
                        madeOrder = incomingPlayerTransaction != null ? incomingPlayerTransaction.OrderQty : 0;
                        rs.Player.Inventory.IncomingInventory = incomingPlayerTransaction != null ? incomingPlayerTransaction.SentQty : 0;
                        rs.Player.Inventory.CurrentInventory += (int)rs.Player.Inventory.IncomingInventory;
                        rs.Player.Inventory.NewOrder = newOrder;
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
                        rs.Player.Inventory.CurrentInventory -= newOrder;
                        outgoingPlayerTransaction.SentQty = sentOrder;
                        transaction.SentQty = newOrder;
                        incomingPlayerTransaction = playerTransactions.FirstOrDefault(m => m.OrderMadeFrom == player.Id && m.OrderReceivePeriod == period);
                        madeOrder = incomingPlayerTransaction != null ? incomingPlayerTransaction.OrderQty : 0;
                        rs.Player.Inventory.IncomingInventory = incomingPlayerTransaction != null ? incomingPlayerTransaction.OrderQty : 0;
                        rs.Player.Inventory.CurrentInventory += (int)rs.Player.Inventory.IncomingInventory;
                        rs.Player.Inventory.NewOrder = newOrder;
                        break;
                    default:
                        break;
                }
                double cost = player.Inventory.CurrentInventory > 0 ? player.Inventory.CurrentInventory : Math.Abs(player.Inventory.CurrentInventory * 2);
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


        [HttpGet]
        [Route("GetDecisions/{id}")]
        public async Task<ActionResult> GetDecisions(int id)
        {
            List<Results> results = await dataProvider.GetDecisions(id);

            if (results != null) return new JsonResult(results);

            return this.NotFound("No player found");
        }

        [HttpGet]
        [Route("GetCompetitorDecisions/{id}")]
        public async Task<ActionResult> GetCompetitorDecisions(int id)
        {
            Dictionary<int, List<Results>> dict = new Dictionary<int, List<Results>>();

            Player player = _GAMEContext.Player.Where(m => m.Id == id).FirstOrDefault();
            List<Player> listOfPlayers = await _GAMEContext.Player.Where(m => m.PlayerRoleId == player.PlayerRoleId).ToListAsync();
            foreach (Player p in listOfPlayers) {
                GameTeamPlayerRelationship gameTeamPlayerRelationship = _GAMEContext.GameTeamPlayerRelationship.Where(m => m.PlayerId == p.Id).FirstOrDefault();
                if (gameTeamPlayerRelationship != null)
                {
                    List<Results> results = await _GAMEContext.Results.Where(m => m.GameTeamPlayerRelationshipId == gameTeamPlayerRelationship.Id).ToListAsync();
                    dict[p.Id] = results;
                }
            }

            return new JsonResult(dict);

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
