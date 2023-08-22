using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Requests;
using Elastic.Transport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MCollector.Plugins.ES
{
    internal class ESQueryHelper
    {
        public static async Task<string> Query(string server, string query, ESQueryArgs queryArgs)
        {
            var settings = new ElasticsearchClientSettings(new Uri(server));
            settings.Authentication(new BasicAuthentication(queryArgs.Username, queryArgs.Password)).ServerCertificateValidationCallback((obj, x, c, e) => true);

            var client = new ElasticsearchClient(settings);

            if (!string.IsNullOrWhiteSpace(query))
            {
                SearchResponse<dynamic> result;
                //纯KQL、Lucene
                if (query.StartsWith('{') == false)
                {
                    //result = await client.SearchAsync<dynamic>(s => s.QueryLuceneSyntax(query));

                    //https://www.elastic.co/guide/en/elasticsearch/reference/current/query-dsl-query-string-query.html#query-string-syntax
                    result = await client.SearchAsync<dynamic>(s => {
                        if (string.IsNullOrWhiteSpace(queryArgs.QueryTarget))
                        {
                            throw new ArgumentNullException("在使用DSL语句时，需要指定索引(target)");
                        }

                        s.Index(Indices.Parse(queryArgs.QueryTarget));

                        //s.AllowNoIndices
                        SetQueryParameters(s, queryArgs.Parameters);

                        s.Query(q => {
                            q.QueryString(c => c.Query(query));
                        });
                    });
                }
                else
                {
                    //result = await client.SearchAsync<dynamic>(s => s.Query(q => q.RawJson(query)));

                    var s = JsonSerializer.Deserialize<SearchRequest>(query)!;

                    //s.AllowNoIndices allow_no_indices
                    SetQueryParameters(s, queryArgs.Parameters);

                    result = await client.SearchAsync<dynamic>(s);
                }


                //data.Content = result.Total.ToString();
                if (result.IsSuccess())
                {
                    if (queryArgs.Output == ESQueryArgs_OutputMode.TotalCount)
                    {
                        return result.Total.ToString();
                    }

                    return JsonSerializer.Serialize(result.Documents);
                }

                throw new Exception(result.ApiCallDetails.OriginalException?.Message ?? result.DebugInformation, result.ApiCallDetails.OriginalException);
            }

            return string.Empty;
        }

        private static void SetQueryParameters<TParameters>(Request<TParameters> request, ESQueryCollectorArgs_QueryParameters parameters) where TParameters : RequestParameters, new()
        {
            if (parameters?.Count > 0)
            {
                var method = request.GetType().GetMethod("Q", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod, new[] { typeof(string), typeof(object) });
                if (method != null)
                {
                    foreach (var param in parameters)
                    {
                        method.Invoke(request, new[] { param.Key, param.Value });
                    }
                }
            }
        }

    }

    internal class ESArgsBase
    {
        //[YamlMember(Alias = "username")]
        public string Username { get; set; }

        public string Password { get; set; }

    }

    internal class ESQueryArgs : ESArgsBase
    {
        [YamlMember(Alias = "target")]
        public string QueryTarget { get; set; }

        public ESQueryCollectorArgs_QueryParameters Parameters { get; set; }

        /// <summary>
        /// 查询模式(返回详情还是总数量)
        /// </summary>
        public ESQueryArgs_OutputMode Output { get; set; }
    }

    internal enum ESQueryArgs_OutputMode
    {
        Details = 0,

        TotalCount
    }

    internal class ESQueryCollectorArgs_QueryParameters : Dictionary<string, object>
    {
        //[YamlMember(Alias = "allow_no_indices")]
        //public string allowNoIndices { get; set; }

        //[YamlMember(Alias = "allow_no_indices")]
        //public string allowNoIndices { get; set; }
    }
}
