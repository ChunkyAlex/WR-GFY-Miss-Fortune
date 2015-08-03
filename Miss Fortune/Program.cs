﻿#region

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

#endregion

namespace Miss_Fortune
{
    internal class Program
    {
        public const string ChampionName = "Miss Fortune";
        public static Orbwalking.Orbwalker Orbwalker;
        public static List<Spell> SpellList = new List<Spell>();
        public static Menu Config;
        private static readonly Obj_AI_Hero Player = ObjectManager.Player;
        private static SpellSlot Flash;
        public static Spell Q, W, E, R;
        public static float RCastTime { get; set; }

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += OnLoad;
        }

        private static void OnLoad(EventArgs args)
        {
            if (Player.ChampionName != "MissFortune") return;

            Q = new Spell(SpellSlot.Q, 650);
            Q.SetTargetted(0.29f, 1400f);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 800);
            E.SetSkillshot(0.5f, 100f, float.MaxValue, false, SkillshotType.SkillshotCircle);
            R = new Spell(SpellSlot.R, 1200);
            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);

            Config = new Menu("Washington Redskins", "Washington Redskins", true);

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            Config.AddSubMenu(new Menu("Orbwalker Settings", "Orbwalker Settings"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalker Settings"));

            //jungleclear
            Config.AddSubMenu(new Menu("Jungleclear", "Jungleclear"));
            Config.SubMenu("Jungleclear").AddItem(new MenuItem("qJungle", "Use Q").SetValue(true));
            Config.SubMenu("Jungleclear").AddItem(new MenuItem("wJungle", "Use W").SetValue(true));
            Config.SubMenu("Jungleclear").AddItem(new MenuItem("eJungle", "Use E").SetValue(true));
            Config.SubMenu("Jungleclear")
                .AddItem(
                    new MenuItem("JungleFarmActive", "JungleFarm!").SetValue(new KeyBind("V".ToCharArray()[0],
                        KeyBindType.Press)));
            //combo
            Config.AddSubMenu(new Menu("Combo Settings", "Combo Settings"));
            Config.SubMenu("Combo Settings").AddItem(new MenuItem("qCombo", "Use Q").SetValue(true));
            Config.SubMenu("Combo Settings").AddItem(new MenuItem("wCombo", "Use W").SetValue(true));
            Config.SubMenu("Combo Settings").AddItem(new MenuItem("eCombo", "Use E").SetValue(true));
            Config.SubMenu("Combo Settings").AddItem(new MenuItem("rCombo", "Use R").SetValue(true));
            Config.SubMenu("Combo Settings")
                .AddItem(new MenuItem("rComboxEnemy", "R on x Enemy").SetValue(new Slider(3, 1, 5)));
            //laneclear
            Config.AddSubMenu(new Menu("Laneclear Settings", "Laneclear Settings"));
            Config.SubMenu("Laneclear Settings").AddItem(new MenuItem("qLaneclear", "Use Q").SetValue(true));
            Config.SubMenu("Laneclear Settings").AddItem(new MenuItem("eLaneclear", "Use E").SetValue(true));
            Config.SubMenu("Laneclear Settings")
                .AddItem(new MenuItem("siegeminionstoQ", "Use Q for Siege Minions").SetValue(true));
            Config.SubMenu("Laneclear Settings")
                .AddItem(new MenuItem("clearMana", "LaneClear Mana Percent").SetValue(new Slider(30, 1)));
            //harass
            Config.AddSubMenu(new Menu("Harass Settings", "Harass Settings"));
            Config.SubMenu("Harass Settings").AddItem(new MenuItem("qHarass", "Use Q").SetValue(true));
            Config.SubMenu("Harass Settings")
                .AddItem(new MenuItem("harassMana", "Harass Mana Percent").SetValue(new Slider(30, 1)));
            //misc
            
            //draw
            Config.AddSubMenu(new Menu("Draw Settings", "Draw Settings"));
            Config.SubMenu("Draw Settings")
                .AddItem(new MenuItem("qDraw", "Q Range").SetValue(new Circle(true, Color.SpringGreen)));
            Config.SubMenu("Draw Settings")
                .AddItem(new MenuItem("eDraw", "E Range").SetValue(new Circle(true, Color.Crimson)));
            Config.SubMenu("Draw Settings")
                .AddItem(new MenuItem("rDraw", "R Range").SetValue(new Circle(true, Color.Gold)));
            Config.SubMenu("Draw Settings")
                .AddItem(new MenuItem("aaRangeDraw", "AA Range").SetValue(new Circle(true, Color.White)));


            

            Config.AddToMainMenu();
            Game.OnUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Orbwalking.AfterAttack += Orbwalking_AfterAttack;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Hero_OnProcessSpellCast;

        }

        private static void Obj_AI_Hero_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
             if (sender.IsMe && args.SData.Name == "MissFortuneBulletTime")
            {
                RCastTime = Game.Time;
                Program.debug(args.SData.Name);
                Orbwalking.Attack = false;
                Orbwalking.Move = false;
                if (Config.Item("forceBlockMove").GetValue<bool>())
                {
                    Program.Orbwalker.SetAttack(true);
                    Program.Orbwalker.SetMovement(true);
                }
            }
        }

        private static void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (!unit.IsMe || Player.IsChannelingImportantSpell() || Player.HasBuff("MissFortuneBulletTime") || Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Combo)
                return;
            if (Q.CanCast(target as Obj_AI_Hero))
            {
                Q.Cast(target as Obj_AI_Hero);
                Utility.DelayAction.Add(150, Orbwalking.ResetAutoAttackTimer);
            }
        }

        private static void debug(string p)
        {
            throw new NotImplementedException();
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (Player.HasBuff("MissFortuneBulletTime"))
            {
                Orbwalker.SetAttack(false);
                Orbwalker.SetMovement(false);
            }
            else
            {
                Orbwalker.SetAttack(true);
                Orbwalker.SetMovement(true);
            }


            switch (Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    Combo();
                    break;

                case Orbwalking.OrbwalkingMode.LaneClear:
                    LaneClear();
                    break;

                case Orbwalking.OrbwalkingMode.LastHit:
                    break;

                case Orbwalking.OrbwalkingMode.Mixed:
                    Harass();
                    break;

                case Orbwalking.OrbwalkingMode.None:
                    break;
            }
        }



        private static void Combo()
        {
            var target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);

            if (target == null) return;

            if (W.IsReady() && Config.Item("wCombo").GetValue<bool>() && target.IsValidTarget(500))
            {
                W.Cast();
            }

            //Ardud Q logic
            if (Q.IsReady())
            
            target = TargetSelector.GetTarget(Q.Range + 500, TargetSelector.DamageType.Physical);
            var allMinion = MinionManager.GetMinions(target.Position, Q.Range, MinionTypes.All, MinionTeam.NotAlly);
            if (target.IsValidTarget(Q.Range))
            {
                Q.Cast();
                
            }
            Obj_AI_Base[] nearstMinion = { null };
            foreach (var minion in
                allMinion.Where(
                    minion =>
                    minion.Distance(ObjectManager.Player) <= target.Distance(ObjectManager.Player)
                    && target.Distance(minion) < 450)
                    .Where(
                        minion =>
                        nearstMinion[0] == null
                        || minion.Distance(ObjectManager.Player) < nearstMinion[0].Distance(ObjectManager.Player)))
            {
                nearstMinion[0] = minion;
            }
            if (nearstMinion[0] != null && nearstMinion[0].IsValidTarget(Q.Range))
            {
                Q.CastOnUnit(nearstMinion[0], Q.Cast());
            }

            {


                if (E.CanCast(target) && Config.Item("eCombo").GetValue<bool>() && !Orbwalker.InAutoAttackRange(target))
                {
                    E.Cast(target);
                }

                if (R.CanCast(target) && Config.Item("rCombo").GetValue<bool>() && !Q.IsReady() && !W.IsReady()
                    && !E.IsReady())
                {
                    var slidercount = Config.Item("rComboxEnemy").GetValue<Slider>().Value;

                    R.CastIfWillHit(target, slidercount);

                    if (R.IsKillable(target) || (target.MaxHealth / target.Health) < 0.2) R.Cast(target.Position);
                }
            }
        }

        private static void Harass()
        {
            if (ObjectManager.Player.ManaPercent < Config.Item("harassMana").GetValue<Slider>().Value) return;

            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);

            if (Q.CanCast(target) && Config.Item("qHarass").GetValue<bool>())
            {
                Q.Cast(target);
            }
        }

        private static void LaneClear()
        {
            if (ObjectManager.Player.ManaPercent < Config.Item("clearMana").GetValue<Slider>().Value) return;
            var allMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range);
            var qminion = allMinions.FirstOrDefault(y => Q.IsKillable(y));

            //siegeminionstoQ
            if (Q.CanCast(qminion) && Config.Item("qLaneclear").GetValue<bool>())
            {
                Q.Cast(qminion);
            }
            if (E.IsReady() && Config.Item("eLaneclear").GetValue<bool>())
            {
                var efarm = Q.GetCircularFarmLocation(allMinions, 200);
                if (efarm.MinionsHit >= 3)
                {
                    E.Cast(efarm.Position);
                }
            }
        }

        private static void JungleFarm()
        {
            var mobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range,
                MinionTypes.All,
                MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

            if (mobs.Count > 0)
            {
                if (Config.Item("qJungle").GetValue<bool>() && Q.IsReady())
                    Q.Cast();

                if (Config.Item("wJungle").GetValue<bool>() && W.IsReady())
                    E.CastOnUnit(mobs[0]);

                if (Config.Item("eJungle").GetValue<bool>() && E.IsReady())
                    E.CastOnUnit(mobs[0]);
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            var menuItem1 = Config.Item("qDraw").GetValue<Circle>();
            var menuItem2 = Config.Item("eDraw").GetValue<Circle>();
            var menuItem3 = Config.Item("rDraw").GetValue<Circle>();

            if (menuItem1.Active && Q.IsReady()) Render.Circle.DrawCircle(Player.Position, Q.Range, Color.SpringGreen);
            if (menuItem2.Active && E.IsReady()) Render.Circle.DrawCircle(Player.Position, E.Range, Color.Crimson);

            if (menuItem3.Active && R.IsReady()) Render.Circle.DrawCircle(Player.Position, R.Range, Color.Gold);
        }

        public struct Spells
        {
            public string ChampionName;
            public string SpellName;
        }

        public static int UltTick { get; set; }
        
    }
}