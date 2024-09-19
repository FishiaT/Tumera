using Microsoft.UI.Xaml.Shapes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TumeraAI.Main.Types;
using TumeraAI.Main.Utils;

namespace TumeraAI.Main.API
{
    class OAIWrapper
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
                //definitely not confusing at all
                var modelList = (JArray)res["data"];
                var realModellist = new List<string>();
                foreach (var model in modelList)
                {
                    resParam.Add("result", true);
                    realModellist.Add(((JValue)model["id"]).Value.ToString());
                    
                }
                resParam.Add("models", realModellist);
            }
            return resParam;
        }

        //public static async Task<Dictionary<string, object>> ChatCompletion(List<Message> messages, string model, string endpoint, string apiKey = "placeholder")
        //{
        //    Dictionary<string, object> resParam = new Dictionary<string, object>();
        //    Dictionary<string, object> requestData = new Dictionary<string, object>();
        //    requestData.Add("model", "tumera-model-placeholder");
        //    if (RuntimeConfig.Seed != -1) requestData.Add("seed", RuntimeConfig.Seed);
        //    if (RuntimeConfig.Temperature != 1) requestData.Add("temperature", RuntimeConfig.Temperature);
        //    if (RuntimeConfig.FrequencyPenalty != 0) requestData.Add("frequency_penalty", RuntimeConfig.FrequencyPenalty);
        //    if (RuntimeConfig.PresencePenalty != 0) requestData.Add("presence_penalty", RuntimeConfig.PresencePenalty);
        //    if (RuntimeConfig.MaxTokens != -1) requestData.Add("max_completion_tokens", RuntimeConfig.MaxTokens);
        //    List<Dictionary<string, string>> convoHistory = new List<Dictionary<string, string>>();
        //    if (!string.IsNullOrEmpty(RuntimeConfig.SystemPrompt))
        //    {
        //        Dictionary<string, string> sysPromptContent = new Dictionary<string, string>();
        //        sysPromptContent.Add("content", RuntimeConfig.SystemPrompt);
        //        sysPromptContent.Add("role", "system");
        //        convoHistory.Add(sysPromptContent);
        //    }
        //    foreach (var message in messages)
        //    {
        //        Dictionary<string, string> messageContent = new Dictionary<string, string>();
        //        messageContent.Add("content", message.Content);
        //        switch (message.Role)
        //        {
        //            case Roles.USER:
        //                messageContent.Add("role", "user");
        //                break;
        //            case Roles.ASSISTANT:
        //                messageContent.Add("role", "assistant");
        //                break;
        //            case Roles.SYSTEM:
        //                messageContent.Add("role", "system");
        //                break;
        //        }
        //        convoHistory.Add(messageContent);
        //    }
        //    requestData.Add("messages", convoHistory);
        //    var response = await client.PostAsync(Endpoints.ChatCompletion, new StringContent(JsonConvert.SerializeObject(requestData, Formatting.Indented), Encoding.UTF8, "application/json"));
        //    var content = await response.Content.ReadAsStringAsync();
        //    var res = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
        //    if (!res.ContainsKey("choices"))
        //    {
        //        resParam.Add("result", false);
        //    }
        //    else
        //    {
        //        resParam.Add("result", true);
        //        foreach (var i in (JArray)res["choices"])
        //        {
        //            var msgParam = i["message"];
        //            resParam.Add("response", (string)msgParam["content"]);
        //        }
        //    }
        //    return resParam;
        //}

        //public static IEnumerable<Dictionary<string, object>> ChatCompletionStream(List<Message> messages, string endpoint, string apiKey = "")
        //{
        //    Dictionary<string, object> resParam = new Dictionary<string, object>();
        //    RestClient client = new RestClient(endpoint);
        //    RestRequest request = new RestRequest(Endpoints.ChatCompletion, Method.Post);
        //    request.AddHeader("Content-Type", "application/json");
        //    if (!string.IsNullOrEmpty(apiKey))
        //    {
        //        request.AddHeader("Authorization", $"Bearer {apiKey}");
        //    }
        //    Dictionary<string, object> requestData = new Dictionary<string, object>();
        //    requestData.Add("model", "tumera-model-placeholder");
        //    if (RuntimeConfig.Seed != -1) requestData.Add("seed", RuntimeConfig.Seed);
        //    if (RuntimeConfig.Temperature != 1) requestData.Add("temperature", RuntimeConfig.Temperature);
        //    if (RuntimeConfig.FrequencyPenalty != 0) requestData.Add("frequency_penalty", RuntimeConfig.FrequencyPenalty);
        //    if (RuntimeConfig.PresencePenalty != 0) requestData.Add("presence_penalty", RuntimeConfig.PresencePenalty);
        //    if (RuntimeConfig.MaxTokens != -1) requestData.Add("max_completion_tokens", RuntimeConfig.MaxTokens);
        //    requestData.Add("stream", true);
        //    List<Dictionary<string, string>> convoHistory = new List<Dictionary<string, string>>();
        //    if (!string.IsNullOrEmpty(RuntimeConfig.SystemPrompt))
        //    {
        //        Dictionary<string, string> sysPromptContent = new Dictionary<string, string>();
        //        sysPromptContent.Add("content", RuntimeConfig.SystemPrompt);
        //        sysPromptContent.Add("role", "system");
        //        convoHistory.Add(sysPromptContent);
        //    }
        //    foreach (var message in messages)
        //    {
        //        Dictionary<string, string> messageContent = new Dictionary<string, string>();
        //        messageContent.Add("content", message.Content);
        //        switch (message.Role)
        //        {
        //            case Roles.USER:
        //                messageContent.Add("role", "user");
        //                break;
        //            case Roles.ASSISTANT:
        //                messageContent.Add("role", "assistant");
        //                break;
        //            case Roles.SYSTEM:
        //                messageContent.Add("role", "system");
        //                break;
        //        }
        //        convoHistory.Add(messageContent);
        //    }
        //    requestData.Add("messages", convoHistory);
        //    request.AddJsonBody(requestData);
        //}
    }
}
