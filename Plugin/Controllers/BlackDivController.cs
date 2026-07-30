using System;
using System.Threading.Tasks;
using EFT;
using UnityEngine;
using WTTClientCommonLib;
using Random = UnityEngine.Random;

namespace BlackDiv.Controllers;

public class BlackDivController : MonoBehaviourSingleton<BlackDivController>
{
    private bool _heliFlyover = false;
    private ClankerController _heli;
    private GameObject _heliPrefab;
    private GameObject _crewAnims;
    private BotsController _botsController;

    private BotOwner[] _gunners = new BotOwner[] { null, null };
    public Transform gunnerLeft;
    public Transform gunnerRight;

    private void Start()
    {
        
    }

    public void InitRaid(BotsController botsController)
    {
        _botsController = botsController;

        botsController.Bots.OnBotRemove += OnGunnerDie;
        
        if (_heli != null)
            Destroy(_heli.gameObject);

        if (botsController == null)
            return;
        
        _heliPrefab = WTTClientCommonLib.WTTClientCommonLib.Instance.AssetLoader.LoadPrefabFromBundle("hh60", "HH60");
        _crewAnims = WTTClientCommonLib.WTTClientCommonLib.Instance.AssetLoader.LoadPrefabFromBundle("hh60", "AnimsRig");
        
        _heliFlyover = false;
        botsController.BotSpawner.OnBotCreated += CheckIfBD;
        Plugin.LogSource.LogInfo("Black Division Controller initialized!");
    }

    void CheckIfBD(BotOwner bot)
    {
        //Plugin.LogSource.LogInfo($"Checking if BD is {bot.Profile.Info.Settings.Role} {WildSpawnTypeExtensions.IsBlackDiv(bot.Profile.Info.Settings.Role)}");
        if (WildSpawnTypeExtensions.IsBlackDiv(bot.Profile.Info.Settings.Role))
        {
            if (_heliFlyover == false && Random.Range(0, 100) > 0)
            {
                TriggerHeli();
            }
        }
    }

    void TriggerHeli()
    {
        _heliFlyover = true;
        
        SpawnHeli();
    }

    void SpawnHeli()
    {
        if (_heliPrefab == null)
        {
            Plugin.LogSource.LogWarning("Heli prefab is null!");
            return;
        }
        Vector3 randomPos = new Vector3(Random.Range(-1200, -1000), 180, Random.Range(-1200, -1000));
        
        var heli = Instantiate(_heliPrefab, randomPos, Quaternion.identity);
        if (heli != null)
        {
            var controller = heli.GetComponent<ClankerController>();
            if (controller == null) return;
            _heli = controller;
            Plugin.LogSource.LogInfo($"Starting Heli Hover...");

            gunnerLeft = controller.GunnerLeft;
            gunnerRight = controller.GunnerRight;

            controller.HoverRadius = 35f;
            controller.HoverExitRadius = 40f;
            
            SpawnHeliGunners();
            Task.Run(async () =>
            {
                controller.SetTargetHover(controller.transform.position);
                await Task.Delay(1000 * 5);
                controller.SetTargetHover(new Vector3(0, 40, 0));
                //controller.StartHover(new Vector3(Random.Range(1200, 1000), 80, Random.Range(1200, 1000)), 120f);
                /*controller.OnStartedHover += () =>
                {
                    Destroy(controller.gameObject);
                    CleanGunners();
                };*/
                controller.OnHeliStartedHover += () =>
                {
                    //TestAnims();
                };
            });
        }
        else
        {
            Plugin.LogSource.LogWarning("Heli didn't spawn!");
        }
    }

    void TestAnims()
    {
        if (_gunners[0] == null || _gunners[1] == null) return;
        _gunners[0].gameObject.GetComponentInChildren<Animator>().SetTrigger("Go");
        Task.Run(async () =>
        {
            await Task.Delay(1000 * 5);
            _gunners[1].gameObject.GetComponentInChildren<Animator>().SetTrigger("Go");
        });
    }

    void OnStateChange(EBotState state)
    {
        if (state != EBotState.Active) return;
        
        
    }

    public ClankerController GetHeli()
    {
        return _heli;
    }

    public int RegisterGunner(BotOwner bot)
    {
        int gunnerSlot = -1;
        
        if (_gunners.Length > 0)
        {
            
            if (_gunners[0] != null)
            {
                _gunners[1] = bot;
                gunnerSlot = 1;
            }
            else
            {
                _gunners[0] = bot;
                gunnerSlot = 0;
            }
        }
        
        bot.GetPlayer.HideWeapon();

        var playerTransform = bot.gameObject.transform.Find("Player");
        var oldAnimator = playerTransform.GetComponent<Animator>();

        if (oldAnimator != null)
        {
            bot.Deactivate();
            
            var animPivot = gunnerSlot == 2 ? _heli.transform.Find("AnimPivotL") : _heli.transform.Find("AnimPivotR");
            bot.gameObject.transform.SetParent(animPivot);
            bot.gameObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            
            var newController = Instantiate(_crewAnims.GetComponent<Animator>().runtimeAnimatorController);
            
            oldAnimator.runtimeAnimatorController = newController;
            oldAnimator.SetBool("Stand", gunnerSlot == 1);
        }

        return gunnerSlot;
    }

    void CleanGunners()
    {
        foreach (var bot in _gunners)
        {
            if (bot != null)
            {
                bot.Dispose();
                Destroy(bot.gameObject);
            }
        }
        _gunners[0] = null;
        _gunners[1] = null;
    }

    void OnGunnerDie(BotOwner bot)
    {
        if (_gunners[0] != null && bot == _gunners[0])
        {
            _gunners[0] = null;
        }
        else if (_gunners[1] != null && bot == _gunners[1])
        {
            _gunners[1] = null;
        }
    }

    async Task SpawnHeliGunners()
    {
        BotSpawner spawner = _botsController.BotSpawner;

        await spawner.SpawnBotByTypeForce(1, (WildSpawnType)848425, BotDifficulty.normal, new BotSpawnParams()
        {
            ShallBeGroup = new ShallBeGroupParams(true, true, 1)
        });
        await spawner.SpawnBotByTypeForce(1, (WildSpawnType)848425, BotDifficulty.normal, new BotSpawnParams()
        {
            ShallBeGroup = new ShallBeGroupParams(true, true, 1)
        });
    }
}