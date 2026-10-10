using AeroScenery.Common;
using AeroScenery.Data.Models;
using System.Collections.Generic;

namespace AeroScenery.Data
{
    public interface IDataRepository
    {
        Settings Settings { get; set; }

        void CreateDatabase();

        List<GridSquare> GetAllGridSquares();

        void UpdateGridSquare(GridSquare gridSquare);

        /// <summary>Inserts a square after orthophoto download (Fixed = 1).</summary>
        void CreateGridSquare(GridSquare gridSquare);

        /// <summary>Inserts a square after an elevation-only download (Fixed = 0, ElevationDownloaded = 1).</summary>
        void CreateDataSquare(GridSquare gridSquare);

        void DeleteGridSquare(GridSquare gridSquare);

        void DeleteGridSquare(string gridSquareName);

        GridSquare FindGridSquare(string key);

        /// <summary>Writes Generate AID/TMC metadata; inserts the square if it does not exist yet.</summary>
        void SaveBuiltInConversion(GridSquare gridSquare);

        void SetOsmDownloaded(string gridSquareName);

        void SetElevationDownloaded(string gridSquareName);

        void UpgradeDatabase();
    }
}
