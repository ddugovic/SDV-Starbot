using StarbotLib.Logging;
using StarbotLib.Pathfinding;
using StarbotLib.Threading;
using StarbotLib.World;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace StarbotLib
{
    [Serializable]
    public class StarbotServerCore : MarshalByRefObject
    {
        public string saveID;
        private bool started;
        public ThreadManager threads;
        public MapManager maps = null;
        public PathManager paths = null;
        public RouteManager routes = null;

        public StarbotServerCore()
        {
            started = false;
        }

        public void Initialize()
        {
            if (!started)
            {
                started = true;
                threads = new ThreadManager(this);
                maps = new MapManager(this);
                paths = new PathManager(this);
                routes = new RouteManager(this);
            }
        }

        public void Load(string saveID)
        {
            if (string.IsNullOrWhiteSpace(saveID))
            {
                throw new ArgumentNullException("Save ID required to load the starbot server.");
            }
            this.saveID = saveID;
            var filePath = GetSaveFilePath();
            try
            {
                using (Stream stream = File.Open(filePath, FileMode.Open))
                {
                    var binaryFormatter = new BinaryFormatter();
                    var saveObject = (StarbotServerSave)binaryFormatter.Deserialize(stream);
                    CLogger.Alert("SAVE LOADED.");
                    // Clear the existing cache
                    maps.mapCache.Clear();
                    // Create real values from the saved ones
                    foreach (var saveMap in saveObject.maps)
                    {
                        var realMap = maps.AddMap(saveMap.mapID);
                        realMap.minX = saveMap.minX;
                        realMap.minY = saveMap.minY;
                        realMap.maxX = saveMap.maxX;
                        realMap.maxY = saveMap.maxY;
                        foreach (var saveLocation in saveMap.locations)
                        {
                            var realLocation = realMap.AddLocation(saveLocation.x, saveLocation.y);
                            realLocation.type = saveLocation.type;
                            realLocation.SetPassable(saveLocation.passable);
                            if (saveLocation.worldObject != null)
                            {
                                realLocation.worldObject = new WorldObject()
                                {
                                    passable = saveLocation.worldObject.passable,
                                    name = saveLocation.worldObject.name,
                                    displayName = saveLocation.worldObject.displayName,
                                    description = saveLocation.worldObject.description,
                                    category = saveLocation.worldObject.category,
                                    actionable = saveLocation.worldObject.actionable
                                };
                            }
                        }
                    }
                    // Now that all the locations are loaded, connect the warps
                    foreach (var saveMap in saveObject.maps)
                    {
                        var realMap = maps.GetMap(saveMap.mapID);
                        foreach (var saveLocation in saveMap.locations.Where(sm => sm.warpTarget != null || sm.warpOrigin != null))
                        {
                            var realLocation = realMap.GetLocation(saveLocation.x, saveLocation.y);
                            if (saveLocation.warpTarget != null)
                            {
                                var realWarpMap = maps.GetMap(saveLocation.warpTarget.mapID);
                                realLocation.warpTarget = realWarpMap.GetLocation(saveLocation.warpTarget.x, saveLocation.warpTarget.y);
                            }
                            if (saveLocation.warpOrigin != null)
                            {
                                var realWarpMap = maps.GetMap(saveLocation.warpOrigin.mapID);
                                realLocation.warpOrigin = realWarpMap.GetLocation(saveLocation.warpOrigin.x, saveLocation.warpOrigin.y);
                            }
                        }
                    }
                    // Import previously calculated paths
                    routes.warpPathCache.Clear();
                    foreach (var savePath in saveObject.paths)
                    {
                        var startMap = maps.GetMap(savePath.start.mapID);
                        var startLocation = startMap.GetLocation(savePath.start.x, savePath.start.y);
                        var targetMap = maps.GetMap(savePath.target.mapID);
                        var targetLocation = startMap.GetLocation(savePath.target.x, savePath.target.y);
                        var realPath = paths.GeneratePathObject(startLocation, targetLocation, savePath.pathUntilTarget);
                        realPath.SetStatus(Pathfinding.Path.Status.NeedsValidation);
                        foreach (var savePathStep in savePath.steps)
                        {
                            realPath.steps.Add(new Step(startMap.GetLocation(savePathStep.x, savePathStep.y)));
                        }
                    }
                    //routes.warpPathCache = saveObject.paths;
                }
            }
            catch (Exception e)
            {
                CLogger.Error("error loading save.", e);
            }
        }

        public void Save()
        {
            lock (this)
            {
                CLogger.Alert("Running save.");
                var saveObject = new StarbotServerSave(saveID, maps.GetSavableMaps(), routes.GetSavablePaths());
                var filePath = GetSaveFilePath();
                try
                {
                    File.Delete(filePath);
                }
                catch (Exception e)
                {
                    CLogger.Error("error deleting original save.", e);
                }
                using (Stream stream = File.Open(filePath, FileMode.Create))
                {
                    new BinaryFormatter().Serialize(stream, saveObject);
                }
            }
        }

        private string GetSaveFilePath()
        {
            var directory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            return directory + System.IO.Path.DirectorySeparatorChar + saveID + ".starbot";
        }
    }
}
