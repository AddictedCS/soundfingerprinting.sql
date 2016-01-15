namespace SoundFingerprinting.SQL.Tests.Integration
{
    using System.Transactions;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using SoundFingerprinting.DAO;
    using SoundFingerprinting.DAO.Data;
    using SoundFingerprinting.InMemory;

    [TestClass]
    public class FingerprintDaoTest : AbstractIntegrationTest
    {
        private readonly IFingerprintDao fingerprintDao;
        private readonly ITrackDao trackDao;

        private TransactionScope transactionPerTestScope;

        public FingerprintDaoTest()
        {
            fingerprintDao = new FingerprintDao();
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
        public void InsertFingerprintsTest()
        {
            TrackData track = new TrackData("isrc", "artist", "title", "album", 1986, 200);
            var trackReference = trackDao.InsertTrack(track);

            var fingerprintReference = fingerprintDao.InsertFingerprint(new FingerprintData(GenericFingerprint, trackReference));

            AssertModelReferenceIsInitialized(fingerprintReference);
        }

        [TestMethod]
        public void MultipleFingerprintsInsertTest()
        {
            const int NumberOfFingerprints = 100;
            for (int i = 0; i < NumberOfFingerprints; i++)
            {
                var trackData = new TrackData("isrc" + i, "artist", "title", "album", 2012, 200);
                var trackReference = trackDao.InsertTrack(trackData);
                var fingerprintReference = fingerprintDao.InsertFingerprint(new FingerprintData(GenericFingerprint, trackReference));

                AssertModelReferenceIsInitialized(fingerprintReference);
            }
        }

        [TestMethod]
        public void ReadFingerprintsTest()
        {
            const int NumberOfFingerprints = 100;
            TrackData track = new TrackData("isrc", "artist", "title", "album", 1986, 200);
            var trackReference = trackDao.InsertTrack(track);

            for (int i = 0; i < NumberOfFingerprints; i++)
            {
                fingerprintDao.InsertFingerprint(new FingerprintData(GenericFingerprint, trackReference));
            }

            var fingerprints = fingerprintDao.ReadFingerprintsByTrackReference(trackReference);

            Assert.IsTrue(fingerprints.Count == NumberOfFingerprints);

            foreach (var fingerprint in fingerprints)
            {
                Assert.IsTrue(GenericFingerprint.Length == fingerprint.Signature.Length);
                for (var i = 0; i < GenericFingerprint.Length; i++)
                {
                    Assert.AreEqual(GenericFingerprint[i], fingerprint.Signature[i]);
                }
            }
        }
    }
}
