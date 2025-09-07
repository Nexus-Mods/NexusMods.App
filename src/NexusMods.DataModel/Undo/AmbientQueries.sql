-- namespace: NexusMods.DataModel.Undo

CREATE SCHEMA IF NOT EXISTS undo;

-- Snapshots based on the last applied revision
CREATE MACRO undo.LastAppliedRevisions(Db, LoadoutId) AS TABLE
SELECT T TxId FROM mdb_Datoms(Db=>db, History=>true, A=>'Loadout/LastAppliedDateTime')
WHERE E = LoadoutId AND IsRetract = false;

-- Snapshots based on explicit snapshot datoms
CREATE MACRO undo.LoadoutSnapshots(Db, LoadoutId) AS TABLE
SELECT E TxId FROM mdb_Datoms(Db=>Db, A=>'LoadoutSnapshot/Snapshot') 
         WHERE V = LoadoutId;       

-- Find all revisions of a given loadout
CREATE MACRO undo.LoadoutRevisionsWithMetadata(Db, LoadoutId) AS TABLE
SELECT DISTINCT Id, Timestamp
FROM mdb_Transaction(Db=>db)
WHERE Id IN (
    SELECT TxId FROM undo.LastAppliedRevisions(db, LoadoutId)
    UNION
    SELECT TxId FROM undo.LoadoutSnapshots(db, LoadoutId))
ORDER BY Timestamp DESC;       
