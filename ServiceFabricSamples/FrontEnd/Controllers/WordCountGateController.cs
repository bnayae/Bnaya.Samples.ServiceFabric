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

// https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-reverseproxy

namespace FrontEnd.Controllers
{
    [Route("api/[controller]")]
    public class WordCountGateController : Controller
    {
        private const string STATEFUL_URL = "http://localhost:19081/ServiceFabricSamples/StatefulWebApi/api/WordCount";

        private string BuildUrl(string key, string relativeUrl = null)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            int partition = key.ToLower()[0] - 'a';
            return BuildUrl(partition, relativeUrl);
        }

        private string BuildUrl(int partition, string relativeUrl = null)
        {
            partition = partition % 26;
            if (string.IsNullOrEmpty(relativeUrl))
                return $"{STATEFUL_URL}?PartitionKey={partition}&PartitionKind=Int64Range";
            else
                return $"{STATEFUL_URL}/{relativeUrl}?PartitionKey={partition}&PartitionKind=Int64Range";
        }   

        // GET api/wordcount
        [HttpGet]
        public async ValueTask<(string key, int count)[]> Get()
        {
            var query = Enumerable.Range(0, 26)
                            .Select(GetByPartition)
                            .Select(m => m.AsTask());
            IEnumerable<(string key, int count)[]> results =
                await Task.WhenAll(query);

            var merged = from e in results
                         from item in e ?? Enumerable.Empty<(string key, int count)>()
                         select item;
            var result = merged.ToArray();
            return result;
        }
        public async ValueTask<(string key, int count)[]> GetByPartition(int partition)
        {
            string url = BuildUrl(partition);
            using (var http = new HttpClient())
            {
                var response = await http.GetAsync(url);
                var json = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<(string key, int count)[]>(json);
                return data;
            }
        }

        // GET api/wordcount/a
        [HttpGet("{key}")]
        public async ValueTask<(string key, int count)> Get(string key)
        {
            string url = BuildUrl(key, key);
            using (var http = new HttpClient())
            {
                var response = await http.GetAsync(url);
                var json = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<(string key, int count)>(json);
                return data;
            }
        }


        // POST api/wordcount/a
#pragma warning disable SG0016 // Controller method is vulnerable to CSRF
        [HttpPost()]
        public async ValueTask<IActionResult> Post([FromBody]string word)
        {
            string url = BuildUrl(word);
            using (var http = new HttpClient())
            {
                var content = new StringContent(word, Encoding.UTF8, "application/json");
                var response = await http.PostAsync(url, content);
                if (response.IsSuccessStatusCode)
                    return Ok();
                else
                    return StatusCode((int)response.StatusCode, word);
            }
        }

        // PUT api/wordcount/a
        [HttpPut()]
        public async ValueTask<IActionResult> PutAsync([FromBody]string word)
        {
            //HttpClientExtensions.PostAsJsonAsync<string>(word)
            string url = BuildUrl(word);
            using (var http = new HttpClient())
            {
                var response = await http.PutAsJsonAsync(url, word);
                //var content = new ObjectContent<string>(word, new JsonMediaTypeFormatter(), "application/json");
                //var content = new ObjectContent(word, Encoding.UTF8, "application/json");
                //var response = await http.PutAsync(url, content);
                if (response.IsSuccessStatusCode)
                    return Ok();
                else
                    return StatusCode((int)response.StatusCode, word);
            }
        }

        // DELETE api/wordcount/a
        [HttpDelete("{word}")]
        public async ValueTask<IActionResult> DeleteAsync(string word)
        {
            string url = BuildUrl(word, word);
            using (var http = new HttpClient())
            {
                var response = await http.DeleteAsync(url);
                if (response.IsSuccessStatusCode)
                    return Ok();
                else
                    return StatusCode((int)response.StatusCode, word);
            }
        }
#pragma warning restore SG0016 // Controller method is vulnerable to CSRF
    }
}
