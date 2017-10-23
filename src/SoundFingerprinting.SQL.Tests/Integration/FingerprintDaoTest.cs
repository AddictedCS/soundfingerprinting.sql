namespace SoundFingerprinting.SQL.Tests.Integration
{
    using System.Transactions;

    using NUnit.Framework;

    using SoundFingerprinting.DAO;
    using SoundFingerprinting.DAO.Data;
    using SoundFingerprinting.InMemory;

    [TestFixture]
    public class FingerprintDaoTest : AbstractIntegrationTest
    {
        private readonly IFingerprintDao fingerprintDao;
        private readonly ITrackDao trackDao;

        private TransactionScope transactionPerTestScope;

        public FingerprintDaoTest()
        {
            var ramStorage = new RAMStorage(50);
            fingerprintDao = new FingerprintDao(ramStorage);
            trackDao = new TrackDao(ramStorage);
        }

        [SetUp]
        public void SetUp()
        {
            transactionPerTestScope = new TransactionScope();
        }

        [TearDown]
        public void TearDown()
        {
            transactionPerTestScope.Dispose();
        }

        [Test]
        public void InsertFingerprintsTest()
        {
            var track = new TrackData("isrc", "artist", "title", "album", 1986, 200);
            var trackReference = trackDao.InsertTrack(track);

            var fingerprintReference = fingerprintDao.InsertFingerprint(new FingerprintData(GenericFingerprint, trackReference));

            AssertModelReferenceIsInitialized(fingerprintReference);
        }

        [Test]
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

        [Test]
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
                CollectionAssert.AreEqual(GenericFingerprint, fingerprint.Signature);
            }
        }
    }
}
