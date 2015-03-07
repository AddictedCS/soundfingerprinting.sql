namespace SoundFingerprinting.SQL
{
    using System;
    using System.Collections.Generic;
    using System.Data;

    using SoundFingerprinting.DAO;
    using SoundFingerprinting.DAO.Data;
    using SoundFingerprinting.Data;
    using SoundFingerprinting.Infrastructure;
    using SoundFingerprinting.SQL.Connection;
    using SoundFingerprinting.SQL.ORM;

    internal class TrackDao : AbstractDao, ITrackDao
    {
        private const string SpInsertTrack = "sp_InsertTrack";
        private const string SpReadTracks = "sp_ReadTracks";
        private const string SpReadTrackById = "sp_ReadTrackById";
        private const string SpReadTrackByArtistSongName = "sp_ReadTrackByArtistAndSongName";
        private const string SpDeleteTrack = "sp_DeleteTrack";
        private const string SpReadTrackByISRC = "sp_ReadTrackISRC";

        private readonly Action<TrackData, IReader> trackReferenceReader = (item, reader) => { item.TrackReference = new ModelReference<int>(reader.GetInt32("Id")); };

        public TrackDao()
            : base(
                DependencyResolver.Current.Get<IDatabaseProviderFactory>(),
                DependencyResolver.Current.Get<IModelBinderFactory>())
        {
            // no op
        }

        public TrackDao(IDatabaseProviderFactory databaseProvider, IModelBinderFactory modelBinderFactory)
            : base(databaseProvider, modelBinderFactory)
        {
        }

        public IModelReference InsertTrack(TrackData track)
        {
            int id = PrepareStoredProcedure(SpInsertTrack)
                            .WithParametersFromModel(track)
                            .Execute()
                            .AsScalar<int>();
            return track.TrackReference = new ModelReference<int>(id);
        }

        public IList<TrackData> ReadAll()
        {
            return PrepareStoredProcedure(SpReadTracks)
                        .Execute()
                        .AsListOfComplexModel(trackReferenceReader);
        }

        public TrackData ReadTrack(IModelReference trackReference)
        {
            return PrepareStoredProcedure(SpReadTrackById)
                        .WithParameter("Id", trackReference.Id, DbType.Int32)
                        .Execute()
                        .AsComplexModel(trackReferenceReader);
        }

        public IList<TrackData> ReadTrackByArtistAndTitleName(string artist, string title)
        {
            return PrepareStoredProcedure(SpReadTrackByArtistSongName)
                        .WithParameter("Artist", artist)
                        .WithParameter("Title", title)
                        .Execute()
                        .AsListOfComplexModel(trackReferenceReader);
        }

        public TrackData ReadTrackByISRC(string isrc)
        {
            return PrepareStoredProcedure(SpReadTrackByISRC)
                        .WithParameter("ISRC", isrc)
                        .Execute()
                        .AsComplexModel(trackReferenceReader);
        }

        public int DeleteTrack(IModelReference trackReference)
        {
            return PrepareStoredProcedure(SpDeleteTrack)
                        .WithParameter("Id", trackReference.Id, DbType.Int32)
                        .Execute()
                        .AsNonQuery();
        }
    }
}
