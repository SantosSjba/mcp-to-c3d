using System.Text.Json.Nodes;

namespace Civil3DMcpPlugin;

/// <summary>
/// Routes JSON-RPC method names to the appropriate command handler.
/// Each method string maps directly to a static async method.
/// </summary>
public static class CommandDispatcher
{
  public static Task<object?> DispatchAsync(
    string method,
    JsonObject? parameters,
    CancellationToken cancellationToken)
  {
    return method switch
    {
      // Plugin / Health
      "getCivil3DHealth" => DrawingCommands.GetCivil3DHealthAsync(),

      // Drawing operations
      "getDrawingInfo" => DrawingCommands.GetDrawingInfoAsync(),
      "getDrawingSettings" => DrawingCommands.GetDrawingSettingsAsync(),
      "saveDrawing" => DrawingCommands.SaveDrawingAsync(parameters),
      "newDrawing" => DrawingCommands.NewDrawingAsync(parameters),
      "undoDrawing" => DrawingCommands.UndoDrawingAsync(parameters),
      "redoDrawing" => DrawingCommands.RedoDrawingAsync(parameters),
      "listCivilObjectTypes" => DrawingCommands.ListCivilObjectTypesAsync(),
      "getSelectedCivilObjectsInfo" => DrawingCommands.GetSelectedCivilObjectsInfoAsync(parameters),

      // Geometry (AutoCAD)
      "createLineSegment" => GeometryCommands.CreateLineSegmentAsync(parameters),
      "createPolyline" => GeometryCommands.CreatePolylineAsync(parameters),
      "create3dPolyline" => GeometryCommands.Create3dPolylineAsync(parameters),
      "createText" => GeometryCommands.CreateTextAsync(parameters),
      "createMText" => GeometryCommands.CreateMTextAsync(parameters),

      // COGO Points
      "listCogoPoints" => PointCommands.ListCogoPointsAsync(parameters),
      "getCogoPoint" => PointCommands.GetCogoPointAsync(parameters),
      "createCogoPoints" => PointCommands.CreateCogoPointsAsync(parameters),
      "deleteCogoPoints" => PointCommands.DeleteCogoPointsAsync(parameters),
      "listPointGroups" => PointCommands.ListPointGroupsAsync(),
      "importCogoPoints" => PointCommands.ImportCogoPointsAsync(parameters),

      // Surfaces
      "listSurfaces" => SurfaceCommands.ListSurfacesAsync(),
      "getSurface" => SurfaceCommands.GetSurfaceAsync(parameters),
      "getSurfaceElevation" => SurfaceCommands.GetSurfaceElevationAsync(parameters),
      "getSurfaceStatistics" => SurfaceCommands.GetSurfaceStatisticsAsync(parameters),
      "createSurface" => SurfaceCommands.CreateSurfaceAsync(parameters),
      "deleteSurface" => SurfaceCommands.DeleteSurfaceAsync(parameters),
      "addSurfacePoints" => SurfaceCommands.AddSurfacePointsAsync(parameters),
      "addSurfaceBreakline" => SurfaceCommands.AddSurfaceBreaklineAsync(parameters),
      "addSurfaceBoundary" => SurfaceCommands.AddSurfaceBoundaryAsync(parameters),
      "extractSurfaceContours" => SurfaceCommands.ExtractSurfaceContoursAsync(parameters),
      "computeSurfaceVolume" => SurfaceCommands.ComputeSurfaceVolumeAsync(parameters),

      // Alignments
      "listAlignments" => AlignmentCommands.ListAlignmentsAsync(),
      "getAlignment" => AlignmentCommands.GetAlignmentAsync(parameters),
      "createAlignment" => AlignmentCommands.CreateAlignmentAsync(parameters),
      "deleteAlignment" => AlignmentCommands.DeleteAlignmentAsync(parameters),
      "alignmentStationToPoint" => AlignmentCommands.StationToPointAsync(parameters),
      "alignmentPointToStation" => AlignmentCommands.PointToStationAsync(parameters),

      // Profiles
      "listProfiles" => ProfileCommands.ListProfilesAsync(parameters),
      "getProfile" => ProfileCommands.GetProfileAsync(parameters),
      "getProfileElevation" => ProfileCommands.GetProfileElevationAsync(parameters),
      "createProfileFromSurface" => ProfileCommands.CreateProfileFromSurfaceAsync(parameters),
      "createLayoutProfile" => ProfileCommands.CreateLayoutProfileAsync(parameters),
      "deleteProfile" => ProfileCommands.DeleteProfileAsync(parameters),

      // Corridors
      "listCorridors" => CorridorCommands.ListCorridorsAsync(),
      "getCorridor" => CorridorCommands.GetCorridorAsync(parameters),
      "rebuildCorridor" => CorridorCommands.RebuildCorridorAsync(parameters),
      "getCorridorSurfaces" => CorridorCommands.GetCorridorSurfacesAsync(parameters),
      "getCorridorFeatureLines" => CorridorCommands.GetCorridorFeatureLinesAsync(parameters),
      "computeCorridorVolumes" => CorridorCommands.ComputeCorridorVolumesAsync(parameters),

      // Pipe Networks
      "listPipeNetworks" => PipeNetworkCommands.ListPipeNetworksAsync(),
      "getPipeNetwork" => PipeNetworkCommands.GetPipeNetworkAsync(parameters),
      "getPipe" => PipeNetworkCommands.GetPipeAsync(parameters),
      "getStructure" => PipeNetworkCommands.GetStructureAsync(parameters),
      "createPipeNetwork" => PipeNetworkCommands.CreatePipeNetworkAsync(parameters),
      "addPipeToNetwork" => PipeNetworkCommands.AddPipeToNetworkAsync(parameters),
      "addStructureToNetwork" => PipeNetworkCommands.AddStructureToNetworkAsync(parameters),
      "checkPipeNetworkInterference" => PipeNetworkCommands.CheckPipeNetworkInterferenceAsync(parameters),

      // Unknown method
      _ => throw new JsonRpcDispatchException(
        "CIVIL3D.INVALID_INPUT",
        $"Plugin method '{method}' is not implemented yet."
      ),
    };
  }
}
