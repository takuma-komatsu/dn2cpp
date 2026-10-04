using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Lua;
using Lua.Standard;

namespace Dn2Cpp;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        var state = LuaState.Create();
        state.OpenStandardLibraries();
        await Language(state);
        await TablesAndLibraries(state);
        await Interop(state);
        await Coroutines(state);
        await Modules(state);
        await Errors(state);
        Console.WriteLine("LuaCSharp end");
        if (args.Length > 0 && args[0] == "before-standard-libraries")
            return;
        await StandardLibraries(state);
    }

    private static async Task StandardLibraries(LuaState state)
    {
        Console.WriteLine("== Lua standard libraries ==");
        Check("basic", await state.DoStringAsync("""
            local t = setmetatable({ value = 3 }, { __index = function() return 7 end })
            rawset(t, 'value', 4)
            local ok, answer = pcall(function() return assert(t.value == 4) end)
            local failed, message = xpcall(function() error('failure') end,
                function() return 'handled' end)
            return type(t), rawget(t, 'value'), t.missing, ok, answer, failed, message,
                tonumber('25'), select('#', 'a', 'b')
            """), "table", 4, 7, true, true, false, "handled", 25, 2);
        Check("io", await state.DoStringAsync("""
            io.write('lua stdout: ', 'bytes', '\n')
            io.stdout:flush()
            return io.type(io.stdout), io.type(io.stdin), io.type(io.stderr)
            """), "file", "file", "file");
        string path = Path.GetTempFileName();
        state.Environment["gateFile"] = path;
        try
        {
            Check("file io", await state.DoStringAsync("""
                local file = assert(io.open(gateFile, 'w+'))
                file:write('first line\n42')
                file:seek('set', 0)
                local first, rest = file:read('*l'), file:read('*a')
                file:close()
                local removed = os.remove(gateFile)
                return first, rest, io.type(file), removed
                """), "first line", "42", "closed file", true);
        }
        finally
        {
            File.Delete(path);
        }
        Check("os", await state.DoStringAsync("""
            local utc = os.date('!*t', 1719835200)
            return utc.year, utc.month, utc.day, utc.hour, utc.isdst,
                os.difftime(100, 40), os.date('!%Y-%m-%d', 1719835200)
            """), 2024, 7, 1, 12, false, 60, "2024-07-01");
        Check("debug", await state.DoStringAsync("""
            local function inspect() return debug.getinfo(1, 'S').what end
            return inspect()
            """), "Lua");
        Console.WriteLine("Lua standard libraries end");
    }

    private static async Task Language(LuaState state)
    {
        Check("arithmetic", await state.DoStringAsync("return 2 + 3 * 4, 2^5, 17 % 5, -3, 9 / 2"),
            14, 32, 2, -3, 4.5);
        Check("control", await state.DoStringAsync("""
            local sum = 0
            for i = 1, 6 do
                if i % 2 == 0 then sum = sum + i end
            end
            local n = 3
            while n > 0 do sum = sum + n; n = n - 1 end
            repeat sum = sum - 1 until sum == 15
            return sum, not false, false or 'fallback', true and 'chosen'
            """), 15, true, "fallback", "chosen");
        Check("closures", await state.DoStringAsync("""
            local function factorial(n)
                if n <= 1 then return 1 end
                return n * factorial(n - 1)
            end
            local function counter(n)
                return function(delta) n = n + delta; return n end
            end
            local c = counter(10)
            local function spread(...) return ... end
            return factorial(6), c(2), c(3), spread('tail', 9)
            """), 720, 12, 15, "tail", 9);
        Console.WriteLine("language end");
    }

    private static async Task TablesAndLibraries(LuaState state)
    {
        Check("tables", await state.DoStringAsync("""
            local t = { 4, 2, 3, name = 'items' }
            table.insert(t, 1, 5)
            local removed = table.remove(t, 3)
            table.sort(t)
            local sum = 0
            for i = 1, #t do sum = sum + t[i] end
            return #t, removed, sum, table.concat(t, ','), t.name
            """), 3, 2, 12, "3,4,5", "items");
        LuaValue[] metaValues = await state.DoStringAsync("""
            local mt = {
                __index = function(t, k) return 'missing:' .. k end,
                __add = function(a, b) return a.value + b.value end
            }
            return { value = 7 }, { value = 8 }, mt
            """);
        LuaTable a = metaValues[0].Read<LuaTable>();
        LuaTable b = metaValues[1].Read<LuaTable>();
        a.Metatable = metaValues[2].Read<LuaTable>();
        b.Metatable = a.Metatable;
        state.Environment["metaA"] = a;
        state.Environment["metaB"] = b;
        Check("metatables", await state.DoStringAsync("""
            local a, b = metaA, metaB
            a.extra = 4
            return a + b, a.absent, a.extra
            """), 15, "missing:absent", 4);
        Check("libraries", await state.DoStringAsync("""
            local word, number = string.match('abc:123', '(%a+):(%d+)')
            local replaced, count = string.gsub('a1b2', '%d', '#')
            return string.upper(word), number, replaced, count,
                string.sub('abcdef', 2, 4), math.floor(3.75), math.abs(-8),
                bit32.band(15, 6)
            """), "ABC", "123", "a#b#", 2, "bcd", 3, 8, 6);
        Console.WriteLine("tables and libraries end");
    }

    private static async Task Interop(LuaState state)
    {
        state.Environment["hostValue"] = 21;
        state.Environment["hostAdd"] = new LuaFunction("hostAdd", (context, ct) =>
            new ValueTask<int>(context.Return(context.GetArgument<double>(0)
                + context.GetArgument<double>(1), "host")));
        Check("host callback", await state.DoStringAsync("return hostAdd(hostValue, 4)"), 25, "host");
        LuaValue[] functions = await state.DoStringAsync("return function(a, b) return a * b, a - b end");
        Check("lua callback", await state.CallAsync(functions[0], new LuaValue[] { 6, 7 }), 42, -1);

        LuaValue[] values = await state.DoStringAsync("return { value = 3 }, nil, true, 'text', 2.5");
        LuaTable table = values[0].Read<LuaTable>();
        table["value"] = 8;
        state.Environment["hostTable"] = table;
        Check("value conversions", new LuaValue[]
        {
            table["value"].Read<double>(), values[1].Type == LuaValueType.Nil,
            values[2].Read<bool>(), values[3].Read<string>(), values[4].Read<double>(),
        }, 8, true, true, "text", 2.5);
        Check("table exchange", await state.DoStringAsync("hostTable.value = hostTable.value + 2; return hostTable.value"), 10);
        Check("table retained", new LuaValue[] { table["value"] }, 10);

        state.Environment["counter"] = new GateCounter { Value = 3 };
        Check("generated userdata", await state.DoStringAsync("""
            counter.value = 5
            local result = counter:add(4)
            local other = counter.create(2)
            local combined = counter + other
            return result, counter.value, combined.value
            """), 9, 9, 11);

        // The callback stays pending until the host releases it, forcing the VM
        // and its custom ValueTask builder through their suspension/resume path.
        var completion = new TaskCompletionSource<int>();
        state.Environment["hostAwait"] = new LuaFunction("hostAwait", async (context, ct) =>
        {
            int value = await completion.Task;
            return context.Return(value + context.GetArgument<int>(0));
        });
        ValueTask<LuaValue[]> pending = state.DoStringAsync("return hostAwait(2) * 3");
        if (pending.IsCompleted)
            throw new InvalidOperationException("The Lua callback must suspend before the host releases it.");
        completion.SetResult(5);
        Check("async callback", await pending, 21);
        Console.WriteLine("interop end");
    }

    private static async Task Coroutines(LuaState state)
    {
        Check("coroutines", await state.DoStringAsync("""
            local co = coroutine.create(function(seed)
                local delta = coroutine.yield(seed + 1, 'yielded')
                return seed + delta
            end)
            local before = coroutine.status(co)
            local ok1, first, tag = coroutine.resume(co, 10)
            local middle = coroutine.status(co)
            local ok2, last = coroutine.resume(co, 7)
            return before, ok1, first, tag, middle, ok2, last, coroutine.status(co)
            """), "suspended", true, 11, "yielded", "suspended", true, 17, "dead");
        Console.WriteLine("coroutines end");
    }

    private static async Task Modules(LuaState state)
    {
        var loader = new GateModuleLoader();
        state.ModuleLoader = loader;
        Check("require", await state.DoStringAsync("""
            local first = require('gate.module')
            local second = require('gate.module')
            return first.answer, first.add(2, 3), first == second
            """), 42, 5, true);
        Check("module cached", new LuaValue[] { loader.LoadCount }, 1);
        Console.WriteLine("modules end");
    }

    private static async Task Errors(LuaState state)
    {
        try
        {
            await state.DoStringAsync("local =", "compile-error");
            throw new InvalidOperationException("Invalid Lua must fail compilation.");
        }
        catch (LuaCompileException)
        {
            Console.WriteLine("compile error: LuaCompileException");
        }
        try
        {
            await state.DoStringAsync("return nil + 1", "runtime-error");
            throw new InvalidOperationException("Lua error must reach the host.");
        }
        catch (LuaRuntimeException)
        {
            Console.WriteLine("runtime error: LuaRuntimeException");
        }
        Check("recovery", await state.DoStringAsync("return 6 * 7"), 42);
        Console.WriteLine("errors end");
    }

    private static void Check(string label, LuaValue[] actual, params LuaValue[] expected)
    {
        if (actual.Length != expected.Length)
            throw new InvalidOperationException(label + ": wrong return count");
        Console.Write(label + ":");
        for (int i = 0; i < actual.Length; i++)
        {
            if (!actual[i].Equals(expected[i]))
                throw new InvalidOperationException(label + ": wrong value at " + i);
            Console.Write(" " + actual[i]);
        }
        Console.WriteLine();
    }
}
