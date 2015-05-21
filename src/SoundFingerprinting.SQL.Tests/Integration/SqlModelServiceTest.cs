namespace SoundFingerprinting.SQL.Tests.Integration
{
    using System.Transactions;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using SoundFingerprinting.SQL;

    [TestClass]
    public class SqlModelServiceTest : ModelServiceTest
    {
        private TransactionScope transactionPerTestScope;

        public SqlModelServiceTest()
        {
            ModelService = new SqlModelService();
        }

        public override sealed IModelService ModelService { get; set; }

        [TestInitialize]
        public void SetUp()
        {
            transactionPerTestScope = new TransactionScope();
        }

        [TestCleanup]
        public void TearDown()
        {
            transactionPerTestScope.Dispose();
        }
    }
}
