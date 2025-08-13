using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.BuiltInEntities;


namespace NexusMods.DataModel.Undo;

public static class Queries
{
    private const string LoadoutAppliedRevisions = "SELECT T FROM mdb_Datoms(Db=>$1, History=>true, A=>'Loadout/LastAppliedDateTime') WHERE E = $2 AND IsRetract = false";
    private const string LoadoutSnapshots = "SELECT E FROM mdb_Datoms(Db=>$1, A=>'LoadoutSnapshot/Snapshot') WHERE V = $2";
    public const string LoadoutRevisionsWithMetadata = $"""
                                                        SELECT DISTINCT Id, Timestamp 
                                                        FROM mdb_Transaction(Db=>$1) 
                                                        WHERE Id IN ({LoadoutAppliedRevisions} 
                                                                    UNION ALL 
                                                                    {LoadoutSnapshots}) 
                                                        ORDER BY Timestamp DESC
                                                        """;
}
