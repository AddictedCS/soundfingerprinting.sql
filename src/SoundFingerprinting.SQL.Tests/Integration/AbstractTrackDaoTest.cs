namespace SoundFingerprinting.MongoDb.Tests.Integration
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using SoundFingerprinting.Audio;
    using SoundFingerprinting.Audio.NAudio;
    using SoundFingerprinting.Builder;
    using SoundFingerprinting.DAO;
    using SoundFingerprinting.DAO.Data;
    using SoundFingerprinting.Strides;
    using SoundFingerprinting.Tests.Integration;

    [TestClass]
    public abstract class AbstractTrackDaoTest : AbstractIntegrationTest
    {
        private readonly IFingerprintCommandBuilder fingerprintCommandBuilder;
        private readonly IAudioService audioService;

        protected AbstractTrackDaoTest()
        {
            this.fingerprintCommandBuilder = new FingerprintCommandBuilder();
            this.audioService = new NAudioService();
        }

        public abstract ITrackDao TrackDao { get; set; }

        public abstract ISubFingerprintDao SubFingerprintDao { get; set; }

        public abstract IHashBinDao HashBinDao { get; set; }

        [TestMethod]
        public void InsertTrackTest()
        {
            var track = this.GetTrack();

            var trackReference = this.TrackDao.InsertTrack(track);

            this.AssertModelReferenceIsInitialized(trackReference);
            this.AssertModelReferenceIsInitialized(track.TrackReference);
        }

        [TestMethod]
        public void MultipleInsertTest()
        {
            var modelReferences = new ConcurrentBag<IModelReference>();
            for (int i = 0; i < 1000; i++)
            {
                var modelReference = this.TrackDao.InsertTrack(new TrackData("isrc", "artist", "title", "album", 2012, 200)
                    {
                        GroupId = "group-id"
                    });

                Assert.IsFalse(modelReferences.Contains(modelReference));
                modelReferences.Add(modelReference);
            }
        }

        [TestMethod]
        public void ReadAllTracksTest()
        {
            const int TrackCount = 5;
            var expectedTracks = this.InsertTracks(TrackCount);
            
            var tracks = this.TrackDao.ReadAll();

            Assert.IsTrue(tracks.Count == TrackCount);
            foreach (var expectedTrack in expectedTracks)
            {
                Assert.IsTrue(tracks.Any(track => track.ISRC == expectedTrack.ISRC));
            }
        }

        [TestMethod]
        public void ReadByIdTest()
        {
            var track = new TrackData("isrc", "artist", "title", "album", 2012, 200)
                {
                    GroupId = "group-id"
                };

            var trackReference = this.TrackDao.InsertTrack(track);

            this.AssertTracksAreEqual(track, this.TrackDao.ReadTrack(trackReference));
        }

        [TestMethod]
        public void InsertMultipleTrackAtOnceTest()
        {
            const int TrackCount = 100;
            var tracks = this.InsertTracks(TrackCount);

            var actualTracks = this.TrackDao.ReadAll();

            Assert.AreEqual(tracks.Count, actualTracks.Count);
            for (int i = 0; i < actualTracks.Count; i++)
            {
                this.AssertModelReferenceIsInitialized(actualTracks[i].TrackReference);
                this.AssertTracksAreEqual(tracks[i], actualTracks.First(track => track.TrackReference.Equals(tracks[i].TrackReference)));
            }
        }

        [TestMethod]
        public void ReadTrackByArtistAndTitleTest()
        {
            TrackData track = this.GetTrack();
            this.TrackDao.InsertTrack(track);

            var tracks = this.TrackDao.ReadTrackByArtistAndTitleName(track.Artist, track.Title);

            Assert.IsNotNull(tracks);
            Assert.IsTrue(tracks.Count == 1);
            this.AssertTracksAreEqual(track, tracks[0]);
        }

        [TestMethod]
        public void ReadByNonExistentArtistAndTitleTest()
        {
            var tracks = this.TrackDao.ReadTrackByArtistAndTitleName("artist", "title");

            Assert.IsTrue(tracks.Count == 0);
        }

        [TestMethod]
        public void ReadTrackByISRCTest()
        {
            TrackData expectedTrack = this.GetTrack();
            this.TrackDao.InsertTrack(expectedTrack);

            TrackData actualTrack = this.TrackDao.ReadTrackByISRC(expectedTrack.ISRC);

            this.AssertTracksAreEqual(expectedTrack, actualTrack);
        }

        [TestMethod]
        public void DeleteCollectionOfTracksTest()
        {
            const int NumberOfTracks = 10;
            var tracks = this.InsertTracks(NumberOfTracks);
            
            var allTracks = this.TrackDao.ReadAll();

            Assert.IsTrue(allTracks.Count == NumberOfTracks);
            foreach (var track in tracks)
            {
                this.TrackDao.DeleteTrack(track.TrackReference);
            }

            Assert.IsTrue(this.TrackDao.ReadAll().Count == 0);
        }

        [TestMethod]
        public void DeleteOneTrackTest()
        {
            TrackData track = this.GetTrack();
            var trackReference = this.TrackDao.InsertTrack(track);

            this.TrackDao.DeleteTrack(trackReference);

            Assert.IsNull(this.TrackDao.ReadTrack(trackReference));
        }

        [TestMethod]
        public void DeleteHashBinsAndSubfingerprintsOnTrackDelete()
        {
            const int StaticStride = 5115;
            const int SecondsToProcess = 20;
            const int StartAtSecond = 30;
            TagInfo tagInfo = this.GetTagInfo();
            int releaseYear = tagInfo.Year;
            TrackData track = new TrackData(tagInfo.ISRC, tagInfo.Artist, tagInfo.Title, tagInfo.Album, releaseYear, (int)tagInfo.Duration);
            var trackReference = this.TrackDao.InsertTrack(track);
            var hashData = this.fingerprintCommandBuilder
                .BuildFingerprintCommand()
                .From(PathToMp3, SecondsToProcess, StartAtSecond)
                .WithFingerprintConfig(config =>
                    {
                        config.SpectrogramConfig.Stride = new IncrementalStaticStride(StaticStride, config.SamplesPerFingerprint);
                    })
                .UsingServices(this.audioService)
                .Hash()
                .Result;

            var subFingerprintReferences = new List<IModelReference>();
            foreach (var hash in hashData)
            {
                var subFingerprintReference = this.SubFingerprintDao.InsertSubFingerprint(hash.SubFingerprint, hash.SequenceNumber, hash.Timestamp, trackReference);
                this.HashBinDao.InsertHashBins(hash.HashBins, subFingerprintReference, trackReference);
                subFingerprintReferences.Add(subFingerprintReference);
            }

            var actualTrack = this.TrackDao.ReadTrackByISRC(tagInfo.ISRC);
            Assert.IsNotNull(actualTrack);
            this.AssertTracksAreEqual(track, actualTrack);

            // Act
            int modifiedRows = this.TrackDao.DeleteTrack(trackReference);

            Assert.IsNull(this.TrackDao.ReadTrackByISRC(tagInfo.ISRC));
            foreach (var id in subFingerprintReferences)
            {
                Assert.IsTrue(id.GetHashCode() != 0);
                Assert.IsNull(this.SubFingerprintDao.ReadSubFingerprint(id));
            }
 
            Assert.IsTrue(this.HashBinDao.ReadHashedFingerprintsByTrackReference(actualTrack.TrackReference).Count == 0);
            Assert.AreEqual(1 + hashData.Count + (25 * hashData.Count), modifiedRows);
        }

        [TestMethod]
        public void InserTrackShouldAcceptEmptyEntriesCodes()
        {
            TrackData track = new TrackData(string.Empty, string.Empty, string.Empty, string.Empty, 1986, 200);
            var trackReference = this.TrackDao.InsertTrack(track);

            var actualTrack = this.TrackDao.ReadTrack(trackReference);

            this.AssertModelReferenceIsInitialized(trackReference);
            this.AssertTracksAreEqual(track, actualTrack);
        }

        private List<TrackData> InsertTracks(int trackCount)
        {
            var tracks = new List<TrackData>();
            for (int i = 0; i < trackCount; i++)
            {
                var track = this.GetTrack();
                tracks.Add(track);
                this.TrackDao.InsertTrack(track);
            }

            return tracks;
        }

        private TrackData GetTrack()
        {
            return new TrackData(Guid.NewGuid().ToString(), "artist", "title", "album", 1986, 360)
                { 
                    GroupId = Guid.NewGuid().ToString().Substring(0, 20) // db max length
                };
        }
    }
}
