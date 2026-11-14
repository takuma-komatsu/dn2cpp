namespace DefaultStructCtorLib;

public struct ExternalPrivate
{
    public int Value;
    public ExternalPrivate() { Value = 73; }
    public ExternalPrivate(int value) { Value = value; }
}
