namespace ReflectReturnLib;

public unsafe class PointerFieldBase
{
    public int* Address = (int*)0x120;
}

public unsafe sealed class PointerFieldOnly : PointerFieldBase
{
    public static void* Shared = (void*)0x340;
}

public static unsafe class PreservedPointerField
{
    public static int* Address = (int*)0x560;
}
