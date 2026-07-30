using System;
using BlackDiv.Behavior.Actions;
using DrakiaXYZ.BigBrain.Brains;
using EFT;

namespace BlackDiv.Behavior.Layers;

public class HeliGunnerLayer : CustomLayer
{
    public Type lastAction;
    public Type nextAction;
    public string nextActionReason;
    
    public HeliGunnerLayer(BotOwner botOwner, int priority) : base(botOwner, priority)
    {
    }

    public override string GetName()
    {
        return "HeliGunner";
    }

    public override bool IsActive()
    {
        return true;
    }

    public void setNextAction(Type actionType, string reason)
    {
        nextAction = actionType;
        nextActionReason = reason;
    }
    
    public void getNextAction()
    {
        lastAction = nextAction;

        EnemyInfo goalEnemy = BotOwner.Memory.GoalEnemy;

        if (goalEnemy != null && goalEnemy.IsVisible)
        {
            nextAction = typeof(ShootFromPlaceAction);
            nextActionReason = "ShootEnemy";
        }
        
        nextAction = typeof(HoldPositionAction);
        nextActionReason = "NoEnemy";
    }

    public override Action GetNextAction()
    {
        EnemyInfo goalEnemy = BotOwner.Memory.GoalEnemy;

        if (goalEnemy != null && goalEnemy.IsVisible)
        {
            nextAction = typeof(ShootFromPlaceAction);
            nextActionReason = "ShootEnemy";
        }
        else
        {
            nextAction = typeof(HoldPositionAction);
            nextActionReason = "NoEnemy";
        }

        return new Action(nextAction, nextActionReason);
    }

    public override bool IsCurrentActionEnding()
    {
        Type currentAction = CurrentAction?.Type;

        if (currentAction == typeof(ShootFromPlaceAction))
        {
            return EndShootFromPlace();
        }
        
        if (currentAction == typeof(HoldPositionAction))
        {
            return EndHoldPosition();
        }
        
        return true; //nextAction != lastAction || (CurrentAction.Type != nextAction && CurrentAction.Type != lastAction);
    }

    public bool EndShootFromPlace()
    {
        EnemyInfo goalEnemy = BotOwner.Memory.GoalEnemy;

        if (goalEnemy != null && goalEnemy.IsVisible)
        {
            return false;
        }

        return true;
    }

    public bool EndHoldPosition()
    {
        return !EndShootFromPlace();
    }
}