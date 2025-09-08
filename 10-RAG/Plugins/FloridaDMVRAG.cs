using System.ComponentModel;
using System.Text.Json.Serialization;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;

namespace Plugins
{
    public class FloridaDMVRAG
    {
        private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
        private readonly SearchIndexClient _indexClient;

        public FloridaDMVRAG(IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator, SearchIndexClient indexClient)
        {
            _embeddingGenerator = embeddingGenerator;
            _indexClient = indexClient;
        }

        [KernelFunction("florida_dmvrag")]
        [Description("Retrieve information from the Florida DMV RAG system based on a query.")]
        [return: Description("Returns the search results.")]
        public async Task<IList<FloridaDMVSearchResults>> SearchAsync([Description("the original prompt optimized for a vector search")] string query)
        {
            Console.WriteLine("FloridaDMV tool was accessed");
            // Convert string query to vector
            var embedding = await _embeddingGenerator.GenerateAsync(query);

            // Get AI Search client for index
            SearchClient searchClient = _indexClient.GetSearchClient("floridadmv");

            // Configure request parameters
            VectorizedQuery vectorQuery = new(embedding.Vector);

            vectorQuery.Fields.Add("text_vector");


            // Configure Search Options

            SearchOptions searchOptions = new()
            {
                Size = 5,
                VectorSearch = new() { Queries = { vectorQuery } }
            };


            // Perform search request
            Response<SearchResults<IndexSchema>> response = await searchClient.SearchAsync<IndexSchema>(searchOptions);;

            var searchResults = new List<FloridaDMVSearchResults>();

            //interate over AI Search result
            await foreach (SearchResult<IndexSchema> result in response.Value.GetResultsAsync())
            {

                // Only add results with score >= 0.8
                if (result.Score < 0.8)
                    continue;


                searchResults.Add(new FloridaDMVSearchResults()
                {
                    Content = result.Document.Content,
                    Score = result.Score
                });


            }
            return searchResults;
        }
        private sealed class IndexSchema
        {
            [JsonPropertyName("parent_id")]
            public string? ParentId { get; set; }

            [JsonPropertyName("chunk")]
            public string? Content { get; set; }

        }

        public class FloridaDMVSearchResults
        {
            public string? Content { get; set; } 
            public double? Score { get; set; }
        }
    }
}