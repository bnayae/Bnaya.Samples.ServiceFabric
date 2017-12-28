using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using System.Threading;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text;
using System.Net.Http.Headers;
using System.Net.Http.Formatting;
using Microsoft.ServiceFabric.Actors;
using BackendActors.Interfaces;
using Microsoft.ServiceFabric.Actors.Client;

namespace FrontEnd.Controllers
{
    [Route("api/[controller]")]
    public class WordCountByActorController : Controller
    {

        // GET api/wordcount
        [HttpGet]
        public async ValueTask<(string key, int count)[]> Get()
        {
            throw new NotImplementedException();
        }


        // GET api/wordcount/a
        [HttpGet("{key}")]
        public async ValueTask<(string key, int count)> Get(string key)
        {
            var id = new ActorId(key);
            IBackendActor instance = ActorProxy.Create<IBackendActor>(id);
            int count = await instance.GetCountAsync(CancellationToken.None);
            return (key, count);
        }


        // POST api/wordcount/a
#pragma warning disable SG0016 // Controller method is vulnerable to CSRF
        [HttpPost()]
        public async ValueTask<IActionResult> Post([FromBody]string word)
        {
            var id = new ActorId(word);
            IBackendActor instance = ActorProxy.Create<IBackendActor>(id);
            await instance.AddOrUpdateAsync(CancellationToken.None);
            return Ok();
        }

        // PUT api/wordcount/a
        [HttpPut()]
        public async ValueTask<IActionResult> PutAsync([FromBody]string word)
        {
            var id = new ActorId(word);
            IBackendActor instance = ActorProxy.Create<IBackendActor>(id);
            await instance.AddOrUpdateAsync(CancellationToken.None);
            return Ok();
        }

        // DELETE api/wordcount/a
        [HttpDelete("{word}")]
        public async ValueTask<IActionResult> DeleteAsync(string word)
        {
            throw new NotImplementedException();
        }
#pragma warning restore SG0016 // Controller method is vulnerable to CSRF
    }
}
