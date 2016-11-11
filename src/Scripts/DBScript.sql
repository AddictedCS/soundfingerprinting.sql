USE master
IF EXISTS (SELECT NAME FROM sys.databases WHERE NAME = 'FingerprintsDb')
BEGIN
	DROP DATABASE FingerprintsDb
END
GO
CREATE DATABASE FingerprintsDb
GO
USE FingerprintsDb
GO
ALTER DATABASE FingerprintsDb SET RECOVERY SIMPLE;
GO
CHECKPOINT;
GO
CHECKPOINT; -- run twice to ensure file wrap-around
GO
DBCC SHRINKFILE(FingerprintsDb_log, 1024);
GO
-- TABLE WHICH WILL CONTAIN TRACK METADATA
CREATE TABLE Tracks
(
	Id INT IDENTITY(1, 1) NOT NULL,
	ISRC VARCHAR(50),
	Artist VARCHAR(255),
	Title VARCHAR(255),
	Album VARCHAR(255),
	ReleaseYear INT DEFAULT 0,
	TrackLengthSec FLOAT DEFAULT 0,
	GroupId VARCHAR(20),
	CONSTRAINT CK_TracksTrackLength CHECK(TrackLengthSec > -1),
	CONSTRAINT CK_ReleaseYear CHECK(ReleaseYear > -1),
	CONSTRAINT PK_TracksId PRIMARY KEY(Id)
)
GO 
-- TABLE WHICH CONTAINS ALL THE INFORMATION RELATED TO SUB-FINGERPRINTS
-- USED BY LSH+MINHASH SCHEMA
CREATE TABLE SubFingerprints
(
	Id BIGINT IDENTITY(1, 1) NOT NULL,
	TrackId INT NOT NULL,
	SequenceNumber INT NOT NULL,
	SequenceAt FLOAT NOT NULL,
    HashTable_0 BIGINT NOT NULL,
    HashTable_1 BIGINT NOT NULL,
    HashTable_2 BIGINT NOT NULL,
    HashTable_3 BIGINT NOT NULL,
    HashTable_4 BIGINT NOT NULL,
    HashTable_5 BIGINT NOT NULL,
    HashTable_6 BIGINT NOT NULL,
    HashTable_7 BIGINT NOT NULL,
    HashTable_8 BIGINT NOT NULL,
    HashTable_9 BIGINT NOT NULL,
    HashTable_10 BIGINT NOT NULL,
    HashTable_11 BIGINT NOT NULL,
    HashTable_12 BIGINT NOT NULL,
    HashTable_13 BIGINT NOT NULL,
    HashTable_14 BIGINT NOT NULL,
    HashTable_15 BIGINT NOT NULL,
    HashTable_16 BIGINT NOT NULL,
    HashTable_17 BIGINT NOT NULL,
    HashTable_18 BIGINT NOT NULL,
    HashTable_19 BIGINT NOT NULL,
    HashTable_20 BIGINT NOT NULL,
    HashTable_21 BIGINT NOT NULL,	
	HashTable_22 BIGINT NOT NULL,	
	HashTable_23 BIGINT NOT NULL,	
	HashTable_24 BIGINT NOT NULL,
	CONSTRAINT PK_SubFingerprintsId PRIMARY KEY(Id),
	CONSTRAINT FK_SubFingerprints_Tracks FOREIGN KEY (TrackId) REFERENCES dbo.Tracks(Id)
)
GO
-- TABLE FOR FINGERPRINTS (NEURAL NASHER)
CREATE TABLE Fingerprints
(
	Id INT IDENTITY(1,1) NOT NULL,
	Signature VARBINARY(4096) NOT NULL,
	TrackId INT NOT NULL,
	CONSTRAINT PK_FingerprintsId PRIMARY KEY(Id),
	CONSTRAINT FK_Fingerprints_Tracks FOREIGN KEY (TrackId) REFERENCES dbo.Tracks(Id)
)
GO
-- TABLE INDEXES
CREATE INDEX IX_TrackIdLookup ON Fingerprints(TrackId) 
GO
CREATE INDEX IX_TrackIdLookupOnSubfingerprints ON SubFingerprints(TrackId) 
GO
-- INSERT A TRACK INTO TRACKS TABLE
IF OBJECT_ID('sp_InsertTrack','P') IS NOT NULL
	DROP PROCEDURE sp_InsertTrack
GO
CREATE PROCEDURE sp_InsertTrack
	@ISRC VARCHAR(50),
	@Artist VARCHAR(255),
	@Title VARCHAR(255),
	@Album VARCHAR(255),
	@ReleaseYear INT,
	@TrackLengthSec FLOAT,
	@GroupId VARCHAR(20)
AS
INSERT INTO Tracks (
	ISRC,
	Artist,
	Title,
	Album,
	ReleaseYear,
	TrackLengthSec,
	GroupId
	) OUTPUT inserted.Id
VALUES
(
 	@ISRC, @Artist, @Title, @Album, @ReleaseYear, @TrackLengthSec, @GroupId
);
GO
-- INSERT INTO SUBFINGERPRINTS
IF OBJECT_ID('sp_InsertSubFingerprint','P') IS NOT NULL
	DROP PROCEDURE sp_InsertSubFingerprint
GO
CREATE PROCEDURE sp_InsertSubFingerprint
	@TrackId INT,
	@SequenceNumber INT,
	@SequenceAt FLOAT,
	@HashTable_0 BIGINT,
    @HashTable_1 BIGINT,
    @HashTable_2 BIGINT,
    @HashTable_3 BIGINT,
    @HashTable_4 BIGINT,
    @HashTable_5 BIGINT,
    @HashTable_6 BIGINT,
    @HashTable_7 BIGINT,
    @HashTable_8 BIGINT,
    @HashTable_9 BIGINT,
    @HashTable_10 BIGINT,
    @HashTable_11 BIGINT,
    @HashTable_12 BIGINT,
    @HashTable_13 BIGINT,
    @HashTable_14 BIGINT,
    @HashTable_15 BIGINT,
    @HashTable_16 BIGINT,
    @HashTable_17 BIGINT,
    @HashTable_18 BIGINT,
    @HashTable_19 BIGINT,
    @HashTable_20 BIGINT,
    @HashTable_21 BIGINT,	
	@HashTable_22 BIGINT,	
	@HashTable_23 BIGINT,	
	@HashTable_24 BIGINT
AS
BEGIN
INSERT INTO SubFingerprints (
	TrackId,
	SequenceNumber,
	SequenceAt,
	HashTable_0,
    HashTable_1,
    HashTable_2,
    HashTable_3,
    HashTable_4,
    HashTable_5,
    HashTable_6,
    HashTable_7,
    HashTable_8,
    HashTable_9,
    HashTable_10,
    HashTable_11,
    HashTable_12,
    HashTable_13,
    HashTable_14,
    HashTable_15,
    HashTable_16,
    HashTable_17,
    HashTable_18,
    HashTable_19,
    HashTable_20,
    HashTable_21,	
	HashTable_22,	
	HashTable_23,	
	HashTable_24
	) OUTPUT inserted.Id
VALUES
(
	@TrackId, @SequenceNumber, @SequenceAt, @HashTable_0, @HashTable_1, @HashTable_2, @HashTable_3, @HashTable_4, @HashTable_5, @HashTable_6,
    @HashTable_7, @HashTable_8, @HashTable_9, @HashTable_10, @HashTable_11, @HashTable_12, @HashTable_13, @HashTable_14, @HashTable_15,
    @HashTable_16, @HashTable_17, @HashTable_18, @HashTable_19, @HashTable_20, @HashTable_21, @HashTable_22, @HashTable_23, @HashTable_24
);
END
GO
-- INSERT A FINGERPRINT INTO FINGERPRINTS TABLE USED BY NEURAL HASHER
IF OBJECT_ID('sp_InsertFingerprint','P') IS NOT NULL
	DROP PROCEDURE sp_InsertFingerprint
GO
CREATE PROCEDURE sp_InsertFingerprint
	@Signature VARBINARY(4096),
	@TrackId INT
AS
BEGIN
INSERT INTO Fingerprints (
	Signature,
	TrackId
	) OUTPUT inserted.Id
VALUES
(
	@Signature, @TrackId
);
END
GO
-- READ ALL TRACKS FROM THE DATABASE
IF OBJECT_ID('sp_ReadTracks','P') IS NOT NULL
	DROP PROCEDURE sp_ReadTracks
GO
CREATE PROCEDURE sp_ReadTracks
AS
SELECT * FROM Tracks
GO
-- READ A TRACK BY ITS IDENTIFIER
IF OBJECT_ID('sp_ReadTrackById','P') IS NOT NULL
	DROP PROCEDURE sp_ReadTrackById
GO
CREATE PROCEDURE sp_ReadTrackById
	@Id INT
AS
SELECT * FROM Tracks WHERE Tracks.Id = @Id
GO
-- READ FINGERPRINTS BY TRACK ID
IF OBJECT_ID('sp_ReadFingerprintByTrackId','P') IS NOT NULL
	DROP PROCEDURE sp_ReadFingerprintByTrackId
GO
CREATE PROCEDURE sp_ReadFingerprintByTrackId
	@TrackId INT
AS
BEGIN
	SELECT * FROM Fingerprints WHERE TrackId = @TrackId
END
GO
--- ------------------------------------------------------------------------------------------------------------
--- READ HASHBINS BY HASHBINS AND THRESHOLD TABLE
--- ADDED 20.10.2013 CIUMAC SERGIU
--- E.g. [25;36;89;56...]
--- -----------------------------------------------------------------------------------------------------------
IF OBJECT_ID('sp_ReadFingerprintsByHashBinHashTableAndThreshold','P') IS NOT NULL
	DROP PROCEDURE sp_ReadFingerprintsByHashBinHashTableAndThreshold
GO
CREATE PROCEDURE sp_ReadFingerprintsByHashBinHashTableAndThreshold
	@HashBin_0 BIGINT, @HashBin_1 BIGINT, @HashBin_2 BIGINT, @HashBin_3 BIGINT, @HashBin_4 BIGINT, 
	@HashBin_5 BIGINT, @HashBin_6 BIGINT, @HashBin_7 BIGINT, @HashBin_8 BIGINT, @HashBin_9 BIGINT,
	@HashBin_10 BIGINT, @HashBin_11 BIGINT, @HashBin_12 BIGINT, @HashBin_13 BIGINT, @HashBin_14 BIGINT, 
	@HashBin_15 BIGINT, @HashBin_16 BIGINT, @HashBin_17 BIGINT, @HashBin_18 BIGINT, @HashBin_19 BIGINT,
	@HashBin_20 BIGINT, @HashBin_21 BIGINT, @HashBin_22 BIGINT, @HashBin_23 BIGINT, @HashBin_24 BIGINT,
	@Threshold INT
AS
SELECT * FROM SubFingerprints, 
	( SELECT Id FROM 
	   (
		SELECT Id FROM SubFingerprints WHERE HashTable_0 = @HashBin_0
		UNION ALL
		SELECT Id FROM SubFingerprints WHERE HashTable_1 = @HashBin_1
		UNION ALL
		SELECT Id FROM SubFingerprints WHERE HashTable_2 = @HashBin_2
		UNION ALL
		SELECT Id FROM SubFingerprints WHERE HashTable_3 = @HashBin_3
		UNION ALL
		SELECT Id FROM SubFingerprints WHERE HashTable_4 = @HashBin_4
		UNION ALL
		SELECT Id FROM SubFingerprints WHERE HashTable_5 = @HashBin_5
		UNION ALL
		SELECT Id FROM SubFingerprints WHERE HashTable_6 = @HashBin_6
		UNION ALL
		SELECT Id FROM SubFingerprints WHERE HashTable_7 = @HashBin_7
		UNION ALL
		SELECT Id FROM SubFingerprints WHERE HashTable_8 = @HashBin_8
		UNION ALL
		SELECT Id FROM SubFingerprints WHERE HashTable_9 = @HashBin_9
		UNION ALL
		SELECT Id FROM SubFingerprints WHERE HashTable_10 = @HashBin_10
		UNION ALL
		SELECT Id FROM SubFingerprints WHERE HashTable_11 = @HashBin_11
		UNION ALL
		SELECT Id FROM SubFingerprints WHERE HashTable_12 = @HashBin_12
		UNION ALL
		SELECT Id FROM SubFingerprints WHERE HashTable_13 = @HashBin_13
		UNION ALL
		SELECT Id FROM SubFingerprints WHERE HashTable_14 = @HashBin_14
		UNION ALL
		SELECT Id FROM SubFingerprints WHERE HashTable_15 = @HashBin_15
		UNION ALL
		SELECT Id FROM SubFingerprints WHERE HashTable_16 = @HashBin_16
		UNION ALL
		SELECT Id FROM SubFingerprints WHERE HashTable_17 = @HashBin_17
		UNION ALL
		SELECT Id FROM SubFingerprints WHERE HashTable_18 = @HashBin_18
		UNION ALL
		SELECT Id FROM SubFingerprints WHERE HashTable_19 = @HashBin_19
		UNION ALL
		SELECT Id FROM SubFingerprints WHERE HashTable_20 = @HashBin_20
		UNION ALL
		SELECT Id FROM SubFingerprints WHERE HashTable_21 = @HashBin_21
		UNION ALL
		SELECT Id FROM SubFingerprints WHERE HashTable_22 = @HashBin_22
		UNION ALL
		SELECT Id FROM SubFingerprints WHERE HashTable_23 = @HashBin_23
		UNION ALL
		SELECT Id FROM SubFingerprints WHERE HashTable_24 = @HashBin_24
	  ) AS Hashes
	 GROUP BY Hashes.Id
	 HAVING COUNT(Hashes.Id) >= @Threshold
	) AS Thresholded
WHERE SubFingerprints.Id = Thresholded.Id	
GO
IF OBJECT_ID('sp_ReadSubFingerprintsByHashBinHashTableAndThresholdWithGroupId','P') IS NOT NULL
	DROP PROCEDURE sp_ReadSubFingerprintsByHashBinHashTableAndThresholdWithGroupId
GO
CREATE PROCEDURE sp_ReadSubFingerprintsByHashBinHashTableAndThresholdWithGroupId
@HashBin_0 BIGINT, @HashBin_1 BIGINT, @HashBin_2 BIGINT, @HashBin_3 BIGINT, @HashBin_4 BIGINT, 
	@HashBin_5 BIGINT, @HashBin_6 BIGINT, @HashBin_7 BIGINT, @HashBin_8 BIGINT, @HashBin_9 BIGINT,
	@HashBin_10 BIGINT, @HashBin_11 BIGINT, @HashBin_12 BIGINT, @HashBin_13 BIGINT, @HashBin_14 BIGINT, 
	@HashBin_15 BIGINT, @HashBin_16 BIGINT, @HashBin_17 BIGINT, @HashBin_18 BIGINT, @HashBin_19 BIGINT,
	@HashBin_20 BIGINT, @HashBin_21 BIGINT, @HashBin_22 BIGINT, @HashBin_23 BIGINT, @HashBin_24 BIGINT,
	@Threshold INT, @GroupId VARCHAR(20)
AS
SELECT * FROM SubFingerprints
    INNER JOIN
	( SELECT Id FROM 
	   (
		SELECT Id FROM SubFingerprints WHERE HashTable_0 = @HashBin_0
		UNION ALL
		SELECT Id FROM SubFingerprints WHERE HashTable_1 = @HashBin_1
		UNION ALL
		SELECT Id FROM SubFingerprints WHERE HashTable_2 = @HashBin_2
		UNION ALL
		SELECT Id FROM SubFingerprints WHERE HashTable_3 = @HashBin_3
		UNION ALL
		SELECT Id FROM SubFingerprints WHERE HashTable_4 = @HashBin_4
		UNION ALL
		SELECT Id FROM SubFingerprints WHERE HashTable_5 = @HashBin_5
		UNION ALL
		SELECT Id FROM SubFingerprints WHERE HashTable_6 = @HashBin_6
		UNION ALL
		SELECT Id FROM SubFingerprints WHERE HashTable_7 = @HashBin_7
		UNION ALL
		SELECT Id FROM SubFingerprints WHERE HashTable_8 = @HashBin_8
		UNION ALL
		SELECT Id FROM SubFingerprints WHERE HashTable_9 = @HashBin_9
		UNION ALL
		SELECT Id FROM SubFingerprints WHERE HashTable_10 = @HashBin_10
		UNION ALL
		SELECT Id FROM SubFingerprints WHERE HashTable_11 = @HashBin_11
		UNION ALL
		SELECT Id FROM SubFingerprints WHERE HashTable_12 = @HashBin_12
		UNION ALL
		SELECT Id FROM SubFingerprints WHERE HashTable_13 = @HashBin_13
		UNION ALL
		SELECT Id FROM SubFingerprints WHERE HashTable_14 = @HashBin_14
		UNION ALL
		SELECT Id FROM SubFingerprints WHERE HashTable_15 = @HashBin_15
		UNION ALL
		SELECT Id FROM SubFingerprints WHERE HashTable_16 = @HashBin_16
		UNION ALL
		SELECT Id FROM SubFingerprints WHERE HashTable_17 = @HashBin_17
		UNION ALL
		SELECT Id FROM SubFingerprints WHERE HashTable_18 = @HashBin_18
		UNION ALL
		SELECT Id FROM SubFingerprints WHERE HashTable_19 = @HashBin_19
		UNION ALL
		SELECT Id FROM SubFingerprints WHERE HashTable_20 = @HashBin_20
		UNION ALL
		SELECT Id FROM SubFingerprints WHERE HashTable_21 = @HashBin_21
		UNION ALL
		SELECT Id FROM SubFingerprints WHERE HashTable_22 = @HashBin_22
		UNION ALL
		SELECT Id FROM SubFingerprints WHERE HashTable_23 = @HashBin_23
		UNION ALL
		SELECT Id FROM SubFingerprints WHERE HashTable_24 = @HashBin_24
	  ) AS Hashes
	 GROUP BY Hashes.Id
	 HAVING COUNT(Hashes.Id) >= @Threshold
	) AS Thresholded ON SubFingerprints.Id = Thresholded.Id	
INNER JOIN Tracks ON SubFingerprints.TrackId = Tracks.Id AND Tracks.GroupId = @GroupId
GO
IF OBJECT_ID('sp_ReadSubFingerprintsByTrackId','P') IS NOT NULL
	DROP PROCEDURE sp_ReadSubFingerprintsByTrackId
GO
CREATE PROCEDURE sp_ReadSubFingerprintsByTrackId
	@TrackId INT
AS
BEGIN
   SELECT * FROM SubFingerprints WHERE SubFingerprints.TrackId = @TrackId
END					 
-- READ TRACK BY ARTIST NAME AND SONG NAME
IF OBJECT_ID('sp_ReadTrackByArtistAndSongName','P') IS NOT NULL
	DROP PROCEDURE sp_ReadTrackByArtistAndSongName
GO
CREATE PROCEDURE sp_ReadTrackByArtistAndSongName
	@Artist VARCHAR(255),
	@Title VARCHAR(255) 
AS
SELECT * FROM Tracks WHERE Tracks.Title = @Title AND Tracks.Artist = @Artist
GO
-- READ TRACK BY ISRC
IF OBJECT_ID('sp_ReadTrackISRC','P') IS NOT NULL
	DROP PROCEDURE sp_ReadTrackISRC
GO
CREATE PROCEDURE sp_ReadTrackISRC
	@ISRC VARCHAR(50)
AS
SELECT * FROM Tracks WHERE Tracks.ISRC = @ISRC
GO
-- DELETE TRACK
IF OBJECT_ID('sp_DeleteTrack','P') IS NOT NULL
	DROP PROCEDURE sp_DeleteTrack
GO
CREATE PROCEDURE sp_DeleteTrack
	@Id INT
AS
BEGIN
	DELETE FROM SubFingerprints WHERE SubFingerprints.TrackId = @Id
	DELETE FROM Tracks WHERE Tracks.Id = @Id
END
GO
