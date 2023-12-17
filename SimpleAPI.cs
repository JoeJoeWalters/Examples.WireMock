using FluentAssertions;
using System.Net;
using WireMock;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Wiremock.Examples
{
    public class SimpleAPI
    {
        private readonly WireMockServer _mockServer;
        private const int _mockPort = 9876;
        private const bool _mockSSL = false;
        private readonly string _baseUri;

        public SimpleAPI()
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
                        .WithHeader("Content-Type", "application/json") //"text/plain")
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
                        .WithHeader("Content-Type", "application/json") //"text/plain")
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


        ~SimpleAPI()
        {
            _mockServer.Stop();
            _mockServer.Dispose();
        }
    }
}