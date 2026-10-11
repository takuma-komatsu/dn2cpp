namespace FormalConstraintScope;

public interface ILibrary { }
public class Base : ILibrary { }
public static class Host
{
    public static void M<T>() where T : Base { }
}
