using HotUpdateCoreLibBase;

namespace HotUpdateCoreLibPatch;

public static class InterpretedDelegateBindings
{
    public static string Run()
    {
        var menu = new CultureMenu();
        CultureHandler handler = menu.ApplyCulture;
        return menu.Inspect(menu.GetType, handler, handler(menu.Input()));
    }
}
