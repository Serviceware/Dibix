namespace Dibix.Sdk.Abstractions
{
    public interface ILockEntryManager
    {
        bool HasEntry(string sectionName, string recordName);
        bool HasEntry(string sectionName, string groupName, string recordName, string signature);
    }
}