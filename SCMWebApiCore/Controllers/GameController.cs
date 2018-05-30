using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SCMWebApiCore.DataProviders;
using SCMWebApiCore.Models;

namespace SCMWebApiCore.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class GameController : Controller
    {

        private SCM_GAMEContext _GAMEContext;
        private readonly IHubContext<ChatHub> hubContext;
        private IDataProvider dataProvider;
        public GameController(SCM_GAMEContext _GAMEContext, IHubContext<ChatHub> hub, IDataProvider dataProvider)
        {
            this._GAMEContext = _GAMEContext;
            this.dataProvider = dataProvider;
            hubContext = hub;
        }


        // GET: api/Game
        [HttpGet]
        public async Task<ActionResult> Get()
        {
            await _GAMEContext.Game.ToListAsync();
            var JsonString = await _GAMEContext.Game.ToListAsync();
            return new JsonResult(JsonString);
        }

        // GET: api/Game/5
        [HttpGet("{id}", Name = "Get")]
        public ActionResult Get(int id)
        {
            Game game = _GAMEContext.Game.Where(m => m.Id == id).FirstOrDefault();
            return new JsonResult(game);
        }
        //GET: api/Game/GetPlayers/1 gameId
        [HttpGet]
        [Route("GetPlayers/{id}")]
        public async Task<ActionResult> GetPlayers(int id)
        {
            await _GAMEContext.PlayerRole.ToListAsync();
            List<Player> players = new List<Player>();
            List<GameTeamPlayerRelationship> gameTeamPlayerRelationships = await _GAMEContext.GameTeamPlayerRelationship.Where(m => m.GameId == id).ToListAsync();
            foreach (GameTeamPlayerRelationship gameteamRs in gameTeamPlayerRelationships)
            {
                Player player = _GAMEContext.Player.Where(m => m.Id == gameteamRs.PlayerId).FirstOrDefault();
                if (player == null) continue;
                players.Add(player);
            }
            return new JsonResult(players);
        }

        [HttpGet]
        [Route("GetTeamInfo/{id}")]
        public async Task<ActionResult> GetTeamInfo(int id)
        {
            await _GAMEContext.PlayerRole.ToListAsync();
            List<GameTeamPlayerRelationship> gameTeamPlayerRelationships = await _GAMEContext.GameTeamPlayerRelationship.Where(m => m.TeamId == id).ToListAsync();
            List<Object> list = new List<Object>();
            foreach (GameTeamPlayerRelationship gameteamRs in gameTeamPlayerRelationships)
            {
                Player player = _GAMEContext.Player.Where(m => m.Id == gameteamRs.PlayerId).FirstOrDefault();
                if (player == null) continue;
                list.Add(new
                {
                    player.PlayerRole.Role,
                    Results = await dataProvider.GetDecisions(player.Id)
                });
            }
            List<Object> TeamCosts = new List<Object>();
            await _GAMEContext.Game.ToListAsync();
            for (int i = 0; i <gameTeamPlayerRelationships.FirstOrDefault().Game.Period; i++)
            {
                double cumulativeCost = await dataProvider.GetWeeklyCost(id, i);
                TeamCosts.Add(new
                {
                    Period = i,
                    WeeklyCost = i == 0 ? 0 : (cumulativeCost / i)
                });
            }
            list.Add(TeamCosts);

            return new JsonResult(list);
        }

        [HttpGet]
        [Route("GetTeams")]
        public async Task<ActionResult> GetTeams()
        {
            await _GAMEContext.Team.ToListAsync();
            var JsonString = await _GAMEContext.Team.ToListAsync();
            return new JsonResult(JsonString);

        }


        [HttpPost]
        [Route("UpdateDemandData")]
        public async Task UpdateDemandData([FromBody] Game game) 
        {
            Game existingGame = await _GAMEContext.Game.Where(m => m.Id == game.Id).FirstOrDefaultAsync();
            if (existingGame == null) return;
            existingGame.DemandInformation = game.DemandInformation;
            await _GAMEContext.SaveChangesAsync();
        }

        // POST: api/Game
        [HttpPost]
        [Route("Create")]
        public void Post()
        {

        }

        // PUT: api/Game/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE: api/ApiWithActions/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
