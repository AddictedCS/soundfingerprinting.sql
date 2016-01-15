## Sound Fingerprinting MSSQL
_soundfingerprinting.sql_ persistent storage implementation which allows storing [soundfingerprinting](https://github.com/AddictedCS/soundfingerprinting) algorithm's data objects in _MSSQL_ database. 
## Usage
The MSSQL database initialization script can be find [here](src/Scripts/DBScript.sql). Do not forget to add connection string <code>FingerprintConnectionString</code> in your app.config file.
```xml
<connectionStrings>
    <add name="FingerprintConnectionString" connectionString="Data Source=(local);Initial Catalog=FingerprintsDb;Integrated Security=True; Connection Timeout=15;" providerName="System.Data.SqlClient"/>
</connectionStrings>
```
Use <code>SqlModelService</code> class when fingerprinting and querying
```csharp
private readonly IModelService modelService = new SqlModelService(); // SQL back end
private readonly IAudioService audioService = new NAudioService(); // use NAudio audio processing library
private readonly IFingerprintCommandBuilder fingerprintCommandBuilder = new FingerprintCommandBuilder();

public void StoreAudioFileFingerprintsInStorageForLaterRetrieval(string pathToAudioFile)
{
    TrackData track = new TrackData("GBBKS1200164", "Adele", "Skyfall", "Skyfall", 2012, 290);
	
    // store track metadata in the database
    var trackReference = modelService.InsertTrack(track);

    // create sub-fingerprints and its hash representation
    var hashedFingerprints = fingerprintCommandBuilder
                                .BuildFingerprintCommand()
                                .From(pathToAudioFile)
                                .UsingServices(audioService)
                                .Hash()
                                .Result;
								
    // store sub-fingerprints and its hash representation in the database 
    modelService.InsertHashedFingerprintsForTrack(hashedFingerprints, trackReference); // insert in SQL backend
}
```
## Binaries
    git clone git@github.com:AddictedCS/soundfingerprinting.sql.git
In order to build latest version of the <code>SoundFingerprinting.SQL</code> assembly run the following command from repository root
    .\build.cmd
## Get it on NuGet
    Install-Package SoundFingerprinting.SQL -Pre
