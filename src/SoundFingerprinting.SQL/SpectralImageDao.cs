namespace SoundFingerprinting.SQL
{
    using System.Collections.Generic;

    using SoundFingerprinting.DAO;
    using SoundFingerprinting.DAO.Data;

    internal class SpectralImageDao : ISpectralImageDao 
    {
        public void InsertSpectralImages(IEnumerable<float[]> spectralImages, IModelReference trackReference)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<SpectralImageData> GetSpectralImagesByTrackReference(IModelReference trackReference)
        {
            throw new System.NotImplementedException();
        }
    }
}
