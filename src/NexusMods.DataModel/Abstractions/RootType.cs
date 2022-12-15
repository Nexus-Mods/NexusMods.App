namespace NexusMods.DataModel.Abstractions;

public enum RootType : byte
{
    ModLists = 0, // Mod Lists
    Tests, // Used in unit tests to validate the functionality of the datastore
}