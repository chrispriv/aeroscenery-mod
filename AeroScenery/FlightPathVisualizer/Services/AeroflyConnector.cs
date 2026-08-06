using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text.Json;

namespace AeroScenery.FlightPathVisualizer.Services
{
    //DEVL_k
    //This class is responsible for connecting to the Aerofly flight simulator and reading data from it using memory-mapped files. It provides methods to read various types of data (double, uint32, uint64, string) from the shared memory, as well as a method to convert heading values from radians to degrees.
    public class AeroflyConnector : IDisposable
    {
        private const string MemoryName = "AeroflyBridgeData";
        private const int MemorySize = 6768;

        private readonly MemoryMappedFile _mmf;
        private readonly MemoryMappedViewAccessor _accessor;

        private static Dictionary<string, int> _offsets;
        private static Dictionary<string, int> _lengths;

        public static AeroflyConnector TryCreate()
        {
            try
            {
                //DEVL_k FINAL PATH TO BE SET (json not integrated yet) AND MISSING ERROR HANDLING, if AeroflyBridge_offsets.json not found!
                // Lade Offsets nur einmal
                if (_offsets == null)
                {
                    //LoadOffsetsFromJson(@"C:\temp\AeroflyBridge_offsets.json");

                    var applicationPath = AeroSceneryManager.Instance.ApplicationPath;
                    LoadOffsetsFromJson(applicationPath + @"\Resources\external_dll\AeroflyBridge_offsets.json");
                }

                var mmf = MemoryMappedFile.OpenExisting(MemoryName);
                var accessor = mmf.CreateViewAccessor(0, MemorySize, MemoryMappedFileAccess.Read);
                return new AeroflyConnector(mmf, accessor);
            }
            catch
            {
                return null;
            }
        }

        private AeroflyConnector(MemoryMappedFile mmf, MemoryMappedViewAccessor accessor)
        {
            _mmf = mmf;
            _accessor = accessor;
        }

        public static void LoadOffsetsFromJson(string jsonPath)
        {
            string json = File.ReadAllText(jsonPath);
            var doc = JsonDocument.Parse(json);

            _offsets = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            _lengths = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            var variables = doc.RootElement.GetProperty("variables");

            foreach (var element in variables.EnumerateArray())
            {
                string name = element.GetProperty("name").GetString();
                int offset = element.GetProperty("byte_offset").GetInt32();
                int length = element.GetProperty("byte_length").GetInt32();

                _offsets[name] = offset;
                _lengths[name] = length;
            }
        }


        public int GetOffset(string variableName)
        {
            if (_offsets != null && _offsets.TryGetValue(variableName, out int offset))
                return offset;
            throw new KeyNotFoundException($"Offset für {variableName} nicht gefunden.");
        }

        //public double ReadDouble(int offset) => _accessor.ReadDouble(offset);
        public uint ReadUInt32(int offset) => _accessor.ReadUInt32(offset);
        public ulong ReadUInt64(int offset) => _accessor.ReadUInt64(offset);

        public double ReadDouble(string variableName)
        {
            if (!_offsets.ContainsKey(variableName))
                throw new KeyNotFoundException($"Variable not found: {variableName}");

            int offset = _offsets[variableName];
            return _accessor.ReadDouble(offset);
        }

        public string ReadString(string variableName)
        {
            if (!_offsets.ContainsKey(variableName))
                throw new KeyNotFoundException($"Variable not found: {variableName}");

            int offset = _offsets[variableName];
            int maxLength = _lengths.ContainsKey(variableName) ? _lengths[variableName] : 64;

            byte[] buffer = new byte[maxLength];
            _accessor.ReadArray(offset, buffer, 0, maxLength);

            int stringLength = Array.IndexOf(buffer, (byte)0);
            if (stringLength < 0) stringLength = maxLength;

            return Encoding.UTF8.GetString(buffer, 0, stringLength).Trim();
        }

        public void Dispose()
        {
            _accessor?.Dispose();
            _mmf?.Dispose();
        }



        public double ConvertAeroflyHeading(double headingRad)
        {
            double deg = -headingRad * 57.2958 + 90;
            return (deg < 0) ? deg + 360 : deg;
        }


    }

}
