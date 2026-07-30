using System.Threading.Tasks;
using EFT;
using UnityEngine;

namespace BlackDiv.Components;

public class BotHeliCrewComponent : MonoBehaviour
{
    private bool _descent = false;
    public BotOwner botOwner;
    public ClankerController assignedHeli;

    void AssignHeli(ClankerController heli)
    {
        assignedHeli = heli;
        botOwner.OnBotStateChange += OnStateChange;
    }
    
    void OnStateChange(EBotState state)
    {
        if (state != EBotState.Active) return;
        
        botOwner.GetPlayer.HideWeapon();
        //BotOwner_0.GetPlayer.GetArmsAnimatorCommon().enabled = false;
        botOwner.gameObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        botOwner.gameObject.transform.localScale = Vector3.one;

        Task.Run(async () =>
        {
            await Task.Delay(1000);
            botOwner.Deactivate();
            botOwner.gameObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        });
    }
}