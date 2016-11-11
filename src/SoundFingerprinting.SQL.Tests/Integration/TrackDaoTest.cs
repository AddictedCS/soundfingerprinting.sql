namespace SoundFingerprinting.SQL.Tests.Integration
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Transactions;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using SoundFingerprinting.Audio;
    using SoundFingerprinting.Audio.NAudio;
    using SoundFingerprinting.Builder;
    using SoundFingerprinting.Configuration;
    using SoundFingerprinting.DAO;
    using SoundFingerprinting.DAO.Data;
    using SoundFingerprinting.SQL;
    using SoundFingerprinting.Strides;

    [TestClass]
    public class TrackDaoTest : AbstractIntegrationTest
    {
        private readonly IFingerprintCommandBuilder fingerprintCommandBuilder;
        private readonly IAudioService audioService;
        private readonly ITrackDao trackDao;
        private readonly ISubFingerprintDao subFingerprintDao;

        private TransactionScope transactionPerTestScope;

        public TrackDaoTest()
        {
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
        public void TrackInsertTest()
        {
            var track = this.GetRandomTrack();

            var trackReference = trackDao.InsertTrack(track);

            AssertModelReferenceIsInitialized(trackReference);
            AssertModelReferenceIsInitialized(track.TrackReference);
        }

        [TestMethod]
        public void MultipleTrackInsertTest()
        {
            const int NumberOfTracks = 1000;
            var modelReferences = new ConcurrentBag<IModelReference>();
            for (int i = 0; i < NumberOfTracks; i++)
            {
                var modelReference = trackDao.InsertTrack(new TrackData("isrc", "artist", "title", "album", 2012, 200)
                {
                    GroupId = "group-id"
                });

                Assert.IsFalse(modelReferences.Contains(modelReference));
                modelReferences.Add(modelReference);
            }

            Assert.AreEqual(NumberOfTracks, trackDao.ReadAll().Count);
        }

        [TestMethod]
        public void ReadAllTracksTest()
        {
            const int TrackCount = 5;
            var expectedTracks = this.InsertRandomTracks(TrackCount);

            var tracks = trackDao.ReadAll();

            Assert.AreEqual(TrackCount, tracks.Count);
            foreach (var expectedTrack in expectedTracks)
            {
                Assert.IsTrue(tracks.Any(track => track.ISRC == expectedTrack.ISRC));
            }
        }

        [TestMethod]
        public void ReadTrackByIdTest()
        {
            var track = new TrackData("isrc", "artist", "title", "album", 2012, 200)
            {
                GroupId = "group-id"
            };

            var trackReference = trackDao.InsertTrack(track);

            AssertTracksAreEqual(track, trackDao.ReadTrack(trackReference));
        }

        [TestMethod]
        public void InsertMultipleTrackAtOnceTest()
        {
            const int TrackCount = 100;
            var tracks = this.InsertRandomTracks(TrackCount);

            var actualTracks = trackDao.ReadAll();

            Assert.AreEqual(tracks.Count, actualTracks.Count);
            for (int i = 0; i < actualTracks.Count; i++)
            {
                AssertModelReferenceIsInitialized(actualTracks[i].TrackReference);
                AssertTracksAreEqual(tracks[i], actualTracks.First(track => track.TrackReference.Equals(tracks[i].TrackReference)));
            }
        }

        [TestMethod]
        public void ReadTrackByArtistAndTitleTest()
        {
            var track = this.GetRandomTrack();
            trackDao.InsertTrack(track);

            var tracks = trackDao.ReadTrackByArtistAndTitleName(track.Artist, track.Title);

            Assert.IsNotNull(tracks);
            Assert.IsTrue(tracks.Count == 1);
            AssertTracksAreEqual(track, tracks[0]);
        }

        [TestMethod]
        public void ReadByNonExistentArtistAndTitleTest()
        {
            var tracks = trackDao.ReadTrackByArtistAndTitleName("artist", "title");

            Assert.IsTrue(tracks.Count == 0);
        }

        [TestMethod]
        public void ReadTrackByISRCTest()
        {
            var expectedTrack = this.GetRandomTrack();
            trackDao.InsertTrack(expectedTrack);

            var actualTrack = trackDao.ReadTrackByISRC(expectedTrack.ISRC);

            AssertTracksAreEqual(expectedTrack, actualTrack);
        }

        [TestMethod]
        public void DeleteCollectionOfTracksTest()
        {
            const int NumberOfTracks = 10;
            var tracks = this.InsertRandomTracks(NumberOfTracks);

            var allTracks = trackDao.ReadAll();

            Assert.IsTrue(allTracks.Count == NumberOfTracks);
            foreach (var track in tracks)
            {
                trackDao.DeleteTrack(track.TrackReference);
            }

            Assert.IsTrue(trackDao.ReadAll().Count == 0);
        }

        [TestMethod]
        public void DeleteOneTrackTest()
        {
            TrackData track = this.GetRandomTrack();
            var trackReference = trackDao.InsertTrack(track);

            trackDao.DeleteTrack(trackReference);

            Assert.IsNull(trackDao.ReadTrack(trackReference));
        }

        [TestMethod]
        public void DeleteHashBinsAndSubfingerprintsOnTrackDelete()
        {
            const int StaticStride = 5115;
            const int SecondsToProcess = 20;
            const int StartAtSecond = 30;

            var tagInfo = GetTagInfo();
            int releaseYear = tagInfo.Year;
            TrackData track = new TrackData(tagInfo.ISRC, tagInfo.Artist, tagInfo.Title, tagInfo.Album, releaseYear, (int)tagInfo.Duration);
            var trackReference = trackDao.InsertTrack(track);
            var hashData = fingerprintCommandBuilder
                .BuildFingerprintCommand()
                .From(PathToMp3, SecondsToProcess, StartAtSecond)
                .WithFingerprintConfig(config =>
                {
                    config.SpectrogramConfig.Stride = new IncrementalStaticStride(StaticStride, config.SamplesPerFingerprint);
                })
                .UsingServices(audioService)
                .Hash()
                .Result;

            subFingerprintDao.InsertHashDataForTrack(hashData, trackReference);

            var actualTrack = trackDao.ReadTrackByISRC(tagInfo.ISRC);
            Assert.IsNotNull(actualTrack);
            AssertTracksAreEqual(track, actualTrack);

            // Act
            int modifiedRows = trackDao.DeleteTrack(trackReference);

            Assert.IsNull(trackDao.ReadTrackByISRC(tagInfo.ISRC));
            Assert.IsTrue(subFingerprintDao.ReadHashedFingerprintsByTrackReference(actualTrack.TrackReference).Count == 0);
            Assert.AreEqual(1 + hashData.Count, modifiedRows);
        }

        [TestMethod]
        public void InserTrackShouldAcceptEmptyEntriesCodes()
        {
            TrackData track = new TrackData(string.Empty, string.Empty, string.Empty, string.Empty, 1986, 200);
            var trackReference = trackDao.InsertTrack(track);

            var actualTrack = trackDao.ReadTrack(trackReference);

            AssertModelReferenceIsInitialized(trackReference);
            AssertTracksAreEqual(track, actualTrack);
        }

        private List<TrackData> InsertRandomTracks(int trackCount)
        {
            var tracks = new List<TrackData>();
            for (int i = 0; i < trackCount; i++)
            {
                var track = this.GetRandomTrack();
                tracks.Add(track);
                trackDao.InsertTrack(track);
            }

            return tracks;
        }

        private TrackData GetRandomTrack()
        {
            return new TrackData(Guid.NewGuid().ToString(), "artist", "title", "album", 1986, 360)
            {
                GroupId = Guid.NewGuid().ToString().Substring(0, 20) // db max length
            };
        }
    }
}
