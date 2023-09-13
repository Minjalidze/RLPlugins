namespace Oxide.Plugins
{
    [Info("BAirHeight", "systemXcrackedZ", "5.7.2")]
    internal class BAirHeight : RustLegacyPlugin
    {
        private float increaseValue = 400.0f;
        private bool isIncrease = true;

        [ChatCommand("setinc")]
        private void SetInc(NetUser user, string cmd, string[] args)
        {
            if (user.admin)
            {
                isIncrease = !isIncrease;
                string status = isIncrease ? "Включено" : "Выключено";
                rust.SendChatMessage(user, "AirHeight", $"Изменение высоты({increaseValue}): {status}");
            }
        }

        private void OnAirdropTargetReached(SupplyDropPlane drop)
        {
            if (!isIncrease) return;
            drop.gameObject.transform.SetLocalPositionY(drop.transform.position.y + increaseValue);
        }
    }
}
