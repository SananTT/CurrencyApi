using System.Net;
using System.Text;

namespace CurrencyApi.IntegrationTests.Api.Fakes;

public sealed class FakeFrankfurterMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCodeToReturn;
    private readonly bool _throwException;

    public FakeFrankfurterMessageHandler(HttpStatusCode statusCodeToReturn, bool throwException = false)
    {
        _statusCodeToReturn = statusCodeToReturn;
        _throwException = throwException;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_throwException)
        {
            throw new HttpRequestException("Simulated upstream network failure");
        }

        var path = request.RequestUri!.PathAndQuery.ToLowerInvariant();
        var content = "";

        if (_statusCodeToReturn == HttpStatusCode.OK)
        {
            if (path.Contains("latest"))
            {
                content = @"{
                  ""amount"": 1.0,
                  ""base"": ""EUR"",
                  ""date"": ""2024-01-01"",
                  ""rates"": {
                    ""USD"": 1.10
                  }
                }";
            }
            else
            {
                content = @"{
                  ""amount"": 1.0,
                  ""base"": ""EUR"",
                  ""start_date"": ""2024-01-01"",
                  ""end_date"": ""2024-01-05"",
                  ""rates"": {
                    ""2024-01-01"": {
                      ""USD"": 1.10
                    }
                  }
                }";
            }
        }
        else
        {
            content = @"{ ""message"": ""error"" }";
        }

        var response = new HttpResponseMessage(_statusCodeToReturn)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        };

        return Task.FromResult(response);
    }
}
