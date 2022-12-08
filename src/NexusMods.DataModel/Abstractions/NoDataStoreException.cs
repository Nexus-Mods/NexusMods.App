namespace NexusMods.DataModel.Abstractions;

public class NoDataStoreException : Exception
{
    public NoDataStoreException() : base("Cannot create entity object without bound data store")
    {
        
    }
    
}