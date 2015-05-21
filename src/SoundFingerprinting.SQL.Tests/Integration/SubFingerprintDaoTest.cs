namespace SoundFingerprinting.SQL.Tests.Integration
{
    using System.Transactions;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using SoundFingerprinting.DAO;
    using SoundFingerprinting.DAO.Data;
    using SoundFingerprinting.SQL;

    [TestClass]
    public class SubFingerprintDaoTest : AbstractIntegrationTest
    {
        private readonly ISubFingerprintDao subFingerprintDao;
        private readonly ITrackDao trackDao;

        private TransactionScope transactionPerTestScope;

        public SubFingerprintDaoTest()
        {
            subFingerprintDao = new SubFingerprintDao();
            trackDao = new TrackDao();
        }

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

        [TestMethod]
        public void InsertSubFingerprintTest()
        {
            TrackData track = new TrackData("isrc", "artist", "title", "album", 1986, 200);
            var trackReference = trackDao.InsertTrack(track);

            var subFingerprintReference = subFingerprintDao.InsertSubFingerprint(GenericSignature, 123, 0.928, trackReference);

            AssertModelReferenceIsInitialized(subFingerprintReference);
        }

        [TestMethod]
        public void ReadSubfingerprintTest()
        {
            TrackData track = new TrackData("isrc", "artist", "title", "album", 1986, 200);
            var trackReference = trackDao.InsertTrack(track);
            var subFingerprintReference = subFingerprintDao.InsertSubFingerprint(GenericSignature, 123, 0.928, trackReference);

            var actual = subFingerprintDao.ReadSubFingerprint(subFingerprintReference);

            AsserSubFingerprintsAreEqual(new SubFingerprintData(GenericSignature, 123, 0.928, subFingerprintReference, trackReference), actual);
        }

        private void AsserSubFingerprintsAreEqual(SubFingerprintData expected, SubFingerprintData actual)
        {
            Assert.AreEqual(expected.SubFingerprintReference, actual.SubFingerprintReference);
            Assert.AreEqual(expected.TrackReference, actual.TrackReference);
            for (int i = 0; i < expected.Signature.Length; i++)
            {
                Assert.AreEqual(expected.Signature[i], actual.Signature[i]);
            }

            Assert.AreEqual(expected.SequenceNumber, actual.SequenceNumber);
            Assert.IsTrue(System.Math.Abs(expected.SequenceAt - actual.SequenceAt) < Epsilon);
        }
    }
}
