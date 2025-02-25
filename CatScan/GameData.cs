using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using BNpcName = Lumina.Excel.Sheets.BNpcName;
using DynamicEvent = Lumina.Excel.Sheets.DynamicEvent;
using Fate = Lumina.Excel.Sheets.Fate;
using Map = Lumina.Excel.Sheets.Map;
using TerritoryType = Lumina.Excel.Sheets.TerritoryType;

// Game data functions
public class GameData
{
    public struct ZoneData
    {
        public string Name = string.Empty;
        public uint MapId = uint.MaxValue;
        public float MapOffsetX = 0.0f;
        public float MapOffsetY = 0.0f;
        public float MapScale = 1.0f;

        public ZoneData() { }
    }

    private static Dictionary<int, ZoneData> _zoneData = new();
    private static string? _cachedZoneName = null;
    private static int _cachedZoneId = -1;

    private static Lumina.Excel.ExcelSheet<TerritoryType> _territoryExcel = null!;
    private static Lumina.Excel.ExcelSheet<Map> _mapExcel = null!;

    private static Dictionary<uint, string> _bnpcNameIdToString = new();
    private static Dictionary<uint, string> _fateIdToString = new();
    private static Dictionary<uint, string> _ceIdToString = new();

    // Debug
    internal static int BNpcNameCacheSize => _bnpcNameIdToString.Count;
    internal static int FateNameCacheSize => _fateIdToString.Count;
    internal static int CENameCacheSize => _ceIdToString.Count;
    internal static Dictionary<uint, string> BNpcNameCache => _bnpcNameIdToString;
    internal static Dictionary<uint, string> FateNameCache => _fateIdToString;
    internal static Dictionary<uint, string> CENameCache => _ceIdToString;

    public static bool IsEnglish;
    public static bool NameDataReady;

    public static void Initialize()
    {
        IsEnglish = (DalamudService.ClientState.ClientLanguage == Dalamud.Game.ClientLanguage.English);

        InitTerritoryData();

        Task.Run(() => {
            try
            {
                InitBNpcNameCache();
            }
            catch (Exception e)
            {
                DalamudService.Log.Error(e, "Failed to find initialize BNPC name cache");
            }

            try
            {
                InitFateNameCache();
            }
            catch (Exception e)
            {
                DalamudService.Log.Error(e, "Failed to find initialize FATE name cache");
            }

            try
            {
                InitCENameCache();
            }
            catch (Exception e)
            {
                DalamudService.Log.Error(e, "Failed to find initialize CE name cache");
            }

            NameDataReady = true;
        });
    }

    private static ZoneData CacheZoneData(int zoneId)
    {
        var zoneData = new ZoneData();

        if (_territoryExcel.TryGetRow((uint)zoneId, out var territoryRow))
        {
            zoneData.MapId = territoryRow.Map.RowId;
            zoneData.Name = territoryRow.PlaceName.ValueNullable?.Name.ToString() ?? "#" + zoneId;

            if (_mapExcel.TryGetRow(zoneData.MapId, out var mapRow))
            {
                zoneData.MapOffsetX = mapRow.OffsetX / -50.0f;
                zoneData.MapOffsetY = mapRow.OffsetY / -50.0f;
                zoneData.MapScale = mapRow.SizeFactor / 100.0f;
            }
        }

        _zoneData.Add(zoneId, zoneData);
        return zoneData;
    }

    public static ZoneData GetZoneData(int zoneId)
    {
        if (_zoneData.TryGetValue(zoneId, out var data))
        {
            return data;
        }
        else
        {
            return CacheZoneData(zoneId);
        }
    }

    private static void InitTerritoryData()
    {
        _territoryExcel = DalamudService.DataManager.GetExcelSheet<TerritoryType>();
        _mapExcel = DalamudService.DataManager.GetExcelSheet<Map>();

        // Pre-load data for known zones
        foreach (var territoryId in CatScan.HuntData.Zones.Keys)
            CacheZoneData(territoryId);
    }

    // This creates an index of all BattleNpc NameIDs which resolve to names we care about
    // See: GetBnpcName()
    private static void InitBNpcNameCache()
    {
        var bnpcNameExcel = DalamudService.DataManager.GetExcelSheet<BNpcName>(Dalamud.Game.ClientLanguage.English);

        var huntNamesList = new List<(byte[], string)>();
        var seenNames = new HashSet<string>();

        foreach (var zone in CatScan.HuntData.Zones.Values)
        {
            foreach (var mark in zone.Marks)
            {
                var lowerName = mark.Name.ToLower(System.Globalization.CultureInfo.InvariantCulture);
                if (seenNames.Contains(lowerName))
                    continue;
                seenNames.Add(lowerName);
                var byteArray = System.Text.Encoding.UTF8.GetBytes(lowerName);
                huntNamesList.Add(new(byteArray, mark.Name));
            }
        }

        // Doing this instead of comparing Strings halves the time this all takes
        // This pretty much works for every monster name except Ü-u-ü-u
        // Fortunately since that name is lowercase -- using ToLower() above catches it
        var fastStricmp = (System.ReadOnlySpan<byte> a, System.ReadOnlySpan<byte> b) =>
        {
            if (a.Length != b.Length)
                return false;

            int n = a.Length;

            // Masking with 0xDF drops the bit that distinguishes between
            // upper-case and lower-case characters in ASCII
            for (int i = 0; i < n; ++i)
                if ((a[i] & 0xDF) != (b[i] & 0xDF))
                    return false;

            return true;
        };

        var huntNames = huntNamesList.ToArray();

        foreach (var row in bnpcNameExcel)
        {
            foreach (var (nameByteArray, nameString) in huntNames)
            {
                if (fastStricmp(nameByteArray, row.Singular.Data.Span))
                    _bnpcNameIdToString.Add(row.RowId, nameString);
            }
        }
    }

    // FATE version of InitBnpcNameCache()
    // See: GetFateName()
    private static void InitFateNameCache()
    {
        var fateExcel = DalamudService.DataManager.GetExcelSheet<Fate>(Dalamud.Game.ClientLanguage.English);

        var fateNamesList = new List<(byte[], string)>();
        var seenNames = new HashSet<string>();

        var addFate = (string fateName) => {
            var lowerName = fateName.ToLower(System.Globalization.CultureInfo.InvariantCulture);
            if (seenNames.Contains(lowerName))
                return;
            seenNames.Add(lowerName);
            var byteArray = System.Text.Encoding.UTF8.GetBytes(lowerName);
            fateNamesList.Add(new(byteArray, fateName));
        };

        foreach (var fateName in CatScan.HuntData.EpicFates)
            addFate(fateName);

        foreach (var eurekaZone in CatScan.HuntData.EurekaZones.Values)
        {
            foreach (var nm in eurekaZone.NMs)
                addFate(nm.FateName);
        }

        var fastStricmp = (System.ReadOnlySpan<byte> a, System.ReadOnlySpan<byte> b) =>
        {
            if (a.Length != b.Length)
                return false;

            int n = a.Length;

            for (int i = 0; i < n; ++i)
                if ((a[i] & 0xDF) != (b[i] & 0xDF))
                    return false;

            return true;
        };

        var fateNames = fateNamesList.ToArray();

        foreach (var row in fateExcel)
        {
            foreach (var (nameByteArray, nameString) in fateNames)
            {
                if (fastStricmp(nameByteArray, row.Name.Data.Span))
                    _fateIdToString.Add(row.RowId, nameString);
            }
        }
    }

    // CE (bozja -- critical engagement) version of InitBnpcNameCache()
    // Unlike monsters and fates, all data can be loaded as there's only 32 CEs
    // See: GetCEName()
    private static void InitCENameCache()
    {
        var ceExcel = DalamudService.DataManager.GetExcelSheet<DynamicEvent>(Dalamud.Game.ClientLanguage.English);

        foreach (var row in ceExcel)
        {
            if (row.RowId != 0)
                _ceIdToString.Add(row.RowId, row.Name.ExtractText());
        }
    }

    // Returns the English name of a known NPC given its NameID
    // That is, one that appears somewhere inside of HuntData
    // For unknown IDs, returns null
    public static string? GetBNpcName(uint nameId)
    {
        _bnpcNameIdToString.TryGetValue(nameId, out var name);
        return name;
    }

    // Returns the English name of a known Fate given its NameID
    // That is, one that appears somewhere inside of HuntData
    // Unlike GetBNpcName, unknown IDs will be resolved if possible
    public static string? GetFateName(uint fateId)
    {
        _fateIdToString.TryGetValue(fateId, out var name);
        if (name == null)
        {
            var fateExcel = DalamudService.DataManager.GetExcelSheet<Fate>(Dalamud.Game.ClientLanguage.English);
            if (fateExcel.TryGetRow(fateId, out var fateRow))
                name = fateRow.Name.ExtractText();
        }
        return name;
    }

    // Returns the English name of a CE given its DynamicEventID
    public static string? GetCEName(uint dynamicEventId)
    {
        _ceIdToString.TryGetValue(dynamicEventId, out var name);
        return name;
    }

    // Given a known English NPC name, try to get the translated version
    public static string TranslateBNpcName(string englishName)
    {
        if (IsEnglish)
            return englishName;

        var bnpcNameExcel = DalamudService.DataManager.GetExcelSheet<BNpcName>();

        uint nameId = 0;

        foreach (var entry in _bnpcNameIdToString)
        {
            if (entry.Value == englishName)
            {
                nameId = entry.Key;
                break;
            }
        }

        if (!bnpcNameExcel.TryGetRow(nameId, out var nameRow))
            return englishName;

        var nameChars = nameRow.Singular.ToString().ToCharArray();

        // Try to imitate the upper-casing logic... Probably broken
        int n = nameChars.Length;
        bool ucnext = true;

        for (int i = 0; i < nameChars.Length; ++i)
        {
            if (ucnext)
            {
                nameChars[i] = char.ToUpper(nameChars[i], System.Globalization.CultureInfo.InvariantCulture);
                ucnext = false;
            }

            if (nameChars[i] == ' ')
                ucnext = true;
        }

        return new string(nameChars);
    }

    public static string? GetZoneName(int zoneId)
    {
        if (zoneId == _cachedZoneId)
            return _cachedZoneName;
        var territoryData = DalamudService.DataManager.GetExcelSheet<TerritoryType>();
        var territoryType = territoryData?.GetRowOrDefault((uint)zoneId);
        _cachedZoneName = territoryType?.PlaceName.ValueNullable?.Name.ToString();
        return _cachedZoneName;
    }
}
