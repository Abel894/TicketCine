using Xunit;

namespace TicketCine.IntegrationTests;

[CollectionDefinition("Postgres")]
public class PostgresCollection : ICollectionFixture<PostgresContainerFixture>
{
}