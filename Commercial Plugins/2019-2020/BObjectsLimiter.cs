namespace Oxide.Plugins

{

    [Info("BObjectsLimiter", "systemXcrackedZ", "1.1.1")]

    [Description("Maded for Bless Rust with Love :)")]

    internal class BObjectsLimiter : RustLegacyPlugin

    {

        // РОМА ИДИ НАХУЙ

        private const int maxObjectsForBuild = 740;

        private void OnStructureBuilt(StructureComponent component, IStructureComponentItem item)

        {

            NetUser user = item.character.netUser;

            if (user == null || user.admin) return;



            int buildComponentCount = component._master._structureComponents.Count;
            var oName = component.gameObject.name.ToLower();
            if (buildComponentCount > maxObjectsForBuild && !oName.EndsWith("door") && !oName.EndsWith("ramp"))

            {

                timer.Once(0.01f, () => NetCull.Destroy(component.gameObject)); item.inventory.AddItemSomehow(item.datablock, Inventory.Slot.Kind.Belt, item.slot, 1);

                rust.Notice(user, $"Вы превысили максимальное количество объектов на одну постройку! ({buildComponentCount}/{maxObjectsForBuild})");

            }

        }

    }

}