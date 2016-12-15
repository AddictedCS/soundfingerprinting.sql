namespace SoundFingerprinting.SQL.Tests.Integration
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Transactions;

    using NUnit.Framework;

    using SoundFingerprinting.Audio;
    using SoundFingerprinting.Audio.NAudio;
    using SoundFingerprinting.Builder;
    using SoundFingerprinting.DAO;
    using SoundFingerprinting.DAO.Data;
    using SoundFingerprinting.Data;
    using SoundFingerprinting.SQL;
    using SoundFingerprinting.Strides;

    [TestFixture]
    public class SubFingerprintDaoTest : AbstractIntegrationTest
    {
        private readonly IFingerprintCommandBuilder fcb;
        private readonly ISubFingerprintDao subFingerprintDao;
        private readonly ITrackDao trackDao;
        private readonly IAudioService audioService;

        private TransactionScope transactionPerTestScope;

        public SubFingerprintDaoTest()
        {
            subFingerprintDao = new SubFingerprintDao();
            trackDao = new TrackDao();
            fcb = new FingerprintCommandBuilder();
            audioService = new NAudioService();
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
        public void ShouldInsertAndReadSubFingerprints()
        {
            var track = new TrackData("isrc", "artist", "title", "album", 1986, 200);
            var trackReference = trackDao.InsertTrack(track);
            const int NumberOfHashBins = 100;
            var hashedFingerprints = Enumerable.Range(0, NumberOfHashBins).Select(i => new HashedFingerprint(GenericSignature, GenericHashBuckets, i, i * 0.928, Enumerable.Empty<string>()));

            InsertHashedFingerprintsForTrack(hashedFingerprints, trackReference);

            var hashedFingerprintss = subFingerprintDao.ReadHashedFingerprintsByTrackReference(track.TrackReference);
            Assert.AreEqual(NumberOfHashBins, hashedFingerprintss.Count);
            foreach (var hashedFingerprint in hashedFingerprintss)
            {
                CollectionAssert.AreEqual(GenericHashBuckets, hashedFingerprint.HashBins);
                CollectionAssert.AreEqual(GenericSignature, hashedFingerprint.SubFingerprint);
            }
        }

        [Test]
        public void ReadByTrackGroupIdWorksAsExpectedTest()
        {
            const int StaticStride = 5115;
            TagInfo tagInfo = GetTagInfo();
            TrackData firstTrack = new TrackData(tagInfo);
            TrackData secondTrack = new TrackData(tagInfo);

            var firstTrackReference = trackDao.InsertTrack(firstTrack);
            var secondTrackReference = trackDao.InsertTrack(secondTrack);

            var hashedFingerprintsForFirstTrack = fcb
                .BuildFingerprintCommand()
                .From(PathToMp3, 20, 0)
                .WithFingerprintConfig(config =>
                {
                    config.Stride = new IncrementalStaticStride(StaticStride);
                    config.Clusters = new[] { "first-group-id" };
                }).UsingServices(audioService)
                .Hash()
                .Result;

            InsertHashedFingerprintsForTrack(hashedFingerprintsForFirstTrack, firstTrackReference);

            var hashedFingerprintsForSecondTrack = fcb
               .BuildFingerprintCommand()
               .From(PathToMp3, 20, 0)
               .WithFingerprintConfig(config =>
               {
                   config.Stride = new IncrementalStaticStride(StaticStride);
                   config.Clusters = new[] { "second-group-id" };
               }).UsingServices(audioService)
               .Hash()
               .Result;
            InsertHashedFingerprintsForTrack(hashedFingerprintsForSecondTrack, secondTrackReference);

            const int ThresholdVotes = 25;
            foreach (var hashedFingerprint in hashedFingerprintsForFirstTrack)
            {
                var subFingerprintData =
                    subFingerprintDao.ReadSubFingerprints(
                        hashedFingerprint.HashBins, ThresholdVotes, new[] { "first-group-id" }).ToList();

                Assert.IsTrue(subFingerprintData.Count == 1);
                Assert.AreEqual(firstTrackReference, subFingerprintData[0].TrackReference);

                subFingerprintData =
                    subFingerprintDao.ReadSubFingerprints(
                        hashedFingerprint.HashBins, ThresholdVotes, new[] { "second-group-id" }).ToList();

                Assert.IsTrue(subFingerprintData.Count == 1);
                Assert.AreEqual(secondTrackReference, subFingerprintData[0].TrackReference);

                subFingerprintData = subFingerprintDao.ReadSubFingerprints(hashedFingerprint.HashBins, ThresholdVotes, Enumerable.Empty<string>()).ToList();
                Assert.AreEqual(2, subFingerprintData.Count);
            }
        }

        [Test]
        public void ReadHashDataByTrackTest()
        {
            TrackData firstTrack = new TrackData("isrc", "artist", "title", "album", 2012, 200);

            var firstTrackReference = trackDao.InsertTrack(firstTrack);

            var firstHashData = fcb
                .BuildFingerprintCommand()
                .From(PathToMp3, 10, 0)
                .UsingServices(audioService)
                .Hash()
                .Result;

            InsertHashedFingerprintsForTrack(firstHashData, firstTrackReference);

            var secondTrack = new TrackData("isrc", "artist", "title", "album", 2012, 200);

            var secondTrackReference = trackDao.InsertTrack(secondTrack);

            var secondHashData = fcb
                .BuildFingerprintCommand()
                .From(PathToMp3, 20, 10)
                .UsingServices(audioService)
                .Hash()
                .Result;

            InsertHashedFingerprintsForTrack(secondHashData, secondTrackReference);

            var resultFirstHashData = subFingerprintDao.ReadHashedFingerprintsByTrackReference(firstTrackReference);
            AssertHashDatasAreTheSame(firstHashData, resultFirstHashData);

            IList<HashedFingerprint> resultSecondHashData = subFingerprintDao.ReadHashedFingerprintsByTrackReference(secondTrackReference);
            AssertHashDatasAreTheSame(secondHashData, resultSecondHashData);
        }

        private void InsertHashedFingerprintsForTrack(IEnumerable<HashedFingerprint> hashedFingerprints, IModelReference trackReference)
        {
            subFingerprintDao.InsertHashDataForTrack(hashedFingerprints, trackReference);
        }
    }
}
