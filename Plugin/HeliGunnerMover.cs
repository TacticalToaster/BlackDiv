using System.Threading.Tasks;
using BlackDiv.Controllers;
using EFT;
using UnityEngine;

namespace BlackDiv;

public class HeliGunnerMover : BotMover
{
    private int _gunnerIndex = -1;
    private ClankerController _heli;
    
    public HeliGunnerMover(BotOwner owner, Player player, AICoversData covers) : base(owner, player, covers)
    {
        BlackDivController instance = MonoBehaviourSingleton<BlackDivController>.Instance;
        if (instance != null)
        {
            _gunnerIndex = instance.RegisterGunner(owner);
            _heli = instance.GetHeli();
            Plugin.LogSource.LogWarning($"Gunner Mover Registered! {_gunnerIndex}");
        }
    }

    public override bool CheckCornerIndexByReachDist(float distCur, Vector3 position)
    {
        return true;
    }

    // Token: 0x0600204E RID: 8270 RVA: 0x0018C52E File Offset: 0x0018A72E
    public override void SetLastContex(float remainDist, Vector3 directionMove, Vector3 targetPos)
    {
    }

    // Token: 0x0600204F RID: 8271 RVA: 0x0018C530 File Offset: 0x0018A730
    public override void SetPose(float targetPose)
    {
    }

    // Token: 0x06002050 RID: 8272 RVA: 0x0018C532 File Offset: 0x0018A732
    public override void Sprint(bool val, bool withDebugCallback = true)
    {
    }

    // Token: 0x06002051 RID: 8273 RVA: 0x0018C534 File Offset: 0x0018A734
    public override void GoToByWay(Vector3[] way, float reachDist)
    {
    }

    // Token: 0x06002052 RID: 8274 RVA: 0x002F6A70 File Offset: 0x002F4C70
    public override void ManualUpdate()
    {
       method_18(); 
        
        BlackDivController instance = MonoBehaviourSingleton<BlackDivController>.Instance;
        
        if (instance != null &&  _gunnerIndex != -1 && instance.gunnerLeft != null)
        {
            Vector3 pos = _gunnerIndex == 0 ? instance.gunnerLeft.position : instance.gunnerRight.position;
            this.BotOwner_0.Mover.Teleport(pos);
        }
        //BotOwner_0.gameObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        this.BotOwner_0.SetPose(0.7f);
    }

    // Token: 0x06002053 RID: 8275 RVA: 0x0018C536 File Offset: 0x0018A736
    public override bool CheckIsOnPoint(Vector3 position)
    {
        return true;
    }
}