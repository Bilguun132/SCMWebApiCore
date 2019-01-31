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
                        int demand = (i <= 6) ? 4 : 8;
                        demandData.Add(demand);
                    }
                    game = new Game
                    {
                        Name = addPlayerClass.GameName,
                        MaxPeriod = 40,
                        DeliveryDelay = 2,
                        FacilitatorId = addPlayerClass.FacilId,
                        DemandInformation = Newtonsoft.Json.JsonConvert.SerializeObject(demandData),
                        GameUrl = "http://winegame.edventist.com/"
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
                            CurrentOrder = 4,
                            CurrentPeriod = 2
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
                                CurrentInventory = 50,
                                Backlogs = 0,
                                IncomingInventory = 4,
                                TotalCost = 0,
                                NewOrder = 4
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
                                PreviousOrder = 4,
                                Inventory = player.Inventory.CurrentInventory,
                                IncomingInventory = 4,
                                TotalCost = 0,
                                Period = 2,
                                OrderQty = 4,
                                SentQty = 4
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

            if (await dataProvider.ShouldUpdateResults(gameTeamPlayerRelationship.TeamId)) await dataProvider.UpdateResults(gameTeamPlayerRelationship.TeamId, gameTeamPlayerRelationship.Team.CurrentPeriod); 

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
