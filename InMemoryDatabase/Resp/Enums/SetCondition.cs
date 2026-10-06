namespace InMemoryDatabase.Resp.Enums
{
    public enum SetCondition
    {
        Always,
        // NX
        IfNotExists,
        // XX
        IfExists
    }
}
