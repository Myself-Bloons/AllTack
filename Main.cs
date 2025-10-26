using System;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Api.ModOptions;
using BTD_Mod_Helper.Extensions;
using MelonLoader;
using UnityEngine;
using Il2CppAssets.Scripts.Models;
using Il2CppAssets.Scripts.Models.Towers;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Attack.Behaviors;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Emissions;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using AllTack;

[assembly: MelonInfo(typeof(AllTack.Main), ModHelperData.Name, ModHelperData.Version, ModHelperData.RepoOwner)]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]

namespace AllTack
{
    public class Main : BloonsTD6Mod
    {
        public static readonly ModSettingBool AllTackEnabled = new(true)
        {
            displayName = "All Tack Enabled",
            button = true,
            enabledText = "ON",
            disabledText = "OFF"
        };

        public static readonly ModSettingHotkey ToggleKey = new(KeyCode.F9)
        {
            displayName = "Toggle All Tack ON/OFF",
            description = "Makes towers projectiles fire like SuperMaelstrom."
        };

        private static GameModel? _lastPatchedModel;

        private static readonly Il2CppReferenceArray<EmissionBehaviorModel> EmptyBehaviors =
            new Il2CppReferenceArray<EmissionBehaviorModel>(0);

        public override void OnNewGameModel(GameModel model)
        {
            if (!AllTackEnabled) return;

            if (_lastPatchedModel != null && ReferenceEquals(_lastPatchedModel, model))
                return;
            _lastPatchedModel = model;

            foreach (var tower in model.towers)
            {
                if (tower == null) continue;
                if (tower.name.Contains("Farm")) continue;

                var attacks = tower.GetAttackModels();
                if (attacks.Count == 0) continue;

                float rangeFactor = 1f;

                foreach (var attack in attacks)
                {
                    attack.RemoveBehaviors<RotateToTargetModel>();

                    foreach (var weapon in attack.weapons)
                    {
                        if (weapon.emission.IsType<LineProjectileEmissionModel>()) continue;

                        float count = 1f;

                        weapon.projectile.pierce /= 2f;

                        if (weapon.emission.IsType<ArcEmissionModel>())
                        {
                            var arc = weapon.emission.Cast<ArcEmissionModel>();
                            count = (float)Math.Ceiling(arc.count / 2f);

                            if (Math.Abs(arc.angle - 360f) < 0.01f)
                            {
                                count /= 3f;
                                rangeFactor *= 1.5f;
                            }
                        }

                        if (weapon.emission.IsType<RandomEmissionModel>() &&
                            weapon.emission.Cast<RandomEmissionModel>().count != 1)
                        {
                            var rnd = weapon.emission.Cast<RandomEmissionModel>();
                            rnd.angle = 360f;
                            rnd.count *= 3;
                        }
                        else
                        {
                            if (!tower.name.Contains("Sentry"))
                            {
                                weapon.emission = new ArcEmissionModel("ArcEmissionModel_", (int)(6 * count), 0, 360, EmptyBehaviors, false, false);
                            }
                        }

                        weapon.animateOnMainAttack = false;
                        weapon.ejectX = 0;
                        weapon.ejectY = 0;
                    }

                    if (!tower.name.Contains("Village"))
                        attack.range *= rangeFactor / 1.5f;
                }

                if (!tower.name.Contains("Village"))
                    tower.range *= rangeFactor / 1.5f;
            }

            MelonLogger.Msg("AllTack: Makes towers projectiles fire like SuperMaelstrom.");
        }

        public override void OnUpdate()
        {
            if (ToggleKey.JustPressed())
            {
                AllTackEnabled.SetValueAndSave(!AllTackEnabled);
                MelonLogger.Msg($"AllTack: {(AllTackEnabled ? "ENABLED" : "DISABLED")}");
            }
        }
    }
}