using UnityEngine;
using RustExtended;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;

namespace Oxide.Plugins
{
    using Random = Core.Random;

    [Info("BWoodCutters", "systemXcrackedZ", "1.2.3")]
    public class BWoodCutters : RustLegacyPlugin
    {
        private const int MaxWood = 12;
        private const int MaxPlanks = 3;
        private const float SpawnTime = 15.0f;
        private const float RecycleTime = 5.0f;
        
        private const int PillarRowsCount = 7;

        private StructureMaster _master;

        private readonly List<ulong> _inZonePlayers = new List<ulong>();
        private readonly List<ulong> _hatchetCutters = new List<ulong>();
        private readonly List<ulong> _pickaxeCutters = new List<ulong>();
        private readonly List<ulong> _recyclePlayers = new List<ulong>();

        private readonly List<GameObject> _pillars = new List<GameObject>();
        
        [ChatCommand("spp")]
        private void CMD_SpawnPillars(NetUser user, string cmd, string[] args)
        {
            if (!user.admin) return;
            var y = .5f;
            var position = user.playerClient.lastKnownPosition;

            if (_master == null)
            {
                _master = NetCull.InstantiateClassic(Facepunch.Bundling.Load<StructureMaster>("content/structures/StructureMasterPrefab"), position, new Quaternion(90, 90, 0, 1), 0);
                _master.SetupCreator(user.playerClient.controllable);
            }

            const int pillarsCount = PillarRowsCount * PillarRowsCount;
            var rotate = user.playerClient.transform.rotation;
            for (var i = 1; i <= pillarsCount; i++)
            {
                
                var obj = NetCull.InstantiateStatic(";struct_wood_pillar", position + new Vector3(0, y, i % PillarRowsCount * 0.65f), new Quaternion(rotate.x + 90, rotate.y + 90, rotate.z, rotate.w));
                _master.AddStructureComponent(obj.GetComponent<StructureComponent>());
                _pillars.Add(obj);
                if (i % PillarRowsCount == 0 && i != 0) y += 0.55f;
            }
        }
        [ChatCommand("dspp")]
        private void CMD_DestroySpawnedPillars(NetUser user, string cmd, string[] args)
        {
            if (!user.admin) return;
            foreach (var structure in new HashSet<StructureComponent>(_master._structureComponents))
                if (structure != null) NetCull.Destroy(structure.gameObject);

            _master._structureComponents.Clear();
            _pillars.Clear();
            NetCull.Destroy(_master.gameObject);
        }
        [ChatCommand("rspp")]
        private void CMD_RotateSpawnedPillars(NetUser user, string cmd, string[] args)
        {
            if (!user.admin) return;
            var arg1 = float.Parse(args[0]);
            var arg2 = float.Parse(args[1]);
            var arg31 = float.Parse(args[2]);
            var arg331 = float.Parse(args[3]);

            foreach (var pillar in new List<GameObject>(_pillars))
            {
                var position = pillar.transform.position;

                NetCull.Destroy(pillar);
                _master._structureComponents.Remove(pillar.GetComponent<StructureComponent>());
                _pillars.Remove(pillar);

                var rotation = new Quaternion(arg1, arg2, arg31, arg331);

                var obj = NetCull.InstantiateStatic(";struct_wood_pillar", position, rotation);
                _master.AddStructureComponent(obj.GetComponent<StructureComponent>());
                _pillars.Add(obj.gameObject);
            }
        }
        [ChatCommand("uspp")]
        private void CMD_UpSpawnedPillars(NetUser user, string cmd, string[] args)
        {
            if (!user.admin) return;
            var arg1 = float.Parse(args[0]);

            foreach (var pillar in new List<GameObject>(_pillars))
            {
                var position = pillar.transform.position;
                var rotation = pillar.transform.rotation; ;

                NetCull.Destroy(pillar);
                _master._structureComponents.Remove(pillar.GetComponent<StructureComponent>());
                _pillars.Remove(pillar);

                var obj = NetCull.InstantiateStatic(";struct_wood_pillar", position + new Vector3(0, arg1), rotation);
                _master.AddStructureComponent(obj.GetComponent<StructureComponent>());
                _pillars.Add(obj.gameObject);
            }
        }
        
        private object ModifyDamage(TakeDamage takeDamage, DamageEvent damage)
        {
            try
            {
                if (takeDamage is ProtectionTakeDamage)
                {
                    var user = damage.attacker.client.netUser;
                    if (user == null) return null;

                    var inv = user.playerClient.rootControllable.idMain.GetComponent<Inventory>();
                    if (inv == null) return null;

                    var structureComponent = damage.victim.idMain as StructureComponent;
                    if (structureComponent == null) return null;

                    if (!_pillars.Contains(structureComponent.gameObject)) return null;

                    var inventoryItem = inv._activeItem?.datablock?.name;
                    if (inventoryItem != null)
                    {
                        switch (inventoryItem)
                        {
                            case "Hatchet":
                                if (_hatchetCutters.Contains(user.userID) || _pickaxeCutters.Contains(user.userID))
                                {
                                    rust.Notice(user, "Вы уже взяли древесину для переработки!");
                                    return null;
                                }
                                _hatchetCutters.Add(user.userID);
                                rust.Notice(user, "Вы взяли бревно для переработки в древесину! Отнесите его на станок.");
                                break;
                            case "Pick Axe":
                                if (_hatchetCutters.Contains(user.userID) || _pickaxeCutters.Contains(user.userID))
                                {
                                    rust.Notice(user, "Вы уже взяли древесину для переработки!");
                                    return null;
                                }
                                _pickaxeCutters.Add(user.userID);
                                rust.Notice(user, "Вы взяли бревно для переработки в планки! Отнесите его на станок.");
                                break;
                        }

                        var position = structureComponent.transform.position;
                        var rotation = structureComponent.transform.rotation;

                        NetCull.Destroy(_pillars[_pillars.IndexOf(structureComponent.gameObject)]);

                        _master._structureComponents.Remove(structureComponent);
                        _pillars.Remove(structureComponent.gameObject);

                        timer.Once(SpawnTime, () =>
                        {
                            var obj = NetCull.InstantiateStatic(";struct_wood_pillar", position, rotation);
                            _master.AddStructureComponent(obj.GetComponent<StructureComponent>());
                            _pillars.Add(obj.gameObject);
                        });

                        return damage;
                    }
                }
            }
            catch
            {
                // ignored
            }
            return null;
        }
        private void OnGetClientMove(HumanController controller, Vector3 newPos)
        {
            var user = controller.netUser;
            if (user == null) return;

            var zone = Zones.Get(newPos);
            if (zone != null)
            {
                var zoneName = zone.Name;

                switch (zoneName)
                {
                    case "HatchetCutters":
                    {
                        if (!_recyclePlayers.Contains(user.userID) && !_inZonePlayers.Contains(user.userID))
                        {
                            _inZonePlayers.Add(user.userID);
                            if (!_hatchetCutters.Contains(user.userID))
                            {
                                rust.Notice(user, "Для работы на данном станке - возьмите древесину для переработки топором.");
                                return;
                            }

                            rust.Notice(user, $"Началась переработка бревна в древесину! Ждите: {RecycleTime} секунд.");
                            _recyclePlayers.Add(user.userID);

                            timer.Once(RecycleTime, () =>
                            {
                                if (_recyclePlayers.Contains(user.userID))
                                {
                                    _recyclePlayers.Remove(user.userID);
                                    var rand = Random.Range(1, MaxWood);
                                    rust.Notice(user,
                                        $"Вы успешно переработали древесину и получили: {rand} древесины!");
                                    Helper.GiveItem(user.playerClient, "Wood", rand);

                                    _inZonePlayers.Remove(user.userID);
                                    _hatchetCutters.Remove(user.userID);
                                }
                            });
                        }
                        return;
                    }
                    case "PickAxeCutters":
                    {
                        if (!_recyclePlayers.Contains(user.userID) && !_inZonePlayers.Contains(user.userID))
                        {
                            _inZonePlayers.Add(user.userID);
                            if (!_pickaxeCutters.Contains(user.userID))
                            {
                                rust.Notice(user, "Для работы на данном станке - возьмите древесину для переработки киркой.");
                                return;
                            }

                            rust.Notice(user, $"Началась переработка бревна в планки! Ждите: {RecycleTime} секунд.");
                            _recyclePlayers.Add(user.userID);

                            timer.Once(RecycleTime, () =>
                            {
                                if (_recyclePlayers.Contains(user.userID))
                                {
                                    _recyclePlayers.Remove(user.userID);

                                    var rand = Random.Range(1, MaxPlanks);
                                    rust.Notice(user, $"Вы успешно переработали древесину и получили: {rand} планок!");
                                    Helper.GiveItem(user.playerClient, "Wood Planks", rand);

                                    _inZonePlayers.Remove(user.userID);
                                    _pickaxeCutters.Remove(user.userID);
                                }
                            });
                        }
                        return;
                    }
                }
                if (_inZonePlayers.Contains(user.userID)) _inZonePlayers.Remove(user.userID);
                if (_recyclePlayers.Contains(user.userID))
                {
                    if (_pickaxeCutters.Contains(user.userID)) _pickaxeCutters.Remove(user.userID);
                    if (_hatchetCutters.Contains(user.userID)) _hatchetCutters.Remove(user.userID);
                    _recyclePlayers.Remove(user.userID);
                    rust.Notice(user, "Вы испортили древесину!");
                }
            }
            else
            {
                if (_inZonePlayers.Contains(user.userID)) _inZonePlayers.Remove(user.userID);

                if (!_recyclePlayers.Contains(user.userID)) return;

                _recyclePlayers.Remove(user.userID);
                rust.Notice(user, "Вы испортили древесину!");
                if (_pickaxeCutters.Contains(user.userID)) _pickaxeCutters.Remove(user.userID);
                if (_hatchetCutters.Contains(user.userID)) _hatchetCutters.Remove(user.userID);
            }
        }
    }
}