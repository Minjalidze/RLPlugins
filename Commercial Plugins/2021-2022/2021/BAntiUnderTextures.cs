using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    public class BAntiUnderTextures : RustLegacyPlugin
    {
        private const float Distance = 1f;

        private static readonly string[] ForbiddenTextures = 
        {
            "Barricade_Fence_Deployable(Clone)",
            "Furnace(Clone)"
        };

        private void OnItemDeployed(DeployableObject deployableObject, IDeployableItem deployableItem)
        {
            if (!ForbiddenTextures.Contains(deployableObject.name) || !IsUnderTexture(
                deployableObject.transform.position, deployableItem.character.playerClient.lastKnownPosition)) return;
            
            deployableItem.character.GetComponent<Inventory>().AddItemAmount(deployableItem.datablock, 1);
            timer.Once(0.01f, () => NetCull.Destroy(deployableObject.gameObject));
        }

        private static bool IsUnderTexture(Vector3 deployablePosition, Vector3 playerPosition) => Vector3.Distance(deployablePosition, playerPosition) <= Distance;
    }
}