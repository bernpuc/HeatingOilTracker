using FluentAssertions;
using HeatingOilTracker.Core.Services;
using System.Net;
using Xunit;

namespace HeatingOilTracker.Tests.Services;

/// <summary>
/// Tests WeatherService HTTP methods using a fake HttpMessageHandler.
/// Requires the internal WeatherService(HttpClient) constructor.
/// </summary>
public class WeatherServiceHttpTests
{
    private static WeatherService CreateSut(string responseJson, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var handler = new FakeHttpMessageHandler(responseJson, statusCode);
        var httpClient = new HttpClient(handler);
        return new WeatherService(httpClient);
    }

    private static string BuildWeatherResponse(string[] dates, double[] maxTemps, double?[] minTemps)
    {
        var datesJson = string.Join(",", dates.Select(d => $"\"{d}\""));
        var maxJson = string.Join(",", maxTemps.Select(t => t.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        var minJson = string.Join(",", minTemps.Select(t => t.HasValue
            ? t.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : "null"));

        return $$"""
            {
              "daily": {
                "time": [{{datesJson}}],
                "temperature_2m_max": [{{maxJson}}],
                "temperature_2m_min": [{{minJson}}]
              }
            }
            """;
    }

    #region GetHistoricalWeatherAsync

    [Fact]
    public async Task GetHistoricalWeatherAsync_ValidResponse_ReturnsWeatherList()
    {
        var json = BuildWeatherResponse(
            ["2024-01-01", "2024-01-02", "2024-01-03"],
            [40.0, 38.0, 42.0],
            [25.0, 22.0, 28.0]
        );
        var sut = CreateSut(json);

        var result = await sut.GetHistoricalWeatherAsync(42.3m, -71.0m, new DateTime(2024, 1, 1), new DateTime(2024, 1, 3));

        result.Should().HaveCount(3);
        result[0].Date.Should().Be(new DateTime(2024, 1, 1));
        result[0].HighTempF.Should().Be(40m);
        result[0].LowTempF.Should().Be(25m);
        result[2].Date.Should().Be(new DateTime(2024, 1, 3));
        result[2].HighTempF.Should().Be(42m);
        result[2].LowTempF.Should().Be(28m);
    }

    [Fact]
    public async Task GetHistoricalWeatherAsync_NullTemperatureValues_FallsBackTo65()
    {
        var json = BuildWeatherResponse(
            ["2024-01-01"],
            [40.0],
            [null]  // null min temp
        );
        var sut = CreateSut(json);

        var result = await sut.GetHistoricalWeatherAsync(42.3m, -71.0m, new DateTime(2024, 1, 1), new DateTime(2024, 1, 1));

        result.Should().HaveCount(1);
        result[0].LowTempF.Should().Be(65m); // fallback value
        result[0].HighTempF.Should().Be(40m);
    }

    [Fact]
    public async Task GetHistoricalWeatherAsync_HttpError_ReturnsEmpty()
    {
        var sut = CreateSut("Internal Server Error", HttpStatusCode.InternalServerError);

        var result = await sut.GetHistoricalWeatherAsync(42.3m, -71.0m, new DateTime(2024, 1, 1), new DateTime(2024, 1, 3));

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetHistoricalWeatherAsync_InvalidJson_ReturnsEmpty()
    {
        var sut = CreateSut("not valid json at all");

        var result = await sut.GetHistoricalWeatherAsync(42.3m, -71.0m, new DateTime(2024, 1, 1), new DateTime(2024, 1, 3));

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetHistoricalWeatherAsync_EmptyDailyArrays_ReturnsEmpty()
    {
        var json = """{"daily":{"time":[],"temperature_2m_max":[],"temperature_2m_min":[]}}""";
        var sut = CreateSut(json);

        var result = await sut.GetHistoricalWeatherAsync(42.3m, -71.0m, new DateTime(2024, 1, 1), new DateTime(2024, 1, 3));

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetHistoricalWeatherAsync_MultipleEntries_ParsesAllDates()
    {
        var dates = Enumerable.Range(0, 10).Select(i => new DateTime(2024, 1, 1).AddDays(i).ToString("yyyy-MM-dd")).ToArray();
        var maxTemps = Enumerable.Range(0, 10).Select(i => 40.0 + i).ToArray();
        var minTemps = Enumerable.Range(0, 10).Select(i => (double?)25.0 + i).ToArray();
        var json = BuildWeatherResponse(dates, maxTemps, minTemps);
        var sut = CreateSut(json);

        var result = await sut.GetHistoricalWeatherAsync(42.3m, -71.0m, new DateTime(2024, 1, 1), new DateTime(2024, 1, 10));

        result.Should().HaveCount(10);
    }

    #endregion

    #region SearchLocationsAsync

    [Fact]
    public async Task SearchLocationsAsync_ValidResponse_ReturnsLocations()
    {
        var json = """
            {
              "results": [
                {
                  "name": "Boston",
                  "latitude": 42.3601,
                  "longitude": -71.0589,
                  "admin1": "Massachusetts",
                  "country": "United States"
                }
              ]
            }
            """;
        var sut = CreateSut(json);

        var result = await sut.SearchLocationsAsync("Boston");

        result.Should().HaveCount(1);
        result[0].Latitude.Should().Be((decimal)42.3601);
        result[0].Longitude.Should().Be((decimal)-71.0589);
        result[0].DisplayName.Should().Be("Boston, Massachusetts, United States");
    }

    [Fact]
    public async Task SearchLocationsAsync_NoAdmin1OrCountry_UsesOnlyName()
    {
        var json = """
            {
              "results": [
                {
                  "name": "Springfield",
                  "latitude": 37.2153,
                  "longitude": -93.2982
                }
              ]
            }
            """;
        var sut = CreateSut(json);

        var result = await sut.SearchLocationsAsync("Springfield");

        result.Should().HaveCount(1);
        result[0].DisplayName.Should().Be("Springfield");
    }

    [Fact]
    public async Task SearchLocationsAsync_NoResultsProperty_ReturnsEmpty()
    {
        var json = """{"generationtime_ms": 0.5}""";
        var sut = CreateSut(json);

        var result = await sut.SearchLocationsAsync("Nowhere");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchLocationsAsync_EmptyResultsArray_ReturnsEmpty()
    {
        var json = """{"results": []}""";
        var sut = CreateSut(json);

        var result = await sut.SearchLocationsAsync("Nowhere");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchLocationsAsync_HttpError_ReturnsEmpty()
    {
        var sut = CreateSut("Service Unavailable", HttpStatusCode.ServiceUnavailable);

        var result = await sut.SearchLocationsAsync("Boston");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchLocationsAsync_InvalidJson_ReturnsEmpty()
    {
        var sut = CreateSut("{bad json}");

        var result = await sut.SearchLocationsAsync("Boston");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchLocationsAsync_MultipleResults_ReturnsAll()
    {
        var json = """
            {
              "results": [
                {"name": "Boston", "latitude": 42.3601, "longitude": -71.0589, "country": "United States"},
                {"name": "Boston", "latitude": 52.9739, "longitude": 0.0215, "country": "United Kingdom"},
                {"name": "Boston", "latitude": 30.7942, "longitude": -83.7843, "country": "United States"}
              ]
            }
            """;
        var sut = CreateSut(json);

        var result = await sut.SearchLocationsAsync("Boston", maxResults: 3);

        result.Should().HaveCount(3);
        result[0].DisplayName.Should().Contain("United States");
        result[1].DisplayName.Should().Contain("United Kingdom");
    }

    #endregion
}

internal class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly string _responseContent;
    private readonly HttpStatusCode _statusCode;

    public FakeHttpMessageHandler(string responseContent, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _responseContent = responseContent;
        _statusCode = statusCode;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_responseContent)
        };
        return Task.FromResult(response);
    }
}
