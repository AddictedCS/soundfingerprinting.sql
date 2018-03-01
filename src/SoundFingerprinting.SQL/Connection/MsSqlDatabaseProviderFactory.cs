namespace SoundFingerprinting.SQL.Connection
{
    using System.Data;
    using System.Data.Common;
    using System.Data.SqlClient;

    internal class MsSqlDatabaseProviderFactory : IDatabaseProviderFactory
    {
        private readonly IConnectionStringFactory connectionStringFactory;
        private readonly DbProviderFactory databaseProvider;

        public MsSqlDatabaseProviderFactory()
        {
            this.connectionStringFactory = new DefaultConnectionStringFactory();
            databaseProvider = SqlClientFactory.Instance;
        }

        public IDbConnection CreateConnection()
        {
            IDbConnection connection = databaseProvider.CreateConnection();
            if (connection != null)
            {
                connection.ConnectionString = connectionStringFactory.GetConnectionString();
                return connection;
            }

            return null;
        }
    }
}