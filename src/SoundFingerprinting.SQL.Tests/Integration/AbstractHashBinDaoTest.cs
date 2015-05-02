namespace SoundFingerprinting.MongoDb.Tests.Integration
{
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using SoundFingerprinting.Audio;
    using SoundFingerprinting.Audio.NAudio;
    using SoundFingerprinting.Builder;
    using SoundFingerprinting.DAO;
    using SoundFingerprinting.DAO.Data;
    using SoundFingerprinting.Data;
    using SoundFingerprinting.Strides;
    using SoundFingerprinting.Tests.Integration;

    [TestClass]
    public abstract class AbstractHashBinDaoTest : AbstractIntegrationTest
    {
        private readonly IFingerprintCommandBuilder fingerprintCommandBuilder;
        private readonly IAudioService audioService;

        protected AbstractHashBinDaoTest()
        {
            this.fingerprintCommandBuilder = new FingerprintCommandBuilder();
            this.audioService = new NAudioService();
        }

        public abstract IHashBinDao HashBinDao { get; set; }

        public abstract ITrackDao TrackDao { get; set; }

        public abstract ISubFingerprintDao SubFingerprintDao { get; set; }

        [TestMethod]
        public void InsertReadTest()
        {
            TrackData track = new TrackData("isrc", "artist", "title", "album", 1986, 200);
            var trackReference = this.TrackDao.InsertTrack(track);
            const int NumberOfHashBins = 100;
            var hashedFingerprints = Enumerable.Range(0, NumberOfHashBins).Select(i => new HashedFingerprint(this.GenericSignature, this.GenericHashBuckets, i, i * 0.928));

            this.InsertHashedFingerprintsForTrack(hashedFingerprints, trackReference);

            var hashedFingerprintss = this.HashBinDao.ReadHashedFingerprintsByTrackReference(track.TrackReference);
            Assert.AreEqual(NumberOfHashBins, hashedFingerprintss.Count);
        }

        [TestMethod]
        public void SameNumberOfHashBinsIsInsertedInAllTablesWhenFingerprintingEntireSongTest()
        {
            const int StaticStride = 5115;
            TagInfo tagInfo = this.GetTagInfo();
            int releaseYear = tagInfo.Year;
            TrackData track = new TrackData(tagInfo.ISRC, tagInfo.Artist, tagInfo.Title, tagInfo.Album, releaseYear, (int)tagInfo.Duration);
            var trackReference = this.TrackDao.InsertTrack(track);
            var hashedFingerprints = this.fingerprintCommandBuilder
                .BuildFingerprintCommand()
                .From(PathToMp3)
                .WithFingerprintConfig(config =>
                {
                    config.SpectrogramConfig.Stride = new IncrementalStaticStride(StaticStride, config.SamplesPerFingerprint);
                })
                .UsingServices(this.audioService)
                .Hash()
                .Result;

            this.InsertHashedFingerprintsForTrack(hashedFingerprints, trackReference);
            
            var hashes = this.HashBinDao.ReadHashedFingerprintsByTrackReference(track.TrackReference);
            Assert.AreEqual(hashedFingerprints.Count, hashes.Count);
            foreach (var data in hashes)
            {
                Assert.AreEqual(25, data.HashBins.Length);
            }
        }

                [TestMethod]
        public void ReadByTrackGroupIdWorksAsExpectedTest()
        {
            const int StaticStride = 5115;
            TagInfo tagInfo = this.GetTagInfo();
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

            var firstTrackReference = this.TrackDao.InsertTrack(firstTrack);
            var secondTrackReference = this.TrackDao.InsertTrack(secondTrack);

            var hashedFingerprints = this.fingerprintCommandBuilder
                .BuildFingerprintCommand()
                .From(PathToMp3, 20, 0)
                .WithFingerprintConfig(config =>
                {
                    config.SpectrogramConfig.Stride = new IncrementalStaticStride(StaticStride, config.SamplesPerFingerprint);
                })
                .UsingServices(this.audioService)
                .Hash()
                .Result;

            this.InsertHashedFingerprintsForTrack(hashedFingerprints, firstTrackReference);
            this.InsertHashedFingerprintsForTrack(hashedFingerprints, secondTrackReference);

            const int ThresholdVotes = 25;
            foreach (var hashedFingerprint in hashedFingerprints)
            {
                var subFingerprintData = this.HashBinDao.ReadSubFingerprintDataByHashBucketsThresholdWithGroupId(hashedFingerprint.HashBins, ThresholdVotes, "first-group-id").ToList();

                Assert.IsTrue(subFingerprintData.Count == 1);
                Assert.AreEqual(firstTrackReference, subFingerprintData[0].TrackReference);

                subFingerprintData = this.HashBinDao.ReadSubFingerprintDataByHashBucketsThresholdWithGroupId(hashedFingerprint.HashBins, ThresholdVotes, "second-group-id").ToList();

                Assert.IsTrue(subFingerprintData.Count == 1);
                Assert.AreEqual(secondTrackReference, subFingerprintData[0].TrackReference);

                subFingerprintData = this.HashBinDao.ReadSubFingerprintDataByHashBucketsWithThreshold(hashedFingerprint.HashBins, ThresholdVotes).ToList();
                Assert.AreEqual(2, subFingerprintData.Count);
            }
        }

        [TestMethod]
        public void ReadHashDataByTrackTest()
        {
            TrackData firstTrack = new TrackData("isrc", "artist", "title", "album", 2012, 200);

            var firstTrackReference = this.TrackDao.InsertTrack(firstTrack);

            var firstHashData = this.fingerprintCommandBuilder
                .BuildFingerprintCommand()
                .From(PathToMp3, 10, 0)
                .UsingServices(this.audioService)
                .Hash()
                .Result;

            this.InsertHashedFingerprintsForTrack(firstHashData, firstTrackReference);

            TrackData secondTrack = new TrackData("isrc", "artist", "title", "album", 2012, 200);

            var secondTrackReference = this.TrackDao.InsertTrack(secondTrack);

            var secondHashData = this.fingerprintCommandBuilder
                .BuildFingerprintCommand()
                .From(PathToMp3, 20, 10)
                .UsingServices(this.audioService)
                .Hash()
                .Result;

            this.InsertHashedFingerprintsForTrack(secondHashData, secondTrackReference);

            var resultFirstHashData = this.HashBinDao.ReadHashedFingerprintsByTrackReference(firstTrackReference);
            this.AssertHashDatasAreTheSame(firstHashData, resultFirstHashData);

            IList<HashedFingerprint> resultSecondHashData = this.HashBinDao.ReadHashedFingerprintsByTrackReference(secondTrackReference);
            this.AssertHashDatasAreTheSame(secondHashData, resultSecondHashData);
        }

        private void InsertHashedFingerprintsForTrack(IEnumerable<HashedFingerprint> hashedFingerprints, IModelReference trackReference)
        {
            foreach (var hashedFingerprint in hashedFingerprints)
            {
                var subFingerprintId = this.SubFingerprintDao.InsertSubFingerprint(hashedFingerprint.SubFingerprint, hashedFingerprint.SequenceNumber, hashedFingerprint.Timestamp, trackReference);
                this.HashBinDao.InsertHashBins(hashedFingerprint.HashBins, subFingerprintId, trackReference);
            }
        }
    }
}
