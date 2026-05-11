using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace IconChanger;

internal class Program
{
    private const ushort RT_ICON = 3;
    private const ushort RT_GROUP_ICON = 14;
    private const uint LOAD_LIBRARY_AS_DATAFILE = 0x00000002;

    private static readonly List<ResId> _resourcesToDelete = new();
    private static IntPtr _enumHModule;

    private readonly record struct ResId(IntPtr Type, IntPtr Name, ushort Language);
    private readonly record struct IconImage(byte Width, byte Height, byte ColorCount, byte Reserved, ushort Planes, ushort BitCount, uint BytesInRes, uint ImageOffset);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr BeginUpdateResource(string pFileName, [MarshalAs(UnmanagedType.Bool)] bool bDeleteExistingResources);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool UpdateResource(IntPtr hUpdate, IntPtr lpType, IntPtr lpName, ushort wLanguage, IntPtr lpData, uint cbData);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool EndUpdateResource(IntPtr hUpdate, [MarshalAs(UnmanagedType.Bool)] bool fDiscard);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeLibrary(IntPtr hModule);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool EnumResourceNames(IntPtr hModule, IntPtr lpType, EnumResNameProc lpEnumFunc, IntPtr lParam);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool EnumResourceLanguages(IntPtr hModule, IntPtr lpType, IntPtr lpName, EnumResLangProc lpEnumFunc, IntPtr lParam);

    private delegate bool EnumResNameProc(IntPtr hModule, IntPtr lpType, IntPtr lpName, IntPtr lParam);
    private delegate bool EnumResLangProc(IntPtr hModule, IntPtr lpType, IntPtr lpName, ushort wLanguage, IntPtr lParam);

    static void Main(string[] args)
    {
        try
        {
            Run(args);
        }
        catch
        {
            // Silent failure
        }
    }

    static void Run(string[] args)
    {
        string baseDir = AppContext.BaseDirectory;
        string targetExe = Path.Combine(baseDir, "WebShell.exe");
        string iconPath = Path.Combine(baseDir, "webapp.ico");

        if (!File.Exists(targetExe) || !File.Exists(iconPath))
            return;

        // Wait for target to be unlocked
        for (int i = 0; i < 20; i++)
        {
            try
            {
                using var fs = new FileStream(targetExe, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                break;
            }
            catch (IOException)
            {
                if (i == 19)
                    return;
                Thread.Sleep(250);
            }
        }

        byte[] iconData = File.ReadAllBytes(iconPath);
        List<IconImage> images = ParseIcon(iconData);
        if (images.Count == 0)
            return;

        var resources = EnumerateIcons(targetExe);

        IntPtr hUpdate = BeginUpdateResource(targetExe, false);
        if (hUpdate == IntPtr.Zero)
            return;

        bool success = false;
        try
        {
            foreach (var res in resources)
            {
                UpdateResource(hUpdate, res.Type, res.Name, res.Language, IntPtr.Zero, 0);
            }

            success = WriteNewIcons(hUpdate, images, iconData);
        }
        finally
        {
            EndUpdateResource(hUpdate, !success);
        }
    }

    private static List<IconImage> ParseIcon(ReadOnlySpan<byte> data)
    {
        var list = new List<IconImage>();
        if (data.Length < 6)
            return list;

        ushort reserved = BinaryPrimitives.ReadUInt16LittleEndian(data);
        ushort type = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(2));
        ushort count = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(4));

        if (reserved != 0 || type != 1)
            return list;

        int offset = 6;
        for (int i = 0; i < count; i++)
        {
            if (offset + 16 > data.Length)
                break;

            var entry = data.Slice(offset, 16);
            byte w = entry[0];
            byte h = entry[1];
            byte cc = entry[2];
            byte r = entry[3];
            ushort planes = BinaryPrimitives.ReadUInt16LittleEndian(entry.Slice(4));
            ushort bitCount = BinaryPrimitives.ReadUInt16LittleEndian(entry.Slice(6));
            uint bytesInRes = BinaryPrimitives.ReadUInt32LittleEndian(entry.Slice(8));
            uint imageOffset = BinaryPrimitives.ReadUInt32LittleEndian(entry.Slice(12));

            list.Add(new IconImage(w, h, cc, r, planes, bitCount, bytesInRes, imageOffset));
            offset += 16;
        }

        return list;
    }

    private static List<ResId> EnumerateIcons(string exePath)
    {
        _resourcesToDelete.Clear();
        _enumHModule = LoadLibraryEx(exePath, IntPtr.Zero, LOAD_LIBRARY_AS_DATAFILE);
        if (_enumHModule == IntPtr.Zero)
            return new List<ResId>(_resourcesToDelete);

        try
        {
            EnumResourceNames(_enumHModule, (IntPtr)RT_ICON, EnumResNameCallback, IntPtr.Zero);
            EnumResourceNames(_enumHModule, (IntPtr)RT_GROUP_ICON, EnumResNameCallback, IntPtr.Zero);
        }
        finally
        {
            FreeLibrary(_enumHModule);
            _enumHModule = IntPtr.Zero;
        }

        return new List<ResId>(_resourcesToDelete);
    }

    private static bool EnumResNameCallback(IntPtr hModule, IntPtr lpType, IntPtr lpName, IntPtr lParam)
    {
        EnumResourceLanguages(hModule, lpType, lpName, EnumResLangCallback, IntPtr.Zero);
        return true;
    }

    private static bool EnumResLangCallback(IntPtr hModule, IntPtr lpType, IntPtr lpName, ushort wLanguage, IntPtr lParam)
    {
        _resourcesToDelete.Add(new ResId(lpType, lpName, wLanguage));
        return true;
    }

    private static bool WriteNewIcons(IntPtr hUpdate, List<IconImage> images, byte[] iconFileData)
    {
        int count = images.Count;
        int groupSize = 6 + (count * 14);
        byte[] groupData = new byte[groupSize];

        BinaryPrimitives.WriteUInt16LittleEndian(groupData.AsSpan(0), 0);
        BinaryPrimitives.WriteUInt16LittleEndian(groupData.AsSpan(2), 1);
        BinaryPrimitives.WriteUInt16LittleEndian(groupData.AsSpan(4), (ushort)count);

        int entryOffset = 6;
        for (int i = 0; i < count; i++)
        {
            var img = images[i];
            ushort id = (ushort)(i + 1);

            byte[] imgBytes = new byte[img.BytesInRes];
            Buffer.BlockCopy(iconFileData, (int)img.ImageOffset, imgBytes, 0, (int)img.BytesInRes);

            GCHandle handle = GCHandle.Alloc(imgBytes, GCHandleType.Pinned);
            try
            {
                if (!UpdateResource(hUpdate, (IntPtr)RT_ICON, (IntPtr)id, 0, handle.AddrOfPinnedObject(), img.BytesInRes))
                    return false;
            }
            finally
            {
                handle.Free();
            }

            var entrySpan = groupData.AsSpan(entryOffset, 14);
            entrySpan[0] = img.Width;
            entrySpan[1] = img.Height;
            entrySpan[2] = img.ColorCount;
            entrySpan[3] = img.Reserved;
            BinaryPrimitives.WriteUInt16LittleEndian(entrySpan.Slice(4), img.Planes);
            BinaryPrimitives.WriteUInt16LittleEndian(entrySpan.Slice(6), img.BitCount);
            BinaryPrimitives.WriteUInt32LittleEndian(entrySpan.Slice(8), img.BytesInRes);
            BinaryPrimitives.WriteUInt16LittleEndian(entrySpan.Slice(12), id);

            entryOffset += 14;
        }

        GCHandle groupHandle = GCHandle.Alloc(groupData, GCHandleType.Pinned);
        try
        {
            if (!UpdateResource(hUpdate, (IntPtr)RT_GROUP_ICON, (IntPtr)1, 0, groupHandle.AddrOfPinnedObject(), (uint)groupData.Length))
                return false;
        }
        finally
        {
            groupHandle.Free();
        }

        return true;
    }
}
