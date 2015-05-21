namespace SoundFingerprinting.SQL.Tests.Integration
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Transactions;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using SoundFingerprinting.Audio;
    using SoundFingerprinting.Audio.NAudio;
    using SoundFingerprinting.Builder;
    using SoundFingerprinting.DAO;
    using SoundFingerprinting.DAO.Data;
    using SoundFingerprinting.Data;
    using SoundFingerprinting.SQL;
    using SoundFingerprinting.Strides;

    [TestClass]
    public class HashBinDaoTest : AbstractIntegrationTest
    {
        private readonly IHashBinDao hashBinDao;
        private readonly ITrackDao trackDao;
        private readonly ISubFingerprintDao subFingerprintDao;
        private readonly IFingerprintCommandBuilder fingerprintCommandBuilder;
        private readonly IAudioService audioService;

        private TransactionScope transactionPerTestScope;

        public HashBinDaoTest()
        {
            hashBinDao = new HashBinDao();
            trackDao = new TrackDao();
            subFingerprintDao = new SubFingerprintDao();
            fingerprintCommandBuilder = new FingerprintCommandBuilder();
            audioService = new NAudioService();
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
        public void InsertAndReadHashBinTest()
        {
            TrackData track = new TrackData("isrc", "artist", "title", "album", 1986, 200);
            var trackReference = trackDao.InsertTrack(track);
            const int NumberOfHashBins = 100;
            var hashedFingerprints = Enumerable.Range(0, NumberOfHashBins).Select(i => new HashedFingerprint(GenericSignature, GenericHashBuckets, i, i * 0.928));

            InsertHashedFingerprintsForTrack(hashedFingerprints, trackReference);

            var hashedFingerprintss = hashBinDao.ReadHashedFingerprintsByTrackReference(track.TrackReference);
            Assert.AreEqual(NumberOfHashBins, hashedFingerprintss.Count);
        }

        [TestMethod]
        public void SameNumberOfHashBinsIsInsertedInAllTablesWhenFingerprintingEntireSongTest()
        {
            const int StaticStride = 5115;
            TagInfo tagInfo = GetTagInfo();
            int releaseYear = tagInfo.Year;
            TrackData track = new TrackData(tagInfo.ISRC, tagInfo.Artist, tagInfo.Title, tagInfo.Album, releaseYear, (int)tagInfo.Duration);
            var trackReference = trackDao.InsertTrack(track);
            var hashedFingerprints = fingerprintCommandBuilder
                .BuildFingerprintCommand()
                .From(PathToMp3)
                .WithFingerprintConfig(config =>
                {
                    config.SpectrogramConfig.Stride = new IncrementalStaticStride(StaticStride, config.SamplesPerFingerprint);
                })
                .UsingServices(audioService)
                .Hash()
                .Result;

            InsertHashedFingerprintsForTrack(hashedFingerprints, trackReference);
            
            var hashes = hashBinDao.ReadHashedFingerprintsByTrackReference(track.TrackReference);
            Assert.AreEqual(hashedFingerprints.Count, hashes.Count);
            foreach (var data in hashes)
            {
                Assert.AreEqual(25, data.HashBins.Length);
            }
        }

        [TestMethod]
        public void ReadHashBinsByTrackGroupIdWorksAsExpectedTest()
        {
            const int StaticStride = 5115;
            TagInfo tagInfo = GetTagInfo();
            int releaseYear = tagInfo.Year;
            TrackData firstTrack = new TrackData(
                tagInfo.ISRC, tagInfo.Artist, tagInfo.Title, tagInfo.Album, releaseYear, (int)tagInfo.Duration)
                {
                    GroupId = "first-group-id"
                };
            TrackData secondTrack = new TrackData(
                tagInfo.ISRC, tagInfo.Artist, tagInfo.Title, tagInfo.Album, releaseYear, (int)tagInfo.Duration)
                {
                    GroupId = "second-group-id"
                };

            var firstTrackReference = trackDao.InsertTrack(firstTrack);
            var secondTrackReference = trackDao.InsertTrack(secondTrack);

            var hashedFingerprints = fingerprintCommandBuilder
                .BuildFingerprintCommand()
                .From(PathToMp3, 20, 0)
                .WithFingerprintConfig(config =>
                {
                    config.SpectrogramConfig.Stride = new IncrementalStaticStride(StaticStride, config.SamplesPerFingerprint);
                })
                .UsingServices(audioService)
                .Hash()
                .Result;

            InsertHashedFingerprintsForTrack(hashedFingerprints, firstTrackReference);
            InsertHashedFingerprintsForTrack(hashedFingerprints, secondTrackReference);

            const int ThresholdVotes = 25;
            foreach (var hashedFingerprint in hashedFingerprints)
            {
                var subFingerprintData = hashBinDao.ReadSubFingerprintDataByHashBucketsThresholdWithGroupId(hashedFingerprint.HashBins, ThresholdVotes, "first-group-id").ToList();

                Assert.IsTrue(subFingerprintData.Count == 1);
                Assert.AreEqual(firstTrackReference, subFingerprintData[0].TrackReference);

                subFingerprintData = hashBinDao.ReadSubFingerprintDataByHashBucketsThresholdWithGroupId(hashedFingerprint.HashBins, ThresholdVotes, "second-group-id").ToList();

                Assert.IsTrue(subFingerprintData.Count == 1);
                Assert.AreEqual(secondTrackReference, subFingerprintData[0].TrackReference);

                subFingerprintData = hashBinDao.ReadSubFingerprintDataByHashBucketsWithThreshold(hashedFingerprint.HashBins, ThresholdVotes).ToList();
                Assert.AreEqual(2, subFingerprintData.Count);
            }
        }

        [TestMethod]
        public void ReadHashDataByTrackTest()
        {
            TrackData firstTrack = new TrackData("isrc", "artist", "title", "album", 2012, 200);

            var firstTrackReference = trackDao.InsertTrack(firstTrack);

            var firstHashData = fingerprintCommandBuilder
                .BuildFingerprintCommand()
                .From(PathToMp3, 10, 0)
                .UsingServices(audioService)
                .Hash()
                .Result;

            InsertHashedFingerprintsForTrack(firstHashData, firstTrackReference);

            TrackData secondTrack = new TrackData("isrc", "artist", "title", "album", 2012, 200);

            var secondTrackReference = trackDao.InsertTrack(secondTrack);

            var secondHashData = fingerprintCommandBuilder
                .BuildFingerprintCommand()
                .From(PathToMp3, 20, 10)
                .UsingServices(audioService)
                .Hash()
                .Result;

            InsertHashedFingerprintsForTrack(secondHashData, secondTrackReference);

            var resultFirstHashData = hashBinDao.ReadHashedFingerprintsByTrackReference(firstTrackReference);
            AssertHashDatasAreTheSame(firstHashData, resultFirstHashData);

            IList<HashedFingerprint> resultSecondHashData = hashBinDao.ReadHashedFingerprintsByTrackReference(secondTrackReference);
            AssertHashDatasAreTheSame(secondHashData, resultSecondHashData);
        }

        private void InsertHashedFingerprintsForTrack(IEnumerable<HashedFingerprint> hashedFingerprints, IModelReference trackReference)
        {
            foreach (var hashedFingerprint in hashedFingerprints)
            {
                var subFingerprintId = subFingerprintDao.InsertSubFingerprint(hashedFingerprint.SubFingerprint, hashedFingerprint.SequenceNumber, hashedFingerprint.Timestamp, trackReference);
                hashBinDao.InsertHashBins(hashedFingerprint.HashBins, subFingerprintId, trackReference);
            }
        }
    }
}
