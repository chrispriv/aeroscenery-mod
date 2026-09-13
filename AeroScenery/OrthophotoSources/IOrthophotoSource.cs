using AeroScenery.AFS2;
using AeroScenery.Common;
using System.Collections.Generic;

namespace AeroScenery.OrthophotoSources
{
    public interface IOrthophotoSource
    {
        List<ImageTile> ImageTilesForGridSquares(AFS2GridSquare afs2GridSquare, int zoomLevel);
    }
}
