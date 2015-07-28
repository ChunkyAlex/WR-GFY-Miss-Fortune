using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using LeagueSharp;
using LeagueSharp.Common;
using Color = System.Drawing.Color;


namespace Miss_Fortune
{
    internal class Program
    {
        public const string ChampionName = "Miss Fortune";

        public static Orbwalking.Orbwalker Orbwalker;

        public static List<Spell> SpellList = new List<Spell>();

        public static Menu Config;

        private static Obj_AI_Hero Player = ObjectManager.Player;

        private static SpellSlot Flash;

        public struct Spells
        {
            public string ChampionName;

            public string SpellName;

            public SpellSlot slot;
        }

        public static Spell Q, W, E, R;

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

            Config.AddSubMenu(new Menu("Combo Settings", "Combo Settings"));
            Config.SubMenu("Combo Settings").AddItem(new MenuItem("qCombo", "Use Q").SetValue(true));
            Config.SubMenu("Combo Settings").AddItem(new MenuItem("wCombo", "Use W").SetValue(true));
            Config.SubMenu("Combo Settings").AddItem(new MenuItem("eCombo", "Use E").SetValue(true));
            Config.SubMenu("Combo Settings").AddItem(new MenuItem("rCombo", "Use R").SetValue(true));
            Config.SubMenu("Combo Settings")
                .AddItem(new MenuItem("rComboxEnemy", "R on x Enemy").SetValue(new Slider(3, 1, 5)));

            Config.AddSubMenu(new Menu("Laneclear Settings", "Laneclear Settings"));
            Config.SubMenu("Laneclear Settings").AddItem(new MenuItem("qLaneclear", "Use Q").SetValue(true));
            Config.SubMenu("Laneclear Settings").AddItem(new MenuItem("eLaneclear", "Use E").SetValue(true));
            Config.SubMenu("Laneclear Settings")
                .AddItem(new MenuItem("siegeminionstoQ", "Use Q for Siege Minions").SetValue(true));
            Config.SubMenu("Laneclear Settings")
                .AddItem(new MenuItem("clearMana", "LaneClear Mana Percent").SetValue(new Slider(30, 1, 100)));

            Config.AddSubMenu(new Menu("Harass Settings", "Harass Settings"));
            Config.SubMenu("Harass Settings").AddItem(new MenuItem("qHarass", "Use Q").SetValue(true));
            Config.SubMenu("Harass Settings")
                .AddItem(new MenuItem("harassMana", "Harass Mana Percent").SetValue(new Slider(30, 1, 100)));

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
        }
        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (Player.HasBuff("MissFortuneBulletTime"))
            {
                Orbwalker.SetAttack(false);
                Orbwalker.SetMovement(false);
            }
            Orbwalking.AfterAttack += Orbwalking_AfterAttack;

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

        private static void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            throw new NotImplementedException();
        }

        private static void AfterAttack(AttackableUnit unit, AttackableUnit target)
        {

            if (!unit.IsMe)
                return;
            if (Player.IsChannelingImportantSpell()
                || Player.HasBuff("MissFortuneBulletTime"))
                return;
            if (!target.IsEnemy)
                return;
            var t = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
            if (Q.IsReady())
                Q.Cast(t);

        }


        private static void Combo()
        {
            var target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);

            if (target == null) return;

            if (W.IsReady() && Config.Item("wCombo").GetValue<bool>())
            {
                if (target.IsValidTarget(500)) W.Cast();
            }

            if (E.CanCast(target) && Config.Item("eCombo").GetValue<bool>())
            {
                if (!Orbwalker.InAutoAttackRange(target)
                    && E.GetPrediction(target).UnitPosition.Distance(Player.Position) > 650
                    && Player.Distance(target.Position) <= 800)
                {
                    E.Cast(target);
                }
            }

            if (R.CanCast(target) && Config.Item("rCombo").GetValue<bool>())
            {
                var slidercount = Config.Item("rComboxEnemy").GetValue<Slider>().Value;

                if (target.IsValidTarget(R.Range) && !Q.IsReady() && !W.IsReady() && !E.IsReady())
                {
                    R.CastIfWillHit(target, slidercount);
                    Orbwalker.SetAttack(false);
                    Orbwalker.SetMovement(false);
                }

                if (R.IsKillable(target)
                    && target.IsValidTarget(R.Range - 100) && !Q.IsReady() && !W.IsReady() && !E.IsReady()) R.Cast(target.ServerPosition);

                if ((target.MaxHealth / target.Health) < 0.2 && target.IsValidTarget(R.Range - 100) && !Q.IsReady() && !W.IsReady() && !E.IsReady()) R.Cast(target.ServerPosition);
            }
        }
       
        private static void Harass()
        {
            if (ObjectManager.Player.ManaPercent < Config.Item("harassMana").GetValue<Slider>().Value) return;

            if (Q.IsReady() && Config.Item("qHarass").GetValue<bool>())
            {
                foreach (var en in HeroManager.Enemies.Where(hero => hero.IsValidTarget(Q.Range) && !hero.IsZombie))
                {
                    if (Q.CanCast(en)) Q.Cast(en);
                }
            }
        }

        private static void LaneClear()
        {
            var allMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All);

            if (ObjectManager.Player.ManaPercent < Config.Item("clearMana").GetValue<Slider>().Value) return;
            //siegeminionstoQ
            if (Q.IsReady() && Config.Item("qLaneclear").GetValue<bool>())
            {
                foreach (var minyon in allMinions)
                {
                    if (minyon.CharData.BaseSkinName.Contains("MinionSiege") && minyon != null)
                    {
                        if (Q.IsKillable(minyon))
                        {
                            Q.CastOnUnit(minyon);
                        }
                    }
                }
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

        private static void Drawing_OnDraw(EventArgs args)
        {
            var menuItem1 = Config.Item("qDraw").GetValue<Circle>();
            var menuItem2 = Config.Item("eDraw").GetValue<Circle>();
            var menuItem3 = Config.Item("rDraw").GetValue<Circle>();

            if (menuItem1.Active && Q.IsReady()) Render.Circle.DrawCircle(Player.Position, Q.Range, Color.SpringGreen);
            if (menuItem2.Active && E.IsReady()) Render.Circle.DrawCircle(Player.Position, E.Range, Color.Crimson);

            if (menuItem3.Active && R.IsReady()) Render.Circle.DrawCircle(Player.Position, R.Range, Color.Gold);



        }

        public static float RCastTime { get; set; }
    }
}