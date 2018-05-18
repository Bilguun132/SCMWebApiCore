using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SCMWebApiCore.Models;

namespace SCMWebApiCore.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class GameController : Controller
    {

        private SCM_GAMEContext _GAMEContext;
        private readonly IHubContext<ChatHub> hubContext;
        public GameController(SCM_GAMEContext _GAMEContext, IHubContext<ChatHub> hub)
        {
            this._GAMEContext = _GAMEContext;
            hubContext = hub;
        }


        // GET: api/Game
        [HttpGet]
        public async Task<ActionResult> Get()
        {
            await _GAMEContext.Game.ToListAsync();
            var JsonString = await _GAMEContext.Player.ToListAsync();
            return new JsonResult(JsonString);
        }

        // GET: api/Game/5
        [HttpGet("{id}", Name = "Get")]
        public ActionResult Get(int id)
        {
            Game game = _GAMEContext.Game.Where(m => m.Id == id).FirstOrDefault();
            return new JsonResult(game);
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
