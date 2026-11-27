using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;

namespace TamaHomeLaunch;

public static class BaudBypass
{
    public static int Apply(string dllPath)
    {
        if (!File.Exists(dllPath))
            throw new FileNotFoundException("System.IO.Ports.dll was not found.", dllPath);

        _ = System.Reflection.Assembly.LoadFrom(dllPath);
        var module = Process.GetCurrentProcess().Modules.Cast<ProcessModule>()
            .First(m => m.ModuleName.Equals("System.IO.Ports.dll", StringComparison.OrdinalIgnoreCase));

        using var stream = File.OpenRead(dllPath);
        using var pe = new PEReader(stream);
        var reader = pe.GetMetadataReader();
        var fieldToken = DwMaxBaudToken(reader);
        var patched = new HashSet<string>();
        foreach (var handle in reader.MethodDefinitions)
        {
            var method = reader.GetMethodDefinition(handle);
            var type = reader.GetTypeDefinition(method.GetDeclaringType());
            if (reader.GetString(type.Namespace) != "System.IO.Ports" || reader.GetString(type.Name) != "SerialStream")
                continue;
            var name = reader.GetString(method.Name);
            if (name is not (".ctor" or "set_BaudRate") || method.RelativeVirtualAddress == 0)
                continue;
            if (PatchMethod(module.BaseAddress, (int)method.RelativeVirtualAddress, name == ".ctor", fieldToken))
                patched.Add(name);
        }
        foreach (var name in new[] { ".ctor", "set_BaudRate" })
        {
            if (!patched.Contains(name))
                throw new InvalidOperationException($"The baud check in SerialStream.{name} was not found.");
        }
        return patched.Count;
    }

    public static void Log(string message)
    {
        try
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TamaHomeLaunch");
            Directory.CreateDirectory(dir);
            File.AppendAllText(Path.Combine(dir, "hook.log"), $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}{Environment.NewLine}");
        }
        catch
        {
        }
    }

    static byte[] DwMaxBaudToken(MetadataReader reader)
    {
        foreach (var handle in reader.FieldDefinitions)
        {
            var field = reader.GetFieldDefinition(handle);
            if (reader.GetString(field.Name) != "dwMaxBaud")
                continue;
            var owner = reader.GetTypeDefinition(field.GetDeclaringType());
            if (reader.GetString(owner.Name) != "COMMPROP")
                continue;
            return BitConverter.GetBytes(0x04000000 | MetadataTokens.GetRowNumber(handle));
        }
        throw new InvalidOperationException("COMMPROP.dwMaxBaud was not found.");
    }

    static bool PatchMethod(IntPtr imageBase, int rva, bool constructor, byte[] fieldToken)
    {
        var header = IntPtr.Add(imageBase, rva);
        var flags = Marshal.ReadByte(header);
        int codeOffset;
        int codeSize;
        if ((flags & 0x3) == 0x2)
        {
            codeOffset = 1;
            codeSize = flags >> 2;
        }
        else
        {
            codeOffset = 12;
            codeSize = Marshal.ReadInt32(IntPtr.Add(header, 4));
        }

        var code = new byte[codeSize];
        var codeAddress = IntPtr.Add(header, codeOffset);
        Marshal.Copy(codeAddress, code, 0, codeSize);
        var at = FindCheck(code, constructor, fieldToken);
        if (at < 0)
            return false;

        var length = constructor ? 12 : 13;
        var replacement = new byte[length];
        replacement[^1] = 0x2B;
        if (!VirtualProtect(IntPtr.Add(codeAddress, at), (nuint)length, 0x40, out var previous))
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        Marshal.Copy(replacement, 0, IntPtr.Add(codeAddress, at), length);
        VirtualProtect(IntPtr.Add(codeAddress, at), (nuint)length, previous, out _);
        return true;
    }

    static int FindCheck(byte[] code, bool constructor, byte[] fieldToken)
    {
        var width = constructor ? 12 : 13;
        for (var i = 0; i + width <= code.Length; i++)
        {
            if (constructor)
            {
                if (code[i] != 0x02 || code[i + 1] != 0x7C || code[i + 6] != 0x7B || code[i + 11] != 0x2C)
                    continue;
                if (!fieldToken.AsSpan().SequenceEqual(code.AsSpan(i + 7, 4)))
                    continue;
                return i;
            }
            if (code[i] != 0x03 || code[i + 1] != 0x02 || code[i + 2] != 0x7C || code[i + 7] != 0x7B || code[i + 12] != 0x31)
                continue;
            if (!fieldToken.AsSpan().SequenceEqual(code.AsSpan(i + 8, 4)))
                continue;
            return i;
        }
        return -1;
    }

    [DllImport("kernel32", SetLastError = true)]
    static extern bool VirtualProtect(IntPtr address, nuint size, uint newProtect, out uint oldProtect);
}
