using uLink;
using Oxide.Core;
using UnityEngine;
using RustExtended;
using System.Collections.Generic;
using System.Collections;

namespace Oxide.Plugins
{
    [Info("BWorldEditor", "systemXcrackedZ", "2.1.3R")]
    internal class BWorldEditor : RustLegacyPlugin
    {
        public static int AdminRank = 100;

        public class SpawnObject
        {
            public SpawnObject(string codeName, string gameObject, Position position, Rotation rotation)
            {
                CodeName = codeName;
                GameObject = gameObject;
                Position = position;
                Rotation = rotation;
            }

            public string CodeName { get; set; }
            public string GameObject { get; set; }
            public Position Position { get; set; }
            public Rotation Rotation { get; set; }
        }

        public class Position
        {
            public Position(float x, float y, float z)
            {
                X = x;
                Y = y;
                Z = z;
            }

            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }
        }
        public class Rotation
        {
            public Rotation(float x, float y, float z, float w)
            {
                X = x;
                Y = y;
                Z = z;
                W = w;
            }

            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }
            public float W { get; set; }
        }

        public List<SpawnObject> SpawnObjects;
        public Dictionary<string, GameObject> SpawnedObjects = new Dictionary<string, GameObject>();

        public static readonly List<string> ObjectsToSpawn = new List<string>
        { "hangar", "ghangar", "warehouse" };
        public static readonly List<string> FileNames = new List<string>
        { "locations.unity3d" };

        public List<AssetBundle> assetBundles = new List<AssetBundle>();
        public List<ulong> loadedPlayers = new List<ulong>();

        [ChatCommand("we")]
        private void OnCMD_WorldEditor(NetUser user, string cmd, string[] args)
        {
            if (!user.CanAdmin() && Users.GetBySteamID(user.userID).Rank < AdminRank)
            {
                return;
            }

            if (args.Length < 1)
            {
                SendMessage(user, "Список доступных команд: ");
                SendMessage(user, "---------------------------------------");
                SendMessage(user, "/we - список команд.");
                SendMessage(user, "---------------------------------------");
                SendMessage(user, "/we spawn - заспавнить объект.");
                SendMessage(user, "/we remove - удалить объект.");
                SendMessage(user, "/we configure - изменить объект.");
                SendMessage(user, "---------------------------------------");
                SendMessage(user, "/we list - список заспавненных объектов.");
                SendMessage(user, "/we spawnlist - список объектов для спавна.");
                SendMessage(user, "---------------------------------------");
                SendMessage(user, "/we spawner - меню создания объектов.");
                SendMessage(user, "/we manager - меню управления объектами.");
                SendMessage(user, "---------------------------------------");
                SendMessage(user, "/we getinfos - информация об объектах.");
                SendMessage(user, "/we reload - перезагрузить плагин.");
                SendMessage(user, "---------------------------------------");
                return;
            }

            switch (args[0])
            {
                case "spawn":
                    {
                        if (args.Length < 3)
                        {
                            SendMessage(user, "Использование команды: /we spawn <codename> <gameobject>");
                            SendMessage(user, "Пример: /we spawn \"build1\" \"hangar\"");
                            return;
                        }

                        var objectToSpawn = ObjectsToSpawn.Find(f => f == args[2]);
                        if (objectToSpawn == null)
                        { SendMessage(user, $"Не найден игровой объект с названием \"{args[2]}\"!"); return; }

                        var zRotation = 0f;

                        if (args[2] == "tower" || args[2] == "warehouse")
                        { zRotation = 90.0f; }

                        var playerPosition = user.playerClient.lastKnownPosition;
                        var spawnObject = new SpawnObject(args[1], args[2], new Position(playerPosition.x, playerPosition.y, playerPosition.z), new Rotation(0, zRotation, zRotation, 1));
                        SpawnObjects.Add(spawnObject);

                        SendMessage(user, $"Объект \"{args[2]}\" был успешно заспавнен под названием \"{args[1]}\"!");
                        SendMessage(user, $"Для удаления объекта - используйте: /we remove \"{args[1]}\"!");
                        SendMessage(user, $"Для изменения объекта - используйте: /we configure \"{args[1]}\"!");

                        var worldEditorVM = user.playerClient.gameObject.GetComponent<WorldEditorVM>();

                        worldEditorVM.SendRPC("CloseEditor");
                        worldEditorVM.SendRPC("OpenEditor", args[1]);

                        InstantiateObject(spawnObject);
                        SaveData(); return;
                    }
                case "remove":
                    {
                        if (args.Length < 2)
                        {
                            SendMessage(user, "Использование команды: /we remove <codename>");
                            SendMessage(user, "Пример: /we spawn \"build1\"");
                            return;
                        }

                        var spawnObject = SpawnObjects.Find(obj => obj.CodeName == args[1]);
                        if (spawnObject == null)
                        {
                            SendMessage(user, $"Объект \"{args[1]}\" не найден в списке заспавненных объектов!");
                            SendMessage(user, "Для спавна объекта - используйте: /we spawn.");
                            SendMessage(user, "Для того, чтобы посмотреть список заспавненных объектов - используйте: /we list.");
                            return;
                        }

                        SendMessage(user, $"Вы успешно удалили объект \"{args[1]}\"!");
                        DestroyObject(spawnObject);
                        SaveData(); return;
                    }
                case "configure":
                    {
                        if (args.Length < 2)
                        {
                            SendMessage(user, "Использование команды: /we configure <codename>");
                            SendMessage(user, "Пример: /we configure \"build1\"");
                            return;
                        }

                        var spawnObject = SpawnObjects.Find(obj => obj.CodeName == args[1]);
                        if (spawnObject == null)
                        {
                            SendMessage(user, $"Объект \"{args[1]}\" не найден в списке заспавненных объектов!");
                            SendMessage(user, "Для спавна объекта - используйте: /we spawn.");
                            SendMessage(user, "Для того, чтобы посмотреть список заспавненных объектов - используйте: /we list.");
                            return;
                        }

                        SendMessage(user, $"Вы открыли редактор объекта \"{args[1]}\"!");

                        var worldEditorVM = user.playerClient.gameObject.GetComponent<WorldEditorVM>();

                        worldEditorVM.SendRPC("CloseEditor");
                        worldEditorVM.SendRPC("OpenEditor", args[1]);

                        return;
                    }

                case "list":
                    {
                        SendMessage(user, "Список заспавненных объектов: ");
                        SendMessage(user, "---------------------------------------");

                        var i = 1;
                        foreach (var spawnedObject in SpawnedObjects)
                        {
                            SendMessage(user, $"{i}) \"{spawnedObject.Key}\" - \"{spawnedObject.Value.name}\"");
                            i++;
                        }

                        return;
                    }
                case "spawnlist":
                    {
                        SendMessage(user, "Список объектов для спавна: ");
                        SendMessage(user, "---------------------------------------");

                        var i = 1;
                        foreach (var objectToSpawn in ObjectsToSpawn)
                        {
                            SendMessage(user, $"{i}) {objectToSpawn}");
                            i++;
                        }
                        return;
                    }

                case "manager":
                    {
                        var worldEditorVM = user.playerClient.gameObject.GetComponent<WorldEditorVM>();

                        worldEditorVM.SendRPC("CloseEditor");
                        worldEditorVM.SendRPC("CloseSpawner");
                        worldEditorVM.SendRPC("CloseManager");

                        worldEditorVM.SendRPC("ClearSpawnedObjects");
                        foreach (var spawnedObject in SpawnedObjects)
                        {
                            worldEditorVM.SendRPC("AddSpawnedObject", spawnedObject.Key);
                        }
                        worldEditorVM.SendRPC("ClearLocationsGrid");
                        foreach (var objectToSpawn in ObjectsToSpawn)
                        {
                            worldEditorVM.SendRPC("AddLocation", objectToSpawn);
                        }

                        worldEditorVM.SendRPC("OpenManager");
                        return;
                    }
                case "spawner":
                    {
                        var worldEditorVM = user.playerClient.gameObject.GetComponent<WorldEditorVM>();

                        worldEditorVM.SendRPC("CloseEditor");
                        worldEditorVM.SendRPC("CloseSpawner");
                        worldEditorVM.SendRPC("CloseManager");

                        worldEditorVM.SendRPC("ClearSpawnedObjects");
                        foreach (var spawnedObject in SpawnedObjects)
                        {
                            worldEditorVM.SendRPC("AddSpawnedObject", spawnedObject.Key);
                        }
                        worldEditorVM.SendRPC("ClearLocationsGrid");
                        foreach (var objectToSpawn in ObjectsToSpawn)
                        {
                            worldEditorVM.SendRPC("AddLocation", objectToSpawn);
                        }

                        worldEditorVM.SendRPC("OpenSpawner");
                        return;
                    }

                case "getinfos":
                    {
                        foreach (var spawnObject in SpawnObjects)
                        {
                            SendMessage(user, $"{spawnObject.CodeName}: " +
                                $"P:{spawnObject.Position.X},{spawnObject.Position.Y},{spawnObject.Position.Z}, " +
                                $"R:{spawnObject.Rotation.Z},{spawnObject.Rotation.Y},{spawnObject.Rotation.Z},{spawnObject.Rotation.W}.");
                        }
                        return;
                    }
                case "reload":
                    {
                        foreach (var spawnObject in SpawnedObjects.Values)
                            NetCull.Destroy(spawnObject);

                        timer.Once(1.5f, () =>
                        {
                            LoadData();
                            
                            foreach (var spawnObject in SpawnObjects)
                            {
                                SpawnedObjects[spawnObject.CodeName] = NetCull.InstantiateStatic(spawnObject.GameObject,
                                new Vector3(spawnObject.Position.X, spawnObject.Position.Y, spawnObject.Position.Z),
                                new Quaternion(spawnObject.Rotation.X, spawnObject.Rotation.Y, spawnObject.Rotation.Z, spawnObject.Rotation.W));
                            }

                            SendMessage(user, "Конфиг плагина успешно перезагружен!");
                        });
                        return;
                    }
            }
        }

        private void LoadVM(PlayerClient playerClient)
        {
            if (playerClient != null && playerClient.netPlayer != null)
            {
                var worldEditorVM = playerClient.gameObject.GetComponent<WorldEditorVM>();
                if (worldEditorVM == null)
                {
                    var playerVM = playerClient.gameObject.AddComponent<WorldEditorVM>();
                    playerVM.playerClient = playerClient;
                    playerVM.bWorldEditor = this;
                    loadedPlayers.Add(playerClient.userID);
                }
            }
        }
        private void UnloadVM(PlayerClient playerClient)
        {
            if (playerClient != null && playerClient.netPlayer != null)
            {
                var worldEditorVM = playerClient.gameObject.GetComponent<WorldEditorVM>();
                if (worldEditorVM != null)
                {
                    UnityEngine.Object.Destroy(worldEditorVM);
                }
            }
        }

        private void OnPlayerConnected(NetUser user)
        {
            var playerClient = user.playerClient;
            if (playerClient != null)
            {
                LoadVM(playerClient);
            }
        }
        private void OnPlayerDisconnected(uLink.NetworkPlayer player)
        {
            var user = NetUser.Find(player);
            if (user != null)
                loadedPlayers.Remove(user.userID);
        }

        private void Loaded()
        {
            try
            {
                LoadData();
            }
            catch
            {
                SpawnObjects = new List<SpawnObject>();
            }

            foreach (var playerClient in PlayerClient.All)
            {
                LoadVM(playerClient);
            }

            var loaderObject = new GameObject().AddComponent<AssetLoader>();
            loaderObject.bWorldEditor = this;

            timer.Once(15f, () =>
            {
                foreach (var spawnObject in SpawnObjects)
                {
                    InstantiateObject(spawnObject);
                }
            });
        }
        private void Unload()
        {
            SaveData();
            foreach (var loadedPlayer in loadedPlayers)
            {
                UnloadVM(Helper.GetPlayerClient(loadedPlayer));
            }
            foreach (var spawnObject in SpawnedObjects)
            {
                NetCull.Destroy(spawnObject.Value);
            }
            for (var i = 0; i < assetBundles.Count; i++)
            {
                var assetBundle = assetBundles[i];
                if (assetBundle != null)
                {
                    assetBundle.Unload(true);
                }
            }
        }
        private void OnServerSave()
        {
            SaveData();
        }
        public void LoadData()
        {
            SpawnObjects = Interface.GetMod().DataFileSystem.ReadObject<List<SpawnObject>>("WorldEditorData");
        }
        public void SaveData()
        {
            Interface.GetMod().DataFileSystem.WriteObject("WorldEditorData", SpawnObjects);
        }

        public void InstantiateObject(SpawnObject spawnObject)
        {
            SpawnedObjects.Add(spawnObject.CodeName, NetCull.InstantiateStatic(spawnObject.GameObject,
                new Vector3(spawnObject.Position.X, spawnObject.Position.Y, spawnObject.Position.Z),
                new Quaternion(spawnObject.Rotation.X, spawnObject.Rotation.Y, spawnObject.Rotation.Z, spawnObject.Rotation.W)));
        }
        public void DestroyObject(SpawnObject spawnObject)
        {
            NetCull.Destroy(SpawnedObjects[spawnObject.CodeName]);
            SpawnedObjects.Remove(spawnObject.CodeName);
            SpawnObjects.Remove(spawnObject);
        }

        private void SendMessage(NetUser user, string message)
        {
            rust.SendChatMessage(user, "WorldEditor", message);
        }

        internal class AssetLoader : UnityEngine.MonoBehaviour
        {
            public BWorldEditor bWorldEditor;
            private void Start()
            {
                StartCoroutine(LoadingAsset());
            }
            private IEnumerator LoadingAsset()
            {
                using (var www = new WWW("http://rage.hostfun.top/project/darkrust/locations.unity3d"))
                {
                    yield return www;

                    var assetBundle = www.assetBundle;
                    foreach (var obj in assetBundle.LoadAll())
                    {
                        if (obj.GetType().ToString() == "UnityEngine.GameObject")
                        {
                            var gameObject = obj as GameObject;
                            if (gameObject != null)
                            {
                                gameObject.AddComponent<Facepunch.NetworkView>();
                                NetworkInstantiator.AddPrefab(gameObject, true);

                                bWorldEditor.assetBundles.Add(assetBundle);
                            }
                        }
                    }
                    Debug.Log($"[WorldEditor]: Locations Successfully Loaded!");
                }
            }
        }
        internal class WorldEditorVM : UnityEngine.MonoBehaviour
        {
            public PlayerClient playerClient;
            public BWorldEditor bWorldEditor;

            [RPC]
            public void ChangeRotation(string gameObjectName, float x, float y, float z)
            {
                if (!bWorldEditor.SpawnedObjects.ContainsKey(gameObjectName))
                {
                    SendError("Объекта который вы пытаетесь отредактировать не существует!");
                    SendRPC("CloseEditor");
                    return;
                }
                var gameObject = bWorldEditor.SpawnedObjects[gameObjectName];

                var objectRotation = gameObject.transform.rotation;
                var objectPosition = gameObject.transform.position;
                var newRotation = new Quaternion(objectRotation.x + x, objectRotation.y + y, objectRotation.z + z, objectRotation.w);

                NetCull.Destroy(gameObject);
                bWorldEditor.SpawnedObjects[gameObjectName] = NetCull.InstantiateStatic(gameObject.name.Replace("(Clone)", ""), objectPosition, newRotation);

                var spawnObject = bWorldEditor.SpawnObjects.Find(f => f.CodeName == gameObjectName);
                spawnObject.Rotation = new Rotation(newRotation.x, newRotation.y, newRotation.z, newRotation.w);
            }
            [RPC]
            public void ChangePosition(string gameObjectName, float x, float y, float z)
            {
                if (!bWorldEditor.SpawnedObjects.ContainsKey(gameObjectName))
                {
                    SendError("Объекта который вы пытаетесь отредактировать не существует!");
                    SendRPC("CloseEditor");
                    return;
                }
                var gameObject = bWorldEditor.SpawnedObjects[gameObjectName];

                var objectRotation = gameObject.transform.rotation;
                var objectPosition = gameObject.transform.position;
                var newPosition = new Vector3(objectPosition.x + x, objectPosition.y + y, objectPosition.z + z);

                NetCull.Destroy(gameObject);
                bWorldEditor.SpawnedObjects[gameObjectName] = NetCull.InstantiateStatic(gameObject.name.Replace("(Clone)", ""), newPosition, objectRotation);

                var spawnObject = bWorldEditor.SpawnObjects.Find(f => f.CodeName == gameObjectName);
                spawnObject.Position = new Position(newPosition.x, newPosition.y, newPosition.z);
            }

            [RPC]
            public void SpawnObject(string codeName, string gameObjectName)
            {
                if (bWorldEditor.SpawnedObjects.ContainsKey(codeName))
                {
                    SendError("Объект с данным названием уже существует!");
                    SendRPC("CloseSpawner");
                    return;
                }
                var zRotation = 0.0f;

                if (gameObjectName == "warehouse")
                { zRotation = 90.0f; }

                var spawnObject = new SpawnObject(codeName, gameObjectName, new Position(playerClient.lastKnownPosition.x, playerClient.lastKnownPosition.y, playerClient.lastKnownPosition.z), new Rotation(0, zRotation, zRotation, 1));
                bWorldEditor.SpawnObjects.Add(spawnObject);

                bWorldEditor.InstantiateObject(spawnObject);
                bWorldEditor.SaveData();
            }
            [RPC]
            public void RemoveObject(string codeName)
            {
                var spawnObject = bWorldEditor.SpawnObjects.Find(obj => obj.CodeName == codeName);
                if (spawnObject == null)
                {
                    SendError("Объекта который вы пытаетесь удалить не существует!");
                    SendRPC("CloseManager");
                    return;
                }
                bWorldEditor.DestroyObject(spawnObject);
                bWorldEditor.SaveData();
            }

            [RPC]
            public void SendError(string message)
            {
                ConsoleNetworker.SendClientCommand(playerClient.netPlayer, $"chat.add WorldEditor {message.Quote()}");
            }

            public void SendRPC(string rpcName, params object[] args)
            {
                playerClient.gameObject.GetComponent<Facepunch.NetworkView>().RPC(rpcName, playerClient.netPlayer, args);
            }
        }
    }
}