using Xunit;

namespace Mailer.IntegrationTests.Fixtures
{
    public static class CollectionNames
    {
        public const string ApiRest = "ApiRest";
        public const string ApiGrpc = "ApiGrpc";
        public const string Services = "Services";
    }

    [CollectionDefinition(CollectionNames.ApiRest)]
    public class ApiRestCollection :
        ICollectionFixture<ApiFixture>
    {
    }

    [CollectionDefinition(CollectionNames.ApiGrpc)]
    public class ApiGrpcCollection :
        ICollectionFixture<ApiFixture>
    {
    }

    [CollectionDefinition(CollectionNames.Services)]
    public class ServicesCollection :
        ICollectionFixture<ApiFixture>
    {
    }
}
