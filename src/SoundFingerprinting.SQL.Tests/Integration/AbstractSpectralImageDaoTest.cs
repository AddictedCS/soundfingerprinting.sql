namespace SoundFingerprinting.MongoDb.Tests.Integration
{
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using SoundFingerprinting.Audio;
    using SoundFingerprinting.Audio.NAudio;
    using SoundFingerprinting.Configuration;
    using SoundFingerprinting.DAO;
    using SoundFingerprinting.DAO.Data;
    using SoundFingerprinting.FFT;
    using SoundFingerprinting.Tests.Integration;
    using SoundFingerprinting.Utils;

    [TestClass]
    public abstract class AbstractSpectralImageDaoTest : AbstractIntegrationTest
    {
        private readonly IAudioService audioService;
        private readonly ISpectrumService spectrumService;

        protected AbstractSpectralImageDaoTest()
        {
            this.audioService = new NAudioService();
            this.spectrumService = new SpectrumService();
        }

        public abstract ISpectralImageDao SpectralImageDao { get; set; }

        public abstract ITrackDao TrackDao { get; set; }

        [TestMethod]
        public void TestSpectralImagesAreInsertedInDataSource()
        {
            TrackData track = new TrackData("isrc", "artist", "title", "album", 1986, 200);
            var trackReference = this.TrackDao.InsertTrack(track);
            var audioSamples = this.audioService.ReadMonoSamplesFromFile(
                PathToMp3, FingerprintConfiguration.Default.SampleRate);
            var spectralImages = this.spectrumService.CreateLogSpectrogram(audioSamples, SpectrogramConfig.Default);
            var concatenatedSpectralImages = new List<float[]>();
            foreach (var spectralImage in spectralImages)
            {
                var concatenatedSpectralImage = ArrayUtils.ConcatenateDoubleDimensionalArray(spectralImage.Image);
                concatenatedSpectralImages.Add(concatenatedSpectralImage);
            }
            
            this.SpectralImageDao.InsertSpectralImages(concatenatedSpectralImages, trackReference);

            var readSpectralImages = this.SpectralImageDao.GetSpectralImagesByTrackId(trackReference);
            Assert.AreEqual(concatenatedSpectralImages.Count, readSpectralImages.Count);
            foreach (var readSpectralImage in readSpectralImages)
            {
                var expectedSpectralImage = concatenatedSpectralImages[readSpectralImage.OrderNumber];
                for (int i = 0; i < expectedSpectralImage.Length; i++)
                {
                    Assert.AreEqual(
                        concatenatedSpectralImages[readSpectralImage.OrderNumber][i], expectedSpectralImage[i]);
                }
            }
        }
    }
}
