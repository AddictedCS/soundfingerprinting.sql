## Sound Fingerprinting MSSQL
_soundfingerprinting.sql_ persistent storage implementation which allows storing [soundfingerprinting](https://github.com/AddictedCS/soundfingerprinting) algorithm's data objects in _MSSQL_ database. 
## Usage
The MSSQL database initialization script can be find [here](src/Scripts/DBScript.sql). Do not forget to add connection string `FingerprintConnectionString` in your `app.config` filex
```xml
<connectionStrings>
    <add name="FingerprintConnectionString" connectionString="Data Source=(local);Initial Catalog=FingerprintsDb;Integrated Security=True; Connection Timeout=15;" providerName="System.Data.SqlClient"/>
</connectionStrings>
```
Use `SqlModelService` class when fingerprinting and querying
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
### Binaries
    git clone git@github.com:AddictedCS/soundfingerprinting.sql.git
In order to build latest version of the `SoundFingerprinting.SQL` assembly run the following command from repository root
    .\build.cmd
### Get it on NuGet
    Install-Package SoundFingerprinting.SQL
	
### Contribute
If you want to contribute you are welcome to open issues or discuss on [issues](https://github.com/AddictedCS/soundfingerprinting/issues) page. Feel free to contact me for any remarks, ideas, bug reports etc. 

### Licence
The framework is provided under [MIT](https://opensource.org/licenses/MIT) licence agreement.
