using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using System.Threading;

namespace StatefulWebApi.Controllers
{
    [Route("api/[controller]")]
    public class WordCountController : Controller
    {
        private readonly IReliableStateManager _stateManager;

        public WordCountController(IReliableStateManager stateManager)
        {
            _stateManager = stateManager;
        }

        // GET api/wordcount
        [HttpGet]
        public async ValueTask<IEnumerable<(string key, int count)>> Get()
        {
            var result = new List<(string key, int count)>();
            var countState = await _stateManager.GetOrAddAsync<IReliableDictionary<string, int>>("Counting").ConfigureAwait(false);
            using (ITransaction tx = _stateManager.CreateTransaction())
            {
                IAsyncEnumerable<KeyValuePair<string, int>> query = await countState.CreateEnumerableAsync(tx).ConfigureAwait(false);
                var enumerator = query.GetAsyncEnumerator();
                while (await enumerator.MoveNextAsync(CancellationToken.None))
                {
                    var cur = enumerator.Current;
                    result.Add((cur.Key, cur.Value));
                }

                await tx.CommitAsync().ConfigureAwait(false);
            }
            return result;
        }

        // GET api/wordcount/a
        [HttpGet("{key}")]
        public async ValueTask<(string key, int count)> Get(string key)
        {
            var countState = await _stateManager.GetOrAddAsync<IReliableDictionary<string, int>>("Counting").ConfigureAwait(false);
            using (ITransaction tx = _stateManager.CreateTransaction())
            {
                var response = await countState.TryGetValueAsync(tx, key, LockMode.Default).ConfigureAwait(false);
                if (response.HasValue)
                    return (key, response.Value);

                await tx.CommitAsync().ConfigureAwait(false);
                return (key, response.HasValue ? response.Value : 0);
            }
        }


        // POST api/wordcount/a
#pragma warning disable SG0016 // Controller method is vulnerable to CSRF
        [HttpPost()]
        public async ValueTask<IActionResult> Post([FromBody]string word)
        {
            var countState = await _stateManager.GetOrAddAsync<IReliableDictionary<string, int>>("Counting").ConfigureAwait(false);
            using (ITransaction tx = _stateManager.CreateTransaction())
            {
                if (!await countState.TryAddAsync(tx, word.ToLower(), 1).ConfigureAwait(false))
                    return new BadRequestObjectResult("Already exists (use put verb)");

                await tx.CommitAsync().ConfigureAwait(false);
            }
            return Ok();
        }

        // PUT api/wordcount/a
        [HttpPut()]
        public async ValueTask<IActionResult> PutAsync([FromBody]string word)
        {
            var countState = await _stateManager.GetOrAddAsync<IReliableDictionary<string, int>>("Counting").ConfigureAwait(false);
            using (ITransaction tx = _stateManager.CreateTransaction())
            {
                await countState.AddOrUpdateAsync(tx, word.ToLower(), 1, (k, v) => v + 1).ConfigureAwait(false);

                await tx.CommitAsync().ConfigureAwait(false);
            }
            return Ok();
        }

        // DELETE api/wordcount/a
        [HttpDelete("{word}")]
        public async ValueTask<IActionResult> DeleteAsync(string word)
        {
            var countState = await _stateManager.GetOrAddAsync<IReliableDictionary<string, int>>("Counting").ConfigureAwait(false);
            using (ITransaction tx = _stateManager.CreateTransaction())
            {
                await countState.TryRemoveAsync(tx, word.ToLower()).ConfigureAwait(false);

                await tx.CommitAsync().ConfigureAwait(false);
            }
            return Ok();
        }
#pragma warning restore SG0016 // Controller method is vulnerable to CSRF
    }
}
