namespace SoundFingerprinting.SQL.Tests.Integration
{
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using SoundFingerprinting.DAO;

    [TestClass]
    public class SpectralImageDaoTest : AbstractIntegrationTest
    {
        private readonly ISpectralImageDao spectralImageDao;

        public SpectralImageDaoTest()
        {
            spectralImageDao = new SpectralImageDao();
        }

        [TestMethod]
        [ExpectedException(typeof(System.NotImplementedException))]
        public void SpectralImagesAreInsertedInDataSourceTest()
        {
            spectralImageDao.InsertSpectralImages(new List<float[]>(), null);
        }
    }
}
