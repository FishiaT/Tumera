using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TumeraAI.Main.Types;
using TumeraAI.Main.Utils;

namespace TumeraAI.Main.API
{
    class OpenAI
    {
        public static async Task<Dictionary<string, object>> CheckEndpointAndGetModelsAsync(string endpoint, string apiKey = "")
        {
            Dictionary<string, object> resParam = new Dictionary<string, object>();
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(endpoint);
            if (!string.IsNullOrEmpty(apiKey))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            }
            var response = await client.GetAsync(Endpoints.Models);
            var content = await response.Content.ReadAsStringAsync();
            var res = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
            if (!res.ContainsKey("data"))
            {
                resParam.Add("result", false);
            }
            else
            {
                var modelList = (JArray)res["data"];
                foreach (var model in modelList)
                {
                    resParam.Add("result", true);
                    resParam.Add("model", ((JValue)model["id"]).Value.ToString());
                }
            }
            return resParam;
        }

        //public static async Task<Dictionary<string, object>> Tokenize(string message, string endpoint, string apiKey = "")
        //{
        //    Dictionary<string, object> resParam = new Dictionary<string, object>();
        //    HttpClient client = new HttpClient();
        //    client.BaseAddress = new Uri(endpoint);
        //    if (!string.IsNullOrEmpty(apiKey))
        //    {
        //        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        //    }
        //    Dictionary<string, string> requestData = new Dictionary<string, string>
        //    {
        //        { "content", message }
        //    };
        //    var response = await client.PostAsync(Endpoints.Tokenize, new StringContent(JsonConvert.SerializeObject(requestData, Formatting.Indented), Encoding.UTF8, "application/json"));
        //    var content = await response.Content.ReadAsStringAsync();
        //    var res = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
        //    if (!res.ContainsKey("tokens"))
        //    {
        //        resParam.Add("result", false);
        //    }
        //    else
        //    {
        //        var token = (JArray)res["tokens"];
        //        resParam.Add("result", true);
        //        List<int> tokens = new List<int>();
        //        foreach (var i in token)
        //        {
        //            tokens.Add((int)i);
        //        }
        //        resParam.Add("tokens", tokens);
        //        resParam.Add("token_count", tokens.Count);
        //    }
        //    return resParam;
        //}

        public static async Task<Dictionary<string, object>> ChatCompletion(List<Message> messages, string endpoint, string apiKey = "")
        {
            Dictionary<string, object> resParam = new Dictionary<string, object>();
            HttpClient client = new HttpClient();
            client.Timeout = Timeout.InfiniteTimeSpan;
            client.BaseAddress = new Uri(endpoint);
            if (!string.IsNullOrEmpty(apiKey))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            }
            Dictionary<string, object> requestData = new Dictionary<string, object>();
            requestData.Add("model", "GPT6withChatGPTPlusMonthlySubscriptionReflection");
            if (RuntimeConfig.Seed != -1) requestData.Add("seed", RuntimeConfig.Seed);
            if (RuntimeConfig.Temperature != 1) requestData.Add("temperature", RuntimeConfig.Temperature);
            if (RuntimeConfig.FrequencyPenalty != 0) requestData.Add("frequency_penalty", RuntimeConfig.FrequencyPenalty);
            if (RuntimeConfig.PresencePenalty != 0) requestData.Add("presence_penalty", RuntimeConfig.PresencePenalty);
            if (RuntimeConfig.MaxTokens != -1) requestData.Add("max_completion_tokens", RuntimeConfig.MaxTokens);
            List<Dictionary<string, string>> convoHistory = new List<Dictionary<string, string>>();
            if (!string.IsNullOrEmpty(RuntimeConfig.SystemPrompt))
            {
                Dictionary<string, string> sysPromptContent = new Dictionary<string, string>();
                sysPromptContent.Add("content", RuntimeConfig.SystemPrompt);
                sysPromptContent.Add("role", "system");
                convoHistory.Add(sysPromptContent);
            }
            foreach (var message in messages)
            {
                Dictionary<string, string> messageContent = new Dictionary<string, string>();
                messageContent.Add("content", message.Content);
                switch (message.Role)
                {
                    case Roles.USER:
                        messageContent.Add("role", "user");
                        break;
                    case Roles.ASSISTANT:
                        messageContent.Add("role", "assistant");
                        break;
                    case Roles.SYSTEM:
                        messageContent.Add("role", "system");
                        break;
                }
                convoHistory.Add(messageContent);
            }
            requestData.Add("messages", convoHistory);
            var response = await client.PostAsync(Endpoints.ChatCompletion, new StringContent(JsonConvert.SerializeObject(requestData, Formatting.Indented), Encoding.UTF8, "application/json"));
            var content = await response.Content.ReadAsStringAsync();
            var res = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
            if (!res.ContainsKey("choices"))
            {
                resParam.Add("result", false);
            }
            else
            {
                resParam.Add("result", true);
                foreach (var i in (JArray)res["choices"])
                {
                    var msgParam = i["message"];
                    resParam.Add("response", (string)msgParam["content"]);
                }
            }
            return resParam;
        }
    }
}
