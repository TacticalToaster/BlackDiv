using System;
using DrakiaXYZ.BigBrain.Brains;
using EFT;

namespace BlackDiv.Behavior.Actions;

public class ShootFromPlaceAction : CustomLogic
{
    private GClass276 baseLogic;
    
    public ShootFromPlaceAction(BotOwner botOwner) : base(botOwner)
    {
        baseLogic = new GClass276(BotOwner);
    }

    public override void Update(CustomLayer.ActionData data)
    {
        baseLogic.UpdateNodeByBrain(null);
    }
}