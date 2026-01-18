using Airport.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Airport.Presentation.Extensions
{
    public static class PaginationExtensions
    {
        private const string PaginationHeader = "X-Pagination";

        public static void AddPaginationMetadata(this HttpResponse response, SummaryWithMetadata data)
        {
            var metadata = new
            {
                data.Summary.TotalCount,
                data.Summary.PageSize,
                data.Summary.CurrentPage,
                data.Summary.TotalPages,
                data.LandingsCount,
                data.DeparturesCount
            };

            response.Headers.Append("Access-Control-Expose-Headers", PaginationHeader);
            response.Headers.Append(PaginationHeader, JsonConvert.SerializeObject(metadata,
                new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                }));
        }
    }
}
