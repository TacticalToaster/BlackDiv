using System;
using DrakiaXYZ.BigBrain.Brains;
using EFT;

namespace BlackDiv.Behavior.Actions;

public class HoldPositionAction : CustomLogic
{
    private GClass278 baseLogic;
    
    public HoldPositionAction(BotOwner botOwner) : base(botOwner)
    {
        baseLogic = new GClass278(BotOwner);
    }

    public override void Update(CustomLayer.ActionData data)
    {
        baseLogic.UpdateNodeByBrain(null);
    }
}