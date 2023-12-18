using FluentAssertions;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using WireMock;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Wiremock.Examples
{
    public class APITests : IDisposable
    {
        private readonly WireMockServer _mockServer;
        private const int _mockPort = 9876;
        private const bool _mockSSL = false;
        private readonly string _baseUri;

        /// <summary>
        /// This is run before every test and must be torn down with the "Dispose" method
        /// so that after every test the port is freed up for the next attempt
        /// </summary>
        public APITests()
        {
            string protocol = _mockSSL ? "https" : "http";
            _baseUri = $"{protocol}://localhost:{_mockPort}";
            _mockServer = WireMockServer.Start(_mockPort, _mockSSL);
        }

        [Fact]
        public async Task SimpleTest()
        {
            // ARRANGE
            HttpClient httpClient = new HttpClient();
            SimpleTestStub();

            // ACT
            var result = await httpClient.GetAsync($"{_baseUri}/tokens");

            // ASSERT
            result.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        private void SimpleTestStub()
        {
            _mockServer
                .Given(Request.Create().WithPath("/tokens").UsingGet())
                .RespondWith(Response.Create()
                        .WithStatusCode(HttpStatusCode.OK)
                        .WithHeader("Content-Type", "application/json")
                        .WithBody("{}")
                    );
        }

        [Fact]
        public async Task ComplexTest()
        {
            // ARRANGE
            HttpClient httpClient = new HttpClient();
            ComplexTestStub();

            // ACT
            var result = await httpClient.GetAsync($"{_baseUri}/tokens/12345678");

            // ASSERT
            string body = await result.Content.ReadAsStringAsync();
            result.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        private void ComplexTestStub()
        {
            _mockServer
                .Given(Request.Create().WithPath("/tokens/*").UsingGet())
                .RespondWith(Response.Create()
                        .WithStatusCode(HttpStatusCode.OK)
                        .WithHeader("Content-Type", "application/json")
                        .WithBody(BodyFactory, "application/json")
                    );
        }

        private string BodyFactory(IRequestMessage message)
        {
            switch (message.AbsolutePathSegments[0].ToLower())
            {
                case "tokens":

                    var value = message.AbsolutePathSegments[1];
                    return value;

                default:

                    return string.Empty;
            }
        }

        [Fact]
        public async Task MatchIdInBodyRootTest()
        {
            // ARRANGE
            HttpClient httpClient = new HttpClient();
            MatchIdInBodyRootTestStub();

            // ACT
            string postValue = "{\"Id\":\"12345678\"}";
            StringContent content = new StringContent(postValue, new MediaTypeHeaderValue("application/json"));
            var result = await httpClient.PostAsync($"{_baseUri}/tokens", content);

            // ASSERT
            string body = await result.Content.ReadAsStringAsync();
            result.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        private void MatchIdInBodyRootTestStub()
        {
            // https://github.com/WireMock-Net/WireMock.Net/wiki/Request-Matching-JsonPathMatcher
            // https://docs.hevodata.com/sources/engg-analytics/streaming/rest-api/writing-jsonpath-expressions/#:~:text=A%20JSONPath%20expression%20begins%20with,%5D)%20called%20the%20bracket%20notation.
            _mockServer
                .Given(Request.Create().WithPath("/tokens").UsingPost().WithBody(new JsonPathMatcher("$.Id[?('123456678')]")))
                .RespondWith(Response.Create()
                        .WithStatusCode(HttpStatusCode.OK)
                        .WithHeader("Content-Type", "application/json")
                        .WithBody("true", "application/json")
                    );
        }


        [Fact]
        public async Task TimeoutRootTest()
        {
            // ARRANGE
            HttpClient httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(1); // Short timeout delay
            TimeoutTestStub();

            // ACT
            string postValue = "{\"Id\":\"523456678\"}";
            StringContent content = new StringContent(postValue, new MediaTypeHeaderValue("application/json"));

            Func<Task> act = async () => await httpClient.PostAsync($"{_baseUri}/tokens", content);

            // ASSERT
            await act.Should()
                .ThrowAsync<TaskCanceledException>();
        }

        private void TimeoutTestStub()
        {
            // Respond with a delay if a certain Id is given
            _mockServer
                .Given(Request.Create().WithPath("/tokens").UsingPost().WithBody(new JsonPathMatcher("$.Id[?('523456678')]")))
                .RespondWith(Response.Create()
                        .WithStatusCode(HttpStatusCode.OK)
                        .WithHeader("Content-Type", "application/json")
                        .WithBody("true", "application/json")
                        .WithDelay(2000)
                    );
        }

        /// <summary>
        /// Must dispose of the mock after every test as it will block the 
        /// next test from running as the port will be in use
        /// </summary>
        public void Dispose()
        {
            _mockServer.Stop();
            _mockServer.Dispose();
        }
    }
}